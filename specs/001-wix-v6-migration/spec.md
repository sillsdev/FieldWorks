# Feature Specification: WiX v6 Migration

**Feature Branch**: 001-wix-v6-migration
**Created**: 2025-12-11
**Status**: Draft
**Input**: User description: "Migrate WiX 3.11 installer to WiX v6, modernize build process, and remove genericinstaller submodule."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developer Builds Installer (Priority: P1)

A developer wants to build the installer locally to verify changes or create a release. They should be able to do this using standard MSBuild commands without relying on legacy batch files or external submodules.

**Why this priority**: This is the core development workflow. Without a working build, no other testing or deployment is possible.

**Independent Test**: Run the installer build on a clean developer machine (e.g., `./build.ps1 -BuildInstaller`, or `msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release`) and verify that a valid .exe or .msi is produced.

**Acceptance Scenarios**:

1. **Given** a clean repository checkout, **When** the developer runs the installer build command, **Then** the build completes without errors and produces the installer artifacts.
2. **Given** the genericinstaller submodule is removed, **When** the build runs, **Then** it succeeds using the migrated custom actions and logic.

---

### User Story 2 - CI Builds Installer (Priority: P1)

The Continuous Integration (CI) system needs to build the installer automatically on code changes. The workflow must be updated to use the new WiX v6 build process.

**Why this priority**: Automated builds are essential for quality assurance and release management.

**Independent Test**: Trigger the GitHub Actions workflow and verify that the "Build Installer" step succeeds and artifacts are uploaded.

**Acceptance Scenarios**:

1. **Given** a push to the repository, **When** the CI workflow triggers, **Then** the installer build step executes the new MSBuild targets and succeeds.
2. **Given** the build completes, **When** artifacts are inspected, **Then** the installer files are present and valid.

---

### User Story 3 - End User Installation (Online) (Priority: P1)

An end user downloads the installer and runs it on a machine with internet access. The installer should detect missing prerequisites, download them, and install the product.

**Why this priority**: This is the primary distribution method for the product.

**Independent Test**: Run the generated installer on a clean VM with internet access. Verify prerequisites are downloaded and the product installs.

**Acceptance Scenarios**:

1. **Given** a machine without prerequisites, **When** the user runs the installer, **Then** it downloads and installs the prerequisites before installing the product.
2. **Given** the installation completes, **When** the user launches the product, **Then** it opens successfully.

---

### User Story 4 - End User Installation (Offline) (Priority: P2)

An end user (or admin) wants to install the product on a machine without internet access. They use an offline layout or bundled installer that includes all prerequisites.

**Why this priority**: Critical for deployments in low-connectivity environments.

**Independent Test**: Create an offline layout (or use the offline bundle), disconnect the VM from the internet, and run the installer.

**Acceptance Scenarios**:

1. **Given** an offline machine and the offline installer media, **When** the user runs the installer, **Then** it installs prerequisites from the local source without attempting to download them.
2. **Given** the installation completes, **When** the user checks the installed programs, **Then** all components are present.

### Edge Cases

- **Build Environment**: What happens if the build machine lacks the specific .NET SDK version required by WiX v6? (Build should fail with a clear error message).
- **Upgrade**: The installer MUST perform a **Major Upgrade** (Seamless Upgrade), automatically removing the previous version before installing the new one.
- **Localization**: What happens if a specific locale build fails? (The entire build process should report the error).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The build system MUST use WiX Toolset v6 for generating all installer artifacts.
- **FR-002**: All build logic currently in batch files MUST be migrated to MSBuild .targets files.
- **FR-003**: The project MUST NOT depend on the genericinstaller git submodule; all necessary code/config from it MUST be migrated into the main repository.
- **FR-004**: The installer MUST support both online (downloading) and offline (bundled) installation of prerequisites.
- **FR-005**: The installer MUST include all existing features, components, and localization variants (roughly a dozen features).
- **FR-006**: The GitHub Actions workflow MUST be updated to install WiX v6 and execute the new build targets.
- **FR-007**: Custom actions currently provided by genericinstaller MUST be re-implemented or migrated to be compatible with WiX v6.
- **FR-008**: The build process MUST support passing parameters (overrides) via MSBuild properties to customize the installer, replacing the old batch file parameter mechanism.
- **FR-009**: The build process MUST support code signing of the generated MSI and Bundle artifacts, configurable via environment variables (e.g., for certificate path/password).
- **FR-010**: The installer UI MUST be ported from the existing custom implementation to maintain the "Dual Directory" selection (App + Project Data) and custom feature tree behavior.
- **FR-011**: The build process MUST automatically download required prerequisites (e.g., .NET runtimes, C++ redistributables) during the build phase using MSBuild targets, rather than relying on pre-existing files.

### Success Criteria

- **Build Success**: The installer builds successfully on both developer machines and the CI environment using the new process.
- **No Legacy Dependencies**: The genericinstaller submodule is removed from the repository.
- **Functional Parity**: The resulting installer provides the same installation options (features, locales, offline/online) as the previous version.
- **Modernization**: The build process uses only MSBuild and WiX v6 tools, with no dependency on legacy batch scripts for the core build logic.

### Assumptions

- The existing WiX 3.11 source code is largely compatible with WiX v6 or can be automatically converted/easily patched.
- The genericinstaller submodule content is available for migration.
- WiX v6 supports the specific prerequisites (e.g., .NET Framework versions) required by the application.
- The team accepts the "breaking changes" inherent in moving to WiX v6 (e.g., different CLI tools, different project file format).

### Key Entities

- **Installer Bundle**: The executable that manages prerequisites and the MSI installation.
- **MSI Package**: The core product installer containing files and registry keys.
- **Build Target**: The MSBuild definition that orchestrates the WiX build process.

## Clarifications

### Session 2025-12-11
- Q: How should the installer handle upgrades from the previous v3-based version? → A: Seamless Upgrade (MajorUpgrade).
- Q: Should the build process support code signing of the installer artifacts? → A: Yes, via Environment Variables.
- Q: How should installer prerequisites (e.g., .NET runtimes) be handled during the build? → A: Yes, via MSBuild Targets (auto-download).

- Q: How should the custom installer UI (Dual Directory selection, etc.) be handled in WiX v6? → A: Port Existing Custom UI.


