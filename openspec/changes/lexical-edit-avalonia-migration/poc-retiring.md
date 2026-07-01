# Poc Retiring

## Goal

Retire `Poc` as a live code path without losing the lexical-edit Avalonia foundation already
proven on this branch.

The target is the "best of version 3" boundary:

- one shared lexical-edit region renderer for product and preview
- lightweight preview/sample data that stays detached from LCModel
- no second detached DTO/slice/editor stack to maintain
- live runtime/bootstrap/styling code renamed by role instead of by spike history

## Decision

Preview now sits at the **region-model boundary**, not at the retired DTO/slice boundary.

- **Product path** stays: compiled definition -> `LexicalEditRegionModel` ->
  `LexicalEditRegionView` -> LCModel-backed edit context.
- **Preview path** becomes: sample `LexicalEditRegionModel` -> `LexicalEditRegionView` ->
  lightweight in-memory edit context.

This keeps preview detached from LCModel while removing the duplicate rendering stack.

## Code Plan

### Keep

- `LexicalEditRegionView` and the owned field controls as the single renderer surface.
- Lightweight sample preview data, but expressed as a region model.
- The WinForms/Avalonia host bridge, Avalonia app bootstrap, and shared density tokens.

### Rename

- `PocWinFormsHostControl` -> `LexicalEditHostControl`
- `PocApp` -> `FwAvaloniaApp`
- `PocAvaloniaHost` -> `FwAvaloniaHost`
- `PocDensity` -> `FwAvaloniaDensity`

### Remove

- `PocEntryDto`
- `PocLexEntrySlice`
- `PocEditSession`
- `MultiWsTextEditor`
- `MorphTypePopupChooser`
- `PocPreviewWindow`
- `PocPreviewDataProvider`
- the old spike-only headless tests that exercised that detached stack

## OpenSpec Fold-Forward

- Treat `lexical-edit-avalonia-poc-spike` as absorbed into
  `lexical-edit-avalonia-migration`.
- Keep the migration change as the surviving source of truth.
- Replace live references to the old spike handoff with this note where a current explanation is
  needed.
- Remove the old spike change files after the migration change records the surviving plan/evidence.

## Implementation Status

- Shared preview path implemented with `LexicalEditPreviewWindow` and
  `LexicalEditPreviewDataProvider`.
- Preview host now routes to `lexical-edit-preview` and renders the shared region view.
- Live runtime types renamed to role-based names (`LexicalEditHostControl`, `FwAvaloniaApp`,
  `FwAvaloniaHost`, `FwAvaloniaDensity`).
- Detached `Poc/` runtime stack deleted.
- Old spike change removed after fold-forward.

## Validation

- `./test.ps1 -TestProject "Src/Common/FwAvalonia/FwAvaloniaTests/FwAvaloniaTests.csproj" -TestFilter "FullyQualifiedName~LexicalEditPreviewTests" -SkipWorktreeLock`
  passed after the shared preview-path change.
- `./test.ps1 -TestProject "Src/Common/FwAvalonia/FwAvaloniaTests/FwAvaloniaTests.csproj" -SkipWorktreeLock`
  found one unrelated existing failure:
  `ReferenceAdd_Success_FiresTheGestureCallbackOnce_AndFailureDoesNot` in
  `RegionEditingViewTests`; the preview-path and rename work itself held.