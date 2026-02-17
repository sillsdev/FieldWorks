---
spec-id: architecture/build-deploy/build-phases
created: 2026-02-05
status: draft
---

# Build Phases

## Purpose

Document the ordered build phases, file roles, and target flow for FieldWorks builds.

## Summary

- `build.ps1` is the single entry point for all builds (Debug, Release, tests, installer).
- Package restore is done directly via `dotnet restore FieldWorks.sln` in build.ps1.
- The traversal build (FieldWorks.proj) handles all 21 build phases including native C++.
- Installer builds use a separate `InstallerBuild.proj` invoked after the traversal completes.

## Context

FieldWorks uses the MSBuild Traversal SDK (`Microsoft.Build.Traversal/4.1.0`) for declarative build ordering. The traversal system defers to project-level MSBuild for each component, maintaining a strict phase order.

## Build Entry Points

### build.ps1 (PowerShell orchestrator)

The top-level entry point. Accepts parameters for configuration, platform, installer, tests, and local library overrides. Executes in this order:

1. **Bootstrap FwBuildTasks** — builds `BuildTools/FwBuildTasks/FwBuildTasks.csproj` so custom MSBuild tasks are available
2. **`dotnet restore FieldWorks.sln`** — NuGet package restore (all projects now in solution)
3. **`msbuild FieldWorks.proj`** — traversal build (21 ordered phases, native then managed)
4. **`msbuild InstallerBuild.proj /t:BuildInstaller`** — (optional) WiX installer build

### FieldWorks.proj (Traversal SDK)

Declares 21 build phases via `ProjectReference` items with `SetPlatform`/`SetConfiguration` metadata. Key phases:

| Phase | Content | Notes |
|-------|---------|-------|
| 2 | NativeBuild.csproj | C++ bridge: delegates to mkall.targets |
| 3–19 | Managed C# projects | Core libraries, services, UI |
| 12 | FxtExe.csproj | Conditional on BuildAdditionalApps=true |
| 15–21 | Test projects | Only when BuildTests=true |

### InstallerBuild.proj (Installer targets)

Non-SDK project that hosts the `BuildInstaller` target. Imports all `.targets` files
needed by the WiX installer pipeline (FwBuildTasks, SetupInclude, Windows, mkall, etc.).
Also hosts `BuildWindowsXslAssemblies` (invoked by `Directory.Build.targets` for XSL transforms).

## Build File Roles

| File | Purpose | Imported By |
|------|---------|-------------|
| `Build/InstallerBuild.proj` | Installer build entry point (BuildInstaller, BuildWindowsXslAssemblies) | build.ps1, Directory.Build.targets |
| `Build/mkall.targets` | Native C++ build orchestration (DebugProcs → GenericLib → FwKernel → Views), clean targets, registry setup | NativeBuild.csproj, InstallerBuild.proj |
| `Build/PackageRestore.targets` | Dependency downloads and CopyDlls (native binaries, ICU, LCM data) | mkall.targets (via Import) |
| `Build/SetupInclude.targets` | Environment setup, configuration normalization, version generation | InstallerBuild.proj, NativeBuild.csproj |
| `Build/Windows.targets` | Windows-specific initialization (output dirs, ICU version, XSL assemblies) | InstallerBuild.proj, NativeBuild.csproj |
| `Build/FwBuildTasks.targets` | UsingTask declarations for custom MSBuild tasks (Make, RegFree, etc.) | InstallerBuild.proj, NativeBuild.csproj |
| `Build/RegFree.targets` | Registration-free COM manifest generation | InstallerBuild.proj, NativeBuild.csproj |
| `Build/Localize.targets` | Crowdin localization integration | InstallerBuild.proj, NativeBuild.csproj |
| `Build/LocalLibrary.targets` | Local Palaso/LCM/Chorus development overrides | InstallerBuild.proj |
| `Build/SilVersions.props` | SIL ecosystem version numbers (shared with Directory.Packages.props) | PackageRestore.targets |
| `Build/Installer.Wix3.targets` | WiX 3 installer entry point | InstallerBuild.proj |
| `Build/Installer.targets` | WiX 6 installer pipeline | InstallerBuild.proj |
| `Build/Installer.legacy.targets` | WiX 3 build logic (harvesting, MSI, bundle) | Installer.Wix3.targets |
| `Build/Src/NativeBuild/NativeBuild.csproj` | Bridge between traversal SDK and native build system | FieldWorks.proj Phase 2 |

## Target Dependency Graph

```
build.ps1
├── msbuild FwBuildTasks.csproj          (bootstrap)
├── dotnet restore FieldWorks.sln        (package restore)
├── msbuild FieldWorks.proj              (traversal)
│   ├── Phase 2: NativeBuild.csproj
│   │   └── allCppNoTest (mkall.targets)
│   │       └── DebugProcs → GenericLib → FwKernel → Views
│   │       └── CopyDlls → downloadDlls (PackageRestore.targets)
│   ├── Phases 3-19: Managed C# projects
│   └── Phases 15-21: Test projects (if BuildTests=true)
└── msbuild InstallerBuild.proj /t:BuildInstaller  (optional)
    └── Wix3 or Wix6 pipeline
```

## Constraints

- Do not bypass the traversal build ordering when adding new projects.
- Native outputs (Phase 2) must be available before managed code generation (Phases 3+).
- Always build through `build.ps1` to ensure correct FwBuildTasks bootstrap and package restore ordering.
- Registration-free COM: do not register COM components globally.

## Anti-patterns

- Invoking `msbuild FieldWorks.proj` directly without first running `dotnet restore FieldWorks.sln`.
- Adding new `.targets` files without documenting them in this spec and updating imports.
- Bypassing `build.ps1` for ad-hoc builds that skip native phases or package restore.
