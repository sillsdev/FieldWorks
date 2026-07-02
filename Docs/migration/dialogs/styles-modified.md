# Styles Modified (`FwStylesModifiedDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.FwStylesModifiedDlg` (`Src/FwCoreDlgs/FwStylesModifiedDlg.cs`) |
| **Area** | App-wide (styles) |
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
| ![fw-styles-modified legacy](./images/fw-styles-modified-before.png) | ![fw-styles-modified avalonia](./images/fw-styles-modified-after.png) |
## What it is
A message box displayed when the stylesheet has been modified, to notify the user that they may want to check their styles.

## Notes / gotchas
- Subclass of FwUpdateReportDlg (report-style dialog).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
