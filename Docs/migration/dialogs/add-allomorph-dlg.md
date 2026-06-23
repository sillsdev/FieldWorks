# Add Allomorph (`AddAllomorphDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.AddAllomorphDlg` (`Src/LexText/LexTextControls/AddAllomorphDlg.cs`) |
| **Area** | Lexicon |
| **Type** | dialog |
| **Primitive** | search+list |
| **State** | coexist |
| **Phase** | 1 |
| **Canonical reference** | EntryGoDialog |
| **JIRA** | LT-XXXXX |

## What it is
Search for an existing entry and add the typed form to it as an allomorph.

## Notes / gotchas
- Subclass of EntryGoDlg (base skipped; subclass is a distinct shipping dialog). State=coexist: Avalonia path is LcmAddAllomorphDialogLauncher under UIMode=New.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

