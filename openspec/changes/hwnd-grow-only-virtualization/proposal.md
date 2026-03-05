## Why

FieldWorks DataTree currently creates a full Win32 HWND tree for every slice — even those never scrolled into view. Each of the 73 slice types inherits from `UserControl` and creates a `SplitContainer` (3 HWNDs) + `SliceTreeNode` (1 HWND) + content control (1+ HWNDs) = **6+ HWNDs per slice**. For a pathological 253-slice lexical entry, this means **~1,500 Win32 HWNDs** allocated just for the detail pane.

HWNDs are expensive kernel USER objects (~800+ bytes kernel heap each, cross-process synchronization, O(N) message broadcast on resize/show/hide, contributing to the 10,000 USER handle process limit). The current architecture hits this on every entry switch and is the single largest remaining render performance bottleneck — ranked #5 (VERY HIGH risk / HIGHEST impact) in `TIMING_ENHANCEMENTS.md`.

A "grow-only" virtualization strategy extends the existing `BecomeRealInPlace` deferred-initialization pattern so that **no slice creates any HWND until it scrolls into view**, but **never destroys an HWND once created**. This avoids the high-risk RootBox lifecycle management, IME composition loss, and accessibility tree instability of full recycle-based virtualization, while still achieving 80-95% HWND reduction for the common case (user views/edits the first screen of fields).

## What Changes

- **Defer HWND creation**: Slices are not added to `DataTree.Controls` (and thus never get their HWND allocated) until `HandleLayout1` determines they are in or near the viewport.
- **Defer SplitContainer + SliceTreeNode**: Move their construction from `Slice()` constructor and `Install()` into a new `EnsureHwndCreated()` method called on demand.
- **Virtual layout model**: Maintain parallel `_sliceTop[]` / `_sliceHeight[]` arrays for positioning non-HWND slices; enable `FindFirstPotentiallyVisibleSlice` binary search to work without HWNDs.
- **Measurement baseline**: Add HWND counting diagnostics and a regression gate test.

## Render Baseline Policy

- Render snapshot files (`*.verified.png`) are treated as immutable truth for this change.
- Any `RenderBaseline` mismatch is a code regression and must be fixed in product/test code.
- Do not accept or regenerate render baselines as part of HWND virtualization implementation.

## Non-goals

- Destroying or recycling HWNDs for off-screen slices (Phase 4 in the design — deferred as high-risk, diminishing-returns work).
- Replacing the WinForms control tree with owner-drawn rendering.
- Changing the Views engine or `IVwRootBox` lifecycle.
- Altering user-visible behavior, layout, or appearance.
- Cross-platform or Avalonia migration.

## Capabilities

### New Capabilities

- `hwnd-grow-only-virtualization`: Deferred HWND creation for DataTree slices — HWNDs are only allocated when a slice first scrolls into view and are never destroyed.
- `hwnd-diagnostic-baseline`: HWND counting infrastructure and regression gate tests for tracking kernel resource usage.

### Modified Capabilities

- `architecture/ui-framework/winforms-patterns`: DataTree/Slice control lifecycle changes — HWND creation is no longer synchronized with `Install()`.

## Impact

- **Primary code:** `Src/Common/Controls/DetailControls/Slice.cs`, `DataTree.cs`, `SliceTreeNode.cs`, `ViewSlice.cs`
- **All slice subclasses** (~73 classes across `Src/Common/Controls/DetailControls/` and `Src/LexText/`) are affected indirectly — any code accessing `SplitCont`, `TreeNode`, or `Control` before the slice is visible needs null guards.
- **Test project:** `Src/Common/Controls/DetailControls/DetailControlsTests/`
- **Benchmark harness:** `Src/Common/RenderVerification/` (HWND counter additions)
- **All FLEx areas** that use `RecordEditView` → `DataTree` benefit: Lexicon Edit, Grammar, Notebook, Lists, Words/Analyses, Interlinear Info Pane.
- **Managed C# only** — no native C++ changes required.
