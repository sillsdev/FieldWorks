# Tasks

> The Grammar rule-editing family extracted from `lexical-edit-avalonia-migration` §20.3.4 (GB-*) as its
> own XL project. Builds on the Phase-1 class-general composer + `RegionEditorPluginRegistry` +
> `RegionEditContextBase` fenced-UOW seam. Architecture rule (design Decision 1): rendering in FwAvalonia,
> ALL LCModel reads/writes in the xWorks/Morphology plugin. Each block build+test-gated; gated behind
> UIMode=New. Read-only first, then editable.

## 0. Testing & reuse standards (apply to every block)
- **Test rubric per block** (mandatory): **T1** unit; **T2** integration (the editor composed/realized on one surface, no-Unsupported); **T4 headless workflow** over a real in-memory cache (edit → commit as ONE undo step → reopen → round-trips; incl. the editable write-back); **T5** PNG + crowding sanity. A block is not done until its editable behavior has a T4.
- **Harnesses (reuse, do not reinvent):** cache-backed T1/T2/T4 inherit `MemoryOnlyBackendProviderTestBase` (SIL.LCModel.Infrastructure) — canonical pattern `StructuredTextWorkflowTests` / `StructuredTextIntegrationTests` (headless `FwAvaloniaRuntime.EnsureInitialized()`); T5 uses `DialogSnapshot.Capture(surface, "RuleFormulaEditor-<NN>-<stage>")` (Src/Common/FwAvalonia/FwAvaloniaTests/Visual/DialogSnapshot.cs) + `DialogLayoutAssert.AssertNoCrowding(root)` (Src/Common/FwAvaloniaDialogs/FwAvaloniaDialogsTests/DialogLayoutAssert.cs). `EngineIsolationAuditTests` (FwAvaloniaTests) MUST stay green (no native Views/Graphite/LCModel in the FwAvalonia editor).
- **Reuse (architecture):** plug in via `RegionEditorPluginRegistry` (mirror `DialogLauncherPlugins`), compose via the §20.1.4 class-general `FullEntryRegionComposer.Compose(ICmObject,…)`, and stage ALL writes through the region's existing fenced `LcmRegionEditSession` via `IRegionEditContext` — the plugin does NOT open its own session. Cell choosers reuse `FwOptionPicker` + `RegionValueFactory.BuildPossibilityOptions()` (`RegionChoiceOption.Depth` for the natural-class hierarchy); no new picker control.

## 1. Cell-model projection + read-only grid (de-risk)
- [x] 1.1 DONE+green: LCModel-free `RuleCell { Kind, DisplayText, TargetGuid }` + `RuleFormulaModel` (ordered cells, rule kind; immutable Insert/Remove/MoveCell helpers) — `Src/Common/FwAvalonia/Region/RuleFormulaModel.cs`; 5 unit tests (`RuleFormulaModelTests`).
- [ ] 1.2 Projector in the Morphology plugin: `PhSegmentRule`/`PhRegularRule` → `RuleFormulaModel` (LHS/RHS/env structure); parity test vs the legacy `Vc` text per rule kind (T1).
- [ ] 1.3 Read-only `RuleFormulaRegionEditor` Avalonia control rendering the cell grid (phoneme/NC/boundary/slot cells) + PNG baseline (T5).
- [ ] 1.4 Register `RuleFormulaRegionEditor` plugin for the regular-rule slice; T2 compose `PhonologicalRuleEdit` over a populated rule with no-Unsupported.

## 2. Editable rule-formula grid
- [ ] 2.1 Cell intent events (insert-at / delete / reorder / set-cell) + the Morphology plugin commit applying them to the rule in ONE fenced UOW.
- [ ] 2.2 Per-cell choosers: phoneme, natural class, boundary marker (reuse the Phase-1 chooser kit).
- [ ] 2.3 Metathesis editor on the grid base (`PhMetathesisRule`).
- [ ] 2.4 `PhSegRuleRHS` right-hand-side recursion so the rule composes end to end; register `PhonologicalRuleEdit`.
- [ ] 2.5 Compound-rule grid (`MoCompoundRule`); register `compoundRuleAdvancedEdit`.
- [ ] 2.6 T4 workflow: insert/delete/reorder a cell → commit → reopen → round-trips; one undo step per gesture.

## 3. Supporting bespoke editors
- [ ] 3.1 `BasicIPASymbolRegionEditor` (derive-on-commit symbol string; LCModel write in plugin) + register `phonemeEdit`.
- [ ] 3.2 `PhEnvStrRepresentationRegionEditor` (validator + insert toolbar + NC chooser; vernacular-string write in plugin) + register `EnvironmentEdit`.
- [ ] 3.3 Natural-class selection editor + register `naturalClassedit`.
- [ ] 3.4 Ad-hoc co-prohibition Key/Other chooser plugins + nested-group composer lane (composed like the ReferenceVector lane — walk groups, emit an editable row per group) + register `AdhocCoprohibEdit`.
- [ ] 3.5 **Tests for the supporting editors** (per the §0 rubric): T1 each (IPA derive-on-commit; PhEnvStr validator + NC chooser; natural-class option set; adhoc Key/Other + nested-group DTO) + **T4 headless workflow** each (edit → commit → reopen round-trips, MemoryOnlyBackendProviderTestBase) + T5 PNG per editor.

## 4. Wiring + validation
- [ ] 4.1 Add the rule tool ids to `LexicalEditSurfaceRegistry`; flip `RecordEditViewSwitchTests` per tool from Legacy→Avalonia as each registers.
- [ ] 4.2 `EngineIsolationAuditTests` stays green (no native Views/Graphite/LCModel in the FwAvalonia editor controls).
- [ ] 4.3 Full `./build.ps1` + `./test.ps1` green; PNG baselines per rule editor reviewed.
- [ ] 4.4 `// PARITY` notes for any intentionally-deferred rule-editor affordance.
