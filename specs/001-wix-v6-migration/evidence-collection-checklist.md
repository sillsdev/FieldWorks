# Evidence Collection Checklist

Use this checklist when a WiX 6 installer validation run fails. The goal is to preserve enough evidence to decide whether the failure is in Burn, MSI authoring, custom actions, prerequisites, machine state, or the legacy-to-WiX6 upgrade path.

## General Run Evidence

- Bundle or MSI artifact path tested. For WiX 6, use explicit paths under `FLExInstaller/wix6/bin/x64/<Configuration>/`.
- Exact command line used.
- Machine state: clean VM, local dev PC, upgraded machine, online/offline, installed prerequisites.
- Bundle log and MSI verbose log.
- Before/after snapshots from `scripts/Agent/Collect-InstallerSnapshot.ps1` where practical.
- Burn dependency provider entries from `Software\Classes\Installer\Dependencies` in those snapshots when investigating provider-key compatibility, upgrade detection, repair, or uninstall behavior.
- ARP/Settings screenshot after install, upgrade, and uninstall.

## Silent Exit or No UI

- Bundle log from `FieldWorksBundle.exe /log <path>` or `%TEMP%` Burn logs.
- Event Viewer Application entries for `FieldWorksBundle.exe`, `msiexec.exe`, `.NET Runtime`, and `Application Error`.
- Crash dumps from `%LOCALAPPDATA%\CrashDumps` if present.

## MSI UI Does Not Appear From Bundle

- Bundle log showing `WixBundleUILevel` and planning for `AppMsiPackage`.
- `AppMsiPackage` authoring evidence for `bal:DisplayInternalUICondition`.
- Full, passive, and quiet mode results. Full UI should show MSI dialogs; passive/quiet should not.

## WiX 3 to WiX 6 Upgrade Fails

- Before-upgrade snapshot from a WiX 3 install.
- WiX 6 bundle log and MSI log from the upgrade run.
- Log evidence for related bundle/package detection and `RemoveExistingProducts`.
- ARP screenshot before and after upgrade.
- Registry exports for FieldWorks install path, data path, version, and uninstall entries.
- Snapshot diff for Burn dependency provider keys and dependents before and after the WiX 6 upgrade.
- Note whether a package-level `<Provides Key="..." />` compatibility decision has been tested.

## Duplicate ARP Entries or Wrong Size

- Settings/Apps screenshot showing all FieldWorks/SIL entries.
- Registry exports from `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall` and `HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall` for matching entries.
- Bundle log lines for package registration and visibility.
- MSI ARP property evidence, especially `ARPSYSTEMCOMPONENT` and package `Visible` state.

## Uninstall Hangs

- Bundle uninstall log and MSI verbose uninstall log.
- Last action visible in the bundle and MSI logs.
- Event Viewer entries at the hang time.
- Crash dumps if present.
- Screenshot of any visible prompt or blocked UI.
- Running process list for `FieldWorks*`, `msiexec`, and bundle processes.

## Offline Install Fails

- Exact `FieldWorksOfflineBundle.exe` path and file hash.
- Proof the VM was disconnected before the run.
- Bundle log showing local prerequisite resolution, not network download attempts.
- Presence of embedded or staged prerequisite payloads in the bundle/cache.

## Custom Action Failure

- MSI log around `Return value 3` or the last `Action start` before a hang.
- Property values immediately before the failing custom action.
- Any FieldWorks trace log if diagnostics were enabled.
- Map the action to the `FLExInstaller/wix6/Shared/CustomActions` source before changing authoring.
