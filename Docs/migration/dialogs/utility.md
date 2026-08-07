# Utilities (`UtilityDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.UtilityDlg` (`Src/FwCoreDlgs/UtilityDlg.cs`) |
| **Area** | App-wide (tools / utilities) |
| **Type** | dialog |
| **Primitive** | plain-form (list + description panes) |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | search+list→EntryGoDialog |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![utility legacy](./images/utility-before.png) | ![utility avalonia](./images/utility-after.png) |
## What it is
Presents the list of utilities defined in `Language Explorer\Configuration\UtilityCatalogInclude.xml`. Each utility implements `IUtility` and can set explanatory labels describing when it is needed and what it does.

## Notes / gotchas
- Plugin-style: items come from `IUtility` implementations enumerated from XML config.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
