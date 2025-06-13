using Wake.Net;

public class EntryPoint
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("μHigh.Net Compiler");
            Console.WriteLine("Usage: uhigh.net <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  <source-file>                     # Compile and run μHigh.Net file");
            Console.WriteLine("  create <project-name> [options]   # Create new μHigh.Net project");
            Console.WriteLine("  build <project-file>              # Build μHigh.Net project");
            Console.WriteLine("  run <project-file>                # Run μHigh.Net project");
            Console.WriteLine("  info <project-file>               # Show project information");
            Console.WriteLine("  add-file <project-file> <file>    # Add source file to project");
            Console.WriteLine("  add-package <project-file> <name> <version> # Add package to project");
            Console.WriteLine();
            Console.WriteLine("File compilation options:");
            Console.WriteLine("  uhigh.net program.uh               # Compile and run in memory");
            Console.WriteLine("  uhigh.net program.uh program.exe   # Compile to executable");
            Console.WriteLine("  uhigh.net program.uh --run         # Compile and run in memory");
            Console.WriteLine("  uhigh.net program.uh --save path   # Save C# code to folder");
            Console.WriteLine();
            Console.WriteLine("Project creation options:");
            Console.WriteLine("  --type <Exe|Library>              # Output type (default: Exe)");
            Console.WriteLine("  --target <framework>              # Target framework (default: net9.0)");
            Console.WriteLine("  --description <text>              # Project description");
            Console.WriteLine("  --author <name>                   # Project author");
            Console.WriteLine("  --dir <path>                      # Project directory (default: current)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  uhigh.net create MyLibrary --type Library");
            Console.WriteLine("  uhigh.net build MyProject.wakeproj MyProject.dll");
            Console.WriteLine();
            Console.WriteLine("Syntax examples:");
            Console.WriteLine("  var obj = Program()               # Constructor call (no 'new' needed)");
            Console.WriteLine("  Console.WriteLine(\"Hello\")        # Static method call");
            Console.WriteLine("  [dotnetfunc] func Console.WriteLine(s: string) {} # .NET method declaration");
            Console.WriteLine();
            Console.WriteLine("Global options:");
            Console.WriteLine("  --verbose                         # Enable verbose output");
            return;
        }

        var verboseMode = args.Contains("--verbose");
        var compiler = new Compiler(verboseMode);
        
        var command = args[0].ToLower();
        bool success = false;

        try
        {
            switch (command)
            {
                case "create":
                    success = await HandleCreateCommand(args, compiler);
                    break;
                    
                case "build":
                    success = await HandleBuildCommand(args, compiler);
                    break;
                    
                case "run":
                    success = await HandleRunCommand(args, compiler);
                    break;
                    
                case "info":
                    success = await HandleInfoCommand(args, compiler);
                    break;
                    
                case "add-file":
                    success = await HandleAddFileCommand(args, compiler);
                    break;
                    
                case "add-package":
                    success = await HandleAddPackageCommand(args, compiler);
                    break;
                    
                case "test":
                    success = await HandleTestCommand(args, compiler);
                    break;
                    
                default:
                    // Treat as source file compilation
                    success = await HandleFileCompilation(args, compiler);
                    break;
            }
        }
        catch (Exception ex)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("error");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($": {ex.Message}");
            Console.ForegroundColor = originalColor;
            
            if (verboseMode)
            {
                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            }
            success = false;
        }
        
        Environment.Exit(success ? 0 : 1);
    }

    private static async Task<bool> HandleCreateCommand(string[] args, Compiler compiler)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Project name is required");
            Console.WriteLine("Usage: uhigh create <project-name> [options]");
            return false;
        }

        var projectName = args[1];
        string? description = null;
        string? author = null;
        string? projectDir = null;
        string outputType = "Exe";
        string target = "net9.0";

        // Parse options
        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--type" when i + 1 < args.Length:
                    outputType = args[++i];
                    break;
                case "--target" when i + 1 < args.Length:
                    target = args[++i];
                    break;
                case "--description" when i + 1 < args.Length:
                    description = args[++i];
                    break;
                case "--author" when i + 1 < args.Length:
                    author = args[++i];
                    break;
                case "--dir" when i + 1 < args.Length:
                    projectDir = args[++i];
                    break;
                case "--verbose":
                    // Already handled
                    break;
            }
        }

        return await compiler.CreateProject(projectName, projectDir, description, author, outputType, target);
    }

    private static async Task<bool> HandleBuildCommand(string[] args, Compiler compiler)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Project file is required");
            Console.WriteLine("Usage: uhigh build <project-file> [output-file]");
            return false;
        }

        var projectFile = args[1];
        string? outputFile = args.Length > 2 ? args[2] : null;

        if (!File.Exists(projectFile))
        {
            Console.WriteLine($"Error: Project file '{projectFile}' not found");
            return false;
        }

        return await compiler.CompileProject(projectFile, outputFile);
    }

    private static async Task<bool> HandleRunCommand(string[] args, Compiler compiler)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Project file is required");
            Console.WriteLine("Usage: uhigh run <project-file>");
            return false;
        }

        var projectFile = args[1];

        if (!File.Exists(projectFile))
        {
            Console.WriteLine($"Error: Project file '{projectFile}' not found");
            return false;
        }

        return await compiler.CompileProject(projectFile);
    }

    private static async Task<bool> HandleInfoCommand(string[] args, Compiler compiler)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Project file is required");
            Console.WriteLine("Usage: uhigh info <project-file>");
            return false;
        }

        var projectFile = args[1];

        if (!File.Exists(projectFile))
        {
            Console.WriteLine($"Error: Project file '{projectFile}' not found");
            return false;
        }

        return await compiler.ListProjectInfo(projectFile);
    }

    private static async Task<bool> HandleAddFileCommand(string[] args, Compiler compiler)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Error: Project file and source file name are required");
            Console.WriteLine("Usage: uhigh add-file <project-file> <source-file>");
            return false;
        }

        var projectFile = args[1];
        var sourceFile = args[2];

        if (!File.Exists(projectFile))
        {
            Console.WriteLine($"Error: Project file '{projectFile}' not found");
            return false;
        }

        return await compiler.AddSourceFileToProject(projectFile, sourceFile, true);
    }

    private static async Task<bool> HandleAddPackageCommand(string[] args, Compiler compiler)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Error: Project file, package name, and version are required");
            Console.WriteLine("Usage: wake.net add-package <project-file> <package-name> <version>");
            return false;
        }

        var projectFile = args[1];
        var packageName = args[2];
        var version = args[3];

        if (!File.Exists(projectFile))
        {
            Console.WriteLine($"Error: Project file '{projectFile}' not found");
            return false;
        }

        return await compiler.AddPackageToProject(projectFile, packageName, version);
    }

    private static async Task<bool> HandleTestCommand(string[] args, Compiler compiler)
    {
        Console.WriteLine("Running μHigh Compiler Tests");
        Console.WriteLine("============================");
        Console.WriteLine();

        var testSuites = Wake.Net.Testing.TestRunner.RunAllTests();
        Wake.Net.Testing.TestRunner.PrintResults(testSuites);

        var totalFailed = testSuites.Sum(s => s.FailedCount);
        return totalFailed == 0;
    }

    private static async Task<bool> HandleFileCompilation(string[] args, Compiler compiler)
    {
        var sourceFile = args[0];
        
        if (!File.Exists(sourceFile))
        {
            Console.WriteLine($"Error: Source file '{sourceFile}' not found");
            return false;
        }

        bool success;
        
        // Check for --save flag
        var saveIndex = Array.IndexOf(args, "--save");
        if (saveIndex >= 0 && saveIndex < args.Length - 1)
        {
            var savePath = args[saveIndex + 1];
            Console.WriteLine($"Saving C# code to: {savePath}");
            success = await compiler.SaveCSharpCode(sourceFile, savePath);
        }
        else if (args.Length == 1 || (args.Length == 2 && args[1] == "--run"))
        {
            // Run in memory
          //  Console.WriteLine("Compiling and running in memory...");
            success = await compiler.CompileAndRunInMemory(sourceFile);
        }
        else if (args.Length >= 2 && !args[1].StartsWith("--"))
        {
            // Compile to file
            var outputFile = args[1];
            Console.WriteLine($"Compiling to executable: {outputFile}");
            success = await compiler.CompileToExecutable(sourceFile, outputFile);
        }
        else
        {
            // Default: run in memory
            //Console.WriteLine("Compiling and running in memory...");
            success = await compiler.CompileAndRunInMemory(sourceFile);
        }

        return success;
    }
}