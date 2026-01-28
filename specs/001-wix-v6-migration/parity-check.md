# WiX 3.x → WiX 6 Parity Check Procedure (FieldWorks)

**Purpose**

This document defines a repeatable parity-check process to ensure the WiX 6 (SDK-style) MSI + Burn bundle implementation fully replaces the legacy WiX 3.x behavior (genericinstaller template + FieldWorks includes).

> Note: this worktree does not currently include a `genericinstaller/` folder, so the default “WiX 3.x baseline” is the in-repo `FLExInstaller/*.wxi` at the chosen baseline ref.

It complements (does not replace):

- [wix3-to-wix6-audit.md](wix3-to-wix6-audit.md) (what we *believe* is equivalent)
- [verification-matrix.md](verification-matrix.md) (what we must *prove* with evidence)
- [golden-install-checklist.md](golden-install-checklist.md) (manual install runs and evidence collection)

Related:

- [tasks.md](tasks.md) (tracks remaining parity/validation work)

**Scope**

Parity check = two kinds of proof:

1) **Behavioral parity** (install, upgrade, uninstall outcomes match expectations).
2) **Build/authoring parity** (any legacy “implicit” steps are explicitly represented in WiX 6 and can be pointed to in-tree).

---

## Inputs / Baselines

**WiX 3.x baseline definition**

- FieldWorks product-specific includes (historical): `FLExInstaller/*.wxi` at baseline ref (e.g., `release/9.3` or commit `6a2d976e`)
- Optional (if available externally): the WiX 3.x generic template authoring from `genericinstaller/BaseInstallerBuild/*` and `genericinstaller/Common/*`

**WiX 6 implementation under test**

- MSI: `FLExInstaller/wix6/FieldWorks.Installer.wixproj` + `FLExInstaller/wix6/Shared/Base/*.wxs` + `FLExInstaller/wix6/Shared/Common/*.wxi`
- Bundle: `FLExInstaller/wix6/FieldWorks.Bundle.wixproj` + `FLExInstaller/wix6/Shared/Base/Bundle.wxs` + theme files
- Build orchestration/staging: `Build/Installer.targets`

---

## Evidence conventions

Use an evidence folder per run:

