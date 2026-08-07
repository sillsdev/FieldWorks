# Export Semantic Domains (`ExportSemanticDomainsDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.ExportSemanticDomainsDlg` (`Src/xWorks/ExportSemanticDomainsDlg.cs`) |
| **Area** | Lists |
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
| ![export-semantic-domains legacy](./images/export-semantic-domains-before.png) | ![export-semantic-domains avalonia](./images/export-semantic-domains-after.png) |
## What it is
Options dialog for exporting the semantic-domains list: choose the writing system and whether to show missing translations with English shown in red.

## Notes / gotchas
- Small options form; the "English in red" checkbox enablement depends on the chosen WS (`m_EnglishInRedCheckBox.Enabled`) — preserve the gating.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
