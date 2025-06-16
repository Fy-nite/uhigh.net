# Additional μHigh Language Features

This document outlines proposed enhancements to the μHigh programming language, building upon the existing feature set.

## High Priority Features

### 1. Enum Support

Define enumerated types for better type safety and code clarity:

```go
enum Status {
    Active,
    Inactive,
    Pending,
    Suspended = 100  // Explicit values
}

enum Color {
    Red = "#FF0000",
    Green = "#00FF00",
    Blue = "#0000FF"
}

// Usage
var currentStatus: Status = Status.Active
var userColor: Color = Color.Red

if currentStatus == Status.Active {
    print("System is running")
}
```

### 2. Interface Definitions

Define contracts that classes must implement:

```go
interface IDrawable {
    func draw()
    func getArea(): float
    var IsVisible: bool { get; set; }
}

interface IComparable<T> {
    func compareTo(other: T): int
}

class Circle : IDrawable {
    private field radius: float
    
    public var IsVisible: bool { get; set; } = true
    
    public func draw() {
        print("Drawing circle with radius " + string(this.radius))
    }
    
    public func getArea(): float {
        return 3.14159 * this.radius * this.radius
    }
}
```

### 3. Generic Types

Enable type-safe containers and methods:

```go
class List<T> {
    private field items: T[]
    private field count: int = 0
    
    public func add(item: T) {
        this.items[this.count] = item
        this.count++
    }
    
    public func get(index: int): T {
        return this.items[index]
    }
    
    public var Count: int { get = this.count }
}

// Generic functions
func swap<T>(a: T, b: T): (T, T) {
    return (b, a)
}

// Usage
var stringList = List<string>()
stringList.add("Hello")
stringList.add("World")

var (x, y) = swap<int>(10, 20)
```

### 4. Pattern Matching & Switch Expressions

Modern pattern matching for cleaner conditional logic:

```go
// Switch expressions
var message = status switch {
    Status.Active => "System running",
    Status.Inactive => "System stopped",
    Status.Pending => "System starting",
    _ => "Unknown status"
}

// Pattern matching with destructuring
var point = (10, 20)
var description = point switch {
    (0, 0) => "Origin",
    (var x, 0) => $"On X-axis at {x}",
    (0, var y) => $"On Y-axis at {y}",
    (var x, var y) when x == y => "On diagonal",
    _ => "General point"
}

// Type pattern matching
func processValue(value: object) {
    var result = value switch {
        string s when s.length > 0 => "Non-empty string",
        int i when i > 0 => "Positive integer",
        float f => "Floating point number",
        null => "Null value",
        _ => "Unknown type"
    }
    print(result)
}
```

### 5. Null Safety Features

Enhanced null handling with nullable types and operators:

```go
// Nullable types
var name: string? = null  // Can be null
var age: int = 25         // Cannot be null

// Null conditional operator
var length = name?.length  // Returns int? (null if name is null)

// Null coalescing operator
var displayName = name ?? "Anonymous"  // Use "Anonymous" if name is null

// Null coalescing assignment
name ??= "Default Name"  // Assign only if name is null

// Null forgiving operator (use with caution)
var upperName = name!.toUpper()  // Assert name is not null

// Pattern matching with null
var result = name switch {
    null => "No name provided",
    string s when s.length == 0 => "Empty name",
    string s => $"Hello, {s}!"
}
```

## Medium Priority Features

### 6. Tuple Types

Lightweight data structures for multiple values:

```go
// Tuple declarations
var point: (float, float) = (10.5, 20.3)
var person: (string name, int age) = ("John", 25)

// Tuple functions
func getCoordinates(): (float x, float y) {
    return (10.5, 20.3)
}

func getPersonInfo(): (string, int, string) {
    return ("Alice", 30, "Engineer")
}

// Destructuring
var (x, y) = getCoordinates()
var (name, age, job) = getPersonInfo()

// Partial destructuring
var (firstName, _, profession) = getPersonInfo()  // Ignore age

// Tuple comparison
var point1 = (1, 2)
var point2 = (1, 2)
if point1 == point2 {
    print("Points are equal")
}
```

