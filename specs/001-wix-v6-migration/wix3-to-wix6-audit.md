# WiX 3.x → WiX 6 Migration Audit (FieldWorks)

**Branch audited:** `001-wix-v6-migration`

**How to verify this audit**

- Parity procedure: [parity-check.md](parity-check.md)
- Evidence tracking: [verification-matrix.md](verification-matrix.md)
- Manual VM runs: [golden-install-checklist.md](golden-install-checklist.md)

## Executive summary

This worktree contains a WiX 6 (SDK-style) MSI + Burn bundle implementation with the critical installer UX pieces (dual-directory UI + feature tree) and the key registry/shortcut behaviors implemented in WiX authoring.

However, this is not yet a fully “WiX 6-native” build pipeline:

- The MSI project still **uses WiX 3 `heat.exe`** to harvest binaries/data, then runs **WiX 6 `wix.exe`** to compile the converted harvest output.
- The legacy WiX 3 batch scripts are still present (and appear to describe the historical pipeline), but the new “source of truth” build path is via MSBuild targets + the SDK-style `.wixproj` files.
- Several spec tasks remain incomplete (notably: end-to-end local artifact verification, CI integration, online/offline end-user validation).

## Scope of audit

Per request, this audit focuses on:

- **UX parity** (dialogs and dialog flow, including the dual-directory selection)
- **File payload parity** (what gets staged and harvested into the MSI)
- **Registry parity** (keys/values written, detection searches)
- **Installer build system parity** (how WiX 3 produced artifacts vs how WiX 6 produces them now)

## Baseline availability / limitations

You requested using commit `6a2d976e` as the WiX 3.11 baseline. In the FieldWorks repo at that commit (and also on `release/9.3`), the installer’s **product-specific includes** existed (`FLExInstaller/*.wxi`), but the **base WiX authoring** (`*.wxs`, dialogs, bundle theme, etc.) did not live in-tree.

If you have the `genericinstaller/` repo available externally, you *can* point to the WiX 3.x baseline authoring directly:

- **WiX 3.x template authoring:** `genericinstaller/BaseInstallerBuild/*` and `genericinstaller/Common/*`
- **FieldWorks-specific WiX 3.x customization:** `FLExInstaller/*.wxi` as of commit `6a2d976e` (these are the same functional “custom include” files that were historically layered on top of the generic template)

This audit therefore treats “WiX 3.x” as **(FieldWorks FLExInstaller includes at `6a2d976e`)** and (optionally) **(genericinstaller template)**, and compares that against the current WiX 6 implementation in this worktree.

> Note: this worktree does not currently include a `genericinstaller/` folder; without it, comparisons to the generic template are limited to the in-repo `FLExInstaller/*.wxi` baseline.

## Where things live now (WiX 6)

### Build orchestration

- MSBuild target entry points: `Build/Installer.targets`
- MSI project: `FLExInstaller/wix6/FieldWorks.Installer.wixproj`
- Bundle project: `FLExInstaller/wix6/FieldWorks.Bundle.wixproj`

### WiX authoring (MSI + bundle)

- MSI package authoring: `FLExInstaller/wix6/Shared/Base/Framework.wxs`
- UI flow: `FLExInstaller/wix6/Shared/Base/WixUI_DialogFlow.wxs`
- Custom dialogs: `FLExInstaller/wix6/Shared/Base/GIInstallDirDlg.wxs`, `GICustomizeDlg.wxs`, `GISetupTypeDlg.wxs`, `GIWelcomeDlg.wxs`, `GIProgressDlg.wxs`
- Bundle authoring: `FLExInstaller/wix6/Shared/Base/Bundle.wxs`
- Bundle theme: `FLExInstaller/wix6/Shared/Base/BundleTheme.xml`, `BundleTheme.wxl`
- MSI localization: `FLExInstaller/wix6/Shared/Base/WixUI_en-us.wxl`

**Includes used by the WiX 6 build (referenced by `Shared/Base/*.wxs`):**

