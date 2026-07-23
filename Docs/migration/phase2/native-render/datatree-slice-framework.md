# DataTree / Slice framework (`DataTree` / `Slice` / `SliceFactory`)

| | |
|---|---|
| **Key files** | `Src/Common/Controls/DetailControls/DataTree.cs`, `Slice.cs`, `SliceFactory.cs` (DetailControls, 73 files) |
| **Area** | Native-render |
| **Type** | native-render |
| **Primitive** | n/a |
| **State** | region-path supersedes |
| **Phase** | 2 |
| **Census stage** | superseded by region path |
| **JIRA** | LT-XXXXX |

## What it is
The legacy detail-editing framework: `DataTree` builds a vertical stack of `Slice` controls from a view definition (`SliceFactory` materializes each field's slice) — the WinForms predecessor of the region/composer path.

## Notes / gotchas
- Phase-2 **retirement target**: the Avalonia region/composer path (`FullEntryRegionComposer` + `FwAvalonia/Region/`) replaces it, not a 1:1 port.
- Frozen during coexistence — no new slice types; new fields go to the region path.
- Large surface (73 DetailControls files); deleted once all detail editing is on the region path.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
