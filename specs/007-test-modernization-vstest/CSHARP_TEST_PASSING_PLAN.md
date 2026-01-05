# C# Test Passing Plan
**Branch:** `fix_csharp_tests`
**Last Updated:** 2025-12-08

## Current Status

- ✅ Build succeeds in container (Debug/x64)
- ✅ `FwUtilsTests` run in container
- ❌ 1 failing test (`FlexBridgeDataVersion`) due to missing version metadata
- 🚧 Other assemblies not yet rerun post-container-build; disabled tests remain `#if false`

## Current Blocker (post-container run)

- `FlexBridgeListenerTests.FlexBridgeDataVersion` expects a non-empty version string. In container the value is empty/null → assertion fails. Likely depends on ProgramData update metadata not seeded in test env.

## Plan / Next Actions

1) **Stabilize `FlexBridgeDataVersion`**
- Find the data source the helper reads (likely `C:\ProgramData\SIL\FieldWorks\DownloadedUpdates\LastCheckUpdateInfo.xml` or similar).
- Provide a deterministic test fixture (seed file or mock) for the version string so the test no longer depends on external install/update state.
- Decide expected behavior when no metadata exists (either assert empty is acceptable or ensure fixture supplies data).

2) **Re-enable and fix skipped tests (`#if false`)**
- `SentenceFinalPunctCapitalizationCheckUnitTest.cs` — missing class; decide implement vs delete.
- `FwWritingSystemSetupDlgTests.cs` — rewrite against `FwWritingSystemSetupModel`.
- `RestoreProjectPresenterTests.cs` — update for current backup/restore API.

3) **Broaden container test coverage after (1)**
- Run next assemblies in-container (xWorksTests, LexTextControlsTests, DetailControlsTests, etc.) once the FwUtils blocker is resolved, to surface any additional issues.

4) **Warnings (optional/cleanup)**
- LNK4099 from graphite2 PDBs and managed compiler warnings remain low-priority; no test impact.

## Notes on COM/Container

- Building **and** testing inside the agent container now yields working COM activation (no BadImageFormatException). Keep using `./build.ps1` and `./test.ps1` from the worktree root to auto-respawn in `fw-agent-2`.

## Running Tests

- Standard: `./test.ps1` (auto-container)
- Skip build after container build: `./test.ps1 -NoBuild`
- Filter: `./test.ps1 -TestFilter "TestCategory!=Slow"`
- Specific project: `./test.ps1 -TestProject "Src/Common/FwUtils/FwUtilsTests"`

## Success Criteria

- Build succeeds
- Tests execute in the container environment
- Zero failing tests in container runs (starting with FwUtils, then broader suites)

## Related Files

- `CSHARP_TEST_FAILURES.md` — current failure summary
- `test.ps1` — test entrypoint
- `Directory.Build.props` — build configuration
- `.github/instructions/testing.instructions.md` — testing guidelines
- `.github/instructions/testing.instructions.md` - Testing guidelines
