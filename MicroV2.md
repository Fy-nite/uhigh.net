# MicroASM Instruction Set Documentation

This document details all instructions available in the MicroASM language.

### Notes

-   **Case Sensitivity:** Instructions and directives (`MOV`, `LBL`, `MACRO`) are case-insensitive. Labels, register names, state variable names, and macro arguments (`my_label`, `RAX`, `counter`, `arg1`) are case-sensitive.
-   **Syntax:** Arguments are separated by spaces. Commas are not used between arguments (e.g., `MOV RAX RBX`).
-   **Labels:** Labels are defined using `LBL name` and referenced in control flow instructions (JMP, CALL, conditional jumps) using `#name`. See Label Scoping section for details.

-   **Memory Access:** Memory can be accessed via labels (for static data defined using `label: DB` etc.), `STATE` variables, stack-relative addressing (`[RBP - offset]`), register indirect (`$RAX`), or indexed addressing (`[RBP + RDI * 8 - 16]`). Direct numeric address access (`$1000`) and direct data initialization (`DB $1000 "..."`) are also possible but require **extreme caution** due to safety and portability risks. See Memory Addressing and Static Data Definition sections for details and warnings.

-   **Immediate Values:** Numeric immediate values (e.g., `10`, `-5`, `0xFF`) are generally treated as 64-bit signed integers (`<QWORD>`) unless the instruction context implies a different size (e.g., initializing a `<BYTE>` state variable, or specific instruction variants). Floating-point immediates (e.g., `3.14`, `-0.5`) are treated as `<DOUBLE>`.
-   **Comments:** Use `;` for single-line comments. Everything after `;` to the end of the line is ignored.

- **Square brackets:** using Square brackets `[]`, you can do simple memory calculations or access memory with `$` prefixed

## Registers

MicroASM provides the following 64-bit registers:

**General Purpose & Special Function:**
-   `RAX`: Accumulator, often used for return values.
-   `RBX`: Base register.
-   `RCX`: Counter register.
-   `RDX`: Data register, often used for division/multiplication results.
-   `RSI`: Source Index, used in string/memory operations.
-   `RDI`: Destination Index, used in string/memory operations.
-   `RBP`: Base Pointer, typically points to the base of the current stack frame.
-   `RSP`: Stack Pointer, points to the top of the stack.
-   `RIP`: Instruction Pointer, holds the address of the next instruction to execute. Generally read-only, modified implicitly by control flow instructions.
-   `R0` - `R15`: Additional general-purpose registers.

**Floating-Point Registers:**
-   `FPR0` - `FPR15`: Dedicated 64-bit floating-point registers. See Floating-Point Support.

**Flags Register:**
-   Contains status flags like ZF, SF, CF, OF. See Flags Register section.


## Flags Register

MicroASM maintains a set of processor flags that reflect the status of the most recent arithmetic or logical operation. These flags are used primarily by conditional jump instructions. The main flags are:

-   **ZF (Zero Flag):** Set if the result of an operation is zero; cleared otherwise.
-   **SF (Sign Flag):** Set if the result of an operation is negative (i.e., the most significant bit is 1); cleared otherwise.
-   **CF (Carry Flag):** Set if an arithmetic operation resulted in a carry out of (for addition) or a borrow into (for subtraction) the most significant bit. Also used by shift/rotate instructions.
-   **OF (Overflow Flag):** Set if the signed result of an arithmetic operation is too large or too small to fit in the destination operand (e.g., adding two large positive numbers results in a negative number).

*(Note: Not all instructions affect all flags. See individual instruction descriptions.)*



## Basic Instructions

### MOV (Move)

```
MOV dest, src
```

Copies a value from the source to the destination register.
Example: `MOV R1 R2` - Copies value from R2 to R1



### MOVZX (Move with Zero Extend)

```
MOVZX dest_reg, src
```

Copies the value from the source (`src`, typically a smaller state variable like `<BYTE>`, `<WORD>`, `<DWORD>`) to the destination register (`dest_reg`, typically `<QWORD>`). The upper bits of the destination register are filled with zeros.

-   `dest_reg`: Must be a register (e.g., RAX, R1).
-   `src`: Can be a register or a `STATE` variable of type `<BYTE>`, `<WORD>`, or `<DWORD>`.

Example: `STATE my_byte <BYTE> 200` ; `MOVZX RAX, my_byte` - RAX will hold the 64-bit value 200 (0x00...00C8).

### MOVSX (Move with Sign Extend)

```
MOVSX dest_reg, src
```

Copies the value from the source (`src`, typically a smaller state variable like `<BYTE>`, `<WORD>`, `<DWORD>`) to the destination register (`dest_reg`, typically `<QWORD>`). The upper bits of the destination register are filled by replicating the sign bit (the most significant bit) of the source value.

-   `dest_reg`: Must be a register (e.g., RAX, R1).
-   `src`: Can be a register or a `STATE` variable of type `<BYTE>`, `<WORD>`, or `<DWORD>`.

Example: `STATE my_byte <BYTE> 200` ; (Binary 11001000, sign bit is 1)
`MOVSX RAX, my_byte` - RAX will hold the 64-bit value -56 (0xFF...FFC8), as the sign bit (1) is extended.

Example: `STATE my_sbyte <BYTE> 100` ; (Binary 01100100, sign bit is 0)
`MOVSX RBX, my_sbyte` - RBX will hold the 64-bit value 100 (0x00...0064), as the sign bit (0) is extended.


### ADD (Addition)

```
ADD dest src
```

Adds the source value to the destination register/memory and stores the result in the destination.
**Flags Affected:** ZF, SF, CF, OF
Example: `ADD R1 R2` - R1 = R1 + R2

### SUB (Subtraction)

```
SUB dest src
```

Subtracts the source value from the destination register/memory and stores the result in the destination.
**Flags Affected:** ZF, SF, CF, OF
Example: `SUB R1 R2` - R1 = R1 - R2

### MUL (Multiplication)

```
MUL dest src
```

Multiplies the destination register/memory (`dest`) by the source value (`src`) and stores the 64-bit result in the destination. Assumes unsigned multiplication for flag setting.
**Flags Affected:**
-   `CF`, `OF`: Set if the theoretical 128-bit result does not fit into the 64-bit destination (i.e., the upper 64 bits are non-zero); cleared otherwise.
-   `ZF`, `SF`: Set based on the 64-bit result stored in `dest`. (SF reflects the MSB of the 64-bit result).
Example: `MUL R1 R2` - R1 = R1 * R2


### DIV (Division)

```
DIV dest src
```

Divides the destination register/memory (`dest`) by the source value (`src`) and stores the unsigned 64-bit quotient in the destination. The remainder is discarded.
**Flags Affected:** None.
**Exceptions:** A division by zero **must** trigger a runtime exception/fault.
Example: `DIV R1 R2` - R1 = R1 / R2 (unsigned quotient)


### INC (Increment)

```
INC dest
```

Increments the value in the destination register/memory by 1.
**Flags Affected:** ZF, SF, OF (Note: INC typically does not affect CF)
Example: `INC R1` - R1 = R1 + 1


### DEC (Decrement)

```
DEC dest
```

Decrements the value in the destination register/memory by 1.
**Flags Affected:** ZF, SF, OF (Note: DEC typically does not affect CF)
Example: `DEC R1` - R1 = R1 - 1


## Flow Control

### JMP (Jump)

```
JMP label
```

Unconditionally jumps to the specified label.
Example: `JMP #loop` - Jumps to label 'loop'


### CMP (Compare)

```
CMP dest, src
```

Compares two values (`dest` - `src`) and sets flags as if a `SUB` operation occurred, but discards the result.
**Flags Affected:** ZF, SF, CF, OF
Example: `CMP R1 R2` - Sets flags based on R1 - R2



## Conditional Jumps

These instructions jump to the specified `label` if the condition, based on the **integer flags** (ZF, SF, CF, OF) set by the last instruction that affects them (e.g., `CMP`, `ADD`, `SUB`, `AND`, `OR`, `XOR`, `INC`, `DEC`), is met.

*(Note: The two-label format `JE label_true label_false` is non-standard for assembly; conditional jumps typically have one target label. We will use the standard single-label format.)*

