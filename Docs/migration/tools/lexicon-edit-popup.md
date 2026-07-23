# Lexicon Edit Popup (`lexiconEditPopup`)

| | |
|---|---|
| **Tool id** | `lexiconEditPopup` |
| **Area** | Lexicon |
| **Type** | tool-screen |
| **Surface** | edit (RecordEditView/detail) |
| **Primitive** | detail (composed editor) |
| **State** | legacy (default) |
| **Phase** | 1 |
| **Canonical reference** | detail editor -> Lexicon Edit entry pane (FullEntryRegionComposer) |
| **JIRA** | LT-XXXXX |

## What it looks like
![Lexicon Edit Popup (`lexiconEditPopup`) (Sena 3, Legacy)](./images/lexicon-edit-popup-01.png)

## What it is
Test/popup composite: dictionary preview over a RecordEditView edit pane for the current entry.

## Notes / gotchas
- NOT a normal navigable screen -- embedded popup/test edit composite (id 'TestEditMulti'); not wired into any sidebar tool list.
- PaneBarContainer > MultiPane: Dictionary preview pane + RecordEditView edit pane.
- Included only for inventory completeness.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
