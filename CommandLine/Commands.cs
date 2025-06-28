using CommandLine;

namespace uhigh.Net.CommandLine
{
    // Base options that apply to all commands
    public class BaseOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }

        [Option("stdlib", Required = false, HelpText = "Path to standard library directory")]
        public string? StdLibPath { get; set; }
    }


    // Language Server Protocol mode
    [Verb("lsp", HelpText = "Start Language Server Protocol mode")]
    public class LspOptions : BaseOptions
    {
        [Option("port", Required = false, HelpText = "TCP port for LSP server")]
        public int? Port { get; set; }

        [Option("stdio", Required = false, Default = true, HelpText = "Use stdio for LSP communication")]
        public bool UseStdio { get; set; } = true;
    }

    // Run tests
    [Verb("test", HelpText = "Run μHigh compiler tests")]
    public class TestOptions : BaseOptions
    {
        [Option("filter", Required = false, HelpText = "Filter tests by name pattern")]
        public string? Filter { get; set; }

        [Option("list", Required = false, HelpText = "List available tests")]
        public bool ListTests { get; set; }
    }
}
