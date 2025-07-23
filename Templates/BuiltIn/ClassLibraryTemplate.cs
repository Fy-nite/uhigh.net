using uhigh.Net.Diagnostics;

namespace uhigh.Net.Templates.BuiltIn
{
    /// <summary>
    /// Template for creating class libraries
    /// </summary>
    public class ClassLibraryTemplate : BaseProjectTemplate
    {
        /// <summary>
        /// Gets the name of the template
        /// </summary>
        public override string Name => "classlib";
        
        /// <summary>
        /// Gets the description of the template
        /// </summary>
        public override string Description => "Creates a class library project";
        
        /// <summary>
        /// Gets the output type this template creates
        /// </summary>
        public override string OutputType => "Library";
        
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
                var targetFramework = GetParameter(parameters, "targetFramework", "net8.0");
                
                var project = new uhighProject
                {
                    Name = projectName,
                    Version = "1.0.0",
                    Description = description,
                    Author = author,
                    Target = targetFramework,
                    OutputType = OutputType,
                    SourceFiles = new List<string> { "Library.uh" },
                    RootNamespace = projectName,
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
                var libraryFilePath = Path.Combine(projectPath, "Library.uh");
                
                var sourceCode = $@"// {projectName} - Class Library
using System
using StdLib

namespace {projectName}
{{
    /// <summary>
    /// Main class for the {projectName} library
    /// </summary>
    public class {projectName}Library
    {{
        /// <summary>
        /// Gets the library name
        /// </summary>
        public static field LibraryName: string = ""{projectName}""
        
        /// <summary>
        /// Gets the library version
        /// </summary>
        public static field Version: string = ""1.0.0""
        
        /// <summary>
        /// Initializes the library
        /// </summary>
        public static func Initialize(): void
        {{
            Console.WriteLine(""Initializing "" + LibraryName + "" v"" + Version);
        }}
        
        /// <summary>
        /// Performs a sample operation
        /// </summary>
        /// <param name=""input"">Input value</param>
        /// <returns>Processed output</returns>
        public static func ProcessData(input: string): string
        {{
            if (input == null)
            {{
                return ""No data provided"";
            }}
            
            return $""Processed: {{input}}"";
        }}
    }}
    
    /// <summary>
    /// Utility class for common operations
    /// </summary>
    public class {projectName}Utils
    {{
        /// <summary>
        /// Validates input data
        /// </summary>
        /// <param name=""data"">Data to validate</param>
        /// <returns>True if valid</returns>
        public static func ValidateData(data: string): bool
        {{
            return data != null && data.Length > 0;
        }}
        
        /// <summary>
        /// Formats data for display
        /// </summary>
        /// <param name=""data"">Data to format</param>
        /// <returns>Formatted string</returns>
        public static func FormatData(data: string): string
        {{
            if (!ValidateData(data))
            {{
                return ""[Invalid Data]"";
            }}
            
            return ""[{projectName}] "" + data;
        }}
    }}
}}";
                
                await File.WriteAllTextAsync(libraryFilePath, sourceCode);
                diagnostics?.ReportInfo($"Created library source file: {libraryFilePath}");
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

A class library created with μHigh.

## Build

```bash
uhigh build {projectName}.uhighproj
```

## Usage

This library provides the following main classes:

- `{projectName}Library`: Main library class with core functionality
- `{projectName}Utils`: Utility class with helper methods

### Example Usage

```csharp
using {projectName};

// Initialize the library
{projectName}Library.Initialize();

// Process some data
var result = {projectName}Library.ProcessData(""test data"");

// Format data for display
var formatted = {projectName}Utils.FormatData(""sample"");
```

## About

This project was created using the class library template for μHigh.
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