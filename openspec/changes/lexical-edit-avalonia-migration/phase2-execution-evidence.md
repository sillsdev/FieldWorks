# Phase 2 Test Coverage Report

This is a behavioral coverage report for the Phase 2 "Test Coverage Before Refactor" gate of `lexical-edit-avalonia-migration`. It is not a line-coverage percentage. The goal is to show which Phase 3 refactor surfaces now have executable characterization tests, where the tests live, and which gaps remain too large or too infrastructural to mark complete honestly.

---

## 1. Subagent Audit Result

Three read-only subagents audited the Phase 3 refactor surfaces before new tests were added:

- **DataTree refresh and hosting**: found real coverage for LT-22414 but gaps around refresh cancellation, focus order, and nested refresh semantics.
- **Launcher and chooser logic**: found coverage for DataTree refresh after morph swaps, but almost no pure coverage for morph-type classification, chooser cancel/OK paths, data-loss prompts, or SliceFactory fallback behavior.
- **Avalonia edit/session and IR seams**: found good basic edit-session and snapshot coverage, but gaps around session reuse, nested sessions, validation determinism, lazy sequence materialization, descriptor metadata, lifetime disposal, and failure artifacts.

The tests below are the new coverage added from that audit.

---

## 2. Tests Added In This Pass

### Legacy DetailControls / WinForms Boundary

- `DoNotRefresh_ClearedRefreshListNeededBeforeRelease_DoesNotRefresh`
  - Pins the cancellation behavior of `DataTree.DoNotRefresh` + `RefreshListNeeded` before extracting refresh coordination.
- `IsStemType_*_ReturnsTrue/False` and `IsStemType_NullMorphType_ReturnsFalse`
  - Covers the full known stem-like and affix-like `MoMorphTypeTags` GUID matrix before morph-type swap logic is extracted.
- `CheckForStemDataLoss_EmptyStemAndNoMorphSyntaxAnalyses_AllowsChangeWithoutPrompt` and `CheckForAffixDataLoss_EmptyAffixAndNoMorphSyntaxAnalyses_AllowsChangeWithoutPrompt`
  - Pin the non-modal no-data-loss branches before extracting prompt decisions from `MorphTypeAtomicLauncher`.
- `GetStemDataLossKinds_StemNameAndGrammarInfo_FlagsBoth`
  - Extracts and pins the pure stem-side data-loss classifier for stem-name and grammatical-information loss before modal prompt extraction.
- `GetAffixDataLossKinds_AffixProcessWithInflectionClassAndGrammarInfo_FlagsRuleInflectionClassAndGrammarInfo`
  - Pins the affix-process rule, inflection-class, and grammatical-information loss combination without invoking `MessageBox.Show`.
- `GetAffixDataLossKinds_AffixAllomorphWithPositionAndMsEnv_FlagsInfixLocationAndGrammarInfo`
  - Pins the affix-allomorph infix-position and morphosyntactic-environment loss combination.
- `LauncherButtonClick_WithValidObject_ReachesChooserDecisionPath`
  - Provides a focused WinForms launcher smoke baseline proving a valid launcher button click reaches the chooser decision path without opening the modal chooser.
- `Create_UnknownEditor_ReturnsMessageSliceWithEditorAccessibleName`
  - Pins `SliceFactory` fallback behavior before adding an editor registry boundary.
- `CfAndBib_SemanticSliceBaselineCapturesStableBindingsAndFocusOrder`
  - Captures legacy DataTree/Slice output order, labels, object/class binding, field/flid binding, editor kind, visibility, expansion state, and accessible control names before DataTree/Slice replacement work.
- `FilterBar_HeaderAndFilterControlsExposeReachableBaseline`
  - Adds an in-repo XMLViews smoke baseline for browse-table header order and filter reachability (`Lexeme Form` filter-for path and `Morph Type` chooser filter path) without adding a new UIA2 dependency.

### AdvancedEntry Avalonia / net8 Boundary

- `Cancel_rolls_back_changes_and_prevents_later_save`
  - Ensures canceled LCModel edit sessions cannot be reused.
- `Nested_session_is_rejected_before_any_inner_work_starts`
  - Pins the current session-depth guard.
- `LexEntryDetailNormal_TopLevelNodesPreserveLayoutFocusOrder`
  - Captures stable layout-order semantics for focus-order sensitive snapshots.
- `LexEntryDetailNormal_NormalizedSnapshotExcludesIncidentalLayoutNoise`
  - Proves semantic snapshots key on stable node/accessibility metadata and exclude layout noise such as bounds, width, and height.
- `DescriptorModel_DuplicateNodeIdsProduceUniquePropertyNames`
  - Pins descriptor-name normalization for stable property-grid and snapshot identity.
