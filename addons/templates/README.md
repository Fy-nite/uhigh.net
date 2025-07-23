# Example External Template

This directory contains examples of how to create external templates for μHigh.

## Creating External Templates

To create an external template:

1. Create a new C# class library project
2. Reference the μHigh compiler (add reference to uhigh.dll)
3. Implement the `IProjectTemplate` interface or extend `BaseProjectTemplate`
4. Compile your template into a .dll file
5. Place the .dll file in this directory

## Example Template Structure

```csharp
using uhigh.Net.Templates;
using uhigh.Net.Diagnostics;

namespace MyTemplates
{
    public class MyCustomTemplate : BaseProjectTemplate
    {
        public override string Name => "mytemplate";
        public override string Description => "My custom project template";
        public override string OutputType => "Exe";
        
        protected override async Task<bool> CreateProjectFileAsync(...)
        {
            // Create project file logic
        }
        
        protected override async Task<bool> CreateSourceFilesAsync(...)
        {
            // Create source files logic
        }
    }
}
```

## Installation

After building your template:
1. Copy the .dll file to this directory
2. Run `uhigh list-templates` to verify it's discovered
3. Use it with `uhigh create MyProject --template mytemplate`

Templates in this directory will be loaded automatically when the μHigh compiler starts.