# Tasks: WiX v6 Migration

**Feature**: WiX v6 Migration
**Status**: Planned
**Spec**: [specs/001-wix-v6-migration/spec.md](specs/001-wix-v6-migration/spec.md)

## Phase 1: Setup (Project Initialization)

**Goal**: Initialize the new WiX v6 project structure and migrate shared code from `genericinstaller`.

- [ ] T001 Create `FLExInstaller/Shared` directory structure
- [ ] T002 Copy `PatchableInstaller/BaseInstallerBuild` content to `FLExInstaller/Shared/Base`
- [ ] T003 Copy `PatchableInstaller/CustomActions` content to `FLExInstaller/Shared/CustomActions`
- [ ] T036 Copy `PatchableInstaller/ProcRunner` content to `FLExInstaller/Shared/ProcRunner`
- [ ] T004 Create `FLExInstaller/FieldWorks.Installer.wixproj` (SDK-style)
- [ ] T005 Create `FLExInstaller/FieldWorks.Bundle.wixproj` (SDK-style)
- [ ] T006 Create `Build/Installer.targets` skeleton

## Phase 2: Foundational (Blocking Prerequisites)

**Goal**: Convert core WiX source files and Custom Actions to be compatible with WiX v6.

- [ ] T007 [P] Convert `FLExInstaller/*.wxs` and `*.wxl` files to WiX v6 syntax using `wix convert`
- [ ] T008 [P] Convert `FLExInstaller/Shared/Base/*.wxs` and `*.wxl` files to WiX v6 syntax
- [ ] T009 [P] Update Custom Actions project to target .NET 4.8
- [ ] T037 [P] Update `ProcRunner` project to target .NET 4.8
- [ ] T010 Implement `DownloadPrerequisites` target in `Build/Installer.targets` (FR-011)
- [ ] T038 Configure `HarvestDirectory` in `FieldWorks.Installer.wixproj` to replace `heat.exe`
- [ ] T011 Port `GIInstallDirDlg.wxs` (Dual Directory UI) to WiX v6
- [ ] T012 Port `GICustomizeDlg.wxs` (Custom Feature Tree) to WiX v6
- [ ] T013 Verify `WixUI_DialogFlow.wxs` compatibility with `WixToolset.UI.wixext`

## Phase 3: User Story 1 - Developer Builds Installer (P1)

**Goal**: Enable developers to build the installer locally using MSBuild.

- [ ] T014 [US1] Implement `BuildInstaller` target in `Build/Installer.targets`
- [ ] T015 [US1] Configure `FieldWorks.Installer.wixproj` to reference `Shared` components
- [ ] T016 [US1] Configure `FieldWorks.Bundle.wixproj` to reference the MSI
- [ ] T017 [US1] Implement Code Signing logic in `FieldWorks.Installer.wixproj` (FR-009)
- [ ] T018 [US1] Implement Code Signing logic in `FieldWorks.Bundle.wixproj` (FR-009)
- [ ] T019 [US1] Verify local build produces `FieldWorks.msi` and `FieldWorks.exe`

## Phase 4: User Story 2 - CI Builds Installer (P1)

**Goal**: Automate the installer build in CI.

- [ ] T020 [US2] Update `.github/workflows/main.yml` (or relevant workflow) to install WiX v6
- [ ] T021 [US2] Update CI workflow to call `msbuild /t:BuildInstaller`
- [ ] T022 [US2] Configure CI environment variables for Code Signing
- [ ] T023 [US2] Verify CI build artifacts are uploaded

## Phase 5: User Story 3 - End User Installation (Online) (P1)

**Goal**: Ensure the installer works for online users (downloading prerequisites).

- [ ] T024 [US3] Configure Bundle `Chain` to enable downloading of prerequisites
- [ ] T025 [US3] Verify `Redistributables.wxi` URLs are valid and accessible
- [ ] T026 [US3] Test Online Install on clean VM (Manual Verification)
- [ ] T034 [US3] Verify localized installer builds (French, Spanish, etc.)
- [ ] T035 [US3] Test Major Upgrade from previous version (Optional)

## Phase 6: User Story 4 - End User Installation (Offline) (P2)

**Goal**: Ensure the installer works for offline users (bundled prerequisites).

- [ ] T027 [US4] Configure Bundle to support offline layout creation
- [ ] T028 [US4] Verify `msbuild /t:Publish` (or similar) creates offline layout
- [ ] T029 [US4] Test Offline Install on disconnected VM (Manual Verification)

## Phase 7: Polish & Cross-Cutting Concerns

**Goal**: Cleanup and documentation.

- [ ] T030 Remove `PatchableInstaller` directory
- [ ] T031 Remove `genericinstaller` submodule reference from `.gitmodules`
- [ ] T032 Update `FLExInstaller/COPILOT.md` with new build instructions
- [ ] T033 Update `ReadMe.md` with new build instructions

## Dependencies

1.  **Phase 1 & 2** must be completed before any User Story phases.
2.  **Phase 3 (US1)** is a prerequisite for **Phase 4 (US2)**.
3.  **Phase 3 (US1)** is a prerequisite for **Phase 5 (US3)** and **Phase 6 (US4)**.

## Parallel Execution Opportunities

- **T007, T008, T009** (Conversion tasks) can be done in parallel.
- **T011, T012** (UI Porting) can be done in parallel.
- **T017, T018** (Signing logic) can be done in parallel.

## Implementation Strategy

1.  **MVP**: Complete Phases 1, 2, and 3. This gives a working local build.
2.  **CI Integration**: Complete Phase 4.
3.  **Validation**: Complete Phases 5 and 6 to ensure end-user scenarios work.
4.  **Cleanup**: Complete Phase 7.
