# Restore Linked Files to Projects Folder (`RestoreLinkedFilesToProjectsFolder`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FdoUi.Dialogs.RestoreLinkedFilesToProjectsFolder` (`Src/FdoUi/Dialogs/RestoreLinkedFilesToProjectsFolder.cs`) |
| **Area** | App-wide |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form (nearest: OptionsDialog) |
| **JIRA** | LT-XXXXX |

## What it is
Restore-linked-files prompt offering to restore linked files into the project's folder (vs another location).

## Notes / gotchas
- Part of the linked-files restore prompt family (with `CantRestoreLinkedFilesToOriginalLocation` and `FilesToRestoreAreOlder`) — migrate together; likely radio-option + OK/Cancel.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
