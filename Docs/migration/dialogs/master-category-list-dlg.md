# Master Category List (`MasterCategoryListDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.MasterCategoryListDlg` (`Src/LexText/LexTextControls/MasterCategoryListDlg.cs`) |
| **Area** | Grammar |
| **Type** | dialog |
| **Primitive** | TREE |
| **State** | coexist |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog |
| **JIRA** | LT-XXXXX |

## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![master-category-list legacy](./images/master-category-list-before.png) | ![master-category-list avalonia](./images/master-category-list-after.png) |
## What it is
Pick a grammatical category (part of speech) from the GOLD master catalog tree and add it to the project.

## Notes / gotchas
- State=coexist: the Avalonia replacement is LcmCreatePartOfSpeechLauncher (UIMode=New), which mirrors this dialog's catalog-load + create-in-project logic exactly. Also reached via MasterCategoryListChooserLauncher and POSPopupTreeManager.
- Hierarchical single-select tree of master categories loaded from the template directory.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

