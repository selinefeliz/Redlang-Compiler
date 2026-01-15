using System.Diagnostics.CodeAnalysis;
//AQUI PONER TODOS LOS VISITANTES
public class AstBuilderVisitor : RedlangBaseVisitor<AstNode>
{
    //para poder parciar el codigo que se esta utilizando en este momento
    //metodo
    public override AstNode VisitProgram([NotNull] RedlangParser.ProgramContext context) //Redlang.ProgramContext context
    {
        var prog = new ProgramNode();
        // la regla: program : (use_stmt | clase_decl)* EOF;
        foreach (var stmt in context.children)
        {
            //por cada sentencia creamos un nuevo nodo
            //hacer esto con todos los demas
            var node = Visit(stmt); //solo llamar visit cuando tenga otra expresion que tenga que ver con eso, con esto rcorre todo lo de use y lo de class
            if (node is UseNode s) prog.Uses.Add(s);
            if (node is ClassNode c) prog.Classes.Add(c);
        }
        return prog;
    }
    //para el use
    public override AstNode VisitUse_stmt([NotNull] RedlangParser.Use_stmtContext context)
    {
        var use = new UseNode
        {
            ClassName = context.IDENT().GetText(),  //Ident es lo que el pone en la declaracion de use
            Line = context.Start.Line,
            Column = context.Start.Column

        };
        return use;

    }

    //class
    public override AstNode VisitClase_decl([NotNull] RedlangParser.Clase_declContext context)
    {
        var objClass = new ClassNode
        {
            ClassName = context.IDENT().GetText(),
            Line = context.Start.Line,
            Column = context.Start.Column,
        };
        //donde sea que este )* es un foreach
        // classMember*
        foreach (var memberCtx in context.classMember())
        {
            //por cada sentencia creamos un nuevo nodo
            //hacer esto con todos los demas
            //visit es para visitar cada parte de lo que este
            var node = Visit(memberCtx);
            if (node is DeclarationNode decl) objClass.Members.Add(decl);
            //objClass.Members.Add((DeclarationNode)node);
        }

        //para verificar que tipo de declaracion tengo
        //context.children
        return objClass;

    }
    //para classMember : declare_stmt | func_decl | entry_func_decl;
    public override AstNode VisitClassMember([NotNull] RedlangParser.ClassMemberContext context)
    {
        if (context.declare_stmt() != null) return Visit(context.declare_stmt());
        if (context.func_decl() != null) return Visit(context.func_decl());
        if (context.entry_func_decl() != null) return Visit(context.entry_func_decl());

        return base.VisitClassMember(context);
    }
    public override AstNode VisitDeclare_stmt([NotNull] RedlangParser.Declare_stmtContext context)
    {
        var variable = new VariableNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Name = context.IDENT(0).GetText() // Primer IDENT es el nombre
        };

