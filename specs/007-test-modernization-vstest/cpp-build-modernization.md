# C++ Build System Modernization Analysis

**Status**: Implementation In Progress (Option 1 - True MSBuild)
**Created**: 2025-01-07
**Last Updated**: 2025-01-08
**Context**: Pre-requisite for GoogleTest migration (T-PRE-02)

## Executive Summary

The FieldWorks C++ build system uses legacy nmake makefiles dating from VC++ 7.1 (VS 2003). This analysis evaluates three modernization approaches and recommends **Option 1: Convert to True MSBuild C++ Projects** as the best balance of effort vs. benefit for the GoogleTest migration goal.

## Current State

### Build System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│ MSBuild Traversal SDK (FieldWorks.proj)                         │
│   └── Phase 2: NativeBuild.csproj                               │
│         └── Build/mkall.targets                                 │
│               └── <Make> task invoking nmake                    │
│                     └── Bld/_init.mak, _rule.mak, _targ.mak     │
│                           └── Individual *.mak files            │
└─────────────────────────────────────────────────────────────────┘
```

### vcxproj Files (All Makefile Wrappers)

| Project | Type | Role |
|---------|------|------|
| DebugProcs.vcxproj | Makefile | Debug support library |
| Generic.vcxproj | Makefile | Foundation utilities |
| TestGeneric.vcxproj | Makefile | Generic unit tests |
| Kernel.vcxproj | Makefile | FwKernel COM DLL |
| views.vcxproj | Makefile | Views COM DLL |
| TestViews.vcxproj | Makefile | Views unit tests |
| XAmpleCOMWrapper.vcxproj | Makefile | Parser COM wrapper |

**Key Finding**: All vcxproj files are `ConfigurationType>Makefile` - they invoke nmake, not MSBuild's native C++ compilation.

### Legacy Makefile Infrastructure

| File | Lines | Role |
|------|-------|------|
| `Bld/_init.mak` | 254 | Compiler/linker flags, paths, macros |
| `Bld/_rule.mak` | 390 | Repetitive PCH rules (genpch, usepch, autopch, nopch) |
| `Bld/_targ.mak` | 190 | Target definitions, directory creation |
| `Bld/_names.mak` | 50 | Output naming conventions |

**Historical Artifacts**:
- References `UpgradeFromVC71.props` (VS 2003 migration)
- Contains `WIN32=1` and `_WIN32_WINNT=0x0500` (Win2000!)
- References `nmcl.exe`/`nmlink.exe` (Bounds Checker from 2003)
- Contains `BUILD_OS=="win95"` conditionals

### Current Build Flow Problems

1. **Environment Dependency**: nmake requires VS Developer Command Prompt (`VsDevCmd.bat`) to set `INCLUDE`, `LIB`, `Path`
2. **Not IDE-Integrated**: VS/VS Code can't build directly - must use command line or call nmake
3. **No IntelliSense**: Makefile projects don't provide proper IntelliSense
4. **No Parallel Compilation**: nmake runs sequentially; MSBuild can parallelize
5. **No NuGet Integration**: Can't use vcpkg manifest mode or NuGet PackageReference

---

## Option 1: Convert to True MSBuild C++ Projects (RECOMMENDED)

### Description

Replace Makefile vcxproj wrappers with real MSBuild C++ projects using `ConfigurationType=DynamicLibrary` or `StaticLibrary`.

### Effort Estimate

| Component | Effort | Complexity |
|-----------|--------|------------|
| DebugProcs | 2 hours | Low - small, few files |
| Generic/GenericLib | 4 hours | Medium - many source files |
| TestGeneric | 2 hours | Low - depends on GenericLib |
| FwKernel | 8 hours | High - COM, IDL, many dependencies |
| Views | 8 hours | High - COM, IDL, complex |
| TestViews | 2 hours | Low - depends on Views |
| XAmpleCOMWrapper | 4 hours | Medium - COM interop |
| **Total** | **~30 hours** | |

### Automation Available

**Partial automation possible via Visual Studio:**
1. Create new "Windows Desktop" > "Dynamic-Link Library" project
2. Add existing source files
3. Configure include paths, preprocessor defines from makefiles
4. Set up project references for dependencies

**No direct nmake-to-vcxproj converter exists** - manual work required.

### Technical Changes Required

```xml
<!-- BEFORE: Makefile wrapper -->
<PropertyGroup>
  <ConfigurationType>Makefile</ConfigurationType>
  <NMakeBuildCommandLine>nmake /nologo /f GenericLib.mak</NMakeBuildCommandLine>