**Jumps based on Zero Flag (ZF):**
-   `JZ label` (Jump if Zero): Jumps if `ZF` is set (result was zero).
-   `JNZ label` (Jump if Not Zero): Jumps if `ZF` is clear (result was not zero).
-   `JE label` (Jump if Equal): Alias for `JZ`. Jumps if `ZF` is set (used after `CMP`).
-   `JNE label` (Jump if Not Equal): Alias for `JNZ`. Jumps if `ZF` is clear (used after `CMP`).

**Jumps based on Sign Flag (SF):**
-   `JS label` (Jump if Sign): Jumps if `SF` is set (result was negative).
-   `JNS label` (Jump if Not Sign): Jumps if `SF` is clear (result was non-negative).

**Jumps based on Carry Flag (CF):**
-   `JC label` (Jump if Carry): Jumps if `CF` is set.
-   `JNC label` (Jump if Not Carry): Jumps if `CF` is clear.
-   `JB label` (Jump if Below): Alias for `JC`. Jumps if `CF` is set (used after `CMP` for unsigned comparison: dest < src).
-   `JNAE label` (Jump if Not Above or Equal): Alias for `JC`.
-   `JAE label` (Jump if Above or Equal): Alias for `JNC`. Jumps if `CF` is clear (used after `CMP` for unsigned comparison: dest >= src).
-   `JNB label` (Jump if Not Below): Alias for `JNC`.

**Jumps based on Overflow Flag (OF):**
-   `JO label` (Jump if Overflow): Jumps if `OF` is set.
-   `JNO label` (Jump if Not Overflow): Jumps if `OF` is clear.

**Jumps based on combinations (Signed Comparisons after `CMP dest, src`):**
-   `JG label` (Jump if Greater): Jumps if `ZF` is clear AND `SF == OF` (dest > src, signed).
-   `JNLE label` (Jump if Not Less or Equal): Alias for `JG`.
-   `JL label` (Jump if Less): Jumps if `SF != OF` (dest < src, signed).
-   `JNGE label` (Jump if Not Greater or Equal): Alias for `JL`.
-   `JGE label` (Jump if Greater or Equal): Jumps if `SF == OF` (dest >= src, signed).
-   `JNL label` (Jump if Not Less): Alias for `JGE`.
-   `JLE label` (Jump if Less or Equal): Jumps if `ZF` is set OR `SF != OF` (dest <= src, signed).
-   `JNG label` (Jump if Not Greater): Alias for `JLE`.

**Example:**
```assembly
CMP R1 10
JL #less_than_ten  ; Jump if R1 < 10 (signed)
CMP R2 0
JZ #is_zero        ; Jump if R2 == 0
ADD R3 R4
JC #carry_occurred ; Jump if addition caused a carry
```

### CALL (Call Subroutine)

```
CALL #label
```

Calls a subroutine located at the specified `label`.
**Behavior:**
1.  Pushes the address of the instruction immediately following the `CALL` onto the stack.
2.  Jumps execution to the specified `label`.
Use the `RET` instruction within the subroutine to return to the pushed address.
Example: `CALL #my_function`

## Stack Operations

### PUSH

```
PUSH src
```

Pushes a value onto the stack.
Example: `PUSH R1`

### POP

```
POP dest
```

Pops a value from the stack into the destination register.
Example: `POP R1`

## I/O Operations
### IN

```
IN dest_addr
```
Reads a line of text from Standard Input (stdin, file descriptor 0) and stores it as a null-terminated string into memory starting at the calculated address `dest_addr`.
-   `dest_addr`: Specifies the destination memory address (label, `$register`, or discouraged `$number`). Must point to a buffer large enough to hold the input line plus newline and null terminator.
**Behavior:** Reads characters until a newline (`\n`) or end-of-file. Stores the characters read, followed by the newline character (`\n`), followed by a null terminator (`\0`) into memory starting at `dest_addr`. If the buffer is too small to hold the input, newline, and null terminator, a runtime error should occur.

### OUT

```
OUT port value
```

Outputs a value or string (prefixed with $) to 1. stdout, 2. stderr with a newline.
Example: `OUT 2 R1` or `OUT 1 $500`

### COUT

```
COUT port value
```
Outputs a single character corresponding to the numeric `value` to the specified `port`.
-   `port`: Same as `OUT` (immediate or register).
-   `value`: The numeric character code (e.g., ASCII/Unicode) to print.
    -   Can be an immediate number (e.g., `65`).
    -   Can be a register containing the character code (e.g., `R1`).
    -   Can be a memory reference (`$register` or discouraged `$number`) from which a single byte value is read.


## Program Control

### HLT (Halt)

```
HLT
```

Stops program execution and returns to the operating system.

### EXIT

```
EXIT code
```

Exits the program with the specified return code.
Example: `EXIT R1`

## Command Line Arguments

### ARGC

```
ARGC dest
```

Gets the count of command line arguments into the destination register.
Example: `ARGC R1`

### GETARG

```
GETARG dest index
```

Gets the command line argument at the specified index into the location passed in the seccond register.
Example: `GETARG R1 R2`

in jmasm and other programing languages, the index is 0-based and comes after the launched program name, unlike how csharp wants -- --<argument> or -<argument> or <argument> to be the passing to the program



## Labels

### LBL (Label)

```
LBL name
```

Defines a label that can be jumped to.
Example: `LBL loop`

## Heap
The masm heap allows for allocating and freeing memory with `MALLOC (ptr) (size)` and `FREE (success) (ptr)`

### MALLOC (ptr) (size)
The malloc instruction allows for allocating (size) bytes in memory

size - Number of bytes
ptr - where the data is

ptr can also be a error code (err codes are negative)
The error codes are as following

Name                  | value | description
----------------------|-------|----------------
HEAP_ERR_OUT_OF_SPACE | -3    | Not enough space to allocate size bytes
HEAP_ERR_INVALID_ARG  | -4    | Size = 0

### FREE (success) (ptr)
The free instruction allows for freeing memory that has been previously allocated with MALLOC

success - Error code or zero if success
ptr - pointer to data that should be freed

Name                   | value | description
-----------------------|-------|----------------
HEAP_ERR_ALREADY_FREE  | -1    | Tried to free a already free chunk
HEAP_ERR_NOT_ALLOCATED | -2    | Tried to free data that has never been allocated with MALLOC

Note: Because of defragmentation stuff calling free twice on the same malloced pointer can have either error.



## Static Data Definition Directives

MicroASM offers multiple ways to define static data in the program's memory image.

### Standard Method (Recommended)

This method uses labels to mark the start of data defined by directives. The assembler manages placement, ensuring safety and portability. This is the recommended approach for most use cases.

**Syntax:**
```assembly
label_name: DIRECTIVE value1, value2, ...
```

-   **`DB` (Define Byte):** Defines 8-bit values (numbers or strings).
-   **`DW` (Define Word):** Defines 16-bit values.
-   **`DD` (Define Dword):** Defines 32-bit values.
-   **`DQ` (Define Qword):** Defines 64-bit values (can store `#label` addresses).
-   **`DF` (Define Float):** Defines 32-bit floats.
-   **`DDbl` (Define Double):** Defines 64-bit doubles.
-   **`RESB`, `RESW`, `RESD`, `RESQ`, `RESF`, `RESDbl`:** Reserve uninitialized space.


### Direct Memory Initialization Method

This method allows writing data directly to a specified numeric memory address at assembly/load time. It offers precise control but requires careful manual memory management and carries significant risks.

-   **`DB $address "string"`:** Writes the bytes of the `string` literal directly into memory starting at the absolute numeric `address`.
    -   **Behavior Note:** Standard MicroASM runtimes (like jmasm) typically append a null terminator (`\0`) after writing the string's bytes when using this directive.
    -   **⚠️ EXTREME WARNING:** This bypasses the assembler's memory management. It is **unsafe**, **not portable**, and highly likely to cause conflicts or crashes if the address is invalid or overlaps with other data/code. Use only when the exact memory layout is known and the risks are fully understood. Prefer the standard `label: DB` method for safety.
    ```assembly
    ; Direct Initialization Example: Writes "Hi\0" starting at address 5000
    DB $5000 "Hi"
    ```
-   **(Other Directives like `DW $address value`, `DD $address value` etc. might exist in specific runtimes but are generally discouraged for the same reasons).**