        // COLON (data_type | IDENT?)
        if (context.data_type() != null)
        {
            variable.Type = (TypeNode)Visit(context.data_type());
        }
        else if (context.IDENT().Length > 1) // Segundo IDENT como tipo personalizado
        {
            variable.Type = new TypeNode
            {
                Name = context.IDENT(1).GetText(),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        // (EQUAL expression)?
        if (context.expression() != null)
        {
            variable.Expression = (ExpressionNode)Visit(context.expression());
        }

        return variable;
    }

    //Asignacion (set)
    public override AstNode VisitSet_stmt([NotNull] RedlangParser.Set_stmtContext context)
    {
        //set_stmt : SET assign_target EQUAL expression SEMI_COLON;
        return new SetNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Target = (AssignTargetNode)Visit(context.assign_target()),
            Value = (ExpressionNode)Visit(context.expression())
        };
    }
    public override AstNode VisitAssign_target([NotNull] RedlangParser.Assign_targetContext context)
    {
        // assign_target : IDENT | array_access | member_access ;
        if (context.IDENT() != null && context.array_access() == null && context.member_access() == null)
        {
            return new IdentifierAssignTarget
            {
                Name = context.IDENT().GetText(),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }
        //array_access : IDENT O_BRACKETS expression C_BRACKETS;
        if (context.array_access() != null)
        {
            var aa = context.array_access();
            return new ArrayAssignTarget
            {
                ArrayName = aa.IDENT().GetText(),
                Index = (ExpressionNode)Visit(aa.expression()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }
        if (context.member_access() != null)
        {
            var ma = context.member_access();

            // IDENT DOT IDENT
            if (ma.IDENT().Length > 1)
            {
                return new MemberAssignTarget
                {
                    ObjectName = ma.IDENT(0).GetText(),
                    MemberName = ma.IDENT(1).GetText(),
                    Line = context.Start.Line,
                    Column = context.Start.Column
                };
            }
            // IDENT DOT func_call
            else if (ma.func_call() != null)
            {
                var funcCall = ma.func_call();
                string funcName = funcCall.IDENT()?.GetText()
                               ?? funcCall.ASK()?.GetText()
                               ?? funcCall.SHOW()?.GetText()
                               ?? funcCall.LEN()?.GetText()
                               ?? funcCall.FILE_OP()?.GetText()
                               ?? funcCall.CONVERT_OP()?.GetText()
                               ?? "unknown";

                return new MemberAssignTarget
                {
                    ObjectName = ma.IDENT(0).GetText(),
                    MemberName = funcName,
                    Line = context.Start.Line,
                    Column = context.Start.Column
                };
            }
        }

        throw new InvalidOperationException("Unknown assign target");
    }
    //  Return
    public override AstNode VisitReturn_stmt([NotNull] RedlangParser.Return_stmtContext context)
    {
        return new ReturnNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Expression = context.expression() != null ? (ExpressionNode)Visit(context.expression()) : null
        };
    }
    //  Funciones
    public override AstNode VisitFunc_decl([NotNull] RedlangParser.Func_declContext context)
    {
        var fn = new FunctionNode
        {
            Name = context.IDENT().GetText(),
            Line = context.Start.Line,
            Column = context.Start.Column,
            Type = (TypeNode)Visit(context.data_type()),
            IsEntry = false
            //Type = (context.data_type() != null) ? (TypeNode)Visit(context.data_type()) : new TypeNode { Name = "void", Line = context.Start.Line, Column = context.Start.Column }
        };
        // Parámetros (param_list?)
        if (context.param_list() != null)
        {
            foreach (var p in context.param_list().param())
            {
                fn.Parameters.Add((ParameterNode)Visit(p));
            }
        }

        if (context.block() != null)
        {
            fn.Body = (BlockNode)Visit(context.block());
        }

        return fn;
    }
    public override AstNode VisitEntry_func_decl([NotNull] RedlangParser.Entry_func_declContext context)
    {
        var func = (FunctionNode)Visit(context.func_decl());
        func.IsEntry = true;
        return func;
    }
    public override AstNode VisitParam([NotNull] RedlangParser.ParamContext context)
    {
        return new ParameterNode
        {
            Name = context.IDENT().GetText(),
            Type = (TypeNode)Visit(context.data_type()),
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }

    //  Bloques y statements
    public override AstNode VisitBlock([NotNull] RedlangParser.BlockContext context)
    {
        var block = new BlockNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
        //block : O_BRACES statement* C_BRACES;
        foreach (var st in context.statement())
        {
            var node = Visit(st);
            if (node is StatementNode s) block.Statements.Add(s);
            // else if (node is ExpressionNode e)
            // {
            //     block.Statements.Add(new ExpressionStatementNode
            //     {
            //         Expression = e,
            //         Line = e.Line,
            //         Column = e.Column
            //     });

            // }
        }

        return block;
    }
    public override AstNode VisitStatement([NotNull] RedlangParser.StatementContext context)
    {
        // statement : declare_stmt | set_stmt | return_stmt | stmt_control | func_call SEMI_COLON | member_access SEMI_COLON;
        if (context.declare_stmt() != null) return Visit(context.declare_stmt());
        if (context.set_stmt() != null) return Visit(context.set_stmt());
        if (context.return_stmt() != null) return Visit(context.return_stmt());
        if (context.stmt_control() != null) return Visit(context.stmt_control());
        if (context.func_call() != null)
        {
            return new ExpressionStatementNode
            {
                Expression = (ExpressionNode)Visit(context.func_call()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }
        if (context.member_access() != null)
        {
            return new ExpressionStatementNode
            {
                Expression = (ExpressionNode)Visit(context.member_access()),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }

        return base.VisitStatement(context);
    }
    // Control structures
    public override AstNode VisitStmt_control([NotNull] RedlangParser.Stmt_controlContext context)
    {
        if (context.check_stmt() != null) return Visit(context.check_stmt());
        if (context.loop_stmt() != null) return Visit(context.loop_stmt());
        if (context.repeat_stmt() != null) return Visit(context.repeat_stmt());
        return base.VisitStmt_control(context);
    }
    //check_stmt : CHECK O_PAREN expression C_PAREN block otherwiseOpcional?;
    public override AstNode VisitCheck_stmt([NotNull] RedlangParser.Check_stmtContext context)
    {
        var check = new CheckNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Condition = (ExpressionNode)Visit(context.expression()),
            ThenBlock = (BlockNode)Visit(context.block())
        };

        if (context.otherwiseOpcional() != null && context.otherwiseOpcional().block() != null)
        {
            check.ElseBlock = (BlockNode)Visit(context.otherwiseOpcional().block());
        }

        return check;
    }
    public override AstNode VisitLoop_stmt([NotNull] RedlangParser.Loop_stmtContext context)
    {
        var loop = new LoopNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };

        // loopInit
        var loopInitCtx = context.loopInit();
        if (loopInitCtx != null)
        {
            // loopInit : decl_head (EQUAL expression)? | accionLoop;
            if (loopInitCtx.decl_head() != null)
            {
                var decl = (VariableNode)Visit(loopInitCtx.decl_head());
                var exprCtx = loopInitCtx.expression();
                if (exprCtx != null)
                {
                    decl.Expression = (ExpressionNode)Visit(exprCtx);
                }
                loop.Init = decl;
            }
            // Caso: accionLoop
            else if (loopInitCtx.accionLoop() != null)
            {
                loop.Init = Visit(loopInitCtx.accionLoop());
            }
        }

        // condición (segundo segmento)
        loop.Condition = (ExpressionNode)Visit(context.expression());

        // acción final
        loop.Iteration = (SetNode)Visit(context.accionLoop());

        // cuerpo
        loop.Body = (BlockNode)Visit(context.block());

        return loop;
    }
    public override AstNode VisitAccionLoop([NotNull] RedlangParser.AccionLoopContext context)
    {
        // SET assign_target EQUAL expression
        return new SetNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Target = (AssignTargetNode)Visit(context.assign_target()),
            Value = (ExpressionNode)Visit(context.expression())
        };
    }
    public override AstNode VisitRepeat_stmt([NotNull] RedlangParser.Repeat_stmtContext context)
    {
        return new RepeatNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Condition = (ExpressionNode)Visit(context.expression()),
            Body = (BlockNode)Visit(context.block())
        };
    }
    public override AstNode VisitDecl_head([NotNull] RedlangParser.Decl_headContext context)
    {
        // decl_head : DECLARE IDENT COLON (data_type | IDENT?);
        var v = new VariableNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Name = context.IDENT(0).GetText()
        };

        if (context.data_type() != null)
        {
            v.Type = (TypeNode)Visit(context.data_type());
        }
        else if (context.IDENT().Length > 1)
        {
            v.Type = new TypeNode
            {
                Name = context.IDENT(1).GetText(),
                Line = context.Start.Line,
                Column = context.Start.Column
            };

        }

        return v;
    }
    // Tipos 
    public override AstNode VisitData_type([NotNull] RedlangParser.Data_typeContext context)
    {
        // data_type : type_base array_specifier? QUESTION?;
        var baseType = (TypeNode)Visit(context.type_base());
        // array_specifier? : O_BRACKETS expression? C_BRACKETS
        if (context.array_specifier() != null)
        {
            baseType.IsArray = true;
            var arr = context.array_specifier();
            if (arr.expression() != null) baseType.ArraySize = (ExpressionNode)Visit(arr.expression());
        }

        if (context.QUESTION() != null) baseType.IsNullable = true;

        return baseType;
    }
    public override AstNode VisitType_base([NotNull] RedlangParser.Type_baseContext context)
    {
        var t = new TypeNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };

        if (context.TYPE_I() != null) t.Name = context.TYPE_I().GetText();
        else if (context.TYPE_F() != null) t.Name = context.TYPE_F().GetText();
        else if (context.TYPE_B() != null) t.Name = context.TYPE_B().GetText();
        else if (context.TYPE_S() != null) t.Name = context.TYPE_S().GetText();
        else if (context.IDENT() != null) t.Name = context.IDENT().GetText();
        else t.Name = context.GetText();

        return t;

    }
    // Literales y arrays
    public override AstNode VisitLiteral([NotNull] RedlangParser.LiteralContext context)
    {
        //literal : BOOL | FLOAT | INT | STRING | NULL | array_literal ;
        var lit = new LiteralNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Raw = context.GetText()
        };

