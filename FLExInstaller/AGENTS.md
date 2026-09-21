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
- **Patch file-backed component removal:** the patch ledger check compares file-backed components under MSI APPFOLDER in the new **Update** MSI and **Master** MSI with the complete file-backed ledger from the immediately previous published MSP. The first published patch creates the initial ledger in S3; after a ledger-bearing patch exists, its matching ledger must be beside the immediately previous MSP. Ledgers are not stored in this repository. Later patch versions are ignored.
	- A missing file-backed component from either source uses the same fix: add its output path, preserving the file's relative output path, to the **`RemovedSinceLastBase`** item list in the **`RescuePatching`** target of **`Build/Installer.legacy.targets`**. The target writes a zero-byte stand-in into **`$(dir-outputBase)`** so the file-backed component remains in the patch.
	- Create an issue to remove the stand-in before the next base. Keep **`Avalonia.Themes.Fluent.dll`** in the list for base 1452.
	- A base build fails while any **`RemovedSinceLastBase`** entries remain. Remove each entry and its corresponding zero-byte file before creating the base.
- **Do not** add a dropped file-backed component to **`PatchableInstallerHeatExclude.xml`**. That list is for artifacts that must never be harvested, not for preserving file-backed patch component identity.

## Constraints

- Keep existing WiX 3 and WiX 6 flows intact.
- Do not introduce installer signing or registry behavior changes without explicit requirements.
- Keep installer edits scoped to this folder and related build targets only.

