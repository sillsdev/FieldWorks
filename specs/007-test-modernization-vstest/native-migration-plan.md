# Native C++ Test Migration Plan: Unit++ to GoogleTest

**Status**: Planning
**Created**: 2025-12-05
**Prerequisites**: Phase 5a native build fixes complete (vcxproj XML, CollectUnit++Tests.cmd)

## Executive Summary

This document outlines the plan to migrate FieldWorks' native C++ tests from the legacy Unit++ framework to GoogleTest. The migration enables:
- VSTest integration via GoogleTest Adapter
- Test discovery in VS Code Test Explorer
- Modern assertion syntax and better error messages
- Active community support and documentation

## Current State

### Test Projects

| Project | Location | Tests | Status |
|---------|----------|-------|--------|
| TestGeneric | `Src/Generic/Test/` | 24 | ⚠️ Builds, crashes (0xC0000005) |
| TestViews | `Src/views/Test/` | ~50+ | ⚠️ Builds, crashes on startup |

**Note**: Both test projects crash with access violation. TestGeneric crash was discovered during vcxproj modernization (Dec 2025) - confirmed that the nmake-built version also crashes, so this is a pre-existing issue.

### Unit++ Framework

The Unit++ framework is located at `Lib/src/unit++/` and provides:
- Test suite registration via `unitpp::suite`
- Assertion macros (`assert_true`, `assert_eq`, `assert_false`, `assert_fail`)
- Global setup/teardown hooks (`GlobalSetup`, `GlobalTeardown`)
- Per-suite setup/teardown (`SuiteSetup`, `SuiteTeardown`, `Setup`, `Teardown`)
- Exception testing via `exception_case<E>`

### Known Issues to Fix Before Migration

#### Issue 1: TestViews.exe Crash (T022)

**Symptom**: Access violation (0xC0000005) during test initialization
**Location**: Crashes in Notifier tests
**Root Cause**: Unknown - likely uninitialized COM object or missing DLL initialization

**Investigation Steps**:
1. Run under debugger to get exact crash location
2. Check if crash is in `GlobalSetup()` or during test registration
3. Verify all required DLLs are present (Views.dll, FwKernel.dll, etc.)
4. Check COM initialization sequence

**Resolution Options**:
- A) Fix in Unit++ before migration
- B) Fix during GoogleTest migration (may be easier to debug with modern framework)

#### Issue 2: Test Build Integration

**Current State**: TestGeneric and TestViews are NOT in the main build system
**Problem**: Must be built manually via nmake

**Resolution**: Add test targets to `Build/mkall.targets` (see Task T-PRE-02)

### Prototype Gotchas (T018)

- **Build output location is brittle**: Unit++ builds and runs only from a fully hydrated repo layout; missing ICU 70 DLLs (`icuin70.dll`, `icuuc70.dll`) in `Output/<Config>` trigger crashes before tests start.
- **Generated registration is mandatory**: `CollectUnit++Tests.cmd` must run to regenerate `Collection.cpp`; skipping it leaves stale test registries and results in empty/incorrect suites.
- **Makefile projects require VS env**: NMake invocations still need `VsDevCmd.bat` to populate `INCLUDE/LIB/PATH` until the vcxproj conversion is complete.
- **Access violations are pre-existing**: Both `TestGeneric.exe` and `TestViews.exe` can crash with 0xC0000005 even when built via nmake; migration work must assume these crashes are legacy defects, not introduced by GoogleTest changes.

## Migration Strategy

### Phase 1: Pre-Migration Fixes (Before GoogleTest)

Complete these before starting the GoogleTest migration:

| Task | Description | Priority |
|------|-------------|----------|
| T-PRE-01 | Fix TestViews.exe crash | High |
| T-PRE-02 | Add native test targets to mkall.targets | Medium |
| T-PRE-03 | Document all existing tests and their dependencies | Medium |

### Phase 2: GoogleTest Infrastructure

