using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.InteropServices;

namespace uhigh.Net.CodeGen
{
    public class InMemoryCompiler
    {
        private class SourceInfo
        {
            public string? Namespace { get; set; }
            public string? MainClassName { get; set; }
            public List<string> AllClasses { get; set; } = new();
            public bool HasMainMethod { get; set; }
        }

        public async Task<bool> CompileAndRun(string csharpCode, string? outputPath = null, string? rootNamespace = null, string? className = null, string outputType = "Exe")
        {
            try
            {
                // Parse the C# code
                var syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
                
                // Extract source information
                var sourceInfo = ExtractSourceInfo(syntaxTree);
                
                // Get references to required assemblies
                var references = GetAssemblyReferences();
                
                // Use extracted info or provided parameters or defaults
                var actualRootNamespace = rootNamespace ?? sourceInfo.Namespace;
                var actualClassName = className ?? sourceInfo.MainClassName ?? "Program";
                var mainTypeName = actualRootNamespace != null 
                    ? $"{actualRootNamespace}.{actualClassName}"
                    : actualClassName;
                
                // Determine output kind based on outputType
                var outputKind = outputType.Equals("Library", StringComparison.OrdinalIgnoreCase) 
                    ? OutputKind.DynamicallyLinkedLibrary 
                    : OutputKind.ConsoleApplication;
                
                // Create compilation
                var compilation = CSharpCompilation.Create(
                    Path.GetFileNameWithoutExtension(outputPath ?? "GeneratedProgram"),
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(
                        outputKind,
                        mainTypeName: outputKind == OutputKind.ConsoleApplication && sourceInfo.HasMainMethod ? mainTypeName : null));
                
                // Compile to memory stream
                using var memoryStream = new MemoryStream();
                var emitResult = compilation.Emit(memoryStream);
                
                if (!emitResult.Success)
                {
                    Console.WriteLine("Compilation failed:");
                    foreach (var diagnostic in emitResult.Diagnostics)
                    {
                        if (diagnostic.Severity == DiagnosticSeverity.Error)
                        {
                            Console.WriteLine($"  Error: {diagnostic.GetMessage()}");
                        }
                    }
                    return false;
                }
                
                // If output path is specified, save to file
                if (outputPath != null)
                {
                    await File.WriteAllBytesAsync(outputPath, memoryStream.ToArray());
                    var fileType = outputType.Equals("Library", StringComparison.OrdinalIgnoreCase) ? "Library" : "Executable";
                    Console.WriteLine($"{fileType} saved to: {outputPath}");
                    
                    // Create runtime configuration file only for executables
                    if (outputKind == OutputKind.ConsoleApplication)
                    {
                        await CreateRuntimeConfigAsync(outputPath);
                    }
                    
                    // Make the file executable on Unix systems (only for executables)
                    if (!OperatingSystem.IsWindows() && outputKind == OutputKind.ConsoleApplication)
                    {
                        File.SetUnixFileMode(outputPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    }
                }
                
                // Only try to execute if it's an executable and has a Main method
                if (outputKind == OutputKind.ConsoleApplication && sourceInfo.HasMainMethod)
                {
                    // Load and execute the assembly
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var assembly = AssemblyLoadContext.Default.LoadFromStream(memoryStream);
                    
                    // Find and invoke the Main method using the correct type name
                    var programType = assembly.GetType(mainTypeName);
                    if (programType == null)
                    {
                        // Try to find any class with a Main method
                        foreach (var type in assembly.GetTypes())
                        {
                            var mainMethodz = type.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                            if (mainMethodz != null)
                            {
                                programType = type;
                                break;
                            }
                        }
                    }
                    
                    if (programType == null)
                    {
                        Console.WriteLine($"Could not find {mainTypeName} type or any type with Main method");
                        return false;
                    }
                    
                    var mainMethod = programType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                    if (mainMethod == null)
                    {
                        Console.WriteLine("Could not find Main method");
                        return false;
                    }
                    // should never need this, but just in case
                    if (!mainMethod.IsStatic)
                    {
                        Console.WriteLine("Main method must be static, cannot execute.");
                        return false;
                    }
                    //Console.WriteLine("Executing compiled program:");
                    //Console.WriteLine("------------------------");

                    // Execute the Main method
                    var parameters = mainMethod.GetParameters();
                    if (parameters.Length == 0)
                    {
                        mainMethod.Invoke(null, null);
                    }
                    else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string[]))
                    {
                        mainMethod.Invoke(null, new object[] { new string[0] });
                    }
                    else
                    {
                        Console.WriteLine("Unsupported Main method signature");
                        return false;
                    }
                    
                    //Console.WriteLine("------------------------");
                    //Console.WriteLine("Program execution completed");
                }
                else
                {
                    Console.WriteLine(outputKind == OutputKind.DynamicallyLinkedLibrary 
                        ? "Library compiled successfully" 
                        : "Executable compiled successfully (no Main method found)");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Runtime error: {ex.Message}");
                return false;
            }
        }

