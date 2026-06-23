# Insert Record (`InsertRecordDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.InsertRecordDlg` (`Src/LexText/LexTextControls/InsertRecordDlg.cs`) |
| **Area** | Notebook |
| **Type** | dialog |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | InsertEntryDialog |
| **JIRA** | LT-XXXXX |

## What it is
Creates a new Data Notebook record (Notebook counterpart of InsertEntryDlg).

## Notes / gotchas
- Owned FW writing-system-aware title control; writes through LcmModel.Infrastructure (UnitOfWork).
- Closest sibling to the already-migrated InsertEntryDialog - copy that pattern.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

