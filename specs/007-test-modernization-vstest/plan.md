# Implementation Plan: Test Modernization (VSTest)

**Branch**: `specs/007-test-modernization-vstest` | **Date**: 2025-11-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/007-test-modernization-vstest/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Replace the legacy NUnit3 console runner in the MSBuild `Test` target with `vstest.console.exe`. This involves adding `NUnit3TestAdapter` to all test projects via `Directory.Build.props`, configuring a global `.runsettings` file, and updating `FieldWorks.targets` to invoke the new runner with appropriate flags (Parallel, TRX logger). Native C++ tests will remain on the legacy runner for now.

**Optional Phase 2**: A detailed plan for migrating legacy C++ tests (`TestViews`, `TestGeneric`) from the custom "Unit++" framework to GoogleTest is included as an optional task. This migration is required to make native tests discoverable in VS Code but is not part of the critical path.

## Technical Context

**Language/Version**: C# (.NET Framework 4.8), MSBuild, PowerShell, C++ (Native)
**Primary Dependencies**: `NUnit3TestAdapter` (NuGet), `Microsoft.NET.Test.Sdk` (NuGet), GoogleTest (Optional)
**Storage**: N/A (Build Artifacts only: `.trx` files)
**Testing**: VSTest Platform (replacing NUnit Console), Unit++ (Legacy Native), GoogleTest (Target Native)
**Target Platform**: Windows (x64)
**Project Type**: Build Infrastructure / Test Tooling
**Performance Goals**: Maintain current test execution time (requires Parallel execution)
**Constraints**: Must preserve `FieldWorks.proj` traversal order and support legacy timeouts.
**Scale/Scope**: ~110 projects, mixed managed/native.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Data integrity**: N/A - No schema or user data changes.
- **Test evidence**: This is a test infrastructure change. Validated by "Independent Test" scenarios in spec (running the build and checking TRX output).
- **I18n/script correctness**: N/A - No product code changes.
- **Licensing**: `NUnit3TestAdapter` is MIT licensed (Compliant). GoogleTest is BSD-3-Clause (Compliant).
- **Stability/performance**: Risk of test instability due to runner change. Mitigated by `Test.runsettings` configuration and parallel execution parity. Native migration (if attempted) carries high risk of breaking legacy test logic; requires careful porting.

## Project Structure

### Documentation (this feature)

```text
specs/007-test-modernization-vstest/
├── plan.md              # This file
├── research.md          # Decisions and Rationale
├── quickstart.md        # How to run/debug tests with VSTest
└── spec.md              # Feature Specification
```

### Source Code (repository root)

```text
# Build Infrastructure
Build/
├── FieldWorks.targets       # UPDATE: Replace NUnit3 task with VSTest exec
└── Test.runsettings         # NEW: Global test configuration

# Configuration
Directory.Build.props        # UPDATE: Add NUnit3TestAdapter reference

# Source (Affected Projects)
Src/
├── Common/
│   └── Tests/               # Example managed test project
└── ... (all managed test projects)

# Native Tests (Optional Phase 2)
Src/
├── views/Test/              # TestViews.vcxproj (Unit++ -> GoogleTest)
└── Generic/Test/            # TestGeneric.vcxproj (Unit++ -> GoogleTest)
```

**Structure Decision**: Modify existing build files (`Build/FieldWorks.targets`, `Directory.Build.props`) and add a new configuration file (`Test.runsettings`) at the root (or `Build/`).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --------- | ---------- | ------------------------------------ |
| N/A       |            |                                      |
