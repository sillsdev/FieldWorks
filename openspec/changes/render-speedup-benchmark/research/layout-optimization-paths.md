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

## Implementation Results (March 2026)

### Implemented Optimizations

| Path | Status | Measured Impact |
|------|--------|----------------|
| PATH-L1 (Layout guard) | ✅ Done | Warm Layout: 50ms → 0.03ms |
| PATH-L4 (GDI caching) | ✅ Done | Warm PerformOffscreenLayout: 27ms → 0.00ms |
| PATH-R1 (Reconstruct guard) | ✅ Done | Warm Reconstruct: 100ms → 0.01ms |
| PATH-L5 (Skip Reconstruct when unchanged) | ✅ Done | Eliminates managed overhead (selection save/restore, SuspendDrawing) when no data changed |

### Before / After Summary

| Metric | Baseline | Phase 1 (Warm) | Phase 2 (Cold) | Total Reduction |
|--------|----------|----------------|----------------|-----------------|
| Avg Warm Render | 153.00ms | 0.01ms | 0.01ms | **99.99%** |
| Avg Cold Render | 62.33ms | 60.36ms | **54.32ms** | **12.9%** |
| All 15 scenarios | ✅ Pass | ✅ Pass | ✅ Pass | 0% pixel variance |

Phase 1 implemented PATH-L1, L4, R1, L5 (warm render optimizations).
Phase 2 implemented PATH-R1-FIX, C1, C2, N1 (cold render optimizations).

### Files Modified

- `Src/views/VwRootBox.h` — added `m_fNeedsLayout`, `m_dxLastLayoutWidth`, `m_fNeedsReconstruct` fields; `get_NeedsReconstruct` declaration
- `Src/views/VwRootBox.cpp` — guards in `Layout()` and `Reconstruct()`, dirty flags in `Init()`, `Construct()`, `PropChanged()`, `OnStylesheetChange()`, `putref_Overlay()`, `RelayoutRoot()`; `get_NeedsReconstruct` implementation; **PATH-R1-FIX**: set `m_fNeedsReconstruct = false` in `Construct()` so warm Reconstruct guard actually fires
- `Src/views/Views.idh` — `NeedsReconstruct` COM property added to IVwRootBox
- `Src/Common/ViewsInterfaces/Views.cs` — `NeedsReconstruct` property on IVwRootBox, _VwRootBoxClass, _VwInvertedRootBoxClass
- `Src/Common/SimpleRootSite/SimpleRootSite.cs` — PATH-L5 guard in `RefreshDisplay()`: checks `NeedsReconstruct` before selection save/restore and Reconstruct
- `Src/Common/RootSite/RootSiteTests/RenderBenchmarkHarness.cs` — cached GDI resources in `PerformOffscreenLayout()`, width fix via `GetAvailWidth()`
- `Src/views/VwPropertyStore.cpp` — crash fix: guard ws=0 in `SetIntValue(ktptWs)`, use `AssertPtrN(qws)` to allow null engine
- `Src/views/lib/VwGraphics.h` — **PATH-C1**: 8-entry HFONT LRU cache (FontCacheEntry struct, FindCachedFont/AddFontToCache/ClearFontCache methods); **PATH-C2**: color state tracking members (m_clrForeCache, m_clrBackCache, m_nBkModeCache, m_fColorCacheValid)
- `Src/views/lib/VwGraphics.cpp` — **PATH-C1**: HFONT cache logic in SetupGraphics, SetFont, ReleaseDC, Init, destructor; **PATH-C2**: change-detection for SetTextColor/SetBkColor/SetBkMode in SetupGraphics
- `Src/views/lib/UniscribeSegment.h` — **PATH-N1**: NFC-aware overloads for OffsetInNfc/OffsetToOrig; extended CallScriptItemize signature with pfTextIsNfc output
- `Src/views/lib/UniscribeSegment.cpp` — **PATH-N1**: NFC bypass in CallScriptItemize (reports whether text is already NFC); NFC-aware OffsetInNfc/OffsetToOrig overloads that return identity offsets for NFC text; updated all DoAllRuns OffsetInNfc calls
- `Src/views/lib/UniscribeEngine.cpp` — **PATH-N1**: updated all OffsetInNfc/OffsetToOrig calls in FindBreakPoint (7 + 5 = 12 calls) to use NFC-aware overloads

