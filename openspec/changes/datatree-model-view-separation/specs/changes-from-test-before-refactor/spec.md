# Changes From Test Before Refactor

## Purpose

During Phase 0 (characterization testing), several code-level issues were discovered in `DataTree.cs`, `Slice.cs`, `ObjSeqHashMap.cs`, and the test infrastructure that should be addressed **before** the Phase 1 partial-class split begins. Fixing these first reduces noise during the structural refactor and prevents propagating known defects into the new architecture.

## Issues Discovered

### 1. `m_monitoredProps` Never Clears (DataTree.cs)

**Location**: `DataTree.MonitorProp()` (~line 560) and `DataTree.RefreshList()` (~line 1413)

**Problem**: `m_monitoredProps` is a `HashSet<Tuple<int,int>>` that accumulates entries across successive `ShowObject` / `RefreshList` calls. It is never cleared, even when the root object changes. Over a long editing session, this can cause unnecessary refresh triggers from stale monitored properties that no longer correspond to visible slices.

**Suggested Fix**: Clear `m_monitoredProps` at the start of `CreateSlices()` or `RefreshList()` when building a new slice tree. The characterization test `MonitoredProps_AccumulatesAcrossRefresh` (when written) should verify this new behavior.

**Risk**: Low — monitored props are used only for deciding whether `PropChanged` triggers a full refresh. Clearing when rebuilding the slice tree aligns intent with implementation.

### 2. `GetFlidIfPossible` Static Cache Collision Risk (DataTree.cs)

**Location**: `DataTree.GetFlidIfPossible()` (~line 3050)

**Problem**: Uses a `static Dictionary<string, int>` cache keyed by field name. If two different classes have fields with the same name but different flids, the first call wins and subsequent calls return the wrong flid. This is a latent bug that could produce incorrect slice generation.

**Suggested Fix**: Change the cache key to include the class ID: `$"{classId}:{fieldName}"`. Add a characterization test that verifies correct flid resolution for same-named fields on different classes.

**Risk**: Very low — adding class ID to the key is strictly more correct.

### 3. `ObjSeqHashMap.Values` May Double-Count Slices (ObjSeqHashMap.cs)

**Location**: `ObjSeqHashMap.Values` property

**Problem**: The `Values` property iterates all lists inside `m_table` and all values in `m_slicesToReuse`, but a slice can exist in both collections simultaneously (added to `m_table` during `Setup`, then also moved to `m_slicesToReuse` during `GetSliceToReuse`). This could return duplicate references.

**Suggested Fix**: Return a deduplicated set (e.g., `HashSet<Slice>`) or document the intentional duplication. Add a test that verifies whether Values contains duplicates after typical Setup/GetSliceToReuse usage.

**Risk**: Low — `Values` is used only during refresh cleanup to dispose leftover slices. Disposing twice is handled, but the double iteration is wasteful.

### 4. `SelectAt(99999)` Magic Number in Navigation (DataTree.cs)

**Location**: `DataTree.FocusFirstPossibleSlice()` and related navigation methods

**Problem**: Uses the literal `99999` as a "select all text" constant when calling `SelectAt()`. This is fragile and undocumented.

**Suggested Fix**: Extract to a named constant: `private const int SelectAllText = 99999;` (or ideally use `int.MaxValue` if the downstream API supports it). This is a mechanical cleanup with no behavioral change.

**Risk**: None — rename only.

### 5. `PropChanged` Uses `BeginInvoke` Without Message Pump (DataTree.cs)

**Location**: `DataTree.PropChanged()` (~line 578)

**Problem**: `PropChanged` calls `BeginInvoke()` to defer `PostponedPropChanged()`, which requires a Windows message pump. In NUnit tests (which run without a message pump), `BeginInvoke` callbacks never execute, making it impossible to test the full `PropChanged → RefreshList` chain in unit tests.

**Impact on Testing**: This is a fundamental testability barrier. The characterization test for `PropChanged` can only verify that `m_fOutOfDate` is set (via `DoNotRefresh`), but cannot verify that `RefreshList` is actually called.

**Suggested Fix (Pre-Refactor)**: No code change needed yet — this is a design constraint to document. During Phase 2 (Extract Collaborators), the model layer should use a synchronous notification pattern (e.g., `Action` callback or event) instead of `BeginInvoke`, enabling full unit test coverage.

**Risk**: N/A — documentation only for now.

### 6. UOW Nesting in Test Harness

**Location**: `DataTreeTests.cs` test setup and tests that modify data

**Problem**: The test base class `MemoryOnlyBackendProviderRestoredForEachTestTestBase` already provides an active UOW (unit of work). Tests that call `NonUndoableUnitOfWorkHelper.Do()` fail with `InvalidOperationException: Nested tasks are not supported`. Not all tests need to modify data, but those that do (e.g., deleting an object for RefreshList testing) must work within the existing UOW.

**Suggested Fix**: Use `Cache.ActionHandlerAccessor` directly (objects can be deleted/modified within the existing UOW without wrapping in `NonUndoableUnitOfWorkHelper`). Document this pattern for future test authors.

