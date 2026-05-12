# FieldWorks Installer Build Map

## Core Files

- `build.ps1`: top-level wrapper. Installer builds are requested with `-BuildInstaller`; patch builds with `-BuildPatch`; WiX 6 is selected with `-InstallerToolset Wix6`.
- `Build/InstallerBuild.proj`: central installer MSBuild project. Defaults `InstallerToolset` to `Wix3`; imports `Installer.Wix3.targets` or `Installer.targets`.
- `Build/Installer.Wix3.targets`: WiX 3 fallback route. Verifies legacy inputs and delegates to legacy genericinstaller/PatchableInstaller scripts when that external checkout is present.
- `Build/Installer.targets`: WiX 6 route. Stages payloads and builds `FieldWorks.Bundle.wixproj` plus `FieldWorks.OfflineBundle.wixproj`.
- `Build/Installer.legacy.targets`: historical WiX 3 reference. Use for understanding old patch/build behavior, not as a WiX 6 implementation pattern.

## WiX 6 Project Files

- `FLExInstaller/wix6/FieldWorks.Installer.wixproj`: MSI project; builds the main package from staged inputs and custom action output.
- `FLExInstaller/wix6/FieldWorks.Bundle.wixproj`: online Burn bundle; downloads/stages prerequisite payloads and chains the MSI.
- `FLExInstaller/wix6/FieldWorks.OfflineBundle.wixproj`: offline Burn bundle; embeds repo-local prerequisite payloads from `FLExInstaller/wix6/libs`.
- `FLExInstaller/wix6/Shared/CustomActions/CustomActions/CustomActions.csproj`: WiX DTF custom action project.
- `FLExInstaller/wix6/Shared/ProcRunner/`: helper executable source used by installer custom behavior.

## Commands

Preferred local commands:

```powershell
./Build/Agent/Setup-InstallerBuild.ps1 -ValidateOnly
./build.ps1 -BuildInstaller
./build.ps1 -BuildInstaller -InstallerToolset Wix6
./build.ps1 -BuildInstaller -Configuration Release -InstallerToolset Wix6
```

Use direct MSBuild only when debugging build infrastructure:

```powershell
msbuild Build/InstallerBuild.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:InstallerToolset=Wix6
```

Patch caution: `build.ps1 -BuildPatch` routes to `/t:BuildPatch`. Before promising WiX 6 patch support, inspect whether the WiX 6 import actually defines the target and whether `.msp` authoring exists.

## Artifacts

WiX 3 fallback/current default:

- `FLExInstaller/bin/x64/<Configuration>/en-US/FieldWorks.msi`
- `FLExInstaller/bin/x64/<Configuration>/FieldWorksBundle.exe`

WiX 6 migration path:

- `FLExInstaller/wix6/bin/x64/<Configuration>/en-US/FieldWorks.msi`
- `FLExInstaller/wix6/bin/x64/<Configuration>/en-US/FieldWorks.wixpdb`
- `FLExInstaller/wix6/bin/x64/<Configuration>/FieldWorksBundle.exe`
- `FLExInstaller/wix6/bin/x64/<Configuration>/FieldWorksBundle.wixpdb`
- `FLExInstaller/wix6/bin/x64/<Configuration>/FieldWorksOfflineBundle.exe` when the offline project builds successfully.

## Hidden-Dependency Checks

- Staged payload root: `BuildDir/FieldWorks_InstallerInput_<Configuration>_<Platform>/`.
- Localization staging depends on `Output/<Configuration>/<locale>/` existing.
- Bundle theme files are staged by flat filename into the culture output folder.
- Offline prerequisites depend on files in `FLExInstaller/wix6/libs`.
- Heat/harvesting changes can break component identity and future patching.
- Custom action entry points referenced in `Framework.wxs` must match methods in `CustomAction.cs`.
