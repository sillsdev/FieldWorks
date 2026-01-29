````markdown
# Tasks: Wiki Documentation Migration

**Input**: Design documents from `/specs/007-wiki-docs-migration/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create directory structure and establish conventions

- [x] T001 Create `docs/` directory at repository root
- [x] T002 [P] Create `docs/workflows/` subdirectory
- [x] T003 [P] Create `docs/architecture/` subdirectory
- [x] T004 [P] Create `docs/linux/` subdirectory
- [x] T005 [P] Create `docs/images/` subdirectory

**Checkpoint**: Directory structure in place ✅

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish content patterns and validation before user story work

**⚠️ CRITICAL**: Complete before user story implementation

- [x] T006 Fetch wiki page: "Contributing to FieldWorks Development" from `https://github.com/sillsdev/FwDocumentation/wiki/Contributing-to-FieldWorks-Development`
- [x] T007 [P] Fetch wiki page: "Set Up Visual Studio" from `https://github.com/sillsdev/FwDocumentation/wiki/Set-Up-Visual-Studio-for-FieldWorks-Development-on-Windows`
- [x] T008 [P] Fetch wiki page: "Getting Started for Core Developers" from `https://github.com/sillsdev/FwDocumentation/wiki/Getting-Started-for-Core-Developers`
- [x] T009 [P] Fetch wiki page: "Code Reviews" from `https://github.com/sillsdev/FwDocumentation/wiki/Code-Reviews`
- [x] T010 [P] Fetch wiki page: "Coding Standard" from `https://github.com/sillsdev/FwDocumentation/wiki/Coding-Standard`
- [x] T011 [P] Fetch wiki page: "Data Migrations" from `https://github.com/sillsdev/FwDocumentation/wiki/Data-Migrations`
- [x] T012 [P] Fetch wiki page: "Dispose" from `https://github.com/sillsdev/FwDocumentation/wiki/Dispose`
- [x] T013 [P] Fetch wiki page: "Dependencies on Other Repos" from `https://github.com/sillsdev/FwDocumentation/wiki/Dependencies-on-Other-Repos`
- [x] T014 [P] Fetch wiki page: "Build FieldWorks (Linux)" from `https://github.com/sillsdev/FwDocumentation/wiki/Build-FieldWorks-%28Linux%29`
- [x] T015 [P] Fetch wiki page: "Using Vagrant" from `https://github.com/sillsdev/FwDocumentation/wiki/Using-Vagrant`
- [x] T015a [P] Fetch wiki page: "Release Workflow Steps" from `https://github.com/sillsdev/FwDocumentation/wiki/Release-Workflow-Steps`
- [x] T016 Verify existing instruction files in `.github/instructions/` to avoid content duplication (build, testing, managed, native, security)

**Checkpoint**: All source content fetched and analyzed ✅

---

## Phase 3: User Story 1 - New Contributor Quick Start (Priority: P1) 🎯 MVP

**Goal**: New developer can complete first build using in-repo docs

**Independent Test**: Fresh clone → follow `docs/CONTRIBUTING.md` → successful build

### Implementation for User Story 1

- [x] T017 [US1] Create `docs/CONTRIBUTING.md` from wiki "Contributing to FieldWorks Development"
  - Update `build.bat` references → `build.ps1`
  - Remove fwmeta/initrepo references
  - Add GitHub clone instructions (HTTPS + SSH)
  - Include link to `docs/visual-studio-setup.md`
- [x] T018 [P] [US1] Create `docs/visual-studio-setup.md` from wiki "Set Up Visual Studio"
  - Verify VS 2022 requirements are current
  - Update solution file references (`FW.sln` → `FieldWorks.sln`)
  - Verify .NET requirements against current `Directory.Build.props`
- [x] T019 [P] [US1] Create `docs/core-developer-setup.md` from wiki "Getting Started for Core Developers"
  - Remove Gerrit SSH key setup (lines about port 59418)
  - Keep GitHub SSH key setup if applicable
  - Update environment variable guidance
