using System.CommandLine;

namespace uhigh.Net.CommandLine
{
    /// <summary>
    /// Factory for creating all μHigh CLI commands
    /// </summary>
    public static class CommandFactory
    {
        /// <summary>
        /// Creates the root command with all subcommands
        /// </summary>
        public static RootCommand CreateRootCommand()
        {
            var rootCommand = new RootCommand("μHigh compiler and development tools");
            
            // Add all subcommands
            rootCommand.AddCommand(CreateCompileCommand());
            rootCommand.AddCommand(CreateReplCommand());
            rootCommand.AddCommand(CreateCreateCommand());
            rootCommand.AddCommand(CreateBuildCommand());
            rootCommand.AddCommand(CreateRunCommand());
            rootCommand.AddCommand(CreateInfoCommand());
            rootCommand.AddCommand(CreateAddFileCommand());
            rootCommand.AddCommand(CreateAddPackageCommand());
            rootCommand.AddCommand(CreateRestorePackagesCommand());
            rootCommand.AddCommand(CreateInstallPackagesCommand());
            rootCommand.AddCommand(CreateSearchPackagesCommand());
            rootCommand.AddCommand(CreateListPackagesCommand());
            rootCommand.AddCommand(CreateAstCommand());
            rootCommand.AddCommand(CreateTestCommand());
            rootCommand.AddCommand(CreateLspCommand());

            return rootCommand;
        }

