## Context

`lexical-edit-avalonia-migration` (Phase 1) rebuilt the functional UI of FieldWorks in Avalonia while keeping WinForms switchable, hosting everything through the WinForms shell, and deleting nothing. The functional risk — every editor, field type, dialog, browse interaction, undo/redo, clipboard/drag-drop, command execution, and the WinForms-density visual system — is retired and tester-validated; Graphite shaping is the only intentional drop. The remaining functional gaps are tracked and burned down in Phase-1 §19.

Phase 2 (`avalonia-end-game`) is the cutover. It absorbs and supersedes `fieldworks-avalonia-shell-migration`, consuming its already-specified Avalonia application/window lifetime, command bridge, screen registry, dialog/service abstractions, and decommissioning plan, and the frozen seam capabilities (`avalonia-command-focus`, `avalonia-ui-scheduler`, `avalonia-lifetime`, `avalonia-edit-sessions`, `avalonia-undo-redo`, `avalonia-validation`) from Phase 1. The shell change's section numbers are referenced where this plan absorbs them.

The native-code reality the spec must be precise about: FieldWorks rendering is now fully managed (Avalonia/Skia/HarfBuzz; Views/Graphite/Uniscribe are replaced and isolation-tested). The retained native `ITsString`/writing-system kernel is the **data model** consumed via managed interop, not a renderer, and is app-wide. ICU is reached via managed `icu.net`. EncConverters, XAmple, spelling, and parser are non-UI services. Therefore "kill WinForms / go net10 / cross-platform" does **not** require removing the native data kernel.

## Goals / Non-Goals

**Goals:**

- Avalonia is the sole default application shell, lifetime, windowing system, and main-screen host; no WinForms default path remains.
- The managed FieldWorks application retargets from .NET Framework 4.8 to .NET 10 and builds/runs on Windows, macOS, and Linux through the repo scripts.
- The WinForms shell/adapters/default dialogs, Gecko/Graphite-on-Avalonia, and native-Views-in-default are deleted under explicit per-target gates.
- Retained native data/services stay behind non-UI seams via managed interop and outside the Avalonia render/editor path.
- Localization, accessibility, and keyboarding are preserved post-cutover.

**Non-Goals:**

- No LCModel/data-schema rewrite; no removal of the native `ITsString`/writing-system data kernel.
- No new functional behavior; functional parity is owned by Phase 1.
- No permanent runtime WinForms/Avalonia fallback switch in the final app.

## Decisions

### 1. Dual lifetime collapses to a single Avalonia lifetime

Phase 1 ran WinForms as the application lifetime and hosted Avalonia content inside it (`WinFormsAvaloniaControlHost`). Phase 2 inverts this permanently: the app starts on the Avalonia classic desktop lifetime with explicit top-level window ownership and active-window tracking. The framework-neutral lifetime/dialog/dispatcher contracts specified by the shell change (its §2) become Avalonia-only implementations; the WinForms compatibility adapters for those contracts are deleted. Canonical startup obligations (diagnostics, project selection/open, LCModel cache init, service registration, safe shutdown, remote-request listener, no-UI/app-server modes) are preserved on the Avalonia path. (Absorbs shell §2, §5.)

### 2. net48 → .NET 10 retarget strategy

The managed FieldWorks projects move from `net48` to `net10` (SDK-style). Strategy:

- **Retarget the managed app graph** (`Src/Common/FieldWorks`, `Src/Common/Framework`, `Src/XCore`, `Src/xWorks`, FLEx screens, and their dependencies) to `net10`. Phase-1 Avalonia projects already target modern .NET; this brings the rest of the default graph forward to a single TFM.
- **Build** (`build.ps1`, `FieldWorks.proj`): keep native-C++-before-managed ordering (a non-negotiable repo constraint) and traversal structure; update SDKs, package references, and analyzers for net10. Build remains driven by the repo scripts — no separate build lane.
- **Stays (behind service seams, via managed interop):** the native C++ `ITsString`/writing-system data kernel, ICU (`icu.net`), EncConverters, XAmple, spelling, and parser. Registration-free COM is preserved — no global COM registration, no registry hacks.
- **Removed:** net48-only and WinForms-only managed projects/code paths in the default graph (see Decision 4), plus net48-pinned packages/analyzers superseded by net10 equivalents.
- **Test discovery** moves to the net10 runner for the retargeted projects while the build/test scripts stay the entry points.

### 3. True cross-platform (Windows / macOS / Linux)

The default app targets all three desktop OSes. The native data kernel and non-UI services are consumed via managed interop on each platform; ICU rides `icu.net`. Packaging produces per-OS artifacts (Windows installer, macOS bundle, Linux package) harvesting the Avalonia runtime and required native dependencies per platform. CI builds, runs, and smoke-tests on all three. Platform-specific concerns (keyboard/IME services, file/font pickers — already Avalonia-managed in Phase 1, fonts, path/casing) are validated per OS rather than assumed.

### 4. Deletion gates (per target)

Each WinForms-era target is deleted only when its replacement is the sole default path and proven. Gates:

