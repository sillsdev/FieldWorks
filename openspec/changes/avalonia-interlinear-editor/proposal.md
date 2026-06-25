## Why

`lexical-edit-avalonia-migration` §20 wired the Words **browse/list** surface (Analyses list, bulk-edit
wordforms) onto Avalonia, but the Words Analyses **edit/detail** pane is dominated by `InterlinearSlice`
— a Sandbox-backed interlinear morph-bundle editor (`OneAnalysisSandbox`, `AnalysisInterlinearRS`), not a
stack of field slices. The §20 plan's verified review scoped this as a genuinely **XL net-new editor**,
its own project (`xcut-review-2026-06-21.json` §C; plan-review D2/D3), and the §20.1.3 host guard shows
`Analyses` edit a safe "unsupported" message under New UI today.

This change builds the Avalonia interlinear editor so the Words Analyses tool reaches full functional
parity on the cross-platform surface: render a wordform's analyses as aligned interlinear (word →
morphemes → glosses → grammatical info), and edit the morph-bundle analysis with write-back and the
correct MSA cleanup the legacy Sandbox performs.

## What Changes

- Add a net-new Avalonia **interlinear renderer** — aligned columns for the wordform line, morpheme
  breakdown, lex-gloss, and grammatical-info, over `WfiWordform`/`WfiAnalysis`/`WfiMorphBundle`. Shipped
  **read-only first** (W-4) to de-risk the layout/alignment and the virtual-flid projection.
- Add the **editable morph-bundle** editor (W-5): the Sandbox role in Avalonia — choose/edit each
  bundle's morph, sense, and MSA; write the analysis back; **prune** MSAs no surviving sense uses
  (`AnalysisInterlinearRS` `CollectReferencedMsas`/`UpdateAnalysis` parity).
- Add the `WfiWordform` **nested-tree compose** (virtual flids for analyses/bundles) and the
  **summary-jtview** header lane (W-1, W-3 — W-3 corrects an over-rated "handled" in the §20 plan).
- Wire the `SpellingStatusChanged` side-effect into the option setter (W-2), and bridge the Words
  context-menu commands to the Avalonia surface (W-6, incl. Go-To-Wordform).
- Register the `Analyses` tool on the Avalonia EDIT surface when the editor lands; remove the §20.1.3
  "unsupported" fallback for it.
- Reuse the Phase-1 class-general composer + `RegionEditorPluginRegistry` + fenced-UOW edit-context seam;
  the interlinear editor plugs in as a `RegionEditorPlugin`, not a new host path.

## Non-goals

- No change to the parser (XAmple/HC), the analysis/approval model, or the interlinear-text (IText)
  document tools — this is the wordform **Analyses** morph-bundle editor only.
- No native Views/Graphite rendering; the interlinear is a managed Avalonia control (Skia/HarfBuzz).
- No work on the Grammar rule editors (its own change, `avalonia-rule-formula-editor`) or the tractable
  §20 tail (stays in `lexical-edit-avalonia-migration`).
- No removal of the WinForms `InterlinearSlice`/Sandbox while UIMode=New is switchable (deletion belongs
  to `avalonia-end-game`).

## Impact

- New Avalonia interlinear controls under `Src/Common/FwAvalonia` and their LCModel-aware
  analysis/morph-bundle adapter under `Src/xWorks` / `Src/LexText/Morphology`.
- `RegionEditorPlugins.RegisterBuiltins` gains the interlinear plugin; `LexicalEditSurfaceRegistry` gains
  `Analyses`.
- Gated behind `UIMode=New` (off by default); WinForms interlinear stays switchable. Engine-isolation
  audit (no native Views/Graphite in FwAvalonia) continues to hold.
