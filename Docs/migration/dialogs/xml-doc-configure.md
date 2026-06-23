# XML Document Configure (legacy) (`XmlDocConfigureDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.XmlDocConfigureDlg` (`Src/xWorks/XmlDocConfigureDlg.cs`) |
| **Area** | Dictionary-config |
| **Type** | dialog |
| **Primitive** | TREE |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog (tree + detail), but this is a large bespoke screen — see gotchas |
| **JIRA** | LT-XXXXX |

## What it is
The older jtview-layout configuration editor (the predecessor to `DictionaryConfigurationDlg`): builds a checkable `TreeView` (`m_tvParts`) from XML `<part>`/`<layout>` nodes and lets the user configure the Dictionary view. Implements `ILayoutConverter`.

## Notes / gotchas
- LARGE, COMPLEX legacy screen with a checkable parts `TreeView`, content `ListView` (writing systems / relation / complex-form / variant / minor-entry types), and per-node detail editing.
- Tightly coupled to the XML jtview layout model (`<part ref>` traversal, `hideConfig` attributes) and `Common.RootSites`.
- Launches the legacy `DictionaryConfigMgrDlg` (`dictionary-config-mgr.md`) from its Manage button (`:4368`). Likely to RETIRE alongside the legacy config path — confirm which configure dialog survives before investing.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
