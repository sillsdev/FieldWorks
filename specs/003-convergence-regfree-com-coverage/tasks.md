# Tasks: RegFree COM Coverage Completion

**Input**: Design documents from `/specs/003-convergence-regfree-com-coverage/`
**Prerequisites**: plan.md (required), spec.md (required for user stories)

**Tests**: Only required where explicitly stated; each user story lists its independent validation criteria.

**Organization**: Tasks are grouped by user story (US1–US4) so every increment is independently testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Task can run in parallel (different files, no sequencing dependency)
- **[Story]**: User story label (e.g., [US1]) for phases 3+.
- Include exact file paths in every description

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish directories, ignore rules, and baseline docs required by all streams.

- [X] T001 Create `scripts/regfree/README.md` describing the audit/manifest/validation toolchain usage pattern.
- [X] T002 Add `scripts/regfree/__init__.py` so shared helpers can be imported across scripts.
- [X] T003 Create `specs/003-convergence-regfree-com-coverage/artifacts/.gitkeep` to keep the artifacts folder under version control.
- [X] T004 Update `.gitignore` to exclude `specs/003-convergence-regfree-com-coverage/artifacts/*.{csv,json,log}` and VM output folders.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Provide shared metadata, automation harnesses, and documentation needed before any user story work begins.

- [X] T005 [P] Create `scripts/regfree/common.py` containing the executable metadata table (FieldWorks, LCMBrowser, UnicodeCharEditor, MigrateSqlDbs, FixFwData, FxtExe, ConverterConsole, Converter, ConvertSFM, SfmStats) with `Id`, `ProjectPath`, `OutputPath`, and `Priority` fields sourced from plan.md.
- [X] T006 [P] Export the metadata into `scripts/regfree/project_map.json` for downstream scripts to consume without importing Python modules.
- [X] T007 [P] Author `scripts/regfree/run-in-vm.ps1` that copies a payload (EXE, manifest, dependencies) into the clean VM checkpoint and runs launch/CLI smoke tests while capturing console output.
- [X] T008 [P] Document artifact formats, retention rules, and log naming inside `specs/003-convergence-regfree-com-coverage/artifacts/README.md`.
- [X] T009 Seed `tests/Integration/RegFreeCom/README.md` with container validation instructions, clean VM steps, and log locations shared by every user story.

**Checkpoint**: Once Phase 2 completes, the shared metadata and VM harness unblock all user stories.

---

## Phase 3: User Story 1 – Evidence-Ready COM Audit (Priority P1)

**Goal**: As the release engineer, I need an automated COM usage audit with durable evidence so we know exactly which executables require manifests.

**Independent Test**: Run `python scripts/regfree/audit_com_usage.py --repo-root . --output-dir specs/003-convergence-regfree-com-coverage/artifacts` and verify the generated CSV/logs capture LCMBrowser + UnicodeCharEditor indicators plus at least one utility executable.

### Implementation

- [X] T010 [P] [US1] Implement `scripts/regfree/audit_com_usage.py` per the design contracts (indicator scanning, CSV/log emission, exit codes for "needs manual review").
- [X] T011 [P] [US1] Add detection fixtures in `tests/Integration/RegFreeCom/test_audit_com_usage.py` that feed sample `.cs` snippets and assert indicator tallies for LCMBrowser- and utility-style code paths.
- [X] T012 [US1] Execute the audit script and store `com_usage_report.csv`, `com_usage_detailed.log`, and supporting JSON under `specs/003-convergence-regfree-com-coverage/artifacts/`.
- [X] T013 [US1] Summarize audit findings (including LexTextExe removal confirmation and evidence paths) in `specs/003-convergence-regfree-com-coverage/artifacts/com_usage_report.md` for stakeholders.

**Checkpoint**: CSV/log artifacts plus documentation provide auditable evidence of COM usage across all EXEs.

---

## Phase 4: User Story 2 – User-Facing Tools (LCMBrowser + UnicodeCharEditor) 🎯 MVP (Priority P1)

