# Export Translated Lists (`ExportTranslatedListsDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.ExportTranslatedListsDlg` (`Src/xWorks/ExportTranslatedListsDlg.cs`) |
| **Area** | Lists |
| **Type** | dialog |
| **Primitive** | MULTI-SELECTOR |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog (checked multi-select of lists + writing systems) |
| **JIRA** | LT-XXXXX |

## What it is
Lets the user select specific lists to export and the specific writing systems to include, via two checkbox `ListView`s (`m_lvLists`, `m_lvWritingSystems`).

## Notes / gotchas
- Two parallel checkbox lists (lists + writing systems) using `ListView.CheckedItems`; column widths are computed from list width at load — preserve checked-item collection semantics.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
