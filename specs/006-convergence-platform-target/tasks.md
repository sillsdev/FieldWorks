# Tasks: PlatformTarget Redundancy Cleanup

**Input**: Design documents in `/specs/006-convergence-platform-target/`
**Prerequisites**: `plan.md`, `spec.md`, optional `research.md`, `data-model.md`, `contracts/platform-target.yaml`, `quickstart.md`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Ensure the FieldWorks workstation and repo state are ready for convergence tooling.

- [x] T002 [P] Confirm `.git/HEAD` points to `specs/006-convergence-platform-target` and the working tree is clean via `git status`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Produce authoritative audit/decision artifacts that every user story depends on.

- [x] T003 Review `Directory.Build.props` to confirm the repo-wide `<PlatformTarget>x64</PlatformTarget>` baseline before editing any `.csproj`.
- [x] T004 [P] Run `python convergence.py platform-target audit --output specs/006-convergence-platform-target/platform_target_audit.csv` to capture the current explicit settings list.
- [x] T005 [P] Fill `specs/006-convergence-platform-target/platform_target_decisions.csv` with `Remove` vs `Keep` decisions (only FwBuildTasks stays AnyCPU) based on the audit output.

**Checkpoint**: Audit and decision CSVs reviewed so user story work can begin.

---

## Phase 3: User Story 1 - Remove Redundant PlatformTarget Entries (Priority: P1) 🎯 MVP

**Goal**: As a build maintainer, I want every SDK-style project to inherit platform settings from `Directory.Build.props` so future platform changes happen in one place.

**Independent Test**: `python convergence.py platform-target validate` reports `redundantCount = 0`, and `msbuild FieldWorks.proj /m /p:Configuration=Debug` succeeds.

### Implementation

- [x] T006 [US1] Execute `python convergence.py platform-target convert --decisions specs/006-convergence-platform-target/platform_target_decisions.csv` to remove redundant `<PlatformTarget>x64</PlatformTarget>` entries.
- [x] T007 [P] [US1] Review the diffs for every touched `Src/**/**/*.csproj` (plus `Build/Src/**/*.csproj`) to confirm only `PlatformTarget` nodes changed.
- [x] T008 [US1] Re-run `python convergence.py platform-target audit --output specs/006-convergence-platform-target/platform_target_audit.csv` to regenerate the audit evidence showing zero redundant entries after conversion.
- [x] T008a [P] [US1] Run `python convergence.py platform-target validate` and attach the CLI output that proves `redundantCount = 0` before starting MSBuild.
- [x] T009 [US1] Run `msbuild FieldWorks.proj /m /p:Configuration=Debug` from the repo root to ensure the traversal build still succeeds without explicit x64 properties.
- [x] T010 [P] [US1] Build the remaining AnyCPU tooling via `msbuild Build/Src/FwBuildTasks/FwBuildTasks.csproj` to confirm it continues loading under AnyCPU MSBuild hosts.

**Checkpoint**: Repo builds cleanly with zero redundant `<PlatformTarget>` declarations, confirmed by the refreshed audit CSV and a passing traversal build.

---

## Phase 4: User Story 2 - Document and Guard AnyCPU Exceptions (Priority: P2)

**Goal**: As the convergence owner, I need the remaining AnyCPU projects to document why they diverge from the x64 default so future audits can verify them quickly.

**Independent Test**: `git grep "<PlatformTarget>AnyCPU" Build Src -n` returns only FwBuildTasks, and the adjacent XML comment describes the build/test tooling rationale recorded in the spec package.

### Implementation

- [x] T011 [US2] Add an XML comment explaining the MSBuild-task hosting requirement next to `<PlatformTarget>AnyCPU</PlatformTarget>` in `Build/Src/FwBuildTasks/FwBuildTasks.csproj`.
- [x] T012 [P] [US2] Update the `FwBuildTasks` row in `specs/006-convergence-platform-target/platform_target_decisions.csv` with the rationale text and evidence link (matching the new XML comment).
- [x] T013 [US2] Update `specs/006-convergence-platform-target/spec.md` (Clarifications + Recommendation sections) with the final AnyCPU exception list and comment policy.
- [x] T014 [US2] Update `specs/006-convergence-platform-target/plan.md` (Constraints / Constitution sections) to reference the comment requirement and AnyCPU tooling rationale.
- [x] T015 [P] [US2] Extend `specs/006-convergence-platform-target/research.md` with pointers to the exact csproj line numbers and justification for each exception.
- [x] T016 [US2] Capture the `git grep "<PlatformTarget>AnyCPU" Build Src -n` output inside `specs/006-convergence-platform-target/platform_target_decisions.csv` (new evidence column) to prove only the documented FwBuildTasks exception remains.
- [x] T017 [US2] Update each affected `Src/**/AGENTS.md` (matching csproj paths listed under `Action=Remove` in `platform_target_decisions.csv`) so the inheritance policy change or the "no additional detail" reasoning is captured per folder.
  - *Note: No AGENTS.md files contained specific build property details, so no updates were required.*

**Checkpoint**: Exceptions are documented in-code and in design docs, and audits can flag regressions instantly.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Finalize documentation and reviewer aids.

- [x] T018 [P] Walk through every command in `specs/006-convergence-platform-target/quickstart.md` and align wording/flags with the commands actually executed (audit/convert/validate/build).
  - *Note: PACKAGE_MANAGEMENT_QUICKSTART.md does not reference PlatformTarget.*
- [x] T019 [P] Ensure `specs/006-convergence-platform-target/platform_target_audit.csv`, `platform_target_decisions.csv`, and `contracts/platform-target.yaml` travel together so reviewers can trace decisions end-to-end.
- [x] T020 Re-verify `specs/006-convergence-platform-target/contracts/platform-target.yaml` reflects the exact CLI flags/outputs observed during implementation and update descriptions if needed.

---

## Dependencies & Execution Order

1. **Setup → Foundational**: T001–T002 must complete before auditing; foundational tasks T003–T005 depend on a configured environment.
2. **Foundational → User Stories**: Both user stories rely on the audit/decision CSV pair created in T004–T005.
3. **Story Order**: US1 (T006–T010) must finish before US2 begins so documentation tasks describe the final state.
4. **Polish**: T018–T020 happen after both stories so every document references completed work.

## Parallel Opportunities

- T002, T004, T005, T007, T010, T012, T015, and T018–T019 act on distinct files and can run in parallel once their prerequisites finish.
- Within US1, conversion (T006) unblocks diff review (T007) while the follow-up audit (T008) and targeted builds (T010) run concurrently.
- Within US2, the csproj comment updates (T011–T012) and documentation tasks (T013–T015) can proceed simultaneously after US1 completes.

## Implementation Strategy

- **MVP Scope**: Completing Phase 3 (US1) delivers the minimum viable outcome—zero redundant `<PlatformTarget>` entries validated by tooling plus a passing traversal build.
- **Incremental Delivery**: After MVP, finish US2 to lock down AnyCPU exceptions, then close with the polish phase for reviewer aids.
- **Testing Cadence**: Invoke `python convergence.py platform-target validate` after conversions, `msbuild FieldWorks.proj` for integration, and `git grep "<PlatformTarget>AnyCPU" Build Src -n` to prove only the documented tools remain.
- **Regression Guards**: Keep `platform_target_audit.csv`, `platform_target_decisions.csv`, and the updated contract file under source control so future audits can diff against them quickly.

