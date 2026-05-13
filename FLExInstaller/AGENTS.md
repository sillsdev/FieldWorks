# FLExInstaller

Minimal installer guidance for agents.

## Defaults

- Use `.\build.ps1 -BuildInstaller` for installer builds.
- Validate prerequisites with `.\Build\Agent\Setup-InstallerBuild.ps1 -ValidateOnly`.
- Follow `.github/instructions/installer.instructions.md` for packaging and evidence rules.

## WiX 3 (PatchableInstaller) notes

- Heat exclusions: **`PatchableInstallerHeatExclude.xml`** is copied to **`PatchableInstaller/BaseInstallerBuild/heat-exclude.xml`** before Heat (see **`Build/Installer.legacy.targets`** `CopyFilesToInstall`).
- **`buildMsi.bat`** passes **`-fv`** to **`light.exe`** so **`MsiAssemblyName`** includes **fileVersion** (same intent as MSBuild **`SetMsiAssemblyNameFileVersion=true`**), which helps GAC servicing when **`AssemblyVersion`** is unchanged but the binary’s **file version** increases.
- Newtonsoft.Json and similar authored components live in **`CustomComponents.wxi`** (overlays **`PatchableInstaller/Common`**), with definitions guarded by **`<?ifdef MASTERBUILDDIR?>`** so patch/update authoring omits them when only **`UPDATEBUILDDIR`** is set. Add matching **`ComponentRef`** entries in **`FLExInstaller/CustomFeatures.wxi`** inside the **same** **`<?ifdef MASTERBUILDDIR?>...<?endif?>`** so patch builds do not emit dangling refs (**LGHT0094**). WiX 6 **`Framework.wxs`** uses the same pattern for **`Feature Complete`**. Do not use **`FeatureRef Id="Complete"`** from an include that appears before **`Framework.wxs`** defines `Complete` (Light **LGHT0095**).

## Constraints

- Keep existing WiX 3 and WiX 6 flows intact.
- Do not introduce installer signing or registry behavior changes without explicit requirements.
- Keep installer edits scoped to this folder and related build targets only.

