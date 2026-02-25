# Tasks: WiX v6 Migration

**Feature**: WiX v6 Migration
**Status**: Planned
**Spec**: [specs/001-wix-v6-migration/spec.md](specs/001-wix-v6-migration/spec.md)

## Phase 1: Setup (Project Initialization)

**Goal**: Initialize the new WiX v6 project structure and migrate shared code from `genericinstaller`.

- [x] T001 Create `FLExInstaller/Shared` directory structure
- [x] T002 Copy `PatchableInstaller/BaseInstallerBuild` content to `FLExInstaller/Shared/Base`
- [x] T003 Copy `PatchableInstaller/CustomActions` content to `FLExInstaller/Shared/CustomActions`
- [x] T036 Copy `PatchableInstaller/ProcRunner` content to `FLExInstaller/Shared/ProcRunner`
- [x] T004 Create `FLExInstaller/wix6/FieldWorks.Installer.wixproj` (SDK-style)
- [x] T005 Create `FLExInstaller/wix6/FieldWorks.Bundle.wixproj` (SDK-style)
- [x] T006 Create `Build/Installer.targets` skeleton

## Phase 0b: Parallel WiX 3 + WiX 6 Transition (NEW)

**Goal**: Reintroduce WiX 3 installer inputs, keep WiX 6 available, and make WiX 3 the default build.

- [x] T080 Restore `Build/Installer.targets` from `release/9.3` as `Build/Installer.Wix3.targets` (preserve legacy batch flow)
- [x] T081 Restore `FLExInstaller/*.wxi` from `release/9.3` into `FLExInstaller/` root (WiX 3 stays in main path)
- [x] T082 Restore `PatchableInstaller/` tree from `release/9.3` (BaseInstallerBuild, Common, CustomActions, ProcRunner, CreateUpdatePatch, libs, resources, props/README)
- [x] T083 Move WiX 6 assets under `FLExInstaller/wix6/` (projects + shared authoring)
- [x] T084 Add `InstallerToolset=Wix3|Wix6` selection to `build.ps1` and `Build/Orchestrator.proj` (default **Wix3**)
- [x] T085 Add explicit targets `BuildInstallerWix3` and `BuildInstallerWix6` with `BuildInstaller` routed to Wix3 by default
- [x] T086 Ensure WiX 3 and WiX 6 include paths are isolated (root vs `FLExInstaller/wix6/Shared/`)
- [x] T087 Update documentation for dual builds (ReadMe, quickstart, `FLExInstaller/COPILOT.md`) and call out default Wix3
- [x] T088 Update CI to build Wix3 by default and add an opt-in Wix6 lane
- [x] T089 Add guardrails that fail the Wix3 build if WiX 6 namespaces/refs leak into Wix3 inputs
- [x] T090 Define MSBuild override properties for installer customization (FR-008) and document them for both toolsets

## Phase 2: Foundational (Blocking Prerequisites)

**Goal**: Convert core WiX source files and Custom Actions to be compatible with WiX v6.

- [x] T007 [P] Convert `FLExInstaller/*.wxs` and `*.wxl` files to WiX v6 syntax using `wix convert`
- [x] T008 [P] Convert `FLExInstaller/Shared/Base/*.wxs` and `*.wxl` files to WiX v6 syntax
- [x] T009 [P] Update Custom Actions project to target .NET 4.8
- [x] T037 [P] Update `ProcRunner` project to target .NET 4.8
- [x] T010 Implement `DownloadPrerequisites` target in `Build/Installer.targets` (FR-011)
- [x] T038 Replace WiX 3 `heat.exe` harvesting with a pinned WixToolset.Heat v6 (NuGet-delivered) approach
- [x] T011 Port `GIInstallDirDlg.wxs` (Dual Directory UI) to WiX v6
- [x] T012 Port `GICustomizeDlg.wxs` (Custom Feature Tree) to WiX v6
- [x] T013 Verify `WixUI_DialogFlow.wxs` compatibility with `WixToolset.UI.wixext`

### Phase 2a: Toolchain Purge (WiX 6-only)

**Goal**: Remove all remaining WiX 3 build-time tool dependencies and legacy build scripts from the active build path.

