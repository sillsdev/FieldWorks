# Native C++ Test Build Fixes

## Overview

This document describes the fixes applied to enable building native C++ test projects (TestGeneric, TestViews) from Visual Studio, VS Code, and the command line.

## Problem Statement

The C++ test projects (`Src/Generic/Test/TestGeneric.vcxproj`, `Src/views/Test/TestViews.vcxproj`) could not be built because:

1. **Malformed XML namespace**: The vcxproj files contained `ns0:` prefixes on all XML elements, causing MSBuild to reject them with error MSB4041.

2. **Missing batch files**: The vcxproj files referenced non-existent batch files (e.g., `mkGenLib-tst.bat`) that were never checked into the repository.

3. **Missing Windows script**: The `CollectUnit++Tests.cmd` script (Windows equivalent of `.sh` script) was missing.

## Root Cause Analysis

### Why the vcxproj files had `ns0:` prefixes

These files were likely processed by a Python XML library that added namespace prefixes when round-tripping. The original files used the default namespace `xmlns="http://schemas.microsoft.com/developer/msbuild/2003"` without a prefix.

### Why the batch files don't exist

The C++ test projects were **never integrated into the modern build system**. The `Build/mkall.targets` file builds the main native components (Generic.lib, DebugProcs.dll, FwKernel.dll, Views.dll) via the `Make` MSBuild task, but the test projects were only buildable via legacy batch files that:
- Were likely in developers' local environments
- Were never committed to version control
- Referenced a workflow that predates the current build infrastructure

### Build System Architecture

```
FieldWorks.proj (Traversal SDK)
    └── Build/Src/NativeBuild/NativeBuild.csproj
            └── Build/mkall.targets (via Make task)
                    ├── DebugProcs.mak → DebugProcs.dll ✅
                    ├── GenericLib.mak → Generic.lib ✅
                    ├── FwKernel.mak → FwKernel.dll ✅
                    └── Views.mak → Views.dll ✅

NOT IN BUILD SYSTEM:
    ├── testGenericLib.mak → testGenericLib.exe ❌
    └── testViews.mak → TestViews.exe ❌
```

## Fixes Applied

### Fix 1: XML Namespace Correction

Removed `ns0:` prefix from all elements in 4 vcxproj files:

```powershell
# Before
<ns0:Project xmlns:ns0="http://schemas.microsoft.com/developer/msbuild/2003" ...>
  <ns0:ItemGroup>...</ns0:ItemGroup>
</ns0:Project>

# After
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ...>
  <ItemGroup>...</ItemGroup>
</Project>
```

**Files fixed:**
- `Src/DebugProcs/DebugProcs.vcxproj`
- `Src/Generic/Test/TestGeneric.vcxproj`
- `Src/views/Test/TestViews.vcxproj`
- `Src/LexText/ParserCore/XAmpleCOMWrapper/XAmpleCOMWrapper.vcxproj`

### Fix 2: Create CollectUnit++Tests.cmd

Created Windows batch equivalent of the Linux shell script:

**File: `Bin/CollectUnit++Tests.cmd`**
```batch
@echo off
REM Usage: CollectUnit++Tests.cmd <module> <filename1> <filename2>...<outputfile>
set BUILD_ROOT=%~dp0..
"%~dp0CollectCppUnitTests.exe" %*
```

This script invokes `CollectCppUnitTests.exe` to generate `Collection.cpp` which contains the Unit++ test suite registration code.

### Fix 3: Update vcxproj NMake Commands

Changed `TestGeneric.vcxproj` to invoke nmake directly instead of batch files:

```xml
<!-- Before -->
<NMakeBuildCommandLine>..\..\..\bin\mkGenLib-tst.bat DONTRUN</NMakeBuildCommandLine>

<!-- After -->
<NMakeBuildCommandLine>nmake /nologo BUILD_CONFIG=Debug BUILD_TYPE=d BUILD_ROOT=$(ProjectDir)..\..\..\ BUILD_ARCH=x64 /f testGenericLib.mak</NMakeBuildCommandLine>
```

## Building C++ Tests

### Prerequisites

1. Visual Studio 2022 with C++ Desktop Development workload
2. Run from VS Developer Command Prompt (or use VsDevCmd.bat)
3. Main native libraries must be built first (`.\build.ps1` or the allCppNoTest target)

### Build Commands

**From Developer Command Prompt:**
```cmd
cd Src\Generic\Test
nmake /nologo BUILD_CONFIG=Debug BUILD_TYPE=d BUILD_ROOT=<repo-root>\ BUILD_ARCH=x64 /f testGenericLib.mak
```

**From PowerShell (using cmd wrapper):**
```powershell
cmd /c "call ""C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"" -arch=amd64 >nul 2>&1 && cd /d <repo-root>\Src\Generic\Test && nmake /nologo BUILD_CONFIG=Debug BUILD_TYPE=d BUILD_ROOT=<repo-root>\ BUILD_ARCH=x64 /f testGenericLib.mak"
```

### Output Location

- `Output/Debug/testGenericLib.exe`
- `Output/Debug/TestViews.exe`

## Running C++ Tests

The test executables require ICU 70 DLLs which are in `Output/Debug/`:
- `icuin70.dll`
- `icuuc70.dll`

Run from the Output/Debug directory:
```cmd
cd Output\Debug
testGenericLib.exe
```

## Known Issues

1. **Exit code 1 with no output**: May indicate missing DLLs or a crash before test output. Check dependencies with `dumpbin /dependents testGenericLib.exe`.

2. **ICU version mismatch**: The native code links against ICU 70, ensure `icuin70.dll` and `icuuc70.dll` are present in the output directory.

## Future Work

1. **Add TestGeneric/TestViews targets to mkall.targets**: Integrate C++ tests into the main build system
2. **VS Code task integration**: Add tasks to build and run C++ tests
3. **GoogleTest migration**: Replace Unit++ framework with modern GoogleTest (see native-migration-plan.md)

## References

- `Build/mkall.targets` - Native build orchestration
- `Build/Src/FwBuildTasks/Make.cs` - Make MSBuild task implementation
- `Src/Generic/Test/testGenericLib.mak` - TestGeneric makefile
- `Src/views/Test/testViews.mak` - TestViews makefile