**Goal**: As a FieldWorks desktop user, I need LCMBrowser.exe and UnicodeCharEditor.exe to run on clean machines by shipping validated registration-free COM manifests.

**Independent Test**: Build both EXEs, confirm `Output/Debug/LCMBrowser.exe.manifest` and `Output/Debug/UnicodeCharEditor.exe.manifest` list required COM classes, then launch them on the clean VM and a developer machine (with COM registered) while exercising complex-script scenarios.

### Implementation

- [ ] T014 [P] [US2] Extend `Build/Src/FwBuildTasks/RegFree.cs` and `RegFreeCreator.cs` to support managed assemblies. The task should use Reflection to find `[ComVisible]` classes and `[Guid]` attributes, adding them to the manifest similar to how TypeLibs are processed.
- [ ] T015 [US2] Update `Src/LCMBrowser/LCMBrowser.csproj` (or `BuildInclude.targets`) to use the updated `RegFree` task, ensuring it processes the managed assembly itself.
- [ ] T016 [US2] Update `Src/UnicodeCharEditor/UnicodeCharEditor.csproj` to use the updated `RegFree` task.
- [ ] T017 [US2] Build both projects (`msbuild` Debug|x64) and capture the generated manifests + SHA256 checksums in `specs/003-convergence-regfree-com-coverage/artifacts/user-tools-manifests.md`.
- [ ] T018 [P] [US2] Implement `scripts/regfree/validate_regfree_manifests.py` covering XML checks, COM class verification, and hooks for VM payload generation. **Verify that the C# generated manifest matches the expected CLSIDs found by the Python audit scripts.**
- [ ] T019 [US2] Run `validate_regfree_manifests.py --executables Output/Debug/LCMBrowser.exe Output/Debug/UnicodeCharEditor.exe` and store `lcmbrowser_validation.log` + `unicodechareditor_validation.log` inside the artifacts directory.
- [ ] T020 [US2] Execute `scripts/regfree/run-in-vm.ps1` with both EXEs on the clean VM and append the observed steps/results to `tests/Integration/RegFreeCom/user-tools-vm.md`.
- [ ] T021 [US2] Perform developer-machine regression runs for both EXEs (with COM registered) to verify manifests take precedence; capture command logs + screenshots in `tests/Integration/RegFreeCom/user-tools-dev.md`.
- [ ] T022 [US2] Exercise complex-script sample projects (RTL + combining marks) on the clean VM and developer machine, documenting outcomes in `tests/Integration/RegFreeCom/user-tools-i18n.md`.
- [ ] T023 [US2] Update `Src/LCMBrowser/COPILOT.md` and `Src/UnicodeCharEditor/COPILOT.md` (or add justification notes) to reflect the new manifest wiring and validation artifacts.

**Checkpoint**: LCMBrowser.exe and UnicodeCharEditor.exe ship with verified manifests and documented clean-VM runs, establishing the MVP.

---

## Phase 5: User Story 3 – Migration Utilities Coverage (MigrateSqlDbs, FixFwData, FxtExe) (Priority P2)

**Goal**: As a support engineer, I need the migration utilities to run without registry COM dependencies and to document any NotRequired cases.

**Independent Test**: For each utility, run add/validate scripts, confirm manifests exist (or NotRequired evidence recorded), and execute binaries on both clean VM and developer machines while covering complex-script data sets.

### Implementation

