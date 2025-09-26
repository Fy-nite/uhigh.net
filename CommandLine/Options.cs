using System.CommandLine;

namespace uhigh.Net.CommandLine
{
    /// <summary>
    /// Factory for creating common options used across commands
    /// </summary>
    public static class CommonOptions
    {
        /// <summary>
        /// Creates a verbose option
        /// </summary>
        public static Option<bool> CreateVerboseOption()
        {
            return new Option<bool>(
                aliases: new[] { "-v", "--verbose" },
                description: "Enable verbose output");
        }

        /// <summary>
        /// Creates a standard library path option
        /// </summary>
        public static Option<string?> CreateStdLibPathOption()
        {
            return new Option<string?>(
                aliases: new[] { "--stdlib", "--stdlib-path" },
                description: "Path to μHigh standard library");
        }

        /// <summary>
        /// Creates a save C# code option
        /// </summary>
        public static Option<string?> CreateSaveCSharpOption()
        {
            return new Option<string?>(
                aliases: new[] { "--save-cs" },
                description: "Save generated C# code to the specified folder");
        }

        /// <summary>
        /// Creates a project file argument (optional)
        /// </summary>
        public static Argument<string?> CreateProjectFileArgument()
        {
            return new Argument<string?>(
                name: "project-file",
                description: "Path to the μHigh project file (if not specified, will search current directory)",
                getDefaultValue: () => null
            );
        }

        /// <summary>
        /// Creates a source file argument
        /// </summary>
        public static Argument<string> CreateSourceFileArgument()
        {
            return new Argument<string>(
                name: "source-file",
                description: "Path to the source file");
        }

        /// <summary>
        /// Finds a .uhighproj file in the specified directory
        /// </summary>
        public static string? FindProjectFile(string? directory = null)
        {
            directory ??= Environment.CurrentDirectory;
            var files = Directory.GetFiles(directory, "*.uhighproj", SearchOption.TopDirectoryOnly);
            if (files.Length == 1)
                return files[0];
            if (files.Length > 1)
                throw new Exception($"Multiple .uhighproj files found in {directory}. Please specify one.");
            return null;
        }

        /// <summary>
        /// Creates the global '--type-errors-as-warnings' option
        /// </summary>
        public static Option<bool> CreateTypeErrorsAsWarningsOption()
        {
            return new Option<bool>("--type-errors-as-warnings", "Treat type errors as warnings instead of errors");
        }
    }

    /// <summary>
    /// Options container for commands that need common options
    /// </summary>
    public class BaseCommandOptions
    {
        public bool Verbose { get; set; }
        public string? StdLibPath { get; set; }
        /// <summary>
        /// Treat type errors as warnings
        /// </summary>
        public bool TypeErrorsAsWarnings { get; set; }
    }
}
