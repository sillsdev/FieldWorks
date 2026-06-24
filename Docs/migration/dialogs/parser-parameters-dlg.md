# Parser Parameters (`ParserParametersDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.ParserParametersDlg` (`Src/LexText/ParserUI/ParserParametersDlg.cs`) |
| **Area** | Grammar |
| **Type** | dialog |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | OptionsDialog |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![parser-parameters legacy](./images/parser-parameters-before.png) | ![parser-parameters avalonia](./images/parser-parameters-after.png) |
## What it is
Edits the active parser's parameters (XML-backed settings).

## Notes / gotchas
- Subclass of ParserParametersBase (base excluded - never instantiated directly).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

