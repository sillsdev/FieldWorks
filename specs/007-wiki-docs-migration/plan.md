# Implementation Plan: Wiki Documentation Migration

**Branch**: `007-wiki-docs-migration` | **Date**: 2025-12-01 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/007-wiki-docs-migration/spec.md`

## Summary

Migrate developer documentation from the external FwDocumentation wiki into the FieldWorks repository, following modern GitHub conventions. Content will be split between `.github/instructions/` (Copilot-facing code guidance) and `docs/` (human-facing onboarding/tutorials). Legacy Gerrit/Jenkins workflows will be rewritten to GitHub equivalents, with each item validated against the codebase.

## Technical Context

**Language/Version**: Markdown documentation (no code changes)
**Primary Dependencies**: None (documentation only)
**Storage**: N/A
**Testing**: Manual link validation, markdown linting
**Target Platform**: GitHub repository documentation
**Project Type**: Documentation migration
**Performance Goals**: N/A
**Constraints**: Must not duplicate content already in `.github/instructions/`
**Scale/Scope**: ~15 wiki pages to migrate, ~5 new instruction files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Data integrity**: ✅ N/A - This feature does not alter stored data or schemas
- **Test evidence**: ✅ Link validation and manual verification planned
- **I18n/script correctness**: ✅ Documentation references Crowdin workflow where applicable
- **Licensing**: ✅ No new dependencies; documentation under same license as repository
- **Stability/performance**: ✅ N/A - Documentation only

**Gate Status**: PASS

## Project Structure

### Documentation (this feature)

```text
specs/007-wiki-docs-migration/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Wiki analysis and decisions
├── data-model.md        # Documentation entity model
├── quickstart.md        # Implementation reference
├── contracts/           # Structure contracts
│   └── documentation-structure.yaml
├── checklists/          # Validation checklists
│   └── requirements.md
└── tasks.md             # Task breakdown (Phase 2)
```

### Target Repository Structure

```text
FieldWorks/
├── .github/
│   └── instructions/           # Curated, minimal instruction files (no new files required for this feature)
│
├── docs/                       # NEW directory
│   ├── CONTRIBUTING.md         # Main entry point
│   ├── visual-studio-setup.md  # VS 2022 setup
│   ├── core-developer-setup.md # Core dev onboarding
│   │
│   ├── workflows/              # Development workflows
│   │   ├── pull-request-workflow.md   # NEW (not from wiki)
│   │   └── release-process.md         # From wiki
│   │
│   ├── architecture/           # Technical architecture
│   │   ├── data-migrations.md  # From wiki
│   │   └── dependencies.md     # From wiki
│   │
│   └── images/                 # Documentation images
│
└── ReadMe.md                   # Updated with doc links
```

**Structure Decision**: Split documentation strategy per clarification session. `.github/instructions/` for code guidance consumed by Copilot; `docs/` for human onboarding and tutorials.

## Implementation Phases

### Phase 1: Foundation (P1 User Stories)

**Goal**: New contributor can complete first build using in-repo docs

**Tasks**:
1. Create `docs/` directory structure
2. Migrate "Contributing to FieldWorks Development" → `docs/CONTRIBUTING.md`
   - Update build commands (build.bat → build.ps1)
   - Remove fwmeta/initrepo references
   - Add GitHub clone instructions
3. Migrate "Set Up Visual Studio" → `docs/visual-studio-setup.md`
   - Verify VS 2022 requirements current
   - Update solution file references
4. Update `ReadMe.md` with links to `docs/CONTRIBUTING.md`

**Validation**: Fresh clone + build following only in-repo docs

### Phase 2: Workflows (P2 User Stories)

**Goal**: Core developer workflow fully documented with GitHub conventions

**Tasks**:
1. Create `docs/workflows/pull-request-workflow.md` (new content)
   - Branch naming conventions
   - PR creation and review process
   - Merge requirements
2. Ensure code review expectations are documented in GitHub-native places
   - `docs/workflows/pull-request-workflow.md`
   - `.github/pull_request_template.md`
3. Migrate "Release Workflow Steps" → `docs/workflows/release-process.md`
   - Mark with CONFIRMATION_NEEDED where uncertain

**Validation**: No Gerrit references remain (search validation)

### Phase 3: Architecture & Standards (P3 User Stories)

**Goal**: Data migration and coding standards documented

**Tasks**:
1. Migrate "Data Migrations" → `docs/architecture/data-migrations.md`
   - Verify file paths against current codebase
   - Update class/namespace references if changed
2. Ensure coding/formatting and commit conventions are discoverable
   - `.editorconfig` for formatting
   - `.github/commit-guidelines.md` for commit rules
4. Migrate "Dependencies on Other Repos" → `docs/architecture/dependencies.md`
   - Remove TeamCity references
   - Update for GitHub Actions

**Validation**: All file paths verified against codebase

### Phase 4: Platform Documentation

**Goal**: Linux docs preserved with appropriate markers

**Tasks**:
1. Create `docs/linux/` directory
2. Migrate Linux build docs with CONFIRMATION_NEEDED markers
3. Migrate Vagrant docs (vagrant/ folder exists in repo)
4. Download and store referenced images

**Validation**: All CONFIRMATION_NEEDED markers clearly visible

### Phase 5: Final Validation

**Goal**: All success criteria met

**Tasks**:
1. Run link checker on all docs
2. Search for obsolete patterns (gerrit, build.bat, C:\fwrepo)
3. Verify no duplicate content with instruction files
4. Manual new contributor test

**Validation**: SC-001 through SC-005 verified

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Outdated file paths | Verify each path before committing |
| Missing Gerrit translation | Create comprehensive mapping table |
| Linux docs stale | Mark with CONFIRMATION_NEEDED |
| Content duplication | Cross-reference existing instruction files |

## Complexity Tracking

> No Constitution Check violations requiring justification.

| Item | Status |
|------|--------|
| Data integrity | N/A - documentation only |
| Test evidence | Link validation planned |
| I18n/script correctness | References Crowdin where applicable |
| Licensing | No new dependencies |
| Stability/performance | N/A |