### 7. Record Types

Immutable data classes with value semantics:

```go
record Person(name: string, age: int)

record Point(x: float, y: float) {
    public func distanceFrom(other: Point): float {
        var dx = this.x - other.x
        var dy = this.y - other.y
        return sqrt(dx * dx + dy * dy)
    }
}

// Usage
var person1 = Person("John", 25)
var person2 = person1 with { age = 26 }  // Create copy with modification

var origin = Point(0, 0)
var point = Point(3, 4)
var distance = point.distanceFrom(origin)
```

### 8. Extension Methods

Add methods to existing types:

```go
extension string {
    func reverse(): string {
        var chars = this.toCharArray()
        reverse(chars)
        return string(chars)
    }
    
    func isPalindrome(): bool {
        return this == this.reverse()
    }
    
    func wordCount(): int {
        return this.split(' ').length
    }
}

extension int {
    func isEven(): bool {
        return this % 2 == 0
    }
    
    func factorial(): int {
        if this <= 1 return 1
        return this * (this - 1).factorial()
    }
}

// Usage
var text = "hello"
var reversed = text.reverse()  // "olleh"
var isPalin = "racecar".isPalindrome()  // true

var number = 5
var isEven = number.isEven()  // false
var fact = number.factorial()  // 120
```

### 9. Async/Await Support

Asynchronous programming support:

```go
import System.Threading.Tasks

async func fetchDataAsync(url: string): Task<string> {
    var client = HttpClient()
    var response = await client.GetStringAsync(url)
    return response
}

async func processDataAsync(): Task {
    try {
        var data = await fetchDataAsync("https://api.example.com/data")
        print("Received: " + data)
    } catch error {
        print("Error fetching data: " + error.message)
    }
}

// Parallel execution
async func fetchMultipleAsync(): Task {
    var task1 = fetchDataAsync("https://api1.example.com")
    var task2 = fetchDataAsync("https://api2.example.com")
    
    var results = await Task.WhenAll(task1, task2)
    
    for result in results {
        print("Result: " + result)
    }
}
```

### 10. Attributes/Annotations

Metadata for classes, methods, and properties:

```go
[Serializable]
[Author("John Doe")]
public class DataModel {
    [JsonProperty("user_name")]
    [Required]
    public var UserName: string
    
    [Range(0, 120)]
    public var Age: int
    
    [Obsolete("Use NewMethod instead")]
    public func oldMethod() {
        // deprecated implementation
    }
    
    [HttpGet("/api/users/{id}")]
    public func getUser(id: int): User {
        // implementation
    }
}

// Custom attributes
[AttributeUsage(AttributeTargets.Method)]
class BenchmarkAttribute : Attribute {
    public var Iterations: int
    
    public func constructor(iterations: int = 1000) {
        this.Iterations = iterations
    }
}

[Benchmark(5000)]
func performanceTest() {
    // method implementation
}
```

## Language Improvements

### 11. String Interpolation

Embedded expressions in strings:

```go
var name = "Alice"
var age = 25
var score = 95.5

// Basic interpolation
var message = $"Hello, {name}! You are {age} years old."

// Expression interpolation
var result = $"Your grade is {score >= 90 ? "A" : "B"}"

// Format specifiers
var pi = 3.14159
var formatted = $"Pi is approximately {pi:F2}"  // "Pi is approximately 3.14"

// Multiline interpolation
var report = $"""
    Student Report:
    Name: {name}
    Age: {age}
    Score: {score:F1}%
    Grade: {score >= 90 ? "A" : score >= 80 ? "B" : "C"}
    """
```

### 12. Range Operators

Range creation and slicing:

