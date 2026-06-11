# DataTree Testing Approach — Critical Analysis

## Context

DataTree is a ~4,700-line god class that combines XML layout parsing, slice
lifecycle management, WinForms control hosting, navigation, mediator messaging,
property change notifications, persistence, and domain-specific jump-to-tool
commands. The current testing effort is writing "characterization tests" before
a planned model/view separation refactoring.

This document asks two questions:

1. **Devil's advocate:** Why might these tests fail to provide meaningful safety
   during the refactoring?
2. **How could the testing be made more effective?**

---

## Part 1: Devil's Advocate — Why These Tests May Not Measure What Matters

### 1.1 Reflection-heavy tests are testing the lock, not the door

A significant number of Wave 3 tests reach into private fields and methods via
`typeof(DataTree).GetField(...)` / `.GetMethod(...)`:

- `m_currentSlice`, `m_currentSliceNew`, `m_postponePropChanged`,
  `m_rch`, `m_listName`, `m_rlu`, `m_currentObjectFlids`, `m_monitoredProps`
- Private methods like `PostponePropChanged`, `RestorePreferences`,
  `SetCurrentSliceNewFromObject`, `CreateAndAssociateNotebookRecord`,
  `DescendantForSlice`, `m_rch_Disposed`, `RefreshList(int, int)`

**The problem:** The whole point of the refactoring is to _change the internals_.
Tests that are coupled to field names and method signatures will break the moment
you rename, extract, or restructure — which is exactly what a model/view split
does. These tests don't protect behavior; they protect implementation topology.
They'll tell you "you moved a field" not "you broke a user-visible behavior."

When the refactoring starts, you'll face a choice: delete most of these tests
(eliminating the safety net) or port them (spending effort adapting tests that
weren't testing outcomes in the first place).

### 1.2 Assert.DoesNotThrow is a code smell, not a specification

Multiple tests follow this pattern:

```csharp
Assert.DoesNotThrow(() => m_dtree.OnFocusFirstPossibleSlice(null));
Assert.DoesNotThrow(() => m_dtree.FixRecordList());
Assert.DoesNotThrow(() => m_dtree.GotoFirstSlice());
```

"It doesn't crash" is the weakest possible assertion. It means:
- You don't know what the method is _supposed_ to do
- You can't detect silent regressions (wrong state, lost data, no-op where
  action was expected)
- The test will pass even after a refactoring that guts the method body to
  `return;`

These tests create an illusion of safety while catching almost nothing that
matters.

### 1.3 Property getter/setter round-trips test the C# language, not the class

Tests like:

```csharp
public void SmallImages_SetterAndGetterRoundTrip() { ... }
public void StyleSheet_SetterAllowsNullRoundTrip() { ... }
public void PersistenceProvider_SetterAndGetterRoundTrip() { ... }
public void SliceSplitPositionBase_SetterUpdatesValue() { ... }
public void DoNotRefresh_GetterReflectsSetter() { ... }
```

These verify that `{ get; set; }` works. The C# compiler already guarantees
this. Unless the property has side effects (like DoNotRefresh triggering a
deferred refresh), testing plain auto-properties adds coverage percentage
without adding confidence.

### 1.4 "Characterization" without behavioral contracts is just archaeology

A characterization test is supposed to document _what the system actually does_
so regressions are detectable. But many of these tests document trivia:

- `Priority_ReturnsMediumColleaguePriority` — will this constant ever change
  during a model/view split? No. Does it matter? Only if the mediator cares.
- `ShouldNotCall_FalseByDefault` — this is a one-liner property.
- `SliceControlContainer_ReturnsSelf` — trivially true for any class that
  implements the interface by returning `this`.
- `LabelWidth_ReturnsExpectedConstant` — magic number `40` hardcoded in both
  production code and test.

The real danger in the refactoring is in the _interactions_: what happens when
ShowObject triggers RefreshList which reuses slices via ObjSeqHashMap which
depends on key equivalence which uses EquivalentKeys with XML node comparison.
That chain is barely tested as an integrated flow.

### 1.5 The test harness itself is a mock of reality

The tests use `MemoryOnlyBackendProviderRestoredForEachTestTestBase` with
a minimal `Inventory` of parts and layouts (loaded from test XML files in the
test directory). This is appropriate for unit tests, but it means:

- **Layout complexity is synthetic.** The real layouts in `DistFiles/` have
  `<choice>`, `<where>`, `<ifnot>`, `<indent>` nesting, custom editors,
  and multi-level ownership chains. The test layouts are 2–3 part configs.
- **Slice types are limited.** The test setup creates generic `Slice` objects.
  In production, DataTree creates `ReferenceVectorSlice`, `PhonologicalRuleFormulaSlice`,
  `StTextSlice`, and dozens of others via editor reflection. None of these
  appear in the tests.
