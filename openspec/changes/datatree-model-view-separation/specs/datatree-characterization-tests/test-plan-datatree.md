# DataTree Characterization Test Plan

## Purpose

This document details every characterization test needed for `DataTree.cs`
before the model/view separation. Tests are organized by subdomain
(matching the partial-class file split). Each entry documents:

- What behavior to lock down
- Whether a test already exists
- The test name and fixture pattern
- Edge cases to cover

Tests are primarily in `Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTests.cs`
and its partial companions (`DataTreeTests.Wave3.*`, `DataTreeTests.Wave4.OffscreenUI.cs`),
unless otherwise noted.

---

## Subdomain 1: XML Layout Parsing (`DataTree.LayoutParsing.cs`)

These tests cover lines ~1743–2960 of DataTree.cs — the XML-to-slice
pipeline from `CreateSlicesFor` through `AddSimpleNode`.

### Existing Tests

| # | Test | Covers |
|---|------|--------|
| 1 | `OneStringAttr` | Single `multistring` slice from layout |
| 2 | `TwoStringAttr` | Two slices, correct ordering |
| 3 | `LabelAbbreviations` | 3 abbreviation modes |
| 4 | `IfDataEmpty` | `visibility="ifdata"` hides empty fields |
| 5 | `NestedExpandedPart` | Header + nested children, indent=0 |
| 6 | `RemoveDuplicateCustomFields` | `customFields="here"` dedup |
| 7 | `BadCustomFieldPlaceHoldersAreCorrected` | Missing `ref` attr fixup |
| 8 | `OwnedObjects` | `<seq>` expansion across senses + etymology |

### New Tests Needed

#### 1.1 `CreateSlicesFor_NullObject_ReturnsEmptySliceList`
- **What:** Call `ShowObject(null, ...)` — verify no slices created
- **Edge case:** Null object after disposal guard

#### 1.2 `GetTemplateForObjLayout_ClassHierarchyWalk`
- **What:** Create an object whose exact class has no layout, but a
  base class does. Verify the base-class layout is used.
- **Fixture:** Add a layout for `CmObject` (generic base) but not for
  the specific subclass. Verify slices match the base layout.
- **Edge case:** Class hierarchy walk terminates at `CmObject` (id=0)

#### 1.3 `GetTemplateForObjLayout_CmCustomItemUsesWsLayout`
- **What:** For `CmCustomItem`, verify the layout selection considers
  the containing list's WS (`Names.BestAnalysisAlternative`) to pick
  between analysis/vernacular name layouts.
- **Fixture:** Create a `CmCustomItem` in a custom list, verify layout
  name resolution. This requires a custom list + custom item in the
  test cache.

#### 1.4 `ApplyLayout_DuplicateCustomFieldPlaceholders`
- **What:** Already partially covered by `RemoveDuplicateCustomFields`.
  Extend to verify that custom field parts are actually injected at the
  placeholder position and that only the first placeholder survives.
- **Assertions:** Slice count matches expected; custom field slice
  appears at the correct index.

#### 1.5 `ProcessSubpartNode_SequenceWithMoreThanThresholdItems`
- **What:** Create an entry with >20 senses (`kInstantSliceMax`).
  Verify that `AddSeqNode` creates some `DummyObjectSlice` instances.
- **Fixture:** New layout `ManySenses` referencing `<seq field="Senses">`.
  Populate 25 senses.
- **Assertions:**
  - Total slice count == 25
  - At least one slice has `IsRealSlice == false`
  - Do **not** assume the first `kInstantSliceMax` are real; current behavior may produce all dummy placeholders for large sequences.

#### 1.6 `ProcessSubpartNode_ThresholdOverride_ExpandedCaller`
- **What:** When the caller node has `expansion="expanded"`, the
  current implementation does not reliably force eager instantiation for all items.
- **Fixture:** Layout with `<part ref="Senses" expansion="expanded">`.
  25 senses.
- **Assertions:** Document observed behavior (which may still include dummy placeholders) and avoid asserting all-real slices unless code changes to guarantee that contract.

#### 1.7 `ProcessSubpartNode_ThresholdOverride_PersistentExpansion`
- **What:** When the user previously expanded a node (stored in
  `PropertyTable`), the threshold is overridden.
- **Setup:** Set the expansion key in `PropertyTable` before
  `ShowObject`.

#### 1.8 `ProcessSubpartNode_ThresholdOverride_EmptySequence`
- **What:** An empty `<seq>` with no items produces zero child slices
  (no dummy, no ghost unless configured).
- **Assertions:** Slice count = 0 for the sequence region.

