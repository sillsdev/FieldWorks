## ADDED Requirements

### Requirement: FieldWorks provides an Avalonia default shell

FieldWorks SHALL provide an Avalonia desktop shell as the final default application shell for migrated workflows.

#### Scenario: Avalonia shell owns default chrome
- **WHEN** FieldWorks runs in the final default mode
- **THEN** the top-level shell, navigation, content host, menus, toolbars, status panes, dialogs, and application chrome SHALL be Avalonia-owned
- **AND** WinForms shell controls SHALL NOT be created for the default path

### Requirement: Shell preserves FieldWorks workflow semantics

The Avalonia shell SHALL preserve project startup, area/tool navigation, command IDs, shortcuts, menu semantics, status/progress behavior, localization, accessibility, and multi-window behavior from the legacy shell.

#### Scenario: Project opens to equivalent workspace
- **WHEN** a user opens a project in the Avalonia shell
- **THEN** the shell SHALL initialize project context, area/tool selection, command state, status panes, and main content equivalent to the legacy baseline for covered workflows

### Requirement: Shell XML imports into typed shell definition

Existing shell configuration XML SHALL import into a typed shell definition during migration.

#### Scenario: Main XML imports deterministically
- **WHEN** `Language Explorer/Configuration/Main.xml` and area/tool includes are imported
- **THEN** the typed shell definition SHALL include commands, lists, areas, tools, menus, context menus, sidebars, toolbars, status panes, listeners, defaults, extension includes, shortcuts, icons, and localization metadata

### Requirement: Unsupported shell constructs are diagnostic

Unsupported shell XML constructs SHALL produce deterministic diagnostics instead of silent omission.

#### Scenario: Unsupported dynamic loader is reported
- **WHEN** shell import encounters a dynamic loader, listener, toolbar widget, or status panel that has no Avalonia equivalent
- **THEN** the importer SHALL report the XML path, command/tool identifier when available, migration severity, and required follow-up

### Requirement: Typed shell definition is the runtime target

The final Avalonia shell SHALL use typed shell definitions as its runtime composition contract, with XML retained only for migration/import/audit scenarios.

#### Scenario: Runtime composition avoids raw XML
- **WHEN** a shell area has passed migration gates
- **THEN** the Avalonia shell SHALL compose commands, navigation, menus, toolbars, panes, and status regions from the typed definition rather than parsing runtime XML

### Requirement: Windowing uses framework-neutral lifetime services

Application startup, shutdown, active-window tracking, modal ownership, and UI dispatch SHALL use framework-neutral interfaces during migration.

#### Scenario: WinForms and Avalonia lifetimes share contract
- **WHEN** shell lifetime behavior is tested
- **THEN** both WinForms compatibility and Avalonia implementations SHALL satisfy the same app lifetime, active-window, dialog-owner, dispatcher, and shutdown contracts

### Requirement: Avalonia implementation owns final desktop lifetime

The final default shell SHALL use Avalonia desktop lifetime and explicit top-level window ownership.

#### Scenario: Default startup creates Avalonia main window
- **WHEN** FieldWorks starts in final default mode
- **THEN** the app SHALL create Avalonia top-level windows through the shell lifetime service
- **AND** it SHALL NOT require WinForms `Application.Run` or `Form` ownership for default shell windows

### Requirement: Shutdown and disposal are deterministic

The shell SHALL deterministically dispose windows, dialogs, project services, cache-bound services, background tasks, and retained native service handles.

#### Scenario: Project shutdown releases shell resources
- **WHEN** a project window closes or the application exits
- **THEN** shell tests SHALL prove windows, dialogs, event subscriptions, cache-bound services, and background tasks are released or canceled

### Requirement: Commands are represented by typed descriptors

Shell commands SHALL expose stable IDs, labels, localized resources, gestures, icons, visibility, enabled state, checked state when applicable, execution targets, and diagnostics independent of WinForms menu/toolstrip adapters.

#### Scenario: Command descriptor drives menu item
- **WHEN** a typed command appears in an Avalonia menu or toolbar
- **THEN** its label, icon, shortcut, enabled state, visibility, automation metadata, and handler target SHALL come from the typed command model

### Requirement: XCore mediator bridges during migration

XCore mediator and property-table behavior MAY remain as compatibility command/state infrastructure during migration, but SHALL NOT own final Avalonia UI composition.

#### Scenario: Mediator handler executes through Avalonia command
- **WHEN** a migrated menu item invokes a legacy XCore command handler
- **THEN** the command SHALL route through an explicit mediator bridge
- **AND** the bridge SHALL expose diagnostics for target resolution and command state

### Requirement: Command parity is validated

The shell migration SHALL validate command availability, shortcuts, context menus, command enablement, one-at-a-time behavior, and target resolution against legacy baselines.

#### Scenario: Shortcut parity is verified
- **WHEN** a legacy shortcut is migrated to Avalonia
- **THEN** automated or semantic tests SHALL prove the shortcut reaches the same command target and state behavior for covered workflows

