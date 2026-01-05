---
applyTo: "**/*.{cs,cpp,h}"
name: "testing.instructions"
description: "FieldWorks testing guidelines (unit/integration)"
---
# Testing guidelines

## Purpose & Scope
Guidance for writing and running deterministic unit and integration tests for FieldWorks.
**CRITICAL**: Always use the provided PowerShell scripts (`test.ps1`, `build.ps1`) to run tests. These scripts handle containerization, environment setup, and dependency management automatically.

## Core Rules
1.  **Always use scripts**: Never run `vstest.console.exe`, `nmake`, or `msbuild` directly for testing unless debugging the build system itself.
2.  **Containerization**: The scripts automatically detect if you are in a worktree and respawn inside the Docker container. This is required for COM/Registry isolation.
3.  **Build First**: Ensure tests are built before running. `test.ps1` builds by default unless `-NoBuild` is passed.

## Running Tests (Managed)

Use `.\test.ps1` for all managed (C#) tests.

```powershell
# Run all tests (builds first)
.\test.ps1

# Run specific project
.\test.ps1 -TestProject "Src/Common/FwUtils/FwUtilsTests"

# Run with filter
.\test.ps1 -TestFilter "TestCategory!=Slow"

# Run without rebuilding (faster iteration)
.\test.ps1 -NoBuild -TestProject "FwUtilsTests"
```

## Running Tests (Native C++)

Use `.\test.ps1 -Native` for native (C++) tests. This wraps `scripts/Agent/Invoke-CppTest.ps1`.

```powershell
# Build and run TestGeneric (default)
.\test.ps1 -Native

# Build and run TestViews
.\test.ps1 -Native -TestProject TestViews

# Run without rebuilding
.\test.ps1 -Native -TestProject TestViews -NoBuild
```

## Building Tests

Tests are built automatically by `test.ps1`. To build explicitly without running:

```powershell
# Build all managed tests
.\build.ps1 -BuildTests

# Build native tests (via Invoke-CppTest backend)
.\scripts\Agent\Invoke-CppTest.ps1 -Action Build -TestProject TestViews
```

## Script Architecture

The testing infrastructure relies on shared PowerShell modules for consistency:
-   **`test.ps1`**: Main entry point. Dispatches to VSTest (managed) or `Invoke-CppTest.ps1` (native).
-   **`scripts/Agent/Invoke-CppTest.ps1`**: Backend for native C++ tests (MSBuild/NMake).
-   **`Build/Agent/FwBuildHelpers.psm1`**: Shared logic for container detection, VS environment, and process cleanup.

## Debugging & Logs
-   **Logs**: Build logs are written to `Output/Build.log` (if configured) or standard output.
-   **Verbosity**: Use `-Verbosity detailed` with `test.ps1` for more output.
-   **Results**: Managed test results (TRX) are saved to `Output/<Configuration>/TestResults/`.

## Writing Tests
-   **Managed**: Use NUnit 3.x. See `Src/Common/FwUtils/FwUtilsTests` for examples.
-   **Native**: Use Unit++. See `Src/Generic/Test/TestGeneric` for examples.
-   **Determinism**: Tests must be hermetic. Avoid external state.

## Troubleshooting
-   **"Class not registered"**: You are likely running outside the container. Use `.\test.ps1`.
-   **"File not found"**: Ensure you built with `-BuildTests` (or let `test.ps1` do it).
-   **Native Crash**: Native tests are fragile. Use `printf` debugging if the debugger is unavailable.

## References
-   **Build Infrastructure**: `Build/FieldWorks.targets` for MSBuild test tasks
-   **Test Configuration**: `Test.runsettings` for VSTest settings
-   **Test Data**: `TestLangProj/` for integration test data
-   **Quickstart**: `specs/007-test-modernization-vstest/quickstart.md` for detailed instructions
