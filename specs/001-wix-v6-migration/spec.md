# Feature Specification: WiX v6 Migration

**Feature Branch**: 001-wix-v6-migration
**Created**: 2025-12-11
**Status**: Draft - harmonized with current repo state
**Input**: User description: "Migrate WiX 3.11 installer to WiX v6, modernize build process, and remove genericinstaller submodule."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developer Builds Installer (Priority: P1)

A developer wants to build the installer locally to verify changes or create a release. They should be able to do this using standard MSBuild commands without relying on legacy batch files or external submodules.

**Why this priority**: This is the core development workflow. Without a working build, no other testing or deployment is possible.

**Independent Test**: Run the WiX 6 installer build on a clean developer machine (e.g., `./build.ps1 -BuildInstaller -InstallerToolset Wix6`, or `msbuild Build/InstallerBuild.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:InstallerToolset=Wix6`) and verify that the MSI plus online and offline bundle artifacts are produced.

**Acceptance Scenarios**:

1. **Given** a clean repository checkout, **When** the developer runs the installer build command, **Then** the build completes without errors and produces the installer artifacts.
2. **Given** the genericinstaller submodule is removed, **When** the build runs, **Then** it succeeds using the migrated custom actions and logic.

---

### User Story 2 - CI Builds Installer (Priority: P1)

The Continuous Integration (CI) system needs to build the installer automatically on code changes. The workflow must be updated to use the WiX 6 build process as the migration target and keep any WiX 3 lane explicitly transitional.

**Why this priority**: Automated builds are essential for quality assurance and release management.

**Independent Test**: Trigger the GitHub Actions workflow and verify that the "Build Installer" step succeeds and artifacts are uploaded.

**Acceptance Scenarios**:

1. **Given** a push to the repository, **When** the CI workflow triggers, **Then** a WiX 6 installer build step executes the SDK-style MSBuild targets and succeeds.
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

- **FR-001**: The WiX 6 build path MUST use WiX Toolset v6 for generating all WiX 6 installer artifacts.
- **FR-002**: WiX 6 build logic MUST be migrated to MSBuild .targets files.
- **FR-003**: The WiX 6 build path MUST NOT depend on the genericinstaller git submodule; all necessary code/config from it MUST be migrated into the main repository.
- **FR-004**: The installer MUST support both online (downloading) and offline (bundled) installation of prerequisites.
- **FR-005**: The installer MUST include all existing features, components, and localization variants (roughly a dozen features).
- **FR-006**: The GitHub Actions workflow MUST be updated to install WiX v6 and execute the new build targets.
- **FR-007**: Custom actions currently provided by genericinstaller MUST be re-implemented or migrated to be compatible with WiX v6.
- **FR-008**: The build process MUST support passing parameters (overrides) via MSBuild properties to customize the installer, replacing the old batch file parameter mechanism.
- **FR-009**: The build process MUST support code signing of the generated MSI and Bundle artifacts, configurable via environment variables (e.g., for certificate path/password).
- **FR-010**: The installer UI MUST be ported from the existing custom implementation to maintain the "Dual Directory" selection (App + Project Data) and custom feature tree behavior.
- **FR-011**: The build process MUST automatically download required prerequisites (e.g., .NET runtimes, C++ redistributables) during the build phase using MSBuild targets, rather than relying on pre-existing files.

## Current Migration State: WiX 6-first with Temporary WiX 3 Fallback

**Goal**: Finish the WiX 6 migration first, then switch the default installer build to WiX 6 once the validation gates pass. The current repo still keeps a WiX 3 fallback path for transition safety, but WiX 3 is no longer the desired end state.

**Current repo snapshot**:

- `Build/InstallerBuild.proj` currently defaults `InstallerToolset` to `Wix3` and conditionally imports `Build/Installer.Wix3.targets` or `Build/Installer.targets`.
- The WiX 6 route is explicit: `./build.ps1 -BuildInstaller -InstallerToolset Wix6` or `msbuild Build/InstallerBuild.proj /t:BuildInstaller /p:InstallerToolset=Wix6`.
- The WiX 6 route builds `FLExInstaller/wix6/FieldWorks.Bundle.wixproj` and `FLExInstaller/wix6/FieldWorks.OfflineBundle.wixproj`.
- `PatchableInstaller/` is not present in this worktree. Existing CI workflows still checkout `sillsdev/genericinstaller` as `PatchableInstaller/` for legacy installer jobs.
- `build.ps1 -BuildPatch -InstallerToolset Wix6` is not a supported path until WiX 6 patch/MSP work is designed and implemented.

### Transitional Requirements

- **TR-001**: The build system MUST support building **either WiX 3 or WiX 6** installers via an explicit MSBuild property or build script flag.
- **TR-002**: The current default installer build produces WiX 3 artifacts. This is a temporary compatibility state, not the migration target.
- **TR-003**: WiX 3 build inputs MUST be preserved and isolated from WiX 6 schema changes (no shared `.wxi` files between toolsets).
- **TR-004**: CI MUST add a WiX 6 installer lane that does not require `genericinstaller` or `PatchableInstaller/`; any WiX 3 lane must be named as legacy/transition.
- **TR-005**: Documentation MUST clearly describe how to build WiX 6 installers, where artifacts are produced, and which WiX 3/genericinstaller dependencies remain transitional.
- **TR-006**: Before the migration is considered complete, the repo default MUST be changed from WiX 3 to WiX 6, or an explicit release decision must document why that switch is deferred.

