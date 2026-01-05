# Tasks: Convergence 004 – Test Exclusion Pattern Standardization

**Input**: Design documents from `/specs/004-convergence-test-exclusion-patterns/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Automated unit/CLI tests are required for each Python utility; no additional integration/UI tests requested.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1 = audit, US2 = conversion, US3 = validation/enforcement)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish script workspace, testing harness, and repo conventions before building tooling.

- [X] T001 Scaffold the new module layout in `scripts/test_exclusions/__init__.py` and `scripts/test_exclusions/README.md` to describe goals and usage conventions.
- [X] T002 Create a dedicated Python test harness by adding `scripts/tests/test_exclusions/__init__.py` and configuring `scripts/tests/conftest.py` for shared fixtures.
- [X] T003 Add tooling ignore entries for generated audit artifacts in `.gitignore` and document expected output folders inside `Output/test-exclusions/README.md`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core utilities that every user story depends on. Complete before any story-specific work.

- [X] T004 Implement shared dataclasses for `Project`, `TestFolder`, `ExclusionRule`, `ValidationIssue`, and `ConversionJob` in `scripts/test_exclusions/models.py` (aligned with `data-model.md`).
- [X] T005 Create an MSBuild XML parser helper in `scripts/test_exclusions/msbuild_parser.py` that can read/write `<Compile Remove>` and `<None Remove>` entries.
- [X] T006 Build a repository scanner utility in `scripts/test_exclusions/repo_scanner.py` to enumerate `.csproj` files and `*Tests` directories under `Src/`.
- [X] T007 [P] Add unit tests for the shared utilities in `scripts/tests/test_exclusions/test_models_and_scanner.py` covering enums, XML parsing, and folder detection edge cases.

**Checkpoint**: Foundation ready—user story work can proceed.

---

## Phase 3: User Story 1 – Repo-wide Audit (Priority: P1) 🎯 MVP

**Goal**: As a build engineer, I can audit every SDK-style project to see its current exclusion pattern, missing folders, and mixed-code flags.

**Independent Test**: Running `python scripts/audit_test_exclusions.py --output Output/test-exclusions/report.json` produces a CSV/JSON report listing each project, pattern type, and issues with exit code 0.

### Implementation

- [X] T008 [P] [US1] Implement the CLI entry point in `scripts/audit_test_exclusions.py` to load repo context, iterate via `repo_scanner`, and emit model instances.
- [X] T009 [US1] Add report serialization (CSV + JSON) in `scripts/test_exclusions/report_writer.py`, invoked by the audit command to persist `Output/test-exclusions/report.json` and `.csv`.
- [X] T010 [P] [US1] Extend the scanner to flag mixed production/test folders and surface them via `ValidationIssue` records stored in `Output/test-exclusions/report.json`.
- [X] T011 [US1] Create CLI-focused unit tests in `scripts/tests/test_exclusions/test_audit_command.py` (use fixture projects under `scripts/tests/fixtures/audit/`).
- [X] T012 [P] [US1] Update `specs/004-convergence-test-exclusion-patterns/quickstart.md` Audit section with the final CLI flags and sample output.
- [X] T028 [US1] Persist mixed-code escalations by writing `Output/test-exclusions/mixed-code.json` via `scripts/test_exclusions/escalation_writer.py` and generating a pre-filled issue template per violating project.
- [X] T029 [US1] Document the escalation workflow (owners, issue template link, required evidence) inside `specs/004-convergence-test-exclusion-patterns/quickstart.md` and `.github/instructions/managed.instructions.md`.

**Checkpoint**: Audit workflow provides actionable inventory (MVP complete).

---

## Phase 4: User Story 2 – Deterministic Conversion (Priority: P1)

**Goal**: As a build engineer, I can convert any project using Pattern B/C (or missing exclusions) to Pattern A through a scriptable workflow with safety checks.

**Independent Test**: Running `python scripts/convert_test_exclusions.py --input Output/test-exclusions/report.json --batch-size 10 --dry-run` prints planned edits; removing `--dry-run` rewrites targeted `.csproj` files and re-runs MSBuild for verification without introducing CS0436 errors.

### Implementation

- [X] T013 [P] [US2] Implement conversion logic in `scripts/convert_test_exclusions.py` that rewrites `.csproj` files using `msbuild_parser` utilities and honors the mixed-code stop policy.
- [X] T014 [US2] Add backup/rollback handling plus MSBuild invocation hooks inside `scripts/test_exclusions/converter.py` so each batch verifies builds locally before marking success.
- [X] T015 [P] [US2] Create regression tests in `scripts/tests/test_exclusions/test_converter.py` covering Pattern B → A replacement, nested folder entry insertion, and dry-run diffs.
- [X] T016 [US2] Document conversion workflow, batching strategy, and dry-run flags inside `specs/004-convergence-test-exclusion-patterns/quickstart.md` (Conversion section).
- [X] T017 [US2] Add a "Conversion Playbook" subsection to `.github/instructions/managed.instructions.md` explaining Pattern A expectations with before/after examples.
- [X] T032 [US2] After each conversion batch, rerun `python scripts/audit_test_exclusions.py --output Output/test-exclusions/report.json` (and CSV equivalent) so `patternType` values reflect the new state and can seed the next conversion run.

**Checkpoint**: Conversion tooling ready for batch runs with documentation support.

---

## Phase 5: User Story 3 – Validation & Enforcement (Priority: P2)

**Goal**: As a release engineer, I can enforce Pattern A through the validator CLI, manual PowerShell wrapper, and reflection-based guard documented in the runbook so regressions are blocked before a release.

**Independent Test**: `python scripts/validate_test_exclusions.py --fail-on-warning` plus `pwsh Build/Agent/validate-test-exclusions.ps1` returns exit code 0 when the repo complies, non-zero otherwise; the validation log clearly lists any violations that must be resolved before promotion.

### Implementation

- [X] T018 [P] [US3] Implement validation CLI in `scripts/validate_test_exclusions.py` that loads models, compares against policy (no wildcards, missing exclusions, mixed code), and emits machine-readable reports.
- [X] T019 [US3] Add severity aggregation and summary printing in `scripts/test_exclusions/validator.py`, shared by both the CLI entry point and the PowerShell wrapper.
- [X] T020 [P] [US3] Create `Build/Agent/validate-test-exclusions.ps1` to wrap the Python validator, integrate with existing Agent task conventions, and expose configurable fail-on-warning behavior.
- [ ] T021 [US3] Capture the COPILOT refresh workflow for every converted folder (update each `Src/**/COPILOT.md`, rerun the detect/propose/validate helpers, and record the new `last-reviewed-tree`).
- [X] T022 [US3] Update the comment block in `Directory.Build.props` describing the required `<Compile Remove="<ProjectName>Tests/**" />` pattern and nested folder guidance.
- [X] T023 [US3] Extend `.github/instructions/managed.instructions.md` with a "Test Exclusion Validation" checklist referencing the validator script, PowerShell wrapper, and assembly guard.
- [X] T024 [P] [US3] Expand `quickstart.md` and related docs with a manual validation checklist (validator CLI, MSBuild run, assembly guard) so contributors know exactly how to run the steps by hand.
- [X] T030 [US3] Implement the reflection-based guard in `scripts/test_exclusions/assembly_guard.py` (plus PowerShell shim `scripts/test_exclusions/assembly_guard.ps1`) that loads produced assemblies and fails when any type name ends with `Test`/`Tests`.
- [X] T031 [US3] Integrate the assembly guard with automation scripts by updating `Build/Agent/validate-test-exclusions.ps1` to call it after MSBuild and capture offending assemblies in the validation log.
- [X] T033 [US3] Extend `scripts/validate_test_exclusions.py` and `Build/Agent/validate-test-exclusions.ps1` to parse MSBuild output for CS0436 warnings/errors and fail the validation run whenever any are detected.
- [X] T034 [US3] Add unit/CLI tests in `scripts/tests/test_exclusions/test_validator_command.py` that cover severity aggregation, CS0436 log parsing, and fail-on-warning behavior for the validator CLI + PowerShell wrapper.
- [X] T035 [US3] Add regression tests for `scripts/test_exclusions/assembly_guard.py` (and its PowerShell shim) that load synthetic assemblies/fixtures to prove the guard fails when `*Test*` types are present and passes otherwise.

**Checkpoint**: Enforcement suite ensures ongoing compliance.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final integration, documentation, and contract alignment once all user stories land.

- [X] T025 [P] Sync `contracts/test-exclusion-api.yaml` with the implemented CLI capabilities (ensure request/response examples mirror actual scripts).
- [ ] T026 Validate end-to-end workflow by executing every step in `quickstart.md` and capturing any adjustments needed.
- [ ] T027 Perform a repo-wide search (`git grep "*Tests/**"`) to confirm no Pattern B remnants remain after conversions and update `specs/004-convergence-test-exclusion-patterns/spec.md` status fields if needed.
- [ ] T036 Update the SDK project template under `Src/Templates/` (and any VS item templates) so newly scaffolded projects ship with the Pattern A `<Compile Remove="<ProjectName>Tests/**" />` block plus nested-folder examples documented in `quickstart.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

