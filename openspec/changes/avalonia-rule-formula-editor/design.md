# Design — Avalonia rule-formula editor family

## Context

The Grammar rule tools edit a structured rule object (`PhRegularRule`/`PhMetathesisRule` via
`PhSegmentRule`, `MoCompoundRule`) whose legacy editor is `RuleFormulaControl`/`RegRuleFormulaControl` —
an `XmlView` root-site interactive grid where each *cell* is a phoneme, natural class, boundary marker,
or a structural slot, and the user inserts/deletes/reorders cells and edits per-cell content. This is
fundamentally unlike the slice-stack detail surface the §20 composer builds, so it is a net-new control,
not a composer lane.

## Goals / non-goals

- Goal: functional parity with the WinForms rule editors — view, insert, delete, reorder, edit cells;
  commit as one undoable UOW; honor the same per-cell choosers (natural class, phoneme, boundary).
- Goal: keep the FwAvalonia view **LCModel-free** and **native-Views-free** (engine-isolation audit).
- Non-goal: re-implementing rule *evaluation* or changing rule semantics/data model.

## Key decisions

1. **Editor lives in FwAvalonia; LCModel writes in an xWorks plugin (plan-review D3).** The Avalonia
   `RuleFormulaRegionEditor` renders an observable cell-model and raises intent events (insert cell at i,
   delete cell i, set cell i to {phoneme|NC|boundary}); a `RegionEditorPlugin` in xWorks/Morphology owns
   ALL `PhSegmentRule`/`MoCompoundRule` reads + mutations inside the fenced `RegionEditContextBase`
   session. The 834-line root-site control is the highest LCModel-leak risk — this split is mandatory.
2. **Plug in via the existing seam, not a new host path.** Register a `RegionEditorPlugin` for each rule
   slice class (mirroring the MSA/feature launcher plugins); the §20.1.4 class-general composer realizes
   it. No new `RecordEditView` branch.
3. **Cell model is a managed projection.** The plugin projects the rule into an ordered list of
   `RuleCell { Kind, DisplayText, TargetGuid }` (LCModel-free DTO, like the region run model); the view
   binds to it. Commit maps cell edits back to LCModel ops. This keeps the view testable headless without
   a cache.
4. **Supporting editors are independent plugins** reused by the grid's cell choosers: BasicIPASymbol
   (derive-on-commit string), PhEnvStrRepresentation (validated vernacular string + insert toolbar + NC
   chooser), natural-class chooser, ad-hoc Key/Other choosers + nested-group lane.
5. **Build read-only first, then editable.** Ship the read-only cell renderer + registration to de-risk
   the projection/parity, then layer insert/delete/reorder/edit. (Mirrors the interlinear change's W-4→W-5.)
   The read-only grid (task 1.3) is a **static Avalonia control bound to the `RuleFormulaModel` DTO** — not
   a composed region row; the editable phase (task 2.1+) registers the `RegionEditorPlugin` that forwards
   cell-intent events back through the region's existing fenced `LcmRegionEditSession` (the plugin reuses
   the session via `IRegionEditContext`; it never opens its own).

## Risks

- LCModel leak into the view (mitigated by Decision 1 + the engine-isolation audit test).
- Cell-model fidelity vs the legacy `Vc` (mitigated by a parity projection test per rule kind).
- Reorder/undo granularity (one UOW per gesture) — covered by T4 workflow tests.
