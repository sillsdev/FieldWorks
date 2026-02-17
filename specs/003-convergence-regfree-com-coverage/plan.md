# Implementation Plan: RegFree COM Coverage Completion
```
scripts/
└── regfree/
    ├── audit_com_usage.py            # COM usage audit CLI
    ├── add_regfree_manifest.py       # Manifest wiring helper
    ├── validate_regfree_manifests.py # XML + VM validation harness
    ├── run-in-vm.ps1                 # Clean VM launcher for EXEs
    └── common.py / project_map.json  # Shared metadata + configuration
```

**Structure Decision**: Extend existing `RegFree` MSBuild task in `FwBuildTasks` to support managed assemblies. The task will use .NET Reflection to identify `[ComVisible]` classes and `[Guid]` attributes in managed DLLs, eliminating the need for external Python build dependencies. Each EXE project imports the target and specifies its unique COM dependencies. COM audit script identifies which COM servers each EXE activates. Manifest validation script confirms all identified CLSIDs are present in generated manifests.

**NEEDS CLARIFICATION**: Whether to create per-EXE manifest files or a shared manifest covering all COM servers (shared approach simpler but larger manifest files).
**Language/Version**: C# (.NET Framework 4.8), C++/C++/CLI (MSVC current toolset), MSBuild (for RegFree.targets extension)
**Primary Dependencies**: Existing RegFree.targets build task, manifest generation tooling from 001-64bit-regfree-com
**Storage**: N/A (manifest files generated at build time, co-located with EXEs)
**Testing**: Manual smoke tests on clean VM, automated launch validation in CI
**Target Platform**: Windows x64 (Windows 10/11)
**Project Type**: Build system extension + manifest generation
**Performance Goals**: No runtime performance impact, manifest generation adds <5 seconds to build time per EXE
**Constraints**: Must cover all COM servers activated by each EXE, manifests must include correct CLSIDs/IIDs/TLBs, must not break existing FieldWorks.exe manifest
**Scale/Scope**: NEEDS CLARIFICATION: Exact count of EXEs requiring manifests (estimated 5-7 plus test executables)

Open unknowns to resolve in research.md:
- **D1**: Complete inventory of FieldWorks EXEs (names, paths, purposes) — NEEDS CLARIFICATION
- **D2**: COM usage audit methodology (manual code review vs. automated detection) — NEEDS CLARIFICATION
- **D3**: Whether test executables need individual manifests or can share a test host manifest (similar to 001 spec) — NEEDS CLARIFICATION
- **D4**: Whether any EXEs have unique COM dependencies not covered by existing RegFree.targets patterns — NEEDS CLARIFICATION

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Data integrity**: No data schema changes. Project files (.csproj) modified to import RegFree.targets. Manifests generated at build time. Git-backed. — **PASS**
- **Test evidence**: Affects COM activation and application launch. Must include: (1) Smoke tests for each EXE on clean VM, (2) COM activation validation (no class-not-registered errors), (3) Automated launch checks in CI. — **REQUIRED** (validation Phase 5)
- **I18n/script correctness**: COM activation may load rendering engines (Graphite, Uniscribe). Smoke tests must include complex script validation (right-to-left, combining marks). — **REQUIRED** (test scenarios Phase 5)
- **Licensing**: No new third-party libraries. Extends existing RegFree.targets. — **PASS**
- **Stability/performance**: Medium-risk change (COM activation failures if manifests incomplete). Mitigation: audit all COM usage, broad include patterns, validation on clean machines. Rollback via git if failures detected. — **REQUIRES MITIGATION** (thorough audit Phase 3)

Proceed to Phase 0 with required clarifications (D1-D4) and validation planning.

Post-design re-check (after Phase 1 artifacts added):
- Data integrity: Git-backed, validated — **PASS**
- Test evidence: Smoke test plan documented — **VERIFY IN TASKS**
- I18n/script correctness: Complex script scenarios included — **VERIFY IN TASKS**
- Licensing: No new deps — **PASS**
- Stability/performance: Audit + broad coverage mitigates risk — **PASS**

## Project Structure

### Documentation (this feature)

```text
specs/003-convergence-regfree-com-coverage/
├── spec.md              # Feature specification (existing)
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0 output (EXE inventory, COM audit methodology, test strategy)
├── data-model.md        # Phase 1 output (ExecutableEntity, COMAuditResult models)
├── quickstart.md        # Phase 1 output (how to run COM audit, how to test manifests)
├── contracts/           # Phase 1 output
│   ├── com-audit-cli.md # Script to scan EXEs for COM usage
│   ├── manifest-validation-cli.md # Script to validate manifest completeness
│   └── smoke-test-checklist.md   # Manual test checklist for each EXE
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
Build/
├── RegFree.targets      # Extend to support multiple EXEs (modify existing)
└── Src/FwBuildTasks/
    ├── RegFree.cs       # MSBuild task (extended for managed assemblies)
    └── RegFreeCreator.cs # Manifest generation logic (extended for Reflection)

Src/
├── Common/FieldWorks/FieldWorks.csproj # Already imports RegFree.targets (reference/pattern)
├── FXT/FxtExe/FxtExe.csproj            # Needs RegFree.targets import
├── LCMBrowser/LCMBrowser.csproj        # Needs RegFree.targets import
├── UnicodeCharEditor/UnicodeCharEditor.csproj # Needs RegFree.targets import
├── MigrateSqlDbs/MigrateSqlDbs.csproj  # Needs RegFree.targets import
├── Utilities/FixFwData/FixFwData.csproj # Needs RegFree.targets import
└── [other EXEs per inventory]          # Import RegFree.targets (list TBD in research)
```

**Structure Decision**: Extend existing RegFree.targets to be parameterizable per EXE (currently hardcoded for FieldWorks.exe). Each EXE project imports the target and specifies its unique COM dependencies if any differ from the standard pattern. COM audit script identifies which COM servers each EXE activates (via static code analysis or instrumentation). Manifest validation script confirms all identified CLSIDs are present in generated manifests. Smoke tests run on clean VM to catch missing entries.

**NEEDS CLARIFICATION**: Whether to create per-EXE manifest files or a shared manifest covering all COM servers (shared approach simpler but larger manifest files).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations. Stability risk mitigated through comprehensive audit and validation. No entries required.
