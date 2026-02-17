---
applyTo: "**/*.{cs,cpp,h}"
name: "testing.instructions"
description: "FieldWorks testing guidelines (unit/integration)"
---
# Testing guidelines

## Purpose & Scope
Guidance for writing deterministic unit and integration tests for FieldWorks and CI expectations.

## Building Tests

### Important: Build with -BuildTests Flag

The normal build (`.\build.ps1`) does **NOT** build test projects by default. You must use:

```powershell
# Build including test projects (required before running tests)
.\build.ps1 -BuildTests

# Or with other options
.\build.ps1 -Configuration Release -BuildTests
```

**Why this matters**: Test projects need to be built by MSBuild to generate binding redirects (e.g., for `Microsoft.Extensions.DependencyModel`). Without this, tests may fail with `FileLoadException`.

### Incremental Builds

If you've built without `-BuildTests` and later add it, some test projects may not rebuild. To force regeneration of binding redirects:

```powershell
# Force full test rebuild
.\build.ps1 -BuildTests -MsBuildArgs @('/t:Rebuild')
```

## Context loading
- Locate tests near their components (e.g., `Src/<Component>.Tests`). Some integration scenarios use `TestLangProj/` data.
- **Current test infrastructure**: MSBuild-based with VSTest (`vstest.console.exe`) via `Build/FieldWorks.targets`
- Test projects are marked with `<IsTestProject>true</IsTestProject>` and automatically receive modern test packages via `Directory.Build.props`

## Deterministic requirements
- Keep tests hermetic: avoid external state; use test data under version control.
- Name tests for intent; include happy path and 1–2 edge cases.
- Timeouts: Default 70 seconds per test; see `Test.runsettings` for configuration.

## Structured output
- Provide clear Arrange/Act/Assert; minimal fixture setup.
- Prefer stable IDs and data to avoid flakiness.

## Key Rules
- Keep tests deterministic and fast where possible; add integration tests only for end-to-end scenarios.
- Name tests clearly for reviewer understanding.
- Tests use NUnit 3.x framework with `[Test]` and `[TestFixture]` attributes.

## Running Tests

### Primary Method: MSBuild with VSTest (Recommended)

The current approach uses MSBuild tasks that invoke `vstest.console.exe` with NUnit3TestAdapter:

```powershell
# Run tests for a specific component
msbuild Build\FieldWorks.targets /t:FwUtilsTests /p:Configuration=Debug /p:Platform=x64 /p:action=test

# With code coverage
msbuild Build\FieldWorks.targets /t:FwUtilsTests /p:Configuration=Debug /p:Platform=x64 /p:action=test /p:EnableCoverage=true
```

**How it works**:
- Tests are executed via VSTest: `vstest.console.exe`
- Test adapter: `NUnit3TestAdapter` v5.2.0 (copied to output via `CopyLocalLockFileAssemblies`)
- Test results: `Output/<Configuration>/TestResults/<ProjectName>.trx` (TRX format)
- Configuration: `Test.runsettings` at repository root

### Direct VSTest Invocation

For faster iteration, run VSTest directly on built assemblies:

```powershell
cd Output\Debug
vstest.console.exe FwUtilsTests.dll /Settings:"..\..\Test.runsettings" /Platform:x64

# With test filtering
vstest.console.exe FwUtilsTests.dll /Settings:"..\..\Test.runsettings" /Platform:x64 /TestCaseFilter:"FullyQualifiedName~MyTestClass"
```

### Category Exclusions

Legacy NUnit categories are translated to VSTest filters:
- `ByHand` → `TestCategory!=ByHand`
- `KnownMonoIssue` → `TestCategory!=KnownMonoIssue`
- `SkipOnTeamCity` → `TestCategory!=SkipOnTeamCity`

### VS Code Integration

Tests can be discovered and run in VS Code Test Explorer:
1. Build the project first (`.\build.ps1` or `msbuild`)
2. Open Test Explorer (beaker icon in Activity Bar)
3. Tests are discovered from built `.dll` files using `NUnit3TestAdapter`

### Coverage Analysis (Legacy)

For code coverage, the legacy NUnit console runner is still used with dotCover:

```powershell
msbuild Build\FieldWorks.targets /t:FwUtilsTests /p:Configuration=Debug /p:Platform=x64 /p:action=cover
```

## References
- **Build Infrastructure**: `Build/FieldWorks.targets` for MSBuild test tasks
- **Test Configuration**: `Test.runsettings` for VSTest settings
- **Test Data**: `TestLangProj/` for integration test data
- **Quickstart**: `specs/007-test-modernization-vstest/quickstart.md` for detailed instructions

## Native C++ Tests (Unit++)

Native C++ tests use the legacy Unit++ framework and are **not** integrated with VSTest.

### Building Native Tests

Requires VS Developer Command Prompt (for nmake and MSVC toolchain):

```cmd
REM Build TestGeneric
cd Src\Generic\Test
nmake /nologo BUILD_CONFIG=Debug BUILD_TYPE=d BUILD_ROOT=%CD%\..\..\..\  BUILD_ARCH=x64 /f testGenericLib.mak

REM Build TestViews
cd Src\views\Test
nmake /nologo BUILD_CONFIG=Debug BUILD_TYPE=d BUILD_ROOT=%CD%\..\..\..\  BUILD_ARCH=x64 /f testViews.mak
```

### Running Native Tests

```cmd
cd Output\Debug
testGenericLib.exe
TestViews.exe
```

### Native Test Dependencies
- Main native libraries must be built first (Generic.lib, DebugProcs.dll, etc.)
- ICU 70 DLLs (`icuin70.dll`, `icuuc70.dll`) must be in Output/Debug/
- `CollectUnit++Tests.cmd` script generates test registration code

### vcxproj Files

The C++ test vcxproj files are "Makefile" projects that wrap nmake. They:
- Cannot be built directly via `msbuild` without VS Developer environment
- Use `NMakeBuildCommandLine` to invoke nmake with proper environment variables
- Require `BUILD_ROOT`, `BUILD_CONFIG`, `BUILD_TYPE`, `BUILD_ARCH` variables

See `specs/007-test-modernization-vstest/native-test-fixes.md` for detailed documentation.
