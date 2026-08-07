# Summary Dialog Find in Lexicon (`SummaryDialogForm`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FdoUi.Dialogs.SummaryDialogForm` (`Src/FdoUi/Dialogs/SummaryDialogForm.cs`) |
| **Area** | Lexicon |
| **Type** | dialog |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | InsertEntryDialog (owned native-render summary control inside a dialog) |
| **JIRA** | LT-XXXXX |

## What it is
Launched from TE's "Find In Lexicon" when a matching lexical entry exists: displays a summary of the entry with buttons to find other similar entries or to open Flex on the relevant entry.

## Notes / gotchas
- `internal` class but a real user-facing dialog (not a sub-control).
- `using SIL.FieldWorks.Common.RootSites` -- review for a native-render summary view (owned control).
- UNUSUAL modal pattern: after `ShowDialog`, the caller must test `ShouldLink` and call `LinkToLexicon()` only AFTER the dialog fully closes (see LT-3461) -- closing first prevents TE jumping in front of Flex. Also test `OtherButtonClicked`. Preserve this close-then-link ordering.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