### Initialized Data

These directives place specific values into memory at the current assembly location.

-   **`DB` (Define Byte):** Defines one or more 8-bit values. Accepts integers (-128 to 255) or string literals. Strings are expanded into sequences of bytes (ASCII/UTF-8 encoding assumed).
    ```assembly
    byte_array: DB 10, 20, 0xFF
    message:    DB "Hello, MicroASM!", 0x0A, 0 ; String, newline, null terminator
    ```
-   **`DW` (Define Word):** Defines one or more 16-bit integer values.
    ```assembly
    word_array: DW 1000, -500, 0xABCD
    ```
-   **`DD` (Define Dword):** Defines one or more 32-bit integer values.
    ```assembly
    dword_array: DD 12345678, -1
    ```
-   **`DQ` (Define Qword):** Defines one or more 64-bit integer values. Can also accept label names (prefixed with `#`) to store their addresses.
    ```assembly
    qword_array: DQ 123456789012345, -100
    address_table: DQ #message, #byte_array, #main ; Store addresses
    ```
-   **`DF` (Define Float):** Defines one or more 32-bit single-precision floating-point values.
    ```assembly
    float_constants: DF 1.0, -3.14, 1.5e-6
    ```
-   **`DDbl` (Define Double):** Defines one or more 64-bit double-precision floating-point values.
    ```assembly
    double_constants: DDbl 3.1415926535, -0.001, 2.71828
    ```

### Uninitialized Data (Reserve Space)

These directives reserve a block of memory without initializing it. The content is undefined until written to at runtime. This is often used for buffers or large arrays.

-   **`RESB count` (Reserve Byte):** Reserves `count` bytes.
-   **`RESW count` (Reserve Word):** Reserves `count` * 2 bytes.
-   **`RESD count` (Reserve Dword):** Reserves `count` * 4 bytes.
-   **`RESQ count` (Reserve Qword):** Reserves `count` * 8 bytes.
-   **`RESF count` (Reserve Float):** Reserves `count` * 4 bytes.
-   **`RESDbl count` (Reserve Double):** Reserves `count` * 8 bytes.

```assembly
input_buffer: RESB 256      ; Reserve 256 bytes for input
results_array: RESD 100     ; Reserve space for 100 Dwords (400 bytes)
matrix_data: RESDbl 16      ; Reserve space for 16 Doubles (128 bytes)
```

**Usage Example:**

```assembly
LBL main
    MOV RDI message     ; Load address of message into RDI
    MOV RSI input_buffer ; Load address of buffer into RSI
    MOV RDX 256         ; Max size
    ; ... call read syscall or MNI function ...
    MOV RAX [qword_array + 8] ; Access second QWORD in qword_array
    HLT

; --- Static Data ---
message:    DB "Enter input: ", 0
qword_array: DQ 1, 2, 3
input_buffer: RESB 256
```



## Label Scoping

By default, all labels defined using `LBL name` are placed in the global scope and can be referenced from anywhere in the program using `#name`.

To create labels that are local to a specific block of code, you can define a named scope.

### Defining Scopes

A named scope is created using a label definition followed by the `SCOPE` keyword and is terminated by the `ENDSCOPE` keyword.

```
LBL scope_name SCOPE
    ; Code and local labels defined here...
    LBL local_label_1
        ; ...
    LBL local_label_2 SCOPE ; Nested scope
        ; ...
        LBL nested_local
            ; ...
    ENDSCOPE ; End of local_label_2 scope

    ; Can jump to local_label_1 directly
    JMP #local_label_1

    ; Can jump to nested_local using qualified name
    JMP #local_label_2.nested_local

ENDSCOPE ; End of scope_name scope
```

### Label Resolution

When an instruction references a label (e.g., `JMP #target`), the assembler/interpreter resolves the name as follows:

1.  It searches for `target` within the current innermost local scope.
2.  If not found, it searches enclosing scopes outwards, one level at a time.
3.  If still not found after checking all enclosing scopes, it searches the global scope.
4.  If the label is not found in any scope, it's an error.

### Duplicate Labels

Defining two labels with the exact same name within the *same* scope (e.g., two global labels named `loop`, or two labels named `inner` directly within the same `myscope SCOPE ... ENDSCOPE` block) is an error.

### Accessing Labels

-   **Within the same or an inner scope:** You can directly reference a label using `#label_name`. The resolution rules will find the correct label (the innermost one available).
-   **From an outer scope:** To reference a label inside a specific scope from outside that scope, you must use a qualified name, separating scope names and the final label name with a dot (`.`). Example: `CALL #outer_scope.inner_scope.target_label`. Direct access to local labels from outside their scope without qualification is not allowed.

### Example

```assembly
LBL global_start
    MOV R1 0
    CALL #utilities.increment_r1 ; Call label inside utilities scope
    CALL #utilities.print_r1    ; Call another label in the same scope
    HLT

LBL utilities SCOPE
    ; This label is local to the 'utilities' scope
    LBL increment_r1
        INC R1
        RET

    ; This label is also local to 'utilities'
    LBL print_r1
        OUT 1 R1
        RET

    ; LBL increment_r1 ; ERROR: Duplicate label 'increment_r1' in scope 'utilities'

ENDSCOPE ; End of utilities scope

; LBL utilities ; ERROR: Duplicate global label 'utilities'

; CALL #increment_r1 ; ERROR: 'increment_r1' not found in global scope
```


## Assembler Macros

Macros provide a way to define reusable blocks of code with parameter substitution. They are processed by the assembler before final code generation.

### Defining Macros

Macros are defined using the `MACRO` and `ENDMACRO` directives.

```
MACRO macro_name [arg1, arg2, ...]
    ; Macro body - sequence of instructions and directives
    ; Use arg1, arg2, etc. as placeholders
ENDMACRO
```

-   `MACRO`: Starts the macro definition.
-   `macro_name`: The name used to invoke the macro. Follows standard identifier rules, case-insensitive like instructions.
-   `[arg1, arg2, ...]`: (Optional) A list of formal parameter names separated by spaces or commas (consistency needed). These names will be replaced by the actual arguments provided during invocation.
-   `ENDMACRO`: Ends the macro definition.

### Invoking Macros

Macros are invoked by using their name as if it were an instruction, followed by the actual arguments.

```
macro_name [param1, param2, ...]
```

-   The assembler replaces the invocation line with the body of the macro.
-   Each occurrence of a formal parameter name (e.g., `arg1`) within the macro body is textually replaced by the corresponding actual argument (e.g., `param1`).
-   The number of arguments provided during invocation must match the number of parameters defined in the macro.

### Local Labels within Macros

If a macro defines labels, invoking the macro multiple times would lead to duplicate label definition errors. To avoid this, labels defined inside a macro body should be made local to that specific macro expansion by prefixing them with `@@`. The assembler automatically replaces `@@label_name` with a unique label name for each expansion (e.g., `..@macro_name@1@label_name`, `..@macro_name@2@label_name`).

```assembly
MACRO increment_if_less value, limit, target_reg
    CMP target_reg, limit
    JGE @@skip_inc      ; Jump if target_reg >= limit
    INC target_reg
@@skip_inc:             ; This label becomes unique per expansion
    ; ... maybe more code ...
ENDMACRO
```

### Example

**Definition:**

```assembly
; Macro to push two registers onto the stack
MACRO push_pair reg1, reg2
    PUSH reg1
    PUSH reg2
ENDMACRO

; Macro to add a value to a register and jump if zero
MACRO add_and_jump_if_zero register, value_to_add, jump_label
    ADD register, value_to_add
    JZ jump_label ; Jump if result is zero
ENDMACRO

; Macro with local label
MACRO delay_loop count_reg
    MOV count_reg 1000 ; Example initial count
@@loop:
    DEC count_reg
    JNZ @@loop         ; Jump back if not zero
ENDMACRO
```

**Invocation:**

```assembly
LBL main
    push_pair R1 R2          ; Expands to PUSH R1, PUSH R2

    MOV R5 10
    add_and_jump_if_zero R5 -10 #handle_zero ; Expands to ADD R5 -10, JZ #handle_zero

    delay_loop R10           ; Expands loop with unique label for @@loop

    ; ... more code ...

#handle_zero:
    ; ... code ...
    HLT
```

