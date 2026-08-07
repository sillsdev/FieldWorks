# Semantic Domains Chooser (`SemanticDomainsChooser`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.SemanticDomainsChooser` (`Src/Common/Controls/DetailControls/SemanticDomainsChooser.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | chooser |
| **Primitive** | TREE |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog |
| **JIRA** | LT-XXXXX |

## What it is
A standalone `Form` for selecting semantic domains: shows the semantic-domain hierarchy in a TreeView plus a search/suggest ListView, used when assigning semantic domains to a sense.

## Notes / gotchas
- Multi-select against a hierarchical possibility list (semantic domains); displays both a TreeView and a ListView (see `SemanticDomainSelectionUtility`).
- Has a search/suggest panel separate from the tree — selecting in one must keep the other in sync.
- Custom `DomainNode : LabelNode` for domain display formatting.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
