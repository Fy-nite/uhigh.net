using System.CommandLine;
using System.CommandLine.Parsing;
using uhigh.Net;
using uhigh.Net.CommandLine;
using uhigh.Net.UbPackage;
using uhigh.Net.Templates;
using uhigh.Net.Diagnostics;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.CommandLine.Invocation;


/// <summary>
/// The entry point class
/// </summary>
public class EntryPoint
{
    /// <summary>
    /// Main the args
    /// </summary>
    /// <param name="args">The args</param>
    public static async Task<int> Main(string[] args)
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

        var rootCommand = CreateRootCommand();
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Creates the root command with all subcommands
    /// </summary>
    /// <returns>The root command</returns>
    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("μHigh compiler and toolchain");

        // Global options
        var typeErrorsAsWarningsOption = CommonOptions.CreateTypeErrorsAsWarningsOption();
        rootCommand.AddGlobalOption(typeErrorsAsWarningsOption);

        // Add all subcommands
        rootCommand.AddCommand(CreateCompileCommand(typeErrorsAsWarningsOption));
        rootCommand.AddCommand(CreateCreateCommand());
        rootCommand.AddCommand(CreateListTemplatesCommand());
        rootCommand.AddCommand(CreateBuildCommand(typeErrorsAsWarningsOption));
        rootCommand.AddCommand(CreateRunCommand(typeErrorsAsWarningsOption));
        rootCommand.AddCommand(CreateInfoCommand());
        rootCommand.AddCommand(CreateAddFileCommand());
        rootCommand.AddCommand(CreateAddPackageCommand());
        rootCommand.AddCommand(CreateInstallPackagesCommand());
        rootCommand.AddCommand(CreateSearchPackagesCommand());
        rootCommand.AddCommand(CreateListPackagesCommand());
        rootCommand.AddCommand(CreateRestorePackagesCommand());
        rootCommand.AddCommand(CreateAstCommand(typeErrorsAsWarningsOption));
        rootCommand.AddCommand(CreateLspCommand());
        rootCommand.AddCommand(CreateTestCommand());
        rootCommand.AddCommand(CreateReplCommand(typeErrorsAsWarningsOption));

        
        // Add .ub package commands
        rootCommand.AddCommand(CreatePackCommand());
        rootCommand.AddCommand(CreateUnpackCommand());
        rootCommand.AddCommand(CreateInstallUbPackageCommand());
        rootCommand.AddCommand(CreateListUbPackagesCommand());
        rootCommand.AddCommand(CreateBuildFromPackageCommand());

        rootCommand.AddCommand(CreateListTargetsCommand());


