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

### Array Indices

Create slices and views of arrays with offset tracking:

```go
// Declare array and indice
var list: array<string> = ["a", "b", "c", "d", "e", "f"]
var item: arrayIndice<string>

// Set value at global index 5
list[5] = "hello"

// Create indice starting at index 5
item = list.createIndice(5)

// Collect item from source array at global index 5
list.collect(item).at(5)  // returns "hello"

// Convert indice to regular array (maps to local indices starting from 0)
var localArray = item.mapToArray()

// Add items to the indice
item.add("meow")
item.add("world")

// Collect all items from source starting at offset
item.collectAll()

// Return all items back to source array at their global positions
item.return(list)

// Access items using global indices
var value = item.at(5)    // Access at global index 5
var local = item[0]       // Access at local index 0 (maps to global index 5)
```

### Array Indice Operations

```go
func main() {
    var data = [10, 20, 30, 40, 50, 60, 70, 80]
    
    // Create a slice starting from index 3
    var slice = data.createIndice(3)
    
    // Collect specific items
    slice.collect(3).collect(4).collect(5)  // Gets items at indices 3, 4, 5
    
    // Add new items to the slice
    slice.add(90)
    slice.add(100)
    
    // Map to local array (indices 0, 1, 2, 3, 4)
    var localData = slice.mapToArray()
    print localData  // [40, 50, 60, 90, 100]
    
    // Modify slice and return to original
    slice[1] = 55  // Modify local index 1 (global index 4)
    slice.return(data)  // Updates original array
    
    print data  // [10, 20, 30, 40, 55, 60, 90, 100]
}
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

// Advanced Math (MathUtils)
MathUtils.Factorial(5)              // 120
MathUtils.GCD(12, 8)               // 4
MathUtils.LCM(4, 6)                // 12
MathUtils.IsPrime(17)              // true
MathUtils.Fibonacci(10)            // [0,1,1,2,3,5,8,13,21,34]
MathUtils.ToRadians(90)            // 1.5708
MathUtils.Distance(0, 0, 3, 4)     // 5.0
MathUtils.CircleArea(5)            // 78.54
```

### String Functions
```go
len("hello")            // String length
uppercase("hello")      // Convert to uppercase
lowercase("HELLO")      // Convert to lowercase
substring("hello", 1, 3) // Extract substring

// Advanced Formatting (Formatter)
Formatter.ToTitleCase("hello world")      // "Hello World"
Formatter.ToCamelCase("hello_world")      // "helloWorld"
Formatter.ToSnakeCase("HelloWorld")       // "hello_world"
Formatter.ToKebabCase("HelloWorld")       // "hello-world"
Formatter.Truncate("Long text", 10)       // "Long te..."
Formatter.Mask("1234567890", '*', 0, 4)   // "******7890"
```

### File System Functions
```go
// File Operations (FileUtils)
FileUtils.ReadText("file.txt")                    // Read entire file
FileUtils.WriteText("file.txt", "content")        // Write to file
FileUtils.ReadLines("file.txt")                   // Read as lines
FileUtils.CopyFile("src.txt", "dest.txt")         // Copy file
FileUtils.FileExists("file.txt")                  // Check existence
FileUtils.GetFileSize("file.txt")                 // Get size in bytes
FileUtils.GetFileSizeFormatted("file.txt")        // "1.2 MB"
FileUtils.BackupFile("important.txt")             // Create backup

// Directory Operations (DirectoryUtils)
DirectoryUtils.CreateDirectory("newfolder")        // Create directory
DirectoryUtils.GetFiles("folder", "*.txt")        // Find files
DirectoryUtils.CopyDirectory("src", "dest")       // Copy folder
DirectoryUtils.GetDirectorySize("folder")         // Size in bytes
DirectoryUtils.CleanDirectory("temp")             // Remove all contents

// Path Operations (PathUtils)
PathUtils.GetHomeDirectory()                      // User home folder
PathUtils.GetDesktopDirectory()                   // Desktop folder
PathUtils.MakeSafeFileName("file:name")           // "filename"
PathUtils.GetUniqueFileName("file.txt")           // "file (1).txt"
```

### Random Functions
```go
// Basic Random (RandomUtils)
RandomUtils.RandomInt(1, 10)                     // Random 1-9
RandomUtils.RandomDouble(0.0, 1.0)               // Random decimal
RandomUtils.RandomBool()                         // true or false
RandomUtils.Choose("apple", "banana", "cherry")   // Random choice
RandomUtils.RandomString(10)                     // Random string
RandomUtils.RandomAlphanumeric(8)                // "aB3xY9mK"
RandomUtils.Shuffle([1, 2, 3, 4, 5])            // Shuffled array

// Advanced Random
RandomUtils.RandomNormal(0, 1)                   // Normal distribution
RandomUtils.RandomColor()                        // (255, 128, 64)
RandomUtils.RandomHexColor()                     // "#FF8040"
RandomUtils.SecureRandomInt(1, 100)              // Crypto-secure

// Dice Rolling (Dice)
Dice.Roll()                                      // Roll d6
Dice.Roll(20)                                    // Roll d20
Dice.RollSum(3, 6)                              // Roll 3d6, sum
Dice.RollAdvantage()                            // Roll twice, take higher
Dice.RollNotation("3d6+2")                     // Parse dice notation
```

