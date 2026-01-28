# Research (Phase 0)

## Decisions and Rationale

### View construction strategy (Path 3)
- Decision: Use **Path 3**: treat LexText **Parts/Layout configuration** as the long-lived view contract. Implement a **managed (C#) interpreter/compiler** that compiles that contract into a stable **Presentation IR** rendered/edited in Avalonia.
- Rationale: Best matches long-term goals: full UI replacement with Avalonia, strong customization, and parity with existing Entry/Edit structure as captured in `specs/010-advanced-entry-view/parity-lcmodel-ui.md`.

Implementation note (to match Track 1):
- Start with the approach documented in `specs/010-advanced-entry-view/presentation-ir-research.md` (Path 3): translate existing managed XMLViews `DisplayCommand` structures into typed Presentation IR; use collector envs only for debugging.
- Enforce the performance acceptance checklist in `specs/010-advanced-entry-view/plan.md` (cache key, async compile boundary, virtualization boundary).

### Sunset legacy C++ UI/view code
- Decision: Over time, **sunset all C++ UI/view code**. New work must not depend on the legacy C++ view runtime.
- Rationale: Reduces long-term maintenance burden and removes cross-language UI complexity while preserving the LCModel interpretation rules.

### Target runtime for Avalonia
- Decision: Use Avalonia 11+ on .NET 8.
- Notes: The exact hosting boundary (in-proc vs out-of-proc) depends on how the LexText host evolves. The immediate requirement is that the LexText command opens an Avalonia window **without relying on WinForms or legacy C++ view components** (FR-001/FR-013).
- Rationale: Aligns with the long‑term migration goal, unlocks modern tooling and HarfBuzz text stack, and avoids rework. The interop seam (strangler pattern) de‑risks progressive migration.
- Alternatives considered: Stay on .NET Framework with Avalonia 0.10.x (faster now but creates a second migration later); cross‑target entire module to both frameworks (added complexity without proportional benefit).

### Persistence strategy (Option A)
- Decision: Detached staged state; single LCModel transaction on Save; Cancel discards; single undo operation.
- Rationale: Atomicity, predictable rollback, and alignment with LCModel and FieldWorks undo patterns.
- Alternatives: Long-running transaction with rollback (risk of locks and side effects); streaming live edits (complex undo, higher risk).

### Validation model
- Decision: Combine IR/staged-state validation + ValidationService preflight that calls LCModel checks (duplicates, referential integrity) without commit.
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
- LCM adapter pattern: Materializer classes own materialization from staged state; UI remains persistence-agnostic.
- Transaction pattern: Open/commit/rollback via LcmTransactionService; ensure exceptions propagate and are surfaced to UI.
- Template application: TemplateService applies defaults at DTO creation and supports per-field override tracking.
