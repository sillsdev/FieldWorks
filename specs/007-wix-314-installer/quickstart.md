# Quickstart: Building FieldWorks Installers

**Feature**: 007-wix-314-installer | **Date**: December 2, 2025

## Quick Start

Use the installer setup script to validate and configure your environment:

```powershell
# Validate prerequisites only
.\Build\Agent\Setup-InstallerBuild.ps1 -ValidateOnly

# Full setup including patch build artifacts
.\Build\Agent\Setup-InstallerBuild.ps1 -SetupPatch
```

## Prerequisites

### Required Software

1. **Visual Studio 2022** with Desktop workloads
2. **WiX Toolset 3.14.x** - [Download from wixtoolset.org](https://wixtoolset.org/releases/)
   - After installation, verify: `where.exe candle.exe` shows WiX bin directory
3. **MSBuild** (included with VS 2022)
4. **.NET Framework 4.8.1 SDK** (included with VS 2022)

### One-Time Setup

```powershell
# Install WiX and configure environment
.\Setup-Developer-Machine.ps1

# Clone installer helper repositories
.\Setup-Developer-Machine.ps1 -InstallerDeps
```

### Repository Setup (Manual Alternative)

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
# Open VS Developer Command Prompt or run:
# & "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\Launch-VsDevShell.ps1"

# Restore packages
msbuild Build/Orchestrator.proj /t:RestorePackages /p:Configuration=Debug /p:Platform=x64

# Build base installer
msbuild Build/Orchestrator.proj /t:BuildBaseInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release /m /v:n
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

# Build patch installer
msbuild Build/Orchestrator.proj /t:BuildPatchInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release /m /v:n
```

### Output Location

After successful build:
- Patch file: `BuildDir/FieldWorks_*.msp`

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

**Fix**: Set registry key:
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

## CI Workflow Reference

The automated build process is defined in:
- Base installer: `.github/workflows/base-installer-cd.yml`
- Patch installer: `.github/workflows/patch-installer-cd.yml`

These workflows use WiX 3.14.x pre-installed on `windows-latest` GitHub runners.

## Version Information

- **WiX Toolset**: 3.14.x (minimum 3.14.0)
- **Target Framework**: .NET Framework 4.8.1
- **Supported Platforms**: Windows 10/11 (x64)
