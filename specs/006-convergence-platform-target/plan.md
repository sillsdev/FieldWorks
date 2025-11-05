# Implementation Plan: PlatformTarget Redundancy Cleanup

**Branch**: `006-convergence-platform-target` | **Date**: 2025-11-08 | **Spec**: specs/006-convergence-platform-target/spec.md
**Input**: Feature specification from `/specs/006-convergence-platform-target/spec.md`

## Summary

Remove redundant explicit PlatformTarget=x64 settings from 22 projects where x64 is already inherited from Directory.Build.props or solution platform configuration. This cleanup reduces noise in project files, improves maintainability by centralizing platform configuration, and eliminates confusion about where platform target is defined. Technical approach: audit all projects to identify explicit PlatformTarget properties, determine which are redundant (inherited value matches explicit value), generate removal script with dry-run mode, remove redundant properties while preserving explicit settings where functionally required (e.g., AnyCPU overrides), validate that builds produce identical outputs. Expected outcome: Cleaner project files, single source of truth for platform configuration, reduced maintenance burden when platform requirements change.

## Technical Context

**Language/Version**: Python 3.11+ (for automation scripts), MSBuild/C# (.NET Framework 4.8 for project files)
**Primary Dependencies**: xml.etree.ElementTree (Python standard library for .csproj parsing), MSBuild for property resolution evaluation
**Storage**: CSV files for audit results
**Testing**: pytest for script unit tests, MSBuild for build validation, binary comparison of outputs pre/post cleanup
**Target Platform**: Windows (developer workstations and CI)
**Project Type**: Build system convergence
**Performance Goals**: Audit script <20 seconds for all projects, conversion script <30 seconds, build time unchanged, output binaries byte-identical pre/post cleanup
**Constraints**: Must preserve explicit PlatformTarget where needed (AnyCPU libraries, conditional settings), must not change build outputs, must maintain solution platform configurations, must be reversible via git
**Scale/Scope**: 22 projects with redundant explicit x64 settings, total 115 projects to audit for comprehensive coverage

Open unknowns resolved in research.md:
- **D1**: Complete list of projects with explicit PlatformTarget and whether each is redundant — To be determined by audit script
- **D2**: Edge cases where explicit x64 is required despite inheritance (e.g., multi-targeting, conditional compilation) — NEEDS CLARIFICATION
- **D3**: Whether to also clean up Platform property in addition to PlatformTarget — NEEDS CLARIFICATION: Platform vs. PlatformTarget semantics in MSBuild
- **D4**: Whether AnyCPU projects should explicitly set PlatformTarget=AnyCPU or rely on SDK default — NEEDS CLARIFICATION: Explicit vs. implicit AnyCPU policy

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Data integrity**: Project files (.csproj) modified (PlatformTarget property removed). Git-backed. Validation ensures build outputs unchanged. — **PASS**
- **Test evidence**: Changes affect build configuration. Must include: (1) Build validation (all projects compile), (2) Binary comparison (outputs byte-identical pre/post), (3) Test execution (all tests pass). — **REQUIRED** (validation Phase 5)
- **I18n/script correctness**: No impact on internationalization. — **N/A**
- **Licensing**: No new dependencies. — **PASS**
- **Stability/performance**: Very low-risk change (removes redundant metadata only). Risk: inadvertently removing functionally required explicit settings. Mitigation: conservative audit (only remove if provably redundant), dry-run validation, binary comparison. Rollback via git if any output changes. — **PASS** (mitigated)

Proceed to Phase 0 with clarifications needed for D2, D3, and D4.

Post-design re-check (after Phase 1 artifacts added):
- Data integrity: Git-backed, validated — **PASS**
- Test evidence: Build + binary comparison + test validation planned — **VERIFY IN TASKS**
- I18n/script correctness: N/A — **PASS**
- Licensing: No new deps — **PASS**
- Stability/performance: Validation ensures no changes — **PASS**

## Project Structure

### Documentation (this feature)

```text
specs/006-convergence-platform-target/
├── spec.md              # Feature specification (existing)
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0 output (MSBuild property evaluation, edge cases, policy)
├── data-model.md        # Phase 1 output (ProjectEntity, PlatformSettingEntity models)
├── quickstart.md        # Phase 1 output (how to run audit/cleanup scripts)
├── contracts/           # Phase 1 output
│   ├── audit-cli.md
│   ├── cleanup-cli.md
│   └── validate-cli.md
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
convergence/
├── audit_framework.py                    # Base classes (existing)
├── convergence.py                        # Unified CLI (existing)
├── convergence_platform_target.py        # New: Extends base classes
│   ├── class PlatformTargetAuditor(ConvergenceAuditor)
│   ├── class PlatformTargetCleanup(ConvergenceConverter)
│   └── class PlatformTargetValidator(ConvergenceValidator)
└── tests/
    └── test_platform_target.py           # Unit tests

Build/
└── Directory.Build.props                 # Reference for inherited PlatformTarget value

Src/
└── [22+ .csproj files]                   # Target files (remove redundant PlatformTarget)
```

**Structure Decision**: Extend convergence framework with PlatformTarget-specific auditor/cleanup tool. Auditor evaluates effective PlatformTarget for each project (resolves MSBuild property inheritance), compares to explicit setting, flags redundancies. Cleanup tool removes redundant explicit properties. Validator performs binary comparison of build outputs to ensure no functional changes. Conservative approach: only remove if provably redundant (inherited value identical to explicit value and no conditionals present).

**NEEDS CLARIFICATION**:
1. Edge cases where explicit PlatformTarget=x64 is functionally required despite matching inherited value (e.g., projects with conditional compilation, multi-targeting, or platform-specific code).
2. Whether to clean up both `<PlatformTarget>` and `<Platform>` properties or only PlatformTarget. MSBuild semantics of Platform vs. PlatformTarget need clarification.
3. Policy for AnyCPU projects: should they explicitly set PlatformTarget=AnyCPU for clarity or rely on SDK default (implicit AnyCPU)? Current state inconsistent.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations. No entries required.
