## Why

Before committing to the full Lexical Edit Avalonia migration (`lexical-edit-avalonia-migration`)
and the DataTree region split (`datatree-model-view-separation`), we need to de-risk the three
unknowns that dominate cost and feasibility:

1. **Host bridge** — can an Avalonia-rendered editing surface run inside the existing
   .NET Framework 4.8 FieldWorks process, selected at runtime beside the WinForms view, without a
   second process or a net8 migration of the shell?
2. **Visual functional fidelity and density** — can an owned Avalonia editor reproduce the
   information density, label/editor affordances, focus order, and writing-system text behavior of
   the WinForms DataTree slices to *near-pixel* (not pixel-perfect) parity?
3. **Feature-flag dual-run** — can the new path be placed behind a flag so the same build runs
   either Avalonia or the legacy WinForms controls, with a safe default of WinForms?

This change is the **proof-of-concept spike** named in the migration roadmap
(`avalonia-migration-roadmap`). It is intentionally small, behind a default-off flag, and does not
change any default behavior. Its purpose is to produce evidence that converts the roadmap's
remaining estimates from guesses into measured numbers, then hand off to the regional migration.

## What Changes

- Add a runtime **feature flag** (default off) that selects between the existing WinForms Lexical
  Edit surface and an Avalonia proof-of-concept surface for a single, narrow vertical slice.
- Build one **owned Avalonia editing slice** that renders three representative editors over the
  current `LexEntry` data: a multi-writing-system **lexeme form** text editor, a **morph type**
  popup chooser, and one **sense gloss** multi-writing-system text editor.
- Host the Avalonia slice **in-process on net48** beside the WinForms `RecordEditView`, evaluating
  `WinFormsAvaloniaControlHost` (Avalonia.Win32 interop) as the primary host-bridge strategy, with
  an out-of-process net8 preview host as the documented fallback if in-proc embedding fails.
- Reuse the existing seam decisions (`avalonia-edit-sessions`, `avalonia-undo-redo`,
  `avalonia-validation`, `avalonia-command-focus`, `avalonia-ui-scheduler`, `avalonia-lifetime`) at
  the minimum level needed to make the slice editable and committable, without reopening them.
- Capture a **semantic + density parity snapshot** comparing the WinForms baseline and the Avalonia
  slice for the same entry, using the normalized snapshot format introduced by the parity tests.
- Produce a short **spike evidence report** with host-bridge findings, a density/fidelity comparison,
  edit-commit/cancel behavior, and a go/no-go recommendation for the regional migration.

## Non-goals

- Replacing or removing any WinForms control, DataTree, Slice, SliceFactory, or native Views code.
- Making Avalonia the default for any surface. The flag defaults to WinForms.
- Implementing the full typed view-definition IR, XML import pipeline, or XML retirement.
- Graphite or native-rendering decommissioning beyond confirming the POC slice does not depend on
  Graphite or native Views at runtime.
- Migrating tables/browse views, nested sequences, custom fields, ghost items, or choosers beyond
  the single morph type chooser in the slice.
- Production-quality undo/redo, accessibility, localization, or performance hardening. The spike
  records gaps; it does not close them.
- Pixel-perfect parity. The target is near-pixel parity with equivalent density and interaction.

## Capabilities

### New Capabilities

- `lexical-edit-avalonia-poc-spike`: Time-boxed, flag-gated, in-process Avalonia proof-of-concept for
  one Lexical Edit vertical slice, with host-bridge, parity, and dual-run evidence requirements.

## Impact

- Managed code: new `Src/Common/FwAvalonia/` (POC host + slice) and a small flag/selection hook in
  `Src/xWorks/RecordEditView.cs` (or its host) guarded so default behavior is unchanged. No native
  (C++) changes.
- Tests: `Src/Common/Controls/DetailControls/DetailControlsTests/` (semantic snapshot baseline the
  POC must match) and new Avalonia.Headless tests for the POC slice.
- Dependencies: may add `Avalonia`, `Avalonia.Win32` / `WinFormsAvaloniaControlHost`, and
  `Avalonia.Headless` package references behind the POC project only; no change to default packaging.
- Build/packaging: the POC project builds but the flag default keeps it out of the default runtime
  path; installer/packaging is unchanged for this spike.