```go
// Range creation
var range1 = 1..10        // Range from 1 to 10 (inclusive)
var range2 = 1..<10       // Range from 1 to 9 (exclusive)
var range3 = ..5          // Range from start to 5
var range4 = 5..          // Range from 5 to end

// Array slicing
var numbers = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
var slice1 = numbers[2..5]    // [3, 4, 5, 6]
var slice2 = numbers[..3]     // [1, 2, 3, 4]
var slice3 = numbers[7..]     // [8, 9, 10]

// Range iteration
for i in 1..10 {
    print(i)
}

// Reverse ranges
for i in 10..1 {
    print(i)  // Prints 10, 9, 8, ..., 1
}
```

### 13. Destructuring Assignment

Extract values from complex data structures:

```go
// Array destructuring
var numbers = [1, 2, 3, 4, 5]
var [first, second, ...rest] = numbers  // first=1, second=2, rest=[3,4,5]

// Object destructuring
var person = Person("John", 25, "Engineer")
var {name, age} = person

// Nested destructuring
var people = [
    ("Alice", 30, ("Engineer", "Tech Corp")),
    ("Bob", 25, ("Designer", "Creative Inc"))
]

for (name, age, (job, company)) in people {
    print($"{name} is a {job} at {company}")
}

// Function parameter destructuring
func processPoint((x, y): (float, float)) {
    print($"Processing point at ({x}, {y})")
}

var point = (10.5, 20.3)
processPoint(point)
```

### 14. Collection Literals

Enhanced syntax for collections:

```go
// List literals
var numbers = [1, 2, 3, 4, 5]
var names = ["Alice", "Bob", "Charlie"]

// Dictionary literals
var scores = {
    "Alice": 95,
    "Bob": 87,
    "Charlie": 92
}

// Set literals
var uniqueNumbers = {1, 2, 3, 4, 5, 1, 2}  // {1, 2, 3, 4, 5}

// Nested collections
var matrix = [
    [1, 2, 3],
    [4, 5, 6],
    [7, 8, 9]
]

// Collection with computed values
var squares = [for i in 1..10 => i * i]  // [1, 4, 9, 16, 25, ...]
var evenNumbers = [for i in 1..20 where i % 2 == 0 => i]
```

## Advanced Features

### 15. LINQ-style Query Syntax

Functional programming constructs:

```go
var people = [
    Person("Alice", 30, "Engineer"),
    Person("Bob", 25, "Designer"),
    Person("Charlie", 35, "Manager")
]

// Method chaining
var adults = people
    .where(p => p.age >= 18)
    .select(p => p.name)
    .orderBy(name => name)
    .toArray()

// Query syntax
var seniorEngineers = from p in people
                     where p.age > 30 && p.job == "Engineer"
                     select p.name

// Grouping
var ageGroups = people
    .groupBy(p => p.age / 10 * 10)  // Group by decade
    .select(g => (ageRange: g.key, count: g.count()))

// Aggregation
var averageAge = people.average(p => p.age)
var totalAge = people.sum(p => p.age)
var oldestPerson = people.maxBy(p => p.age)
```

### 16. Operator Overloading

Custom operators for user-defined types:

```go
class Vector {
    public field x: float
    public field y: float
    
    public func constructor(x: float, y: float) {
        this.x = x
        this.y = y
    }
    
    // Arithmetic operators
    public func operator+(other: Vector): Vector {
        return Vector(this.x + other.x, this.y + other.y)
    }
    
    public func operator-(other: Vector): Vector {
        return Vector(this.x - other.x, this.y - other.y)
    }
    
    public func operator*(scalar: float): Vector {
        return Vector(this.x * scalar, this.y * scalar)
    }
    
    // Comparison operators
    public func operator==(other: Vector): bool {
        return this.x == other.x && this.y == other.y
    }
    
    // Indexer
    public func operator[](index: int): float {
        return index == 0 ? this.x : this.y
    }
    
    public func operator[]=(index: int, value: float) {
        if index == 0 {
            this.x = value
        } else {
            this.y = value
        }
    }
}

// Usage
var v1 = Vector(1, 2)
var v2 = Vector(3, 4)
var v3 = v1 + v2      // Vector(4, 6)
var v4 = v1 * 2.0     // Vector(2, 4)
var isEqual = v1 == v2 // false
var component = v1[0]  // 1
```