### Notes

-   Macro expansion is typically textual substitution before full parsing.
-   Macros cannot be defined inside other macros (usually).
-   Recursive macro expansion might be disallowed or limited by the assembler to prevent infinite loops.
-   Arguments are substituted literally. If an argument contains spaces or commas, it might need quoting depending on the assembler's parsing rules (TBD/Implementation defined).



## State Definition

Allows for the declaration of named, typed variables, managed by the assembler/runtime.

### STATE

```
STATE name <Type> [initial_value]
```

Declares a named state variable with a specific data type, reserving storage space in memory.

-   `name`: The identifier for the state variable. Case-sensitive. Must not conflict with labels or other state variables within the same scope.
-   `<Type>`: (Mandatory) Specifies the data type and size. Supported types:
    -   `<BYTE>`: 8-bit integer.
    -   `<WORD>`: 16-bit integer.
    -   `<DWORD>`: 32-bit integer.
    -   `<QWORD>`: 64-bit integer.
    -   `<FLOAT>`: 32-bit single-precision floating-point (IEEE 754).
    -   `<DOUBLE>`: 64-bit double-precision floating-point (IEEE 754).
    -   `<PTR>`: Pointer/address (architecture-dependent size, typically 64-bit).
-   `[initial_value]`: (Optional) An immediate value compatible with `<Type>` used to initialize the variable's memory location at load time. Defaults to 0 if omitted.

**Usage:**

Once declared, the `name` represents the memory location of the variable. Instructions interacting with the state variable operate according to its declared `<Type>`.

```assembly
STATE counter <QWORD> 0       ; Global 64-bit counter, initialized to 0
STATE status_flags <BYTE> 1   ; Global 8-bit flags, initialized to 1

LBL main SCOPE
    STATE loop_index <DWORD> 5 ; Local 32-bit loop_index for main scope

    MOV counter 10          ; OK (assuming immediate 10 fits QWORD)
    MOV status_flags 255    ; OK (255 fits BYTE)
    ; MOV status_flags 300  ; ERROR: Initializer/value too large for <BYTE>

    INC counter             ; Performs 64-bit increment
    INC status_flags        ; Performs 8-bit increment

    ; Type compatibility checks may apply depending on instruction
    ; MOV RAX status_flags  ; Might require explicit extension or be handled by MOV
    ; ADD counter status_flags ; Likely an ERROR due to type/size mismatch

ENDSCOPE ; End of main scope
```

**Scope:**

`STATE` declarations follow the label scoping rules. Accessing local state from outside its scope requires qualified names (e.g., `main.loop_index`).

**Storage & Alignment:**

The assembler allocates memory for each state variable according to its `<Type>`, potentially applying alignment rules suitable for the target architecture.


## Floating-Point Support

MicroASM includes support for single-precision (`<FLOAT>`) and double-precision (`<DOUBLE>`) floating-point operations conforming to the IEEE 754 standard.

### Floating-Point Registers

A dedicated set of floating-point registers are available:

-   `FPR0` - `FPR15`: 16 general-purpose floating-point registers. These registers hold `<DOUBLE>` (64-bit) values. `<FLOAT>` values are typically promoted to `<DOUBLE>` when loaded into these registers, or specific instructions handle the conversion.

### Floating-Point State Variables

Use the `STATE` instruction with `<FLOAT>` or `<DOUBLE>` types to declare floating-point variables in memory.

```assembly
STATE pi <DOUBLE> 3.1415926535
STATE tolerance <FLOAT> 0.001
STATE fp_results[10] <DOUBLE> ; Example: Reserve space for 10 doubles (requires array support or manual address calculation)
```

### Floating-Point Instructions

Floating-point instructions typically operate on FPR registers or memory locations defined with `<FLOAT>` or `<DOUBLE>` types.

#### FMOV (Floating-Point Move)

```
FMOV dest, src
```

Copies a floating-point value.

-   `dest`: FPR register or memory location (`<FLOAT>`/`<DOUBLE>`).
-   `src`: FPR register, memory location (`<FLOAT>`/`<DOUBLE>`), or immediate floating-point value.

**Behavior:**
-   `FPR <- FPR`: Copies value between FPR registers.
-   `FPR <- Mem`: Loads `<FLOAT>` or `<DOUBLE>` from memory into FPR. `<FLOAT>` is promoted to `<DOUBLE>`.
-   `Mem <- FPR`: Stores `<DOUBLE>` from FPR to memory. If Mem is `<FLOAT>`, precision may be lost (truncation/rounding based on implementation).
-   `FPR <- Immediate`: Loads an immediate float/double value into FPR.

**Examples:**
```assembly
FMOV FPR1 FPR0             ; FPR1 = FPR0
FMOV FPR2 pi               ; FPR2 = value of state variable 'pi'
FMOV tolerance FPR3        ; tolerance = value from FPR3 (potential precision loss)
FMOV FPR4 1.618            ; FPR4 = 1.618
FMOV FPR5 [R1 + R2 * 8]    ; Load DOUBLE from calculated address into FPR5
MOV [R1 + 8] FPR6          ; Store FPR6 into memory address R1+8
```





#### FADD, FSUB, FMUL, FDIV (Floating-Point Arithmetic)

```
FADD dest, src
FSUB dest, src
FMUL dest, src
FDIV dest, src
```

Performs double-precision arithmetic.

-   `dest`: Destination FPR register. Result is stored here.
-   `src`: Source FPR register, memory location (`<FLOAT>`/`<DOUBLE>`), or immediate float/double. If source is `<FLOAT>`, it's promoted to `<DOUBLE>` before the operation.

**Examples:**
```assembly
FADD FPR1 FPR0             ; FPR1 = FPR1 + FPR0
FSUB FPR2 pi               ; FPR2 = FPR2 - pi
FMUL FPR3 2.0              ; FPR3 = FPR3 * 2.0
FDIV FPR4 tolerance        ; FPR4 = FPR4 / tolerance (tolerance promoted to double)
```

#### FCMP (Floating-Point Compare)

```
FCMP reg1, reg2_or_mem_or_imm
```

Compares two floating-point values (`reg1` vs `reg2_or_mem_or_imm`) and sets the dedicated floating-point condition flags.

-   `reg1`: An FPR register.
-   `reg2_or_mem_or_imm`: An FPR register, memory location (`<FLOAT>`/`<DOUBLE>`), or immediate float/double.

**Behavior:** Sets the following floating-point condition flags based on the comparison result:
    -   `FE` (Equal): Set if `reg1 == reg2_or_mem_or_imm`.
    -   `FLT` (Less Than): Set if `reg1 < reg2_or_mem_or_imm`.
    -   `FGT` (Greater Than): Set if `reg1 > reg2_or_mem_or_imm`.
    -   `FUO` (Unordered): Set if either `reg1` or `reg2_or_mem_or_imm` is NaN (Not a Number).
    *(Note: These flags are independent of the main integer flags like ZF, SF etc.)*

**Example:**
```assembly
FCMP FPR1 FPR2
; Follow with conditional jumps like FJE, FJLT, FJUO etc.

FCMP FPR3 0.0
; Sets FE if FPR3 is 0.0, FLT if negative, FGT if positive (assuming not NaN)
```

#### Conversion Instructions

Instructions to convert between integer and floating-point types, and between float and double.

```
CVTSI2SD dest_fpr, src_int_reg_or_mem   ; Convert Signed Integer to Double
CVTUI2SD dest_fpr, src_uint_reg_or_mem  ; Convert Unsigned Integer to Double
CVTSD2SI dest_int_reg, src_fpr          ; Convert Double to Signed Integer (truncates)
CVTSD2UI dest_uint_reg, src_fpr         ; Convert Double to Unsigned Integer (truncates)

CVTSS2SD dest_fpr, src_float_reg_or_mem ; Convert Float to Double
CVTSD2SS dest_float_mem, src_fpr        ; Convert Double to Float (precision loss)
```
*(Note: Naming convention examples: CVT=Convert, S=Signed, U=Unsigned, I=Integer, SD=Double, SS=Single/Float. Needs refinement based on desired operand types)*