</PropertyGroup>

<!-- AFTER: True MSBuild C++ -->
<PropertyGroup>
  <ConfigurationType>DynamicLibrary</ConfigurationType>
</PropertyGroup>
<ItemGroup>
  <ClCompile Include="*.cpp" />
</ItemGroup>
<ItemGroup>
  <ClInclude Include="*.h" />
</ItemGroup>
```

### Pros
- ✅ Full Visual Studio/VS Code IntelliSense
- ✅ Parallel compilation out of the box
- ✅ Direct NuGet/vcpkg integration (GoogleTest via vcpkg manifest)
- ✅ MSBuild caching and incremental builds
- ✅ Works with GitHub Actions without VS Developer shell setup
- ✅ Standard approach all Windows developers know

### Cons
- ❌ ~30 hours conversion effort
- ❌ Risk of breaking working builds during transition
- ❌ COM/IDL compilation requires careful setup
- ❌ Must maintain backward compatibility with `mkall.targets`

### Migration Path

1. Start with TestGeneric (simplest, already building via nmake)
2. Convert GenericLib next (TestGeneric dependency)
3. Convert FwKernel and Views (complex COM projects)
4. Convert TestViews last
5. Keep mkall.targets as fallback during transition
6. Remove nmake infrastructure after all projects converted

---

## Option 2: Modernize nmake Files (Keep Wrapper Structure)

### Description

Keep the Makefile vcxproj wrapper approach but modernize the underlying makefiles:
- Remove Win95/NT4 conditionals
- Update compiler flags for VS 2022
- Improve incremental build support
- Add parallel build targets

### Effort Estimate

| Task | Effort |
|------|--------|
| Clean up `_init.mak` | 4 hours |
| Clean up `_rule.mak` | 4 hours |
| Clean up `_targ.mak` | 2 hours |
| Update individual makefiles | 8 hours |
| Test and validate | 8 hours |
| **Total** | **~26 hours** |

### Pros
- ✅ Lower risk (builds already work)
- ✅ Familiar to team (if they know nmake)
- ✅ Preserves existing validation

### Cons
- ❌ Still no IntelliSense in VS/VS Code
- ❌ Still requires VS Developer Command Prompt
- ❌ Can't use vcpkg manifest mode easily
- ❌ nmake expertise increasingly rare
- ❌ Doesn't solve the TestViews build issue (still needs VsDevCmd.bat)

### Why Not Recommended

This option doesn't solve the fundamental problem: **the vcxproj files can't build without manually setting up the VS environment**. GoogleTest integration via vcpkg requires MSBuild, not nmake.

---

## Option 3: Convert to CMake

### Description

Convert all C++ projects to CMake, which generates platform-appropriate build files (MSBuild on Windows, Make on Linux).

### Effort Estimate

| Task | Effort |
|------|--------|
| Create root CMakeLists.txt | 4 hours |
| CMakeLists.txt per component | 20 hours |
| vcpkg integration | 4 hours |
| COM/IDL build rules | 16 hours |
| Testing and validation | 16 hours |
| CI integration | 8 hours |
| **Total** | **~68 hours** |

### Existing Infrastructure

The `.github/instructions/cmake-vcpkg.instructions.md` file mentions vcpkg manifest mode and CMake, suggesting **future plans exist**. However:
- No `vcpkg.json` manifest exists yet
- No `CMakeLists.txt` files in Src/ (only in `Lib/src/graphite2` and `Lib/src/unit++`)
- No `CMakePresets.json` exists

### Pros
- ✅ Industry standard for cross-platform C++
- ✅ Excellent vcpkg integration
- ✅ Modern tooling (clangd, cmake-tools)
- ✅ Better support for mixed C++/C# projects
- ✅ Future-proof investment

### Cons
- ❌ Highest effort (~68 hours)
- ❌ Team must learn CMake
- ❌ COM/IDL requires custom CMake rules
- ❌ Overkill for Windows-only project
- ❌ Delays GoogleTest migration significantly

### Why Not Recommended Now

CMake is the "right" long-term answer but is overkill for the immediate goal (fix TestViews, enable GoogleTest). Consider after GoogleTest migration is complete.

---

## Recommendation

### For GoogleTest Migration: Option 1 (True MSBuild C++ Projects)

**Rationale**:
1. **Directly solves the problem**: vcxproj files will build from VS/VS Code without VsDevCmd.bat
2. **Enables vcpkg**: GoogleTest can be added via vcpkg manifest mode
3. **Reasonable effort**: ~30 hours vs. ~68 hours for CMake
4. **Familiar to team**: Standard VS C++ projects

### Phased Approach

#### Phase A: TestGeneric Conversion (8 hours)
1. Create `TestGeneric.vcxproj` as true C++ project
2. Port compiler flags from `Bld/_init.mak`
3. Add GenericLib as project reference
4. Validate all 24 tests pass
5. Add GoogleTest via vcpkg manifest

#### Phase B: GenericLib Conversion (8 hours)
1. Create `Generic.vcxproj` as true C++ static library
2. Add DebugProcs as project reference
3. Update TestGeneric reference

#### Phase C: DebugProcs Conversion (4 hours)
1. Simple static library conversion
2. Minimal dependencies

#### Phase D: TestViews Conversion (10 hours)
1. Create `TestViews.vcxproj` as true C++ project
2. Add Views/FwKernel as project references
3. Fix 0xC0000005 crash with better debugging
4. Add GoogleTest

#### Phase E: FwKernel/Views Conversion (Optional, High Risk)
- Consider deferring until after GoogleTest works
- These are complex COM DLLs with IDL compilation
- Existing nmake builds work for production

---

## Automation Tools

### Available Scripts/Tools

| Tool | Purpose | Applicability |
|------|---------|---------------|
| VS "Add Existing Item" | Add source files to new vcxproj | Useful |
| VS "Property Pages" | Configure include paths, defines | Useful |
| `cmake --export-compile-commands` | Generate compile_commands.json | N/A (no CMake yet) |
| `vcpkg integrate install` | Add vcpkg to MSBuild | Required for GoogleTest |

### No Direct Converters

There is no tool to convert nmake makefiles to vcxproj automatically. The conversion must be done manually by:
1. Creating a new C++ project in VS
2. Adding source files
3. Configuring compiler/linker settings to match makefile

### Recommended Helper Script

A PowerShell script could automate extracting:
- Source file lists from makefiles
- Include paths
- Preprocessor definitions
- Library dependencies

This would accelerate manual conversion.

---

## Decision Points

### Question 1: Start with Test Projects or Libraries?

**Answer**: Start with test projects (TestGeneric, TestViews) because:
- Smaller scope
- Lower risk (test code, not production)
- Validates approach before touching core libraries

### Question 2: Convert FwKernel/Views or Keep nmake?

**Answer**: Keep nmake for FwKernel/Views initially because:
- They work via mkall.targets
- COM/IDL conversion is complex and risky
- Test projects are the immediate priority

### Question 3: vcpkg or Git Submodule for GoogleTest?

**Answer**: vcpkg (manifest mode) because:
- Instructions file already mentions vcpkg manifest mode
- Better version management
- Industry standard for C++ packages
- Works with MSBuild seamlessly

---

## Next Steps

1. **Create Task**: Add T-PRE-04 to tasks.md for TestGeneric vcxproj conversion
2. **Prototype**: Convert TestGeneric.vcxproj to true C++ project
3. **Add vcpkg**: Create vcpkg.json manifest with GoogleTest
4. **Validate**: Run tests via VS Test Explorer
5. **Document**: Update native-migration-plan.md with results

---

## CRITICAL: Makefile-to-vcxproj Conversion Checklist

When converting any nmake makefile to a true MSBuild vcxproj, you MUST perform a thorough line-by-line comparison. The legacy makefiles contain subtle settings that are easy to miss but cause runtime failures.

### Mandatory Comparison Process

**Step 1: Extract ALL settings from the makefile chain**

Read these files in order (each includes the previous):
1. The project's `.mak` file (e.g., `testGenericLib.mak`)
2. `Bld/_init.mak` - compiler flags, paths, preprocessor defines
3. `Bld/_rule.mak` - compilation rules, PCH handling
4. `Bld/_targ.mak` - linking rules, output directories
5. Any `*Inc.mak` files (e.g., `GenericInc.mak`) - object file lists

**Step 2: Check for these commonly-missed items**

| Category | What to Look For | Where in Makefile |
|----------|------------------|-------------------|
| **Preprocessor** | All `/D` defines | `DEFS=` in `_init.mak` |
| **Includes** | All `/I` paths | `USER_INCLUDE=` in project .mak |
| **Libraries** | All `.lib` dependencies | `LINK_LIBS=` in `_init.mak` and project .mak |
| **Lib Paths** | `/LIBPATH:` directories | `LINK_OPTS=` in `_init.mak` |
| **Runtime** | `/MD` vs `/MDd` vs `/MT` | `CL_OPTS=` in `_init.mak` |
| **Exceptions** | `/EHa` vs `/EHsc` | `CL_OPTS=` in `_init.mak` |
| **RTTI** | `/GR` flag | `CL_OPTS=` in `_init.mak` |
| **Warnings** | `/W4 /WX` | `CL_OPTS=` in `_init.mak` |
| **Debug Info** | `/Zi`, `/Fd` | `CL_OPTS=` in `_init.mak` |
| **Optimization** | `/Od` (debug) vs `/O2` (release) | `CL_OPTS=` in `_init.mak` |
| **Subsystem** | `/subsystem:console` or `windows` | `LINK_OPTS=` in `_init.mak` |
| **Output Name** | `BUILD_PRODUCT`, `BUILD_EXTENSION` | Project .mak file |

**Step 3: Check for environment variables and paths**

| Variable | Purpose | vcxproj Equivalent |
|----------|---------|-------------------|
| `BUILD_ROOT` | Repository root | `$(ProjectDir)..\..\..\` or `$(FwRoot)` |
| `OUT_DIR` | Output directory | `$(OutDir)` |
| `INT_DIR` | Intermediate objects | `$(IntDir)` |
| `COM_OUT_DIR` | Generated COM files | Usually `$(FwRoot)\Output\$(Configuration)\Common` |
| `USER_INCLUDE` | Include paths | `<AdditionalIncludeDirectories>` |

**Step 4: Check for DLL dependencies at runtime**

| DLL Type | Location | How to Handle |
|----------|----------|---------------|
| ICU 70 | `Output/$(Config)/lib/x64/` | Ensure DLLs are copied or PATH is set |
| unit++.lib | `Lib/$(Config)/` | Static link (no DLL) |
| Generic.lib | `Lib/$(Config)/` | Static link (no DLL) |
| DebugProcs.lib | `Lib/$(Config)/` | Static link (no DLL) |

**Step 5: Verify Unicode/Character Set handling**

FieldWorks code has `#define _UNICODE` and `#define UNICODE` in `common.h`.
**Do NOT set `<CharacterSet>Unicode</CharacterSet>`** in vcxproj - this causes duplicate macro definition warnings treated as errors.

