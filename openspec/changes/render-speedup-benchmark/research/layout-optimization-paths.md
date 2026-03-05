# Layout & Reconstruct Optimization Paths

**Date**: March 2026
**Context**: Analysis of render benchmark timing data showing Reconstruct (44.5%) and PerformOffscreenLayout (45.1%) dominate warm render time.

## The Core Problem: Redundant Layout Passes

### Evidence

The warm render pipeline currently executes:

```
Reconstruct()
  └─ Construct()          → rebuild box tree (COM→managed→COM)
  └─ Layout()             → full layout pass #1 (line-breaking, positioning)
PerformOffscreenLayout()
  └─ Layout()             → full layout pass #2 (identical work, same width)
```

**`VwRootBox::Reconstruct()`** (VwRootBox.cpp:2521) calls `Layout()` internally after `Construct()`.
Then the caller (benchmark harness, or `OnPaint`/`OnLayout` in the real app) calls `Layout()` again.

This means every warm render does the full layout tree traversal **twice**.

### Measured Impact

From the latest benchmark run (15 scenarios, Debug, SIL-XPS):

| Stage | Calls | Total (ms) | Avg (ms) | Share % |
|-------|------:|-----------:|---------:|--------:|
| PerformOffscreenLayout | 30 | 1545.66 | 51.52 | 45.1% |
| Reconstruct | 15 | 1527.54 | 101.84 | 44.5% |
| DrawTheRoot | 15 | 195.60 | 13.04 | 5.7% |
| CreateView | 15 | 81.24 | 5.42 | 2.4% |
| MakeRoot | 15 | 74.64 | 4.98 | 2.2% |

Worst-case scenario: `long-prose` — 4 sections × 80 verses — warm render = **581.76ms**.

## Optimization Paths

### PATH-L1: Width-Invariant Layout Guard (VwRootBox)

**Change**: Add a dirty flag + last-width cache to `VwRootBox::Layout()`. If the box tree hasn't changed and the available width is the same, skip the entire layout traversal.

**Files**:
- `Src/views/VwRootBox.h` — add `m_fNeedsLayout`, `m_dxLastLayoutWidth` fields
- `Src/views/VwRootBox.cpp` — gate `Layout()`, set dirty in `Construct()`, `PropChanged()`, `OnStylesheetChange()`

**Expected gain**: Eliminates Layout #2 entirely. ~45% of warm render time.

**Risk**: Low. Purely additive guard. If the flag is wrong, Layout still runs — no correctness risk, only a missed optimization.

### PATH-L2: Deferred Layout in Reconstruct

**Change**: `Reconstruct()` currently calls `Layout()` immediately after `Construct()`. Since the caller almost always calls `Layout()` again (via OnPaint, OnLayout, or explicit call), the internal Layout is redundant. Instead, set the dirty flag and let the external Layout call handle it.

**Files**:
- `Src/views/VwRootBox.cpp` — remove `Layout()` call from `Reconstruct()`, set `m_fNeedsLayout = true`

**Expected gain**: Combined with PATH-L1, ensures exactly one Layout per Reconstruct cycle. ~20-30% reduction in Reconstruct time.

**Risk**: Medium. Some callers may depend on `Reconstruct()` leaving the box tree fully laid out. Requires `Reconstruct()` callers to trigger layout before reading dimensions. Mitigated by PATH-L1 ensuring the next `Layout()` call does the work. Also, `Reconstruct()` reads `FieldHeight()` after layout to detect size changes — this needs to remain.

**Decision**: After analysis, `Reconstruct()` itself needs dimensions immediately (for `RootBoxSizeChanged` callback). So we keep the internal Layout call but rely on PATH-L1 to make the second external call a no-op.

### PATH-L3: Per-Paragraph Layout Caching

**Change**: `VwParagraphBox::DoLayout()` runs the full `RunParaBuilder → MainLoop` line-breaking algorithm on every call, even when the paragraph's text source and available width haven't changed. Add a dirty flag at the paragraph level.

