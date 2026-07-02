# Master Category List Chooser Launcher (`MasterCategoryListChooserLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.MasterCategoryListChooserLauncher` (`Src/LexText/LexTextControls/MSAPopupTreeManager.cs`) |
| **Area** | Grammar |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog |
| **JIRA** | LT-XXXXX |

## What it is
Idle-time launcher that opens MasterCategoryListDlg to choose a grammatical category for a sense (added to avoid LT-11548 dispose race in MSAPopupTreeManager).

## Notes / gotchas
- Defined inside MSAPopupTreeManager.cs (line 489). Opens MasterCategoryListDlg (which has its own Avalonia coexist path via LcmCreatePartOfSpeechLauncher).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