#### 1.9 `AddSimpleNode_IfData_MultiString_EmptyAnalysis`
- **What:** `visibility="ifdata"` on a `MultiString` field with an
  empty analysis writing system. Verify the slice is hidden.
- **Fixture:** Layout with `editor="multistring" ws="analysis"
  visibility="ifdata"`; object has empty analysis WS.

#### 1.10 `AddSimpleNode_IfData_MultiString_NonEmptyVernacular`
- **What:** Same field with data in vernacular WS, but layout
  requests `ws="vernacular"`. Verify the slice is shown.

#### 1.11 `AddSimpleNode_IfData_StText_EmptyParagraph`
- **What:** `StText` field with a single empty paragraph. Verify the
  slice is hidden when `visibility="ifdata"`.
- **Edge case:** The code specifically checks for `StText` with one
  empty paragraph and treats it as empty data.

#### 1.12 `AddSimpleNode_IfData_Summary_SuppressesNode`
- **What:** When a `summary` node attribute is present and the
  referenced data is empty, the summary is suppressed.

#### 1.13 `AddSimpleNode_CustomEditor_ReflectionLookup`
- **What:** When `editor` attribute names a class (e.g.,
  `SIL.FieldWorks.XWorks.MorphologyEditor.PhonologicalRuleFormulaSlice`),
  the factory resolves it via reflection. Verify this path works for
  a known editor class.
- **Note:** This is more of an integration test; may belong in
  `SliceFactoryTests`.

#### 1.14 `ProcessSubpartNode_IfNotCondition`
- **What:** `<ifnot>` node checks `XmlVc.ConditionPasses` and includes
  children only when the condition is _not_ met.
- **Fixture:** Layout with `<ifnot field="..."
  class="LexSense">...</ifnot>`. Test with objects that do and don't
  match.

#### 1.15 `ProcessSubpartNode_ChoiceWhereOtherwise`
- **What:** `<choice>` node evaluates `<where>` clauses in order;
  first match wins; `<otherwise>` is the fallback.
- **Fixture:** Layout with `<choice><where
  class="MoStemAllomorph">...<where class="MoAffixAllomorph">...
  <otherwise>...</otherwise></choice>`.

#### 1.16 `AddAtomicNode_GhostSliceCreation`
- **What:** When an atomic property is null and `ghost="fieldName"` is
  specified, a ghost slice is created to allow inline object creation.
- **Assertions:** Ghost slice has `IsGhostSlice == true`.

#### 1.17 `InterpretLabelAttribute_DollarOwnerPrefix`
- **What:** When `label` attribute starts with `$owner.`, the label is
  resolved by walking up the ownership chain.
- **Fixture:** Layout with `label="$owner.Form"` on a sense-level
  slice. Verify the label comes from the owning entry.

#### 1.18 `SetNodeWeight_ValidWeights`
- **What:** `weight` XML attribute is parsed and applied to slice.
  Test all valid weight values: `field`, `heavy`, `light`.

#### 1.19 `GetFlidIfPossible_CachingBehavior`
- **What:** `GetFlidIfPossible` uses a static `Dictionary` cache
  keyed by `"className-fieldName"`. Verify that repeated calls return
  the cached value. Note potential collision risk if two classes share
  the same field name with different flids — document this risk even
  if we don't fix it.

---

## Subdomain 2: ShowObject & Show-Hidden Fields (`DataTree.Persistence.cs`)

### Existing Tests

| # | Test | Covers |
|---|------|--------|
| 1 | `ShowObject_ShowHiddenEnabledForCurrentTool_...` | Tool-keyed show-hidden |
| 2 | `ShowObject_NoCurrentContentControl_...` | Fallback to lexiconEdit |
| 3 | `ShowObject_NonLexiconCurrentContentControl_...` | Override for LexEntry |
| 4 | `ShowObject_ShowHiddenForDifferentTool_...` | Tool isolation |
| 5 | `ShowObject_ShowHiddenEnabled_RevealsNeverVisibility...` | `visibility="never"` reveal |
| 6 | `OnPropertyChanged_ShowHiddenFields_Toggles...` | Toggle via mediator message |
| 7 | `OnDisplayShowHiddenFields_CheckedState_...` | Menu checked state |
| 8 | `ShowObject_ShowHiddenEnabled_BypassesSliceFilter` | Filter bypass |

### New Tests Needed

#### 2.1 `ShowObject_SameRootAndDescendant_DoesRefreshList`
- **What:** Calling `ShowObject` with the same root, same layout, same
  descendant triggers `RefreshList(false)` not `CreateSlices(true)`.
- **Assertions:** Slice instances survive (reference equality).

