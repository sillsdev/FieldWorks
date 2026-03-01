# Timing Enhancements

## 2026-02-20 — DataTree timing optimization pass

### Enhancement 1: Bulk tab-index reset during slice construction

#### 1) What changed
- File: `Src/Common/Controls/DetailControls/DataTree.cs`
- `InsertSlice(int index, Slice slice)` now skips per-insert `ResetTabIndices(index)` while `ConstructingSlices == true`.
- `CreateSlices(bool differentObject)` now performs the single `ResetTabIndices(0)` in `finally` to keep indices correct even on error paths.

#### 2) How it improved timing
- Goal: avoid repeated tab-index walks during bulk slice generation by doing one bulk renumber pass.
- Also improves robustness: if `CreateSlicesFor` throws, tab-index normalization still executes from `finally`.

#### 3) Measured timing savings / inner metrics
- Baseline file: `Output/RenderBenchmarks/datatree-timings-baseline-before-opt1.json`
- After file: `Output/RenderBenchmarks/datatree-timings.json`
- Current run deltas:
  - `timing-extreme` populate: **102.9 ms -> 98.3 ms** (**+4.5% faster**, +4.6 ms)
  - `timing-deep` and `timing-shallow` totals were noisy/regressed on this workstation run, so no reliable end-to-end gain claim for those scenarios.
- Inner-call metric:
  - Worst-case (head insertions into a 253-slice set) changes `SetTabIndex` work from O(N^2) to O(N), saving up to **32,131** `SetTabIndex` calls.
  - In the current timing scenarios most insertions are append-like, so realized savings are smaller than worst-case.

#### 4) Why this is safe architecturally
- Keeps existing `DataTree` responsibility boundaries intact (no new coupling).
- Preserves behavior contract by guaranteeing a full tab-index normalization pass before `ConstructingSlices` is cleared.
- Devil’s-advocate concern addressed: partial-failure path could leave inconsistent tab order; mitigation implemented by moving the final reset into `finally`.

---

### Enhancement 2: Suspend layout during `DummyObjectSlice.BecomeReal()` expansion

#### 1) What changed
- File: `Src/Common/Controls/DetailControls/DataTree.cs`
- In `DummyObjectSlice.BecomeReal(int index)`, wrapped `CreateSlicesFor(...)` with:
  - `containingTree.DeepSuspendLayout();`
  - `try { ... } finally { containingTree.DeepResumeLayout(); }`

#### 2) How it improved timing
- Prevents unsuspended, per-slice layout work during lazy expansion when dummy slices become real (scroll-triggered/visibility-triggered path).
- Aligns this path with existing `ShowObject()` and `RefreshList()` behavior, both of which already use deep layout suspension.

#### 3) Measured timing savings / inner metrics
- `DataTreeTiming*` scenarios do not currently force dummy-slice expansion (`kInstantSliceMax` path is not exceeded), so direct wall-clock impact is not represented in this run.
- Inner-call metric (architectural/mechanical): for N slices materialized in `BecomeReal`, layout pass pressure is reduced from per-insert behavior toward a single resumed layout pass (O(N) -> O(1) layout-pass count class).
- For a hypothetical 253-slice lazy expansion, this avoids up to ~252 intermediate layout-pass opportunities.

#### 4) Why this is safe architecturally
- Uses existing `DataTree` layout-suspension abstraction (`DeepSuspendLayout`/`DeepResumeLayout`) rather than introducing new state.
- Preserves existing call graph (`BecomeReal -> CreateSlicesFor`) and only scopes layout batching around it.
- Devil’s-advocate concern addressed: missing resume on exceptions could corrupt layout state; mitigation implemented with `try/finally`.

---

## Devil’s-advocate stage summary

### Risks considered
- Hidden behavioral change to tab order / keyboard navigation.
- Exception-path invariant breakage during slice construction.
- Reentrancy/layout-state imbalance in lazy expansion.
- Over-optimization without measurable gains in current benchmark shape.

### Actions taken
- Added final tab-index normalization in `CreateSlices(...).finally`.
- Wrapped `BecomeReal()` expansion in `DeepSuspendLayout/DeepResumeLayout` with `try/finally`.
- Rebuilt and reran `DataTreeTiming` targeted tests after changes.

### Verification
- `./build.ps1 -BuildTests` succeeded.
- `./test.ps1 -TestFilter "DataTreeTiming" -NoBuild` passed.

---

## 2026-02-20 — Paint pipeline optimization pass

