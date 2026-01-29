# Quickstart: Building the Installer

## Prerequisites

1.  **Visual Studio 2022** with ".NET Desktop Development" workload.
2.  **WiX Toolset v3** plus the **Visual Studio WiX Toolset v3 extension** (required for the WiX 3 default build path).
3.  **WiX Toolset v6** (for the WiX 6 path; installed via .NET tool or NuGet, handled by build).
4.  **Internet Connection** (for downloading NuGet packages and prerequisites).

## Building Locally

To build the installer, run the following command from the repository root:

```powershell
# Build the installer (Debug configuration, default = WiX 3)
./build.ps1 -BuildInstaller

# Build the WiX 6 installer (Debug)
./build.ps1 -BuildInstaller -InstallerToolset Wix6

# Or run MSBuild directly (WiX 3 default)
msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release

# Or run MSBuild directly (WiX 6)
msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release /p:InstallerToolset=Wix6

# Build Release version (WiX 3 default)
./build.ps1 -BuildInstaller -Configuration Release

# Build Release version (WiX 6)
./build.ps1 -BuildInstaller -Configuration Release -InstallerToolset Wix6

# Or run MSBuild directly (WiX 3 default)
msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Release /p:Platform=x64 /p:config=release

# Or run MSBuild directly (WiX 6)
msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Release /p:Platform=x64 /p:config=release /p:InstallerToolset=Wix6
```

## Artifacts

WiX 3 build artifacts are produced under `FLExInstaller/bin/<platform>/<configuration>/` (bundle outputs are culture-specific under `en-US/`).

WiX 6 build artifacts are produced under `FLExInstaller/wix6/bin/<platform>/<configuration>/` (bundle outputs are culture-specific under `en-US/`).

- `FieldWorks.msi`: The main MSI package.
- `FieldWorksBundle.exe`: The bootstrapper bundle (includes prerequisites).
- `*.wixpdb`: Debug symbols for MSI/bundle.

## Customization via MSBuild properties (FR-008)

Override installer identity/versioning by passing MSBuild properties. Common properties:

- **WiX 3 default path** (`FLExInstaller/` + `PatchableInstaller/`):
	- `ApplicationName`, `SafeApplicationName`, `Manufacturer`, `SafeManufacturer`
	- `UpgradeCodeGuid`, `BuildVersionSegment`
	- `InstallersBaseDir`, `AppBuildDir`, `BinDirSuffix`, `DataDirSuffix`, `L10nDirSuffix`, `FontDirSuffix`
- **WiX 6 opt-in path** (`FLExInstaller/wix6/`):
	- `ApplicationName`, `SafeApplicationName`, `Manufacturer`, `SafeManufacturer`
	- `BundleId`, `UpgradeCode`, `BuildVersionSegment`
	- `AppBuildDir`, `BinDirSuffix`, `DataDirSuffix`, `L10nDirSuffix`, `FontDirSuffix`, `IcuVersion`

Examples:

```powershell
# WiX 3 default with custom version segment
./build.ps1 -BuildInstaller -MsBuildArgs '/p:BuildVersionSegment=1357'

# WiX 6 with custom name/version
./build.ps1 -BuildInstaller -InstallerToolset Wix6 -MsBuildArgs '/p:ApplicationName="FieldWorks" /p:BuildVersionSegment=1357'
```

### Artifact checklist (x64/Debug, WiX 6)

- [ ] `FLExInstaller/wix6/bin/x64/Debug/en-US/FieldWorks.msi`
- [ ] `FLExInstaller/wix6/bin/x64/Debug/en-US/FieldWorks.wixpdb`
- [ ] `FLExInstaller/wix6/bin/x64/Debug/FieldWorksBundle.exe`
- [ ] `FLExInstaller/wix6/bin/x64/Debug/FieldWorksBundle.wixpdb`

## Troubleshooting

- **Missing Prerequisites**: Ensure you have internet access. The build attempts to download required redistributables.
- **Signing Errors**: If `SIGN_INSTALLER` is set but no certificate is provided, the build will fail. Unset the variable for local testing.
- **WiX Errors**: Check the build log for specific WiX error codes (e.g., `WIX0001`).
