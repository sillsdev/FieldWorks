# Golden Install Checklist (FieldWorks WiX 6)

**Purpose**: A repeatable manual validation script for the WiX 6 installer, designed to generate consistent evidence for review and regression tracking.

**Recommended cadence**: Run this checklist at least once per major installer change and before release candidate tagging.

## Test environment

- VM: Windows (clean snapshot recommended)
- Snapshot names (suggested):
  - `FW-Clean`
  - `FW-HasPrereqs` (VC++ + .NET already installed)
  - `FW-HasOldFieldWorks` (a prior released FieldWorks installed)
  - `FW-Offline` (no internet access)
- Evidence folder: `C:\Temp\FwInstallerEvidence\YYYY-MM-DD\`

## Artifacts under test

- Bundle: `FieldWorks.exe`
- MSI: `FieldWorks.msi`
- Optional: offline layout / offline bundle artifact (if supported)

Record exact artifact paths + hashes:
- ☐ Bundle path + SHA256:
- ☐ MSI path + SHA256:

## Logging setup

Preferred:
- Bundle log: `FieldWorks.exe /log C:\Temp\FwInstallerEvidence\bundle.log`
- MSI log: `msiexec /i FieldWorks.msi /l*v C:\Temp\FwInstallerEvidence\msi-install.log`

If bundle log switch differs, capture `%TEMP%` logs and record filenames here:
- ☐ Bundle log path(s):

## Scenario A: Online clean install (primary)

Run on snapshot: `FW-Clean` (internet enabled)

1) Launch bundle with logging
- ☐ Bundle UI opens
- ☐ License/branding/theme appears correct

2) Install directories
- ☐ Choose non-default App directory
- ☐ Choose non-default Data directory
- ☐ Install completes successfully

3) Feature selection
- ☐ Select a non-default feature set (document what you chose)
- ☐ Verify installed payload matches feature selection

4) Post-install verification
- ☐ App files exist under chosen App directory
- ☐ Data files exist under chosen Data directory
- ☐ Desktop shortcut exists (if expected)
- ☐ Start menu shortcuts exist (docs/tools/help)
- ☐ `silfw:` protocol registered (verify registry + a simple invocation)
- ☐ Environment variables are set as expected (including PATH modifications)
- ☐ Registry keys/values exist under the expected HKLM location (paths + version)

Evidence to attach:
- ☐ `bundle.log`
- ☐ `msi-install.log`
- ☐ Screenshots of directory selection + feature selection
- ☐ Registry exports (before/after) for relevant keys

## Scenario B: Online install with prereqs already present (detection)

Run on snapshot: `FW-HasPrereqs`

- ☐ Bundle detects prereqs and skips downloads/installs where appropriate
- ☐ Install succeeds and behaves the same as Scenario A

Evidence:
- ☐ `bundle.log` showing detection decisions

## Scenario C: Major upgrade from previous released version

Run on snapshot: `FW-HasOldFieldWorks`

1) Confirm baseline
- ☐ Prior FieldWorks version installed and launches
- ☐ Record version number

2) Run new bundle
- ☐ Upgrade path proceeds without side-by-side installs
- ☐ Any data directory lock/restriction behavior matches expectations

3) Post-upgrade checks
- ☐ Single ARP entry for FieldWorks as expected
- ☐ App launches and existing projects/data remain intact
- ☐ Registry values updated to new version

Evidence:
- ☐ Upgrade bundle/MSI logs
- ☐ Screenshot of ARP showing installed version

## Scenario D: Uninstall

Run after Scenario A or C

- ☐ Uninstall succeeds without errors
- ☐ Registry keys removed (as expected)
- ☐ Environment variables removed/restored (as expected)
- ☐ Shortcuts removed

Evidence:
- ☐ Uninstall logs
- ☐ Registry/env snapshots before/after

## Scenario E: Offline install

Run on snapshot: `FW-Offline` (no internet)

Precondition:
- ☐ Offline artifact/layout is available locally

- ☐ Install completes without requiring network access
- ☐ Prereqs are available from offline media/layout

Evidence:
- ☐ Offline install logs
- ☐ Proof network disabled (note VM settings)

## Scenario F: Localization smoke test (at least one locale)

- ☐ Build/run installer in one non-English locale
- ☐ Installer UI strings load correctly
- ☐ Install succeeds

Evidence:
- ☐ Screenshots of localized UI
- ☐ Build artifact listing

## Results summary

- Date:
- Tester:
- VM images/snapshots used:
- Pass/fail summary:
- Issues found (link to issues/notes):
