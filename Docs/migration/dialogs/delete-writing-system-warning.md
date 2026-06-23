# Delete Writing System Warning (`DeleteWritingSystemWarningDialog`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.DeleteWritingSystemWarningDialog` (`Src/FwCoreDlgs/DeleteWritingSystemWarningDialog.cs`) |
| **Area** | App-wide (writing-system management) |
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
| ![delete-writing-system-warning legacy](./images/delete-writing-system-warning-before.png) | ![delete-writing-system-warning avalonia](./images/delete-writing-system-warning-after.png) |
## What it is
A warning dialog used in place of a plain MessageBox because custom text is required for the "Yes" button when deleting a writing system.

## Notes / gotchas
- Confirmation dialog; modal.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
