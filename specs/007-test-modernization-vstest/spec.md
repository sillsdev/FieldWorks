# Feature Specification: Test Modernization (Option B - VSTest)

**Feature Branch**: `007-test-modernization-vstest`
**Created**: 2025-11-21
**Status**: Draft
**Input**: User description: "Implement Option B from TEST_MIGRATION_PATHS.md: Replace NUnit3 tasks with an MSBuild wrapper over vstest.console, retaining traversal ordering, timeouts, and filters."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - CI/Build Script Execution via VSTest (Priority: P1)

As a developer or CI agent, I want the build script (`FieldWorks.proj` / `build.ps1`) to execute managed tests using `vstest.console.exe` instead of the legacy NUnit console runner, so that I can produce standard TRX results and align with modern .NET tooling.

**Why this priority**: This is the core infrastructure change required to decouple from the legacy NUnit runner and enable future improvements.

**Independent Test**: Run `.\build.ps1 -Configuration Debug -Platform x64` (or specific test target) and verify that tests execute, pass, and produce `.trx` files in the output directory.

**Acceptance Scenarios**:

1. **Given** a clean build of FieldWorks, **When** I run the full test suite via `build.ps1`, **Then** all managed test assemblies are executed using `vstest.console.exe`.
2. **Given** a test failure, **When** running the build, **Then** the build fails and reports the error in a standard format (TRX/Console).
3. **Given** existing test categories (e.g., `exclude:HardToTest`), **When** running the build, **Then** these filters are respected by the VSTest runner.

---

### User Story 2 - VS Code Test Explorer Integration (Priority: P2)

As a developer using VS Code, I want to discover and run tests directly from the Test Explorer UI, so that I can debug and iterate on tests efficiently without leaving the editor.

**Why this priority**: Improves developer inner-loop productivity and debugging experience.

**Independent Test**: Open the repo in VS Code, build the solution, and verify that tests appear in the Test Explorer and can be run/debugged.

**Acceptance Scenarios**:

1. **Given** the FieldWorks workspace in VS Code, **When** I open the Test Explorer, **Then** I see a hierarchy of managed tests.
2. **Given** a specific test, **When** I click "Debug Test", **Then** the debugger attaches and hits breakpoints in the test code.

---

### User Story 3 - Legacy Parity (Timeouts & Reporting) (Priority: P3)

As a release manager, I want the new test runner to respect existing timeout configurations and produce reports compatible with our CI pipeline, so that we don't lose stability or visibility during the migration.

**Why this priority**: Ensures no regression in build reliability or reporting capabilities.

**Independent Test**: Verify that long-running tests do not time out prematurely and that generated reports contain necessary data.

**Acceptance Scenarios**:

1. **Given** a test project with specific timeout settings in MSBuild, **When** executed via VSTest, **Then** those timeouts are enforced.
2. **Given** a test run completion, **When** inspecting the output, **Then** a `.trx` file is generated containing pass/fail results.

---

### User Story 4 - Native Test Modernization (Optional / Phase 2) (Priority: Optional)

As a developer, I want legacy C++ tests (`TestViews`, `TestGeneric`) to be migrated from the custom "Unit++" framework to GoogleTest, so that they can be discovered and run in VS Code alongside managed tests.

**Why this priority**: Enables a unified testing experience but is not critical for the initial VSTest migration.

**Independent Test**: Verify that a migrated C++ test project builds, runs via the GoogleTest adapter, and appears in VS Code Test Explorer.

**Acceptance Scenarios**:

1. **Given** a legacy C++ test project, **When** migrated to GoogleTest, **Then** it can be executed by `vstest.console.exe` using the GoogleTest adapter.
2. **Given** the VS Code Test Explorer, **When** the project is built, **Then** the native tests appear in the list.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The build system MUST replace the `<NUnit3>` MSBuild task with an invocation of `vstest.console.exe`.
- **FR-002**: All managed test projects MUST reference the `NUnit3TestAdapter` NuGet package (via `Directory.Build.props`) to enable discovery by VSTest and VS Code.
- **FR-003**: The build system MUST translate existing NUnit category filters (e.g., `cat != Exclude`) into VSTest filter syntax (`/TestCaseFilter:"TestCategory!=Exclude"`).
- **FR-004**: The build system MUST pass appropriate timeout values to the VSTest runner.
- **FR-005**: The build system MUST output test results in TRX format to `Output/$(Configuration)/TestResults/`.
- **FR-006**: The change MUST NOT alter the traversal build order defined in `FieldWorks.proj`.
- **FR-007**: Test projects MUST set `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` (via `Directory.Build.props`) to ensure test adapters are copied to the output directory for discovery by external tools.
- **FR-008**: The build system MUST invoke VSTest with the `/Parallel` switch to enable concurrent assembly execution.
- **FR-009**: The solution MUST include a global `.runsettings` file to centralize configuration (e.g., `DefaultTimeout`, `ThreadApartmentState`) for both CI and IDE usage.
- **FR-010**: The build system MUST support an optional `-Coverage` switch (defaulting to false) to enable code coverage collection during test execution.
- **FR-011**: The specification MUST include a detailed, optional plan for migrating legacy C++ tests (`TestViews`, `TestGeneric`) from "Unit++" to GoogleTest to enable VSTest integration.

## Clarifications

### Session 2025-11-21
- Q: Should VSTest run in parallel? → A: Yes, enable /Parallel by default to maintain performance parity.
- Q: Where should test results be stored? → A: `Output/$(Configuration)/TestResults/` to keep artifacts organized.
- Q: How should test settings be configured? → A: Use a global `.runsettings` file for consistency between CI and IDEs.
- Q: Should code coverage be enabled? → A: Optional via build switch (default off).
- Q: How to handle Native C++ tests? → A: Leave as-is (Option A) for the main migration. Add an optional requirement to migrate them to GoogleTest later.

### Key Entities

- **FieldWorks.targets**: The central MSBuild file defining the `Test` target; currently invokes `<NUnit3>`.
- **Directory.Build.props**: The shared configuration file where `NUnit3TestAdapter` and `CopyLocalLockFileAssemblies` must be defined.
- **NUnit3TestAdapter**: The NuGet package required for VSTest to interface with NUnit tests.
- **vstest.console.exe**: The command-line runner for the Visual Studio Test Platform.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of currently passing managed tests pass under `vstest.console.exe`.
- **SC-002**: VS Code Test Explorer successfully discovers tests in `Src/Common` and `Src/LexText` (representative large projects).
- **SC-003**: CI build produces `.trx` files for all test assemblies.

## Constitution Alignment Notes

- **Data integrity**: No data migration required; this is a tooling change.
- **Internationalization**: No changes to product code or localization.
- **Licensing**: `NUnit3TestAdapter` is MIT licensed (compatible).
