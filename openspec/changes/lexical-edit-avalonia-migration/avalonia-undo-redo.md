# Avalonia Undo/Redo Plan

Undo/redo is a shell and LCModel contract, not a local Avalonia convenience. The migrated editor must integrate with the existing FieldWorks action-handler behavior before it can be enabled by default.

## Current State

| Area | Current Behavior |
|---|---|
| Legacy app | LCModel write operations are normally wrapped in undoable tasks and participate in the global FieldWorks undo/redo stack. |
| AdvancedEntry spike | [AdvancedEntryEditSession.cs](Src/LexText/AdvancedEntry.Avalonia/Services/AdvancedEntryEditSession.cs) uses a concrete save/cancel session. Local UI currently exposes save/cancel, not a full undo/redo coordinator. |
| Tests | Edit-session tests characterize save/cancel/nested-session behavior, but do not yet prove global undo/redo after save. |

`IUndoRedoCoordinator` is a proposed Phase 3/8 seam. It is not current implementation.

## Contract

The migrated region MUST:

1. Wrap committed model changes in the same undo/redo mechanism expected by LCModel and the FieldWorks shell.
2. Avoid creating a separate Avalonia-only undo history for committed LCModel state.
3. Keep transient text-edit undo local to the focused text control only until the edit commits.
4. Disable or route global undo/redo commands while a session is in a state where replay would corrupt the draft/LCModel boundary.
5. Refresh the migrated region after external undo/redo without losing focus when possible.

## Routing Model

| Command Source | First-Slice Behavior | Shell-Integrated Behavior |
|---|---|---|
| `Ctrl+Z` / `Ctrl+Y` inside text control | Let the control handle local text undo while focus remains in an uncommitted editor. | Same, unless shell command routing explicitly owns the shortcut. |
| Save command | Commits through edit session and creates one LCModel undoable action. | Also updates shell command state and dirty indicators. |
| Global Undo/Redo menu/toolbar | Out of scope for preview spike. | Routed through an `IUndoRedoCoordinator` that delegates to LCModel action handler and refreshes the region. |
| Cancel | Rolls back active session and must not create a committed undo action. | Same, with shell state notification. |

## Required Tests

| Test Area | Cases |
|---|---|
| Commit grouping | Multiple field edits saved together produce one undoable action. |
| Global undo | After save, global undo restores all changed LCModel values and refreshes the Avalonia view. |
| Global redo | Redo reapplies saved values and refreshes without duplicating sequence items. |
| Cancel | Cancel restores values and does not add an undoable action. |
| Focus | Undo/redo after save keeps or restores a sensible focus target; destroyed editors do not receive focus. |
| Dirty state | Command enablement reflects clean, dirty, saving, canceled, and faulted states. |
| External mutation | Legacy DataTree or parser mutation during/after session is detected and refreshed or rejected safely. |

## Phase Gates

| Phase | Gate |
|---|---|
| Phase 3 | Extract coordinator only after edit-session lifecycle tests exist. |
| Phase 5 | First editable slice has save/cancel/global undo/global redo tests on real fields. |
| Phase 8 | Shell command bridge proves XCore menu/toolbar/keyboard routing against the same coordinator. |
| Phase 9 | Default-enabled region passes external undo/redo refresh tests and retains rollback. |