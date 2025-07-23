# μHigh Template System

The μHigh compiler includes a powerful template system that allows you to create custom project templates for different types of applications.

## Overview

The template system enables:
- **Built-in Templates**: Pre-configured templates for common project types
- **External Templates**: Custom templates loaded from addons folder  
- **Extensible Architecture**: Easy to create new templates by implementing interfaces
- **Parameter Support**: Templates can accept custom parameters for configuration

## Available Built-in Templates

### Console Application (`console`)
Creates a console application with a main entry point.

**Features:**
- Main method with command-line argument handling
- Basic console output examples
- README.md with build/run instructions

**Usage:**
```bash
uhigh create MyConsoleApp --template console
uhigh create MyConsoleApp --template console --author "Your Name" --description "My awesome console app"
```

### Class Library (`classlib`)
Creates a class library project for reusable components.

**Features:**
- Library class with version and initialization methods
- Utility class with common operations
- Library-specific README.md

**Usage:**
```bash
uhigh create MyLibrary --template classlib
uhigh create MyLibrary --template classlib --author "Your Name"
```

### Test Project (`test`)
Creates a test project using μHigh's built-in testing framework.

**Features:**
- Sample test classes with `[TestingOnly]` attribute
- Example test methods with `[TestWith]` and `[Expect]` attributes
- Manual testing examples
- Testing framework documentation

**Usage:**
```bash
uhigh create MyTests --template test
```

## Command Line Usage

### Creating Projects
```bash
# Use default template (console)
uhigh create MyProject

# Specify template explicitly  
uhigh create MyProject --template console
uhigh create MyLibrary --template classlib
uhigh create MyTests --template test

# Add template parameters
uhigh create MyProject --template console --author "Your Name" --description "Project description"
```

### Listing Templates
```bash
# Show all available templates
uhigh list-templates
```

This displays:
- Template name and description
- Output type (Exe, Library, etc.)
- Version and author information
- Available parameters

## Template Parameters

All templates support these common parameters:

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--author` | Project author name | None |
| `--description` | Project description | None |
| `--target-framework` | .NET target framework | `net8.0` |

## Creating External Templates

You can create custom templates by implementing the `IProjectTemplate` interface or extending `BaseProjectTemplate`.

### Step 1: Create Template Class

```csharp
using uhigh.Net.Templates;
using uhigh.Net.Diagnostics;

namespace MyTemplates
{
    public class WebAppTemplate : BaseProjectTemplate
    {
        public override string Name => "webapp";
        public override string Description => "Creates a web application";
        public override string OutputType => "Exe";
        public override string Author => "Your Name";
        
        protected override async Task<bool> CreateProjectFileAsync(
            string projectName, 
            string projectPath, 
            Dictionary<string, object>? parameters, 
            DiagnosticsReporter? diagnostics)
        {
            // Create project file logic
            var project = new uhighProject
            {
                Name = projectName,
                OutputType = "Exe",
                SourceFiles = new List<string> { "Server.uh" },
                // ... configure project
            };
            
            var projectFilePath = Path.Combine(projectPath, $"{projectName}.uhighproj");
            return await ProjectFile.SaveAsync(project, projectFilePath, diagnostics);
        }
        
        protected override async Task<bool> CreateSourceFilesAsync(
            string projectName, 
            string projectPath, 
            Dictionary<string, object>? parameters, 
            DiagnosticsReporter? diagnostics)
        {
            // Create source files
            var sourceCode = $@"// {projectName} - Web Application
using System
using StdLib

namespace {projectName}
{{
    public class WebServer
    {{
        public static func Main(args: string[]): void
        {{
            Console.WriteLine(""Starting web server..."");
            // Web server logic here
        }}
    }}
}}";
            
            var sourceFile = Path.Combine(projectPath, "Server.uh");
            await File.WriteAllTextAsync(sourceFile, sourceCode);
            return true;
        }
    }
}
```

### Step 2: Build Template

1. Create a new C# class library project
2. Add reference to the μHigh compiler (reference `uhigh.dll`)
3. Implement your template class
4. Build the project to create a `.dll` file

### Step 3: Install Template

1. Copy the compiled `.dll` file to the `addons/templates` directory
2. The template will be automatically discovered when you run μHigh commands

Example directory structure:
```
uhigh-compiler/
├── uhigh.exe
├── addons/
│   └── templates/
│       ├── MyCustomTemplates.dll
│       └── AnotherTemplate.dll
```

### Step 4: Use Template

```bash
uhigh list-templates  # Should show your custom template
uhigh create MyWebApp --template webapp
```

## Template Interface Reference

### IProjectTemplate Interface

```csharp
public interface IProjectTemplate
{
    string Name { get; }                    // Template identifier
    string Description { get; }             // Human-readable description
    string OutputType { get; }              // "Exe", "Library", etc.
    string Version { get; }                 // Template version
    string Author { get; }                  // Template author
    
    Task<bool> CreateProjectAsync(          // Main creation method
        string projectName, 
        string projectPath, 
        Dictionary<string, object>? parameters = null, 
        DiagnosticsReporter? diagnostics = null);
    
    Dictionary<string, string> GetParameters();     // Available parameters
    bool ValidateParameters(Dictionary<string, object>? parameters);  // Parameter validation
}
```

### BaseProjectTemplate Abstract Class

Provides common functionality:
- Parameter handling with `GetParameter<T>()` helper
- Project file creation workflow
- Error handling and logging
- Template validation

Override these methods in your template:
- `CreateProjectFileAsync()` - Create the .uhighproj file
- `CreateSourceFilesAsync()` - Create source code files  
- `CreateAdditionalFilesAsync()` (optional) - Create README, config files, etc.

## Template Discovery

Templates are discovered in this order:
1. **Built-in templates** - Always available
2. **External templates** - Loaded from `addons/templates` directory

If there's a name conflict, built-in templates take precedence and a warning is shown.

## Best Practices

### Template Design
- Use descriptive template names (lowercase, no spaces)
- Provide clear descriptions
- Include README.md files with usage instructions
- Support common parameters (author, description, etc.)

### Code Generation
- Use project name in namespaces and class names
- Avoid string interpolation with curly braces in generated code
- Include proper documentation comments
- Follow μHigh coding conventions

### Error Handling
- Use DiagnosticsReporter for logging
- Validate parameters before use
- Handle file system errors gracefully
- Provide meaningful error messages

## Troubleshooting

### Template Not Found
- Check template name spelling
- Run `uhigh list-templates` to see available templates
- Ensure external template .dll is in `addons/templates` directory

### Build Errors in Generated Projects
- Check generated source code syntax
- Ensure proper string escaping in templates
- Verify project file structure
- Test with simple template first

### External Template Not Loading
- Verify .dll file is compiled correctly
- Check that template class implements `IProjectTemplate`
- Ensure all dependencies are available
- Look for error messages in verbose output (`-v` flag)

## Examples

See the built-in templates in the `Templates/BuiltIn/` directory for complete examples:
- `ConsoleAppTemplate.cs` - Simple console application
- `ClassLibraryTemplate.cs` - Reusable library  
- `TestProjectTemplate.cs` - Testing framework integration