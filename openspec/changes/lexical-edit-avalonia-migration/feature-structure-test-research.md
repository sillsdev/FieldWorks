# FwFeatureStructureEditor — T0 Test Research / Traceability Matrix

(§19.0 rubric, Phase-1 §19b Stage 1.) Maps every WinForms feature-editor
behavior × edge case × workflow to the headless test that covers it. The control
under test is `FwFeatureStructureEditor`; tests live in
`Src/Common/FwAvaloniaDialogs/FwAvaloniaDialogsTests/FwFeatureStructureEditorTests.cs`.

WinForms truth: `FeatureStructureTreeView.cs`, `MsaInflectionFeatureListDlg.cs`
(`BuildFeatureStructure`/`UpdateFeatureStructure`), `PhonologicalFeatureChooserDlg.cs`
(flat closed-feature/value case), `MasterInflectionFeatureListDlg.cs` (create-new).

## Behavior matrix

| # | WinForms behavior (source) | New-control behavior | Test (tier) |
| --- | --- | --- | --- |
| B1 | Tree built from feature system: complex features nest children from `TypeRA.FeaturesRS` (`AddNode(IFsComplexFeature)`) | `SetNodes` folds depth-tagged nodes; complex rows are expandable parents | `Renders_FeatureSystem_AsTree` (T1) |
| B2 | Closed feature shows its `ValuesSorted` as radio nodes + trailing "None of the above" (`AddNode(IFsClosedFeature)`, `Sort`) | Closed row expands to value radios; control auto-appends a `<None>` radio | `ClosedFeature_ShowsValues_PlusNone` (T1) |
| B3 | Exactly one value per closed feature; picking a value unchecks siblings (`HandleCheckBoxNodes`) | Value radios in a closed-feature group are mutually exclusive | `PickingValue_DeselectsSiblings` (T1) |
| B4 | Picking a closed value commits it (`Chosen=true`, radioSelected) | Pick commits to `Assignments` and raises `AssignmentsChanged` | `PickingValue_CommitsAndEmits` (T1) |
| B5 | "None of the above" / no pick ⇒ feature unspecified, no spec emitted (`BuildFeatureStructure` skips) | `<None>` selected ⇒ no assignment for that feature | `NoneSelected_EmitsNoAssignment` (T1), `ClosedFeature_NoValueChosen_IsValid` (T3) |
| B6 | Complex feature expand reveals nested features (`AddNode` recursion) | Complex expand/collapse reveals/hides nested closed features | `ComplexFeature_ExpandCollapse` (T1) |
| B7 | Loading existing FS marks matching values Chosen, recurses complex (`PopulateTreeFromFeatureStructure`) | `SetAssignments` seeds radios + expands ancestors, silently (no event) | `SetAssignments_SeedsSelection_Silently` (T1) |
| B8 | Add a second feature assignment (pick a value under another feature) | Second pick adds a second assignment; both present | `AddingSecondAssignment_KeepsBoth` (T1), workflow T4 |
| B9 | Remove an assignment (re-select "None of the above") | Re-selecting `<None>` removes that feature's assignment | `ReselectingNone_RemovesAssignment` (T1) |
| B10 | Output = recursive-ascent FS; nesting implicit in parent chain (`BuildFeatureStructure`) | `Assignments` is flat `(closedFeatureId,valueId)`; host rebuilds nesting | `Assignments_AreFlatPerClosedFeature` (T1) |
| B11 | Create-new feature via `MasterInflectionFeatureListDlg` link → `DialogResult.Yes` | Inline "Create a new feature…" raises `CreateNewFeatureRequested` (no create) | `CreateFeature_RaisesRequest` (T1) |
| B12 | New values added in feature-system editor (not inline) | Per-feature "Add a value…" raises `CreateNewValueRequested(featureId)` | `CreateValue_RaisesRequest_WithFeatureId` (T1) |
| B13 | Host adds the created definition + selects it | `AcceptCreatedFeature` / `AcceptCreatedValue` add + (for value) select | `AcceptCreatedValue_AddsAndSelects` (T1) |
| B14 | Tree sorts alphabetically; auto-expands; selects first (`Sort`, dialog load) | Deterministic node order preserved from seam (document order) | covered structurally by B1/B2 render asserts |
| B15 | Keyboard: Space toggles the radio (`OnKeyUp`) | Space/Enter on a highlighted value toggles + commits | `Keyboard_SpaceTogglesValue` (T1) |
| B16 | (new-view affordance) type-ahead filter, mirrors `FwPosChooser` | Filter narrows to a flat result list; tree hides | `Filter_NarrowsToMatchingNodes` (T1) |
| B17 | `AlreadyInTree` guards against a complex type referencing an ancestor (no infinite loop) | Deeply nested input renders bounded; no hang | `DeeplyNestedComplex_Renders` (T3) |

