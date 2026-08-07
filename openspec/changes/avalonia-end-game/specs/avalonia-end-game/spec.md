## ADDED Requirements

### Requirement: Avalonia is the sole default shell

FieldWorks SHALL use Avalonia as the sole default application shell, with no WinForms shell, host, dynamic content host, or `FlexUIAdapter` default behavior created in the default application path.

#### Scenario: Default startup creates only the Avalonia shell
- **WHEN** FieldWorks starts in the default (cutover) mode
- **THEN** the top-level shell, windowing, navigation, content host, menus, toolbars, status panes, and dialogs SHALL be Avalonia-owned
- **AND** no WinForms shell, WinForms dynamic content host, WinForms main-window service, or `FlexUIAdapter` default path SHALL be created

#### Scenario: Startup audit fails on hidden WinForms shell
- **WHEN** default-startup dependency validation runs
- **THEN** it SHALL fail if the default path creates a WinForms shell, dynamic content host, WinForms main-window service, or `FlexUIAdapter` default behavior

### Requirement: Application uses a single Avalonia lifetime

The default application SHALL run on a single Avalonia desktop lifetime with explicit top-level window ownership, not a WinForms application lifetime hosting Avalonia content.

#### Scenario: No WinForms application lifetime hosts the default app
- **WHEN** the default app starts, opens a project, and shuts down
- **THEN** application lifetime, window ownership, active-window tracking, modal ownership, UI dispatch, and shutdown SHALL be provided by the Avalonia implementation
- **AND** the default path SHALL NOT require `Application.Run`, a WinForms `Form` host, or a WinForms message loop

### Requirement: Application retargets to .NET 10

The managed FieldWorks application SHALL retarget from .NET Framework 4.8 to .NET 10, built through the repository build script with native-C++-before-managed ordering and registration-free COM preserved.

#### Scenario: Managed graph builds on net10 via repo scripts
- **WHEN** the default managed application graph is built through `build.ps1` / `FieldWorks.proj`
- **THEN** the managed projects SHALL target .NET 10
- **AND** native C++ SHALL build before managed projects
- **AND** COM SHALL remain registration-free with no global COM registration or registry hacks

#### Scenario: net48-only default paths are removed
- **WHEN** the retarget is complete
- **THEN** the default application graph SHALL contain no net48-only or WinForms-only default code path or package required for default startup

### Requirement: Application runs on Windows, macOS, and Linux

The default application SHALL build, run, and package on Windows, macOS, and Linux.

#### Scenario: App launches on each supported OS
- **WHEN** the cutover application is built and launched on Windows, on macOS, and on Linux
- **THEN** it SHALL start, open or create a project, and present the Avalonia shell on each OS
- **AND** continuous integration SHALL build and smoke-test the app on all three OSes

### Requirement: WinForms shell and default-path UI infrastructure are removed

The WinForms shell, WinForms main-window services, WinForms-only default dialogs, the `FlexUIAdapter` default path, Gecko/Graphite-on-Avalonia assumptions, and native Views rendering SHALL be removed from the default application.

#### Scenario: Default dependency audit fails on removed infrastructure
- **WHEN** default-app dependency validation runs after cutover
- **THEN** it SHALL fail if the default path references the WinForms shell, WinForms-only default dialogs, the `FlexUIAdapter` default behavior, Gecko/XULRunner, native Graphite shaping, or native Views display/layout/hit-testing/selection/editor-realization for default UI

#### Scenario: Deletion lands behind a per-target gate
- **WHEN** a WinForms-era target is deleted
- **THEN** its Avalonia replacement SHALL already be the sole default path
- **AND** full-app smoke and dependency-audit evidence SHALL exist on all three OSes before the deletion lands

### Requirement: Retained native services stay behind non-UI seams

The retained native `ITsString`/writing-system data kernel, ICU, EncConverters, XAmple, spelling, and parser services SHALL remain available behind non-UI service seams via managed interop and SHALL NOT participate in Avalonia display, layout, hit testing, selection, or editor realization.

#### Scenario: Native data kernel is consumed as data, not as a renderer
- **WHEN** the Avalonia shell or screens use `ITsString`/writing-system data, ICU, EncConverters, XAmple, spelling, or parser services
- **THEN** those services SHALL be reached through non-UI service contracts via managed interop
- **AND** they SHALL NOT be on the Avalonia render or editor path

#### Scenario: Native data kernel is not removed by the cutover
- **WHEN** WinForms removal and the net10/cross-platform cutover complete
- **THEN** the native `ITsString`/writing-system data kernel SHALL remain as the retained data model
- **AND** native-render removal SHALL NOT remove the native data kernel

### Requirement: Cutover requires proven Phase-1 functional parity

WinForms removal SHALL NOT begin until the Phase-1 functional parity burn-down shows no open functional regression.

#### Scenario: WinForms deletion is blocked by open functional regressions
- **WHEN** any WinForms-removal step is proposed
- **THEN** the `lexical-edit-avalonia-migration` Phase-1 functional parity burn-down SHALL show zero open functional regressions before the step proceeds

### Requirement: Cross-platform packaging and CI

Installer/packaging logic SHALL produce per-OS artifacts harvesting the Avalonia runtime and required native dependencies for Windows, macOS, and Linux, validated in continuous integration on each OS.

#### Scenario: Per-OS artifacts are produced and validated
- **WHEN** packaging runs for the cutover application
- **THEN** it SHALL produce the artifact for each of Windows, macOS, and Linux including the Avalonia runtime and required native dependencies for that OS
- **AND** CI SHALL build and smoke-test the packaged app on each OS

### Requirement: Localization, accessibility, and keyboarding survive the cutover

The cutover application SHALL preserve localization, accessibility identity, and keyboarding behavior present before the cutover.

#### Scenario: Localized, accessible, keyboard-capable app post-cutover
- **WHEN** the cutover application runs on a supported OS
- **THEN** user-facing strings SHALL resolve from existing localization resources
- **AND** controls SHALL expose stable accessibility identity for UIA/accessibility tooling
- **AND** writing-system keyboard/IME switching SHALL behave equivalently to the pre-cutover baseline for covered workflows
