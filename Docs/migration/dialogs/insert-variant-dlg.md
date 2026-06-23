# Insert Variant (`InsertVariantDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.InsertVariantDlg` (`Src/LexText/LexTextControls/LinkVariantToEntryOrSense.cs`) |
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
| ![insert-variant legacy](./images/insert-variant-before.png) | ![insert-variant avalonia](./images/insert-variant-after.png) |
## What it is
Insert a new variant, reusing LinkVariantToEntryOrSense logic to detect an already-inserted selected variant.

## Notes / gotchas
- Subclass of LinkVariantToEntryOrSense (same file, line 427). Source TODO flags refactor of m_fBackRefToVariant logic.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

