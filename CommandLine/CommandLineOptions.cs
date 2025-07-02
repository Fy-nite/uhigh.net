using CommandLine;

namespace uhigh.Net.CommandLine
{
    /// <summary>
    /// The compile options class
    /// </summary>
    [Verb("compile", HelpText = "Compile a μHigh source file")]
    public class CompileOptions
    {
        /// <summary>
        /// Gets or sets the value of the source file
        /// </summary>
        [Value(0, Required = true, HelpText = "Path to the source file")]
        public string SourceFile { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the output file
        /// </summary>
        [Value(1, Required = false, HelpText = "Output file name (optional)")]
        public string? OutputFile { get; set; }

        /// <summary>
        /// Gets or sets the value of the run in memory
        /// </summary>
        [Option('r', "run", Required = false, HelpText = "Run the program after compiling")]
        public bool RunInMemory { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }

        /// <summary>
        /// Gets or sets the value of the save c sharp to
        /// </summary>
        [Option("save-cs", Required = false, HelpText = "Save generated C# code to the specified folder")]
        public string? SaveCSharpTo { get; set; }
    }

    /// <summary>
    /// The repl options class
    /// </summary>
    [Verb("repl", HelpText = "Start a REPL (Read-Eval-Print Loop) for μHigh")]
    public class ReplOptions
    {
        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;  
        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
        /// <summary>
        /// Gets or sets the value of the save c sharp to
        /// </summary>
        [Option("save-cs", Required = false, HelpText = "Save generated C# code to the specified folder")]
        public string? SaveCSharpTo { get; set; }
    }

