using uhigh.Net;
using uhigh.Net.CommandLine;
using CommandLine;

/// <summary>
/// The entry point class
/// </summary>
public class EntryPoint
{
    /// <summary>
    /// Main the args
    /// </summary>
    /// <param name="args">The args</param>
    public static async Task Main(string[] args)
    {
        // Handle the case where user provides a file directly without a verb
        if (args.Length > 0 && !args[0].StartsWith("-") && 
            (args[0].EndsWith(".uh") || args[0].EndsWith(".uhigh")) &&
            !IsKnownVerb(args[0]))
        {
            // Convert to compile command
            var compileArgs = new List<string> { "compile" };
            compileArgs.AddRange(args);
            args = compileArgs.ToArray();
        }

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
            TestOptions,
            ReplOptions>(args)
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
                (ReplOptions opts) => HandleReplCommand(opts),
                
                errors => HandleParseError(errors));
    }

    /// <summary>
    /// Ises the known verb using the specified arg
    /// </summary>
    /// <param name="arg">The arg</param>
    /// <returns>The bool</returns>
    private static bool IsKnownVerb(string arg)
    {
        var knownVerbs = new[] { 
            "compile", "create", "build", "run", "info", "add-file", 
            "add-package", "install-packages", "search-packages", 
            "list-packages", "restore-packages", "ast", "lsp", "test", "repl" 
        };
        return knownVerbs.Contains(arg.ToLower());
    }

    /// <summary>
    /// Handles the compile command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
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

    /// <summary>
    /// Handles the create command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
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

    /// <summary>
    /// Handles the build command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
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

    /// <summary>
    /// Handles the run command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
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

    /// <summary>
    /// Handles the info command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
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

    /// <summary>
    /// Handles the add file command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
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

    /// <summary>
    /// Handles the add package command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
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

    /// <summary>
    /// Handles the install packages command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
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

    /// <summary>
    /// Handles the search packages command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
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
            
            Console.WriteLine($"Found {packages.Count} packages:");
            foreach (var package in packages)
            {
                // Console.WriteLine($"  {package.Name} v{package.Version}");
                if (!string.IsNullOrEmpty(package.Description))
                {
                    Console.WriteLine($"    {package.Description}");
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

    /// <summary>
    /// Handles the list packages command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
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
            
            if (project.Dependencies.Count == 0)
            {
                Console.WriteLine("No packages found in project");
                return 0;
            }
            
            Console.WriteLine($"Packages in {project.Name}:");
            foreach (var dep in project.Dependencies)
            {
                Console.WriteLine($"  {dep.Name} v{dep.Version}");
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            WriteError($"List packages failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Handles the restore packages command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandleRestorePackagesCommand(RestorePackagesOptions options)
    {
        try
        {
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
                Console.WriteLine("Packages restored successfully");
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

    /// <summary>
    /// Handles the ast command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandleAstCommand(AstOptions options)
    {
        try
        {
            var compiler = new Compiler(options.Verbose, options.StdLibPath);
            var success = await compiler.PrintAST(options.SourceFile);
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"AST command failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Handles the lsp command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandleLspCommand(LspOptions options)
    {
        // For now, redirect to the simple LSP test
        await SimpleLSPTest.TestMain(new[] { "simple-lsp" });
        return 0;
    }

    /// <summary>
    /// Handles the test command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandleTestCommand(TestOptions options)
    {
        try
        {
            Console.WriteLine("Running μHigh Tests...");
            Console.WriteLine();
            
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

    /// <summary>
    /// Handles the repl command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandleReplCommand(ReplOptions options)
    {
        try
        {
            Console.WriteLine("Starting μHigh REPL...");
            
            var repl = new uhigh.Net.Repl.ReplSession(
                verboseMode: options.Verbose,
                stdLibPath: options.StdLibPath,
                saveCSharpTo: options.SaveCSharpTo);
            
            await repl.StartAsync();
            return 0;
        }
        catch (Exception ex)
        {
            WriteError($"REPL failed: {ex.Message}");
            if (options.Verbose)
            {
                Console.WriteLine($"Stack trace:\n{ex.StackTrace}");
            }
            return 1;
        }
    }

    /// <summary>
    /// Handles the parse error using the specified errors
    /// </summary>
    /// <param name="errors">The errors</param>
    /// <returns>A task containing the int</returns>
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

    /// <summary>
    /// Writes the error using the specified message
    /// </summary>
    /// <param name="message">The message</param>
    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}