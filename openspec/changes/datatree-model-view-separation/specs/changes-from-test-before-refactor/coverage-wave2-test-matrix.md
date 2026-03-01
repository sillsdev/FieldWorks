# Coverage Wave 2 Test Matrix (2026-02-25)

## Goal

Increase characterization coverage before refactor by adding deterministic tests in three areas:

- `DataTree` pure logic and key matching
- `DataTree` command/message handlers
- `DataTree` navigation + UI-adjacent helper logic

Current baseline from latest coverage artifacts:

- `DataTree`: 40.59% line / 28.03% branch
- `Slice`: 30.27% line / 19.4% branch
- `ObjSeqHashMap`: 98.39% line / 94.44% branch

## Rerun Status (2026-02-25, refreshed)

Reran managed coverage assessment via:

- `./Build/Agent/Run-ManagedCoverageAssessment.ps1 -TestFilter "FullyQualifiedName~DetailControls"`

Latest focused class coverage after Wave 2:

- `DataTree`: **51.91%** line / **38.83%** branch
- `Slice`: **30.88%** line / **19.95%** branch
- `ObjSeqHashMap`: **98.39%** line / **94.44%** branch

## Rerun Status (2026-02-25, post-Wave 3)

After Wave 3 test additions and test-file regrouping, coverage was rerun with:

- `./Build/Agent/Run-ManagedCoverageAssessment.ps1 -NoBuild -TestFilter "FullyQualifiedName~DetailControls"`

Latest focused class coverage:

- `DataTree`: **57.96%** line / **43.36%** branch
- `Slice`: **32.55%** line / **21.72%** branch
- `ObjSeqHashMap`: **98.39%** line / **94.44%** branch

Net effect: post-Wave 3 work significantly improved DataTree and modestly improved Slice (from additional exercised shared paths).

## Rerun Status (2026-02-25, continued incremental additions)

After adding another deterministic batch and rerunning:

- `./test.ps1 -NoBuild -TestFilter "FullyQualifiedName~DataTreeTests"`
- `./Build/Agent/Run-ManagedCoverageAssessment.ps1 -NoBuild -TestFilter "FullyQualifiedName~DataTreeTests"`

Latest focused class coverage from `coverage-gap-assessment.md`:

- `DataTree`: **57.46%** line / **42.87%** branch
- `Slice`: **26.88%** line / **17.9%** branch
- `ObjSeqHashMap`: **62.9%** line / **61.11%** branch

Note: this latest increment kept tests green but did **not** move focused class percentages in the managed assessment output, so the next batch should target methods not currently represented in the focused class method map.

### Correction: build-backed reruns required

Subsequent verification found that earlier reruns using `-NoBuild` were not compiling the newest test edits, so coverage appeared artificially flat. The current baseline below is from build-backed runs:

- `./test.ps1 -TestFilter "FullyQualifiedName~DataTreeTests"`
- `./Build/Agent/Run-ManagedCoverageAssessment.ps1 -TestFilter "FullyQualifiedName~DataTreeTests"`

Latest focused class coverage after additional deterministic tests:

- `DataTree`: **61.09%** line / **45.34%** branch
- `Slice`: **26.88%** line / **17.9%** branch
- `ObjSeqHashMap`: **62.9%** line / **61.11%** branch

Incremental gain from the build-backed continuation pass:

- `DataTree`: **+1.26 line** / **+1.40 branch** (from 59.83 / 43.94)

Wave 2 desired target:

- `DataTree` line coverage toward ~46-50%
- `DataTree` branch coverage toward ~34-38%

## Harness Types

- **H1 — Existing DataTree fixture**: `DataTreeTests` with in-memory backend, `Mediator`, `PropertyTable`, `Form` host.
- **H2 — Reflection helper harness**: invoke private methods (`BindingFlags.NonPublic`) for pure logic methods.
- **H3 — Command XML harness**: create `Command` from inline XML + `UIItemDisplayProperties` for `OnDisplay*` handlers.
- **H4 — Navigation host harness**: use existing `Form` host and `ShowObject` layouts (`NavigationTest`, `CfOnly`, `CfAndBib`) to drive slice focus/visibility.
- **H5 — Bitmap paint harness (planned, not in initial implementation pass)**: `Bitmap` + `Graphics.FromImage` + `PaintEventArgs`.

## Planned Tests (Detailed)

## Package A — Pure Logic and Key Matching (`DataTreeTests.cs`)

