# Overwrite Existing Project (`OverwriteExistingProject`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.BackupRestore.OverwriteExistingProject` (`Src/FwCoreDlgs/BackupRestore/OverwriteExistingProject.cs`) |
| **Area** | App-wide (backup / restore) |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form→nearest |
| **JIRA** | LT-XXXXX |

## What it is
TBD — confirmation shown during restore when the target project already exists, to confirm overwriting (infer from class name; fill on pickup).

## What it looks like (before / after)
**Live-capture / on-pickup.** This confirmation only appears *mid-restore* (when the target project already
exists), so it can't be opened unattended without actually performing a restore (which would modify a
project — out of scope for read-only capture). Its parent RestoreProjectDlg is captured — see
[restore-project.md](./restore-project.md).

## Notes / gotchas
- Confirmation dialog launched from the Restore-a-Project dialog (RestoreProjectDlg); modal.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
