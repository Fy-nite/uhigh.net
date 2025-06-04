using Wake.Net.Lexer;
using Wake.Net.Parser;
using Wake.Net.CodeGen;
using Wake.Net.Diagnostics;
using System.Diagnostics;

namespace Wake.Net
{
    public class Compiler
    {
        private readonly bool _verboseMode;

        public Compiler(bool verboseMode = false)
        {
            _verboseMode = verboseMode;
        }

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

                // Use in-memory compiler
                var inMemoryCompiler = new InMemoryCompiler();
                
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
                
                var inMemoryCompiler = new InMemoryCompiler();
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
                
                var inMemoryCompiler = new InMemoryCompiler();
                var success = await inMemoryCompiler.CompileAndRun(csharpCode, null, "Generated");
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
                var lexer = new Lexer.Lexer(source, diagnostics);
                var tokens = lexer.Tokenize();
                
                if (diagnostics.HasErrors)
                {
                    throw new Exception("Tokenization failed");
                }
                
                // Parse
                var parser = new Parser.Parser(tokens, diagnostics);
                var ast = parser.Parse();
                
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

        public async Task<bool> CompileProject(string projectPath, string? outputFile = null)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, projectPath);
            
            try
            {
                // Load project file
                var project = await ProjectFile.LoadAsync(projectPath, diagnostics);
                if (project == null)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                diagnostics.ReportInfo($"Compiling project: {project.Name}");

                // Compile all source files and combine
                var allCSharpCode = new List<string>();
                var hasMainMethod = false;
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
                    var csharpCode = CompileToCS(source, diagnostics, projectRootNamespace, projectClassName);
                    
                    if (diagnostics.HasErrors)
                    {
                        diagnostics.PrintSummary();
                        return false;
                    }

                    allCSharpCode.Add(csharpCode);
                    
                    // Check if this file contains a main method
                    if (csharpCode.Contains("static void Main") || csharpCode.Contains("static async Task Main"))
                    {
                        hasMainMethod = true;
                    }
                }

                if (!hasMainMethod && project.OutputType == "Exe")
                {
                    diagnostics.ReportError("No main method found in project files");
                    diagnostics.PrintSummary();
                    return false;
                }

                // Combine all C# code
                var combinedCode = string.Join("\n\n", allCSharpCode);

                // Use in-memory compiler with project root namespace and class name
                var inMemoryCompiler = new InMemoryCompiler();
                
                if (outputFile != null)
                {
                    var success = await inMemoryCompiler.CompileToExecutable(combinedCode, outputFile, projectRootNamespace, projectClassName);
                    diagnostics.PrintSummary();
                    return success;
                }
                else
                {
                    var success = await inMemoryCompiler.CompileAndRun(combinedCode, null, projectRootNamespace, projectClassName);
                    diagnostics.PrintSummary();
                    return success;
                }
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Project compilation failed: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

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

                var allCSharpCode = new List<string>();

                // Compile each source file
                foreach (var sourceFile in project.SourceFiles)
                {
                    if (!File.Exists(sourceFile))
                    {
                        diagnostics.ReportError($"Source file not found: {sourceFile}");
                        continue;
                    }

                    var source = await File.ReadAllTextAsync(sourceFile);
                    var csharpCode = CompileToCS(source, diagnostics, project.RootNamespace ?? project.Name);
                    
                    if (diagnostics.HasErrors)
                    {
                        diagnostics.PrintSummary();
                        return false;
                    }

                    allCSharpCode.Add(csharpCode);
                }

                // Combine and save C# code
                var combinedCode = string.Join("\n\n", allCSharpCode);
                var outputFileName = $"{project.Name}.cs";
                var outputPath = Path.Combine(outputFolder, outputFileName);
                
                await File.WriteAllTextAsync(outputPath, combinedCode);
                Console.WriteLine($"C# code saved to: {outputPath}");

                // Create project file with dependencies
                await CreateProjectFileFromWakeProject(outputFolder, project);
                
                if (_verboseMode)
                {
                    Console.WriteLine("Generated C# code:");
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

        private async Task CreateProjectFileFromWakeProject(string outputFolder, WakeProject wakeProject)
        {
            var dependencies = "";
            if (wakeProject.Dependencies.Count > 0)
            {
                var deps = wakeProject.Dependencies.Select(d => $@"    <PackageReference Include=""{d.Name}"" Version=""{d.Version}"" />");
                dependencies = $@"
  <ItemGroup>
{string.Join("\n", deps)}
  </ItemGroup>";
            }

            var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>{wakeProject.OutputType}</OutputType>
    <TargetFramework>{wakeProject.Target}</TargetFramework>
    <AssemblyName>{wakeProject.Name}</AssemblyName>
    <RootNamespace>{wakeProject.RootNamespace ?? wakeProject.Name}</RootNamespace>
    <Nullable>{(wakeProject.Nullable ? "enable" : "disable")}</Nullable>
    <Version>{wakeProject.Version}</Version>
  </PropertyGroup>{dependencies}

</Project>";

            var projectPath = Path.Combine(outputFolder, $"{wakeProject.Name}.csproj");
            await File.WriteAllTextAsync(projectPath, projectContent);
            if (_verboseMode)
            {
                Console.WriteLine("Generated project file:");
            Console.WriteLine($"Project file created: {projectPath}");
            Console.WriteLine($"Build with: dotnet build \"{projectPath}\"");
            Console.WriteLine($"Run with: dotnet run --project \"{projectPath}\"");
                Console.WriteLine(projectContent);
            }
        }

        public async Task<bool> CreateProject(string projectName, string? projectDir = null, string? description = null, string? author = null, string outputType = "Exe", string target = "net9.0")
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode);
            
            try
            {
                projectDir ??= Environment.CurrentDirectory;
                var fullProjectDir = Path.Combine(projectDir, projectName);

                var project = new WakeProject
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
                    Console.WriteLine($"Project file: {Path.Combine(fullProjectDir, $"{projectName}.wakeproj")}");
                    Console.WriteLine($"Main file: {Path.Combine(fullProjectDir, "main.wake")}");
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
                    var defaultContent = $@"// {fileName}.wake - Generated by Wake.Net

// Add your Wake.Net code here
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
    }
}
