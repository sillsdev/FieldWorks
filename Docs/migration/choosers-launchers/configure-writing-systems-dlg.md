# Configure Writing Systems (`ConfigureWritingSystemsDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.ConfigureWritingSystemsDlg` (`Src/Common/Controls/DetailControls/ConfigureWritingSystemsDlg.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | dialog |
| **Primitive** | MULTI-SELECTOR |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog (multi-select checklist) |
| **JIRA** | LT-XXXXX |

## What it is
Used by `MultiStringSlice` to choose *which* writing systems a multi-string slice displays (a checklist of available WSs). Distinct from `writing-system-properties.md`, which edits a single WS's properties — this one only selects the set of WSs shown.

## Notes / gotchas
- NOT the WS properties editor; it is a WS visibility selector for a slice. Keep the two migration tickets separate.
- Result feeds back into the slice's displayed-WS list; verify ordering and persistence of the selection.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
