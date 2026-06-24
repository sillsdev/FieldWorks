# Apply Style (`FwApplyStyleDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.FwApplyStyleDlg` (`Src/FwCoreDlgs/FwApplyStyleDlg.cs`) |
| **Area** | App-wide (styles) |
| **Type** | dialog |
| **Primitive** | plain-form (style list) |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | search+list→EntryGoDialog |
| **JIRA** | LT-XXXXX |

## What it is
Lets the user apply an existing paragraph/character style to the current selection (a lighter sibling of the full Styles dialog).

## What it looks like (before / after)
**Live-capture / on-pickup.** The Format → Apply Style command is **disabled unless there is a live text
selection** to apply a style to (verified live: greyed out in the lexicon tool). The live-UIA menu-capture
harness (`Capture-MenuDialogs.ps1`) skips disabled leaves, so this dialog is captured when its ticket is
worked, from a text-editing context where the command is enabled. Its sibling Styles dialog (FwStylesDlg)
is captured — see [styles.md](./styles.md).

## Notes / gotchas
- Views-coupled (references `IVwRootSite`/selection to apply styles to the active view).
- Shares the style-list helper infrastructure with the full Styles dialog (FwStylesDlg).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
