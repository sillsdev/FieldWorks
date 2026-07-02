# Discourse Export (`DiscourseExportDialog`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Discourse.DiscourseExportDialog` (`Src/LexText/Discourse/DiscourseExportDialog.cs`) |
| **Area** | Texts&Words |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | InsertEntryDialog |
| **JIRA** | LT-XXXXX |

## What it is
Exports the discourse constituent chart; subclass of ExportDialog.

## Notes / gotchas
- Subclasses shared ExportDialog (in FwControls, outside this tree) - migrate alongside the InterlinearExportDialog/ExportDialog family.
- Source comment notes considerable overlap with InterlinearExportDialog; common code could move down to ExportDialog.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

