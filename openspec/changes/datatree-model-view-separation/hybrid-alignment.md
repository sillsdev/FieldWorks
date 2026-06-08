# Hybrid Alignment: DataTree split as the first migrated region

This change (`datatree-model-view-separation`) is sequenced by `avalonia-migration-roadmap` as
**Phase 1 — the first concrete migrated region** of the Lexical Edit Avalonia program
(`lexical-edit-avalonia-migration`). It is no longer a standalone end state that "stops at the
abstraction boundary"; it is the swap point that the program's two-adapter feature flag selects.

## What changes about this plan's framing

The four phases (characterization tests → partial split → extract collaborators → model/view split)
are unchanged. What changes is where the boundary types connect:

| This change (Plan A) | Aligns to (program / Plan B) |
|----------------------|------------------------------|
| `SliceSpec` | A concrete realization of the **typed view-definition node**. Keep it lightweight; it is the DataTree region's instance of the program IR, not a competing type. |
| `IDataTreeView` | One of the **two adapters** selected by the program's feature flag (`FW_AVALONIA_LEXEDIT`). `DataTree` (WinForms) and `AvaloniaDataTreeView` both implement it. |
| `DataTreeModel` | Feeds the program's `ILexicalEditorRegistry`/refresh seams; it is UI-agnostic and shared by both adapters. |
| Phase 0 characterization tests | Extend the program's **parity automation** (semantic snapshot + density), so the same baseline gates the Avalonia view. |

## Sequencing and gates

- **Enter** this region only after the POC spike (`lexical-edit-avalonia-poc-spike`) passes **Gate 0**
  (host bridge proven, density acceptable, dual-run flag works).
- **Exit** this region at **Gate 1**: `AvaloniaDataTreeView` consumes the same
  `DataTreeModel`/`SliceSpec` as WinForms, is selected by the flag, matches the semantic + density
  baseline within tolerance, and instantiates no native Views or Graphite at runtime.
- WinForms `DataTree` stays the **default** for this region until Gate 1 passes.

## Practical effect on Phase 3

Phase 3 (model/view split) should produce `IDataTreeView` and `SliceSpec` with the program seams in
mind: do not invent a parallel boundary vocabulary, and leave a clear seam for
`AvaloniaDataTreeView : IDataTreeView` to be added behind the flag. The previously-open Phase 3
questions (SliceFactory location, `DummyObjectSlice` lazy expansion, `StTextDataTree` override) are
resolved in favor of whatever keeps `SliceSpec` a faithful concrete instance of the program's typed
node.

See `Docs/avalonia-migration-approach-comparison.md` for the full rationale and
`openspec/changes/avalonia-migration-roadmap/design.md` for the master sequence and gate definitions.
