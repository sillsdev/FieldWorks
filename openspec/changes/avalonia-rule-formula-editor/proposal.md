## Why

`lexical-edit-avalonia-migration` §20 wired Grammar onto the Avalonia surface for every tool whose
detail layout is composed from standard slices (`posEdit`, feature lists, the flat-table editors). The
remaining Grammar tools — phonological/morphological **rule** editing — cannot be composed that way: the
legacy detail surface is a bespoke interactive **root-site grid** (`RegRuleFormulaControl`, ~834 lines +
its `Vc`), not a stack of field slices. The §20 plan's verified review explicitly scoped this as a
genuinely **XL net-new editor**, its own project (cross-cutting review `xcut-review-2026-06-21.json` §C,
plan-review D2/D3), and the §20.1.3 host guard currently shows these tools a safe "unsupported" message
under New UI rather than a broken surface.

This change builds the Avalonia rule-editing family so Grammar reaches full functional parity on the
cross-platform surface: the rule-formula grid (regular + metathesis phonological rules, compound rules),
plus the supporting bespoke cell/segment editors those tools need — IPA symbol, phonological-environment
string, ad-hoc co-prohibition groups, and natural-class selection.

## What Changes

- Add a net-new Avalonia **rule-formula editor** (`RuleFormulaRegionEditor`) — an interactive cell grid
  over a `PhSegmentRule` / `MoCompoundRule` (insert/delete/reorder cells; each cell a phoneme, natural
  class, boundary, or segment slot), the parity of `RegRuleFormulaControl`. LCModel-free view; all cell
  mutations on the rule go through an xWorks plugin/adapter.
- Add the **metathesis** rule editor on the same grid base (`MetaRuleFormulaControl` parity).
- Add the **phonological-rule** detail recursion (`PhSegRuleRHS` right-hand-side structure) so the rule
  tool composes end to end.
- Add the supporting bespoke editors the rule tools embed: **BasicIPASymbol** (derive-on-commit),
  **PhEnvStrRepresentation** (validator + insert toolbar + natural-class chooser), **ad-hoc
  co-prohibition** Key/Other choosers + nested-group lane, and **natural-class** selection.
- Register the rule tools on the Avalonia EDIT surface (`compoundRuleAdvancedEdit`, `naturalClassedit`,
  `phonemeEdit`, `EnvironmentEdit`, `PhonologicalRuleEdit`, `AdhocCoprohibEdit`) as each editor lands;
  remove the §20.1.3 "unsupported" fallback for the registered ones.
- Reuse the §20.1.4 class-general composer + 4-key layout resolution + `RegionEditorPluginRegistry`
  seam built in Phase 1; these editors plug in as `RegionEditorPlugin`s, not new host paths.

## Non-goals

- No change to the rule **execution** engine (XAmple/HC parser), phonological-rule semantics, or the
  LCModel rule data model — this is editor UI only.
- No native Views/Graphite rendering; the grid is a managed Avalonia control (Skia/HarfBuzz text).
- No work on the Words interlinear editor (its own change, `avalonia-interlinear-editor`) or the
  remaining tractable §20 tail (stays in `lexical-edit-avalonia-migration`).
- No removal of the WinForms rule controls while UIMode=New is switchable (deletion belongs to
  `avalonia-end-game`).

## Impact

- New Avalonia editor controls under `Src/Common/FwAvalonia` (+ `*.Avalonia` where Views-free) and their
  LCModel-aware adapters/plugins under `Src/xWorks` / `Src/LexText/Morphology`.
- `RegionEditorPlugins.RegisterBuiltins` gains the rule/segment editors; `LexicalEditSurfaceRegistry`
  gains the rule tool ids.
- Gated behind `UIMode=New` (off by default); WinForms rule editors stay switchable. Engine-isolation
  audit (no native Views/Graphite in FwAvalonia) continues to hold.
