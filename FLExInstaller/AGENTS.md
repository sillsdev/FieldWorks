# FLExInstaller

Minimal installer guidance for agents.

## Defaults

- Use `.\build.ps1 -BuildInstaller` for installer builds.
- Validate prerequisites with `.\Build\Agent\Setup-InstallerBuild.ps1 -ValidateOnly`.
- Follow `.github/instructions/installer.instructions.md` for packaging and evidence rules.

## WiX 3 (PatchableInstaller) notes

- Heat exclusions: **`PatchableInstallerHeatExclude.xml`** is copied to **`PatchableInstaller/BaseInstallerBuild/heat-exclude.xml`** before Heat (see **`Build/Installer.legacy.targets`** `CopyFilesToInstall`).
- **`buildMsi.bat`** passes **`-fv`** to **`light.exe`** so **`MsiAssemblyName`** includes **fileVersion** (same intent as MSBuild **`SetMsiAssemblyNameFileVersion=true`**), which helps GAC servicing when **`AssemblyVersion`** is unchanged but the binary’s **file version** increases.
- Newtonsoft.Json and similar authored components live in **`CustomComponents.wxi`** (overlays **`PatchableInstaller/Common`**), not in the generic PatchableInstaller tree. Wire them into **`Feature Complete`** with **`ComponentRef`** entries in **`PatchableInstaller/BaseInstallerBuild/Framework.wxs`** (FieldWorks Newtonsoft) and/or **`PatchableInstaller/Common/CustomFeatures.wxi`**; do not use **`FeatureRef Id="Complete"`** from an include that appears before **`Framework.wxs`** defines `Complete` (Light **LGHT0095**). Prefer **`Framework.wxs`** for wiring that must survive **`git checkout`** on **`PatchableInstaller/Common`** (LGHT0139 for GAC assemblies if refs are missing).

## Constraints

- Keep existing WiX 3 and WiX 6 flows intact.
- Do not introduce installer signing or registry behavior changes without explicit requirements.
- Keep installer edits scoped to this folder and related build targets only.

