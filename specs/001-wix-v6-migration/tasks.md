# Tasks: WiX v6 Migration

**Feature**: WiX v6 Migration
**Status**: In Progress - WiX 6-first migration, legacy WiX 3 fallback still present
**Spec**: [specs/001-wix-v6-migration/spec.md](specs/001-wix-v6-migration/spec.md)

## Phase 1: Setup (Project Initialization)

**Goal**: Initialize the new WiX v6 project structure and migrate shared code from `genericinstaller`.

- [x] T001 Create `FLExInstaller/wix6/Shared` directory structure
- [x] T002 Copy/migrate legacy `BaseInstallerBuild` content to `FLExInstaller/wix6/Shared/Base`
- [x] T003 Copy/migrate legacy `CustomActions` content to `FLExInstaller/wix6/Shared/CustomActions`
- [x] T036 Copy/migrate legacy `ProcRunner` content to `FLExInstaller/wix6/Shared/ProcRunner`
- [x] T004 Create `FLExInstaller/wix6/FieldWorks.Installer.wixproj` (SDK-style)
- [x] T005 Create `FLExInstaller/wix6/FieldWorks.Bundle.wixproj` (SDK-style)
- [x] T006 Create `Build/Installer.targets` skeleton

## Phase 0b: Parallel WiX 3 + WiX 6 Transition

**Goal**: Keep the WiX 3 path available as a temporary fallback while finishing the WiX 6 migration and preparing to make WiX 6 the default installer path.

- [x] T080 Restore `Build/Installer.targets` from `release/9.3` as `Build/Installer.Wix3.targets` (preserve legacy batch flow)
- [x] T081 Restore `FLExInstaller/*.wxi` from `release/9.3` into `FLExInstaller/` root (WiX 3 stays in main path)
- [x] T082 Keep `PatchableInstaller/` out of the worktree; migrate required genericinstaller behavior into `FLExInstaller/wix6/` instead
- [x] T083 Move WiX 6 assets under `FLExInstaller/wix6/` (projects + shared authoring)
- [x] T084 Add `InstallerToolset=Wix3|Wix6` selection to `build.ps1` and `Build/InstallerBuild.proj` (current default **Wix3**)
- [x] T085 Route `BuildInstaller` through the selected toolset and add `BuildInstallerWix6` as an explicit WiX 6 entry point
- [x] T086 Ensure WiX 3 and WiX 6 include paths are isolated (root vs `FLExInstaller/wix6/Shared/`)
- [x] T087 Update documentation for dual builds (ReadMe, quickstart, `FLExInstaller/COPILOT.md`) and call out current Wix3 default
- [x] T088 Update CI to build WiX 6 as the migration lane, with any WiX 3 jobs clearly marked legacy/transition
- [x] T089 Add guardrails that fail the Wix3 build if WiX 6 namespaces/refs leak into Wix3 inputs
- [x] T090 Define MSBuild override properties for installer customization (FR-008) and document them for both toolsets
- [ ] T095 Decide and implement the default switch from `InstallerToolset=Wix3` to `InstallerToolset=Wix6`, or document the release blocker keeping WiX 3 as default — **Decision: Keep WiX 3 as default for now.** Remaining release blocker: T026 (clean VM install not yet validated). T022 is resolved (`wix6-installer-cd.yml` created with `wix burn detach/reattach` signing). Once T026 passes, change line 19 of `Build/InstallerBuild.proj` from `Wix3` to `Wix6` and update docs. Once T022 is done and T026 passes, change line 19 of `Build/InstallerBuild.proj` from `Wix3` to `Wix6` and update docs.

## Phase 2: Foundational (Blocking Prerequisites)

**Goal**: Convert core WiX source files and Custom Actions to be compatible with WiX v6.

- [x] T007 [P] Convert `FLExInstaller/*.wxs` and `*.wxl` files to WiX v6 syntax using `wix convert`
- [x] T008 [P] Convert `FLExInstaller/wix6/Shared/Base/*.wxs` and `*.wxl` files to WiX v6 syntax
- [x] T009 [P] Update Custom Actions project to target .NET 4.8
- [x] T037 [P] Update `ProcRunner` project to target .NET 4.8
- [x] T010 Implement `DownloadPrerequisites` target in `Build/Installer.targets` (FR-011)
- [x] T038 Replace WiX 3 `heat.exe` harvesting with a pinned WixToolset.Heat v6 (NuGet-delivered) approach
- [x] T011 Port `GIInstallDirDlg.wxs` (Dual Directory UI) to WiX v6
- [x] T012 Port `GICustomizeDlg.wxs` (Custom Feature Tree) to WiX v6
- [x] T013 Verify `WixUI_DialogFlow.wxs` compatibility with `WixToolset.UI.wixext`

