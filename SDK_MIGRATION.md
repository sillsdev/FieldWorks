# FieldWorks SDK Migration - Comprehensive Summary

**Migration Period**: November 7-21, 2025
**Base Commit**: `8e508dab484fafafb641298ed9071f03070f7c8b`
**Final Commit**: `58d04c191260188832554740dfa642702c45721b`
**Total Commits**: 115
**Status**: ✅ **COMPLETE** - All systems operational

---

## Executive Summary

FieldWorks has completed a comprehensive modernization effort migrating from legacy .NET Framework project formats to modern SDK-style projects. This migration encompasses:

- **119 project files** converted to SDK-style format
- **336 C# source files** updated
- **111 projects** successfully building with new SDK format
- **64-bit only** architecture enforcement (x86/Win32 removed)
- **Registration-free COM** implementation (Native + Managed)
- **Unified launcher**: FieldWorks.exe replaced the historical LexText.exe stub across build, installer, and documentation
- **MSBuild Traversal SDK** for declarative builds
- **Test framework modernization** (RhinoMocks → Moq, NUnit 4 ready)
- **Central Package Management (CPM)** via `Directory.Packages.props`
- **Unified test runner** (`test.ps1`) for managed and native tests
- **Stale DLL detection** via single-pass pre-build validation
- **Installer validation tooling** with snapshot-based evidence collection
- **Binding redirect cleanup** — eliminated manual `<bindingRedirect>` entries
- **Developer environment tooling** (Defender exclusions, dependency verification)
- **AGENTS.md documentation convention** for AI agent and developer onboarding
- **140 legacy files** removed

**Key Achievement**: Zero legacy build paths remain. Everything uses modern SDK tooling.

---

## Table of Contents

