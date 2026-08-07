# SFM -> Texts & Words Mapping (`SfmToTextsAndWordsMappingDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.SfmToTextsAndWordsMappingDlg` (`Src/LexText/LexTextControls/SfmToTextsAndWordsMappingBaseDlg.cs`) |
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
| ![sfm-to-texts-and-words-mapping legacy](./images/sfm-to-texts-and-words-mapping-before.png) | ![sfm-to-texts-and-words-mapping avalonia](./images/sfm-to-texts-and-words-mapping-after.png) |
## What it is
Maps an SFM marker to a Texts & Words destination during interlinear SFM import.

## Notes / gotchas
- Uses encoding-converter (SilEncConverters40) + writing-system selection. Sibling of the DataNotebook import-mapping dialogs.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

