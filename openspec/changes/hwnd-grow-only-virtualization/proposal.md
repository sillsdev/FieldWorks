## Why

Update (March 2026): this change is now scoped to **measurement baseline only**.

Recent render benchmark analysis shows warm render cost is dominated by duplicate Views-engine layout passes (`Reconstruct` + `PerformOffscreenLayout`) rather than WinForms HWND allocation. The current branch therefore prioritizes lower-risk, higher-impact Views optimizations first, and keeps HWND work as diagnostics-only groundwork.

FieldWorks DataTree currently creates a full Win32 HWND tree for every slice — even those never scrolled into view. Each of the 73 slice types inherits from `UserControl` and creates a `SplitContainer` (3 HWNDs) + `SliceTreeNode` (1 HWND) + content control (1+ HWNDs) = **6+ HWNDs per slice**. For a pathological 253-slice lexical entry, this means **~1,500 Win32 HWNDs** allocated just for the detail pane.

HWNDs are expensive kernel USER objects (~800+ bytes kernel heap each, cross-process synchronization, O(N) message broadcast on resize/show/hide, contributing to the 10,000 USER handle process limit). The current architecture hits this on every entry switch and is the single largest remaining render performance bottleneck — ranked #5 (VERY HIGH risk / HIGHEST impact) in `TIMING_ENHANCEMENTS.md`.

The previously explored "grow-only" virtualization strategy is explicitly deferred due to cross-cutting UI lifecycle risk (focus, event subscription timing, accessibility tree stability) for this milestone.

## What Changes

- **Measurement baseline only**: keep HWND counting diagnostics and regression gate tests.
- **Render telemetry only**: log handle counts during render harness runs to preserve baseline visibility.
- **No behavioral virtualization in this branch**: deferred install/lazy HWND/layout virtualization code paths are out of scope and reverted.

## Non-goals

- Destroying or recycling HWNDs for off-screen slices (Phase 4 in the design — deferred as high-risk, diminishing-returns work).
- Replacing the WinForms control tree with owner-drawn rendering.
- Changing the Views engine or `IVwRootBox` lifecycle.
- Altering user-visible behavior, layout, or appearance.
- Cross-platform or Avalonia migration.

## Capabilities

### New Capabilities

- `hwnd-diagnostic-baseline`: HWND counting infrastructure and regression gate tests for tracking kernel resource usage.

### Modified Capabilities

- None (runtime behavior intentionally unchanged for this change).

## Impact

- **Primary code:** `Src/Common/Controls/DetailControls/HwndDiagnostics.cs`
- **Counters only:** `Src/Common/Controls/DetailControls/Slice.cs`, `Src/Common/Controls/DetailControls/DataTree.cs`
- **Benchmark harness telemetry:** `Src/Common/RenderVerification/DataTreeRenderHarness.cs`
- **Baseline test coverage:** `Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeHwndCountTest.cs`
- **Managed C# only** — no native C++ behavior change.