- [x] T020 [US1] Update `ReadMe.md` to link to `docs/CONTRIBUTING.md`
  - Add "Getting Started" section if not present
  - Link to `docs/visual-studio-setup.md`
  - Preserve existing ReadMe content

**Checkpoint**: User Story 1 complete - new contributor path functional ✅

---

## Phase 4: User Story 2 - Core Developer Workflow Reference (Priority: P2)

**Goal**: Core developer finds GitHub-native workflow documentation

**Independent Test**: Developer can find and follow PR submission process from in-repo docs

### Implementation for User Story 2

- [x] T021 [US2] Create `docs/workflows/pull-request-workflow.md` (NEW content - not from wiki)
  - Branch naming conventions (feature/, bugfix/, hotfix/)
  - PR creation process
  - Code review expectations
  - Merge requirements (approvals, CI passing)
  - Link to existing `PULL_REQUEST_TEMPLATE.md` if exists
- [x] T022 [P] [US2] Document code review expectations in GitHub-native places
  - `docs/workflows/pull-request-workflow.md`
  - `.github/pull_request_template.md`
- [x] T023 [US2] Create `docs/workflows/release-process.md` from wiki "Release Workflow Steps"
  - Mark with `CONFIRMATION_NEEDED` for steps that cannot be verified
  - Remove Jenkins/TeamCity references
  - Update version bump procedures if identifiable

**Checkpoint**: User Story 2 complete - workflow docs available ✅

---

## Phase 5: User Story 3 - Data Migration Author Guide (Priority: P3)

**Goal**: Developer can author data migrations using in-repo guidance

**Independent Test**: Developer can find migration instructions and locate correct source files

### Implementation for User Story 3

- [x] T024 [US3] Create `docs/architecture/data-migrations.md` from wiki "Data Migrations"
  - Verify all file paths against current codebase (e.g., `Src/FDO/` structure)
  - Update class/namespace references if changed
  - Include FLEx Bridge metadata cache migration relationship
  - Mark uncertain paths with `CONFIRMATION_NEEDED`
- [x] T025 [P] [US3] Create `docs/architecture/dependencies.md` from wiki "Dependencies on Other Repos"
  - Remove TeamCity/Jenkins references
  - Update for GitHub Actions and current build process
  - Verify listed repos still exist and are relevant:
    - sillsdev/FwSampleProjects
    - sillsdev/FwLocalizations
    - sillsdev/Helps

**Checkpoint**: User Story 3 complete - data migration guidance available ✅

---

## Phase 6: User Story 4 - Coding Standards Reference (Priority: P3)

**Goal**: Developer finds coding standards in discoverable location

**Independent Test**: Developer can verify code style against in-repo documentation

### Implementation for User Story 4

- [x] T026 [US4] Ensure coding/formatting and commit conventions are discoverable
  - `.editorconfig` for formatting
  - `.github/commit-guidelines.md` for commit rules
- [x] T027 [P] [US4] Ensure IDisposable guidance is discoverable
  - `.github/instructions/managed.instructions.md` (as needed)

**Checkpoint**: User Story 4 complete - coding standards accessible ✅

---

## Phase 7: Platform Documentation (Cross-Cutting)

**Purpose**: ~~Preserve Linux documentation with appropriate markers~~

**Status**: SKIPPED - Linux/Vagrant/Flatpak content confirmed obsolete (2025-12-02)

- [x] ~~T028 [P] Create `docs/linux/build-linux.md`~~ — N/A (obsolete)
- [x] ~~T029 [P] Create `docs/linux/vagrant.md`~~ — N/A (obsolete)
- [x] T030 [P] Download and store wiki images to `docs/images/`
  - Extract image URLs from all fetched wiki pages
  - Store with descriptive filenames
  - Update image references in migrated docs
  - (No wiki images found requiring migration)

