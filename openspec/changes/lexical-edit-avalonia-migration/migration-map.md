# Migration Map: Speckit Advanced Entry to OpenSpec Lexical Edit

This change migrates the useful Speckit material from `specs/010-advanced-entry-view/` into OpenSpec while expanding scope from Advanced New Entry to the full Lexical Edit Avalonia migration.

| Speckit source | OpenSpec destination | Notes |
|---|---|---|
| `spec.md` | `proposal.md`, `specs/lexical-edit-avalonia-migration/spec.md` | Reframed from Advanced New Entry to full Lexical Edit migration. Existing FR/SC ideas become phased migration requirements and acceptance gates. |
| `plan.md` | `design.md`, `tasks.md` | Carries over Path 3, Preview Host, cache/async/virtualization, and headless testing, but changes XML from long-term contract to transitional import source. |
| `research.md` | `design.md`, `specs/lexical-edit-view-definition/spec.md` | Preserves Avalonia/.NET 8, diagnostics, validation, and IR direction. |
| `presentation-ir-research.md` | `design.md`, `specs/lexical-edit-view-definition/spec.md` | Preserves Inventory/LayoutCache/XMLViews reuse research and adds XML retirement gates. |
| `parity-lcmodel-ui.md` | `specs/lexical-edit-parity-automation/spec.md`, `tasks.md` | Becomes the baseline checklist for semantic parity scenarios. |
| `tasks.md` | `tasks.md` | Existing completed Advanced Entry spike tasks are treated as prior art; new tasks sequence refactor-first Lexical Edit migration. |
| `quickstart.md` | Future implementation quickstart | Not copied yet because this OpenSpec change is architectural and phased. |
| `data-model.md` | Future typed view-definition data model | Not copied verbatim; its concepts feed the IR/view-definition requirements. |
| `contracts/openapi.yaml` | Not migrated | Internal API contract is too narrow for the broader Lexical Edit architecture. Revisit if a service boundary becomes externally callable. |
