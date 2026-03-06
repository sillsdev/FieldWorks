## ADDED Requirements

### Requirement: FieldWorks UI automation uses a repo-conformant managed test project

The repository SHALL host desktop UI automation in an SDK-style managed test project that follows current FieldWorks project conventions.

**Affected Paths**
- `Src/Common/FieldWorks/FieldWorksUiAutomationTests/**`
- `Src/Common/FieldWorks/FieldWorks.csproj`
- `FieldWorks.sln`
- `Directory.Packages.props`
- `testui.ps1`

#### Scenario: Project structure matches current managed test conventions

- **WHEN** the UI automation project is added
- **THEN** it SHALL target `net48`
- **AND** it SHALL be an `IsTestProject` project executed through `testui.ps1`
- **AND** it SHALL use central PackageReference version management from `Directory.Packages.props`
- **AND** it SHALL build `FieldWorks.exe` into the shared `Output/<Configuration>/` layout before tests run

### Requirement: Desktop UI automation uses a dedicated runner script

FieldWorks SHALL run desktop UI automation through a dedicated top-level script so the suite remains operationally separate from standard managed tests.

**Affected Paths**
- `testui.ps1`
- `test.ps1`
- `Src/Common/FieldWorks/FieldWorksUiAutomationTests/**`

#### Scenario: UI automation does not share the standard test entrypoint

- **WHEN** a developer, CI job, or agent executes desktop UI automation
- **THEN** the supported entrypoint SHALL be `testui.ps1`
- **AND** `test.ps1` SHALL remain focused on non-UI managed/native test flows
- **AND** UI-specific setup such as sandbox orchestration, screenshot handling, and desktop-environment validation SHALL not be required for non-UI test runs

### Requirement: Local UI automation defaults to a warm Sandbox with mapped-folder communication

FieldWorks SHALL default local desktop UI automation to a warm Windows Sandbox that stays alive between test invocations, maps the host build output read-only, and communicates results through a mapped writable folder with file-based signaling.

**Affected Paths**
- `testui.ps1`
- `Src/Common/FieldWorks/FieldWorksUiAutomationTests/README.md`

#### Scenario: Default local run uses a warm Sandbox

- **WHEN** `testui.ps1` is run locally without an explicit execution-mode override
- **THEN** it SHALL launch or reuse a warm Windows Sandbox instance with prerequisites pre-provisioned
- **AND** it SHALL map the host's `Output/Debug/` folder read-only into the sandbox so the sandbox sees the latest build
- **AND** it SHALL map a writable results folder into the sandbox for TRX, screenshots, and status files
- **AND** only the main repo checkout SHALL be supported for sandbox mapping (not worktrees)

#### Scenario: Sandbox stays alive between test runs

- **WHEN** a developer re-runs `testui.ps1` after a previous sandbox-mode run
- **THEN** `testui.ps1` SHALL reuse the existing sandbox session if it is still running
- **AND** prerequisite installation SHALL be skipped because the sandbox is already provisioned
- **AND** the developer SHALL be able to explicitly tear down the sandbox when iteration is complete

#### Scenario: Results return to host through mapped-folder signaling

- **WHEN** the sandbox-side test runner completes a test run
- **THEN** it SHALL write TRX output and screenshots to the mapped writable results folder
- **AND** it SHALL write a structured status file (e.g. `_status.json`) with phase and pass/fail counts during the run
- **AND** it SHALL write a sentinel file (e.g. `_done`) upon completion
- **AND** the host-side `testui.ps1` SHALL poll the status file for progress and treat the sentinel as the completion signal

#### Scenario: Direct desktop mode remains available for debugging

- **WHEN** a developer opts out of Sandbox mode for troubleshooting
- **THEN** `testui.ps1` SHALL support an explicit direct-desktop execution mode
- **AND** that mode SHALL preserve the same test assembly, categories, and artifact conventions as the default Sandbox path

