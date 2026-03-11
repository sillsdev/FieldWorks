## Speckit Migration Notes

- Source of truth migrated from `specs/001-render-speedup/` into this OpenSpec change.
- Existing completion states from Speckit are preserved below.
- New OpenSpec-only tasks are marked with `OS-` IDs.

## Phase 1: Setup (Shared Infrastructure)

- [x] T001 Create scenario definition file at `Src/Common/RootSite/RootSiteTests/TestData/RenderBenchmarkScenarios.json`.
- [x] T002 Update `.gitignore` to exclude `Output/RenderBenchmarks/**` artifacts.
- [x] T002a Add feature-flag config file at `Src/Common/RootSite/RootSiteTests/TestData/RenderBenchmarkFlags.json`.

## Phase 2: Foundational (Blocking)

- [x] T003 Create harness class in `Src/Common/RootSite/RootSiteTests/RenderBenchmarkHarness.cs`.
- [x] T004 Add bitmap diff utility in `Src/Common/RootSite/RootSiteTests/RenderBitmapComparer.cs`.
- [x] T005 Add benchmark models + JSON serialization in `Src/Common/RootSite/RootSiteTests/RenderBenchmarkResults.cs`.
- [x] T006 Add scenario data builder helpers in `Src/Common/RootSite/RootSiteTests/RenderScenarioDataBuilder.cs`.
- [x] T007 Add deterministic environment validator in `Src/Common/RootSite/RootSiteTests/RenderEnvironmentValidator.cs`.
- [x] T008 Add trace log parser in `Src/Common/RootSite/RootSiteTests/RenderTraceParser.cs`.
- [x] T008a Add diagnostics toggle helper in `Src/Common/RootSite/RootSiteTests/RenderDiagnosticsToggle.cs`.
- [x] T008b Add regression comparer in `Src/Common/RootSite/RootSiteTests/RenderBenchmarkComparer.cs`.

## Phase 3: User Story 1 - Pixel-Perfect Render Baseline (P1)

- [x] T009 Implement baseline test in `Src/Common/RootSite/RootSiteTests/RenderBaselineTests.cs`.
- [x] T009a Pivot: Adopt native capture via `VwDrawRootBuffered` in `RenderBenchmarkHarness.cs`.
- [ ] T009b Pivot: Replace `DummyBasicView` with production `StVc` in benchmark test base.
- [x] T010 Add baseline snapshot for simple scenario.
- [x] T011 Wire environment hash validation into harness.
- [x] T011a Document `DrawToBitmap` limitations and skip list.

## Phase 4: User Story 2 - Rendering Timing Suite (P2)

- [x] T012 Populate five timing scenarios (simple, medium, complex, deep-nested, custom-field-heavy).
- [x] T013 Implement timing suite in `RenderTimingSuiteTests.cs`.
- [x] T014 Add report writer in `RenderBenchmarkReportWriter.cs`.
- [x] T015 Add baseline snapshots for remaining scenarios.
- [x] T016 Emit results to `Output/RenderBenchmarks/results.json` and summary to `Output/RenderBenchmarks/summary.md`.
- [x] T016a Implement run comparison in report writer using `RenderBenchmarkComparer.cs`.
- [x] T016b Add reproducible test data guidance in migrated quickstart docs.

## Phase 5: User Story 3 - Rendering Trace Diagnostics (P3)

- [x] T017 Add trace timing helper in `Src/views/VwRenderTrace.h`.
- [ ] T018 Instrument `VwRootBox::Layout/PrepareToDraw/DrawRoot/PropChanged` in `Src/views/VwRootBox.cpp`.
- [ ] T019 Instrument render entry points in `Src/views/VwEnv.cpp`.
- [ ] T020 Instrument lazy expansion paths in `Src/views/VwLazyBox.cpp`.
- [x] T021 Add trace switch/config in `Src/Common/FieldWorks/FieldWorks.Diagnostics.dev.config`.
- [x] T022 Integrate trace parsing into `RenderBenchmarkReportWriter.cs`.
- [ ] T022a Add trace validation test in native views tests.

## Phase 6: Polish & Cross-Cutting

- [x] T023 Update quickstart with harness usage and output paths.
- [x] T024 Review/update `Src/views/AGENTS.md` for tracing changes.
- [x] T025 Review/update `Src/Common/RootSite/AGENTS.md` for harness/tests.
- [x] T026 Add explicit edge-case validations in timing suite.

## OpenSpec Continuation Tasks

- [x] OS-1 Switch DataTree timing benchmarks to stable test-layout mode for workload scaling.
- [x] OS-2 Add DataTree timing workload-growth guard test (monotonic complexity).
- [x] OS-3 Run targeted verification for `DataTreeTiming*` and confirm green. Fixed test layout (Normal needs real parts + subsense recursion) and FieldWorks.sln duplicate Kernel project.
- [x] OS-4 Add benchmark timing artifact schema notes (required keys, meaning, and comparability rules).
- [x] OS-5 Execute one measured hotspot optimization and capture before/after evidence. Applied ResetTabIndices O(N²)→O(N) fix and BecomeReal DeepSuspendLayout. See `Output/RenderBenchmarks/OPT1-evidence.md`.
## Phase 7: Optimization Research & Implementation

