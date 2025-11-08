# Implementation Plan: Test Exclusion Pattern Standardization

**Branch**: `004-convergence-test-exclusion-patterns` | **Date**: 2025-11-08 | **Spec**: specs/004-convergence-test-exclusion-patterns/spec.md
**Input**: Feature specification from `/specs/004-convergence-test-exclusion-patterns/spec.md`

## Summary

Standardize Compile exclusion patterns for test files across 35 SDK-style projects to eliminate inconsistency and confusion. Currently 3 different patterns in use: Pattern A (`<ProjectName>Tests/**`), Pattern B (`*Tests/**`), and Pattern C (various custom patterns). Technical approach: audit all test projects to identify current exclusion patterns, establish canonical pattern (Pattern A: explicit project-name-based exclusion for clarity), generate conversion script with dry-run mode, apply pattern consistently across all projects, validate that builds succeed and no test files are inadvertently excluded or included. Expected outcome: Single consistent pattern across solution, clear guidelines for new projects, reduced cognitive overhead for developers.

## Technical Context

**Language/Version**: Python 3.11+ (for automation scripts), MSBuild/C# (.NET Framework 4.8 for project files)
**Primary Dependencies**: xml.etree.ElementTree (Python standard library for .csproj parsing)
**Storage**: CSV files for audit results
**Testing**: pytest for script unit tests, MSBuild for build validation
**Target Platform**: Windows (developer workstations and CI)
**Project Type**: Build system convergence
**Performance Goals**: Audit script <20 seconds for 35 projects, conversion script <30 seconds, build time unchanged
**Constraints**: Must not inadvertently exclude non-test files, must preserve explicit inclusions if present, must maintain compatibility with SDK-style Compile auto-inclusion, changes must be deterministic and reversible
**Scale/Scope**: 35 test projects with Compile exclusions (22 with Pattern A, 8 with Pattern B, 5 with Pattern C or variations)

Open unknowns resolved in research.md:
- **D1**: Canonical pattern selection rationale (Pattern A vs. B vs. C) — Pattern A selected for explicitness and clarity
- **D2**: Edge cases: projects with both test and non-test code (how to exclude only test portion) — NEEDS CLARIFICATION
- **D3**: Whether to enforce exclusions via Directory.Build.props vs. per-project — NEEDS CLARIFICATION: Per-project clearer but more verbose; props-based more DRY but less explicit

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Data integrity**: Project files (.csproj) modified (Compile Remove elements). Git-backed. Validation ensures only intended exclusions changed. — **PASS**
- **Test evidence**: Changes affect which files are compiled. Must include: (1) Build validation (all projects compile), (2) Test execution (all tests still discovered and run), (3) Verification that no non-test files excluded. — **REQUIRED** (validation Phase 5)
- **I18n/script correctness**: Test files may include internationalization test data. No impact on runtime rendering. — **N/A**
- **Licensing**: No new dependencies. — **PASS**
- **Stability/performance**: Low-risk change (build-time only). Risk: inadvertently excluding source files or including test files in production. Mitigation: dry-run validation, build smoke tests. — **PASS** (mitigated)

Proceed to Phase 0 with clarifications needed for D2 and D3.

Post-design re-check (after Phase 1 artifacts added):
- Data integrity: Git-backed, validated — **PASS**
- Test evidence: Build + test execution validation planned — **VERIFY IN TASKS**
- I18n/script correctness: N/A — **PASS**
- Licensing: No new deps — **PASS**
- Stability/performance: Validation mitigates risk — **PASS**

## Project Structure

### Documentation (this feature)

```text
specs/004-convergence-test-exclusion-patterns/
├── spec.md              # Feature specification (existing)
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0 output (pattern analysis, edge cases, decision rationale)
├── data-model.md        # Phase 1 output (TestProjectEntity, ExclusionPattern models)
├── quickstart.md        # Phase 1 output (how to run audit/conversion scripts)
├── contracts/           # Phase 1 output
│   ├── audit-cli.md
│   ├── convert-cli.md
│   └── validate-cli.md
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
convergence/
├── audit_framework.py                    # Base classes (existing)
├── convergence.py                        # Unified CLI (existing)
├── convergence_test_exclusion_patterns.py # New: Extends base classes
│   ├── class TestExclusionAuditor(ConvergenceAuditor)
│   ├── class TestExclusionConverter(ConvergenceConverter)
│   └── class TestExclusionValidator(ConvergenceValidator)
└── tests/
    └── test_test_exclusion_patterns.py   # Unit tests

Src/
└── [35 test .csproj files]               # Target files (modify Compile Remove elements)

Build/
└── Directory.Build.props                 # NEEDS CLARIFICATION: Consider centralizing exclusion pattern here
```

**Structure Decision**: Extend convergence framework with test exclusion-specific auditor/converter. Audit detects current patterns (A/B/C), converter applies canonical pattern (Pattern A by default), validator ensures build succeeds and test discovery works. CSV output allows human review before applying changes. Dry-run mode prevents accidental exclusions.

**NEEDS CLARIFICATION**: Whether to centralize pattern in Directory.Build.props (e.g., `<CompileRemove>$(ProjectName)Tests/**</CompileRemove>`) for DRY or keep per-project for explicitness. Centralized approach requires MSBuild property expansion understanding; per-project more straightforward but verbose.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations. No entries required.
