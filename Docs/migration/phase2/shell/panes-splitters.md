# Panes / splitters (`CollapsingSplitContainer` / `MultiPane` / `PaneBarContainer`)

| | |
|---|---|
| **Key files** | `Src/XCore/CollapsingSplitContainer.cs` (+ `MultiPane.cs`, `PaneBarContainer.cs`) |
| **Area** | Shell |
| **Type** | shell |
| **Primitive** | n/a |
| **State** | legacy |
| **Phase** | 2 |
| **Census stage** | 11d |
| **JIRA** | LT-XXXXX |

## What it is
The content-pane layout primitives: collapsible split containers, multi-pane hosting, and the pane-bar container that frames each tool's editing surface.

## Notes / gotchas
- Reimplement as Avalonia layout (Grid/GridSplitter + pane-bar control); persisted split sizes live in the PropertyTable.
- These host the region/composer content during coexistence — the Avalonia host plugs in here before they retire.
- Deleted at end of coexistence with the legacy shell window.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