- `FLExInstaller/wix6/Shared/Common/CustomActionSteps.wxi`
- `FLExInstaller/wix6/Shared/Common/CustomComponents.wxi`
- `FLExInstaller/wix6/Shared/Common/CustomFeatures.wxi`
- `FLExInstaller/wix6/Shared/Common/Overrides.wxi`
- `FLExInstaller/wix6/Shared/Common/Redistributables.wxi`

> Note: the repo also contains `FLExInstaller/*.wxi` at the top level; these are the historical FieldWorks include files and are useful as the WiX 3.x baseline reference, but the current WiX 6 authoring under `Shared/Base/` includes the `Shared/Common/` copies.

### Custom actions and helper binaries

- Custom Actions (CA): `FLExInstaller/wix6/Shared/CustomActions/CustomActions/CustomActions.csproj`
- ProcRunner: `FLExInstaller/wix6/Shared/ProcRunner/ProcRunner/ProcRunner.csproj`

## Component-by-component parity map

> Legend for **Parity**:
> - **PASS**: present and appears equivalent
> - **PARTIAL**: present but with notable divergence / risk
> - **GAP**: missing or not wired into the WiX 6 build

| Component | WiX 3.x (legacy) implementation | WiX 6 (current) implementation | Parity | Notes / evidence |
|---|---|---|---|---|
| MSI build pipeline | `genericinstaller/BaseInstallerBuild/buildMsi.bat` (heat → candle/light → sign) | `FLExInstaller/wix6/FieldWorks.Installer.wixproj` (heat → `wix.exe convert` → SDK compile) | PARTIAL | Still depends on `heat.exe` (WiX 3). The “WiX 6 compile” is via WixToolset.Sdk. |
| Bundle build pipeline | `genericinstaller/BaseInstallerBuild/buildExe.bat` (candle/light, online+offline, insignia signing) | `FLExInstaller/wix6/FieldWorks.Bundle.wixproj` (SDK bundle build) + `StageBundlePayloads` downloads | PARTIAL | The WiX 6 project only compiles `Shared/Base/Bundle.wxs`; offline bundle output is not clearly produced. |
| Artifact orchestration | (historically batch-driven) `buildBaseInstaller.bat` chains `buildMsi.bat` + `buildExe.bat` | `Build/Installer.targets` provides `BuildInstaller` target | PASS | `BuildInstaller` depends on staging + prerequisites + ProcRunner build. |
| Input staging for harvest | (legacy assumed in external scripts; not in-repo baseline) | `Build/Installer.targets` target `StageInstallerInputs` | PASS | Copies Output + DistFiles + fonts + ICU + localizations into `BuildDir/FieldWorks_InstallerInput_*`. |
| Harvesting app files | `buildMsi.bat` uses `heat.exe dir %MASTERBUILDDIR% ... -cg HarvestedAppFiles ... -dr APPFOLDER` | `FieldWorks.Installer.wixproj` target `HarvestAppAndData` uses `heat.exe dir $(_MasterBuildDirFull) ... -cg HarvestedAppFiles ... -dr APPFOLDER` | PASS | Same component group ID + same directory ref pattern. |
| Harvesting data files | `buildMsi.bat` uses `heat.exe dir %MASTERDATADIR% ... -cg HarvestedDataFiles ... -dr HARVESTDATAFOLDER` | `FieldWorks.Installer.wixproj` target `HarvestAppAndData` uses `heat.exe dir $(_MasterDataDirFull) ... -cg HarvestedDataFiles ... -dr HARVESTDATAFOLDER` | PASS | Same component group ID + same directory ref pattern. |
| MSI major upgrade | (legacy expected) | `Framework.wxs` `<MajorUpgrade ... AllowDowngrades="yes" Schedule="afterInstallInitialize" />` | PASS | Major upgrade is explicitly declared in MSI authoring. Downgrades are allowed; the newer product is removed early to avoid file-versioning blocks. |
| ARP/installer metadata | (legacy expected) | `Framework.wxs` sets `ARPPRODUCTICON`, `ARPNOREMOVE`, `DISPLAYNAME`, `FULL_VERSION_NUMBER` | PASS | Matches the typical FieldWorks behavior (remove disabled, branded icon). |
| Dual-directory UI (App + Data) | (legacy expected; custom dialogs) | `GIInstallDirDlg.wxs` + `Framework.wxs` sets `WIXUI_INSTALLDIR=APPFOLDER`, `WIXUI_PROJECTSDIR=DATAFOLDER` | PASS | Both directories are first-class and driven by custom UI. |
| Custom feature selection UI | (legacy expected; custom dialog) | `GICustomizeDlg.wxs` + `CustomFeatures.wxi` | PASS | Feature tree is defined in include and shown via dialog flow. |
| Dialog flow wiring | (legacy expected) | `Framework.wxs` `<UIRef Id="WixUI_DialogFlow"/>`; flow defined in `WixUI_DialogFlow.wxs` | PASS | Custom UI flow is explicitly referenced by MSI package. |
| Upgrade/path restrictions UX | (legacy expected) | `Framework.wxs` sets `EXPLANATIONTEXT` when upgrading and data folder is already existing | PASS | Text indicates data folder becomes fixed during upgrade. |
| Registry: install path + version | (legacy expected) | `Framework.wxs` writes HKLM `SOFTWARE\[REGISTRYKEY]` values for app folder + version | PASS | Component `RegKeyValues` writes `Program_Files_Directory_*` and `FieldWorksVersion`. |
| Registry: settings/data directories | (legacy expected) | `Framework.wxs` components `RegKeySettingsDir` and `HarvestDataDir` write HKLM values | PASS | Also uses `RegistrySearch` to read prior install values. |
| Registry: uninstall cleanup | (legacy expected) | `Framework.wxs` uses `ForceDeleteOnUninstall="yes"` for HKLM key | PASS | Combined with conditional CA `DeleteRegistryVersionNumber` on uninstall. |
| Shortcuts (desktop/start menu) | (legacy expected) | `Framework.wxs` components `ApplicationShortcutDesktop` and `ApplicationShortcutMenu` | PASS | Desktop + start menu + uninstall shortcut are present. |
| Start menu shortcuts (help/docs/utilities) | FieldWorks includes at `6a2d976e`: `FLExInstaller/CustomComponents.wxi` `StartMenuShortcuts` group | `FLExInstaller/Shared/Common/CustomComponents.wxi` `StartMenuShortcuts` group | PASS | Adds shortcuts like Help, Morphology Intro, Unicode Character Editor, EULA, and font documentation. |
| Environment variables | FieldWorks includes at `6a2d976e`: `FLExInstaller/CustomComponents.wxi` `FwEnvironmentVars` group | `FLExInstaller/Shared/Common/CustomComponents.wxi` `FwEnvironmentVars` group | PASS | Sets system env vars including `PATH` prefix, `FIELDWORKSDIR`, `ICU_DATA`, and `WEBONARY_API`. |
| URL protocol registration | FieldWorks includes at `6a2d976e`: `FLExInstaller/CustomComponents.wxi` registry under HKCR | `FLExInstaller/Shared/Common/CustomComponents.wxi` registry under HKCR | PASS | Registers `silfw` URL protocol and command handler. |
| ProcRunner install | (legacy expected) | `Framework.wxs` installs `ProcRunner_5.0.exe` under CommonFiles | PASS | Component `ProcRunner` is referenced by the main feature. |
| Custom actions (path checks, close apps, etc.) | (legacy expected) | `Framework.wxs` defines CA entries from `CustomActions.CA.dll` + includes `CustomActionSteps.wxi` | PASS | Core CA hooks present; detailed sequencing is inside include. |
| Bundle prerequisites: .NET 4.8 | `genericinstaller/BaseInstallerBuild/Bundle.wxs` defines `NetFx48Web` | `FLExInstaller/Shared/Base/Bundle.wxs` references `NetFx48Web` | PASS | Online bootstrapper includes netfx group. |
| Bundle prerequisites: VC++ redists | (legacy expected) | `Bundle.wxs` defines `redist_vc*` groups + `Redistributables.wxi` composes them | PASS | Uses registry detection for each VC redist. |
| Bundle prerequisite: FLEx Bridge offline | FieldWorks includes at `6a2d976e`: `FLExInstaller/Redistributables.wxi` adds `FLExBridge_Offline.exe` in `FlexBridgeInstaller` group | `FLExInstaller/Shared/Common/Redistributables.wxi` adds `FLExBridge_Offline.exe` | PASS | Detected via registry under `SIL\FLEx Bridge\9`. |
| Bundle theme + license UX | (legacy expected) | `Bundle.wxs` uses `WixStandardBootstrapperApplication Theme="hyperlinkLicense"` and theme variables | PASS | Theme + WXL are referenced; license is bundled from resources. |
| Offline bundle | `genericinstaller/BaseInstallerBuild/buildExe.bat` builds OfflineBundle via candle/light | `OfflineBundle.wxs` exists but is not compiled by `FieldWorks.Bundle.wixproj` | GAP | Offline bundle scenario appears not wired into current WiX 6 build output. |
| Code signing | `buildMsi.bat`/`buildExe.bat` call `signingProxy` and use `insignia` for bundle engine | `FieldWorks.Installer.wixproj`/`FieldWorks.Bundle.wixproj` call `signingProxy.bat` after build | PARTIAL | Bundle engine-signing parity with WiX 3/`insignia` is not demonstrated in the WiX 6 targets. |

