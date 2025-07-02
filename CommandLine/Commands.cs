using CommandLine;

namespace uhigh.Net.CommandLine
{
    // Base options that apply to all commands
    /// <summary>
    /// The base options class
    /// </summary>
    public class BaseOptions
    {
        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }

        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option("stdlib", Required = false, HelpText = "Path to standard library directory")]
        public string? StdLibPath { get; set; }
    }


    // Language Server Protocol mode
    /// <summary>
    /// The lsp options class
    /// </summary>
    /// <seealso cref="BaseOptions"/>
    [Verb("lsp", HelpText = "Start Language Server Protocol mode")]
    public class LspOptions : BaseOptions
    {
        /// <summary>
        /// Gets or sets the value of the port
        /// </summary>
        [Option("port", Required = false, HelpText = "TCP port for LSP server")]
        public int? Port { get; set; }

        /// <summary>
        /// Gets or sets the value of the use stdio
        /// </summary>
        [Option("stdio", Required = false, Default = true, HelpText = "Use stdio for LSP communication")]
        public bool UseStdio { get; set; } = true;
    }

    // Run tests
    /// <summary>
    /// The test options class
    /// </summary>
    /// <seealso cref="BaseOptions"/>
    [Verb("test", HelpText = "Run μHigh compiler tests")]
    public class TestOptions : BaseOptions
    {
        /// <summary>
        /// Gets or sets the value of the filter
        /// </summary>
        [Option("filter", Required = false, HelpText = "Filter tests by name pattern")]
        public string? Filter { get; set; }

        /// <summary>
        /// Gets or sets the value of the list tests
        /// </summary>
        [Option("list", Required = false, HelpText = "List available tests")]
        public bool ListTests { get; set; }
    }
}
