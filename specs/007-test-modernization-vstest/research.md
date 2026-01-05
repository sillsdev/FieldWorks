# Research & Decisions: Test Modernization (VSTest)

## Decisions

### 1. Test Runner: VSTest.Console.exe
**Decision**: Use `vstest.console.exe` as the primary runner for managed tests.
**Rationale**: Standard .NET tool, produces TRX output, integrates with Azure DevOps and VS Code.
**Alternatives**: `dotnet test` (rejected for now as this is a mixed .NET Framework repo, though VSTest is the underlying engine for `dotnet test` anyway).

### 2. Parallel Execution
**Decision**: Enable `/Parallel` switch by default.
**Rationale**: Essential to maintain build performance parity with NUnit's parallel execution.
**Risk**: Some legacy tests might not be thread-safe.
**Mitigation**: Use `.runsettings` to control `ThreadApartmentState` if needed, or disable parallel for specific assemblies via runsettings/properties if absolutely necessary (though goal is global parallel).

### 3. Result Storage
**Decision**: Store results in `Output/$(Configuration)/TestResults/`.
**Rationale**: Keeps artifacts separated from build outputs but easily accessible. Standard pattern.

### 4. Configuration
**Decision**: Use a global `Test.runsettings` file at the repository root.
**Rationale**: Centralizes configuration (timeouts, parallelism, deployment) for both CI and local VS Code usage.

### 5. Native C++ Tests
**Decision**: **Option A (Legacy)** - Leave them as-is for this migration.
**Rationale**: They use a custom "Unit++" framework incompatible with VSTest adapters. Migration to GoogleTest is a separate, large effort (captured as Optional Phase 2).
**Research Findings**:
- **Framework**: Custom "Unit++" library (`Lib/src/unit++`).
- **Projects**: `TestViews.vcxproj`, `TestGeneric.vcxproj`.
- **Build**: Makefile projects invoking batch scripts (`mkvw-tst.bat`).
- **Complexity**: High. Requires rewriting test macros (`TEST`, `SUITE`) to GoogleTest equivalents (`TEST_F`, `TEST`), replacing custom assertions, and configuring the GoogleTest adapter for VS Code discovery.
- **Gotchas**:
    - "Unit++" likely has custom setup/teardown semantics that differ from GoogleTest fixtures.
    - Dependency on `Lib/src/unit++` must be replaced with a NuGet reference to `Microsoft.GoogleTestAdapter` or vcpkg port.
    - The current build system (Makefiles) needs to be updated to link against GoogleTest libraries instead of Unit++.

### 6. Adapter Deployment
**Decision**: Use `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` in `Directory.Build.props`.
**Rationale**: Ensures `NUnit3TestAdapter.dll` is copied to the output directory, allowing `vstest.console.exe` to find it without complex path configuration.

### 7. NUnit Filter Translation
**Decision**: Support basic category exclusion only (e.g., `cat != Exclude`).
**Rationale**: The current build system primarily uses simple exclusion filters. Complex NUnit expression parsing is out of scope for the initial migration.
**Implementation**: Map `cat != Value` to `/TestCaseFilter:"TestCategory!=Value"`.

## Unknowns Resolved
- **Parallelism**: Confirmed enabled.
- **Coverage**: Confirmed optional switch.
- **Native Strategy**: Confirmed legacy retention with optional migration plan.
- **Filter Logic**: Confirmed basic category exclusion support.

## Technical Issues Discovered

### Issue 1: Microsoft.Extensions.DependencyModel Version Conflict
**Date**: 2025-12-02
**Symptom**: All tests fail with `FileLoadException: Could not load file or assembly 'Microsoft.Extensions.DependencyModel, Version=2.0.4.0'`
**Root Cause**:
- `icu.net 3.0.1` (via SIL.LCModel.Core) was compiled against DependencyModel 2.0.4.0
- `ParatextData 9.5.0.20` requires DependencyModel 9.0.9
- NuGet resolves to highest version (9.0.9) but icu.net requests 2.0.4 at runtime
- CLR refuses to load 9.0.9 when 2.0.4 is requested (version mismatch)

**Solution**: Add centralized PackageReference to `Directory.Build.props`:
```xml
<PackageReference Include="Microsoft.Extensions.DependencyModel" Version="9.0.9" />
```
This forces all projects to see the version conflict and auto-generate binding redirects:
```xml
<bindingRedirect oldVersion="0.0.0.0-9.0.0.9" newVersion="9.0.9" />
```

**Verification**: FwUtilsTests passes (182 passed, 7 skipped, 0 failed)

