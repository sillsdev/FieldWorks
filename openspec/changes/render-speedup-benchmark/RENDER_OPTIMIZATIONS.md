# Render Optimizations (Consolidated)

This file is the single source of truth for render/layout optimization work done on branch `001-render-speedup`.

## Scope and Evidence

- Branch commit history since `main`:
  - `d6741a0bd` perf: cold render optimizations (HFONT cache, color caching, NFC bypass, reconstruct guard fix)
  - `b7f52166b` perf(render): benchmark and snapshot stabilization
  - `d0066f324` perf(render): DataTree layout speedups and benchmark coverage
  - `c4678e824` squashed foundational change set
- Latest benchmark outputs used here:
  - `Output/RenderBenchmarks/summary.md`
  - `Output/RenderBenchmarks/results.json`
  - `Output/RenderBenchmarks/datatree-timings.json`
  - `Output/Debug/TestResults/vstest.console.log`

## Main Paths and Bottlenecks (Lexical Edit View)

There are two related pipelines.

1. Views-engine benchmark pipeline (`RenderTimingSuite`)
- `CreateView` -> `MakeRoot` -> `PerformOffscreenLayout` -> capture (`PrepareToDraw` + `DrawTheRoot`)
- Current stage breakdown (`summary.md`, latest run):
  - `PerformOffscreenLayout`: 67.9% (dominant)
  - `DrawTheRoot`: 17.2%
  - `CreateView`: 7.2%
  - `MakeRoot`: 7.1%
  - `PrepareToDraw`/`Reconstruct`: negligible

2. Lexical DataTree edit-view pipeline (`DataTreeTiming`)
- `PopulateSlices` (WinForms + XML-driven slice construction)
- layout convergence/visibility operations
- composite capture (`DrawToBitmap` + Views overlay)
- Current timing examples (`datatree-timings.json`, latest run):
  - `timing-shallow`: total 985.3 ms
  - `timing-deep`: total 385.8 ms
  - `timing-extreme`: total 465.1 ms
  - `paint-extreme`: total 479.4 ms
  - detailed paint line in test log: capture about 4798 ms for full 1007x6072 surface

Cold-start interpretation:
- For Views pipeline, cold time is still mostly layout (`PerformOffscreenLayout`).
- For DataTree lexical edit view, total cold-like startup cost is often dominated by slice population + full-surface paint/capture work.

## What Was Done and Why It Helped

## A. Warm-path structural wins (major)

1. Layout/reconstruct guarding (`VwRootBox` + `SimpleRootSite`)
- Added and wired guard state (`m_fNeedsLayout`, `m_fNeedsReconstruct`, width checks, `NeedsReconstruct` COM surface).
- Effect: redundant warm reconstruct/layout cycles are bypassed when content/width is unchanged.
- Result trend: warm render collapsed from baseline triple-digit ms to near-zero in benchmark runs.

2. Harness-side redundant work removal (`RenderBenchmarkHarness`)
- Cached offscreen GDI resources and removed repeated setup overhead in warm path.
- Effect: reduced benchmark harness overhead and made warm numbers stable.

## B. Cold-path wins (targeted)

Implemented in `d6741a0bd`:

1. PATH-C1 HFONT cache in `VwGraphics`
- New 8-entry LRU-like cache keyed by font-relevant `LgCharRenderProps` region.
- Avoids repetitive `CreateFontIndirect`/delete churn in mixed writing-system text.
- Measured contribution: modest but real cold-start improvement.

2. PATH-C2 color state cache in `VwGraphics`
- Skips redundant `SetTextColor`/`SetBkColor`/`SetBkMode` calls.
- Measured contribution: small cold-start improvement.

3. PATH-N1 NFC bypass in Uniscribe path
- Added NFC-awareness flag flow and fast identity-offset path when text is already NFC.
- Reduced repeated normalization/fetch overhead in line-break and run handling.
- Measured contribution: largest cold improvement among the late native optimizations.

## C. DataTree/WinForms hot-path wins

Implemented mainly in `d0066f324` and `b7f52166b`:

- Construction batching and tab-index optimization (`ConstructingSlices` guards)
- Layout churn reduction (`SetWidthForDataTreeLayout` early-exit, size-change guards)
- Paint-path reductions (clip culling, cached XML attribute reads, high-water visibility tracking)
- Binary search for first visible slice in paint path
- Added optimization regression tests (`DataTreeOpt_*`) and expanded timing coverage

These changes reduced repeated O(N) and O(N^2)-like behavior in common DataTree layout/paint loops.

## What Was Considered and Discarded (or Deferred)

1. Deferred layout inside `Reconstruct` (PATH-L2)
- Considered removing internal layout call.
- Rejected because `Reconstruct` callers rely on immediate post-reconstruct dimensions.

2. Paragraph-level cache for reconstruct flow (PATH-L3)
- Considered caching `VwParagraphBox` layout across reconstruct.
- Deferred for this path because reconstruct rebuilds boxes, so previous paragraph objects are not reused.

3. `ShowSubControls` visibility guard (DataTree)
- Assessed as too little benefit for added complexity in current benchmarks.

4. Partial-paint-only gains that do not move full-capture benchmarks
- Clip culling helps real partial paints, but full-surface benchmark captures show little/no gain from that specific mechanism.

5. Some data-tree optimizations had architecture benefit but limited measurable impact in current scenarios
- Example: lazy expansion suspension path is valid but timing scenarios do not always trigger it.

## What Could Still Be Done (Cold Start Only)

The following estimates are for cold behavior, not warm steady-state. Risk and savings are relative and based on current stage shares (`summary.md`) and DataTree measurements.

1. Reduce `PerformOffscreenLayout` cost further (highest ROI)
- Ideas: cache itemization/shaping artifacts at segment level, reduce repeat line-break work, smarter reuse of per-run analysis.
- Risk: Medium-High (native text pipeline correctness is sensitive).
- Potential cold savings: 10-25% total cold (because this stage is about 68% of cold benchmark time).

2. Lower DataTree `PopulateSlices` startup overhead
- Ideas: defer creation for non-visible optional slices, precompiled layout metadata, stricter construction batching.
- Risk: Medium (high interaction surface with existing slice lifecycle).
- Potential cold savings: 15-35% on lexical edit startup scenarios, especially deep/extreme entries.

3. Reduce full-surface capture/paint work for very tall views
- Ideas: avoid work on non-visible regions for cold startup render path when possible; keep viewport-first rendering and defer below-fold composition.
- Risk: Medium.
- Potential cold savings: 10-20% in extreme lexical entries (bigger in test-capture workflows than normal UI first paint).

4. Startup/JIT warming for first scenario spikes
- Ideas: pre-touch key code paths and objects during app startup phase.
- Risk: Low-Medium (trade startup work for smoother first action).
- Potential cold savings: 5-15% on first-open latency only.

5. Medium-term architectural path: reduce WinForms control explosion in DataTree
- Ideas: stronger virtualization and view-model separation in edit view pipeline.
- Risk: High.
- Potential cold savings: 25-50%+ on heavy lexical entries, but not a short patch-cycle change.

## Current Status Snapshot

- Latest run (`2026-03-09`) in `summary.md`:
  - Avg cold render: 64.98 ms
  - Avg warm render: 0.01 ms
  - Top contributor remains `PerformOffscreenLayout`.
- DataTree timing remains the largest practical lexical edit-view cost center for deep/extreme structures.

## Notes

- This document intentionally consolidates and replaces prior speedup/timing markdown artifacts for this branch.
- Keep future updates here instead of creating new speedup summary files.