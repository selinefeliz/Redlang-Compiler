using System;
using Antlr4.Runtime;

class ProgramRunner
{
    static void Main(string[] args)
    {
        // 1) Elegir archivo: argumento > Samples/example.rl
        string path;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            path = args[0];
        }
        else
        {
            path = Path.Combine("Samples", "prof1.cds"); // ruta relativa al folder del proyecto
        }

        // Resolver ruta absoluta (más fácil para depuración)
        path = Path.GetFullPath(path);
        if (!File.Exists(path))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: archivo de entrada no encontrado: {path}");
            Console.ResetColor();
            return;
        }

        string code;
        try
        {
            code = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error leyendo el archivo: {ex.Message}");
            Console.ResetColor();
            return;
        }

        // //input stream
        // AntlrInputStream inputStream = new(code);

        // //Redlang Lexer y Token Stream
        // RedlangLexer lexer = new(inputStream);
        // CommonTokenStream tokenStream = new(lexer);
        // //Parser
        // RedlangParser parser = new(tokenStream);

        // RedlangParser.ProgramContext tree = parser.program();

        // //create visitor
        // AstBuilderVisitor visitor = new();
        // //parse tree y build 
        // var ast = (ProgramNode)visitor.Visit(tree); //depurar aqui y ver las variables ClassName

        try
        {
            // Crear input stream de ANTLR
            AntlrInputStream inputStream = new(code);

            // Lexer y tokens
            RedlangLexer lexer = new(inputStream);
            CommonTokenStream tokenStream = new(lexer);

            // Parser
            RedlangParser parser = new(tokenStream);

            // Manejo de errores para mostrar en consola
            parser.RemoveErrorListeners();
            parser.AddErrorListener(ConsoleErrorListener<IToken>.Instance);

            // Generar parse tree
            //RedlangParser.ProgramContext tree = parser.Program()
            var tree = parser.program();

            // Construir AST
            AstBuilderVisitor visitor = new AstBuilderVisitor();
            var ast = (ProgramNode)visitor.Visit(tree);

            // Imprimir AST de forma navegable
            Console.WriteLine("==== ASt ==== \n");
            PrintAst(ast);


            // ===== ANALISIS SEMANTICO =====
            Console.WriteLine("\n=== SEMANTIC ANALYSIS ===");
            SemanticAnalyzer analyzer = new SemanticAnalyzer();


            Console.WriteLine($"Total symbols in global scope: {analyzer.GlobalScope.Symbols.Count}");
            try
            {
                analyzer.Analyze(ast);
                Console.WriteLine("\n=== SYMBOL TABLE ===");
                analyzer.GlobalScope.Print();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Semantic error: " + ex.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    // Función recursiva para imprimir el AST, no interesa que nombre tiene, solo poner los nodos que se impriman
    static void PrintAst(AstNode? node, int indent = 0)
    {
        if (node == null) return;
        string padding = new(' ', indent * 2);
        Console.WriteLine(padding + node.GetType().Name);

        switch (node)
        {
            case ProgramNode program:
                foreach (var stmt in program.Uses)
                    PrintAst(stmt, indent + 1);
                foreach (var stmt in program.Classes)
                    PrintAst(stmt, indent + 1);
                break;

            case ClassNode cls:
                foreach (var func in cls.Members)
                    PrintAst(func, indent + 1);
                break;

            case BlockNode block:
                foreach (var stmt in block.Statements)
                    PrintAst(stmt, indent + 1);
                break;
            case FunctionNode func:
                if (func.Parameters != null)
                {
                    foreach (var p in func.Parameters)
                        PrintAst(p, indent + 1);
                }
                PrintAst(func.Body, indent + 1);
                break;
            case FuncCallNode funcCallNode:
                foreach (var arg in funcCallNode.Arguments)
                    PrintAst(arg, indent + 1);
                break;
            case LoopNode loop:
                if (loop.Init != null) PrintAst(loop.Init, indent + 1);
                PrintAst(loop.Condition, indent + 1);
                PrintAst(loop.Iteration, indent + 1);
                PrintAst(loop.Body, indent + 1);
                break;
            case CheckNode check:
                PrintAst(check.Condition, indent + 1);
                PrintAst(check.ThenBlock, indent + 1);
                PrintAst(check.ElseBlock, indent + 1);
                break;
            case RepeatNode repeat:
                PrintAst(repeat.Condition, indent + 1);
                PrintAst(repeat.Body, indent + 1);
                break;
            case ExpressionStatementNode exprStmt:
                PrintAst(exprStmt.Expression, indent + 1);
                break;
            case VariableNode varDecl:
                if (varDecl.Expression != null)
                    PrintAst(varDecl.Expression, indent + 1);
                break;
            case BinaryExpressionNode binaryExp:
                PrintAst(binaryExp.Left, indent + 1);
                PrintAst(binaryExp.Right, indent + 1);
                break;
            case UnaryExpressionNode unaryExp:
                PrintAst(unaryExp.Operand, indent + 1);
                break;
            case SetNode setNode:
                PrintAst(setNode.Target, indent + 1);
                PrintAst(setNode.Value, indent + 1);
                break;
            case ArrayAccessNode arrayAccess:
                PrintAst(arrayAccess.Index, indent + 1);
                break;
            case ArrayAssignTarget arrayAssignTarget:
                PrintAst(arrayAssignTarget.Index, indent + 1);
                break;
            case ArrayLiteralNode arrayLiteral:
                foreach (var element in arrayLiteral.Elements)
                    PrintAst(element, indent + 1);
                break;
            case DeclarationNode declarationNode:
                PrintAst(declarationNode.Type, indent + 1);
                break;
            case MemberAccessNode memberAccess:
                if (memberAccess.Call != null)
                    PrintAst(memberAccess.Call, indent + 1);
                break;
            case ParameterNode parameterNode:
                PrintAst(parameterNode.Type, indent + 1);
                break;
            case ReturnNode returnNode:
                if (returnNode.Expression != null)
                    PrintAst(returnNode.Expression, indent + 1);
                break;
            case TypeNode typeNode:
                if (typeNode.ArraySize != null)
                    PrintAst(typeNode.ArraySize, indent + 1);
                break;
            case IdentifierNode:
            case IdentifierAssignTarget:
            case MemberAssignTarget:
            case StatementNode:
            case LiteralNode:
            case UseNode:
                // No hay nodos hijos que imprimir
                break;
            default:
                // fallback genérico para cualquier nodo nuevo
                // Busca propiedades que sean nodos o listas de nodos
                var props = node.GetType().GetProperties();
                foreach (var prop in props)
                {
                    var val = prop.GetValue(node);
                    switch (val)
                    {
                        case AstNode child:
                            PrintAst(child, indent + 1);
                            break;

                        case IEnumerable<AstNode> list:
                            foreach (var childNode in list)
                                PrintAst(childNode, indent + 1);
                            break;
                    }
                }
                break;

        }

    }


}