1. [Migration Overview](#migration-overview)
2. [Project Conversions](#project-conversions)
3. [Build System Modernization](#build-system-modernization)
4. [Central Package Management (CPM)](#central-package-management-cpm)
5. [64-bit and Reg-Free COM](#64-bit-and-reg-free-com)
6. [Test Framework Upgrades](#test-framework-upgrades)
7. [Unified Test Runner](#unified-test-runner)
8. [Code Fixes and Patterns](#code-fixes-and-patterns)
9. [Migration Bug Fixes (LT-223xx)](#migration-bug-fixes-lt-223xx)
10. [Legacy Removal](#legacy-removal)
11. [Tooling and Automation](#tooling-and-automation)
12. [Installer Validation Tooling](#installer-validation-tooling)
13. [Developer Environment Setup](#developer-environment-setup)
14. [Documentation](#documentation)
15. [Statistics](#statistics)
16. [Lessons Learned](#lessons-learned)
17. [Build Challenges Summary](#build-challenges-summary)
18. [Validation and Next Steps](#validation-and-next-steps)

---

## Migration Overview

### Timeline and Phases

The migration occurred in multiple coordinated phases:

#### **Phase 1: Initial SDK Conversion** (Commits 1-21)
- Automated conversion of 119 .csproj files using `convertToSDK.py`
- Package reference updates and conflict resolution
- Removal of obsolete files
- Initial NUnit 3 → NUnit 4 migration

#### **Phase 2: Build Error Resolution** (Commits 22-40)
- Fixed package version mismatches (NU1605 errors)
- Resolved duplicate AssemblyInfo attributes (CS0579)
- Fixed XAML code generation issues (CS0103)
- Addressed interface member changes (CS0535)
- Resolved type conflicts (CS0436)

#### **Phase 3: Test Framework Modernization** (Commits 41-55)
- RhinoMocks → Moq conversion (6 projects, 8 test files)
- NUnit assertions upgrade (NUnit 3 → NUnit 4)
- Test infrastructure updates

#### **Phase 4: 64-bit Only Migration** (Commits 56-70)
- Removed Win32/x86/AnyCPU platform configurations
- Enforced x64 platform across all projects
- Updated native VCXPROJ files
- CI enforcement of x64-only builds

#### **Phase 5: Registration-Free COM** (Commits 71-78)
- Manifest generation implementation
- COM registration elimination
- Test host creation for reg-free testing

#### **Phase 6: Traversal SDK** (Commits 79-86)
- Complete MSBuild Traversal SDK implementation
- Legacy build path removal
- Build script modernization

#### **Phase 7: Final Polish** (Commits 87-93)
- Documentation completion
- Legacy file cleanup
- Build validation

#### **Phase 8: Convergence & Infrastructure** (Commits 94-115)
- **Convergence Specs**: Implemented Specs 002, 003, 004, 006
- **RegFree Overhaul**: Managed assembly support, tooling suite
- **Critical Fixes**: GDI double-buffering for black screen regression

#### **Phase 9: Central Package Management & Dependency Cleanup** (Post-115)
- **CPM Migration**: All NuGet packages centralized to `Directory.Packages.props`
- **Binding Redirect Cleanup**: Stripped manual `<bindingRedirect>` entries from 7+ `App.config` files
- **Stale DLL Detection**: Unified single-pass pre-build validation via `Remove-StaleDlls.ps1`
- **Dead Target Removal**: TeamCity download targets removed from `mkall.targets`

#### **Phase 10: Runtime Bug Fixes & Stabilization** (Post-115)
- **LT-22382**: Stale/mismatched DLL detection and Newtonsoft version conflict
- **LT-22392**: Memory leak in `TextsTriStateTreeView` (with regression test)
- **LT-22393**: Removed stale `DistFiles/Aga.Controls.dll` conflicting with NuGet version
- **LT-22394**: `System.Security.Permissions` / `DotNetZip` dependency pinning
- **LT-22395**: Missing information dialog on Text Chart tab
- **LT-22384**: Stack overflow error resolution
- **LT-22414**: Morph Type slice rebuild after SwapValues
- **XPath Safety**: Fixed XPath injection in `Directory.Build.targets` and XCore lookups

### Key Success Factors

1. **Automation First**: Created Python scripts for bulk conversions
2. **Systematic Approach**: Tackled one error category at a time
3. **Comprehensive Testing**: Validated each phase before proceeding
4. **Clear Documentation**: Maintained detailed records of all changes
5. **Reversibility**: Kept commits atomic for easy rollback if needed

---

## Project Conversions

### Total Projects Converted: 119

All FieldWorks C# projects have been converted from legacy .NET Framework format to modern SDK-style format.

#### **Conversion Approach**

**Automated Conversion** via `Build/convertToSDK.py`:
- Detected project dependencies automatically
- Converted assembly references to ProjectReference or PackageReference
- Preserved conditional property groups
- Set proper SDK type (standard vs. WindowsDesktop for WPF/XAML)
- Handled GenerateAssemblyInfo settings

**Key SDK Features Enabled**:
- Implicit file inclusion (no manual `<Compile Include>` needed)
- Simplified project structure
- PackageReference instead of packages.config
- Automatic NuGet restore
- Better incremental build support

### Project Categories

| Category | Count | Notable |
|----------|------:|--------|
| Build Infrastructure | 3 | `FwBuildTasks`, `NUnitReport`, `NativeBuild` (NEW) |
| Core Libraries | 18 | FwUtils, xCore, RootSite, FwCoreDlgs, XMLUtils, etc. |
| UI Controls | 8 | FwControls, Widgets, XMLViews, DetailControls, FlexUIAdapter |
| LexText Components | 18 | LexEdDll, MorphologyEditorDll, ITextDll, ParserCore, Discourse |
| Plugins and Tools | 12 | ParatextImport, FixFwData, UnicodeCharEditor, GenerateHCConfig |
| Utilities | 7 | Sfm2Xml, ConvertSFM, ComManifestTestHost (NEW) |
| External Libraries | 7 | ScrChecks, ObjectBrowser, Converter suite |
| Applications | 2 | `FieldWorks.csproj` (main app), `FxtExe.csproj` |
| Test Projects | 46 | All follow `<Component>Tests.csproj` pattern; 6 migrated RhinoMocks → Moq |

### SDK Format Template

All projects use `Microsoft.NET.Sdk` (or `Microsoft.NET.Sdk.WindowsDesktop` for WPF) targeting `net48` / `x64`. Key properties:
- `GenerateAssemblyInfo` — set per-project based on whether custom `AssemblyInfo.cs` attributes exist
- `<Compile Remove="ProjectTests/**" />` — excludes co-located test folders from production assembly
- Package versions managed centrally via `Directory.Packages.props` (see [CPM](#central-package-management-cpm))

### Package Version Standardization

| Package | Version | Notes |
|---------|---------|-------|
| `SIL.Core` / `SIL.Core.Desktop` | 17.0.0-* | Wildcard pre-release |
| `SIL.LCModel` / `SIL.LCModel.Core` / `SIL.LCModel.Utils` | 11.0.0-* | Wildcard pre-release |
| `System.Resources.Extensions` | 8.0.0 | Upgraded from 6.0.0 (NU1605 fix) |
| `NUnit` | 4.4.0 | Upgraded from 3.x |
| `Moq` | 4.20.70 | Replaced RhinoMocks |

---

## Central Package Management (CPM)

**Status**: ✅ Complete - All NuGet packages managed centrally

### Overview

All NuGet package versions are now centralized in `Directory.Packages.props` files, eliminating version drift across 110+ projects. Individual `.csproj` files use `<PackageReference>` without explicit `Version=` attributes; versions are resolved from the central file.

### Architecture

```
FieldWorks/
├── Directory.Packages.props          # Root CPM file (all shared packages)
├── Build/Src/Directory.Packages.props # Build-tool-specific overrides
└── FLExInstaller/Directory.Packages.props # Installer-specific overrides
```

### Key Changes

1. **Created `Directory.Packages.props`** at the repo root with 80+ package version entries
2. **Stripped explicit `Version=` attributes** from all `<PackageReference>` items across 108 project files
3. **Created `scripts/Agent/Migrate-ToCpm.ps1`** automation script for bulk migration
4. **Per-layer overrides** for build tools and installer projects that require different versions

### Binding Redirect Cleanup

With CPM ensuring all projects resolve to the same package version, most manual `<bindingRedirect>` entries in `App.config` files became unnecessary:

- **7 `App.config` files simplified** — ~140 lines of binding redirects removed
- **Files affected**: `FieldWorks/App.config`, `AppForTests.config`, `GenerateHCConfig/App.config`, `LCMBrowser/App.config`, `ParaText8PluginTests/App.config`, `UnicodeCharEditor/App.config`
- **Stale CPM migration artifacts** (leftover `packages.config`) deleted

### Benefits

1. **Single source of truth** for package versions — no more NU1605 version mismatch warnings
2. **Easier upgrades** — change one line to update a package across all projects
3. **Eliminated binding redirects** — consistent resolution removes the need for manual overrides
4. **Clearer dependency audit** — all package versions visible in one file

---

## Build System Modernization

### MSBuild Traversal SDK Implementation

**Status**: ✅ Complete - All builds use traversal SDK

#### **New Build Architecture**

**Core Files**:
1. **`FieldWorks.proj`** - Main traversal orchestrator (NEW)
   - Defines 21 build phases
   - Declarative dependency ordering
   - 110+ projects organized by dependency layer

2. **`Build/Orchestrator.proj`** - SDK-style build entry point (NEW)
   - Replaces legacy `Build/FieldWorks.proj`
   - Provides RestorePackages, BuildBaseInstaller, BuildPatchInstaller targets

3. **`Build/Src/NativeBuild/NativeBuild.csproj`** - Native build wrapper (NEW)
   - Bridges traversal SDK and native C++ builds
   - Referenced by FieldWorks.proj Phase 2

#### **Build Phases in FieldWorks.proj**

```
Phase 1:  FwBuildTasks (build infrastructure)
Phase 2:  Native C++ (via NativeBuild.csproj → mkall.targets)
Phase 3:  Code Generation (ViewsInterfaces from IDL)
Phase 4:  Foundation (FwUtils, FwResources, XMLUtils, Reporting)
Phase 5:  XCore Framework
Phase 6:  Basic UI (RootSite, SimpleRootSite)
Phase 7:  Controls (FwControls, Widgets)
Phase 8:  Advanced UI (Filters, XMLViews, Framework)
Phase 9:  FDO UI (FdoUi, FwCoreDlgs)
Phase 10: LexText Core (ParserCore, ParserUI)
Phase 11: LexText Apps (Lexicon, Morphology, Interlinear)
Phase 12: xWorks and Applications
Phase 13: Plugins (Paratext, Pathway)
Phase 14: Utilities
Phase 15-21: Test Projects (organized by component layer)
```

#### **Build Scripts Modernized**

**`build.ps1`** (Windows PowerShell):
- **Before**: 164 lines with `-UseTraversal` flag and legacy paths
- **After**: ~876 lines, always uses traversal
- Automatically bootstraps FwBuildTasks
- Initializes VS Developer environment
- Supports `/m` parallel builds
- **Stale DLL detection**: Runs `Remove-StaleDlls.ps1` pre-build to catch version-mismatched binaries
- **Diagnostics config**: Optionally copies dev trace config for Debug builds (`-TraceCrashes` or `UseDevTraceConfig`)
- **Installer support**: `-BuildInstaller` flag triggers full installer build pipeline

**Note**: `build.sh` is not supported in this repo (FieldWorks is Windows-first). Use `.\build.ps1`.

**Removed Parameters**:
- `-UseTraversal` (now always on)
- `-Targets` (use `msbuild Build/Orchestrator.proj /t:TargetName`)

#### **Updated Build Targets**

**`Build/mkall.targets`** - Native C++ orchestration:
- **Removed**: 210 lines of legacy targets
  - `mkall`, `remakefw*`, `allCsharp`, `allCpp` (test variants)
  - PDB download logic (SDK handles automatically)
  - Symbol package downloads
  - TeamCity-specific download targets (77 lines of dead code)
- **Kept**: `allCppNoTest` target for native-only builds

**`Build/Installer.targets`** - Installer builds:
- **Added**: `BuildFieldWorks` target that calls `FieldWorks.proj`
- **Removed**: Direct `remakefw` calls
- Now integrates with traversal build system

**`Build/RegFree.targets`** - Registration-free COM:
- Generates application manifests post-build
- Handles COM class/typelib/interface entries
- Integrated with EXE projects via BuildInclude.targets

#### **Build Usage**

**Standard Development**:
```powershell
# Windows
.\build.ps1                           # Debug x64
.\build.ps1 -Configuration Release    # Release x64

# Direct MSBuild
msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /m

# Dotnet CLI
dotnet build FieldWorks.proj
```

**Installer Builds**:
```powershell
msbuild Build/Orchestrator.proj /t:RestorePackages
msbuild Build/Orchestrator.proj /t:BuildBaseInstaller /p:Configuration=Debug /p:Platform=x64
```

**Native Only**:
```powershell
msbuild Build\Src\NativeBuild\NativeBuild.csproj /p:Configuration=Debug /p:Platform=x64
```

#### **Benefits Achieved**

1. **Declarative Dependencies**: Clear phase ordering vs. scattered targets
2. **Automatic Parallelism**: Safe parallel builds within phases
3. **Better Incremental Builds**: MSBuild tracks inputs/outputs per project
4. **Modern Tooling Support**: Works with dotnet CLI, VS Code, Rider
5. **Clear Error Messages**: "Cannot generate Views.cs without native artifacts. Run: msbuild Build\Src\NativeBuild\NativeBuild.csproj"
6. **Simplified Scripts**: Single code path, easier maintenance

---

## Unified Test Runner

**Status**: ✅ Complete - `test.ps1` handles all test orchestration

### Overview

`test.ps1` is a 530-line PowerShell script that provides a single entry point for running all FieldWorks tests — managed (NUnit via VSTest) and native (C++ via `Invoke-CppTest.ps1`).

### Usage

```powershell
# Run all tests (builds first)
.\test.ps1

# Run tests without building
.\test.ps1 -NoBuild

# Run tests for a specific project
.\test.ps1 -ProjectFilter "xWorksTests"

# Run with a test name filter
.\test.ps1 -TestFilter "TestClassName"

# Run native C++ tests only
.\test.ps1 -Native

# Run native tests without building
.\test.ps1 -Native -NoBuild

# List available test projects
.\test.ps1 -ListTests
```

### Key Features

1. **VSTest integration**: Uses `dotnet test` with `Test.runsettings` for managed test projects
2. **Native test support**: Dispatches to `scripts/Agent/Invoke-CppTest.ps1` for C++ googletest/unit++ tests
3. **Smart filtering**: Filter by project name or test name
4. **Build-aware**: Optionally builds before testing, respects configuration/platform
5. **CI-friendly**: Returns proper exit codes, structured output

### Related Scripts

| Script | Purpose |
|--------|--------|
| `test.ps1` | Main test orchestrator |
| `scripts/Agent/Invoke-CppTest.ps1` | Native C++ test runner (519 lines) |
| `Build/Agent/Run-VsTests.ps1` | VSTest wrapper for managed tests |
| `Build/Agent/Rebuild-TestProjects.ps1` | Rebuild test projects selectively |
| `Test.runsettings` | VSTest settings (timeouts, parallelism) |

## 64-bit and Reg-Free COM

### 64-bit Only Migration

**Status**: ✅ Complete - All x86/Win32/AnyCPU configurations removed

#### **Changes Made**

**1. Solution Platforms** (`FieldWorks.sln`):
- **Removed**: Debug|x86, Release|x86, Debug|AnyCPU, Release|AnyCPU, Debug|Win32, Release|Win32
- **Kept**: Debug|x64, Release|x64

**2. C# Projects** (`Directory.Build.props`):
```xml
<PropertyGroup>
  <PlatformTarget>x64</PlatformTarget>
  <Platforms>x64</Platforms>
  <Prefer32Bit>false</Prefer32Bit>
</PropertyGroup>
```

**3. Native C++ Projects** (8 VCXPROJ files):
- Removed Win32 configurations
- Kept x64 configurations
- Updated MIDL settings for 64-bit

**4. CI Enforcement** (`.github/workflows/CI.yml`):
```yaml
- name: Build
  run: ./build.ps1 -Configuration Debug -Platform x64
```

#### **Benefits**

- **Simpler maintenance**: One platform instead of 2-3
- **Consistent behavior**: No WOW64 emulation issues
- **Modern hardware**: All target systems are 64-bit
- **Smaller solution**: Faster solution loading in VS

### Registration-Free COM Implementation

**Status**: ✅ Complete - Comprehensive Native + Managed Support

#### **Architecture**

**Key Components**:
1. **RegFree MSBuild Task** (`Build/Src/FwBuildTasks/RegFree.cs`)
   - **New**: Uses `System.Reflection.Metadata` for lock-free inspection
   - **New**: Supports managed assemblies (`[ComVisible]`, `[Guid]`)
   - Generates `<file>`, `<comClass>`, `<typelib>`, `<clrClass>` entries
   - Handles dependent assemblies and proxy stubs

2. **Tooling Suite** (`scripts/regfree/`)
   - `audit_com_usage.py`: Scans codebase for COM instantiation patterns
   - `extract_clsids.py`: Harvests CLSIDs/IIDs from source
   - `generate_app_manifests.py`: Automates manifest creation for apps

3. **Build Integration** (`Build/RegFree.targets`):
   - Triggered post-build for WinExe projects
   - Processes all native DLLs and managed assemblies in output directory
   - Generates `<ExeName>.exe.manifest`

#### **Generated Manifests**

**FieldWorks.exe.manifest**:
- Main application manifest
- References `FwKernel.X.manifest` and `Views.X.manifest`
- Includes dependent assembly declarations
- **New**: Includes managed COM components (e.g., DotNetZip)

**FwKernel.X.manifest**:
- COM interface proxy stubs
- Interface registrations for marshaling

**Views.X.manifest**:
- **27+ COM classes registered**:
  - VwGraphicsWin32, VwCacheDa, VwRootBox
  - LgLineBreaker, TsStrFactory, TsPropsFactory
  - UniscribeEngine, GraphiteEngine
  - And more...

#### **Installer Integration**

**WiX Changes** (`FLExInstaller/CustomComponents.wxi`):
- Manifest files added to component tree
- Manifests co-located with FieldWorks.exe
- **No COM registration actions** in installer

**Validation**:
- FieldWorks.exe launches on clean VM without COM registration
- No `REGDB_E_CLASSNOTREG` errors
- Fully self-contained installation

#### **Test Infrastructure**

**ComManifestTestHost** (NEW):
- Test host with reg-free COM manifest
- Allows running COM-dependent tests without registration
- Located at `Src/Utilities/ComManifestTestHost/`

---

## Test Framework Upgrades

### RhinoMocks → Moq Migration

**Status**: ✅ Complete — 6 projects, 8 test files converted via `convert_rhinomocks_to_moq.py`

Key pattern changes:
- `MockRepository.GenerateStub<T>()` → `new Mock<T>().Object`
- `.Stub(...).Return(v)` → `.Setup(...).Returns(v)`
- `GetArgumentsForCallsMadeOn` → `Callback<T>` capture (manual conversion)
- Out parameters: `.OutRef(...)` → inline `out` variables in `.Setup(...)`

See `RHINOMOCKS_TO_MOQ_MIGRATION.md` for the full pattern catalog.

### NUnit 3 → NUnit 4 Migration

**Status**: ✅ Complete — All 46 test projects upgraded (NUnit 4.4.0, NUnit3TestAdapter 5.2.0)

Assertion syntax updated via `Build/convert_nunit.py`:
- `Assert.IsTrue(x)` → `Assert.That(x, Is.True)` (and similar for `IsFalse`, `IsNull`, `AreEqual`, etc.)
- Constraint-model assertions (`Assert.That`) are backwards-compatible and preferred

---

## Code Fixes and Patterns

~80 compilation errors fixed across 12 categories. See `MIGRATION_ANALYSIS.md` for full code examples.

| # | Error | Symptom | Fix |
|---|-------|---------|-----|
| 1 | NU1605 — Package version mismatch | Transitive downgrade warnings | Align explicit versions (e.g., `System.Resources.Extensions` 6.0→8.0) |
| 2 | CS0579 — Duplicate AssemblyInfo | SDK auto-generates when `GenerateAssemblyInfo=false` | Set to `true` per-project; remove manual `AssemblyInfo.cs` duplicates |
| 3 | CS0103 — XAML codegen missing | `InitializeComponent()` not generated | Use `Microsoft.NET.Sdk.WindowsDesktop` + `<UseWPF>true</UseWPF>` |
| 4 | CS0535 — Interface member missing | `IThreadedProgress.Canceling` added in SIL package update | Implement new members in all implementations |
| 5 | CS0436 — Type conflicts | Test files compiled into production assembly | `<Compile Remove="ProjectTests/**" />` |
| 6 | CS0234/CS0246 — Missing packages | Package references lost during conversion | Add explicit `<PackageReference>` entries |
| 7 | CS0738 — Generic interface mismatch | Mock used `ITextRepository` instead of `IRepository<IText>` | Use correct generic interfaces |
| 8 | NU1503 — C++ NuGet warnings | NuGet restore skips VCXPROJ | Suppress with `<NoWarn>NU1503</NoWarn>` |
| 9 | GDI+ / GDI rendering regression | `System.Drawing.Bitmap` pixel format incompatible with GDI `BitBlt` | Replace GDI+ double-buffer with native GDI DC |
| 10 | XPath injection | Apostrophes in tool/clerk IDs break XPath predicates | `concat()` quoting in build targets and XCore lookups |
| 11 | Stale DLL version mismatch | Old DLLs in `Output/` cause `FileLoadException` at runtime | Pre-build scanner `Remove-StaleDlls.ps1` |
| 12 | Transitive dependency conflicts | Runtime conflicts (e.g., `System.Security.Permissions`) | Global version pins in `Directory.Build.props`; remove stale `DistFiles/` copies |


---


## Migration Bug Fixes (LT-223xx)

The SDK migration exposed several latent bugs and dependency conflicts that were masked by the legacy build system. These were discovered and fixed during post-migration stabilization:

### Runtime Failures

| Ticket | Summary | Root Cause | Fix |
|--------|---------|------------|-----|
| **LT-22382** | Newtonsoft.Json version conflict; stale binaries | Mismatched DLL versions in `Output/` vs NuGet packages | Created `Remove-StaleDlls.ps1`; pinned package versions in CPM |
| **LT-22383** | Interlinear text missing (Gecko dependency) | Missing assembly reference after SDK conversion | Added explicit `<PackageReference>` |
| **LT-22384** | Stack overflow error | Recursive call path exposed by changed initialization order | Fixed recursive control initialization |

### UI Issues

| Ticket | Summary | Root Cause | Fix |
|--------|---------|------------|-----|
| **LT-22378** | Duplicate Create + Edit button on new entry | UI state not reset after SDK control initialization changes | Fixed button state management |
| **LT-22395** | Information dialog missing on Text Chart tab | `MessageBox` trigger key change caused "Do not show again" to persist | Renamed trigger from `TextChartNewFeature` to `TextChartTemplateWarning` to reset user preference |
| **LT-22414** | Morph Type slice disappears after SwapValues | Slice not rebuilt after property swap | Added explicit rebuild of Morph Type slice after `SwapValues` |

### Memory and Resource Issues

| Ticket | Summary | Root Cause | Fix |
|--------|---------|------------|-----|
| **LT-22392** | Memory leak in `TextsTriStateTreeView` | Event handlers not unsubscribed on dispose | Fixed disposal pattern; added regression test (LT-22396) |
| **LT-22393** | `Aga.Controls.dll` version conflict | `DistFiles/Aga.Controls.dll` (1.7.0.0) conflicted with NuGet 1.7.7.0 | Removed stale `DistFiles/` copy |
| **LT-22394** | `System.Security.Permissions` conflict | `DotNetZip` pulling different transitive version | Added global version pin (9.0.9) in `Directory.Build.props` |

### Key Lessons

1. **SDK-style projects change assembly resolution order** — stale binaries in output directories can shadow NuGet packages
2. **Transitive dependency conflicts only manifest at runtime** — compile-time success doesn't guarantee runtime correctness
3. **UI initialization order can change subtly** when project structure changes — regression testing is critical
4. **`DistFiles/` binaries must be audited** after moving to NuGet — old copies cause silent conflicts

---

## Legacy Removal

### Files Removed: 140

#### **Build Scripts** (29 batch files)
- `Bin/*.bat`, `Bin/*.cmd` - Pre-MSBuild build entry points
  - `mkall.bat`, `RemakeFw.bat`, `mk*.bat`
  - `CollectUnit++Tests.bat`, `BCopy.bat`
  - Duplicated functionality now in mkall.targets

#### **Legacy Tools** (12 binaries)
- `Bin/*.exe`, `Bin/*.dll` - Old build/test utilities
  - Replaced by modern SDK tooling or NuGet packages

#### **Obsolete Projects** (3 files)
- `Build/FieldWorks.proj` (non-SDK) - Replaced by `Build/Orchestrator.proj`
- `Build/native.proj` - Optional wrapper (removed)
- Legacy project files from non-SDK era

#### **Deprecated Configuration** (5 files)
- Old packages.config files
- Legacy NuGet.config entries
- Obsolete .targets includes

#### **Documentation** (0 files removed, but many updated)
All legacy references updated to point to new paths

#### **Test Infrastructure**
- nmock source (6 projects) - Replaced by Moq
- Legacy test helpers - Modernized

### Legacy Build Targets Removed

**From `Build/mkall.targets`** (210 lines removed):
- `mkall` - Use traversal build via build.ps1
- `remakefw` - Use traversal build
- `remakefw-internal`, `remakefw-ci`, `remakefw-jenkins` - No longer needed
- `allCsharp` - Managed by traversal SDK
- `allCpp` - Use `allCppNoTest` instead
- `refreshTargets` - Use `GenerateVersionFiles` if needed
- PDB download logic - SDK handles automatically
- Symbol package downloads - No longer needed

### Impact

**Before Migration**:
- Multiple build entry points (batch, PowerShell, Bash, MSBuild)
- Scattered build logic across 30+ files
- Manual dependency management
- Platform-specific quirks

**After Migration**:
- Single entry point: `build.ps1` → `FieldWorks.proj`
- Centralized build logic in traversal SDK
- Automatic dependency resolution
- Consistent build experience via traversal ordering

---

## Tooling and Automation

### Migration Scripts

| Script | Purpose |
|--------|---------|
| `Build/convertToSDK.py` | Bulk-converted 119 `.csproj` files to SDK format with intelligent dependency mapping |
| `Build/convert_nunit.py` | Automated NUnit 3 → 4 assertion syntax (20+ patterns) |
| `convert_rhinomocks_to_moq.py` | Automated RhinoMocks → Moq conversion |
| `add_package_reference.py` | Bulk-add `PackageReference` to multiple projects |
| `scripts/Agent/Migrate-ToCpm.ps1` | Automated migration to Central Package Management |

### Build Reliability Scripts

| Script | Purpose |
|--------|---------|
| `Build/Agent/Remove-StaleDlls.ps1` | Pre-build scanner: compares `Output/` DLLs against `Directory.Packages.props` versions |
| `Build/Agent/Verify-FwDependencies.ps1` | Environment validation (VS, .NET SDK, native toolchain, NuGet) |

---

## Installer Validation Tooling

**Status**: ✅ Complete - Snapshot-based evidence collection

### Overview

A comprehensive installer validation ecosystem was built to verify that FieldWorks installs correctly on clean machines. This uses a before/after snapshot approach to detect exactly what the installer changes on a system.

### Snapshot-Based Evidence Pipeline

| Script | Purpose |
|--------|---------|
| `scripts/Agent/Collect-InstallerSnapshot.ps1` | Capture machine state (registry, files, uninstall entries) as JSON |
| `scripts/Agent/Compare-InstallerSnapshots.ps1` | Diff two snapshots and produce a change report |
| `scripts/Agent/Invoke-InstallerCheck.ps1` | End-to-end: snapshot → install → snapshot → diff |
| `scripts/Agent/Compare-InstallerEvidenceRuns.ps1` | Compare multiple evidence runs |

### Hyper-V Clean-VM Testing

For isolated installer testing on pristine Windows environments:

| Script | Purpose |
|--------|---------|
| `scripts/Agent/New-HyperVTestVm.ps1` | Create a new Hyper-V test VM from Windows ISO |
| `scripts/Agent/New-HyperVCleanBaseline.ps1` | Create a clean baseline checkpoint |
| `scripts/Agent/Invoke-HyperVInstallerParity.ps1` | Run installer parity tests on Hyper-V VM |
| `scripts/Agent/Invoke-HyperVInstallerRun.ps1` | Execute installer inside Hyper-V guest |
| `scripts/Agent/Copy-HyperVParityPayload.ps1` | Copy build artifacts to Hyper-V guest |
| `scripts/Agent/Remove-FieldWorksAll.ps1` | Complete FieldWorks removal for clean slate |

### WiX 3 / WiX 6 Side-by-Side Support

- `Build/Installer.legacy.targets` — WiX 3 build targets (preserved for parity validation)
- `Build/Installer.Wix3.targets` — WiX 3-specific target overrides
- `Build/Installer.targets` — WiX 6 build targets (primary path)
- `scripts/Agent/New-Wix6ParityFixPlan.ps1` — Generate fix plans for WiX 3→6 parity gaps

---

## Developer Environment Setup

**Status**: ✅ Complete - Automated setup and verification

### `Setup-Developer-Machine.ps1`

Comprehensive developer machine setup script (350+ lines) that:
- Verifies Visual Studio 2022 installation with required workloads
- Checks .NET Framework 4.8 SDK and targeting pack
- Validates native build toolchain (MSVC, Windows SDK)
- Configures build environment variables
- Verifies NuGet feed access

### `Build/Agent/Setup-DefenderExclusions.ps1`

Windows Defender real-time scanning can add 30-50% to build times. This 390-line script:
- Adds process exclusions for `msbuild.exe`, `dotnet.exe`, `devenv.exe`, `cl.exe`, `link.exe`
- Adds path exclusions for `Output/`, `Obj/`, `packages/`, NuGet cache directories
- Requires elevated permissions; provides a dry-run preview mode
- Reversible — can remove all exclusions it added

### `Build/Agent/Verify-FwDependencies.ps1`

262-line script that validates the full build environment:
- Checks VS developer command prompt availability
- Validates NuGet package restore capability
- Verifies native toolchain (MIDL, `cl.exe`, Windows SDK headers)
- Checks for required environment variables
- Reports pass/fail with actionable fix suggestions

### `Setup-Local-Localization.ps1`

Helper script for reproducing localization-related issues (LT-22391):
- Generates localized `App.config` files for testing
- Configures culture-specific resource loading

---

## Documentation

The migration produced 125+ markdown files. Key documents:

| Document | Lines | Purpose |
|----------|------:|--------|
| `MIGRATION_ANALYSIS.md` | 413 | Detailed error fixes and validation steps |
| `TRAVERSAL_SDK_IMPLEMENTATION.md` | 327 | 21-phase build architecture |
| `NON_SDK_ELIMINATION.md` | 121 | Pure SDK achievement |
| `RHINOMOCKS_TO_MOQ_MIGRATION.md` | 151 | Mock framework conversion patterns |
| `MIGRATION_FIXES_SUMMARY.md` | 207 | Issue breakdown and patterns |
| `Docs/traversal-sdk-migration.md` | 239 | Developer migration guide |
| `Docs/64bit-regfree-migration.md` | 209 | 64-bit/reg-free plan |
| `.github/instructions/build.instructions.md` | — | Traversal-focused build guide |
| `specs/001-64bit-regfree-com/` | — | Requirements, plan, tasks, quickstart |

Per-folder `AGENTS.md` files describe architecture, dependencies, and testing for each major `Src/` subfolder. Kept current via `detect_copilot_needed.py` and `scaffold_copilot_markdown.py`.

---

## Statistics

| Metric | Value |
|--------|------:|
| Total commits | 115 |
| Files changed | 728 |
| Projects converted | 119 (111 SDK + 8 VCXPROJ) |
| Production projects | 73 |
| Test projects | 46 |
| Compilation errors fixed | ~80 |
| Files removed | 140 |
| Lines added / removed | ~15,000 / ~18,000 |
| RhinoMocks → Moq projects | 6 → 0 |
| NUnit version | 3.x → 4.4.0 |
| Build entry points | 30+ batch files → 1 (`build.ps1`) |
| Platforms | x86+x64+AnyCPU → x64 only |

---

## Lessons Learned

**What worked well**: Automation-first approach (Python scripts for 90% of conversions), systematic error resolution (one category at a time), incremental validation (no major rollbacks), comprehensive documentation.

**Key pitfalls**: Transitive dependency conflicts only visible at runtime; SDK auto-include silently compiles test code into production assemblies; RhinoMocks → Moq has no 1:1 mapping for advanced patterns; `GenerateAssemblyInfo` needs per-project evaluation.

**Best practices established**:
- Always set `GenerateAssemblyInfo` explicitly per-project
- Exclude test directories: `<Compile Remove="ProjectTests/**" />`
- Use `Microsoft.NET.Sdk.WindowsDesktop` for WPF projects
- Keep `PlatformTarget=x64` and `Prefer32Bit=false` everywhere
- Use Traversal SDK for multi-project ordering with declared dependencies
- Prefer modern test frameworks (NUnit 4, Moq) for active maintenance

---


## Build Challenges Summary

Key challenges and lessons from the migration, summarized for reference. See `MIGRATION_ANALYSIS.md` and `MIGRATION_FIXES_SUMMARY.md` for full detail.

| Challenge | Resolution | Lesson |
|-----------|-----------|--------|
| Mass conversion of 119 projects | Automated via `convertToSDK.py` (575 lines) | Invest upfront in automation; script handled 95% of cases |
| Native ↔ managed ordering | Traversal SDK with 21 ordered phases | Declare dependencies explicitly; let MSBuild parallelize |
| Transitive dependency conflicts (NU1605) | Central Package Management + version pinning | Use `Directory.Packages.props` from the start |
| Test code in production assemblies (CS0436) | Explicit `<Compile Remove>` for nested test folders | SDK auto-include means you must explicitly exclude tests |
| XAML codegen failures (CS0103) | Switch to `Microsoft.NET.Sdk.WindowsDesktop` | Check project type during conversion |
| Stale DLLs after migration | Pre-build scanner (`Remove-StaleDlls.ps1`) | Always validate output directory against declared versions |
| RegFree COM manifest generation | `RegFree.targets` + installer integration | Start with full EXE audit, not just the main app |
| GDI+ / GDI rendering regression | Replaced `System.Drawing.Bitmap` with native GDI DC | Test rendering early; pixel-format mismatches are silent |
| Mock framework migration (RhinoMocks → Moq) | Automated script + manual patterns for complex cases | `GetArgumentsForCallsMadeOn` → `Callback` capture requires manual work |
| GenerateAssemblyInfo inconsistency | Per-project evaluation; ~40 projects switched to `true` | Evaluate per-project needs upfront, don't blanket-set |

---

## Validation and Next Steps

### Validation Checklist

#### **Build Validation** ✅
- [x] Clean build completes: `.\build.ps1`
- [x] Release build completes: `.\build.ps1 -Configuration Release`
- [x] Linux build: N/A (FieldWorks is Windows-first; `build.sh` is not supported)
- [x] Incremental builds work correctly
- [x] Parallel builds safe: `.\build.ps1 -MsBuildArgs @('/m')`
- [x] Native-only build: `msbuild Build\Src\NativeBuild\NativeBuild.csproj`
- [x] Individual project builds work

#### **Installer Validation** ✅
- [x] Base installer builds successfully
- [x] Patch installer builds successfully
- [x] Manifests included in installer
- [x] Clean install works on test VM (via Hyper-V evidence pipeline)
- [x] FieldWorks.exe launches without COM registration

#### **Test Validation** ✅
- [x] All test projects build
- [x] Test suites run successfully via `test.ps1`
- [x] COM tests work with reg-free manifests
- [x] No test regressions from framework changes

#### **CI Validation** ✅
- [x] CI builds pass
- [x] x64 platform enforced
- [x] Manifests uploaded as artifacts
- [x] Commit message checks pass
- [x] Whitespace checks pass

#### **Documentation Validation** ✅
- [x] Build instructions accurate
- [x] Migration guides complete
- [x] Architecture documentation current
- [x] All cross-references valid
- [x] Troubleshooting sections helpful

### Known Issues (Resolved)

#### **Issue 1: Installer Testing** ✅ Resolved
- **Status**: Validated via Hyper-V evidence pipeline and snapshot-based testing
- **Resolution**: Created full installer validation tooling suite (see [Installer Validation Tooling](#installer-validation-tooling))

#### **Issue 2: Full Test Suite Run** ✅ Resolved
- **Status**: Full suite runs via `test.ps1`
- **Resolution**: Created unified test runner supporting managed (VSTest) and native (C++) tests

#### **Issue 3: Com Manifest Test Host Integration** ✅ Resolved
- **Status**: Integrated with test infrastructure
- **Resolution**: `ComManifestTestHost` used in reg-free COM test scenarios

### Next Steps

#### **Immediate** ✅ Completed

1. **Full Test Suite** — Runs via `.\test.ps1`
2. **Installer Validation** — Built and validated via evidence pipeline
3. **Performance Baseline** — Build times profiled

#### **Short Term** ✅ Completed

1. **Test Host Integration** — ComManifestTestHost integrated
2. **Additional Executable Manifests** — FieldWorks.exe manifest validated
3. **CI Enhancements** — Build, test, whitespace, and commit message checks active

4. **Developer Experience**
   - VS Code tasks for build, test, installer, worktree management, and multi-agent workflows (`.vscode/tasks.json`)
   - Add build troubleshooting FAQ
   - Streamline onboarding documentation

#### **Medium Term (Quarter 1)**

1. **Complete 64-bit Migration**
   - Remove any remaining x86 references
   - Audit all native dependencies
   - Update third-party component handling

2. **Test Suite Stabilization**
   - Address flaky tests
   - Improve test performance
   - Expand code coverage

3. **Build Optimization**
   - Profile build times
   - Optimize slow projects
   - Improve caching strategies

4. **Documentation Maintenance**
   - Keep migration docs current
   - Add examples for common scenarios
   - Create video walkthroughs

#### **Long Term (Year 1)**

1. **Consider .NET Upgrade**
   - Evaluate .NET 8+ migration path
   - Assess third-party compatibility
   - Plan phased approach

2. **Build System Evolution**
   - Explore additional MSBuild SDK benefits
   - ✅ Central Package Management — implemented via `Directory.Packages.props`
   - Evaluate build caching solutions

3. **Automation Expansion**
   - More build process automation
   - Automated dependency updates
   - Continuous integration improvements

---

## Appendix: Key References

### Repository Structure

```
FieldWorks/
├── Build/                    # Build system
│   ├── Src/
│   │   ├── FwBuildTasks/    # Custom MSBuild tasks
│   │   ├── NativeBuild/     # Native C++ build wrapper (NEW)
│   │   └── NUnitReport/     # Test reporting
│   ├── Orchestrator.proj    # Build entry point (NEW, replaces FieldWorks.proj)
│   ├── mkall.targets        # Native build orchestration (modernized)
│   ├── Installer.targets    # Installer build (updated)
│   ├── RegFree.targets      # Reg-free COM manifest generation (NEW)
│   ├── SetupInclude.targets # Environment setup
│   └── convertToSDK.py      # Project conversion script (NEW)
├── Src/                      # All source code
│   ├── Common/               # Shared components
│   ├── LexText/              # Lexicon and text components
│   ├── xWorks/               # xWorks application
│   ├── FwCoreDlgs/           # Core dialogs
│   ├── Utilities/            # Utility projects
│   └── XCore/                # XCore framework
├── Lib/                      # External libraries
├── Output/                   # Build output (Debug/, Release/)
├── Obj/                      # Intermediate build files
├── .github/                  # CI/CD and documentation
│   ├── instructions/         # Domain-specific guidelines
│   ├── workflows/            # GitHub Actions
│   └── memory.md             # Build system decisions
├── Docs/                     # Technical documentation
├── scripts/                  # Automation and agent scripts
│   └── Agent/                # Build, test, and installer validation scripts
├── FieldWorks.proj           # Traversal build orchestrator (NEW)
├── Directory.Build.props     # Global MSBuild properties
├── Directory.Packages.props  # Central Package Management (NEW)
├── FieldWorks.sln            # Main solution (x64 only)
├── build.ps1                 # Windows build script (modernized)
└── test.ps1                  # Test runner script (NEW)
```

### Key Files

| File                                       | Purpose                            | Status                               |
| ------------------------------------------ | ---------------------------------- | ------------------------------------ |
| `FieldWorks.proj`                          | MSBuild Traversal SDK orchestrator | NEW                                  |
| `Build/Orchestrator.proj`                  | SDK-style build entry point        | NEW (replaces Build/FieldWorks.proj) |
| `Build/Src/NativeBuild/NativeBuild.csproj` | Native build wrapper               | NEW                                  |
| `Build/RegFree.targets`                    | Manifest generation                | NEW                                  |
| `Directory.Build.props`                    | Global properties (x64, net48)     | Enhanced                             |
| `Directory.Packages.props`                 | Central Package Management         | NEW                                  |
| `build.ps1`                                | Windows build script               | Modernized                           |
| `test.ps1`                                 | Unified test runner                | NEW (530 lines)                      |
| `Build/Agent/Remove-StaleDlls.ps1`        | Pre-build stale DLL scanner        | NEW                                  |
| `Build/Agent/Verify-FwDependencies.ps1`   | Environment validation             | NEW                                  |
| `Build/Agent/Setup-DefenderExclusions.ps1`| Defender exclusion management       | NEW                                  |
| `scripts/Agent/Migrate-ToCpm.ps1`         | CPM migration automation           | NEW                                  |
| `scripts/Agent/Invoke-CppTest.ps1`        | Native C++ test runner             | NEW                                  |
| `scripts/Agent/Invoke-InstallerCheck.ps1` | Installer evidence pipeline        | NEW                                  |

### Migration Documents

| Document                          | Lines | Purpose                    |
| --------------------------------- | ----- | -------------------------- |
| `MIGRATION_ANALYSIS.md`           | 413   | Detailed error fixes       |
| `TRAVERSAL_SDK_IMPLEMENTATION.md` | 327   | Traversal SDK architecture |
| `NON_SDK_ELIMINATION.md`          | 121   | Pure SDK achievement       |
| `RHINOMOCKS_TO_MOQ_MIGRATION.md`  | 151   | Test framework conversion  |
| `MIGRATION_FIXES_SUMMARY.md`      | 207   | Issue breakdown            |
| `Docs/traversal-sdk-migration.md` | 239   | Developer guide            |
| `Docs/64bit-regfree-migration.md` | 209   | 64-bit/reg-free plan       |
| `SDK-MIGRATION.md` (this file)    | ~1050 | Comprehensive summary      |

### Build Commands

```powershell
# Standard Development
.\build.ps1                              # Debug x64 build
.\build.ps1 -Configuration Release       # Release x64 build

# Direct MSBuild
msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /m
dotnet build FieldWorks.proj

# Testing
.\test.ps1                               # Run all tests
.\test.ps1 -NoBuild                      # Run tests without building
.\test.ps1 -ProjectFilter "xWorksTests"  # Run specific project tests
.\test.ps1 -TestFilter "ClassName"       # Run by test name
.\test.ps1 -Native                       # Run native C++ tests

# Installers
msbuild Build/Orchestrator.proj /t:RestorePackages
msbuild Build/Orchestrator.proj /t:BuildBaseInstaller /p:config=release

# Native Only
msbuild Build\Src\NativeBuild\NativeBuild.csproj /p:Configuration=Debug /p:Platform=x64

# Individual Project
msbuild Src/Common/FwUtils/FwUtils.csproj

# Clean
git clean -dfx Output/ Obj/
.\build.ps1
```

### Contact and Support

For questions about this migration:
- **Build System**: See `.github/instructions/build.instructions.md`
- **Project Conversions**: Review `MIGRATION_ANALYSIS.md` for patterns
- **Test Frameworks**: See `RHINOMOCKS_TO_MOQ_MIGRATION.md`
- **64-bit/Reg-Free**: See `Docs/64bit-regfree-migration.md`

---

## Conclusion

The FieldWorks SDK migration represents a comprehensive modernization of a large, complex codebase:

✅ **119 projects** successfully converted to SDK format
✅ **Zero legacy build paths** - fully modern architecture
✅ **64-bit only** - simplified platform support
✅ **Registration-free COM** - self-contained installation
✅ **MSBuild Traversal SDK** - declarative, maintainable builds
✅ **Central Package Management** - unified dependency versions
✅ **Modern test frameworks** - NUnit 4, Moq
✅ **Unified test runner** - `test.ps1` for managed and native tests
✅ **Stale DLL detection** - automated pre-build validation
✅ **Installer validation tooling** - snapshot-based evidence collection
✅ **Developer environment automation** - setup, Defender exclusions, dependency verification
✅ **15+ migration bug fixes** - runtime issues discovered and resolved (LT-22378 through LT-22414)
✅ **140 legacy files removed** - reduced maintenance burden
✅ **Comprehensive documentation** - knowledge transfer complete

**The migration is operationally complete**. All builds work, all systems function, and the codebase is positioned for future growth.

**Key Takeaway**: A well-planned, systematically executed migration with strong automation and documentation can successfully modernize even large legacy codebases.

---

*Document Version: 1.1*
*Last Updated: 2026-02-17*
*Migration Status: ✅ COMPLETE*
