# Main window (`xWindow.cs`)

| | |
|---|---|
| **Key files** | `Src/XCore/xWindow.cs` (2,498 lines, `: Form`) (+ `Src/xWorks/FwXWindow.cs`) |
| **Area** | Shell |
| **Type** | shell |
| **Primitive** | n/a |
| **State** | legacy |
| **Phase** | 2 |
| **Census stage** | 11a |
| **JIRA** | LT-XXXXX |

## What it is
The top-level XCore application window (`: Form`): hosts the mediator/property table, menu/toolbar adapters, sidebar, and the content pane tree; `FwXWindow` is the FieldWorks specialization.

## Notes / gotchas
- Reimplement as the Avalonia main window; the 2,498-line `Form` subclass is the central WinForms coupling point.
- Owns the mediator/PropertyTable instance and broadcasts content-switching — keep the bridge seam working during coexistence.
- Deleted at end of coexistence once the net10 shell window is default.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
