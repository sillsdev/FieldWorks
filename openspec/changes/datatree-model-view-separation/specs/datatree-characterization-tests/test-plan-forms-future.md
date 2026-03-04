# Test Plan: Headless DataTree Model (Approach 3 — Future)

## Goal

Fully separate the model layer from the view layer so that all
layout-engine logic, slice metadata computation, visibility rules,
navigation, and property-change handling can be tested as pure C#
classes with **zero WinForms dependency**.

This is the end-state architecture for the `datatree-model-view`
branch. It builds on Approach 2 (the `IDataTreePainter` seam) and
replaces the current god-class `DataTree : UserControl` with a
model+view pair.

---

## 1. Architecture

### 1.1 Target Decomposition

```
┌──────────────────────────────────────────────┐
│  DataTreeView : UserControl                  │
│  - Hosts WinForms controls (SplitContainers) │
│  - Painting (OnPaint, DrawLabel)             │
│  - Focus management                          │
│  - Scroll position                           │
│  - Delegates to DataTreeModel for all logic  │
└──────────┬───────────────────────────────────┘
           │ uses
┌──────────▼───────────────────────────────────┐
│  DataTreeModel (plain C#, no UI dependency)  │
│  - Layout engine: XML → List<SliceSpec>      │
│  - Visibility rules (ifdata, never, hidden)  │
│  - Navigation (current index, next, prev)    │
│  - ObjSeqHashMap (slice reuse)               │
│  - Property change → refresh decisions       │
│  - Expand/collapse state                     │
│  - MonitoredProps tracking                   │
└──────────────────────────────────────────────┘
```

### 1.2 SliceSpec Data Object

The core output of the layout engine, replacing the current Slice
class for model-level concerns:

```csharp
public class SliceSpec
{
    public string Label { get; set; }
    public string Abbreviation { get; set; }
    public int Indent { get; set; }
    public int Flid { get; set; }
    public string FieldName { get; set; }
    public string EditorType { get; set; }
    public SliceVisibility Visibility { get; set; }
    public ObjectWeight Weight { get; set; }
    public int ObjectHvo { get; set; }
    public Guid ObjectGuid { get; set; }
    public XmlNode ConfigurationNode { get; set; }
    public ArrayList Key { get; set; }
    public Type SliceType { get; set; } // e.g. typeof(StringSlice)
}
```

### 1.3 New Project

```
SIL.FieldWorks.Common.Controls.DetailControls.Model
├── DataTreeModel.cs
├── SliceSpec.cs
├── SliceVisibility.cs
├── LayoutEngine.cs        // XML → SliceSpec list
├── NavigationModel.cs     // current, next, prev
├── SliceReuseMap.cs       // extracted from ObjSeqHashMap
└── VisibilityRules.cs     // ifdata, never, show-hidden
```

**Dependencies:** `SIL.LCModel`, `System.Xml` — no `System.Windows.Forms`.

---

## 2. Extraction Strategy

### Phase 1: Extract Layout Engine

The core pure function buried in DataTree:

```
(XML layouts, XML parts, LCM cache, object, layoutName)
  → ordered List<SliceSpec>
```

This is currently spread across:
- `DataTree.CreateSlicesFor` (line ~1800)
- `DataTree.ProcessPartChildren` (line ~2000)
- `DataTree.AddAtomicNode` (line ~2420)
- `DataTree.AddSeqNode` (line ~2500)
- `DataTree.MakeGhostSlice` (line ~2458)
- `SliceFactory.Create` (external)

**Extraction approach:** Create `LayoutEngine.ComputeSliceSpecs(...)` that
returns `List<SliceSpec>`. DataTree calls this and then creates actual
WinForms Slice controls from the specs.

### Phase 2: Extract Navigation Model

Currently: `CurrentSlice`, `GotoNextSlice`, `GotoPreviousSliceBeforeIndex`,
`FocusFirstPossibleSlice` are all on DataTree and mix model state
(which slice is current) with view behavior (focus, scrolling).

**Extraction:** `NavigationModel` holds the current index and provides
`MoveNext()`, `MovePrevious()`, `MoveToFirst()` operating on
`List<SliceSpec>`. The view layer translates model index changes into
focus/scroll actions.

### Phase 3: Extract Visibility Rules

Currently: visibility logic is scattered across `ShowObject`,
`HandleShowHiddenFields`, `ProcessPartChildren`, and individual slice
checks.

**Extraction:** `VisibilityRules.ShouldShow(SliceSpec, bool showHidden)`
is a pure function. The view layer calls it during layout.