        private SourceInfo ExtractSourceInfo(SyntaxTree syntaxTree)
        {
            var sourceInfo = new SourceInfo();
            var root = syntaxTree.GetCompilationUnitRoot();

            // Find namespace declarations
            var namespaceDecl = root.DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();

            if (namespaceDecl != null)
            {
                sourceInfo.Namespace = namespaceDecl.Name.ToString();
            }

            // Find all class declarations
            var classDeclarations = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .ToList();

            foreach (var classDecl in classDeclarations)
            {
                var className = classDecl.Identifier.ValueText;
                sourceInfo.AllClasses.Add(className);

                // Check if this class has a Main method
                var hasMainMethod = classDecl.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Any(m => m.Identifier.ValueText == "Main" && 
                             m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

                if (hasMainMethod)
                {
                    sourceInfo.MainClassName = className;
                    sourceInfo.HasMainMethod = true;
                }
            }

            // If no class with Main method found, use the first class as default
            if (sourceInfo.MainClassName == null && sourceInfo.AllClasses.Count > 0)
            {
                sourceInfo.MainClassName = sourceInfo.AllClasses.First();
            }
            // if (_verboseMode)
            // {
            //     Console.WriteLine($"Found {sourceInfo.AllClasses.Count} classes: {string.Join(", ", sourceInfo.AllClasses)}");
            // Console.WriteLine($"Extracted source info: Namespace='{sourceInfo.Namespace}', MainClass='{sourceInfo.MainClassName}', HasMain={sourceInfo.HasMainMethod}");
            // }
            
            return sourceInfo;
        }
        
        private static MetadataReference[] GetAssemblyReferences()
        {
            var references = new List<MetadataReference>();
            
            // Use only the basic, compatible references
            references.AddRange(new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                // add mscorlib and system assemblies
                MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.IEnumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                // MetadataReference.CreateFromFile(typeof(System.Text.StringBuilder).Assembly.Location),
                // MetadataReference.CreateFromFile(typeof(System.ComponentModel.Component).Assembly.Location)
            });
            
            // Add additional assemblies if they exist
            try
            {
                references.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location));
            }
            catch { }
            
