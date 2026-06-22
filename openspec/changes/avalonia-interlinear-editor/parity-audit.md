# Functional-completeness & visual-parity audit — Avalonia interlinear editor

Reference ("before"): the legacy WinForms `AnalysisInterlinearRs` (Sandbox-backed `InterlinearSlice`),
rendered by `InterlinVc` with `kfragSingleInterlinearAnalysisWithLabelsLeftAlign`. The legacy IText
**Analyze** view (user-supplied screenshot) shares the same `InterlinVc` line structure.
"After": `InterlinearRegionEditor` (FwAvalonia), PNGs in `Output/Snapshots/Interlinear-0[1-5]-*.png`.

NOTE on evidence: no live FLEx capture was possible this session (no WinForms/WinApp MCP, no project with
parsed analyses). The "before" is a user-supplied static image + the legacy rendering code; visual rows
marked "needs live" are honest unknowns, not confirmed matches. Everything ships behind **UIMode=New
(off by default)** — no deferral is a default-user regression.

## A. Functional parity (data + behavior) — verified against legacy code + green tests

| Element (legacy source) | Status | Evidence |
|---|---|---|
| 4 interlinear lines: Morphemes / Lex. Entries / Lex. Gloss / Lex. Gram. Info. (`MakeRoot` LineChoices) | ✅ Present | `InterlinearAnalysisModel`, projector; `InterlinearAnalysisProjectorTests` |
| Affix markers on morphemes (`kfragPrefix`/`kfragPostfix`, e.g. `ka-`, `-a`) | ✅ Present | `ApplyMorphTypeMarkers`; projector test asserts `ka-` |
| Homograph numbers on lex entries (`LexEntryVc`, e.g. `mu₁`) | ✅ Present | `HeadWord.Text` (carries homograph) |
| Per-morpheme **morph / lex-entry** edit (choose a different entry sharing the surface form) | ✅ Present | `ChooseMorph` (`MorphServices.GetMatchingMorphs`); `InterlinearMSAPruneTests.ChooseMorph…`; editable Lex. Entries line |
| Per-morpheme **sense** edit (gloss combo) | ✅ Present | `InterlinearBundleEditChoices`; `InterlinearVisualTests` |
| Per-morpheme **MSA / grammatical-info** edit (category combo) | ✅ Present | `ChooseMsa`; `InterlinearMSAPruneTests` |
| Write-back re-points `MorphRA`/`SenseRA`/`MsaRA` (`UpdateRealAnalysisMethod` parity) | ✅ Present | `InterlinearWriteBackWorkflowTests` (T4); `ChooseMorph` cascade |
| **MSA prune** (`CollectReferencedMsas` → delete `CanDelete`) | ✅ Present | `InterlinearMSAPruneTests` (5) + T4 |
| One undoable UOW per gesture | ✅ Present | T4 (single Undo reverts edit + restores pruned MSA) |
| Editable only for human-approved analyses (`deParams editable`) | ✅ Present | `IsApproved` gate in the plugin |
| Multi-analysis display (approved / no-opinion / disapproved sections) | ✅ Present | composer descent; `Compose_WordformRooted…` |
| UI-mode gate (New → Avalonia, Legacy → WinForms) | ✅ Present | `RecordEditViewSwitchTests` Analyses case |
| Engine isolation (no Sandbox / native Views / LCModel in the view) | ✅ Present | `EngineIsolationAuditTests` |
| **Re-segmentation** (morpheme-breaker: change the surface Morphemes line / bundle count) | ⏸ Deferred (cited) | legacy `SandboxBase.MorphemeBreaker`; the Morphemes (surface-form) line stays read-only. PARITY note in `tasks.md` 3.1. |

**Functional verdict:** parity on rendering + morph/entry + sense + MSA editing + write-back/MSA-prune +
undo + gating. The remaining gap is re-segmentation (the heavy morpheme-breaker subsystem), deferred with a
citation. Three of the four interlinear lines are editable; only the surface Morphemes line is read-only.

## B. Visual similarity — partially confirmable (static image only)

| Element | Status | Note |
|---|---|---|
| Left label pile (Morphemes / Lex. Entries / Lex. Gloss / Lex. Gram. Info.) | ✅ Added | navy italic labels, column 0 |
| Blue label color | ✅ Approximated | navy `#1A1A8C`; exact legacy RGB not matched |
| Aligned per-morpheme columns | ✅ Match | Grid Auto shared-width |
| Editable affordance | ◑ Equivalent, different style | legacy = boxed dropdown combos w/ arrows; ours = underlined cells opening flyout pickers |
| Per-line data colors (`LabelRGBFor` — gloss/gram tints) | ⏸ Needs live | ours: gloss italic, gram gray; exact tints not replicated |
| Parser-guess styling (`UsingGuess` — guessed analysis tint) | ⏸ Needs live | not replicated |
| RTL alignment policy | ⚠ Diverges (cited) | legacy keeps the label pile LEFT even in RTL (`…LeftAlign`); ours flips the whole control (label pile moves to the right edge under RTL) |
| Wordform line placement | ◑ Minor divergence | ours renders the wordform line atop each analysis; the legacy *slice* relies on the summary header above it (the wordform is still shown, just sourced differently) |

**Visual verdict:** structurally similar (labels + 4 lines + aligned columns + affix markers now match the
reference). The remaining differences are color fidelity, parser-guess styling, the RTL label-alignment
policy, and the editable affordance style — none confirmable as "matching" without a live legacy capture,
all tracked here, all UIMode=New-gated.

## C. To fully verify visual similarity
Enable WinForms/WinApp MCP (or run FLEx manually) on a project with parsed wordform analyses, open
Words ▸ Analyses on a populated wordform under both UI modes, and capture before/after for side-by-side.
