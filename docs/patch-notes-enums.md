# Patch Notes: Enum Support

ID: feature-enums
Date: 2025-10-08

Summary:
- Initial end-to-end support for enums in μHigh.

What's new:
- Lexer: `enum` keyword already recognized.
- Parser: Enum declarations parsed into AST `EnumDeclaration` with optional base type and members with optional explicit values.
- AST: `EnumDeclaration` and `EnumMember` nodes present.
- Codegen:
  - C#: Emits real C# `enum` with modifiers and optional base type; members include explicit values if provided.
  - Java: Emits Java `enum` declarations (existing); left as-is.
  - Multi-file C#: `MultiFileGenerator` now writes enums to their own files and prints content properly.

Tests:
- Added parser test for enum declarations.
- Added codegen test validating C# enum emission and explicit value handling.

Notes:
- Flags attribute support and semantic validation (e.g., duplicate members, usage typing) are deferred.
- Other backends (C++, JS, etc.) do not yet special-case enums beyond existing behavior.

Next steps:
- Enforce enum type safety in SemanticAnalyzer (comparisons, assignments).
- Optional `[flags]` attribute parsing and emission.
- Enum usage in expressions (qualified access, pattern matching) typing checks.
