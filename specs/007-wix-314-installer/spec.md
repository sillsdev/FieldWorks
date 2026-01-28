# Feature Specification: WiX 3.14 Installer Upgrade & x64-Only Build Migration

**Feature Branch**: `007-wix-314-installer`
**Created**: December 2, 2025
**Status**: Draft
**Input**: User description: "With an upgraded Wix 3.14, confirm the ability to build an installer and patch for FieldWorks, modernizing and documenting any infrastructure as needed."

## Scope Expansion (December 2, 2025)

During implementation, it was discovered that the installer build system still contained x86 references that prevented local validation. Since FieldWorks is now x64-only, this spec has been expanded to include:

1. **Original scope**: WiX 3.14.x upgrade (remove downgrade workaround)
2. **Expanded scope**: Complete migration of build infrastructure to x64-only

### Rationale for Expansion
- The installer build system references `win-x86` paths for encoding converters and other dependencies
- Native C++ build targets still included x86 configurations
- Trying to validate WiX 3.14.x locally exposed these x86 artifacts blocking the build
- Since FieldWorks is x64-only, cleaning up all x86 references simplifies maintenance

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Remove WiX Downgrade Workaround (Priority: P1)

As a maintainer, I need the CI/CD workflows to use the pre-installed WiX 3.14.x on GitHub's `windows-latest` runners instead of downgrading to WiX 3.11.2, so that the build process is simpler and uses modern tooling.

**Why this priority**: The current workflows contain a workaround step that actively downgrades WiX from 3.14.x to 3.11.2. This adds build time, complexity, and prevents using WiX improvements. Removing this workaround is the core enabler for all other improvements.

**Independent Test**: Build a base installer using WiX 3.14.x without the downgrade step and verify the resulting installer functions correctly.

**Acceptance Scenarios**:

1. **Given** the `base-installer-cd.yml` workflow, **When** the workflow runs on `windows-latest`, **Then** it uses the pre-installed WiX 3.14.x without any downgrade steps and produces a valid installer.

2. **Given** the `patch-installer-cd.yml` workflow, **When** the workflow runs on `windows-latest`, **Then** it uses the pre-installed WiX 3.14.x without any downgrade steps and produces a valid patch file.

3. **Given** a `windows-latest` runner with WiX 3.14.x, **When** the `insignia` tool is used to extract and reattach burn engines, **Then** the signing workflow completes successfully.

---

### User Story 2 - Validate Base Installer Build (Priority: P1)

As a release manager, I need to verify that the base installer built with WiX 3.14.x installs FieldWorks correctly, so that users can install the software without issues.

**Why this priority**: The base installer is the primary distribution mechanism for new users. Validating it works with WiX 3.14.x is essential before any release.

**Independent Test**: Build the base installer with WiX 3.14.x, install it on a clean Windows system, and verify FieldWorks launches and core functionality works.

**Acceptance Scenarios**:

1. **Given** a base installer built with WiX 3.14.x, **When** a user runs the offline installer on Windows 10/11, **Then** FieldWorks installs successfully and can be launched.

2. **Given** a base installer built with WiX 3.14.x, **When** a user runs the online installer on Windows 10/11 with internet connectivity, **Then** FieldWorks installs successfully including all prerequisites.

3. **Given** a system with an older version of FieldWorks installed, **When** the new base installer is run, **Then** it upgrades the existing installation without data loss.

---

### User Story 3 - Validate Patch Installer Build (Priority: P1)

As a release manager, I need to verify that patch installers built with WiX 3.14.x apply correctly to base installations, so that users can receive updates without full reinstalls.

**Why this priority**: Patches are the primary mechanism for delivering updates to existing users. They must work correctly with WiX 3.14.x to maintain the update infrastructure.

**Independent Test**: Build a patch installer with WiX 3.14.x, apply it to a base installation, and verify the update is applied correctly.

**Acceptance Scenarios**:

1. **Given** a base installation from a WiX 3.14.x-built installer, **When** a WiX 3.14.x-built patch is applied, **Then** the patch installs successfully and the version number updates.

2. **Given** a base installation from an older WiX 3.11.x-built installer (build 1188), **When** a WiX 3.14.x-built patch is applied, **Then** the patch installs successfully (backward compatibility).

3. **Given** a patch installation, **When** the user launches FieldWorks, **Then** all features work correctly and no errors are logged related to the upgrade.

---

### User Story 4 - Document Installer Build Process (Priority: P2)

As a developer, I need clear documentation on how to build installers locally and in CI, so that I can troubleshoot issues and make informed changes to the installer infrastructure.

**Why this priority**: Documentation ensures maintainability and reduces bus factor. While the installer works without documentation, having it prevents knowledge loss.

**Independent Test**: A new developer can follow the documentation to build an installer locally without additional guidance.

**Acceptance Scenarios**:

1. **Given** the installer documentation, **When** a developer follows the steps, **Then** they can build a base installer locally on their development machine.

2. **Given** the installer documentation, **When** a developer needs to understand the CI workflow, **Then** they can find explanations of each step and its purpose.

3. **Given** the installer documentation, **When** troubleshooting a build failure, **Then** the developer can find guidance on common issues and their solutions.

