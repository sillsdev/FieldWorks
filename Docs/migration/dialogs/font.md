# Font (`FwFontDialog`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.FwFontDialog` (`Src/FwCoreDlgs/FwFontDialog.cs`) |
| **Area** | App-wide (styles / formatting) |
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
| ![fw-font legacy](./images/fw-font-before.png) | ![fw-font avalonia](./images/fw-font-after.png) |
## What it is
A FieldWorks font picker dialog (implements `IFontDialog`) — choose font family, size, and attributes.

## Notes / gotchas
- Implements `IFontDialog`; hosts owned controls `FwFontAttributes` and `FontFeaturesButton`. Fold those into this dialog's migration.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
