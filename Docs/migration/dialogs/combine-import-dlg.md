# Combine (FLEx) Import (`CombineImportDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.CombineImportDlg` (`Src/LexText/LexTextControls/CombineImportDlg.cs`) |
| **Area** | Lexicon |
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
| ![combine-import legacy](./images/combine-import-before.png) | ![combine-import avalonia](./images/combine-import-after.png) |
## What it is
Imports a project sent from The Combine (web word-collection tool).

## Notes / gotchas
- Implements IFwExtension. Uses Ionic.Zip + FileDialog wrappers.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

