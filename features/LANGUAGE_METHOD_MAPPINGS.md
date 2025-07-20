# Language Method Mapping Functions

This document describes a set of utility functions for mapping common operations (such as array access, string manipulation, and collection methods) to their native equivalents in different target languages supported by μHigh.

These mapping functions are used by the code generators to produce idiomatic code for each target language.

## Implementation Status

✅ **IMPLEMENTED**: The method mappings described below are now actively converted by the code generators.

Note: any function not listed here might not properly convert to their target language equivalents, if that happens, please open an issue on the Github repository and use the built in compiler `#ifdef` to conditionally compile code for specific languages.
This will allow you to write target-specific code that can be compiled only for the desired language.

## Function List

### Add_to

Adds an element to a collection or array.

**Usage**: `Add_to(collection, element)`

| μHigh Call                | C# Output                | JavaScript Output     | C++ Output           | Notes                        |
|---------------------------|--------------------------|----------------------|----------------------|------------------------------|
| `Add_to(arr, x)`          | `arr.Add(x)`             | `arr.push(x)`        | `arr.push_back(x)`   | array/List                   |
| `Add_to(set, x)`          | `set.Add(x)`             | `set.add(x)`         | `set.insert(x)`      | Set/HashSet                  |

### Remove_from

Removes an element (or at index) from a collection or array.

**Usage**: `Remove_from(collection, element_or_index)`

| μHigh Call                | C# Output                | JavaScript Output     | C++ Output           | Notes                        |
|---------------------------|--------------------------|----------------------|----------------------|------------------------------|
| `Remove_from(arr, x)`     | `arr.Remove(x)`          | `arr.splice(x, 1)`   | `arr.erase(x)`       | array/List                   |
| `Remove_from(set, x)`     | `set.Remove(x)`          | `set.delete(x)`      | `set.erase(x)`       | Set/HashSet                  |

### Length_of

Gets the length or count of a collection, array, or string.

**Usage**: `Length_of(collection_or_string)`

| μHigh Call           | C# Output         | JavaScript Output     | C++ Output           | Notes                        |
|----------------------|------------------|----------------------|----------------------|------------------------------|
| `Length_of(arr)`     | `arr.Count`      | `arr.length`         | `arr.size()`         | array/List                   |
| `Length_of(str)`     | `str.Length`     | `str.length`         | `str.length()`       | string                       |

### Index_of

Accesses an element by index or key.

**Usage**: `Index_of(collection, index_or_key)`

| μHigh Call                | C# Output         | JavaScript Output     | C++ Output           | Notes                        |
|---------------------------|------------------|----------------------|----------------------|------------------------------|
| `Index_of(arr, i)`        | `arr[i]`         | `arr[i]`             | `arr[i]`             | array/List                   |
| `Index_of(dict, k)`       | `dict[k]`        | `dict[k]`            | `dict[k]`            | Dictionary/Map               |

### Substring_of

Extracts a substring.

**Usage**: `Substring_of(string, start_index, length)`

| μHigh Call                       | C# Output                      | JavaScript Output                | C++ Output              |
|----------------------------------|-------------------------------|----------------------------------|-------------------------|
| `Substring_of(str, start, len)`  | `str.Substring(start, len)`   | `str.substring(start, start+len)`| `str.substr(start, len)`|

### ToUpper / ToLower

Converts a string to uppercase or lowercase.

**Usage**: `ToUpper(string)` or `ToLower(string)`

| μHigh Call         | C# Output         | JavaScript Output     | C++ Output                       |
|--------------------|------------------|----------------------|----------------------------------|
| `ToUpper(str)`     | `str.ToUpper()`  | `str.toUpperCase()`  | `/* use std::transform */`       |
| `ToLower(str)`     | `str.ToLower()`  | `str.toLowerCase()`  | `/* use std::transform */`       |

### Contains_in

Checks if a collection contains an element or if a string contains a substring.

**Usage**: `Contains_in(collection_or_string, element_or_substring)`

| μHigh Call                | C# Output              | JavaScript Output     | C++ Output                |
|---------------------------|----------------------|----------------------|---------------------------|
| `Contains_in(arr, x)`     | `arr.Contains(x)`     | `arr.includes(x)`    | `std::find(...) != end()` |
| `Contains_in(str, x)`     | `str.Contains(x)`     | `str.includes(x)`    | `str.find(x) != string::npos` |

### Unary Operators

Support for unary expressions is also implemented:

| μHigh Expression  | C# Output       | JavaScript Output | C++ Output     | Notes                        |
|-------------------|----------------|------------------|---------------|------------------------------|
| `!expr`           | `!expr`        | `!expr`          | `!expr`       | Logical NOT                  |
| `-expr`           | `-expr`        | `-expr`          | `-expr`       | Unary minus                  |
| `++var`           | `++var`        | `++var`          | `++var`       | Pre-increment                |
| `--var`           | `--var`        | `--var`          | `--var`       | Pre-decrement                |
| `var++`           | `var++`        | `var++`          | `var++`       | Post-increment               |
| `var--`           | `var--`        | `var--`          | `var--`       | Post-decrement               |

---

## Example Usage

Here are some examples of how these methods work in μHigh code:

```uhigh
// Adding elements to an array
var numbers = [1, 2, 3]
Add_to(numbers, 42)  // C#: numbers.Add(42), JS: numbers.push(42)

// Getting length
var count = Length_of(numbers)  // C#: numbers.Count, JS: numbers.length

// String manipulation
var name = "hello"
var upper = ToUpper(name)      // C#: name.ToUpper(), JS: name.toUpperCase()
var substring = Substring_of(name, 1, 3)  // C#: name.Substring(1, 3)

// Array access
var first = Index_of(numbers, 0)  // C#: numbers[0], JS: numbers[0]

// Using unary operators
var x = 5
var negative = -x    // Unary minus
var notTrue = !true  // Logical NOT
++x                  // Pre-increment
--x                  // Pre-decrement
var y = x++          // Post-increment
var z = x--          // Post-decrement
```

These will generate the appropriate target-language code automatically based on your compilation target.

---