**Purpose**: Apply the 10 optimization techniques identified in FORMS_SPEEDUP_PLAN.md, prioritized by ROI. Each requires timing infrastructure to be in place (OS-3/OS-5 first).

**Priority order** (from FORMS_SPEEDUP_PLAN priority matrix):

- [ ] OPT-9 Custom Fields Collapse-by-Default — highest ROI, lowest risk. Defer rendering of custom field sections until user expands. Expected: ~98% reduction in custom field rendering cost.
- [x] OPT-1 SuspendLayout/ResumeLayout Batching — skip redundant per-slice `ResetTabIndices` during `ConstructingSlices`; wrap `BecomeReal()` with `DeepSuspendLayout`/`DeepResumeLayout`. Eliminates O(N²) tab index recalc and unsuspended lazy expansion.
- [ ] OPT-2 Double Buffering — enable `OptimizedDoubleBuffer` on key panels. Eliminates flicker; low risk.
- [ ] OPT-4 Lazy Control Creation — defer tab/section content creation until first access. Expected: 50%+ faster initial load.
- [ ] OPT-6 Background Data Loading — move LCModel data loading off UI thread via async/await. Non-blocking UI.
- [ ] OPT-10 XML/Layout Resolution Caching — pre-warm `LayoutCache` at startup; extend cache key with user override hash.
- [ ] OPT-7 Smart Invalidation — replace broad `Invalidate()` calls with targeted field-level invalidation. Expected: 2–3× faster updates.
- [ ] OPT-5 DisplayCommand Caching Enhancement — pre-compile layout operations to skip `ProcessFrag` tree walk for repeated layouts. Expected: 30–50% faster redraw.
- [ ] OPT-3 Manual Control Virtualization (spike first) — render only visible controls for long sense/example lists. Expected: 5–10× for large lists. High risk; needs spike.
- [ ] OPT-8 Control Count Reduction (spike first) — owner-draw composite controls for read-heavy panels. Expected: 20–30% faster layout. High risk; accessibility concerns.
- [ ] OPT-11 Integration Validation — all optimizations enabled together, full harness passes, performance targets met.

**Acceptance guard**: Each optimization must show relative improvement % over its own baseline run; no absolute ms thresholds (targets are % improvement).

**Feature flags**: Ship optimizations behind environment variable flags (e.g., `FW_PERF_COLLAPSE_DEFAULT`, `FW_PERF_ASYNC_LOAD`) for gradual rollout and rollback. See `research/FORMS_SPEEDUP_PLAN.md` Feature Flags section.

## Phase 8: Layout & Reconstruct Optimization (Native Views Engine)

**Purpose**: Eliminate redundant layout passes in the C++ Views engine that cause double-work during warm renders. Analysis shows Reconstruct (44.5%) + PerformOffscreenLayout (45.1%) together consume 89.6% of warm render time, and the second layout pass is provably redundant. See `research/layout-optimization-paths.md`.

- [x] PATH-L1 Width-invariant layout guard — add `m_fNeedsLayout` + `m_dxLastLayoutWidth` dirty-flag to `VwRootBox::Layout()`. When called with same width and box tree is not dirty, return immediately. Set dirty in `Construct()`, `PropChanged()`, `OnStylesheetChange()`, `putref_Overlay()`. Warm Layout drops from ~50ms to ~0.03ms.
- [x] PATH-L4 Harness GDI resource caching — cache offscreen Bitmap/Graphics/HDC/VwGraphics across calls instead of allocating per-call. Eliminates ~27ms overhead per warm PerformOffscreenLayout call.
- [x] PATH-R1 Reconstruct guard — add `m_fNeedsReconstruct` dirty-flag to `VwRootBox::Reconstruct()`. When called with no data change since last construction, skip entirely. Set dirty in `PropChanged()`, `OnStylesheetChange()`. Warm Reconstruct drops from ~100ms to ~0.01ms.
- [x] PATH-L1-VERIFY Run full benchmark suite and compare before/after timing evidence. Result: **99.99% warm render reduction** (153.00ms → 0.01ms). All 15 scenarios pass with 0% pixel variance. Cold render unaffected (62.33ms → 62.95ms).

**Deferred** (future iterations):
- [x] PATH-L5 Skip Reconstruct when data unchanged — gate `SimpleRootSite.RefreshDisplay()` on `VwRootBox.NeedsReconstruct` and cover it with focused tests.
- [ ] PATH-L3 Per-paragraph layout caching — dirty-flag line-breaking in `VwParagraphBox::DoLayout()`.
- [ ] PATH-L2 Deferred layout in Reconstruct — remove internal `Layout()` call from `Reconstruct()` (blocked: `RootBoxSizeChanged` callback needs dimensions immediately).