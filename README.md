# μHigh Programming Language

A simple, modern programming language that compiles to C# and runs on .NET. μHigh provides an accessible syntax while leveraging the full power of the .NET ecosystem.

## Quick Start

### Installation

```bash
git clone https://github.com/fy-nite/uhigh.net
cd uhigh.net
dotnet build
```

### Hello World

Create `hello.uh`:
```uhigh
func main() {
    print("Hello, μHigh!")
}
```

Run it:
```bash
uhigh hello.uh
```

### Interactive Development

Try the REPL for immediate feedback:
```bash
uhigh repl
```

## Key Features

- **Simple Syntax** - Easy to learn, familiar to C#/Java developers
- **Project System** - Full support for executables and libraries with NuGet packages
- **Interactive REPL** - Experiment with code in real-time
- **.NET Integration** - Direct access to .NET libraries and frameworks
- **Modern Tooling** - Language server, syntax highlighting, and debugging support

## Usage

### Single File Compilation
```bash
# Compile and run in memory
uhigh myfile.uh

# Create executable
uhigh myfile.uh myapp.exe

# Save generated C# code
uhigh compile myfile.uh --save-cs ./output
```

### Project Management
```bash
# Create new project
uhigh create MyApp --type Exe

# Build project
uhigh build MyApp/MyApp.uhighproj

# Add dependencies
uhigh add-package MyApp/MyApp.uhighproj Newtonsoft.Json

# Manage packages
uhigh restore-packages MyApp/MyApp.uhighproj
```

### Development Tools
```bash
# Interactive REPL
uhigh repl

# View AST
uhigh ast myfile.uh

# Language server for IDEs
uhigh lsp

# Run tests
uhigh test
```

## Language Overview

μHigh combines familiar syntax with modern features:

```uhigh
// Variables and functions
var name = "World"
const PI = 3.14159

func greet(person: string) {
    return "Hello, " + person + "!"
}

// Classes and namespaces
namespace MyApp {
    public class Calculator {
        public func add(a: int , b: int): int {
            return a + b
        }
    }
}

// Control flow
if name != "" {
    print(greet(name))
} else {
    print("Hello, Anonymous!")
}

// Integration with .NET
Console.WriteLine("Direct .NET access")
```

### Current Features

- Variables (`var`) and constants (`const`)
- Functions with parameters and return values
- Classes with methods and properties (fields vs properties)
- Namespaces for code organization
- Control flow: `if/else`, `while`, `for` loops
- .NET library integration and NuGet packages
- Project system with dependency management
- Expression evaluation and arithmetic operations

### Planned Features

- Arrays and collections
- Error handling (`try/catch`)
- String manipulation functions
- Type checking and explicit declarations
- Lambda functions and advanced OOP
- Standard library expansion

## Documentation

- **[Language Guide](docs/learn-uhigh.md)** - Complete tutorial for beginners
- **[Language Reference](LANGUAGE.md)** - Detailed syntax documentation
- **[Project Features](features/README.md)** - Current implementation status

## IDE Support

### Visual Studio Code
1. Install the μHigh extension from the marketplace, or
2. Copy `Syntax/uhigh.tmLanguage.json` to your extensions folder

### Other IDEs
- **Visual Studio 2022**: Use LSP server with `uhigh lsp`
- **Any Editor**: TextMate grammar available in `Syntax/` folder

## Why μHigh?

**For Beginners**: μHigh offers a gentle learning curve with clear, readable syntax that doesn't overwhelm new programmers.

**For .NET Developers**: Leverage your existing .NET knowledge while enjoying a more concise syntax for rapid prototyping and scripting.

**For Everyone**: Get the performance and ecosystem of .NET with the simplicity of a modern scripting language.

## Examples

### Basic Program
```uhigh
func main() {
    var name = input("What's your name? ")
    print("Nice to meet you, " + name + "!")
}
```

### Class Definition
```uhigh
public class Person {
    private field name: string
    public var Age: int { get; set; }
    
    public func constructor(personName: string) {
        this.name = personName
    }
    
    public func greet() {
        Console.WriteLine("Hi, I'm " + this.name)
    }
}
```

### Project Structure
```
MyProject/
├── MyProject.uhighproj    # Project configuration
├── main.uh               # Entry point
├── utils.uh              # Helper functions
└── models/
    └── user.uh           # Data models
```

## Contributing

We welcome contributions! Whether it's:
- Bug reports and feature requests
- Documentation improvements
- Code contributions
- IDE extensions and tooling

Check our [GitHub repository](https://github.com/fy-nite/uhigh.net) to get started.

## License

AGPL
---

*μHigh: Simple syntax, powerful runtime* ⚡
