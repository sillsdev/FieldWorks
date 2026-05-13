## 1. Prerequisites and Inventory

- [ ] 1.1 Confirm Lexical Edit Avalonia migration gates are complete or explicitly tracked as blockers.
- [ ] 1.2 Inventory shell entry points in `Src/Common/FieldWorks/`, `Src/Common/Framework/`, `Src/XCore/`, and `Src/xWorks/`.
- [ ] 1.3 Inventory `Main.xml`, area/tool XML, command IDs, menus, toolbars, sidebars, status panes, listeners, defaults, includes, and localization metadata.
- [ ] 1.4 Inventory remaining default-path WinForms, RootSite/native Views, XMLViews, Gecko, Graphite, browser/PDF, and dialog dependencies by main screen.
- [ ] 1.5 Define hard gates for a completely Avalonia default app: no default WinForms shell, no default native viewing/rendering, no Graphite, no Gecko Graphite, passing accessibility/localization/performance/smoke evidence.

## 2. Shell Contracts

- [ ] 2.1 Extract framework-neutral managed interfaces for app lifetime, main window, active-window registry, dialog owner, modal state, UI dispatcher, shutdown, progress, settings, and status services.
- [ ] 2.2 Add compatibility adapters for current WinForms `FwApp`, `FieldWorksManager`, xWorks window, and dialog-owner behavior.
- [ ] 2.3 Remove direct `Form`/`Control` requirements from new shell-facing contracts before Avalonia shell construction begins.
- [ ] 2.4 Add contract tests for startup, active-window tracking, dialog ownership, shutdown, and UI dispatch behavior.

## 3. Typed Shell Composition

- [ ] 3.1 Build typed shell-definition importer for `Main.xml`, area/tool XML, includes, extension hooks, resources, listeners, and default properties.
- [ ] 3.2 Represent commands, lists, areas/tools, menus, context menus, toolbars, status panes, shortcuts, icons, localization metadata, and screen registrations.
- [ ] 3.3 Add diagnostics for unsupported commands, listeners, dynamic loaders, toolbar widgets, status panels, and extension constructs.
- [ ] 3.4 Add deterministic shell-definition snapshot tests.

## 4. Command Routing and State

- [ ] 4.1 Define typed command descriptors with stable IDs, labels, gestures, icons, visibility, enabled state, target resolution, and diagnostics.
- [ ] 4.2 Bridge XCore mediator handlers and property-table state into Avalonia commands.
- [ ] 4.3 Add tests for command enable/visible state, shortcuts, one-at-a-time commands, command target selection, and mediator bridge behavior.
- [ ] 4.4 Add menu/context-menu automation metadata and localization checks.

## 5. Avalonia Shell Skeleton

- [ ] 5.1 Create Avalonia shell project and integrate it into `FieldWorks.sln` and `FieldWorks.proj` traversal only after runtime strategy is approved.
- [ ] 5.2 Implement main window, app lifetime, navigation regions, content host, status/progress region, diagnostics hooks, theme resources, and accessibility root metadata.
- [ ] 5.3 Run the shell in preview/sample mode before LCModel project startup.
- [ ] 5.4 Add Avalonia.Headless tests for shell creation, navigation host swapping, command dispatch, status updates, dialog ownership, focus traversal, and pane state.

## 6. Navigation and Screen Registry

- [ ] 6.1 Map area/tool IDs from typed shell definition to an Avalonia screen registry.
- [ ] 6.2 Implement area/tool navigation and persisted `areaChoice`/`currentContentControl` compatibility.
- [ ] 6.3 Add screen manifests for each migrated main screen, including commands, state, native-boundary status, accessibility, performance, rollback, and default-switch gates.
- [ ] 6.4 Add memory-project and sample-project navigation tests.

## 7. Menus, Toolbars, Status, and Layout

- [ ] 7.1 Render menu and context-menu structures with labels, shortcuts, icons, separators, extension items, visibility, and enablement.
- [ ] 7.2 Render standard/format/insert/view toolbars, including writing-system and style selectors.
- [ ] 7.3 Render status panels for message, progress, area, sort, filter, parsing, and record number.
- [ ] 7.4 Implement split panes, side panes, record-list region, content panes, collapse/restore behavior, and layout persistence.
- [ ] 7.5 Evaluate a docking library only if owned Avalonia controls cannot meet documented FieldWorks workflows.

## 8. Dialogs and Global Services

- [ ] 8.1 Introduce dialog service for project, writing-system, settings, import/export, find/replace, styles, help, feedback, and utility dialogs.
- [ ] 8.2 Migrate high-frequency dialogs first and retain explicit legacy adapters only while blocked.
- [ ] 8.3 Add owner/modal, cancellation, focus return, accessibility, and localization tests for migrated dialogs.
- [ ] 8.4 Isolate browser/PDF/print behind replaceable services and select a non-Graphite default strategy.

## 9. Main Screen Migration

- [ ] 9.1 Migrate Lexicon screens after Lexical Edit gates.
- [ ] 9.2 Migrate Words/Interlinear screens.
- [ ] 9.3 Migrate Grammar/Morphology screens.
- [ ] 9.4 Migrate Notebook screens.
- [ ] 9.5 Migrate Lists screens.
- [ ] 9.6 Migrate dictionary preview/export, print, browser/PDF-dependent workflows, or isolate them outside the default path until replaced.

## 10. Startup, Shutdown, Installer, and Default Switch

- [ ] 10.1 Add Avalonia app startup path with project selection, cache creation, splash/safe-mode behavior, remote request listener, no-UI/app-server modes, and update checks accounted for.
- [ ] 10.2 Add shutdown/disposal tests for windows, caches, dialogs, background services, and retained native services.
- [ ] 10.3 Update installer/runtime packaging and dependency harvest for Avalonia shell assets.
- [ ] 10.4 Add feature flag/default selector for Avalonia shell.
- [ ] 10.5 Run full local build/test and app smoke gates before default switch.
- [ ] 10.6 Make Avalonia shell default only after hard gates pass.
- [ ] 10.7 Remove WinForms shell default path, FlexUIAdapter default dependency, WinForms dynamic content host, retired dialogs, and obsolete shell XML runtime pieces.