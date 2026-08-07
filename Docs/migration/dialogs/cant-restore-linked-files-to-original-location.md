# Cannot Restore Linked Files to Original Location (`CantRestoreLinkedFilesToOriginalLocation`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FdoUi.Dialogs.CantRestoreLinkedFilesToOriginalLocation` (`Src/FdoUi/Dialogs/CantRestoreLinkedFilesToOriginalLocation.cs`) |
| **Area** | App-wide |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form (nearest: OptionsDialog) |
| **JIRA** | LT-XXXXX |

## What it is
Restore-linked-files prompt shown when linked files cannot be restored to their original location; offers the user an alternative location choice.

## Notes / gotchas
- Part of the linked-files restore prompt family (with `FilesToRestoreAreOlder` and `RestoreLinkedFilesToProjectsFolder`) — migrate together; likely radio-option + OK/Cancel.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
