# Project Location (`ProjectLocationDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.ProjectLocationDlg` (`Src/FwCoreDlgs/ProjectLocationDlg.cs`) |
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
| ![project-location legacy](./images/project-location-before.png) | ![project-location avalonia](./images/project-location-after.png) |
## What it is
Supports controlling the location and sharing of the project folder.

## Notes / gotchas
- Modal; folder/path selection plus sharing options.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
