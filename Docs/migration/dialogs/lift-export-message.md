# LIFT Export Message (`LiftExportMessageDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.LiftExportMessageDlg` (`Src/xWorks/LiftExportMessageDlg.cs`) |
| **Area** | Lexicon |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | plain-form (nearest: OptionsDialog) |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![lift-export-message legacy](./images/lift-export-message-before.png) | ![lift-export-message avalonia](./images/lift-export-message-after.png) |
## What it is
Informational message dialog shown after a LIFT export.

## Notes / gotchas
- Simple message-and-button plain-form; TBD — confirm exact message/buttons on pickup.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
