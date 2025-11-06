# Tasks: FieldWorks 64-bit only + Registration-free COM

Branch: 001-64bit-regfree-com | Spec: specs/001-64bit-regfree-com/spec.md | Plan: specs/001-64bit-regfree-com/plan.md

This task list is organized by user story. Each task is specific and immediately executable. Use the checklist format to track progress.

---

## Phase 1 — Setup (infrastructure and policy)

- [x] T001 Ensure x64 defaults in root `Directory.Build.props` (set `<PlatformTarget>x64</PlatformTarget>` and `<Platforms>x64</Platforms>`) in `Directory.Build.props`
- [ ] T002 Remove Win32 configs from solution platforms in `FieldWorks.sln`
- [ ] T003 [P] Remove Win32 (and AnyCPU host) configurations from native VCXPROJ/C++-CLI projects tied to COM activation (keep x64 only) across `Src/**`
- [ ] T004 Enforce x64 in CI: update pipeline to pass `/p:Platform=x64` in `.github/workflows/CI.yml`
- [ ] T005 Audit build scripts for COM registration and remove calls (e.g., `regsvr32`/`DllRegisterServer`) in `Build/Installer.targets`
- [ ] T006 Document build/run instructions for x64-only and reg-free activation in `specs/001-64bit-regfree-com/quickstart.md`

## Phase 2 — Foundational (blocking prerequisites)

- [ ] T007 Verify and, if needed, adjust reg-free build target defaults (Platform, fragments) in `Build/RegFree.targets`
- [ ] T008 Ensure FieldWorks EXE triggers reg-free generation by including project BuildInclude if required in `Src/Common/FieldWorks/BuildInclude.targets`
- [ ] T009 Create BuildInclude for LexText EXE mirroring FieldWorks (imports `../../../Build/RegFree.targets`, AfterBuild depends on RegFree) in `Src/LexText/LexTextExe/BuildInclude.targets`
- [ ] T010 Wire LexText EXE to use its BuildInclude (as needed per project import conventions) in `Src/LexText/LexTextExe/LexTextExe.csproj`

## Phase 3 — User Story 1 (P1): Build and run without COM registration

Goal: Developers can build and run FieldWorks/LexText on a clean machine without administrator privileges; COM activates via manifests.

Independent Test: Build Debug|x64; launch core EXEs on a clean VM with no COM registrations; expect zero class-not-registered errors.

- [ ] T011 [P] [US1] Remove x86 PropertyGroups from FieldWorks project in `Src/Common/FieldWorks/FieldWorks.csproj`
- [ ] T012 [P] [US1] Remove x86 PropertyGroups from LexText project in `Src/LexText/LexTextExe/LexTextExe.csproj`
- [ ] T013 [P] [US1] Ensure FieldWorks manifest generation produces `<file>/<comClass>/<typelib>` entries (broad DLL include or dependent manifests) in `Build/RegFree.targets`
- [ ] T014 [P] [US1] Ensure LexText manifest generation produces `<file>/<comClass>/<typelib>` entries in `Build/RegFree.targets`
- [ ] T015 [US1] Run local smoke: build x64 and launch FieldWorks; capture and attach manifest in `Output/Debug/FieldWorks.exe.manifest`
- [ ] T016 [US1] Run local smoke: build x64 and launch LexText; capture and attach manifest in `Output/Debug/Flex.exe.manifest`

## Phase 4 — User Story 2 (P2): Ship and run as 64‑bit only

Goal: End users and QA receive x64-only builds; install/launch succeed without COM registration.

Independent Test: Build Release|x64, stage artifacts on a clean machine, launch without COM registration.

