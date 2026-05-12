# Remaining WiX 6 Issues and Validation Gaps

This is a working TODO list of blocking issues and validation gaps for the WiX 6 installer, based on current observed behavior and the repo state.

**Release posture**: Do not make WiX 6 the default installer route until the WiX 3 to WiX 6 upgrade, ARP presentation, uninstall, online install, and offline install checks below have evidence. The repo still defaults `InstallerToolset` to `Wix3`, which should be treated as a migration blocker or explicit release decision.

## Upgrade over WiX 3 (keep settings, same install shape)

Goal: installing the WiX 6 bundle over an existing WiX 3 FieldWorks install should behave like a proper upgrade (same install locations, old install removed, settings preserved).

**Hard requirement:** FieldWorks must be single-instance on a machine. The WiX 6 installer must not allow side-by-side installs of FieldWorks from any previous WiX 3 or WiX 6 generation. Installing the WiX 6 bundle must remove/replace all prior FieldWorks installs.

- [ ] Upgrade detects an existing WiX 3 installation reliably (via UpgradeCode / related bundle detection) and enters “upgrade” behavior.
- [ ] Upgrade uses the same default install paths as WiX 3 (APPFOLDER / DATAFOLDER defaults match).
- [ ] Upgrade does not install to a new/different location when upgrading from WiX 3.
- [ ] Upgrade removes/uninstalls the old WiX 3 install (no side-by-side installs, ever).
- [ ] Upgrade keeps all user settings (projects, preferences, registry-based settings, config files, etc.).
- [ ] Upgrade keeps all user data in-place (no duplication or orphaned data folders).
- [ ] Add explicit evidence for the above (bundle log + MSI log + before/after install paths + ARP snapshot).
- [ ] Verify Burn provider compatibility across WiX 3 to WiX 6. The current WiX 6 `MsiPackage` has no package-level `<Provides Key="..." />`; extract the WiX 3 baseline provider/package identity and determine whether a compatibility provider key is required.
- [ ] Test same-version major upgrades for WiX 6 dev builds: install a WiX 6 build, reinstall the same `VersionNumber`, and confirm it replaces rather than side-by-side installs.

## Product icon parity

Goal: WiX 6 install should use the same icon experience as WiX 3.

- [x] ARP icon matches WiX 3 (Programs & Features / Settings > Apps).
- [x] Start Menu shortcuts use the same icon as WiX 3.
- [x] Desktop shortcut (if installed) uses the same icon as WiX 3.

## ARP entries and installed size look wrong (multiple entries)

Observed:
- ARP shows one “FieldWorks Language Explorer 9.9” around ~900MB.
- ARP shows multiple “FieldWorks Language Explorer 9 Packages” entries (each ~454MB).
- WiX 3 shows a single entry around ~458MB.

Note: the WiX 6 authoring has been updated to use “FieldWorks” branding and 9.3.x versioning, but ARP behavior still needs to be re-validated in the clean-machine lane.

Goal: “one install” presentation similar to WiX 3, with sane size reporting.

- [ ] Only one primary ARP entry is visible for FieldWorks (matching WiX 3’s behavior).
- [ ] “Packages” entry/entries are not visible to the user (or are collapsed into a single consistent representation).
- [ ] Reported installed size is reasonable and comparable to WiX 3 (no duplicate counting of the same payload).
- [x] Prevent side-by-side MSI installs when `VersionNumber` does not change (dev builds): enable same-version major upgrades.
- [x] Remove MSI ARP flags that make entries non-removable (`ARPNOREMOVE` / `NoRemove=1`) so stale installs can be cleaned up.
- [ ] Validate which packages/components are causing duplicate ARP entries and fix authoring so they don’t register as separate products.
- [ ] Confirm the final state with an ARP snapshot and (optionally) Windows Installer product inventory evidence.

## Uninstall hangs from Add/Remove Programs

Observed: uninstall from Settings / Add-Remove Programs gets stuck and does not move forward.

- [ ] Reproduce uninstall hang deterministically and capture evidence (bundle log, MSI log, any temp logs).
- [ ] Identify whether the hang is in Burn (bootstrapper) vs MSI execution vs a custom action.
- [ ] Fix uninstall to complete successfully from ARP without user intervention.
- [ ] Verify post-uninstall cleanup matches expectations (ARP removed, registry keys cleaned up, shortcuts removed, env vars restored if applicable).

Diagnostics to capture for every uninstall hang reproduction:

- Bundle uninstall log and MSI verbose uninstall log.
- `%TEMP%` WiX/Burn logs and any `WixBundleLog_AppMsiPackage` path from the bundle log.
- Before/after snapshots from `scripts/Agent/Collect-InstallerSnapshot.ps1`.
- Event Viewer Application entries around the hang.
- Crash dumps from `%LOCALAPPDATA%\CrashDumps` if present.

Use [evidence-collection-checklist.md](evidence-collection-checklist.md) for symptom-specific evidence requirements.

## Online and offline clean-machine validation

- [ ] Online bundle installs on a clean VM with internet access, downloads or skips prerequisites correctly, shows MSI internal UI in full UI mode, and launches FieldWorks after install.
- [ ] Passive and quiet online installs do not show MSI internal UI and return expected exit codes.
- [ ] Offline bundle `FieldWorksOfflineBundle.exe` installs on a disconnected VM using embedded/local prerequisites only.
- [ ] Offline bundle evidence includes proof no network was required.

## Burn engine signing and code signing parity

Goal: Ensure signed WiX 6 bundles match the WiX 3 release expectations.

- [ ] Verify `signingProxy.bat` signs the MSI and both bundle EXEs when signing is enabled.
- [ ] Verify Burn engine/container signing behavior is equivalent to the old WiX 3 `insignia` flow, or document the WiX 6 SDK signing behavior that replaces it.
- [ ] Capture Authenticode evidence for MSI, online bundle, and offline bundle.

## Patch/MSP support is deferred

The WiX 6 migration currently supports MSI major upgrades, not WiX 6 MSP patch generation. The legacy `BuildPatch` target remains in the WiX 3 path only.

- [ ] Define a separate WiX 6 patch design before adding any `BuildPatch` support.
- [ ] Include `PatchBaseline`, base MSI and `.wixpdb` retention, component GUID stability, and repair/uninstall compatibility in that design.
- [ ] Do not treat `build.ps1 -BuildPatch -InstallerToolset Wix6` as supported until the design is implemented and tested.
