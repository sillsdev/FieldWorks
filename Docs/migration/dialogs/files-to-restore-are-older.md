# Files to Restore Are Older (`FilesToRestoreAreOlder`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FdoUi.Dialogs.FilesToRestoreAreOlder` (`Src/FdoUi/Dialogs/FilesToRestoreAreOlder.cs`) |
| **Area** | App-wide |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form (nearest: OptionsDialog) |
| **JIRA** | LT-XXXXX |

## What it is
Restore-linked-files prompt shown when the files being restored are older than existing files; asks the user how to proceed (overwrite / keep).

## Notes / gotchas
- Part of the linked-files restore prompt family (with `CantRestoreLinkedFilesToOriginalLocation` and `RestoreLinkedFilesToProjectsFolder`) — migrate together; likely radio-option + OK/Cancel.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
