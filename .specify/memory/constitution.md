# FieldWorks (FLEx) Constitution
<!--
Sync Impact Report
- Version change: n/a → 1.0.0
- Modified principles: n/a (initial adoption)
- Added sections: Core Principles; Additional Constraints; Development Workflow & Quality Gates; Governance
- Removed sections: none
- Templates requiring updates:
	- ✅ .specify/templates/plan-template.md (Constitution Check gates aligned)
	- ✅ .specify/templates/spec-template.md (edge cases/reminders aligned)
	- ✅ .specify/templates/tasks-template.md (task guidance aligned)
	- ⚠ .specify/templates/commands/*.md (no command templates found in repo; none updated)
- Deferred TODOs:
	- TODO(RATIFICATION_DATE): Original adoption date not known; update when confirmed.
-->

## Core Principles

### I. Data Integrity and Backward Compatibility
FieldWorks MUST preserve user data across versions. All schema/data changes MUST include
safe, tested migrations with rollback/backup guidance. Any change that risks data loss
MUST be explicitly flagged, reviewed, and accompanied by a mitigation plan.

### II. Test and Review Discipline (Non‑Negotiable)
Changes that affect core data, text rendering/layout, installers, or public contracts
MUST include automated tests (unit and/or integration) and pass code review. Red‑Green‑
Refactor is encouraged; PRs MUST state test coverage for risk areas and link to failing
tests when fixing defects.

### III. Internationalization and Script Correctness
The system MUST handle complex scripts and multilingual data correctly. Rendering and
text processing MUST maintain correctness for non‑Latin scripts and use established
libraries (e.g., ICU/Graphite) where applicable. Regressions in script handling are
release‑blocking.

### IV. User‑Centered Stability and Performance
Windows is the primary supported platform. Releases MUST prioritize stability, predictable
workflows, and sensible performance on typical field hardware, including offline use.
Feature flags or staged rollouts SHOULD be used for risky changes.

### V. Licensing and Open Collaboration
The codebase is licensed under GNU LGPL 2.1 or later. All contributions MUST comply with
license requirements and third‑party license obligations. Development occurs openly on
GitHub following documented coding standards and review practices.

### VI. Documentation Fidelity
Architecture and folder documentation (including `Src/**/COPILOT.md`) MUST match released behavior. Unknowns SHOULD be recorded with targeted `FIXME(<topic>)` markers until verified—never replaced by speculation.

## Additional Constraints

- Platform scope: Windows-only builds today; document tooling/platform changes before release.
- Data migrations: Provide migration plans, backup expectations, and tested upgrade paths for every schema/model change.
- Accessibility & localization: Ensure new UI stays accessible/localizable and externalize user-facing strings.

## Development Workflow & Quality Gates

- All PRs MUST describe risk, cite test evidence (or justify omissions), and state data impact.
- Changes affecting rendering, migrations, or installers MUST include integration tests or scripted validation steps.
- Updates touching `Src/**` code MUST either refresh the paired `COPILOT.md` or explain why existing documentation remains accurate.
- “Constitution Check” gates in feature plans MUST confirm migration coverage, script/i18n validation, test coverage for risk areas, and licensing compliance for new dependencies.

## Governance

This Constitution guides FieldWorks development practices and supersedes conflicting
guidance elsewhere. Amendments are proposed via GitHub pull request with:

1. A summary of the change and rationale.
2. Version bump per semantic rules below.
3. A migration/communication plan when altering principles or gates.

Constitution Versioning:
- MAJOR: Backward‑incompatible governance changes (e.g., remove/redefine principles).
- MINOR: Add a new principle/section or materially expand guidance.
- PATCH: Clarifications and non‑semantic refinements.

Compliance Review:
- Reviewers MUST verify “Constitution Check” gates are satisfied before approval.
- CI SHOULD enforce lint/test gates and block if required evidence is missing.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE) | **Last Amended**: 2025-10-28
