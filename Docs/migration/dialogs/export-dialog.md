# Export (`ExportDialog`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.ExportDialog` (`Src/xWorks/ExportDialog.cs`) |
| **Area** | Lexicon |
| **Type** | dialog |
| **Primitive** | TABLE |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog (list of export formats + Export action) |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![notebook-export legacy](./images/notebook-export-before.png) | ![notebook-export avalonia](./images/notebook-export-after.png) |
## What it is
XML-configurable export chooser: lists available export formats/targets in a `ListView` (`m_exportList`, columns data/format/extension/filter/description), the user picks one and runs the export (default is the main lexicon export). Subclassed for other areas (see `NotebookExportDialog`).

## Notes / gotchas
- Base class is instantiated directly (main lexicon export) AND subclassed — migrate as a parameterised/overridable surface, not a one-off.
- Driven by an XML configuration file (`ConfigurationFilePath`); export options are data-driven, not hardcoded — preserve the config-file contract.
- `ListView` master list with column sorting; uses `Common.RootSites` (review for any Views coupling on the export path).
- Standard FXT export pipeline by default; subclasses override `ConfigureItem`/the export process.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
