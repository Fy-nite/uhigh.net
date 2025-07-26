using uhigh.Net.Diagnostics;

namespace uhigh.Net.Templates.BuiltIn
{
    /// <summary>
    /// Template for creating console applications
    /// </summary>
    public class ConsoleAppTemplate : BaseProjectTemplate
    {
        /// <summary>
        /// Gets the name of the template
        /// </summary>
        public override string Name => "console";
        
        /// <summary>
        /// Gets the description of the template
        /// </summary>
        public override string Description => "Creates a console application with a main entry point";
        
        /// <summary>
        /// Gets the output type this template creates
        /// </summary>
        public override string OutputType => "Exe";
        
        /// <summary>
        /// Gets the author of the template
        /// </summary>
        public override string Author => "μHigh Built-in";
        
        /// <summary>
        /// Creates the project file for the template
        /// </summary>
        protected override async Task<bool> CreateProjectFileAsync(string projectName, string projectPath, Dictionary<string, object>? parameters, DiagnosticsReporter? diagnostics)
        {
            try
            {
                var description = GetParameter(parameters, "description", (string?)null);
                var author = GetParameter(parameters, "author", (string?)null);
                var targetFramework = GetParameter(parameters, "targetFramework", "net9.0");
                
                var project = new uhighProject
                {
                    Name = projectName,
                    Version = "1.0.0",
                    Description = description,
                    Author = author,
                    Target = targetFramework,
                    OutputType = OutputType,
                    SourceFiles = new List<string> { "main.uh" },
                    RootNamespace = projectName,
                    // Note: stdlib dependency removed due to .NET 9.0 targeting issue
                    // TODO: Re-add when stdlib supports .NET 9.0
                    Dependencies = new List<PackageReference>(),
                    Nullable = true
                };
                
                var projectFilePath = Path.Combine(projectPath, $"{projectName}.uhighproj");
                return await ProjectFile.SaveAsync(project, projectFilePath, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics?.ReportError($"Failed to create project file: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Creates the source files for the template
        /// </summary>
        protected override async Task<bool> CreateSourceFilesAsync(string projectName, string projectPath, Dictionary<string, object>? parameters, DiagnosticsReporter? diagnostics)
        {
            try
            {
                var mainFilePath = Path.Combine(projectPath, "main.uh");
                
                var sourceCode = $@"// {projectName} - Console Application
using System

namespace {projectName}
{{
    public class Program
    {{
        /// <summary>
        /// Main entry point for the console application
        /// </summary>
        /// <param name=""args"">Command line arguments</param>
        /// <returns>void</returns>
        public static func Main(args: string[]) : void
        {{
            Console.WriteLine(""Hello, μHigh! Welcome to {projectName}!"");
            
            if (args.Length > 0)
            {{
                Console.WriteLine(""Arguments received:"");
                for (var i = 0; i < args.Length; i++)
                {{
                    Console.WriteLine(""  ["" + i + ""]: "" + args[i]);
                }}
            }}
        }}
    }}
}}";
                
                await File.WriteAllTextAsync(mainFilePath, sourceCode);
                diagnostics?.ReportInfo($"Created main source file: {mainFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                diagnostics?.ReportError($"Failed to create source files: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Creates additional files for the template
        /// </summary>
        protected override async Task CreateAdditionalFilesAsync(string projectName, string projectPath, Dictionary<string, object>? parameters, DiagnosticsReporter? diagnostics)
        {
            try
            {
                // Create a README file
                var readmeFilePath = Path.Combine(projectPath, "README.md");
                var readmeContent = $@"# {projectName}

A console application created with μHigh.

## Build

```bash
uhigh build {projectName}.uhighproj
```

## Run

```bash
uhigh run {projectName}.uhighproj
```

## About

This project was created using the console application template for μHigh.
";
                
                await File.WriteAllTextAsync(readmeFilePath, readmeContent);
                diagnostics?.ReportInfo($"Created README file: {readmeFilePath}");
            }
            catch (Exception ex)
            {
                diagnostics?.ReportWarning($"Failed to create additional files: {ex.Message}");
            }
        }
    }
}