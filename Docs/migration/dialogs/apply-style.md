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

## Notes / gotchas
- Views-coupled (references `IVwRootSite`/selection to apply styles to the active view).
- Shares the style-list helper infrastructure with `FwStylesDlg`.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
