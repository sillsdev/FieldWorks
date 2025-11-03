# Research (Phase 0)

## Decisions and Rationale

### Target runtime for Avalonia
- Decision: Use Avalonia 11+ on .NET 8 (target framework: net8.0). Establish a clean interop boundary to the existing .NET Framework host via shared netstandard2.0 contracts for DTOs and a narrow service bridge (IPC/COM). Prefer out‑of‑proc (Named Pipes) returning the committed EntryRef for robust isolation; allow in‑proc only when all referenced assemblies are netstandard2.0/net8.0 compatible.
- Rationale: Aligns with the long‑term migration goal, unlocks modern tooling and HarfBuzz text stack, and avoids rework. The interop seam (strangler pattern) de‑risks progressive migration.
- Alternatives considered: Stay on .NET Framework with Avalonia 0.10.x (faster now but creates a second migration later); cross‑target entire module to both frameworks (added complexity without proportional benefit).

### Persistence strategy (Option A)
- Decision: Detached DTOs; single LCModel transaction on Save; Cancel discards; single undo operation.
- Rationale: Atomicity, predictable rollback, and alignment with LCModel and FieldWorks undo patterns.
- Alternatives: Long-running transaction with rollback (risk of locks and side effects); streaming live edits (complex undo, higher risk).

### Validation model
- Decision: Combine DTO attribute validation + ValidationService preflight that calls LCModel checks (duplicates, referential integrity) without commit.
- Rationale: Early user feedback with domain-backed assurance before Save.
- Alternatives: Only domain validation at Save time (poorer UX); only DTO validation (risks domain rejection at commit).

### Observability
- Decision: Log validation failures, Save attempts, Save duration, and materialized child counts; include WS coverage in debug logs.
- Rationale: Aids QA and performance tuning; aligns with existing diagnostics.
- Alternatives: Minimal logging (harder to triage in pilot).

### Undo/Redo integration
- Decision: Publish Save as a single undoable step via the existing FieldWorks undo stack (LcmUndoAdapter or equivalent service).
- Rationale: Matches user expectations and existing model.
- Alternatives: No undo (unacceptable); fine-grained per-child undo (misaligned with single-transaction Save).

### Accessibility & localization
- Decision: WS-aware labels and editors; all strings externalized; test RTL and combining marks.
- Rationale: Constitution i18n principle; domain criticality.
- Alternatives: Defer to later (unacceptable).

## Best practices tasks
- Avalonia.PropertyGrid custom editors: follow sample patterns (cell factory, operations menu), keep editors MVVM-friendly.
- Dynamic visibility: prefer attribute-based ([PropertyVisibilityCondition], [ConditionTarget]) with [DependsOnProperty] for reactivity; use runtime filter only for advanced cases.
- Theming: unify styles in Themes/; override PropertyGrid control template sparingly; leverage resource dictionaries.

## Patterns tasks
- LCM adapter pattern: Mapper classes own materialization; DTOs remain persistence-agnostic.
- Transaction pattern: Open/commit/rollback via LcmTransactionService; ensure exceptions propagate and are surfaced to UI.
- Template application: TemplateService applies defaults at DTO creation and supports per-field override tracking.