### Phase 2a: Toolchain Purge (WiX 6-only)

**Goal**: Remove all remaining WiX 3 build-time tool dependencies and legacy build scripts from the WiX 6 active build path.

- [x] T040 Ensure installer build does not rely on WiX 3 binaries on PATH (harvesting uses WixToolset.Heat v6 from NuGet)
- [x] T041 Remove reliance on `candle.exe`/`light.exe`/`insignia.exe` in any invoked build step (MSBuild/CI)
- [x] T042 Remove or quarantine legacy `build*.bat` scripts under `FLExInstaller/wix6/Shared/Base/` so they cannot be mistaken as source-of-truth
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

- [x] T044 [US1] Verify build succeeds on a machine with *no* `genericinstaller` checkout present
- [x] T045 [US1] Verify build succeeds with *no* WiX 3 installed (or at minimum: WiX 3 tools not present on PATH) — Build log (`Output/InstallerEvidence/wix6-installer-build.Installer.log`) confirmed zero invocations of `candle.exe`/`light.exe`/`insignia.exe`. Local dev machine has WiX 3 at `C:\Users\johnm\AppData\Local\FieldWorksTools\Wix314` on PATH but the build ignores them. CI.yml `wix6_installer_build` enforces `-RequireNoWix3ToolsOnPath` on clean runner (no WiX 3 installed).
- [x] T046 [US1] Record artifact locations + names (MSI, bundle EXE, wixpdb) in a short checklist under this spec
- [x] T066 [US1] Add small-grained installer artifact verification tests (non-installing): MSI properties + artifact presence

## Phase 4: User Story 2 - CI Builds Installer (P1)

**Goal**: Automate the installer build in CI.

- [x] T020 [US2] Update CI workflow to include WiX v6 — Done: `wix6_installer_build` job in `CI.yml` installs WiX 6 via NuGet during MSBuild restore; no separate tool install needed.
- [x] T021 [US2] Update CI workflow to call `build.ps1 -BuildInstaller -InstallerToolset Wix6` — Done: `CI.yml` line 127.
- [x] T022 [US2] Configure code signing for WiX 6 release workflow — Created `.github/workflows/wix6-installer-cd.yml`: builds with `FILESTOSIGNLATER=./signExternally` + `/p:SignOutput=true`; uses `wix burn detach`/`wix burn reattach` (WiX 6 replacement for `insignia -ib`/`-ab`) plus `sillsdev/codesign/trusted-signing-action@v3` for engine and bundle signing. Requires `TRUSTED_SIGNING_CREDENTIALS`, `AWS_ACCESS_KEY_ID`, and `AWS_SECRET_ACCESS_KEY` secrets (already used by WiX 3 lane). No `PatchableInstaller` needed.
- [x] T023 [US2] Verify CI build artifacts are uploaded — Done: `CI.yml` uploads MSI, online/offline bundles, `.wixpdb`, build log, and evidence artifacts.

### Phase 4a: CI Verification (Parity + provenance)

**Goal**: CI proves the installer is fully WiX 6-based and does not rely on external templates.

- [x] T047 [US2] Add a CI check that fails if the WiX 6 build invokes legacy WiX 3 tools (`candle.exe`, `light.exe`, `insignia.exe`) or an unpinned system `heat.exe`
- [x] T048 [US2] Add a CI check that fails if any build step requires a `PatchableInstaller/` directory or `genericinstaller` checkout
- [x] T096 [US2] Add a WiX 6 installer CI job that runs `./build.ps1 -BuildInstaller -InstallerToolset Wix6` and uploads MSI, online bundle, offline bundle, and `.wixpdb` artifacts

## Phase 5: User Story 3 - End User Installation (Online) (P1)

**Goal**: Ensure the installer works for online users (downloading prerequisites).