**Risk**: None — the fix is already applied in the current test code.

### 7. `ManySenses` Layout Resolution in Test Harness

**Location**: `Test.fwlayout` / `TestParts.xml` and `DataTreeTests.ManySenses_LargeSequence_CreatesSomeSlices`

**Problem**: The `ManySenses` layout uses `<part ref="Senses" param="GlossOnly" expansion="expanded"/>` with a `seq` element that expands to child items. In the test harness, all 25 senses produce slices, but none are "real" (`IsRealSlice` returns false for all). This suggests that when the child count exceeds `kInstantSliceMax` (20), the DataTree wraps entire sequences in DummyObjectSlice, not individual items.

**Impact**: The characterization test documents this behavior but cannot assert the mix of real vs dummy slices. This limitation should be understood before refactoring the DummyObjectSlice pathway.

**Suggested Fix**: No code change needed — add a detailed comment in the test documenting the observed behavior and the threshold mechanism.

**Risk**: None — documentation only.

### 8. Missing Test Coverage for Key Behaviors

The following behaviors from the test plans (`test-plan-datatree.md`, `test-plan-slice.md`) are partially or fully uncovered in the current characterization test batch. These should be completed before Phase 1 begins:

| Area | Gap | Priority |
|------|-----|----------|
| Slice.Expand/Collapse | Persistence of expansion state via PropertyTable | Medium |
| Slice.GenerateChildren | NothingResult, PossibleResult, PersistentExpansion | Medium |
| DataTree.FieldAt | DummyObjectSlice expansion on access | High |
| DataTree.GetMessageTargets | Visible vs hidden vs no current slice | Low |
| DataTree.PostponePropChanged | Deferred refresh chain | Medium |
| SliceFactory.Create | Editor dispatch: multistring, string, unknown, null | Low |
| Slice.GetCanDeleteNow | Required/optional field logic | Medium |
| Slice.GetCanMergeNow | Same-class sibling check | Medium |

## Recommended Order of Changes

1. **Items 4, 6** (zero-risk renames and pattern documentation) — immediate
2. **Item 8** (complete missing test coverage) — before Phase 1
3. **Items 1, 2, 3** (functional fixes) — during or just before Phase 1, with characterization tests verifying both old and new behavior
4. **Items 5, 7** (design constraints) — addressed during Phase 2 architecture changes

## Relationship to Other Phases

- **Phase 0** (characterization tests): This spec captures discoveries made during Phase 0.
- **Phase 1** (partial-class split): Items 1–4 should be resolved before splitting, so the split starts from a cleaner baseline.
- **Phase 2** (extract collaborators): Item 5 directly informs the notification pattern for `DataTreeModel`.
- **Phase 3** (model/view separation): Item 7 informs testing strategy for `SliceSpec` generation.

## Coverage Findings (2026-02-25)

Coverage was re-run locally using `Build/Agent/Run-TestCoverage.ps1` and assessed via
`.github/skills/managed-test-coverage-assessment/scripts/Assess-CoverageGaps.ps1`.

### Focused Class Coverage Snapshot

| Class | Line % | Branch % |
|------|-------:|---------:|
| `DataTree` | 40.59 | 28.03 |
| `Slice` | 30.27 | 19.40 |
| `ObjSeqHashMap` | 98.39 | 94.44 |

### Gap Classification Summary (Top Focused Methods)

| Suggested Resolution | Count |
|----------------------|------:|
| `add-tests-or-evaluate-relevance` | 109 |
| `add-targeted-tests` | 43 |
| `add-unit-tests` | 41 |
| `simplify-architecture-or-add-ui-harness` | 23 |
| `add-functional-tests` | 16 |
| `dead-code-or-debug-path-review` | 3 |

### Implemented Coverage-Reduction Tests (This Batch)

The following tests were implemented to reduce deterministic unit-test gaps:

- `DataTreeTests.DoNotRefresh_GetterReflectsSetter`
- `DataTreeTests.GetFlidIfPossible_ValidField_ReturnsFlid`
- `DataTreeTests.GetFlidIfPossible_InvalidField_ReturnsZero_AndCachesInvalidKey`
- `DataTreeTests.GetFlidIfPossible_InvalidField_SecondCallDoesNotGrowCache`
- `SliceTests.IsSequenceNode_TrueForOwningSequence`
- `SliceTests.IsCollectionNode_TrueForNonOwningSequenceField`
- `ObjSeqHashMapTests.Report_DoesNotThrow_WhenMapContainsEntries`

### Artifacts

- `Output/Debug/Coverage/coverage-summary.md`
- `Output/Debug/Coverage/coverage-summary.json`
- `Output/Debug/Coverage/coverage-gap-assessment.md`
- `Output/Debug/Coverage/coverage-gap-assessment.json`

### Current Prioritization After This Run

1. Continue with deterministic `add-unit-tests` in `DataTree` and `Slice` (non-UI paths).
2. Defer `simplify-architecture-or-add-ui-harness` methods unless extracted into pure collaborators.
3. Review `dead-code-or-debug-path-review` candidates with maintainers before any removal.
