# Test Plan: Offscreen UI & Rendering Tests (Approach 2)

## Goal

Exercise DataTree and Slice painting, layout, visibility, and control
behavior without showing windows on screen. Uses bitmap-backed
`Graphics`, `SetVisibleCore` reflection, and a small interface
extraction to create a testable seam for rendering.

This plan also incorporates actionable, non-breaking improvements from
`testing-approach-2.md` §2.1–§2.6.

---

## 1. Production Code Changes (Non-Breaking)

### 1.1 Extract `IDataTreePainter` Interface

A small interface that abstracts the line-drawing logic between slices.
DataTree implements it as its default behavior. Tests can substitute a
recording implementation.

```csharp
// New file: IDataTreePainter.cs
namespace SIL.FieldWorks.Common.Framework.DetailControls
{
    /// <summary>
    /// Abstracts the painting operations that DataTree performs between
    /// slices, allowing tests to intercept/record draw calls without a
    /// real screen.
    /// </summary>
    public interface IDataTreePainter
    {
        /// <summary>
        /// Paint separator lines between slices.
        /// </summary>
        void PaintLinesBetweenSlices(Graphics gr, int width);
    }
}
```

**DataTree changes:**
- Add `public IDataTreePainter Painter { get; set; }` property,
  defaulting to `this` in the constructor.
- DataTree implements `IDataTreePainter` explicitly.
- `OnPaint` delegates to `Painter.PaintLinesBetweenSlices(...)` instead
  of calling `HandlePaintLinesBetweenSlices` directly.
- `HandlePaintLinesBetweenSlices` becomes `internal` (was `private`).

This is backward-compatible: existing code sees no difference because
the Painter defaults to the DataTree itself.

### 1.2 Make Private Methods Internal

The following methods change from `private` to `internal` to allow
direct testing via the existing `InternalsVisibleTo("DetailControlsTests")`:

| Method | Current | New | Rationale |
|--------|---------|-----|-----------|
| `HandlePaintLinesBetweenSlices` | private | internal | Direct bitmap test |
| `SameSourceObject` | private static | internal static | Unit test paint logic |
| `IsChildSlice` | private static | internal static | Unit test paint logic |

### 1.3 Test Category Attributes

Add `[Category]` attributes to existing and new tests for lifespan
tracking during the refactoring (from testing-approach-2.md §2.6):

| Category | Meaning |
|----------|---------|
| `SurvivesRefactoring` | Tests behavioral contracts preserved across model/view split |
| `PreRefactoring` | Tests documenting current internals; expected to be rewritten |
| `KnownBug` | Tests that document bugs as current behavior |
| `OffscreenUI` | Tests exercising painting/layout/visibility without a screen |

---

## 2. Test Infrastructure

### 2.1 `OffscreenGraphicsContext` (test helper)

Disposable helper that creates a `Bitmap` + `Graphics` + synthetic
`PaintEventArgs` for offscreen rendering:

```csharp
internal class OffscreenGraphicsContext : IDisposable
{
    public Bitmap Bitmap { get; }
    public Graphics Graphics { get; }

    public OffscreenGraphicsContext(int width = 800, int height = 600)
    {
        Bitmap = new Bitmap(width, height);
        Graphics = Graphics.FromImage(Bitmap);
    }

    public PaintEventArgs CreatePaintEventArgs()
    {
        return new PaintEventArgs(Graphics,
            new Rectangle(0, 0, Bitmap.Width, Bitmap.Height));
    }

    public PaintEventArgs CreatePaintEventArgs(Rectangle clip)
    {
        return new PaintEventArgs(Graphics, clip);
    }

    public void Dispose()
    {
        Graphics?.Dispose();
        Bitmap?.Dispose();
    }
}
```

Pattern inspired by `VwGraphicsTests.GraphicsObjectFromImage` which
already uses `Bitmap(1000,1000)` + `Graphics.FromImage` for offscreen
VwGraphics testing.

### 2.2 `RecordingPainter` (test double)

Records all paint operations for assertion without drawing:

```csharp
internal class RecordingPainter : IDataTreePainter
{
    public List<(Point From, Point To, float PenWidth)> DrawnLines { get; } = new();
    public int PaintCallCount { get; private set; }

    public void PaintLinesBetweenSlices(Graphics gr, int width)
    {
        PaintCallCount++;
        // Optionally record using a recording Graphics wrapper
    }
}
```

### 2.3 Visibility Helper (existing)

Already implemented in `DataTreeTests`:
```csharp
SetControlVisibleForTest(Control control, bool visible)
// Uses reflection to call Control.SetVisibleCore(bool)
```

This makes controls report `Visible=true` without `Form.Show()`.

---

## 3. Test Plan

### 3.1 Slice.DrawLabel — Bitmap Rendering