- [x] T024 [US3] Configure Bundle `Chain` to enable downloading of prerequisites — Done: `Bundle.wxs` uses `<PackageGroupRef Id="NetFx48Web" />` (WixToolset.NetFx.wixext, downloads .NET 4.8 from Microsoft CDN at runtime). vcredists and FLExBridge have `DownloadUrl` attributes and are also pre-staged/embedded at build time. FLExBridge offline prereq is embedded via `Compressed="yes"` for robust offline install.
- [x] T025 [US3] Verify `Redistributables.wxi` URLs are valid and accessible — All 5 vcredist DownloadUrls return HTTP 200 (verified locally). `NetFx48Web` URL is managed by WixToolset.NetFx.wixext (not in-repo). All prereq files also successfully downloaded during offline bundle build (confirmed by 1.7 GB artifact).
- [ ] T026 [US3] Test Online Install on clean VM (Manual Verification)
- [ ] T034 [US3] Verify localized installer builds (French, Spanish, etc.) — **Gap**: WiX 6 build currently produces only `en-US` MSI (`FLExInstaller/wix6/bin/x64/Release/en-US/FieldWorks.msi`). Multi-culture support requires: (1) Add `.wxl` files per target culture to `FLExInstaller/wix6/Shared/`; (2) Set `Cultures` property in `FieldWorks.Installer.wixproj`; (3) Update `Build/Installer.targets` to invoke multi-culture builds; (4) Verify output structure. WiX 3 used a `Localize` MSBuild target (see `Build/Installer.legacy.targets` line 93). This is a P2 parity gap.
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

- [x] T027 [US4] Configure WiX 6 offline bundle authoring to embed local prerequisites
- [ ] T028 [US4] Verify the offline bundle artifact installs successfully without network access
- [ ] T029 [US4] Test Offline Install on disconnected VM (Manual Verification)

### Phase 6a: Offline Bundle Wiring (WiX 6)

**Goal**: Produce an offline-capable bundle from WiX 6 build outputs (not legacy candle/light scripts).

- [x] T055 [US4] Wire `OfflineBundle.wxs` into the WiX 6 build via `FieldWorks.OfflineBundle.wixproj` and `Build/Installer.targets`
- [x] T056 [US4] Define the offline distribution format as a single `FieldWorksOfflineBundle.exe` artifact produced by MSBuild
- [x] T057 [US4] Verify offline prerequisites are embedded/available (NetFx, VC++ redists, FLEx Bridge offline)

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
- [ ] T059 Remove workflow references that pull `genericinstaller` into the worktree (`base-installer-cd.yml`, `patch-installer-cd.yml`) — **Blocked by T095/T022.** The legacy WiX 3 workflows legitimately need genericinstaller for WiX 3 builds. Removing these checkouts must wait until WiX 6 becomes the default release lane (T095 resolved) and the WiX 3 workflows are decommissioned. The WiX 6 CI job (`CI.yml` `wix6_installer_build`) already has no genericinstaller reference.
- [x] T060 Repo-wide verification: no remaining references to `PatchableInstaller` in WiX 6 active build paths, CI jobs, and developer setup docs — Done: `CI.yml` `wix6_installer_build` job has no `PatchableInstaller` checkout; `FLExInstaller/wix6/` has no references; `Build/Installer.targets` (WiX 6 path) has no references. `Setup-Developer-Machine.ps1 -InstallerDeps` still clones genericinstaller→PatchableInstaller but only for WiX 3 builds (gated by flag, not in WiX 6 path). `Test-Wix6InstallerBuildEvidence.ps1` actively checks and fails if PatchableInstaller directory is present.
- [x] T097 Retire or clearly label legacy WiX 3 CI jobs that still checkout `sillsdev/genericinstaller` as `PatchableInstaller/`

## Phase 8: Validation & Regression Proof

**Goal**: Provide repeatable evidence that WiX 6 implementation fully replaces the legacy template and behaves identically for users.

- [x] T061 Create verification matrix: `specs/001-wix-v6-migration/verification-matrix.md`
- [x] T062 Add golden install checklist: `specs/001-wix-v6-migration/golden-install-checklist.md`
- [x] T067 Create and run WiX3→WiX6 parity check procedure (`specs/001-wix-v6-migration/parity-check.md`) — Procedure document created; partial results recorded. Toolchain/build parity: all ☑ (no genericinstaller, no WiX3 PATH, correct artifact names, staging roots). Upgrade/uninstall parity: ☑ (T098/T099/T093). Remaining checklist items (MSI payload, UX, env vars, custom actions, clean VM) require VM validation (T049-T054, T064).
- [x] T063 Capture and archive installer logs for each scenario (MSI log + bundle log) and document where to find them — Logs captured and documented in `Output/InstallerEvidence/`. Key artifacts: `wix6-quiet-install/bundle.log` + `bundle_001_AppMsiPackage.log` (quiet install+upgrade, T093/T098); `wix6-quiet-install-uninstall/bundle.log` (quiet uninstall, T093); `wix6-installer-build.Installer.log` (Release build, T045); `wix6-release-build-audit.txt` (audit evidence). Remaining scenarios not yet logged: WiX 3 baseline (T092, blocked), upgrade from released FW (T064, needs VM), offline bundle (T028, needs VM).
- [ ] T064 Smoke-test upgrade from a previous released FieldWorks version on VM and verify data-path lock behavior
- [ ] T065 Validate localization: build and run at least one non-English installer UI end-to-end — **Blocked by T034** (multi-culture WiX 6 build not yet implemented). WiX 6 currently only produces `en-US` MSI.

