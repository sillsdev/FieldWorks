# Building FieldWorks Installers

This guide explains how to build FieldWorks installers locally and describes the CI workflow process.

> **Note:** FieldWorks is **x64-only**. The x86 (32-bit) platform is no longer supported.

## Prerequisites

### Required Software

1. **Visual Studio 2022** with Desktop workloads (C++ and .NET)
2. **WiX Toolset 3.14.x** - [Download from wixtoolset.org](https://wixtoolset.org/releases/)
   - After installation, verify: `where.exe candle.exe` shows WiX bin directory
3. **MSBuild** (included with VS 2022)
4. **.NET Framework 4.8.1 SDK** (included with VS 2022)

### Repository Setup

```powershell
# Clone main repository
git clone https://github.com/sillsdev/fieldworks.git
cd fieldworks

# Clone required helper repositories
git clone https://github.com/sillsdev/FwHelps.git DistFiles/Helps
git clone https://github.com/sillsdev/genericinstaller.git PatchableInstaller
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
msbuild Build/Orchestrator.proj /t:BuildBaseInstaller /p:Configuration=Release /p:Platform=x64 /p:config=release /m /v:n
```

### Output Location

After successful build:
- Offline installer: `BuildDir/FieldWorks_*_Offline_x64.exe`
- Online installer: `BuildDir/FieldWorks_*_Online_x64.exe`

## Building a Patch Installer

### Prerequisites

You need base build artifacts from a prior base build:
- `BuildDir.zip` - Extract to `BuildDir/`
- `ProcRunner.zip` - Extract to `PatchableInstaller/ProcRunner/ProcRunner/bin/Release/net48/`

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
1. Checkout main repo and helper repositories (FwHelps, genericinstaller, FwLocalizations, liblcm)
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

Both workflows use **WiX 3.14.x** pre-installed on `windows-latest` GitHub runners.

## Troubleshooting

### "candle.exe not found"

**Cause**: WiX Toolset not installed or not in PATH.

**Fix**:
1. Install WiX 3.14.x from [wixtoolset.org](https://wixtoolset.org/releases/)
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