### 17. Memory Management Features

Advanced memory and resource management:

```go
// Using statement for automatic disposal
using var file = File.Open("data.txt") {
    var content = file.ReadAllText()
    print(content)
}  // file is automatically closed

// Reference semantics
func swapValues(ref a: int, ref b: int) {
    var temp = a
    a = b
    b = temp
}

var x = 10
var y = 20
swapValues(ref x, ref y)  // x=20, y=10

// Span and Memory for efficient array operations
func processLargeArray(data: Span<int>) {
    for i in 0..<data.length {
        data[i] = data[i] * 2
    }
}

var numbers = [1, 2, 3, 4, 5]
processLargeArray(numbers[1..3])  // Process slice without copying
```

## Tooling & Development Experience

### 18. Built-in Testing Framework

Integrated unit testing support:

```go
import Testing

class MathTests {
    [Test]
    func testAddition() {
        var result = add(2, 3)
        assert result == 5, "Addition should work correctly"
    }
    
    [Test]
    func testDivision() {
        var result = divide(10, 2)
        assert result == 5
    }
    
    [Test]
    [ExpectedException(typeof(DivideByZeroException))]
    func testDivisionByZero() {
        divide(10, 0)  // Should throw exception
    }
    
    [TestCase(1, 1, 2)]
    [TestCase(5, 3, 8)]
    [TestCase(-1, 1, 0)]
    func testAdditionWithCases(a: int, b: int, expected: int) {
        var result = add(a, b)
        assert result == expected
    }
}

// Test setup and teardown
class DatabaseTests {
    private var connection: DatabaseConnection
    
    [SetUp]
    func setUp() {
        this.connection = DatabaseConnection.connect("test_db")
    }
    
    [TearDown]
    func tearDown() {
        this.connection.close()
    }
    
    [Test]
    func testUserCreation() {
        var user = createUser("test@example.com")
        assert user != null
    }
}
```

### 19. Documentation Comments

Rich documentation with examples:

```go
/// Calculates the area of a rectangle.
/// 
/// This function multiplies the width and height to compute the area.
/// Both parameters must be positive numbers.
/// 
/// @param width The width of the rectangle in units
/// @param height The height of the rectangle in units
/// @returns The area as a floating-point number
/// @throws ArgumentException when width or height is negative
/// 
/// @example
/// ```
/// var area = calculateArea(5.0, 3.0)  // Returns 15.0
/// ```
/// 
/// @see calculatePerimeter for perimeter calculation
/// @since Version 1.2.0
func calculateArea(width: float, height: float): float {
    if width < 0 || height < 0 {
        throw ArgumentException("Width and height must be positive")
    }
    return width * height
}

/// Represents a geometric shape with area and perimeter calculations.
/// 
/// @example
/// ```
/// var circle = Circle(5.0)
/// var area = circle.getArea()
/// var perimeter = circle.getPerimeter()
/// ```
class Circle {
    /// The radius of the circle
    /// @remarks Must be positive
    public var Radius: float { get; set; }
    
    /// Creates a new circle with the specified radius
    /// @param radius The radius of the circle
    public func constructor(radius: float) {
        this.Radius = radius
    }
}
```

These features would significantly enhance μHigh's capabilities while maintaining its goal of simplicity and readability. The implementation priority should focus on the most commonly used features first (enums, interfaces, null safety) before moving to more advanced concepts.

Raw C# code injection for advanced scenarios:

```go
func optimized_function() {
    sharp {
        // Raw C# code - no validation performed
        var result = System.Math.Pow(2, 10);
        Console.WriteLine($"2^10 = {result}");
        
        // Access .NET APIs directly
        var process = System.Diagnostics.Process.GetCurrentProcess();
        Console.WriteLine($"Process: {process.ProcessName}");
    }
}
```