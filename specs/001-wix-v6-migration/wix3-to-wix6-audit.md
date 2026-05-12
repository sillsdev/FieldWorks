# WiX 3.x → WiX 6 Migration Audit (FieldWorks)

**Branch audited:** `001-wix-v6-migration`

**How to verify this audit**

- Parity procedure: [parity-check.md](parity-check.md)
- Evidence tracking: [verification-matrix.md](verification-matrix.md)
- Manual VM runs: [golden-install-checklist.md](golden-install-checklist.md)

## Executive summary

This worktree contains a WiX 6 (SDK-style) MSI + Burn bundle implementation with the critical installer UX pieces (dual-directory UI + feature tree) and the key registry/shortcut behaviors implemented in WiX authoring.

However, this is not yet a finished WiX 6 migration:

- The MSI project now uses **WixToolset.Heat 6.0.2** from NuGet/repo packages to harvest binaries/data, then compiles with the WiX 6 SDK.
- The legacy WiX 3 batch scripts are still present for the fallback route, but the migration source of truth is the MSBuild target plus SDK-style `.wixproj` files under `FLExInstaller/wix6/`.
- Several spec tasks remain incomplete, notably: CI integration, WiX 3 to WiX 6 upgrade validation, ARP/uninstall diagnostics, and clean-machine online/offline end-user validation.
- `Build/InstallerBuild.proj` still defaults `InstallerToolset` to `Wix3`; this is a current-state mismatch with the WiX 6-first migration goal.

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
- Offline bundle project: `FLExInstaller/wix6/FieldWorks.OfflineBundle.wixproj`

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
| MSI build pipeline | `genericinstaller/BaseInstallerBuild/buildMsi.bat` (heat → candle/light → sign) | `FLExInstaller/wix6/FieldWorks.Installer.wixproj` (WixToolset.Heat 6.0.2 → SDK compile) | PASS | Harvesting is pinned to WixToolset.Heat 6.0.2 and no longer depends on WiX 3 tools on PATH. |
| Bundle build pipeline | `genericinstaller/BaseInstallerBuild/buildExe.bat` (candle/light, online+offline, insignia signing) | `FLExInstaller/wix6/FieldWorks.Bundle.wixproj` and `FLExInstaller/wix6/FieldWorks.OfflineBundle.wixproj` (SDK bundle builds) + payload staging | PARTIAL | Online and offline bundles are wired, but signing parity and clean-machine runtime validation are not yet proven. |
| Artifact orchestration | (historically batch-driven) `buildBaseInstaller.bat` chains `buildMsi.bat` + `buildExe.bat` | `Build/Installer.targets` provides `BuildInstaller` target | PASS | `BuildInstaller` depends on staging + prerequisites + ProcRunner build. |
| Input staging for harvest | (legacy assumed in external scripts; not in-repo baseline) | `Build/Installer.targets` target `StageInstallerInputs` | PASS | Copies Output + DistFiles + fonts + ICU + localizations into `BuildDir/FieldWorks_InstallerInput_*`. |
| Harvesting app files | `buildMsi.bat` uses `heat.exe dir %MASTERBUILDDIR% ... -cg HarvestedAppFiles ... -dr APPFOLDER` | `FieldWorks.Installer.wixproj` target `HarvestAppAndData` uses `heat.exe dir $(_MasterBuildDirFull) ... -cg HarvestedAppFiles ... -dr APPFOLDER` | PASS | Same component group ID + same directory ref pattern. |
| Harvesting data files | `buildMsi.bat` uses `heat.exe dir %MASTERDATADIR% ... -cg HarvestedDataFiles ... -dr HARVESTDATAFOLDER` | `FieldWorks.Installer.wixproj` target `HarvestAppAndData` uses `heat.exe dir $(_MasterDataDirFull) ... -cg HarvestedDataFiles ... -dr HARVESTDATAFOLDER` | PASS | Same component group ID + same directory ref pattern. |
| MSI major upgrade | (legacy expected) | `Framework.wxs` `<MajorUpgrade AllowSameVersionUpgrades="yes" Schedule="afterInstallInitialize" />`; bundles declare `RelatedBundle Action="Upgrade"` | PARTIAL | Major upgrade is authored, but WiX 3 to WiX 6 upgrade behavior, same-version dev upgrades, ARP results, and uninstall behavior still need evidence. |
| ARP/installer metadata | (legacy expected) | `Framework.wxs` sets `ARPPRODUCTICON`, `ARPNOREMOVE`, `DISPLAYNAME`, `FULL_VERSION_NUMBER` | PASS | Matches the typical FieldWorks behavior (remove disabled, branded icon). |
| Dual-directory UI (App + Data) | (legacy expected; custom dialogs) | `GIInstallDirDlg.wxs` + `Framework.wxs` sets `WIXUI_INSTALLDIR=APPFOLDER`, `WIXUI_PROJECTSDIR=DATAFOLDER` | PASS | Both directories are first-class and driven by custom UI. |
| Custom feature selection UI | (legacy expected; custom dialog) | `GICustomizeDlg.wxs` + `CustomFeatures.wxi` | PASS | Feature tree is defined in include and shown via dialog flow. |
| Dialog flow wiring | (legacy expected) | `Framework.wxs` `<UIRef Id="WixUI_DialogFlow"/>`; flow defined in `WixUI_DialogFlow.wxs` | PASS | Custom UI flow is explicitly referenced by MSI package. |
| Upgrade/path restrictions UX | (legacy expected) | `Framework.wxs` sets `EXPLANATIONTEXT` when upgrading and data folder is already existing | PASS | Text indicates data folder becomes fixed during upgrade. |
| Registry: install path + version | (legacy expected) | `Framework.wxs` writes HKLM `SOFTWARE\[REGISTRYKEY]` values for app folder + version | PASS | Component `RegKeyValues` writes `Program_Files_Directory_*` and `FieldWorksVersion`. |
| Registry: settings/data directories | (legacy expected) | `Framework.wxs` components `RegKeySettingsDir` and `HarvestDataDir` write HKLM values | PASS | Also uses `RegistrySearch` to read prior install values. |
| Registry: uninstall cleanup | (legacy expected) | `Framework.wxs` uses `ForceDeleteOnUninstall="yes"` for HKLM key | PASS | Combined with conditional CA `DeleteRegistryVersionNumber` on uninstall. |
| Shortcuts (desktop/start menu) | (legacy expected) | `Framework.wxs` components `ApplicationShortcutDesktop` and `ApplicationShortcutMenu` | PASS | Desktop + start menu + uninstall shortcut are present. |
| Start menu shortcuts (help/docs/utilities) | FieldWorks includes at `6a2d976e`: `FLExInstaller/CustomComponents.wxi` `StartMenuShortcuts` group | `FLExInstaller/wix6/Shared/Common/CustomComponents.wxi` `StartMenuShortcuts` group | PASS | Adds shortcuts like Help, Morphology Intro, Unicode Character Editor, EULA, and font documentation. |
| Environment variables | FieldWorks includes at `6a2d976e`: `FLExInstaller/CustomComponents.wxi` `FwEnvironmentVars` group | `FLExInstaller/wix6/Shared/Common/CustomComponents.wxi` `FwEnvironmentVars` group | PASS | Sets system env vars including `PATH` prefix, `FIELDWORKSDIR`, `ICU_DATA`, and `WEBONARY_API`. |
| URL protocol registration | FieldWorks includes at `6a2d976e`: `FLExInstaller/CustomComponents.wxi` registry under HKCR | `FLExInstaller/wix6/Shared/Common/CustomComponents.wxi` registry under HKCR | PASS | Registers `silfw` URL protocol and command handler. |
| ProcRunner install | (legacy expected) | `Framework.wxs` installs `ProcRunner_5.0.exe` under CommonFiles | PASS | Component `ProcRunner` is referenced by the main feature. |
| Custom actions (path checks, close apps, etc.) | (legacy expected) | `Framework.wxs` defines CA entries from `CustomActions.CA.dll` + includes `CustomActionSteps.wxi` | PASS | Core CA hooks present; detailed sequencing is inside include. |
| Bundle prerequisites: .NET 4.8 | `genericinstaller/BaseInstallerBuild/Bundle.wxs` defines `NetFx48Web` | `FLExInstaller/wix6/Shared/Base/Bundle.wxs` references `NetFx48Web` | PASS | Online bootstrapper includes netfx group. |
| Bundle prerequisites: VC++ redists | (legacy expected) | `Bundle.wxs` defines `redist_vc*` groups + `Redistributables.wxi` composes them | PASS | Uses registry detection for each VC redist. |
| Bundle prerequisite: FLEx Bridge offline | FieldWorks includes at `6a2d976e`: `FLExInstaller/Redistributables.wxi` adds `FLExBridge_Offline.exe` in `FlexBridgeInstaller` group | `FLExInstaller/wix6/Shared/Common/Redistributables.wxi` adds `FLExBridge_Offline.exe` | PASS | Detected via registry under `SIL\FLEx Bridge\9`. |
| Bundle theme + license UX | (legacy expected) | `Bundle.wxs` uses `WixStandardBootstrapperApplication Theme="hyperlinkLicense"` and theme variables | PASS | Theme + WXL are referenced; license is bundled from resources. |
| Offline bundle | `genericinstaller/BaseInstallerBuild/buildExe.bat` builds OfflineBundle via candle/light | `FieldWorks.OfflineBundle.wixproj` compiles `Shared/Base/OfflineBundle.wxs`; `Build/Installer.targets` invokes it in `BuildInstaller` | PARTIAL | Offline artifact production is wired. Disconnected-machine install and prerequisite embedding behavior still need runtime evidence. |
| Code signing | `buildMsi.bat`/`buildExe.bat` call `signingProxy` and use `insignia` for bundle engine | `FieldWorks.Installer.wixproj`/`FieldWorks.Bundle.wixproj` call `signingProxy.bat` after build | PARTIAL | Bundle engine-signing parity with WiX 3/`insignia` is not demonstrated in the WiX 6 targets. |

