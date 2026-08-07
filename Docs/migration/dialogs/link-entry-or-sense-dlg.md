# Link Entry or Sense (`LinkEntryOrSenseDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.LinkEntryOrSenseDlg` (`Src/LexText/LexTextControls/LinkEntryOrSenseDlg.cs`) |
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
| ![link-entry-or-sense legacy](./images/link-entry-or-sense-before.png) | ![link-entry-or-sense avalonia](./images/link-entry-or-sense-after.png) |
## What it is
Search for and select an existing lexical entry or sense to link to.

## Notes / gotchas
- Subclass of EntryGoDlg. State=coexist: Avalonia path is LcmLinkEntryOrSenseDialogLauncher under UIMode=New. Base class for LinkVariantToEntryOrSense.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

