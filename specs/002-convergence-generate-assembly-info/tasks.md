---
description: "Task list for GenerateAssemblyInfo Template Reintegration"
---

# Tasks: GenerateAssemblyInfo Template Reintegration

**Input**: Design documents from `/specs/002-convergence-generate-assembly-info/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Validation relies on the Python automation plus full MSBuild Debug/Release runs; no standalone unit tests are mandated beyond script-level assertions.

**Organization**: Tasks are grouped by user story so each increment can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Safe to execute in parallel (touches separate files; no ordering constraints)
- **[Story]**: Maps the task to a specific user story (US1, US2, US3)
- Include exact file paths in every description for traceability

## Path Conventions

- Python automation lives under `scripts/GenerateAssemblyInfo/`
- Generated artifacts land in `Output/GenerateAssemblyInfo/`
- Feature documentation stays in `specs/002-convergence-generate-assembly-info/`
- Project files span `Src/**/*.csproj` with representative examples noted per task

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the automation workspace and developer documentation called for by the plan.

- [X] T001 Create package scaffold in `scripts/GenerateAssemblyInfo/__init__.py` with a module docstring summarizing the template-linking workflow.
- [X] T002 [P] Seed `Output/GenerateAssemblyInfo/.gitkeep` and update `Output/.gitignore` so CSV/JSON audit artifacts are preserved for review.
- [X] T003 Wire the new automation entry points into `specs/002-convergence-generate-assembly-info/quickstart.md`, covering environment prerequisites and command placeholders.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core utilities every user story requires before audit/convert/validate can run.

- [X] T004 [P] Implement `scripts/GenerateAssemblyInfo/project_scanner.py` to enumerate every `Src/**/*.csproj` and capture metadata into the `ManagedProject` structure defined in `data-model.md`.
- [X] T005 [P] Add `scripts/GenerateAssemblyInfo/assembly_info_parser.py` that inspects on-disk `AssemblyInfo*.cs` files (e.g., `Src/Common/FieldWorks/Properties/AssemblyInfo.cs`) and records custom attributes/conditional blocks.
- [X] T006 [P] Create `scripts/GenerateAssemblyInfo/git_restore.py` capable of running `git show <sha>:<path>` so deleted AssemblyInfo files can be restored exactly as described in research decision 3.
- [X] T007 Build `scripts/GenerateAssemblyInfo/reporting.py` to emit CSV/JSON rows matching `ManagedProject`, `AssemblyInfoFile`, and `ValidationFinding` records.
- [X] T008 Define a shared CLI argument module in `scripts/GenerateAssemblyInfo/cli_args.py` covering common flags (branch, output paths, restore map) used by every script.
- [X] T032 [P] Extend `scripts/GenerateAssemblyInfo/history_diff.py` (or equivalent helper) to compare today’s tree against `git log -- src/**/AssemblyInfo*.cs`, emitting `Output/GenerateAssemblyInfo/restore_map.json` so restoration work uses exact commit hashes.
- [X] T033 Schedule an “ambiguous projects” checkpoint that filters `Output/GenerateAssemblyInfo/generate_assembly_info_audit.csv` for `NeedsReview` entries, records owner decisions in `specs/002-convergence-generate-assembly-info/research.md`, and blocks conversion until sign-off.

**Checkpoint**: Foundational helpers exist; user stories can now consume them without reimplementing plumbing.

---

## Phase 3: User Story 1 - Repository-wide audit (Priority: P1) 🎯 MVP

**Goal**: As a build engineer, I can inventory all 115 managed projects to understand their template/import state and generate actionable CSV decisions.

**Independent Test**: `py -3.11 scripts/GenerateAssemblyInfo/audit_generate_assembly_info.py --output Output/GenerateAssemblyInfo/generate_assembly_info_audit.csv` lists every project with category T/C/G and highlights missing template imports.

### Implementation for User Story 1

- [X] T009 [P] [US1] Implement the main CLI workflow in `scripts/GenerateAssemblyInfo/audit_generate_assembly_info.py`, wiring together `project_scanner`, `assembly_info_parser`, and `cli_args`.
- [X] T010 [P] [US1] Classify each project per `data-model.md` (Template-only, Template+Custom, Needs Fix) and compute `remediationState` transitions inside `scripts/GenerateAssemblyInfo/audit_generate_assembly_info.py`.
- [X] T011 [US1] Write the CSV + optional JSON outputs to `Output/GenerateAssemblyInfo/generate_assembly_info_audit.csv` and ensure headers align with the spec’s Implementation Checklist.
- [X] T012 [US1] Document the audit workflow (inputs, sample command, interpretation) in `specs/002-convergence-generate-assembly-info/spec.md` under Success Metrics for transparency.

**Parallel Example (US1)**: T009 and T010 can proceed concurrently once the foundational modules exist, because CLI wiring and classification logic touch separate functions within `audit_generate_assembly_info.py`.

---

## Phase 4: User Story 2 - Template reintegration & restoration (Priority: P1)

**Goal**: As a build engineer, I can apply scripted fixes that link `Src/CommonAssemblyInfo.cs`, toggle `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>`, and restore missing custom AssemblyInfo files.

**Independent Test**: `py -3.11 scripts/GenerateAssemblyInfo/convert_generate_assembly_info.py --decisions Output/GenerateAssemblyInfo/decisions.csv --restore-map Output/GenerateAssemblyInfo/restore.json` updates representative projects (e.g., `Src/Common/FieldWorks/FieldWorks.csproj`, `Src/CacheLight/CacheLight.csproj`) without producing CS0579 warnings.

### Implementation for User Story 2

- [X] T013 [P] [US2] Extend `scripts/GenerateAssemblyInfo/convert_generate_assembly_info.py` to insert `<Compile Include="..\\..\\CommonAssemblyInfo.cs" Link="Properties\\CommonAssemblyInfo.cs" />` into each `Src/**/*.csproj` that lacks the shared template link.
- [X] T014 [P] [US2] Integrate `git_restore.py` so the convert script can recreate deleted `AssemblyInfo*.cs` files under their original paths (e.g., `Src/LexText/Properties/AssemblyInfo.LexText.cs`) using commit hashes from `restore.json`.
- [X] T015 [US2] Force `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` plus an explanatory XML comment into affected `.csproj` files and guard against duplicate property groups.
- [X] T016 [US2] Ensure the convert script normalizes `<Compile Include>` entries so every custom AssemblyInfo file is compiled exactly once, updating `specs/002-convergence-generate-assembly-info/research.md` with any discovered edge cases.
- [X] T031 [US2] Evaluate Template-only candidates surfaced by the audit, documenting justification in `Output/GenerateAssemblyInfo/decisions.csv`; when gaps exist, scaffold minimal `Properties/AssemblyInfo.<Project>.cs` files with the missing attributes and link them via `convert_generate_assembly_info.py`.

**Parallel Example (US2)**: T013 and T014 can run in parallel because one touches template-link insertion logic while the other implements git restoration helpers; they only converge when T015 integrates both.

---

## Phase 5: User Story 3 - Validation & compliance reporting (Priority: P2)

**Goal**: As a build engineer, I can verify the entire repository satisfies the template policy and capture evidence (validation report + MSBuild output + documentation).

**Independent Test**: `py -3.11 scripts/GenerateAssemblyInfo/validate_generate_assembly_info.py --report Output/GenerateAssemblyInfo/validation_report.txt --run-build` completes without errors and a subsequent `msbuild FieldWorks.sln /m /p:Configuration=Debug` run shows zero CS0579 warnings.

### Implementation for User Story 3

- [X] T017 [P] [US3] Implement structural validations in `scripts/GenerateAssemblyInfo/validate_generate_assembly_info.py` (template import present, `GenerateAssemblyInfo=false`, AssemblyInfo file on disk when required).
- [X] T018 [US3] Add MSBuild invocation + log parsing inside the validate script to assert no CS0579 warnings remain, capturing logs under `Output/GenerateAssemblyInfo/msbuild-validation.log`.
- [X] T019 [P] [US3] Introduce a lightweight reflection harness (e.g., `scripts/GenerateAssemblyInfo/reflect_attributes.py`) that loads representative assemblies and asserts CommonAssemblyInfo attributes are present exactly once; invoke it from the validate script and stash logs under `Output/GenerateAssemblyInfo/reflection.log`.
- [X] T020 [US3] Execute the FieldWorks regression suite (e.g., `msbuild FieldWorks.sln /t:Test /p:Configuration=Debug` inside the fw-agent container) and store TRX/summary output under `Output/GenerateAssemblyInfo/tests/` to prove runtime safety.
- [X] T021 [US3] Capture build-duration metrics by running pre/post `msbuild FieldWorks.sln /m /p:Configuration=Release` timings, writing comparisons to `Output/GenerateAssemblyInfo/build-metrics.json` to enforce the ±5% guardrail.
- [X] T022 [US3] Add a validation step that cross-references `restore_map.json` with on-disk `AssemblyInfo*.cs` files, failing the run if a previously existing file remains missing.
- [X] T023 [US3] Produce `Output/GenerateAssemblyInfo/validation_report.txt` summarizing residual findings and reference it from `specs/002-convergence-generate-assembly-info/quickstart.md`.
- [X] T024 [US3] Update Success Metrics and Timeline sections in `specs/002-convergence-generate-assembly-info/spec.md` with before/after counts plus links to the validation artifacts.

**Parallel Example (US3)**: T017 and T018 can proceed simultaneously after the foundational modules are ready, because structural checks and MSBuild integration touch different sections of `validate_generate_assembly_info.py`.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Align documentation and engineering guidelines once all user stories are complete.

- [X] T025 [P] Update `Directory.Build.props` with an explicit note linking the restored `CommonAssemblyInfoTemplate` policy, including guidance on when `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` is mandatory.
- [X] T026 [P] Refresh `scripts/templates/*.csproj` (or the authoritative scaffold referenced in `quickstart.md`) so new managed projects automatically import `CommonAssemblyInfo.cs` and start with a GenerateAssemblyInfo comment block.
- [X] T027 [P] Refresh `.github/instructions/managed.instructions.md` to describe the "template + custom AssemblyInfo" policy plus the new automation scripts.
- [X] T028 [P] Capture final audit/conversion/validation statistics in `specs/002-convergence-generate-assembly-info/plan.md` and `specs/002-convergence-generate-assembly-info/data-model.md` (update entity state descriptions accordingly).
- [X] T029 Run `quickstart.md` end-to-end and document the expected output paths in `specs/002-convergence-generate-assembly-info/quickstart.md`, adjusting any command flags discovered during dry runs.
- [X] T030 File follow-up GitHub issues for each project that still requires manual review after conversion/validation, referencing the relevant entries in `Output/GenerateAssemblyInfo/validation_report.txt` and linking them in `spec.md` Phase 5.

---

## Dependencies & Execution Order

1. **Phase 1 (Setup)** has no prerequisites.
2. **Phase 2 (Foundational)** depends on Setup and blocks all user stories.
3. **User Story Phases (3–5)** each depend on Phase 2 completion.
   - US1 must complete before US2 (conversion script depends on the audit CSV schema).
   - US3 can begin once US2 has produced converted projects to validate.
4. **Phase 6 (Polish)** depends on all user stories reaching their independent test criteria.

## Parallel Execution Opportunities

- During Phase 2, T004–T008 marked [P] can be split across contributors because they modify different helper modules.
- Once Phase 2 finishes, US1 tasks T009–T010 and US2 tasks T013–T014 can run in parallel provided they keep separate branches until integration.
- Validation (US3) tasks T017–T018 can also run concurrently, accelerating the final compliance check.

## Implementation Strategy

1. **MVP (US1)**: Finish Phases 1–3 to obtain a trustworthy audit CSV; stop here if downstream approvals are pending.
2. **Incremental delivery**: After MVP, implement US2 to remediate projects, re-run audit to confirm improvements, then proceed to US3 for validation evidence.
3. **Documentation + Policy**: Phase 6 ensures long-term maintainability by updating managed code guidelines and quickstart instructions.

---
