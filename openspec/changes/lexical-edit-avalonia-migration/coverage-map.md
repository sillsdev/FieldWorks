# Coverage Map: Lexical Edit Avalonia Migration

This map records the characterization coverage needed before refactoring the standard Lexical Edit path toward Avalonia. It separates current repo behavior from proposed seams so Phase 3 does not proceed on invented interfaces.

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
| Chooser forms | [Src/Common/Controls/XMLViews/ReallySimpleListChooser.cs](Src/Common/Controls/XMLViews/ReallySimpleListChooser.cs) and [Src/Common/Controls/XMLViews/ChooserCommandBase.cs](Src/Common/Controls/XMLViews/ChooserCommandBase.cs) | Existing isolated chooser tests, not enough for migration parity | Keyboard search, expand/collapse, double-click commit, cancel, invalid target, and transaction rollback. |

No UIA2/FlaUI/System.Windows.Automation harness was found in the repo. Phase 2 now uses an in-repo smoke substitute for launcher and XMLViews reachability; a full UIA2 parity harness remains a later infrastructure decision.

## 5. Layout Overrides and Dictionary Configuration

| Surface | Current Source | Existing Evidence | Required Baselines |
|---|---|---|---|
| Part/layout inventory | `DistFiles/Language Explorer/Configuration/Parts` | `PartsLayoutLoader` and presentation snapshot tests | Shipped `LexEntry`, `LexSense`, `Morphology`, `CmPossibility`, and custom-field placeholder fixtures. |
| Runtime layout cache | `LayoutCache` / `Inventory` usage in xWorks and XMLViews | Existing xWorks migrator tests | AdvancedEntry fixture proving default + project override precedence before XML retirement. |
| Dictionary/reversal configs | [Src/xWorks/DictionaryConfigurationMigrator.cs](Src/xWorks/DictionaryConfigurationMigrator.cs) and migrator tests | Broad migration tests under `Src/xWorks/xWorksTests/DictionaryConfigurationMigrators` | Selected customer-style config fixtures with expected typed IR and failure artifacts. |
| CSS/browser styling | [Src/xWorks/CssGenerator.cs](Src/xWorks/CssGenerator.cs) and XHTML/preview paths | Existing export tests | Explicit decision: outside migrated default path, converted to Avalonia resources, or preserved for legacy preview/export only. |

Override handling must be evidence-first: every selected fixture needs input XML/CSS, expected typed definition or diagnostic output, and an artifact path on mismatch.

## 6. AdvancedEntry Avalonia Seams

| Seam | Current Source | Current Coverage | Required Before First Editable Slice |
|---|---|---|---|
| Edit session | [Src/LexText/AdvancedEntry.Avalonia/Services/AdvancedEntryEditSession.cs](Src/LexText/AdvancedEntry.Avalonia/Services/AdvancedEntryEditSession.cs) | Save/cancel/nested-session tests | Decide direct LCModel fenced undo-task vs staged draft semantics; add global undo/redo-after-save tests. |
| Validation | [Src/LexText/AdvancedEntry.Avalonia/Services/ValidationService.cs](Src/LexText/AdvancedEntry.Avalonia/Services/ValidationService.cs) | Required-field, deterministic order, lazy skip tests | `INotifyDataErrorInfo` or `DataValidationErrors` adapter, localization/resource key, severity, async stale-result suppression. |
| Command/focus | [Src/LexText/AdvancedEntry.Avalonia/Views/MainWindow.axaml](Src/LexText/AdvancedEntry.Avalonia/Views/MainWindow.axaml) | Local `Ctrl+S` and `Escape` bindings plus view-model command state/close-request tests | Text-editor focus/caret restore and popup focus return remain Phase 6 control work. XCore bridge remains shell-phase work. |
| UI scheduling | Direct `Dispatcher.UIThread.InvokeAsync` in [MainWindowViewModel.cs](Src/LexText/AdvancedEntry.Avalonia/ViewModels/MainWindowViewModel.cs) | Headless tests flush dispatcher manually | Thin scheduler fake with cancellation, exception propagation, and no false completion for `Post`. |
| Lifetime | [MainWindowViewModel.cs](Src/LexText/AdvancedEntry.Avalonia/ViewModels/MainWindowViewModel.cs) and [MainWindow.axaml.cs](Src/LexText/AdvancedEntry.Avalonia/Views/MainWindow.axaml.cs) | Save/cancel lifetime tests, late loader result disposal, close cancellation, and DataContext unsubscribe checks | Broader leak instrumentation remains for shell/global lifetime work. |

## 7. Snapshot Normalization

| Surface | Current Source | Current Coverage | Remaining Phase 4 Work |
|---|---|---|---|
| Presentation IR semantic snapshots | [Src/LexText/AdvancedEntry.Avalonia/AdvancedEntry.Avalonia.Tests/PresentationCompilerSnapshotTests.cs](Src/LexText/AdvancedEntry.Avalonia/AdvancedEntry.Avalonia.Tests/PresentationCompilerSnapshotTests.cs) | Normalized LexEntry detail snapshot keys on stable node IDs, root binding placeholders, editor kind, writing-system placeholder, ghost state, focus order, and accessibility identity; explicit noise test rejects bounds/width/height keys | Replace placeholders with first-class class/flid/object/writing-system metadata once the typed definition model carries them. |

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