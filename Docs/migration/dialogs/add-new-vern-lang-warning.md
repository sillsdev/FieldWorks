# Add New Vernacular Language Warning (`AddNewVernLangWarningDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.AddNewVernLangWarningDlg` (`Src/FwCoreDlgs/AddNewVernLangWarningDlg.cs`) |
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
| ![add-new-vern-lang-warning legacy](./images/add-new-vern-lang-warning-before.png) | ![add-new-vern-lang-warning avalonia](./images/add-new-vern-lang-warning-after.png) |
## What it is
A warning to dissuade users from adding multiple vernacular languages when they usually want multiple writing systems of the same vernacular language.

## Notes / gotchas
- Confirmation/warning dialog; modal.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
