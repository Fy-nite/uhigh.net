using uhigh.Net;
using uhigh.Net.CommandLine;
using CommandLine;

public class EntryPoint
{
    public static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<
            CompileOptions, 
            CreateOptions, 
            BuildOptions, 
            RunOptions, 
            InfoOptions, 
            AddFileOptions, 
            AddPackageOptions, 
            InstallPackagesOptions,
            SearchPackagesOptions,
            ListPackagesOptions,
            RestorePackagesOptions,
            AstOptions,
            LspOptions, 
            TestOptions>(args)
            .MapResult(
                (CompileOptions opts) => HandleCompileCommand(opts),
                (CreateOptions opts) => HandleCreateCommand(opts),
                (BuildOptions opts) => HandleBuildCommand(opts),
                (RunOptions opts) => HandleRunCommand(opts),
                (InfoOptions opts) => HandleInfoCommand(opts),
                (AddFileOptions opts) => HandleAddFileCommand(opts),
                (AddPackageOptions opts) => HandleAddPackageCommand(opts),
                (InstallPackagesOptions opts) => HandleInstallPackagesCommand(opts),
                (SearchPackagesOptions opts) => HandleSearchPackagesCommand(opts),
                (ListPackagesOptions opts) => HandleListPackagesCommand(opts),
                (RestorePackagesOptions opts) => HandleRestorePackagesCommand(opts),
                (AstOptions opts) => HandleAstCommand(opts),
                (LspOptions opts) => HandleLspCommand(opts),
                (TestOptions opts) => HandleTestCommand(opts),
                
                errors => HandleParseError(errors));
    }

    private static async Task<int> HandleCompileCommand(CompileOptions options)
    {
        try
        {
            var compiler = new Compiler(options.Verbose, options.StdLibPath);
            bool success;

            if (!File.Exists(options.SourceFile))
            {
                Console.WriteLine($"Error: Source file '{options.SourceFile}' not found");
                return 1;
            }

            if (!string.IsNullOrEmpty(options.SaveCSharpTo))
            {
                success = await compiler.SaveCSharpCode(options.SourceFile, options.SaveCSharpTo);
            }
            else if (options.RunInMemory || string.IsNullOrEmpty(options.OutputFile))
            {
                success = await compiler.CompileAndRunInMemory(options.SourceFile);
            }
            else
            {
                success = await compiler.CompileToExecutable(options.SourceFile, options.OutputFile);
            }

            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Compilation failed: {ex.Message}");
            if (options.Verbose)
            {
                Console.WriteLine($"Stack trace:\n{ex.StackTrace}");
            }
            return 1;
        }
    }

    private static async Task<int> HandleCreateCommand(CreateOptions options)
    {
        try
        {
            var compiler = new Compiler(options.Verbose, options.StdLibPath);
            var success = await compiler.CreateProject(
                options.ProjectName,
                options.Directory,
                options.Description,
                options.Author,
                options.OutputType,
                options.TargetFramework);

            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Project creation failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleBuildCommand(BuildOptions options)
    {
        try
        {
            var compiler = new Compiler(options.Verbose, options.StdLibPath);

            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file '{options.ProjectFile}' not found");
                return 1;
            }

            bool success;
            if (!string.IsNullOrEmpty(options.SaveCSharpTo))
            {
                Console.WriteLine($"Generating C# files to: {options.SaveCSharpTo}");
                success = await compiler.SaveCSharpCodeFromProject(options.ProjectFile, options.SaveCSharpTo);
                if (success)
                {
                    Console.WriteLine("Each μHigh source file has been converted to a separate C# file.");
                }
            }
            else
            {
                success = await compiler.CompileProject(options.ProjectFile, options.OutputFile);
            }
            
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Build failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleRunCommand(RunOptions options)
    {
        try
        {
            var compiler = new Compiler(options.Verbose, options.StdLibPath);

            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file '{options.ProjectFile}' not found");
                return 1;
            }

            bool success;
            if (!string.IsNullOrEmpty(options.SaveCSharpTo))
            {
                success = await compiler.SaveCSharpCodeFromProject(options.ProjectFile, options.SaveCSharpTo);
            }
            else
            {
                success = await compiler.CompileProject(options.ProjectFile);
            }
            
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Run failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleInfoCommand(InfoOptions options)
    {
        try
        {
            var compiler = new Compiler(options.Verbose, options.StdLibPath);

            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file '{options.ProjectFile}' not found");
                return 1;
            }

            var success = await compiler.ListProjectInfo(options.ProjectFile);
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Info command failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleAddFileCommand(AddFileOptions options)
    {
        try
        {
            var compiler = new Compiler(options.Verbose, options.StdLibPath);

            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file '{options.ProjectFile}' not found");
                return 1;
            }

            var success = await compiler.AddSourceFileToProject(options.ProjectFile, options.SourceFile, options.CreateFile);
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Add file failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleAddPackageCommand(AddPackageOptions options)
    {
        try
        {
            var compiler = new Compiler(options.Verbose, options.StdLibPath);

            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file '{options.ProjectFile}' not found");
                return 1;
            }

            var success = await compiler.AddPackageToProject(options.ProjectFile, options.PackageName, options.Version);
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Add package failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleInstallPackagesCommand(InstallPackagesOptions options)
    {
        try
        {
            Console.WriteLine($"Installing packages for project: {options.ProjectFile}");
            
            var project = await uhigh.Net.ProjectFile.LoadAsync(options.ProjectFile);
            if (project == null)
            {
                WriteError($"Project file '{options.ProjectFile}' not found or invalid");
                return 1;
            }

            var projectDir = Path.GetDirectoryName(options.ProjectFile) ?? "";
            var nugetManager = new uhigh.Net.NuGet.NuGetManager();
            var success = await nugetManager.RestorePackagesAsync(project, projectDir, force: true);
            
            if (success)
            {
                Console.WriteLine("All packages installed successfully");
                return 0;
            }
            else
            {
                WriteError("Some packages failed to install");
                return 1;
            }
        }
        catch (Exception ex)
        {
            WriteError($"Install packages failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleSearchPackagesCommand(SearchPackagesOptions options)
    {
        try
        {
            Console.WriteLine($"Searching for packages: {options.SearchTerm}");
            
            var nugetManager = new uhigh.Net.NuGet.NuGetManager();
            var packages = await nugetManager.SearchPackagesAsync(options.SearchTerm, options.Take);
            
            if (packages.Count == 0)
            {
                Console.WriteLine("No packages found");
                return 0;
            }

            Console.WriteLine($"\nFound {packages.Count} packages:");
            Console.WriteLine();
            
            foreach (var package in packages)
            {
                Console.WriteLine($"📦 {package.Id} v{package.Version}");
                if (!string.IsNullOrEmpty(package.Description))
                {
                    Console.WriteLine($"   {package.Description}");
                }
                if (package.Authors.Count > 0)
                {
                    Console.WriteLine($"   Authors: {string.Join(", ", package.Authors)}");
                }
                if (package.TotalDownloads > 0)
                {
                    Console.WriteLine($"   Downloads: {package.TotalDownloads:N0}");
                }
                if (!string.IsNullOrEmpty(package.ProjectUrl))
                {
                    Console.WriteLine($"   Project: {package.ProjectUrl}");
                }
                Console.WriteLine();
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            WriteError($"Package search failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleListPackagesCommand(ListPackagesOptions options)
    {
        try
        {
            var project = await uhigh.Net.ProjectFile.LoadAsync(options.ProjectFile);
            if (project == null)
            {
                WriteError($"Project file '{options.ProjectFile}' not found or invalid");
                return 1;
            }

            Console.WriteLine($"Packages in project '{project.Name}':");
            Console.WriteLine();
            
            if (project.Dependencies.Count == 0)
            {
                Console.WriteLine("No packages found");
                return 0;
            }

            foreach (var package in project.Dependencies)
            {
                Console.WriteLine($"📦 {package.Name} v{package.Version}");
                if (!string.IsNullOrEmpty(package.RequiredFor))
                {
                    Console.WriteLine($"   Required for: {package.RequiredFor}");
                }
                if (!string.IsNullOrEmpty(package.Source))
                {
                    Console.WriteLine($"   Source: {package.Source}");
                }
                if (package.CompileOnly)
                {
                    Console.WriteLine($"   Compile-time only");
                }
                Console.WriteLine();
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            WriteError($"List packages failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleRestorePackagesCommand(RestorePackagesOptions options)
    {
        try
        {
            Console.WriteLine($"Restoring packages for project: {options.ProjectFile}");
            
            var project = await uhigh.Net.ProjectFile.LoadAsync(options.ProjectFile);
            if (project == null)
            {
                WriteError($"Project file '{options.ProjectFile}' not found or invalid");
                return 1;
            }

            var projectDir = Path.GetDirectoryName(options.ProjectFile) ?? "";
            var nugetManager = new uhigh.Net.NuGet.NuGetManager();
            var success = await nugetManager.RestorePackagesAsync(project, projectDir, options.Force);
            
            if (success)
            {
                Console.WriteLine("All packages restored successfully");
                return 0;
            }
            else
            {
                WriteError("Some packages failed to restore");
                return 1;
            }
        }
        catch (Exception ex)
        {
            WriteError($"Restore packages failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleLspCommand(LspOptions options)
    {
        try
        {
            Console.WriteLine("Starting μHigh Language Server...");
            if (options.UseStdio)
            {
                Console.WriteLine("Using stdio for communication");
            }
            else if (options.Port.HasValue)
            {
                Console.WriteLine($"Listening on port {options.Port.Value}");
            }

            // TODO: Implement LSP server startup
            // var host = new uhigh.Net.LanguageServer.LanguageServerHost(
            //     Console.OpenStandardInput(), 
            //     Console.OpenStandardOutput());
            // await host.RunAsync();

            Console.WriteLine("LSP server implementation not yet available");
            return 0;
        }
        catch (Exception ex)
        {
            WriteError($"LSP server failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleTestCommand(TestOptions options)
    {
        try
        {
            Console.WriteLine("Running μHigh Compiler Tests");
            Console.WriteLine("============================");
            Console.WriteLine();

            if (options.ListTests)
            {
                Console.WriteLine("Available test suites:");
                Console.WriteLine("- Lexer tests");
                Console.WriteLine("- Parser tests");
                Console.WriteLine("- Code generation tests");
                Console.WriteLine("- Integration tests");
                return 0;
            }

            var testSuites = uhigh.Net.Testing.TestRunner.RunAllTests();
            uhigh.Net.Testing.TestRunner.PrintResults(testSuites);

            var totalFailed = testSuites.Sum(s => s.FailedCount);
            return totalFailed == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Test execution failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleAstCommand(AstOptions options)
    {
        try
        {
            var compiler = new Compiler(options.Verbose, options.StdLibPath);

            if (!File.Exists(options.SourceFile))
            {
                WriteError($"Source file '{options.SourceFile}' not found");
                return 1;
            }

            var success = await compiler.PrintAST(options.SourceFile);
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"AST generation failed: {ex.Message}");
            if (options.Verbose)
            {
                Console.WriteLine($"Stack trace:\n{ex.StackTrace}");
            }
            return 1;
        }
    }

    private static async Task<int> HandleParseError(IEnumerable<Error> errors)
    {
        var errorsList = errors.ToList();
        if (errorsList.Any(e => e is HelpRequestedError || e is VersionRequestedError))
        {
            return 0; // Help/version requested is not an error
        }
        
        Console.WriteLine("Command line parsing failed:");
        foreach (var error in errorsList)
        {
            Console.WriteLine($"  {error}");
        }
        return 1;
    }

    private static void WriteError(string message)
    {
        Console.Error.WriteLine(message);
    }
}