## Edge-case matrix (T3)

| # | Edge case (WinForms basis) | Test |
| --- | --- | --- |
| E1 | Empty feature system (no features) — `CheckFeatureStructure` returns false | `EmptyFeatureSystem_RendersEmpty_NoCrash` |
| E2 | Deeply nested complex features (complex → complex → closed) | `DeeplyNestedComplex_Renders` |
| E3 | Closed feature with no value chosen (`<None>`) — unspecified is valid | `ClosedFeature_NoValueChosen_IsValid` |
| E4 | Rapid expand/collapse then pick (state stays coherent) | `RapidExpandCollapseThenPick_StaysCoherent` |
| E5 | RTL / complex-script feature & value names (display safety) | `ComplexScriptNames_RenderWithoutCrowding` |
| E6 | Large feature system (perf / virtualization sanity) | `LargeFeatureSystem_RendersWithinBudget` |

## Workflow (T4)

| W1 | open editor → expand a complex feature → pick a closed value → add a second feature → read back the assignment set | `Workflow_ExpandPickAddSecond_ReadBack` |

## Integration (T2)

| I1 | Feature editor + an `FwPosChooser` in one host panel: selections compose, change events don't cross-talk, the combined read-back reflects both | `Integration_FeatureEditorWithPosChooser_Compose` |
| I2 | Two feature editors in one panel don't share state / cross-fire events | `Integration_TwoFeatureEditors_NoCrosstalk` |

## Visual (T5)

PNGs captured before `AssertNoCrowding`, READ and reported:
`FwFeatureStructureEditor-01-initial`, `-02-expanded`, `-03-value-picked`,
`-04-multi-feature`. Full-project run for order-dependence.

---

# Stage 2 — MSA / sense wiring + IFsFeatStruc adapter (T0)

(Phase-1 §19b Stage 2.) The Stage-1 editor is now wired into the real MSA editor
(`FwMsaGroupBox`, infl/deriv inflection features) and the sense create path, with
an LCModel-aware adapter (`FwFeatureStructureAdapter`) that round-trips
`IFsFeatStruc`. WinForms truth: `MsaInflectionFeatureListDlg.cs`
(`PopulateTreeFromPos`, `BuildFeatureStructure`, the `_Closing` rebuild),
`FeatureStructureTreeView.cs` (`AddNode`/`PopulateTreeFromFeatureStructure`),
`InsertEntryDlg.SetEntryMsa`. Tests: `FwMsaGroupBoxTests` (box wiring, T1/T3),
`FwFeatureStructureAdapterTests` (adapter unit, T1), `LcmInsertEntryDialogLauncherTests`
/ `LcmAddNewSenseDialogLauncherTests` / `LcmMsaCreatorDialogLauncherTests`
(integration/workflow, T2/T4), plus the box PNG stages (T5).

## Behavior matrix