    /// <summary>
    /// The create options class
    /// </summary>
    [Verb("create", HelpText = "Create a new μHigh project")]
    public class CreateOptions
    {
        /// <summary>
        /// Gets or sets the value of the project name
        /// </summary>
        [Value(0, Required = true, HelpText = "Project name")]
        public string ProjectName { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the directory
        /// </summary>
        [Value(1, Required = false, HelpText = "Directory to create the project in")]
        public string? Directory { get; set; }

        /// <summary>
        /// Gets or sets the value of the description
        /// </summary>
        [Value(2, Required = false, HelpText = "Project description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the value of the author
        /// </summary>
        [Value(3, Required = false, HelpText = "Author name")]
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the value of the output type
        /// </summary>
        [Option('t', "type", Required = false, HelpText = "Project type (Exe or Library)")]
        public string OutputType { get; set; } = "Exe";

        /// <summary>
        /// Gets or sets the value of the target framework
        /// </summary>
        [Option('f', "framework", Required = false, HelpText = ".NET target framework")]
        public string TargetFramework { get; set; } = "net8.0";

        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

    /// <summary>
    /// The build options class
    /// </summary>
    [Verb("build", HelpText = "Build a μHigh project")]
    public class BuildOptions
    {
        /// <summary>
        /// Gets or sets the value of the project file
        /// </summary>
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the output file
        /// </summary>
        [Value(1, Required = false, HelpText = "Output file name (optional)")]
        public string? OutputFile { get; set; }

        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }

        /// <summary>
        /// Gets or sets the value of the save c sharp to
        /// </summary>
        [Option("save-cs", Required = false, HelpText = "Save generated C# code to the specified folder")]
        public string? SaveCSharpTo { get; set; }
    }

    /// <summary>
    /// The run options class
    /// </summary>
    [Verb("run", HelpText = "Run a μHigh project")]
    public class RunOptions
    {
        /// <summary>
        /// Gets or sets the value of the project file
        /// </summary>
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
        /// <summary>
        /// Gets or sets the value of the save c sharp to
        /// </summary>
        [Option("save-cs", Required = false, HelpText = "Save generated C# code to the specified folder")]
        public string? SaveCSharpTo { get; set; }
    }

    /// <summary>
    /// The info options class
    /// </summary>
    [Verb("info", HelpText = "Show information about a μHigh project")]
    public class InfoOptions
    {
        /// <summary>
        /// Gets or sets the value of the project file
        /// </summary>
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

    /// <summary>
    /// The add file options class
    /// </summary>
    [Verb("add-file", HelpText = "Add a source file to a μHigh project")]
    public class AddFileOptions
    {
        /// <summary>
        /// Gets or sets the value of the project file
        /// </summary>
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the source file
        /// </summary>
        [Value(1, Required = true, HelpText = "Path to the source file to add")]
        public string SourceFile { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the create file
        /// </summary>
        [Option('c', "create", Required = false, HelpText = "Create the file if it doesn't exist")]
        public bool CreateFile { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

    /// <summary>
    /// The add package options class
    /// </summary>
    [Verb("add-package", HelpText = "Add a NuGet package to a μHigh project")]
    public class AddPackageOptions
    {
        /// <summary>
        /// Gets or sets the value of the project file
        /// </summary>
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the package name
        /// </summary>
        [Value(1, Required = true, HelpText = "Name of the NuGet package to add")]
        public string PackageName { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the version
        /// </summary>
        [Value(2, Required = false, HelpText = "Version of the package (latest if not specified)")]
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the value of the compile time only
        /// </summary>
        [Option('c', "compile-only", Required = false, HelpText = "Include package only for compilation, not runtime")]
        public bool CompileTimeOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the include prerelease
        /// </summary>
        [Option('p', "prerelease", Required = false, HelpText = "Include prerelease versions")]
        public bool IncludePrerelease { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the source
        /// </summary>
        [Option('s', "source", Required = false, HelpText = "NuGet package source URL")]
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

    /// <summary>
    /// The restore packages options class
    /// </summary>
    [Verb("restore-packages", HelpText = "Restore NuGet packages for a μHigh project")]
    public class RestorePackagesOptions
    {
        /// <summary>
        /// Gets or sets the value of the project file
        /// </summary>
        [Value(0, Required = true, HelpText = "Path to the μHigh project file")]
        public string ProjectFile { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the force
        /// </summary>
        [Option('f', "force", Required = false, HelpText = "Force re-download of packages")]
        public bool Force { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

    /// <summary>
    /// The install packages options class
    /// </summary>
    [Verb("install-packages", HelpText = "Install NuGet packages for a μHigh project")]
    public class InstallPackagesOptions
    {
        /// <summary>
        /// Gets or sets the value of the project file
        /// </summary>
        [Value(0, MetaName = "project-file", Required = true, HelpText = "Path to the μHigh project file (.uhighproj)")]
        public string ProjectFile { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }
        
        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option('s', "stdlib", Required = false, HelpText = "Path to standard library directory")]
        public string? StdLibPath { get; set; }
    }

    /// <summary>
    /// The search packages options class
    /// </summary>
    [Verb("search-packages", HelpText = "Search for available NuGet packages")]
    public class SearchPackagesOptions
    {
        /// <summary>
        /// Gets or sets the value of the search term
        /// </summary>
        [Value(0, MetaName = "search-term", Required = true, HelpText = "Package name or search term")]
        public string SearchTerm { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the value of the take
        /// </summary>
        [Option('t', "take", Required = false, Default = 10, HelpText = "Number of results to show")]
        public int Take { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }
    }

    /// <summary>
    /// The list packages options class
    /// </summary>
    [Verb("list-packages", HelpText = "List packages in a μHigh project")]
    public class ListPackagesOptions
    {
        /// <summary>
        /// Gets or sets the value of the project file
        /// </summary>
        [Value(0, MetaName = "project-file", Required = true, HelpText = "Path to the μHigh project file (.uhighproj)")]
        public string ProjectFile { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }
    }

    /// <summary>
    /// The ast options class
    /// </summary>
    [Verb("ast", HelpText = "Print the Abstract Syntax Tree of a μHigh source file")]
    public class AstOptions
    {
        /// <summary>
        /// Gets or sets the value of the source file
        /// </summary>
        [Value(0, Required = true, HelpText = "Path to the source file")]
        public string SourceFile { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the verbose
        /// </summary>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets the value of the std lib path
        /// </summary>
        [Option("stdlib-path", Required = false, HelpText = "Path to μHigh standard library")]
        public string? StdLibPath { get; set; }
    }

    
}