        if (context.BOOL() != null)
        {
            lit.Value = context.BOOL().GetText() == "true";
        }
        else if (context.INT() != null)
        {
            if (long.TryParse(context.INT().GetText(), out var ival)) lit.Value = ival;
            else lit.Value = context.INT().GetText();
        }
        else if (context.FLOAT() != null)
        {
            if (double.TryParse(context.FLOAT().GetText(), out var dval)) lit.Value = dval;
            else lit.Value = context.FLOAT().GetText();
        }
        else if (context.STRING() != null)
        {
            lit.Value = context.STRING().GetText().Trim('"');
        }
        else if (context.NULL() != null) lit.Value = null;
        //ojo- agregado
        else if (context.array_literal() != null) return Visit(context.array_literal());

        return lit;
    }
    //array_literal : O_BRACKETS (arg_list)? C_BRACKETS;
    public override AstNode VisitArray_literal([NotNull] RedlangParser.Array_literalContext context)
    {
        var arr = new ArrayLiteralNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };

        var args = context.arg_list();
        if (args != null)
        {
            foreach (var e in args.expression())
            {
                arr.Elements.Add((ExpressionNode)Visit(e));
            }
        }

        return arr;
    }
    //  Accesos a miembros y arreglos y llamadas
    //array_access : IDENT O_BRACKETS expression C_BRACKETS;
    public override AstNode VisitArray_access([NotNull] RedlangParser.Array_accessContext context)
    {
        return new ArrayAccessNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            ArrayName = context.IDENT().GetText(),
            Index = (ExpressionNode)Visit(context.expression())
        };
    }
    public override AstNode VisitMember_access([NotNull] RedlangParser.Member_accessContext context)
    {
        // member_access : IDENT DOT IDENT | IDENT DOT func_call;
        var memberAccess = new MemberAccessNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            ObjectName = context.IDENT(0).GetText()
        };
        if (context.IDENT().Length > 1) // IDENT DOT IDENT
        {
            memberAccess.MemberName = context.IDENT(1).GetText();
        }
        else if (context.func_call() != null) // IDENT DOT func_call
        {
            memberAccess.Call = (FuncCallNode)Visit(context.func_call());
        }
        return memberAccess;
    }
    /*
    func_call
    : (ASK | SHOW | LEN | FILE_OP | CONVERT_OP) O_PAREN arg_list? C_PAREN
    | IDENT O_PAREN arg_list? C_PAREN;*/
    public override AstNode VisitFunc_call([NotNull] RedlangParser.Func_callContext context)
    {
        // built-ins (si es ask, show, len, etc.) or IDENT()
        var first = context.GetChild(0).GetText();
        var call = new FuncCallNode
        {
            FunctionName = first,
            Line = context.Start.Line,
            Column = context.Start.Column
        };

        var args = context.arg_list();
        if (args != null)
        {
            foreach (var e in args.expression())
            {
                call.Arguments.Add((ExpressionNode)Visit(e));
            }
        }

        return call;
    }
    //  Expresiones (etiquetas de la gramática)
    public override AstNode VisitLogicalOr([NotNull] RedlangParser.LogicalOrContext context)
    {
        // expression OR expression
        return new BinaryExpressionNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Operator = "or",
            Left = (ExpressionNode)Visit(context.expression(0)),
            Right = (ExpressionNode)Visit(context.expression(1))
        };
    }
    public override AstNode VisitLogicalAnd([NotNull] RedlangParser.LogicalAndContext context)
    {
        //expression AND expression
        return new BinaryExpressionNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Operator = "and",
            Left = (ExpressionNode)Visit(context.expression(0)),
            Right = (ExpressionNode)Visit(context.expression(1))
        };
    }
    public override AstNode VisitLogicalNot([NotNull] RedlangParser.LogicalNotContext context)
    {
        //NOT expression
        return new UnaryExpressionNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Operator = "not",
            Operand = (ExpressionNode)Visit(context.expression())
        };
    }
    public override AstNode VisitRelational([NotNull] RedlangParser.RelationalContext context)
    {
        //expression comparator expression
        return new BinaryExpressionNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Operator = context.comparator().GetText(),
            Left = (ExpressionNode)Visit(context.expression(0)),
            Right = (ExpressionNode)Visit(context.expression(1))
        };
    }
    public override AstNode VisitAddSub([NotNull] RedlangParser.AddSubContext context)
    {
        //expression (PLUS | MINUS) expression
        return new BinaryExpressionNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Operator = context.GetChild(1).GetText(),
            Left = (ExpressionNode)Visit(context.expression(0)),
            Right = (ExpressionNode)Visit(context.expression(1))
        };
    }
    public override AstNode VisitMulDiv([NotNull] RedlangParser.MulDivContext context)
    {
        //expression (MULTIPLY | DIVIDE | MODULO)
        return new BinaryExpressionNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Operator = context.GetChild(1).GetText(),
            Left = (ExpressionNode)Visit(context.expression(0)),
            Right = (ExpressionNode)Visit(context.expression(1))
        };
    }
    public override AstNode VisitUnaryMinus([NotNull] RedlangParser.UnaryMinusContext context)
    {
        //MINUS expression 
        return new UnaryExpressionNode
        {
            Line = context.Start.Line,
            Column = context.Start.Column,
            Operator = "-",
            Operand = (ExpressionNode)Visit(context.expression())
        };
    }
    public override AstNode VisitAtom([NotNull] RedlangParser.AtomContext context)
    {
        // atom -> factor
        if (context.factor() != null)
            return Visit(context.factor());

        return base.VisitAtom(context);
    }
    public override AstNode VisitFactor([NotNull] RedlangParser.FactorContext context)
    {
        // factor : IDENT | literal | array_access | member_access | func_call | O_PAREN expression C_PAREN ;
        if (context.IDENT() != null)
        {
            return new IdentifierNode
            {
                Name = context.IDENT().GetText(),
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }
        if (context.literal() != null) return Visit(context.literal());
        if (context.array_access() != null) return Visit(context.array_access());
        if (context.member_access() != null) return Visit(context.member_access());
        if (context.func_call() != null) return Visit(context.func_call());
        if (context.O_PAREN() != null && context.expression() != null) return (ExpressionNode)Visit(context.expression());

        return base.VisitFactor(context);
    }

}