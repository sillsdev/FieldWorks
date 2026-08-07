# Tasks

> Phase 2 (cutover). This change absorbs `fieldworks-avalonia-shell-migration`; absorbed shell task
> sections are cited as "shell §N". No new functional behavior lands here — functional parity is owned
> by Phase 1 (`lexical-edit-avalonia-migration`). These tasks are lifecycle, retarget, packaging, and
> gated deletion only.

## 0. Preconditions and Phase-1 parity gate

- [ ] 0.1 Confirm the Phase-1 functional parity burn-down (`lexical-edit-avalonia-migration` §19, incl. §19h tester burn-down) shows zero open functional regressions; otherwise track each blocker explicitly and hold WinForms-removal tasks (§8) until clear.
- [ ] 0.2 Inventory the default-path WinForms shell, main-window services, `FlexUIAdapter` default behavior, WinForms-only default dialogs, Gecko/Graphite-on-Avalonia, and native-Views-in-default targets to be removed, each with its replacement and deletion gate (Decision 4 of design.md).
- [ ] 0.3 Inventory net48-only / WinForms-only managed projects, packages, and analyzers in the default graph that the net10 retarget removes or replaces.
- [ ] 0.4 Confirm the retained native services (ITsString/WS data kernel, ICU/`icu.net`, EncConverters, XAmple, spelling, parser) are reachable only behind non-UI seams and record them as out-of-scope for render/editor-path removal.

## 1. net48 → .NET 10 retarget

- [ ] 1.1 Retarget the managed app graph (`Src/Common/FieldWorks`, `Src/Common/Framework`, `Src/XCore`, `Src/xWorks`, FLEx screens, and dependencies) from `net48` to `net10`.
- [ ] 1.2 Update `build.ps1` / `FieldWorks.proj` for net10 while preserving native-C++-before-managed ordering and the traversal structure; keep the repo scripts as the only build/test entry points.
- [ ] 1.3 Update package references and analyzers for net10; remove net48-pinned packages superseded by net10 equivalents.
- [ ] 1.4 Move test discovery to the net10 runner for retargeted projects; keep `test.ps1` as the entry point.
- [ ] 1.5 Preserve registration-free COM and the managed-interop boundary to the retained native data kernel and services; no global COM registration, no registry hacks.
- [ ] 1.6 Build the retargeted default graph green via `build.ps1` and let the build fail loudly on residual net48/WinForms coupling.

## 2. Single Avalonia application lifetime and windowing (absorb shell §2, §5)

- [ ] 2.1 Make Avalonia classic desktop lifetime the default application lifetime with explicit top-level window ownership and active-window tracking; collapse the Phase-1 dual lifetime.
- [ ] 2.2 Provide Avalonia-only implementations of the framework-neutral lifetime/dialog-owner/dispatcher/shutdown contracts (shell §2); retire the WinForms compatibility adapters for those contracts.
- [ ] 2.3 Preserve canonical startup on the Avalonia path: diagnostics, project selection/open, LCModel cache init, service registration, splash/safe-mode, remote-request listener, no-UI/app-server modes, update checks, safe shutdown.
- [ ] 2.4 Add Avalonia.Headless tests for app lifetime, active-window tracking, dialog ownership, UI dispatch, and deterministic shutdown/disposal.

## 3. Shell composition: navigation, menus, toolbars, status, screen registry (absorb shell §3, §6, §7)

- [ ] 3.1 Compose navigation, content host, side/record panes, menus, context menus, toolbars, and status/progress regions on the Avalonia path from the typed shell definition (shell §3, §7).
- [ ] 3.2 Drive area/tool navigation through the Avalonia screen registry with persisted `areaChoice`/`currentContentControl` compatibility (shell §6).
- [ ] 3.3 Add Avalonia.Headless tests for navigation host swapping, menu/toolbar/status rendering, pane state, and layout persistence.

## 4. Command routing and state without XCore-WinForms (absorb shell §4)

- [ ] 4.1 Route shell commands and state through the Avalonia command path (typed descriptors, shell-global `avalonia-command-focus`) with no default-path `FlexUIAdapter`/WinForms menu/toolstrip adapter.
- [ ] 4.2 Run the XCore mediator/property-table only as a non-WinForms compatibility bridge where still needed; it SHALL NOT own default UI composition.
- [ ] 4.3 Add tests for command enable/visible/checked state, shortcuts, one-at-a-time commands, and target resolution without WinForms adapters.