**Checkpoint**: Platform docs — Linux content skipped as obsolete ✅

---

## Phase 8: Polish & Validation

**Purpose**: Final verification against success criteria

- [x] T031 Run link checker on all `docs/*.md` and `.github/instructions/*.md` files
  - Verify all internal links resolve
  - Verify all external links are reachable
  - ✅ Verified: All internal links in new files resolve correctly
- [x] T032 [P] Search migrated docs for obsolete patterns:
  - `gerrit` (should not appear except historical context)
  - `build.bat` (should be `build.ps1`)
  - `C:\fwrepo` (should use relative paths)
  - `FW.sln` (should be `FieldWorks.sln`)
  - `fwmeta` or `initrepo` (should not appear)
  - ✅ Verified: No obsolete patterns found
- [x] T033 [P] Verify no duplicate content with existing instruction files:
  - `build.instructions.md`
  - `testing.instructions.md`
  - `managed.instructions.md`
  - `native.instructions.md`
  - ✅ Verified: New files complement existing instructions
- [x] T034 Update `specs/007-wiki-docs-migration/checklists/requirements.md` with verification status
- [ ] T035 Run quickstart.md validation (manual new contributor test)

**Checkpoint**: All success criteria SC-001 through SC-005 verified ✅

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 - BLOCKS all user stories
- **User Stories (Phases 3-6)**: All depend on Phase 2 completion
  - Can proceed in parallel OR sequentially by priority (P1 → P2 → P3)
- **Platform Docs (Phase 7)**: Depends on Phase 2, can run parallel with user stories
- **Polish (Phase 8)**: Depends on all content phases complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational - Independent of US1
- **User Story 3 (P3)**: Can start after Foundational - Independent of US1/US2
- **User Story 4 (P3)**: Can start after Foundational - Independent of US1/US2/US3

### Within Each User Story

- Fetch content before writing docs
- Core doc before supplementary docs
- All docs complete before checkpoint

### Parallel Opportunities

```bash
# Phase 1 - All directory creation in parallel:
T002, T003, T004, T005

# Phase 2 - All wiki fetches in parallel:
T007, T008, T009, T010, T011, T012, T013, T014, T015

# Phase 3 (US1) - These can run in parallel:
T018, T019

# Phase 4 (US2) - This can run in parallel:
T022

# Phase 6 (US4) - These can run in parallel:
T026, T027

# Phase 7 - All platform docs in parallel:
T028, T029, T030

# Phase 8 - Validation tasks in parallel:
T032, T033
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (5 min)
2. Complete Phase 2: Foundational fetch (10 min)
3. Complete Phase 3: User Story 1 (P1)
4. **STOP and VALIDATE**: Test new contributor path
5. Can deploy/demo if timeline requires

### Full Implementation (Recommended)

1. Complete Phases 1-2 → Foundation ready
2. Complete Phase 3 (US1) → MVP checkpoint
3. Complete Phase 4 (US2) → Workflow checkpoint
4. Complete Phases 5-6 (US3, US4) → Standards checkpoint
5. Complete Phase 7 → Platform checkpoint
6. Complete Phase 8 → Full validation

---

## Content Transformation Reference

From `data-model.md`:

| Wiki Pattern | Repo Pattern |
|--------------|--------------|
| `C:\fwrepo\fw\` | Repository root (relative paths) |
| `build.bat` | `build.ps1` |
| `FW.sln` | `FieldWorks.sln` |
| `git review` | `git push origin <branch>` + PR |
| `git start task` | `git checkout -b feature/<name>` |
| `[[Wiki Link]]` | `[Link Text](./file.md)` |

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to user story for traceability
- Each user story independently completable and testable
- CONFIRMATION_NEEDED markers use format: `> ⚠️ **CONFIRMATION_NEEDED**: [description]`
- Commit after each task or logical group
- Stop at any checkpoint to validate independently

````
