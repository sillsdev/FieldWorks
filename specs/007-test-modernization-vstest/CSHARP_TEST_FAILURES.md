# C# Test Failures Report

**Generated:** 2025-12-16
**Branch:** `007-test-modernization-vstest`
**Build Configuration:** Debug/x64

## Executive Summary

| Metric | Count |
|--------|-------|
| Total Tests | 4048 |
| Passed | 3973 |
| Failed | 0 |
| Skipped | 75 |

**Current blocker:** None (latest `test.ps1` run is green).

## Test Environment

- **Host OS:** Windows (SIL-XPS)
- **Execution:** `./test.ps1 -NoBuild`
- **Runner:** `vstest.console.exe` via test script
- **Platform:** x64
- **Framework:** .NET Framework 4.8
- **Results:** `Output\Debug\TestResults\johnm_SIL-XPS_2025-12-16_20_52_02.trx`

## Failure Analysis

### Failing Tests

- None (0 failed tests in the latest run).

### Previously Suspected COM Issues

- COM activation is now working in-container after building inside the container (manifests generated; no `BadImageFormatException` or activation errors in this run).
- The earlier blanket COM-registration failure state is resolved for the scope of `FwUtilsTests`.

## Build Status

✅ **Build Succeeded in container** (Native + managed). Warnings are limited to external PDBs for `graphite2` (LNK4099) and do not block tests.

## Actions Taken This Cycle

1. Ran full managed test suite with VSTest (`./test.ps1 -NoBuild`).
2. Confirmed 0 failures and captured updated TRX output.

## Next Steps (highest impact)

1. Optional: reduce the number of skipped tests (75) if they represent work to complete rather than intentional OS-conditional skips.
2. Keep the recommended workflow of `./build.ps1 -BuildTests` followed by `./test.ps1 -NoBuild` for fast validation cycles.

## Prior Migration Work (RhinoMocks → Moq)

The following test files were migrated from RhinoMocks to Moq 4.20.70:

1. `Src/LexText/Interlinear/InterlinearTests/ComboHandlerTests.cs`
2. `Src/LexText/Interlinear/InterlinearTests/GlossToolLoadsGuessContentsTests.cs`
3. `Src/Common/Controls/XMLViews/XMLViewsTests/ConfiguredExportTests.cs`
4. `Src/xWorks/xWorksTests/DictionaryPublicationDecoratorTests.cs`
5. `Src/xWorks/xWorksTests/XhtmlDocViewTests.cs`
6. `Src/xWorks/xWorksTests/RecordEditViewTests.cs`

## Recommendations

- Address `FlexBridgeDataVersion` deterministically, then expand container test runs to the next assemblies (xWorksTests, LexTextControlsTests, etc.).
- Continue to prefer in-container builds/tests for COM-dependent suites.
