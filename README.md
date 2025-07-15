# μHigh Programming Language

A simple, modern language that compiles to C# and runs on .NET. μHigh offers accessible syntax with full .NET power.

## Quick Start

**Install:**
```bash
git clone https://github.com/fy-nite/uhigh.net
cd uhigh.net
dotnet build
```

**Hello World:**
```uhigh
func main() {
    print("Hello, μHigh!")
}
```
```bash
uhigh hello.uh
```

**REPL:**
```bash
uhigh repl
```

## Features

- Simple, C#/Java-like syntax
- Project system: executables, libraries, NuGet support
- Interactive REPL
- .NET integration
- Modern tooling: LSP, syntax highlighting, debugging

## Usage

**Single File:**
```bash
uhigh myfile.uh           # Run in memory
uhigh myfile.uh myapp.exe # Build executable
uhigh compile myfile.uh --save-cs ./output # Save C# code
```

**Projects:**
```bash
uhigh create MyApp --type Exe
uhigh build MyApp/MyApp.uhighproj
uhigh add-package MyApp/MyApp.uhighproj Newtonsoft.Json
uhigh restore-packages MyApp/MyApp.uhighproj
```

**Tools:**
```bash
uhigh repl
uhigh ast myfile.uh
uhigh lsp
uhigh test
```

## Language Overview

μHigh combines familiar syntax with modern features:

```uhigh
var name = "World"
const PI = 3.14159

func greet(person) {
    return "Hello, " + person + "!"
}

namespace MyApp {
    public class Calculator {
        public func add(a, b) {
            return a + b
        }
    }
}

if (name != "") {
    print(greet(name))
} else {
    print("Hello, Anonymous!")
}

Console.WriteLine("Direct .NET access")
```

## Examples

**Basic:**
```uhigh
func main() {
    var name = input("What's your name? ")
    print("Nice to meet you, " + name + "!")
}
```

**Class:**
```uhigh
public class Person {
    private field name: string
    public var Age: int 
    public func constructor(personName: string) {
        this.name = personName
    }
    public func greet() {
        Console.WriteLine("Hi, I'm " + this.name)
    }
}
```

**Project Structure:**
```
MyProject/
├── MyProject.uhighproj
├── main.uh
├── utils.uh
└── models/
    └── user.uh
```

## Documentation

- [Language Guide](docs/learn-uhigh.md)
- [Language Reference](LANGUAGE.md)
- [Project Features](features/README.md)

## Why μHigh?

- **Beginners:** Easy, readable syntax
- **.NET Developers:** Familiar, concise, rapid prototyping
- **Everyone:** Simplicity + .NET ecosystem

## Contributing

We welcome:
- Bug reports, feature requests
- Docs and code contributions
- IDE/tooling extensions

See [GitHub](https://github.com/fy-nite/uhigh.net) to get started.

## License

See [LICENSE](LICENSE) for details.

---

*μHigh: Simple syntax, powerful runtime* ⚡