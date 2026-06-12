---
name: fieldworks-avalonia-ui
description: "Build, review, or fix Avalonia UI code in FieldWorks: XAML, MVVM, view models, owned controls, headless tests, preview host, accessibility identity, and product-vs-preview wiring. Use for any change under Src/Common/FwAvalonia/, Src/Common/FwAvaloniaPreviewHost/, or Src/**/*.Avalonia/, and for net48/net8 Avalonia test changes — even if the request only mentions a control, a binding, a style, or a flaky UI test. For whole-surface migration planning use fieldworks-winforms-to-avalonia-migration first."
---

# FieldWorks Avalonia UI

## Use This For

- Avalonia XAML, view models, commands, lifetimes, dispatching, and
  resource/style changes.
- New or changed projects under `Src/**/**/*.Avalonia/`,
  `Src/Common/FwAvalonia/`, and `Src/Common/FwAvaloniaPreviewHost/`.
- Preview Host module registration, sample data providers, and UI
  diagnostics (see `.github/instructions/avalonia.instructions.md` for
  build/preview commands and project layout rules).
- UI host wiring that selects between Avalonia and legacy UI — apply
  `fieldworks-ui-wiring-review` alongside this skill.

## Start From the Established Patterns

Do not design controls or seams from scratch. The migration hub skill
(`fieldworks-winforms-to-avalonia-migration`) documents the decided
architecture; its `references/architecture-patterns.md` covers owned
controls, writing-system text fields, dialogs/flyouts, validation, and
lifetime. Canonical code to imitate:

- Owned field controls: `Src/Common/FwAvalonia/Region/FwFieldControls.cs`,
  `FwOptionPicker.cs`, `RegionMenuFlyout.cs`, `HoverReveal.cs`
- Region view + focus memory: `LexicalEditRegionView.cs`,
  `RegionFocusMemory.cs`
- Seams (scheduler, lifetime, clipboard, edit sessions):
  `Src/Common/FwAvalonia/Seams/ISeams.cs`
- Headless test setup: `Src/Common/FwAvalonia/FwAvaloniaTests/TestAppBuilder.cs`;
  examples in `RegionEditingTests.cs`, `VisualParityAndDensityTests.cs`
- Density constants: `Src/Common/FwAvalonia/Poc/PocDensity.cs`

## Required Checks

- Use current Avalonia docs for uncertain APIs; do not guess dispatcher,
  headless, automation, or binding behavior.
- Keep product UI strings localizable (`FwAvaloniaStrings.resx` or the
  StringTable lane); prototype hardcoded strings must be called out as gaps.
- Stamp stable, nonlocalized `AutomationProperties.AutomationId` (derived
  from IR `StableId` where applicable) and localized
  `AutomationProperties.Name` on user-facing controls.
- UI logic stays in bindings/view models where practical; avoid
  logic-heavy code-behind.
- Marshal to the UI thread through `IUiScheduler` (or Avalonia dispatcher
  in non-region code); no hidden `Task.Run`, no sync-over-async.
- Keep preview data lightweight unless the change explicitly opts into
  LCModel/project data; product-facing paths use real edit-session/domain
  contracts — detached DTO-only models remain preview-only.
- Headless tests: simulate input on `Window`, flush with
  `Dispatcher.UIThread.RunJobs()`, and capture visual regression frames
  with Skia (`UseHeadlessDrawing=false` + `CaptureRenderedFrame()`).
- Evidence runs through `./build.ps1` and `./test.ps1` via the normal repo
  graph, not branch-only lanes.

## Review Red Flags

- A Common project directly references a feature module without an
  explicit architecture decision.
- Preview-only code launched from product UI without a feature gate.
- Tests manually call `OnPropertyChanged(...)`, `ShowRecord()`, or similar
  instead of proving the real broadcast/wiring path.
- The active Avalonia path drives hidden legacy rendering/menu
  infrastructure (see the hub skill's hard rules).
- Sleep-based or timing-sensitive UI tests.
- Claims of accessibility, localization, IME, or keyboard parity without
  executable evidence (see the hub skill's
  `references/parity-evidence.md` §"Evidence language").

## Handoff

Report Avalonia docs consulted, tests run, remaining prototype gaps,
whether the change is product-facing or preview-only, and how the live
wiring path was validated for each affected host. For parity work, say
whether visual evidence is control-level headless capture or live desktop
capture, and which automation identities were assigned.

## Keep This Skill Current

When a control pattern, headless-test technique, or Avalonia API gotcha
proves out (or a pointer above goes stale), update this skill in the same
PR — and route durable architecture lessons through the protocol in
`fieldworks-winforms-to-avalonia-migration/references/lessons-learned.md`.
