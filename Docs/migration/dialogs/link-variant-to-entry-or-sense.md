# Link Variant to Entry or Sense (`LinkVariantToEntryOrSense`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.LexText.Controls.LinkVariantToEntryOrSense` (`Src/LexText/LexTextControls/LinkVariantToEntryOrSense.cs`) |
| **Area** | Lexicon |
| **Type** | dialog |
| **Primitive** | search+list |
| **State** | coexist |
| **Phase** | 1 |
| **Canonical reference** | EntryGoDialog |
| **JIRA** | LT-XXXXX |

## What it is
Search for and link a variant to its main entry/sense, with extra variant-back-reference logic.

## Notes / gotchas
- Subclass of LinkEntryOrSenseDlg; instantiated directly (e.g. from the interlinear Sandbox combo handlers) AND subclassed by InsertVariantDlg. State=coexist via the LinkEntryOrSenseDlg/LcmLinkEntryOrSenseDialogLauncher path. Adds m_fBackRefToVariant logic.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.

