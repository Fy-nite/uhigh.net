# Language Type Mapping Table

This document describes how μHigh language types map to native types in different target languages (C#, JavaScript, LLVM IR).  
Use this as a reference for writing target-agnostic μHigh code and for implementing code generators.

## Type Mapping Table

| μHigh Type         | C# Type         | JavaScript Type | LLVM IR Type        | Notes                        |
|--------------------|-----------------|-----------------|--------------------|------------------------------|
| `int`              | `int`           | `number`        | `i32`              | 32-bit signed integer        |
| `long`             | `long`          | `number`        | `i64`              | 64-bit signed integer        |
| `float`            | `float`         | `number`        | `float`            | 32-bit floating point        |
| `double`           | `double`        | `number`        | `double`           | 64-bit floating point        |
| `decimal`          | `decimal`       | `number`        | `double`           | High-precision numeric       |
| `byte`             | `byte`          | `number`        | `i8`               | 8-bit unsigned integer       |
| `sbyte`            | `sbyte`         | `number`        | `i8`               | 8-bit signed integer         |
| `short`            | `short`         | `number`        | `i16`              | 16-bit signed integer        |
| `ushort`           | `ushort`        | `number`        | `i16`              | 16-bit unsigned integer      |
| `uint`             | `uint`          | `number`        | `i32`              | 32-bit unsigned integer      |
| `ulong`            | `ulong`         | `number`        | `i64`              | 64-bit unsigned integer      |
| `bool`             | `bool`          | `boolean`       | `i1`               | Boolean value                |
| `string`           | `string`        | `string`        | `%String*`         | String reference type        |
| `char`             | `char`          | `string`        | `i8`               | Character (single byte)      |
| `Guid`             | `Guid`          | `string`        | `%Guid*`           | Unique identifier            |
| `object`           | `object`        | `object`        | `%Object*`         | Base object type             |
| `array<T>`         | `List<T>`       | `Array<T>`      | `%Array*`          | Generic array/collection     |
| `array`            | `List<object>`  | `Array<any>`    | `%Array*`          | Untyped array                |
| `Dictionary<K,V>`  | `Dictionary<K,V>`| `Map<K,V>`     | `%Dictionary*`     | Key-value map                |
| `Set<T>`           | `HashSet<T>`    | `Set<T>`        | `%Set*`            | Unordered unique collection  |
| `Tuple<T1,T2>`     | `Tuple<T1,T2>`  | `[T1, T2]`      | `{T1, T2}`         | Fixed-size heterogeneous tuple |
| `enum`             | `enum`          | `object`        | `i32`              | Enum type (integer in LLVM)  |
| `void`             | `void`          | `void`          | `void`             | No return value              |
| `DateTime`         | `DateTime`      | `Date`          | `%DateTime*`       | Date/time type               |
| `any`              | `object`        | `any`           | `%Object*`         | Dynamic/unknown type         |
| `T` (generic)      | `T`             | `T`             | `%generic*`        | Type parameter               |
| `Func<T>`          | `Func<T>`       | `Function`      | `function ptr`     | Function/lambda type         |

## Example Usage

Suppose you declare a variable in μHigh:

```uhigh
var count: int = 42
```

- In C#: `int count = 42;`
- In JavaScript: `let count = 42;`
- In LLVM IR: `%count = alloca i32`, `store i32 42, i32* %count`

For a string array:

```uhigh
var names: array<string> = ["Alice", "Bob"]
```

- In C#: `List<string> names = new List<string> { "Alice", "Bob" };`
- In JavaScript: `let names = ["Alice", "Bob"];`
- In LLVM IR: *complex structure creation with array initialization*

## Notes

- μHigh types are case-insensitive (`int`, `Int`, `INT` are equivalent).
- For generic types, use angle brackets: `array<T>`, `Dictionary<K,V>`.
- For custom classes, the type name maps to appropriate representation in each target.
- LLVM IR uses pointer types extensively for complex data structures.
- The LLVM backend enables native performance with optimized machine code generation.

---
---
