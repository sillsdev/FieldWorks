# FieldWorks SDK Migration — Postmortem & Blame Index

**Migration Period**: November 7-21, 2025
**Base Commit**: `8e508dab484fafafb641298ed9071f03070f7c8b`
**Final Commit**: `58d04c191260188832554740dfa642702c45721b`
**Total Commits**: 115
**Status**: ✅ **COMPLETE** - All systems operational

---

## Executive Summary

FieldWorks migrated from legacy .NET Framework project formats to modern SDK-style projects (Nov 7–21, 2025). 119 projects converted, 140 legacy files removed, 15+ runtime bugs fixed post-migration. Zero legacy build paths remain.

**If something broke after this migration**, start with the Quick Blame Lookup below, then check [Code Fixes and Patterns](#code-fixes-and-patterns) and [Migration Bug Fixes](#migration-bug-fixes-lt-223xx).

---

## Quick Blame Lookup

*"Did the SDK migration cause this?"* — check here first.

| Symptom | Likely Cause | Fix / Ticket |
|---------|-------------|---------------|
| `FileLoadException` or wrong DLL version at runtime | Stale DLLs in `Output/` shadowing NuGet packages | Run `Remove-StaleDlls.ps1`; see LT-22382 |
| `REGDB_E_CLASSNOTREG` (COM class not registered) | Missing reg-free COM manifest | Rebuild with `RegFree.targets`; check `FieldWorks.exe.manifest` |
| Black screen / rendering corruption | GDI+ `Bitmap` incompatible with GDI `BitBlt` after SDK switch | Use native GDI DC instead of `System.Drawing.Bitmap` |
| `InitializeComponent()` not found (CS0103) | WPF project using wrong SDK | Switch to `Microsoft.NET.Sdk.WindowsDesktop` |
| Duplicate type errors (CS0436) | Test code compiled into production assembly | Add `<Compile Remove="ProjectTests/**" />` |
| Duplicate `AssemblyInfo` attributes (CS0579) | SDK auto-generates + manual `AssemblyInfo.cs` | Set `GenerateAssemblyInfo=true`; remove manual duplicates |
| NuGet version conflict (NU1605) | Transitive downgrade across projects | Pin version in `Directory.Packages.props` |
| Stack overflow on startup | Recursive init path exposed by changed load order | See LT-22384 |
| Memory leak in tree views | Event handlers not unsubscribed | See LT-22392 |
| UI button duplicated / dialog missing | Control init order changed subtly | See LT-22378, LT-22395, LT-22414 |
| `System.Security.Permissions` conflict | `DotNetZip` transitive dependency | Global pin in `Directory.Build.props`; see LT-22394 |
| XPath failures with apostrophes | Unquoted string literals in XPath predicates | Use `concat()` quoting; see Code Fixes #10 |

---

## Table of Contents

1. [Quick Blame Lookup](#quick-blame-lookup)
2. [Migration Overview](#migration-overview)
3. [Project Conversions](#project-conversions)
4. [Build System Modernization](#build-system-modernization)
5. [Central Package Management (CPM)](#central-package-management-cpm)
6. [64-bit and Reg-Free COM](#64-bit-and-reg-free-com)
7. [Test Framework Upgrades](#test-framework-upgrades)
8. [Code Fixes and Patterns](#code-fixes-and-patterns)
9. [Migration Bug Fixes (LT-223xx)](#migration-bug-fixes-lt-223xx)
10. [Legacy Removal](#legacy-removal)
11. [Tooling and Automation](#tooling-and-automation)
12. [Statistics](#statistics)
13. [Lessons Learned](#lessons-learned)
14. [Validation Status](#validation-status)
15. [Appendix: Related Documents](#appendix-related-documents)

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

21 ordered build phases defined in `FieldWorks.proj` — native C++ first (Phase 2), managed code (Phases 3–14), tests last (Phases 15–21). See the file for details.

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

Build commands: see `.github/instructions/build.instructions.md` or `build.ps1 -Help`.

---

## 64-bit and Reg-Free COM

### 64-bit Only Migration

**Status**: ✅ Complete - All x86/Win32/AnyCPU configurations removed

#### **Changes Made**

**1. Solution Platforms** (`FieldWorks.sln`):
- **Removed**: Debug|x86, Release|x86, Debug|AnyCPU, Release|AnyCPU, Debug|Win32, Release|Win32
- **Kept**: Debug|x64, Release|x64

**2. C# Projects**: `<PlatformTarget>x64</PlatformTarget>` + `<Prefer32Bit>false</Prefer32Bit>` in `Directory.Build.props`

**3. Native C++ Projects**: 8 VCXPROJ files — Win32 configurations removed, MIDL updated for 64-bit

**4. CI Enforcement**: `./build.ps1 -Configuration Debug -Platform x64` in `.github/workflows/CI.yml`

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

**Generated manifests**: `FieldWorks.exe.manifest` (main app + managed COM), `FwKernel.X.manifest` (proxy stubs), `Views.X.manifest` (27+ COM classes). Installer includes manifests with zero COM registration actions. `ComManifestTestHost` enables reg-free COM testing.

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

### Infrastructure Created

Created `test.ps1` (unified managed + native test runner), installer validation pipeline (`scripts/Agent/Invoke-InstallerCheck.ps1` + Hyper-V clean-VM testing), and developer environment automation (`Setup-Developer-Machine.ps1`, `Setup-DefenderExclusions.ps1`, `Verify-FwDependencies.ps1`). WiX 3/6 side-by-side targets maintained for parity validation. VS Code tasks for build, test, installer, worktree management, and multi-agent workflows (`.vscode/tasks.json`).

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

## Validation Status

All validation gates passed: clean/release/incremental builds, installer (base + patch on clean VM), full test suite via `test.ps1`, CI enforcement (x64, manifests, whitespace, commit messages).

### Known Issues (All Resolved)

| Issue | Resolution |
|-------|------------|
| Installer testing on clean machines | Built Hyper-V snapshot pipeline (`Invoke-InstallerCheck.ps1`) |
| Native + managed test orchestration | Created unified `test.ps1` (managed VSTest + native C++) |
| Reg-free COM in test context | Created `ComManifestTestHost` for manifest-aware test execution |

---

## Appendix: Related Documents

| Document | Purpose |
|----------|--------|
| `MIGRATION_ANALYSIS.md` | Detailed error fixes and code examples |
| `TRAVERSAL_SDK_IMPLEMENTATION.md` | 21-phase traversal SDK architecture |
| `RHINOMOCKS_TO_MOQ_MIGRATION.md` | Mock framework conversion patterns |
| `MIGRATION_FIXES_SUMMARY.md` | Issue breakdown and patterns |
| `Docs/traversal-sdk-migration.md` | Developer migration guide |
| `Docs/64bit-regfree-migration.md` | 64-bit / reg-free COM plan |
| `.github/instructions/build.instructions.md` | Build commands and usage |

---

## Conclusion

119 projects converted, 140 legacy files removed, 15+ runtime bugs fixed (LT-22378–LT-22414), zero legacy build paths remain. The migration is operationally complete.

---

*Document Version: 2.0 — Postmortem / Blame Index*
*Last Updated: 2026-02-17*
*Migration Status: ✅ COMPLETE*