| # | Test | Asserts |
|---|------|---------|
| 1 | `DrawLabel_WithLabel_DrawsToGraphics` | Call `DrawLabel(0, 0, gr, 800)` on a Slice with `Label="Test"`. Verify no exception and that the bitmap has non-white pixels. |
| 2 | `DrawLabel_WithAbbreviation_UsesAbbrevWhenNarrow` | Set `SplitCont.SplitterDistance` ≤ `MaxAbbrevWidth`. Verify `DrawLabel` uses `Abbreviation` text. |
| 3 | `DrawLabel_WithSmallImages_DrawsIconBeforeText` | Set `SmallImages` to a real `ImageCollection`. Verify icon is drawn at (x,y) and text starts after icon width. |
| 4 | `DrawLabel_NullLabel_DoesNotThrow` | `Label = null`, call `DrawLabel`. Document current behavior (likely NRE — characterize it). |

**Category:** `OffscreenUI`, `SurvivesRefactoring`

### 3.2 DataTree.HandlePaintLinesBetweenSlices — Offscreen Lines

| # | Test | Asserts |
|---|------|---------|
| 5 | `HandlePaintLinesBetweenSlices_TwoSlices_DrawsLine` | Initialize DataTree + ShowObject with "CfAndBib" layout (2 slices). Force visibility. Create bitmap Graphics + PaintEventArgs. Call `HandlePaintLinesBetweenSlices`. Verify bitmap has drawn pixels between slice positions. |
| 6 | `HandlePaintLinesBetweenSlices_SingleSlice_DrawsNothing` | ShowObject with "CfOnly" (1 slice). Verify no lines drawn. |
| 7 | `HandlePaintLinesBetweenSlices_HeaderSlice_SkipsLine` | Configure a slice with `header="true"` attribute. Verify line is skipped per the existing logic. |
| 8 | `PaintLinesBetweenSlices_ViaInterface_DelegatesToPainter` | Set `Painter` to `RecordingPainter`. Trigger paint. Assert `PaintCallCount == 1`. |

**Category:** `OffscreenUI`, `SurvivesRefactoring`

### 3.3 DataTree Layout — HandleLayout1 with Forced Visibility

| # | Test | Asserts |
|---|------|---------|
| 9 | `HandleLayout1_PositionsSlicesVertically` | Initialize + ShowObject. `SetVisibleCore(true)` on parent and DataTree. Call `HandleLayout1(true, ClientRectangle)`. Assert each slice's `Top` is greater than previous slice's `Top + Height`. |
| 10 | `HandleLayout1_HeavyWeightSlice_AddsMargin` | Create a slice with `Weight = ObjectWeight.heavy`. Verify the gap includes `HeavyweightRuleThickness + HeavyweightRuleAboveMargin`. |

**Category:** `OffscreenUI`, `SurvivesRefactoring`

### 3.4 Behavioral Contract Tests (from testing-approach-2.md §2.1)

These test invariants that survive the model/view split:

| # | Test | Contract |
|---|------|----------|
| 11 | `ShowObject_ProducesCorrectSliceOrder` | Given layout XML + object → correct ordered list of labels |
| 12 | `VisibilityIfData_HidesWhenEmpty` | `visibility="ifdata"` hides when data is empty |
| 13 | `ShowHiddenFields_TogglesVisibility` | Setting ShowHiddenFields property shows/hides "never" fields |
| 14 | `Expand_CreatesChildSlices` | Expanding a collapsed node creates child slices in correct order |
| 15 | `Collapse_RemovesChildSlices` | Collapsing removes descendant slices from the tree |

**Category:** `SurvivesRefactoring`

### 3.5 Static Helper Tests (IsChildSlice, SameSourceObject)

| # | Test | Asserts |
|---|------|---------|
| 16 | `IsChildSlice_MatchingPrefix_ReturnsTrue` | Two slices where second's key extends first's |
| 17 | `IsChildSlice_DifferentKeys_ReturnsFalse` | Non-matching key prefixes |
| 18 | `SameSourceObject_SameHvo_ReturnsTrue` | Same Object.Hvo |
| 19 | `SameSourceObject_DifferentHvo_ReturnsFalse` | Different Object.Hvo |

**Category:** `SurvivesRefactoring`

---

## 4. File Layout

| File | Contents |
|------|----------|
| `IDataTreePainter.cs` | Interface (production, DetailControls project) |
| `DataTree.cs` | Implement interface, add Painter property, widen visibility |
| `DataTreeTests.Wave4.OffscreenUI.cs` | Tests §3.1–3.5 above |
| `OffscreenGraphicsContext.cs` | Test helper (DetailControlsTests project) |
| `RecordingPainter.cs` | Test double (DetailControlsTests project) |

---

## 5. Implementation Order

1. Create `IDataTreePainter.cs` interface
2. DataTree: implement interface, add `Painter` property, widen method visibility
3. Create `OffscreenGraphicsContext.cs` test helper
4. Create `RecordingPainter.cs` test double
5. Create `DataTreeTests.Wave4.OffscreenUI.cs` with initial tests
6. Build + verify all existing tests still pass
7. Run coverage assessment to measure improvement

## 6. Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| `SetVisibleCore` may trigger unintended side effects | Already proven safe in existing Wave3 tests |
| Bitmap-based Graphics may differ from screen Graphics | We test structure (pixels drawn vs. not drawn), not exact rendering |
| Interface extraction could break callers | Default implementation is DataTree itself — fully backward compatible |
| Tests may be fragile on CI (headless Windows) | WinForms handle creation works without a desktop session on Windows |
