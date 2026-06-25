# Design — Avalonia interlinear (morph-bundle) editor

## Context

The Words `Analyses` tool edits a `WfiWordform`'s analyses. The legacy detail editor is `InterlinearSlice`
hosting a `OneAnalysisSandbox` — an interlinear grid (wordform line over per-morpheme columns of
morph / lex-gloss / grammatical-info) backed by the Sandbox cache, with write-back through
`AnalysisInterlinearRS` (`CollectReferencedMsas` → `UpdateAnalysis` → delete MSAs no surviving sense uses).
This is a net-new control, not a composer slice-stack.

## Goals / non-goals

- Goal: parity — render aligned interlinear for a wordform's analyses; edit each morph-bundle's
  morph/sense/MSA; commit the analysis with the same MSA-prune the Sandbox does; one undoable UOW.
- Goal: FwAvalonia view **LCModel-free** and **native-Views-free** (engine-isolation audit); no Sandbox
  (a WinForms/Views construct) in the Avalonia path.
- Non-goal: parser, approval model, or IText document interlinear.

## Key decisions

1. **No Sandbox in the view.** Replace the Sandbox role with a managed `InterlinearAnalysisModel`
   projection (lines → bundles → {morph, sense, msa} choices, all as LCModel-free DTOs with guids) the
   Avalonia control binds to; the xWorks/Morphology plugin owns the projection (read) and the write-back +
   MSA prune (write) inside the fenced `RegionEditContextBase` UOW (plan-review D3). The view never
   touches LCModel or the Sandbox.
2. **Plug in via the existing seam.** Register a `RegionEditorPlugin` for the interlinear slice class; the
   §20.1.4 class-general composer realizes it. No new `RecordEditView` branch.
3. **Read-only first (W-4), then editable (W-5).** Ship the read-only aligned renderer + virtual-flid
   nested-tree compose to de-risk alignment/projection, then layer per-bundle editing + write-back. This
   is the explicit de-risk ordering from the §20 plan.
4. **Write-back fidelity is the risk.** `UpdateRealAnalysis` + MSA prune (`AnalysisInterlinearRS:285-296`)
   must match legacy exactly — covered by a T4 workflow test on a real cache (edit a bundle → commit →
   the analysis updates and an orphaned MSA is pruned).
5. **Alignment is layout, not a grid control.** Columns align via a measured shared-width pass over the
   morpheme columns (managed Avalonia layout), not a native table — keeps it Skia-rendered + headless-testable.
6. **Nested-tree flids (W-1).** Wordform → Analyses → Bundles is realized by the plugin's projector
   walking the real LCModel sequences (`WfiWordform.AnalysesOC`, `WfiAnalysis.MorphBundlesOS`) into the
   `InterlinearAnalysisModel`; the composer does NOT need new virtual flids — the plugin supplies the model
   directly (consistent with the rule-formula projector). If a header summary-jtview (W-3) needs a composed
   row, it uses the existing jtview/embedded-view lane, not a synthesized flid.
7. **Command routing (W-6).** Words context-menu commands (Go-To-Wordform, etc.) route through the SAME
   `RegionEditorServices`/host-command seam the lexeme editor's row commands use (no new command bus); the
   bridge raises the command key the host already broadcasts via `Publisher`, so no command appears on the
   legacy surface when UIMode=New.

## Risks

- Write-back / MSA-prune divergence from the Sandbox (mitigated by Decision 4 parity workflow test).
- Sandbox semantics leaking expectations into the projection (mitigated by Decision 1 DTO boundary).
- Complex-script alignment in the interlinear columns (HarfBuzz shaping; RTL) — T3 edge tests.
