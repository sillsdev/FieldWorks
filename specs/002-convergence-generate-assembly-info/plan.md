# Implementation Plan: GenerateAssemblyInfo Template Reintegration

**Branch**: `spec/002-convergence-generate-assembly-info` | **Date**: 2025-11-14 | **Spec**: `specs/002-convergence-generate-assembly-info/spec.md`
**Input**: Feature specification from `/specs/002-convergence-generate-assembly-info/spec.md`

## Summary

FieldWorks currently mixes SDK-generated and manually managed assembly metadata, causing duplicate attribute warnings and lost custom metadata. This plan audits all 115 managed projects, re-introduces the shared `CommonAssemblyInfoTemplate` by linking `Src/CommonAssemblyInfo.cs`, restores any deleted per-project `AssemblyInfo*.cs`, and enforces `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` with explanatory comments. Supporting scripts (audit, convert, validate) will automate the remediation, while repository-wide documentation (`Directory.Build.props`, scaffolding templates, managed instructions) is brought up to date and repeatable compliance signals are produced before merge.

**Process Controls**: Phase 2 now generates a `restore_map.json` by diffing git history and requires an "ambiguous project" checkpoint before any conversions land. Later phases re-validate that every historically-present `AssemblyInfo*.cs` exists post-conversion and log follow-up GitHub issues for anything still pending manual action.

## Technical Context

**Language/Version**: C# (.NET Framework 4.8, SDK-style csproj) plus Python 3.11 scripts for automation
**Primary Dependencies**: MSBuild 17.x, CommonAssemblyInfoTemplate pipeline (`Src/CommonAssemblyInfoTemplate.cs` → `Src/CommonAssemblyInfo.cs`), git history access, Python stdlib + `xml.etree.ElementTree`
**Storage**: N/A (metadata lives in source-controlled `.csproj`/`AssemblyInfo.cs` files)
**Testing**: MSBuild Debug/Release builds, reflection harness to inspect restored attributes, FieldWorks NUnit/regression suites, custom validation script output reviewed in CI, and build-time telemetry for the ±5% guardrail
**Target Platform**: Windows x64 developer container `fw-agent-1` (Visual Studio 2022 toolset)
**Project Type**: Large multi-project desktop solution (FieldWorks.sln with 115 managed csproj)
**Performance Goals**: Zero CS0579 warnings, no net increase in MSBuild wall-clock time beyond ±5%, template regeneration remains under 1s
**Constraints**: Must run inside fw-agent containers, retain legacy AssemblyInfo namespaces, avoid touching runtime behavior outside metadata, document every exception inline
**Scale/Scope**: 115 managed projects across `Src/**`, plus Build infrastructure updates and three repository scripts

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Pre-Phase Status**
- **Data integrity**: Metadata-only change; plan restores any deleted AssemblyInfo files from git history (Tasks 1.2/2.3) ensuring no data loss. PASS
- **Test evidence**: Validation script + Debug/Release MSBuild runs (Phase 4) cover risk areas; installer build also observed. PASS
- **I18n/script correctness**: Assembly attributes affect localized product strings; template already centralized; no new rendering paths but reflection spot-checks ensure multilingual correctness. PASS
- **Licensing**: No new dependencies beyond Python stdlib; LGPL 2.1+ already satisfied. PASS
- **Stability/performance**: Build-only change; constraints capture ±5% tolerance and require CI validation. PASS

**Post-Phase Status (after design)**
- Same as above with added OpenAPI contract + script quickstart documenting mitigations and execution order. PASS

## Project Structure

### Documentation (this feature)

```text
specs/002-convergence-generate-assembly-info/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── generate-assembly-info.yaml
└── tasks.md   # produced by /speckit.tasks (future)
```

### Source Code (repository root)

```text
Src/
├── Common/
│   ├── CommonAssemblyInfoTemplate.cs
│   └── CommonAssemblyInfo.cs      # generated; linked into every project
├── Common/FieldWorks/FieldWorks.csproj
├── CacheLight/CacheLight.csproj
└── ... (112 additional managed projects consuming the template)

Build/
├── SetupInclude.targets           # generates CommonAssemblyInfo.cs
└── Src/FwBuildTasks/...           # localization tasks referencing CommonAssemblyInfo

scripts/
└── GenerateAssemblyInfo/
    ├── audit_generate_assembly_info.py
    ├── convert_generate_assembly_info.py
    └── validate_generate_assembly_info.py

tests/
└── (existing NUnit suites executed after remediation)

Directory.Build.props
└── Centralized documentation for template usage and GenerateAssemblyInfo comments

scripts/templates/
└── Project scaffolding artifacts updated so new csproj files import the restored template automatically
```

**Structure Decision**: Reuse existing `Src/**` csproj locations, centralize automation under `scripts/GenerateAssemblyInfo/`, and update Build tooling/validation so template enforcement is consistent across FieldWorks solutions.

**Complexity Tracking**

**Final Statistics (Audit)**:
- Template-only: 25
- Template+Custom: 76
- NeedsFix: 0

**Validation Enhancements**: Phase 5 now layers structural checks, deterministic MSBuild invocations, a tiny reflection harness that inspects the regenerated assemblies, a full FieldWorks test-suite sweep, and timestamped build logs so the ±5% performance guardrail is enforced with evidence captured in `Output/GenerateAssemblyInfo/`.
**Escalation Workflow**: The validation report cross-references `restore_map.json` to ensure no historic AssemblyInfo files were lost, while Phase 6 tracks unresolved projects via follow-up GitHub issues linked from the spec’s review section.
No Constitution violations anticipated; table not required.
