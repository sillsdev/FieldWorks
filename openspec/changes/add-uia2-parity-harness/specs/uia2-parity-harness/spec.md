## ADDED Requirements

### Requirement: Parity smoke scenarios are backend-neutral

The harness SHALL define shared smoke scenarios independently from WinForms, UIA2, Appium, Avalonia, and concrete control classes. Each scenario SHALL have a stable scenario id, scope classification, fixture description, ordered user-observable actions, expected normalized observations, backend eligibility, and artifact policy.

Affected paths SHALL include shared managed test infrastructure under a focused harness project or existing shared test-support location, plus scenario definitions near the owning tests for `Src/Common/Controls/DetailControls/`, `Src/Common/Controls/XMLViews/`, `Src/xWorks/`, and `Src/LexText/AdvancedEntry.Avalonia/`.

#### Scenario: Same scenario targets multiple backends

- **WHEN** a smoke scenario is registered for both a legacy WinForms surface and a migrated Avalonia surface
- **THEN** the harness SHALL execute the same scenario definition against each eligible backend without duplicating assertions in backend-specific test code

#### Scenario: Scenario actions remain user-observable

- **WHEN** a scenario focuses a field, invokes a launcher, opens a chooser, changes a filter, types text, or cancels a popup
- **THEN** the scenario SHALL express those steps as user-observable actions rather than direct access to private control fields or implementation-specific event handlers

### Requirement: Test scopes are explicit

The harness SHALL classify each scenario as unit/integration, slice/partial integration, or whole-app journey. Slice scenarios SHALL host the smallest meaningful screen or surface. Whole-app journey scenarios SHALL launch FieldWorks or an approved application host and navigate through startup or shell behavior.

#### Scenario: Avalonia slice can run truly headless

- **WHEN** a migrated Avalonia behavior does not require FieldWorks startup, shell navigation, native window ownership, or external dialog ownership
- **THEN** the scenario SHALL be eligible for the Avalonia.Headless slice harness

#### Scenario: Whole-app journey uses desktop automation

- **WHEN** a scenario requires app startup, project open, shell navigation, menu/toolbar routing, top-level dialog ownership, or real platform window behavior
- **THEN** the scenario SHALL be eligible only for the desktop automation harness

#### Scenario: Legacy WinForms slice is not treated as headless

- **WHEN** a focused partial-integration scenario targets a legacy WinForms surface
- **THEN** the scenario SHALL use a realized-window desktop automation backend or existing non-UI characterization tests, not Avalonia.Headless

### Requirement: Backend abstraction supports headless and desktop automation lanes

The harness SHALL provide a backend abstraction that can run a scenario through Avalonia.Headless slice automation, focused WinForms UIA2/FlaUI-style automation, Appium/WinAppDriver full-app automation, or future compatible automation backends. Scenario code MUST NOT reference FlaUI, `System.Windows.Automation`, Appium, WinForms controls, or Avalonia controls directly except through backend adapters.

#### Scenario: Avalonia backend exposes headless capabilities

- **WHEN** the Avalonia.Headless backend starts a scenario for a migrated control
- **THEN** it SHALL use Avalonia.Headless test infrastructure to drive input, focus, flyouts or context menus, validation state, accessibility metadata, layout realization, and disposal checks without launching the full FieldWorks application shell

#### Scenario: WinForms backend exposes UIA2 capabilities

- **WHEN** the WinForms UIA2 backend starts a scenario for a legacy launcher, chooser, or XMLViews table/filter surface
- **THEN** it SHALL realize a test window or owned control surface, expose it through UIA2-compatible automation, and report capabilities for focus, invoke, window/dialog observation, table/header discovery, and accessibility metadata where available

#### Scenario: Appium backend exposes full-app capabilities

- **WHEN** the Appium/WinAppDriver backend starts a whole-app journey scenario
- **THEN** it SHALL launch or attach to the approved application host, navigate through the required startup path, and report capabilities for window discovery, accessibility-id lookup, input, focus, dialogs, menus, and screenshots

### Requirement: Paired mode compares normalized observations

The harness SHALL support a paired mode in which one test run executes the same scenario against two eligible backends, then compares normalized observations. Normalized observations SHALL include stable node id, accessible id/name, role or editor kind, enabled and visible state, focus owner, available actions, selected value or displayed text where applicable, table header order, filter affordances, popup/dialog state, and diagnostics.

#### Scenario: Matching observations pass paired parity

- **WHEN** both backends complete the same scenario and emit equivalent normalized observations
- **THEN** the paired parity result SHALL pass even if raw WinForms/UIA2, Appium, and Avalonia control tree details differ

#### Scenario: Differences are classified

- **WHEN** paired observations differ
- **THEN** the harness SHALL classify the difference as missing node, wrong order, missing action, focus mismatch, accessibility mismatch, value mismatch, unsupported legacy-only behavior, unsupported Avalonia behavior, or backend infrastructure failure

### Requirement: Execution modes are explicit and CI-safe

