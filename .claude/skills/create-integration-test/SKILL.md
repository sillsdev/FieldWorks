---
name: create-integration-test
description: Turn a [class]-integration-test-plan.md into headless integration tests, one per plan item, each driving the scenario, asserting the outcome, and capturing a labeled snapshot. Use for TDD before implementation or as verification after it, whenever a test plan exists under Docs/migration/working/.
---

# Create Integration Test

Consumes the test plan a convert-* skill produced with the developer and
turns it into real tests, one plan item at a time. Plans exist for
dialogs, slices, AND owned controls (a control converted through the
custom-control sub-cycle gets its own plan under its own class name).

Input: the class name (or find the single plan when only one exists).
Plan location: `Docs/migration/working/<Class>/<Class>-integration-test-plan.md`.
If no plan exists, stop and say so -- the plan is authored with the
developer in the convert-* flow, never invented here.

## What each plan item becomes

One headless `[AvaloniaTest]` that:

1. **Drives** the scenario with the headless input pipeline (typing, focus,
   commands) from a constructed dialog/control in the state the item names.
2. **Asserts** the behavioral outcome in code -- this is the pass/fail gate
   (enabled state, staged value, validation message, payload content).
3. **Captures** a labeled snapshot as reviewable evidence
   (`DialogSnapshot.Capture`, name `<Class>-NN-<kebab-label>` numbered in
   plan order). Snapshots document; assertions gate.

Known headless limit: pointer hit-testing does not run headless. A plan item
that genuinely requires mouse interaction is implemented as far as the
model/keyboard path allows and flagged in the report as needing the manual
checkpoint.

## Where tests land

Derive the location from the SUT by the repository's test-mirroring rule
(tests mirror their SUT's project and subfolder):

- Dialog View/VM (`Src/Common/FwAvaloniaDialogs/`, flat) ->
  `FwAvaloniaDialogsTests/<Class>IntegrationTests.cs`
- Owned control in `Src/Common/FwAvaloniaDialogs/Controls/` (dialog
  composite) -> `FwAvaloniaDialogsTests/Controls/`; owned control in
  `Src/Common/FwAvalonia/` (shared) -> the `FwAvaloniaTests/` mirror of its
  subfolder
- Launcher edge (`Src/LexText/LexTextControls/Avalonia/`) ->
  `LexTextControlsTests/Avalonia/`
- Slice control (`Src/Common/FwAvalonia/Detail/`) ->
  `FwAvaloniaTests/Detail/` (create the folder on first use)
- Composer wiring (`Src/xWorks/Avalonia/Composer/`) ->
  `xWorksTests/Avalonia/Composer/`

## Process

Work the plan top to bottom, one item at a time: write the test, run it,
and record the result against the plan item (passing, failing-as-expected
for TDD, or blocked-needs-manual). Keep generated code to the repository
comment standard. When the plan is exhausted, run the owning suite in full
and report: items implemented, items flagged manual-only, suite counts, and
any plan item whose wording did not survive contact with the code (report
it back for the developer to re-decide -- do not silently reinterpret).
