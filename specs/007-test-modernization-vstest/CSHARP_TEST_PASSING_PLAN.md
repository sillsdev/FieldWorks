# C# Test Passing Plan
**Branch:** `007-test-modernization-vstest`
**Last Updated:** 2025-12-16

## Current Status

- ✅ Build succeeds (Debug/x64)
- ✅ Full managed test run passes: `./test.ps1 -NoBuild`
	- Total: 4048
	- Passed: 3973
	- Skipped: 75
	- Failed: 0

## Current Blocker

- None.

## Plan / Next Actions

1) **Keep the fast green loop**
- Build test binaries when needed: `./build.ps1 -BuildTests`
- Validate without rebuilding: `./test.ps1 -NoBuild`

2) **Optional: reduce skipped tests**
- Review the 75 skipped tests and determine which are intentional OS-conditional skips vs gaps worth addressing.

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