---

### User Story 5 - Update Agent Instructions (Priority: P3)

As an AI agent or developer, I need accurate instructions that reflect the current WiX version (3.14.x), so that guidance is consistent with the actual build infrastructure.

**Why this priority**: Incorrect documentation causes confusion and wasted time. While lower priority than functional changes, it's important for ongoing maintenance.

**Independent Test**: Review all agent instructions and verify WiX version references are accurate.

**Acceptance Scenarios**:

1. **Given** the `installer.instructions.md` file, **When** it references WiX tooling, **Then** it specifies version 3.14.x (not 3.11.x).

2. **Given** the `AGENTS.md` file, **When** it mentions WiX prerequisites, **Then** it accurately reflects the current required version.

---

### User Story 6 - Migrate Build Infrastructure to x64-Only (Priority: P1)

As a developer, I need the build infrastructure to be x64-only, so that builds don't fail looking for x86 dependencies that no longer exist.

**Why this priority**: FieldWorks is x64-only, but the build system still references x86 paths and configurations. This causes build failures when trying to build installers locally or in CI.

**Independent Test**: Build a base installer locally using only x64 tooling and verify no x86 references cause failures.

**Acceptance Scenarios**:

1. **Given** the build targets files, **When** building for x64, **Then** no x86 file paths are referenced.

2. **Given** the encoding converters package, **When** copying native files, **Then** only `win-x64` files are copied (not `win-x86`).

3. **Given** the native C++ projects, **When** building, **Then** only x64 configurations are used.

4. **Given** the installer build targets, **When** building base or patch installers, **Then** only x64 architecture is supported.

---

### Edge Cases

- What happens when WiX 3.14.2+ is released and runners update?
  - The build should continue to work as 3.14.x versions are expected to be compatible.

- What happens if a user has WiX 3.11.x installed locally?
  - Local builds should work with either version; document minimum version requirements.

- How does this affect developers building installers on machines without WiX?
  - Document that WiX 3.14.x installation is required for local installer builds.

- What happens to existing patches built with WiX 3.11.x?
  - They should continue to apply to existing installations; new patches will be built with 3.14.x.

- What happens if someone tries to build for x86?
  - Build will fail with a clear error message indicating x64-only support.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: CI workflows MUST build installers using WiX 3.14.x pre-installed on `windows-latest` runners.
- **FR-002**: CI workflows MUST NOT include WiX downgrade steps.
- **FR-003**: Base installers MUST be buildable using `msbuild Build/Orchestrator.proj /t:BuildBaseInstaller`.
- **FR-004**: Patch installers MUST be buildable using `msbuild Build/Orchestrator.proj /t:BuildPatchInstaller`.
- **FR-005**: The `insignia` tool MUST work correctly with WiX 3.14.x for burn engine extraction/reattachment.
- **FR-006**: Installer documentation MUST exist in the repository explaining the build process.
- **FR-007**: All Copilot instructions MUST reference WiX 3.14.x instead of 3.11.x.
- **FR-008**: Patches built with WiX 3.14.x MUST apply successfully to base installations built with WiX 3.11.x (backward compatibility).
- **FR-009**: Build infrastructure MUST use x64-only paths for all native dependencies.
- **FR-010**: Build targets MUST NOT reference x86 architecture or win-x86 paths.
- **FR-011**: Installer builds MUST produce only x64 installers.

### Key Entities

- **Base Installer**: Full installation package (online and offline variants) that installs FieldWorks from scratch.
- **Patch Installer**: Incremental update package (.msp) that updates an existing FieldWorks installation.
- **Burn Engine**: The bootstrapper component extracted from installers for code signing.
- **Build Artifacts**: BuildDir.zip and ProcRunner.zip released with each base build for patch generation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: CI workflow execution time for base installer build decreases by at least 30 seconds (removing chocolatey WiX downgrade step).
- **SC-002**: 100% of installer builds on `windows-latest` complete successfully using WiX 3.14.x.
- **SC-003**: Base installers install successfully on Windows 10 and Windows 11 test systems.
- **SC-004**: Patch installers apply successfully to both WiX 3.11.x and WiX 3.14.x base installations.
- **SC-005**: A developer unfamiliar with the installer can build one locally within 30 minutes using only the documentation.
- **SC-006**: Zero references to WiX 3.11 remain in active documentation files (excluding historical notes).

## Constitution Alignment Notes

- Data integrity: Installer upgrades must preserve user data (projects, settings, preferences). This is existing behavior that must be validated, not changed.
- Internationalization: No impact—installers already support localized installations.
- Licensing: WiX 3.14.x uses MS-RL license (Microsoft Reciprocal License), compatible with existing project licensing.

## Assumptions

- The `windows-latest` GitHub runner will continue to have WiX 3.14.x pre-installed.
- The WiX 3.14.x tooling is backward compatible with WiX 3.11.x .wixproj and .wxs files.
- The existing `FLExInstaller/` WiX source files do not require modification for WiX 3.14.x.
- The `sillsdev/genericinstaller` repository works with WiX 3.14.x.
- Code signing with Azure Trusted Signing works with WiX 3.14.x-produced artifacts.
