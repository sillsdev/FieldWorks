# Implementation Plan: GenerateAssemblyInfo Convergence

**Branch**: `002-convergence-generate-assembly-info` | **Date**: 2025-11-08 | **Spec**: specs/002-convergence-generate-assembly-info/spec.md
**Input**: Feature specification from `/specs/002-convergence-generate-assembly-info/spec.md`

## Summary

Standardize GenerateAssemblyInfo property across all 115 SDK-style projects in FieldWorks, establishing clear criteria for when to use SDK-generated metadata (true) vs. manual AssemblyInfo.cs files (false). Currently 52 projects use false and 63 use true with no documented rationale, causing confusion and potential duplicate attribute errors. Technical approach: audit all projects using Python scripts, apply decision criteria based on project characteristics (shared AssemblyInfo, COM visibility, project type), generate conversion recommendations with risk assessment, and provide automated conversion with dry-run validation. Expected outcome: 100% consistency, zero CS0579 errors, clear documentation for future project creation.

## Technical Context

**Language/Version**: Python 3.11+ (for automation scripts), MSBuild/C# (.NET Framework 4.8 for project files)
**Primary Dependencies**: xml.etree.ElementTree (Python standard library for .csproj parsing), subprocess (for MSBuild validation)
**Storage**: CSV files for audit results and conversion decisions (human-reviewable), git for version control
**Testing**: pytest for script unit tests, MSBuild for solution build validation
**Target Platform**: Windows (developer workstations and CI)
**Project Type**: Build system convergence with automated tooling
**Performance Goals**: Audit script <30 seconds for all 115 projects, conversion script <60 seconds, full solution build time unchanged (±2%)
**Constraints**: Must not break existing builds, must preserve manual AssemblyInfo.cs where functionally required (shared files, COM GUIDs), must support rollback via git, scripts must be deterministic (same input = same output)
**Scale/Scope**: 115 SDK-style projects across solution, ~15 projects with shared AssemblyInfo.cs references, ~8 COM-visible assemblies

Open unknowns resolved in research.md:
- Decision criteria for GenerateAssemblyInfo established (D1)
- Shared AssemblyInfo.cs pattern detection methodology defined (D2)
- COM-visible assembly identification approach confirmed (D3)
- Version stamping and strong naming compatibility verified (D4)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Data integrity**: Project files (.csproj) modified but backed by git version control. Validation script ensures no unintended changes beyond GenerateAssemblyInfo property. Backup/rollback via `git checkout` if issues detected. — **PASS** (git-backed)
- **Test evidence**: Changes affect build metadata generation. Must include: (1) Solution build validation pre/post convergence, (2) Assembly metadata inspection tests, (3) CS0579 error detection in build output. — **REQUIRED** (validation script Phase 5)
- **I18n/script correctness**: Assembly attributes may include culture-specific strings (AssemblyCulture, NeutralResourcesLanguage). Review projects with explicit culture attributes and ensure SDK generation respects existing patterns or retains manual control. — **REQUIRED** (audit Phase 3)
- **Licensing**: Python scripts use standard library only (no new dependencies). No third-party libraries introduced. — **PASS**
- **Stability/performance**: Low-risk change (metadata generation only, no runtime behavior change). Validation ensures no CS0579 errors. Build time impact measured (<2% tolerance). Rollback via git if unexpected issues. — **PASS** (mitigated via validation)

Proceed to Phase 0 with required validations planned for Phase 3 (I18n check) and Phase 5 (test evidence).

Post-design re-check (after Phase 1 artifacts added):
- Data integrity: Git-backed, validated — **PASS**
- Test evidence: Validation script includes build check, metadata inspection, error detection — **PASS**
- I18n/script correctness: Culture attribute handling documented in research.md — **PASS**
- Licensing: No new deps — **PASS**
- Stability/performance: Validation gates ensure safety — **PASS**

## Project Structure

### Documentation (this feature)

```text
specs/002-convergence-generate-assembly-info/
├── spec.md              # Feature specification (existing)
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0 output (decision criteria, patterns, edge cases)
├── data-model.md        # Phase 1 output (ProjectEntity, AuditResult, ConversionDecision models)
├── quickstart.md        # Phase 1 output (how to run audit/conversion/validation scripts)
├── contracts/           # Phase 1 output (script CLIs, CSV schemas)
│   ├── audit-cli.md
│   ├── convert-cli.md
│   ├── validate-cli.md
│   └── csv-schemas.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
convergence/
├── audit_framework.py                    # Base classes (existing from CONVERGENCE-FRAMEWORK.md)
├── convergence.py                        # Unified CLI (existing)
├── convergence_generate_assembly_info.py # New: Extends base classes for this convergence
│   ├── class GenerateAssemblyInfoAuditor(ConvergenceAuditor)
│   ├── class GenerateAssemblyInfoConverter(ConvergenceConverter)
│   └── class GenerateAssemblyInfoValidator(ConvergenceValidator)
└── tests/
    └── test_generate_assembly_info.py    # Unit tests for convergence script

Build/
└── Directory.Build.props                 # Reference point for inherited properties (no changes)

Src/
└── [115 .csproj files]                   # Target files for convergence (modify GenerateAssemblyInfo property)
```

**Structure Decision**: Extend existing convergence framework (`audit_framework.py` base classes) with convergence-specific implementation in `convergence_generate_assembly_info.py`. This follows the DRY architecture established in CONVERGENCE-FRAMEWORK.md, reuses common utilities (parse_csproj, find_property_value), and integrates with unified CLI (`python convergence.py generate-assembly-info <action>`). Scripts output CSV for human review and support dry-run mode for safe validation before applying changes.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations. No entries required.
