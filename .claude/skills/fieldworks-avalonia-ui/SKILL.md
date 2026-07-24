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
- Density constants: `Src/Common/FwAvalonia/FwAvaloniaDensity.cs`
- **Dialog kit (XAML + CommunityToolkit.Mvvm + compiled bindings):**
  `Src/Common/FwAvaloniaDialogs/` — `OptionsDialogView.axaml`/`.axaml.cs` +
  `OptionsDialogViewModel.cs`; headless tests in `FwAvaloniaDialogsTests/`.
  This is the verified template for hand-authored dialogs — see
  "Converting a WinForms dialog (MVVM kit)" below.

**Re-implementing a Phase-1 deferred screen (JIRA tickets).** The full recipe (per-screen
`Docs/migration/<screen>.md` on the never-merged `phase1-docs` branch, stub recovery from git
history, which canonical screen to copy, `UIMode=New` re-wiring) is canonical in the migration
hub skill — `.claude/skills/fieldworks-winforms-to-avalonia-migration/SKILL.md`; start there.

## Converting a WinForms dialog (MVVM kit)

Hand-authored dialogs/wizards use **XAML + CommunityToolkit.Mvvm + compiled
bindings** — NOT the region/IR pattern (that is only for XML-view-definition
surfaces). Full step-by-step, the working template, and the decision
history/rationale pointer: `references/dialog-conversion.md`. The shape, per
dialog:

1. **View** `XyzDialogView.axaml` (+ `.axaml.cs`): a `UserControl` (not a
   `Window` — see modality below), `x:DataType` set to the view-model,
   compiled `{Binding}`s, and a stable `AutomationProperties.AutomationId`
   on every interactive control. Reuse owned controls (`FwMultiWsTextField`,
   `FwOptionPicker`) for writing-system fields and list pickers.
2. **View-model** `XyzDialogViewModel.cs`: `ObservableObject` with
   `[ObservableProperty]` state and `[RelayCommand]` actions; expose the
   result (e.g. `Accepted`). Keep it LCModel-free for the view; bind real
   settings/domain through the app-settings/edit-session seams.
3. **Tests** `XyzDialogTests.cs` (headless `[AvaloniaTest]`): assert the
   compiled bindings propagate both directions and the commands fire, plus
   the per-stage PNG captures and subjective checks under "Dialog spacing"
   below — together, the per-dialog definition of done.

### Style system (density + borders, per surface)

The font/density tokens and the field-border rule are a GLOBAL system, calibrated to WinForms density —
not the roomy Fluent defaults — and applied per-control-tree (the only mechanism that renders in BOTH the
runtime host and the headless tests). Full detail, the calibrated numbers, and the per-surface intent:
**`references/style-system.md`**. Headlines: **dialog inputs are BOXED** (`Border.fwFieldHost`),
**detail/region values are FLAT** with subtle separators, **browse keeps its grid lines** — just denser
font everywhere; one source of truth per family (`DialogTheme.axaml` for dialogs, `FwSurfaceStyles` for
region/browse); anything that must render headlessly uses a **CONCRETE value, never a Fluent
`DynamicResource`**.

### Dialog spacing

All dialog spacing/borders come from the shared tokens in
`Src/Common/FwAvaloniaDialogs/DialogTheme.axaml` (applied to each dialog body by
`DialogThemeBootstrap.Apply(this)`, called from every dialog view ctor). See `style-system.md`'s "Dialog
spacing tokens" table for the current calibrated values — that table is the single source of truth; don't
copy the numbers here too. Headlines (full rules + rationale:
`references/dialog-conversion.md` §2a-bis):

- Every dialog root carries `Classes="fwDialogRoot"` (window padding); no root `Margin` literal.
- No text-bearing or `PART_*Host` control with 0 padding — host borders carry `Classes="fwFieldHost"`.
- OK/Cancel use the standard button-strip gap tokens.
- Never hardcode a margin/spacing literal — use a token; add new tokens to `DialogTheme.axaml`.
- The headless `DialogLayoutAssert.AssertNoCrowding(view)` tripwire gates this in every dialog's
  realized-view test.
