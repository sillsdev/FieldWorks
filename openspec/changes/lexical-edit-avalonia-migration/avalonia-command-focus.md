# Avalonia Command and Focus Plan

Command and focus behavior must be built in two layers: local first-slice behavior that can run in the preview host, then FieldWorks shell/XCore routing for production integration.

## Current State

| Item | Source | Current Behavior |
|---|---|---|
| Local key bindings | `010-advanced-entry-preview-prototype` | Prototype has local `Ctrl+S` save and `Escape` cancel bindings. |
| View-model commands | `010-advanced-entry-preview-prototype` | Prototype has save/cancel commands and close request callback. |
| Shell bridge | Not implemented | No current `IXCoreCommandBridge` or production mediator adapter. |

## First-Slice Local Contract

The preview-host and first editable slice must support:

- Keyboard save/cancel commands.
- Tab/Shift+Tab navigation in layout order.
- Focus restoration after validation failure, refresh, save failure, and popup close.
- Stable automation names/IDs for controls where Avalonia exposes them.
- No dependency on XCore mediator or WinForms message routing.

## Shell-Phase Contract

When integrated into FieldWorks, the migrated region must additionally:

- Route menu, toolbar, and accelerator commands through XCore without duplicating command state.
- Keep global undo/redo, save, cancel, refresh, and navigation enablement in sync with active edit-session state.
- Avoid stealing shortcuts from focused text controls when local text editing should handle them.
- Return focus to the shell/record list predictably when the migrated region closes or rolls back.

## Required Tests

| Test Area | Cases |
|---|---|
| Local shortcuts | `Ctrl+S` invokes save once; `Escape` invokes cancel once; disabled commands do not execute. |
| Focus order | Tab order matches Presentation IR order and legacy baseline for selected fixture. |
| Validation focus | Save with blocking error focuses first invalid materialized node and exposes error metadata. |
| Refresh focus | Refresh/rebuild keeps focus on equivalent node when possible; otherwise chooses documented fallback. |
| Popup/chooser focus | Opening and closing chooser/popup restores focus and selection/caret. |
| Text control ownership | Text-edit shortcuts remain local until command bridge explicitly routes them. |
| Shell bridge | XCore menu/toolbar/keyboard command state matches view-model/session state. |

## Architecture Notes

- Keep first-slice commands as view-model commands so headless tests can exercise them.
- Introduce a shell command bridge only in the integration phase, behind an interface owned by the host composition layer.
- Do not route commands by directly referencing WinForms controls from Avalonia production code.
- Focus keys should be stable Presentation IR node IDs, not visual tree indexes.

## Phase Gates

| Phase | Gate |
|---|---|
| Phase 5 | Local command/focus tests pass for first editable slice. |
| Phase 6 | Accessibility and keyboard traversal evidence exists for selected fixture. |
| Phase 8 | Shell bridge tests prove XCore command routing without breaking preview-host isolation. |