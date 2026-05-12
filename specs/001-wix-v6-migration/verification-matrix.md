# WiX 6 Migration Verification Matrix (FieldWorks)

**Purpose**: Provide repeatable, reviewable verification that the WiX 6 implementation fully replaces the legacy WiX 3.x behavior.

**How to use this document**
- Treat each row as a *claim* that must be proven.
- For each row, attach evidence (logs/screenshots/file listings) and record where it lives.
- Prefer running on a clean VM snapshot and keeping evidence in a consistent location (e.g., `C:\Temp\FwInstallerEvidence\<date>\...`).
- For failure triage, use [evidence-collection-checklist.md](evidence-collection-checklist.md).

## Evidence conventions

- **Bundle log**: run the bundle with a log switch (recommended): `FieldWorks.exe /log C:\Temp\FwInstallerEvidence\bundle.log`
- **MSI log**: run MSI with verbose logging: `msiexec /i FieldWorks.msi /l*v C:\Temp\FwInstallerEvidence\msi-install.log`
- **Uninstall log**: `msiexec /x {PRODUCT-CODE} /l*v C:\Temp\FwInstallerEvidence\msi-uninstall.log` (or uninstall via ARP and capture bundle/MSI logs if possible)
- **Agent-script evidence**: repository helper scripts write under `Output/InstallerEvidence/<RunId>/`. When testing WiX 6 with `scripts/Agent/Invoke-Installer.ps1`, pass an explicit `-InstallerPath` pointing at `FLExInstaller/wix6/bin/...`; otherwise the default resolver may select legacy WiX 3 artifact paths.

> Note: exact command-line switches can vary by bootstrapper; if `/log` is not accepted, capture `%TEMP%` logs and record their names/paths in the Evidence field.

## Matrix

