# Add New User (`AddNewUserDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.AddNewUserDlg` (`Src/FwCoreDlgs/AddNewUserDlg.cs`) |
| **Area** | App-wide (user properties) |
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
| ![add-new-user legacy](./images/add-new-user-before.png) | ![add-new-user avalonia](./images/add-new-user-after.png) |
## What it is
Used by the User Properties dialog when the Add button is clicked, to add a new user.

## Notes / gotchas
- Child of `FwUserProperties`, which is itself largely obsolete (see notes there).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
