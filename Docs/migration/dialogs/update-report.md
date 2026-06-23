# Update Report (`FwUpdateReportDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.FwUpdateReportDlg` (`Src/FwCoreDlgs/FwUpdateReportDlg.cs`) |
| **Area** | App-wide |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form→nearest |
| **JIRA** | LT-XXXXX |

## What it is
Displays somewhat technical information reporting changes that were applied automatically to a project but which the user might want to review.

## Notes / gotchas
- Intended as an abstract base but kept concrete so the Designer works in derived classes; subclassed by `FwStylesModifiedDlg`. Override `HelpTopicKey` per use.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
