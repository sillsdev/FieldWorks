# Help About (`FwHelpAbout`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.FwHelpAbout` (`Src/FwCoreDlgs/FwHelpAbout.cs`) |
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
| ![help-about legacy](./images/help-about-before.png) | ![help-about avalonia](./images/help-about-after.png) |
## What it is
The FieldWorks Help > About dialog (previously HelpAboutDlg in AfDialog.cpp) — shows version/build/credits.

## Notes / gotchas
- Read-only informational dialog; modal.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