## 5. Global dialogs and services as Avalonia shell services (absorb shell §8)

- [ ] 5.1 Host the global dialogs/services as Avalonia shell services — the *functional* dialogs were built and validated in Phase 1; here they are composed/owned by the Avalonia shell (project, writing-system, settings, import/export, find/replace, styles, help, feedback, progress, keyboarding, clipboard, print).
- [ ] 5.2 Provide the non-Graphite browser/PDF default strategy or keep affected paths outside the default boundary (per Phase-1 5.6–5.8); finalize the recommended-not-decided decision before the Gecko deletion gate (§8.4).
- [ ] 5.3 Add owner/modal, focus-return, cancellation, accessibility, and localization tests for the shell-hosted dialogs/services.

## 6. Main-screen composition area by area (absorb shell §9)

- [ ] 6.1 Compose the remaining main screens (Lexicon, Words/Interlinear, Grammar/Morphology, Notebook, Lists) through the Avalonia screen registry with no default-path WinForms dynamic content host.
- [ ] 6.2 Verify each area's screen manifest evidence (commands, navigation, accessibility, localization, native-boundary status, performance) on the Avalonia path.

## 7. Cross-platform: macOS and Linux bring-up, packaging, CI

- [ ] 7.1 Bring up build + run on macOS and on Linux; validate the retained native data kernel and `icu.net` interop per OS.
- [ ] 7.2 Validate per-OS keyboard/IME, file/font pickers (Avalonia-managed), fonts, and path/casing behavior.
- [ ] 7.3 Produce per-OS packaging artifacts (Windows installer, macOS bundle, Linux package) harvesting the Avalonia runtime and required native dependencies for each OS.
- [ ] 7.4 Add CI lanes that build and smoke-test the app on Windows, macOS, and Linux.

## 8. Decommission / DELETE WinForms default path (the deletions Phase 1 forbade) (absorb shell §10.7)

> Each deletion lands only behind its per-target gate (Decision 4 of design.md): the Avalonia
> replacement is sole-default and full-app smoke + dependency-audit evidence is green on all three OSes.
> Blocked until §0.1 (Phase-1 burn-down to zero) is satisfied.

- [ ] 8.1 Delete the WinForms shell and WinForms main-window services from the default path (gate: Avalonia single-lifetime shell default + smoke green; startup audit fails on hidden WinForms shell).
- [ ] 8.2 Delete the `FlexUIAdapter` default behavior (gate: command routing/state + screen composition run on the Avalonia path with no default-path adapter dependency).
- [ ] 8.3 Delete WinForms-only default dialogs per dialog (gate: Avalonia equivalent is sole default + parity evidence).
- [ ] 8.4 Delete Gecko/XULRunner and Graphite-on-Avalonia assumptions from the default path (gate: no default-path Gecko/native-Graphite call path; browser/PDF replaced or moved outside default per §5.2).
- [ ] 8.5 Delete native Views rendering from the default path (gate: dependency audit proves no default-path native display/layout/measurement/hit-testing/selection/editor-realization). The native `ITsString`/WS data kernel is explicitly NOT deleted — it is the retained data model.
- [ ] 8.6 Remove obsolete net48/WinForms shell-XML runtime pieces and dead build wiring left by the deletions above.

## 9. Final validation

- [ ] 9.1 Full-app smoke on all three OSes: launch, open/create project, switch representative areas/tools, execute representative commands, show dialogs, close windows, shut down cleanly.
- [ ] 9.2 UIA/accessibility evidence on the cutover shell and screens.
- [ ] 9.3 Performance budget evidence against the recorded baselines for representative workflows.
- [ ] 9.4 Installer/packaging harvest validated per OS (runtime + native dependencies present and launchable).
- [ ] 9.5 Localization evidence: user-facing strings resolve from existing resources post-cutover.
- [ ] 9.6 Final default-app dependency audit green: no WinForms shell, `FlexUIAdapter` default, WinForms-only default dialogs, Gecko/native-Graphite, or native-Views-in-default remain; retained native data/services confirmed behind non-UI seams.