Use `<CharacterSet>NotSet</CharacterSet>` instead.

**Step 6: Check for pre/post-build steps**

| Step | Makefile Location | vcxproj Equivalent |
|------|-------------------|-------------------|
| Generate Collection.cpp | Custom rule in project .mak | `<Target Name="GenerateCollection" BeforeTargets="ClCompile">` |
| Register COM | `BUILD_REGSVR=1` | Usually not needed for test projects |
| Copy DLLs | Various | `<Target AfterTargets="Build">` with `<Copy>` |

**Step 7: Verify the exact source files**

Extract from makefile:
```makefile
OBJ_ALL=$(OBJ_KERNELTESTSUITE)
OBJ_KERNELTESTSUITE=\
    $(INT_DIR)\genpch\Collection.obj\
    $(INT_DIR)\autopch\ModuleEntry.obj\
    $(INT_DIR)\autopch\testGeneric.obj\
```

Map to vcxproj:
```xml
<ItemGroup>
  <ClCompile Include="Collection.cpp" />
  <ClCompile Include="..\ModuleEntry.cpp" />
  <ClCompile Include="testGeneric.cpp" />
</ItemGroup>
```

### Validation Steps After Conversion

1. **Build succeeds**: `msbuild Project.vcxproj /p:Configuration=Debug /p:Platform=x64`
2. **No warnings**: Check for macro redefinition, missing includes, etc.
3. **Links correctly**: No unresolved externals
4. **Runs successfully**: Execute and verify exit code 0
5. **Same output**: Compare output with nmake-built version
6. **Tests pass**: All unit tests pass (not just "runs without crash")