### Test coverage assessment (before optimization)

**Existing tests exercising the target code path (HandlePaintLinesBetweenSlices):**
- 6 snapshot tests (`DataTreeRender_Simple`, `_Deep`, `_Extreme`, `_Collapsed`, `_Expanded`, `_MultiWs`) — each calls `CaptureCompositeBitmap()` → `DrawToBitmap` → `OnPaint` → `HandlePaintLinesBetweenSlices`. Full bitmap comparison (Verify) validates separator line correctness.
- 4 timing tests exercise populate + capture pipeline.

**Coverage gaps identified:**
- No test measuring paint-time independently of populate-time.
- No test for partial repaint (clip-rect smaller than full area, e.g., scrolling).

**Tests added before optimization:**
- `DataTreeTiming_PaintPerformance` — measures capture time for the extreme scenario (126 senses), exercises the full `OnPaint` → `HandlePaintLinesBetweenSlices` pipeline. Ran green against unmodified code.

### Enhancement 3: Clip-rect culling in HandlePaintLinesBetweenSlices

#### 1) What changed
- File: `Src/Common/Controls/DetailControls/DataTree.cs`
- `HandlePaintLinesBetweenSlices(PaintEventArgs pea)` now uses `pea.ClipRectangle` to skip slices whose separator lines fall entirely outside the paint region.
- Slices above the paint region are skipped with `continue`.
- Once a slice's Y position exceeds `clipRect.Bottom`, the loop exits with `break` (safe because HandleLayout1 positions slices in monotonically increasing Y order).

#### 2) How it improved timing
- During full repaints (window restore, full `Invalidate()`), clip rect = full client area, so all separator lines are drawn (no change).
- During partial repaints (scrolling, localized `Invalidate(region)`), only separator lines in the exposed strip are drawn. For an entry with N slices and a visible strip showing K slices, this reduces the per-paint iteration from O(N) to O(K), plus avoids O(N-K) `XmlUtils.GetOptionalBooleanAttributeValue` XML attribute parsing calls on off-screen slices.

#### 3) Measured timing savings / inner metrics
- Baseline file: `Output/RenderBenchmarks/datatree-timings-baseline-before-paint-opt.json`
- For the extreme scenario (253 slices), a scroll repaint that shows ~15 visible slices would iterate only ~15 slices instead of 253 — an **~94% reduction** in separator-line work per partial repaint.
- Full-repaint benchmarks (DrawToBitmap) show no timing change because clip rect = full area.

#### 4) Why this is safe architecturally
- Visual output is identical: GDI+ already clips DrawLine calls outside the clip rect; this optimization avoids calling DrawLine and XML parsing in the first place.
- The `break` relies on HandleLayout1's monotonic Y positioning invariant, which is maintained by the sequential `yTop += tci.Height + 1` accumulation in HandleLayout1.
- 6 existing snapshot tests validate full bitmap output including all separator lines.

---

### Enhancement 4: Enable OptimizedDoubleBuffer on DataTree

#### 1) What changed
- File: `Src/Common/Controls/DetailControls/DataTree.cs`
- Added `SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true)` in the DataTree constructor.

#### 2) How it improved timing
- Eliminates flicker during paint by compositing the DataTree's own painting (background + separator lines) to an offscreen buffer before blitting to screen.
- `AllPaintingInWmPaint` suppresses the WM_ERASEBKGND erase-then-paint cycle, preventing the visible "white flash" before separator lines are drawn.
- Does NOT affect child control painting (slices paint themselves independently).

#### 3) Measured timing savings / inner metrics
- This is primarily a visual quality improvement (flicker elimination), not a wall-clock speed improvement.
- Memory overhead: one offscreen bitmap per DataTree (~3MB at 1024×768×32bpp). Negligible.

#### 4) Why this is safe architecturally
- `base.OnPaint(e)` in DataTree's OnPaint paints the background with BackColor, so suppressing WM_ERASEBKGND does not leave unpainted areas.
- The re-entrant paint path (klsChecking state) calls `Invalidate(false)` and returns without painting — this is unchanged; the next normal paint fills the buffer correctly.
- Standard WinForms optimization pattern used widely in the framework.

---

## Devil's-advocate stage summary

### Risks considered
- `break` in separator culling assumes monotonic slice Y ordering (invariant maintained by HandleLayout1).
- Heavy-weight rule lines extend beyond slice bounds — accounted for by `maxLineExtent` padding.
- AllPaintingInWmPaint could leave unpainted areas if OnPaint doesn't call base — confirmed base.OnPaint is called in the normal path.
- Re-entrant paint path does not call base.OnPaint — unchanged behavior; next paint corrects it.