| Area | Requirement / expected outcome | Verification method | Evidence to capture | Status |
|---|---|---|---|---|
| Toolchain | Build/install does not require `genericinstaller` checkout | Build in a fresh clone/worktree without genericinstaller present | Build logs showing success + no missing path errors | ☑ |
| Toolchain | Build does not invoke WiX 3 tools (`candle.exe`, `light.exe`, `insignia.exe`) | Run build with command echoing enabled (or CI check) and search logs | Build logs (or CI log) demonstrating absence of those tool names (note: WiX v6 Heat is currently expected and emits `HEAT5149`) | ☑ |
| Toolchain | WiX 6 is the intended migration default | Inspect `Build/InstallerBuild.proj` default and build scripts; decide/switch default after gates | Change record or release decision explaining why `InstallerToolset` remains `Wix3` | ☐ |
| Build outputs | Local build produces expected artifacts | Build installer and confirm output filenames/locations | File listing of output folder + hashes | ☑ |
| Build outputs | Offline bundle artifact is produced | Run `./build.ps1 -BuildInstaller -InstallerToolset Wix6` and inspect `FLExInstaller/wix6/bin/x64/<config>/` | File listing showing `FieldWorksOfflineBundle.exe` and `.wixpdb` | ☑ |
| Staging | Installer input staging copies correct payload | Inspect staged input folder(s) and compare to expected | Folder tree / file listing of staged inputs | ☑ |
| Harvesting | MSI contains harvested app files under APPFOLDER | Install, then verify files exist under chosen APPFOLDER; optionally inspect MSI tables | MSI log + file listing under install dir | ☐ |
| Harvesting | MSI contains harvested data files under DATAFOLDER | Install with non-default data dir and verify data payload exists | MSI log + file listing under data dir | ☐ |
| UI (dual dirs) | Dual-directory UI allows choosing App + Data directories | Run bundle/MSI; select custom App + Data paths; verify install uses them | Screenshots of dialog selections + MSI log showing resolved properties | ☐ |
| UI (features) | Feature selection UI matches expected feature tree | Choose a non-default feature subset and verify installed components reflect it | Screenshots + MSI log showing feature states | ☐ |
| UI flow | Dialog flow and navigation behave correctly | Run through install and confirm Back/Next/Cancel behavior | Bundle log + screenshots of key dialogs | ☐ |
| Upgrade | Major upgrade removes prior version without side-by-side | Install old version, then install new version; verify single entry + expected behavior | MSI/bundle logs for upgrade + ARP screenshot | ☐ |
| Upgrade | WiX 3 to WiX 6 bundle detection works across Burn provider changes | Install WiX 3 bundle, then run WiX 6 bundle; inspect detection/plan/apply logs | Bundle log showing related bundle/package detection, planned uninstall/upgrade, and final single ARP entry | ☐ |
| Upgrade | Provider-key compatibility decision is recorded | Extract baseline WiX 3 provider/package identity and compare to WiX 6 authoring | Notes showing whether package-level `<Provides Key="..." />` is needed, plus test evidence after any change | ☐ |
| Upgrade | Data path lock behavior on upgrade (if required) | Upgrade with existing data folder and confirm rules (e.g., data path fixed) | MSI/bundle logs + screenshot of any explanation text | ☐ |
| Registry | Install writes expected HKLM keys/values (paths + version) | After install, inspect registry values | Export of relevant registry keys (reg export) | ☐ |
| Registry | Uninstall removes expected registry keys/values | Uninstall; confirm keys removed | Export before/after + uninstall log | ☐ |
| Shortcuts | Desktop shortcut installed when expected | Install with defaults; verify desktop link target | Screenshot + link properties (or file listing) | ☐ |
| Shortcuts | Start menu shortcuts installed (docs/tools/help) | Verify Start Menu folder contains expected links | Screenshot/file listing | ☐ |
| Protocol | `silfw:` URL protocol registration works | After install: run `start silfw:test` or click a test link | Registry export + observed behavior note | ☐ |
| Env vars | Environment variables are created/updated correctly | Check System Environment Variables (and PATH modifications) | Screenshot/export of env vars + reboot/logoff note | ☐ |
| Env vars | Env vars removed/restored on uninstall | Uninstall; confirm env vars removed and PATH restored (as appropriate) | Before/after env snapshot + uninstall log | ☐ |
| Custom actions | Custom actions run without blocking UI (no modal asserts) | Run install/uninstall/upgrade; check for assertion dialogs/hangs | Bundle/MSI logs + any trace log if enabled | ☐ |
| Uninstall | Uninstall via ARP completes without hang | Install WiX 6 bundle, uninstall from Settings/Apps, monitor completion | Bundle uninstall log, MSI uninstall log, Event Viewer entries, before/after snapshot | ☐ |
| Prereqs (online) | Online bundle downloads prerequisites successfully | Install on VM with internet; observe downloads succeed | Bundle log + screenshot of progress | ☐ |
| Prereqs (detect) | Prereq detection works (skips if already installed) | Preinstall VC++/.NET; run bundle and confirm it skips | Bundle log showing detection results | ☐ |
| FLEx Bridge | Offline FLEx Bridge prerequisite is detected/installed as expected | Validate on clean VM and on VM with existing FLEx Bridge | Bundle log + registry evidence | ☐ |
| Theme/license | Bundle theme and license UX appear correct | Run bundle and confirm theme + license link/text | Screenshot(s) | ☐ |
| Offline mode | Offline installer story works end-to-end | Build `FieldWorksOfflineBundle.exe`; install on disconnected VM | Bundle log + proof no network required + prerequisite detection/install evidence | ☐ |
| Localization | At least one non-English locale builds and runs end-to-end | Build localized bundle/MSI; run and confirm UI strings load | Screenshot(s) + artifact listing | ☐ |
| Signing | Signing is applied where required | Inspect signatures of MSI/EXE; ensure verification passes | Sigcheck output or file properties screenshots | ☐ |
| Patch/MSP | WiX 6 patch support is explicitly deferred or implemented | Review targets and patch design before using `-BuildPatch -InstallerToolset Wix6` | Design note or build/test evidence for WiX 6 `BuildPatch` | ☐ |

## Notes / deviations

Record any deviations from expected behavior here, with links to logs/evidence and any follow-up tasks.

