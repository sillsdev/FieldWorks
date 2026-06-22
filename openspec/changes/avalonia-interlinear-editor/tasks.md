# Tasks

> The Words Analyses interlinear editor extracted from `lexical-edit-avalonia-migration` §20.3.5 (W-*) as
> its own XL project. Builds on the Phase-1 class-general composer + `RegionEditorPluginRegistry` +
> `RegionEditContextBase` fenced-UOW seam. Architecture rule (design Decision 1): NO Sandbox in the view;
> rendering in FwAvalonia over an LCModel-free `InterlinearAnalysisModel`, ALL LCModel reads/writes +
> MSA-prune in the xWorks/Morphology plugin. Each block build+test-gated; gated behind UIMode=New.
> Read-only first (W-4), then editable (W-5).

## 0. Testing & reuse standards (apply to every block)
- **Test rubric per block** (mandatory): **T1** unit; **T2** integration (interlinear composed/realized on the `Analyses` surface, no-Unsupported, aligned columns render); **T4 headless workflow** over a real in-memory cache (edit a bundle → commit as ONE undo step → reopen → round-trips, AND the orphaned-MSA prune occurs); **T5** PNG + crowding sanity. No editable block is done without its T4.
- **Harnesses (reuse, do not reinvent):** cache-backed T1/T2/T4 inherit `MemoryOnlyBackendProviderTestBase` (SIL.LCModel.Infrastructure) — canonical pattern `StructuredTextWorkflowTests` / `StructuredTextIntegrationTests` (headless `FwAvaloniaRuntime.EnsureInitialized()`); T5 uses `DialogSnapshot.Capture(surface, "Interlinear-<NN>-<stage>")` (Src/Common/FwAvalonia/FwAvaloniaTests/Visual/DialogSnapshot.cs) + `DialogLayoutAssert.AssertNoCrowding(root)` (Src/Common/FwAvaloniaDialogs/FwAvaloniaDialogsTests/DialogLayoutAssert.cs). `EngineIsolationAuditTests` (FwAvaloniaTests) MUST stay green — no Sandbox/native Views/Graphite/LCModel in the FwAvalonia interlinear control.
- **Reuse (architecture):** plug in via `RegionEditorPluginRegistry` (mirror `DialogLauncherPlugins`), compose via the §20.1.4 class-general `FullEntryRegionComposer.Compose(ICmObject,…)`, stage ALL writes through the region's existing fenced `LcmRegionEditSession` via `IRegionEditContext` (plugin reuses the session; never opens its own). Per-bundle morph/sense/MSA choosers reuse `FwOptionPicker` + `RegionValueFactory.BuildPossibilityOptions()`; no new picker control.

## 1. Projection + compose
- [ ] 1.1 (W-1) `WfiWordform` nested-tree compose (analyses → bundles via virtual flids) — composer/plugin.
- [x] 1.2 DONE+green: `InterlinearAnalysisModel` (wordform → lines → bundles → {morph, gloss, grammaticalInfo} + morph/sense/msa GUIDs; `HasAnalysis`), LCModel-free — `Src/Common/FwAvalonia/Region/InterlinearAnalysisModel.cs`; 3 unit tests (`InterlinearAnalysisModelTests`).
- [ ] 1.3 Projector in the Morphology plugin: `WfiAnalysis`/`WfiMorphBundle` → `InterlinearAnalysisModel`; parity test vs the legacy interlinear (T1).
- [ ] 1.4 (W-3) summary-slice param'd jtview in the header lane (corrects the §20 over-rated "handled").

## 2. Read-only interlinear renderer (W-4, de-risk)
- [ ] 2.1 Avalonia interlinear control: aligned wordform / morpheme / lex-gloss / grammatical-info columns (shared-width measured layout, Skia-rendered).
- [ ] 2.2 Register the `InterlinearSlice` plugin in read-only mode (editable=false); T2 compose `Analyses` edit with no-Unsupported.
- [ ] 2.3 PNG baseline (T5) + T3 edge: empty analysis, multi-analysis wordform, RTL/complex-script morphemes.

## 3. Editable morph-bundle (W-5)
- [ ] 3.1 Per-bundle editing intents: choose/edit morph, sense, MSA (reuse Phase-1 chooser kit).
- [ ] 3.2 Plugin write-back: `UpdateRealAnalysis` parity + **MSA prune** (`CollectReferencedMsas`→delete-if-`CanDelete`) inside one fenced UOW.
  - [ ] 3.2.1 **T1 `MSAPruneTests`** (before the 3.3 workflow): given a bundle whose chosen sense is removed, the plugin identifies the orphaned MSA, `CanDelete` is true, the delete succeeds; and the inverse — when a surviving sense still uses the MSA, it is NOT pruned. `MemoryOnlyBackendProviderTestBase`.
- [ ] 3.3 T4 workflow (real cache): edit a bundle → commit → reopen round-trips AND an orphaned MSA is pruned; one undo step per gesture.

## 4. Side-effects, commands, wiring
- [ ] 4.1 (W-2) `SpellingStatusChanged` side-effect in the option setter (ref EnumComboSlice parity).
- [ ] 4.2 (W-6) bridge Words context-menu commands to the Avalonia surface (incl. Go-To-Wordform).
- [ ] 4.3 Register `Analyses` in `LexicalEditSurfaceRegistry`; flip `RecordEditViewSwitchTests` for `Analyses` Legacy→Avalonia.
- [ ] 4.4 `EngineIsolationAuditTests` stays green (no native Views/Graphite/Sandbox/LCModel in the FwAvalonia interlinear control).
- [ ] 4.5 Full `./build.ps1` + `./test.ps1` green; PNG baselines reviewed; `// PARITY` notes for any deferred interlinear affordance.
