## Context

FieldWorks currently starts as a WinForms/xWorks application. Startup, project selection, main-window construction, command routing, property-table state, menus, toolbars, sidebars, status panes, dialogs, and dynamic content hosting are tied to WinForms/XCore concepts. Lexical Edit Avalonia work creates a migrated region, but a fully Avalonia product requires a later shell/windowing migration that replaces the default application host and all main screens without preserving WinForms as the hidden runtime shell.

The architecture review identified several constraints:

- `FwApp`, `FieldWorksManager`, xWorks windows, and many dialogs assume `System.Windows.Forms.Form` ownership.
- XCore mediator/property-table behavior is both command bus and UI composition state.
- Shell composition is driven by XML configuration such as `Main.xml`, including commands, lists, menus, sidebars, toolbar items, listeners, defaults, extension includes, and localization metadata.
- Main screens outside Lexical Edit may still depend on WinForms controls, XMLViews, RootSite/native Views, Gecko, or native rendering.
- Avalonia brings different application lifetime, dispatcher, command, focus, validation, accessibility, styling, and window ownership patterns.
- Retained custom linguistics services can stay only as non-UI service dependencies.

## Goals / Non-Goals

**Goals:**

- Make Avalonia the default FieldWorks application lifetime, shell, windowing system, and main-screen host.
- Preserve command IDs, shortcuts, menus, navigation semantics, status/progress behavior, localization, extension hooks, project startup behavior, and multi-window behavior.
- Keep XCore/Mediator compatibility during migration while moving UI composition to typed shell definitions and Avalonia services.
- Host migrated screens through registered Avalonia views and explicit view models/presenters.
- Keep retained native/external services outside the Avalonia display/layout/input boundary.

**Non-Goals:**

- No LCModel rewrite or storage schema change.
- No requirement that every feature surface migrate in the first shell skeleton.
- No permanent WinForms or native UI island in the final default app.
- No Graphite or Gecko Graphite default-path compatibility layer.

## Decisions

### 1. Phase two depends on regional Avalonia readiness

The shell migration starts after Lexical Edit has proven the critical regional patterns: typed view definitions, parity automation, Graphite-free default path, edit sessions, and no native viewing/rendering inside migrated regions. The shell must not become a container for unresolved native Views or Graphite dependencies.

### 2. Application lifetime and windowing use framework-neutral ports first

Introduce interfaces for desktop lifetime, main window, active-window registry, dialog owner, UI dispatcher, shutdown, and modal state while WinForms remains default. Avalonia then implements those ports using classic desktop lifetime and explicit window ownership.

### 3. Shell XML becomes typed shell definition during transition

Existing XCore shell XML remains a migration input, not the final runtime composition model. A typed shell definition captures commands, menus, toolbars, sidebars, status panes, areas/tools, includes, listeners, shortcuts, icons, defaults, and localization metadata with diagnostics for unsupported constructs.

### 4. Command routing is bridged before it is replaced

XCore mediator/property-table remains a compatibility command and state bridge during migration. Avalonia commands expose stable IDs, labels, gestures, icons, visibility, enabled state, target resolution, and diagnostics independent of WinForms menu/toolstrip adapters.

### 5. Main screens migrate by registry and manifest

An Avalonia screen registry maps area/tool IDs to presenters and views. Each migrated screen has a manifest covering entry points, shell commands, state, accessibility, performance, legacy adapters, native boundary status, and rollback/default-switch behavior.

### 6. Shell services are testable outside the full app

Navigation, commands, dialog ownership, status/progress, settings persistence, accessibility metadata, and shell composition are tested through pure services, typed snapshots, and Avalonia.Headless before full-app smoke tests.

### 7. The shell phase consumes earlier seam capabilities instead of redefining them

The shell phase consumes the previously chosen seam capabilities from `lexical-edit-avalonia-migration` rather than reopening those decisions by default. In particular, `avalonia-command-focus` is promoted from screen-local usage to shell-global command and target routing here, while `avalonia-ui-scheduler` and `avalonia-lifetime` are promoted from local editor seams to application-wide services. If those choices later prove wrong, the pivot triggers in `lexical-edit-avalonia-migration/seam-recommendations.md` govern when to change direction.

## Risks / Trade-offs

- Runtime split between .NET Framework managed code and newer Avalonia projects -> Resolve host/runtime strategy early and avoid hidden cross-runtime assumptions.
- XCore extension behavior may depend on WinForms adapters -> Preserve command IDs and add typed diagnostics for unsupported UI constructs.
- WinForms dialog ownership is widespread -> Introduce dialog-owner contracts and migrate high-frequency dialogs before default switch.
- Main screens outside Lexical Edit may still use native Views or Gecko -> Keep explicit legacy boundaries and block default-path completion until each screen manifest passes.
- UI automation can become flaky -> Push deep behavior into service/snapshot tests and reserve UI automation for shell smoke, accessibility, and platform behavior.
- Browser/PDF/print scope may exceed shell work -> Treat browser/PDF as replaceable global services with their own decision gates.

## Migration Plan

1. Confirm Lexical Edit regional gates or track unresolved blockers explicitly.
2. Inventory current shell entry points, XML composition, command IDs, dialogs, main screens, startup/shutdown paths, native/WinForms dependencies, and browser/PDF paths.
3. Extract framework-neutral shell/lifetime/dialog/dispatcher/command/navigation/status/settings/accessibility ports while WinForms remains default, consuming `avalonia-ui-scheduler` and `avalonia-lifetime` rather than redefining them.
4. Build typed shell-definition importer and snapshot tests for `Main.xml` and area/tool includes.
5. Build an Avalonia shell preview path with sample data and migrated regions.
6. Bridge XCore commands and property state into typed Avalonia command/state services, consuming the shell-global phase of `avalonia-command-focus`.
7. Implement Avalonia navigation, content host, menus, context menus, toolbars, side panes, status/progress, and dialog service.
8. Migrate main screens area by area using screen manifests and legacy-host boundaries only for non-migrated screens.
9. Add startup/shutdown, installer/runtime, accessibility, localization, performance, and full-app smoke gates.
10. Make Avalonia shell default only after hard gates pass; then retire WinForms shell and default-path adapters.

## Open Questions

1. What runtime target is required for the final application host, and what bridge is needed for remaining .NET Framework projects?
2. Which shell XML extension points must remain supported for partner or add-on workflows?
3. Which docking/layout behavior requires owned controls versus a third-party Avalonia docking library?
4. Which browser/PDF engine replaces Gecko-backed preview, print, and PDF workflows?
5. Which main screens outside Lexical Edit block the first Avalonia shell default switch?