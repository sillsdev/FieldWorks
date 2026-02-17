# Research: WiX v6 Migration

**Status**: In Progress
**Date**: 2025-12-11

## 1. GenericInstaller Migration

**Goal**: Remove dependency on `genericinstaller` submodule and `PatchableInstaller` folder structure.

**Findings**:
- `PatchableInstaller` contains `BaseInstallerBuild` (UI, common wxs) and `CustomActions`.
- `FLExInstaller` references these files.
- **Decision**: Move `PatchableInstaller/BaseInstallerBuild` and `PatchableInstaller/CustomActions` into `FLExInstaller/Shared` or similar.
- **Task**: Audit `FLExInstaller` .wxs files to find all references to `PatchableInstaller`.

## 2. WiX v6 Conversion

**Goal**: Convert WiX 3.11 source to WiX v6.

**Findings**:
- WiX v6 uses SDK-style projects (`<Project Sdk="WixToolset.Sdk/5.0.0">`).
- `wix convert` tool can automate much of the .wxs conversion.
- **Decision**: Create new `.wixproj` files for the installer components.
- **Task**: Run `wix convert` on a sample file to verify changes.

## 3. UI Porting

**Goal**: Maintain "Dual Directory" selection and custom feature tree.

**Findings**:
- `GIInstallDirDlg.wxs` implements the dual directory logic.
- `GICustomizeDlg.wxs` implements the restricted feature tree.
- These are standard WiX UI dialogs with custom XML.
- **Decision**: Copy these .wxs files into the new project and reference them. Ensure `WixToolset.UI.wixext` is referenced.
- **Task**: Verify `WixUI_DialogFlow.wxs` logic is compatible with WiX v6 UI extension points.

## 4. MSBuild Integration & Harvesting

**Goal**: Replace batch files with MSBuild targets.

**Findings**:
- Current build uses `buildBaseInstaller.bat` etc.
- `buildMsi.bat` builds `../ProcRunner/ProcRunner.sln` (C# helper).
- `buildMsi.bat` uses `heat.exe` to harvest `MASTERBUILDDIR` and `MASTERDATADIR`.
- **Decision**:
    - Create `Build/Installer.targets` to define the build process.
    - Migrate `ProcRunner` to `FLExInstaller/Shared/ProcRunner`.
    - Use `<HarvestDirectory>` in `.wixproj` to replace `heat.exe` calls.
- **Task**: Define targets for `Restore`, `Build`, `Sign`, `Publish`.
- **Prerequisites**: Use `DownloadFile` task or similar in MSBuild to fetch .NET/C++ redistributables if not present (FR-011).

## 5. Code Signing

**Goal**: Sign artifacts using env vars.

**Findings**:
- `buildExe.bat` uses `insignia` to sign the bundle engine.
- WiX v6 `WixToolset.Bal.wixext` handles engine signing automatically if `SignOutput` is configured.
- **Decision**: Use `WixToolset.Sdk` signing integration.
- **Task**: Configure `SignOutput` to use certificate from environment variables.

## 6. Unknowns & Risks

- **Risk**: `PatchableInstaller` might have hidden dependencies on other repos (unlikely given the submodule nature, but possible).
- **Risk**: Custom Actions might depend on old .NET versions. (Need to ensure they target .NET 4.8 or .NET Standard).
- **Unknown**: Exact list of prerequisites to download. (Need to check `Redistributables.wxi`).

## 7. Alternatives Considered

- **Keep Submodule**: Rejected (FR-003).
- **Standard UI**: Rejected (FR-010) - need dual directory support.
- **PowerShell Build Script**: Rejected (FR-002) - prefer MSBuild targets for better integration.