| Planned test | Target method(s) | Desired coverage impact | Harness |
|---|---|---:|---|
| `EquivalentKeys_LengthMismatch_ReturnsFalse` | `EquivalentKeys` | +2 lines, branch false path | H2 |
| `EquivalentKeys_XmlNodesWithSameNameInnerAndAttributes_ReturnsTrue` | `EquivalentKeys` | +7 lines, xml/attr loop true path | H2 |
| `EquivalentKeys_XmlNodesWithAttributeMismatch_ReturnsFalse` | `EquivalentKeys` | +5 lines, xml attr mismatch branch | H2 |
| `EquivalentKeys_IntComparisonHonorsCheckFlag` | `EquivalentKeys` | +4 lines, int/fCheckInts paths | H2 |
| `EquivalentKeys_DifferentNonComparableTypes_ReturnsFalse` | `EquivalentKeys` | +2 lines, terminal false branch | H2 |
| `FindMatchingSlices_FindsSliceForObjectAndKey` | `FindMatchingSlices`, `EquivalentKeys` | +10 lines, match path | H1+H2 |
| `FindMatchingSlices_NoMatch_ReturnsNulls` | `FindMatchingSlices` | +6 lines, no-match loop path | H1+H2 |
| `IsChildSlice_MatchingPrefix_ReturnsTrue` | `IsChildSlice` | +6 lines, positive path | H2 |
| `IsChildSlice_ShortOrNullSecondKey_ReturnsFalse` | `IsChildSlice` | +4 lines, null/len guard | H2 |
| `IsChildSlice_MismatchedPrefix_ReturnsFalse` | `IsChildSlice` | +4 lines, mismatch loop branch | H2 |
| `GetClassId_DelegatesToMetadataCache` | `GetClassId` | +2 lines | H1 |

**Package A expected gain (DataTree):** ~45-55 lines

## Package B — Command/Message Handlers (`DataTreeTests.cs`)

| Planned test | Target method(s) | Desired coverage impact | Harness |
|---|---|---:|---|
| `GetMessageTargets_NotVisible_ReturnsEmpty` | `GetMessageTargets` | +4 lines, visibility false path | H1 |
| `GetMessageTargets_VisibleWithoutCurrentSlice_ReturnsTreeOnly` | `GetMessageTargets` | +5 lines, visible/default path | H1 |
| `GetMessageTargets_VisibleWithCurrentSlice_ReturnsSliceAndTree` | `GetMessageTargets` | +5 lines, current-slice path | H1 |
| `OnDisplayShowHiddenFields_AllowedAndSet_ShowsChecked` | `OnDisplayShowHiddenFields` | +8 lines, allowed/checked path | H1+H3 |
| `OnDisplayShowHiddenFields_NotAllowed_Disables` | `OnDisplayShowHiddenFields` | +6 lines, disallowed path | H1+H3 |
| `OnDelayedRefreshList_ArgumentTogglesDoNotRefresh` | `OnDelayedRefreshList` | +3 lines | H1 |
| `OnDisplayInsertItemViaBackrefVector_MatchingClass_Enabled` | `OnDisplayInsertItemViaBackrefVector` | +8 lines, enabled path | H1+H3 |
| `OnDisplayInsertItemViaBackrefVector_WrongClass_Disabled` | `OnDisplayInsertItemViaBackrefVector` | +7 lines, disabled path | H1+H3 |
| `OnDisplayDemoteItemInVector_NonRnRoot_Disables` | `OnDisplayDemoteItemInVector` | +7 lines, guard branch | H1+H3 |
| `OnDisplayJumpToTool_ValidCommand_Enables` | `OnDisplayJumpToTool` | +8 lines, happy path | H1+H3 |

**Package B expected gain (DataTree):** ~60-75 lines

## Package C — Navigation and Utility Paths (`DataTreeTests.cs`)

| Planned test | Target method(s) | Desired coverage impact | Harness |
|---|---|---:|---|
| `NextFieldAtIndent_FindsNextAtSameIndent` | `NextFieldAtIndent` | +6 lines | H1+H4 |
| `NextFieldAtIndent_StopsWhenIndentDecreases` | `NextFieldAtIndent` | +4 lines | H1+H4 |
| `PrevFieldAtIndent_FindsPreviousAtSameIndent` | `PrevFieldAtIndent` | +6 lines | H1+H4 |
| `PrevFieldAtIndent_StopsWhenIndentDecreases` | `PrevFieldAtIndent` | +4 lines | H1+H4 |
| `IndexOfSliceAtY_ReturnsExpectedIndexAndMinusOneAfterLast` | `IndexOfSliceAtY`, `HeightOfSliceOrNullAt` | +10 lines | H1+H4 |
| `GotoNextSliceAfterIndex_AtEnd_ReturnsFalse` | `GotoNextSliceAfterIndex` | +6 lines, fail path | H1+H4 |
| `GotoPreviousSliceBeforeIndex_AtStart_ReturnsFalse` | `GotoPreviousSliceBeforeIndex` | +6 lines, fail path | H1+H4 |
| `MakeSliceVisible_TargetSliceAndPriorSlicesBecomeVisible` | `MakeSliceVisible` | +10 lines | H1+H4 |
| `GotoFirstSlice_SetsCurrentSlice_WhenFocusable` | `GotoFirstSlice`, `GotoNextSliceAfterIndex` | +8 lines | H1+H4 |

