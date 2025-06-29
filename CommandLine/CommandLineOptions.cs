using CommandLine;

namespace uhigh.Net.CommandLine
{
    [Verb("compile", HelpText = "Compile a μHigh source file")]
    public class CompileOptions
    {
        [Value(0, Required = true, HelpText = "Path to the source file")]
        public string SourceFile { get; set; } = "";

        [Value(1, Required = false, HelpText = "Output file name (optional)")]
        public string? OutputFile { get; set; }

        [Option('r', "run", Required = false, HelpText = "Run the program after compiling")]
        public bool RunInMemory { get; set; } = false;

        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }

        [Option("save-cs", Required = false, HelpText = "Save generated C# code to the specified folder")]
        public string? SaveCSharpTo { get; set; }
    }

    [Verb("create", HelpText = "Create a new μHigh project")]
    public class CreateOptions
    {
        [Value(0, Required = true, HelpText = "Project name")]
        public string ProjectName { get; set; } = "";

        [Value(1, Required = false, HelpText = "Directory to create the project in")]
        public string? Directory { get; set; }

        [Value(2, Required = false, HelpText = "Project description")]
        public string? Description { get; set; }

        [Value(3, Required = false, HelpText = "Author name")]
        public string? Author { get; set; }

        [Option('t', "type", Required = false, HelpText = "Project type (Exe or Library)")]
        public string OutputType { get; set; } = "Exe";

        [Option('f', "framework", Required = false, HelpText = ".NET target framework")]
        public string TargetFramework { get; set; } = "net8.0";

        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

    [Verb("build", HelpText = "Build a μHigh project")]
    public class BuildOptions
    {
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        [Value(1, Required = false, HelpText = "Output file name (optional)")]
        public string? OutputFile { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }

        [Option("save-cs", Required = false, HelpText = "Save generated C# code to the specified folder")]
        public string? SaveCSharpTo { get; set; }
    }

    [Verb("run", HelpText = "Run a μHigh project")]
    public class RunOptions
    {
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
        [Option("save-cs", Required = false, HelpText = "Save generated C# code to the specified folder")]
        public string? SaveCSharpTo { get; set; }
    }

    [Verb("info", HelpText = "Show information about a μHigh project")]
    public class InfoOptions
    {
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

    [Verb("add-file", HelpText = "Add a source file to a μHigh project")]
    public class AddFileOptions
    {
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        [Value(1, Required = true, HelpText = "Path to the source file to add")]
        public string SourceFile { get; set; } = "";

        [Option('c', "create", Required = false, HelpText = "Create the file if it doesn't exist")]
        public bool CreateFile { get; set; } = false;

        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

    [Verb("add-package", HelpText = "Add a NuGet package to a μHigh project")]
    public class AddPackageOptions
    {
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        [Value(1, Required = true, HelpText = "Name of the NuGet package to add")]
        public string PackageName { get; set; } = "";

        [Value(2, Required = false, HelpText = "Version of the package (latest if not specified)")]
        public string? Version { get; set; }

        [Option('c', "compile-only", Required = false, HelpText = "Include package only for compilation, not runtime")]
        public bool CompileTimeOnly { get; set; } = false;

        [Option('p', "prerelease", Required = false, HelpText = "Include prerelease versions")]
        public bool IncludePrerelease { get; set; } = false;

        [Option('s', "source", Required = false, HelpText = "NuGet package source URL")]
        public string? Source { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

    [Verb("restore-packages", HelpText = "Restore NuGet packages for a μHigh project")]
    public class RestorePackagesOptions
    {
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        [Option('f', "force", Required = false, HelpText = "Force re-download of packages")]
        public bool Force { get; set; } = false;

        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

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

    [Verb("ast", HelpText = "Print the Abstract Syntax Tree of a μHigh source file")]
    public class AstOptions
    {
        [Value(0, Required = true, HelpText = "Path to the source file")]
        public string SourceFile { get; set; } = "";

        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

    
}