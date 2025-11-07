---
applyTo: "**/*"
description: "FieldWorks build guidelines and inner-loop tips"
---
# Build guidelines and inner-loop tips

## Quick Start

### Recommended: Traversal SDK Build (Modern)
```powershell
# Full build with automatic dependency ordering
.\build.ps1 -UseTraversal

# Specific configuration
.\build.ps1 -UseTraversal -Configuration Release -Platform x64
```

**Benefits:**
- Declarative dependency ordering (110+ projects organized into 21 phases)
- Automatic parallel builds where safe
- Better incremental build performance
- Works with `dotnet build dirs.proj`
- Clear error messages when prerequisites missing

### Legacy Build (Maintained for Compatibility)
```powershell
# Traditional build approach
.\build.ps1

# Specific target
.\build.ps1 -Targets ViewsInterfaces
```

## Build Architecture

### Traversal Build Phases
The `dirs.proj` file defines a declarative build order:

1. **Phase 1**: FwBuildTasks (build infrastructure)
2. **Phase 2**: Native C++ components (via `allCppNoTest` target)
   - DebugProcs  GenericLib  FwKernel  Views
   - Generates IDL files: `ViewsTlb.idl`, `FwKernelTlb.json`
3. **Phase 3**: Code generation
   - ViewsInterfaces (idlimport: ViewsTlb.idl + FwKernelTlb.json  Views.cs)
4. **Phases 4-14**: Managed projects in dependency order
   - Foundation (FwUtils, FwResources, xCore)
   - UI Components (RootSite, Controls, Widgets)
   - Applications (xWorks, LexText)
5. **Phases 15-21**: Test projects

### Dependency Enforcement
The build will fail with clear errors if prerequisites are missing:
```
Error: Cannot generate Views.cs without native artifacts.
Run: msbuild Build\FieldWorks.proj /t:allCppNoTest
```

## Context loading
- Always initialize the environment when using scripts: `source ./environ`.
- The build system automatically sets up VS Developer Environment if needed.
- Environment variables (`fwrt`, `Platform`, etc.) are set by `SetupInclude.targets`.

## Deterministic requirements

### Inner loop (Developer Workflow)
- **First build**: Use traversal for automatic ordering: `.\build.ps1 -UseTraversal`
- **Incremental**: Only changed projects rebuild (MSBuild tracks `Inputs`/`Outputs`)
- **Avoid full clean** unless:
  - Native artifacts corrupted (delete `Output/`, then rebuild native first)
  - Generated code out of sync (delete `Src/Common/ViewsInterfaces/Views.cs`)

### Choose the right path
- **Full system build**: `.\build.ps1 -UseTraversal` (recommended)
- **Single project**: `msbuild Src/<Path>/<Project>.csproj`
- **Solution file**: `msbuild FieldWorks.sln /m /p:Configuration=Debug`
- **Native only**: `msbuild Build/FieldWorks.proj /t:allCppNoTest`
- **Managed only**: `msbuild Build/FieldWorks.proj /t:allCsharp` (requires native artifacts)
- **Installer**: Only build when changing installer logic (requires WiX Toolset)

### Configuration Options
```powershell
# Debug build (default, includes PDB symbols)
.\build.ps1 -UseTraversal -Configuration Debug

# Release build (optimized, smaller binaries)
.\build.ps1 -UseTraversal -Configuration Release

# Platform selection (x64 is default and recommended)
.\build.ps1 -UseTraversal -Platform x64
```

## Troubleshooting

### Native Artifacts Missing
**Symptom**: ViewsInterfaces fails with "Cannot generate Views.cs"

**Solution**:
```powershell
# Build native components first
msbuild Build/FieldWorks.proj /t:allCppNoTest /p:Configuration=Debug /p:Platform=x64

# Then continue with traversal build
.\build.ps1 -UseTraversal
```

### Build Order Issues
**Symptom**: Project X fails because it can''t find assembly from project Y

**Solution**: The traversal build handles this automatically. If using legacy build:
- Check `Build/FieldWorks.targets` for target dependencies
- Ensure proper `DependsOnTargets` in custom targets
- Consider switching to traversal build: `.\build.ps1 -UseTraversal`

### Parallel Build Race Conditions
**Symptom**: Random failures in parallel builds

**Solution**:
- Traversal SDK respects dependencies and avoids races
- For legacy build, reduce parallelism: `/m:1`
- Report race conditions so dependencies can be added to `dirs.proj`

### Clean Build Required
```powershell
# Nuclear option: delete all build artifacts
git clean -dfx Output/ Obj/

# Then rebuild native first
msbuild Build/FieldWorks.proj /t:allCppNoTest /p:Configuration=Debug /p:Platform=x64

# Then full traversal build
.\build.ps1 -UseTraversal
```

## Structured output
- Build output goes to: `Output/<Configuration>/` (e.g., `Output/Debug/`)
- Intermediate files: `Obj/<Project>/`
- Build logs: Use `-LogFile` parameter: `.\build.ps1 -UseTraversal -LogFile build.log`
- Scan for first error in failures; subsequent errors often cascade from the first

## Advanced Usage

### Direct MSBuild Invocation
```powershell
# Traversal build with MSBuild
msbuild dirs.proj /p:Configuration=Debug /p:Platform=x64 /m

# Legacy build with MSBuild  
msbuild Build/FieldWorks.proj /t:all /p:Configuration=Debug /p:Platform=x64
```

### Building Specific Project Groups
```powershell
# Native C++ only
msbuild Build/FieldWorks.proj /t:allCppNoTest

# Managed C# only (requires native artifacts exist)
msbuild Build/FieldWorks.proj /t:allCsharpNoTests

# Just tests
msbuild Build/FieldWorks.proj /t:test /p:action=test
```

### Dotnet CLI (Traversal Only)
```powershell
# Works with dirs.proj
dotnet build dirs.proj

# Restore packages
dotnet restore dirs.proj --packages packages/
```

## Don''t modify targets lightly
- `Build/FieldWorks.targets`: Auto-generated by GenerateFwTargets; regenerate with `/t:refreshTargets`
- `Build/mkall.targets`: Core build orchestration; changes affect all developers
- `Build/SetupInclude.targets`: Environment setup; touch only when absolutely needed
- `dirs.proj`: Traversal build order; verify changes don''t create circular dependencies

## References
- **CI/CD**: `.github/workflows/` for CI steps
- **Build Infrastructure**: `Build/` for targets/props and build infrastructure
- **Traversal Project**: `dirs.proj` for declarative build order
- **Shared Properties**: `Directory.Build.props` for all projects
- **Native Build**: `Build/mkall.targets` for C++ build orchestration