| Task | Description |
|------|-------------|
| T-GT-01 | Add GoogleTest via vcpkg or as submodule |
| T-GT-02 | Create CMake or vcxproj for GoogleTest-based tests |
| T-GT-03 | Configure GoogleTest adapter for VSTest |
| T-GT-04 | Set up VS Code test discovery |

### Phase 3: Test Migration (TestGeneric)

Migrate TestGeneric first (smaller, working):

| Task | Description |
|------|-------------|
| T-MIG-01 | Create `TestGeneric_gtest.cpp` with GoogleTest main |
| T-MIG-02 | Migrate TestSmartBstr tests |
| T-MIG-03 | Migrate TestUtil tests |
| T-MIG-04 | Migrate TestUtilXml tests |
| T-MIG-05 | Migrate TestUtilString tests |
| T-MIG-06 | Migrate TestErrorHandling tests |
| T-MIG-07 | Validate all 24 tests pass |

### Phase 4: Test Migration (TestViews)

Migrate TestViews after TestGeneric success:

| Task | Description |
|------|-------------|
| T-MIG-10 | Create `TestViews_gtest.cpp` with GoogleTest main |
| T-MIG-11 | Migrate TestNotifier (fix crash first) |
| T-MIG-12 | Migrate remaining test suites |
| T-MIG-13 | Validate all tests pass |

### Phase 5: Cleanup

| Task | Description |
|------|-------------|
| T-CLN-01 | Remove Unit++ framework from build (keep source for reference) |
| T-CLN-02 | Update documentation |
| T-CLN-03 | Update CI pipeline |

## API Mapping: Unit++ → GoogleTest

### Test Structure

```cpp
// Unit++ Style
namespace TestGenericLib
{
    class TestSmartBstr : public unitpp::suite
    {
        void testEqualityToLiteral()
        {
            SmartBstr bstr(L"pineapple");
            unitpp::assert_true("bstr should == literal", bstr == L"pineapple");
        }

    public:
        TestSmartBstr();
    };
}

// Constructor registers tests
TestSmartBstr::TestSmartBstr()
{
    add("EqualityToLiteral", testcase(this, "EqualityToLiteral",
        &TestSmartBstr::testEqualityToLiteral));
}
```

```cpp
// GoogleTest Style
namespace TestGenericLib
{
    class TestSmartBstr : public ::testing::Test
    {
    protected:
        void SetUp() override { /* per-test setup */ }
        void TearDown() override { /* per-test cleanup */ }
    };

    TEST_F(TestSmartBstr, EqualityToLiteral)
    {
        SmartBstr bstr(L"pineapple");
        EXPECT_TRUE(bstr == L"pineapple") << "bstr should == literal";
    }
}
```

### Assertion Mapping

| Unit++ | GoogleTest | Notes |
|--------|------------|-------|
| `assert_true("msg", expr)` | `EXPECT_TRUE(expr) << "msg"` | Non-fatal |
| `assert_true("msg", expr)` | `ASSERT_TRUE(expr) << "msg"` | Fatal (stops test) |
| `assert_false("msg", expr)` | `EXPECT_FALSE(expr) << "msg"` | |
| `assert_eq("msg", expected, actual)` | `EXPECT_EQ(expected, actual) << "msg"` | |
| `assert_fail("msg")` | `FAIL() << "msg"` | |

### Setup/Teardown Mapping

| Unit++ | GoogleTest | Scope |
|--------|------------|-------|
| `GlobalSetup(bool)` | `main()` or `Environment::SetUp()` | Process |
| `GlobalTeardown()` | `main()` or `Environment::TearDown()` | Process |
| `SuiteSetup()` | `SetUpTestSuite()` (static) | Test suite |
| `SuiteTeardown()` | `TearDownTestSuite()` (static) | Test suite |
| `Setup()` | `SetUp()` | Each test |
| `Teardown()` | `TearDown()` | Each test |

### Exception Testing