### Common Pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| Missing `/EHa` | C++ exceptions not caught at boundaries | Add `<ExceptionHandling>Async</ExceptionHandling>` |
| Missing `/GR` | `dynamic_cast` fails | Add `<RuntimeTypeInfo>true</RuntimeTypeInfo>` |
| Wrong runtime | LNK2038 mismatch errors | Match `/MD` or `/MDd` exactly |
| Missing ICU DLLs | 0xC0000135 (DLL not found) | Copy from `lib/x64/` or set PATH |
| Missing defines | Compilation errors or wrong behavior | Check all `/D` flags in `_init.mak` |
| CharacterSet=Unicode | C4005 macro redefinition errors | Use `NotSet` - code defines UNICODE itself |

---

## Implementation Progress

### Status Summary

| Project | Status | Notes |
|---------|--------|-------|
| TestGeneric.vcxproj | ✅ Converted | True MSBuild Application; builds successfully |
| TestViews.vcxproj | ⬜ Not started | Similar approach needed |
| Generic.vcxproj | ⬜ Not started | Library (StaticLibrary) |
| DebugProcs.vcxproj | ⬜ Not started | Library (DynamicLibrary) |
| FwKernel.vcxproj | ⬜ Not started | COM DLL - more complex |
| views.vcxproj | ⬜ Not started | COM DLL - more complex |

