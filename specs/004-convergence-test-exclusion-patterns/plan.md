# Implementation Plan: Convergence 004 – Test Exclusion Pattern Standardization

**Branch**: `spec/004-convergence-test-exclusion-patterns` | **Date**: 2025-11-14 | **Spec**: `specs/004-convergence-test-exclusion-patterns/spec.md`
**Input**: Feature specification from `/specs/004-convergence-test-exclusion-patterns/spec.md`

**Note**: Generated via `/speckit.plan`; this file now lives with the rest of the feature artifacts in `specs/004-convergence-test-exclusion-patterns/`.

## Summary

FieldWorks currently uses three competing SDK-style test exclusion patterns, increasing maintenance risk and allowing test code to leak into production assemblies. This plan standardizes every SDK-style project on the explicit `<ProjectName>Tests/**` convention (Pattern A), adds nested-folder coverage via targeted entries, and streamlines auditing, conversion, and validation so CS0436 regressions are prevented. Tooling deliverables include repo-wide audit/convert/validate scripts that refresh the authoritative report after every conversion batch, a mixed-code escalation report with owner hand-off guidance, a reflection-based assembly guard plus log-parsing helpers, per-project policy updates (including the SDK project template), and documentation refresh steps (COPILOT + instructions) that enforce the clarified "no mixed test code" rule alongside required unit/CLI tests for every enforcement tool.

## Technical Context

**Language/Version**: C#/.NET Framework projects using SDK-style MSBuild + Python 3.11 helper scripts
**Primary Dependencies**: MSBuild traversal infrastructure, Directory.Build.props, custom Python tooling (audit/convert/validate)
**Storage**: N/A (edits confined to `.csproj` files and repo metadata)
**Testing**: `msbuild FieldWorks.proj` (Debug) plus targeted project builds; Python unit tests for helper scripts (audit, convert, validate, assembly guard); scripted log parsing that surfaces CS0436 conflicts without relying on pipeline automation
**Target Platform**: Windows 10/11 x64 developer environments (including hosted build agents when needed)
**Project Type**: Large multi-solution desktop suite (119 SDK-style projects spanning managed/native code)
**Performance Goals**: Zero CS0436 errors, no test folders copied to production outputs, conversions complete within one working session (~4 hours)
**Constraints**: Must keep exclusions explicit per project, detect mixed test/non-test code and flag manually, avoid breaking existing build ordering, operate within Git worktree/container model
**Scale/Scope**: ~80 projects with existing exclusions plus ~40 candidates requiring verification; thousands of `.csproj` lines touched across `Src/**`

## Constitution Check

*Gate status before Phase 0*: **PASS** – No persisted data or schema changes occur; work is limited to build metadata.

- **Data integrity**: Not applicable (no user data touched). Risk mitigation focuses on ensuring production assemblies stay test-free; validation scripts will block regressions.
- **Test evidence**: Repeatable MSBuild traversal builds plus script-level unit tests will demonstrate pattern compliance. Each conversion batch requires at least one FieldWorks Debug build.
- **I18n/script correctness**: No UI/text rendering impact; existing guidance maintained.
- **Licensing**: Helper scripts rely on Python standard library only; no new third-party licenses introduced.
- **Stability/performance**: Build risk mitigated via incremental conversion (Phase 2) and disciplined validation runs; no runtime feature flags needed.

Re-run this checklist after Phase 1 to confirm tooling design keeps these guarantees.

## Project Structure

### Documentation (this feature)

```text
specs/004-convergence-test-exclusion-patterns/
├── plan.md         # Current file
├── research.md     # Phase 0 decisions
├── data-model.md   # Phase 1 entity + relationship definitions
├── quickstart.md   # How to apply scripts + validation
├── contracts/      # OpenAPI describing automation endpoints
└── tasks.md        # Created later by /speckit.tasks
```

### Source Code (repository root)

```text
Src/
├── Common/*                # Majority of SDK-style class libraries needing updates
├── LexText/*               # Application-specific projects & nested components
├── Utilities/*             # Shared tools (many with test subprojects)
└── XCore/*                 # Core frameworks and their paired tests

Build/
├── Agent/                  # Scripts for lint and other automation helpers
└── Src/NativeBuild/        # Included for completeness; no direct edits

scripts/
└── *.ps1 / *.py            # Location for new automation entry points if needed

.github/
└── workflows/              # Reference when sharing reusable automation snippets
```

**Structure Decision**: Operate within existing mono-repo layout—touch only `.csproj` files under `Src/**`, add helper scripts under `scripts/` (or `Build/Agent` if shared), update the SDK project template under `Src/Templates/` with Pattern A defaults, and update documentation under `.github/instructions` per spec Phase 3.

## Complexity Tracking

No Constitution violations are anticipated; section intentionally left empty.

## Phase 0: Outline & Research

1. **Unknown / Risk Inventory**
   - Verify automation approach for auditing current patterns vs. manual review.
   - Confirm conversion tooling strategy (scripted vs. hand edits) for 35+ projects.
   - Determine validation coverage (manual script runs plus traversal builds) that enforces the clarified "no mixed test code" rule.
2. **Research Tasks** (documented in `research.md`)
   - Research standard pattern rationale and why Pattern A best fits FieldWorks.
   - Document automation workflow (audit → convert → validate) including script responsibilities.
   - Capture policy for handling mixed test/non-test folders and escalation path.
   - Define validation checkpoints (scripted local runs + traversal builds) to prove exclusions remain correct.
3. **Outputs**
   - `research.md` now contains four decisions with rationale and alternative trade-offs, resolving all open questions.
   - Three user stories (US1 audit, US2 conversion, US3 validation + assembly guard) drive downstream planning and mapping.

## Phase 1: Design & Contracts

1. **Data Modeling**
   - `data-model.md` enumerates entities: Project, TestFolder, ExclusionRule, ValidationIssue, and ConversionJob, including relationships and validation rules.
2. **API/Automation Contracts**
   - `contracts/test-exclusion-api.yaml` (OpenAPI 3.1) defines endpoints for auditing projects, converting a project to Pattern A, and running validation. This mirrors the planned Python tooling surface for future automation or service wrappers.
3. **Quickstart Guide**
   - `quickstart.md` instructs developers on prerequisites, running audit/convert/validate scripts, handling mixed-test policy escalations (with concrete owner workflow), and documenting the manual validation + assembly guard steps alongside COPILOT refresh expectations.
4. **Agent Context Update**
   - `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot` has been executed so downstream agents inherit the new automation/tooling context.

## Phase 2: Implementation Planning Preview

- Break work into repeatable batches (10–15 projects per PR) leveraging the scripts defined above.
- Create `tasks.md` via `/speckit.tasks` to capture actionable units: script authoring, conversion sweeps (with audit regeneration per batch), documentation changes (instructions + template), COPILOT refreshes, mixed-code escalations, assembly guard automation, log-parsing helpers, and validation builds.
- Re-run the Constitution check once tooling proves it blocks CS0436 regressions and leaked test types; note any new risks before coding begins, explicitly calling out the required unit/CLI test coverage for validator + guard tooling.

No further gates remain before moving to `/speckit.tasks`.
