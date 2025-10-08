# Feature Design Template
Status: Draft

Use this template for all new language / tooling feature proposals.

## Metadata
- Feature: <Name>
- Tracking ID: feature-<kebab-name>
- Author:
- Status: Draft | Review | Accepted | Implemented | Deferred | Rejected
- Target Phase: (1|2|3|4)
- PR(s): (link later)

## 1. Motivation
Why is this needed? What problems does it solve? Provide concrete examples of current friction.

## 2. Goals
Bullet list of explicit goals.

## 3. Non-Goals
Boundaries to avoid scope creep.

## 4. User Stories / Examples
Code examples before & after.

## 5. Syntax
Formal grammar diff or EBNF style snippet.

## 6. Semantics
Behavioral rules, evaluation order, side-effects.

## 7. Type Rules
Typing judgments, inference notes, edge cases.

## 8. Lowering / Desugaring
How this maps onto existing AST / IR / codegen constructs.

## 9. Backend Code Generation Notes
Per backend (C#, Java, JS, LLVM, MicroASM, etc.)—highlight divergences or fallbacks.

## 10. Runtime / Performance Considerations
Complexity, allocations, hot paths.

## 11. Tooling Impact
- Parser changes
- Semantic analyzer
- LSP (hover, completion, rename)
- Formatter

## 12. Diagnostics
Common errors, suggested messages & hints.

## 13. Testing Strategy
List categories: parser, semantics, codegen, integration, negative tests.

## 14. Migration / Compatibility
Any breaking changes? Version gating required?

## 15. Alternatives Considered
Concise rejected designs + why.

## 16. Open Questions
Unresolved issues needing consensus.

## 17. Acceptance Checklist
- [ ] Spec reviewed
- [ ] Parser implemented
- [ ] Semantic rules enforced
- [ ] Codegen all backends or documented omissions
- [ ] Tests added
- [ ] Docs updated
- [ ] Patch notes written

---
End of document.