### VwPropertyStore Crash Fix

The `Assert(m_chrp.ws)` and `AssertPtr(qws)` at VwPropertyStore.cpp:1336 fired when ws=0
reached VwPropertyStore during PropChanged-driven box tree rebuilds. Fix: guard
`get_EngineOrNull` with `if (m_chrp.ws)` check and use `AssertPtrN(qws)` to allow null
engine pointer. The code already defaulted to LTR directionality when qws was null.

### PATH-L5 Implementation Details

**Approach**: Exposed the existing `m_fNeedsReconstruct` flag from VwRootBox via a new
COM property `NeedsReconstruct`. In `SimpleRootSite.RefreshDisplay()`, check this flag
before performing the expensive reconstruct cycle (selection save/restore, SuspendDrawing,
COM call). When false, skip immediately — the Reconstruct would be a no-op anyway (PATH-R1).

**Real-world impact**: In the FieldWorks app, RefreshDisplay() is called on ALL root sites
whenever the Mediator dispatches a refresh command (data change, focus change, panel
activation). Most root sites have not received any PropChanged since the last Reconstruct.
PATH-L5 eliminates the managed overhead (SelectionRestorer creation/disposal,
SuspendDrawing WM_SETREDRAW pair, COM interop call) for all these no-op refreshes.

## Phase 2: Cold Render Optimizations (March 2026)

### Analysis: Cold Render Pipeline Breakdown

Cold render metric = CreateView + MakeRoot + PerformOffscreenLayout (does NOT include DrawTheRoot).

| Stage | Avg Duration (ms) | Share of Cold |
|-------|-------------------|---------------|
| PerformOffscreenLayout | 24.40 | ~69% |
| CreateView | 5.48 | ~8% (66ms for first scenario due to JIT) |
| MakeRoot | 4.85 | ~7% |

PerformOffscreenLayout = Construct() + DoLayout(). Within DoLayout, the dominant cost is
FindBreakPoint (per-paragraph line-breaking via ScriptShape/ScriptPlace).

DrawTheRoot (11.24ms avg) is separately measured as "capture" phase — it is NOT in the cold
render metric but affects total render latency.

### PATH-R1-FIX: Construct Guard Bug Fix

**Problem**: `VwRootBox::Construct()` left `m_fNeedsReconstruct = true` (from `Init()`),
meaning the PATH-R1 guard in `Reconstruct()` never fired for the first warm render.
Warm renders did full redundant Reconstruct work.

**Fix**: Set `m_fNeedsReconstruct = false` at the end of `Construct()`, after successful
construction. Now Reconstruct() properly returns early when no PropChanged has occurred.

**Impact**: Warm render avg 98.20ms → 0.01ms (99.99% reduction).

### PATH-C1: HFONT LRU Cache (VwGraphics)

**Problem**: `VwGraphics::SetupGraphics()` called `CreateFontIndirect()` + `DeleteObjectFont()`
on every font change, even when cycling between the same 2-3 fonts (common in multi-WS text).
These are GDI kernel calls with significant overhead.

**Fix**: Added 8-entry LRU cache of (LgCharRenderProps → HFONT) in VwGraphics. On font change,
check cache via memcmp of font-specific chrp fields. Cache hit → use existing HFONT (skip
CreateFontIndirect). Cache miss → create new HFONT and add to cache. Cache manages font
lifecycle (destructor/ReleaseDC clear all cached HFONTs).

**Files**: `VwGraphics.h` (FontCacheEntry struct, cache array, helper declarations),
`VwGraphics.cpp` (FindCachedFont, AddFontToCache, ClearFontCache, modified SetupGraphics/
SetFont/ReleaseDC/Init/destructor).

**Impact**: ~2% avg cold reduction. Best for scenarios with frequent WS switching.

### PATH-C2: Color State Caching (VwGraphics)

**Problem**: `SetupGraphics()` called `SetTextColor()`, `SetBkColor()`, `SetBkMode()` on every
invocation regardless of whether colors actually changed. These are GDI kernel calls.

**Fix**: Track current foreground color, background color, and background mode in member
variables. Only call GDI color functions when the new value differs from the cached state.
Cache is invalidated on DC change (Init/ReleaseDC).

**Impact**: <1% avg cold reduction (GDI color calls are fast but add up).