### Requirement: Main screens register through typed screen registry

Main screens SHALL register through a typed Avalonia screen registry keyed by stable area/tool identifiers.

#### Scenario: Area tool resolves to registered screen
- **WHEN** the user selects a migrated area/tool
- **THEN** the shell SHALL resolve the screen through the registry and create the registered presenter/view rather than a WinForms dynamic content host

### Requirement: Each migrated screen has a manifest

Each migrated main screen SHALL have a manifest describing commands, state, content host, shell services, legacy adapters, native-boundary status, accessibility IDs, performance budgets, rollback behavior, and default-switch gates.

#### Scenario: Screen completion requires manifest evidence
- **WHEN** a screen is proposed for Avalonia completion
- **THEN** its manifest SHALL identify passing evidence for command routing, navigation, accessibility, localization, native-boundary status, Graphite-free behavior when relevant, and performance budgets

### Requirement: Legacy content is explicit and temporary

Non-migrated screens MAY be hosted through explicit legacy boundaries during transition, but the final default app SHALL NOT permanently embed WinForms or native viewing UI.

#### Scenario: Legacy island is tracked
- **WHEN** a non-migrated screen is hosted inside the Avalonia shell during transition
- **THEN** the screen SHALL have a manifest identifying why it remains legacy, which commands it supports, and what gates remove the legacy host

### Requirement: Entry points support Avalonia host transition

FieldWorks entry points SHALL support a transition from WinForms application lifetime to Avalonia application lifetime without bypassing project startup, diagnostics, cache initialization, or command registration requirements.

#### Scenario: Avalonia host follows canonical startup
- **WHEN** the Avalonia shell startup path is enabled
- **THEN** startup SHALL still initialize diagnostics, project selection/opening, LCModel cache, service registration, command infrastructure, and safe shutdown hooks through documented entry points

### Requirement: Hidden WinForms startup is disallowed in final mode

Final default startup SHALL NOT secretly create WinForms shell windows or run WinForms application lifetime to host Avalonia content.

#### Scenario: Startup audit detects WinForms shell dependency
- **WHEN** default-startup validation runs
- **THEN** it SHALL fail if the default path creates the retired WinForms shell, dynamic content host, or WinForms-only main-window services

### Requirement: Final shell excludes native UI boundaries

The final Avalonia shell SHALL NOT use native Views, Graphite, Gecko Graphite rendering, or other native viewing/rendering/editor infrastructure for default UI composition.

#### Scenario: Native UI dependency fails default audit
- **WHEN** default-shell dependency validation runs
- **THEN** it SHALL fail if default shell, chrome, navigation, dialogs, or migrated screens instantiate native viewing/rendering/editor infrastructure

### Requirement: Retained native services stay outside UI composition

Native or external linguistics services SHALL remain outside Avalonia UI composition when exposed through explicit non-UI service contracts.

#### Scenario: Linguistics service is allowed
- **WHEN** the Avalonia shell or migrated screens invoke XAmple, spelling, parser tools, ICU, Encoding Converters, or similar services
- **THEN** those services SHALL remain outside Avalonia display, layout, hit testing, focus, selection, and editor realization responsibilities

### Requirement: Shell migration uses layered validation

The shell migration SHALL use shell-definition snapshots, command tests, WinForms UIA baselines, Avalonia.Headless tests, integration tests, semantic UI snapshots, full-app smoke tests, and dependency audits.

#### Scenario: Shell behavior is frozen before replacement
- **WHEN** a shell subsystem is replaced
- **THEN** the migration SHALL identify existing baseline evidence or add shell contract, semantic, UIA, or integration tests for that behavior

### Requirement: Full-app smoke gates protect default switch

Before Avalonia shell becomes default, full-app smoke tests SHALL launch the app, open or create a project, switch representative areas/tools, execute representative commands, show dialogs, close windows, and shut down cleanly.

#### Scenario: Default switch waits for smoke gates
- **WHEN** Avalonia shell is proposed as default
- **THEN** default-switch validation SHALL include full-app smoke evidence, accessibility evidence, localization evidence, performance evidence, and dependency audit evidence

### Requirement: Installer packages Avalonia shell runtime

Installer and packaging logic SHALL include the Avalonia shell runtime assets required by the default application host.

#### Scenario: Avalonia shell artifacts are harvested
- **WHEN** installer packaging runs for a build where Avalonia shell is default
- **THEN** required Avalonia assemblies, native dependencies, resources, configuration, and generated shell definitions SHALL be included

### Requirement: Shell localization survives typed migration

Typed shell definitions SHALL preserve localizable labels, tooltips, menu text, command text, status text, dialog text, shortcut descriptions, and resource identifiers from existing shell configuration and resources.

#### Scenario: Imported command keeps localization identity
- **WHEN** shell XML or resources define a localizable command label or tooltip
- **THEN** the typed shell definition SHALL retain localization identity so Crowdin/resource workflows can update the Avalonia shell text
