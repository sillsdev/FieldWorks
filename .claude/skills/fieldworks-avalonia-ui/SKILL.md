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

**Re-implementing a Phase-1 deferred screen (JIRA tickets).** Many screens were documented
and backed out to land the Phase-1 PR; each has a ticket + a `Docs/migration/<screen>.md`
(what it is, legacy PNGs, parity checklist, gotchas, the git sha of the backed-out stub).
**`Docs/migration/` (including `_TEMPLATE.md`) lives on the separate, never-merged
`phase1-docs` branch, not in this checkout** — pull the doc from that branch rather than
assuming it exists in your working tree. Start there: recover the stub from git history as a skeleton, copy the **canonical screen**
for that primitive (the hub skill's "Phase-1 Landing Strategy" names them — e.g. ChooserDialog
for tree/multi-select, OptionsDialog for tabs, InsertEntryDialog for owned-control forms), then
follow the per-region Workflow. Re-wire the call site behind `UIMode=New` (Legacy keeps the
WinForms path).

## Converting a WinForms dialog (MVVM kit)

Hand-authored dialogs/wizards use **XAML + CommunityToolkit.Mvvm + compiled
bindings** — NOT the region/IR pattern (that is only for XML-view-definition
surfaces). Decided 2026-06-15; the original rationale doc
(`avalonia-migration-roadmap/complete-migration-program.md` §11.3) was
relocated to the `phase1-docs` branch and is not present here — this dialog
kit's own template (below and `references/dialog-conversion.md`) is the
proven shape that decision produced. Full step-by-step + the working
template: `references/dialog-conversion.md`. The shape, per dialog:

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
   compiled bindings propagate both directions and the commands fire, AND
   capture a PNG at **each interaction stage** (empty, populated, error/disabled,
   post-action) for visual review — this is the per-dialog definition of done.

### Style system (density + borders, per surface)

The font/density tokens and the field-border rule are a GLOBAL system, calibrated to WinForms density
(~12px font, ~22px control height) — not the roomy Fluent defaults — and applied per-control-tree (the only
mechanism that renders in BOTH the runtime host and the headless tests). Full detail, the calibrated numbers,
and the per-surface intent: **`references/style-system.md`**. The short version:

- **Dialog inputs are BOXED** (`TextBox`/`ComboBox`, and the `PART_*Host` borders wrapping owned editors get a
  visible 1px gray border via `Border.fwFieldHost`); **detail/region values are FLAT** with subtle separators
  (the DataTree look — do not box them); **browse keeps its grid lines** — just denser font everywhere.
- One source of truth per family: dialog density/borders in `DialogTheme.axaml` (applied by
  `DialogThemeBootstrap.Apply`); region/browse font in `FwSurfaceStyles` (applied in those view ctors).
- **Borders/colors that must render headlessly use a CONCRETE value, never a Fluent `DynamicResource`** — the
  field-host border is `#FF7A7A7A`, because `ControlStrokeColorDefault` resolved to nothing headlessly.

### Dialog spacing

All dialog spacing/borders come from the shared tokens in
`Src/Common/FwAvaloniaDialogs/DialogTheme.axaml` (applied to each dialog body by
`DialogThemeBootstrap.Apply(this)`, called from every dialog view ctor). See `style-system.md`'s "Dialog
spacing tokens" table for the current calibrated values (window padding, control gap, label-field gap,
min control height, field padding, font size) — that table is the single source of truth; don't copy the
numbers here too. Hard rules:

- **Every dialog root carries `DialogWindowPadding`** — put `Classes="fwDialogRoot"` on the root
  `UserControl` and drop any root `Margin` literal.
- **No text-bearing or `PART_*Host` control with 0 padding** — host borders carry `Classes="fwFieldHost"`
  (a real border + `DialogFieldPadding`); never leave a host border without a `BorderThickness`.
- **OK/Cancel use the standard button strip gap** (`DialogButtonStripGap` + `DialogControlGapAbove`).
- **Never hardcode a margin/spacing literal — use a token** (`DialogControlGap`, `DialogLabelFieldGap`,
  …); add a new token to `DialogTheme.axaml` rather than inlining a number.
- The headless `DialogLayoutAssert.AssertNoCrowding(view)` tripwire gates this in every dialog's
  realized-view test. Full detail: `references/dialog-conversion.md` §2a-bis.
- **Capture a PNG at EACH stage and look at it (hard rule, part of the per-dialog definition of done).**
  Every dialog test captures `DialogSnapshot.Capture(view, "<Surface>-<NN>-<stage>")` (→ gitignored
  per-surface folder `Output/Snapshots/<Surface>/<NN>-<stage>.png`) at every meaningful UI state it drives — empty/initial, populated,
  validation-error / OK-disabled, post-action — *before* any `AssertNoCrowding`. After running the dialog
  tests, the agent MUST **Read each per-stage PNG** and explicitly answer the **six subjective-quality
  questions** per image — (1) alignment of words/columns/icons/labels, (2) appropriate spacing between
  words/fields/graphics, (3) borders/containment, (4) clipping/overflow, (5) overlap, (6) legibility &
  consistency — writing the answers down, not just glancing. An off-looking PNG is a real finding even when
  `AssertNoCrowding` passes. The loop is: capture → run → Read each PNG → answer the six questions → fix
  view/token → re-capture. Applies to region/browse surfaces too. Full checklist + workflow:
  `references/visual-snapshot-testing.md`.

Rules specific to dialogs:

- **It lives in `Src/Common/FwAvaloniaDialogs/`** (the dedicated XAML project),
  never in the pure-C# `FwAvalonia` foundation. Add new dialog projects to
  `FieldWorks.sln` (restore + VS) but build them in `build.ps1`'s Avalonia
  loop; keep the XAML compile off the main `FieldWorks.proj` traversal (the
  exclude pattern there). Exclude any nested test folder from the library's
  compile glob (`<Compile Remove="XxxTests/**/*.cs"/>`).
- **Modality during coexistence:** no Avalonia `Window.ShowDialog`. Show the
  dialog `UserControl` via **`AvaloniaDialogHost.ShowModal(owner, view, vm, title)`**
  (in `Src/Common/FwAvalonia/`), which hosts it in a WinForms-owned modal `Form`
  (per the hub's `dialog-ownership.md`). The view-model implements
  **`IDialogViewModel`** and raises `CloseRequested(bool)` from OK/Cancel — no
  windowing in the VM. A dialog is "view + VM + `ShowModal`."
- **Scope:** simple/confirmation/settings dialogs are good junior+AI work;
  Views-engine-coupled dialogs (Find/Replace, Styles host `IVwRootSite`)
  belong with the document engine (Stage 9), NOT this kit.

## Required Checks

- Use current Avalonia docs for uncertain APIs; do not guess dispatcher,
  headless, automation, or binding behavior.
- Keep field labels on the StringTable lane. Product-facing Avalonia chrome
  should join the existing LocalizationManager/L10NSharp XLIFF catalog,
  preferably by reusing existing `Palaso`/`Chorus` ids when they actually
  match; otherwise add unique Avalonia-prefixed ids there. The current
  English-default source is the accessor code, not a parallel Avalonia
  `.resx` runtime lane. Prototype hardcoded strings must be called out as
  gaps.
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
- If Avalonia chrome uses LocalizationManager, bootstrap it once per
  preview-host or headless-test process before localized strings are
  requested; do not pay that cost per test.
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
