# C# Test Failures Report

**Generated:** 2025-12-08
**Branch:** `fix_csharp_tests`
**Build Configuration:** Debug/x64 (container)

## Executive Summary

| Metric | Count |
|--------|-------|
| Total Tests | 316 |
| Passed | 310 |
| Failed | 1 |
| Skipped | 5 |

**Current blocker:** One failing test in `FwUtilsTests`: `FlexBridgeListenerTests.FlexBridgeDataVersion` expects a non-empty version string but receives `null/empty` in the container environment.

## Test Environment

- **Host OS:** Windows 11 (worktree agent-2)
- **Execution:** `./test.ps1 -NoBuild` (auto-respawned into container `fw-agent-2`)
- **Runner:** `vstest.console.exe` via test script
- **Platform:** x64
- **Framework:** .NET Framework 4.8
- **Native Build:** Performed inside container (registration-free COM manifests generated)

## Failure Analysis

### Failing Test

- **Test:** `SIL.FieldWorks.Common.FwUtils.FlexBridgeListenerTests.FlexBridgeDataVersion`
- **File:** `Src/Common/FwUtils/FwUtilsTests/FLExBridgeHelperTests.cs:22`
- **Observed:** `FLExBridgeDataVersion: ''` (empty) → `Assert.That(value, Is.Not.Null.Or.Empty)` fails.
- **Likely Cause:** Test expects version data from FieldWorks update metadata (e.g., ProgramData `DownloadedUpdates\LastCheckUpdateInfo.xml` or similar) that is not present/populated in the container environment.

### Previously Suspected COM Issues

- COM activation is now working in-container after building inside the container (manifests generated; no `BadImageFormatException` or activation errors in this run).
- The earlier blanket COM-registration failure state is resolved for the scope of `FwUtilsTests`.

## Build Status

✅ **Build Succeeded in container** (Native + managed). Warnings are limited to external PDBs for `graphite2` (LNK4099) and do not block tests.

## Actions Taken This Cycle

1. Built inside container (`./build.ps1 -Configuration Debug`), ensuring native DLLs and SxS manifests align with container registry state.
2. Ran full `FwUtilsTests` inside container (`./test.ps1 -NoBuild`). All tests pass except one (`FlexBridgeDataVersion`).
3. Confirmed the earlier `VecProp` crash fix remains stable in container.

## Next Steps (highest impact)

1. **Fix `FlexBridgeDataVersion` test:**
   - Determine required source for the version string (likely ProgramData `DownloadedUpdates` metadata or embedded resource).
   - Provide a deterministic test fixture (seed file or test double) so the test does not depend on host/container state.
   - Update the test to tolerate empty data only if that represents a legitimate scenario (decide expected behavior).

2. **Re-enable skipped test files (still `#if false`):**
   - `SentenceFinalPunctCapitalizationCheckUnitTest.cs`
   - `FwWritingSystemSetupDlgTests.cs`
   - `RestoreProjectPresenterTests.cs`

3. **Extend container runs to broader suites (post-fix):**
   - After addressing `FlexBridgeDataVersion`, run additional assemblies in-container to surface any remaining issues.

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
