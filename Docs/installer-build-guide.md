# Building FieldWorks Installers

This guide explains how to build FieldWorks installers locally and describes the CI workflow process.

> **Note:** FieldWorks is **x64-only**. The x86 (32-bit) platform is no longer supported.

## Quick Start

Use the installer setup script to validate your environment:

```powershell
# Validate prerequisites (no changes)
.\Build\Agent\Setup-InstallerBuild.ps1 -ValidateOnly

# Full setup including patch build artifacts
.\Build\Agent\Setup-InstallerBuild.ps1 -SetupPatch
```

## Prerequisites

### Required Software

1. **Visual Studio 2022** with Desktop workloads (C++ and .NET)
2. **WiX Toolset v6** via `WixToolset.Sdk` (restored via NuGet as part of the build)
3. **MSBuild** (included with VS 2022)
4. **.NET Framework 4.8.1 SDK** (included with VS 2022)

### One-Time Setup

Run the developer machine setup script to install WiX and configure your environment:

```powershell
# Install WiX and configure PATH/environment variables
.\Setup-Developer-Machine.ps1

# Also clone installer helper repositories
.\Setup-Developer-Machine.ps1 -InstallerDeps
```

### Repository Setup

If not using `Setup-Developer-Machine.ps1 -InstallerDeps`, clone manually:

```powershell
# Clone main repository
git clone https://github.com/sillsdev/fieldworks.git
cd fieldworks

# Clone required helper repositories
git clone https://github.com/sillsdev/FwHelps.git DistFiles/Helps
git clone https://github.com/sillsdev/FwLocalizations.git Localizations
git clone https://github.com/sillsdev/liblcm.git Localizations/LCM
```

## Building a Base Installer

### Full Build (Recommended)

```powershell
# Open VS Developer Command Prompt (x64) or run:
# & "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\Launch-VsDevShell.ps1" -Arch amd64

# Restore packages
msbuild Build/Orchestrator.proj /t:RestorePackages /p:Configuration=Debug /p:Platform=x64

# Build base installer (x64 only)
msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Release /p:Platform=x64 /p:config=release /m /v:n
```

### Output Location

After successful build, artifacts are produced by the WiX SDK projects under `FLExInstaller/bin/<platform>/<configuration>/` (bundle outputs are culture-specific under `en-US/`).

## Building a Patch Installer

### Prerequisites

You need base build artifacts from a prior base build:
- `BuildDir.zip` - Extract to `BuildDir/`
- `ProcRunner.zip` - Extract to `FLExInstaller/Shared/ProcRunner/ProcRunner/bin/Release/net48/`

These can be downloaded from GitHub Releases (e.g., `build-1188`).

### Build Command

```powershell
# Restore packages
msbuild Build/Orchestrator.proj /t:RestorePackages /p:Configuration=Debug /p:Platform=x64

# Build patch installer (x64 only)
msbuild Build/Orchestrator.proj /t:BuildPatchInstaller /p:Configuration=Release /p:Platform=x64 /p:config=release /m /v:n
```

### Output Location

After successful build:
- Patch file: `BuildDir/FieldWorks_*.msp`

## CI Workflow Reference

The automated build process is defined in two GitHub Actions workflows:

### Base Installer Workflow (`.github/workflows/base-installer-cd.yml`)

**Triggers:**
- Scheduled: Every Monday at 02:30 UTC
- Manual: `workflow_dispatch` with optional parameters

**Key Steps:**
1. Checkout main repo and helper repositories (FwHelps, FwLocalizations, liblcm)
2. Install .NET 4.8.1 targeting pack
3. Setup MSBuild environment
4. Build base installer using `msbuild Build/Orchestrator.proj /t:BuildBaseInstaller`
5. Extract burn engines using `insignia -ib` for code signing
6. Sign engines and bundles using Azure Trusted Signing
7. Reattach engines using `insignia -ab`
8. Upload to S3 (if `make_release: true`)
9. Create GitHub Release with `BuildDir.zip` and `ProcRunner.zip` artifacts

