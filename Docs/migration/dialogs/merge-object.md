# Merge Object (`MergeObjectDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FdoUi.Dialogs.MergeObjectDlg` (`Src/FdoUi/Dialogs/MergeObjectDlg.cs`) |
| **Area** | Lexicon |
| **Type** | dialog |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog (pick a merge target from a list) with an owned preview control |
| **JIRA** | LT-XXXXX |

## What it is
Lets the user merge the current object into another object of the same kind: pick the target to merge into, with a preview/summary of the candidate.

## Notes / gotchas
- Uses an owned `FwTextBox` (`m_fwTextBoxBottomMsg`, `SIL.FieldWorks.Common.Widgets.FwTextBox`) for the bottom message — needs an owned-control / styled-text equivalent.
- Destructive operation (merge); confirm undo/transaction semantics on pickup.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