#### 2.2 `ShowObject_DifferentRoot_RecreatesAllSlices`
- **What:** Changing the root object disposes old slices and creates
  new ones.
- **Assertions:** Old slice references are disposed; new slices exist.

#### 2.3 `ShowObject_SameRootDifferentDescendant_SetsCurrentSliceNew`
- **What:** Same root but different descendant → only
  `SetCurrentSliceNewFromObject` is called.
- **Assertions:** After idle processing, `CurrentSlice.Object` matches
  the descendant.

#### 2.4 `ShowObject_NoOp_WhenAllParametersUnchanged`
- **What:** Calling `ShowObject` with identical parameters is a no-op.
- **Assertions:** Slice count and references unchanged.

#### 2.5 `RefreshList_DoNotRefresh_DefersRefresh`
- **What:** Set `DoNotRefresh = true`, call `RefreshList`. Verify slices
  are NOT rebuilt. Then set `DoNotRefresh = false` — verify the deferred
  refresh fires.
- **Assertions:** Slice count only changes after `DoNotRefresh = false`.

#### 2.6 `RefreshList_CurrentSliceSurvivesRefresh`
- **What:** If the current slice's identity (type + config + caller +
  label + object GUID) matches a slice after refresh, it remains
  current.
- **Assertions:** `CurrentSlice` after refresh has same `Key` as before.

#### 2.7 `RefreshList_InvalidRoot_CallsReset`
- **What:** If `m_root` is invalid (e.g., deleted), `RefreshList` should
  call `Reset()` and produce zero slices.

#### 2.8 `GetShowHiddenFieldsToolName_LexEntry_FallbackToLexiconEdit`
- **What:** Already partially covered. Add explicit unit test that
  exercises the 4 branches:
  1. `ILexEntry` + no `currentContentControl` → `"lexiconEdit"`
  2. `ILexEntry` + `currentContentControl = "notebookEdit"` →
     `"lexiconEdit"` (override)
  3. `ILexEntry` + `currentContentControl = "lexiconEdit-variant"` →
     `"lexiconEdit-variant"` (starts with `"lexiconEdit"`)
  4. Non-`ILexEntry` + `currentContentControl = "notebookEdit"` →
     `"notebookEdit"` (pass-through)

#### 2.9 `SetCurrentSlicePropertyNames_ConstructsCorrectKeys`
- **What:** Verify the property-table keys follow the pattern
  `{area}${tool}$CurrentSlicePartName` and
  `{area}${tool}$CurrentSliceObjectGuid`.
- **Setup:** Set `areaChoice = "lexicon"`,
  `currentContentControl = "lexiconEdit"` in `PropertyTable`.

---

## Subdomain 3: Slice Refresh & Reuse (`DataTree.SliceManagement.cs`)

### Existing Tests

None directly test reuse. `OwnedObjects` test exercises
`CreateSlices` indirectly.

### New Tests Needed

#### 3.1 `CreateSlices_SliceReuse_SameRootRefresh`
- **What:** After initial `ShowObject`, capture slice references. Call
  `RefreshList(false)`. Verify that slices with matching keys are
  reused (same .NET object reference).
- **Assertions:** `object.ReferenceEquals(oldSlice, newSlice)` for
  matching keys.
- **Fixture:** Simple 2-slice layout.

#### 3.2 `CreateSlices_DifferentObject_NoReuse`
- **What:** Call `ShowObject` with object A, then with object B. Verify
  no reuse (old slices disposed).
- **Assertions:** All old slice references have `IsDisposed == true`.

#### 3.3 `ObjSeqHashMap_RetrievalByKey`
- **What:** Unit test `ObjSeqHashMap` directly — insert keyed slices,
  retrieve by key, verify removed entries are correct.
- **Note:** This is a standalone data-structure test, could go in a
  new `ObjSeqHashMapTests.cs`.

#### 3.4 `ObjSeqHashMap_ClearUnwantedPart_DifferentObject`
- **What:** After `ClearUnwantedPart(true)`, most entries are cleared.
  Verify aggressive cleanup.

#### 3.5 `ObjSeqHashMap_ClearUnwantedPart_SameObject`
- **What:** After `ClearUnwantedPart(false)`, entries are preserved
  for reuse.

#### 3.6 `MonitoredProps_AccumulatesAcrossRefresh`
- **What:** Call `ShowObject`, note `m_monitoredProps` count. Call
  `RefreshList`. Verify props are NOT cleared (they accumulate).
- **Risk documentation:** This is by design but could leak memory over
  very long sessions — document in test comment.

---

## Subdomain 4: Navigation & Focus (`DataTree.Navigation.cs`)

### Existing Tests

None.