```cpp
// Unit++ Style
testcase tc(this, "ExceptionTest", &Test::testThrows);
add("throws", exception_case<out_of_range>(tc));

// GoogleTest Style
TEST_F(Test, ExceptionTest)
{
    EXPECT_THROW(functionThatThrows(), std::out_of_range);
}
```

### Global Setup with COM/ICU

```cpp
// GoogleTest global environment for COM/ICU initialization
class FwTestEnvironment : public ::testing::Environment
{
public:
    void SetUp() override
    {
#if defined(WIN32) || defined(_M_X64)
        ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
#endif
        ::OleInitialize(NULL);
        RedirectRegistry();
        StrUtil::InitIcuDataDir();
    }

    void TearDown() override
    {
        ::OleUninitialize();
#if defined(WIN32) || defined(_M_X64)
        ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
#endif
    }
};

// In main():
int main(int argc, char **argv)
{
    ::testing::InitGoogleTest(&argc, argv);
    ::testing::AddGlobalTestEnvironment(new FwTestEnvironment);
    return RUN_ALL_TESTS();
}
```

## GoogleTest Installation Options

### Option A: vcpkg (Recommended)

```powershell
# Install vcpkg if not present
git clone https://github.com/microsoft/vcpkg.git
.\vcpkg\bootstrap-vcpkg.bat

# Install GoogleTest
.\vcpkg\vcpkg install gtest:x64-windows

# Integrate with MSBuild
.\vcpkg\vcpkg integrate install
```

**Pros**: Easy integration, automatic updates
**Cons**: Adds vcpkg dependency

### Option B: Git Submodule

```powershell
git submodule add https://github.com/google/googletest.git Lib/src/googletest
```

**Pros**: Self-contained, version controlled
**Cons**: Manual updates required

### Option C: NuGet Package

Add to vcxproj:
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.googletest.v140.windesktop.msvcstl.static.rt-dyn" Version="1.8.1.7" />
</ItemGroup>
```

**Pros**: Familiar NuGet workflow
**Cons**: Older versions, less control

## VSTest Integration

### GoogleTest Adapter

Install the GoogleTest adapter for VSTest:

```powershell
# Via NuGet (in test vcxproj)
<PackageReference Include="GoogleTestAdapter" Version="0.18.0" />
```

Or install VS extension: "Test Adapter for Google Test"

### .runsettings Configuration

Add to `Test.runsettings`:
```xml
<RunSettings>
  <GoogleTestAdapterSettings>
    <SolutionSettings>
      <Settings>
        <WorkingDir>$(SolutionDir)Output\$(Configuration)</WorkingDir>
        <PathExtension>$(SolutionDir)Output\$(Configuration)</PathExtension>
      </Settings>
    </SolutionSettings>
  </GoogleTestAdapterSettings>
</RunSettings>
```

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| TestViews crash persists after migration | Medium | High | Debug in Unit++ first; isolate failing tests |
| COM initialization issues | Medium | Medium | Test global environment carefully |
| ICU version conflicts | Low | Medium | Ensure ICU 70 DLLs in test output |
| Build system integration | Medium | Medium | Test incrementally; keep fallback |

## Success Criteria

1. **All existing tests pass**: 24 TestGeneric + all TestViews tests
2. **VSTest discovery works**: Tests appear in VS/VS Code Test Explorer
3. **CI integration**: Tests run via `vstest.console.exe` in pipeline
4. **No regression**: Test execution time comparable to Unit++

## References

- [GoogleTest Documentation](https://google.github.io/googletest/)
- [GoogleTest Primer](https://google.github.io/googletest/primer.html)
- [Test Adapter for Google Test](https://marketplace.visualstudio.com/items?itemName=ChristianSoltenborn.GoogleTestAdapter)
- [vcpkg GoogleTest](https://vcpkg.io/en/packages.html) (search for `gtest`)
- Unit++ source: `Lib/src/unit++/`
- Existing tests: `Src/Generic/Test/`, `Src/views/Test/`
