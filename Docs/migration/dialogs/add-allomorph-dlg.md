# Add Allomorph (`AddAllomorphDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.AddAllomorphDlg` (`Src/LexText/LexTextControls/AddAllomorphDlg.cs`) |
| **Area** | Lexicon |
| **Type** | dialog |
| **Primitive** | search+list |
| **State** | coexist |
| **Phase** | 1 |
| **Canonical reference** | EntryGoDialog |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![add-allomorph legacy](./images/add-allomorph-before.png) | ![add-allomorph avalonia](./images/add-allomorph-after.png) |
## What it is
Search for an existing entry and add the typed form to it as an allomorph.

## Notes / gotchas
- Subclass of EntryGoDlg (base skipped; subclass is a distinct shipping dialog). State=coexist: Avalonia path is LcmAddAllomorphDialogLauncher under UIMode=New.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

