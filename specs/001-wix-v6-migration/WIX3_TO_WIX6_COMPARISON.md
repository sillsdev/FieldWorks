# WiX3 → WiX6 Evidence Comparison (temp/wix3 vs temp/wix6)

This report compares the two provided evidence dumps:
- Baseline: `temp/wix3`
- Candidate: `temp/wix6`

Comparison criteria: `EVIDENCE_CRITERA.md` (derived from specs/001-wix-v6-migration/verification-matrix.md, specs/001-wix-v6-migration/parity-check.md, specs/001-wix-v6-migration/wix3-to-wix6-audit.md)

## Evidence inventory

Both evidence folders contain the required baseline artifacts:
- `command.txt`
- `computerinfo.txt`
- `exitcode.txt`
- `run-info.txt`
- `uninstall-pre.txt`
- `uninstall-post.txt`
- Bundle log: `wix*-bundle.log`
- MSI package log: `wix*-bundle_006_AppMsiPackage.log`
- Supporting temp logs under `temp/`

## High-level outcome

- Both runs completed successfully with `ExitCode=0`.
- Both runs executed an 8-package chain (NetFx48 + VC runtimes + FLEx Bridge + FieldWorks MSI).
- The candidate run uses Burn v6 (expected modernization); the baseline uses Burn v3.11.

## Per-criterion assessment

### A) Inputs are comparable (environment parity)

Status: PASS

- Both runs are on the same VM identity (`CsName` appears as `TEST`) and same OS line:
  - Windows 11 Pro, build 26200.
- Small differences exist in captured fields (e.g., reported total physical memory and username presence), but nothing indicates the machine image materially differs.

Evidence:
- `temp/wix3/computerinfo.txt`
- `temp/wix6/computerinfo.txt`

### B) Invocation is comparable

Status: PASS

Both runs are quiet install runs with logging enabled:
- Baseline: `...Wix3Baseline.exe /quiet /norestart /log ...\wix3-bundle.log`
- Candidate: `...Wix6Candidate.exe /quiet /norestart /log ...\wix6-bundle.log`

Evidence:
- `temp/wix3/command.txt`
- `temp/wix6/command.txt`

### C) Bundle-level success

Status: PASS

- Both runs report `ExitCode=0`.
- Both bundle logs show apply completion with `result: 0x0`.

Evidence:
- `temp/wix3/exitcode.txt`
- `temp/wix6/exitcode.txt`
- `temp/wix3/wix3-bundle.log` (contains `i399: Apply complete, result: 0x0`)
- `temp/wix6/wix6-bundle.log` (contains `i399: Apply complete, result: 0x0`)

### D) Package chain and planning parity

Status: PASS (with expected modernization differences)

Both logs show:
- `Detect begin, 8 packages`
- Detected package IDs match (NetFx48Web, vc8, vc10, vc11, vc12, vc14, FBInstaller, AppMsiPackage)
- Planned action: install prereqs and MSI on a clean box

Notable difference (modernization / logging semantics):
- Baseline Burn v3 shows `cache: Yes/No` with fewer fields.
- Candidate Burn v6 shows `cache: Vital`, plus explicit cache strategy and registration-state fields.

Evidence:
- `temp/wix3/wix3-bundle.log` (Detect/Plan sections)
- `temp/wix6/wix6-bundle.log` (Detect/Plan sections)

### E) MSI-level success

Status: PASS

Both MSI logs show a first-time install of a cached MSI under `C:\ProgramData\Package Cache\{GUID}v<version>\...`.

Notable differences:
- MSI filename in the cache differs:
  - WiX3 run: `FieldWorks_9.2.11.1.msi`
  - WiX6 run: `FieldWorks.msi`
  This looks like a packaging/name change, not a functional failure.

Evidence:
- `temp/wix3/wix3-bundle_006_AppMsiPackage.log`
- `temp/wix6/wix6-bundle_006_AppMsiPackage.log`

### F) Post-install footprint parity (ARP + prereqs)

Status: PARTIAL (because the two runs intentionally install different FieldWorks versions)

What matches:
- `uninstall-pre.txt` is identical in both runs (Edge + WebView2 present).
- Both post snapshots show:
  - FieldWorks installed
  - FLEx Bridge installed
  - VC++ runtimes installed (2008/2010/2012/2013/2019 family entries)

What differs:
- FieldWorks DisplayVersion differs:
  - WiX3: FieldWorks 9.2.11.1
  - WiX6: FieldWorks 9.9.1.1

Interpretation:
- If the intent of this lane is “**old release vs new build**”, then the version delta is expected and this criterion should focus on presence/shape of entries.
- If the intent is “**same product bits, different toolchain**”, then this comparison is not strict enough; you’ll want to run baseline and candidate from the same product version so DisplayVersion can be compared directly.

Evidence:
- `temp/wix3/uninstall-pre.txt` / `temp/wix6/uninstall-pre.txt`
- `temp/wix3/uninstall-post.txt` / `temp/wix6/uninstall-post.txt`

### G) Known/expected warnings

Status: PASS

Both bundle logs contain:
- `w363: Could not create system restore point, error: 0x80070422. Continuing...`

This appears consistent across runs and did not prevent success.

Evidence:
- `temp/wix3/wix3-bundle.log`
- `temp/wix6/wix6-bundle.log`

## Notable modernizations / expected differences

- Burn engine:
  - WiX3 baseline uses Burn v3.11.2.
  - WiX6 candidate uses Burn v6.0.0.
  This is an expected modernization and will change log format/details.

- Bundle version variable differs as expected:
  - `WixBundleVersion = 9.2.11.1` (WiX3)
  - `WixBundleVersion = 9.9.1.1` (WiX6)

Evidence:
- `temp/wix3/wix3-bundle.log` (variable section)
- `temp/wix6/wix6-bundle.log` (variable section)

## Evidence gaps (not present in these dumps)

This comparison is limited to silent install + prereq + MSI completion and ARP snapshots.

Not present here (so not evaluated):
- Interactive UX evidence (dual-directory UI, feature selection UI, dialog flow screenshots)
- Registry exports for key FieldWorks HKLM/HKCR values
- Shortcut/link verification
- URL protocol verification
- Environment variable/PATH verification
- Upgrade and uninstall scenario evidence

These are tracked as verification requirements in specs/001-wix-v6-migration/verification-matrix.md and specs/001-wix-v6-migration/parity-check.md.

## TODOs (follow-ups for unexplained or incomplete items)

1) Decide the strictness goal of the parity lane:
   - If “same product version” parity is required, re-run WiX3 baseline and WiX6 candidate using installers built from the same FieldWorks version so ARP version comparison is meaningful.

2) Add (or capture) additional evidence required for sign-off:
   - Registry exports (install + uninstall)
   - Shortcut and protocol verification
   - Environment variable/PATH snapshots
   - Upgrade run evidence (old → new)
   - Uninstall run evidence (and post-uninstall snapshots)

3) Investigate candidate-only extra logs:
   - `temp/wix6/temp/StructuredQuery.log`
   - `temp/wix6/temp/FieldWorks_Language_Explorer_9.9_20251229085611.elevated.log`
   Confirm whether these are expected additions from the newer version/tooling or indicate new behavior worth documenting.
