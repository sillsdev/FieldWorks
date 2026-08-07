## Why

Phase 1 (`lexical-edit-avalonia-migration`) derisked the hard part. Every functional surface — detail editors, every field type, the browse table with full sort/filter/bulk-edit, the dialog kit, undo/redo, clipboard/drag-drop, command execution, the WinForms-density visual system — was rebuilt in Avalonia with **WinForms still switchable in/out**, **everything still hosted through the WinForms shell**, and **no code deleted**. Full linguistic depth was preserved with no functional regression; Graphite shaping is the only intentional drop (rendered via OpenType/HarfBuzz instead, per `graphite-transition-support`). The real buttons, dialogs, strings, editors, and undo behavior were implemented and tester-validated.

What remains is the frame rip-out. FieldWorks still **starts and composes** through a WinForms/XCore shell on .NET Framework 4.8, so the default product is a fully functional Avalonia island inside the old shell rather than a true cross-platform application. Phase 2 is the cutover: kill WinForms completely, retarget the managed app from net48 to .NET 10, and ship a real cross-platform (Windows/macOS/Linux) Avalonia application.

Because the functional behavior was already proven in Phase 1, this change reads as **"it works or it doesn't."** The risk is not whether a button or editor behaves correctly — that was validated. The risk is purely lifecycle, retarget, packaging, and the disciplined removal of code that Phase 1 deliberately left in place.

## What Changes

- Absorb and supersede `fieldworks-avalonia-shell-migration`: this change consumes the frozen seam decisions and the shell's already-specified Avalonia application/window lifetime, command bridge, screen registry, dialog/service abstractions, and final WinForms decommissioning, and is the single active Phase-2 plan as of 2026-06-20.
- Make Avalonia the **sole** default shell: no WinForms shell, host, dynamic content host, or `FlexUIAdapter` default path is created in the default application path.
- Retarget the FieldWorks managed projects and build (`build.ps1` / `FieldWorks.proj`) from **.NET Framework 4.8 to .NET 10**, including package, analyzer, and language-level changes.
- Establish a **single Avalonia application lifetime** (classic desktop lifetime, explicit top-level window ownership) in place of the Phase-1 dual lifetime where WinForms hosts Avalonia content.
- Bring the application up as **true cross-platform**: build, run, package, and CI on Windows, macOS, and Linux.
- **Delete** the code Phase 1 forbade removing, behind per-target gates: the WinForms shell and main-window services, WinForms-only default dialogs, the `FlexUIAdapter` default behavior, Gecko/Graphite-on-Avalonia assumptions, and native Views rendering from the default path.
- Keep retained native services (the native `ITsString`/writing-system kernel as the **data model**, ICU via managed `icu.net`, EncConverters, XAmple, spelling, parser) behind non-UI service seams; these are consumed via managed interop and are **not** in the Avalonia render/editor path.
- Gate the entire cutover on the **Phase-1 functional parity burn-down reaching zero** (`lexical-edit-avalonia-migration` §19): no open functional regression before any WinForms removal.

## Non-goals

- No LCModel rewrite, lexicon data schema change, or project-format change.
- No removal of the retained native `ITsString`/writing-system **data kernel**; killing WinForms and going net10/cross-platform does not require removing the native data model consumed via managed interop.
- No removal of retained non-UI linguistics services (XAmple, spelling, parser, ICU, EncConverters) when isolated behind service seams.
- No new functional behavior, editor, dialog, or field type — that work belongs to Phase 1; Phase 2 is lifecycle/retarget/packaging/deletion only.
- No re-derisking of Avalonia functional parity; this change consumes Phase-1's proven parity rather than reopening it.
- No runtime WinForms/Avalonia fallback switch in the final app: the cutover gate is the Phase-1 burn-down, not a permanent dual-run flag.

## Capabilities

### New Capability

- `avalonia-end-game`: the Phase-2 cutover — Avalonia as the sole default shell, net48 → .NET 10 retarget, single Avalonia application lifetime, cross-platform (Windows/macOS/Linux) build/packaging/CI, gated deletion of the WinForms shell/adapters/Gecko-Graphite-on-Avalonia/native-Views-in-default, retained native services kept behind non-UI seams, and the Phase-1-parity cutover precondition.

### Architecture Areas Covered

- `architecture/layers/entry-points`: single Avalonia application lifetime replacing the WinForms host; canonical startup preserved.
- `architecture/build-deploy/build-phases`: net48 → .NET 10 retarget of managed projects and the traversal build; native-first ordering preserved.
- `architecture/build-deploy/installer`: cross-platform packaging/installers and Avalonia runtime harvest; WinForms/Gecko retirement.
- `architecture/ui-framework/winforms-patterns`: WinForms shell, adapters, and dialogs removed from the default path under deletion gates.
- `architecture/ui-framework/xcore-mediator`: XCore-WinForms composition removed from the default path; command routing/state runs without WinForms adapters.
- `architecture/interop/native-boundary`: retained native data/services behind non-UI seams; no native viewing/rendering in the default app; registration-free COM preserved.
- `architecture/testing/test-strategy`: full-app smoke, accessibility, and performance gates run on all three OSes.
- `architecture/build-deploy/localization`: localization, accessibility, and keyboarding preserved through the cutover.

## Impact

- Managed shell/framework code: `Src/Common/FieldWorks/`, `Src/Common/Framework/`, `Src/XCore/`, `Src/xWorks/`, and main FLEx screens under `Src/LexText/` — retargeted to .NET 10 and stripped of the WinForms default path.
- Avalonia code: `Src/Common/FwAvalonia/`, `Src/Common/FwAvaloniaPreviewHost/`, and the Avalonia shell projects become the sole default UI.
- Build/deploy: `build.ps1`, `FieldWorks.proj`, package/analyzer references, installer/runtime packaging, and CI across Windows/macOS/Linux.
- Native/interop: native `ITsString`/writing-system kernel, ICU (`icu.net`), EncConverters, XAmple, spelling, and parser remain behind non-UI service seams via managed interop; native Views/Graphite/Gecko rendering is removed from the default path; registration-free COM is preserved.
- Deletion: WinForms shell + main-window services, WinForms-only default dialogs, `FlexUIAdapter` default, Gecko/Graphite-on-Avalonia, and native-Views-in-default are removed under per-target gates (the deletions Phase 1 forbade).
- Precondition: gated on the Phase-1 functional parity burn-down (`lexical-edit-avalonia-migration` §19) reaching zero open functional regressions.
