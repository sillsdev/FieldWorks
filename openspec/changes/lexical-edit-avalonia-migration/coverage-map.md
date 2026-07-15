# Coverage Map: Lexical Edit Avalonia Migration

This map records the characterization coverage needed before refactoring the standard Lexical Edit path toward Avalonia. It separates current repo behavior from proposed seams so Phase 3 does not proceed on invented interfaces.

This Phase 1/2 branch now carries legacy WinForms/DataTree/XMLViews characterization and planning, the net48 `FwAvalonia` spike and preview host, typed view-definition/seam foundation code, and product-facing app-wide lexical-edit UI mode wiring through existing `RecordEditView` hosts. The older net8 `AdvancedEntry.Avalonia` prototype remains on `010-advanced-entry-preview-prototype` as a separate prototype track. Branch scope should be checked against the branch-only diff from `main`, not inferred from same-day commit timestamps.

## Coverage Status Legend

| Status | Meaning |
|---|---|
| Covered | Executable characterization exists for the current boundary. |
| Partial | Some executable coverage exists, but named edge cases remain open. |
| Planned seam | No production seam exists yet; tests must be added during or before extraction. |
| Blocked | Clean tests require a new seam, harness, or approved dependency. |

## 1. DataTree Refresh and Slice Output

| Surface | Current Source | Current Coverage | Missing Before Risky Refactor |
|---|---|---|---|
| `PropChanged` handling | [Src/Common/Controls/DetailControls/DataTree.cs](Src/Common/Controls/DetailControls/DataTree.cs#L639) | LT-22414 refresh tests in [MorphTypeAtomicLauncherTests.cs](Src/Common/Controls/DetailControls/DetailControlsTests/MorphTypeAtomicLauncherTests.cs) | Nested/re-entrant `DoNotRefresh`, rapid deferred notifications, focus restoration after refresh. |
| Full refresh/rebuild | [Src/Common/Controls/DetailControls/DataTree.cs](Src/Common/Controls/DetailControls/DataTree.cs#L1967) | `DoNotRefresh_*` regression coverage | Explicit refresh coordinator tests once `ILexicalRefreshCoordinator` exists. |
| Slice semantic output | [Src/Common/Controls/DetailControls/DataTree.cs](Src/Common/Controls/DetailControls/DataTree.cs#L1879) and [Src/Common/Controls/DetailControls/Slice.cs](Src/Common/Controls/DetailControls/Slice.cs) | `CfAndBib_SemanticSliceBaselineCapturesStableBindingsAndFocusOrder` | Broader fixture set covering ghost slices, nested sequences, custom fields, and accessibility identity. |

Phase 3 must keep the legacy tests green while extracting refresh coordination. The planned `ILexicalRefreshCoordinator` is not a current repo type.

## 2. SliceFactory and Editor Resolution

| Surface | Current Source | Current Coverage | Missing Before Registry Extraction |
|---|---|---|---|
| Legacy editor factory | [Src/Common/Controls/DetailControls/SliceFactory.cs](Src/Common/Controls/DetailControls/SliceFactory.cs) | `SliceFactoryTests.SetConfigurationDisplayPropertyIfNeeded_Works`; `Create_UnknownEditor_ReturnsMessageSliceWithEditorAccessibleName` | Matrix for common editor keys (`string`, `multistring`, `jtview`, `possatomic`, `custom`, `customwithparams`, `autocustom`) and reuse-map compatibility. |
| Launcher dispatch | [Src/Common/Controls/DetailControls/Slice.cs](Src/Common/Controls/DetailControls/Slice.cs#L2772) plus launcher subclasses | Morph-type launcher click smoke reaches the chooser decision path without opening modal UI | Extracted chooser-result model and chooser adapter tests for full OK/Cancel semantics. |
| Proposed registry | Planned `ILexicalEditorRegistry` boundary | None yet | Contract tests proving unknown editors emit diagnostics, known editors resolve deterministically, and legacy fallback remains available. |

Do not describe `ILexicalEditorRegistry` or a `GetEditorType` implementation as current code. They are Phase 3 extraction targets.

## 3. Launchers, Choosers, and Morph Type Swap

| Surface | Current Source | Current Coverage | Gap / Blocker |
|---|---|---|---|
| Morph classification | [Src/Common/Controls/DetailControls/MorphTypeAtomicLauncher.cs](Src/Common/Controls/DetailControls/MorphTypeAtomicLauncher.cs) | Full `IsStemType` GUID matrix | Covered for current extraction target. |
| Data-loss decisions | [Src/Common/Controls/DetailControls/MorphTypeAtomicLauncher.cs](Src/Common/Controls/DetailControls/MorphTypeAtomicLauncher.cs#L226) and [Src/Common/Controls/DetailControls/MorphTypeAtomicLauncher.cs](Src/Common/Controls/DetailControls/MorphTypeAtomicLauncher.cs#L345) | Empty no-prompt tests plus pure classifiers for stem-name, inflection-class, infix-location, grammatical-info, and rule loss | The final yes/no prompt response still uses `MessageBox.Show`; Phase 3 should put that behind a dialog-service seam. |
| Chooser OK/Cancel | `MorphTypeChooser`, `ReallySimpleListChooser`, `ChooserCommand` | Launcher click path reaches chooser decision code for a valid object | Full OK/Cancel selection semantics remain blocked until modal UI and selection decisions are behind a humble-object or dialog-service seam. |
| Refresh side effects | `MorphTypeAtomicLauncher.SwapValues` | LT-22414 refresh tests | Focus restoration and obsolete-slice disposal need focused tests during Phase 3. |

The proposed `MorphTypeSwapController` does not exist. Phase 3 should extract from `MorphTypeAtomicLauncher`, starting with classification, data-loss issue detection, chooser result interpretation, and refresh/focus side-effect orchestration.

## 4. XMLViews Browse, Tables, and Choosers

| Surface | Current Source | Current Coverage | Missing Tests |
|---|---|---|---|
| Browse host | [Src/xWorks/RecordBrowseView.cs](Src/xWorks/RecordBrowseView.cs) | Existing xWorks tests plus `FilterBar_HeaderAndFilterControlsExposeReachableBaseline` for header order and filter/chooser reachability | Sort-state and keyboard-navigation baselines remain for table migration work. |
| XML table renderer | [Src/Common/Controls/XMLViews/XmlView.cs](Src/Common/Controls/XMLViews/XmlView.cs) | Existing XMLViews reset/refresh tests | UIA2 or equivalent smoke harness before claiming parity. |
| Chooser forms | [Src/Common/Controls/XMLViews/ReallySimpleListChooser.cs](Src/Common/Controls/XMLViews/ReallySimpleListChooser.cs) and [Src/Common/Controls/XMLViews/ChooserCommandBase.cs](Src/Common/Controls/XMLViews/ChooserCommandBase.cs) | Existing isolated chooser tests plus `WinFormsUiaSmokeTests` realized-window smoke for chooser tree and cancel-button invoke reachability | Keyboard search, expand/collapse, accept/cancel outcome semantics, invalid target, and transaction rollback still need deeper parity coverage. |

A net48 `System.Windows.Automation` harness now exists for both `FwAvaloniaPreviewHost` (`FwAvaloniaPreviewHostTests/PreviewHostUiaTests.cs`) and legacy WinForms reachability smoke (`xWorksTests/WinFormsUiaSmokeTests.cs`). The current branch now has true UIA smoke for morph-type launcher/chooser reachability and XMLViews filter-bar combo reachability; deeper shell-level workflow/accessibility parity is still a later infrastructure decision.

## 5. Layout Overrides and Dictionary Configuration

| Surface | Current Source | Existing Evidence | Required Baselines |
|---|---|---|---|
| Part/layout inventory | `DistFiles/Language Explorer/Configuration/Parts` | Prototype loader/snapshot coverage split to `010-advanced-entry-preview-prototype` | Shipped `LexEntry`, `LexSense`, `Morphology`, `CmPossibility`, and custom-field placeholder fixtures still need foundation-level parity evidence before XML retirement. |
| Runtime layout cache | `LayoutCache` / `Inventory` usage in xWorks and XMLViews | Existing xWorks migrator tests | Prototype default/override fixture coverage lives on `010-advanced-entry-preview-prototype`; selected production override fixtures still need acceptance evidence. |
| Dictionary/reversal configs | [Src/xWorks/DictionaryConfigurationMigrator.cs](Src/xWorks/DictionaryConfigurationMigrator.cs) and migrator tests | Broad migration tests under `Src/xWorks/xWorksTests/DictionaryConfigurationMigrators` | Selected customer-style config fixtures with expected typed IR and failure artifacts. |
| CSS/browser styling | [Src/xWorks/CssGenerator.cs](Src/xWorks/CssGenerator.cs) and XHTML/preview paths | Existing export tests | Explicit decision: outside migrated default path, converted to Avalonia resources, or preserved for legacy preview/export only. |

Override handling must be evidence-first: every selected fixture needs input XML/CSS, expected typed definition or diagnostic output, and an artifact path on mismatch.

## 6. Avalonia Seams and Net48 Foundations

The older net8-specific prototype coverage remains split to `010-advanced-entry-preview-prototype`, but this branch now contains net48 `FwAvalonia` seam contracts, typed view-definition foundation code, and net48 preview-host smoke coverage. The table below distinguishes current branch evidence from the still-separate prototype lane so Phase 3 work does not over-claim either one.

| Seam | Current Source | Current Coverage | Required Before First Editable Slice |
|---|---|---|---|
| Edit session | Current branch `Src/Common/FwAvalonia/Seams` + real LCModel-backed region editing; older prototype branch keeps additional experiments | `SeamTests` contract coverage plus `RegionEditingViewTests` / `LexicalEditRegionEditingTests`, with preview-path smoke in `LexicalEditPreviewTests` | Keep the preview lane detached at the region-model boundary while product editing stays LCModel-backed and globally undoable. |
| Validation | Current branch seam specs + typed model metadata; older prototype branch keeps extra net8-specific experimentation | Current branch has typed model/view-definition foundation but not full product validation presentation evidence | `INotifyDataErrorInfo` or `DataValidationErrors` adapter, localization/resource key, severity, async stale-result suppression, and product-path tests. |
| Command/focus | Current branch seam contracts/specs plus preview-host UIA smoke; older prototype branch keeps extra local-command experiments | Preview-host UIA smoke proves stable automation identities and popup reachability for the shared preview host | Text-editor focus/caret restore, popup focus return in product hosts, and real XCore bridge behavior remain Phase 6 and shell-phase work. |
| UI scheduling | Current branch `IUiScheduler`/`ImmediateUiScheduler` | `SeamTests` cover the current thin scheduler seam | Cancellation, exception propagation, and no false completion for deferred work in product paths. |
| Lifetime | Current branch `IRegionLifetime`/`RegionLifetime` | `SeamTests` cover the current thin lifetime seam | Broader leak instrumentation and shell/global lifetime work remain future tasks. |

## 7. Snapshot Normalization

| Surface | Current Source | Current Coverage | Remaining Phase 4 Work |
|---|---|---|---|
| Presentation IR semantic snapshots | Current branch `Src/Common/FwAvalonia/ViewDefinition` plus `ViewDefinitionTests`; older prototype branch keeps extra control-level experiments | Current branch covers determinism, stable IDs, field binding, editor classification, writing-system metadata, visibility, and expansion over the typed view-definition model | Add first-class localization/resource identity, accessibility identity, product-vs-preview routing metadata, and broader override fixtures before claiming production parity. |

## 8. Hard Gates Before Phase 3 Refactor

Phase 3 seam extraction should not start for a surface until one of these is true:

1. The current behavior has executable characterization tests listed in this map.
2. The gap is explicitly blocked by a planned seam and the Phase 3 task includes the first test to write.
3. The behavior is consciously deferred with owner/risk notes in the relevant plan doc.

Additional global gates:

- `git diff --check` must be clean.
- Relevant `./test.ps1 -TestProject ...` commands must pass for touched areas.
- `openspec validate lexical-edit-avalonia-migration --strict` must pass after task/doc changes.
- Any default-path migration claim must include a forbidden-symbol audit, Graphite/native viewing proof, accessibility metadata checks, localization/resource checks, and rollback/default-off evidence.

## 9. Path 3 Bundle: Semantic + Visual + Accessibility/Workflow

Path 3 is the migration-quality lane for judging visual fidelity: one scenario bundle combines semantic parity, visual/density parity, and accessibility/workflow parity so reviewers and AI can inspect the same evidence set.

Canonical bundle contract for every Path 3 scenario, even when only the legacy side exists so far:

- `scenarioId`: stable scenario identifier shared by all artifacts.
- `bundleId`: concrete artifact-set identifier for the scenario run.
- `failureSummaryId`: one ID reused by semantic, visual, workflow/accessibility, and diff artifacts.
- `semantic`: semantic snapshot artifact.
- `visual.legacy`: matched WinForms screenshot(s).
- `workflow.legacy`: workflow/accessibility evidence for the legacy surface.
- `visual.avalonia`, `workflow.avalonia`, `performance`: either present artifacts or an explicit `pending` marker.

Legacy baselines are therefore first-class Path 3 bundles, not ad hoc precursor artifacts.

| Lane | Source of Truth | Current Status | Path 3 Blocking Gaps |
|---|---|---|---|
| Semantic parity | `DataTreeTests` semantic baseline + typed IR snapshots | Partial | Broader fixture set for ghost/custom-field/accessibility identity; selected override fixtures. |
| Visual parity | WinForms render baselines and Avalonia rendered frames/screenshots | Partial | Live Avalonia screenshots once the host work lands; matched DPI/framing rules. |
| Workflow/accessibility parity | UIA2/FlaUI/Appium on live windows; in-repo smoke substitutes meanwhile | Partial | Task 2.4 true UIA2/FlaUI baselines; remaining 2.7 keyboard/IME/focus-restoration/localization work. |
| Failure evidence | `RenderFailureArtifactBundler`, semantic snapshots, trace/log output | Partial | Shared `failureSummaryId` wiring across semantic, visual, and workflow lanes. |

Path 3 blockers before a region can claim strong visual fidelity:

1. A scenario bundle must exist with all three lanes or a documented reason one lane is still pending.
2. Avalonia.Headless evidence is sufficient only for control-level visual behavior; it does not replace desktop workflow/accessibility evidence.
3. Live WinForms/Avalonia screenshots must use matched framing, DPI, and state whenever density or wrapping is under review.
4. A failure report must classify the broken lane rather than emitting only a raw image diff.