- [x] T040 Ensure installer build does not rely on WiX 3 binaries on PATH (harvesting uses WixToolset.Heat v6 from NuGet)
- [x] T041 Remove reliance on `candle.exe`/`light.exe`/`insignia.exe` in any invoked build step (MSBuild/CI)
- [x] T042 Remove or quarantine legacy `build*.bat` scripts under `FLExInstaller/Shared/Base/` so they cannot be mistaken as source-of-truth
- [x] T043 Add a pinned, reproducible acquisition path for WiX 6 tooling (NuGet/MSBuild SDK) used by both local builds and CI

## Phase 3: User Story 1 - Developer Builds Installer (P1)

**Goal**: Enable developers to build the installer locally using MSBuild.

- [x] T014 [US1] Implement `BuildInstaller` target in `Build/Installer.targets`
- [x] T015 [US1] Configure `FieldWorks.Installer.wixproj` to reference `Shared` components
- [x] T016 [US1] Configure `FieldWorks.Bundle.wixproj` to reference the MSI
- [x] T017 [US1] Implement Code Signing logic in `FieldWorks.Installer.wixproj` (FR-009)
- [x] T018 [US1] Implement Code Signing logic in `FieldWorks.Bundle.wixproj` (FR-009)
- [x] T019 [US1] Verify local build produces `FieldWorks.msi` and `FieldWorksBundle.exe`

### Phase 3a: Local Verification (WiX6-only proof)

**Goal**: Prove a developer can build the installer without any external `genericinstaller` repo and without WiX 3 installed.

- [ ] T044 [US1] Verify build succeeds on a machine with *no* `genericinstaller` checkout present
- [ ] T045 [US1] Verify build succeeds with *no* WiX 3 installed (or at minimum: WiX 3 tools not present on PATH)
- [x] T046 [US1] Record artifact locations + names (MSI, bundle EXE, wixpdb) in a short checklist under this spec
- [x] T066 [US1] Add small-grained installer artifact verification tests (non-installing): MSI properties + artifact presence

## Phase 4: User Story 2 - CI Builds Installer (P1)

**Goal**: Automate the installer build in CI.

- [ ] T020 [US2] Update `.github/workflows/main.yml` (or relevant workflow) to install WiX v6
- [ ] T021 [US2] Update CI workflow to call `msbuild Build/Orchestrator.proj /t:BuildInstaller`
- [ ] T022 [US2] Configure CI environment variables for Code Signing
- [ ] T023 [US2] Verify CI build artifacts are uploaded

### Phase 4a: CI Verification (Parity + provenance)

**Goal**: CI proves the installer is fully WiX 6-based and does not rely on external templates.

- [ ] T047 [US2] Add a CI check that fails if the build invokes WiX 3 tools (`heat.exe`, `candle.exe`, `light.exe`, `insignia.exe`)
- [ ] T048 [US2] Add a CI check that fails if any build step requires a `PatchableInstaller/` directory or `genericinstaller` checkout

## Phase 5: User Story 3 - End User Installation (Online) (P1)

**Goal**: Ensure the installer works for online users (downloading prerequisites).

- [ ] T024 [US3] Configure Bundle `Chain` to enable downloading of prerequisites
- [ ] T025 [US3] Verify `Redistributables.wxi` URLs are valid and accessible
- [ ] T026 [US3] Test Online Install on clean VM (Manual Verification)
- [ ] T034 [US3] Verify localized installer builds (French, Spanish, etc.)
- [ ] T035 [US3] Test Major Upgrade from previous version (Optional)

### Phase 5a: Online Install Validation (Behavioral parity)

**Goal**: Validate installer UX + behavior parity (paths, registry, shortcuts, env vars) for the online scenario.

- [ ] T049 [US3] Validate dual-directory UX (App + Data) on clean VM (choose non-default paths; verify install succeeds)
- [ ] T050 [US3] Validate feature selection UI (select/deselect features; verify payload matches selection)
- [ ] T051 [US3] Validate registry keys/values are written and removed on uninstall (including upgrade scenarios)
- [ ] T052 [US3] Validate shortcuts (desktop + start menu + uninstall) and URL protocol registration (`silfw:`)
- [ ] T053 [US3] Validate environment variables are set and removed correctly (including PATH modifications)
- [ ] T054 [US3] Validate custom action behavior (app-close, path checks) and confirm no modal assertion UI blocks installs

