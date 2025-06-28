using CommandLine;

namespace uhigh.Net.CommandLine
{
    [Verb("install-packages", HelpText = "Install NuGet packages for a μHigh project")]
    public class InstallPackagesOptions
    {
        [Value(0, MetaName = "project-file", Required = true, HelpText = "Path to the μHigh project file (.uhighproj)")]
        public string ProjectFile { get; set; } = "";
        
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }
        
        [Option('s', "stdlib", Required = false, HelpText = "Path to standard library directory")]
        public string? StdLibPath { get; set; }
    }

    [Verb("search-packages", HelpText = "Search for available NuGet packages")]
    public class SearchPackagesOptions
    {
        [Value(0, MetaName = "search-term", Required = true, HelpText = "Package name or search term")]
        public string SearchTerm { get; set; } = "";
        
        [Option('t', "take", Required = false, Default = 10, HelpText = "Number of results to show")]
        public int Take { get; set; } = 10;
        
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }
    }

    [Verb("list-packages", HelpText = "List packages in a μHigh project")]
    public class ListPackagesOptions
    {
        [Value(0, MetaName = "project-file", Required = true, HelpText = "Path to the μHigh project file (.uhighproj)")]
        public string ProjectFile { get; set; } = "";
        
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }
    }
}