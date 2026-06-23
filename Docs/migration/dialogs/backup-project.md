# Backup Project (`BackupProjectDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.BackupRestore.BackupProjectDlg` (`Src/FwCoreDlgs/BackupRestore/BackupProjectDlg.cs`) |
| **Area** | App-wide (backup / restore) |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form→nearest |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![backup-project legacy](./images/backup-project-before.png) | ![backup-project avalonia](./images/backup-project-after.png) |
## What it is
The Backup Project dialog — configures and runs a backup of the current project (implements `IBackupProjectView`).

## Notes / gotchas
- View/presenter split (`IBackupProjectView` / `BackupProjectPresenter`).
- May launch `ChangeDefaultBackupDir` for the backup-directory option.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