**Examples:**
```assembly
STATE counter <QWORD> 100
STATE result <FLOAT>
CVTSI2SD FPR0 counter     ; Convert QWORD in counter to DOUBLE in FPR0
FMUL FPR0 1.5             ; Multiply by 1.5
CVTSD2SS result FPR0      ; Convert result back to FLOAT in 'result' state
```

### Floating-Point Conditional Jumps




Define conditional jump instructions that branch based on the result of `FCMP`. Examples:

-   `FJE label`: Jump if Equal
-   `FJNE label`: Jump if Not Equal
-   `FJLT label`: Jump if Less Than
-   `FJLE label`: Jump if Less Than or Equal
-   `FJGT label`: Jump if Greater Than
-   `FJGE label`: Jump if Greater Than or Equal
-   `FJUO label`: Jump if Unordered (e.g., comparison involving NaN)

### MNI for Complex Functions

Standard mathematical functions like `sin`, `cos`, `sqrt`, `pow`, `log`, etc., are typically provided via MNI calls, operating on FPR registers or memory locations.

```assembly
MNI Math.sqrt FPR1 FPR0   ; FPR0 = sqrt(FPR1)
MNI Math.sin FPR2 FPR2    ; FPR2 = sin(FPR2)
```


## Type Conversions

MicroASM uses explicit typing via the `STATE` instruction. Moving data between locations (registers, state variables) generally requires the source and destination to be compatible.

### Simple Size Extension (Integer Types)

When moving smaller integer types (e.g., `<BYTE>`, `<WORD>`, `<DWORD>`) into larger integer registers (e.g., `<QWORD>` registers like RAX), specific instructions control how the upper bits are filled:

-   `MOVZX`: Move with Zero Extend. Fills the upper bits of the destination with zeros.
-   `MOVSX`: Move with Sign Extend. Fills the upper bits by copying the sign bit (most significant bit) of the source.

See the instruction definitions below for details. Direct `MOV` between integer state variables of different sizes is generally disallowed; use `MOVZX` or `MOVSX` to load into a register first.

### Complex Conversions

Conversions between fundamentally different types (e.g., integer to string, integer to float, pointer to integer) or operations requiring significant computation or memory allocation are handled explicitly, typically through **MNI (Micro Native Interface)** functions.

**Example:** Converting a `<QWORD>` state variable to a string for output.

```assembly
STATE my_number <QWORD> 12345
STATE string_buffer <PTR>

LBL main
    ; Allocate memory for the string representation
    MNI Memory.allocate 64 R1 ; Allocate 64 bytes, store pointer in R1
    MOV string_buffer R1      ; Store buffer pointer in our state variable

    ; Convert the number to a string using an MNI function
    ; Assumes MNI function takes dest buffer ptr (R1), src value (my_number)
    MNI Convert.qwordToString R1 my_number

    ; Now string_buffer points to the null-terminated string in the allocated memory
    OUT 1 string_buffer ; Output the string

    ; Free the allocated buffer when done
    MNI Memory.free string_buffer
    HLT
```

## Dynamic Memory Management (via MNI)

MicroASM itself does not include core instructions for dynamic memory allocation (heap management). Instead, it relies on the runtime environment providing this functionality through the **MNI (Micro Native Interface)**.

The specific names and exact parameters might vary slightly depending on the runtime implementation, but the following represent the expected standard memory management functions:

### MNI `Memory.allocate`

```
MNI Memory.allocate size_arg result_ptr_reg
```

Requests a block of memory from the runtime's heap.

-   `size_arg`: The number of bytes to allocate. Can be an immediate value or a register containing the size (e.g., `<QWORD>`).
-   `result_ptr_reg`: The register where the runtime will place the pointer (`<PTR>`) to the beginning of the allocated block.

**Behavior:**

-   On success, `result_ptr_reg` will contain the base address of a contiguous block of memory of at least `size_arg` bytes. The contents of the allocated memory are undefined.
-   On failure (e.g., insufficient memory), `result_ptr_reg` will be set to 0 (null pointer). The runtime might also set an error flag if applicable.

**Example:**

```assembly
STATE buffer_ptr <PTR>
; Request 1024 bytes
MNI Memory.allocate 1024 R1
MOV buffer_ptr R1 ; Store the resulting pointer

; Check for allocation failure
CMP R1 0
JE #allocation_failed
; ... use buffer_ptr ...
#allocation_failed:
    ; Handle error
```

### MNI `Memory.free`

```
MNI Memory.free ptr_arg
```

Releases a previously allocated block of memory back to the runtime's heap, making it available for future allocations.

-   `ptr_arg`: The pointer (`<PTR>`) to the beginning of the memory block to free. This must be a pointer previously returned by a successful call to `Memory.allocate`. Can be an immediate value (if the address is known) or a register containing the pointer.

**Behavior:**

-   Frees the memory block associated with `ptr_arg`.
-   Attempting to free a null pointer (0) should be safe and do nothing.
-   Attempting to free an invalid pointer (not obtained from `Memory.allocate` or already freed) results in undefined behavior (likely a runtime crash or memory corruption).
-   Accessing memory after it has been freed results in undefined behavior.

**Example:**

```assembly
; Assuming buffer_ptr holds a valid pointer from Memory.allocate
MNI Memory.free buffer_ptr
MOV buffer_ptr 0 ; Good practice to nullify pointer after freeing
```

*(Note: A `Memory.reallocate` function might also be provided by the MNI implementation for resizing existing allocations.)*

This approach clearly defines the expected interface for memory management without adding complex instructions to the core language. The actual heap implementation resides within the specific MicroASM runtime or OS layer.


## Assembler Directives

Directives provide instructions to the assembler itself, rather than generating machine code directly.

### #include (Include File)

```
#include "path.to.file.masm"
#include <standard.library.file.masm>
```

Includes the contents of another source file at the location of the directive. This allows for code modularity and the creation of libraries. The dots (`.`) are used as separators in the path definition and a .masm extention is automaticly applied to the end if no dots are found.
-   `"path.to.file"`: Uses double quotes for user-defined include files. The assembler typically searches for these files relative to the directory of the current source file first, and then potentially in other user-specified include directories, translating the dots into appropriate directory separators for the host system.
-   `<standard.library.file>`: Uses angle brackets for standard library include files. The assembler typically searches for these files only in predefined system/standard library include paths, translating the dots into appropriate directory separators.

**Search Paths:** The exact search order (e.g., current directory, `-I` command-line paths, standard library paths) and how the dot-separated path is mapped to the host filesystem is defined by the specific assembler implementation.

**Example:**

```assembly
; Include a user-defined file (e.g., my_macros.masm in the same directory)
#include "my_macros.masm"

; Include a standard library file (e.g., stdio.io in a standard library path)
#include <stdio.io>

LBL main
    ; Code that uses definitions from included files...
    print_string message ; Assumes print_string macro is in stdio.io
    HLT

STATE message <PTR>
LBL setup
    DB $1000 "Hello from included files!\n"
    MOV message 1000
```




# MNI

Micro assembly can interact with native code to allow users to make native system calls ether through the JNI or using C in python through `python.h`

examples of MNI can be found inside [mni-functions](mni-instructions.md)

# Extended MicroASM Instruction Set Documentation

This document details additional instructions for the MicroASM language.

## Bitwise Operations

### AND (Bitwise AND)

```
AND dest src
```

Performs a bitwise AND operation between the source and destination, storing the result in the destination.
**Flags Affected:** ZF, SF (CF and OF are cleared)
Example: `AND R1 R2` - R1 = R1 & R2

### OR (Bitwise OR)

```
OR dest src
```

Performs a bitwise OR operation between the source and destination, storing the result in the destination.
**Flags Affected:** ZF, SF (CF and OF are cleared)
Example: `OR R1 R2` - R1 = R1 | R2

### XOR (Bitwise XOR)

```
XOR dest src
```

Performs a bitwise XOR operation between the source and destination, storing the result in the destination.
**Flags Affected:** ZF, SF (CF and OF are cleared)
Example: `XOR R1 R2` - R1 = R1 ^ R2

### NOT (Bitwise NOT)

```
NOT dest
```

Inverts all bits in the destination register.
**Flags Affected:** None
Example: `NOT R1` - R1 = ~R1

### SHL (Shift Left)

```
SHL dest count
```

Shifts the bits in the destination register/memory left by the specified count. The last bit shifted out is placed in CF.
**Flags Affected:** CF, ZF, SF (OF behavior depends on count)
Example: `SHL R1 R2` - R1 = R1 << R2

