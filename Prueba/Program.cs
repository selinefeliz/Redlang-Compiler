using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Antlr4.Runtime;

class ProgramRunner
{
    static void Main(string[] args)
    {
        string entryPath;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            entryPath = Path.GetFullPath(args[0]);
        }
        else
        {
            entryPath = Path.GetFullPath(Path.Combine("EjemplosPruebas", "prof1.cds"));
            // entryPath = Path.GetFullPath(Path.Combine("Samples", "TestProject", "MainEntryPoint.cds"));sel
        }

        if (!File.Exists(entryPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Entry file not found: {entryPath}");
            Console.ResetColor();
            return;
        }

        string projectDir = Directory.GetCurrentDirectory();
        // Console.WriteLine($"Project Root: {projectDir}");

        // Find relevant source files
        var sourceFiles = Directory.GetFiles(projectDir, "*.cds", SearchOption.AllDirectories)
                                   .Where(f => {
                                       string fullF = Path.GetFullPath(f);
                                       if (fullF.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) ||
                                           fullF.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) ||
                                           fullF.Contains("Legacy")) 
                                           return false;
                                       
                                       return true;
                                   })
                                   .ToArray();

        List<ProgramNode> projectNodes = new List<ProgramNode>();
        ProgramNode? entryNode = null;
        Console.WriteLine("=========== SELI COMPILER - Redlang  =========\n");

        Console.WriteLine($"Found {sourceFiles.Length} source files in project.");

        foreach (var file in sourceFiles)
        {
            var node = ParseFile(file);
            if (node != null)
            {
                // Solo incluimos archivos que sean el entryPoint O que no tengan un metodo de entrada
                // Esto permite tener varios archivos con 'Main' en la misma carpeta (como tests separados)
                // y que no choquen entre sí si no son el archivo que queremos ejecutar.
                bool isMainEntry = string.Equals(Path.GetFullPath(file), entryPath, StringComparison.OrdinalIgnoreCase);
                bool hasAnotherEntry = node.Classes.Any(c => c.Members.Any(m => m is FunctionNode fn && fn.IsEntry));

                if (hasAnotherEntry && !isMainEntry)
                {
                    // Console.WriteLine($"Skipping file with alternative entry point: {Path.GetFileName(file)}");
                    continue;
                }

                Console.WriteLine($"Parsing: {Path.GetFileName(file)}");
                projectNodes.Add(node);
                if (isMainEntry)
                {
                    entryNode = node;
                }
            }
        }

        if (entryNode == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Entry file could not be parsed or identified.");
            Console.ResetColor();
            return;
        }

        try
        {
            Console.WriteLine("\n=== SEMANTIC ANALYSIS ===");
            SemanticAnalyzer analyzer = new SemanticAnalyzer();
            analyzer.AnalyzeProject(projectNodes, entryNode);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nCompilation/Analysis Successful.");
            Console.ResetColor();
            
            //generar el codifo .ll
            Console.WriteLine("\n=== CODE GENERATION ===");
            //eliminar 
            string outputPath = Path.Combine(projectDir, "program.ll");
            string exePath = Path.Combine(projectDir, "program.out");

            using (var generator = new CodeGenerator())
            {
                generator.Generate(projectNodes);
                generator.WriteToFile(outputPath);
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Code Generated Successfully at: {outputPath}");
                Console.ResetColor();
            }

            // Compilar con Clang vía WSL (Backend)
            Console.WriteLine("\n=== COMPILING WITH CLANG (WSL) ===");
            
            string llFile = "program.ll";
            string outFile = "program.out";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = $"clang -Wno-override-module -o {outFile} {llFile}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi)!)
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Compilation Successful!");
                    Console.WriteLine($"Executable created: {Path.Combine(projectDir, outFile)}");
                    Console.ResetColor();

                    // EJECUCIÓN AUTOMÁTICA VÍA WSL
                    Console.WriteLine("\n=== RUNNING PROGRAM (WSL) ===\n");
                    ProcessStartInfo runPsi = new ProcessStartInfo
                    {
                        FileName = "wsl",
                        Arguments = $"./{outFile}",
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };
                    
                    using (Process runProcess = Process.Start(runPsi)!)
                    {
                        runProcess.WaitForExit();
                        Console.WriteLine($"\n[Program exited with code {runProcess.ExitCode}]");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Clang Compilation Failed:");
                    Console.WriteLine(error);
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nError: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static ProgramNode? ParseFile(string path)
    {
        try
        {
            string input = File.ReadAllText(path);
            var inputStream = new AntlrInputStream(input);
            var lexer = new RedlangLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new RedlangParser(tokenStream);
            var context = parser.program();
            var visitor = new AstBuilderVisitor();
            return (ProgramNode)visitor.Visit(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing {path}: {ex.Message}");
            return null;
        }
    }
}
