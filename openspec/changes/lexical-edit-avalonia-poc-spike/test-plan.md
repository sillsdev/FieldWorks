# POC Spike Test Plan

This plan defines the tests that establish the WinForms baseline the POC must match, and the
Avalonia.Headless tests that prove the POC slice. It is intentionally small and reuses the existing
DetailControls characterization harness.

## Test layers

| Layer | Project | Runs against | Purpose |
|-------|---------|--------------|---------|
| Semantic baseline | `DetailControlsTests` | WinForms `DataTree`/`Slice` | Lock the bindings, editor kinds, and focus order the POC must reproduce. |
| Density baseline | manual + screenshot evidence | WinForms `RecordEditView` | Capture rows/column widths/line height at 100% and 150% DPI. |
| Flag/selection | new POC unit test | surface resolver | Prove default = WinForms, Avalonia only when flag on. |
| Avalonia slice | Avalonia.Headless | POC slice | Prove three editors, popup focus return, commit/cancel, no native/Graphite. |
| Dual-run | manual + screenshot evidence | full app, same build | Prove flag off vs on both load/edit/commit the same entry. |

## 1. WinForms semantic baseline (already runnable today)

These run against existing code and ship in this change so the baseline is locked before any POC code
exists. They extend the existing
`DataTreeTests.CfAndBib_SemanticSliceBaselineCapturesStableBindingsAndFocusOrder`.

- **`SemanticSnapshot_CfAndBib_IsStableAndCapturesPocBaseline`**
  - Realize the `CfAndBib` layout, build a normalized semantic snapshot string
    (`#order | label | field | flid | editor | visibility | a11y`), and assert it matches the values
    already proven by the existing baseline test.
  - Realize a second time and assert the snapshot is byte-for-byte identical (determinism), which is
    the property the Avalonia parity comparison relies on.

Rationale: every asserted value is already proven by the existing passing baseline test, so the new
test adds a reusable normalized-snapshot format with no new brittle expectations.

## 2. Flag / two-adapter selection

- **`Surface_DefaultsToWinForms_WhenFlagUnset`** — resolver returns the WinForms surface when no flag
  is set.
- **`Surface_SelectsAvalonia_WhenFlagEnabled`** — resolver returns the Avalonia surface only when
  `FW_AVALONIA_LEXEDIT` (or the `PropertyTable`/registry override) is enabled.
- **`Surface_FlagOff_ConstructsNoAvaloniaRuntime`** — with the flag off, no Avalonia app/host/slice is
  constructed (assert via a construction counter or guard).

## 3. Avalonia.Headless slice tests

- **`PocSlice_RendersThreeEditors`** — lexeme form, morph type chooser entry point, and sense gloss are
  present and bound to the live `LexEntry`.
- **`PocSlice_MorphTypeChooser_ReturnsFocusToHost`** — opening then closing the chooser flyout returns
  focus to the host editor.
- **`PocSlice_CommitWritesLcmAndCancelLeavesUnchanged`** — editing then committing updates LCModel to
  match the equivalent WinForms edit; cancelling leaves LCModel unchanged.
- **`PocSlice_DoesNotInstantiateNativeViewsOrGraphite`** — rendering and committing complete without
  constructing native Views or Graphite render engines (assert via instrumentation/guard).
- **`PocSlice_WritingSystemText_UsesProjectFontSettings`** — each writing-system alternative uses the
  configured font family/size/flow direction and OpenType feature settings.

## 4. Density / fidelity evidence (measured, not asserted)

Captured into `spike-evidence.md`, not as pass/fail unit tests, because the target is near-pixel:

- Visible rows for the slice at 100% and 150% DPI (WinForms vs Avalonia).
- Label column width, editor column width, and line height (WinForms vs Avalonia).
- Side-by-side screenshots of the same entry under flag off and flag on.
- A classification table: accepted near-pixel variance, font/rendering variance, missing data,
  regression.

## 5. Validation commands

- `./test.ps1` filtered to `DetailControlsTests` for the semantic baseline.
- `./test.ps1` filtered to the new POC test project for flag/headless tests.
- `./build.ps1` then launch with the flag off to confirm the default path is unchanged.
- `CI: Full local check` before commit/push.

## Exit criteria

- Section 1 and Section 2 tests pass.
- Section 3 headless tests pass (or the documented out-of-process fallback is in use with equivalent
  evidence).
- Section 4 evidence is captured at both DPIs with classified diffs.
- `spike-evidence.md` records a go/no-go.
