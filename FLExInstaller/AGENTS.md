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
- **Patch error `PYRO0305: The File '<name>' was removed in the patch`:** a file present in the base/**Master** harvest is missing from the new **Update** harvest, and WiX 3 **`pyro.exe`** forbids removing files in a patch. Typical trigger: a code change stops emitting a file the base release still ships — e.g. reg-free COM manifests dropped by a "reduce COM usage" change (`ManagedLgIcuCollator.manifest`, `ManagedVwWindow.manifest`).
	- **Stopgap fix (patch against the existing base):** add the dropped file to the **`RemovedSinceLastBase`** item list in the **`RescuePatching`** target of **`Build/Installer.legacy.targets`** (runs via `BuildProduct`). It writes a zero-byte placeholder into the build output (**`$(dir-outputBase)`**) so the file appears in both harvests and `pyro` treats it as *changed*, not *removed*. Mirror the existing entries (`ManagedVwWindow.manifest`, `SimpleRootSite.manifest`) and add **only** the basenames actually dropped — usually just the `*.manifest`, not a still-shipping `.dll`.
	- **Permanent fix:** cut a new **Base** build so the file is absent from Master too, then delete the now-stale `RemovedSinceLastBase` entries (the target comment notes a base build should warn when these exist).
	- **Do not** add it to **`PatchableInstallerHeatExclude.xml`** — that list is for artifacts that must never be harvested (build-output dedup / test-only files), not for reconciling files removed since the base.

## Constraints

- Keep existing WiX 3 and WiX 6 flows intact.
- Do not introduce installer signing or registry behavior changes without explicit requirements.
- Keep installer edits scoped to this folder and related build targets only.

