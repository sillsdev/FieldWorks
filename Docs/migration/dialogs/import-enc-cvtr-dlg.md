# Import Encoding Converter (`ImportEncCvtrDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.DataNotebook.ImportEncCvtrDlg` (`Src/LexText/LexTextControls/DataNotebook/ImportEncCvtrDlg.cs`) |
| **Area** | Notebook |
| **Type** | dialog |
| **Primitive** | plain-form |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | InsertEntryDialog |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![import-enc-cvtr legacy](./images/import-enc-cvtr-before.png) | ![import-enc-cvtr avalonia](./images/import-enc-cvtr-after.png) |
## What it is
Chooses an encoding converter for a given writing system during import.

## Notes / gotchas
- Wraps an encoding-converter chooser (SilEncConverters40).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