### Issue 2: VSTest Cleanup Crash (0xC0000005) - RESOLVED
**Date**: 2025-12-02
**Symptom**: Tests pass but VSTest exits with code -1073741819 (0xC0000005 = Access Violation)
**Timing**: Crash occurs AFTER all tests complete, during process cleanup
**Root cause**: Native COM objects (VwCacheDa, ICU, etc.) being finalized by the CLR after native DLLs are unloaded

**Solution**: Added `<InIsolation>true</InIsolation>` to `Test.runsettings`
- Runs tests in a separate child process from VSTest host
- When test process crashes during cleanup, VSTest still reports results correctly
- Exit code is now 1 (skipped tests present) instead of crash code

**Additional fixes applied**:
1. Added `AssemblySetupFixture.cs` with `[OneTimeTearDown]` that forces GC cleanup
2. Added proper COM cleanup in `IVwCacheDaTests.TestTeardown()` using `Marshal.ReleaseComObject`
3. Updated exit code documentation in `Test.runsettings` header

**Exit Code Reference** (documented in Test.runsettings):
- `0`: All tests passed, no skipped
- `1`: Tests failed OR tests skipped (check output for actual counts)
- Exit code 1 with 0 failed tests = SUCCESS

### Issue 3: System.Memory Version Conflict - RESOLVED
**Date**: 2025-12-02
**Symptom**: Tests fail with `FileLoadException: Could not load file or assembly 'System.Memory, Version=4.0.1.2'`
**Root cause**: Various packages require different System.Memory versions (4.5.0 to 4.6.0), binding redirects point to 4.0.1.2 but output has 4.0.5.0
**Solution**: Added `<PackageReference Include="System.Memory" Version="4.5.5" />` to Directory.Build.props
- Using 4.5.5 (highest 4.5.x) for better compatibility with net48 runtime
**Verification**: FiltersTests, WidgetsTests, ViewsInterfacesTests all pass

## Build Integration

### build.ps1 -RunTests Parameter
Added integrated test execution to the main build script:
```powershell
# Build and run all tests
.\build.ps1 -RunTests

# Build and run tests with filter
.\build.ps1 -RunTests -TestFilter "TestCategory!=Slow"

# Build tests without running them
.\build.ps1 -BuildTests
```

The `-RunTests` parameter:
- Implies `-BuildTests` (test projects are included in build)
- Uses `Build/Agent/Run-VsTests.ps1` for execution
- Parses and displays clear pass/fail/skip counts
- Optional `-TestFilter` for VSTest filter expressions

## Test Runner Scripts

Created helper scripts in `Build/Agent/`:

### Run-VsTests.ps1
Simplified test runner that parses VSTest output for clear results:
```powershell
# Run specific tests
.\Build\Agent\Run-VsTests.ps1 FwUtilsTests.dll,xCoreTests.dll

# Run all tests
.\Build\Agent\Run-VsTests.ps1 -All

# Rebuild before running
.\Build\Agent\Run-VsTests.ps1 FwUtilsTests.dll -Rebuild

# With filter
.\Build\Agent\Run-VsTests.ps1 FwUtilsTests.dll -Filter "TestCategory!=Slow"
```

### Rebuild-TestProjects.ps1
Rebuilds test projects that need binding redirect updates:
```powershell
# Check which need rebuilding
.\Build\Agent\Rebuild-TestProjects.ps1 -DryRun

# Rebuild projects missing redirects
.\Build\Agent\Rebuild-TestProjects.ps1

# Force rebuild all
.\Build\Agent\Rebuild-TestProjects.ps1 -Force
```

### Issue 3: Build Flag Required for Tests
**Date**: 2025-12-02
**Discovery**: The normal build (`.\build.ps1`) does NOT include test projects

**Root Cause**: `FieldWorks.proj` only includes test projects when `BuildTests=true`:
```xml
<ItemGroup Label="Phase 15: Test Projects - Foundation" Condition="'$(BuildTests)'=='true'">
```

**Impact**:
- Test DLLs may be stale or missing binding redirects
- Running `vstest.console.exe` on pre-existing test DLLs may fail with `FileLoadException`

**Solution**: Always build with `-BuildTests` before running tests:
```powershell
.\build.ps1 -BuildTests
```

**Why binding redirects matter**:
- `Directory.Build.props` adds centralized package references (e.g., `Microsoft.Extensions.DependencyModel 9.0.9`)
- MSBuild generates binding redirects during build when it detects version conflicts
- Without building, old `.dll.config` files won't have required redirects

**C++ Tests (Native)**: Built separately via nmake, not affected by `-BuildTests` flag
- Use VS tasks: `Test: Build C++ TestGeneric (nmake)` and `Test: Build C++ TestViews (nmake)`
- Or manual: `nmake /f testGenericLib.mak` in `Src/Generic/Test`
