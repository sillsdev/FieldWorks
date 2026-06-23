# Related Words (`RelatedWords`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FdoUi.Dialogs.RelatedWords` (`Src/FdoUi/Dialogs/RelatedWords.cs`) |
| **Area** | Lexicon |
| **Type** | dialog |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | InsertEntryDialog (owned native-render control inside a dialog) |
| **JIRA** | LT-XXXXX |

## What it is
Shows words semantically/lexically related to a selected word (or a "not in dictionary" message), letting the user navigate to a related entry.

## Notes / gotchas
- HARD Views coupling: hosts an embedded `RelatedWordsView : SimpleRootSite` (`Src/FdoUi/Dialogs/RelatedWords.cs:553`) with its own `IVwRootBox`. Migration needs a native-render owned control or a re-implemented related-words view.
- `using SIL.FieldWorks.Common.RootSites`.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
