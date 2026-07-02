# Sidebar / navigation (`SilSidePane` / `OutlookBar`)

| | |
|---|---|
| **Key files** | `Src/XCore/SilSidePane/OutlookBar.cs` (+ `OutlookBarButton.cs`, the `SilSidePane/` controls) |
| **Area** | Shell |
| **Type** | shell |
| **Primitive** | n/a |
| **State** | legacy |
| **Phase** | 2 |
| **Census stage** | 11d |
| **JIRA** | LT-XXXXX |

## What it is
The left-hand area/tool navigation sidebar: an Outlook-bar-style stack of buttons (`SilSidePane`) driving the active area and tool selection.

## Notes / gotchas
- Reimplement as an Avalonia navigation control; selection state flows through the mediator/PropertyTable.
- Custom-drawn WinForms `OutlookBar` widgetry; no direct Avalonia equivalent — rebuild rather than wrap.
- Deleted at end of coexistence with the legacy shell window.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