        return rootCommand;
    }

    /// <summary>
    /// Creates the compile command
    /// </summary>
    private static Command CreateCompileCommand(Option<bool> typeErrorsAsWarningsOption)
    {
        var sourceFileArg = CommonOptions.CreateSourceFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var saveCsOption = CommonOptions.CreateSaveCSharpOption();
        var outputOption = new Option<string?>("--output", "Output executable file path");
        var runInMemoryOption = new Option<bool>("--run", "Run the compiled code in memory");
        var targetOption = new Option<string>("--target", () => "csharp", "Code generation target (e.g. csharp, javascript)");

        var command = new Command("compile", "Compile a μHigh source file")
        {
            sourceFileArg,
            verboseOption,
            stdLibOption,
            saveCsOption,
            outputOption,
            runInMemoryOption,
            targetOption
        };

        command.SetHandler(async (sourceFile, verbose, stdLibPath, saveCsTo, output, runInMemory, target, typeErrorsAsWarnings) =>
        {
            var options = new CompileOptions
            {
                SourceFile = sourceFile,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                SaveCSharpTo = saveCsTo,
                OutputFile = output,
                RunInMemory = runInMemory,
                Target = target,
                TypeErrorsAsWarnings = typeErrorsAsWarnings
            };
            Environment.ExitCode = await HandleCompileCommand(options);
        }, sourceFileArg, verboseOption, stdLibOption, saveCsOption, outputOption, runInMemoryOption, targetOption, typeErrorsAsWarningsOption);

        return command;
    }

    /// <summary>
    /// Creates the create command
    /// </summary>
    private static Command CreateCreateCommand()
    {
        var projectNameArg = new Argument<string>("project-name", "Name of the project to create");
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var directoryOption = new Option<string?>("--directory", "Directory to create the project in");
        var descriptionOption = new Option<string?>("--description", "Project description");
        var authorOption = new Option<string?>("--author", "Project author");
        var outputTypeOption = new Option<string>("--output-type", () => "Exe", "Output type (Exe, Library)");
        var targetFrameworkOption = new Option<string>("--target-framework", () => "net9.0", "Target framework");
        var templateOption = new Option<string>("--template", () => "console", "Project template to use (console, classlib, test)");

        var command = new Command("create", "Create a new μHigh project")
        {
            projectNameArg,
            verboseOption,
            stdLibOption,
            directoryOption,
            descriptionOption,
            authorOption,
            outputTypeOption,
            targetFrameworkOption,
            templateOption
        };

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectName = context.ParseResult.GetValueForArgument(projectNameArg);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var stdLibPath = context.ParseResult.GetValueForOption(stdLibOption);
            var directory = context.ParseResult.GetValueForOption(directoryOption);
            var description = context.ParseResult.GetValueForOption(descriptionOption);
            var author = context.ParseResult.GetValueForOption(authorOption);
            var outputType = context.ParseResult.GetValueForOption(outputTypeOption);
            var targetFramework = context.ParseResult.GetValueForOption(targetFrameworkOption);
            var template = context.ParseResult.GetValueForOption(templateOption);
            
            var options = new CreateOptions
            {
                ProjectName = projectName,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                Directory = directory,
                Description = description,
                Author = author,
                OutputType = outputType,
                TargetFramework = targetFramework,
                Template = template
            };
            
            Environment.ExitCode = await HandleCreateCommand(options);
        });

        return command;
    }

    /// <summary>
    /// Creates the list-templates command
    /// </summary>
    private static Command CreateListTemplatesCommand()
    {
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();

        var command = new Command("list-templates", "List available project templates")
        {
            verboseOption,
            stdLibOption
        };

        command.SetHandler(async (verbose, stdLibPath) =>
        {
            var options = new CreateOptions
            {
                ProjectName = "", // Not needed for listing
                Verbose = verbose,
                StdLibPath = stdLibPath
            };
            
            Environment.ExitCode = await HandleListTemplatesCommand(options);
        }, verboseOption, stdLibOption);

        return command;
    }

    /// <summary>
    /// Creates the build command
    /// </summary>
    private static Command CreateBuildCommand(Option<bool> typeErrorsAsWarningsOption)
    {
        var projectFileArg = CommonOptions.CreateProjectFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var saveCsOption = CommonOptions.CreateSaveCSharpOption();
        var outputOption = new Option<string?>("--output", "Output executable file path");

        var command = new Command("build", "Build a μHigh project")
        {
            projectFileArg,
            verboseOption,
            stdLibOption,
            saveCsOption,
            outputOption
        };

        command.SetHandler(async (projectFile, verbose, stdLibPath, saveCsTo, output, typeErrorsAsWarnings) =>
        {
            // If projectFile is null, try to find one in current directory
            if (string.IsNullOrEmpty(projectFile))
            {
                try
                {
                    projectFile = CommonOptions.FindProjectFile();
                }
                catch (Exception ex)
                {
                    WriteError(ex.Message);
                    Environment.ExitCode = 1;
                    return;
                }
                if (string.IsNullOrEmpty(projectFile))
                {
                    WriteError("No .uhighproj file found in current directory.");
                    Environment.ExitCode = 1;
                    return;
                }
            }
            var options = new BuildOptions
            {
                ProjectFile = projectFile!,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                SaveCSharpTo = saveCsTo,
                OutputFile = output,
                TypeErrorsAsWarnings = typeErrorsAsWarnings
            };
            Environment.ExitCode = await HandleBuildCommand(options);
        }, projectFileArg, verboseOption, stdLibOption, saveCsOption, outputOption, typeErrorsAsWarningsOption);

        return command;
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Creates the run command
    /// </summary>
    private static Command CreateRunCommand(Option<bool> typeErrorsAsWarningsOption)
    {
        var projectFileArg = new Argument<string?>("project-file", () => null, "Path to the μHigh project file or source file (optional - will auto-detect .uhighproj if not specified)");
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var saveCsOption = CommonOptions.CreateSaveCSharpOption();

        var command = new Command("run", "Run a μHigh project or source file")
        {
            projectFileArg,
            verboseOption,
            stdLibOption,
            saveCsOption
        };

        command.SetHandler(async (projectFile, verbose, stdLibPath, saveCsTo, typeErrorsAsWarnings) =>
        {
            var options = new RunOptions
            {
                ProjectFile = projectFile!,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                SaveCSharpTo = saveCsTo,
                TypeErrorsAsWarnings = typeErrorsAsWarnings
            };
            Environment.ExitCode = await HandleRunCommand(options);
        }, projectFileArg, verboseOption, stdLibOption, saveCsOption, typeErrorsAsWarningsOption);

        return command;
    }

    /// <summary>
    /// Creates the info command
    /// </summary>
    private static Command CreateInfoCommand()
    {
        var projectFileArg = CommonOptions.CreateProjectFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();

        var command = new Command("info", "Show project information")
        {
            projectFileArg,
            verboseOption,
            stdLibOption
        };

        command.SetHandler(async (projectFile, verbose, stdLibPath) =>
        {
            var options = new InfoOptions
            {
                ProjectFile = projectFile,
                Verbose = verbose,
                StdLibPath = stdLibPath
            };
            Environment.ExitCode = await HandleInfoCommand(options);
        }, projectFileArg, verboseOption, stdLibOption);

        return command;
    }

    /// <summary>
    /// Creates the add-file command
    /// </summary>
    private static Command CreateAddFileCommand()
    {
        var projectFileArg = CommonOptions.CreateProjectFileArgument();
        var sourceFileArg = CommonOptions.CreateSourceFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var createFileOption = new Option<bool>("--create", "Create the file if it doesn't exist");

        var command = new Command("add-file", "Add a source file to a project")
        {
            projectFileArg,
            sourceFileArg,
            verboseOption,
            stdLibOption,
            createFileOption
        };

        command.SetHandler(async (projectFile, sourceFile, verbose, stdLibPath, createFile) =>
        {
            var options = new AddFileOptions
            {
                ProjectFile = projectFile,
                SourceFile = sourceFile,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                CreateFile = createFile
            };
            Environment.ExitCode = await HandleAddFileCommand(options);
        }, projectFileArg, sourceFileArg, verboseOption, stdLibOption, createFileOption);

        return command;
    }

    /// <summary>
    /// Creates the add-package command
    /// </summary>
    private static Command CreateAddPackageCommand()
    {
        var projectFileArg = CommonOptions.CreateProjectFileArgument();
        var packageNameArg = new Argument<string>("package-name", "Name of the package to add");
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var versionOption = new Option<string?>("--version", "Package version");

        var command = new Command("add-package", "Add a NuGet package to a project")
        {
            projectFileArg,
            packageNameArg,
            verboseOption,
            stdLibOption,
            versionOption
        };

        command.SetHandler(async (projectFile, packageName, verbose, stdLibPath, version) =>
        {
            var options = new AddPackageOptions
            {
                ProjectFile = projectFile,
                PackageName = packageName,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                Version = version
            };
            Environment.ExitCode = await HandleAddPackageCommand(options);
        }, projectFileArg, packageNameArg, verboseOption, stdLibOption, versionOption);

        return command;
    }

    /// <summary>
    /// Creates the install-packages command
    /// </summary>
    private static Command CreateInstallPackagesCommand()
    {
        var projectFileArg = CommonOptions.CreateProjectFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();

        var command = new Command("install-packages", "Install all packages for a project")
        {
            projectFileArg,
            verboseOption,
            stdLibOption
        };

        command.SetHandler(async (projectFile, verbose, stdLibPath) =>
        {
            var options = new InstallPackagesOptions
            {
                ProjectFile = projectFile,
                Verbose = verbose,
                StdLibPath = stdLibPath
            };
            Environment.ExitCode = await HandleInstallPackagesCommand(options);
        }, projectFileArg, verboseOption, stdLibOption);

        return command;
    }

    /// <summary>
    /// Creates the search-packages command
    /// </summary>
    private static Command CreateSearchPackagesCommand()
    {
        var searchTermArg = new Argument<string>("search-term", "Term to search for");
        var verboseOption = CommonOptions.CreateVerboseOption();
        var takeOption = new Option<int>("--take", () => 10, "Number of results to return");

        var command = new Command("search-packages", "Search for NuGet packages")
        {
            searchTermArg,
            verboseOption,
            takeOption
        };

        command.SetHandler(async (searchTerm, verbose, take) =>
        {
            var options = new SearchPackagesOptions
            {
                SearchTerm = searchTerm,
                Verbose = verbose,
                Take = take
            };
            Environment.ExitCode = await HandleSearchPackagesCommand(options);
        }, searchTermArg, verboseOption, takeOption);

        return command;
    }

    /// <summary>
    /// Creates the list-packages command
    /// </summary>
    private static Command CreateListPackagesCommand()
    {
        var projectFileArg = CommonOptions.CreateProjectFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();

        var command = new Command("list-packages", "List packages in a project")
        {
            projectFileArg,
            verboseOption
        };

        command.SetHandler(async (projectFile, verbose) =>
        {
            var options = new ListPackagesOptions
            {
                ProjectFile = projectFile,
                Verbose = verbose
            };
            Environment.ExitCode = await HandleListPackagesCommand(options);
        }, projectFileArg, verboseOption);

        return command;
    }

    /// <summary>
    /// Creates the restore-packages command
    /// </summary>
    private static Command CreateRestorePackagesCommand()
    {
        var projectFileArg = CommonOptions.CreateProjectFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var forceOption = new Option<bool>("--force", "Force restore even if packages exist");

        var command = new Command("restore-packages", "Restore packages for a project")
        {
            projectFileArg,
            verboseOption,
            stdLibOption,
            forceOption
        };

        command.SetHandler(async (projectFile, verbose, stdLibPath, force) =>
        {
            var options = new RestorePackagesOptions
            {
                ProjectFile = projectFile,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                Force = force
            };
            Environment.ExitCode = await HandleRestorePackagesCommand(options);
        }, projectFileArg, verboseOption, stdLibOption, forceOption);

        return command;
    }

    /// <summary>
    /// Creates the ast command
    /// </summary>
    private static Command CreateAstCommand(Option<bool> typeErrorsAsWarningsOption)
    {
        var sourceFileArg = CommonOptions.CreateSourceFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();

        var command = new Command("ast", "Print the AST for a source file")
        {
            sourceFileArg,
            verboseOption,
            stdLibOption
        };

        command.SetHandler(async (sourceFile, verbose, stdLibPath, typeErrorsAsWarnings) =>
        {
            var options = new AstOptions
            {
                SourceFile = sourceFile,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                TypeErrorsAsWarnings = typeErrorsAsWarnings
            };
            Environment.ExitCode = await HandleAstCommand(options);
        }, sourceFileArg, verboseOption, stdLibOption, typeErrorsAsWarningsOption);

        return command;
    }

    /// <summary>
    /// Creates the lsp command
    /// </summary>
    private static Command CreateLspCommand()
    {
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var portOption = new Option<int?>("--port", "Port for LSP server");
        var useStdioOption = new Option<bool>("--stdio", () => true, "Use stdio for communication");

        var command = new Command("lsp", "Start the Language Server Protocol server")
        {
            verboseOption,
            stdLibOption,
            portOption,
            useStdioOption
        };

        command.SetHandler(async (verbose, stdLibPath, port, useStdio) =>
        {
            var options = new LspOptions
            {
                Verbose = verbose,
                StdLibPath = stdLibPath,
                Port = port,
                UseStdio = useStdio
            };
            Environment.ExitCode = await HandleLspCommand(options);
        }, verboseOption, stdLibOption, portOption, useStdioOption);

        return command;
    }

    /// <summary>
    /// Creates the test command
    /// </summary>
    private static Command CreateTestCommand()
    {
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var skipFileOption = new Option<string?>("--skip", "File of tests to skip");
        var listTestsOption = new Option<bool>("--list", "List available tests");

        var command = new Command("test", "Run tests")
        {
            verboseOption,
            stdLibOption,
            listTestsOption,
            skipFileOption
        };
        command.SetHandler((verbose, stdLibPath, listTests, skipFile) =>
        {
            var options = new TestOptions
            {
                Verbose = verbose,
                StdLibPath = stdLibPath,
                ListTests = listTests,
                SkipFile = skipFile
            };
            var ExitCode = HandleTestCommand(options);
            return Task.FromResult(ExitCode);
        }, verboseOption, stdLibOption, listTestsOption, skipFileOption);

        return command;
    }

    /// <summary>
    /// Creates the repl command
    /// </summary>
    private static Command CreateReplCommand(Option<bool> typeErrorsAsWarningsOption)
    {
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var saveCsOption = CommonOptions.CreateSaveCSharpOption();

        var command = new Command("repl", "Start the interactive REPL")
        {
            verboseOption,
            stdLibOption,
            saveCsOption
        };

        command.SetHandler(async (verbose, stdLibPath, saveCsTo, typeErrorsAsWarnings) =>
        {
            var options = new ReplOptions
            {
                Verbose = verbose,
                StdLibPath = stdLibPath,
                SaveCSharpTo = saveCsTo,
                TypeErrorsAsWarnings = typeErrorsAsWarnings
            };
            Environment.ExitCode = await HandleReplCommand(options);
        }, verboseOption, stdLibOption, saveCsOption, typeErrorsAsWarningsOption);

        return command;
    }

    /// <summary>

    /// Creates the pack command
    /// </summary>
    private static Command CreatePackCommand()
    {
        var projectFileArg = CommonOptions.CreateProjectFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var outputOption = new Option<string?>("--output", "Output .ub package file path");

        var command = new Command("pack", "Package a μHigh project into a .ub file")
        {
            projectFileArg,
            verboseOption,
            stdLibOption,
            outputOption
        };

        command.SetHandler(async (projectFile, verbose, stdLibPath, output) =>
        {
            var options = new PackOptions
            {
                ProjectFile = projectFile,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                OutputFile = output
            };
            Environment.ExitCode = await HandlePackCommand(options);
        }, projectFileArg, verboseOption, stdLibOption, outputOption);

        return command;
    }

    /// <summary>
    /// Creates the unpack command
    /// </summary>
    private static Command CreateUnpackCommand()
    {
        var packageFileArg = new Argument<string>("package-file", "Path to the .ub package file");
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var outputOption = new Option<string?>("--output", "Output directory to extract to");

        var command = new Command("unpack", "Extract a .ub package")
        {
            packageFileArg,
            verboseOption,
            stdLibOption,
            outputOption
        };

        command.SetHandler(async (packageFile, verbose, stdLibPath, output) =>
        {
            var options = new UnpackOptions
            {
                PackageFile = packageFile,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                OutputDirectory = output
            };
            Environment.ExitCode = await HandleUnpackCommand(options);
        }, packageFileArg, verboseOption, stdLibOption, outputOption);

        return command;
    }

    /// <summary>
    /// Creates the install-ub-package command
    /// </summary>
    private static Command CreateInstallUbPackageCommand()
    {
        var projectFileArg = CommonOptions.CreateProjectFileArgument();
        var packageFileArg = new Argument<string>("package-file", "Path to the .ub package file");
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var cacheDirOption = new Option<string?>("--cache-dir", "Directory to store package cache");

        var command = new Command("install-ub-package", "Install a .ub package as a dependency")
        {
            projectFileArg,
            packageFileArg,
            verboseOption,
            stdLibOption,
            cacheDirOption
        };

        command.SetHandler(async (projectFile, packageFile, verbose, stdLibPath, cacheDir) =>
        {
            var options = new InstallUbPackageOptions
            {
                ProjectFile = projectFile,
                PackageFile = packageFile,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                PackageCachePath = cacheDir
            };
            Environment.ExitCode = await HandleInstallUbPackageCommand(options);
        }, projectFileArg, packageFileArg, verboseOption, stdLibOption, cacheDirOption);

        return command;
    }

    /// <summary>
    /// Creates the list-ub-packages command
    /// </summary>
    private static Command CreateListUbPackagesCommand()
    {
        var projectFileArg = CommonOptions.CreateProjectFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();

        var command = new Command("list-ub-packages", "List installed .ub packages in a project")
        {
            projectFileArg,
            verboseOption,
            stdLibOption
        };

        command.SetHandler(async (projectFile, verbose, stdLibPath) =>
        {
            var options = new ListUbPackagesOptions
            {
                ProjectFile = projectFile,
                Verbose = verbose,
                StdLibPath = stdLibPath
            };
            Environment.ExitCode = await HandleListUbPackagesCommand(options);
        }, projectFileArg, verboseOption, stdLibOption);

        return command;
    }

    /// <summary>
    /// Creates the build-from-package command
    /// </summary>
    private static Command CreateBuildFromPackageCommand()
    {
        var packageFileArg = new Argument<string>("package-file", "Path to the .ub package file");
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var outputOption = new Option<string?>("--output", "Output executable file path");

        var command = new Command("build-from-package", "Build an executable directly from a .ub package")
        {
            packageFileArg,
            verboseOption,
            stdLibOption,
            outputOption
        };

        command.SetHandler(async (packageFile, verbose, stdLibPath, output) =>
        {
            var options = new BuildFromPackageOptions
            {
                PackageFile = packageFile,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                OutputFile = output
            };
            Environment.ExitCode = await HandleBuildFromPackageCommand(options);
        }, packageFileArg, verboseOption, stdLibOption, outputOption);
        return command;
    }


    /// Creates the list-targets command
    /// </summary>
    private static Command CreateListTargetsCommand()
    {
        var command = new Command("list-targets", "List available code generation targets");
        command.SetHandler(() =>
        {
            var compiler = new Compiler();
            compiler.ListAvailableTargets();
        });

        return command;
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
            "list-packages", "restore-packages", "ast", "lsp", "test", "repl",
            "pack", "unpack", "install-ub-package", "list-ub-packages", "build-from-package"

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
            var compiler = new Compiler(options.Verbose, options.StdLibPath, options.Target, options.TypeErrorsAsWarnings);
            bool success;

            if (!File.Exists(options.SourceFile))
            {
                Console.WriteLine($"Error: Source file '{options.SourceFile}' not found");
                return 1;
            }

            // If target is not csharp, use modular backend
            if (!string.IsNullOrEmpty(options.Target) && options.Target.ToLower() != "csharp")
            {
                var source = await File.ReadAllTextAsync(options.SourceFile);
                var diagnostics = new uhigh.Net.Diagnostics.DiagnosticsReporter(options.Verbose, options.SourceFile, false, options.TypeErrorsAsWarnings);
                var code = compiler.CompileToTarget(source, options.Target, diagnostics);

                var outputFile = options.OutputFile;
                if (string.IsNullOrEmpty(outputFile))
                {
                    var generator = uhigh.Net.CodeGen.CodeGeneratorRegistry.GetGenerator(options.Target);
                    var ext = generator?.FileExtension ?? ".txt";
                    outputFile = Path.ChangeExtension(options.SourceFile, ext);
                }

                await File.WriteAllTextAsync(outputFile, code);
                Console.WriteLine($"Generated {options.Target} code: {outputFile}");
                diagnostics.PrintSummary();
                return diagnostics.HasErrors ? 1 : 0;
            }

            // Default: C# backend
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
            
            // Initialize template discovery
            var templateDiscovery = new TemplateDiscovery(options.Verbose ? new DiagnosticsReporter(true) : null);
            
            // Discover templates from built-in and addons folder
            var addonsPath = Path.Combine(AppContext.BaseDirectory, "addons", "templates");
            await templateDiscovery.DiscoverTemplatesAsync(addonsPath);
            
            // Get the requested template
            var template = templateDiscovery.GetTemplate(options.Template);
            if (template == null)
            {
                WriteError($"Template '{options.Template}' not found. Available templates:");
                foreach (var templateName in templateDiscovery.ListTemplateNames())
                {
                    var templateInfo = templateDiscovery.GetTemplate(templateName);
                    Console.WriteLine($"  {templateName}: {templateInfo?.Description}");
                }
                return 1;
            }
            
            // Prepare template parameters
            var templateParameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(options.Description))
                templateParameters["description"] = options.Description;
            if (!string.IsNullOrEmpty(options.Author))
                templateParameters["author"] = options.Author;
            if (!string.IsNullOrEmpty(options.TargetFramework))
                templateParameters["targetFramework"] = options.TargetFramework;
            
            // Create project using template
            var projectDir = options.Directory ?? Environment.CurrentDirectory;
            var fullProjectDir = Path.Combine(projectDir, options.ProjectName);
            
            var diagnostics = new DiagnosticsReporter(options.Verbose);
            var success = await template.CreateProjectAsync(options.ProjectName, fullProjectDir, templateParameters, diagnostics);
            
            if (success)
            {
                Console.WriteLine($"Created project '{options.ProjectName}' using template '{template.Name}'");
                Console.WriteLine($"Project directory: {fullProjectDir}");
                Console.WriteLine($"Project file: {Path.Combine(fullProjectDir, $"{options.ProjectName}.uhighproj")}");
                Console.WriteLine($"Template: {template.Description}");
                Console.WriteLine($"Output type: {template.OutputType}");
            }
            
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Project creation failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Handles the list templates command
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandleListTemplatesCommand(CreateOptions options)
    {
        try
        {
            // Initialize template discovery
            var templateDiscovery = new TemplateDiscovery(options.Verbose ? new DiagnosticsReporter(true) : null);
            
            // Discover templates from built-in and addons folder
            var addonsPath = Path.Combine(AppContext.BaseDirectory, "addons", "templates");
            await templateDiscovery.DiscoverTemplatesAsync(addonsPath);
            
            Console.WriteLine("Available μHigh Project Templates:");
            Console.WriteLine("====================================");
            
            var templates = templateDiscovery.Templates.Values.OrderBy(t => t.Name);
            foreach (var template in templates)
            {
                Console.WriteLine();
                Console.WriteLine($"Template: {template.Name}");
                Console.WriteLine($"  Description: {template.Description}");
                Console.WriteLine($"  Output Type: {template.OutputType}");
                Console.WriteLine($"  Version: {template.Version}");
                Console.WriteLine($"  Author: {template.Author}");
                
                var parameters = template.GetParameters();
                if (parameters.Any())
                {
                    Console.WriteLine("  Parameters:");
                    foreach (var param in parameters)
                    {
                        Console.WriteLine($"    --{param.Key}: {param.Value}");
                    }
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  uhigh create MyProject --template console");
            Console.WriteLine("  uhigh create MyLibrary --template classlib");
            Console.WriteLine("  uhigh create MyTests --template test");
            
            return 0;
        }
        catch (Exception ex)
        {
            WriteError($"Failed to list templates: {ex.Message}");
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
            var compiler = new Compiler(options.Verbose, options.StdLibPath, "csharp", options.TypeErrorsAsWarnings);
            bool success;

            // Load the project file (async)
            var project = await uhigh.Net.ProjectFile.LoadAsync(options.ProjectFile);
            if (project == null)
            {
                WriteError($"Failed to load project: {options.ProjectFile}");
                return 1;
            }

            // Clear all previous defines and set preprocessor symbol for backend
            uhigh.Net.Preprocessor.Preprocessor.ClearDefines();
            uhigh.Net.Preprocessor.Preprocessor.SetTargetLanguage(project.Backend);

            // Continue with build logic
            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file '{options.ProjectFile}' not found");
                return 1;
            }

            if (!string.IsNullOrEmpty(options.SaveCSharpTo))
            {
                success = await compiler.SaveCSharpCode(options.ProjectFile, options.SaveCSharpTo);
                if (success)
                {
                    Console.WriteLine($"Saved C# code to {options.SaveCSharpTo}");
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
            var compiler = new Compiler(options.Verbose, options.StdLibPath, "csharp", options.TypeErrorsAsWarnings);
            string? fileToRun;

            // Determine what file to run
            if (string.IsNullOrEmpty(options.ProjectFile))
            {
                // Auto-detect .uhighproj file in current directory
                fileToRun = FindProjectFileInCurrentDirectory();
                if (fileToRun == null)
                {
                    return 1; // Error already reported by FindProjectFileInCurrentDirectory
                }
            }
            else
            {
                fileToRun = options.ProjectFile;
            }

            // Check if the file is a .uh source file
            if (fileToRun.EndsWith(".uh", StringComparison.OrdinalIgnoreCase) || 
                fileToRun.EndsWith(".uhigh", StringComparison.OrdinalIgnoreCase))
            {
                // Run as a source file directly
                if (!File.Exists(fileToRun))
                {
                    WriteError($"Source file '{fileToRun}' not found");
                    return 1;
                }

                bool success;
                if (!string.IsNullOrEmpty(options.SaveCSharpTo))
                {
                    success = await compiler.SaveCSharpCode(fileToRun, options.SaveCSharpTo);
                }
                else
                {
                    success = await compiler.CompileAndRunInMemory(fileToRun);
                }
                
                return success ? 0 : 1;
            }
            else
            {
                // Run as a project file
                if (!File.Exists(fileToRun))
                {
                    WriteError($"Project file '{fileToRun}' not found");
                    return 1;
                }

                bool success;
                if (!string.IsNullOrEmpty(options.SaveCSharpTo))
                {
                    success = await compiler.SaveCSharpCodeFromProject(fileToRun, options.SaveCSharpTo);
                }
                else
                {
                    success = await compiler.CompileProjectAndRun(fileToRun);
                }
                
                return success ? 0 : 1;
            }

        }
        catch (Exception ex)
        {
            WriteError($"Run failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Finds a .uhighproj file in the current directory
    /// </summary>
    /// <returns>The path to the project file, or null if not found or multiple found</returns>
    private static string? FindProjectFileInCurrentDirectory()
    {
        var currentDir = Environment.CurrentDirectory;
        var projectFiles = Directory.GetFiles(currentDir, "*.uhighproj");

        if (projectFiles.Length == 0)
        {
            WriteError("No .uhighproj file found in the current directory. Please specify a project file or navigate to a directory containing a .uhighproj file.");
            return null;
        }
        else if (projectFiles.Length > 1)
        {
            WriteError($"Multiple .uhighproj files found in the current directory:");
            foreach (var file in projectFiles)
            {
                WriteError($"  {Path.GetFileName(file)}");
            }
            WriteError("Please specify which project file to use.");
            return null;
        }
        else
        {
            var projectFile = projectFiles[0];
            if (Environment.GetEnvironmentVariable("UHIGH_VERBOSE") == "1")
            {
                Console.WriteLine($"Auto-detected project file: {Path.GetFileName(projectFile)}");
            }
            return projectFile;
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
            var projectManager = new uhigh.Net.ProjectSystem.ProjectManager(options.Verbose, options.StdLibPath);

            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file '{options.ProjectFile}' not found");
                return 1;
            }

            var success = await projectManager.ListProjectInfo(options.ProjectFile);
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
            var projectManager = new uhigh.Net.ProjectSystem.ProjectManager(options.Verbose, options.StdLibPath);

            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file '{options.ProjectFile}' not found");
                return 1;
            }

            var success = await projectManager.AddSourceFileToProject(options.ProjectFile, options.SourceFile, options.CreateFile);
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
            var projectManager = new uhigh.Net.ProjectSystem.ProjectManager(options.Verbose, options.StdLibPath);

            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file '{options.ProjectFile}' not found");
                return 1;
            }

            var success = await projectManager.AddPackageToProject(options.ProjectFile, options.PackageName, options.Version!);
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
    /// Creates the ast command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandleAstCommand(AstOptions options)
    {
        try
        {
            var compiler = new Compiler(options.Verbose, options.StdLibPath, "csharp", options.TypeErrorsAsWarnings);
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
        await UhighLanguageServer.srv.StartServerAsync();
        return 0;
    }

    /// <summary>
    /// Handles the test command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static int HandleTestCommand(TestOptions options)
    {
        try
        {
            Console.WriteLine("Running μHigh Tests...");
            Console.WriteLine();
            List<string> skip = new();
            if (options.SkipFile != null)
            {
                using StreamReader reader = new(options.SkipFile);
                while (!reader.EndOfStream) { string? line = reader.ReadLine(); if (line != null) { skip.Add(line.Trim()); } }
            }

            var testSuites = uhigh.Net.Testing.TestRunner.RunAllTests(skip);
            uhigh.Net.Testing.TestRunner.PrintResults(testSuites);

            var totalFailed = testSuites.Sum(s => s.Counts.Failed);
            return totalFailed;
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
    /// Handles the pack command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandlePackCommand(PackOptions options)
    {
        try
        {
            var diagnostics = new uhigh.Net.Diagnostics.DiagnosticsReporter();
            var packageManager = new uhigh.Net.UbPackage.UbPackageManager(diagnostics);

            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file not found: {options.ProjectFile}");
                return 1;
            }

            // Determine output file if not specified
            var outputFile = options.OutputFile;
            if (string.IsNullOrEmpty(outputFile))
            {
                var projectName = Path.GetFileNameWithoutExtension(options.ProjectFile);
                outputFile = Path.Combine(Path.GetDirectoryName(options.ProjectFile) ?? "", $"{projectName}.ub");
            }

            var success = await packageManager.PackAsync(options.ProjectFile, outputFile);
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Pack command failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Handles the unpack command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandleUnpackCommand(UnpackOptions options)
    {
        try
        {
            var diagnostics = new uhigh.Net.Diagnostics.DiagnosticsReporter();
            var packageManager = new uhigh.Net.UbPackage.UbPackageManager(diagnostics);

            if (!File.Exists(options.PackageFile))
            {
                WriteError($"Package file not found: {options.PackageFile}");
                return 1;
            }

            // Determine output directory if not specified
            var outputDir = options.OutputDirectory;
            if (string.IsNullOrEmpty(outputDir))
            {
                var packageName = Path.GetFileNameWithoutExtension(options.PackageFile);
                outputDir = Path.Combine(Path.GetDirectoryName(options.PackageFile) ?? "", packageName);
            }

            var success = await packageManager.UnpackAsync(options.PackageFile, outputDir);
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Unpack command failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Handles the install-ub-package command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandleInstallUbPackageCommand(InstallUbPackageOptions options)
    {
        try
        {
            var diagnostics = new uhigh.Net.Diagnostics.DiagnosticsReporter();
            var packageManager = new uhigh.Net.UbPackage.UbPackageManager(diagnostics);

            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file not found: {options.ProjectFile}");
                return 1;
            }

            if (!File.Exists(options.PackageFile))
            {
                WriteError($"Package file not found: {options.PackageFile}");
                return 1;
            }

            var success = await packageManager.InstallAsync(options.PackageFile, options.ProjectFile, options.PackageCachePath);
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Install ub-package command failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Handles the list-ub-packages command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandleListUbPackagesCommand(ListUbPackagesOptions options)
    {
        try
        {
            var diagnostics = new uhigh.Net.Diagnostics.DiagnosticsReporter();
            var packageManager = new uhigh.Net.UbPackage.UbPackageManager(diagnostics);

            if (!File.Exists(options.ProjectFile))
            {
                WriteError($"Project file not found: {options.ProjectFile}");
                return 1;
            }

            var installedPackages = await packageManager.ListInstalledAsync(options.ProjectFile);
            
            if (installedPackages.Count == 0)
            {
                Console.WriteLine("No .ub packages installed in this project.");
                return 0;
            }

            Console.WriteLine($"Installed .ub packages:");
            foreach (var (name, version, path) in installedPackages)
            {
                Console.WriteLine($"  {name} v{version}");
                if (options.Verbose)
                {
                    Console.WriteLine($"    Path: {path}");
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            WriteError($"List ub-packages command failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Handles the build-from-package command using the specified options
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>A task containing the int</returns>
    private static async Task<int> HandleBuildFromPackageCommand(BuildFromPackageOptions options)
    {
        try
        {
            var diagnostics = new uhigh.Net.Diagnostics.DiagnosticsReporter();
            var packageManager = new uhigh.Net.UbPackage.UbPackageManager(diagnostics);

            if (!File.Exists(options.PackageFile))
            {
                WriteError($"Package file not found: {options.PackageFile}");
                return 1;
            }

            var success = await packageManager.BuildFromPackageAsync(options.PackageFile, options.OutputFile);
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            WriteError($"Build from package command failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Handles the parse error using the specified errors
    /// </summary>
    /// <param name="errors">The errors</param>
    /// <returns>A task containing the int</returns>
    private static int HandleParseError(IEnumerable<ParseError> errors)
    {
        var errorsList = errors.ToList();

        if (!errorsList.Any())
        {
            return 0;
        }
        
        Console.WriteLine("Command line parsing failed:");
        foreach (var error in errorsList)
        {
            Console.WriteLine($"  {error}");
        }
        return 1;
    }

}
      