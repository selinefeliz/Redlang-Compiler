public class SemanticAnalyzer
{
    //AGREGADO PARA IMPRIMIR TABLA DE SIMBOLOS
    public SymbolTable GlobalScope => _globalScope;
    // Scope tracking
    private ClassSymbol _currentClass = null!;

    private SymbolTable _currentScope;
    private readonly SymbolTable _globalScope = new();

    // Return tracking
    private bool _hasReturn = false;
    private bool _returnedNull = false;
    private bool _returnNullType = false;
    private string? _expectedReturnType;

    // Entry method tracking
    private ClassSymbol? _entryClass;
    private FunctionSymbol? _entryMethod;

    // Numeric type ranking for widening conversions
    // private static readonly Dictionary<string, int> NumericRank = [];
    private static readonly Dictionary<string, int> NumericRank = new();


    // Constructor
    public SemanticAnalyzer()
    {
        NumericRank["i"] = 1;
        NumericRank["f"] = 2;

        _currentScope = _globalScope;

        // Initialize built-in functions
        RegisterBuiltInFunctions();
    }

    // Methods
    // Register built-in functions (globally accessible)
    private void RegisterBuiltInFunctions()
    {
        // len(array) : i
        _globalScope.Add(new FunctionSymbol
        {
            Name = "len",
            ReturnType = "i",
            // Parameters = [new VariableSymbol { Name = "arr", Type = "any", IsArray = true }]
            Parameters = new List<VariableSymbol> { new VariableSymbol { Name = "arr", Type = "any", IsArray = true } }
        });

        // convertToInt(value) : i
        _globalScope.Add(new FunctionSymbol
        {
            Name = "convertToInt",
            ReturnType = "i",
            Parameters = [new VariableSymbol { Name = "value", Type = "any" }]
        });

        // convertToFloat(value) : f
        _globalScope.Add(new FunctionSymbol
        {
            Name = "convertToFloat",
            ReturnType = "f",
            Parameters = [new VariableSymbol { Name = "value", Type = "any" }]
        });

        // convertToBool(value) : b
        _globalScope.Add(new FunctionSymbol
        {
            Name = "convertToBool",
            ReturnType = "b",
            Parameters = [new VariableSymbol { Name = "value", Type = "any" }]
        });

        // TODO -AGREGADO- : Add more or remove built-in functions as needed
        // show(...) and ask(...) are treated as built-ins that return void
        _globalScope.Add(new FunctionSymbol
        {
            Name = "show",
            ReturnType = "void",
            Parameters = new List<VariableSymbol> { new VariableSymbol { Name = "value", Type = "any" } }
        });
        _globalScope.Add(new FunctionSymbol
        {
            Name = "ask",
            ReturnType = "void",
            Parameters = new List<VariableSymbol> { new VariableSymbol { Name = "dest", Type = "any" } }
        });
    }

    // Analyze AST (Semantic Analysis)
    // This is the method to call to start semantic analysis on the AST
    // The 'ProgramNode' represents the root of the AST, substitute with your actual AST root node type as needed
    public void Analyze(ProgramNode program)
    {
        // Validate imports
        foreach (var useNode in program.Uses)
        {
            // TODO : Implement 'use' semantic analysis
            // TODO : Verify if imported class exists
            // TODO : Import symbols from the imported class into the current scope
            // TODO : Leave as is for now
            Console.WriteLine($"Importing class: {useNode.ClassName}");
        }

        // Register all classes into the global scope
        // First pass: Register ClassSymbols for all classes. This allows for forward declarations and checks for duplicate class names.
        foreach (var classNode in program.Classes)
        {
            if (_globalScope.Lookup(classNode.ClassName) != null)
                throw new SemanticException($"class '{classNode.ClassName}' is already defined.");
            var classSym = new ClassSymbol
            {
                Name = classNode.ClassName,
                ClassScope = new SymbolTable(_globalScope),
                Fields = new Dictionary<string, VariableSymbol>(),
                Methods = new Dictionary<string, FunctionSymbol>()
            };

            _globalScope.Add(classSym);
        }

        // Register class members (fields and methods) for each class
        // Second pass: Populate ClassSymbols with their members.
        foreach (var classNode in program.Classes) RegisterClassMembers(classNode);
        // Third pass: analyze definitions (bodies) using the populated symbol tables
        foreach (var classNode in program.Classes)
            AnalyzeClassDefinitions(classNode);
        // Verify if an entry method exists
        if (_entryMethod == null || _entryClass == null)
            throw new SemanticException("no entry method found, can't execute program.");
        // Validate that the entry class only has the entry method
        string entryVarErrMsg = _entryClass.Fields.Count > 0 ? $"{_entryClass.Fields.Count} fields" : "";
        string entryFuncErrMsg = _entryClass.Methods.Count > 1 ? $"{_entryClass.Methods.Count} methods" : "";

        if (!string.IsNullOrEmpty(entryVarErrMsg) || !string.IsNullOrEmpty(entryFuncErrMsg))
        {
            string connector = !string.IsNullOrEmpty(entryVarErrMsg) && !string.IsNullOrEmpty(entryFuncErrMsg) ? " and " : "";

            throw new SemanticException($"the entry class \"{_entryClass.Name}\" contains the entry method but has {entryFuncErrMsg}{connector}{entryVarErrMsg}. The entry class can only contain the entry method.");
        }

        // if (symbol.Nullable) type += "?";

        // return type;
    }
    // Semantic analysis visitors
    // TODO : Implement semantic analysis methods for various AST nodes, in order to determinante semantic correctness
    private void RegisterClassMembers(ClassNode classNode)
    {
        var sym = _globalScope.Lookup(classNode.ClassName) as ClassSymbol;
        if (sym == null) throw new SemanticException($"internal: class symbol for '{classNode.ClassName}' not found in global scope.");

        // Set current class for convenience
        // but don't change _currentScope permanently here
        var previousScope = _currentScope;
        _currentClass = sym;
        _currentScope = sym.ClassScope;

        foreach (var member in classNode.Members)
        {
            switch (member)
            {
                case VariableNode v:
                    // Determine type string
                    string vtype = v.Type != null ? GetTypeString(v.Type) : throw new SemanticException($"field '{v.Name}' must declare a type.");
                    var varSym = new VariableSymbol
                    {
                        Name = v.Name,
                        Type = vtype,
                        Nullable = v.Type?.IsNullable ?? false,
                        IsArray = v.Type?.IsArray ?? false,
                        ArraySize = v.Type?.ArraySize is LiteralNode lit && lit.Value is long iv ? (int?)iv : null
                    };

                    // Add to class symbol dictionaries and scope
                    if (sym.Fields.ContainsKey(varSym.Name))
                        throw new SemanticException($"field '{varSym.Name}' already defined in class '{sym.Name}'.");

                    sym.Fields[varSym.Name] = varSym;
                    sym.ClassScope.Add(varSym);
                    break;

                case FunctionNode fn:
                    // Return type
                    string retType = fn.Type != null ? GetTypeString(fn.Type) : "void";
                    var funcSym = new FunctionSymbol
                    {
                        Name = fn.Name,
                        ReturnType = retType,
                        NullableType = fn.Type?.IsNullable ?? false,
                        IsEntry = fn.IsEntry,
                        ParentClass = sym
                    };

                    // Parameters
                    if (fn.Parameters != null)
                    {
                        foreach (var p in fn.Parameters)
                        {
                            var pType = GetTypeString(p.Type);

                            // Detectar parámetros duplicados
                            if (funcSym.Parameters.Any(x => x.Name == p.Name))
                                throw new SemanticException($"parameter '{p.Name}' is already declared in method '{fn.Name}'.");

                            funcSym.Parameters.Add(new VariableSymbol
                            {
                                Name = p.Name,
                                Type = pType,
                                Nullable = p.Type.IsNullable,
                                IsArray = p.Type.IsArray
                            });
                        }
                    }

                    if (sym.Methods.ContainsKey(funcSym.Name))
                        throw new SemanticException($"method '{funcSym.Name}' already defined in class '{sym.Name}'.");

                    sym.Methods[funcSym.Name] = funcSym;
                    // Also add as symbol to class scope so calls like "suma(...)" inside the class resolve
                    sym.ClassScope.Add(funcSym);

                    // If it's entry, remember it (only one allowed)
                    if (funcSym.IsEntry)
                    {
                        if (_entryMethod != null)
                            throw new SemanticException("multiple entry methods found.");
                        _entryMethod = funcSym;
                        _entryClass = sym;
                    }
                    break;

                default:
                    throw new SemanticException($"unknown member type in class '{classNode.ClassName}'.");
            }
        }

        _currentScope = previousScope;
    }
    // Analyze class members: check initializers, method bodies, etc.
    private void AnalyzeClassDefinitions(ClassNode classNode)
    {
        var classSym = _globalScope.Lookup(classNode.ClassName) as ClassSymbol;
        if (classSym == null) throw new SemanticException($"class '{classNode.ClassName}' symbol not found.");

        _currentClass = classSym;
        // Start analyzing inside the class scope
        var prevScope = _currentScope;
        _currentScope = classSym.ClassScope;

        foreach (var member in classNode.Members)
        {
            switch (member)
            {
                case VariableNode v:
                    // If there is an initializer, check compatibility
                    if (v.Expression != null)
                    {
                        var exprType = EvaluateExpression(v.Expression);
                        var fieldSym = classSym.Fields[v.Name];
                        var targetType = fieldSym.Type;
                        if (!AreTypesCompatible(targetType, fieldSym.Nullable, fieldSym.IsArray, exprType))
                            throw new SemanticException($"cannot assign value of type '{exprType}' to field '{v.Name}' of type '{FormatType(fieldSym)}'.");
                    }
                    break;

                case FunctionNode fn:
                    // Find function symbol
                    if (!classSym.Methods.TryGetValue(fn.Name, out var funcSym))
                        throw new SemanticException($"internal: method symbol '{fn.Name}' not found in class '{classSym.Name}'.");

                    AnalyzeMethodBody(funcSym, fn);
                    break;

                default:
                    throw new SemanticException($"unknown member type in class '{classNode.ClassName}'.");
            }
        }

        _currentScope = prevScope;
    }

    // Analyze a function body providing the corresponding FunctionSymbol
    private void AnalyzeMethodBody(FunctionSymbol funcSym, FunctionNode fnNode)
    {
        // Save previous scope and set up a fresh scope for method (child of class scope)
        var previousScope = _currentScope;
        _currentScope = new SymbolTable(_currentClass.ClassScope);

        // Register parameters into method scope
        foreach (var param in funcSym.Parameters)
        {
            // check duplicate param names
            if (_currentScope.Lookup(param.Name) != null && _currentScope.Symbols.ContainsKey(param.Name))
                throw new SemanticException($"parameter '{param.Name}' is already defined in method '{funcSym.Name}'.");

            _currentScope.Add(param);
        }

        // Setup return expectation
        _expectedReturnType = funcSym.ReturnType;
        _hasReturn = false;
        _returnedNull = false;
        _returnNullType = false;
        _returnNullType = funcSym.NullableType;
        // Analyze statements in method body
        if (fnNode.Body != null)
        {
            foreach (var stmt in fnNode.Body.Statements)
            {
                VisitStatement(stmt);
            }
        }

        // If function expects a non-void return but no return found, error
        if (_expectedReturnType != "void" && !_hasReturn)
        {
            throw new SemanticException($"method '{funcSym.Name}' does not return a value of expected type '{_expectedReturnType}'.");
        }

        // Restore scope
        _currentScope = previousScope;
    }
    // Visitors for statements and nodes
    private void VisitStatement(AstNode node)
    {
        switch (node)
        {
            case VariableNode v:
                VisitDeclaration(v);
                break;
            case SetNode s:
                VisitAssignment(s);
                break;
            case ReturnNode r:
                VisitReturn(r);
                break;
            case ExpressionStatementNode es:
                // Evaluate expression for side-effects and type-check builtin calls
                EvaluateExpression(es.Expression);
                break;
            case CheckNode ck:
                VisitCheck(ck);
                break;
            case LoopNode lp:
                VisitLoop(lp);
                break;
            case RepeatNode rp:
                VisitRepeat(rp);
                break;
            default:
                throw new SemanticException($"unsupported statement type '{node.GetType().Name}'.");
        }
    }
    private void VisitDeclaration(VariableNode v)
    {
        // variable declaration inside a method/block
        var varType = v.Type != null ? GetTypeString(v.Type) : throw new SemanticException($"local variable '{v.Name}' must declare a type.");
        var varSymbol = new VariableSymbol
        {
            Name = v.Name,
            Type = varType,
            Nullable = v.Type.IsNullable,
            IsArray = v.Type.IsArray,
            ArraySize = v.Type.ArraySize is LiteralNode ln && ln.Value is long iv ? (int?)iv : null
        };

        // Check duplicates in the same scope
        if (_currentScope.Symbols.ContainsKey(varSymbol.Name))
            throw new SemanticException($"variable '{varSymbol.Name}' is already declared in this scope.");
        var existingParam = _currentScope.Lookup(varSymbol.Name) as VariableSymbol;
        if (existingParam != null && existingParam != null && existingParam.Type != null)
            throw new SemanticException($"local variable '{varSymbol.Name}' conflicts with an existing parameter.");

        var type = ResolveType(v.Type);
        // If initializer present, evaluate and check compatibility
        if (v.Expression != null)
        {
            var exprType = EvaluateExpression(v.Expression);

            // null check
            if (exprType == "null" && !varSymbol.Nullable)
                throw new SemanticException($"variable '{v.Name}' is not nullable but received null.");

            if (!AreTypesCompatible(type.Type, type.Nullable, type.IsArray, exprType))
                throw new SemanticException($"cannot assign value of type '{exprType}' to variable '{varSymbol.Name}' of type '{FormatType(varSymbol)}'.");
        }

        // Add to scope
        _currentScope.Add(varSymbol);
    }
    private void VisitAssignment(SetNode set)
    {
        // Resolve target type
        string targetType;
        bool targetNullable = false;
        bool targetIsArray = false;

        switch (set.Target)
        {
            case IdentifierAssignTarget idt:
                var sym = _currentScope.Lookup(idt.Name) as VariableSymbol;
                if (sym == null)
                    throw new SemanticException($"variable '{idt.Name}' is not declared.");
                targetType = sym.Type;
                targetNullable = sym.Nullable;
                targetIsArray = sym.IsArray;
                break;

            case ArrayAssignTarget aat:
                var arrSym = _currentScope.Lookup(aat.ArrayName) as VariableSymbol;
                if (arrSym == null)
                    throw new SemanticException($"array '{aat.ArrayName}' is not declared.");
                if (!arrSym.IsArray) throw new SemanticException($"'{aat.ArrayName}' is not an array.");
                targetType = arrSym.Type;
                targetNullable = arrSym.Nullable;
                targetIsArray = false; // assigning to element, element is not array
                                       // Check index is numeric and integer
                var idxType = EvaluateExpression(aat.Index);
                if (!IsNumericType(idxType))
                    throw new SemanticException($"array index must be numeric (got '{idxType}').");
                break;

            case MemberAssignTarget mat:
                // find object variable and then its member
                var objSym = _currentScope.Lookup(mat.ObjectName) as VariableSymbol;
                if (objSym == null)
                    throw new SemanticException($"object '{mat.ObjectName}' is not declared.");
                var classSym = _globalScope.Lookup(objSym.Type) as ClassSymbol;
                if (classSym == null)
                    throw new SemanticException($"type '{objSym.Type}' is not a class.");
                if (!classSym.Fields.TryGetValue(mat.MemberName, out var memberField))
                    throw new SemanticException($"class '{classSym.Name}' does not contain field '{mat.MemberName}'.");
                targetType = memberField.Type;
                targetNullable = memberField.Nullable;
                targetIsArray = memberField.IsArray;
                break;

            default:
                throw new SemanticException($"unsupported assign target '{set.Target.GetType().Name}'.");
        }

        // Evaluate value expression
        var valueType = EvaluateExpression(set.Value);

        if (valueType == "null")
        {
            if (!targetNullable)
                throw new SemanticException($"cannot assign null to non-nullable variable '{targetType}'.");
            return;
        }

        if (!AreTypesCompatible(targetType, targetNullable, targetIsArray, valueType))
            throw new SemanticException($"cannot assign value of type '{valueType}' to target of type '{(targetIsArray ? targetType + "[]" : targetType)}{(targetNullable ? "?" : "")}'.");
    }
    private void VisitReturn(ReturnNode ret)
    {
        if (ret.Expression == null)
        {
            // returning nothing => only valid if expected is void or nullable?
            if (_expectedReturnType != "void")
            {
                // If expected type is nullable maybe allow null, but here there is no expression — treat as error
                throw new SemanticException($"return statement without value in function expecting '{_expectedReturnType}'.");
            }
            _hasReturn = true;
            return;
        }

        var exprType = EvaluateExpression(ret.Expression);

        if (_expectedReturnType == null)
            throw new SemanticException("internal: expected return type not set.");

        // Handle returning null
        if (exprType == "null")
        {
            // null only valid if method return type allows null
            if (!_returnNullType)
                throw new SemanticException($"cannot return null for non-nullable return type '{_expectedReturnType}'.");
            _returnedNull = true;
            _hasReturn = true;
            return;
        }
        else
        {
            // plain compatibility check
            // expectedReturnType might be like "i" or class name
            // if expected is array or nullable, it's handled by AreTypesCompatible via passing flags
            bool expNullable = _returnNullType; // metadata from AnalyzeMethodBody
            bool expArray = false; // if you track array-return in FunctionSymbol you could set this accordingly

            if (!AreTypesCompatible(_expectedReturnType, expNullable, expArray, exprType))
                throw new SemanticException($"return type mismatch: cannot convert '{exprType}' to '{_expectedReturnType}'.");

        }

        _hasReturn = true;
    }
    private void VisitCheck(CheckNode ck)
    {
        var condType = EvaluateExpression(ck.Condition);
        if (condType != "b") throw new SemanticException("condition expression must be boolean.");

        // Execute then block in new scope
        var prevScope = _currentScope;
        _currentScope = new SymbolTable(prevScope);
        foreach (var s in ck.ThenBlock.Statements) VisitStatement(s);
        _currentScope = prevScope;

        if (ck.ElseBlock != null)
        {
            prevScope = _currentScope;
            _currentScope = new SymbolTable(prevScope);
            foreach (var s in ck.ElseBlock.Statements) VisitStatement(s);
            _currentScope = prevScope;
        }
    }
    private void VisitLoop(LoopNode loop)
    {
        // LoopInit may be a VariableNode (decl) or assignment (SetNode)
        var prevScope = _currentScope;
        _currentScope = new SymbolTable(prevScope);

        if (loop.Init != null)
        {
            switch (loop.Init)
        {
            case VariableNode v:
                VisitDeclaration(v);  // agrega la variable al scope actual
                break;
            case SetNode s:
                VisitAssignment(s);   // si es asignación, debe existir antes
                break;
            default:
                throw new SemanticException("unsupported loop init node.");
        }
        }

        var condType = EvaluateExpression(loop.Condition);
        if (condType != "b") throw new SemanticException("loop condition must be boolean.");

        // iteration is a SetNode
        if (loop.Iteration != null)
            VisitAssignment(loop.Iteration);

        // body
        foreach (var s in loop.Body.Statements) VisitStatement(s);

        _currentScope = prevScope;
    }
    private void VisitRepeat(RepeatNode rp)
    {
        var prevScope = _currentScope;
        _currentScope = new SymbolTable(prevScope);

        var condType = EvaluateExpression(rp.Condition);
        if (condType != "b") throw new SemanticException("repeat/while condition must be boolean.");

        foreach (var s in rp.Body.Statements) VisitStatement(s);

        _currentScope = prevScope;
    }
    // Expression evaluator: returns a type string for the expression.
    // Examples: "i", "f", "s", "b", "MyClass", "i[]", "unknown[]", "null", "void"
    private string EvaluateExpression(ExpressionNode expr)
    {
        switch (expr)
        {
            case LiteralNode lit:
                if (lit.Value == null) return "null";
                if (lit.Value is long) return "i";
                if (lit.Value is double) return "f";
                if (lit.Value is bool) return "b";
                if (lit.Value is string) return "s";
                return "unknown";

            case IdentifierNode id:
                var sym = _currentScope.Lookup(id.Name);
                if (sym == null) throw new SemanticException($"identifier '{id.Name}' is not declared.");
                if (sym is VariableSymbol vs) return vs.IsArray ? vs.Type + "[]" : vs.Type;
                if (sym is FunctionSymbol) return "func";
                if (sym is ClassSymbol) return sym.Name;
                throw new SemanticException($"identifier '{id.Name}' has unsupported symbol type.");

            case ArrayAccessNode aa:
                var arrSym = _currentScope.Lookup(aa.ArrayName) as VariableSymbol;
                if (arrSym == null) throw new SemanticException($"array '{aa.ArrayName}' is not declared.");
                if (!arrSym.IsArray) throw new SemanticException($"'{aa.ArrayName}' is not an array.");
                var idxType = EvaluateExpression(aa.Index);
                if (!IsNumericType(idxType)) throw new SemanticException("array index must be numeric.");
                return arrSym.Type; // element type

            case ArrayLiteralNode al:
                if (al.Elements.Count == 0) return "unknown[]";
                var firstType = EvaluateExpression(al.Elements[0]);
                for (int i = 1; i < al.Elements.Count; i++)
                {
                    var t = EvaluateExpression(al.Elements[i]);
                    if (!AreTypesCompatible(firstType, false, firstType.EndsWith("[]"), t))
                        throw new SemanticException("array literal elements must have compatible types.");
                }
                return firstType + "[]";

            case FuncCallNode fc:
                // Direct function call (global or in current scope)
                // First try to find a function in current scope
                var fSym = _currentScope.Lookup(fc.FunctionName) as FunctionSymbol
                           ?? _globalScope.Lookup(fc.FunctionName) as FunctionSymbol;
                if (fSym != null)
                {
                    // Check args count and types
                    if (fSym.Parameters.Count != fc.Arguments.Count)
                        throw new SemanticException($"function '{fSym.Name}' expects {fSym.Parameters.Count} args but got {fc.Arguments.Count}.");

                    for (int i = 0; i < fc.Arguments.Count; i++)
                    {
                        var argType = EvaluateExpression(fc.Arguments[i]);
                        var p = fSym.Parameters[i];
                        if (!AreTypesCompatible(p.Type, p.Nullable, p.IsArray, argType))
                            throw new SemanticException($"argument {i + 1} of function '{fSym.Name}' expects type '{FormatType(p)}' but got '{argType}'.");
                    }

                    return fSym.ReturnType;
                }

                // Not found: it might be a call on a class factory (ClassName() constructor) or unresolved
                // If function name matches a class name, interpret as constructor returning class instance
                var clsSym = _globalScope.Lookup(fc.FunctionName) as ClassSymbol;
                if (clsSym != null)
                {
                    // constructors: we don't check parameters here; return the class type
                    return clsSym.Name;
                }

                throw new SemanticException($"function '{fc.FunctionName}' is not declared.");

            case MemberAccessNode ma:
                // object.member OR object.call(...)
                var objectSym = _currentScope.Lookup(ma.ObjectName) as VariableSymbol;
                if (objectSym == null) throw new SemanticException($"object '{ma.ObjectName}' is not declared.");
                var objectClassSym = _globalScope.Lookup(objectSym.Type) as ClassSymbol;
                if (objectClassSym == null) throw new SemanticException($"type '{objectSym.Type}' is not a class.");
                if (ma.MemberName != null)
                {
                    if (!objectClassSym.Fields.TryGetValue(ma.MemberName, out var ffield))
                        throw new SemanticException($"class '{objectClassSym.Name}' does not contain field '{ma.MemberName}'.");
                    return ffield.IsArray ? ffield.Type + "[]" : ffield.Type;
                }
                else if (ma.Call != null)
                {
                    var call = ma.Call;
                    // resolve method in class
                    if (!objectClassSym.Methods.TryGetValue(call.FunctionName, out var methodSym))
                        throw new SemanticException($"method '{call.FunctionName}' not found in class '{objectClassSym.Name}'.");
                    // check args
                    if (methodSym.Parameters.Count != call.Arguments.Count)
                        throw new SemanticException($"method '{methodSym.Name}' expects {methodSym.Parameters.Count} args but got {call.Arguments.Count}.");
                    for (int i = 0; i < call.Arguments.Count; i++)
                    {
                        var argType = EvaluateExpression(call.Arguments[i]);
                        var p = methodSym.Parameters[i];
                        if (!AreTypesCompatible(p.Type, p.Nullable, p.IsArray, argType))
                            throw new SemanticException($"argument {i + 1} of method '{methodSym.Name}' expects type '{FormatType(p)}' but got '{argType}'.");
                    }
                    return methodSym.ReturnType;
                }
                throw new SemanticException($"invalid member access in '{ma.ObjectName}'.");

            case BinaryExpressionNode be:
                var left = EvaluateExpression(be.Left);
                var right = EvaluateExpression(be.Right);
                string op = be.Operator;

                // Logical ops
                if (op == "and" || op == "or")
                {
                    if (left != "b" || right != "b") throw new SemanticException("logical operators require boolean operands.");
                    return "b";
                }

                // Relational / comparison
                if (new[] { "==", "!=", ">=", "<=", ">", "<", "===", "!==" }.Contains(op))
                {
                    // allow numeric or same-type comparisons
                    if (IsNumericType(left) && IsNumericType(right)) return "b";
                    if (left == right) return "b";
                    throw new SemanticException($"cannot compare types '{left}' and '{right}'.");
                }

                // Add/Sub/Mul/Div/Mod -> numeric
                if (new[] { "+", "-", "*", "/", "%" }.Contains(op))
                {
                    // allow strings on + for concatenation
                    if (op == "+" && (left == "s" || right == "s"))
                    {
                        return "s";
                    }

                    if (!IsNumericType(left) || !IsNumericType(right)) throw new SemanticException($"operator '{op}' requires numeric operands.");

                    // widening rule: choose higher rank
                    NumericRank.TryGetValue(left.TrimEnd('[', ']'), out var lrank);
                    NumericRank.TryGetValue(right.TrimEnd('[', ']'), out var rrank);
                    if (lrank >= rrank) return left;
                    return right;
                }

                throw new SemanticException($"unsupported binary operator '{op}'.");

            case UnaryExpressionNode ue:
                var operandType = EvaluateExpression(ue.Operand);
                if (ue.Operator == "-")
                {
                    if (!IsNumericType(operandType)) throw new SemanticException("unary '-' requires a numeric operand.");
                    return operandType;
                }
                if (ue.Operator == "not")
                {
                    if (operandType != "b") throw new SemanticException("operator 'not' requires a boolean operand.");
                    return "b";
                }
                throw new SemanticException($"unsupported unary operator '{ue.Operator}'.");

            default:
                throw new SemanticException($"unsupported expression node '{expr.GetType().Name}'.");
        }
    }
    // Helper instance methods
    private Symbol CheckMemberExists(string name) => _currentScope.Lookup(name) ?? throw new SemanticException($"\"{name}\" is not declared.");

    // Helper static methods
    private static string GetTypeString(TypeNode typeNode)
    {
        return typeNode.IsArray ? (typeNode.Name) : (typeNode.Name);
    }

    private static string FormatType(VariableSymbol symbol)
    {
        var type = symbol.Type;

        if (symbol.IsArray) type += "[]";
        if (symbol.Nullable) type += "?";

        return type;
    }
    private (string Type, bool Nullable, bool IsArray) ResolveType(TypeNode node)
    {
        bool nullable = node.IsNullable;
        bool isArray = node.IsArray;

        string baseType = node.Name;

        if (!IsPrimitiveType(baseType) && _globalScope.Lookup(baseType) is not ClassSymbol)
            throw new SemanticException($"Unknown type \"{baseType}\".");

        return (baseType, nullable, isArray);
    }
    private static bool IsPrimitiveType(string type) => type is "i" or "f" or "s" or "b";

    private static bool IsNumericType(string type) => type is "i" or "f";

    private static bool AreTypesCompatible(string targetType, bool targetNullable, bool targetIsArray, string sourceType)
    {
        // Handle null
        if (sourceType == "null") return targetNullable;

        // Handle emptry arrays
        if (sourceType == "unknown[]" && targetIsArray) return true;

        // Handle 'any' type ---AGREGADO
        if (targetType == "any") return true;

        // Extract base type and check array compatibility
        bool sourceIsArray = sourceType.EndsWith("[]");
        string sourceBase = sourceIsArray ? sourceType.TrimEnd('[', ']') : sourceType;

        if (targetIsArray != sourceIsArray) return false;

        // Same base types
        if (targetType == sourceBase) return true;

        // Numeric widening (i -> f)
        // Integer can be assigned to float, but not vice versa
        if (NumericRank.TryGetValue(sourceBase, out var sRank) && NumericRank.TryGetValue(targetType, out var tRank) && sRank <= tRank) return true;

        return false;
    }

}

