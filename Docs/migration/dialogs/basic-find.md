# Basic Find (`BasicFindDialog`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.BasicFindDialog` (`Src/FwCoreDlgs/BasicFindDialog.cs`) |
| **Area** | App-wide |
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
| ![basic-find legacy](./images/basic-find-before.png) | ![basic-find avalonia](./images/basic-find-after.png) |
## What it is
A no-frills find dialog; all work is done by the caller via events fired by the dialog (implements `IBasicFindView`).

## Notes / gotchas
- View/presenter split (`IBasicFindView`); caller owns the search logic.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
