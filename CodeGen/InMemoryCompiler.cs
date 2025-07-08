using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace uhigh.Net.CodeGen
{
    /// <summary>
    /// The in memory compiler class
    /// </summary>
    public class InMemoryCompiler
    {
        /// <summary>
        /// The base directory
        /// </summary>
        private static readonly string DefaultStdLibPath = Path.Combine(AppContext.BaseDirectory, "stdlib");
        /// <summary>
        /// The std lib path
        /// </summary>
        private readonly string _stdLibPath;
        
        // Add caching for assemblies and references
        /// <summary>
        /// The assembly cache
        /// </summary>
        private static readonly ConcurrentDictionary<string, byte[]> _assemblyCache = new();
        /// <summary>
        /// The reference cache
        /// </summary>
        private static readonly ConcurrentDictionary<string, MetadataReference[]> _referenceCache = new();
        /// <summary>
        /// The compilation lock
        /// </summary>
        private static readonly object _compilationLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCompiler"/> class
        /// </summary>
        /// <param name="stdLibPath">The std lib path</param>
        public InMemoryCompiler(string? stdLibPath = null)
        {
            _stdLibPath = stdLibPath ?? DefaultStdLibPath;
        }

        /// <summary>
        /// The source info class
        /// </summary>
        private class SourceInfo
        {
            /// <summary>
            /// Gets or sets the value of the namespace
            /// </summary>
            public string? Namespace { get; set; }
            /// <summary>
            /// Gets or sets the value of the main class name
            /// </summary>
            public string? MainClassName { get; set; }
            /// <summary>
            /// Gets or sets the value of the all classes
            /// </summary>
            public List<string> AllClasses { get; set; } = new();
            /// <summary>
            /// Gets or sets the value of the has main method
            /// </summary>
            public bool HasMainMethod { get; set; }
            /// <summary>
            /// Gets or sets the value of the has main function
            /// </summary>
            public bool HasMainFunction { get; set; } // Add this to track μHigh main functions
        }

        /// <summary>
        /// Gets the assembly references using the specified std lib path
        /// </summary>
        /// <param name="stdLibPath">The std lib path</param>
        /// <param name="additionalAssemblies">The additional assemblies</param>
        /// <returns>The references array</returns>
        private static MetadataReference[] GetAssemblyReferences(string? stdLibPath = null, List<string>? additionalAssemblies = null)
        {
            // Create cache key
            var cacheKey = $"{stdLibPath ?? "default"}:{string.Join(";", additionalAssemblies ?? new List<string>())}";
            
            if (_referenceCache.TryGetValue(cacheKey, out var cachedReferences))
            {
                return cachedReferences;
            }
            
            var references = new List<MetadataReference>();
            
            // Use only the basic, compatible references
            references.AddRange(new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.IEnumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(InMemoryCompiler).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(StdLib.Temporal<>).Assembly.Location)
            });

            // Add μHigh compiler assembly (uhigh.dll) if present
            var uhighDllPath = Path.Combine(AppContext.BaseDirectory, "uhigh.dll");
            if (File.Exists(uhighDllPath))
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(uhighDllPath));
                }
                catch { /* ignore if already loaded or error */ }
            }

            // Add standard library references with caching
            var stdLibReferences = GetStandardLibraryReferences(stdLibPath);
            references.AddRange(stdLibReferences);
            
            // Add NuGet package references
            if (additionalAssemblies != null)
            {
                foreach (var assembly in additionalAssemblies)
                {
                    try
                    {
                        if (File.Exists(assembly))
                        {
                            references.Add(MetadataReference.CreateFromFile(assembly));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not load assembly {Path.GetFileName(assembly)}: {ex.Message}");
                    }
                }
            }
            
            // Add additional system assemblies safely
            TryAddSystemAssembly(references, "System.Runtime");
            TryAddSystemAssembly(references, "System.Collections");
            
            var referencesArray = references.ToArray();
            _referenceCache[cacheKey] = referencesArray;
            return referencesArray;
        }

        /// <summary>
        /// Tries the add system assembly using the specified references
        /// </summary>
        /// <param name="references">The references</param>
        /// <param name="assemblyName">The assembly name</param>
        private static void TryAddSystemAssembly(List<MetadataReference> references, string assemblyName)
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(Assembly.Load(assemblyName).Location));
            }
            catch
            {
                // Ignore if assembly cannot be loaded
            }
        }

        /// <summary>
        /// Gets the standard library references using the specified std lib path
        /// </summary>
        /// <param name="stdLibPath">The std lib path</param>
        /// <returns>The references</returns>
        private static List<MetadataReference> GetStandardLibraryReferences(string? stdLibPath = null)
        {
            var references = new List<MetadataReference>();
            var libsPath = stdLibPath ?? DefaultStdLibPath;

            if (!Directory.Exists(libsPath))
            {
                // Try alternative paths for global tool installation
                var alternativePaths = new[]
                {
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "stdlib"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".uhigh", "stdlib"),
                    "/usr/local/share/uhigh/stdlib",
                    "/opt/uhigh/stdlib"
                };

                foreach (var altPath in alternativePaths)
                {
                    if (Directory.Exists(altPath))
                    {
                        libsPath = altPath;
                        break;
                    }
                }

                if (!Directory.Exists(libsPath))
                {
                    // Don't show warning for REPL - it's expected that stdlib might not exist
                    // Console.WriteLine($"Warning: Standard library directory not found at: {libsPath}");
                    return references;
                }
            }

            var dllFiles = Directory.GetFiles(libsPath, "*.dll", SearchOption.AllDirectories);
            foreach (var dllFile in dllFiles)
            {
                try
                {
                    var reference = MetadataReference.CreateFromFile(dllFile);
                    references.Add(reference);
                    // Console.WriteLine($"Added standard library: {Path.GetFileName(dllFile)}");
                }
                catch (Exception)
                {
                    // Console.WriteLine($"Warning: Could not load standard library {Path.GetFileName(dllFile)}: {ex.Message}");
                }
            }

            return references;
        }

        /// <summary>
        /// Compiles the and run using the specified csharp code
        /// </summary>
        /// <param name="csharpCode">The csharp code</param>
        /// <param name="outputPath">The output path</param>
        /// <param name="rootNamespace">The root namespace</param>
        /// <param name="className">The class name</param>
        /// <param name="outputType">The output type</param>
        /// <param name="additionalAssemblies">The additional assemblies</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> CompileAndRun(string csharpCode, string? outputPath = null, string? rootNamespace = null, string? className = null, string outputType = "Exe", List<string>? additionalAssemblies = null)
        {
            try
            {
                // Create hash for caching
                var codeHash = csharpCode.GetHashCode().ToString();
                var cacheKey = $"{codeHash}:{rootNamespace}:{className}";
                
                byte[] assemblyBytes;
                
                // Check cache first for identical code
                if (_assemblyCache.TryGetValue(cacheKey, out var cachedBytes))
                {
                    assemblyBytes = cachedBytes;
                }
                else
                {
                    // Compile with better error handling
                    lock (_compilationLock)
                    {
                        assemblyBytes = CompileToAssemblyBytes(csharpCode, rootNamespace, className, outputType, additionalAssemblies)!;
                        if (assemblyBytes == null) return false;
                        
                        // Cache successful compilation
                        _assemblyCache[cacheKey] = assemblyBytes;
                    }
                }
                
                // Save to file if requested
                if (outputPath != null)
                {
                    await File.WriteAllBytesAsync(outputPath, assemblyBytes);
                    var fileType = outputType.Equals("Library", StringComparison.OrdinalIgnoreCase) ? "Library" : "Executable";
                    Console.WriteLine($"{fileType} saved to: {outputPath}");
                    
                    if (outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase))
                    {
                        await CreateRuntimeConfigAsync(outputPath);
                    }
                    
                    if (!OperatingSystem.IsWindows() && outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase))
                    {
                        File.SetUnixFileMode(outputPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    }
                }
                
                // Execute if it's an executable
                if (outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase))
                {
                    return await ExecuteAssembly(assemblyBytes, rootNamespace, className);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compilation/execution error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Compiles the to assembly bytes using the specified csharp code
        /// </summary>
        /// <param name="csharpCode">The csharp code</param>
        /// <param name="rootNamespace">The root namespace</param>
        /// <param name="className">The class name</param>
        /// <param name="outputType">The output type</param>
        /// <param name="additionalAssemblies">The additional assemblies</param>
        /// <returns>The byte array</returns>
        private byte[]? CompileToAssemblyBytes(string csharpCode, string? rootNamespace, string? className, string outputType, List<string>? additionalAssemblies)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
                var sourceInfo = ExtractSourceInfo(syntaxTree);
                var references = GetAssemblyReferences(_stdLibPath, additionalAssemblies);
                
                var actualRootNamespace = rootNamespace ?? sourceInfo.Namespace;
                var actualClassName = className ?? sourceInfo.MainClassName ?? "Program";
                
                string? mainTypeName = null;
                if (actualRootNamespace != null)
                {
                    mainTypeName = $"{actualRootNamespace}.{actualClassName}";
                }
                else
                {
                    mainTypeName = actualClassName;
                }
                
                var outputKind = outputType.Equals("Library", StringComparison.OrdinalIgnoreCase) 
                    ? OutputKind.DynamicallyLinkedLibrary 
                    : OutputKind.ConsoleApplication;
                
                // Use unique assembly name to avoid conflicts
                var uniqueAssemblyName = $"GeneratedAssembly_{DateTime.Now.Ticks}_{Guid.NewGuid().ToString("N")[..8]}";
                
                var compilation = CSharpCompilation.Create(
                    uniqueAssemblyName,
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(
                        outputKind,
                        mainTypeName: outputKind == OutputKind.ConsoleApplication && sourceInfo.HasMainMethod ? mainTypeName : null,
                        optimizationLevel: OptimizationLevel.Release)); // Use release mode for better performance
                
                using var memoryStream = new MemoryStream();
                var emitResult = compilation.Emit(memoryStream);
                
                if (!emitResult.Success)
                {
                    Console.WriteLine("Compilation failed:");
                    foreach (var diagnostic in emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        Console.WriteLine($"  Error: {diagnostic.GetMessage()}");
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

        /// <summary>
        /// Executes the assembly using the specified assembly bytes
        /// </summary>
        /// <param name="assemblyBytes">The assembly bytes</param>
        /// <param name="rootNamespace">The root namespace</param>
        /// <param name="className">The class name</param>
        /// <exception cref="InvalidOperationException">Unsupported Main method signature</exception>
        /// <returns>A task containing the bool</returns>
        private async Task<bool> ExecuteAssembly(byte[] assemblyBytes, string? rootNamespace, string? className)
        {
            try
            {
                using var memoryStream = new MemoryStream(assemblyBytes);
                var assembly = AssemblyLoadContext.Default.LoadFromStream(memoryStream);
                
                var actualClassName = className ?? "Program";
                var mainTypeName = rootNamespace != null ? $"{rootNamespace}.{actualClassName}" : actualClassName;
                
                var programType = assembly.GetType(mainTypeName);
                if (programType == null)
                {
                    // Try to find any class with a Main method
                    foreach (var type in assembly.GetTypes())
                    {
                        var mainMethod = type.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                        if (mainMethod != null)
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
                
                var mainMethods = programType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                if (mainMethods == null)
                {
                    Console.WriteLine("Could not find Main method");
                    return false;
                }

                // Execute with timeout protection
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                await Task.Run(() =>
                {
                    var parameters = mainMethods.GetParameters();
                    if (parameters.Length == 0)
                    {
                        mainMethods.Invoke(null, null);
                    }
                    else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string[]))
                    {
                        mainMethods.Invoke(null, new object[] { new string[0] });
                    }
                    else
                    {
                        throw new InvalidOperationException("Unsupported Main method signature");
                    }
                }, cancellationTokenSource.Token);
                
                return true;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Execution timed out after 30 seconds");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Runtime error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extracts the source info using the specified syntax tree
        /// </summary>
        /// <param name="syntaxTree">The syntax tree</param>
        /// <returns>The source info</returns>
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

                // Check if this class has a Main method (C# style)
                var hasMainMethod = classDecl.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Any(m => m.Identifier.ValueText == "Main" && 
                             m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

                if (hasMainMethod)
                {
                    sourceInfo.MainClassName = className;
                    sourceInfo.HasMainMethod = true;
                }

                // Also check for μHigh-style main method (lower case)
                var hasMainFunction = classDecl.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Any(m => m.Identifier.ValueText == "main" && 
                             m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

                if (hasMainFunction)
                {
                    sourceInfo.MainClassName = className;
                    sourceInfo.HasMainFunction = true;
                    sourceInfo.HasMainMethod = true; // Treat as main method for execution
                }
            }

            // Check for global main function (not in a class)
            var globalMethods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => !m.Ancestors().OfType<ClassDeclarationSyntax>().Any());

            foreach (var method in globalMethods)
            {
                if ((method.Identifier.ValueText == "Main" || method.Identifier.ValueText == "main") &&
                    method.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)))
                {
                    sourceInfo.HasMainMethod = true;
                    if (method.Identifier.ValueText == "main")
                    {
                        sourceInfo.HasMainFunction = true;
                    }
                }
            }

            // If no class with Main method found, use the first class as default
            if (sourceInfo.MainClassName == null && sourceInfo.AllClasses.Count > 0)
            {
                sourceInfo.MainClassName = sourceInfo.AllClasses.First();
            }
            
            return sourceInfo;
        }
        
        /// <summary>
        /// Compiles the to bytes using the specified csharp code
        /// </summary>
        /// <param name="csharpCode">The csharp code</param>
        /// <returns>A task containing the byte array</returns>
        public Task<byte[]?> CompileToBytes(string csharpCode)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
                
                var references = GetAssemblyReferences(_stdLibPath);
                
                var compilation = CSharpCompilation.Create(
                    "dotHighAssembly",
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
                    Environment.Exit(1); // exit on fail
                }
                
                return Task.FromResult<byte[]?>(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compilation failed: {ex.Message}");
                return Task.FromResult<byte[]?>(null);
            }
        }
        
        /// <summary>
        /// Compiles the to executable using the specified csharp code
        /// </summary>
        /// <param name="csharpCode">The csharp code</param>
        /// <param name="outputPath">The output path</param>
        /// <param name="rootNamespace">The root namespace</param>
        /// <param name="className">The class name</param>
        /// <param name="outputType">The output type</param>
        /// <param name="additionalAssemblies">The additional assemblies</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> CompileToExecutable(string csharpCode, string outputPath, string? rootNamespace = null, string? className = null, string outputType = "Exe", List<string>? additionalAssemblies = null)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
                
                // Extract source information
                var sourceInfo = ExtractSourceInfo(syntaxTree);
                
                var references = GetAssemblyReferences(_stdLibPath, additionalAssemblies);
                
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
                    
                    Environment.Exit(1);    
                }
                
                // Copy required assemblies to build directory
                await CopyRequiredAssemblies(buildDir, additionalAssemblies);
                
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
        
        /// <summary>
        /// Copies the required assemblies using the specified build dir
        /// </summary>
        /// <param name="buildDir">The build dir</param>
        /// <param name="additionalAssemblies">The additional assemblies</param>
        private Task CopyRequiredAssemblies(string buildDir, List<string>? additionalAssemblies = null)
        {
            // Copy standard library DLLs to build directory
            if (Directory.Exists(_stdLibPath))
            {
                var stdLibDlls = Directory.GetFiles(_stdLibPath, "*.dll", SearchOption.AllDirectories);
                foreach (var dll in stdLibDlls)
                {
                    try
                    {
                        var fileName = Path.GetFileName(dll);
                        var destPath = Path.Combine(buildDir, fileName);
                        if (!File.Exists(destPath))
                        {
                            File.Copy(dll, destPath);
                            Console.WriteLine($"Copied standard library: {fileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not copy standard library {Path.GetFileName(dll)}: {ex.Message}");
                    }
                }
            }
            
            // Copy μHigh compiler assembly (uhigh.dll) to build directory if present
            var uhighDllPath = Path.Combine(AppContext.BaseDirectory, "uhigh.dll");
            if (File.Exists(uhighDllPath))
            {
                var destPath = Path.Combine(buildDir, "uhigh.dll");
                if (!File.Exists(destPath))
                {
                    File.Copy(uhighDllPath, destPath);
                    Console.WriteLine("Copied μHigh compiler assembly: uhigh.dll");
                }
            }

            // Copy NuGet package assemblies to build directory
            if (additionalAssemblies != null)
            {
                foreach (var assembly in additionalAssemblies)
                {
                    try
                    {
                        if (File.Exists(assembly))
                        {
                            var fileName = Path.GetFileName(assembly);
                            var destPath = Path.Combine(buildDir, fileName);
                            if (!File.Exists(destPath))
                            {
                                File.Copy(assembly, destPath);
                                Console.WriteLine($"Copied NuGet assembly: {fileName}");
                            }
                            
                            // Also copy any related files (pdb, xml, etc.)
                            var assemblyDir = Path.GetDirectoryName(assembly);
                            if (assemblyDir != null)
                            {
                                var baseName = Path.GetFileNameWithoutExtension(assembly);
                                var relatedFiles = Directory.GetFiles(assemblyDir, $"{baseName}.*")
                                    .Where(f => !f.Equals(assembly, StringComparison.OrdinalIgnoreCase));
                                
                                foreach (var relatedFile in relatedFiles)
                                {
                                    var relatedFileName = Path.GetFileName(relatedFile);
                                    var relatedDestPath = Path.Combine(buildDir, relatedFileName);
                                    if (!File.Exists(relatedDestPath))
                                    {
                                        try
                                        {
                                            File.Copy(relatedFile, relatedDestPath);
                                            Console.WriteLine($"Copied related file: {relatedFileName}");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Warning: Could not copy related file {relatedFileName}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not copy NuGet assembly {Path.GetFileName(assembly)}: {ex.Message}");
                    }
                }
            }
            
            Console.WriteLine("Required assemblies copied to build directory");
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Creates the runtime config using the specified executable path
        /// </summary>
        /// <param name="executablePath">The executable path</param>
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