- **No real WinForms hosting.** Tests use `m_parent = new Form()` without
  `.Show()`, so layout, painting, focus, and visibility checks never fire
  properly. Navigation tests (`GotoNextSlice`, `GotoPreviousSliceBeforeIndex`)
  are testing navigation logic in a context where no slice is actually visible
  or focusable.

The refactoring risk isn't "does DataTree work with 2-slice test layouts?"
It's "does DataTree work with the 47 real layouts that FieldWorks ships?"

### 1.6 Coverage percentage ≠ confidence

The test plan targets 84 tests across 7 subdomains and claims priority based
on "zero coverage today." But coverage metrics count lines hit, not behaviors
verified. A test that calls `ShowObject` and asserts `Slices.Count > 0` "covers"
thousands of lines of XML parsing, slice creation, and reuse logic — without
actually verifying any of it works correctly.

The most dangerous code paths are:
- Re-entrant `PropChanged` → `RefreshListAndFocus` → `CreateSlices` while
  layout is suspended
- `ObjSeqHashMap` reuse across refresh with stale keys
- `DummyObjectSlice.BecomeReal` mid-scroll
- Thread safety of `m_fSuspendSettingCurrentSlice`

None of these are addressed by the current tests in a way that would catch
a regression.

### 1.7 Tests document bugs as "expected behavior"

Several tests explicitly lock down questionable behavior:

- `OnJumpToLexiconEditFilterAnthroItems_WithoutCurrentSlice_ThrowsNullReferenceException`
  — This test asserts that a NullReferenceException is thrown. That's a bug,
  not a feature. Characterizing it means you'll need to keep the NRE after
  refactoring, or remember to delete the test.
- `MonitoredProps_AccumulatesAcrossRefresh_CurrentBehavior` — Documents a
  potential memory leak. The companion `[Explicit]` test acknowledges this
  should be fixed, but the characterization test will _prevent_ fixing it
  during refactoring because it asserts the leak persists.
- `AddAtomicNode_WhenFlidIsZero_ThrowsApplicationException` — Documents an
  internal validation check that's only reachable through reflection.

Characterizing bugs creates a maintenance drag: every bug you document is a
test you'll later need to update or delete when the bug is fixed.

### 1.8 The refactoring will change the public API surface

The model/view separation means DataTree will be split into (at least):
- A model layer (data, business rules, slice metadata)
- A view layer (WinForms controls, painting, focus)

Tests that call `m_dtree.ShowObject()` and then check `m_dtree.Slices[0].Label`
are testing the _combined_ model+view. After the split, there won't be a single
object that does both. Every test that touches both "what slices exist" and
"what controls are visible" will need rewriting.

If that's the case, these tests are a temporary scaffold — which is fine, but
the test plan doesn't acknowledge this. It presents them as long-lived safety
nets, which they won't be.

---

## Part 2: How to Test More Effectively

### 2.1 Identify the behavioral contracts that survive the refactoring

Before writing more tests, answer: **what invariants must be preserved after
the model/view split?** These are the tests worth writing:

| Contract | Survives refactoring? |
|----------|----------------------|
| Given layout XML + object → correct ordered list of (label, type, indent, object) | Yes — this is the model |
| `visibility="ifdata"` hides when data is empty | Yes |
| `visibility="never"` hides unless show-hidden is on | Yes |
| Refreshing same object reuses matching slice metadata | Maybe — depends on design |
| MonitorProp + PropChanged → refresh | Yes — notification contract |
| Navigation order matches slice order | Yes — but implementation changes |
| JumpToTool resolves correct GUID | Yes — business logic |
| Context menu handler dispatch | Probably yes |

Tests for the "Yes" contracts are worth investing in. Tests for internal
mechanics (ObjSeqHashMap, EquivalentKeys, DummyObjectSlice) will break.

### 2.2 Test at the right boundary: layout → slice metadata

Instead of testing `DataTree` as a god object, extract the testable core:

**The layout engine is a pure function:**
```
(XML layouts, XML parts, LCM object, layout name) → ordered list of SliceSpec
```

Where `SliceSpec` is a data object: `{ Label, Indent, FieldName, Flid, EditorType,
Visibility, ObjectHvo, ConfigurationNode }`.

If you extract this function (even before the full refactoring), you can:
- Test it without WinForms
- Test it without a parent Form
- Test with real production layouts from `DistFiles/`
- Assert on structured data instead of control tree state
- Keep the tests across the refactoring because the function signature is stable

This is the single most impactful change: **separate "what slices should exist"
from "how are they rendered."**

### 2.3 Use production layouts as golden-file tests

The test plan uses synthetic 2-part layouts. This misses the combinatorial
complexity of real layouts. A more effective approach:

1. Run DataTree with each real layout (from `DistFiles/Language Explorer/Configuration/`)
   and a representative LCM object
2. Serialize the resulting slice list to a golden file:
   `{ label, indent, type, object class, flid }`