- `C:\Temp\FwInstallerEvidence\YYYY-MM-DD\<scenario>\`

Capture:

- Bundle log(s)
- MSI verbose log(s)
- Screenshots for UX claims
- Registry exports for key claims
- A short “diff notes” file listing any gaps found

Local development PC lane (manual/scripted):

- Run installer(s) directly on the developer machine.
- Capture logs using the standard helper scripts or manual `msiexec`/bundle `/log` flags.
- Evidence is written under `Output\InstallerEvidence\` and can be published to `specs/001-wix-v6-migration/evidence/`.

---

## Procedure

### 1) Establish the baseline and the current target

Decide and record:

- Baseline git ref: `release/9.3` (default) or `6a2d976e` (if required)
- Target branch: `001-wix-v6-migration`

### 2) Build artifacts (target branch)

Use the repo build entrypoint:

- Build (WiX 6): `./build.ps1 -BuildInstaller -Configuration Release -InstallerToolset Wix6`

Record:

- The artifact paths (MSI + bundle) and hashes (SHA256).

### 3) Build pipeline parity review (source parity)

This is a review step: the goal is to identify **implicit legacy behavior** and verify it has an explicit equivalent in WiX 6.

Suggested workflow:

- Compare the “what drives the build” between baseline and current:
  - baseline (in-repo): `FLExInstaller/*.wxi` at the selected baseline ref
  - baseline (optional external): `genericinstaller/BaseInstallerBuild/buildMsi.bat`, `buildExe.bat`
  - current: `Build/Installer.targets`, `FLExInstaller/*.wixproj`

Recommended commands (repo-local wrappers preferred):

- Show a baseline file:
  - `./scripts/Agent/Git-Search.ps1 -Action show -Ref origin/release/9.3 -Path FLExInstaller/Fonts.wxi -HeadLines 200`
- Diff a baseline file vs current:
  - `./scripts/Agent/Git-Search.ps1 -Action diff -Ref origin/release/9.3 -Path FLExInstaller/Fonts.wxi`

Output of this step:

- Populate the **Implicit→Explicit Mapping Table** section below.
- Any suspected gaps become follow-up tasks (and should also be noted in `wix3-to-wix6-audit.md`).

### 4) Install behavior parity (VM runs)

Follow `golden-install-checklist.md` for the actual interactive validation runs.

For parity runs on the local development PC:

- Run WiX3 baseline and WiX6 candidate sequentially on the same machine, ensuring uninstall/cleanup between runs.
- Capture logs and screenshots (when needed) for both runs and record any deltas in the parity notes.

Suggested command patterns:

- Bundle (with logs):
  - `FieldWorks.exe /log C:\Temp\FwInstallerEvidence\YYYY-MM-DD\bundle.log`
- MSI (verbose log):
  - `msiexec /i FieldWorks.msi /l*v C:\Temp\FwInstallerEvidence\YYYY-MM-DD\msi-install.log`

Repo helper (preferred when available):

- `./scripts/Agent/Invoke-Installer.ps1 -InstallerType Bundle -Configuration Release -Arguments @('/passive') -IncludeTempLogs`

Output of this step:

- Check off the items in the **Parity Checklist** section below.
- Attach evidence paths.

### 5) Review and sign-off

At minimum, review should include:

- A second person validates the mapping table entries (links/paths correct).
- A second person spot-checks a sample of evidence logs/screenshots for the highest-risk areas:
  - custom actions
  - upgrade
  - env vars and PATH
  - offline bundle story (if claimed)

---

## Context7: WiX command reference lookup (how to use it)

When adding/adjusting steps that use WiX tools (e.g., `wix.exe build`, `wix.exe msi validate`, Burn logging switches), use Context7 to fetch up-to-date official patterns.

Suggested workflow:

1) Resolve the library ID:

- Use the Context7 resolver for “Wix Toolset” / “WixToolset”

2) Fetch docs focused on the command/topic you need:

- Topic examples: `wix.exe build`, `wix.exe msi validate`, `Burn logs`, `bootstrapper /log`, `MSI ICE validation`

3) Update this document with:

- The command syntax used
- What output/log artifacts prove success

---

## Parity Checklist (fill during verification)

### Toolchain / Build

- [ ] Build succeeds without `genericinstaller` checkout present (fresh clone/worktree)
- [ ] Build does not rely on WiX 3 being on PATH
- [x] Build outputs match expected filenames/locations
- [ ] Staging folder contains expected payload roots:
  - [x] `FieldWorks`
  - [x] `FieldWorks_Data`
  - [x] `FieldWorks_L10n`
  - [x] `FieldWorks_Font`

### MSI payload (install result)

- [ ] APPFOLDER contains app binaries after install
- [ ] DATAFOLDER contains data payload after install
- [ ] Localization payload installed (spot-check at least one locale folder)
- [ ] Fonts behavior matches intent:
  - [ ] Fonts installed when missing
  - [ ] Existing fonts not overwritten (registry-gated)
  - [ ] Uninstall does not remove fonts (Permanent)

### UX

- [ ] Dual-directory UI works (custom App + Data paths)
- [ ] Feature selection UI works and affects payload installed
- [ ] Dialog flow navigation behaves correctly

### Registry / shortcuts / protocol / env

- [ ] HKLM registry keys/values written (paths + version)
- [ ] Desktop shortcut(s) behave as expected
- [ ] Start menu shortcuts (docs/tools/help) present
- [ ] `silfw:` protocol registered and test invocation works
- [ ] Environment variables set correctly (including PATH modifications)

### Custom actions

- [ ] CloseApplications CA behaves correctly (no 1603)
- [ ] No modal assertion UI blocks install (debug/dev)
- [ ] Custom actions logs captured and reviewed

### Bundle prerequisites

- [ ] .NET 4.8 prereq behavior correct (download/detect)
- [ ] VC++ prereq behavior correct (download/detect)
- [ ] FLEx Bridge offline prereq behavior correct (detect/install)

### Upgrade / uninstall

- [ ] Major upgrade from previous release works (single ARP entry)
- [ ] Data path restriction behavior on upgrade matches expectations
- [ ] Uninstall removes expected registry values and shortcuts
- [ ] Uninstall restores env vars/PATH appropriately

### Offline story (if claimed)

- [ ] Offline artifact/layout produced reproducibly
- [ ] Offline install succeeds on a disconnected VM

---

## Implicit → Explicit Mapping Table (fill during audit)

Populate one row per behavior/step. Keep entries short and point to the most specific file/target possible.

| Behavior / step (short) | Was implicit before (WiX3 baseline) | How it’s expressed in v6 | Where it lives now (path + identifier) | Status | Notes / evidence |
|---|---|---|---|---|---|
| Example: CA runtime selection | DTF/SfxCA expected `CustomAction.config` present in CA payload (in WiX3-era tooling this was typically ensured by the CA project output) | Explicitly copied to output and packaged | `FLExInstaller/Shared/CustomActions/CustomActions/CustomActions.csproj` (`Content Include="CustomAction.config"` + `CopyToOutputDirectory`) | ☑ | Confirmed in project file; historic example (optional external baseline): `genericinstaller/CustomActions/CustomActions/CustomActions.csproj` copies `CustomAction.config` to output |
| Example: L10n payload inclusion | Legacy pipeline staged + harvested l10n implicitly | Heat harvest + feature reference | `FLExInstaller/wix6/FieldWorks.Installer.wixproj` (`HarvestedL10nFiles`) + `Framework.wxs` feature refs | ☐ | Install file listing |
| Example: Fonts install semantics | Font components existed, relied on legacy schema allowances | Explicit components with registry gating + Permanent | `FLExInstaller/Fonts.wxi` (`ComponentGroup Fonts`) + referenced from `Framework.wxs` | ☐ | Registry + file check |
| Staged payload roots exist | `Build/Installer.targets` defined versioned staging dirs (e.g., `$(SafeApplicationName)_$(MinorVersion)_Build_$(Platform)` + `objects/...` suffixes) | Explicit staging target creates expected roots under `FieldWorks_InstallerInput_<Config>_<Platform>\objects\...` | `Build/Installer.targets` (`StageInstallerInputs`) and `BuildDir\FieldWorks_InstallerInput_Release_x64\objects\*` | ☑ | Verified on 2025-12-17 (Release build) |
| Bundle logs captured | Ad-hoc logging varied by invocation | Standardized helper writes log under repo output | `scripts/Agent/Invoke-Installer.ps1` (bundle log path) | ☑ | `Output\InstallerEvidence\20251217-local-bundle-passive\bundle.log` |
| FLEx Bridge offline prereq (bundle) | Bundle included/offered offline bridge prereq | `ExePackage` is defined in the shared prereqs include and referenced from the bundle chain via `PackageGroupRef` | `FLExInstaller/Shared/Common/Redistributables.wxi` (`PackageGroup FlexBridgeInstaller` + `ExePackage FBInstaller`) and `PackageGroup vcredists` (`PackageGroupRef FlexBridgeInstaller`) | ☑ | Evidence: `Output\InstallerEvidence\flexbridge-parity\bundle.log` contains `Detected package: FBInstaller` and `Planned package: FBInstaller` (still needs clean-VM behavior validation) |
| MSI ICE validation | Typically implicit (or run manually) | Explicit `wix.exe msi validate -sice ICE60 -wx` in build | `FLExInstaller/wix6/FieldWorks.Installer.wixproj` (`WindowsInstallerValidation`) | ☑ | Present in Release build output |
| | | | | | |
| | | | | | |
| | | | | | |
| | | | | | |

---

## Results (fill after running)

- Date: 2025-12-17
- Baseline ref: `release/9.3`
- Target build artifacts (Release):
  - `FLExInstaller\wix6\bin\x64\Release\en-US\FieldWorks.msi` (SHA256 `7AED3302AD3A273DAAA29D97B5829E91E684699C9150803AA26A35E29B415C09`)
  - `FLExInstaller\wix6\bin\x64\Release\en-US\FieldWorks.wixpdb`
  - `FLExInstaller\wix6\bin\x64\Release\FieldWorksBundle.exe` (SHA256 `365901C88C211C1A7019CBCB8EFB54523D212393976DA518A388E42CF1CC7F62`)
  - `FLExInstaller\wix6\bin\x64\Release\FieldWorksBundle.wixpdb`
- Staging root (exists): `BuildDir\FieldWorks_InstallerInput_Release_x64\objects\`
- Evidence folder (local run): `Output\InstallerEvidence\20251217-local-bundle-passive\`
  - Bundle log: `Output\InstallerEvidence\20251217-local-bundle-passive\bundle.log`
- Evidence folder (local run, FLEx Bridge chain verification): `Output\InstallerEvidence\flexbridge-parity\`
  - Bundle log: `Output\InstallerEvidence\flexbridge-parity\bundle.log`
- Summary:
  - Pass: Build artifacts + staging roots present for Release.
  - Pass: Bundle runs with `/log` and captures Burn log (local run).
  - Gaps: Behavioral parity validation on clean VM not yet recorded (local bundle run shows bundle/MSI already present).
  - Follow-ups (task IDs / issues): T067, T049-T057, T063-T065.
