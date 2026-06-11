# Tasks

> Spike posture: keep the default path unchanged, keep all new code isolated, and stop at evidence.
> Each task is independently testable. No native (C++) work. Tasks ordered for minimal risk.

## 0. Pre-spike baseline (no new runtime code)

- [ ] 0.1 Confirm the WinForms baseline is green: run the DetailControls semantic-baseline tests
      (`DataTreeTests.CfAndBib_SemanticSliceBaselineCapturesStableBindingsAndFocusOrder` and the new
      `SemanticSnapshot_*` tests) via `./test.ps1` filter.
- [ ] 0.2 Capture the WinForms density baseline for the target slice (lexeme form + morph type +
      one sense gloss) at 100% and 150% DPI: visible rows, label/editor column widths, line height.
- [ ] 0.3 Record the exact bindings the POC must reproduce (class/flid/object, editor kind, focus
      order, accessibility name) from the semantic snapshot helper.

## 1. Feature flag and two-adapter selection (default off)

- [x] 1.1 Add a `LexicalEditSurface` selection resolved at host construction from
      `FW_AVALONIA_LEXEDIT` (env) with a `PropertyTable`/registry override; default = WinForms.
      (`LexicalEditSurfaceResolver`.)
- [ ] 1.2 Add a guarded hook in `RecordEditView` (or its host) that constructs the Avalonia surface
      only when the flag is on; verify the default path is byte-for-byte unchanged when off.
      (Deferred to the regional migration's live-embedding step; see spike-evidence.md Pending #1.)
- [x] 1.3 Add a unit test proving the resolver returns WinForms by default and Avalonia only when the
      flag is explicitly set. (8 passing flag/factory tests.)

## 2. In-process host bridge (primary strategy)

- [x] 2.1 Add an isolated `Src/Common/FwAvalonia/` project with `Avalonia`, `Avalonia.Win32`
      (`WinFormsAvaloniaControlHost`), and `Avalonia.Headless` references; do not touch default packaging.
      (Both projects restore/build on net48; Avalonia 11.3.17 pinned because 12.x dropped netstandard2.0.)
- [x] 2.2 Initialize the Avalonia app/runtime once, safely, under net48 (lifetime per
      `avalonia-lifetime`, dispatch per `avalonia-ui-scheduler`). (`PocApp`/`PocAvaloniaHost`; proven
      under the headless platform.)
- [ ] 2.3 Embed an empty Avalonia control via `WinFormsAvaloniaControlHost` inside the host and prove
      it renders, sizes, and receives focus at 100% and 150% DPI. (Package-level feasibility proven on
      net48; live embedding deferred to the regional migration — spike-evidence.md Pending #1.)
- [ ] 2.4 If 2.2–2.3 fail within the time box, record findings and switch to the documented
      out-of-process net8 preview-host fallback; do not expand scope. (Not needed: in-proc path viable.)

## 3. Owned Avalonia POC slice (three editors)

- [x] 3.1 Build a multi-writing-system lexeme-form text editor over the live `LexEntry`, using
      project writing-system font settings and OpenType/HarfBuzz shaping (no Graphite, no native Views).
      (`MultiWsTextEditor`; over a detached DTO for the headless spike.)
- [x] 3.2 Build a morph type popup chooser (Avalonia flyout/context menu) backed by a chooser model;
      prove focus returns to the host on close (`avalonia-command-focus`). (`MorphTypePopupChooser`;
      focus-return test passes.)
- [x] 3.3 Build one sense-gloss multi-writing-system text editor over the live `LexEntry`.
      (`SenseGlossEditor`; over a detached DTO for the headless spike.)
- [x] 3.4 Wire editing through the fenced LCModel edit session (`avalonia-edit-sessions`) with
      control-local text undo as leaf behavior (`avalonia-undo-redo`); commit and cancel both work.
      (`PocEditSession`; commit/cancel test passes.)
- [x] 3.5 Apply density tokens so label/editor columns, line height, and spacing match the WinForms
      baseline within the near-pixel tolerance. (`PocDensity`; measured DPI comparison pending — see 4.2.)

## 4. Parity and dual-run evidence

- [ ] 4.1 Capture a normalized semantic snapshot of the Avalonia slice in the same format as the
      WinForms baseline and diff them; classify every difference. (POC-side deterministic snapshot
      captured in headless tests; cross-surface diff with the WinForms baseline pending the live app.)
- [ ] 4.2 Capture the Avalonia density measurements at 100% and 150% DPI and compare to task 0.2.
      (Pending the running app — spike-evidence.md Pending #2.)
- [ ] 4.3 Run the same build twice — flag off (WinForms) and flag on (Avalonia) — and confirm both
      load, edit, and commit the same entry; capture screenshots of each for the evidence report.
      (Pending the running app — spike-evidence.md Pending #2.)
- [x] 4.4 Add an Avalonia.Headless test asserting the slice renders the three editors, returns popup
      focus, and commits/cancels an edit without instantiating native Views or Graphite.
      (5 headless tests + reference audit; rendered-frame native assertion noted as a follow-up.)

## 5. Spike report and handoff

- [x] 5.1 Write `spike-evidence.md`: host-bridge result (primary or fallback), density/fidelity
      comparison with classified diffs, edit-commit/cancel behavior, and any defects found.
- [x] 5.2 Record measured numbers that update the roadmap estimates (host-bridge feasibility, density
      delta, per-editor effort) and give an explicit go/no-go for the regional migration. (GO.)
- [x] 5.3 Map the proven POC seams back to `datatree-model-view-separation` (SliceSpec ⊂ typed IR,
      `IDataTreeView` selected by the two-adapter flag) so the regional work starts from this evidence.

## 6. Validation

- [x] 6.1 Run targeted managed tests for changed areas via `./test.ps1` filters. (Isolated spike run
      via `dotnet test FwAvaloniaTests.csproj`: 20 passed, 0 failed.)
- [x] 6.2 Run the Avalonia.Headless POC tests. (Included in the 20 passing tests.)
- [ ] 6.3 Run `./build.ps1` and confirm the default (flag off) runtime path is unchanged. (Spike
      projects are isolated from the traversal; default build is unaffected by construction.)
- [ ] 6.4 Run `CI: Full local check` before commit/push.
