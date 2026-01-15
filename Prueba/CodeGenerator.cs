using LLVMSharp.Interop;

public class CodeGenerator : IDisposable
{
    private bool _disposed;

    private readonly LLVMModuleRef _module;
    private readonly LLVMBuilderRef _builder;
    private readonly LLVMContextRef _context;

    private ClassNode? _currentClassNode;
    private LLVMValueRef _currentFunction;

    private readonly Dictionary<string, ClassNode> _classNodes = [];
    private readonly Dictionary<string, LLVMTypeRef> _classTypes = [];
    private readonly Dictionary<string, LLVMValueRef> _functions = [];
    private readonly Dictionary<string, LLVMTypeRef> _functionTypes = [];
    private readonly Dictionary<string, LLVMValueRef> _namedValues = [];
    private readonly Dictionary<string, LLVMTypeRef> _variableTypes = [];
    private readonly Dictionary<string, string> _variableClassNames = [];

    // Funciones del Runtime
    private LLVMValueRef _putsFunc;
    private LLVMTypeRef _putsType;
    private LLVMValueRef _scanfFunc;
    private LLVMTypeRef _scanfType;
    private LLVMValueRef _printfFunc;
    private LLVMTypeRef _printfType;
    private LLVMValueRef _mallocFunc;
    private LLVMTypeRef _mallocType;
    private LLVMValueRef _atollFunc;
    private LLVMTypeRef _atollType;
    private LLVMValueRef _atofFunc;
    private LLVMTypeRef _atofType;
    private LLVMValueRef _strcmpFunc;
    private LLVMTypeRef _strcmpType;
    private LLVMValueRef _fflushFunc;
    private LLVMTypeRef _fflushType;

    public CodeGenerator(string moduleName = "RedLangModule")
    {
        _context = LLVMContextRef.Create();
        _module = _context.CreateModuleWithName(moduleName);
        _builder = _context.CreateBuilder();

        DeclareRuntimeFunctions();
    }

    private void DeclareRuntimeFunctions()
    {
        _printfType = LLVMTypeRef.CreateFunction(_context.Int32Type, [LLVMTypeRef.CreatePointer(_context.Int8Type, 0)], true);
        _printfFunc = _module.AddFunction("printf", _printfType);

        _scanfType = LLVMTypeRef.CreateFunction(_context.Int32Type, [LLVMTypeRef.CreatePointer(_context.Int8Type, 0)], true);
        _scanfFunc = _module.AddFunction("scanf", _scanfType);

        _putsType = LLVMTypeRef.CreateFunction(_context.Int32Type, [LLVMTypeRef.CreatePointer(_context.Int8Type, 0)]);
        _putsFunc = _module.AddFunction("puts", _putsType);

        _fflushType = LLVMTypeRef.CreateFunction(_context.Int32Type, [LLVMTypeRef.CreatePointer(_context.Int8Type, 0)]);
        _fflushFunc = _module.AddFunction("fflush", _fflushType);

        _atollType = LLVMTypeRef.CreateFunction(_context.Int64Type, [LLVMTypeRef.CreatePointer(_context.Int8Type, 0)]);
        _atollFunc = _module.AddFunction("atoll", _atollType);

        _atofType = LLVMTypeRef.CreateFunction(_context.DoubleType, [LLVMTypeRef.CreatePointer(_context.Int8Type, 0)]);
        _atofFunc = _module.AddFunction("atof", _atofType);

        _strcmpType = LLVMTypeRef.CreateFunction(_context.Int32Type, [LLVMTypeRef.CreatePointer(_context.Int8Type, 0), LLVMTypeRef.CreatePointer(_context.Int8Type, 0)]);
        _strcmpFunc = _module.AddFunction("strcmp", _strcmpType);

        _mallocType = LLVMTypeRef.CreateFunction(LLVMTypeRef.CreatePointer(_context.Int8Type, 0), [_context.Int64Type]);
        _mallocFunc = _module.AddFunction("malloc", _mallocType);
    }