            try
            {
                references.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location));
            }
            catch { }
            
            return references.ToArray();
        }
        
        public async Task<byte[]?> CompileToBytes(string csharpCode)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
                
                var references = GetAssemblyReferences();
                
                var compilation = CSharpCompilation.Create(
                    "GeneratedAssembly",
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(OutputKind.ConsoleApplication));
                
                using var memoryStream = new MemoryStream();
                var emitResult = compilation.Emit(memoryStream);
                
                if (!emitResult.Success)
                {
                    foreach (var diagnostic in emitResult.Diagnostics)
                    {
                        if (diagnostic.Severity == DiagnosticSeverity.Error)
                        {
                            Console.WriteLine($"Compilation Error: {diagnostic.GetMessage()}");
                        }
                    }
                    return null;
                }
                
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compilation failed: {ex.Message}");
                return null;
            }
        }
        
        public async Task<bool> CompileToExecutable(string csharpCode, string outputPath, string? rootNamespace = null, string? className = null, string outputType = "Exe")
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
                
                // Extract source information
                var sourceInfo = ExtractSourceInfo(syntaxTree);
                
                var references = GetAssemblyReferences();
                
                // Use extracted info or provided parameters or defaults
                var actualRootNamespace = rootNamespace ?? sourceInfo.Namespace ?? "Generated";
                var actualClassName = className ?? sourceInfo.MainClassName ?? "Program";
                var mainTypeName = $"{actualRootNamespace}.{actualClassName}";
                
                // Determine output kind and file extension
                var outputKind = outputType.Equals("Library", StringComparison.OrdinalIgnoreCase) 
                    ? OutputKind.DynamicallyLinkedLibrary 
                    : OutputKind.ConsoleApplication;
                
                // Adjust output path extension based on output type
                if (outputType.Equals("Library", StringComparison.OrdinalIgnoreCase))
                {
                    outputPath = Path.ChangeExtension(outputPath, ".dll");
                }
                else
                {
                    outputPath = Path.ChangeExtension(outputPath, ".exe");
                }
                
                // Create build directory
                var buildDir = Path.Combine(Path.GetDirectoryName(outputPath)!, "build");
                Directory.CreateDirectory(buildDir);
                
                var finalPath = Path.Combine(buildDir, Path.GetFileName(outputPath));
                
                var compilation = CSharpCompilation.Create(
                    Path.GetFileNameWithoutExtension(outputPath),
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(
                        outputKind,
                        mainTypeName: outputKind == OutputKind.ConsoleApplication && sourceInfo.HasMainMethod ? mainTypeName : null));
                
                var emitResult = compilation.Emit(finalPath);
                
                if (!emitResult.Success)
                {
                    Console.WriteLine("Compilation failed:");
                    foreach (var diagnostic in emitResult.Diagnostics)
                    {
                        if (diagnostic.Severity == DiagnosticSeverity.Error)
                        {
                            Console.WriteLine($"  Error: {diagnostic.GetMessage()}");
                        }
                    }
                    return false;
                }
                
                // Copy required assemblies to build directory
                await CopyRequiredAssemblies(buildDir);
                
                // Create runtime configuration file only for executables
                if (outputKind == OutputKind.ConsoleApplication)
                {
                    await CreateRuntimeConfigAsync(finalPath);
                    Console.WriteLine($"Executable created: {finalPath}");
                    Console.WriteLine($"Run with: dotnet \"{finalPath}\"");
                }
                else
                {
                    Console.WriteLine($"Library created: {finalPath}");
                }
                
                Console.WriteLine($"Build directory: {buildDir}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compilation failed: {ex.Message}");
                return false;
            }
        }
        
        private async Task CopyRequiredAssemblies(string buildDir)
        {
            // No assembly copying needed - let runtime handle dependencies
            Console.WriteLine("Skipping assembly copying - using framework dependencies");
        }
        
        private async Task CreateRuntimeConfigAsync(string executablePath)
        {
            var runtimeConfigPath = Path.ChangeExtension(executablePath, ".runtimeconfig.json");
            
            // Get the actual runtime version
            var runtimeVersion = Environment.Version;
            var tfm = $"net{runtimeVersion.Major}.{runtimeVersion.Minor}";
            
            var runtimeConfig = new
            {
                runtimeOptions = new
                {
                    tfm = tfm,
                    framework = new
                    {
                        name = "Microsoft.NETCore.App",
                        version = $"{runtimeVersion.Major}.{runtimeVersion.Minor}.0"
                    },
                    configProperties = new
                    {
                        System_Runtime_Serialization_EnableUnsafeBinaryFormatterSerialization = false
                    }
                }
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(runtimeConfig, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(runtimeConfigPath, json);
            Console.WriteLine($"Runtime config created: {runtimeConfigPath}");
        }
    }
}
