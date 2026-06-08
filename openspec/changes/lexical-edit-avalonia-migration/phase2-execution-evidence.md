# Phase 2 Test Coverage Report

This is a behavioral coverage report for the Phase 2 "Test Coverage Before Refactor" gate of `lexical-edit-avalonia-migration`. It is not a line-coverage percentage. The goal is to show which Phase 3 refactor surfaces now have executable characterization tests, where the tests live, and which gaps remain too large or too infrastructural to mark complete honestly.

This narrowed Phase 1/2 branch keeps OpenSpec planning plus legacy WinForms/DataTree/XMLViews characterization coverage. The Avalonia Preview Host, `AdvancedEntry.Avalonia` prototype, and net8 Avalonia.Headless/property-grid tests have been split to `010-advanced-entry-preview-prototype`. Product command/menu wiring has been split to `010-advanced-entry-product-launcher-spike`. The unrelated `RecordList` sorting change was dropped from this scope.

---

## 1. Subagent Audit Result

Three read-only subagents audited the Phase 3 refactor surfaces before new tests were added:

- **DataTree refresh and hosting**: found real coverage for LT-22414 but gaps around refresh cancellation, focus order, and nested refresh semantics.
- **Launcher and chooser logic**: found coverage for DataTree refresh after morph swaps, but almost no pure coverage for morph-type classification, chooser cancel/OK paths, data-loss prompts, or SliceFactory fallback behavior.
- **Avalonia edit/session and IR seams**: audited as future/prototype coverage. The implementation and net8 tests now live on `010-advanced-entry-preview-prototype`, not this foundation branch.

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

### Split Avalonia Prototype Boundary

The following coverage belongs to `010-advanced-entry-preview-prototype`, not this branch: edit-session save/cancel tests, Presentation IR snapshot tests, descriptor metadata tests, validation determinism tests, Avalonia view-model lifetime tests, and snapshot failure artifacts. This branch keeps the plan and identifies those seams, but does not claim the prototype implementation as Phase 1/2 foundation evidence.

---

## 3. Phase 2 Task Status

| Task | Status | Evidence | Remaining Gap |
| :--- | :--- | :--- | :--- |
| 2.1 DataTree refresh state transitions and postponed `PropChanged` behavior | Covered | `MorphTypeAtomicLauncherTests`: LT-22414 tests plus `DoNotRefresh_ClearedRefreshListNeededBeforeRelease_DoesNotRefresh` | Nested `DoNotRefresh` semantics are still a design question for Phase 3 extraction. |
| 2.2 Launcher pure-logic tests, morph type swap, chooser paths | Covered for Phase 2 | Full `IsStemType` matrix; no-data-loss checks; pure positive data-loss classifiers; launcher click smoke path; morph swap refresh regression tests | Full modal OK/Cancel chooser-result handling remains a Phase 3 seam-extraction target. |
| 2.3 Semantic baseline capture | Partially covered for legacy boundary | `DataTreeTests.CfAndBib_SemanticSliceBaselineCapturesStableBindingsAndFocusOrder` | Typed IR/snapshot normalization moved to the preview prototype branch; ghost-state and override fixture coverage remain future work. |
| 2.4 Focused UIA2 smoke baselines | Not complete; smoke substitute only | `LauncherButtonClick_WithValidObject_ReachesChooserDecisionPath`; `FilterBar_HeaderAndFilterControlsExposeReachableBaseline` | Full UIA2/FlaUI/Appium parity harness remains future work and must not be implied by these in-repo smoke tests. |
| 2.5 Failure artifact bundling | Not covered in this branch | None in the narrowed foundation branch | Snapshot/render artifact bundling moved with the prototype and still needs render-parity evidence later. |
| 2.6 Undo/redo and LCModel transaction characterization | Not covered in this branch | None in the narrowed foundation branch | Edit-session and commit-fence characterization moved to `010-advanced-entry-preview-prototype`. |
| 2.7 Keyboard/IME, focus restoration, accessibility metadata, localization, disposal/unsubscribe | Not covered in this branch | None in the narrowed foundation branch | True text-editor IME, popup focus restoration, accessibility metadata, localization, and disposal coverage remain future/prototype work. |
| 2.8 Snapshot normalization rules | Not covered in this branch | None in the narrowed foundation branch | Normalized Presentation IR snapshots moved to `010-advanced-entry-preview-prototype`; Phase 4 still needs first-class class/flid/object/writing-system metadata. |