## Notable divergences / risks

1. **Default still points at WiX 3:** `Build/InstallerBuild.proj` defaults `InstallerToolset` to `Wix3`. That is acceptable only as a transition state; the migration is not complete until the default is switched or the blocker is explicitly documented.
2. **CI still has genericinstaller/PatchableInstaller dependencies:** base and patch installer workflows still checkout `sillsdev/genericinstaller` as `PatchableInstaller/` for legacy jobs.
3. **Burn provider compatibility is unproven:** The bundles use `RelatedBundle Action="Upgrade"`, but `AppMsiPackage` has no package-level `<Provides Key="..." />`. WiX 3 to WiX 6 upgrade detection and repair/uninstall behavior must be validated before release.
4. **Offline install story is built but unverified:** The offline bundle project is wired and produces a distinct artifact, but disconnected-machine install has not been proven.
5. **Patch/MSP support is not implemented for WiX 6:** `BuildPatch` remains a WiX 3 target. A WiX 6 patch design must cover `PatchBaseline`, `.wixpdb` retention, component GUID stability, and replacement of `buildPatch.bat`.

## Recommendations (to finish parity)

- Add a WiX 6 CI lane that builds without `genericinstaller`/`PatchableInstaller/` and uploads MSI, online bundle, offline bundle, and `.wixpdb` artifacts.
- Decide when to switch `InstallerToolset` default to `Wix6`; treat a continued WiX 3 default as an explicit release blocker.
- Validate WiX 3 to WiX 6 upgrade, ARP presentation, and uninstall behavior with logs and before/after snapshots.
- Keep WiX 6 patch/MSP work out of the first migration unless a separate design covers baseline artifacts and component identity stability.

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
