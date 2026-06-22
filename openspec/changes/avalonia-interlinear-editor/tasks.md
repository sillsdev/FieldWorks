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
- [x] 1.1 DONE+green (W-1): `WfiWordform` nested-tree compose proven FREE via the §20.1.4 class-general
  composer — `FullEntryRegionComposer.Compose(wordform)` already walks the analyses sequence
  (`WalkSequence`→`DescendInto`) into each `WfiAnalysis` layout and reaches the InterlinearAnal plugin row
  (design Decision 6: no new virtual flids). Proven by `InterlinearSlicePluginTests.Compose_WordformRooted_DescendsIntoAnalyses_AndRendersTheInterlinear`.
- [x] 1.2 DONE+green: `InterlinearAnalysisModel` (wordform → lines → bundles → {morph, gloss, grammaticalInfo} + morph/sense/msa GUIDs; `HasAnalysis`), LCModel-free — `Src/Common/FwAvalonia/Region/InterlinearAnalysisModel.cs`; 3 unit tests (`InterlinearAnalysisModelTests`).
- [x] 1.3 DONE+green: Projector in xWorks (`InterlinearAnalysisProjector.ProjectAnalysis`): `WfiAnalysis`/`WfiMorphBundle` → `InterlinearAnalysisModel` (morph form / `SenseRA.Gloss` / `MsaRA.InterlinearAbbr` + GUIDs); T1 parity vs live LCModel values — `InterlinearAnalysisProjectorTests` (4 tests).
- [~] 1.4 (W-3) DEFERRED (cited): the wordform/analysis HeavySummary header already composes via the
  class-general composer's Summary/EmbeddedView lanes (the `Compose(wordform)` output carries the section
  headers + per-analysis header rows — see `Compose_WordformRooted` evidence). The remaining param'd-jtview
  visual parity (exact summary-line rendering) needs live-FLEx visual comparison; deferred to the live-app
  parity pass. Gated behind UIMode=New (off by default), so no default-user regression.

## 2. Read-only interlinear renderer (W-4, de-risk)
- [x] 2.1 DONE+green: Avalonia interlinear control `InterlinearRegionEditor` — aligned wordform / morpheme / lex-gloss / grammatical-info columns (Grid Auto-column shared-width measured layout, Skia-rendered, RTL-aware), LCModel-free.
- [x] 2.2 DONE+green: `InterlinearSlicePlugin` registered in read-only mode (claims `MorphologyEditor.InterlinearSlice`); T2 compose `Analyses` edit with no-Unsupported (`InterlinearSlicePluginTests`, analysis- AND wordform-rooted). `EngineIsolationAuditTests` green.
- [x] 2.3 DONE+green: PNG baselines (T5) + T3 edges: empty analysis (bare wordform), multi-analysis wordform, RTL/complex-script morphemes — `InterlinearVisualTests` (4 tests, each + `AssertNoCrowding`).

## 3. Editable morph-bundle (W-5)
- [x] 3.1 DONE+green: Per-bundle editing intents — MORPH/ENTRY (Lex. Entries line), SENSE (lex-gloss line),
  and MSA (grammatical-info line) choosers, each a button opening the shared `FwOptionPicker` flyout (reuse,
  no new picker). Candidates: morph/entry via `MorphServices.GetMatchingMorphs` (other entries sharing the
  surface form); sense/MSA from the morpheme's owning entry. Picking an entry re-points `MorphRA` and resets
  sense/MSA to the new entry's default. `InterlinearBundleEditChoices` DTO + editable `InterlinearRegionEditor`;
  editable only for human-approved analyses (legacy `deParams editable="true"`).
  // PARITY (deferred): only RE-SEGMENTATION (the morpheme-breaker: changing the surface Morphemes line /
  bundle count) stays out — its own subsystem (`SandboxBase.MorphemeBreaker`), tracked here, not in spec.md.