### New Tests Needed

#### 4.1 `GotoFirstSlice_SetsCurrentSliceToFirstFocusable`
- **What:** With 5 slices (first is a header, rest are data), call
  `GotoFirstSlice()`. Verify `CurrentSlice` is the first data slice
  (skipping the header).
- **Fixture:** `Nested-Expanded` layout produces a header + children.

#### 4.2 `GotoNextSlice_AdvancesToNextFocusable`
- **What:** Set `CurrentSlice` to slice[1], call `GotoNextSlice()`.
  Verify `CurrentSlice` is slice[2].

#### 4.3 `GotoNextSlice_AtEnd_DoesNotChange`
- **What:** Set `CurrentSlice` to the last slice, call
  `GotoNextSlice()`. Verify `CurrentSlice` is unchanged.

#### 4.4 `GotoPreviousSliceBeforeIndex_ReversesNavigation`
- **What:** Set `CurrentSlice` to slice[2], call
  `GotoPreviousSliceBeforeIndex(2)`. Verify `CurrentSlice` is
  slice[1] (or the nearest focusable before index 2).

#### 4.5 `FocusFirstPossibleSlice_DescendantSpecific`
- **What:** Set `m_descendant` to a specific owned object. Verify
  `FocusFirstPossibleSlice` selects a slice belonging to that
  descendant, not the first slice overall.

#### 4.6 `FocusFirstPossibleSlice_FallsBackToFirstFocusable`
- **What:** Set `m_descendant` to an object without a matching slice.
  Verify focus falls back to the first focusable slice.

#### 4.7 `CurrentSlice_Setter_NullThrows`
- **What:** `CurrentSlice = null` throws `ArgumentException`.
- **Assertions:** `Assert.Throws<ArgumentException>`.

#### 4.8 `CurrentSlice_Setter_FiresChangedEvent`
- **What:** Subscribe to `CurrentSliceChanged`, set `CurrentSlice`.
  Verify event fires.

#### 4.9 `CurrentSlice_Setter_SuspendedStoresInNew`
- **What:** During `m_fSuspendSettingCurrentSlice`, setting
  `CurrentSlice` only stores in `m_currentSliceNew`.

#### 4.10 `DescendantForSlice_RootLevelSlice_ReturnsRoot`
- **What:** For a slice with no `ParentSlice`, `DescendantForSlice`
  returns `m_root`.

#### 4.11 `DescendantForSlice_NestedSlice_ReturnsHeaderObject`
- **What:** For a slice under a header, `DescendantForSlice` returns
  the header's object.

#### 4.12 `MakeSliceRealAt_RealizeDummySlice`
- **What:** At an index containing a `DummyObjectSlice`, calling
  `MakeSliceRealAt` replaces it with a real slice.
- **Assertions:** After call, `Slices[i].IsRealSlice == true`.

---

## Subdomain 5: Messaging & IxCoreColleague (`DataTree.Messaging.cs`)

### Existing Tests

| # | Test | Covers |
|---|------|--------|
| 1 | `OnDisplayShowHiddenFields_CheckedState_...` | Menu display |
| 2 | `OnPropertyChanged_ShowHiddenFields_Toggles...` | Property toggle |

### New Tests Needed

#### 5.1 `GetMessageTargets_VisibleWithCurrentSlice`
- **What:** With a visible DataTree and a current slice, verify
  `GetMessageTargets()` returns `[currentSlice, this]`.
- **Assertions:** Array length == 2; `[0]` is the slice.

#### 5.2 `GetMessageTargets_NotVisible_ReturnsSliceOnly`
- **What:** Hide DataTree, verify `GetMessageTargets()` returns
  only `[currentSlice]` (or empty if no current slice).

#### 5.3 `GetMessageTargets_NoCurrentSlice_ReturnsSelfOnly`
- **What:** Visible DataTree, no current slice. Returns `[this]`.

#### 5.4 `ShouldNotCall_WhenDisposed_ReturnsTrue`
- **What:** After `Dispose()`, `ShouldNotCall` returns `true`.

#### 5.5 `PropChanged_MonitoredProp_TriggersRefresh`
- **What:** Register `MonitorProp(hvo, tag)`. Fire `PropChanged` with
  that pair. Verify `RefreshListAndFocus` is called (or verify the
  observable effect: slices are rebuilt).

#### 5.6 `PropChanged_UnmonitoredProp_NoFullRefresh`
- **What:** Fire `PropChanged` with an unregistered `(hvo, tag)`.
  Verify no full refresh occurs.

#### 5.7 `PropChanged_UnmonitoredDuringUndo_RootVectorChange_FullRefresh`
- **What:** Set up an undo-in-progress state. Fire `PropChanged` on
  the root object's owning sequence. Verify `RefreshList(true)`.

