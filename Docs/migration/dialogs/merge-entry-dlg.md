# Merge Entry (`MergeEntryDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.MergeEntryDlg` (`Src/LexText/LexTextControls/MergeEntryDlg.cs`) |
| **Area** | Lexicon |
| **Type** | dialog |
| **Primitive** | search+list |
| **State** | coexist |
| **Phase** | 1 |
| **Canonical reference** | EntryGoDialog |
| **JIRA** | LT-XXXXX |

## What it is
Search for and select the entry to merge the current entry into.

## Notes / gotchas
- Subclass of EntryGoDlg. State=coexist: Avalonia path is LcmMergeEntryDialogLauncher under UIMode=New. Merge is destructive - preserve confirmation/undo semantics.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

