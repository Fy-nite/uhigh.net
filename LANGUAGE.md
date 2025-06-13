# μHigh Language Reference

This document provides a comprehensive reference for the μHigh programming language syntax and features.

## Syntax

### Constants

Define values that cannot be changed:

```go
const PI 3.14159
const MAX_COUNT 100
```

### Variables

Declare variables that can store values:

```go
var counter
var result
```

### Assignment

Assign values to variables:

```go
counter = 1
result = counter + 41
```

### Print

Print strings or expressions:

```go
print("Hello, World!")
print(result)
```

### Input

Read input from the user:

```go
input counter
```

### Control Flow

Conditional statements and loops:

```go
if counter == 1 {
        print "Counter is one"
} else {
        print "Counter is not one"
}

while counter < 10 {
        print counter
        counter = counter + 1
}
```

### For Loops

Iterate with for loops:

```go
for i = 0; i < 10; i = i + 1 {
        print i
}

// Range-based for loop
for i in range(10) {
        print i
}

// For-each loop (for arrays)
var numbers = [1, 2, 3, 4, 5]
for num in numbers {
        print num
}
```

### Arrays

Declare and manipulate arrays:

```go
// Array declaration
var numbers = [1, 2, 3, 4, 5]
var names = ["Alice", "Bob", "Charlie"]
var empty = []

// Array access
print numbers[0]  // prints 1
names[1] = "Robert"

// Array methods
var length = len(numbers)
append(numbers, 6)
var first = pop(numbers, 0)
```

### String Operations

String manipulation and operations:

```go
var greeting = "Hello"
var name = "World"
var message = greeting + ", " + name + "!"

// String methods
var length = len(message)
var upper = uppercase(message)
var lower = lowercase(message)
var substr = substring(message, 0, 5)
```

### Data Types

Explicit type declarations:

```go
var count: int = 42
var price: float = 19.99
var name: string = "μHigh"
var isActive: bool = true

// Type casting
var num = int("123")
var text = string(456)
var decimal = float(42)
```

### Functions

Define reusable code blocks:

```go
func add(a, b) {
        return a + b
}

func main() {
        var sum
        sum = add(10, 20)
        print sum
}
```

### Advanced Functions

Function overloading and default parameters:

```go
// Default parameters
func greet(name = "World") {
        print "Hello, " + name + "!"
}

// Multiple return values
func divide(a, b) {
        return a / b, a % b
}

var quotient, remainder = divide(10, 3)
```

### Error Handling

Try-catch blocks for error handling:

```go
try {
        var result = divide(10, 0)
        print result
} catch error {
        print "Error: " + error
}
```

### Inline Assembly

Embed MicroASM code directly:

```go
func optimized_add(a, b) {
        asm {
                LOAD R0, a
                ADD R0, b
                STORE result, R0
        }
        return result
}
```

### Include

Include other μHigh files:

```go
include "utils.uh"
```

### Comments

Single-line and multi-line comments:

```go
// This is a single-line comment

/*
This is a
multi-line comment
*/

var x = 42  // Inline comment
```

### Conditions
- Equal: ==
- Not equal: !=
- Less than: <
- Greater than: >
- Less or equal: <=
- Greater or equal: >=

### Logical Operators
- And: &&
- Or: ||
- Not: !

```go
if x > 0 && y < 10 {
        print "Valid range"
}

if !isActive || count == 0 {
        print "Inactive or empty"
}
```

### Expressions
- Numbers: `42`
- Variables: `x`
- Arithmetic: `x + y`, `10 * 5`

### Advanced Expressions
- Ternary operator: `condition ? value1 : value2`
- Increment/Decrement: `x++`, `--y`
- Compound assignment: `+=`, `-=`, `*=`, `/=`

```go
var result = x > 0 ? "positive" : "non-positive"
counter++
total += amount
```

## Built-in Functions

### Math Functions
```go
abs(-5)         // Absolute value
sqrt(16)        // Square root
pow(2, 3)       // Power (2^3)
min(5, 3)       // Minimum
max(5, 3)       // Maximum
random()        // Random number 0-1
```

### String Functions
```go
len("hello")            // String length
uppercase("hello")      // Convert to uppercase
lowercase("HELLO")      // Convert to lowercase
substring("hello", 1, 3) // Extract substring
```

### Array Functions
```go
len(array)              // Array length
append(array, item)     // Add item to end
pop(array, index)       // Remove and return item
sort(array)             // Sort array
reverse(array)          // Reverse array
```

## Examples

### Basic Example

```go
func main() {
        var x = 42
        print("The answer is:")
        print(x)
}
```

### Input and Output Example

```go
func main() {
        var name = ""
        print("Enter your name:")
        input name
        print("Hello, " + name + "!")
}
```

### Array Example

```go
func main() {
        var numbers = [5, 2, 8, 1, 9]
        print "Original array:"
        for num in numbers {
                print num
        }
        
        sort(numbers)
        print "Sorted array:"
        for i = 0; i < len(numbers); i++ {
                print numbers[i]
        }
}
```

### Error Handling Example

```go
func safe_divide(a, b) {
        try {
                if b == 0 {
                        throw "Division by zero"
                }
                return a / b
        } catch error {
                print "Error: " + error
                return 0
        }
}

func main() {
        var result = safe_divide(10, 0)
        print "Result: " + result
}
```

### Type System Example

```go
func calculate_area(length: float, width: float): float {
        return length * width
}

func main() {
        var l: float = 5.5
        var w: float = 3.2
        var area: float = calculate_area(l, w)
        print "Area: " + string(area)
}
```

### Classes

Define custom types with properties, fields, and methods:

```go
public class Person {
    private field name: string     // Field - direct storage
    private field age: int = 0     // Field with default value
    
    public var Name: string {      // Property - can have logic
        get { return this.name }
        set { this.name = value }
    }
    
    public func constructor(name: string, age: int) {
        this.name = name
        this.age = age
    }
    
    public func greet() {
        Console.WriteLine("Hello, I'm " + this.name)
    }
}
```

### Fields vs Properties

- **Fields** (`field`) - Direct storage, compiled to C# fields
- **Properties** (`var`) - Can have getters/setters, compiled to C# properties

```go
public class Example {
    private field _data: string           // Private field
    public field MaxCount: int = 100      // Public field with default
    readonly field Id: string             // Readonly field
    static field Counter: int = 0         // Static field
    
    // Auto-implemented properties
    public var Name: string { get; set; }
    public var ReadOnlyProp: string { get; }
    
    // Expression-bodied properties
    public var FullName: string {
        get = this.FirstName + " " + this.LastName
    }
    
    // Block-bodied properties
    public var Data: string {
        get {
            return this._data?.ToUpper()
        }
        set {
            this._data = value?.Trim()
        }
    }
    
    // Property with initializer
    public var Status: string = "Active"
}
```

### Property Accessor Syntax

μHigh supports several property accessor patterns:

```go
// Auto-implemented properties
var Name: string { get; set; }          // Read-write
var ReadOnly: string { get; }           // Read-only

// Expression-bodied accessors
var FullName: string {
    get = FirstName + " " + LastName    // Computed property
}

// Block-bodied accessors
var Value: int {
    get {
        return this._value * 2
    }
    set {
        this._value = value / 2
    }
}

// Mixed accessor styles
var Mixed: string {
    get = this._data?.ToUpper()         // Expression-bodied getter
    set {                               // Block-bodied setter
        this._data = value?.Trim()
    }
}
```
