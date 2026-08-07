# Interlinear Export (`InterlinearExportDialog`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.IText.InterlinearExportDialog` (`Src/LexText/Interlinear/InterlinearExportDialog.cs`) |
| **Area** | Texts&Words |
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
| ![interlinear-export legacy](./images/interlinear-export-before.png) | ![interlinear-export avalonia](./images/interlinear-export-after.png) |
## What it is
Exports interlinear text; subclass of ExportDialog.

## Notes / gotchas
- Subclasses shared ExportDialog (outside this tree); migrate with the ExportDialog family.
- Couples to RootSites (SIL.FieldWorks.Common.RootSites) for view rendering.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

