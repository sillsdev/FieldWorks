# Tasks: WiX 3.14 Installer Upgrade

**Input**: Design documents from `/specs/007-wix-314-installer/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, quickstart.md ✅

**Tests**: Manual validation specified - CI workflow success and installer execution testing.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Preparation)

**Purpose**: Ensure working branch and understand current state

- [X] T001 Create feature branch `007-wix-314-installer` from `release/9.3`
- [X] T002 Verify WiX 3.14.x is installed locally: run `where.exe candle.exe` and check version
- [X] T003 Review current workflow files to understand downgrade step location in `.github/workflows/base-installer-cd.yml` and `.github/workflows/patch-installer-cd.yml`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: None required - this is a CI/documentation change with no code dependencies

**Checkpoint**: Proceed directly to User Story implementation

---

## Phase 3: User Story 1 - Remove WiX Downgrade Workaround (Priority: P1) 🎯 MVP

**Goal**: Remove the chocolatey WiX downgrade step from both CI workflows

**Independent Test**: Trigger a PR build and verify workflows complete without WiX-related errors

### Implementation for User Story 1

- [X] T004 [P] [US1] Remove "Downgrade Wix Toolset" step from `.github/workflows/base-installer-cd.yml`
- [X] T005 [P] [US1] Remove "Downgrade Wix Toolset" step from `.github/workflows/patch-installer-cd.yml`
- [ ] T006 [US1] Push changes and verify PR workflow runs succeed (no downgrade, uses WiX 3.14.x; validates FR-005 `insignia` compatibility via burn engine extraction/reattachment)

**Checkpoint**: Both workflows should execute successfully using pre-installed WiX 3.14.x

---

## Phase 4: User Story 2 - Validate Base Installer Build (Priority: P1)

**Goal**: Confirm base installer builds and installs correctly with WiX 3.14.x

**Independent Test**: Download CI-built installer, install on test system, launch FieldWorks

### Validation for User Story 2

- [ ] T007 [US2] Trigger `base-installer-cd.yml` workflow manually (workflow_dispatch) with `make_release: false`
- [ ] T008 [US2] Download offline installer artifact from workflow run
- [ ] T009 [US2] Install on Windows 10/11 test system (clean or with existing FW)
- [ ] T010 [US2] Launch FieldWorks and verify basic functionality (create project, open project)
- [ ] T011 [US2] Document validation results in PR description

**Checkpoint**: Base installer installs and FieldWorks launches successfully

---

## Phase 5: User Story 3 - Validate Patch Installer Build (Priority: P1)

**Goal**: Confirm patch installer builds and applies correctly with WiX 3.14.x

**Independent Test**: Build patch, apply to WiX 3.11.x base (build-1188), verify upgrade

### Validation for User Story 3

- [ ] T012 [US3] Trigger `patch-installer-cd.yml` workflow manually with `base_release: build-1188` and `make_release: false`
- [ ] T013 [US3] Download patch artifact (.msp) from workflow run
- [ ] T014 [US3] Install base build-1188 on test system (WiX 3.11.x-built base)
- [ ] T015 [US3] Apply WiX 3.14.x-built patch to the base installation
- [ ] T016 [US3] Verify version number updated and FieldWorks functions correctly
- [ ] T017 [US3] Document backward compatibility validation in PR description

**Checkpoint**: Patch applies successfully to WiX 3.11.x base installation

---

## Phase 6: User Story 4 - Document Installer Build Process (Priority: P2)

**Goal**: Create comprehensive documentation for local and CI installer builds

**Independent Test**: New developer can follow docs to build installer locally

### Implementation for User Story 4

- [X] T018 [US4] Create `Docs/installer-build-guide.md` with content from `specs/007-wix-314-installer/quickstart.md`
- [X] T019 [US4] Add CI workflow explanation section to `Docs/installer-build-guide.md`
- [ ] T020 [US4] Review documentation by attempting local build following only the guide (validates FR-003/FR-004 MSBuild commands work locally) - **Requires WiX installed locally**
- [X] T021 [US4] Update `Docs/` index or README if one exists to link new guide (added link in `Docs/CONTRIBUTING.md`)

**Checkpoint**: Documentation complete and validated

---

## Phase 7: User Story 5 - Update Copilot Instructions (Priority: P3)

**Goal**: Update all documentation references from WiX 3.11.x to WiX 3.14.x

**Independent Test**: Grep for "3.11" in documentation files returns no matches

### Implementation for User Story 5

- [X] T022 [US5] Update WiX version reference in `.github/instructions/installer.instructions.md` from "3.11.x" to "3.14.x"
- [X] T023 [US5] Verify `.github/copilot-instructions.md` already references WiX 3.14.x (confirmed - already says "WiX 3.14.x")
- [X] T024 [US5] Search repository for any other "WiX 3.11" references: `git grep -i "wix.*3\.11"`
- [X] T025 [US5] Update any additional references found (updated: `.serena/memories/project_overview.md`, `Build/Agent/Verify-FwDependencies.ps1`)

**Checkpoint**: Zero references to WiX 3.11 in active documentation ✓

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [ ] T026 [P] Run whitespace check: `.\Build\Agent\check-and-fix-whitespace.ps1`
- [ ] T027 [P] Run commit message lint: `.\Build\Agent\commit-messages.ps1`
- [ ] T028 Measure CI execution time improvement (compare `base-installer-cd` workflow duration before and after change in GitHub Actions summary; expect ≥30s reduction)
- [ ] T029 Update PR description with all validation results and success criteria metrics
- [ ] T030 Request code review

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Skipped - no blocking prerequisites for this feature
- **User Story 1 (Phase 3)**: Depends on Setup - core workflow changes
- **User Story 2 (Phase 4)**: Depends on US1 completion - validates base installer
- **User Story 3 (Phase 5)**: Depends on US1 completion - validates patch installer
- **User Story 4 (Phase 6)**: Can start in parallel with US2/US3 - documentation
- **User Story 5 (Phase 7)**: Can start in parallel with US2/US3/US4 - doc updates
- **Polish (Phase 8)**: Depends on all user stories complete

### User Story Dependencies

```
US1 (Remove Workaround) ─┬─> US2 (Validate Base) ─┐
                         │                         ├──> Polish
                         └─> US3 (Validate Patch) ─┤
                                                   │