        /// <summary>
        /// Creates the compile command
        /// </summary>
        public static Command CreateCompileCommand()
        {
            var command = new Command("compile", "Compile a μHigh source file");
            
            var sourceFileArg = CommonOptions.CreateSourceFileArgument();
            var outputFileArg = new Argument<string?>("output-file", () => null, "Output file name (optional)");
            var runOption = new Option<bool>(new[] { "-r", "--run" }, "Run the program after compiling");
            var verboseOption = CommonOptions.CreateVerboseOption();
            var stdlibOption = CommonOptions.CreateStdLibPathOption();
            var saveCsOption = CommonOptions.CreateSaveCSharpOption();

            command.AddArgument(sourceFileArg);
            command.AddArgument(outputFileArg);
            command.AddOption(runOption);
            command.AddOption(verboseOption);
            command.AddOption(stdlibOption);
            command.AddOption(saveCsOption);
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the REPL command
        /// </summary>
        public static Command CreateReplCommand()
        {
            var command = new Command("repl", "Start a REPL (Read-Eval-Print Loop) for μHigh");
            
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateSaveCSharpOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the create project command
        /// </summary>
        public static Command CreateCreateCommand()
        {
            var command = new Command("create", "Create a new μHigh project");
            
            var projectNameArg = new Argument<string>("project-name", "Project name");
            var directoryArg = new Argument<string?>("directory", () => null, "Directory to create the project in");
            var descriptionArg = new Argument<string?>("description", () => null, "Project description");
            var authorArg = new Argument<string?>("author", () => null, "Author name");
            
            var typeOption = new Option<string>(new[] { "-t", "--type" }, () => "Exe", "Project type (Exe or Library)");
            var frameworkOption = new Option<string>(new[] { "-f", "--framework" }, () => "net8.0", ".NET target framework");
            
            command.AddArgument(projectNameArg);
            command.AddArgument(directoryArg);
            command.AddArgument(descriptionArg);
            command.AddArgument(authorArg);
            command.AddOption(typeOption);
            command.AddOption(frameworkOption);
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the build command
        /// </summary>
        public static Command CreateBuildCommand()
        {
            var command = new Command("build", "Build a μHigh project");
            
            var projectFileArg = CommonOptions.CreateProjectFileArgument();
            var outputFileArg = new Argument<string?>("output-file", () => null, "Output file name (optional)");
            
            command.AddArgument(projectFileArg);
            command.AddArgument(outputFileArg);
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateSaveCSharpOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the run command
        /// </summary>
        public static Command CreateRunCommand()
        {
            var command = new Command("run", "Run a μHigh project");
            
            command.AddArgument(CommonOptions.CreateProjectFileArgument());
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateSaveCSharpOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the info command
        /// </summary>
        public static Command CreateInfoCommand()
        {
            var command = new Command("info", "Show information about a μHigh project");
            
            command.AddArgument(CommonOptions.CreateProjectFileArgument());
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the add-file command
        /// </summary>
        public static Command CreateAddFileCommand()
        {
            var command = new Command("add-file", "Add a source file to a μHigh project");
            
            var projectFileArg = CommonOptions.CreateProjectFileArgument();
            var sourceFileArg = new Argument<string>("source-file", "Path to the source file to add");
            var createOption = new Option<bool>(new[] { "-c", "--create" }, "Create the file if it doesn't exist");
            
            command.AddArgument(projectFileArg);
            command.AddArgument(sourceFileArg);
            command.AddOption(createOption);
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the add-package command
        /// </summary>
        public static Command CreateAddPackageCommand()
        {
            var command = new Command("add-package", "Add a NuGet package to a μHigh project");
            
            var projectFileArg = CommonOptions.CreateProjectFileArgument();
            var packageNameArg = new Argument<string>("package-name", "Name of the NuGet package to add");
            var versionArg = new Argument<string?>("version", () => null, "Version of the package (latest if not specified)");
            
            var compileOnlyOption = new Option<bool>(new[] { "-c", "--compile-only" }, "Include package only for compilation, not runtime");
            var prereleaseOption = new Option<bool>(new[] { "-p", "--prerelease" }, "Include prerelease versions");
            var sourceOption = new Option<string?>(new[] { "-s", "--source" }, "NuGet package source URL");
            
            command.AddArgument(projectFileArg);
            command.AddArgument(packageNameArg);
            command.AddArgument(versionArg);
            command.AddOption(compileOnlyOption);
            command.AddOption(prereleaseOption);
            command.AddOption(sourceOption);
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the restore-packages command
        /// </summary>
        public static Command CreateRestorePackagesCommand()
        {
            var command = new Command("restore-packages", "Restore NuGet packages for a μHigh project");
            
            var forceOption = new Option<bool>(new[] { "-f", "--force" }, "Force re-download of packages");
            
            command.AddArgument(CommonOptions.CreateProjectFileArgument());
            command.AddOption(forceOption);
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the install-packages command
        /// </summary>
        public static Command CreateInstallPackagesCommand()
        {
            var command = new Command("install-packages", "Install NuGet packages for a μHigh project");
            
            command.AddArgument(CommonOptions.CreateProjectFileArgument());
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the search-packages command
        /// </summary>
        public static Command CreateSearchPackagesCommand()
        {
            var command = new Command("search-packages", "Search for available NuGet packages");
            
            var searchTermArg = new Argument<string>("search-term", "Package name or search term");
            var takeOption = new Option<int>(new[] { "-t", "--take" }, () => 10, "Number of results to show");
            
            command.AddArgument(searchTermArg);
            command.AddOption(takeOption);
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the list-packages command
        /// </summary>
        public static Command CreateListPackagesCommand()
        {
            var command = new Command("list-packages", "List packages in a μHigh project");
            
            command.AddArgument(CommonOptions.CreateProjectFileArgument());
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the AST command
        /// </summary>
        public static Command CreateAstCommand()
        {
            var command = new Command("ast", "Print the Abstract Syntax Tree of a μHigh source file");
            
            command.AddArgument(CommonOptions.CreateSourceFileArgument());
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the test command
        /// </summary>
        public static Command CreateTestCommand()
        {
            var command = new Command("test", "Run μHigh compiler tests");
            
            var filterOption = new Option<string?>("--filter", "Filter tests by name pattern");
            var listOption = new Option<bool>("--list", "List available tests");
            
            command.AddOption(filterOption);
            command.AddOption(listOption);
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }

        /// <summary>
        /// Creates the LSP command
        /// </summary>
        public static Command CreateLspCommand()
        {
            var command = new Command("lsp", "Start Language Server Protocol mode");
            
            var portOption = new Option<int?>("--port", "TCP port for LSP server");
            var stdioOption = new Option<bool>("--stdio", () => true, "Use stdio for LSP communication");
            
            command.AddOption(portOption);
            command.AddOption(stdioOption);
            command.AddOption(CommonOptions.CreateVerboseOption());
            command.AddOption(CommonOptions.CreateStdLibPathOption());
            command.AddOption(CommonOptions.CreateNoExceptionOption()); // Add here

            return command;
        }
    }
}
