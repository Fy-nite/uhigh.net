# μHigh Language Server Protocol (LSP) Implementation

## Overview

This project successfully implements a Language Server Protocol (LSP) server for the μHigh programming language, enabling rich editor support in VS Code and other LSP-compatible editors.

## Architecture

### Core Components

1. **Lexer & Parser** (`/Lexer`, `/Parser`)
   - Tokenizes and parses μHigh source code
   - Generates Abstract Syntax Trees (AST)
   - Provides error reporting and diagnostics

2. **Modular Code Generation** (`/CodeGen`)
   - **ICodeGenerator Interface**: Pluggable code generation system
   - **C# Generator**: Full-featured C# code generation
   - **JavaScript Generator**: Modern ES6+ JavaScript generation
   - **Plugin System**: Load custom generators from DLLs
   - **Registry**: Centralized generator discovery and management

3. **LSP Server** (`/LanguageServer`)
   - **Protocol Models** (`/Protocol`): LSP message types and structures
   - **Core Server** (`/Core`): Main LSP server implementation
   - **Document Management** (`/Core`): Tracks open documents and changes
   - **Language Services** (`/Services`): Provides completions, hover, diagnostics, symbols

4. **VS Code Extension** (`/vscode-extension`)
   - Language configuration and syntax highlighting
   - Client-side LSP integration
   - File associations and editor features

### Key Features Implemented

- ✅ **Syntax Highlighting**: Complete TextMate grammar for μHigh
- ✅ **Diagnostics**: Real-time error and warning reporting
- ✅ **Document Symbols**: Navigate functions, variables, and types
- ✅ **Basic Completions**: Keyword and context-aware suggestions
- ✅ **Hover Information**: Type and documentation display
- ✅ **LSP Protocol Compliance**: Standard LSP 3.x implementation
- ✅ **Multi-Target Code Generation**: C#, JavaScript, and plugin support

## Usage

### Running the Compiler with Different Targets

```bash
# Compile to C# and run (default)
dotnet run --framework net8.0 -- program.uh

# Compile to JavaScript
dotnet run --framework net8.0 -- compile program.uh --target javascript

# List available targets
dotnet run --framework net8.0 -- list-targets
```

### Creating Custom Code Generators

Create a new class implementing `ICodeGenerator`:

```csharp
public class PythonGenerator : ICodeGenerator
{
    public CodeGeneratorInfo Info => new()
    {
        Name = "Python Code Generator",
        Description = "Generates Python 3.x code",
        Version = "1.0.0",
        SupportedFeatures = new() { "functions", "classes" }
    };

    public string TargetName => "python";
    public string FileExtension => ".py";

    // Implement interface methods...
}

// Register the generator
CodeGeneratorRegistry.Register("python", () => new PythonGenerator());
```

### Plugin Development

Create a separate assembly with generators:

```csharp
// In YourPlugin.dll
public class CustomGeneratorFactory : ICodeGeneratorFactory
{
    public string TargetName => "custom";
    public CodeGeneratorInfo GeneratorInfo => new() { /* ... */ };
    public ICodeGenerator CreateGenerator() => new CustomGenerator();
    public bool CanHandle(CodeGeneratorConfig config) => true;
}
```

Place the DLL in the `plugins` directory and it will be auto-loaded.

## Sample μHigh Code

```uhigh
func main() {
    Console.WriteLine("Hello from μHigh!")
    var x = 42
    var name = "World"
    Console.WriteLine("Number: " + x.ToString())
    Console.WriteLine("Greeting to " + name)
}
```

**Generated C#:**
```csharp
using System;
// ... other usings

namespace Generated
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello from μHigh!");
            var x = 42;
            var name = "World";
            Console.WriteLine("Number: " + x.ToString());
            Console.WriteLine("Greeting to " + name);
        }
    }
}
```

**Generated JavaScript:**
```javascript
(function main() {
    console.log("Hello from μHigh!");
    let x = 42;
    let name = "World";
    console.log("Number: " + x.toString());
    console.log("Greeting to " + name);
})();
```

## Build Status

- ✅ **Compilation**: Project builds successfully with warnings only
- ✅ **LSP Server**: Starts and accepts connections
- ✅ **Extension**: Compiles and provides syntax highlighting
- ✅ **Integration**: Full end-to-end functionality
- ✅ **Multi-Target**: C# and JavaScript generation working
- ✅ **Plugin System**: Dynamic generator loading

## Technical Details

### Code Generator Architecture

```
ICodeGenerator (Interface)
├── CSharpGenerator (Built-in)
├── JavaScriptGenerator (Built-in)
└── CustomGenerator (Plugin)

CodeGeneratorRegistry
├── Factory Management
├── Plugin Loading
└── Target Discovery

CodeGeneratorConfig
├── Target-specific options
├── Output configuration
└── Feature flags
```

### Available Targets

| Target | Description | Features | Dependencies |
|--------|-------------|----------|--------------|
| csharp | C# code generation | Full μHigh feature set | .NET 8.0+ |
| javascript | Modern JavaScript | Functions, classes, async | Node.js 16+ |
### Available Targets

| Target | Description | Features | Dependencies |
|--------|-------------|----------|--------------|
| csharp | C# code generation | Full μHigh feature set | .NET 8.0+ |
| javascript | Modern JavaScript | Functions, classes, async | Node.js 16+ |
