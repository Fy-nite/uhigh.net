using uhigh.Net.Lexer;
using uhigh.Net.Parser;
using uhigh.Net.CodeGen;
using uhigh.Net.Diagnostics;
using System.Diagnostics;

namespace uhigh.Net
{
    /// <summary>
    /// The compiler class
    /// </summary>
    public class Compiler
    {
        /// <summary>
        /// The verbose mode
        /// </summary>
        private readonly bool _verboseMode;
        /// <summary>
        /// The std lib path
        /// </summary>
        private readonly string? _stdLibPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="Compiler"/> class
        /// </summary>
        /// <param name="verboseMode">The verbose mode</param>
        /// <param name="stdLibPath">The std lib path</param>
        public Compiler(bool verboseMode = false, string? stdLibPath = null)
        {
            _verboseMode = verboseMode;
            // Use provided path or try to find it relative to the executable
            _stdLibPath = stdLibPath ?? Path.Combine(AppContext.BaseDirectory, "stdlib");
        }

        /// <summary>
        /// Compiles the file using the specified source file
        /// </summary>
        /// <param name="sourceFile">The source file</param>
        /// <param name="outputFile">The output file</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> CompileFile(string sourceFile, string? outputFile = null)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, sourceFile);
            
            try
            {
                // Read source code
                var source = await File.ReadAllTextAsync(sourceFile);
                
                // Compile to C#
                var csharpCode = CompileToCS(source, diagnostics);
                
                if (diagnostics.HasErrors)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                // Use in-memory compiler with standard library path
                var inMemoryCompiler = new InMemoryCompiler(_stdLibPath);
                
                if (outputFile != null)
                {
                    var success = await inMemoryCompiler.CompileToExecutable(csharpCode, outputFile);
                    diagnostics.PrintSummary();
                    return success;
                }
                else
                {
                    var success = await inMemoryCompiler.CompileAndRun(csharpCode);
                    diagnostics.PrintSummary();
                    return success;
                }
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Compilation failed: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        /// <summary>
        /// Compiles the to executable using the specified source file
        /// </summary>
        /// <param name="sourceFile">The source file</param>
        /// <param name="outputFile">The output file</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> CompileToExecutable(string sourceFile, string outputFile)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, sourceFile);
            
            try
            {
                var source = await File.ReadAllTextAsync(sourceFile);
                var csharpCode = CompileToCS(source, diagnostics);
                
                if (diagnostics.HasErrors)
                {
                    diagnostics.PrintSummary();
                    return false;
                }
                
                var inMemoryCompiler = new InMemoryCompiler(_stdLibPath);
                if (_verboseMode)
                {
                    Console.WriteLine("Generated C# code:");
                    Console.WriteLine(csharpCode);
                    Console.WriteLine();
                }
                
                var success = await inMemoryCompiler.CompileToExecutable(csharpCode, outputFile, "Generated");
                diagnostics.PrintSummary();
                return success;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Compilation failed: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        /// <summary>
        /// Compiles the and run in memory using the specified source file
        /// </summary>
        /// <param name="sourceFile">The source file</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> CompileAndRunInMemory(string sourceFile)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, sourceFile);
            
            try
            {
                var source = await File.ReadAllTextAsync(sourceFile);
                var csharpCode = CompileToCS(source, diagnostics);
                
                if (diagnostics.HasErrors)
                {
                    diagnostics.PrintSummary();
                    return false;
                }
                
                var inMemoryCompiler = new InMemoryCompiler(_stdLibPath);
                var success = await inMemoryCompiler.CompileAndRun(csharpCode, null,null);
                // Print the generated C# code if in verbose mode plus diagnostics summary
                if (_verboseMode)
                {
                    Console.WriteLine("Generated C# code:");
                    Console.WriteLine(csharpCode);
                    Console.WriteLine();
                    diagnostics.PrintSummary();
                }
                return success;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Compilation failed: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        /// <summary>
        /// Compiles the to cs using the specified source
        /// </summary>
        /// <param name="source">The source</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="rootNamespace">The root namespace</param>
        /// <param name="className">The class name</param>
        /// <exception cref="Exception">Parsing failed</exception>
        /// <exception cref="Exception">Tokenization failed</exception>
        /// <returns>The string</returns>
        public string CompileToCS(string source, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            diagnostics ??= new DiagnosticsReporter(_verboseMode);
            
            try
            {
                if (_verboseMode)
                {
                    diagnostics.ReportInfo("Starting μHigh compilation pipeline");
                }
                
                // Tokenize
                var lexer = new Lexer.Lexer(source, diagnostics, _verboseMode);
                var tokens = lexer.Tokenize();
                
                if (diagnostics.HasErrors)
                {
                    throw new Exception("Tokenization failed");
                }
                
                // Parse
                var parser = new Parser.Parser(tokens, diagnostics, _verboseMode);
                var ast = parser.Parse();

                // Handle include statements before code generation
                ast = ProcessIncludes(ast, diagnostics, new HashSet<string>());

                if (diagnostics.HasErrors)
                {
                    throw new Exception("Parsing failed");
                }
                
                // Generate C# with root namespace and class name
                var generator = new CSharpGenerator();
                var result = generator.Generate(ast, diagnostics, rootNamespace, className);
                
                if (_verboseMode)
                {
                    diagnostics.ReportInfo("μHigh compilation pipeline completed successfully");
                }
                return result;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Compilation pipeline failed: {ex.Message}", exception: ex);
                throw;
            }
        }

        // Add new method to compile and return AST
        /// <summary>
        /// Compiles the to ast using the specified source
        /// </summary>
        /// <param name="source">The source</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <exception cref="Exception">Parsing failed</exception>
        /// <exception cref="Exception">Tokenization failed</exception>
        /// <returns>The program</returns>
        public Program CompileToAST(string source, DiagnosticsReporter? diagnostics = null)
        {
            diagnostics ??= new DiagnosticsReporter(_verboseMode);
            
            try
            {
                if (_verboseMode)
                {
                    diagnostics.ReportInfo("Starting μHigh AST compilation");
                }
                
                // Tokenize
                var lexer = new Lexer.Lexer(source, diagnostics, _verboseMode);
                var tokens = lexer.Tokenize();
                
                if (diagnostics.HasErrors)
                {
                    throw new Exception("Tokenization failed");
                }
                
                // Parse
                var parser = new Parser.Parser(tokens, diagnostics, _verboseMode);
                var ast = parser.Parse();

                // Handle include statements
                ast = ProcessIncludes(ast, diagnostics, new HashSet<string>());

                if (diagnostics.HasErrors)
                {
                    throw new Exception("Parsing failed");
                }
                
                if (_verboseMode)
                {
                    diagnostics.ReportInfo("μHigh AST compilation completed successfully");
                }
                return ast;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"AST compilation failed: {ex.Message}", exception: ex);
                throw;
            }
        }

        // Add method to print AST from file
        /// <summary>
        /// Prints the ast using the specified source file
        /// </summary>
        /// <param name="sourceFile">The source file</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> PrintAST(string sourceFile)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, sourceFile);
            
            try
            {
                var source = await File.ReadAllTextAsync(sourceFile);
                var ast = CompileToAST(source, diagnostics);
                
                if (diagnostics.HasErrors)
                {
                    diagnostics.PrintSummary();
                    return false;
                }
                
                Console.WriteLine("Abstract Syntax Tree (AST):");
                Console.WriteLine("===========================");
                Console.WriteLine();
                PrintASTNode(ast, 0);
                
                diagnostics.PrintSummary();
                return true;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to print AST: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        // Add helper method to recursively print AST nodes
        /// <summary>
        /// Prints the ast node using the specified node
        /// </summary>
        /// <param name="node">The node</param>
        /// <param name="depth">The depth</param>
        private void PrintASTNode(ASTNode node, int depth)
        {
            var indent = new string(' ', depth * 2);
            var nodeType = node.GetType().Name;

            void PrintBlockStart(string header)
            {
                Console.WriteLine($"{indent}{header} {{");
            }
            void PrintBlockEnd()
            {
                Console.WriteLine($"{indent}}}");
            }

            switch (node)
            {
                case Program program:
                    PrintBlockStart("Program");
                    foreach (var stmt in program.Statements)
                        PrintASTNode(stmt, depth + 1);
                    PrintBlockEnd();
                    break;

                case NamespaceDeclaration ns:
                    PrintBlockStart($"namespace {ns.Name}");
                    foreach (var member in ns.Members)
                        PrintASTNode(member, depth + 1);
                    PrintBlockEnd();
                    break;

                case ClassDeclaration cls:
                    PrintBlockStart($"class {cls.Name}{(cls.BaseClass != null ? $" : {cls.BaseClass}" : "")}");
                    foreach (var member in cls.Members)
                        PrintASTNode(member, depth + 1);
                    PrintBlockEnd();
                    break;

                case StructDeclaration s:
                    PrintBlockStart($"struct {s.Name}");
                    foreach (var field in s.Fields)
                        PrintASTNode(field, depth + 1);
                    PrintBlockEnd();
                    break;

                case ModuleDeclaration m:
                    PrintBlockStart($"module {m.Name}");
                    foreach (var member in m.Members)
                        PrintASTNode(member, depth + 1);
                    PrintBlockEnd();
                    break;

                case FunctionDeclaration func:
                    PrintBlockStart($"func {func.Name}({string.Join(", ", func.Parameters.Select(p => $"{p.Type ?? "var"} {p.Name}"))}){(func.ReturnType != null ? $": {func.ReturnType}" : "")}");
                    foreach (var stmt in func.Body)
                        PrintASTNode(stmt, depth + 1);
                    PrintBlockEnd();
                    break;

                case MethodDeclaration method:
                    PrintBlockStart($"{(method.IsStatic ? "static " : "")}method {method.Name}({string.Join(", ", method.Parameters.Select(p => $"{p.Type ?? "var"} {p.Name}"))}){(method.ReturnType != null ? $": {method.ReturnType}" : "")}");
                    foreach (var stmt in method.Body)
                        PrintASTNode(stmt, depth + 1);
                    PrintBlockEnd();
                    break;

                case PropertyDeclaration prop:
                    PrintBlockStart($"property {prop.Name}{(prop.Type != null ? $": {prop.Type}" : "")}");
                    foreach (var accessor in prop.Accessors)
                        PrintASTNode(accessor, depth + 1);
                    PrintBlockEnd();
                    break;

                case PropertyAccessor accessor:
                    PrintBlockStart($"{accessor.Type} accessor");
                    if (accessor.Body != null)
                        PrintASTNode(accessor.Body, depth + 1);
                    foreach (var stmt in accessor.Statements)
                        PrintASTNode(stmt, depth + 1);
                    PrintBlockEnd();
                    break;

                case FieldDeclaration field:
                    Console.WriteLine($"{indent}field {(field.IsStatic ? "static " : "")}{field.Name}{(field.Type != null ? $": {field.Type}" : "")}{(field.Initializer != null ? " = ..." : "")};");
                    break;

                case VariableDeclaration var:
                    Console.WriteLine($"{indent}{(var.IsConstant ? "const " : "var ")}{var.Name}{(var.Type != null ? $": {var.Type}" : "")}{(var.Initializer != null ? " = ..." : "")};");
                    if (var.Initializer != null)
                        PrintASTNode(var.Initializer, depth + 1);
                    break;

                case LetDeclaration let:
                    Console.WriteLine($"{indent}let {(let.IsMutable ? "mut " : "")}{let.Name}{(let.Type != null ? $": {let.Type}" : "")}{(let.Initializer != null ? " = ..." : "")};");
                    if (let.Initializer != null)
                        PrintASTNode(let.Initializer, depth + 1);
                    break;

                case EnumDeclaration en:
                    PrintBlockStart($"enum {en.Name}{(en.BaseType != null ? $": {en.BaseType}" : "")}");
                    foreach (var member in en.Members)
                        PrintASTNode(member, depth + 1);
                    PrintBlockEnd();
                    break;

                case EnumMember em:
                    Console.WriteLine($"{indent}{em.Name}{(em.Value != null ? " = ..." : "")},");
                    if (em.Value != null)
                        PrintASTNode(em.Value, depth + 1);
                    break;

                case InterfaceDeclaration iface:
                    PrintBlockStart($"interface {iface.Name}");
                    foreach (var member in iface.Members)
                        PrintASTNode(member, depth + 1);
                    PrintBlockEnd();
                    break;

                case TypeAliasDeclaration typeAlias:
                    Console.WriteLine($"{indent}type {typeAlias.Name} = {typeAlias.Type.Name};");
                    break;

                case ImportStatement import:
                    Console.WriteLine($"{indent}import {import.ClassName}{(import.Alias != null ? $" as {import.Alias}" : "")};");
                    break;

                case IncludeStatement include:
                    Console.WriteLine($"{indent}include \"{include.FileName}\";");
                    break;

                case ExpressionStatement exprStmt:
                    PrintASTNode(exprStmt.Expression, depth);
                    break;

                case AssignmentExpression assign:
                    Console.Write($"{indent}");
                    PrintASTNode(assign.Target, 0);
                    Console.Write($" {assign.Operator} ");
                    PrintASTNode(assign.Value, 0);
                    Console.WriteLine(";");
                    break;

                case IfStatement ifStmt:
                    PrintBlockStart("if (" + GetExprPreview(ifStmt.Condition) + ")");
                    foreach (var stmt in ifStmt.ThenBranch)
                        PrintASTNode(stmt, depth + 1);
                    if (ifStmt.ElseBranch != null && ifStmt.ElseBranch.Any())
                    {
                        Console.WriteLine($"{indent}else");
                        PrintBlockStart("{");
                        foreach (var stmt in ifStmt.ElseBranch)
                            PrintASTNode(stmt, depth + 2);
                        PrintBlockEnd();
                    }
                    PrintBlockEnd();
                    break;

                case WhileStatement whileStmt:
                    PrintBlockStart("while (" + GetExprPreview(whileStmt.Condition) + ")");
                    foreach (var stmt in whileStmt.Body)
                        PrintASTNode(stmt, depth + 1);
                    PrintBlockEnd();
                    break;

                case ForStatement forStmt:
                    if (forStmt.IsForInLoop)
                        PrintBlockStart($"for ({forStmt.IteratorVariable} in {GetExprPreview(forStmt.IterableExpression)})");
                    else
                        PrintBlockStart("for (...)");
                    foreach (var stmt in forStmt.Body)
                        PrintASTNode(stmt, depth + 1);
                    PrintBlockEnd();
                    break;

                case ReturnStatement ret:
                    Console.Write($"{indent}return");
                    if (ret.Value != null)
                    {
                        Console.Write(" ");
                        PrintASTNode(ret.Value, 0);
                    }
                    Console.WriteLine(";");
                    break;

                case BreakStatement:
                    Console.WriteLine($"{indent}break;");
                    break;

                case ContinueStatement:
                    Console.WriteLine($"{indent}continue;");
                    break;

                case SharpBlock sharp:
                    Console.WriteLine($"{indent}# {{ ... }}");
                    break;

                case MatchStatement matchStmt:
                    PrintBlockStart("match (" + GetExprPreview(matchStmt.Value) + ")");
                    foreach (var arm in matchStmt.Arms)
                        PrintASTNode(arm, depth + 1);
                    PrintBlockEnd();
                    break;

                case MatchArm arm:
                    Console.Write($"{indent}case ");
                    if (arm.IsDefault)
                        Console.Write("_");
                    else
                        Console.Write(string.Join(", ", arm.Patterns.Select(GetExprPreview)));
                    Console.Write(" => ");
                    PrintASTNode(arm.Result, 0);
                    Console.WriteLine();
                    break;

                case SwitchStatement sw:
                    PrintBlockStart("switch (" + GetExprPreview(sw.Value) + ")");
                    foreach (var sc in sw.Cases)
                        PrintASTNode(sc, depth + 1);
                    PrintBlockEnd();
                    break;

                case SwitchCase sc:
                    Console.Write($"{indent}case ");
                    if (sc.IsDefault)
                        Console.Write("default");
                    else if (sc.Pattern != null)
                        PrintASTNode(sc.Pattern, 0);
                    Console.WriteLine(":");
                    foreach (var stmt in sc.Statements)
                        PrintASTNode(stmt, depth + 1);
                    break;

                case BlockExpression block:
                    PrintBlockStart("block");
                    foreach (var stmt in block.Statements)
                        PrintASTNode(stmt, depth + 1);
                    PrintBlockEnd();
                    break;

                case LambdaExpression lambda:
                    Console.Write($"{indent}lambda ({string.Join(", ", lambda.Parameters.Select(p => p.Name))}) ");
                    if (lambda.IsExpressionLambda && lambda.Body != null)
                    {
                        Console.Write("=> ");
                        PrintASTNode(lambda.Body, 0);
                        Console.WriteLine();
                    }
                    else if (lambda.IsBlockLambda)
                    {
                        PrintBlockStart("{");
                        foreach (var stmt in lambda.Statements)
                            PrintASTNode(stmt, depth + 1);
                        PrintBlockEnd();
                    }
                    break;

                case CallExpression call:
                    Console.Write($"{indent}");
                    PrintASTNode(call.Function, 0);
                    Console.Write("(");
                    for (int i = 0; i < call.Arguments.Count; i++)
                    {
                        if (i > 0) Console.Write(", ");
                        PrintASTNode(call.Arguments[i], 0);
                    }
                    Console.WriteLine(")");
                    break;

                case ConstructorCallExpression ctor:
                    Console.Write($"{indent}new {ctor.ClassName}(");
                    for (int i = 0; i < ctor.Arguments.Count; i++)
                    {
                        if (i > 0) Console.Write(", ");
                        PrintASTNode(ctor.Arguments[i], 0);
                    }
                    Console.WriteLine(")");
                    break;

                case ArrayExpression arr:
                    Console.Write($"{indent}[");
                    for (int i = 0; i < arr.Elements.Count; i++)
                    {
                        if (i > 0) Console.Write(", ");
                        PrintASTNode(arr.Elements[i], 0);
                    }
                    Console.WriteLine("]");
                    break;

                case IndexExpression idx:
                    Console.Write($"{indent}");
                    PrintASTNode(idx.Object, 0);
                    Console.Write("[");
                    PrintASTNode(idx.Index, 0);
                    Console.WriteLine("]");
                    break;

                case MemberAccessExpression member:
                    PrintASTNode(member.Object, 0);
                    Console.Write($".{member.MemberName}");
                    break;

                case QualifiedIdentifierExpression qid:
                    Console.Write($"{indent}{qid.Name}");
                    break;

                case IdentifierExpression id:
                    Console.Write($"{indent}{id.Name}");
                    break;

                case LiteralExpression lit:
                    Console.Write($"{indent}{(lit.Value is string ? $"\"{lit.Value}\"" : lit.Value)}");
                    break;

                case Parameter param:
                    Console.WriteLine($"{indent}param {param.Name}{(param.Type != null ? $": {param.Type}" : "")}");
                    break;

                case TypeAnnotation typeAnn:
                    Console.WriteLine($"{indent}type {typeAnn.Name}{(typeAnn.TypeArguments.Count > 0 ? $"<{string.Join(", ", typeAnn.TypeArguments.Select(t => t.Name))}>" : "")}");
                    break;

                case AttributeDeclaration attr:
                    Console.WriteLine($"{indent}[{attr.Name}{(attr.Arguments.Count > 0 ? $"({string.Join(", ", attr.Arguments.Select(GetExprPreview))})" : "")}]");
                    break;

                default:
                    Console.WriteLine($"{indent}// {nodeType}");
                    break;
            }

            // Helper for expression preview (single-line)
            string GetExprPreview(ASTNode? expr)
            {
                if (expr == null) return "";
                return expr switch
                {
                    IdentifierExpression id => id.Name,
                    LiteralExpression lit => lit.Value?.ToString() ?? "null",
                    QualifiedIdentifierExpression qid => qid.Name,
                    CallExpression call => $"{GetExprPreview(call.Function)}(...)",
                    MemberAccessExpression mem => $"{GetExprPreview(mem.Object)}.{mem.MemberName}",
                    BinaryExpression bin => $"{GetExprPreview(bin.Left)} {bin.Operator} {GetExprPreview(bin.Right)}",
                    _ => expr.GetType().Name
                };
            }
        }

        /// <summary>
        /// Saves the c sharp code using the specified source file
        /// </summary>
        /// <param name="sourceFile">The source file</param>
        /// <param name="outputFolder">The output folder</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> SaveCSharpCode(string sourceFile, string outputFolder)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, sourceFile);
            
            try
            {
                var source = await File.ReadAllTextAsync(sourceFile);
                var csharpCode = CompileToCS(source, diagnostics);
                
                if (diagnostics.HasErrors)
                {
                    diagnostics.PrintSummary();
                    return false;
                }
                
                // Create output directory if it doesn't exist
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                    Console.WriteLine($"Created directory: {outputFolder}");
                }
                
                // Generate filename based on source file
                var sourceFileName = Path.GetFileNameWithoutExtension(sourceFile);
                var outputFileName = $"{sourceFileName}.cs";
                var outputPath = Path.Combine(outputFolder, outputFileName);
                
                // Save the C# code
                await File.WriteAllTextAsync(outputPath, csharpCode);
                Console.WriteLine($"C# code saved to: {outputPath}");
                
                // Also create a basic project file for convenience
                await CreateProjectFile(outputFolder, sourceFileName);
                
                if (_verboseMode)
                {
                    Console.WriteLine("Generated C# code:");
                    Console.WriteLine(csharpCode);
                }
                
                diagnostics.PrintSummary();
                return true;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to save C# code: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }
        
        /// <summary>
        /// Creates the project file using the specified output folder
        /// </summary>
        /// <param name="outputFolder">The output folder</param>
        /// <param name="projectName">The project name</param>
        private async Task CreateProjectFile(string outputFolder, string projectName)
        {
            var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <AssemblyName>{projectName}</AssemblyName>
    <RootNamespace>Generated</RootNamespace>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>";

            var projectPath = Path.Combine(outputFolder, $"{projectName}.csproj");
            await File.WriteAllTextAsync(projectPath, projectContent);
            Console.WriteLine($"Project file created: {projectPath}");
            Console.WriteLine($"Build with: dotnet build \"{projectPath}\"");
            Console.WriteLine($"Run with: dotnet run --project \"{projectPath}\"");
        }

        /// <summary>
        /// Compiles the c sharp to executable using the specified cs file
        /// </summary>
        /// <param name="csFile">The cs file</param>
        /// <param name="outputFile">The output file</param>
        /// <returns>A task containing the bool</returns>
        private async Task<bool> CompileCSharpToExecutable(string csFile, string outputFile)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build --configuration Release --output \"{Path.GetDirectoryName(outputFile)}\"",
                        WorkingDirectory = Path.GetDirectoryName(csFile),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();
                
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Compiles the project using the specified project path
        /// </summary>
        /// <param name="projectPath">The project path</param>
        /// <param name="outputFile">The output file</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> CompileProject(string projectPath, string? outputFile = null)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, projectPath);
            var overallTimer = Stopwatch.StartNew();
            
            try
            {
                // Validate that this is actually a project file
                if (!projectPath.EndsWith(".uhighproj", StringComparison.OrdinalIgnoreCase))
                {
                    diagnostics.ReportError($"Expected a .uhighproj file, but got: {projectPath}");
                    diagnostics.PrintSummary();
                    return false;
                }

                // Load project file
                using (var loadTimer = new NuGet.OperationTimer("Loading project", diagnostics, _verboseMode))
                {
                    var project = await ProjectFile.LoadAsync(projectPath, diagnostics);
                    if (project == null)
                    {
                        diagnostics.PrintSummary();
                        return false;
                    }

                    diagnostics.ReportInfo($"Compiling project: {project.Name} (OutputType: {project.OutputType})");

                    // Define projectDir for resolving relative paths
                    var projectDir = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? "";
                    diagnostics.ReportInfo($"Project directory: {projectDir}");

                    // Restore NuGet packages first
                    var nugetManager = new NuGet.NuGetManager(diagnostics);
                    var restoreSuccess = await nugetManager.RestorePackagesAsync(project, projectDir);
                    if (!restoreSuccess)
                    {
                        diagnostics.ReportWarning("Some packages failed to restore, compilation may fail");
                    }

                    // Get NuGet package assemblies with proper target framework
                    var nugetAssemblies = new List<string>();
                    using (var resolveTimer = new NuGet.OperationTimer("Resolving package assemblies", diagnostics, _verboseMode))
                    {
                        foreach (var package in project.Dependencies)
                        {
                            var assemblies = nugetManager.GetPackageAssemblies(package, project.Target);
                            nugetAssemblies.AddRange(assemblies);
                            
                            diagnostics.ReportInfo($"Added {assemblies.Count} assemblies from package {package.Name} v{package.Version}");
                        }

                        if (nugetAssemblies.Count > 0)
                        {
                            diagnostics.ReportInfo($"Total NuGet assemblies found: {nugetAssemblies.Count}");
                            if (_verboseMode)
                            {
                                foreach (var assembly in nugetAssemblies)
                                {
                                    diagnostics.ReportInfo($"  - {Path.GetFileName(assembly)}");
                                }
                            }
                        }
                    }

                    // Parse all source files and collect them
                    var allPrograms = new List<Program>();
                    var projectRootNamespace = project.RootNamespace ?? project.Name;
                    var projectClassName = project.ClassName ?? "Program";

                    if (project.SourceFiles.Count == 0)
                    {
                        diagnostics.ReportError("No source files found in project");
                        diagnostics.PrintSummary();
                        return false;
                    }

                    using (var parseTimer = new NuGet.OperationTimer("Parsing source files", diagnostics, _verboseMode))
                    {
                        diagnostics.ReportInfo($"Processing {project.SourceFiles.Count} source files:");

                        foreach (var sourceFileRelative in project.SourceFiles)
                        {
                            // Resolve source file path relative to project directory
                            var fullSourcePath = Path.IsPathRooted(sourceFileRelative) 
                                ? sourceFileRelative 
                                : Path.Combine(projectDir, sourceFileRelative);
                            

                            fullSourcePath = Path.GetFullPath(fullSourcePath);
                            

                            diagnostics.ReportInfo($"Processing: {Path.GetRelativePath(projectDir, fullSourcePath)}");
                                
                            if (!File.Exists(fullSourcePath))
                            {
                                diagnostics.ReportError($"Source file not found: {fullSourcePath}");
                                continue;
                            }

                            // Skip non-μHigh files
                            if (!fullSourcePath.EndsWith(".uh", StringComparison.OrdinalIgnoreCase))
                            {
                                diagnostics.ReportWarning($"Skipping non-μHigh source file: {fullSourcePath}");
                                continue;
                            }

                            try
                            {
                                var source = await File.ReadAllTextAsync(fullSourcePath);
                                diagnostics.ReportInfo($"Read {source.Length} characters from {Path.GetFileName(fullSourcePath)}");
                                
                                // Compile to AST
                                var ast = CompileToAST(source, diagnostics, Path.GetFileName(fullSourcePath));
                                
                                if (diagnostics.HasErrors)
                                {
                                    diagnostics.ReportError($"Failed to compile {fullSourcePath}");
                                    continue;
                                }
                                
                                allPrograms.Add(ast);
                                diagnostics.ReportInfo($"Successfully compiled {Path.GetFileName(fullSourcePath)}");
                            }
                            catch (Exception ex)
                            {
                                diagnostics.ReportError($"Failed to process {fullSourcePath}: {ex.Message}");
                                continue;
                            }
                        }
                    }

                    if (allPrograms.Count == 0)
                    {
                        diagnostics.ReportError("No valid source files were compiled");
                        diagnostics.PrintSummary();
                        return false;
                    }

                    diagnostics.ReportInfo($"Successfully processed {allPrograms.Count} source files");

                    // Generate combined C# code
                    string combinedCode;
                    using (var generateTimer = new NuGet.OperationTimer("Generating C# code", diagnostics, _verboseMode))
                    {
                        var generator = new CSharpGenerator();
                        combinedCode = generator.GenerateCombined(allPrograms, diagnostics, projectRootNamespace, projectClassName);
                        
                        if (diagnostics.HasErrors)
                        {
                            diagnostics.PrintSummary();
                            return false;
                        }

                        // Check for main method in executable projects
                        var hasMainMethod = combinedCode.Contains("static void Main") || combinedCode.Contains("static async Task Main");
                        
                        if (!hasMainMethod && project.OutputType.Equals("Exe", StringComparison.OrdinalIgnoreCase))
                        {
                            diagnostics.ReportError("No main method found in executable project. Make sure you have a 'func main()' function.");
                            diagnostics.PrintSummary();
                            return false;
                        }

                        if (_verboseMode)
                        {
                            diagnostics.ReportInfo("Generated combined C# code:");
                            Console.WriteLine("=".PadRight(50, '='));
                            Console.WriteLine(combinedCode);
                            Console.WriteLine("=".PadRight(50, '='));
                            Console.WriteLine();
                        }
                    }

                    // Use in-memory compiler with project configuration
                    bool success;
                    using (var compileTimer = new NuGet.OperationTimer("Compiling to executable", diagnostics, _verboseMode))
                    {
                        var inMemoryCompiler = new InMemoryCompiler(_stdLibPath);
                        
                        if (outputFile != null)
                        {
                            // Resolve output file relative to project directory
                            if (!Path.IsPathRooted(outputFile))
                            {
                                outputFile = Path.Combine(projectDir, outputFile);
                            }
                            
                            success = await inMemoryCompiler.CompileToExecutable(combinedCode, outputFile, projectRootNamespace, projectClassName, project.OutputType, nugetAssemblies);
                            if (success)
                            {
                                Console.WriteLine($"Project compiled successfully to: {outputFile}");
                            }
                        }
                        else
                        {
                            // Default output file based on project name and type
                            var defaultOutputFile = project.OutputType.Equals("Library", StringComparison.OrdinalIgnoreCase) 
                                ? Path.Combine(projectDir, $"{project.Name}.dll")
                                : Path.Combine(projectDir, $"{project.Name}.exe");
                            
                            success = await inMemoryCompiler.CompileToExecutable(combinedCode, defaultOutputFile, projectRootNamespace, projectClassName, project.OutputType, nugetAssemblies);
                            if (success)
                            {
                                Console.WriteLine($"Project compiled successfully to: {defaultOutputFile}");
                            }
                        }
                    }

                    overallTimer.Stop();
                    diagnostics.ReportInfo($"Total compilation time: {NuGet.OperationTimer.FormatDuration(overallTimer.Elapsed)}");
                    diagnostics.PrintSummary();
                    return success;
                }
            }
            catch (Exception ex)
            {
                overallTimer.Stop();
                diagnostics.ReportFatal($"Project compilation failed: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        // Helper method to check for main methods
        /// <summary>
        /// Checks the for main method using the specified program
        /// </summary>
        /// <param name="program">The program</param>
        /// <returns>The has namespace main</returns>
        private bool CheckForMainMethod(Program program)
        {
            // Check for top-level main function
            var hasTopLevelMain = program.Statements.OfType<FunctionDeclaration>().Any(f => f.Name == "main");
            if (hasTopLevelMain) return true;

            // Check for Main method in classes
            var hasClassMain = program.Statements.OfType<ClassDeclaration>().Any(c => 
                c.Members.OfType<MethodDeclaration>().Any(m => m.Name == "Main"));
            if (hasClassMain) return true;

            // Check for Main method in namespaces
            var hasNamespaceMain = program.Statements.OfType<NamespaceDeclaration>().Any(ns =>
                ns.Members.OfType<ClassDeclaration>().Any(c =>
                    c.Members.OfType<MethodDeclaration>().Any(m => m.Name == "Main")));

            return hasNamespaceMain;
        }

        // Add helper method to compile to AST
        /// <summary>
        /// Compiles the to ast using the specified source
        /// </summary>
        /// <param name="source">The source</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="fileName">The file name</param>
        /// <exception cref="Exception">Include processing failed for {fileName}</exception>
        /// <exception cref="Exception">Parsing failed for {fileName}</exception>
        /// <exception cref="Exception">Tokenization failed for {fileName}</exception>
        /// <returns>The program</returns>
        private Program CompileToAST(string source, DiagnosticsReporter diagnostics, string fileName = "")
        {
            try
            {
                if (_verboseMode)
                {
                    diagnostics.ReportInfo($"Compiling {fileName} to AST");
                }
                
                // Tokenize
                var lexer = new Lexer.Lexer(source, diagnostics, _verboseMode);
                var tokens = lexer.Tokenize();
                
                if (diagnostics.HasErrors)
                {
                    throw new Exception($"Tokenization failed for {fileName}");
                }
                
                if (_verboseMode)
                {
                    diagnostics.ReportInfo($"Generated {tokens.Count} tokens for {fileName}");
                }
                
                // Parse
                var parser = new Parser.Parser(tokens, diagnostics, _verboseMode);
                var ast = parser.Parse();

                if (diagnostics.HasErrors)
                {
                    throw new Exception($"Parsing failed for {fileName}");
                }
                
                if (_verboseMode)
                {
                    diagnostics.ReportInfo($"Generated AST with {ast.Statements.Count} statements for {fileName}");
                }

                // Handle include statements
                ast = ProcessIncludes(ast, diagnostics, new HashSet<string>());

                if (diagnostics.HasErrors)
                {
                    throw new Exception($"Include processing failed for {fileName}");
                }

                return ast;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to compile {fileName}: {ex.Message}", exception: ex);
                throw;
            }
        }

        // Add helper method to combine multiple programs
        /// <summary>
        /// Combines the programs using the specified programs
        /// </summary>
        /// <param name="programs">The programs</param>
        /// <returns>The program</returns>
        private Program CombinePrograms(List<Program> programs)
        {
            var combinedStatements = new List<Statement>();
            var seenImports = new HashSet<string>();
            var seenTypeAliases = new HashSet<string>();
            var seenNamespaces = new Dictionary<string, List<Statement>>();

            foreach (var program in programs)
            {
                foreach (var statement in program.Statements)
                {
                    // Deduplicate imports
                    if (statement is ImportStatement import)
                    {
                        var importKey = $"{import.ClassName}:{import.AssemblyName}";
                        if (!seenImports.Contains(importKey))
                        {
                            seenImports.Add(importKey);
                            combinedStatements.Add(statement);
                        }
                        continue;
                    }

                    // Deduplicate type aliases
                    if (statement is TypeAliasDeclaration typeAlias)
                    {
                        if (!seenTypeAliases.Contains(typeAlias.Name))
                        {
                            seenTypeAliases.Add(typeAlias.Name);
                            combinedStatements.Add(statement);
                        }
                        continue;
                    }

                    // Combine namespace declarations with same name
                    if (statement is NamespaceDeclaration ns)
                    {
                        if (!seenNamespaces.ContainsKey(ns.Name))
                        {
                            seenNamespaces[ns.Name] = new List<Statement>();
                        }
                        seenNamespaces[ns.Name].AddRange(ns.Members);
                        continue;
                    }

                    // Add all other statements
                    combinedStatements.Add(statement);
                }
            }

            // Add combined namespaces
            foreach (var kvp in seenNamespaces)
            {
                combinedStatements.Add(new NamespaceDeclaration
                {
                    Name = kvp.Key,
                    Members = kvp.Value
                });
            }

            return new Program { Statements = combinedStatements };
        }

        /// <summary>
        /// Saves the project as c sharp using the specified project path
        /// </summary>
        /// <param name="projectPath">The project path</param>
        /// <param name="outputFolder">The output folder</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> SaveProjectAsCSharp(string projectPath, string outputFolder)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, projectPath);
            
            try
            {
                var project = await ProjectFile.LoadAsync(projectPath, diagnostics);
                if (project == null)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                // Create output directory
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // Parse all source files
                var allPrograms = new List<Program>();
                var projectRootNamespace = project.RootNamespace ?? project.Name;
                var projectClassName = project.ClassName ?? "Program";

                foreach (var sourceFile in project.SourceFiles)
                {
                    if (!File.Exists(sourceFile))
                    {
                        diagnostics.ReportError($"Source file not found: {sourceFile}");
                        continue;
                    }

                    var source = await File.ReadAllTextAsync(sourceFile);
                    
                    var lexer = new Lexer.Lexer(source, diagnostics);
                    var tokens = lexer.Tokenize();
                    
                    if (diagnostics.HasErrors)
                    {
                        diagnostics.PrintSummary();
                        return false;
                    }
                    
                    var parser = new Parser.Parser(tokens, diagnostics);
                    var ast = parser.Parse();
                    ast = ProcessIncludes(ast, diagnostics, new HashSet<string>());

                    if (diagnostics.HasErrors)
                    {
                        diagnostics.PrintSummary();
                        return false;
                    }
                    
                    allPrograms.Add(ast);
                }

                if (allPrograms.Count == 0)
                {
                    diagnostics.ReportError("No valid source files found");
                    diagnostics.PrintSummary();
                    return false;
                }

                // Generate combined C# code with deduplicated using statements
                var generator = new CSharpGenerator();
                var combinedCode = generator.GenerateCombined(allPrograms, diagnostics, projectRootNamespace, projectClassName);
                
                if (diagnostics.HasErrors)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                // Save combined C# code
                var outputFileName = $"{project.Name}.cs";
                var outputPath = Path.Combine(outputFolder, outputFileName);
                
                await File.WriteAllTextAsync(outputPath, combinedCode);
                Console.WriteLine($"Combined C# code saved to: {outputPath}");

                // Create project file with dependencies
                await CreateProjectFileFromuhighProject(outputFolder, project);
                
                if (_verboseMode)
                {
                    Console.WriteLine("Generated combined C# code:");
                    Console.WriteLine(combinedCode);
                }
                
                diagnostics.PrintSummary();
                return true;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to save project as C#: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        /// <summary>
        /// Creates the project file fromuhigh project using the specified output folder
        /// </summary>
        /// <param name="outputFolder">The output folder</param>
        /// <param name="uhighProject">The uhigh project</param>
        private async Task CreateProjectFileFromuhighProject(string outputFolder, uhighProject uhighProject)
        {
            var dependencies = "";
            if (uhighProject.Dependencies.Count > 0)
            {
                var deps = uhighProject.Dependencies.Select(d => $@"    <PackageReference Include=""{d.Name}"" Version=""{d.Version}"" />");
                dependencies = $@"
  <ItemGroup>
{string.Join("\n", deps)}
  </ItemGroup>";
            }

            var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <AssemblyName>{uhighProject.Name}</AssemblyName>
    <RootNamespace>{uhighProject.RootNamespace ?? uhighProject.Name}</RootNamespace>
    <Nullable>{(uhighProject.Nullable ? "enable" : "disable")}</Nullable>
    <Version>{uhighProject.Version}</Version>
  </PropertyGroup>{dependencies}

</Project>";

            var projectPath = Path.Combine(outputFolder, $"{uhighProject.Name}.csproj");
            await File.WriteAllTextAsync(projectPath, projectContent);
            Console.WriteLine($"Project file created: {projectPath}");
            Console.WriteLine($"Build with: dotnet build \"{projectPath}\"");
            Console.WriteLine($"Run with: dotnet run --project \"{projectPath}\"");
        }

        /// <summary>
        /// Creates the project using the specified project name
        /// </summary>
        /// <param name="projectName">The project name</param>
        /// <param name="projectDir">The project dir</param>
        /// <param name="description">The description</param>
        /// <param name="author">The author</param>
        /// <param name="outputType">The output type</param>
        /// <param name="target">The target</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> CreateProject(string projectName, string? projectDir = null, string? description = null, string? author = null, string outputType = "Exe", string target = "net9.0")
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode);
            
            try
            {
                projectDir ??= Environment.CurrentDirectory;
                var fullProjectDir = Path.Combine(projectDir, projectName);

                var project = new uhighProject
                {
                    Name = projectName,
                    Version = "1.0.0",
                    Description = description,
                    Author = author,
                    Target = target,
                    OutputType = outputType,
                    SourceFiles = new List<string> { "main.uh" },
                    RootNamespace = projectName,
                    Nullable = true
                };

                var success = await ProjectFile.CreateAsync(projectName, fullProjectDir, diagnostics);
                
                if (success)
                {
                    Console.WriteLine($"Created project '{projectName}' in '{fullProjectDir}'");
                    Console.WriteLine($"Project file: {Path.Combine(fullProjectDir, $"{projectName}.uhighproj")}");
                    Console.WriteLine($"Main file: {Path.Combine(fullProjectDir, "main.uh")}");
                    Console.WriteLine($"Output type: {outputType}");
                }
                
                diagnostics.PrintSummary();
                return success;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to create project: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        /// <summary>
        /// Lists the project info using the specified project path
        /// </summary>
        /// <param name="projectPath">The project path</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> ListProjectInfo(string projectPath)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, projectPath);
            
            try
            {
                var project = await ProjectFile.LoadAsync(projectPath, diagnostics);
                if (project == null)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                Console.WriteLine($"Project: {project.Name}");
                Console.WriteLine($"Version: {project.Version}");
                Console.WriteLine($"Target: {project.Target}");
                Console.WriteLine($"Output Type: {project.OutputType}");
                
                if (!string.IsNullOrEmpty(project.Description))
                    Console.WriteLine($"Description: {project.Description}");
                
                if (!string.IsNullOrEmpty(project.Author))
                    Console.WriteLine($"Author: {project.Author}");
                
                if (!string.IsNullOrEmpty(project.RootNamespace))
                    Console.WriteLine($"Root Namespace: {project.RootNamespace}");
                
                Console.WriteLine($"Nullable: {project.Nullable}");
                
                Console.WriteLine("\nSource Files:");
                foreach (var file in project.SourceFiles)
                {
                    var exists = File.Exists(file) ? "✓" : "✗";
                    Console.WriteLine($"  {exists} {file}");
                }

                if (project.Dependencies.Count > 0)
                {
                    Console.WriteLine("\nDependencies:");
                    foreach (var dep in project.Dependencies)
                    {
                        Console.WriteLine($"  {dep.Name} v{dep.Version}");
                    }
                }

                if (project.Properties.Count > 0)
                {
                    Console.WriteLine("\nProperties:");
                    foreach (var prop in project.Properties)
                    {
                        Console.WriteLine($"  {prop.Name} = {prop.Value}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to read project info: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        /// <summary>
        /// Adds the source file to project using the specified project path
        /// </summary>
        /// <param name="projectPath">The project path</param>
        /// <param name="sourceFile">The source file</param>
        /// <param name="createFile">The create file</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> AddSourceFileToProject(string projectPath, string sourceFile, bool createFile = false)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, projectPath);
            
            try
            {
                var project = await ProjectFile.LoadAsync(projectPath, diagnostics);
                if (project == null)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                // Make path relative to project directory
                var projectDir = Path.GetDirectoryName(projectPath) ?? "";
                var relativePath = Path.GetRelativePath(projectDir, sourceFile);
                
                if (project.SourceFiles.Contains(relativePath))
                {
                    Console.WriteLine($"Source file '{relativePath}' is already in the project");
                    return true;
                }

                // Create file if requested and it doesn't exist
                if (createFile && !File.Exists(sourceFile))
                {
                    var fileName = Path.GetFileNameWithoutExtension(sourceFile);
                    var defaultContent = $@"// {fileName}.uhigh - Generated by uhigh.Net

// Add your uhigh.Net code here
";
                    await File.WriteAllTextAsync(sourceFile, defaultContent);
                    Console.WriteLine($"Created source file: {sourceFile}");
                }

                project.SourceFiles.Add(relativePath);
                var success = await ProjectFile.SaveAsync(project, projectPath, diagnostics);
                
                if (success)
                {
                    Console.WriteLine($"Added source file '{relativePath}' to project");
                }
                
                diagnostics.PrintSummary();
                return success;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to add source file: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        /// <summary>
        /// Adds the package to project using the specified project path
        /// </summary>
        /// <param name="projectPath">The project path</param>
        /// <param name="packageName">The package name</param>
        /// <param name="version">The version</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> AddPackageToProject(string projectPath, string packageName, string version)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, projectPath);
            
            try
            {
                var project = await ProjectFile.LoadAsync(projectPath, diagnostics);
                if (project == null)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                // Check if package already exists
                var existingPackage = project.Dependencies.FirstOrDefault(d => d.Name == packageName);
                if (existingPackage != null)
                {
                    Console.WriteLine($"Package '{packageName}' already exists with version {existingPackage.Version}");
                    Console.WriteLine($"Updating to version {version}");
                    existingPackage.Version = version;
                }
                else
                {
                    project.Dependencies.Add(new PackageReference
                    {
                        Name = packageName,
                        Version = version
                    });
                }

                var success = await ProjectFile.SaveAsync(project, projectPath, diagnostics);
                
                if (success)
                {
                    Console.WriteLine($"Added package '{packageName}' v{version} to project");
                }
                
                diagnostics.PrintSummary();
                return success;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to add package: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        // Add this helper method to recursively process includes
        /// <summary>
        /// Processes the includes using the specified ast
        /// </summary>
        /// <param name="ast">The ast</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="includedFiles">The included files</param>
        /// <returns>The program</returns>
        private Program ProcessIncludes(Program ast, DiagnosticsReporter diagnostics, HashSet<string> includedFiles)
        {
            var newStatements = new List<Statement>();
            foreach (var stmt in ast.Statements)
            {
                if (stmt is IncludeStatement include)
                {
                    var filePath = include.FileName.Trim('"');
                    if (includedFiles.Contains(filePath))
                    {
                        diagnostics.ReportError($"Recursive include detected: {filePath}");
                        continue;
                    }
                    if (!File.Exists(filePath))
                    {
                        diagnostics.ReportError($"Included file not found: {filePath}");
                        continue;
                    }
                    includedFiles.Add(filePath);
                    var includedSource = File.ReadAllText(filePath);
                    var lexer = new Lexer.Lexer(includedSource, diagnostics);
                    var tokens = lexer.Tokenize();
                    var parser = new Parser.Parser(tokens, diagnostics);
                    var includedAst = parser.Parse();
                    var processedAst = ProcessIncludes(includedAst, diagnostics, includedFiles);
                    newStatements.AddRange(processedAst.Statements);
                }
                else
                {
                    newStatements.Add(stmt);
                }
            }
            return new Program { Statements = newStatements };
        }

        /// <summary>
        /// Saves the c sharp code from project using the specified project path
        /// </summary>
        /// <param name="projectPath">The project path</param>
        /// <param name="outputFolder">The output folder</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> SaveCSharpCodeFromProject(string projectPath, string outputFolder)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, projectPath);
            
            try
            {
                var project = await ProjectFile.LoadAsync(projectPath, diagnostics);
                if (project == null)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                // Create output directory
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                    Console.WriteLine($"Created directory: {outputFolder}");
                }

                var generatedFiles = new List<string>();
                var projectRootNamespace = project.RootNamespace ?? project.Name;
                var projectClassName = project.ClassName ?? "Program";
                var hasMainMethod = false;

                // Compile each source file separately
                foreach (var sourceFile in project.SourceFiles)
                {
                    if (!File.Exists(sourceFile))
                    {
                        diagnostics.ReportError($"Source file not found: {sourceFile}");
                        continue;
                    }

                    var source = await File.ReadAllTextAsync(sourceFile);
                    var csharpCode = CompileToCS(source, diagnostics, projectRootNamespace, projectClassName);
                    
                    if (diagnostics.HasErrors)
                    {
                        diagnostics.PrintSummary();
                        return false;
                    }

                    // Check if this file contains a main method
                    if (csharpCode.Contains("static void Main") || csharpCode.Contains("static async Task Main"))
                    {
                        hasMainMethod = true;
                    }

                    // Generate C# filename based on source file
                    var sourceFileName = Path.GetFileNameWithoutExtension(sourceFile);
                    var outputFileName = $"{sourceFileName}.cs";
                    var outputPath = Path.Combine(outputFolder, outputFileName);
                    
                    await File.WriteAllTextAsync(outputPath, csharpCode);
                    generatedFiles.Add(outputFileName);
                    Console.WriteLine($"Generated: {outputPath}");
                }

                if (generatedFiles.Count == 0)
                {
                    diagnostics.ReportError("No source files were successfully compiled");
                    diagnostics.PrintSummary();
                    return false;
                }

                // Create project file that references all generated C# files
                await CreateProjectFileFromuhighProjectWithFiles(outputFolder, project, generatedFiles, hasMainMethod);
                
                if (_verboseMode)
                {
                    Console.WriteLine($"Generated {generatedFiles.Count} C# files:");
                    foreach (var file in generatedFiles)
                    {
                        Console.WriteLine($"  - {file}");
                    }
                }
                
                diagnostics.PrintSummary();
                return true;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to save project as C#: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        /// <summary>
        /// Creates the project file fromuhigh project with files using the specified output folder
        /// </summary>
        /// <param name="outputFolder">The output folder</param>
        /// <param name="uhighProject">The uhigh project</param>
        /// <param name="generatedFiles">The generated files</param>
        /// <param name="hasMainMethod">The has main method</param>
        private async Task CreateProjectFileFromuhighProjectWithFiles(string outputFolder, uhighProject uhighProject, List<string> generatedFiles, bool hasMainMethod)
        {
            var dependencies = "";
            if (uhighProject.Dependencies.Count > 0)
            {
                var deps = uhighProject.Dependencies.Select(d => $@"    <PackageReference Include=""{d.Name}"" Version=""{d.Version}"" />");
                dependencies = $@"
  <ItemGroup>
{string.Join("\n", deps)}
  </ItemGroup>";
            }

            // Include all generated C# files in the project
            var compileItems = generatedFiles.Select(f => $@"    <Compile Include=""{f}"" />");
        

            // Determine output type - if no main method found in an Exe project, warn but still create
            var outputType = uhighProject.OutputType;
            if (outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase) && !hasMainMethod)
            {
                Console.WriteLine("Warning: No main method found in executable project. You may need to add one.");
            }

            var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>{outputType}</OutputType>
    <TargetFramework>{uhighProject.Target}</TargetFramework>
    <AssemblyName>{uhighProject.Name}</AssemblyName>
    <RootNamespace>{uhighProject.RootNamespace ?? uhighProject.Name}</RootNamespace>
    <Nullable>{(uhighProject.Nullable ? "enable" : "disable")}</Nullable>
    <Version>{uhighProject.Version}</Version>
  </PropertyGroup>{dependencies}

</Project>";

            var projectPath = Path.Combine(outputFolder, $"{uhighProject.Name}.csproj");
            await File.WriteAllTextAsync(projectPath, projectContent);
            
            Console.WriteLine($"Project file created: {projectPath}");
            Console.WriteLine($"Build with: dotnet build \"{projectPath}\"");
            if (outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Run with: dotnet run --project \"{projectPath}\"");
            }
            
            if (_verboseMode)
            {
                Console.WriteLine("Generated project file:");
                Console.WriteLine(projectContent);
            }
        }

        internal async Task<bool> CompileProjectAndRun(string projectFile)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, projectFile);

            try
            {
                // Compile the project to an executable in a temp directory
                var tempDir = Path.Combine(Path.GetTempPath(), $"uhigh_build_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                var project = await ProjectFile.LoadAsync(projectFile, diagnostics);
                if (project == null)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                var exeName = project.OutputType.Equals("Library", StringComparison.OrdinalIgnoreCase)
                    ? $"{project.Name}.dll"
                    : $"{project.Name}.exe";
                var outputPath = Path.Combine(tempDir, exeName);

                var success = await CompileProject(projectFile, outputPath);
                if (!success)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                // Run the executable (dotnet for dll, direct for exe)
                if (project.OutputType.Equals("Library", StringComparison.OrdinalIgnoreCase))
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"\"{outputPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };
                    process.Start();
                    string stdOut = await process.StandardOutput.ReadToEndAsync();
                    string stdErr = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    Console.Write(stdOut);
                    if (!string.IsNullOrWhiteSpace(stdErr))
                        Console.Error.Write(stdErr);
                    return process.ExitCode == 0;
                }
                else
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = outputPath,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };
                    process.Start();
                    string stdOut = await process.StandardOutput.ReadToEndAsync();
                    string stdErr = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    Console.Write(stdOut);
                    if (!string.IsNullOrWhiteSpace(stdErr))
                        Console.Error.Write(stdErr);
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to compile and run project: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
            finally
            {
             
            }
        }
    }
}