### Validation Functions
```go
// Data Validation (Validator)
Validator.IsEmail("user@example.com")            // true
Validator.IsUrl("https://example.com")           // true
Validator.IsPhoneNumber("(555) 123-4567")        // true
Validator.IsCreditCard("4111111111111111")       // true (test card)
Validator.IsIPv4("192.168.1.1")                 // true
Validator.IsHexColor("#FF6600")                  // true
Validator.IsStrongPassword("MyPass123!")         // true
Validator.InRange(5, 1, 10)                     // true
Validator.LengthInRange("hello", 3, 10)         // true

// String Validation
Validator.IsAlpha("hello")                       // true (letters only)
Validator.IsAlphanumeric("hello123")             // true
Validator.IsNumeric("12345")                     // true
Validator.IsInteger("-42")                       // true
Validator.IsDecimal("3.14159")                   // true
```

### Array Functions
```go
len(array)              // Array length
append(array, item)     // Add item to end
pop(array, index)       // Remove and return item
sort(array)             // Sort array
reverse(array)          // Reverse array

// Advanced Collections (Collections)
Collections.Range(1, 10, 2)                     // [1, 3, 5, 7, 9]
array.Chunk(3)                                  // Split into chunks of 3
array.Flatten()                                 // Flatten nested arrays
array.Diff(otherArray)                          // Find differences
array.Rotate(2)                                 // Rotate elements
array.MostFrequent()                            // Most common element
array.SlidingWindow(3)                          // Moving window of size 3
```

### Statistical Functions
```go
// Statistics
Statistics.Mean([1, 2, 3, 4, 5])               // 3.0
Statistics.Median([1, 2, 3, 4, 5])             // 3.0
Statistics.Mode([1, 2, 2, 3, 3, 3])            // 3.0
Statistics.StandardDeviation([1, 2, 3, 4, 5])   // 1.58
Statistics.Variance([1, 2, 3, 4, 5])           // 2.5
Statistics.Correlation(xValues, yValues)        // -0.9 to 1.0
Statistics.Percentile([1, 2, 3, 4, 5], 75)     // 4.0
```

### Temporal Functions (Advanced Time/State Management)
```go
// Temporal containers track changes over time
var data = myObject.ToTemporal()
data.Update(newValue, "reason for change")
var pastValue = data.GetSecondsAgo(30)          // Value 30 seconds ago
var history = data.GetHistory()                 // All changes

// Time utilities
TimeUtils.Now                                   // Current UTC time
TimeUtils.Since(startTime)                      // Duration since
TimeUtils.FormatDuration(timeSpan)              // "2h 30m 15s"
TimeUtils.RoundToMinute(dateTime)               // Round to minute

// Rate limiting
var limiter = RateTracker(TimeSpan.FromMinutes(1), 10)
if (limiter.TryExecute()) {
    // Action allowed (max 10 per minute)
}
```

### Reactive Programming
```go
// Observable values
var observable = Observable("initial", true)    // Track history
observable.Subscribe((old, new) => {            // Watch changes
    Console.WriteLine($"Changed from {old} to {new}")
})

// Single parameter lambda (common pattern)
observable.Subscribe(value => Console.WriteLine("New value: " + value))

// Multi-parameter lambda
var processor = (x, y) => x + y + 1

observable.Value = "new value"                  // Triggers notification

// Event streams with lambdas
var events = EventStream<string>()
events.Subscribe(data => Console.WriteLine("Event: " + data))
events.Emit("Hello World")
var recent = events.GetEventsInLastMinutes(5)   // Last 5 minutes
```

### Lambda Expressions

Lambda expressions provide a concise way to represent anonymous functions:

```go
// Single parameter lambda (no parentheses needed)
var double = x => x * 2

// Multi-parameter lambda (parentheses required)
var add = (x, y) => x + y

// Lambda with type annotations
var process = (x: int, y: int) => x + y

// Lambda in method calls
numbers.forEach(x => print(x))

// Lambda with event subscription  
observable.Subscribe(value => {
    print("Received: " + value)
})

// Multi-line lambda expressions
var complexProcessor = (data, options) => {
    var result = processData(data)
    if (options.verbose) {
        print("Processing: " + data)
    }
    return result
}
```