**Files**:
- `Src/views/VwSimpleBoxes.h` / `VwTextBoxes.h` — add `m_fLayoutDirty`, `m_dxLastLayoutWidth` to `VwParagraphBox`
- `Src/views/VwTextBoxes.cpp` — gate `DoLayout()`, mark dirty on text source change

**Expected gain**: For `long-prose` (320 paragraphs), if Reconstruct rebuilds all boxes but Layout finds them identical to before, 319 out of 320 paragraph layouts are skipped. Expected 50-80% reduction in Layout time for unchanged content.

**Risk**: Medium. Must ensure dirty flag is set correctly on all text mutation paths: `PropChanged`, direct text source modification, style changes, font changes. Over-dirtying is safe (falls back to full layout); under-dirtying would produce stale layout.

**Decision**: Since Reconstruct destroys ALL boxes and rebuilds them via Construct, the paragraphs are brand new objects — they don't carry state from the previous tree. This means paragraph-level caching only helps when Layout is called multiple times on the SAME box tree (which is exactly what PATH-L1 addresses). PATH-L3 is therefore not applicable to the Reconstruct→Layout flow, but IS valuable for the `LayoutFull()` path used by `OnStylesheetChange`, overlay changes, and resize. Defer to future iteration.

### PATH-L4: Harness Double-Layout Elimination

**Change**: The benchmark harness calls `Reconstruct()` (which includes Layout) and then `PerformOffscreenLayout()` (which calls Layout again). With PATH-L1, the second call becomes a no-op. Additionally, the harness allocates a throwaway `Bitmap(800,600)` each time just to get an HDC for the redundant Layout.

**Files**:
- `Src/Common/RootSite/RootSiteTests/RenderBenchmarkHarness.cs` — cache bitmap/graphics, or skip second layout when width unchanged

**Expected gain**: Eliminates 1.92MB bitmap allocation per warm render + COM object creation overhead. ~5-15ms per render.

**Risk**: Very low. Test-only code. No production impact.

### PATH-L5: Skip Reconstruct When Data Unchanged

**Change**: In `SimpleRootSite.RefreshDisplay()`, check whether actual data has changed before calling `Reconstruct()`. Many `RefreshDisplay()` calls are triggered by focus changes, panel activations, or other non-data events.

**Files**:
- `Src/Common/SimpleRootSite/SimpleRootSite.cs` — add data version tracking

**Expected gain**: In real app scenarios, eliminates entire Reconstruct+Layout cycle when data hasn't changed. 100% of render time saved for no-op refreshes.

**Risk**: Medium. Must correctly identify all paths that should trigger Reconstruct (data change, style change, overlay change, writing system change). Under-triggering would show stale content. Defer to future iteration with more testing breadth.

## Implementation Priority

| Priority | Path | Expected Gain | Risk | Effort |
|----------|------|---------------|------|--------|
| 1 | PATH-L1 | 45% of warm render | Low | Small (C++ header + 2 methods) |
| 2 | PATH-L4 | 5-15ms/render | Very Low | Small (test code only) |
| 3 | PATH-L5 | 100% for no-op refreshes | Medium | Medium (needs data version API) |
| 4 | PATH-L3 | 50-80% of Layout for unchanged paragraphs | Medium | Medium (C++ paragraph class) |
| 5 | PATH-L2 | 20-30% of Reconstruct | Medium | Small but risky (callers depend on post-Reconstruct dimensions) |

## Acceptance Criteria

- All existing render benchmark tests pass (15 scenarios).
- All existing RenderBaseline and HwndVirtualization tests pass.
- Warm render average decreases by ≥40% from current baseline (153ms → ≤92ms).
- No visual regression in pixel-perfect snapshots.
- Layout guard correctly triggers re-layout when width changes, data changes, or style changes.