- [ ] T017 [P] [US2] Remove/disable COM registration steps in WiX includes (registration actions/registry table) in `FLExInstaller/CustomActionSteps.wxi`
- [ ] T018 [P] [US2] Remove/disable COM registration steps in WiX components (registry/value/provider bits) in `FLExInstaller/CustomComponents.wxi`
- [ ] T019 [P] [US2] Ensure generated EXE manifests are packaged intact by installer in `FLExInstaller/Redistributables.wxi`
- [ ] T020 [US2] Verify native COM DLLs remain co-located with the EXEs (installer output and build drop) to satisfy manifest `<file>` references across `Output/**` and installer staging
- [ ] T021 [US2] Update installer docs/notes to reflect reg-free COM and x64-only in `specs/001-64bit-regfree-com/quickstart.md`

## Phase 5 — User Story 3 (P3): CI builds x64-only, no registry writes

Goal: CI produces x64-only artifacts and does not perform COM registration; tests pass with reg-free manifests.

Independent Test: CI logs show `/p:Platform=x64`; no `regsvr32` invocations; EXE manifests present as artifacts; basic COM-activating tests pass.

- [ ] T022 [P] [US3] Enforce `/p:Platform=x64` and remove x86 matrix in `.github/workflows/CI.yml`
- [ ] T023 [P] [US3] Add CI step to upload EXE manifests for inspection in `.github/workflows/CI.yml`
- [ ] T024 [US3] Add CI smoke step: launch minimal COM scenario under test VM/container (no registry) in `.github/workflows/CI.yml`

## Phase 6 — Shared manifest-enabled test host (per plan FR‑011)

- [ ] T025 [P] Create new console host project for COM-activating tests in `Src/Utilities/ComManifestTestHost/ComManifestTestHost.csproj`
- [ ] T026 [P] Add Program.cs that activates a known COM class (no registry) in `Src/Utilities/ComManifestTestHost/Program.cs`
- [ ] T027 [P] Add BuildInclude that imports reg-free target and AfterBuild wiring in `Src/Utilities/ComManifestTestHost/BuildInclude.targets`
- [ ] T028 Add project to solution under Utilities group in `FieldWorks.sln`
- [ ] T029 Integrate host with test harness (invoke via existing test runner scripts) in `Bin/testWrapper.cmd`
- [ ] T030 [US3] Run COM-activating test suites under the new host, document ≥95% pass rate, and capture evidence in `specs/001-64bit-regfree-com/quickstart.md`

## Final Phase — Polish & Cross-cutting

- [ ] T031 Update `Docs/64bit-regfree-migration.md` with final plan changes and verification steps in `Docs/64bit-regfree-migration.md`
- [ ] T032 Re-run developer docs check and CI parity scripts in `Build/Agent` (no file change)
- [ ] T033 Add a short section to repo ReadMe linking to migration doc in `ReadMe.md`

---

## Dependencies (story completion order)

1. Phase 1 → Phase 2 → US1 (Phase 3)
2. US1 → US2 (installer packaging relies on working manifests)
3. US1 → US3 (CI validation relies on working manifests)
4. Test host (Phase 6) supports US3 smoke and future test migrations

## Parallel execution examples

- T011/T012 (remove x86 configs) can run in parallel
- T013/T014 (manifest wiring per EXE) can run in parallel
- T017–T020 (WiX and packaging adjustments) can run in parallel after US1
- T022/T023 (CI workflow updates) can run in parallel
- T025–T027 (test host project scaffolding) can run in parallel

## Implementation strategy (MVP first)

MVP is US1: enable reg-free COM and x64-only for FieldWorks.exe and LexText.exe on dev machines. Defer installer (US2) and CI validation (US3) until US1 is fully green.

---

## Format validation

All tasks follow the required checklist format: `- [ ] T### [P]? [USn]? Description with file path`.

## Summary

- Total tasks: 33
- Task count per user story: US1 = 6, US2 = 5, US3 = 4 (others are setup/foundational/polish)
- Parallel opportunities: 13 marked [P]
- Independent test criteria:
  - US1: Launch core EXEs on clean VM; no COM registration
  - US2: Install Release|x64 on clean machine; launch without COM registration
  - US3: CI logs show x64-only; manifests uploaded; smoke passes