| # | WinForms behavior (source) | New behavior | Test (tier) |
| --- | --- | --- | --- |
| S1 | The box shows the inflection-feature affordance for infl/deriv affixes (MsaInflectionFeatureListDlg over InflFeatsOA / FromMsFeaturesOA) | `FwMsaGroupBox` shows the feature editor for Inflectional/Derivational | `InflectionFeatures_ShownForInflectional/Derivational` (T1) |
| S2 | No infl-feature editor for stem/root/unclassified | hidden for those types | `InflectionFeatures_HiddenForStemRootUnclassified` (T1/T3) |
| S3 | Switching affix type adds/removes the feature affordance | switching reconfigures the panel live | `InflectionFeatures_HidesWhenSwitchingToStem` (T3) |
| S4 | The chosen feature values ride the MSA | the assignment set flows into `FwSandboxMsa.InflectionFeatures` (infl/deriv only) | `Payload_Inflectional_CarriesInflectionFeatures`, `Payload_Stem_NoInflectionFeatures` (T1) |
| S5 | A value pick is a user change | a pick raises `MsaChanged` | `MsaChanged_FiresOnInflectionFeaturePick` (T1) |
| S6 | The feature system follows the POS (PopulateTreeFromPos) | the host re-feeds nodes on `MainPosChanged` | `InflectionFeatures_RefreshOnMainPosChange` (T1, launcher) |
| S7 | Create-feature / add-value links (MasterInflectionFeatureListDlg) | the box forwards `CreateNewFeatureRequested`/`CreateNewValueRequested` (Stage 3 no-op) | `CreateFeature/Value_ForwardThroughBox` (T1) |
| A1 | `PopulateTreeFromPos` + `AddNode(IFsFeatDefn)` (closed→values, complex→nested) | `FwFeatureStructureAdapter.BuildNodes` builds the depth-tagged system from `InflectableFeatsRC` | `BuildNodes_ClosedFeature_EmitsValues`, `BuildNodes_ComplexFeature_NestsChildren` (T1) |
| A2 | `PopulateTreeFromFeatureStructure` seeds the chosen radios | `ReadAssignments` returns the flat set from an existing FS | `ReadAssignments_RoundTripsClosedValue` (T1) |
| A3 | `BuildFeatureStructure` recursive-ascent `GetOrCreateValue` builds nested FS | `WriteFeatures` rebuilds nesting from the flat set + node list | `WriteFeatures_RebuildsClosedValue`, `WriteFeatures_RebuildsNestedComplex` (T1) |
| A4 | `_Closing` deletes an emptied FS (LT-13596) | `ApplyInflectionFeatures` clears the FS when the set is empty | `ApplyInflectionFeatures_EmptySet_DeletesFs` (T1) |
| A5 | `_Closing` clears extant specs before rebuild | `WriteFeatures` clears `FeatureSpecsOC` first | `WriteFeatures_ClearsExisting` (T1) |
| A6 | Owning property: InflFeatsOA (infl) / FromMsFeaturesOA (deriv) | adapter scopes to those; stem MSA is a no-op | `ApplyInflectionFeatures_StemMsa_NoOp` (T1) |

## Edge-case matrix (T3)

| # | Edge case | Test |
| --- | --- | --- |
| SE1 | No features chosen (empty set) ⇒ no FS / FS deleted | `ApplyInflectionFeatures_EmptySet_DeletesFs` (adapter), `Payload_Inflectional_NoFeatures_Empty` (box) |
| SE2 | Deeply-nested complex feature round-trips (complex→complex→closed) | `WriteFeatures_RebuildsNestedComplex`, `Workflow…` |
| SE3 | Switching MSA type clears/keeps the editor visibility correctly | `InflectionFeatures_HidesWhenSwitchingToStem` (box) |
| SE4 | RTL / complex-script feature & value names render without crowding | `InflectionFeatures_ComplexScriptNames_NoCrowding` (box) |
| SE5 | Stale/unresolvable assignment id is dropped on write | `WriteFeatures_DropsUnresolvableId` (adapter) |
| SE6 | Cancel vs commit: cancel writes nothing | covered by launcher Apply-only-on-OK (the modal loop) + `WriteFeatures` only called on create |

## Integration (T2)

| I3 | Insert Entry: MorphType (affix) + MSA POS + slot + **inflection feature** compose on one realized dialog; the payload carries all; the created entry's MSA round-trips the IFsFeatStruc | `Show_InflectionalAffix_ComposesMsaPosSlotAndFeatures` (launcher, real cache) |
| I4 | Add New Sense: same composition on the sense create path | `CreateSense_InflectionalAffix_PersistsInflectionFeatures` (launcher) |

## Workflow (T4)