    public string Generate(List<ProgramNode> projectNodes)
    {
        // 1. Registrar Clases
        foreach (var program in projectNodes)
        {
            foreach (var classNode in program.Classes)
                _classNodes[classNode.ClassName] = classNode;
        }

        // 2. Definir Tipos (Structs)
        foreach (var program in projectNodes)
        {
            foreach (var classNode in program.Classes)
                DefineClassType(classNode);
        }

        // 3. Declarar Métodos (Prototipos)
        foreach (var program in projectNodes)
        {
            foreach (var classNode in program.Classes)
            {
                foreach (var member in classNode.Members)
                {
                    if (member is FunctionNode method)
                    {
                        DeclareMethod(classNode, method);
                    }
                }
            }
        }

        // 4. Generar Cuerpos de Métodos
        FunctionNode? entryMethod = null;
        ClassNode? entryClass = null;

        foreach (var program in projectNodes)
        {
            foreach (var classNode in program.Classes)
            {
                foreach (var member in classNode.Members)
                {
                    if (member is FunctionNode method)
                    {
                        if (method.IsEntry)
                        {
                            entryMethod = method;
                            entryClass = classNode;
                        }
                        GenerateMethodBody(classNode, method);
                    }
                }
            }
        }

        if (entryMethod == null || entryClass == null)
            throw new Exception("No entry method found");

        GenerateEntryPoint(entryClass, entryMethod);

        _module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        return GetIR();
    }

    private void DefineClassType(ClassNode classNode)
    {
        var classType = _context.CreateNamedStruct(classNode.ClassName);
        var fieldTypes = new List<LLVMTypeRef>();

        // Filtrar solo los campos (VariableNode)
        foreach (var member in classNode.Members)
        {
            if (member is VariableNode field)
            {
                fieldTypes.Add(MapType(field.Type));
            }
        }

        classType.StructSetBody([.. fieldTypes], false);
        _classTypes[classNode.ClassName] = classType;
    }

    private void DeclareMethod(ClassNode classNode, FunctionNode method)
    {
        var returnType = MapType(method.Type);
        var paramTypes = new List<LLVMTypeRef>();
        // Añadir el parámetro implícito 'this' (excepto para Main)
        if (!method.IsEntry)
        {
            paramTypes.Add(LLVMTypeRef.CreatePointer(_classTypes[classNode.ClassName], 0));
        }

        foreach (var param in method.Parameters!)
        {
            paramTypes.Add(MapType(param.Type));
        }

        var funcType = LLVMTypeRef.CreateFunction(returnType, [.. paramTypes], false);
        var funcName = $"{classNode.ClassName}_{method.Name}";
        
        var function = _module.AddFunction(funcName, funcType);
        _functions[funcName] = function;
        _functionTypes[funcName] = funcType;
    }

    private void GenerateMethodBody(ClassNode classNode, FunctionNode method)
    {
        var funcName = $"{classNode.ClassName}_{method.Name}";
        _currentFunction = _functions[funcName];
        _currentClassNode = classNode;

        var entryBlock = _context.AppendBasicBlock(_currentFunction, "entry");
        _builder.PositionAtEnd(entryBlock);

        _namedValues.Clear();
        _variableTypes.Clear();

        // Parámetros
        int paramOffset = method.IsEntry ? 0 : 1;
        if (method.Parameters != null)
        {
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                var paramNode = method.Parameters[i];
                var paramValue = _currentFunction.GetParam((uint)(i + paramOffset));
                paramValue.Name = paramNode.Name; 

                var paramType = MapType(paramNode.Type);
                var alloca = _builder.BuildAlloca(paramType, paramNode.Name);
                _builder.BuildStore(paramValue, alloca);

                _namedValues[paramNode.Name] = alloca;
                _variableTypes[paramNode.Name] = paramType;
                _variableClassNames[paramNode.Name] = paramNode.Type.Name;
            }
        }

        foreach (var stmt in method.Body.Statements)
        {
            GenerateStatement(stmt);
        }

