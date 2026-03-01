# Test Harness & Fixture Plan

## Purpose

This document details the test infrastructure enhancements needed to
support the 129 new characterization tests across DataTree and Slice.
It covers: fixture XML additions, helper method needs, base class
choices, and standalone data-structure test files.

---

## 1. Existing Test Infrastructure

### Base Class
- `MemoryOnlyBackendProviderRestoredForEachTestTestBase` — provides
  per-test `LcmCache` with in-memory backend. Both `DataTreeTests`
  and `SliceTests` inherit from this.

### Fixture-Level Setup (DataTreeTests)
- `GenerateLayouts()` — scans `*Tests/*.fwlayout` for layout XML
- `GenerateParts()` — scans `*Tests/*Parts.xml` for part XML
- Both produce `Inventory` objects used to initialize `DataTree`

### Per-Test Setup (DataTreeTests)
```
CustomFieldForTest → "testField" on LexEntry
ILexEntry → CitationForm="rubbish", Bibliography="My rubbishy..."
DataTree + Mediator + PropertyTable
Form hosting DataTree
```

### Shared Helpers
- `SliceTests.CreateXmlElementFromOuterXmlOf(string)` — parses XML
  string to `XmlElement`. Used by `SliceTests` and `SliceFactoryTests`.
- `SliceTests.GenerateSlice(cache, datatree)` — creates a configured
  Slice with parent DataTree.
- `SliceTests.GeneratePath()` — returns 7-element ArrayList mimicking
  a real slice Key path.

---

## 2. New XML Fixture Additions

### 2.1 `Test.fwlayout` — New Layouts Needed

Add the following layouts to the existing fixture file:

| Layout Name | Class | Purpose | Parts |
|-------------|-------|---------|-------|
| `ManySenses` | `LexEntry` | Test kInstantSliceMax threshold (>20 items) | `<part ref="Senses" />` |
| `ManySensesExpanded` | `LexEntry` | Test threshold override with `expansion="expanded"` | `<part ref="Senses" expansion="expanded" />` |
| `EmptySeq` | `LexEntry` | Test empty sequence (no items) | `<part ref="Senses" visibility="ifdata" />` |
| `GhostAtomic` | `LexEntry` | Test ghost slice creation for null atomic | `<part ref="Pronunciation" ghost="Pronunciations" />` |
| `IfNotTest` | `LexEntry` | Test `<ifnot>` conditional | `<ifnot field="..." class="LexSense">` child parts |
| `ChoiceTest` | varies | Test `<choice>/<where>/<otherwise>` | Multiple `<where>` clauses |
| `OwnerLabel` | `LexSense` | Test `$owner.` label prefix | `<part label="$owner.CitationForm" ...>` |
| `WeightTest` | `LexEntry` | Test `weight` attribute parsing | Parts with `weight="heavy"`, `weight="light"` |
| `IfDataMultiString` | `LexEntry` | Test ifdata for multistring fields | Parts with `visibility="ifdata" ws="analysis"` |
| `IfDataStText` | `LexEntry` | Test ifdata for StText empty paragraph | Part referencing a StText field |
| `NavigationTest` | `LexEntry` | 5+ slices for navigation testing | 5 simple `<part>` refs |
| `HeaderWithChildren` | `LexEntry` | Header + 3 children for focus tests | Nested header with children |

### 2.2 `TestParts.xml` — New Parts Needed

| Part ID | Purpose |
|---------|---------|
| `LexEntry-Detail-Pronunciation` | Atomic field for ghost slice test |
| `LexEntry-Detail-ManySenses` | Sequence with senses |
| Navigation parts (5x) | Simple string slices for nav tests |
| Header parts | Header + children configuration |

---

## 3. New Helper Methods

### 3.1 DataTreeTests Helpers

#### `CreateEntryWithSenses(int count)`
```csharp
private ILexEntry CreateEntryWithSenses(int count)
{
    var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
        .Create();
    var senseFactory = Cache.ServiceLocator
        .GetInstance<ILexSenseFactory>();
    for (int i = 0; i < count; i++)
        senseFactory.Create(entry);
    return entry;
}
```
**Used by:** ManySenses threshold tests, navigation tests.

