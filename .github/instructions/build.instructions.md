---
applyTo: "**/*"
name: "build.instructions"
description: "FieldWorks build guidelines and inner-loop tips"
---
# Build guidelines and inner-loop tips

## Purpose & Scope
This file describes the build system and inner-loop tips for developers working on FieldWorks. Use it for top-level build instructions, not for project-specific guidance.

## Quick Start

FieldWorks uses the **MSBuild Traversal SDK** for declarative build ordering. All builds use `FieldWorks.proj`.

### Windows (PowerShell)
```powershell
# Full build with automatic dependency ordering
.\build.ps1

# Specific configuration
.\build.ps1 -Configuration Release -Platform x64

# With parallel builds and detailed logging
.\build.ps1 -MsBuildArgs @('/m', '/v:detailed')
```

### Linux/macOS (Bash)
```bash
# Full build
./build.sh

# Release build
./build.sh -c Release

# With parallel builds
./build.sh -- /m
```

**Benefits:**
- Declarative dependency ordering (110+ projects organized into 21 phases)
- Automatic parallel builds where safe
- Better incremental build performance
- Works with `dotnet build FieldWorks.proj`
- Clear error messages when prerequisites missing

## Build Architecture

### Traversal Build Phases
The `FieldWorks.proj` file defines a declarative build order:

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
Run: msbuild Build\Src\NativeBuild\NativeBuild.csproj
```

## Developer environment setup

- On Windows: Use `.\build.ps1` (automatically sets up VS Developer Environment) or open a Developer Command Prompt for Visual Studio before running manual `msbuild` commands.
- On Linux/macOS: Use `./build.sh` and ensure `msbuild`, `dotnet`, and native build tools are installed.
- Environment variables (`fwrt`, `Platform`, etc.) are set by `SetupInclude.targets` during build.

## Deterministic requirements

### Inner loop (Developer Workflow)
- **First build**: `.\build.ps1` or `./build.sh` (traversal handles automatic ordering)
- **Incremental**: Only changed projects rebuild (MSBuild tracks `Inputs`/`Outputs`)
- **Avoid full clean** unless:
  - Native artifacts corrupted (delete `Output/`, then rebuild native first)
  - Generated code out of sync (delete `Src/Common/ViewsInterfaces/Views.cs`)

### Choose the right path
- **Full system build**: `.\build.ps1` or `./build.sh` (uses FieldWorks.proj traversal)
- **Direct MSBuild**: `msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /m`
- **Dotnet CLI**: `dotnet build FieldWorks.proj` (requires .NET SDK)
- **Single project**: `msbuild Src/<Path>/<Project>.csproj` (for quick iterations)
- **Native only**: `msbuild Build\Src\NativeBuild\NativeBuild.csproj` (Phase 2 of traversal)
- **Installer**: See `Build/Installer.targets` for installer build targets (requires WiX Toolset)

### Configuration Options
```powershell
# Debug build (default, includes PDB symbols)
.\build.ps1 -Configuration Debug

# Release build (optimized, smaller binaries)
.\build.ps1 -Configuration Release

# Platform selection (x64 is default and recommended)
.\build.ps1 -Platform x64
```

## Troubleshooting

### Native Artifacts Missing
**Symptom**: ViewsInterfaces fails with "Cannot generate Views.cs"

**Solution**:
```powershell
# Build native components first
msbuild Build\Src\NativeBuild\NativeBuild.csproj /p:Configuration=Debug /p:Platform=x64

# Then continue with full build
.\build.ps1
```

### Build Order Issues
**Symptom**: Project X fails because it can't find assembly from project Y

**Solution**: The traversal build handles this automatically through `FieldWorks.proj`:
- Check that the dependency is listed in an earlier phase than the dependent
- Verify both projects are included in `FieldWorks.proj`
- If you find a missing dependency, update `FieldWorks.proj` phase ordering

### Parallel Build Race Conditions
**Symptom**: Random failures in parallel builds

**Solution**:
- Traversal SDK respects dependencies and avoids races
- If you encounter race conditions, reduce parallelism: `.\build.ps1 -MsBuildArgs @('/m:1')`
- Report race conditions so dependencies can be added to `FieldWorks.proj`

### Clean Build Required
```powershell
# Nuclear option: delete all build artifacts
git clean -dfx Output/ Obj/

# Then rebuild native first
msbuild Build\Src\NativeBuild\NativeBuild.csproj /p:Configuration=Debug /p:Platform=x64

# Then full build
.\build.ps1
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
msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /m

# With tests
msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /p:action=test /m
```

### Building Specific Project Groups
```powershell
# Native C++ only (Phase 2 of traversal)
msbuild Build\Src\NativeBuild\NativeBuild.csproj

# Specific phase from FieldWorks.proj (not typically needed)
# The traversal build handles ordering automatically
```

### Dotnet CLI (Traversal Only)
```powershell
# Works with FieldWorks.proj
dotnet build FieldWorks.proj

# Restore packages
dotnet restore FieldWorks.proj --packages packages/
```

## Don't modify build files lightly
- **`FieldWorks.proj`**: Traversal build order; verify changes don't create circular dependencies
- **`Build/mkall.targets`**: Native C++ build orchestration; changes affect all developers
- **`Build/SetupInclude.targets`**: Environment setup; touch only when absolutely needed
- **`Directory.Build.props`**: Shared properties for all projects; changes affect everyone

## Running Tests

### With MSBuild (current method)
```powershell
# Run all tests
msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /p:action=test

# Run specific test target
msbuild Build\FieldWorks.targets /t:CacheLightTests /p:Configuration=Debug /p:Platform=x64 /p:action=test
```

Test results: `Output/Debug/<ProjectName>.dll-nunit-output.xml`

### With dotnet test (under development)
```powershell
# Future simplified approach
dotnet test FieldWorks.sln --configuration Debug
```

See `.github/instructions/testing.instructions.md` for detailed test execution guidance.

## References
- **CI/CD**: `.github/workflows/` for CI steps
- **Build Infrastructure**: `Build/` for targets/props and build infrastructure
- **Traversal Project**: `FieldWorks.proj` for declarative build order
- **Shared Properties**: `Directory.Build.props` for all projects
- **Native Build**: `Build/mkall.targets` for C++ build orchestration
- **Testing**: `.github/instructions/testing.instructions.md` for test execution
