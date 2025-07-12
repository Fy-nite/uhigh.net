using uhigh.Net;
using uhigh.Net.CommandLine;
using System.CommandLine;
using System.CommandLine.Parsing;

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

        // Add all subcommands
        rootCommand.AddCommand(CreateCompileCommand());
        rootCommand.AddCommand(CreateCreateCommand());
        rootCommand.AddCommand(CreateBuildCommand());
        rootCommand.AddCommand(CreateRunCommand());
        rootCommand.AddCommand(CreateInfoCommand());
        rootCommand.AddCommand(CreateAddFileCommand());
        rootCommand.AddCommand(CreateAddPackageCommand());
        rootCommand.AddCommand(CreateInstallPackagesCommand());
        rootCommand.AddCommand(CreateSearchPackagesCommand());
        rootCommand.AddCommand(CreateListPackagesCommand());
        rootCommand.AddCommand(CreateRestorePackagesCommand());
        rootCommand.AddCommand(CreateAstCommand());
        rootCommand.AddCommand(CreateLspCommand());
        rootCommand.AddCommand(CreateTestCommand());
        rootCommand.AddCommand(CreateReplCommand());

        return rootCommand;
    }

    /// <summary>
    /// Creates the compile command
    /// </summary>
    private static Command CreateCompileCommand()
    {
        var sourceFileArg = CommonOptions.CreateSourceFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var saveCsOption = CommonOptions.CreateSaveCSharpOption();
        var outputOption = new Option<string?>("--output", "Output executable file path");
        var runInMemoryOption = new Option<bool>("--run", "Run the compiled code in memory");

        var command = new Command("compile", "Compile a μHigh source file")
        {
            sourceFileArg,
            verboseOption,
            stdLibOption,
            saveCsOption,
            outputOption,
            runInMemoryOption
        };

        command.SetHandler(async (sourceFile, verbose, stdLibPath, saveCsTo, output, runInMemory) =>
        {
            var options = new CompileOptions
            {
                SourceFile = sourceFile,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                SaveCSharpTo = saveCsTo,
                OutputFile = output,
                RunInMemory = runInMemory
            };
            Environment.ExitCode = await HandleCompileCommand(options);
        }, sourceFileArg, verboseOption, stdLibOption, saveCsOption, outputOption, runInMemoryOption);

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
        var targetFrameworkOption = new Option<string>("--target-framework", () => "net8.0", "Target framework");

        var command = new Command("create", "Create a new μHigh project")
        {
            projectNameArg,
            verboseOption,
            stdLibOption,
            directoryOption,
            descriptionOption,
            authorOption,
            outputTypeOption,
            targetFrameworkOption
        };

        command.SetHandler(async (projectName, verbose, stdLibPath, directory, description, author, outputType, targetFramework) =>
        {
            var options = new CreateOptions
            {
                ProjectName = projectName,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                Directory = directory,
                Description = description,
                Author = author,
                OutputType = outputType,
                TargetFramework = targetFramework
            };
            Environment.ExitCode = await HandleCreateCommand(options);
        }, projectNameArg, verboseOption, stdLibOption, directoryOption, descriptionOption, authorOption, outputTypeOption, targetFrameworkOption);

        return command;
    }

    /// <summary>
    /// Creates the build command
    /// </summary>
    private static Command CreateBuildCommand()
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

        command.SetHandler(async (projectFile, verbose, stdLibPath, saveCsTo, output) =>
        {
            var options = new BuildOptions
            {
                ProjectFile = projectFile,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                SaveCSharpTo = saveCsTo,
                OutputFile = output
            };
            Environment.ExitCode = await HandleBuildCommand(options);
        }, projectFileArg, verboseOption, stdLibOption, saveCsOption, outputOption);

        return command;
    }

    /// <summary>
    /// Creates the run command
    /// </summary>
    private static Command CreateRunCommand()
    {
        var projectFileArg = CommonOptions.CreateProjectFileArgument();
        var verboseOption = CommonOptions.CreateVerboseOption();
        var stdLibOption = CommonOptions.CreateStdLibPathOption();
        var saveCsOption = CommonOptions.CreateSaveCSharpOption();

        var command = new Command("run", "Run a μHigh project")
        {
            projectFileArg,
            verboseOption,
            stdLibOption,
            saveCsOption
        };

        command.SetHandler(async (projectFile, verbose, stdLibPath, saveCsTo) =>
        {
            var options = new RunOptions
            {
                ProjectFile = projectFile,
                Verbose = verbose,
                StdLibPath = stdLibPath,
                SaveCSharpTo = saveCsTo
            };
            Environment.ExitCode = await HandleRunCommand(options);
        }, projectFileArg, verboseOption, stdLibOption, saveCsOption);

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
    private static Command CreateAstCommand()
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

        command.SetHandler(async (sourceFile, verbose, stdLibPath) =>
        {
            var options = new AstOptions
            {
                SourceFile = sourceFile,
                Verbose = verbose,
                StdLibPath = stdLibPath
            };
            Environment.ExitCode = await HandleAstCommand(options);
        }, sourceFileArg, verboseOption, stdLibOption);

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
            Environment.ExitCode = HandleTestCommand(options);
        }, verboseOption, stdLibOption, listTestsOption, skipFileOption);

        return command;
    }

    /// <summary>
    /// Creates the repl command
    /// </summary>
    private static Command CreateReplCommand()
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

        command.SetHandler(async (verbose, stdLibPath, saveCsTo) =>
        {
            var options = new ReplOptions
            {
                Verbose = verbose,
                StdLibPath = stdLibPath,
                SaveCSharpTo = saveCsTo
            };
            Environment.ExitCode = await HandleReplCommand(options);
        }, verboseOption, stdLibOption, saveCsOption);

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
                success = await compiler.CompileProjectAndRun(options.ProjectFile);
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

            var success = await compiler.AddPackageToProject(options.ProjectFile, options.PackageName, options.Version!);
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
            if (options.SkipFile != null) {
                using StreamReader reader = new(options.SkipFile);
                while (!reader.EndOfStream) {string? line = reader.ReadLine(); if (line != null) {skip.Add(line.Trim());}}
            }
            
            var testSuites = uhigh.Net.Testing.TestRunner.RunAllTests(skip);
            uhigh.Net.Testing.TestRunner.PrintResults(testSuites);
            
            var totalFailed = testSuites.Sum(s => s.Counts.Failed);
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
