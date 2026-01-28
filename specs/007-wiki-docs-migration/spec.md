# Feature Specification: Wiki Documentation Migration

**Feature Branch**: `007-wiki-docs-migration`
**Created**: 2025-12-01
**Status**: Draft
**Input**: User description: "Analyze, pull and update all documentation from FwDocumentation wiki into this repository following modern GitHub documentation conventions"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - New Contributor Quick Start (Priority: P1)

A new developer wants to contribute to FieldWorks. They visit the repository and find clear, up-to-date setup instructions directly in the repo (not scattered across an external wiki). They can complete environment setup and make their first build within 2 hours by following in-repo documentation.

**Why this priority**: The contributor experience is the primary audience for this documentation. Having setup instructions in-repo reduces friction and ensures docs stay version-aligned with code.

**Independent Test**: A developer with no prior FieldWorks experience can follow the `docs/CONTRIBUTING.md` guide and successfully build the project.

**Acceptance Scenarios**:

1. **Given** a developer visits the FieldWorks repo, **When** they look for setup instructions, **Then** they find a clear link from `ReadMe.md` to `docs/CONTRIBUTING.md`
2. **Given** a developer follows the contributing guide, **When** they complete the prerequisite steps, **Then** they can run `.\build.ps1` successfully
3. **Given** a developer needs platform-specific instructions, **When** they check the contributing guide, **Then** they find the supported Windows setup path (and any legacy/non-supported platforms are clearly marked)

---

### User Story 2 - Core Developer Workflow Reference (Priority: P2)

A core developer needs to reference the git workflow, code review process, or release procedures. They find this information in organized markdown files within the repository, with modern GitHub conventions (branch protection, PR templates, etc.) replacing legacy Gerrit workflows.

**Why this priority**: Core developers need quick access to workflow documentation without navigating an external wiki. This also enables version-specific workflow docs.

**Independent Test**: A core developer can find and follow the pull request submission process entirely from in-repo documentation.

**Acceptance Scenarios**:

1. **Given** a developer needs to submit a code change, **When** they check `docs/workflows/`, **Then** they find step-by-step PR submission instructions
2. **Given** a developer needs to understand code review expectations, **When** they read the workflow docs, **Then** they find modern GitHub-based review guidelines
3. **Given** documentation references legacy Gerrit workflows, **When** the migration is complete, **Then** all Gerrit references are either removed or clearly marked as historical

---

### User Story 3 - Data Migration Author Guide (Priority: P3)

A developer needs to create a data migration for FLEx. They find clear, current instructions for writing migrations, including the relationship with FLEx Bridge metadata cache migrations.

**Why this priority**: Data migrations are a specialized but critical task. Having clear, in-repo guidance prevents mistakes that could corrupt user data.

**Independent Test**: A developer can create a new data migration by following the guide without external wiki reference.

**Acceptance Scenarios**:

1. **Given** a developer needs to write a data migration, **When** they check `docs/architecture/data-migrations.md`, **Then** they find step-by-step instructions
2. **Given** a migration requires FLEx Bridge coordination, **When** the developer reads the guide, **Then** they understand the metadata cache migration requirement

---

### User Story 4 - Coding Standards Reference (Priority: P3)

A developer wants to ensure their code follows FieldWorks conventions. They find coding standards in a discoverable location within the repo, integrated with existing `.editorconfig` and instruction files.

**Why this priority**: Consistent code style improves maintainability. Having standards in-repo makes them enforceable and discoverable.

**Independent Test**: A developer can verify their code meets standards by referencing in-repo documentation and tooling.

**Acceptance Scenarios**:

1. **Given** a developer writes new code, **When** they check for coding/formatting and commit conventions, **Then** they find `.editorconfig` and `.github/commit-guidelines.md`
2. **Given** the coding standards exist, **When** they are compared to `.editorconfig`, **Then** they are consistent and complementary

---

### Edge Cases

- What happens when wiki content is outdated or conflicts with current repo state? **Decision**: Current repo state takes precedence; outdated wiki content is either updated or marked as historical.
- What happens when wiki pages reference Gerrit/Jenkins workflows that no longer apply? **Decision**: Update to GitHub-native equivalents or mark as historical context.
- What happens when wiki images/screenshots are referenced? **Decision**: Download and store in `docs/images/` with appropriate attribution.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Repository MUST use a split documentation strategy: `.github/instructions/` for code guidance (Copilot-facing), `docs/` for onboarding/tutorials (human-facing)
- **FR-002**: `ReadMe.md` MUST link to the main developer documentation entry point
- **FR-003**: Documentation MUST include a contributor setup guide covering Windows prerequisites and build steps
- **FR-004**: Documentation MUST include a git workflow guide updated for GitHub (replacing Gerrit references)
- **FR-005**: Documentation MUST make coding/formatting and commit conventions discoverable via `.editorconfig` and `.github/commit-guidelines.md` (and MUST avoid duplicating guidance across multiple sources)
- **FR-006**: Documentation MUST include data migration authoring guide with current file paths and procedures
- **FR-007**: Documentation MUST NOT contain broken internal links
- **FR-008**: Documentation MUST use relative links for in-repo references
- **FR-009**: Each migrated item MUST be validated against codebase: confirmed active → migrate with GitHub equivalent; confirmed obsolete → remove; uncertain → mark with `CONFIRMATION_NEEDED` annotation
- **FR-010**: Platform-specific documentation MUST be evaluated against current repo reality (active → migrate; obsolete → remove; uncertain → mark with `CONFIRMATION_NEEDED`)

### Key Entities

- **Documentation Category**: A logical grouping of related documentation (e.g., "Getting Started", "Workflows", "Architecture")
- **Wiki Page**: An individual page from the FwDocumentation wiki to be migrated or archived
- **Migration Status**: Whether a wiki page is migrated, updated, archived, or deprecated

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of essential wiki pages (Contributing, Setup, Coding Standards, Data Migrations) are migrated to `docs/`
- **SC-002**: Zero broken internal links in migrated documentation
- **SC-003**: New contributor can complete first build within 2 hours following only in-repo documentation
- **SC-004**: ReadMe.md contains clear entry points to developer documentation
- **SC-005**: All Gerrit-specific workflow content is updated to GitHub equivalents or clearly marked as historical

## Assumptions

- The FwDocumentation wiki (https://github.com/sillsdev/FwDocumentation/wiki) remains the source of truth for existing content during migration
- Modern GitHub conventions include: `CONTRIBUTING.md`, `docs/` folder, relative links, PR templates
- Some wiki content may be obsolete (e.g., Gerrit, Jenkins references) and requires updating rather than direct migration
- Linux development documentation should be preserved even though the primary target is Windows
- After migration, a deprecation notice will be added to the wiki manually (out of scope for this feature)

## Clarifications

### Session 2025-12-01

- Q: Where should migrated wiki content primarily live? → A: `.github/instructions/` for code guidance; `docs/` for onboarding/tutorials
- Q: What should happen to the original FwDocumentation wiki? → A: Add deprecation notice pointing to in-repo docs (manual, out of scope)
- Q: How should legacy Gerrit/Jenkins workflow content be handled? → A: Rewrite to GitHub equivalents; validate each item against codebase (active → migrate, obsolete → remove, uncertain → mark `CONFIRMATION_NEEDED`)

## Constitution Alignment Notes

- Data integrity: N/A - this feature does not alter stored data or schemas
- Internationalization: Documentation should reference localization workflows (Crowdin) where applicable
- Licensing: No new third-party libraries introduced; documentation is under the same license as the repository
