# Remaining WiX 6 Issues

This is a working TODO list of remaining issues for the WiX 6 installer, based on current observed behavior.

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