| W2 | create entry → inflectional affix → pick POS → assign an inflection feature value → commit → reopen (read the FS back) → verify the IFsFeatStruc round-tripped | `Workflow_CreateInflAffix_AssignFeature_RoundTrips` (launcher, real cache) |

## Visual (T5)

Box PNGs captured before `AssertNoCrowding`, READ and reported:
`FwMsaGroupBox-06-infl-features-empty`, `FwMsaGroupBox-07-infl-feature-assigned`.
Full-project `FwAvaloniaDialogsTests` run for order-dependence.

## Scope / PARITY

- MSA **inflection features** (infl `InflFeatsOA`, deriv-FROM `FromMsFeaturesOA`)
  are fully assigned + persisted + round-tripped on the Insert Entry / Add New
  Sense create paths (those launchers own their created MSA).
- `// PARITY §19b`: the **MSA Creator** launcher SHOWS + seeds + refreshes the
  feature editor but does not round-trip on commit — its caller applies a
  `SandboxGenericMSA` (which carries no `IFsFeatStruc`); full wiring needs the
  call sites (MSAPopupTreeManager / MSADlgLauncher), outside file ownership.
- `// PARITY §19b`: **sense gloss / MGA** features are NOT wired — the legacy
  Insert Entry / Add New Sense create flow collects MSA + gloss TEXT, not gloss
  MGA features (MGA is a separate assistant dialog), so there is no create-flow
  WinForms truth to mirror; deferred with this note rather than invented.
- Deriv **TO** features (`ToMsFeaturesOA`) and stem `MsFeaturesOA` are out of
  scope (the box scopes the editor to the infl/deriv-FROM surface, the common
  create case), consistent with the Stage-6 inflection-class scoping.

---

# Stage 3 — Standalone feature dialogs + create-feature/value flow (T0)

(Phase-1 §19b Stage 3, completing §19b.) The Stage-1 editor + Stage-2 adapter are
now hosted in **standalone Avalonia chooser dialogs** (the inflection-feature and
phonological-feature choosers), the **create-feature / add-value** flow replaces
the Stage-2 deferred no-op, and the MSA-creator inflection-feature PARITY is
closed where clean. WinForms truth: `MsaInflectionFeatureListDlg.cs`
(`_Closing` rebuild + LT-13596), `PhonologicalFeatureChooserDlg.cs`
(`_Closing`, the flat closed-feature case), `MasterInflectionFeatureListDlg.cs` /
`MasterPhonologicalFeatureListDlg.cs` (blank-create links), `MasterListDlg.cs`.

New code: `FeatureChooserDialogViewModel`/`View` + `CreateFeatureDialogViewModel`/`View`
(FwAvaloniaDialogs); `LcmInflectionFeatureChooserLauncher`,
`LcmPhonologicalFeatureChooserLauncher`, `LcmCreateFeatureLauncher`,
`LcmInflectionFeatureCreateWiring` + `FwFeatureStructureAdapter.BuildPhonologicalNodes`
/ `ApplyFeaturesToOwner` / `GetInflectionFeaturePos(ICmObject,flid)` (LexText). Gates:
`MsaInflectionFeatureListDlgLauncher` + `PhonologicalFeatureListDlgLauncher`
(LexEd slices); the three MSA-section launchers wire the create no-op; the
`MSAPopupTreeManager` gated sites round-trip the chosen inflection features.
Tests: `FeatureChooserDialogTests`, `CreateFeatureDialogTests` (FwAvaloniaDialogsTests),
`LcmCreateFeatureLauncherTests`, `LcmFeatureChooserLauncherTests` (LexTextControlsTests).

## Behavior matrix (the 4 WinForms dialogs × behaviors)

