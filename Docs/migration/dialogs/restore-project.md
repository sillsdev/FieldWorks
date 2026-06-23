# Restore Project (`RestoreProjectDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.BackupRestore.RestoreProjectDlg` (`Src/FwCoreDlgs/BackupRestore/RestoreProjectDlg.cs`) |
| **Area** | App-wide (backup / restore) |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form→nearest |
| **JIRA** | LT-XXXXX |

## What it is
The Restore Project dialog — picks a backup and restores it into a project.

## Notes / gotchas
- Driven by `RestoreProjectPresenter`.
- May surface `OverwriteExistingProject` when the restore target already exists.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