**Note**: During the transition, **FR-001/FR-002/FR-003 apply to the WiX 6 path only**. The WiX 3 path intentionally retains its legacy build flow only as a fallback until the transition ends.

### Changes in this spec that can break WiX 3 (must be reversed or isolated)

- **WiX 6 schema changes to `FLExInstaller/*.wxi`** (namespace and element changes) make those files incompatible with WiX 3.
- **`Build/Installer.targets` rewritten for WiX 6** and **WiX 3 batch pipelines removed/quarantined** (see tasks T040–T043, T030–T031).
- **Removal of `PatchableInstaller`/`genericinstaller` assumptions** eliminates WiX 3 build inputs and scripts. The current worktree has removed the in-tree `PatchableInstaller/` folder, but CI still checks out the external repo for legacy jobs.
- **Custom action wiring changed to WiX 4+ binaries** (e.g., `Wix4UtilCA_X64`), incompatible with WiX 3.

### Current legacy WiX 3 fallback state

The transition originally considered restoring the full WiX 3 project tree. The current worktree instead has this split:

- `Build/Installer.Wix3.targets` contains the legacy WiX 3 orchestration.
- `FLExInstaller/*.wxi` remains the WiX 3-compatible FieldWorks include baseline.
- `PatchableInstaller/` is **not** restored in-tree.
- `.github/workflows/base-installer-cd.yml` and `.github/workflows/patch-installer-cd.yml` still checkout `sillsdev/genericinstaller` into `PatchableInstaller/` for legacy CI behavior.

This means “WiX 6-first” cleanup is not complete: the repo has removed the in-tree generic installer, but CI still depends on it for some legacy jobs. Do not reintroduce a `genericinstaller` submodule as the migration solution; migrate any remaining required behavior into the WiX 6 path or keep it behind an explicitly legacy job until that job is retired.

### What else must change (high-level)

- **Split installer inputs by toolset** with **WiX 3 in `FLExInstaller/` root** and **WiX 6 under `FLExInstaller/wix6/`**, and prevent cross-use.
- **Relocate WiX 6 assets** (projects + shared authoring) under `FLExInstaller/wix6/` to avoid collisions with WiX 3 authoring.
- **Add toolset selection property** (`InstallerToolset=Wix3|Wix6`) to `build.ps1` and `Build/InstallerBuild.proj`. The current default is Wix3; the migration target is to change the default to Wix6 after validation.
- **Provide clear MSBuild entry points** for the imported WiX 3 and WiX 6 routes. Current state has `BuildInstaller` imported per toolset plus `BuildInstallerWix6`; an explicit WiX 3 alias is not required if `InstallerToolset=Wix3` remains documented.
- **Update documentation + CI** so WiX 6 is the primary migration path, and WiX 3 is described as transitional fallback.

### Success Criteria

- **Build Success**: WiX 6 online bundle, offline bundle, MSI, and `.wixpdb` files build successfully on developer machines and CI without a `genericinstaller` checkout.
- **Default Switch Ready**: `InstallerToolset=Wix6` is proven enough that the repo can make WiX 6 the default installer route, or the remaining blockers are explicitly tracked.
- **No Legacy Submodule**: The genericinstaller submodule remains removed, and no WiX 6 build or validation lane requires a `PatchableInstaller/` checkout.
- **Functional Parity**: The WiX 6 path provides the same installation options (features, locales, offline/online) as the previous version.
- **Modernization (WiX 6 path)**: WiX 6 build uses MSBuild + WiX 6 tools only, with no legacy batch scripts in the WiX 6 path.

### Assumptions

- The existing WiX 3.11 source code is largely compatible with WiX v6 or can be automatically converted/easily patched.
- The genericinstaller submodule content is available for migration.
- WiX v6 supports the specific prerequisites (e.g., .NET Framework versions) required by the application.
- The team accepts the "breaking changes" inherent in moving to WiX v6 (e.g., different CLI tools, different project file format).

### Patch/MSP Support (future / out of scope for first WiX 6 migration)

The first migration target is a working WiX 6 MSI + online/offline bundle using major upgrades. Patch infrastructure (MSP generation, `PatchBaseline`, base-build `.wixpdb` retention, and a WiX 6 `BuildPatch` target) is intentionally separate work.

Future patch work must prove:

- Component GUID and file identity stability across base/update builds.
- Base MSI and `.wixpdb` artifact retention for patch creation.
- A WiX 6 replacement for the legacy `PatchableInstaller/CreateUpdatePatch` and `buildPatch.bat` flow.
- MSP apply, repair, uninstall, and upgrade behavior on clean machines.

Until that work exists, `build.ps1 -BuildPatch -InstallerToolset Wix6` should be treated as unsupported.

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