### Phase 4: Extract Slice Reuse

`ObjSeqHashMap` already exists as a separate class. Rename to
`SliceReuseMap`, clean up its API, and make it part of the model
project. Already 90% covered by existing tests.

---

## 3. Test Strategy

### 3.1 Golden-File / Snapshot Tests

For each real layout in `DistFiles/Language Explorer/Configuration/`:

1. Run `LayoutEngine.ComputeSliceSpecs(cache, layout, testObject)`
2. Serialize the result to JSON: `[{label, indent, editorType, flid, objectClass}]`
3. Compare against a golden file checked into the repo

**Benefits:**
- Tests the real combinatorial complexity (`<choice>`, `<where>`,
  `<ifnot>`, `<indent>`, multi-level ownership)
- No WinForms, no Form, no Graphics
- Regressions are immediately visible as golden-file diffs
- Covers thousands of lines of XML parsing logic

### 3.2 Unit Tests for Pure Model Classes

```csharp
[Test]
public void LayoutEngine_CfOnly_ProducesOneSliceSpec()
{
    var engine = new LayoutEngine(layouts, parts);
    var specs = engine.ComputeSliceSpecs(cache, entry, "CfOnly");

    Assert.That(specs.Count, Is.EqualTo(1));
    Assert.That(specs[0].Label, Is.EqualTo("Citation Form"));
    Assert.That(specs[0].EditorType, Is.EqualTo("multistring"));
}

[Test]
public void NavigationModel_MoveNext_AdvancesIndex()
{
    var nav = new NavigationModel(specs);
    nav.MoveTo(0);
    nav.MoveNext();
    Assert.That(nav.CurrentIndex, Is.EqualTo(1));
}

[Test]
public void VisibilityRules_IfData_EmptyField_ReturnsFalse()
{
    var spec = new SliceSpec { Visibility = SliceVisibility.IfData };
    // ... setup empty field
    Assert.That(VisibilityRules.ShouldShow(spec, cache, showHidden: false),
        Is.False);
}
```

### 3.3 Integration Tests (from testing-approach-2.md §2.5)

Small number of end-to-end tests that exercise real workflows:

1. **Show → Edit → Refresh**: ShowObject, modify citation form via
   LCM, trigger PropChanged, verify slice list is refreshed correctly
2. **Large list + scroll**: 30+ senses, verify DummyObjectSlice →
   BecomeReal at correct indices
3. **Toggle show-hidden**: Flip property, verify correct slices appear/disappear
4. **Switch objects**: Show entry A, then show entry B, verify clean
   slate with correct slices

These tests use `DataTreeModel` directly — no WinForms needed.

---

## 4. Migration Path from Approach 2

| Approach 2 artifact | Evolves into |
|---------------------|-------------|
| `IDataTreePainter` interface | View-layer contract |
| `RecordingPainter` test double | View-layer test infrastructure |
| `OffscreenGraphicsContext` | View-layer test infrastructure |
| `HandlePaintLinesBetweenSlices` (internal) | `DataTreeView.PaintLines(...)` |
| `HandleLayout1` (protected internal) | `DataTreeView.PerformSliceLayout(...)` |
| Behavioral contract tests | Move to model test project |
| Static helpers (IsChildSlice, SameSourceObject) | `LayoutEngine` or `NavigationModel` |

Tests written under Approach 2 with `[Category("SurvivesRefactoring")]`
are expected to survive by moving to the model test project with
minimal changes.

---

## 5. Timeline and Prerequisites

| Step | Prerequisite | Effort |
|------|-------------|--------|
| Approach 2 (current plan) | None | Small (current sprint) |
| Phase 1: Layout Engine extraction | Approach 2 done, high test coverage | Large (dedicated sprint) |
| Phase 2: Navigation Model | Phase 1 | Medium |
| Phase 3: Visibility Rules | Phase 1 | Medium |
| Phase 4: Slice Reuse cleanup | Phase 1 | Small |
| Golden-file test suite | Phase 1 | Medium |
| DataTreeView refactoring | Phases 1–4 | Large |

---

## 6. Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Layout engine extraction is complex (4700-line class) | High | Incremental: extract one method at a time, verify coverage after each |
| XML config nodes are used by both model and view | High | SliceSpec captures all config data needed by view; config nodes stay in model |
| SliceFactory creates actual WinForms controls | High | Split into SpecFactory (model) + ControlFactory (view) |
| Real layouts may have undocumented edge cases | Medium | Golden-file tests catch regressions from day one |
| Performance regression from extra allocation | Low | SliceSpec is a simple POCO; profile if needed |
