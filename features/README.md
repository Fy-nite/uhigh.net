# μHigh Programming Language

μHigh is a simple high-level programming language that compiles to C# and then to .NET executables or libraries.

## Table of Contents
- [Features](#features)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Language Reference](#language-reference)
- [Syntax Highlighting](#syntax-highlighting)
- [μHigh vs C#](#μhigh-vs-c)
- [Roadmap](#roadmap)

## Features

### Current Features
- **Project system** with support for executables and libraries
- Variable declarations (var) and constants (const)
- **Function-based variable assignments** (e.g., `var x = input()`)
- **Class definitions** with methods and properties
- **Namespace organization** for better code structure
- **Import system** for using C# libraries and custom classes
- **Dot notation support** for qualified method calls (e.g., `Console.WriteLine()`)
- **Static method calls** using qualified names
- Basic arithmetic operations (+, -, *, /, %)
- Increment and decrement operators (++, --)
- Compound assignment operators (+=, -=, *=, /=)
- Print statements for strings and expressions
- Input statements for reading user input
- Comments using //
- If/else conditionals
- While loops with break and continue support
- For loops with break and continue support
- Basic functions
- Expression grouping with parentheses
- Include other .uh files
- Compiles to C# and then to .NET executables or libraries

### Planned Features
- **Arrays and Collections**: Support for array data structures
- **Data types**: Type checking and explicit type declarations
- **String manipulation**: Built-in string functions
- **Error handling**: Try/catch mechanism
- **Inline assembly**: Ability to embed C# directly
- **Standard library**: Common utility functions
- **Module system**: Better code organization beyond simple includes
- **Debugging tools**: Source maps and debug symbols
- **Switch statements**: Multi-way branching
- **Ternary operator**: Conditional expressions
- **Lambda functions**: Anonymous functions
- **Inheritance and polymorphism**: Advanced OOP features

## Getting Started

### Installation

1. Clone the repository:
     ```bash
     git clone https://github.com/fy-nite/uhigh.net
     cd uhigh.net
     ```

2. Make sure you have .NET 6.0 or later installed:
     ```bash
     dotnet --version
     ```

3. Build the compiler:
     ```bash
     dotnet build
     ```

### Your First μHigh Program

Create a file named `hello.uh`:

```uhigh
func main() {
    print("Hello, μHigh!")
}
```

Compile and run it:

```bash
uhigh hello.uh
# This creates and runs the program in memory
```

### Create a Project

```bash
# Create an executable project
uhigh create MyApp --type Exe

# Create a library project
uhigh create MyLibrary --type Library

# Build the project
uhigh build MyApp/MyApp.uhighproj
```

## Usage

### Compile a single file

```bash
uhigh source.uh
```

This will compile and run the program in memory.

### Specify output file

```bash
uhigh source.uh output.exe
```

This will compile `source.uh` and create `output.exe`.

### Create and build projects

```bash
# Create a new project
uhigh create MyProject --type Exe --author "Your Name"

# Build the project
uhigh build MyProject/MyProject.uhighproj

# Build to specific output
uhigh build MyProject/MyProject.uhighproj MyProject.exe
```

## Language Reference

For detailed syntax documentation and examples, see [LANGUAGE.md](LANGUAGE.md).

## Syntax Highlighting

### Visual Studio Code

1. Copy the `Syntax/uhigh.tmLanguage.json` file to your VS Code extensions folder
2. Create a `package.json` file to register the language:

```json
{
  "name": "uhigh-syntax",
  "displayName": "μHigh Language Support",
  "description": "Syntax highlighting for μHigh programming language",
  "version": "1.0.0",
  "engines": {
    "vscode": "^1.0.0"
  },
  "categories": ["Programming Languages"],
  "contributes": {
    "languages": [{
      "id": "uhigh",
      "aliases": ["μHigh", "uhigh"],
      "extensions": [".uh"],
      "configuration": "./language-configuration.json"
    }],
    "grammars": [{
      "language": "uhigh",
      "scopeName": "source.uhigh",
      "path": "./uhigh.tmLanguage.json"
    }]
  }
}
```

### Visual Studio 2022

For Visual Studio 2022, you can use the TextMate grammar through extensions or create a custom language service. The grammar file can be integrated using:

1. **VSIX Extension**: Create a Visual Studio extension that includes the TextMate grammar
2. **Language Server Protocol**: Implement an LSP server that uses the grammar
3. **Third-party Extensions**: Use extensions like "TextMate Grammars" for VS 2022

### Supported Features

The syntax highlighting includes:
- **Keywords**: `func`, `var`, `const`, `class`, `namespace`, `if`, `else`, `while`, `for`, etc.
- **Modifiers**: `public`, `private`, `protected`, `static`, `readonly`, etc.
- **Types**: `int`, `float`, `string`, `bool`, and custom types
- **Comments**: Single-line (`//`) and multi-line (`/* */`)
- **Strings**: Double-quoted strings with escape sequences
- **Numbers**: Integers and floating-point numbers
- **Operators**: Arithmetic, comparison, logical, and assignment operators
- **Attributes**: `[AttributeName]` syntax
- **Functions**: Function declarations and calls
- **Classes**: Class declarations with inheritance
- **Namespaces**: Namespace declarations with dotted names

## μHigh vs C#

μHigh provides a simpler syntax compared to C#, making it easier to write and understand code for beginners. Here are some benefits:

- **Readability**: μHigh code is more readable and concise.
- **Ease of Use**: μHigh simplifies common programming tasks.
- **Maintainability**: μHigh code is easier to maintain and modify.

C#, on the other hand, is a more powerful language that provides more features and control but requires more effort to write and understand.

μHigh exists to provide benefits of high-level programming languages with a simpler syntax, while still leveraging the power and performance of the .NET runtime.

## Roadmap

The planned features listed above represent our development priorities. Contributions and feedback are welcome to help shape the future of μHigh.