US4 (Documentation) ───────────────────────────────┤
                                                   │
US5 (Copilot Instructions) ────────────────────────┘
```

### Parallel Opportunities

**After US1 completes:**
- US2 (Validate Base) and US3 (Validate Patch) can run in parallel
- US4 (Documentation) can start immediately
- US5 (Copilot Instructions) can start immediately

**Within Phase 3 (US1):**
- T004 and T005 can run in parallel (different files)

**Within Phase 8 (Polish):**
- T026 and T027 can run in parallel

---

## Parallel Example: User Stories 2-5

```bash
# After US1 is complete, launch all validation/documentation in parallel:

# Team Member A:
Task: T007-T011 "Validate Base Installer Build"

# Team Member B:
Task: T012-T017 "Validate Patch Installer Build"

# Team Member C:
Task: T018-T021 "Document Installer Build Process"

# Team Member D (or same as C):
Task: T022-T025 "Update Copilot Instructions"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 3: User Story 1 (T004-T006) - Remove downgrade workaround
3. **STOP and VALIDATE**: Verify CI workflow succeeds
4. This alone delivers SC-001 (30s+ time savings) and FR-001/FR-002

### Incremental Delivery

1. **MVP (US1)**: Remove workaround → Verify CI passes → Merge-ready for urgent needs
2. **Add US2**: Validate base installer → Confidence for release managers
3. **Add US3**: Validate patch backward compatibility → Full release readiness
4. **Add US4**: Documentation → Knowledge transfer complete
5. **Add US5**: Copilot instructions → Developer experience complete

### Success Criteria Mapping

| Success Criteria | Validated By |
|------------------|--------------|
| SC-001: 30s time savings | T028 (measure CI time) |
| SC-002: 100% build success | T006 (PR workflow), T007 (base), T012 (patch) |
| SC-003: Base installer works | T009, T010 |
| SC-004: Patch backward compat | T014, T015, T016 |
| SC-005: 30min local build | T020 (doc validation) |
| SC-006: Zero 3.11 references | T024, T025 |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- User Stories 2-5 can proceed in parallel after US1 completes
- Manual validation tasks (US2, US3) require test system access
- T006, T007, T012 require GitHub Actions workflow triggers (push or workflow_dispatch)
- Commit after each logical task group
- PR can be opened after T006 for early review while validation continues