| # | WinForms behavior (source) | New behavior | Test (tier) |
| --- | --- | --- | --- |
| D1 | `MsaInflectionFeatureListDlg` hosts the feature tree for a POS; OK rebuilds the IFsFeatStruc | `LcmInflectionFeatureChooserLauncher` feeds `BuildNodes(pos)` + seeds the FS; Apply rebuilds it | `InflectionBuildInput_FeedsPosSystemAndSeedsExistingAssignments`, `ApplyFeaturesToOwner_Inflection_*` (T1) |
| D2 | The chooser returns the chosen FS; empty pick ⇒ FS deleted (LT-13596) | Apply deletes the emptied inflection FS (deleteWhenEmpty: true) | `ApplyFeaturesToOwner_Inflection_RebuildsThenEmptyDeletesTheFs` (T1/T3) |
| D3 | `PhonologicalFeatureChooserDlg` works over `PhFeatureSystemOA` (flat closed features) | `LcmPhonologicalFeatureChooserLauncher` feeds `BuildPhonologicalNodes`; no empty-delete | `PhonologicalBuildInput_FeedsFlatPhFeatureSystem` (T1) |
| D4 | The dialog renders + OK/Cancel + returns the chosen feature structure | `FeatureChooserDialogViewModel` hosts the editor, no OK gate, snapshots on OK | `Renders_TheEditorMounted_AndOkAlwaysEnabled`, `SeedsExistingAssignments_AndSnapshotsThemOnOk`, `PickValue_FlowsIntoResultOnOk`, `NoPick_OkSnapshotsEmptySet` (T1) |
| D5 | `MasterInflectionFeatureListDlg.linkLabel1` blank-create: closed feature → MsFeatureSystem + "Infl" type | `LcmCreateFeatureLauncher.CreateClosedFeature(Inflection)` | `CreateClosedFeature_Inflection_AddsToMsFeatureSystemAndInflType` (T1) |
| D6 | `MasterPhonologicalFeatureListDlg.linkLabel1` blank-create: closed feature → PhFeatureSystem + +/- values | `LcmCreateFeatureLauncher.CreateClosedFeature(Phonological)` | `CreateClosedFeature_Phonological_AddsToPhFeatureSystemWithPlusMinusValues` (T1) |
| D7 | Feature-system add-value: a new `IFsSymFeatVal` under a closed feature | `LcmCreateFeatureLauncher.CreateValue` | `CreateValue_AddsSymbolicValueToTheClosedFeature` (T1) |
| D8 | The create dialog returns the chosen feature defn; OK gated until valid | `CreateFeatureDialogViewModel` gates OK on a non-empty name | `FeatureDialog_OkGatedOnNonEmptyName`, `ValueDialog_UsesValueCaptions_AndGates` (T1) |
| D9 | Create-feature / add-value links reached from the chooser | the chooser forwards `CreateNewFeatureRequested`/`CreateNewValueRequested`; the launcher runs the create + feeds the node back | `CreateFeatureRequest_ForwardsThroughTheDialog`, `CreateValueRequest_ForwardsWithFeatureId`, `AcceptCreatedFeature_AddsItToTheEditor`, `AcceptCreatedValue_AddsAndSelects` (T1/T2) |

## Edge-case matrix (T3)

| # | Edge case | Test |
| --- | --- | --- |
| DE1 | Empty feature system renders, OK still enabled | `EmptyFeatureSystem_RendersWithoutCrowding` (dialog), `PhonologicalBuildInput_*` empty |
| DE2 | Empty pick (cancel-vs-commit): commit-empty deletes the inflection FS / leaves the phonological one | `ApplyFeaturesToOwner_Inflection_RebuildsThenEmptyDeletesTheFs`, `NoPick_OkSnapshotsEmptySet` |
| DE3 | Duplicate / unknown feature id on add-value is a no-op | `CreateValue_UnknownFeatureId_ReturnsNull`, `CreateClosedFeature_EmptyName_ReturnsNull` |
| DE4 | Cancel writes nothing | `CancelCommand_ClosesWithoutAccepting` (both dialogs); launcher Apply-only-on-OK |
| DE5 | RTL / complex-script feature & value names render | `ComplexScriptNames_RenderWithoutCrowding` (dialog) |
| DE6 | Deeply-nested complex feature still round-trips (Stage-1/2 coverage carries) | `FwFeatureStructureEditorTests.DeeplyNestedComplex_Renders`, `WriteFeatures_RebuildsNestedComplex` |

## Integration (T2)