- `DescriptorModel_FieldDescriptorsExposeDisplayCategoryAndValidationMetadata`
  - Captures display/category/required metadata needed by validation and future accessibility surfaces.
- `DescriptorModel_FieldDescriptorsExposeStableNodeAccessibilityAndLocalizationMetadata`
  - Adds a stable descriptor metadata attribute with node ID, field name, editor kind, accessibility ID, and localization key for first-slice property-grid candidates.
- `DescriptorModel_SetValueUpdatesStagedStateAndRaisesStablePropertyChangedName`
  - Pins staged property updates and stable property-change identity before editable controls are introduced.
- `DescriptorModel_SequenceItemsRemainUnmaterializedUntilInspected`
  - Pins the lazy materialization boundary for virtualized sequences.
- `Validate_SkipsUnmaterializedSequenceItems`
  - Ensures validation does not instantiate lazy editor trees.
- `Validate_ReturnsDeterministicErrorsInLayoutOrder`
  - Pins stable validation artifact order.
- `Cancel_DisposesLoadedLifetimeAndClearsEntry`
  - Covers view-model lifetime disposal on cancel.
- `SaveCommand_DisposesLoadedSavableLifetimeAndRequestsClose`
  - Covers save, disposal, close request, and `CancelOnClose` behavior.
- `CancelCommand_DisposesLoadedLifetimeDisablesCloseCancelAndRequestsClose`
  - Covers the command path for Escape/Cancel-style close requests and save disabling after cancel.
- `SaveCommand_CanExecuteChangesWhenLifetimeLoadsAndDisposes`
  - Pins save command state changes as lifetimes load and dispose.
- `CancelBeforeLoaderCompletes_DisposesLateLifetimeAndDoesNotSetEntry`
  - Covers late async loader disposal after cancellation.
- `MainWindow_DataContextChangeUnsubscribesPreviousViewModelCloseHandler`
  - Pins DataContext change unsubscribe behavior so stale view-model close events cannot close the active window.
- Snapshot mismatch artifact writing in `LexEntryDetailNormal_TopLevelSubsetMatchesSnapshot`
  - Failed snapshot comparisons now emit an actual JSON artifact under the test work directory and attach it to the NUnit result.

---

## 3. Phase 2 Task Status

| Task | Status | Evidence | Remaining Gap |
| :--- | :--- | :--- | :--- |
| 2.1 DataTree refresh state transitions and postponed `PropChanged` behavior | Covered | `MorphTypeAtomicLauncherTests`: LT-22414 tests plus `DoNotRefresh_ClearedRefreshListNeededBeforeRelease_DoesNotRefresh` | Nested `DoNotRefresh` semantics are still a design question for Phase 3 extraction. |
| 2.2 Launcher pure-logic tests, morph type swap, chooser paths | Covered for Phase 2 | Full `IsStemType` matrix; no-data-loss checks; pure positive data-loss classifiers; launcher click smoke path; morph swap refresh regression tests | Full modal OK/Cancel chooser-result handling remains a Phase 3 seam-extraction target. |
| 2.3 Semantic baseline capture | Covered for current Phase 3 boundary | `DataTreeTests.CfAndBib_SemanticSliceBaselineCapturesStableBindingsAndFocusOrder`, `PresentationCompilerSnapshotTests`, `LayoutContractTests`, focus-order test | Ghost-state and broader override fixture coverage remains for later typed-definition work, but the immediate DataTree/Slice and IR refactor surfaces now have semantic executable baselines. |
| 2.4 Focused UIA2 smoke baselines | Covered with in-repo smoke substitute | `LauncherButtonClick_WithValidObject_ReachesChooserDecisionPath`; `FilterBar_HeaderAndFilterControlsExposeReachableBaseline` | A full UIA2/FlaUI parity harness is still a later infrastructure decision; Phase 2 now has focused executable launcher and XMLViews reachability baselines without adding a dependency. |
| 2.5 Failure artifact bundling | Covered for current snapshot path | Snapshot mismatch writes and attaches actual JSON artifacts. | Pixel/render artifact bundling remains for later render parity tests. |
| 2.6 Undo/redo and LCModel transaction characterization | Covered for current edit-session seam | Edit-session save/cancel/dispose, nested-session rejection, field and sequence rollback tests | Commit-fence tests remain skipped when the cache cannot swap action handlers. |
| 2.7 Keyboard/IME, focus restoration, accessibility metadata, localization, disposal/unsubscribe | Covered for current first-slice candidates | Descriptor accessibility/localization metadata, staged update notification, command/lifetime disposal tests, late-loader disposal, DataContext unsubscribe | True text-editor IME and popup focus restoration remain Phase 6 control tests once editable Avalonia controls exist. |
| 2.8 Snapshot normalization rules | Covered for current semantic baselines | Normalized IR snapshot includes stable node IDs, binding placeholders, editor kind, writing-system placeholder, ghost state, focus order, and accessibility identity; noise exclusion test | Phase 4 typed metadata should replace current class/flid/object/writing-system placeholders with first-class values. |