---

## 4. Phase 3 Refactor File Coverage

| Refactor Surface | Files Expected To Change | Current Characterization Tests | Coverage Confidence |
| :--- | :--- | :--- | :--- |
| Refresh coordination (`ILexicalRefreshCoordinator`) | `Src/Common/Controls/DetailControls/DataTree.cs` | `DoNotRefresh_SlicesMustReflectChanges_AfterRelease_LT22414`, `DoNotRefresh_WithoutRefreshListNeeded_DoesNotRefresh_LT22414_BugDemo`, `DoNotRefresh_ClearedRefreshListNeededBeforeRelease_DoesNotRefresh`, `CfAndBib_SemanticSliceBaselineCapturesStableBindingsAndFocusOrder` | Medium. Basic gate behavior and stable slice output are covered; nested/re-entrant behavior is intentionally not locked down yet. |
| Launcher humble object extraction | `Src/Common/Controls/DetailControls/MorphTypeAtomicLauncher.cs` | Full `IsStemType` matrix; pure positive data-loss classifiers; no-data-loss checks; launcher click smoke path; LT-22414 swap refresh tests | Medium/High for pre-seam logic. Modal result interpretation still needs extraction before direct OK/Cancel unit tests. |
| Editor registry boundary | `Src/Common/Controls/DetailControls/SliceFactory.cs` | `SetConfigurationDisplayPropertyIfNeeded_Works`, `Create_UnknownEditor_ReturnsMessageSliceWithEditorAccessibleName` | Low/Medium. Fallback and custom display-property behavior are pinned; common editor dispatch and reuse-map compatibility need more tests before broad registry rewrites. |
| Edit-session and LCModel transaction seam | Future `AdvancedEntry`/edit-session targets | Split to `010-advanced-entry-preview-prototype` | Not foundation-branch evidence. |
| Typed IR and snapshot normalization | Future `PresentationCompiler`/IR targets | Split to `010-advanced-entry-preview-prototype` | Not foundation-branch evidence. |
| Property-grid first-slice candidates | Future property-grid/editor targets | Split to `010-advanced-entry-preview-prototype` | Not foundation-branch evidence. |
| Validation seam | Future validation service targets | Split to `010-advanced-entry-preview-prototype` | Not foundation-branch evidence. |
| Native library/bootstrap for headless tests | Future net8 test bootstrap targets | Split to `010-advanced-entry-preview-prototype` | Not foundation-branch evidence. |

---

## 5. Verification Commands

```powershell
.\test.ps1 -TestProject "Src/Common/Controls/DetailControls/DetailControlsTests/DetailControlsTests.csproj"
```

Result: 88 passed, 1 skipped, 0 failed.
Current Phase 2 rerun: 93 total, 92 passed, 1 skipped, 0 failed.

```powershell
.\test.ps1 -TestProject "Src/xWorks/xWorksTests/xWorksTests.csproj"
```

Current Phase 2 rerun: 1202 passed, 0 failed.

```powershell
.\test.ps1
```

Earlier broad-branch rerun before the split: 4329 total, 4253 executed/passed, 0 failed. This should be rerun after the narrowed foundation branch is committed.

```powershell
git diff --check
```

Result: no whitespace errors in the current diff.

```powershell
openspec validate lexical-edit-avalonia-migration --strict
```

Result: change is valid.
