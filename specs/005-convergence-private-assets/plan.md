# Implementation Plan: PrivateAssets Standardization for Test Packages

**Branch**: `005-convergence-private-assets` | **Date**: 2025-11-08 | **Spec**: specs/005-convergence-private-assets/spec.md
**Input**: Feature specification from `/specs/005-convergence-private-assets/spec.md`

## Summary

Standardize PrivateAssets="All" on test framework PackageReferences (NUnit, Moq, FluentAssertions, etc.) across 26 test projects to prevent test dependencies from leaking into consuming projects. Currently inconsistently applied: some test projects have PrivateAssets, others don't, creating potential dependency pollution. Technical approach: audit all test projects for test-related PackageReferences, identify which lack PrivateAssets, generate conversion script to add PrivateAssets="All" to identified packages, validate that builds succeed and test dependencies are properly isolated. Expected outcome: Consistent package isolation across all tests, no test packages appearing in production project dependency graphs, clear pattern for future test projects.

## Technical Context

**Language/Version**: Python 3.11+ (for automation scripts), MSBuild/C# (.NET Framework 4.8 for project files)
**Primary Dependencies**: xml.etree.ElementTree (Python standard library for .csproj parsing)
**Storage**: CSV files for audit results
**Testing**: pytest for script unit tests, MSBuild for build validation, dependency graph validation
**Target Platform**: Windows (developer workstations and CI)
**Project Type**: Build system convergence
**Performance Goals**: Audit script <15 seconds for 26 projects, conversion script <20 seconds, build time unchanged
**Constraints**: Must only affect test packages (not production dependencies), must preserve existing PrivateAssets if already set, must not break test execution, must handle both direct and transitive package references appropriately
**Scale/Scope**: 26 test projects, estimated 4-6 test packages per project (NUnit, NUnit3TestAdapter, Moq, FluentAssertions, etc.)

Open unknowns resolved in research.md:
- **D1**: Definitive list of test framework packages requiring PrivateAssets — Base list: NUnit, NUnit3TestAdapter, NUnit.Console, Moq, FluentAssertions, xunit.*, Microsoft.NET.Test.Sdk, coverlet.*, MSTest.* (expand in research if others found)
- **D2**: Whether to apply PrivateAssets to all packages in test projects or only known test frameworks — NEEDS CLARIFICATION: Known frameworks only (conservative) vs. all non-production packages (aggressive)
- **D3**: Handling of packages with existing PrivateAssets values (e.g., PrivateAssets="Compile") — NEEDS CLARIFICATION: Overwrite to "All" vs. merge vs. skip

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Data integrity**: Project files (.csproj) modified (PackageReference attributes). Git-backed. Validation ensures only PrivateAssets added/modified. — **PASS**
- **Test evidence**: Changes affect package dependency resolution. Must include: (1) Build validation (all projects compile), (2) Test execution (all tests run successfully), (3) Dependency graph verification (test packages not in production graphs). — **REQUIRED** (validation Phase 5)
- **I18n/script correctness**: No impact on internationalization. — **N/A**
- **Licensing**: No new dependencies. Test packages already present, only metadata changed. — **PASS**
- **Stability/performance**: Low-risk change (build-time metadata only). Risk: breaking transitive dependencies if PrivateAssets too aggressive. Mitigation: apply only to known test frameworks, validate test execution. — **PASS** (mitigated)

Proceed to Phase 0 with clarifications needed for D2 and D3.

Post-design re-check (after Phase 1 artifacts added):
- Data integrity: Git-backed, validated — **PASS**
- Test evidence: Build + test + dependency graph validation planned — **VERIFY IN TASKS**
- I18n/script correctness: N/A — **PASS**
- Licensing: No new deps — **PASS**
- Stability/performance: Validation mitigates risk — **PASS**

## Project Structure

### Documentation (this feature)

```text
specs/005-convergence-private-assets/
├── spec.md              # Feature specification (existing)
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0 output (package list, edge cases, policy decisions)
├── data-model.md        # Phase 1 output (TestProjectEntity, PackageReferenceEntity models)
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
├── convergence_private_assets.py         # New: Extends base classes
│   ├── class PrivateAssetsAuditor(ConvergenceAuditor)
│   ├── class PrivateAssetsConverter(ConvergenceConverter)
│   └── class PrivateAssetsValidator(ConvergenceValidator)
└── tests/
    └── test_private_assets.py            # Unit tests

Src/
└── [26 test .csproj files]               # Target files (modify PackageReference PrivateAssets)
```

**Structure Decision**: Extend convergence framework with PrivateAssets-specific auditor/converter. Auditor scans test projects for test framework PackageReferences lacking PrivateAssets, converter adds PrivateAssets="All" to identified packages, validator runs build and checks dependency graphs. Use conservative approach: apply only to known test frameworks initially (extensible list in script config).

**NEEDS CLARIFICATION**: 
1. Whether to apply PrivateAssets to all packages in test projects (aggressive, ensures isolation but may break edge cases) or only to known test frameworks (conservative, safer but may miss some test packages).
2. How to handle packages with existing PrivateAssets (e.g., PrivateAssets="Compile" for analyzers): overwrite to "All", merge, or skip.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations. No entries required.
