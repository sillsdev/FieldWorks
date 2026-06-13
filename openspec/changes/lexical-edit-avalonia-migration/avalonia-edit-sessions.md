# Avalonia Edit Sessions

This plan separates the current AdvancedEntry implementation from the proposed edit-session seam needed for production Lexical Edit migration.

## Current State

| Item | Source | Current Behavior |
|---|---|---|
| Concrete session | `010-advanced-entry-preview-prototype` | Prototype starts a fenced LCModel undo task for a selected entry, then exposes `Save()` and `Cancel()`. |
| View-model ownership | `010-advanced-entry-preview-prototype` | Prototype loads one entry lifetime, disposes it on save/cancel, and requests window close through a callback. |
| Existing coverage | `AdvancedEntryEditSessionTests`, `MainWindowViewModelLifetimeTests` | Save/cancel behavior, nested-session rejection, and basic lifetime disposal are characterized. |

There is no implemented `ILexicalEditSession` with `GetValue`, `SetValue`, or `Commit` semantics. Any such API is a proposed Phase 3 seam.

## Architectural Decision Needed

Before first editable slice work expands, choose one model and encode it in tests:

| Option | Pros | Risks | Required Tests |
|---|---|---|---|
| Direct LCModel fenced undo task | Matches current spike and existing LCModel action-handler behavior. | UI edits affect model before save if not staged carefully; cancel must reliably roll back all touched data. | Multi-field cancel rollback, save creates one undoable action, global undo after save, nested sessions rejected before mutation. |
| Staged draft model | Cleaner validation and cancel semantics before commit. | More code; must map drafts to LCModel objects and handle stale model state. | Draft isolation, conflict/stale object detection, commit transaction, rollback on partial failure. |

Default recommendation for Phase 3: keep the current direct fenced undo-task model for the first slice, but add tests that prove cancel/save/global undo semantics before broadening editable fields.

## Proposed Seam Contract

The extracted seam should be introduced only with tests. It should provide:

- One active session per editable root object unless nested sessions are explicitly designed.
- Explicit lifecycle states: `Active`, `Saved`, `Canceled`, `Disposed`, `Faulted`.
- Main-thread LCModel write enforcement.
- Deterministic cancellation/rollback even after validation errors.
- Save result that reports changed objects/flids for refresh coordination.
- No direct UI dependency, dialog dependency, or WinForms dependency.

## Required Tests

| Test Area | Cases |
|---|---|
| Lifecycle | Save once, cancel once, dispose without save, double save/cancel no-op or exception by contract, nested session rejected. |
| Rollback | Single-field rollback, multi-field rollback, sequence add/remove rollback, object creation rollback, stale reference rollback. |
| Commit | Save creates one undoable action; global undo/redo restores values; failure during commit does not leave partial state. |
| Refresh | Save reports changed objects/flids; cancel reports no committed changes; disposed session does not emit late refresh. |
| Threading | LCModel writes happen on approved thread; background validation/layout cannot mutate cache. |
| Localization/diagnostics | User-facing save/cancel errors use resources; diagnostics include entry HVO/class/flid without leaking unsafe input across native boundaries. |

## Phase Gates

| Phase | Gate |
|---|---|
| Phase 2 | Current concrete session tests pass and gaps are recorded in the coverage report. |
| Phase 3 | Introduce seam with lifecycle/rollback contract tests before moving logic out of the view model. |
| Phase 5 | First editable slice proves save/cancel/global undo/redo against real LCModel fields. |
| Phase 8 | Shell integration proves XCore/global command routing uses the same session semantics. |