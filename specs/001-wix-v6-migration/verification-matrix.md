# WiX 6 Migration Verification Matrix (FieldWorks)

**Purpose**: Provide repeatable, reviewable verification that the WiX 6 implementation fully replaces the legacy WiX 3.x behavior.

**How to use this document**
- Treat each row as a *claim* that must be proven.
- For each row, attach evidence (logs/screenshots/file listings) and record where it lives.
- Prefer running on a clean VM snapshot and keeping evidence in a consistent location (e.g., `C:\Temp\FwInstallerEvidence\<date>\...`).

## Evidence conventions

- **Bundle log**: run the bundle with a log switch (recommended): `FieldWorks.exe /log C:\Temp\FwInstallerEvidence\bundle.log`
- **MSI log**: run MSI with verbose logging: `msiexec /i FieldWorks.msi /l*v C:\Temp\FwInstallerEvidence\msi-install.log`
- **Uninstall log**: `msiexec /x {PRODUCT-CODE} /l*v C:\Temp\FwInstallerEvidence\msi-uninstall.log` (or uninstall via ARP and capture bundle/MSI logs if possible)

> Note: exact command-line switches can vary by bootstrapper; if `/log` is not accepted, capture `%TEMP%` logs and record their names/paths in the Evidence field.

## Matrix

| Area | Requirement / expected outcome | Verification method | Evidence to capture | Status |
|---|---|---|---|---|
| Toolchain | Build/install does not require `genericinstaller` checkout | Build in a fresh clone/worktree without genericinstaller present | Build logs showing success + no missing path errors | ☐ |
| Toolchain | Build does not invoke WiX 3 tools (`candle.exe`, `light.exe`, `insignia.exe`) | Run build with command echoing enabled (or CI check) and search logs | Build logs (or CI log) demonstrating absence of those tool names (note: WiX v6 Heat is currently expected and emits `HEAT5149`) | ☐ |
| Build outputs | Local build produces expected artifacts | Build installer and confirm output filenames/locations | File listing of output folder + hashes | ☑ |
| Staging | Installer input staging copies correct payload | Inspect staged input folder(s) and compare to expected | Folder tree / file listing of staged inputs | ☑ |
| Harvesting | MSI contains harvested app files under APPFOLDER | Install, then verify files exist under chosen APPFOLDER; optionally inspect MSI tables | MSI log + file listing under install dir | ☐ |
| Harvesting | MSI contains harvested data files under DATAFOLDER | Install with non-default data dir and verify data payload exists | MSI log + file listing under data dir | ☐ |
| UI (dual dirs) | Dual-directory UI allows choosing App + Data directories | Run bundle/MSI; select custom App + Data paths; verify install uses them | Screenshots of dialog selections + MSI log showing resolved properties | ☐ |
| UI (features) | Feature selection UI matches expected feature tree | Choose a non-default feature subset and verify installed components reflect it | Screenshots + MSI log showing feature states | ☐ |
| UI flow | Dialog flow and navigation behave correctly | Run through install and confirm Back/Next/Cancel behavior | Bundle log + screenshots of key dialogs | ☐ |
| Upgrade | Major upgrade removes prior version without side-by-side | Install old version, then install new version; verify single entry + expected behavior | MSI/bundle logs for upgrade + ARP screenshot | ☐ |
| Upgrade | Data path lock behavior on upgrade (if required) | Upgrade with existing data folder and confirm rules (e.g., data path fixed) | MSI/bundle logs + screenshot of any explanation text | ☐ |
| Registry | Install writes expected HKLM keys/values (paths + version) | After install, inspect registry values | Export of relevant registry keys (reg export) | ☐ |
| Registry | Uninstall removes expected registry keys/values | Uninstall; confirm keys removed | Export before/after + uninstall log | ☐ |
| Shortcuts | Desktop shortcut installed when expected | Install with defaults; verify desktop link target | Screenshot + link properties (or file listing) | ☐ |
| Shortcuts | Start menu shortcuts installed (docs/tools/help) | Verify Start Menu folder contains expected links | Screenshot/file listing | ☐ |
| Protocol | `silfw:` URL protocol registration works | After install: run `start silfw:test` or click a test link | Registry export + observed behavior note | ☐ |
| Env vars | Environment variables are created/updated correctly | Check System Environment Variables (and PATH modifications) | Screenshot/export of env vars + reboot/logoff note | ☐ |
| Env vars | Env vars removed/restored on uninstall | Uninstall; confirm env vars removed and PATH restored (as appropriate) | Before/after env snapshot + uninstall log | ☐ |
| Custom actions | Custom actions run without blocking UI (no modal asserts) | Run install/uninstall/upgrade; check for assertion dialogs/hangs | Bundle/MSI logs + any trace log if enabled | ☐ |
| Prereqs (online) | Online bundle downloads prerequisites successfully | Install on VM with internet; observe downloads succeed | Bundle log + screenshot of progress | ☐ |
| Prereqs (detect) | Prereq detection works (skips if already installed) | Preinstall VC++/.NET; run bundle and confirm it skips | Bundle log showing detection results | ☐ |
| FLEx Bridge | Offline FLEx Bridge prerequisite is detected/installed as expected | Validate on clean VM and on VM with existing FLEx Bridge | Bundle log + registry evidence | ☐ |
| Theme/license | Bundle theme and license UX appear correct | Run bundle and confirm theme + license link/text | Screenshot(s) | ☐ |
| Offline mode | Offline installer story works end-to-end | Build offline artifact/layout; install on disconnected VM | Bundle log + proof no network required | ☐ |
| Localization | At least one non-English locale builds and runs end-to-end | Build localized bundle/MSI; run and confirm UI strings load | Screenshot(s) + artifact listing | ☐ |
| Signing | Signing is applied where required | Inspect signatures of MSI/EXE; ensure verification passes | Sigcheck output or file properties screenshots | ☐ |

## Notes / deviations

Record any deviations from expected behavior here, with links to logs/evidence and any follow-up tasks.

- 2025-12-17: Release build outputs + staging roots captured in [specs/001-wix-v6-migration/parity-check.md](specs/001-wix-v6-migration/parity-check.md) (hashes + staging root).
- 2025-12-17: Bundle log captured at `Output\InstallerEvidence\20251217-local-bundle-passive\bundle.log` (local run; not a clean VM).
- 2025-12-17: FLEx Bridge package is now present in the bundle chain: `Output\InstallerEvidence\flexbridge-parity\bundle.log` contains `Detected package: FBInstaller` and `Planned package: FBInstaller` (still needs clean-VM behavior validation).