### Actions taken
- Added `maxLineExtent` constant accounting for `HeavyweightRuleThickness + HeavyweightRuleAboveMargin` in culling check.
- Used `continue` (not `break`) for above-clip slices to be safe regardless of ordering.
- Added `DataTreeTiming_PaintPerformance` test for paint-time regression detection.

### Verification
- `./build.ps1 -BuildTests` succeeded.
- `./test.ps1 -TestFilter "DataTreeTiming" -NoBuild` passed (5 tests: 3 timing + 1 workload + 1 paint perf).
---

## 2026-02-21 — Layout path optimization pass

### Enhancement 5: Early-exit guard in Slice.SetWidthForDataTreeLayout

#### 1) What changed
- File: `Src/Common/Controls/DetailControls/Slice.cs`
- `SetWidthForDataTreeLayout(int width)` now returns immediately when `m_widthHasBeenSetByDataTree && Width == width`, avoiding redundant splitter resizing and event handler churn on every layout pass.

#### 2) How it improved timing
- During `HandleLayout1`, every slice receives `SetWidthForDataTreeLayout(desiredWidth)` on each layout pass. After the first pass, the width is typically stable, so subsequent calls perform `SplitContainer.SplitterDistance` manipulation and `SizeChanged` event detach/reattach for no effect.
- For N slices with stable width, this saves N × (event unsubscribe + Width set + SplitterDistance set + event resubscribe) per layout pass — roughly **O(N) work eliminated per steady-state layout**.

#### 3) Measured timing savings / inner metrics
- Baseline file: `Output/RenderBenchmarks/datatree-timings-baseline-before-layout-opt.json`
- Inner-call metric: For 253 slices, each subsequent layout pass skips 253 × 4 operations (unsubscribe, Width set, SplitterDistance set, resubscribe).
- `ViewSlice` already had its own `Width == width` guard in its override; this extends the same pattern to the base `Slice` class.

#### 4) Why this is safe architecturally
- The `m_widthHasBeenSetByDataTree` flag is set on first successful call, ensuring the guard only activates after initial setup.
- `Slice.OnSizeChanged` event handler (subscribed during the first call) keeps `m_splitter.Size` synchronized with control width changes from external sources (parent resize, etc.).
- `ViewSlice` and `InterlinearSlice` overrides call `base.SetWidthForDataTreeLayout(width)`, so the guard protects all slice types.

---

### Enhancement 6: Skip OnSizeChanged splitter loop during ConstructingSlices

#### 1) What changed
- File: `Src/Common/Controls/DetailControls/DataTree.cs`
- `OnSizeChanged(EventArgs e)` now returns early (after calling `base.OnSizeChanged(e)`) when `ConstructingSlices` is true.

#### 2) How it improved timing
- During bulk slice construction, each `Controls.Add(slice)` can trigger `OnSizeChanged` on the DataTree. The handler walks all slices calling `AdjustSliceSplitPosition(slice)` — O(N) per trigger.
- With this guard, the O(N) loop is deferred until construction completes. `HandleLayout1(fFull=true)` called from `OnLayout` after `DeepResumeLayout` handles width synchronization for all slices.
- For 253 slices, this avoids up to 253 × O(N) = O(N²) split-position adjustments during initial load.

#### 3) Measured timing savings / inner metrics
- The optimization targets the construction-time hot path. `InstallSlice` already calls `AdjustSliceSplitPosition` individually, and `HandleLayout1(fFull=true)` calls `SetWidthForDataTreeLayout` for all slices after construction, so correctness is maintained.

#### 4) Why this is safe architecturally
- `ConstructingSlices` is already a well-established guard flag used by `InsertSlice` (tab-index skip) and the layout system.
- `base.OnSizeChanged(e)` is still called (fires the `SizeChanged` event for external subscribers).
- After construction completes, the next `OnLayout` → `HandleLayout1(fFull=true)` pass synchronizes all slice widths.

---

### Enhancement 7: Skip SetWidthForDataTreeLayout for non-visible slices in paint path

#### 1) What changed
- File: `Src/Common/Controls/DetailControls/DataTree.cs`
- In `HandleLayout1`, the `SetWidthForDataTreeLayout(desiredWidth)` call is now guarded by `if (fFull || fSliceIsVisible)`, so the paint path (`fFull=false`) only synchronizes width for visible slices.