- [ ] T024 [P] [US3] Extend `scripts/regfree/project_map.json` + `com_usage_report.csv` to include MigrateSqlDbs, FixFwData, and FxtExe audit evidence with priority tags.
- [ ] T025 [US3] Apply `add_regfree_manifest.py` to `Src/MigrateSqlDbs/MigrateSqlDbs.csproj`, ensuring `BuildInclude.targets` imports `Build/RegFree.targets` and the RegFree property is set.
- [ ] T026 [US3] Apply `add_regfree_manifest.py` to `Src/Utilities/FixFwData/FixFwData.csproj` with the same wiring.
- [ ] T027 [US3] Audit `Src/FXT/FxtExe/FxtExe.csproj`; if COM indicators exist, run the manifest script, otherwise annotate the NotRequired rationale in `specs/003-convergence-regfree-com-coverage/artifacts/com_usage_report.md`.
- [ ] T028 [US3] Build each executable above, then commit manifest details + checksums to `specs/003-convergence-regfree-com-coverage/artifacts/migration-utilities-manifests.md`.
- [ ] T029 [US3] Run `validate_regfree_manifests.py` for the utilities (or note NotRequired) and store per-EXE logs inside the artifacts folder.
- [ ] T030 [US3] Use `scripts/regfree/run-in-vm.ps1` to execute the utilities on the clean VM, capturing output/screenshots within `tests/Integration/RegFreeCom/migration-utilities-vm.md`.
- [ ] T031 [US3] Perform developer-machine regression + complex-script validation for each manifest-enabled utility, recording outputs in `tests/Integration/RegFreeCom/migration-utilities-dev.md`.
- [ ] T032 [US3] Update `Src/MigrateSqlDbs/COPILOT.md`, `Src/Utilities/FixFwData/COPILOT.md`, and `Src/FXT/FxtExe/COPILOT.md` to document the manifest status (or NotRequired rationale) with links to the validation evidence.

**Checkpoint**: Migration utilities either ship with manifests or have documented evidence explaining why they do not require one.

---

## Phase 6: User Story 4 – Supporting Utilities & Installer Parity (Priority P3)

**Goal**: As QA and installer engineers, we need complete documentation/installer integration covering low-priority utilities (ConvertSFM, SfmStats, Converter/ConverterConsole) and ensuring manifests are packaged everywhere required.

**Independent Test**: Installer build includes every needed manifest file (or marked as NotRequired), documentation tables list final status, clean/developer-machine runs succeed without COM registration errors, and complex-script usage is documented.

### Implementation

- [ ] T033 [P] [US4] Confirm audit results for `Src/Utilities/SfmToXml/ConvertSFM/ConvertSFM.csproj`, `Src/Utilities/SfmStats/SfmStats.csproj`, `Lib/src/Converter/Converter/Converter.csproj`, and `Lib/src/Converter/ConvertConsole/ConverterConsole.csproj`, annotating NotRequired evidence where appropriate inside `specs/003-convergence-regfree-com-coverage/artifacts/com_usage_report.md`.
- [ ] T034 [US4] For any supporting utility flagged as COM-using, run `add_regfree_manifest.py`, rebuild, and capture manifests/logs similar to earlier phases; otherwise record "NotRequired" justification files under `specs/003-convergence-regfree-com-coverage/artifacts/`.
- [ ] T035 [US4] Update `SDK-MIGRATION.md` with the final manifest coverage matrix, linking to the artifacts generated in this feature.
- [ ] T036 [US4] Modify `FLExInstaller/CustomComponents.wxi` (and related WiX fragments) to package every new `.exe.manifest` alongside its executable.
- [ ] T037 [US4] Rebuild the installer via `msbuild Build/Orchestrator.proj /t:BuildBaseInstaller /p:Configuration=Release` and capture the log at `specs/003-convergence-regfree-com-coverage/artifacts/installer-validation.log`.
- [ ] T038 [US4] Run the refreshed installer on the clean VM, recording step-by-step checks in `tests/Integration/RegFreeCom/installer-validation.md`.
- [ ] T039 [US4] Refresh `specs/003-convergence-regfree-com-coverage/quickstart.md` with the final command set (audit → manifest → validation → installer) and link to the produced artifacts.
- [ ] T040 [US4] Update `Docs/64bit-regfree-migration.md` with the completed manifest coverage, clean VM + dev-machine validation steps, and troubleshooting guidance.
- [ ] T041 [US4] Publish `specs/003-convergence-regfree-com-coverage/artifacts/com_usage_reference.md` summarizing each EXE, manifest status, COM classes, and evidence links.
- [ ] T042 [US4] Perform developer-machine regression + complex-script validation for supporting utilities/Converter binaries, capturing logs in `tests/Integration/RegFreeCom/supporting-utilities-dev.md`.
- [ ] T043 [US4] Update `Src/Utilities/SfmToXml/COPILOT.md`, `Src/Utilities/SfmStats/COPILOT.md`, `Lib/src/Converter/Converter/COPILOT.md`, and `Lib/src/Converter/ConvertConsole/COPILOT.md` (or equivalent docs) with the manifest/not-required outcomes.