## Notable divergences / risks

1. **Heat.exe dependency remains:** The MSI build depends on WiX 3 `heat.exe` for harvesting. This is likely intentional short-term, but it means a “pure WiX 6” toolchain isn’t achieved yet.
2. **Offline install story incomplete:** The presence of `OfflineBundle.wxs` and the legacy batch suggests offline bundles existed historically, but the current WiX 6 project compiles only `Bundle.wxs`.
3. **End-to-end verification not recorded:** Spec task `T019` (“Verify local build produces FieldWorks.msi and FieldWorks.exe”) is still unchecked; likewise CI integration tasks are unchecked.

## Recommendations (to finish parity)

- Wire offline bundle output into the WiX 6 build (either compile `OfflineBundle.wxs` as a second bundle project/output, or implement layout creation per the spec).
- Replace the custom `heat.exe` harvesting step with a WiX 6-compatible harvesting mechanism (or a controlled, version-pinned `heat.exe` tool acquisition step) so the build is reproducible.
- Complete `T019` + add a short “artifact checklist” (expected filenames + locations) so regressions are easy to spot.

## Files reviewed

- `genericinstaller/BaseInstallerBuild/Framework.wxs`
- `genericinstaller/BaseInstallerBuild/Bundle.wxs`
- `genericinstaller/BaseInstallerBuild/OfflineBundle.wxs`
- `genericinstaller/BaseInstallerBuild/GI*.wxs`
- `genericinstaller/BaseInstallerBuild/WixUI_DialogFlow.wxs`
- `genericinstaller/BaseInstallerBuild/buildMsi.bat`
- `genericinstaller/BaseInstallerBuild/buildExe.bat`
- FieldWorks (WiX 3.x includes) at `6a2d976e`: `FLExInstaller/CustomComponents.wxi`, `FLExInstaller/CustomFeatures.wxi`, `FLExInstaller/Redistributables.wxi`

- `Build/Installer.targets`
- `FLExInstaller/wix6/FieldWorks.Installer.wixproj`
- `FLExInstaller/wix6/FieldWorks.Bundle.wixproj`
- `FLExInstaller/wix6/Shared/Base/Framework.wxs`
- `FLExInstaller/wix6/Shared/Base/WixUI_DialogFlow.wxs`
- `FLExInstaller/wix6/Shared/Base/GI*.wxs`
- `FLExInstaller/wix6/Shared/Base/Bundle.wxs`
- `FLExInstaller/wix6/Shared/Common/Redistributables.wxi`
- `FLExInstaller/wix6/Shared/Common/CustomComponents.wxi`
- `FLExInstaller/wix6/Shared/Common/CustomFeatures.wxi`
- `WIX_MIGRATION_STATUS.md`