#### 2) How it improved timing
- `HandleLayout1` is called from two paths: `OnLayout` with `fFull=true` (ensures all slices have correct width) and `OnPaint` with `fFull=false` (ensures visible slices are real + positioned).
- In the paint path, off-screen slices don't need width synchronization — they'll get it on the next full layout pass or when they scroll into view.
- For 253 slices with ~15 visible, this skips ~238 `SetWidthForDataTreeLayout` calls per paint — **~94% reduction** in width-sync work per partial paint.

#### 3) Measured timing savings / inner metrics
- Combined with Enhancement 5 (early-exit guard), visible slices that already have correct width will also short-circuit, making the paint-path width sync effectively zero-cost for stable layouts.

#### 4) Why this is safe architecturally
- `OnLayout` always calls `HandleLayout1(fFull=true)`, which synchronizes all slice widths unconditionally.
- The paint path only needs slices to be real and positioned (for `BecomeReal` and Y-positioning); width is a layout concern, not a paint concern.
- The `fSliceIsVisible` variable was already computed in HandleLayout1 for the `BecomeReal` decision, so this adds no new computation.

---

## Devil's-advocate stage summary

### Risks considered
- Enhancement 5: Could the early-exit miss a width change from an external source? No — `Slice.OnSizeChanged` event handler re-syncs `m_splitter.Size` on any resize, and the next `HandleLayout1` call with the new `desiredWidth` would bypass the guard since `Width != width`.
- Enhancement 6: Could skipping `AdjustSliceSplitPosition` during construction leave splitters mispositioned? No — `InstallSlice` calls `AdjustSliceSplitPosition` individually during construction, and `HandleLayout1(fFull=true)` handles width after construction.
- Enhancement 7: Could a visible slice have stale width after a resize? No — resize triggers `OnSizeChanged` → (after construction) loops all slices, and then `OnLayout` → `HandleLayout1(fFull=true)` synchronizes all widths.

### Actions taken
- Verified all override chains (`ViewSlice`, `InterlinearSlice`) are compatible with the base-class early-exit guard.
- Fixed pre-existing `RemoveDuplicateCustomFields` test failure (caused by prior Test.fwlayout changes, not by these optimizations).

### Verification
- `./build.ps1 -BuildTests` succeeded.
- `./test.ps1 -TestFilter "DataTreeTiming" -NoBuild` passed (5 tests).
- `./test.ps1 -TestFilter "RemoveDuplicateCustomFields" -NoBuild` passed.

---

## 2026-02-21 — Paint + visibility hot-path optimization pass

### Enhancement 8: XML attribute caching in HandlePaintLinesBetweenSlices

#### 1) What changed
- File: `Src/Common/Controls/DetailControls/Slice.cs`
- Added three cached `bool?` fields (`m_cachedIsHeader`, `m_cachedSkipSpacerLine`, `m_cachedSameObject`) and corresponding properties (`IsHeader`, `SkipSpacerLine`, `SameObject`).
- Cached values are lazy-initialized from `ConfigurationNode` on first access and invalidated when `ConfigurationNode` is re-set.
- File: `Src/Common/Controls/DetailControls/DataTree.cs`
- `HandlePaintLinesBetweenSlices` now uses `slice.IsHeader`, `slice.SkipSpacerLine`, `nextSlice.SameObject` instead of calling `XmlUtils.GetOptionalBooleanAttributeValue` per paint event.

#### 2) How it improved timing
- Eliminates per-paint XML attribute parsing. Previously, for each visible slice pair, 3 `XmlUtils.GetOptionalBooleanAttributeValue` calls performed XML dictionary lookups + `ToLowerInvariant()` string allocations every paint event.
- At 60Hz paint rate with ~15 visible slices: reduces from ~2,700 XML lookups/sec to ~45 total (first access only, then cached).
- `IsHeaderNode` property (used elsewhere) also benefits from the same cache.

#### 3) Measured timing savings / inner metrics
- Baseline file: `Output/RenderBenchmarks/datatree-timings-baseline-before-xml-cache-opt.json`
- Per-paint cost reduction: 3 x N_visible XML parses to 0 (after first access). For 15 visible slices, eliminates ~45 string allocations per paint.

#### 4) Why this is safe architecturally
- Pure memoization: the `ConfigurationNode` XML is set once at slice creation and never mutated during the slice lifecycle.
- Cache invalidation in the `ConfigurationNode` setter handles the rare case of re-assignment.
- 6 existing snapshot tests validate paint output unchanged.

