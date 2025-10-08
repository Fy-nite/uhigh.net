# μHigh Language Evolution Roadmap (Q4 2025 Draft)

Status: DRAFT
Last Updated: 2025-10-02
Owner: (add maintainer)

## Vision
Progress μHigh from a capable transpiled scripting language into a robust, strongly-typed, multi-backend language with modern ergonomics, tooling, and safe scalability.

## Guiding Principles
1. Predictable: Clear, unambiguous semantics over cleverness.
2. Incremental: Ship small vertical slices (parse → analyze → codegen → tests → docs) each PR.
3. Portable: All new features must compile cleanly across existing backends (C#, Java, JS, MicroASM) or degrade gracefully.
4. Tooling-first: LSP + formatter + diagnostics mature alongside language features.
5. No silent magic: Prefer explicit syntax over hidden inference (except obvious local inference).

## Phase Overview
| Phase | Focus | Representative Features | Target Benefit |
|-------|-------|-------------------------|----------------|
| 1 | Foundational Ergonomics | Enums, Tuples, Basic Pattern Matching, Formatter, LSP Hover/Definition, Test DSL, Error Improvements | Developer productivity & clarity |
| 2 | Type Power & Interop | Generics (functions), Structs, FFI (C), Coroutines (async/await), Coverage, Package Lockfile, Compile-Time Eval (limited) | Reuse + ecosystem + performance |
| 3 | Advanced Semantics | ADTs (union types), Full Pattern Matching (destructuring), Channels & spawn, WASM backend, Hygienic Macros (basic), Source Maps, Reflection Metadata | Expressiveness + new deployment targets |
| 4 | Safety & Performance | Borrowing-lite analysis, Arena allocators, Actor model, Ownership warnings (later enforcement), Advanced static analysis passes | Reliability + performance scaling |

## Phase 1 (Active)
### Goals
- Ship *visible* improvements quickly.
- Establish conventions for proposals & docs.
- Lay syntactic groundwork for later ADTs & generics.

### Features
1. Enums (simple + explicit value + optional [flags])
2. Tuples (expressions + destructuring + multiple return reuse)
3. Pattern Matching (literals + enum cases v1)
4. Code Formatter (deterministic style + CI check)
5. LSP Enhancements (hover types, go to definition, references, basic rename)
6. Test DSL sugar (`test "name" { ... }`)
7. Error Message Enrichment (spans, hints, did-you-mean)

### Milestone Exit Criteria
- All 7 features merged behind version flag (if needed) or stable.
- Added ≥12 new unit tests (≥2 per feature).
- Formatter used in >90% tracked files (auto-check in CI).
- LSP latency (hover) < 80ms median in sample project.
- Docs updated: LANGUAGE.md references new constructs.

## Phase 2 (Planned)
- Generics (function-level monomorphization)
- Structs (value types; no inheritance)
- FFI (extern C API + basic marshaling) 
- Coroutines (async lowering—C# backend first, others fallback to sync or error)
- Coverage Instrumentation (statement mapping) 
- Package Lockfile (deterministic builds)
- Compile-Time Functions (`comptime` pure subset)

## Phase 3 (Exploratory)
- Algebraic Data Types (unions with payloads)
- Full Pattern Matching (destructure tuples, arrays, ADTs)
- Concurrency: channels + `spawn`
- WASM Backend (baseline subset) 
- Hygienic Macros (template + pattern forms)
- Source Maps (multi-backend mapping file)
- Reflection Metadata (opt-in attributes)

## Phase 4 (Research)
- Ownership / Borrow Checking (advisory → enforce)
- Region / Arena Allocation Blocks
- Actor Model Runtime
- Advanced Static Analysis (escape, purity, unused mutation)

## Feature Templates
All design proposals must follow:
```
# Feature: <Name>
Status: (Draft|Accepted|Implemented)
Author:
Tracking ID: feature-<kebab-name>

## Motivation
## Goals / Non-Goals
## Syntax
## Semantics
## Desugaring / Lowering
## Type Rules
## Code Generation Notes (per backend if needed)
## Migration / Interaction with Existing Features
## Testing Strategy
## Open Questions
```

## Cross-Cutting Concerns
| Concern | Action |
|---------|--------|
| Diagnostics | Centralize formatting & hints in `DiagnosticsReporter` |
| Versioning | Introduce language version flag (future) |
| Backend Divergence | Add capability matrix doc |
| Performance Baselines | Add microbenchmarks after struct + arenas |

## Risk Register
| Risk | Mitigation |
|------|------------|
| Feature creep in macros | Scope macros to non-recursive templates initially |
| Generics code bloat | On-demand instantiation + pruning unused |
| Async semantics mismatch across backends | Define spec: async = logical future + await suspension points |
| Ownership model complexity | Start with warnings & lints only |

## Metrics
| Metric | Target |
|--------|--------|
| CI green rate | >95% |
| New feature test coverage | ≥80% lines in touched files |
| LSP hover median latency | <80ms |
| Formatter adoption | ≥90% tracked source lines |

## Implementation Ordering (Phase 1 Fine-Grain)
1. Enum parsing + AST + codegen (C# backend -> enum, others -> constants)  
2. Tuple syntax (reuse multi-return) + destructuring  
3. Basic pattern match (desugar to if-chain)  
4. Formatter (token stream → rule engine)  
5. Hover & definition (SemanticAnalyzer symbol table exposure)  
6. Test DSL sugar (parse to function + register)  
7. Error enrichment (collect spans; suggestion table)  

## Open Questions
- Should enums support methods in v1? (Default: No)  
- Tuple mutability semantics? (Immutable by default)  
- Match exhaustiveness warnings in v1? (No—Phase 2)  
- Formatter configuration file? (Later, start fixed)  

## Next Action (Proposed)
Implement `feature-enums` (create proposal doc + parser change) as first vertical slice.

---
Refer to individual `design-*.md` proposal documents for each feature's evolving specification.