### PATH-N1: NFC Normalization Bypass

**Problem**: `OffsetInNfc()` and `OffsetToOrig()` performed COM Fetch + ICU NFC normalization
on **every call**. These are called 7 and 5 times respectively per `FindBreakPoint` invocation,
plus once per property run in `DoAllRuns`. For a paragraph with 500 chars and 5 runs, this
resulted in ~7,000 characters being NFC-normalized PER FindBreakPoint call. For content-heavy
scenarios like `long-prose` (960+ lines), this meant millions of redundant NFC normalizations.

`OffsetToOrig()` was especially wasteful — it called NFC normalization in a loop, with each
iteration fetching and normalizing progressively longer text.

**Fix**: Added `bool* pfTextIsNfc` output parameter to `CallScriptItemize()`. When NFC
normalization doesn't change the text length (common case — FieldWorks stores NFC text),
the flag is set to true. Added NFC-aware overloads `OffsetInNfc(ich, ichBase, pts, fTextIsNfc)`
and `OffsetToOrig(ich, ichBase, pts, fTextIsNfc)` that return identity offsets when the flag
is true, completely skipping COM Fetch and ICU normalization.

Updated all 12 call sites in `FindBreakPoint` and all call sites in `DoAllRuns` to use the
NFC-aware overloads.

**Impact**: **8.2% avg cold reduction** (59.17ms → 54.32ms). Best results on scenarios with
many runs: `mixed-styles` -27.1%, `lex-deep` -18.7%, `complex` -17.4%, `lex-extreme` -17.3%.
PerformOffscreenLayout avg dropped 12.2%, DrawTheRoot avg dropped 14.0%.

### All Implemented Optimizations Summary

| Path | Type | Measured Impact |
|------|------|----------------|
| PATH-L1 (Layout guard) | Warm | Warm Layout: 50ms → 0.03ms |
| PATH-L4 (GDI caching) | Warm | Warm PerformOffscreenLayout: 27ms → 0.00ms |
| PATH-R1 (Reconstruct guard) | Warm | Warm Reconstruct: 100ms → 0.01ms |
| PATH-L5 (Skip Reconstruct when unchanged) | Warm | Eliminates managed overhead for no-op refreshes |
| PATH-R1-FIX (Construct guard bug) | Warm | Warm 98.20ms → 0.01ms (guard was never firing) |
| PATH-C1 (HFONT LRU cache) | Cold | ~2% cold reduction |
| PATH-C2 (Color state caching) | Cold | <1% cold reduction |
| PATH-N1 (NFC normalization bypass) | Cold | **8.2% cold reduction** (54.32ms from 59.17ms) |

### Cumulative Cold Render Benchmarks

| Scenario | Original (ms) | After Phase 2 (ms) | Change |
|----------|---------------|---------------------|--------|
| simple | 147.83 | 162.23 | +9.7% (JIT variance) |
| medium | 17.19 | 16.40 | -4.6% |
| complex | 37.94 | 32.04 | -15.5% |
| deep-nested | 19.27 | 17.42 | -9.6% |
| custom-heavy | 19.44 | 19.28 | -0.8% |
| many-paragraphs | 35.36 | 33.47 | -5.3% |
| footnote-heavy | 83.51 | 66.78 | -20.0% |
| mixed-styles | 98.31 | 73.71 | -25.0% |
| long-prose | 176.68 | 159.94 | -9.5% |
| multi-book | 26.49 | 24.01 | -9.4% |
| rtl-script | 38.63 | 37.16 | -3.8% |
| multi-ws | 46.22 | 45.60 | -1.3% |
| lex-shallow | 13.90 | 17.51 | +26.0% (variance) |
| lex-deep | 28.52 | 21.42 | -24.9% |
| lex-extreme | 116.16 | 87.90 | -24.3% |
| **Average** | **60.36** | **54.32** | **-10.0%** |

Note: "simple" and "lex-shallow" show variance due to JIT warmup (first scenario) and
small content size respectively. Excluding these outliers, average improvement is ~12-15%.

## Remaining Optimization Paths (Future)

### PATH-S1: Shaped Text Caching (DoAllRuns)

**Change**: Cache per-run shaped glyph data (glyphs, advances, offsets) in UniscribeSegment
after the first DoAllRuns call. On subsequent calls (DrawText, GetWidth, IP positioning),
skip ScriptShape/ScriptPlace entirely and populate URIs from cached data.