**Package C expected gain (DataTree):** ~50-65 lines

## Planned but deferred to next pass (higher harness complexity)

| Planned test area | Method(s) | Why deferred | Harness |
|---|---|---|---|
| Paint state machine tests | `OnPaint`, `HandlePaintLinesBetweenSlices` | Need controlled paint/layout surface and robustness checks | H5 |
| Context menu popup behavior | `OnShowContextMenu`, `GetSliceContextMenu` | Requires reliable context menu handler/event plumbing in tests | H1+H3 |
| Focus idle-queue behavior | `OnFocusFirstPossibleSlice`, `DoPostponedFocusSlice`, `FocusFirstPossibleSlice` | Message-pump sensitivity in CI | H4 + pump surrogate |
| Deep slice expansion matrix | `Slice.GenerateChildren`, `CreateIndentedNodes` | Needs richer layout fixture and expansion-state matrix | New Slice lifecycle harness |

## Subagent Implementation Plan

- **Subagent A (Pure Logic):** implement Package A tests + minimal reflection helpers.
- **Subagent B (Command Handlers):** implement Package B tests + command helper method(s).
- **Subagent C (Navigation):** implement Package C tests using existing layouts and form host.

All code changes are expected in:

- `Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTests.cs`

Validation after all subagents complete:

- `./test.ps1 -NoBuild -TestFilter "FullyQualifiedName~DetailControls"`
- Coverage rerun via `./Build/Agent/Run-ManagedCoverageAssessment.ps1 -TestFilter "FullyQualifiedName~DetailControls"`

## Wave 3 Follow-on Work (current)

To keep test growth manageable, DataTree tests are now being split into logical partial-class files:

- `DataTreeTests.cs` (core fixture/setup + existing suites)
- `DataTreeTests.Wave3.CommandsAndProps.cs` (command handlers + low-cost property coverage)
- `DataTreeTests.Wave3.Navigation.cs` (additional navigation edge/path coverage)

Wave 3 focus is on additional deterministic methods still listed as 0% in the gap report, especially:

- `OnDisplayJumpToLexiconEditFilterAnthroItems`
- `OnDisplayJumpToNotebookEditFilterAnthroItems`
- `OnJumpToTool`
- `OnReadyToSetCurrentSlice`
- `OnFocusFirstPossibleSlice`
- `get_LastSlice`, `get_LabelWidth`, `get_Priority`, `get_ShouldNotCall`, `get_SliceControlContainer`

Wave 3 implementation status:

- ✅ Added grouped files:
	- `DataTreeTests.Wave3.CommandsAndProps.cs`
	- `DataTreeTests.Wave3.Navigation.cs`
- ✅ Added deterministic tests for:
	- Jump-to-tool display/action handlers (`OnDisplayJumpToLexicon...`, `OnDisplayJumpToNotebook...`, `OnJumpToTool`)
	- Message-target path where tree is hidden but `CurrentSlice` exists
	- Property/unit gaps (`Priority`, `ShouldNotCall`, `SliceControlContainer`, `LabelWidth`, `LastSlice`, `SliceSplitPositionBase`, `SmallImages`, `StyleSheet`, `PersistenceProvder`, `ConstructingSlices`, `HasSubPossibilitiesSlice`)
	- Additional navigation edge paths (`GotoFirstSlice` with no slices, `GotoNextSlice` with null/last current, `IndexOfSliceAtY` empty tree, `GotoPreviousSliceBeforeIndex` empty tree)
	- Context menu plumbing (`GetSliceContextMenu`, `SetContextMenuHandler`, non-popup `OnShowContextMenu`)
	- Additional low-risk action/utility probes (`RefreshDisplay`, `NotebookRecordRefersToThisText`, `SetCurrentObjectFlids/ClearCurrentObjectFlids`, `PostponePropChanged`, `PrepareToGoAway`, `OnInsertItemViaBackrefVector` wrong-class guard, `OnDemoteItemInVector` null-root guard)
	- Additional deterministic gap reducers (`PropChanged` monitored/unmonitored with refresh suppression, `ResetRecordListUpdater` no-owner path, `OnInsertItemViaBackrefVector` missing-field guard, `OnDemoteItemInVector` non-notebook-root guard)