## Phase 6: User Story 4 - End User Installation (Offline) (P2)

**Goal**: Ensure the installer works for offline users (bundled prerequisites).

- [ ] T027 [US4] Configure Bundle to support offline layout creation
- [ ] T028 [US4] Verify `msbuild /t:Publish` (or similar) creates offline layout
- [ ] T029 [US4] Test Offline Install on disconnected VM (Manual Verification)

### Phase 6a: Offline Bundle Wiring (WiX 6)

**Goal**: Produce an offline-capable bundle from WiX 6 build outputs (not legacy candle/light scripts).

- [ ] T055 [US4] Wire `OfflineBundle.wxs` into the WiX 6 build (second bundle project or conditional compile/output)
- [ ] T056 [US4] Define the offline distribution format (single EXE vs layout folder) and make it reproducible via MSBuild target
- [ ] T057 [US4] Verify offline prerequisites are embedded/available (NetFx, VC++ redists, FLEx Bridge offline)

## Phase 7: Polish & Cross-Cutting Concerns

**Goal**: Cleanup and documentation.

- [x] T030 Remove `PatchableInstaller` directory
- [x] T031 Remove `genericinstaller` submodule reference from `.gitmodules`
- [x] T032 Update `FLExInstaller/COPILOT.md` with new build instructions
- [x] T033 Update `ReadMe.md` with new build instructions
- [x] T039 Create WiX 3.x → WiX 6 parity audit report (`specs/001-wix-v6-migration/wix3-to-wix6-audit.md`)

### Phase 7a: Remove External Template Dependency (genericinstaller)

**Goal**: Ensure the repository no longer instructs or requires developers/CI to fetch `genericinstaller`/`PatchableInstaller`.

- [x] T058 Remove any developer setup steps that clone/expect `PatchableInstaller` (e.g., `Setup-Developer-Machine.ps1`)
- [x] T059 Remove workspace configuration references that pull `genericinstaller` into the worktree (optional but recommended)
- [x] T060 Repo-wide verification: no remaining references to `PatchableInstaller` in active build paths (docs/scripts/targets)

## Phase 8: Validation & Regression Proof

**Goal**: Provide repeatable evidence that WiX 6 implementation fully replaces the legacy template and behaves identically for users.

- [x] T061 Create verification matrix: `specs/001-wix-v6-migration/verification-matrix.md`
- [x] T062 Add golden install checklist: `specs/001-wix-v6-migration/golden-install-checklist.md`
- [ ] T067 Create and run WiX3→WiX6 parity check procedure (`specs/001-wix-v6-migration/parity-check.md`)
- [ ] T063 Capture and archive installer logs for each scenario (MSI log + bundle log) and document where to find them
- [ ] T064 Smoke-test upgrade from a previous released FieldWorks version on VM and verify data-path lock behavior
- [ ] T065 Validate localization: build and run at least one non-English installer UI end-to-end

### Phase 8a: Sustainable Clean-Machine Verification (CI)

**Goal**: Make clean-machine installer verification repeatable and sustainable.

- [x] T068 Adopt C# 8 language version repo-wide (keep nullable disabled initially)
- [ ] T069 Add CI installer verification on `windows-latest` (build installer, run installer check, upload evidence artifacts)
- [x] T071 Add installer evidence tooling scripts (snapshot + compare + check) and wire them into VS Code tasks

### Phase 8b: Local Development PC Verification (No Sandbox/Hyper-V)

**Goal**: Validate WiX3 and WiX6 installers directly on the local development PC and capture evidence without sandboxing.

- [ ] T091 Define local validation checklist for Wix3 and Wix6 (install/upgrade/uninstall + log capture on dev PC)
- [ ] T092 Run Wix3 default installer locally and archive MSI/bundle logs
- [ ] T093 Run Wix6 opt-in installer locally and archive MSI/bundle logs
- [ ] T094 Document evidence locations and expectations for local-only testing in spec docs


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