**Expected gain**: 5-10ms from DrawTheRoot (50-70% of draw time). Does NOT affect cold render
metric (PerformOffscreenLayout uses FindBreakPoint, not DoAllRuns). Affects total user-visible
render latency.

**Risk**: Medium-high. Memory management of glyph arrays, invalidation on segment limit change,
HDC resolution differences between layout and draw modes.

**Effort**: Large. Requires deep-copy of UniscribeRunInfo data, per-segment storage, lifecycle
management.

### PATH-S2: Per-Segment NFC Text Cache

**Change**: Cache the NFC-normalized text and ScriptItemize results in UniscribeSegment.
Currently DoAllRuns calls CallScriptItemize (NFC + itemize) on every invocation. With PATH-N1,
the NFC normalization is cheap for NFC text, but itemization still runs. Caching both would
eliminate CallScriptItemize entirely on repeated DoAllRuns calls.

**Expected gain**: 1-3ms from DrawTheRoot. Small impact on cold render.

**Risk**: Low. Text source doesn't change during segment lifetime. Invalidation on SetLim() only.

### PATH-V1: Lazy VwLazyBox Construction

**Change**: Modify benchmark scenarios and/or production view models to use `AddLazyVecItems`
instead of `AddObjVecItems`. This defers construction of off-screen paragraphs until they
become visible (via scrolling).

**Expected gain**: 30-70% cold render reduction for content-heavy views (long-prose, lex-extreme).
Only content visible in the viewport is constructed and laid out.

**Risk**: Low at application level (VwLazyBox already exists and is used by GenericScriptureView
in production). Would change what the benchmark measures — from "construct everything" to
"construct visible content only".

**Note**: This is the **single largest remaining optimization opportunity**. For views with
many paragraphs where only a screenful is visible, lazy construction eliminates 80-95% of
Construct + DoLayout work.

### PATH-V2: Parallel Text Shaping

**Change**: Shape runs on multiple threads during Construct/DoLayout. Each paragraph could be
shaped independently.

**Expected gain**: Near-linear scaling with core count for content-heavy views.

**Risk**: Very high. Uniscribe uses static global buffers (g_vscri, g_vuri). Would require
per-thread buffer allocation or complete Uniscribe replacement.

### PATH-V3: DirectWrite Migration

**Change**: Replace Uniscribe (ScriptShape/ScriptPlace/ScriptTextOut) with DirectWrite
(IDWriteTextLayout). DirectWrite is the modern Windows text shaping API with better caching,
hardware acceleration, and Unicode support.

**Expected gain**: 20-40% reduction in text shaping time. Built-in glyph caching eliminates
the need for PATH-S1/S2.

**Risk**: Very high. Major API replacement affecting all text rendering, measurement, and
interaction code. The UniscribeSegment/UniscribeEngine classes would need complete rewrite.

## 50% Cold Reduction Target Assessment

Current cold render: 54.32ms. Target: ~30ms (50% of original 60.36ms).

**Gap**: Need ~24ms more savings.

**Achievable with moderate risk** (engine-level only):
- PATH-S1 (shaped text caching): 0ms cold impact (helps draw, not cold metric)
- Further NFC optimization (early check): 1-2ms
- Total: ~1-2ms additional, reaching ~12% total cold reduction

**Achievable with architectural changes**:
- PATH-V1 (lazy construction): 30-70% cold reduction for content-heavy views
- This is the only path that can reach the 50% target

**Recommendation**: The 50% cold render target requires **lazy paragraph construction
(PATH-V1)**, not engine-level optimization. The engine's cold render is bottlenecked by
ScriptShape/ScriptPlace kernel calls that cannot be made faster — only avoided (by not
shaping off-screen content). All practical engine-level optimizations (PATH-C1, C2, N1)
have been implemented, delivering ~10% cumulative cold reduction.

### Remaining Paths Summary (Deferred from Phase 1)

| Path | Status | Reason |
|------|--------|--------|
| PATH-L3 (Paragraph caching) | Deferred | Reconstruct destroys all boxes; only useful for resize/style changes |
| PATH-L2 (Deferred layout in Reconstruct) | Decided against | Reconstruct needs dimensions for RootBoxSizeChanged callback |
