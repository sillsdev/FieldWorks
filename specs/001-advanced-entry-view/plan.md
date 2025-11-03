# Implementation Plan: Advanced New Entry Avalonia View

**Branch**: `001-advanced-entry-view` | **Date**: 2025-11-04 | **Spec**: `specs/001-advanced-entry-view/spec.md`
**Input**: Feature specification from `/specs/001-advanced-entry-view/spec.md`

## Summary

Create a new Avalonia-based Advanced New Entry view that replaces `InsertEntryDlg`, using Avalonia.PropertyGrid to host “form-like” editing and custom editors for FieldWorks-specific types. Adopt Option A: edits are staged in detached DTOs; Save materializes a `LexEntry` subtree in a single LCModel transaction; Cancel discards. Provide a summary preview, validation (DTO + LCModel preflight), and optional single undo unit for the Save.

## Technical Context

**Language/Version**: C#; Avalonia UI on .NET 8 (target framework: net8.0).
**Primary Dependencies**: Avalonia UI, Avalonia.PropertyGrid (MIT), CommunityToolkit.Mvvm, FieldWorks LCModel/liblcm, existing logging/diagnostics.
**Storage**: LCModel project database via LcmCache and domain services (no schema change).
**Testing**: NUnit-style unit + integration tests (FieldWorks standard); UI behavior covered by view-model tests and editor-specific interaction tests.
**Target Platform**: Windows desktop (FieldWorks primary platform).
**Project Type**: New managed project for Avalonia UI module (net8.0) integrated into FieldWorks.
**Performance Goals**: Save (DTO→LCM materialization) avg ≤ 250 ms for typical entries; validation preflight ≤ 150 ms; UI responsiveness ≥ 60 fps for editor interactions.
**Constraints**: Offline-capable; multi‑writing‑system correctness; respect existing project WS defaults; no writes during edit; single-transaction Save.
**Scale/Scope**: Single feature view (Advanced New Entry), 3–5 custom editors, ~20–30 classes (DTOs, mappers, services, editors).

## Constitution Check

Pre‑design gates (pass/plan):
- Data integrity: No schema change; transactional Save; add automated parity tests (legacy vs new) and preflight checks. PASS (with planned tests).
- Test evidence: Add unit tests for mappers/validation; integration test to assert materialized entries match legacy Entry/Edit representation. PASS (planned coverage required).
- I18n/script correctness: Validate RTL/combining marks; WS-aware editors; leverage Harfbuzz from Avalonia directly. PASS (tests to be authored).
- Licensing: Avalonia UI + Avalonia.PropertyGrid (MIT) compatible with LGPL 2.1+; include notices. PASS.
- Stability/performance: No live writes; single transaction; feature gated (command entry point). PASS (flagging supported via UI visibility).

Re‑check after Phase 1 to ensure tests and NFR targets are anchored in design docs.

## Project Structure

### Documentation (this feature)

```text
specs/001-advanced-entry-view/
├── plan.md              # This file (/speckit.plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # Phase 1 output (internal module contract)
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
Src/
└── LexText/AdvancedEntry.Avalonia/             # Avalonia UI project (feature module)
    ├── Models/                                 # DTOs: EntryModel, SenseModel, etc.
    ├── Mappers/                                # EntryMapper, SenseMapper, ...
    ├── Services/                               # ValidationService, LcmTransactionService, TemplateService
    ├── Editors/                                # WsTextEditor, PossibilityTreeEditor, FeatureStructureEditor, ReferencePickerEditor
    ├── Views/                                  # PropertyGrid host, EntrySummaryView
    ├── Themes/                                 # XAML styles/templates for PropertyGrid and editors
    └── AdvancedEntry.Avalonia.Tests/           # NUnit tests (DTOs, mappers, editors via interaction harness)
```

**Structure Decision**: Single new managed UI project under `Src/` for the Avalonia view, plus a colocated test project. Exact folder naming to align with existing conventions (confirm in Phase 0).

## Complexity Tracking

| Violation      | Why Needed                                                                    | Simpler Alternative Rejected Because                                      |
| -------------- | ----------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| New UI project | Avalonia requires separate XAML/runtime toolchain                             | Embedding into legacy WinForms obscures separation and increases coupling |
| Custom editors | FieldWorks-specific types (WS strings, possibility lists, feature structures) | Built‑ins cannot model required semantics                                 |
