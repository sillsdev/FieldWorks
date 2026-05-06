# Quickstart: Building the Installer

## Prerequisites

1.  **Visual Studio 2022** with ".NET Desktop Development" workload.
2.  **WiX Toolset v3** plus the **Visual Studio WiX Toolset v3 extension** (required for the WiX 3 default build path).
3.  **WiX Toolset v6** (for the WiX 6 path; installed via .NET tool or NuGet, handled by build).
4.  **Internet Connection** (for downloading NuGet packages and prerequisites).

## Building Locally

To build the installer, run the following command from the repository root:

```powershell
# Build the installer (build.ps1 forces installer builds to Release; current default = WiX 3 fallback)
./build.ps1 -BuildInstaller

# Build the WiX 6 installer (Release; installer builds are forced to Release)
./build.ps1 -BuildInstaller -InstallerToolset Wix6 -Configuration Release

# Or run MSBuild directly (WiX 3 fallback)
msbuild Build/InstallerBuild.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:InstallerToolset=Wix3

# Or run MSBuild directly (WiX 6, Debug)
msbuild Build/InstallerBuild.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:InstallerToolset=Wix6

# Build Release version (WiX 3 fallback)
./build.ps1 -BuildInstaller -Configuration Release

# Build Release version (WiX 6)
./build.ps1 -BuildInstaller -Configuration Release -InstallerToolset Wix6

# Or run MSBuild directly (WiX 3 fallback)
msbuild Build/InstallerBuild.proj /t:BuildInstaller /p:Configuration=Release /p:Platform=x64 /p:InstallerToolset=Wix3

# Or run MSBuild directly (WiX 6)
msbuild Build/InstallerBuild.proj /t:BuildInstaller /p:Configuration=Release /p:Platform=x64 /p:InstallerToolset=Wix6
```

The migration target is WiX 6. `Build/InstallerBuild.proj` currently defaults `InstallerToolset` to `Wix3`, so pass `-InstallerToolset Wix6` or `/p:InstallerToolset=Wix6` whenever validating the migration path.

## Artifacts

WiX 3 fallback artifacts are produced under `FLExInstaller/bin/<platform>/<configuration>/` (bundle outputs are culture-specific under `en-US/`). This path is retained only for transition safety.

WiX 6 build artifacts are produced under `FLExInstaller/wix6/bin/<platform>/<configuration>/` (MSI outputs are culture-specific under `en-US/`).

- `FieldWorks.msi`: The main MSI package.
- `FieldWorksBundle.exe`: The online bootstrapper bundle.
- `FieldWorksOfflineBundle.exe`: The offline bootstrapper bundle with local prerequisite payloads embedded.
- `*.wixpdb`: Debug symbols for MSI/bundle.

## Customization via MSBuild properties (FR-008)

Override installer identity/versioning by passing MSBuild properties. Common properties:

- **WiX 3 fallback path** (`FLExInstaller/` plus legacy genericinstaller inputs when needed):
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

### Artifact checklist (x64/Release, WiX 6)

- [ ] `FLExInstaller/wix6/bin/x64/Release/en-US/FieldWorks.msi`
- [ ] `FLExInstaller/wix6/bin/x64/Release/en-US/FieldWorks.wixpdb`
- [ ] `FLExInstaller/wix6/bin/x64/Release/FieldWorksBundle.exe`
- [ ] `FLExInstaller/wix6/bin/x64/Release/FieldWorksBundle.wixpdb`
- [ ] `FLExInstaller/wix6/bin/x64/Release/FieldWorksOfflineBundle.exe`
- [ ] `FLExInstaller/wix6/bin/x64/Release/FieldWorksOfflineBundle.wixpdb`

### Build evidence audit (WiX 6)

After a WiX 6 installer build, run the audit helper to verify artifact presence, hashes, offline prerequisite payload availability, and build-log isolation from legacy WiX 3 tooling:

```powershell
./Build/Agent/Test-Wix6InstallerBuildEvidence.ps1 -Configuration Release -Platform x64 -BuildLogPath ./wix6-installer-build.log

# CI additionally proves the WiX 6 lane does not have WiX 3 command-line tools on PATH:
./Build/Agent/Test-Wix6InstallerBuildEvidence.ps1 -Configuration Release -Platform x64 -BuildLogPath ./wix6-installer-build.log -RequireNoWix3ToolsOnPath
```

The main CI workflow now has a `Build WiX 6 installer artifacts` job that runs the WiX 6 installer build with `FastBundleBuild=0`, audits the build log, uploads MSI/online/offline bundle artifacts, and publishes `Output/InstallerEvidence/**` for review. Legacy release workflows that still use `PatchableInstaller/` are labeled as WiX 3 transitional lanes.

## Troubleshooting

- **Missing Prerequisites**: Ensure you have internet access. The build attempts to download required redistributables.
- **Signing Errors**: If `SIGN_INSTALLER` is set but no certificate is provided, the build will fail. Unset the variable for local testing.
- **WiX Errors**: Check the build log for specific WiX error codes (e.g., `WIX0001`).
