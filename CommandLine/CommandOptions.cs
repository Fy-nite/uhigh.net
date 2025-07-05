using System.CommandLine;

namespace uhigh.Net.CommandLine
{
    /// <summary>
    /// Options for the compile command
    /// </summary>
    public class CompileOptions : BaseCommandOptions
    {
        public string SourceFile { get; set; } = "";
        public string? OutputFile { get; set; }
        public bool RunInMemory { get; set; }
        public string? SaveCSharpTo { get; set; }
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the REPL command
    /// </summary>
    public class ReplOptions : BaseCommandOptions
    {
        public string? SaveCSharpTo { get; set; }
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the create command
    /// </summary>
    public class CreateOptions : BaseCommandOptions
    {
        public string ProjectName { get; set; } = "";
        public string? Directory { get; set; }
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string OutputType { get; set; } = "Exe";
        public string TargetFramework { get; set; } = "net8.0";
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the build command
    /// </summary>
    public class BuildOptions : BaseCommandOptions
    {
        public string ProjectFile { get; set; } = "";
        public string? OutputFile { get; set; }
        public string? SaveCSharpTo { get; set; }
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the run command
    /// </summary>
    public class RunOptions : BaseCommandOptions
    {
        public string ProjectFile { get; set; } = "";
        public string? SaveCSharpTo { get; set; }
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the info command
    /// </summary>
    public class InfoOptions : BaseCommandOptions
    {
        public string ProjectFile { get; set; } = "";
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the add-file command
    /// </summary>
    public class AddFileOptions : BaseCommandOptions
    {
        public string ProjectFile { get; set; } = "";
        public string SourceFile { get; set; } = "";
        public bool CreateFile { get; set; }
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the add-package command
    /// </summary>
    public class AddPackageOptions : BaseCommandOptions
    {
        public string ProjectFile { get; set; } = "";
        public string PackageName { get; set; } = "";
        public string? Version { get; set; }
        public bool CompileTimeOnly { get; set; }
        public bool IncludePrerelease { get; set; }
        public string? Source { get; set; }
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the restore-packages command
    /// </summary>
    public class RestorePackagesOptions : BaseCommandOptions
    {
        public string ProjectFile { get; set; } = "";
        public bool Force { get; set; }
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the install-packages command
    /// </summary>
    public class InstallPackagesOptions : BaseCommandOptions
    {
        public string ProjectFile { get; set; } = "";
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the search-packages command
    /// </summary>
    public class SearchPackagesOptions
    {
        public string SearchTerm { get; set; } = "";
        public int Take { get; set; } = 10;
        public bool Verbose { get; set; }
    }

    /// <summary>
    /// Options for the list-packages command
    /// </summary>
    public class ListPackagesOptions
    {
        public string ProjectFile { get; set; } = "";
        public bool Verbose { get; set; }
    }

    /// <summary>
    /// Options for the AST command
    /// </summary>
    public class AstOptions : BaseCommandOptions
    {
        public string SourceFile { get; set; } = "";
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the test command
    /// </summary>
    public class TestOptions : BaseCommandOptions
    {
        public string? Filter { get; set; }
        public bool ListTests { get; set; }
        public bool NoException { get; set; } // Add this
    }

    /// <summary>
    /// Options for the LSP command
    /// </summary>
    public class LspOptions : BaseCommandOptions
    {
        public int? Port { get; set; }
        public bool UseStdio { get; set; } = true;
        public bool NoException { get; set; } // Add this
    }
}