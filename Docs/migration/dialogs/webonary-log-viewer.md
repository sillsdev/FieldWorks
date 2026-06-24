# Webonary Log Viewer (`WebonaryLogViewer`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.WebonaryLogViewer` (`Src/xWorks/WebonaryLogViewer.cs`) |
| **Area** | Dictionary-config |
| **Type** | dialog |
| **Primitive** | TABLE |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog (filterable log grid) |
| **JIRA** | LT-XXXXX |

## What it is
Displays the Webonary upload log in a `DataGridView`, with a status-level filter combo (Full Log / errors / warnings) backed by `WebonaryStatusCondition`.

## Notes / gotchas
- `DataGridView` (not `ListView`) loaded from a log file; filter uses a custom `ComboBoxItem` and a checked-combo (`SIL.Windows.Forms.CheckedComboBox`).
- Read-only viewer; pairs with `UploadToWebonaryDlg`.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