| DI1 | Create a feature → it appears in the rebuilt node system → its value is assignable | `Integration_CreatePhonologicalFeature_AppearsInRebuiltNodeSystem` (launcher), `AcceptCreatedFeature_AddsItToTheEditor` (dialog) |
| DI2 | The MSA editor's feature editor → CreateNewFeature → master-list replacement → AcceptCreatedFeature → the new feature appears + is selectable | `AcceptCreatedFeature_AddsItToTheEditor` + the MSA launchers' create-wiring (`LcmInflectionFeatureCreateWiring`) |

## Workflow (T4, real cache)

| DW1 | edit MSA → need a feature that doesn't exist → create it → add its value → assign → commit → reopen → the IFsFeatStruc round-trips | `Workflow_CreateFeature_AssignValue_Commit_Reopen_RoundTrips` (launcher, real cache) |

## Visual (T5)

Dialog PNGs captured before `AssertNoCrowding`, READ and reported:
`FeatureChooser-01-initial`, `-02-seeded`, `-03-value-picked`, `-04-empty`,
`-05-rtl`; `CreateFeature-01-empty`, `CreateValue-01-empty`. Full-project
`FwAvaloniaDialogsTests` run (284 green) for order-dependence.

## Scope / PARITY (Stage 3 close-out)

- The standalone **inflection-feature chooser** + **create-feature/value** flow
  are functional + fully tested (the §19b must-haves).
- `// PARITY §19b Stage 3`: the **create-feature CATALOG import** (pick from
  EticGlossList.xml / PhonFeatsEticGlossList.xml via `AddFeatureFromXml`) is NOT
  ported — it needs the MGA assembly + the WinForms GlossList tree parser,
  outside this stage's clean reach. The **blank-create** primitive (the common
  "I need a feature that doesn't exist yet" case) is wired; the catalog import
  remains the legacy MasterInflectionFeatureListDlg / MasterPhonologicalFeatureListDlg.
- `// PARITY §19b Stage 3`: the phonological chooser's **rule-constraint polarity**
  surface (agree/disagree `IPhFeatureConstraint`, used only from the rule-formula
  control) is NOT ported; the rule-formula call site keeps the legacy dialog. The
  plain phoneme / NC-features value-assignment case is gated to the new chooser.
- **MSA-creator PARITY closed where clean**: the `MSAPopupTreeManager` gated sites
  (`m_sense.SandboxMSA = ...`, a definite resolved MSA) now round-trip the chosen
  inflection features via `LcmMsaCreatorDialogLauncher.Show(..., out chosenBoxMsa)`
  + `ApplyInflectionFeatures`, in the same undo task. `// PARITY §19b`: the
  `MSADlgLauncher.UpdateOrReplace` path is left with a precise note — UpdateOrReplace
  may replace the MSA with a different instance, so the post-apply target is
  ambiguous within a reasonable slice.
- Sense-gloss / MGA features remain deferred (no create-flow WinForms truth) per
  the existing Stage-2 note — not invented.

**Known gaps confirmed by `xcut-review-2026-06-21.json` (tasks.md 19i.12–19i.14),
not covered by tests above:**
- Inflection-feature **siblings render unsorted**: `FwFeatureStructureAdapter.BuildNodes`/
  `AddDefnNode` walk `InflectableFeatsRC`/`FeaturesRS` in raw model order with no
  `Sort()`, unlike legacy `MsaInflectionFeatureListDlg.FinishLoading` (`ExpandAll()` +
  `Sort()`); `BuildPhonologicalNodes` DOES `OrderBy(Name)`, so the omission is
  inflection-specific, not a general limitation.
- **Phonological feature values show the full Name, not the Abbreviation**:
  `ValueName` returns `val.Name?.BestAnalysisAlternative`; legacy
  `PhonologicalFeatureChooserDlg.PopulateValuesCombo` uses
  `val.Abbreviation.BestAnalysisAlternative` (e.g. `+`/`-`).
- The editor **opens fully collapsed with no initial selection** on a fresh feature
  structure (every closed feature seeds `<None>`, so the `ExpandAncestors` guard never
  fires); legacy `FinishLoading` called `ExpandAll()` and selected `Nodes[0]`.
