# Quickstart: Running Tests with VSTest

## Overview
FieldWorks tests now use `vstest.console.exe` with the NUnit3TestAdapter (v5.2.0) for running unit tests. This replaces the legacy NUnit3 console runner.

## Prerequisites: Build with -BuildTests

**Important**: Before running tests, you must build with the `-BuildTests` flag:

```powershell
# Build including test projects (REQUIRED before running tests)
.\build.ps1 -BuildTests

# Or with other options
.\build.ps1 -Configuration Release -BuildTests
```

**Why?** Test projects are excluded from the default build. Without `-BuildTests`:
- Test DLLs may be stale or missing
- Binding redirects won't be generated (causing `FileLoadException` errors)
- New package dependencies won't be resolved

## Running Tests from Command Line

### Full Suite via MSBuild
To run all managed tests using the build system:
```powershell
# Run tests for a specific project (e.g., FwUtilsTests)
msbuild Build\FieldWorks.targets /t:FwUtilsTests /p:Configuration=Debug /p:Platform=x64 /p:action=test
```

### Direct VSTest Invocation
For faster iteration, run VSTest directly:
```powershell
cd Output\Debug
vstest.console.exe FwUtilsTests.dll /Settings:"..\..\Test.runsettings" /Platform:x64
```

### With Test Filtering
Use `/TestCaseFilter` to run specific tests:
```powershell
vstest.console.exe FwUtilsTests.dll /Settings:"..\..\Test.runsettings" /Platform:x64 /TestCaseFilter:"FullyQualifiedName~TestClassName"
```

### With Code Coverage
To enable code coverage:
```powershell
msbuild Build\FieldWorks.targets /t:FwUtilsTests /p:Configuration=Debug /p:Platform=x64 /p:action=test /p:EnableCoverage=true
```

## Test Results Location
TRX result files are output to: `Output/<Configuration>/TestResults/<ProjectName>.trx`

## Running Tests in VS Code

1.  **Open Test Explorer**: Click the Beaker icon in the Activity Bar.
2.  **Discover**: The extension uses `Test.runsettings` settings (configured in `.vscode/settings.json`).
3.  **Run/Debug**: Click the "Run" (Play) or "Debug" (Bug) icon next to any test or class.

## Configuration

Global settings are defined in `Test.runsettings` at the repository root:
*   **DefaultTimeout**: 70 seconds (matches MSBuild Exec timeout)
*   **TestSessionTimeout**: 10 minutes (for long-running test sessions)
*   **Parallelism**: Enabled by default (`MaxCpuCount=0` and `NumberOfTestWorkers=0` for auto-detect)
*   **Platform**: x64 (matches FieldWorks build configuration)

## Category Exclusion Translation
Legacy NUnit category exclusions are translated to VSTest filter syntax automatically:
| NUnit Category | VSTest Filter |
|---------------|---------------|
| `ByHand` | `TestCategory!=ByHand` |
| `KnownMonoIssue` | `TestCategory!=KnownMonoIssue` |
| `SkipOnTeamCity` | `TestCategory!=SkipOnTeamCity` |

## Troubleshooting

*   **`FileLoadException` for DependencyModel or other assemblies?**
    - Rebuild with `-BuildTests`: `.\build.ps1 -BuildTests`
    - This regenerates binding redirects in `.dll.config` files
*   **Tests not found?** Ensure the project has been built with `-BuildTests`. VSTest discovers tests from the output `.dll` files.
*   **"Adapter not found" error?** Verify that `NUnit3.TestAdapter.dll` is in the output directory (it should be copied automatically via `CopyLocalLockFileAssemblies`).
*   **Platform mismatch warning?** Ensure the `TargetPlatform` in `Test.runsettings` matches your build configuration (x64).
*   **Exit code 1 but no failures?** This means tests were skipped. VSTest returns 1 for skipped tests. Check output for actual results.
*   **Exit code 0xC0000005 (Access Violation)?** The `InIsolation` setting in `Test.runsettings` should prevent this. If it occurs, the tests likely passed but cleanup crashed. Check output for actual results.

---

## Native C++ Tests (Unit++)

The legacy C++ test projects use the Unit++ framework. These are **not** integrated into VSTest and must be run separately.

### Building C++ Tests

**Prerequisites:**
1. Visual Studio 2022 with C++ Desktop Development workload
2. Main native libraries must be built first (`.\build.ps1`)
3. Run from VS Developer Command Prompt

**Build TestGeneric:**
```cmd
cd Src\Generic\Test
nmake /nologo BUILD_CONFIG=Debug BUILD_TYPE=d BUILD_ROOT=%CD%\..\..\..\  BUILD_ARCH=x64 /f testGenericLib.mak
```

**Build TestViews:**
```cmd
cd Src\views\Test
nmake /nologo BUILD_CONFIG=Debug BUILD_TYPE=d BUILD_ROOT=%CD%\..\..\..\  BUILD_ARCH=x64 /f testViews.mak
```

### Running C++ Tests

```cmd
cd Output\Debug
testGenericLib.exe
TestViews.exe
```

### Known Issues

- **vcxproj files**: The Visual Studio project files are "Makefile" projects that wrap nmake. They cannot be built directly via `msbuild` without VS Developer environment.
- **ICU dependencies**: Tests require `icuin70.dll` and `icuuc70.dll` in the output directory.
- **No VSTest integration**: C++ tests use Unit++ framework, not NUnit/VSTest.

### Future: GoogleTest Migration

Phase 5 of this spec includes optional migration from Unit++ to GoogleTest, which would enable:
- Native VSTest adapter integration
- VS Code Test Explorer support for C++ tests
- Modern test discovery and filtering

See `native-migration-plan.md` for details.