- **WinForms shell + main-window services:** deletable once the Avalonia single-lifetime shell is default and full-app smoke passes on all three OSes with no default-path dependency on the WinForms shell (startup audit fails on any hidden WinForms shell creation).
- **`FlexUIAdapter` default behavior:** deletable once command routing/state and screen composition run through the Avalonia path with no default-path adapter dependency.
- **WinForms-only default dialogs:** deletable per dialog once its Avalonia equivalent (built/hosted in Phase 1) is the sole default and parity evidence exists.
- **Gecko / Graphite-on-Avalonia:** deletable once the default app has no runtime call path through Gecko/XULRunner or native Graphite shaping; browser/PDF behavior is either replaced by the chosen non-Graphite strategy or moved outside the default boundary (per Phase-1 5.6–5.8).
- **Native Views rendering in default path:** deletable once dependency audit proves no default-path call path through native display/layout/measurement/hit-testing/selection/editor-realization. The native `ITsString`/WS **data kernel** is explicitly **not** under this gate — it is the retained data model, not a renderer.

Each deletion is reversible only by revert (it is a cutover, not a flag), so each gate requires green smoke + audit evidence before the deletion lands.

### 5. Phase-1-proven precondition

The cutover does not start removing WinForms until the Phase-1 functional parity burn-down (`lexical-edit-avalonia-migration` §19, including the §19h tester burn-down to zero) shows no open functional regression. This is the load-bearing gate: because functional behavior was proven in Phase 1, Phase-2 validation is lifecycle/retarget/packaging/deletion correctness, not feature correctness.

### 6. Rollback posture

This is a cutover, not a dual-run feature flag. There is no permanent runtime WinForms fallback in the final app. The safety mechanism is the **gate**, not a switch: the Phase-1 burn-down must be zero before deletions begin, and each per-target deletion (Decision 4) lands only behind green smoke + dependency-audit evidence on all three OSes. Rollback of a bad step is by source revert of that step, not by toggling WinForms back on at runtime.

### 7. Consume frozen seams and the shell spec rather than redefining them

`avalonia-end-game` does not reopen the seam decisions or re-specify shell composition. The shell-global phases of `avalonia-command-focus`, `avalonia-ui-scheduler`, and `avalonia-lifetime` and the shell change's composition/registry/dialog-service specifications are consumed as-is. Where this change references shell behavior it cites the absorbed shell section numbers (§2–§10) rather than restating them.

## Risks / Trade-offs

- **net10 retarget surfaces hidden net48/WinForms coupling** → retarget the managed graph incrementally to a single TFM and let the build fail loudly on residual coupling; deletions (Decision 4) gate on audits, not assumptions.
- **Cross-platform native interop gaps (kernel, ICU, keyboard/IME) on macOS/Linux** → validate the retained native data kernel and `icu.net` per OS in CI before claiming cross-platform; treat per-OS keyboard/IME as an explicit bring-up item.
- **Packaging/installer divergence across three OSes** → per-OS harvest with a shared runtime list; CI builds the installer artifact on each OS.
- **Deleting too early breaks a not-yet-replaced path** → per-target gates require the Avalonia replacement to be sole-default and smoke-green before deletion.
- **Phase-1 burn-down slips** → the precondition (Decision 5) blocks the cutover; Phase 2 work that does not delete WinForms (retarget prep, cross-platform bring-up scaffolding) can proceed, but no WinForms removal lands until burn-down is zero.

## Migration Plan

1. Confirm the Phase-1 functional parity burn-down (`lexical-edit-avalonia-migration` §19) is at zero open functional regressions; otherwise track the blockers explicitly.
2. Retarget the managed app graph net48 → .NET 10 (projects, build, packages, analyzers, test discovery), keeping native-first build ordering and registration-free COM.
3. Stand up the single Avalonia application lifetime + windowing as the default (absorb shell §2–§5).
4. Compose the shell — navigation/menus/toolbars/status/screen-registry — on the Avalonia path (absorb shell §3, §6, §7).
5. Route commands and state without XCore-WinForms adapters (absorb shell §4).
6. Host the global dialogs/services (built in Phase 1) as Avalonia shell services (absorb shell §8).
7. Migrate any remaining main-screen composition area by area (absorb shell §9).
8. Bring up macOS and Linux: build, run, package, CI on all three OSes (extends shell §10).
9. Delete the WinForms shell, adapters, default dialogs, Gecko/Graphite-on-Avalonia, and native-Views-in-default under per-target gates (the deletions Phase 1 forbade; absorb shell §10.7).
10. Final validation: full-app smoke on all three OSes, UIA/accessibility, performance budgets, installer harvest.

## Open Questions

1. Exact .NET 10 SLA/support window alignment with the FieldWorks release cadence and the installer's bundled-runtime vs framework-dependent choice per OS.
2. macOS/Linux packaging format decisions (e.g. notarized `.app`/`.dmg`; `.deb`/`.rpm`/AppImage/Flatpak) and code-signing per OS.
3. The non-Graphite browser/PDF default strategy decision (recommended-not-decided in Phase-1 `gecko-pdf-audit.md`) must be finalized before the Gecko deletion gate.
4. Per-OS keyboard/IME service coverage parity (Keyman/system IME) on macOS/Linux versus the Windows baseline.
5. Whether any retained native service requires a per-OS build of its interop layer, and how that is harvested per platform.