3. Diff against the golden file after each code change

This gives you whole-system regression coverage without writing individual
assertions. It's the classic characterization test pattern — snapshot testing —
and it's far more robust than hand-written assertions for each branch.

### 2.4 Replace reflection-based access with testable seams

Instead of:
```csharp
var field = typeof(DataTree).GetField("m_currentSlice", BindingFlags.Instance | BindingFlags.NonPublic);
field.SetValue(m_dtree, slice);
```

Create `internal` methods or use `[InternalsVisibleTo]` (already available
since the tests are in the same assembly area). Better yet, extract the logic
into a separate class where the state is part of the public contract:

```csharp
// Instead of testing DataTree's private field directly:
var nav = new SliceNavigator(slices);
nav.MoveTo(slice);
Assert.That(nav.Current, Is.SameAs(slice));
```

This way, the tests survive extraction because they're testing the extracted
class directly.

### 2.5 Write integration tests that exercise real user workflows

The most dangerous regressions in a refactoring are the ones where "each piece
works but the whole doesn't." Consider a small number of end-to-end tests:

1. **Show a LexEntry, edit citation form, verify refresh** — exercises
   ShowObject → slice creation → PropChanged → RefreshList → slice reuse
2. **Show an entry with 30 senses, scroll to sense 25** — exercises
   DummyObjectSlice → BecomeReal → layout
3. **Toggle show-hidden-fields on/off** — exercises property change →
   HandleShowHiddenFields → slice visibility
4. **Switch from one entry to another** — exercises slice disposal →
   new slice creation → focus management

These four tests cover more real-world risk than 50 getter/setter checks.

### 2.6 Explicitly tag tests by lifespan

Not all characterization tests are equal. Tag them:

- **`[Category("SurvivesRefactoring")]`** — Tests behavioral contracts that
  should pass before _and_ after the model/view split.
- **`[Category("PreRefactoring")]`** — Tests that document current internals
  and are expected to be deleted/rewritten during the split.
- **`[Category("KnownBug")]`** — Tests that document bugs (NRE on null current
  slice, etc.) which should become `Assert.DoesNotThrow` or be deleted after fix.

This makes the test suite actionable during the refactoring instead of a wall
of red that requires triage.

### 2.7 Don't test constants and trivial properties

Remove or don't write tests for:
- `Priority_ReturnsMediumColleaguePriority` (constant)
- `ShouldNotCall_FalseByDefault` (trivial)
- `SliceControlContainer_ReturnsSelf` (identity)
- `LabelWidth_ReturnsExpectedConstant` (constant)
- `SmallImages_SetterAndGetterRoundTrip` (auto-property)
- `StyleSheet_SetterAllowsNullRoundTrip` (auto-property)

These inflate test count and coverage without detecting regressions. The time
spent writing and maintaining them is better spent on the behavioral contracts
listed in §2.1.

### 2.8 Address the ObjSeqHashMap gap directly

`ObjSeqHashMap` is the most critical data structure for the refactoring (it
controls slice reuse during refresh), yet it has zero direct tests. The test
plan mentions this (§3.3–3.5) but no tests have been written.

This should be the highest priority: extract `ObjSeqHashMap` into its own file
with its own test fixture, and test:
- Insert → retrieve by key
- ClearUnwantedPart(true) vs ClearUnwantedPart(false)
- Key collision behavior
- Behavior with disposed slices in the map

These tests are cheap, fast, and directly relevant to the refactoring.

---

## Summary

| Issue | Severity | Recommendation |
|-------|----------|----------------|
| Reflection-coupled tests break on refactoring | High | Extract testable seams; use internal visibility |
| Assert.DoesNotThrow tests catch nothing | High | Replace with specific outcome assertions |
| Getter/setter round-trips are wasteful | Medium | Delete; focus on behavioral contracts |
| Synthetic layouts miss real complexity | High | Add golden-file tests with production layouts |
| Tests document bugs as expected behavior | Medium | Tag as `[Category("KnownBug")]`; plan for fixes |
| ObjSeqHashMap untested | High | Standalone test fixture, highest priority |
| No test lifespan tagging | Medium | Add categories for refactoring lifecycle |
| No integrated workflow tests | High | Write 4–5 end-to-end scenarios |

### Bottom line

The current testing effort is building _coverage_ but not _confidence_. Many
tests are tightly coupled to internals that will change, assert trivial
properties, or document bugs as specifications. The test plan optimizes for
"number of tests" when it should optimize for "number of behavioral contracts
preserved across the refactoring."

The most effective changes are:
1. Extract the layout-to-slice-metadata function and test it in isolation
2. Add golden-file tests with real production layouts
3. Test `ObjSeqHashMap` directly
4. Tag tests by expected lifespan
5. Stop writing getter/setter and `DoesNotThrow` tests
