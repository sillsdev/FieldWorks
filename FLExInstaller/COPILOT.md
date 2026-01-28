# FLExInstaller (WiX 3 default)

This folder contains the WiX 3 installer inputs restored from release/9.3. The default installer build uses the legacy WiX 3 batch pipeline under PatchableInstaller. WiX 3 builds require the **Visual Studio WiX Toolset v3 extension** so `Wix.CA.targets` is available under the MSBuild extensions path. WiX 6 authoring lives under FLExInstaller/wix6/ (see FLExInstaller/wix6/COPILOT.md).

## Build (local)

```powershell
# WiX 3 default (Debug)
.\build.ps1 -BuildInstaller

# WiX 6 opt-in (Debug)
.\build.ps1 -BuildInstaller -InstallerToolset Wix6

# WiX 3 default (Release)
.\build.ps1 -BuildInstaller -Configuration Release

# WiX 6 opt-in (Release)
.\build.ps1 -BuildInstaller -Configuration Release -InstallerToolset Wix6
```

## Outputs

- WiX 3 default: `FLExInstaller/bin/x64/<Config>/` (MSI under `en-US/`)
- WiX 6 opt-in: `FLExInstaller/wix6/bin/x64/<Config>/` (MSI under `en-US/`)

## Customization via MSBuild properties (FR-008)

Pass overrides through `-MsBuildArgs`:

```powershell
# WiX 3 default with custom version segment
.\build.ps1 -BuildInstaller -MsBuildArgs '/p:BuildVersionSegment=1357'

# WiX 6 with custom name/version
.\build.ps1 -BuildInstaller -InstallerToolset Wix6 -MsBuildArgs '/p:ApplicationName="FieldWorks" /p:BuildVersionSegment=1357'
```

Common override properties:

- **WiX 3 default path**: `ApplicationName`, `SafeApplicationName`, `Manufacturer`, `SafeManufacturer`, `UpgradeCodeGuid`, `BuildVersionSegment`, `InstallersBaseDir`, `AppBuildDir`, `BinDirSuffix`, `DataDirSuffix`, `L10nDirSuffix`, `FontDirSuffix`
- **WiX 6 opt-in path**: `ApplicationName`, `SafeApplicationName`, `Manufacturer`, `SafeManufacturer`, `BundleId`, `UpgradeCode`, `BuildVersionSegment`, `AppBuildDir`, `BinDirSuffix`, `DataDirSuffix`, `L10nDirSuffix`, `FontDirSuffix`, `IcuVersion`

## Key files

- `*.wxi`: WiX 3 include files used by the legacy pipeline
- `PatchableInstaller/`: legacy WiX 3 batch pipeline inputs
- `wix6/`: WiX 6 SDK-style projects and shared authoring
