# Learn μHigh Programming Language

Welcome to the official μHigh tutorial! This guide will teach you the μHigh programming language from the ground up, with hands-on examples and exercises.

## Table of Contents
1. [Getting Started](#getting-started)
2. [Your First Program](#your-first-program)
3. [Variables and Constants](#variables-and-constants)
4. [Basic Input and Output](#basic-input-and-output)
5. [Data Types](#data-types)
6. [Operators](#operators)
7. [Control Flow](#control-flow)
8. [Functions](#functions)
9. [Classes and Objects](#classes-and-objects)
10. [Namespaces](#namespaces)
11. [Advanced Features](#advanced-features)
12. [Best Practices](#best-practices)
13. [Exercises](#exercises)

## Getting Started

Before we begin coding, make sure you have μHigh set up:

1. Clone the repository and build the compiler
2. Create a new file with the `.uh` extension, or use the interactive REPL
3. Run the provided install script to set up your environment
4. Open your favorite text editor or IDE (like Visual Studio Code)
5. Start writing your μHigh programs!
6. Run the compiler to see your code in action with `uhigh yourfile.uh`
7. Or experiment interactively with `uhigh repl`

**Quick Start with REPL**: If you want to try μHigh immediately without creating files, start the interactive REPL with `uhigh repl` and begin typing μHigh expressions and statements.

**Multi-line Input in REPL**: When using the REPL, you can create multi-line blocks by pressing Ctrl+Enter for newlines. This is especially useful for if statements, loops, functions, and classes. Press Enter alone to execute your code.

Note: μHigh is designed to be similar to C# and Java, so if you have experience with those languages, you'll find μHigh familiar.
Another Note: This tutorial assumes the μHigh compiler has fully implemented the features discussed here, so make sure you have the latest version.
If any features are not yet implemented, you can still learn the syntax and concepts, and apply them once they are available.

## Your First Program

Let's start with the traditional "Hello, World!" program:

```go
namespace MyFirstApp
{
    public class Program 
    {
        public static func Main(args: string[]): void {
            Console.WriteLine("Hello, World!")
        }
    }
}
```

**What's happening here?**
- `namespace` groups related code together
- `class Program` defines a class named Program
- `public static func Main` is the entry point of your program
- `Console.WriteLine()` prints text to the screen

**Try it yourself:** Change the message to say "Hello, μHigh!" instead.

## Variables and Constants

Variables store data that can change, while constants store data that never changes.

### Variables

```go
// Declare a variable
var message: string
var count: int
var price: float

// Declare and assign at the same time
var name: string = "Alice"
var age: int = 25
var isStudent: bool = true

// μHigh can often figure out the type automatically
var city = "New York"  // Automatically a string
var temperature = 72.5  // Automatically a float
```

### Constants

```go
// Constants never change once set
const PI = 3.14159
const MAX_USERS = 1000
const COMPANY_NAME = "Tech Corp"
```

**Exercise:** Create variables for your name, age, and favorite color. Print them out.

## Basic Input and Output

### Output with Console.WriteLine

```go
Console.WriteLine("Simple text")
Console.WriteLine("Hello, " + name)  // Combining strings
Console.WriteLine("Age: " + age.ToString())  // Converting numbers to strings
```

### Input from User

```go
using StdLib
// Usage
var userName = IO.Input("What's your name? ")
Console.WriteLine("Hello, " + userName + "!")
```

## Data Types

μHigh supports several built-in data types:

### Basic Types

```go
// Numbers
var wholeNumber: int = 42
var decimalNumber: float = 3.14159
var bigNumber: long = 1000000000

// Text
var singleChar: char = 'A'
var text: string = "Hello, World!"

// True/False
var isValid: bool = true
var isComplete: bool = false

// Special
var nothing: void  // Used for functions that don't return anything
```

### Working with Strings

```go
var firstName = "John"
var lastName = "Doe"
var fullName = firstName + " " + lastName

// String properties and methods (using C# methods)
var length = fullName.Length
var uppercase = fullName.ToUpper()
var lowercase = fullName.ToLower()
```

Note: Any method that is default in C# is also available in μHigh, such as `ToUpper()`, `ToLower()`, `Length`, etc.
Please refer to the CSharp documentation for more details on these methods.

**Exercise:** Create a program that asks for your first and last name, then displays your full name in uppercase.

## Operators

### Arithmetic Operators

```go
var a = 10
var b = 3

var sum = a + b        // Addition: 13
var difference = a - b  // Subtraction: 7
var product = a * b     // Multiplication: 30
var quotient = a / b    // Division: 3 (integer division)
var remainder = a % b   // Modulo: 1
```

### Comparison Operators

```go
var x = 5
var y = 10

var isEqual = x == y        // false
var isNotEqual = x != y     // true
var isLess = x < y          // true
var isGreater = x > y       // false
var isLessOrEqual = x <= y  // true
var isGreaterOrEqual = x >= y // false
```

### Logical Operators

```go
var isSunny = true
var isWarm = false

var niceDay = isSunny && isWarm  // AND: false
var okDay = isSunny || isWarm    // OR: true
var notSunny = !isSunny          // NOT: false
```

### Assignment Operators

```go
var counter = 10

counter = counter + 5  // Traditional way
counter += 5           // Shorthand: same as above
counter -= 3           // Subtract and assign
counter *= 2           // Multiply and assign
counter /= 4           // Divide and assign

counter++              // Increment by 1
counter--              // Decrement by 1
```

## Control Flow

### If Statements

```go
var age = 18

if age >= 18 {
    Console.WriteLine("You can vote!")
} else {
    Console.WriteLine("Too young to vote.")
}

// Multiple conditions
var score = 85

if score >= 90 {
    Console.WriteLine("Grade: A")
} else if score >= 80 {
    Console.WriteLine("Grade: B")
} else if score >= 70 {
    Console.WriteLine("Grade: C")
} else {
    Console.WriteLine("Grade: F")
}
```

### While Loops

```go
// Count from 1 to 5
var counter = 1
while counter <= 5 {
    Console.WriteLine("Count: " + counter)
    counter++
}

// Loop until user enters "quit"
var input = ""
while input != "quit" {
    input = input("Enter command (or 'quit' to exit): ")
    if (input != "quit") {
        Console.WriteLine("You entered: " + input)
    }
}
```

### For Loops

```go
// Traditional for loop
for var i = 0; i < 10; i++ {
    Console.WriteLine("Number: " + i)
}

// For-in loop (when arrays are implemented)
var numbers = [1, 2, 3, 4, 5]
for num in numbers {
    Console.WriteLine("Value: " + num)
}
```

**Exercise:** Write a program that asks the user to guess a number between 1 and 10, and keeps asking until they get it right.

## Functions

Functions are reusable blocks of code that perform specific tasks.

### Basic Functions

```go
// Function that doesn't return anything
public static func greet(): void {
    Console.WriteLine("Hello from a function!")
}

// Function that takes parameters
public static func greetPerson(name: string): void {
    Console.WriteLine("Hello, " + name + "!")
}

// Function that returns a value
public static func add(a: int, b: int): int {
    return a + b
}

// Using the functions
greet()
greetPerson("Alice")
var result = add(5, 3)
Console.WriteLine("5 + 3 = " + result)
```

### More Function Examples

```go
// Function with multiple parameters
public static func calculateArea(width: float, height: float): float {
    return width * height
}

// Function that uses other functions
public static func describeRectangle(w: float, h: float): void {
    var area = calculateArea(w, h)
    Console.WriteLine("Rectangle: " + w + " x " + h)
    Console.WriteLine("Area: " + area)
}
```

**Exercise:** Write a function that takes a temperature in Celsius and returns it in Fahrenheit. Formula: F = C × 9/5 + 32

## Classes and Objects

Classes are blueprints for creating objects. Objects are instances of classes.

### Your First Class

```go
public class Person {
    // Fields store data
    private field name: string
    private field age: int
    
    // Constructor initializes the object
    public Person(personName: string, personAge: int) {
        this.name = personName
        this.age = personAge
    }
    
    // Methods are functions that belong to the class
    public func greet(): void {
        Console.WriteLine("Hi, I'm " + this.name + " and I'm " + this.age + " years old.")
    }
    
    public func haveBirthday(): void {
        this.age++
        Console.WriteLine("Happy birthday! " + this.name + " is now " + this.age)
    }
}
```

### Using Classes

```go
// Create objects (instances of the class)
var person1 = Person("Alice", 25)
var person2 = Person("Bob", 30)

// Call methods on the objects
person1.greet()
person2.greet()

person1.haveBirthday()
```


## Namespaces

Namespaces organize your code and prevent naming conflicts.

```go
namespace MyCompany.Utils {
    public class Calculator {
        public static func add(a: int, b: int): int {
            return a + b
        }
    }
}

namespace MyCompany.Models {
    public class User {
        public field Name: string 
        public field Email: string
    }
}

// Using classes from different namespaces
namespace MyCompany.Main {
    public class Program {
        public static func Main(args: array<string>): void {
            var result = Utils.Calculator.add(5, 3)
            var user = Models.User()
            user.Name = "John Doe"
        }
    }
}
```

## Advanced Features

### Match Expressions (Pattern Matching)

```go
match command  {
    "help" => showHelp(),
    "exit" => exitProgram(),
    "save" => saveFile(),
    _ => Console.WriteLine("Unknown command: " + command)
}
```

### Static Methods and Fields

```go
public class MathUtils {
    public static field PI: float = 3.14159
    
    public static func square(x: float): float {
        return x * x
    }
    
    public static func circleArea(radius: float): float {
        return PI * square(radius)
    }
}

// Usage - no need to create an instance
var area = MathUtils.circleArea(5.0)
```

## Best Practices

### 1. Naming Conventions
- Use **PascalCase** for classes, methods, and properties: `MyClass`, `DoSomething()`
- Use **camelCase** for variables and fields: `userName`, `totalAmount`
- Use **UPPER_CASE** for constants: `MAX_SIZE`, `DEFAULT_COLOR`

### 2. Code Organization
```go
// Good: Organized and clear
namespace MyApp.Models {
    public class User {
        private field id: int
        public field Name: string 
        
        public func constructor(userId: int, userName: string) {
            this.id = userId
            this.Name = userName
        }
    }
}
```

### 3. Comment Your Code
```go
// Calculate the compound interest
// Formula: A = P(1 + r/n)^(nt)
public static func calculateCompoundInterest(
    principal: float,  // Initial amount
    rate: float,       // Annual interest rate
    compounds: int,    // Times compounded per year
    years: int         // Number of years
): float {
    var base = 1 + (rate / compounds)
    var exponent = compounds * years
    return principal * Math.Pow(base, exponent)
}
```

## Exercises

### Beginner Exercises

1. **Personal Introduction**
   - Create a program that asks for your name, age, and hobby
   - Display a formatted introduction message

2. **Simple Calculator**
   - Ask user for two numbers and an operation (+, -, *, /)
   - Display the result

3. **Number Guessing Game**
   - Generate a random number between 1-100
   - Let the user guess until they get it right
   - Give "higher" or "lower" hints

### Intermediate Exercises

4. **Grade Calculator**
   - Create a `Student` class with name and grades
   - Add methods to calculate average and letter grade
   - Create multiple students and display a report

5. **Simple Banking System**
   - Create `BankAccount` class with deposit/withdraw methods
   - Track transaction history
   - Implement overdraft protection

6. **Library Management**
   - Create `Book` and `Library` classes
   - Implement check-out/check-in functionality
   - Track due dates and overdue books

### Advanced Exercises

7. **Command Line Calculator**
   - Support multiple operations in one expression
   - Handle parentheses and order of operations
   - Add functions like sqrt, pow, etc.

8. **Text-Based Adventure Game**
   - Create classes for Player, Room, Item
   - Implement movement, inventory, and interactions
   - Save/load game state

## What's Next?

Congratulations! You've learned the basics of μHigh programming. Here are some next steps:

1. **Explore the Standard Library**: Learn about built-in .NET classes and methods
2. **Build Projects**: Start with small programs and gradually make them more complex
3. **Read the Language Reference**: Check out [LANGUAGE.md](../LANGUAGE.md) for detailed syntax
4. **Join the Community**: Contribute to the μHigh project or help other learners

### Useful Resources

- [Language Reference](../LANGUAGE.md) - Complete syntax documentation
- [README](../README.md) - Project overview and setup
- [Feature Roadmap](../features/v1impl.md) - Upcoming language features

Happy coding with μHigh! 🚀