# DPI Fix Implementation

Date: 2026-03-10

## Goal

Make the render-speedup fast paths safe when monitor DPI changes without a width change.

## Findings Being Fixed

- `VwRootBox::Layout()` PATH-L1 compared width only and returned before refreshing `m_ptDpiSrc`.
- `SimpleRootSite` only forced layout when available width changed, so many hosts would never call native `Layout()` on a DPI-only change.
- `VwLayoutStream::ConstructAndLayout()` already had a TODO noting that it should also check DPI.

## Implementation Plan

- [x] Track DPI as part of the native root-box layout cache key.
- [x] Refresh native cached DPI after incremental relayout.
- [x] Force managed layout when `SimpleRootSite.Dpi` changes.
- [x] Make `VwLayoutStream::ConstructAndLayout()` relayout on DPI changes.
- [x] Add native regression coverage for same-width DPI changes.

## Files

- `Src/views/VwRootBox.cpp`
- `Src/views/VwRootBox.h`
- `Src/views/VwLayoutStream.cpp`
- `Src/Common/SimpleRootSite/SimpleRootSite.cs`
- `Src/views/Test/TestVwRootBox.h`

## Validation

- [x] Run native `TestViews`.
- [x] Run a managed build for touched managed code.
- [x] Run a focused managed test pass if available (`SimpleRootSiteTests`).

## Notes

- This change treats DPI as layout state, not reconstruct state.
- `NeedsReconstruct` / PATH-L5 remain data- and structure-driven; DPI changes only force relayout.
- `RootSiteTests` still has unrelated pre-existing failures in this worktree, so validation used the narrower `SimpleRootSiteTests` pass for the managed change.