- 2025-12-17: Release build outputs + staging roots captured in [specs/001-wix-v6-migration/parity-check.md](specs/001-wix-v6-migration/parity-check.md) (hashes + staging root).
- 2025-12-17: Bundle log captured at `Output\InstallerEvidence\20251217-local-bundle-passive\bundle.log` (local run; not a clean VM).
- 2025-12-17: FLEx Bridge package is now present in the bundle chain: `Output\InstallerEvidence\flexbridge-parity\bundle.log` contains `Detected package: FBInstaller` and `Planned package: FBInstaller` (still needs clean-VM behavior validation).
- Current repo: `FieldWorks.OfflineBundle.wixproj` is wired into `Build/Installer.targets`; offline artifact production and disconnected-machine runtime behavior are separate claims and both need evidence.
- Current repo: `Build/InstallerBuild.proj` still defaults to `Wix3`; treat this as a migration blocker until switched or documented.
- 2026-04-29: `CI.yml` now includes a `Build WiX 6 installer artifacts` job that checks out the helper repos (but not `PatchableInstaller/`), runs `./build.ps1 -BuildInstaller -InstallerToolset Wix6` with `FastBundleBuild=0`, audits the build log with `Build/Agent/Test-Wix6InstallerBuildEvidence.ps1`, and uploads MSI, online bundle, offline bundle, `.wixpdb`, logs, and evidence artifacts.
- 2026-04-30: Local Release installer-only build succeeded without `PatchableInstaller/` present and produced `FieldWorks.msi`, `FieldWorksBundle.exe`, and `FieldWorksOfflineBundle.exe`; `Build/Agent/Test-Wix6InstallerBuildEvidence.ps1` verified artifact hashes and offline prerequisite payload availability.
- 2026-04-30: WinApp MCP smoke-tested the WiX 6 Release bundle UI by toggling the license checkbox, reaching the Install button, and canceling before install; evidence is under `Output/InstallerEvidence/20260430-winapp-mcp-clickthrough/`.
- 2026-04-30: `Collect-InstallerSnapshot.ps1` and `Compare-InstallerSnapshots.ps1` now capture and diff Burn dependency provider registry entries from `Software\Classes\Installer\Dependencies`, so upgrade/ARP evidence runs can record provider-key changes instead of relying on manual registry notes.
- 2026-04-30: The WiX 6 CI audit now runs `Test-Wix6InstallerBuildEvidence.ps1 -RequireNoWix3ToolsOnPath`. Local dev-machine validation correctly failed while `C:\Users\johnm\AppData\Local\FieldWorksTools\Wix314` was on PATH, so T045 remains open until CI or a sanitized local environment proves the full build with no WiX 3 tools on PATH.
- 2026-04-30: The main CI workflow now has an opt-in `workflow_dispatch` WiX 6 quiet install/uninstall verification (`run_wix6_installer_check=true`) that runs `Invoke-InstallerCheck.ps1`, captures before/after snapshots, installer logs, uninstall logs, and uploads `Output/InstallerEvidence/**` with the existing WiX 6 evidence artifact.
- 2026-04-30: `Output/InstallerEvidence/20260430-winapp-mcp-manual-wix6-install/` captures a local WinApp MCP WiX 6 full-UI attempt. WinApp drove the bundle license and Install path, Burn installed `FBInstaller`, and the MSI log reached `GIWelcomeDlg` with `UILevel=5`. A non-elevated WinApp/VS Code host could observe the elevated MSI `&Next` focus and capture screenshots but could not advance the MSI dialog via UIA click, coordinate click, keyboard, or direct Win32 button messages. The aborted run left one added Burn dependency provider (`{BDFC2A1E-094B-45A3-ADC4-B681631B5828}_v9.3.9.1`) but no tracked file, uninstall entry, or registry-value changes. Full manual MSI UI validation needs an elevated automation host.
- 2026-04-30: **T093 ☑** `Output/InstallerEvidence/wix6-quiet-install/` — full quiet install/uninstall cycle completed with `Invoke-InstallerCheck.ps1 -RunUninstall -AssertUninstallClean`. Install exit 0, uninstall exit 0. Pre-install WiX 3 bundles detected as upgrades and uninstalled automatically by Burn. After-uninstall diff vs pre-install baseline: 0 added files, 0 added registry values, 5604 files removed, 1 ARP entry removed — perfectly clean. `FBInstaller` detection condition shows `Absent` on every run (present on machine but Burn re-installs passively, exits 0); noted as a detection-condition investigation item (see T099).