### SHR (Shift Right)

```
SHR dest count
```

Shifts the bits in the destination register/memory right by the specified count (logical shift: fills with 0). The last bit shifted out is placed in CF.
**Flags Affected:** CF, ZF, SF (OF behavior depends on count)
Example: `SHR R1 R2` - R1 = R1 >> R2


### SAR (Shift Arithmetic Right)

```
SAR dest count
```

Shifts the bits in the destination register/memory right by the specified count (arithmetic shift: fills with sign bit). The last bit shifted out is placed in CF.
**Flags Affected:** CF, ZF, SF (OF behavior depends on count)
Example: `SAR R1 R2` - R1 = R1 >> R2 (arithmetic)


## Memory Addressing Extensions

### MOVADDR (Move from Address with Offset)

```
MOVADDR dest src offset
```

Copies a value from the memory address calculated as src+offset to the destination register.
Example: `MOVADDR R1 R2 R3` - R1 = Memory[R2 + R3]

### MOVTO (Move to Address with Offset)

```
MOVTO dest offset src
```

Copies a value from the source register to the memory address calculated as dest+offset.
Example: `MOVTO R1 R2 R3` - Memory[R1 + R2] = R3


### Direct Address (`$<number>`)

Using a `$` prefix followed by an immediate numeric value attempts to access that specific, hardcoded memory address.

```assembly
MOV R1 $1000         ; Attempts to read QWORD from address 1000 into R1
MOV $2000 R2         ; Attempts to write QWORD from R2 into address 2000
```

**⚠️ EXTREME WARNING: Use With Caution (or preferably, not at all!) ⚠️**

Directly accessing hardcoded memory addresses is **highly discouraged** and **extremely dangerous** in MicroASM (and most modern contexts).

*   **No Guarantees:** You have absolutely no guarantee that the address (`$1000`, `$2000`, etc.) is valid, mapped, writable, or won't conflict with program code, the stack, runtime data, or other essential memory regions.
*   **Portability Nightmare:** Addresses are specific to a particular runtime instance and memory layout. Code using direct addresses is **not portable**.
*   **Likely Crashes:** Incorrect usage will almost certainly lead to crashes, memory corruption, or completely unpredictable behavior. Think of it like juggling chainsaws blindfolded – maybe you can, but why would you?

**Prefer safer alternatives:** Use labels defined with data directives (`DB`, `DQ`, `RESB`, etc.), `STATE` variables, stack-relative addressing (`[RBP - offset]`), or pointers obtained from the runtime (`MNI Memory.allocate`, `SYSCALL mmap`).

Use `$address` only if you are absolutely certain you know the exact memory layout provided by a specific, non-portable runtime environment and understand the severe risks involved.

### Register Indirect (`$<register>`)

Using a `$` prefix followed by a register name accesses the memory address *stored within* that register. This is the **standard and safe** way to use pointers. The register should contain a valid memory address (e.g., from a label, `STATE <PTR>`, or memory allocation).

```assembly
STATE buffer_ptr <PTR>
LBL my_string DB "Data", 0

MOV R1 buffer_ptr    ; R1 holds address from allocator
MOV R2 my_string     ; R2 holds address of static string data
MOV R3 $R1           ; Read value from address in R1
MOV [$R2+1] 'A'      ; Write 'A' to address in R2 + 1 (modifies string)
```

## Stack Frame Management

### ENTER (Create Stack Frame)

```
ENTER framesize
```