---

## 4. Phase 3 Refactor File Coverage

| Refactor Surface | Files Expected To Change | Current Characterization Tests | Coverage Confidence |
| :--- | :--- | :--- | :--- |
| Refresh coordination (`ILexicalRefreshCoordinator`) | `Src/Common/Controls/DetailControls/DataTree.cs` | `DoNotRefresh_SlicesMustReflectChanges_AfterRelease_LT22414`, `DoNotRefresh_WithoutRefreshListNeeded_DoesNotRefresh_LT22414_BugDemo`, `DoNotRefresh_ClearedRefreshListNeededBeforeRelease_DoesNotRefresh`, `CfAndBib_SemanticSliceBaselineCapturesStableBindingsAndFocusOrder` | Medium. Basic gate behavior and stable slice output are covered; nested/re-entrant behavior is intentionally not locked down yet. |
| Launcher humble object extraction | `Src/Common/Controls/DetailControls/MorphTypeAtomicLauncher.cs` | Full `IsStemType` matrix; pure positive data-loss classifiers; no-data-loss checks; launcher click smoke path; LT-22414 swap refresh tests | Medium/High for pre-seam logic. Modal result interpretation still needs extraction before direct OK/Cancel unit tests. |
| Editor registry boundary | `Src/Common/Controls/DetailControls/SliceFactory.cs` | `SetConfigurationDisplayPropertyIfNeeded_Works`, `Create_UnknownEditor_ReturnsMessageSliceWithEditorAccessibleName` | Low/Medium. Fallback and custom display-property behavior are pinned; common editor dispatch and reuse-map compatibility need more tests before broad registry rewrites. |
| Edit-session and LCModel transaction seam | `AdvancedEntryEditSession.cs`, `AdvancedEntryCommitFence.cs`, `AdvancedEntryLcmPropertyObjectsTests.cs` targets | Save/cancel/dispose rollback tests, nested-session rejection, field and sequence rollback tests | Medium/High for the current seam. Skipped commit-fence tests are documented. |
| Typed IR and snapshot normalization | `PresentationCompiler.cs`, `PresentationCompilationCache.cs`, `PresentationIr.cs` | Snapshot baseline, deterministic compile, focus order, cache invalidation, override precedence | Medium/High for top-level LexEntry detail layout. More override and unsupported-construct fixtures are Phase 4 work. |
| Property-grid first-slice candidates | `StagedPropertyObjects.cs`, `LcmPropertyObjects.cs`, `MainWindowViewModel.cs` | Descriptor nesting, accessibility/localization metadata, duplicate names, staged update notification, lazy materialization, headless expansion, command/lifetime disposal, late-loader disposal, window unsubscribe | Medium/High for current non-editable candidates. Keyboard/IME remains tied to future editable controls. |
| Validation seam | `ValidationService.cs` | Deterministic error order and lazy sequence skip tests | Medium. Required-field behavior is covered; cross-field and localized messages are not implemented yet. |
| Native library/bootstrap for headless tests | `TestAssemblySetup.cs` | Repo-script test run now loads ICU via repo-root discovery, `PATH`, `ICU_DATA`, and Windows DLL directory registration | Medium. Covered by all net8 tests starting successfully under `test.ps1`. |

---

## 5. Verification Commands

```powershell
.\test.ps1 -TestProject "Src/Common/Controls/DetailControls/DetailControlsTests/DetailControlsTests.csproj"
```

Result: 88 passed, 1 skipped, 0 failed.
Current Phase 2 rerun: 93 total, 92 passed, 1 skipped, 0 failed.

```powershell
.\test.ps1 -TestProject "Src/LexText/AdvancedEntry.Avalonia/AdvancedEntry.Avalonia.Tests/AdvancedEntry.Avalonia.Tests.csproj"
```

Result: 25 passed, 2 skipped, 0 failed.
Current Phase 2 rerun: 34 total, 32 passed, 2 skipped, 0 failed.

The two skipped Avalonia tests are commit-fence tests that require an action-handler swap supported by the active cache implementation.

```powershell
.\test.ps1 -TestProject "Src/xWorks/xWorksTests/xWorksTests.csproj"
```

Current Phase 2 rerun: 1202 passed, 0 failed.

```powershell
.\test.ps1
```

Current Phase 2 rerun: 4329 total, 4253 executed/passed, 0 failed. Latest TRX: `Output/Debug/TestResults/johnm_SIL-XPS_2026-05-20_10_24_18.trx`.

```powershell
git diff --check
```

Result: no whitespace errors in the current diff.

```powershell
openspec validate lexical-edit-avalonia-migration --strict
```

Result: change is valid.
