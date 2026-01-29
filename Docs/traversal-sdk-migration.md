# MSBuild Traversal SDK Migration Guide

## Overview

FieldWorks has migrated to **Microsoft.Build.Traversal SDK** for its build system. This provides declarative dependency ordering, automatic parallel builds, and better incremental build performance.

## What Changed

### For Regular Development

**Before:**
```powershell
# Old way - required -UseTraversal flag
.\build.ps1 -UseTraversal
.\build.ps1 -Targets all
```

**After:**
```powershell
# New way - traversal is default
.\build.ps1
.\build.ps1 -Configuration Release
```

### For Linux/macOS

FieldWorks is Windows-first; `build.sh` is not supported in this repo. Use `.\build.ps1`.

## Build Architecture

The build is now organized into 21 phases in `FieldWorks.proj`:

1. **Phase 1**: FwBuildTasks (build infrastructure)
2. **Phase 2**: Native C++ (DebugProcs, GenericLib, FwKernel, Views)
3. **Phase 3**: Code generation (ViewsInterfaces)
4. **Phases 4-14**: Managed C# projects (grouped by dependency)
5. **Phases 15-21**: Test projects

MSBuild automatically:
- Builds phases in order
- Parallelizes within phases where safe
- Tracks incremental changes
- Reports clear dependency errors

## Common Scenarios

### Full Build
```powershell
# Debug (default)
.\build.ps1

# Release
.\build.ps1 -Configuration Release

# With parallel builds
.\build.ps1 -MsBuildArgs @('/m')
```

### Incremental Build
Just run `.\build.ps1` again - MSBuild tracks what changed.

### Clean Build
```powershell
# Remove build artifacts
git clean -dfx Output/ Obj/

# Rebuild
.\build.ps1
```

### Single Project
```powershell
# Still works for quick iterations
msbuild Src/Common/FwUtils/FwUtils.csproj
```

### Native Components Only
```powershell
# Build just C++ components (Phase 2)
msbuild Build\Src\NativeBuild\NativeBuild.csproj
```

## Installer Builds

Installer builds use traversal internally but are invoked via MSBuild targets:

```powershell
# Base installer (calls traversal build via Installer.targets)
msbuild Build/Orchestrator.proj /t:BuildBaseInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release

# Patch installer (calls traversal build via Installer.targets)
msbuild Build/Orchestrator.proj /t:BuildPatchInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release
```

Note: The installer targets in `Build/Installer.targets` have been modernized to call `FieldWorks.proj` instead of the old `remakefw` target.

### Individual Project Builds
You can still build individual projects:
```powershell
msbuild Src/xWorks/xWorks.csproj /p:Configuration=Debug
```

### Output Directories
- Build output: `Output/Debug/` or `Output/Release/`
- Intermediate files: `Obj/<ProjectName>/`

## Troubleshooting

### "Cannot generate Views.cs without native artifacts"

**Problem**: ViewsInterfaces needs native build outputs (ViewsTlb.idl, FwKernelTlb.json)

**Solution**: Build native components first:
```powershell
msbuild Build\Src\NativeBuild\NativeBuild.csproj /p:Configuration=Debug /p:Platform=x64
.\build.ps1
```

### "Project X can't find assembly from Project Y"

**Problem**: Build order issue

**Solution**: The traversal build handles this automatically. If you see this:
1. Ensure both projects are in `FieldWorks.proj`
2. Check that Y is in an earlier phase than X
3. Report the issue so `FieldWorks.proj` can be updated

### Build Failures After Git Pull

**Problem**: Generated files or native artifacts out of sync

**Solution**: Clean and rebuild:
```powershell
git clean -dfx Output/ Obj/
.\build.ps1
```

### Parallel Build Race Conditions

**Problem**: Random failures with `/m` flag

**Solution**: Reduce parallelism temporarily:
```powershell
.\build.ps1 -MsBuildArgs @('/m:1')
```

Then report the race condition so dependencies can be fixed in `FieldWorks.proj`.

## Benefits

### Declarative Dependencies
- 110+ projects organized into 21 clear phases
- Dependencies expressed in `FieldWorks.proj`, not scattered across targets files
- Easy to understand build order

### Automatic Parallelism
- MSBuild parallelizes within phases where safe
- No manual `/m` tuning needed
- Respects inter-phase dependencies

### Better Incremental Builds
- MSBuild tracks `Inputs` and `Outputs` for each project
- Only rebuilds what changed
- Faster iteration during development

### Modern SDK Support
- Works with `dotnet build FieldWorks.proj`
- Compatible with modern .NET SDK tools
- Easier CI/CD integration

### Clear Error Messages
- "Cannot generate Views.cs..." tells you exactly what's missing
- Build failures point to specific dependency issues
- Easier troubleshooting

## Technical Details

### FieldWorks.proj Structure
```xml
<Project Sdk="Microsoft.Build.Traversal/4.1.0">
  <!-- Phase 1: Build Infrastructure -->
  <ItemGroup Label="Phase 1: Build Tasks">
    <ProjectReference Include="Build\Src\FwBuildTasks\FwBuildTasks.csproj" />
  </ItemGroup>

  <!-- Phase 2: Native C++ -->
  <ItemGroup Label="Phase 2: Native C++ Components">
    <ProjectReference Include="Build\FieldWorks.proj" Targets="allCppNoTest" />
  </ItemGroup>

  <!-- Phase 3: Code Generation -->
  <ItemGroup Label="Phase 3: Code Generation - ViewsInterfaces">
    <ProjectReference Include="Src\Common\ViewsInterfaces\ViewsInterfaces.csproj" />
  </ItemGroup>

  <!-- Phases 4-21: Managed projects and tests... -->
</Project>
```

### Build Flow
1. **RestorePackages**: Restore NuGet packages (handled by build.ps1)
2. **Traversal Build**: MSBuild processes FieldWorks.proj
   - Phase 1: Build FwBuildTasks (needed for custom tasks)
   - Phase 2: Build native C++ via mkall.targets
   - Phase 3: Generate ViewsInterfaces code from native IDL
   - Phases 4-14: Build managed projects in dependency order
   - Phases 15-21: Build test projects
3. **Output**: All binaries in `Output/<Configuration>/`

### Build Infrastructure
- **`FieldWorks.proj`** - Main build orchestration using Traversal SDK
- **`Build/FieldWorks.proj`** - Entry point for RestorePackages and installer targets
- **`Build/mkall.targets`** - Native C++ build orchestration (called by FieldWorks.proj Phase 2)
- **`Build/Installer.targets`** - Installer-specific targets (now calls FieldWorks.proj instead of remakefw)

## Migration Checklist for Scripts/CI

- [ ] Replace `.\build.ps1 -UseTraversal` with `.\build.ps1`
- [ ] Replace `.\build.ps1 -Targets all` with `.\build.ps1`
- [ ] For installer builds, use `msbuild Build/FieldWorks.proj /t:BuildBaseInstaller` instead of `.\build.ps1 -Target BuildBaseInstaller`
- [ ] Update documentation to show traversal as the standard approach
- [ ] Test that incremental builds work correctly
- [ ] Verify parallel builds are safe (`/m` flag)

## Questions?

See [.github/instructions/build.instructions.md](.github/instructions/build.instructions.md) for comprehensive build documentation.