        // Return void implícito
        if ((method.Type.Name == "void") && 
            (_currentFunction.LastBasicBlock.Terminator.Handle == IntPtr.Zero))
        {
            _builder.BuildRetVoid();
        }
    }

    private void GenerateStatement(StatementNode stmt)
    {
        switch (stmt)
        {
            case VariableNode decl: 
                GenerateDeclaration(decl);
                break;
            case SetNode set:
                GenerateSet(set);
                break;
            case ReturnNode ret:
                GenerateReturn(ret);
                break;
            case ExpressionStatementNode exprStmt:
                GenerateExpression(exprStmt.Expression);
                break;
            case CheckNode check:
                GenerateCheck(check);
                break;
            case LoopNode loop:
                GenerateLoop(loop);
                break;
            case RepeatNode repeat:
                GenerateRepeat(repeat);
                break;
        }
    }

    private void GenerateDeclaration(VariableNode decl)
    {
        var type = MapType(decl.Type);
        var alloca = _builder.BuildAlloca(type, decl.Name);

        if (decl.Expression != null)
        {
            var initVal = GenerateExpression(decl.Expression);
            _builder.BuildStore(initVal, alloca);
        }
        else
        {
            // Valor por defecto (0)
            _builder.BuildStore(LLVMValueRef.CreateConstInt(_context.Int64Type, 0, false), alloca);
        }

        _namedValues[decl.Name] = alloca;
        _variableTypes[decl.Name] = type;
        if (decl.Type != null) _variableClassNames[decl.Name] = decl.Type.Name;
    }

    private void GenerateSet(SetNode set)
    {
        if (set.Target is IdentifierAssignTarget idTarget)
        {
            if (_namedValues.TryGetValue(idTarget.Name, out var alloca))
            {
                var val = GenerateExpression(set.Value);
                _builder.BuildStore(val, alloca);
            }
            else if (_currentClassNode != null)
            {
                 int fieldIdx = 0;
                 foreach (var member in _currentClassNode.Members)
                 {
                     if (member is VariableNode field)
                     {
                         if (field.Name == idTarget.Name)
                         {
                             var val = GenerateExpression(set.Value);
                             var thisPtr = _currentFunction.GetParam(0);
                             var fieldPtr = _builder.BuildStructGEP2(_classTypes[_currentClassNode.ClassName], thisPtr, (uint)fieldIdx, "fieldptr");
                             _builder.BuildStore(val, fieldPtr);
                             return;
                         }
                         fieldIdx++;
                     }
                 }
                 throw new Exception($"WRITE: Variable {idTarget.Name} not declared in class {_currentClassNode?.ClassName ?? "global"}");
            }
            else
            {
                throw new Exception($"WRITE: Variable {idTarget.Name} not declared (no class context)");
            }
        }
        else if (set.Target is ArrayAssignTarget arrTarget)
        {
             if (!_namedValues.TryGetValue(arrTarget.ArrayName, out var arrayAlloca))
                throw new Exception($"Array {arrTarget.ArrayName} not found");
             
             var index = GenerateExpression(arrTarget.Index);
             var val = GenerateExpression(set.Value);
             
             var arrayVarType = _variableTypes[arrTarget.ArrayName];
             var basePtr = _builder.BuildLoad2(arrayVarType, arrayAlloca, "ptrload");
             
             // Asegurarnos que tenemos el tipo del elemento (si es i64*, el elemento es i64)
             var elemType = arrayVarType.ElementType;
             if (elemType.Handle == IntPtr.Zero) elemType = _context.Int64Type;

             var elementPtr = _builder.BuildGEP2(elemType, basePtr, new LLVMValueRef[] { index }, "elemsetptr");
             _builder.BuildStore(val, elementPtr);
        }
    }

    private void GenerateReturn(ReturnNode ret)
    {
        if (ret.Expression != null)
        {
            var val = GenerateExpression(ret.Expression);
            _builder.BuildRet(val);
        }
        else
        {
            _builder.BuildRetVoid();
        }
    }

    private void GenerateCheck(CheckNode check)
    {
        var cond = GenerateExpression(check.Condition);
        bool hasElse = check.ElseBlock != null;

        var thenBB = _context.AppendBasicBlock(_currentFunction, "then");
        var elseBB = hasElse ? _context.AppendBasicBlock(_currentFunction, "else") : default;
        var mergeBB = _context.AppendBasicBlock(_currentFunction, "merge");

        _builder.BuildCondBr(cond, thenBB, hasElse ? elseBB : mergeBB);

        // Then
        _builder.PositionAtEnd(thenBB);
        GenerateBlock(check.ThenBlock);
        if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            _builder.BuildBr(mergeBB);

        // Else
        if (hasElse)
        {
            _builder.PositionAtEnd(elseBB);
            GenerateBlock(check.ElseBlock!);
            if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
                _builder.BuildBr(mergeBB);
        }

        _builder.PositionAtEnd(mergeBB);
    }

    private void GenerateLoop(LoopNode loop)
    {
        // Init
        if (loop.Init != null)
        {
            if (loop.Init is VariableNode d) GenerateDeclaration(d);
            else if (loop.Init is SetNode s) GenerateSet(s);
        }

        var condBB = _context.AppendBasicBlock(_currentFunction, "loopCond");
        var bodyBB = _context.AppendBasicBlock(_currentFunction, "loopBody");
        var endBB = _context.AppendBasicBlock(_currentFunction, "loopEnd");

        _builder.BuildBr(condBB);

        _builder.PositionAtEnd(condBB);
        var cond = GenerateExpression(loop.Condition);
        _builder.BuildCondBr(cond, bodyBB, endBB);

        _builder.PositionAtEnd(bodyBB);
        GenerateBlock(loop.Body);
        
        GenerateSet(loop.Iteration);
        _builder.BuildBr(condBB);

        _builder.PositionAtEnd(endBB);
    }

    private void GenerateRepeat(RepeatNode repeat)
    {
        var condBB = _context.AppendBasicBlock(_currentFunction, "repeatCond");
        var bodyBB = _context.AppendBasicBlock(_currentFunction, "repeatBody");
        var endBB = _context.AppendBasicBlock(_currentFunction, "repeatEnd");

        _builder.BuildBr(condBB);

        _builder.PositionAtEnd(condBB);
        var cond = GenerateExpression(repeat.Condition);
        _builder.BuildCondBr(cond, bodyBB, endBB);

        _builder.PositionAtEnd(bodyBB);
        GenerateBlock(repeat.Body);
        _builder.BuildBr(condBB);

        _builder.PositionAtEnd(endBB);
    }

    private void GenerateBlock(BlockNode block)
    {
        foreach (var s in block.Statements) GenerateStatement(s);
    }

    private LLVMValueRef GenerateExpression(ExpressionNode expr)
    {
        switch (expr)
        {
            case LiteralNode lit:
                if (lit.Value is int i) return LLVMValueRef.CreateConstInt(_context.Int64Type, (ulong)i, true);
                if (lit.Value is long l) return LLVMValueRef.CreateConstInt(_context.Int64Type, (ulong)l, true); 
                if (lit.Value is double d) return LLVMValueRef.CreateConstReal(_context.DoubleType, d);
                if (lit.Value is string s) return _builder.BuildGlobalStringPtr(s, "str_lit"); 
                if (lit.Value is bool b) return LLVMValueRef.CreateConstInt(_context.Int1Type, b ? 1ul : 0ul, false);
                if (lit.Value == null) return LLVMValueRef.CreateConstPointerNull(LLVMTypeRef.CreatePointer(_context.Int8Type, 0));
                break;

            case IdentifierNode id:
                if (_namedValues.TryGetValue(id.Name, out var allocaVal))
                    return _builder.BuildLoad2(_variableTypes[id.Name], allocaVal, id.Name);
                
                // Buscar en campos de la clase actual
                if (_currentClassNode != null && !_currentClassNode.Members.Any(m => m is FunctionNode fn && fn.IsEntry && fn.Name == "Main"))
                {
                    int fieldIdx = 0;
                    foreach (var member in _currentClassNode.Members)
                    {
                        if (member is VariableNode field)
                        {
                            if (field.Name == id.Name)
                            {
                                var thisPtr = _currentFunction.GetParam(0);
                                var fieldPtr = _builder.BuildStructGEP2(_classTypes[_currentClassNode.ClassName], thisPtr, (uint)fieldIdx, "fieldptr");
                                return _builder.BuildLoad2(MapType(field.Type), fieldPtr, id.Name);
                            }
                            fieldIdx++;
                        }
                    }
                }
                throw new Exception($"READ: Variable {id.Name} not found in class {_currentClassNode?.ClassName ?? "global"}");

            case BinaryExpressionNode bin:
                return GenerateBinaryExpression(bin);
            
            case FuncCallNode call:
                return GenerateCall(call);

            case MemberAccessNode member:
                if (member.Call != null) 
                {
                     if (_variableClassNames.TryGetValue(member.ObjectName, out var className))
                     {
                          string staticCall = $"{className}_{member.Call.FunctionName}";
                          if (_functions.ContainsKey(staticCall))
                          {
                               var func = _functions[staticCall];
                               var funcType = _functionTypes[staticCall];
                               var args = new List<LLVMValueRef>();
                               
                               // Pasar 'this' como primer argumento
                               if (_namedValues.TryGetValue(member.ObjectName, out var objAlloca))
                               {
                                   var objPtr = _builder.BuildLoad2(_variableTypes[member.ObjectName], objAlloca, "this_ptr");
                                   args.Add(objPtr);
                               }

                               foreach(var arg in member.Call.Arguments) args.Add(GenerateExpression(arg));
                               string callName = funcType.ReturnType.Kind == LLVMTypeKind.LLVMVoidTypeKind ? "" : "calltmp";
                               return _builder.BuildCall2(funcType, func, args.ToArray(), callName);
                          }
                     }
                }
                throw new Exception($"Member access not found or not implemented for {member.ObjectName}");

            case UnaryExpressionNode unary:
                var operand = GenerateExpression(unary.Operand);
                if (unary.Operator == "not") 
                {
                    // Si es i1 lo negamos, si es i64 lo comparamos con 0
                    if (operand.TypeOf.Kind == LLVMTypeKind.LLVMIntegerTypeKind && operand.TypeOf.IntWidth == 1)
                        return _builder.BuildNot(operand, "nottmp");
                    return _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, operand, LLVMValueRef.CreateConstInt(operand.TypeOf, 0, false), "nottmp");
                }
                if (unary.Operator == "-") return _builder.BuildNeg(operand, "negtmp");
                break;

            case ArrayAccessNode arrayAcc:
                if (_namedValues.TryGetValue(arrayAcc.ArrayName, out var arrayPtr))
                {
                     var index = GenerateExpression(arrayAcc.Index);
                     var arrayVarType = _variableTypes[arrayAcc.ArrayName];
                     var basePtr = _builder.BuildLoad2(arrayVarType, arrayPtr, "arrbase");
                     
                     var elemType = arrayVarType.ElementType;
                     if (elemType.Handle == IntPtr.Zero) elemType = _context.Int64Type;

                     var elementPtr = _builder.BuildGEP2(elemType, basePtr, new LLVMValueRef[] { index }, "elemptr");
                     return _builder.BuildLoad2(elemType, elementPtr, "elemval");
                }
                throw new Exception($"READ: Array {arrayAcc.ArrayName} not found");

            case ArrayLiteralNode arrayLit:
                var elemCount = (ulong)arrayLit.Elements.Count;
                var elementLLVMValues = arrayLit.Elements.Select(GenerateExpression).ToArray();
                var firstElemType = elementLLVMValues.Length > 0 ? elementLLVMValues[0].TypeOf : _context.Int64Type;
                
                var bytesPerElem = LLVMValueRef.CreateConstInt(_context.Int64Type, 8, false); // Asumimos 64 bits/punteros
                var totalBytes = LLVMValueRef.CreateConstInt(_context.Int64Type, elemCount * 8, false);
                var rawPtr = _builder.BuildCall2(_mallocType, _mallocFunc, new LLVMValueRef[] { totalBytes }, "arrlit");
                var typedPtr = _builder.BuildBitCast(rawPtr, LLVMTypeRef.CreatePointer(firstElemType, 0), "arrtyped");

                for (int m = 0; m < arrayLit.Elements.Count; m++)
                {
                    var idx = LLVMValueRef.CreateConstInt(_context.Int64Type, (ulong)m, false);
                    var targetElemPtr = _builder.BuildGEP2(firstElemType, typedPtr, new LLVMValueRef[] { idx }, "arritertmp");
                    _builder.BuildStore(elementLLVMValues[m], targetElemPtr);
                }
                return typedPtr;
        }
        return LLVMValueRef.CreateConstInt(_context.Int64Type, 0, false);
    }

    private LLVMValueRef GenerateBinaryExpression(BinaryExpressionNode bin)
    {
        var left = GenerateExpression(bin.Left);
        var right = GenerateExpression(bin.Right);
        bool isFloat = left.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind || right.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind;

        switch (bin.Operator)
        {
            case "+": return isFloat ? _builder.BuildFAdd(left, right, "faddtmp") : _builder.BuildAdd(left, right, "addtmp");
            case "-": return isFloat ? _builder.BuildFSub(left, right, "fsubtmp") : _builder.BuildSub(left, right, "subtmp");
            case "*": return isFloat ? _builder.BuildFMul(left, right, "fmultmp") : _builder.BuildMul(left, right, "multmp");
            case "/": return isFloat ? _builder.BuildFDiv(left, right, "fdivtmp") : _builder.BuildSDiv(left, right, "divtmp"); 
            case "%": return _builder.BuildSRem(left, right, "modtmp");
            case "and": return _builder.BuildAnd(left, right, "andtmp");
            case "or": return _builder.BuildOr(left, right, "ortmp");
            case "<": return isFloat ? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, left, right, "flttmp") : _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, left, right, "lttmp");
            case ">": return isFloat ? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGT, left, right, "fgttmp") : _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, left, right, "gttmp");
            case "<=": return isFloat ? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLE, left, right, "fletmp") : _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, left, right, "letmp");
            case ">=": return isFloat ? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGE, left, right, "fgetmp") : _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, left, right, "getmp");
            case "==": 
                if (left.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
                    return _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, _builder.BuildCall2(_strcmpType, _strcmpFunc, new LLVMValueRef[] { left, right }, "strcmp"), LLVMValueRef.CreateConstInt(_context.Int32Type, 0, false), "streq");
                return isFloat ? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, left, right, "feqtmp") : _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, left, right, "eqtmp");
            case "!=": 
                return isFloat ? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, left, right, "fnetmp") : _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, left, right, "netmp");
        }
        throw new Exception($"Operator {bin.Operator} not implemented");
    }

    private LLVMValueRef GenerateCall(FuncCallNode call)
    {
        if (call.FunctionName == "print" || call.FunctionName == "show") 
        {
             var argVal = GenerateExpression(call.Arguments[0]);
             var argType = argVal.TypeOf;
             
             LLVMValueRef fmt;
             if (argType.Kind == LLVMTypeKind.LLVMPointerTypeKind) 
                fmt = _builder.BuildGlobalStringPtr("%s\n", "fmt_str");
             else if (argType.Kind == LLVMTypeKind.LLVMDoubleTypeKind)
                fmt = _builder.BuildGlobalStringPtr("%f\n", "fmt_dbl");
             else
                fmt = _builder.BuildGlobalStringPtr("%ld\n", "fmt_num");

             return _builder.BuildCall2(_printfType, _printfFunc, new [] { fmt, argVal }, "print_tmp");
        }
        
        if (call.FunctionName == "ask" || call.FunctionName == "scanf")
        {
             if (call.Arguments.Count > 0 && call.Arguments[0] is IdentifierNode id)
             {
                  if (_namedValues.TryGetValue(id.Name, out var alloca))
                  {
                      var type = _variableTypes[id.Name];
                      if (type.Kind == LLVMTypeKind.LLVMPointerTypeKind) // Es un String (i8*)
                      {
                           // Para Strings, asignamos un buffer de memoria dinámicamente con malloc
                           var size = LLVMValueRef.CreateConstInt(_context.Int64Type, 1024, false);
                           var buffer = _builder.BuildCall2(_mallocType, _mallocFunc, new LLVMValueRef[] { size }, "str_buffer");
                           _builder.BuildStore(buffer, alloca);
                           
                           var fmt = _builder.BuildGlobalStringPtr("%s", "fmt_scan_s");
                           return _builder.BuildCall2(_scanfType, _scanfFunc, new LLVMValueRef[] { fmt, buffer }, "scan_tmp");
                      }
                      else
                      {
                           var fmt = _builder.BuildGlobalStringPtr("%ld", "fmt_scan_i");
                           return _builder.BuildCall2(_scanfType, _scanfFunc, new LLVMValueRef[] { fmt, alloca }, "scan_tmp");
                      }
                  }
             }
             return _builder.BuildCall2(_scanfType, _scanfFunc, new LLVMValueRef[] { _builder.BuildGlobalStringPtr("", "dummy") }, "scan_tmp");
        }

        if (call.FunctionName == "len")
        {
             // Simulación de len(): Por ahora devolvemos un valor fijo o 0 para evitar fallos de IR
             // En una implementación real, se usarían metadatos del arreglo.
             return LLVMValueRef.CreateConstInt(_context.Int64Type, 5, false);
        }

        if (call.FunctionName == "convertToInt")
        {
             var arg = GenerateExpression(call.Arguments[0]);
             return _builder.BuildCall2(_atollType, _atollFunc, new LLVMValueRef[] { arg }, "conv_int");
        }

        if (call.FunctionName == "convertToFloat")
        {
             var arg = GenerateExpression(call.Arguments[0]);
             return _builder.BuildCall2(_atofType, _atofFunc, new LLVMValueRef[] { arg }, "conv_float");
        }

        if (call.FunctionName == "convertToBoolean")
        {
             var arg = GenerateExpression(call.Arguments[0]);
             var trueStr = _builder.BuildGlobalStringPtr("true", "true_const");
             var cmp = _builder.BuildCall2(_strcmpType, _strcmpFunc, new LLVMValueRef[] { arg, trueStr }, "boolcmp");
             return _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, cmp, LLVMValueRef.CreateConstInt(_context.Int32Type, 0, false), "is_true");
        }

        string targetName = $"{_currentClassNode?.ClassName}_{call.FunctionName}";
        if(!_functions.ContainsKey(targetName)) targetName = call.FunctionName;

        if(!_functions.ContainsKey(targetName))
        {
            // Verificamos si es un constructor
            if (_classNodes.ContainsKey(call.FunctionName))
            {
                var classType = _classTypes[call.FunctionName];
                // Alocación básica para objeto
                var size = LLVMValueRef.CreateConstInt(_context.Int64Type, 1024, false); // Tamaño arbitrario para prueba
                var ptr = _builder.BuildCall2(_mallocType, _mallocFunc, new LLVMValueRef[] { size }, "objptr");
                return _builder.BuildBitCast(ptr, LLVMTypeRef.CreatePointer(classType, 0), "objcast");
            }
            throw new Exception($"Function or Class {targetName} not found");
        }

        var func = _functions[targetName];
        var funcType = _functionTypes[targetName];
        
        var args = new List<LLVMValueRef>();
        
        // Si la función llamada es un método de la clase actual y NO es un punto de entrada (como Main)
        // debemos pasar el puntero 'this' actual (que es siempre el parámetro 0 en métodos de instancia)
        if (_currentClassNode != null && targetName.StartsWith($"{_currentClassNode.ClassName}_"))
        {
            // Verificamos que la función destino realmente espere el parámetro 'this' 
            // comparando la cantidad de argumentos esperada vs la proveída
            if (func.ParamsCount > (uint)call.Arguments.Count)
            {
                args.Add(_currentFunction.GetParam(0));
            }
        }

        foreach(var arg in call.Arguments)
            args.Add(GenerateExpression(arg));

        string cName = funcType.ReturnType.Kind == LLVMTypeKind.LLVMVoidTypeKind ? "" : "calltmp";
        return _builder.BuildCall2(funcType, func, args.ToArray(), cName);
    }

    private void GenerateEntryPoint(ClassNode entryClass, FunctionNode entryMethod)
    {
        var mainType = LLVMTypeRef.CreateFunction(_context.Int32Type, [], false);
        var mainFunc = _module.AddFunction("main", mainType);
        
        var entryBB = _context.AppendBasicBlock(mainFunc, "entry");
        _builder.PositionAtEnd(entryBB);

        string entryInfo = $"{entryClass.ClassName}_{entryMethod.Name}";
        
        if (_functions.TryGetValue(entryInfo, out var userEntry))
        {
             var userEntryType = _functionTypes[entryInfo];
             _builder.BuildCall2(userEntryType, userEntry, Array.Empty<LLVMValueRef>(), "");
        }

        _builder.BuildRet(LLVMValueRef.CreateConstInt(_context.Int32Type, 0, false));
    }

    private LLVMTypeRef MapType(TypeNode typeNode)
    {
        var type = MapType(typeNode.Name);
        if (typeNode.IsArray)
        {
            return LLVMTypeRef.CreatePointer(type, 0);
        }
        return type;
    }

    private LLVMTypeRef MapType(string type)
    {
        switch (type)
        {
            case "i": return _context.Int64Type;
            case "f": return _context.DoubleType;
            case "s": return LLVMTypeRef.CreatePointer(_context.Int8Type, 0);
            case "b": return _context.Int1Type;
            case "void": return _context.VoidType;
        }

        if (_classTypes.TryGetValue(type, out var classType))
        {
            return LLVMTypeRef.CreatePointer(classType, 0);
        }

        throw new Exception($"Unknown type: {type}");
    }

    public void WriteToFile(string path) => _module.PrintToFile(path);
    public string GetIR() => _module.ToString();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool _)
    {
        if (_disposed) return;
        _builder.Dispose();
        _module.Dispose();
        _context.Dispose();
        _disposed = true;
    }
}
