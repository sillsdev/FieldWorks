# Delete Project (`FwDeleteProjectDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.FwDeleteProjectDlg` (`Src/FwCoreDlgs/FwDeleteProjectDlg.cs`) |
| **Area** | App-wide (project management) |
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
| ![fw-delete-project legacy](./images/fw-delete-project-before.png) | ![fw-delete-project avalonia](./images/fw-delete-project-after.png) |
## What it is
TBD — confirm/delete a FieldWorks project (infer from class name; fill on pickup).

## Notes / gotchas
- Confirmation-style project deletion dialog; modal.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