### TestViews Architecture Decision: Link .obj Files vs Static Library

**Decision Date**: 2025-12-06
**Decision**: Link individual `.obj` files directly (Option 1)

TestViews has a unique dependency pattern: it links against ~40 individual `.obj` files from the Views project's intermediate directory (`Obj/$(Configuration)/Views/autopch/*.obj`). This is because:
1. Views builds as a DLL with only public API exported
2. TestViews needs access to internal implementation details for testing
3. No `Views.lib` static library exists

**Options Considered**:

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| **1. Link .obj files** | Specify all .obj in `<AdditionalDependencies>` | Minimal changes, fast to implement | Fragile coupling to IntDir layout, no /MP parallel |
| **2. Create Views.lib** | Modify Views to output static library | Clean architecture, project references | Requires Views.vcxproj changes, higher risk |
| **3. Object Library** | New project producing only .obj files | Single source of truth | Unfamiliar pattern, sparse documentation |

**Rationale for Option 1**:
- ⚠️ **Views is planned for sunset** in 2025 - investing in architectural improvements is not justified
- Option 1 maintains parity with existing makefile behavior
- Minimizes risk of introducing new issues in code scheduled for deprecation
- Conversion can be completed quickly, allowing focus on other priorities

**Tradeoffs Accepted**:
- Build order dependency on Views compilation (Views must build first)
- Manual maintenance if Views source files change (unlikely given sunset timeline)
- No `/MP` parallel compilation benefit for test sources (acceptable for test project)

---

### Completed: TestGeneric.vcxproj (2025-01-07)

**Original**: Makefile wrapper invoking `nmake /f testGenericLib.mak`
**Converted to**: True MSBuild C++ Application (`ConfigurationType=Application`)

**Key conversion decisions**:
1. **CharacterSet=NotSet** - `common.h` defines `_UNICODE`/`UNICODE` internally, so MSBuild's automatic definition causes redefinition errors
2. **ICU 70 libraries** - Using 64-bit ICU from `lib/x64/` (icuin70.lib, icuuc70.lib, icudt70.lib)
3. **Library directories** - Added `$(FwRoot)\Output\$(Configuration)\lib\x64` for ICU DLLs at link time
4. **Original preserved** - Backup at `TestGeneric.vcxproj.makefile.bak`

**Verified**:
- ✅ Builds successfully from VS/VS Code without VsDevCmd.bat
- ✅ Same compiler warnings as nmake build (LNK4099 PDB warnings expected)
- ⚠️ **Crash on run**: Exit code 0xC0000005 (access violation) - **pre-existing issue**, nmake-built version also crashes identically

### Lessons Learned

1. **Makefile wrappers hide issues** - The nmake build appeared to work but tests crash; converting to MSBuild didn't change this
2. **ICU DLL location** - Runtime needs DLLs in `Output/Debug/` or PATH set
3. **Common.h defines matter** - Many macros defined by common.h must not be redefined by MSBuild
4. **Both test executables crash** - TestGeneric and TestViews both crash with 0xC0000005, suggesting a common issue

---

## References

- [MSBuild C++ Projects](https://docs.microsoft.com/en-us/cpp/build/reference/vcxproj-files-and-wildcards)
- [vcpkg Manifest Mode](https://vcpkg.io/en/docs/users/manifests.html)
- [GoogleTest via vcpkg](https://vcpkg.io/en/packages.html) (search `gtest`)
- Current makefiles: `Bld/_init.mak`, `Bld/_rule.mak`, `Bld/_targ.mak`
- Build orchestration: `Build/mkall.targets`
