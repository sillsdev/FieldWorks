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

## What it is
Pick a grammatical category (part of speech) from the GOLD master catalog tree and add it to the project.

## Notes / gotchas
- State=coexist: the Avalonia replacement is LcmCreatePartOfSpeechLauncher (UIMode=New), which mirrors this dialog's catalog-load + create-in-project logic exactly. Also reached via MasterCategoryListChooserLauncher and POSPopupTreeManager.
- Hierarchical single-select tree of master categories loaded from the template directory.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