The harness SHALL provide explicit execution modes for `HeadlessOnly`, `DesktopOnly`, and `Paired`. CI and agent entry points SHALL select modes deliberately and SHALL NOT accidentally require interactive desktop automation when running the stable headless subset. Backend filters MAY further select `AvaloniaHeadless`, `WinFormsUia2`, `AppiumFullApp`, or later adapters.

#### Scenario: Normal CI runs headless-capable smoke scenarios

- **WHEN** normal CI or `./test.ps1` runs on an environment without desktop automation support
- **THEN** headless-capable smoke scenarios SHALL run and desktop-required scenarios SHALL report a clear skipped or not-runnable reason instead of failing as infrastructure errors

#### Scenario: Agent requests paired parity evidence

- **WHEN** an agent or developer invokes the paired parity-smoke entry point on a Windows desktop environment with the required desktop automation support
- **THEN** the harness SHALL run both eligible backends for eligible scenarios and write paired comparison artifacts

#### Scenario: Desktop mode is isolated from the fast lane

- **WHEN** the desktop automation harness requires Appium, WinAppDriver, UIA2, or an interactive desktop session
- **THEN** those prerequisites SHALL be checked before scenario actions run and SHALL NOT be required by the default headless-only fast lane

### Requirement: Desktop automation windows are realized without relying on visible user interaction

The desktop automation harness SHALL create, launch, or attach to realized HWND-backed windows or application windows that Windows automation can inspect. Tests MAY position windows offscreen only when the backend proves the automation tree can still discover, focus, and invoke the relevant elements; otherwise the backend SHALL use deterministic on-screen placement in the automation desktop.

#### Scenario: Offscreen placement is accepted only when automation works

- **WHEN** a desktop automation scenario runs with offscreen placement enabled
- **THEN** the backend SHALL verify the root automation element and required child controls are discoverable before executing scenario actions

#### Scenario: Desktop automation environment is unavailable

- **WHEN** Windows automation cannot access the realized test surface, interactive desktop, automation server, or required automation patterns
- **THEN** the backend SHALL stop the scenario before user actions and report an environment capability result with diagnostics

### Requirement: Failure artifacts are actionable

The harness SHALL write failure artifacts that identify the scenario id, scope, backend, execution mode, fixture, action step, normalized observations, backend capability report, raw UIA/Appium/Avalonia tree where available, screenshot where available, and comparison differences.

#### Scenario: Paired comparison fails

- **WHEN** a paired scenario fails because two backends emit different observations
- **THEN** the artifact bundle SHALL include both backend observation files and a comparison summary that names the first differing stable node or action

#### Scenario: Backend infrastructure fails

- **WHEN** a backend cannot start, find a control, invoke an action, or capture observations
- **THEN** the artifact bundle SHALL distinguish infrastructure failure from product behavior mismatch

### Requirement: Repo integration follows FieldWorks build and test rules

The harness SHALL be integrated through `FieldWorks.proj`, `FieldWorks.sln`, and repo test scripts instead of ad hoc `dotnet build` or raw MSBuild commands. Managed harness projects SHALL respect .NET Framework 4.8/C# 7.3 constraints when they reference legacy WinForms projects, and Avalonia harness projects SHALL use the existing net8/Avalonia test conventions.

#### Scenario: Build and solution integration is present

- **WHEN** the harness adds a project, test assembly, script, or shared test-support library
- **THEN** it SHALL be reachable from the repository traversal build/test flow and from the solution used by developers

#### Scenario: Area instructions are followed

- **WHEN** implementation touches `Src/Common/Controls`, `Src/xWorks`, `Src/LexText/AdvancedEntry.Avalonia`, `Build/Agent`, or `scripts/Agent`
- **THEN** the change SHALL follow the root `AGENTS.md`, `openspec/AGENTS.md`, and the applicable area-specific instruction files before review

### Requirement: Initial scenarios cover launcher, table/filter, and first full-app journey

The first harness implementation SHALL include executable scenarios for morph-type launcher or chooser reachability and XMLViews table header/filter reachability, then add Avalonia counterparts as migrated controls become available. The first desktop automation implementation SHALL also define one pilot whole-app journey candidate before it becomes a gating test.

#### Scenario: Legacy launcher scenario is represented

- **WHEN** the initial WinForms UIA2 backend is available
- **THEN** a legacy launcher scenario SHALL focus the launcher field, invoke the launcher button, observe the chooser or chooser-decision surface, and cancel or close it deterministically

#### Scenario: Table filter scenario is represented

- **WHEN** the initial WinForms UIA2 backend is available for XMLViews browse/table surfaces
- **THEN** a legacy table/filter scenario SHALL observe stable header order and filter affordances such as show-all, filter-for, or choose actions

#### Scenario: Avalonia counterpart is added incrementally

- **WHEN** a migrated Avalonia control implements the corresponding launcher, chooser, table, or filter behavior
- **THEN** the same scenario SHALL become eligible for Avalonia.Headless and paired execution before the migrated surface is considered parity-covered

#### Scenario: Whole-app pilot is explicit

- **WHEN** the Appium/WinAppDriver backend is introduced
- **THEN** the change SHALL choose and document one pilot whole-app journey, such as app startup to Lexical Edit, project open to AdvancedEntry preview, or a narrower full-app launcher/chooser path