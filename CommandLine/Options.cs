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
        /// Creates a project file argument
        /// </summary>
        public static Argument<string> CreateProjectFileArgument()
        {
            return new Argument<string>(
                name: "project-file",
                description: "Path to the μHigh project file");
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
        /// Creates a no-exception mode option
        /// </summary>
        public static Option<bool> CreateNoExceptionOption()
        {
            return new Option<bool>(
                aliases: new[] { "--no-exception" },
                description: "Suppress exceptions in lexer/parser and report errors via diagnostics only");
        }
    }

    /// <summary>
    /// Options container for commands that need common options
    /// </summary>
    public class BaseCommandOptions
    {
        public bool Verbose { get; set; }
        public string? StdLibPath { get; set; }
        public bool NoException { get; set; } // Add this property
    }
}