### Phase 8a: Sustainable Clean-Machine Verification (CI)

**Goal**: Make clean-machine installer verification repeatable and sustainable.

- [x] T068 Adopt C# 8 language version repo-wide (keep nullable disabled initially)
- [x] T069 Add CI installer verification on `windows-latest` (build installer, run installer check, upload evidence artifacts)
- [x] T071 Add installer evidence tooling scripts (snapshot + compare + check) and wire them into VS Code tasks

### Phase 8b: Local Development PC Verification (No Sandbox/Hyper-V)

**Goal**: Validate WiX3 and WiX6 installers directly on the local development PC and capture evidence without sandboxing.

- [x] T091 Define local validation checklist for Wix3 and Wix6 (install/upgrade/uninstall + log capture on dev PC)
- [ ] T092 Run Wix3 default installer locally and archive MSI/bundle logs — Blocked: local worktree does not have `PatchableInstaller/` (`genericinstaller`) cloned, which is required for the WiX 3 `InstallerDir` build path. To proceed: run `Setup-Developer-Machine.ps1 -InstallerDeps` to clone PatchableInstaller, then `.\build.ps1 -BuildInstaller -InstallerToolset Wix3 -Configuration Release`, then `Invoke-Installer.ps1`. Previous 9.3.8.1 WiX3 bundles were successfully installed (evidenced by WiX6 upgrade test T098 detecting and uninstalling them).
- [x] T093 Run Wix6 opt-in installer locally and archive MSI/bundle logs
- [x] T094 Document evidence locations and expectations for local-only testing in spec docs

### Phase 9: WiX 6 Upgrade and Patch Follow-up

**Goal**: Separate the major-upgrade migration from future MSP patch work so the WiX 6 migration can finish without pretending patch support already exists.

- [x] T098 [US3] Validate WiX 3 to WiX 6 upgrade detection — VALIDATED: quiet install evidence shows both WiX 3 bundles (`FieldWorks_9.3.8.1_Offline.exe` at `{b51d3664-...}` and `{fb50e03d-...}`) detected as Upgrade type and uninstalled by Burn before WiX 6 installed. No extra `<Provides Key>` required.
- [x] T099 [US3] Validate same-version major upgrades from WiX 6 dev builds do not create side-by-side installs — VALIDATED: second quiet install of same 9.3.9.1 bundle detected `AppMsiPackage: state: Present, execute: None`; no side-by-side install.
- [x] T100 [US3] Diagnose ARP duplicate package entries and uninstall hang — DIAGNOSED: MSI has `ARPSYSTEMCOMPONENT=1` (hidden from Programs and Features); only the bundle entry "FieldWorks Installer" is visible to users — no user-visible duplicate. Uninstall via bundle: exit 0, 5604 files cleanly removed. Snapshot script had `$matches` automatic-variable collision bug (fixed to `$isMatch` in `Collect-InstallerSnapshot.ps1`).
- [ ] T101 Define a separate WiX 6 patch/MSP design: `PatchBaseline`, base `.wixpdb` retention, component GUID stability, and a replacement for legacy `buildPatch.bat` — **Future work (post WiX 6 release).** Key design decisions needed: (1) MSP-based patch vs. full-bundle upgrade-only approach; (2) `.wixpdb` archival process for each release (needed as baseline for patch comparison); (3) Component GUID stability guarantees across builds; (4) New `wix6-patch-cd.yml` workflow to replace `patch-installer-cd.yml`. The WiX 6 SDK supports `wix msi patch` and `wix burn` commands for patching. Blocked: requires at least two shipped WiX 6 releases to patch between.
- [ ] T102 Implement WiX 6 `BuildPatch` only after T101 is approved and validated — **Future work (post WiX 6 release).**


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