#### `ShowObjectAndGetSlices(string layoutName, ILexEntry entry = null)`
```csharp
private List<Slice> ShowObjectAndGetSlices(
    string layoutName, ICmObject obj = null)
{
    obj ??= m_entry;
    m_dtree.ShowObject(obj, layoutName, null, obj, false);
    return m_dtree.Slices.ToList();
}
```
**Used by:** Most characterization tests.

#### `AssertSliceLabels(params string[] expectedLabels)`
```csharp
private void AssertSliceLabels(params string[] expectedLabels)
{
    var actual = m_dtree.Slices
        .Select(s => s.Label).ToArray();
    CollectionAssert.AreEqual(expectedLabels, actual);
}
```
**Used by:** Layout parsing tests, navigation tests.

#### `AssertSliceCount(int expected)`
```csharp
private void AssertSliceCount(int expected)
{
    Assert.AreEqual(expected, m_dtree.Controls.Count,
        $"Expected {expected} slices but found " +
        $"{m_dtree.Controls.Count}");
}
```

#### `SimulateShowHidden(string toolName, bool show)`
```csharp
private void SimulateShowHidden(string toolName, bool show)
{
    m_propertyTable.SetProperty(
        $"ShowHiddenFields-{toolName}", show, true);
}
```

### 3.2 SliceTests Helpers

#### `CreateSliceWithConfig(string xmlConfig)`
```csharp
private Slice CreateSliceWithConfig(string xmlConfig)
{
    var slice = new Slice();
    slice.ConfigurationNode =
        CreateXmlElementFromOuterXmlOf(xmlConfig);
    return slice;
}
```

#### `InstallSliceInDataTree(Slice slice, DataTree dt = null)`
```csharp
private void InstallSliceInDataTree(Slice slice, DataTree dt)
{
    dt ??= m_DataTree;
    slice.Cache = Cache;
    slice.Install(dt);
}
```

---

## 4. New Test Files

### 4.1 `ObjSeqHashMapTests.cs`
- **Location:** `DetailControlsTests/ObjSeqHashMapTests.cs`
- **Purpose:** Unit tests for the `ObjSeqHashMap` data structure in
  isolation (no DataTree, no Form, no WinForms).
- **Base class:** `NUnit.Framework.TestFixture` (no LCM needed)
- **Tests:**

| # | Test | What |
|---|------|------|
| 1 | `Add_And_Retrieve` | Add keyed slices, retrieve by key |
| 2 | `Remove_ReturnsCorrectSlice` | Remove returns the first match |
| 3 | `ClearUnwantedPart_True_ClearsAll` | `differentObject=true` clears |
| 4 | `ClearUnwantedPart_False_PreservesEntries` | `differentObject=false` preserves |
| 5 | `DuplicateKeys_FIFO` | Multiple slices with same key → FIFO retrieval |
| 6 | `MissingKey_ReturnsNull` | Retrieving nonexistent key returns null |

### 4.2 `SliceFactoryTests.cs` (Existing — Extend)
- **Current coverage:** 1 test (`SetConfigurationDisplayPropertyIfNeeded`)
- **New tests:**

| # | Test | What |
|---|------|------|
| 1 | `Create_MultistringEditor_ReturnsMultiStringSlice` | Factory dispatch for `editor="multistring"` |
| 2 | `Create_StringEditor_ReturnsStringSlice` | Factory dispatch for `editor="string"` |
| 3 | `Create_UnknownEditor_ReflectionFallback` | Custom editor class resolved via reflection |
| 4 | `Create_NullEditor_ReturnsBasicSlice` | No editor attribute → default Slice |

---

## 5. Test Configuration Updates

### 5.1 `DetailControlsTests.csproj`
The project uses SDK-style format with auto-inclusion. No changes
needed unless new test files are placed outside the project directory.
Verify that `ObjSeqHashMapTests.cs` auto-includes.

### 5.2 `Test.runsettings`
No changes needed. The existing settings file applies to all managed
tests via `.\test.ps1`.

---

## 6. Test Execution Strategy

### Phase 0 Implementation Order

