# Patch Notes: Java Code Generator Backend

**Date:** 2025-09-26
**Feature ID:** UHIGH-42
**Type:** New Feature

## Summary
A new Java code generator backend (`JavaGenerator`) has been added to the Micro-High compiler. This enables code generation for Java from μHigh source code.

## Changes
- Added `JavaGenerator.cs` implementing `ICodeGenerator` for Java output.
- Added `JavaGeneratorFactory.cs` for backend registration.
- Registered the Java generator in `CodeGeneratorRegistry`.
- Updated `TypeMappingGenerator` to support Java type and method mappings.

## Usage
You can now target Java by specifying the backend as `java` in your compile options or CLI.

## Notes
- Initial implementation provides basic type and method mappings.
- Further improvements may be needed for full language feature support.

---
