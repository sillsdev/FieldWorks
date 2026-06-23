# Morph Type Chooser (`MorphTypeChooser`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.MorphTypeChooser` (`Src/Common/Controls/DetailControls/MorphTypeChooser.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | chooser |
| **Primitive** | TREE |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog |
| **JIRA** | LT-XXXXX |

## What it is
A `SimpleListChooser` subclass that picks a morph type, adding a "Show all types" toggle to filter the hierarchical morph-type list.

## Notes / gotchas
- Hierarchical (extends the tree-based `SimpleListChooser`/`ReallySimpleListChooser`); not flat.
- Adds a `&Show all types` link/button that re-loads the candidate list between the restricted and full type sets.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
