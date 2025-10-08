# Feature: Enums
Status: Draft
Tracking ID: feature-enums
Target Phase: 1
Author: (add)

## 1. Motivation
Currently constants are represented via `const` declarations or magic strings/ints. Enums provide:
- Semantic grouping of related named values
- Type-safe comparisons & future pattern matching integration
- Cleaner code in control flow and API signatures

## 2. Goals
- Define named sets of integral constants
- Allow explicit underlying values
- Support simple attribute `[flags]` for bitmask sets
- Integrate with upcoming pattern matching (Phase 1 basic)
- Generate backend-specific enum constructs where available (C#, Java)

## 3. Non-Goals (Phase 1)
- Enum methods or associated functions
- Generic enums
- Enum value reflection APIs
- Mixed-type payload variants (belongs to ADTs Phase 3)

## 4. Examples
```go
enum Color { Red, Green, Blue }

enum Code { Ok = 0, NotFound = 404, ServerError = 500 }

[flags] enum Permission { Read = 1, Write = 2, Execute = 4 }

var c: Color = Color.Red
if c == Color.Green { print "Go" }

var perms: Permission = Permission.Read | Permission.Write
```

## 5. Syntax
Proposed grammar additions (informal):
```
EnumDecl ::= ('[flags]')? 'enum' Identifier '{' EnumMemberList '}'
EnumMemberList ::= EnumMember (',' EnumMember)* (',')?
EnumMember ::= Identifier ('=' IntegerLiteral)?
QualifiedEnumValue ::= Identifier '.' Identifier
```

## 6. Semantics
- Underlying type: implicit 32-bit signed integer (could extend later).
- First unassigned member = 0; subsequent unassigned = previous + 1.
- Explicit values must be constant-foldable integer expressions (v1: literal only).
- `[flags]` indicates bitmask semantics; formatter does not enforce power-of-two but linter may warn.

## 7. Type Rules
- Enum type is distinct from `int`; implicit cast to int is NOT allowed (Phase 1) to avoid silent misuse.
- Explicit cast syntax later (not in v1) OR helper builtin `int(Color.Red)` (optional—decision pending).
- Comparisons: Only enum == enum of same type, else diagnostic.
- Assignment: Variable of enum type must be initialized with declared enum member.

## 8. Lowering / Desugaring
AST Node: `EnumDeclaration { Name, IsFlags, Members: List<EnumMember { Name, Value(int) }> }`
Symbol Table: Binds `Color` as a type symbol; each member as constant symbol namespaced under `Color`.
Codegen:
- C#: `public enum Color { Red = 0, Green = 1, Blue = 2 }`
- Java: Generate class with static finals (Phase 1) OR true enum if backend pipeline can support.
- JS: Object literal mapping: `const Color = { Red: 0, Green: 1, Blue: 2 }`.
- MicroASM: Emit constants in a special section or as `STATE`? (Simpler: comment placeholder + inline numeric use.)

## 9. Backend Notes
- Ensure name collision avoidance (prefix with namespace/module if implemented).
- Flags: add `[System.Flags]` attribute in C#.
- Java flags: no direct semantics; just values.

## 10. Performance Considerations
Negligible; compile-time only constructs.

## 11. Tooling Impact
Parser: Add enum production.
Semantic: Add symbol kinds; enforce uniqueness & type constraints.
LSP: Hover on `Color.Red` shows `Color.Red : Color = 0`.
Formatter: Ensure trailing commas optional; spacing normalized.

## 12. Diagnostics (Examples)
- Duplicate member: `Enum member 'Red' already defined in 'Color'.`
- Mixed type: (N/A v1)
- Invalid assignment: `Cannot assign value of type 'int' to variable of type 'Color'.`
- Unknown member: `'Purple' is not a member of enum 'Color'.`

## 13. Testing Strategy
- Parse tests: minimal, explicit values, flags attribute, trailing comma
- Semantic: duplicate detection, ordering auto-increment, type mismatch
- Codegen snapshots per backend
- Negative: invalid value reuse, unknown reference

## 14. Migration
No breaking changes; new syntax only.

## 15. Alternatives
- Use `const` groups (less type safety)
- Prefixed naming (verbose)

## 16. Open Questions
- Allow implicit int→enum if literal fits? (Current: No)
- Provide builtin cast helper now? (Leaning: Defer)

## 17. Acceptance Checklist
- [ ] Spec reviewed
- [ ] Parser implemented
- [ ] Semantic rules enforced
- [ ] Codegen all target backends or documented omissions
- [ ] Tests added
- [ ] Docs updated (LANGUAGE.md + patch notes)
- [ ] Patch notes file created: `patch-notes-enums.md`

---
End of document.