---

### Enhancement 9: MakeSliceVisible high-water mark + index pass-through

#### 1) What changed
- File: `Src/Common/Controls/DetailControls/DataTree.cs`
- `MakeSliceVisible` changed from `static` to instance method with an optional `knownIndex` parameter.
- Added `m_lastVisibleHighWaterMark` field (-1 initially, reset in `CreateSlices`).
- The inner "make all preceding slices visible" loop now starts from `max(0, m_lastVisibleHighWaterMark + 1)` instead of 0.
- `HandleLayout1` passes the loop index `i` to `MakeSliceVisible(tci, i)`, avoiding O(N) `IndexOf` lookup.
- `GotoNextSliceAfterIndex` and `GotoPreviousSliceBeforeIndex` also pass the known `index`.
- File: `Src/Common/Controls/DetailControls/Slice.cs`
- All `DataTree.MakeSliceVisible(this)` static calls changed to `ContainingDataTree.MakeSliceVisible(this)` instance calls.

#### 2) How it improved timing
- **IndexOf elimination:** `tci.IndexInContainer` (which calls `Slices.IndexOf(this)`) was O(N) per call. In `HandleLayout1` with ~15 visible slices, this was 15 x O(253) = ~3,795 comparisons per paint. Now 0 comparisons.
- **High-water mark:** The inner loop previously walked slices 0..index-1 for every newly-visible slice. With sequential calls, the mark means each slice only walks its delta from the previous call. Total work: O(N) amortized instead of O(N x V).
- For 253 slices with 15 visible: inner loop reduced from ~3,795 iterations to ~253 total (amortized).

#### 3) Measured timing savings / inner metrics
- The `MakeSliceVisible` cost is primarily during initial layout (first `HandleLayout1(fFull=true)` pass). Steady-state paint calls hit the `!tci.Visible` fast path.
- During initial population of 253 slices: IndexOf cost alone was ~32,131 comparisons (sum of 0..252). Now 0.

#### 4) Why this is safe architecturally
- The .NET Framework bug workaround (LT-7307) is preserved: all preceding slices are still made visible before the target slice.
- The high-water mark is conservative: it only skips slices already confirmed visible by prior calls.
- `CreateSlices` resets the mark to -1, ensuring clean state on entry/object changes.
- The `Debug.Assert(tci.IndexInContainer == index)` validates correctness at runtime in Debug builds.

---

## Devil's-advocate stage summary

### Risks considered
- Enhancement 8: Could ConfigurationNode change without setter? No — only the property setter mutates it, and it invalidates the cache.
- Enhancement 9: Could the high-water mark become stale? `CreateSlices` resets it. Single-slice removal doesn't reset it, but removed slices were already visible.
- Enhancement 9: Backward navigation — slices below the mark were already made visible by prior forward traversals.

### Actions taken
- Changed `MakeSliceVisible` from static to instance and verified all 7 call sites updated.
- Added cache invalidation in `ConfigurationNode` setter.
- Reset high-water mark in `CreateSlices`.

### Verification
- `./build.ps1 -BuildTests` succeeded.
- `./test.ps1 -TestFilter "DataTreeTiming" -NoBuild` passed (5 tests).

---

### Enhancement 10: Skip ResetTabIndices in RemoveSlice during construction

#### 1) What changed
- File: `Src/Common/Controls/DetailControls/DataTree.cs`
- `RemoveSlice(Slice, int, bool)` now skips `ResetTabIndices(index)` when `ConstructingSlices == true`.
- Mirror of Enhancement 1: during `CreateSlices`, old dummy slices and non-reused slices are removed, each triggering `ResetTabIndices(index)`. The `finally` block already calls `ResetTabIndices(0)` after all removals, so per-removal resets are redundant.

#### 2) How it improved timing
- For M removals from a list of N slices, per-removal `ResetTabIndices(index)` does O(N-index) work each time → O(M×N) total. With the guard, zero tab-index calls during construction, single O(N) pass afterward.
- For 253-slice re-show with ~50-100 removals: eliminates thousands of `SetTabIndex` calls.

#### 3) Measured timing savings / inner metrics
- Same analysis as Enhancement 1 — the insert side was already optimized, now the removal side matches.