- **Capture a PNG at EACH interaction stage via `DialogSnapshot.Capture(view, "<Surface>-<NN>-<stage>")`
  (→ flat gitignored folder `Output/Snapshots/<Surface>-<NN>-<stage>.png`), then Read each PNG and answer
  the six subjective-quality questions — a hard rule and part of the per-dialog definition of done,
  for region/browse surfaces too.** The canonical checklist, the six questions, and the
  capture → run → Read → judge → fix → re-capture loop: `references/visual-snapshot-testing.md`.

Rules specific to dialogs:

- **It lives in `Src/Common/FwAvaloniaDialogs/`** (the dedicated XAML project),
  never in the pure-C# `FwAvalonia` foundation. Avalonia projects — including
  the XAML-compiled ones — are ordinary members of the `FieldWorks.proj`
  traversal (the `Src` glob); a new dialog project just needs adding to
  `FieldWorks.sln` (restore + VS). Exclude any nested test folder from the
  library's compile glob (`<Compile Remove="XxxTests/**/*.cs"/>`).
- **Modality during coexistence:** no Avalonia `Window.ShowDialog` — show the
  dialog `UserControl` via **`AvaloniaDialogHost.ShowModal`**; the view-model
  implements **`IDialogViewModel`** and raises `CloseRequested(bool)` from
  OK/Cancel. Mechanics + code: `references/dialog-conversion.md` §2.
- **Coexistence sync with the WinForms twin:** while both implementations
  ship, they are edited together — the apply-order mirroring, divergence
  register, and paired-edit rules live in the `dialog-update` skill.
- **Scope:** simple/confirmation/settings dialogs are good junior+AI work;
  Views-engine-coupled dialogs (Find/Replace, Styles host `IVwRootSite`)
  belong with the document engine (Stage 9), NOT this kit.

## Required Checks

- Use current Avalonia docs for uncertain APIs; do not guess dispatcher,
  headless, automation, or binding behavior.
- Keep field labels on the StringTable strategy. Product-facing
  FieldWorks-owned strings go in the project `.resx` and are consumed via
  the string accessor (`FwAvaloniaStrings`/`FwAvaloniaDialogsStrings`),
  never hardcoded; the neutral resx is the English source of truth.
  Prototype hardcoded strings must be called out as gaps.
- Stamp stable, nonlocalized `AutomationProperties.AutomationId` (derived
  from IR `StableId` where applicable) and localized
  `AutomationProperties.Name` on user-facing controls.
- UI logic stays in bindings/view models where practical; avoid
  logic-heavy code-behind.
- For any Avalonia "select from a list" surface, prefer the shared
  `FwOptionPicker` pattern in `Src/Common/FwAvalonia/Region/FwOptionPicker.cs`
  (AutoCompleteBox-based, keyboard-safe, search-capable, compact density)
  over ad hoc `ListBox` popups or one-off editable selectors. Reach for a raw
  `ComboBox` only when the UX explicitly needs an always-visible inline combo
  rather than the shared flyout selector.
- Do not fix Avalonia keyboard, focus, filtering, selection, popup, or
  rendering bugs by patching `System.Windows.Forms` hosts, WinForms
  interop message handling, or other legacy host-only routes unless the
  task explicitly targets interop behavior. Default to fixing the issue
  inside the Avalonia control tree or Avalonia-owned seams.
- Marshal to the UI thread through `IUiScheduler` (or Avalonia dispatcher
  in non-region code); no hidden `Task.Run`, no sync-over-async.
- Keep preview data lightweight unless the change explicitly opts into
  LCModel/project data; product-facing paths use real edit-session/domain
  contracts — detached DTO-only models remain preview-only.
- Headless tests: simulate input on `Window`, flush with
  `Dispatcher.UIThread.RunJobs()`, and capture visual regression frames
  with Skia (`UseHeadlessDrawing=false` + `CaptureRenderedFrame()`).
- Resx satellite assemblies need no runtime bootstrap; only tests that
  exercise genuine Chorus-supplied UI need an L10NSharp
  LocalizationManager.
- Evidence runs through `./build.ps1` and `./test.ps1` via the normal repo
  graph, not branch-only build paths.

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