**Checkpoint**: Documentation and installer deliverables reflect complete RegFree COM coverage for all executables.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanup, verification, and handoff tasks affecting multiple stories.

- [ ] T044 [P] Run the quickstart flow end-to-end and note the timestamp + outputs in `specs/003-convergence-regfree-com-coverage/quickstart.md` to confirm it stays accurate.
- [ ] T045 Consolidate final validation evidence into `specs/003-convergence-regfree-com-coverage/artifacts/ValidationRunSummary.md`, referencing every executable and installer result.
- [ ] T046 Perform a top-level traversal build (`msbuild FieldWorks.proj /m /p:Configuration=Debug /p:Platform=x64`) to ensure manifest wiring regresses nothing else; document the result in `tests/Integration/RegFreeCom/build-smoke.md`.
- [ ] T047 Add automated launch/manifest validation to CI (e.g., update `.github/workflows/*` or `Build/Agent/*` scripts) so `audit_com_usage.py`, `validate_regfree_manifests.py`, and `run-in-vm.ps1` gating steps run on PRs.

---

## Dependencies & Execution Order

1. **Setup (Phase 1)** → required before metadata/scripts exist.
2. **Foundational (Phase 2)** → depends on Setup; blocks every user story.
3. **User Stories (Phases 3–6)** → depend on Foundational completion; execute in priority order (US1/US2 as MVP, followed by US3, then US4).
4. **Polish (Phase 7)** → requires all targeted user stories to finish.

### User Story Dependencies

- **US1 (P1)**: Runs immediately after Foundational; no other story dependencies.
- **US2 (P1)**: Depends on US1 artifacts (audit data + metadata) to confirm LCMBrowser/UnicodeCharEditor requirements.
- **US3 (P2)**: Depends on US1 audit outputs and US2 scripts (`add_regfree_manifest.py`, validation) being available.
- **US4 (P3)**: Depends on US2 & US3 so installer/doc updates include the final manifest set.

### Parallel Opportunities

- Setup tasks T001–T004 can be split among contributors.
- Foundational tasks T005–T009 are [P]-marked where parallel-friendly.
- Within US1, T010–T012 can run concurrently once scripts and tests exist; documentation (T013) follows.
- For US2 and US3, running manifest script on different EXEs (T015–T032) can proceed in parallel after the script exists.
- US4 tasks T033–T043 have partial coupling: documentation updates (T035, T039–T041) can occur while installer authoring (T036–T038) proceeds.

---

## Implementation Strategy

### MVP Scope
- Complete Phases 1–4 (through US2) to ship LCMBrowser and UnicodeCharEditor with validated manifests plus the audit pipeline. This delivers immediate user value and unblocks downstream teams.

### Incremental Delivery
1. **Increment 1**: Setup + Foundational + US1 (audit evidence).
2. **Increment 2**: US2 (User-facing manifests) – declare MVP.
3. **Increment 3**: US3 (migration utilities) – extend coverage to medium-priority EXEs.
4. **Increment 4**: US4 + Polish – finalize installer/documentation and verification artifacts.

### Parallel Team Strategy
- Developer A focuses on scripts/common infrastructure.
- Developer B handles LCMBrowser/UnicodeCharEditor manifest work while Developer C starts utility manifests once US2 artifacts land.
- QA/Docs resources tackle US4 once US3 finishes.