**Inputs:**
- `fw_ref`: Branch/tag/SHA for main repository
- `helps_ref`, `installer_ref`, `localizations_ref`, `lcm_ref`: Refs for helper repos
- `make_release`: Whether to create a release (default: false)

### Patch Installer Workflow (`.github/workflows/patch-installer-cd.yml`)

**Triggers:**
- Push to `release/9.3` branch
- Scheduled: Every Monday at 03:30 UTC
- Manual: `workflow_dispatch` with parameters

**Key Steps:**
1. Checkout repos (same as base installer)
2. Download base build artifacts from GitHub Release
3. Set registry key for WiX temp file handling
4. Build patch using `msbuild Build/Orchestrator.proj /t:BuildPatchInstaller`
5. Sign patch using Azure Trusted Signing
6. Upload to S3 (if `make_release: true`)

**Inputs:**
- `base_release`: GitHub release tag for base build artifacts (e.g., `build-1188`)
- `base_build_number`: Numeric base build number
- `make_release`: Whether to upload to S3 (default: true)

### WiX Version

Workflows should use **WiX Toolset v6** via `WixToolset.Sdk` restored from NuGet.

## Troubleshooting

### WiX tool resolution failures

**Cause**: NuGet restore/build tools not fully restored, or missing VS build prerequisites.

**Fix**:
1. Ensure `msbuild Build/Orchestrator.proj /t:RestorePackages /p:Configuration=Debug /p:Platform=x64` succeeds.
2. Re-run `\Build\Agent\Setup-InstallerBuild.ps1 -ValidateOnly` and resolve any reported issues.
2. Add WiX bin directory to PATH: `C:\Program Files (x86)\WiX Toolset v3.14\bin`

### "Build artifacts missing"

**Cause**: Prerequisites not built before installer.

**Fix**: Run full traversal build first:
```powershell
.\build.ps1
```

### "Switch.System.DisableTempFileCollectionDirectoryFeature" error

**Cause**: Windows/.NET feature conflict with WiX temp file handling.

**Fix**: Set registry key (requires admin):
```powershell
$paths = @(
    "HKLM:\SOFTWARE\Microsoft\.NETFramework\AppContext",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\.NETFramework\AppContext"
)
foreach ($path in $paths) {
    if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
    New-ItemProperty -Path $path -Name "Switch.System.DisableTempFileCollectionDirectoryFeature" -Value "true" -Type String -Force
}
```

### Patch fails to apply to base installation

**Cause**: Version mismatch or incompatible component GUIDs.

**Fix**:
1. Ensure patch build number is higher than base build number
2. Verify you're applying patch to the correct base version
3. Check that component GUIDs haven't changed between versions

### "module machine type 'x86' conflicts with target machine type 'x64'"

**Cause**: Stale object files from a previous x86 build.

**Fix**: Clean and rebuild:
```powershell
Remove-Item -Recurse -Force Obj/ -ErrorAction SilentlyContinue
.\build.ps1
```

## Architecture Requirements

FieldWorks requires **64-bit Windows** (x64):
- All native C++ code is compiled for x64
- All managed assemblies target AnyCPU but run in 64-bit mode
- The installer only produces x64 packages

**Note:** x86 (32-bit) is no longer supported as of the 9.3 release series.

## Version Information

- **WiX Toolset**: 3.14.x (minimum 3.14.0)
- **Target Framework**: .NET Framework 4.8.1
- **Supported Platforms**: Windows 10/11 (x64 only)
- **Supported Architectures**: x64 only (x86 deprecated)

## Related Documentation

- [WiX Toolset Documentation](https://wixtoolset.org/documentation/)
- [Core Developer Setup](core-developer-setup.md)
- [Visual Studio Setup](visual-studio-setup.md)
