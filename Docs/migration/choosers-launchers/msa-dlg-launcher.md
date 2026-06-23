# MSA Dialog Launcher (`MSADlgLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.LexEd.MSADlgLauncher` (`Src/LexText/Lexicon/MSADlgLauncher.cs`) |
| **Area** | Lexicon |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | coexist |
| **Phase** | 1 |
| **Canonical reference** | InsertEntryDialog |
| **JIRA** | LT-XXXXX |

## What it is
ButtonLauncher slice that opens the Create Grammatical Info (MSA) dialog from a sense's MSA slice.

## Notes / gotchas
- State=coexist: in UIMode=New it calls LcmMsaCreatorDialogLauncher.Show seeded from the existing MSA; Legacy keeps WinForms MsaCreatorDlg. The launcher (slice button) is a distinct component from the MsaCreatorDlg it opens.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