### Requirement: Launched FieldWorks instances use isolated project and settings roots

Desktop UI automation SHALL launch `FieldWorks.exe` against a deterministic, test-owned data root and a deterministic, test-owned settings root.

**Affected Paths**
- `Src/Common/FwUtils/FwDirectoryFinder.cs`
- `Src/Common/FwUtils/FwApplicationSettings.cs`
- `Src/Common/FieldWorks/FieldWorksUiAutomationTests/Infrastructure/**`
- `TestLangProj/**`

#### Scenario: Automation launch does not depend on the developer profile

- **WHEN** a UI automation fixture prepares a test run
- **THEN** it SHALL create a temporary workspace for the target project data
- **AND** it SHALL launch `FieldWorks.exe` with process-scoped overrides that point project discovery and user settings at that workspace
- **AND** it SHALL clean up that workspace after the run completes

#### Scenario: First-run prompts are suppressed for automation runs

- **WHEN** `FieldWorks.exe` is launched by the UI automation harness on a clean runner or Windows server session
- **THEN** update/reporting settings SHALL already be seeded in the isolated settings root
- **AND** analytics and assertion UI SHALL be disabled for that child process
- **AND** the app SHALL proceed directly to the main window for the requested project without a first-run prompt blocking the smoke test

### Requirement: The harness provides stable launch, focus, and navigation primitives

The UI automation harness SHALL provide reusable primitives for launching FieldWorks, locating the main window, restoring focus, navigating menus, and closing the app safely.

**Affected Paths**
- `Src/Common/FieldWorks/FieldWorksUiAutomationTests/Infrastructure/FwUiTestBase.cs`
- `Src/Common/FieldWorks/FieldWorksUiAutomationTests/Infrastructure/TestSteps.cs`

#### Scenario: Focus-sensitive interactions stay within FieldWorks

- **WHEN** a test needs to send keyboard input or click shell elements after launch
- **THEN** the harness SHALL restore or verify focus on the FieldWorks main window before the action executes
- **AND** it SHALL fail with diagnostic context instead of sending input to an unrelated foreground application

#### Scenario: Pattern-first actions minimize physical-input dependence

- **WHEN** the harness can complete an interaction through stable UI Automation patterns instead of real mouse or keyboard input
- **THEN** it SHALL prefer the pattern-based action
- **AND** physical input SHALL be reserved for controls or transitions that cannot be handled reliably through UI Automation patterns alone

#### Scenario: Popup menus remain discoverable

- **WHEN** a test opens a multi-level menu or popup surface
- **THEN** the harness SHALL search the desktop automation tree as well as the main window tree so popup elements created outside the root window remain discoverable

#### Scenario: Shutdown targets the app process, not the foreground window

- **WHEN** a fixture tears down
- **THEN** the harness SHALL request shutdown against the launched `FieldWorks.exe` process directly
- **AND** it SHALL wait for exit and fall back to termination only if graceful close fails

### Requirement: Initial smoke coverage produces screenshot evidence

The first UI automation slice SHALL cover launch and a small number of stable navigation scenarios, and it SHALL preserve screenshot evidence for failures.

**Affected Paths**
- `Src/Common/FieldWorks/FieldWorksUiAutomationTests/SmokeTests/**`
- `Src/Common/FieldWorks/FieldWorksUiAutomationTests/NavigationTests/**`

#### Scenario: Launch smoke test validates the shell

- **WHEN** the smoke suite launches FieldWorks with `TestLangProj`
- **THEN** it SHALL verify that the main window appears
- **AND** it SHALL verify that the project is opened successfully
- **AND** it SHALL verify that a stable set of shell/menu elements is visible before the fixture passes

#### Scenario: Failure artifacts are preserved

- **WHEN** a UI automation test fails
- **THEN** the harness SHALL capture at least one screenshot to a deterministic output location
- **AND** the failure output SHALL identify the step or checkpoint that failed