1. **Week 1:** Add XML fixtures + helper methods (foundation)
2. **Week 2:** DataTree Navigation tests (12 tests, zero coverage)
3. **Week 2:** ObjSeqHashMap standalone tests (6 tests)
4. **Week 3:** DataTree Reuse tests (6 tests, depends on ObjSeqHashMap)
5. **Week 3:** DataTree Messaging tests (10 tests)
6. **Week 4:** XML Layout Parsing tests (19 tests)
7. **Week 4:** Slice Core + Lifecycle tests (19 tests)
8. **Week 5:** Slice Menu Commands tests (14 tests)
9. **Week 5:** Remaining Slice tests (31 tests)
10. **Week 6:** ShowObject extensions + JumpToTool (13 tests)

### Running Tests
```powershell
# All managed tests
.\test.ps1

# Just DetailControls tests
.\test.ps1 -Filter "DetailControls"

# Specific test class
.\test.ps1 -Filter "DataTreeTests"
```

---

## 7. Risk Areas Identified During Research

### 7.1 `m_monitoredProps` Never Cleared
The `HashSet<(int, int)>` accumulates across `RefreshList` calls and
is only nulled during `Dispose`. This means entries from previous
slice builds remain. Tests should document this behavior (not fix it
in Phase 0) via a comment:
```csharp
// NOTE: m_monitoredProps accumulates by design. Entries from
// previous ShowObject/RefreshList calls persist until Dispose.
// This could theoretically cause unnecessary refreshes but
// has not been observed as a problem in practice.
```

### 7.2 `GetFlidIfPossible` Static Cache Collision
The static `Dictionary<string, int>` keyed by `"className-fieldName"`
could collide if two classes have identically-named fields with
different flids. Tests should document this:
```csharp
// NOTE: GetFlidIfPossible uses a static cache keyed by
// "className-fieldName". If two classes define a field with
// the same name but different flids, the cache returns the
// first one seen. This is a latent bug but has no known
// manifestation in the current schema.
```

### 7.3 `BeginInvoke` in `PropChanged`
When `m_postponePropChanged` is true, `PropChanged` calls
`BeginInvoke(RefreshListAndFocus)`. This is hard to test in NUnit
because `BeginInvoke` requires a Windows message pump. Tests for
this path should either:
- Use a `Form.Show()` + `Application.DoEvents()` pattern, or
- Document the limitation and test only the synchronous path.

### 7.4 `SelectAt(99999)` Heuristic
`SetDefaultCurrentSlice` calls `SelectAt(99999)` on
`MultiStringSlice`/`StringSlice` to place the cursor at the end.
The magic number is a convention (any large value works). Tests
should verify cursor-at-end behavior without relying on the specific
value.

---

## 8. Cross-Cutting: Tests Needed for Phase 2+ Classes

These tests are NOT Phase 0 characterization tests but are listed
here for completeness. They will be created when their respective
classes are extracted.

| Class | File | Test Count | Notes |
|-------|------|------------|-------|
| `SliceLayoutBuilder` | `SliceLayoutBuilderTests.cs` | ~15 | Can test without Form/WinForms |
| `ShowHiddenFieldsManager` | `ShowHiddenFieldsManagerTests.cs` | ~6 | Pure logic, no UI deps |
| `DataTreeNavigator` | `DataTreeNavigatorTests.cs` | ~8 | May need mock Slice list |
| `DataTreeModel` | `DataTreeModelTests.cs` | ~10 | Core Phase 3 tests; no WinForms |
| `SliceSpec` | `SliceSpecTests.cs` | ~5 | Data class, trivial |

---

## Summary

| Category | File | Tests |
|----------|------|-------|
| DataTree characterization | `DataTreeTests.cs` | 65 new |
| Slice characterization | `SliceTests.cs` | 64 new |
| ObjSeqHashMap unit | `ObjSeqHashMapTests.cs` | 6 new |
| SliceFactory extensions | `SliceFactoryTests.cs` | 4 new |
| **Phase 0 Total** | | **139 new tests** |
| Phase 2+ (future) | Various | ~44 |
| **Grand Total** | | **183** |