1. **Setup** → 2. **Foundational** → 3. **US1** → 4. **US2** → 5. **US3** → 6. **Polish**
   - Phases 3–5 may overlap once Foundational is complete, but US2 relies on audit reports from US1, and US3 depends on converter outputs to ensure validation rules reflect final behavior.

### User Story Dependencies

- **US1** has no story prerequisites (depends only on Foundational utilities).
- **US2** consumes the audit outputs (dependency: US1) to determine conversion targets.
- **US3** depends on both US1 (for compliance signals) and US2 (for standardized patterns) before enforcement hardens the rules.

### Task-Level Notes

- Tasks marked **[P]** may run concurrently when they touch distinct files/modules.
- Non-[P] tasks should follow listed order to maintain deterministic diffs and dependency clarity.

---

## Parallel Execution Examples

- **Foundational**: T007 tests can proceed while T005/T006 are built because they reference fixture data.
- **User Story 1**: T008 (CLI) and T010 (mixed-code detection) can run in parallel; T009 (report writer) depends only on shared models.
- **User Story 2**: T013 (conversion script) and T015 (tests) can run concurrently once converter scaffolding exists.
- **User Story 3**: T018 (validator CLI) and T020 (Agent wrapper) can execute in parallel; COPILOT refresh work (T021) waits for both, while T030/T031 can begin once validator plumbing exists.
- **Polish**: T025 and T027 can proceed simultaneously, as one updates contracts and the other validates repo state.

---

## Implementation Strategy

1. **MVP (US1)**: Complete Setup + Foundational → deliver audit tooling (T001–T012, T028–T029). Validate by running the audit command, examining `mixed-code.json`, and filing sample escalations.
2. **Increment 2 (US2)**: Layer deterministic conversions (T013–T017). Ship once dry-run + real conversions succeed on a representative batch.
3. **Increment 3 (US3)**: Add validator, assembly guard, and the documented manual validation workflow (T018–T024, T030–T031) so future regressions are blocked before release sign-off.
4. **Polish**: Align contracts/docs and run end-to-end validation (T025–T027).

Each increment is independently testable; stop after any increment for demo/review if needed.

---

## Summary

- **Total tasks**: 31
- **Per user story**: US1 = 7 tasks, US2 = 5 tasks, US3 = 9 tasks
- **Parallel opportunities**: 8 tasks marked [P]
- **Independent test criteria**: Documented per user story above
- **MVP scope**: Phases 1–3 (Setup, Foundational, US1) deliver actionable audit outputs + escalation workflow
- **Validation**: All tasks follow the required checkbox + ID + story label format