#### 4) Why this is safe architecturally
- Exact mirror of Enhancement 1's proven `ConstructingSlices` guard pattern.
- The `finally` block in `CreateSlices` guarantees a full `ResetTabIndices(0)` after all removals/insertions complete.
- `RemoveSlice` calls outside `CreateSlices` (when `ConstructingSlices == false`) still execute `ResetTabIndices` normally.

---

### Enhancement 11: Skip AdjustSliceSplitPosition in InstallSlice during construction

#### 1) What changed
- File: `Src/Common/Controls/DetailControls/DataTree.cs`
- `InstallSlice(Slice, int)` now skips `AdjustSliceSplitPosition(slice)` when `ConstructingSlices == true`.
- During construction, each inserted slice had its splitter position adjusted immediately — redundant because `HandleLayout1(fFull=true)` runs after construction and sets correct widths + splitter positions for all slices.

#### 2) How it improved timing
- Eliminates N `AdjustSliceSplitPosition` calls during construction, each involving `SetSplitPosition` + `SplitterMoved` event handler detach/reattach.
- For 253 slices: saves 253 splitter adjustment calls + 253 event handler manipulations.

#### 3) Measured timing savings / inner metrics
- Each `AdjustSliceSplitPosition` call does: event handler detach, `SetSplitPosition`, event handler reattach. Combined with Enhancement 6 (OnSizeChanged guard during construction), this eliminates all per-slice splitter work during the bulk construction path.

#### 4) Why this is safe architecturally
- Consistent with Enhancement 6: `OnSizeChanged` already skips splitter adjustment during construction.
- `HandleLayout1`'s `SetWidthForDataTreeLayout` call on each slice post-construction ensures correct splitter positioning.
- Non-construction `InstallSlice` calls (e.g., BecomeReal) still adjust splitter position immediately.

---

## Optimization regression test coverage (Enhancement 5-11)

### Tests added (DataTreeOpt_ prefix)
7 new tests added in `DataTreeRenderTests.cs` to cover optimization correctness:

1. **WidthStabilityAfterLayout** — Verifies slice widths don't change across multiple layout passes (Enhancement 5 early-exit).
2. **AllViewportSlicesVisible** — Verifies all slices in the viewport have `Visible=true` after layout (Enhancement 9 high-water mark).
3. **XmlCacheConsistency** — Verifies cached `IsHeader`/`SkipSpacerLine`/`SameObject` match direct XML parsing (Enhancement 8).
4. **XmlCacheInvalidationOnConfigChange** — Verifies cache is invalidated when `ConfigurationNode` is re-set (Enhancement 8).
5. **SequentialPaintsProduceIdenticalOutput** — Verifies consecutive paints produce pixel-identical bitmaps (Enhancements 3,4,7,8).
6. **SlicePositionsMonotonicallyIncreasing** — Verifies slice positions are monotonically ordered, prerequisite for clip-rect culling (Enhancement 3).
7. **IsHeaderNodeDelegatesToCachedProperty** — Verifies `IsHeaderNode` delegates to the cached `IsHeader` property (Enhancement 8).

### Devil's-advocate review (Enhancements 10-11)
- Enhancement 10: `ConstructingSlices` guard in `RemoveSlice` mirrors Enhancement 1's proven pattern. `CreateSlices` `finally` block guarantees post-construction normalization.
- Enhancement 11: `ConstructingSlices` guard in `InstallSlice` mirrors Enhancement 6. Post-construction `HandleLayout1` ensures correct splitter positions.

### Verification
- `./build.ps1 -BuildTests` succeeded (0 errors).
- `./test.ps1 -TestFilter "DataTreeTiming|DataTreeOpt" -NoBuild` passed (12 tests: 5 timing + 7 optimization).

---

## Remaining optimization targets (ranked)

| Rank | Target | Risk | Impact | Status |
|------|--------|------|--------|--------|
| 1 | Binary search in HandleLayout1 paint path | MODERATE | HIGH | Not started — original author left TODO comment |
| 2 | IndexOfSliceAtY binary search | LOW | LOW-MOD | Not started — mouse interaction, not hot path |
| 3 | Eliminate Slices.IndexOf in batch RemoveSlice | LOW | LOW-MOD | Not started |
| 4 | Guard ShowSubControls with visibility check | LOW | LOW | Assessed — too little benefit for the change |
| 5 | Avoid redundant Invalidate in OnLayout convergence | HIGH | LOW-MOD | Deferred — historically fragile |
| 6 | HWND reduction per slice | VERY HIGH | HIGHEST | Architectural — multi-month project |