- [x] 3.2 DONE+green: Plugin write-back (`InterlinearAnalysisWriteBack`): re-point bundle sense (deriving MSA)
  OR MSA independently + **MSA prune** (`CollectReferencedMsas` + bundle's own MSA → delete-if-`CanDelete`)
  inside the region's shared fenced UOW (legacy `AnalysisInterlinearRs.SaveChanges` parity).
  - [x] 3.2.1 DONE+green: **T1 `InterlinearMSAPruneTests`** (4 tests) — bundle moves off an MSA no surviving
    sense uses → identified, `CanDelete`, deleted; inverse (surviving sense still uses it) → NOT pruned;
    independent MSA choice leaves the sense; clearing the sense clears sense+MSA. `MemoryOnlyBackendProviderTestBase`.
- [x] 3.3 DONE+green: T4 workflow (`InterlinearWriteBackWorkflowTests`, real cache + composed fenced host):
  edit a bundle's sense → commit → re-project round-trips (gloss/sense/MSA) AND the orphaned MSA is pruned;
  a single Undo reverts the sense change AND restores the pruned MSA (one atomic undo step per gesture).

## 4. Side-effects, commands, wiring
- [~] 4.1 (W-2) DEFERRED (cited): the Spelling enum combo composes on the Avalonia surface (WalkEnumCombo over
  `WfiWordform.SpellingStatus`), but the legacy `sideEffect="SpellingStatusChanged"` is not yet re-fired.
  Plumbing is scoped (carry `sideEffect` on `ViewNode` via `XmlLayoutImporter`, reflect-invoke `(old,new)` in
  the enum option setter — EnumComboSlice parity), but `IWfiWordform.SpellingStatusChanged` propagates to the
  external spelling dictionary, which is not cleanly verifiable headless — so it lands with the live-app
  spelling-engine verification rather than as an unverifiable green. Gated behind UIMode=New (off by default):
  spelling-status changes still persist the enum value; only the dictionary side-effect is deferred.
- [~] 4.2 (W-6) DEFERRED (cited): the composed rows already carry their menu/context-menu/hotlinks bindings
  (the plugin row + section headers preserve `menu=`/`mnuDataTree-*`), and command execution rides the same
  `RegionEditorServices`/host-command seam the lexeme-editor rows use (design Decision 7 — no new command bus).
  Verifying the specific Words commands (incl. Go-To-Wordform) end-to-end requires the live FLEx shell;
  deferred to the live-app command-routing pass. Gated behind UIMode=New (off by default).
- [x] 4.3 DONE+green: Registered `Analyses` in `LexicalEditSurfaceRegistry.DefaultSupportedTools`; moved the
  `RecordEditViewSwitchTests` `Analyses` case from `NonMigratedRecordEditTools_FallBackToLegacy` to
  `RegisteredRecordEditTools_ResolveToAvalonia` (UIMode=New → Avalonia; UIMode=Legacy keeps WinForms) — green,
  including that broad integration test constructing RecordEditView over the WfiWordform root. Resolver tests
  (`LexicalEditSurfaceResolverTests`) stay green. Satisfies the spec's UI-mode-gate scenario.
- [x] 4.4 DONE+green: `EngineIsolationAuditTests` stays green (no native Views/Graphite/Sandbox/LCModel in
  the FwAvalonia interlinear control — confirmed each test cycle).
- [x] 4.5 DONE: Full managed `./test.ps1 -SkipNative` green — all 49 managed test assemblies pass
  (xWorksTests 1648, FwAvaloniaTests 826, LexEd/IText/DetailControls/area + RecordEditView all green) EXCEPT
  38 pre-existing/environmental `RootSiteTests` render-baseline-harness failures (`RenderBaselineTests`/
  `RenderBenchmarkHarness` — the render-speedup-benchmark infra, needs display/baselines; orthogonal to this
  change, which touches zero native/RootSite code). Native skipped (managed-only change). PNG baselines
  emitted by `InterlinearVisualTests` (Interlinear-01..05). `// PARITY` notes: morph re-segmentation (3.1);
  W-2/W-3/W-6 deferrals cited above.
