# μHigh Programming Language

μHigh is a simple high-level programming language that compiles to C# and then to .NET executables.

## Table of Contents
- [Features](#features)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Language Reference](#language-reference)
- [μHigh vs C#](#μhigh-vs-c)
- [Roadmap](#roadmap)

## Features

### Current Features
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
- Compiles to C# and then to .NET executables

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
     git clone https://github.com/your-username/wake.net
     cd wake.net
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

```go
func main() {
    print("Hello, μHigh!")
}
```

Compile and run it:

```bash
dotnet run hello.uh
# This creates hello.exe which you can run directly
./hello.exe
```

## Usage

### Compile a single file

```bash
dotnet run source.uh
```

This will generate a `source.exe` file with the compiled executable.

### Specify output file

```bash
dotnet run source.uh output.exe
```

This will compile `source.uh` and create `output.exe`.

## Language Reference

For detailed syntax documentation and examples, see [LANGUAGE.md](LANGUAGE.md).

## μHigh vs C#

μHigh provides a simpler syntax compared to C#, making it easier to write and understand code for beginners. Here are some benefits:

- **Readability**: μHigh code is more readable and concise.
- **Ease of Use**: μHigh simplifies common programming tasks.
- **Maintainability**: μHigh code is easier to maintain and modify.

C#, on the other hand, is a more powerful language that provides more features and control but requires more effort to write and understand.

μHigh exists to provide benefits of high-level programming languages with a simpler syntax, while still leveraging the power and performance of the .NET runtime.

## Roadmap

The planned features listed above represent our development priorities. Contributions and feedback are welcome to help shape the future of μHigh.