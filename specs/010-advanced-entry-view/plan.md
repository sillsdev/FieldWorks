# Implementation Plan: Advanced New Entry Avalonia View

**Branch**: `010-advanced-entry-view` | **Date**: 2025-11-04 | **Spec**: `specs/010-advanced-entry-view/spec.md`
**Input**: Feature specification from `/specs/010-advanced-entry-view/spec.md`

## Summary

Create a new Avalonia-based Advanced New Entry view that replaces `InsertEntryDlg`, using Avalonia.PropertyGrid to host “form-like” editing and custom editors for FieldWorks-specific types.

Architecture decisions:
- **Persistence strategy (chosen)**: **LCModel-first edit session** backed by a **long-lived undo task**. Editing mutates the in-memory LCModel cache, but **no persistence occurs until an explicit Save**. Save ends the undo task and triggers the explicit commit; Cancel/Close (and error paths) roll back to the start of the edit session.
- **View construction (Path 3)**: the UI is driven by the existing **Parts/Layout configuration** as the view contract. We implement a new **managed (C#) interpreter/compiler** that turns those definitions into a stable **Presentation IR** which is rendered/edited in Avalonia.

Parity scope and requirements are tracked in `specs/010-advanced-entry-view/parity-lcmodel-ui.md`.

## Technical Context

**Language/Version**: C#; Avalonia UI on .NET 8 (target framework: net8.0).
**Primary Dependencies**: Avalonia UI, Avalonia.PropertyGrid (MIT), CommunityToolkit.Mvvm, FieldWorks LCModel/liblcm, existing logging/diagnostics.
**Storage**: LCModel project database via LcmCache and domain services (no schema change).
**Testing**: Prefer layered tests (IR/staging/validation/materialization) plus a small set of **headless Avalonia interaction** tests for UI wiring (expand/collapse, type, hover, right-click) with no OS-visible windows. Avoid full Windows UI automation.
**Target Platform**: Windows desktop (FieldWorks primary platform).
**Project Type**: New managed project for Avalonia UI module (net8.0) integrated into FieldWorks.
**Performance Goals**: Save (DTO→LCM materialization) avg ≤ 250 ms for typical entries; validation preflight ≤ 150 ms; UI responsiveness ≥ 60 fps for editor interactions.
**Constraints**: Offline-capable; multi‑writing‑system correctness; respect existing project WS defaults; no writes during edit; single-transaction Save.
**Additional Constraints**: Sunset all legacy C++ UI/view code over time; new work must not depend on the legacy C++ view runtime.
**Scale/Scope**: Single feature view (Advanced New Entry), 3–5 custom editors, ~20–30 classes (DTOs, mappers, services, editors).

## Chosen Path: Path 3 (Parts/Layout as contract)

We treat the existing LexText Parts/Layout definitions as a long-lived, highly customizable contract for what the editor should show and how it is structured (sections, ordering, nested sequences, “ghost” items, custom-field patterns). This approach best matches:
- Long-term migration to **all-Avalonia UI**
- **Customizable views** independent of the underlying model
- Parity expectations captured in `specs/010-advanced-entry-view/parity-lcmodel-ui.md`

Implementation summary:
- **Load** Parts/Layout definitions (starting with LexEntry).
- **Compile** into a UI-agnostic **Presentation IR**.
- **Bind** IR to Avalonia.PropertyGrid via a PropertyGrid adapter (editor selection, operations).
- **Bind** IR to an LCModel-backed editing model (LCModel objects are the domain model).
- **Validate** without committing (IR-required + LCModel preflight).
- **Save** ends the edit-session undo task and performs the explicit commit.
- **Cancel/Close/error** rolls back the edit-session undo task (no persistence).

Required safeguards for this persistence strategy:
- **Commit fencing**: while Advanced Entry is in an active edit session, prevent unrelated code paths from calling commit/save (e.g., project save commands, import flows).
- **Undo task ownership**: prevent other services from ending or replacing the outer undo task during editing (e.g., record navigation auto-save behaviors that call `EndUndoTask()` opportunistically).
- **Side-effect audit**: avoid or explicitly handle operations that persist outside the LCModel commit path (e.g., writing system saves, settings writes).

Research that grounds this choice and identifies reuse points:
- `specs/010-advanced-entry-view/presentation-ir-research.md` (existing managed contract resolution + `XmlVc` + `DisplayCommand` layer).

Initial implementation choice (to remove ambiguity for Track 1):
- Start with **DisplayCommand → typed IR translation** (Path 3 in `presentation-ir-research.md`). Use a collector env only as a debugging/verification tool, not as the exported IR.

## Contract entry points & resolution (required for Path 3)

- Root contract for LexEntry editor (initial Track 1 entry point):
    - Root class: `LexEntry`
    - Root layout type: `detail`
    - Root layout name: `Normal`
    - Shipped defaults:
        - `DistFiles/Language Explorer/Configuration/Parts/LexEntry.fwlayout` (layout inventory)
        - `DistFiles/Language Explorer/Configuration/Parts/LexEntryParts.xml` (part inventory)
- Resolution order: project/user overrides → shipped defaults under `DistFiles/Language Explorer/Configuration/Parts/` → fallback behavior (fail-fast with user-facing error).
- Caching: cache resolved/compiled IR per project + configuration fingerprint; invalidate on config changes.

## Path 3 performance acceptance checklist (enforced, not aspirational)

Goal: large, complex layouts (including many custom fields) must not make the UI feel “slow” because expensive layout/custom-field work is happening on the UI thread.

This checklist is the non-negotiable “definition of done” for Track 1 work.

### A) Cache keys and invalidation (compile once, reuse many)
- A stable cache key exists for “compiled presentation” results. Minimum recommended inputs:
    - Project identity (project path + database name)
    - Root class id (e.g., LexEntry)
    - Root layout/part id
    - Configuration fingerprint (default config version + user override set)
    - Writing system profile inputs that affect layout (if applicable)
- Cache invalidation is explicit (config version change, overrides change, WS profile change).
- Compiled results are deterministic: same key → same IR (snapshot test).

### B) Async compilation boundary (no heavy work on UI thread)
- UI thread work is limited to:
    - requesting compilation
    - binding already-compiled IR to view-models
    - incremental UI updates from staged state
- The compiler performs all expensive operations off the UI thread:
    - contract resolution (Inventory/LayoutCache)
    - semantic interpretation / translation (e.g., `DisplayCommand` translation)
    - custom-field expansion logic (if needed)
- Compilation supports cancellation (closing window or switching entry points cancels in-flight work).

### C) Virtualization boundary (don’t create 100s of live editors)
- Sequences (senses/examples/variants/custom-field lists) have an explicit virtualization strategy.
    - Rendering must not instantiate editor controls for every child node up-front.
    - The boundary must be explicit in the IR (e.g., “sequence nodes are virtualized by default”).
- The UI can still validate required fields without forcing full control creation (validation is IR/staged-state driven).

### D) Grounding in existing code (reuse, don’t re-invent)
- Contract resolution must reuse existing override/unification semantics (Inventory + LayoutCache), as documented in `specs/010-advanced-entry-view/presentation-ir-research.md`.
- Compilation should leverage existing managed XMLViews semantics where feasible (e.g., `XmlVc` + `DisplayCommand`) without requiring the legacy C++ view runtime.

## Testing Strategy (Layered + Headless)

Goal: verify complex UI behavior (expand/collapse, edit, hover, right-click) quickly and deterministically, while keeping tests **invisible** (no pop-up windows) and compatible with the repo’s migration from WinForms/C++ to Avalonia.

### Default: Layered tests (fast, stable)
- IR + contract loading: determinism, resolution order, nested template compilation.
- Editing session: round-trip edits and verify rollback-on-cancel restores the LCModel state.
- Validation: required fields + IR-driven rules return actionable errors without committing.
- Transactions (Track 2): targeted integration tests for “Save commits”, “Cancel rolls back”, and “no commits occur during editing.”

### Targeted: Headless Avalonia interaction tests (invisible UI)
- Use Avalonia’s headless testing approach to host the AdvancedEntry view without a real OS window.
- Exercise only the critical UI wiring that cannot be proven via pure unit tests:
    - expand/collapse nested sequences and objects
    - pointer hover and context menu invocation
    - typing into editors updates staged state

### Explicit non-goal
- Do not introduce full Windows UI Automation (UIA/FlaUI/WinAppDriver) for this feature work; it is slower, flakier, and requires real windows.

## Developer Preview Host (Option 1)

To enable fast UI iteration without launching the full FieldWorks application, add a shared Avalonia “Preview Host” executable.

- **Goal**: Build/run a single small app that discovers Avalonia modules and launches one by id, optionally with sample/empty data.
- **Approach**: Modules register themselves via an assembly-level attribute that specifies:
    - Module id + display name
    - Window type to instantiate
    - Optional data provider type for sample data

Proposed paths:

```text
Src/Common/FwAvalonia/
    Preview/
        FwPreviewModuleAttribute.cs
        IFwPreviewDataProvider.cs
Src/Common/FwAvaloniaPreviewHost/          # executable
scripts/Agent/Run-AvaloniaPreview.ps1      # convenience launcher
```

## Constitution Check

Pre‑design gates (pass/plan):
- Data integrity: No schema change; transactional Save; add automated parity tests (legacy vs new) and preflight checks. PASS (with planned tests).
- Test evidence: Add unit tests for mappers/validation; integration test to assert materialized entries match legacy Entry/Edit representation. PASS (planned coverage required).
- I18n/script correctness: Validate RTL/combining marks; WS-aware editors; leverage Harfbuzz from Avalonia directly. PASS (tests to be authored).
- Licensing: Avalonia UI + Avalonia.PropertyGrid (MIT) compatible with LGPL 2.1+; include notices. PASS.
- Stability/performance: No live writes; single transaction; feature gated (command entry point). PASS (flagging supported via UI visibility).

Note: “No live writes” here means **no persistence to the project database unless Save is invoked**. The LCModel cache may be mutated during editing, but Cancel/Close/error paths must roll back before any commit.

Re‑check after Phase 1 to ensure tests and NFR targets are anchored in design docs.

## Project Structure

### Documentation (this feature)

```text
specs/010-advanced-entry-view/
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
    ├── Layout/                                 # Parts/Layout loading + compilation (managed interpreter)
    ├── Presentation/                            # Presentation IR types + utilities
    ├── Staging/                                # Detached staged state keyed by IR nodes
    ├── Materialization/                        # Stage → LCModel mapping (Entry/Sense/Example/etc.)
    ├── Services/                               # ValidationService, LcmTransactionService, TemplateService
    ├── Editors/                                # WsTextEditor, PossibilityTreeEditor, FeatureStructureEditor, ReferencePickerEditor
    ├── Views/                                  # PropertyGrid host, EntrySummaryView
    ├── Themes/                                 # XAML styles/templates for PropertyGrid and editors
    └── AdvancedEntry.Avalonia.Tests/           # NUnit tests (DTOs, mappers, editors via interaction harness)
```

Preview tooling (shared across Avalonia modules):

```text
Src/
└── Common/FwAvaloniaPreviewHost/               # Avalonia Preview Host app
scripts/
└── Agent/Run-AvaloniaPreview.ps1               # builds + launches preview host
```

**Structure Decision**: Single new managed UI project under `Src/` for the Avalonia view, plus a colocated test project. Exact folder naming to align with existing conventions (confirm in Phase 0).

## Complexity Tracking

| Violation      | Why Needed                                                                    | Simpler Alternative Rejected Because                                      |
| -------------- | ----------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| New UI project | Avalonia requires separate XAML/runtime toolchain                             | Embedding into legacy WinForms obscures separation and increases coupling |
| Custom editors | FieldWorks-specific types (WS strings, possibility lists, feature structures) | Built‑ins cannot model required semantics                                 |