Creates a standard stack frame for a procedure. Typically equivalent to:
1.  `PUSH RBP` (Save caller's frame pointer)
2.  `MOV RBP, RSP` (Set current frame pointer)
3.  `SUB RSP, framesize` (Allocate space for local variables)

-   `framesize`: An immediate value specifying the number of bytes to allocate for local variables on the stack. Must be non-negative.

Example: `ENTER 64` - Creates a stack frame, allocating 64 bytes for locals.

### LEAVE (Destroy Stack Frame)

```
LEAVE
```

Destroys the current stack frame created by `ENTER`, preparing for a `RET`. Typically equivalent to:
1.  `MOV RSP, RBP` (Deallocate local variables)
2.  `POP RBP` (Restore caller's frame pointer)

Example: `LEAVE`

**Function Prologue/Epilogue Example:**
```assembly
LBL my_function SCOPE
    ENTER 16      ; Setup stack frame, 16 bytes for locals
    ; ... function body, use [RBP - offset] for locals ...
    MOV R1 [RBP - 8] ; Access a local variable
    ; ...
    LEAVE         ; Restore stack
    RET           ; Return to caller
ENDSCOPE
```

## String/Memory Operations

### COPY (Memory Copy)

```
COPY dest src len
```

Copies len bytes from the source address to the destination address.
Example: `COPY R1 R2 R3` - Copies R3 bytes from address R2 to address R1

### FILL (Memory Fill)

```
FILL dest value len
```

Fills len bytes at the destination address with the specified value.
Example: `FILL R1 R2 R3` - Fills R3 bytes at address R1 with value R2


### CMP_MEM (Memory Compare)

```
CMP_MEM dest src len
```
Compares `len` bytes starting at `dest` address with `len` bytes starting at `src` address. Comparison is byte-by-byte unsigned.
**Flags Affected:**
-   `ZF`: Set if all `len` bytes are identical; cleared otherwise.
-   `CF`: Set if the byte in `src` is greater than the byte in `dest` at the first differing position; cleared otherwise (including if all bytes are equal).
-   `SF`, `OF`: Undefined or unaffected.
Example: `CMP_MEM R1 R2 R3` - Compares R3 bytes at addresses R1 and R2, sets ZF/CF.

## System Call Interface

MicroASM provides a low-level mechanism to directly request services from the underlying operating system or runtime environment using the `SYSCALL` instruction. This is distinct from MNI, which might wrap these calls or provide higher-level abstractions.

### SYSCALL Instruction

```
SYSCALL
```

Executes a system call. The specific operation performed is determined by the value in a designated register (typically `RAX`), and arguments are passed via other specific registers according to a defined convention.

### Calling Convention

The standard convention for `SYSCALL` is based on common practices (similar to Linux x86_64):



-   **System Call Number:** The specific system call being requested is placed in the `RAX` register.
-   **Arguments:** Up to six arguments are passed in the following registers, in order:
    1.  `RDI`
    2.  `RSI`
    3.  `RDX`
    4.  `R10`
    5.  `R8`
    6.  `R9`
-   **Return Value:** The primary return value from the system call is placed back in the `RAX` register by the OS/runtime.
-   **Error Indication:** A common convention (though OS-dependent) is for `RAX` to contain a small negative value (e.g., -1 to -4095) on error, corresponding to an error code (like `errno` in C). Success is often indicated by a non-negative return value.

*(Note: The specific system call numbers and their exact argument meanings are defined by the target operating system or runtime environment, not by the MicroASM language itself. This section defines the *mechanism* for making the call.)*

*(Note: The specific system call numbers and their exact argument meanings are defined by the **MicroASM runtime environment standard**, not by the underlying host operating system. The `SYSCALL` instruction triggers a request to the MicroASM runtime, which then **virtualizes** the call by translating the abstract MicroASM system call number and arguments into appropriate, safe actions on the host system (e.g., using host OS library functions). This ensures portability and security, preventing MicroASM code from directly executing arbitrary host OS calls.)*

### Common System Call Examples (Illustrative)

These examples assume a **hypothetical MicroASM runtime standard** for system call numbers and behavior. The actual numbers and behavior **must** be defined by the specific MicroASM runtime target you are using.

**Example 1: Exiting the Program (like `exit(0)`)**

```assembly
; Assume syscall number for exit is 60
MOV RAX 60   ; System call number for exit
MOV RDI 0    ; Exit code 0 (success)
SYSCALL      ; Make the call - program terminates
```

**Example 2: Writing to Standard Output (like `write(1, buffer, count)`)**

```assembly
STATE message <PTR>
STATE msg_len <QWORD>

LBL setup
    DB $1000 "Hello from SYSCALL!\n"
    MOV message 1000
    MOV msg_len 19

LBL write_message
    ; Assume syscall number for write is 1
    MOV RAX 1       ; System call number for write
    MOV RDI 1       ; File descriptor 1 (stdout)
    MOV RSI message ; Pointer to the buffer (address of string)
    MOV RDX msg_len ; Number of bytes to write
    SYSCALL         ; Make the call

    ; RAX will contain the number of bytes written, or a negative error code
    ; (Error checking omitted for brevity)

    HLT
```

**Example 3: Reading from Standard Input (like `read(0, buffer, count)`)**

```assembly
STATE input_buffer <PTR>
STATE buffer_size <QWORD> 128 ; Max bytes to read

LBL setup_read
    MNI Memory.allocate buffer_size R1 ; Allocate buffer
    MOV input_buffer R1

LBL read_input
    ; Assume syscall number for read is 0
    MOV RAX 0           ; System call number for read
    MOV RDI 0           ; File descriptor 0 (stdin)
    MOV RSI input_buffer ; Pointer to the buffer
    MOV RDX buffer_size ; Max number of bytes to read
    SYSCALL             ; Make the call

    ; RAX will contain number of bytes read, 0 on EOF, or negative error code
    ; (Process the input in RAX bytes at input_buffer...)

    MNI Memory.free input_buffer
    HLT
```

### SYSCALL vs MNI

-   **`SYSCALL`:** A single instruction providing direct, low-level access to OS/runtime kernel functions using a standardized register convention. The available functions and their numbers are OS/runtime-dependent.
-   **MNI:** A more flexible interface calling potentially higher-level functions implemented in native code (e.g., C, Python extensions). MNI functions have their own names and argument passing mechanisms (as defined in the MNI documentation) and might perform more complex tasks, potentially involving multiple system calls or other library functions.

Use `SYSCALL` for direct OS interaction when needed. Use MNI for accessing pre-built native functions, complex operations, or when a higher level of abstraction is desired.


## MicroASM Runtime System Call Standard (v1.0)

This section defines the standard set of system calls expected to be provided by a compliant MicroASM runtime environment via the `SYSCALL` instruction. Runtimes **must** implement these calls with the specified numbers, arguments, and behavior to ensure portability of MicroASM programs relying on `SYSCALL`.

**General Conventions:**

-   **System Call Number:** Passed in `RAX`.
-   **Arguments:** Passed in `RDI`, `RSI`, `RDX`, `R10`, `R8`, `R9` in order.
-   **Return Value:** Returned in `RAX`.
-   **Errors:** On error, `RAX` returns a negative value corresponding to a standard error code (see Error Codes section below). A non-negative value in `RAX` generally indicates success.

**Standard System Calls:**

| Number (`RAX`) | Name        | `RDI` (Arg1)             | `RSI` (Arg2)             | `RDX` (Arg3)             | Return (`RAX` on Success) | Description                                                                 |
| :------------- | :---------- | :----------------------- | :----------------------- | :----------------------- | :------------------------ | :-------------------------------------------------------------------------- |
| 0              | `read`      | File Descriptor (`fd`)   | Buffer Pointer (`<PTR>`) | Count (`<QWORD>`)        | Bytes Read (`<QWORD>`)    | Read up to `count` bytes from `fd` into `buffer`. Returns 0 on EOF.         |
| 1              | `write`     | File Descriptor (`fd`)   | Buffer Pointer (`<PTR>`) | Count (`<QWORD>`)        | Bytes Written (`<QWORD>`) | Write `count` bytes from `buffer` to `fd`.                                  |
| 2              | `open`      | Path Pointer (`<PTR>`)   | Flags (`<QWORD>`)        | Mode (`<QWORD>`)         | File Descriptor (`fd`)    | Open file at `path`. `flags` define access (e.g., read/write), `mode` permissions. |
| 3              | `close`     | File Descriptor (`fd`)   | -                        | -                        | 0                         | Close the given file descriptor.                                            |
| 5              | `fstat`     | File Descriptor (`fd`)   | Stat Buf Pointer (`<PTR>`) | -                        | 0                         | Get file status (size, type, etc.) for `fd`, store in `stat_buf`.           |
| 9              | `mmap`      | Addr Hint (`<PTR>`)      | Length (`<QWORD>`)       | Prot (`<QWORD>`)         | Pointer (`<PTR>`)         | Map memory. (Simplified interface; details TBD).                            |
| 11             | `munmap`    | Address (`<PTR>`)        | Length (`<QWORD>`)       | -                        | 0                         | Unmap memory region.                                                        |
| 35             | `nanosleep` | Timespec Ptr (`<PTR>`)   | Remainder Ptr (`<PTR>`)  | -                        | 0                         | Pause execution for duration specified in `timespec`.                       |
| 60             | `exit`      | Exit Code (`<QWORD>`)    | -                        | -                        | *No return*               | Terminate the program with the given exit code.                             |
| 201            | `time`      | Time Ptr (`<PTR>`)       | -                        | -                        | Seconds (`<QWORD>`)       | Get time as seconds since epoch. If `time_ptr` is non-null, store there too. |

**Standard File Descriptors:**

-   `0`: Standard Input (stdin)
-   `1`: Standard Output (stdout)
-   `2`: Standard Error (stderr)

**Notes on Specific Calls:**

-   **`open` Flags/Mode:** The exact numeric values for flags (e.g., `O_RDONLY`, `O_WRONLY`, `O_CREAT`) and mode bits need to be standardized by the runtime environment specification. A minimal set should be defined.
-   **`fstat` Buffer:** The layout of the `stat` buffer needs to be defined by the runtime standard (e.g., specifying offsets for size, modification time, etc.).
-   **`mmap`/`munmap`:** These provide basic memory mapping. The `prot` flags (read/write/execute permissions) also need standard definitions. This can be an alternative or supplement to MNI `Memory.allocate`/`free` for certain use cases.
-   **`nanosleep` Timespec:** The `timespec` structure format (seconds, nanoseconds) needs to be defined.

### Error Codes

When a system call fails, `RAX` will contain a negative value. The runtime standard should define mappings for common error conditions, similar to POSIX `errno` values. Examples:

| Error Code | Symbolic Name (Example) | Description                     |
| :--------- | :---------------------- | :------------------------------ |
| -1         | `EPERM`                 | Operation not permitted         |
| -2         | `ENOENT`                | No such file or directory       |
| -9         | `EBADF`                 | Bad file descriptor             |
| -12        | `ENOMEM`                | Not enough memory               |
| -14        | `EFAULT`                | Bad address (invalid pointer)   |
| -22        | `EINVAL`                | Invalid argument                |
| ...        | ...                     | ...                             |

*(This list is illustrative; the runtime standard needs a complete definition)*

This provides a baseline set of system calls. Runtime implementers would map these abstract calls and error codes to the corresponding host OS functionalities. MicroASM programmers can then use these `SYSCALL` numbers reliably across different compliant runtimes.

## _start label start

If code has a `lbl _start` then that becomes the entry point and initalization doesn't run. You are expected to initalize the stack in your _start. Now if there is no _start then the default "_start" will run. The default _start is **not** masm code and C code essentally equavalent to

```
lbl _start
mov RSP 65536 ; 65536 is the ram size
mov RBP 0
jmp #main
```

Your program **has** to exit with hlt. If you write your own _start that does
```
call #main
hlt
```
That allows for your main function to use `ret` because the _start label handles the running of the `hlt` instruction

## Additional MNI Functions

Since Micro-Assembly contains a lot of functions that are not implemented in the language, we can use MNI to call them.

*(Note: The following list provides **examples** of potential MNI functions. The actual functions available, their exact names (`Math.sin`, `Memory.allocate`, etc.), arguments, and behavior are determined by the specific MicroASM runtime environment and loaded MNI libraries, not by this core language specification.)*




### Math Operations

```assembly
MNI Math.sin R1 R2        ; Calculate sine of angle in R1, result in R2
MNI Math.cos R1 R2        ; Calculate cosine of angle in R1, result in R2
MNI Math.tan R1 R2        ; Calculate tangent of angle in R1, result in R2
MNI Math.sqrt R1 R2       ; Calculate square root of R1, result in R2
MNI Math.pow R1 R2 R3     ; Calculate R1^R2, result in R3
MNI Math.log R1 R2        ; Calculate natural logarithm of R1, result in R2
MNI Math.round R1 R2      ; Round R1 to nearest integer, result in R2
MNI Math.floor R1 R2      ; Floor R1, result in R2
MNI Math.ceil R1 R2       ; Ceiling R1, result in R2
MNI Math.random R1        ; Generate random number between 0 and 100, result in R1
```

### Memory Management

```assembly
MNI Memory.allocate R1 R2  ; Allocate R1 bytes, address stored in R2
MNI Memory.free R1         ; Free memory at address R1
MNI Memory.copy R1 R2 R3   ; Copy R3 bytes from address R1 to address R2
MNI Memory.set R1 R2 R3    ; Set R3 bytes at address R1 to value R2
MNI Memory.zeroFill R1 R2  ; Fill R2 bytes at address R1 with zeros
```

### Advanced String Operations

```assembly
MNI StringOperations.toUpper R1 R2    ; Convert string at R1 to uppercase, result at R2
MNI StringOperations.toLower R1 R2    ; Convert string at R1 to lowercase, result at R2
MNI StringOperations.trim R1 R2       ; Trim whitespace from string at R1, result at R2
MNI StringOperations.parseInt R1 R2   ; Parse string at R1 to integer, result in R2
MNI StringOperations.parseFloat R1 R2 ; Parse string at R1 to float, result in R2
MNI StringOperations.format R1 R2 R3  ; Format string at R1 with args at R2, result at R3
MNI StringOperations.contains R1 R2   ; Check if string at R1 contains string at R2, result in RFLAGS
MNI StringOperations.startsWith R1 R2 ; Check if string at R1 starts with string at R2, result in RFLAGS
MNI StringOperations.endsWith R1 R2   ; Check if string at R1 ends with string at R2, result in RFLAGS
```

### Data Structures

```assembly
MNI DataStructures.createList R1      ; Create list, handle stored in R1
MNI DataStructures.destroyList R1     ; Destroy list with handle R1
MNI DataStructures.addItem R1 R2      ; Add item from address R2 to list R1
MNI DataStructures.getItem R1 R2 R3   ; Get item at index R2 from list R1, store at address R3
MNI DataStructures.removeItem R1 R2   ; Remove item at index R2 from list R1
MNI DataStructures.getSize R1 R2      ; Get size of list R1, result in R2
MNI DataStructures.clear R1           ; Clear all items from list R1

MNI DataStructures.createMap R1       ; Create map, handle stored in R1
MNI DataStructures.destroyMap R1      ; Destroy map with handle R1
MNI DataStructures.putItem R1 R2 R3   ; Put item from address R3 with key at address R2 into map R1
MNI DataStructures.getMapItem R1 R2 R3 ; Get item with key at address R2 from map R1, store at address R3
MNI DataStructures.removeMapItem R1 R2 ; Remove item with key at address R2 from map R1
MNI DataStructures.hasKey R1 R2       ; Check if map R1 has key at address R2, result in RFLAGS
```

### Network Operations

```assembly
MNI Network.httpGet R1 R2            ; HTTP GET request to URL at address R1, response stored at address R2
MNI Network.httpPost R1 R2 R3        ; HTTP POST request to URL at R1 with data at R2, response at R3
MNI Network.openSocket R1 R2 R3      ; Open socket to host at address R1, port R2, handle stored in R3
MNI Network.closeSocket R1           ; Close socket with handle R1
MNI Network.sendData R1 R2 R3        ; Send R3 bytes of data at address R2 through socket R1
MNI Network.receiveData R1 R2 R3     ; Receive up to R3 bytes from socket R1, store at address R2
MNI Network.getHostByName R1 R2      ; Get IP address of hostname at address R1, store at address R2
```

### Debugging Operations

```assembly
MNI Debug.breakpoint                 ; Insert a breakpoint for debugger
MNI Debug.dumpRegisters R1           ; Dump all registers to address R1
MNI Debug.watchAddress R1            ; Set watch on memory address R1
MNI Debug.printStack R1              ; Print R1 elements from the stack
MNI Debug.traceOn                    ; Enable instruction tracing
MNI Debug.traceOff                   ; Disable instruction tracing
```

### File System Operations

```assembly
MNI FileSystem.open R1 R2 R3         ; Open file at path R1 with mode R2 (r,w,a), handle in R3
MNI FileSystem.close R1              ; Close file with handle R1
MNI FileSystem.read R1 R2 R3 R4      ; Read R3 bytes from file R1 into buffer R2, bytes read in R4
MNI FileSystem.write R1 R2 R3 R4     ; Write R3 bytes from buffer R2 to file R1, bytes written in R4
MNI FileSystem.seek R1 R2            ; Seek to position R2 in file R1
MNI FileSystem.tell R1 R2            ; Get current position in file R1, store in R2
MNI FileSystem.fileSize R1 R2        ; Get size of file with handle R1, store in R2
MNI FileSystem.delete R1             ; Delete file at path R1
MNI FileSystem.exists R1             ; Check if file at path R1 exists, result in RFLAGS
MNI FileSystem.mkdir R1              ; Create directory at path R1
MNI FileSystem.rmdir R1              ; Remove directory at path R1
```

### System Operations

```assembly
MNI System.exec R1 R2                ; Execute command at address R1, exit code in R2
MNI System.sleep R1                  ; Sleep for R1 milliseconds
MNI System.getEnv R1 R2              ; Get environment variable at address R1, store at address R2
MNI System.setEnv R1 R2              ; Set environment variable at address R1 to value at address R2
MNI System.getTime R1                ; Get current time in milliseconds since epoch, store in R1
MNI System.getDate R1                ; Get current date as string, store at address R1
```

## Example Program Using New Instructions

Here's a complete example that demonstrates several of the new instructions:

```assembly
; Allocate memory for strings
MNI Memory.allocate 100 R1    ; Allocate buffer for input string
MNI Memory.allocate 100 R2    ; Allocate buffer for upper case result
MNI Memory.allocate 200 R3    ; Allocate buffer for formatted output

; Store a test string
DB $1000 "Hello, World!"
MOV R4 1000
COPY R1 R4 13                 ; Copy the string to our allocated buffer

; Convert to uppercase
MNI StringOperations.toUpper R1 R2

; Format a string with the result
DB $2000 "Original: %s, Uppercase: %s"
MOV R4 2000
MNI StringOperations.format R4 R1 R3  ; Format with both strings

; Output the result
MNI IO.write 1 R3
MNI IO.flush 1

; Free allocated memory
MNI Memory.free R1
MNI Memory.free R2
MNI Memory.free R3

HLT
```
Here is a example of _start

```assembly
lbl _start
mov RSP 65536 ; init stack
mov RBP 0 ; init base pointer
DB $200 "Starting Program\n"
out 1 $200
call #main
hlt

lbl main
mov RAX 1
mov RBX 2
add RAX RBX
out 1 RAX ; outputs 3
cout 1 10 ; \n
ret ; Can use ret here because #main was called from _start and _start handles the hlt.
```

Here is a malloc example
## Example
```
lbl main
MALLOC rax 15 ; allocate 14 bytes
CMP rax 0 ; Err codes are negative
jl #error

MOVTO rax 0 72   ; H
MOVTO rax 1 101  ; e
MOVTO rax 2 108  ; l
MOVTO rax 3 108  ; l
MOVTO rax 4 111  ; o
MOVTO rax 5 44   ; ,
MOVTO rax 6 32   ;  
MOVTO rax 7 87   ; W
MOVTO rax 8 111  ; o
MOVTO rax 9 114  ; r
MOVTO rax 10 108 ; l
MOVTO rax 11 100 ; d
MOVTO rax 12 33  ; !
MOVTO rax 13 10  ; \n
MOVTO rax 14 0   ; null terminator

out 1 $rax

FREE rax rax ; Free the 15 bytes and set rax to zero (if free success else rax = the error code)

hlt

lbl error
DB $100 "Error while allocating memory: "
out 1 $100
out 1 rax
cout 1 10 ; \n
```

## Notes

- Register values often contain memory addresses for MNI functions that work with strings or complex data
- For MNI functions, check the documentation to understand whether a register should contain a direct value or a memory address
- All numeric values are treated as 64-bit integers unless specified otherwise
- String operations assume null-terminated strings
- Always free allocated memory to prevent memory leaks