#### 5.8 `DeepSuspendResumeLayout_Counting`
- **What:** Call `DeepSuspendLayout()` twice, then `DeepResumeLayout()`
  twice. Verify layout is only resumed on the second resume call.
- **Assertions:** After first resume, layout is still suspended.

#### 5.9 `OnPropertyChanged_UnknownProperty_NoOp`
- **What:** Call `OnPropertyChanged("someRandomProperty")`. Verify no
  side effects (no refresh, no exception).

#### 5.10 `PostponePropChanged_DefersRefresh`
- **What:** Set `m_postponePropChanged = true` (via the event), fire
  `PropChanged` on a monitored prop. Verify `BeginInvoke` is used
  rather than a synchronous call.
- **Note:** Hard to test directly; may need to verify via a flag or
  mock the control's BeginInvoke.

---

## Subdomain 6: Lifecycle & Persistence (`DataTree.cs` core)

### Existing Tests

None directly test lifecycle. Setup/teardown exercises init + dispose.

### New Tests Needed

#### 6.1 `Initialize_SetsRequiredFields`
- **What:** After `Initialize(cache, false, layouts, parts)`, verify
  `m_cache`, `m_mdc`, `m_sda` are set.

#### 6.2 `Dispose_UnsubscribesNotifications`
- **What:** After `Dispose()`, verify `m_sda.RemoveNotification` was
  called (the DataTree no longer receives property changes).

#### 6.3 `Dispose_CurrentSlice_GetsDeactivated`
- **What:** Set a current slice, then `Dispose()`. Verify the slice
  received `SetCurrentState(false)`.

#### 6.4 `Dispose_DoubleDispose_IsNoOp`
- **What:** Call `Dispose()` twice. No exception thrown.

#### 6.5 `CheckDisposed_AfterDispose_Throws`
- **What:** After `Dispose()`, calling `CheckDisposed()` throws
  `ObjectDisposedException`.

---

## Subdomain 7: JumpToTool (`DataTree.Messaging.cs`)

### Existing Tests

| # | Test | Covers |
|---|------|--------|
| 1 | `GetGuidForJumpToTool_UsesRootObject_WhenNoCurrentSlice` | Null current slice → root GUID |

### New Tests Needed

#### 7.1 `GetGuidForJumpToTool_ConcordanceTool_LexEntry`
- **What:** With `tool = "concordance"`, current slice on a `LexEntry`.
  Verify the resolved GUID is the entry's GUID.

#### 7.2 `GetGuidForJumpToTool_ConcordanceTool_LexSense`
- **What:** Current slice on a `LexSense` under the entry. Verify
  the GUID is the sense's GUID.

#### 7.3 `GetGuidForJumpToTool_LexiconEditTool`
- **What:** With `tool = "lexiconEdit"`. Verify the GUID resolution
  walks ownership to find the owning `LexEntry`.

#### 7.4 `GetGuidForJumpToTool_ForEnableOnly_DoesNotCreate`
- **What:** With `forEnableOnly = true` and a tool that might create
  an object (e.g., `notebookEdit`). Verify no object creation occurs.

---

## Test Count Summary

| Subdomain | Existing | New | Total |
|-----------|----------|-----|-------|
| XML Layout Parsing | 8 | 19 | 27 |
| ShowObject & Show-Hidden | 8 | 9 | 17 |
| Slice Refresh & Reuse | 0 | 6 | 6 |
| Navigation & Focus | 0 | 12 | 12 |
| Messaging & IxCoreColleague | 2 | 10 | 12 |
| Lifecycle & Persistence | 0 | 5 | 5 |
| JumpToTool | 1 | 4 | 5 |
| **Total** | **19** | **65** | **84** |

---

## Priority Order for Implementation

1. **Navigation & Focus** (12 tests) — zero coverage today, critical
   for user-facing behavior during Avalonia migration
2. **Slice Refresh & Reuse** (6 tests) — zero coverage, `ObjSeqHashMap`
   is a complex data structure with subtle semantics
3. **XML Layout Parsing** (19 new tests) — partial coverage exists
   but the threshold logic, `ifData` variants, and ghost slices are
   unprotected
4. **Messaging** (10 new tests) — `PropChanged` and
   `DeepSuspendLayout` have re-entrancy risks
5. **ShowObject extensions** (9 new tests) — good existing coverage,
   but root-change and no-op paths are untested
6. **Lifecycle** (5 tests) — simple but important for dispose safety
7. **JumpToTool** (4 tests) — lower risk, specialized feature
