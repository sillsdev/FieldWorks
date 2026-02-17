# Evidence Criteria (WiX 3 → WiX 6 parity)

This document defines **what counts as comparable evidence** and **how to judge parity** when comparing a WiX 3 baseline installer run vs a WiX 6 candidate installer run.

**Sources of truth** (this document is derived from these):
- specs/001-wix-v6-migration/verification-matrix.md
- specs/001-wix-v6-migration/parity-check.md
- specs/001-wix-v6-migration/wix3-to-wix6-audit.md

## Scope

- This is **runtime evidence comparison criteria** (what happened when we ran the installer on a clean VM).
- It does **not** replace build/authoring parity review; see the mapping table in specs/001-wix-v6-migration/parity-check.md.

## Evidence bundle (required artifacts)

For each run folder (baseline and candidate), the evidence is considered complete if it contains:

### 1) Run metadata

- `run-info.txt`
  - Must include mode (Wix3 or Wix6), start timestamp, and the installer path.
- `command.txt`
  - Must include the *exact* command line used.
- `exitcode.txt`
  - Must capture the process exit code.
- `computerinfo.txt`
  - Must capture OS edition/build and enough host facts to establish “same VM image / same patch level”.

### 2) Installer logs (bundle + MSI)

- Bundle log: `*-bundle.log`
- MSI package log: `*-bundle_*_AppMsiPackage.log`

> The exact filenames are not required, but bundle + MSI logs must exist and be readable.

### 3) Software inventory snapshots (ARP / uninstall registry)

- `uninstall-pre.txt`
  - Snapshot taken *before* running the installer.
- `uninstall-post.txt`
  - Snapshot taken after the scenario completes.

> For an install scenario, `uninstall-post.txt` is “after install”. For an uninstall scenario it should be “after uninstall” (and the scenario must be labeled accordingly).

### 4) Supporting `%TEMP%` logs

- A `temp/` folder containing high-signal prereq logs, such as:
  - `dd_vcredist*.log`, `dd_vcredistMSI*.txt`, `dd_vcredistUI*.txt`
  - `FLEx_Bridge_*.log`
  - `msedge_installer.log` / `wmsetup.log` / other prereq setup logs

These logs are “supporting evidence”; parity evaluation should rely primarily on the bundle/MSI logs, using temp logs to explain anomalies.

## Comparison criteria

When comparing two evidence bundles, apply the checks below.

### A) Inputs are comparable (environment parity)

PASS if all are true:
- OS edition family is comparable (e.g., both Windows Pro), same architecture.
- OS build number and display version are the same (or differences are explicitly justified).
- Both runs are from the same “clean checkpoint class” (i.e., both represent a clean machine baseline).

Acceptable differences:
- Different timestamps, log filenames, and temp GUIDs.
- Minor differences in uptime/memory usage.

### B) Invocation is comparable

PASS if:
- The bundle is executed with the intended flags (typical: `/quiet /norestart /log <path>`).
- Both runs use the same scenario type (install vs uninstall vs upgrade).

Acceptable differences:
- The installer path differs (baseline vs candidate payload location).

### C) Bundle-level success

FAIL if any of the following are true:
- Non-zero `ExitCode` in `exitcode.txt`.
- Bundle log shows apply complete with a non-zero result.
- Bundle log indicates a restart is required when the scenario claims `norestart` parity.

PASS if:
- Apply completes successfully (result 0) and `ExitCode=0`.

### D) Package chain and planning parity

PASS if:
- Both bundle logs show the same package IDs in Detect/Plan (names may vary, IDs should match).
- For each package, the planned execute action is compatible with expectations:
  - Required prereqs install when absent.
  - Required prereqs are skipped when already present.

Notes:
- WiX 3 vs WiX 6 Burn logs will differ in formatting/verbosity; compare semantics (package IDs, state, planned execute).

### E) MSI-level success

PASS if:
- MSI log shows successful install with no “return value 3” failure.
- The MSI log indicates the intended install mode (per-machine, quiet/basic UI).

### F) Post-install footprint parity (ARP + prereqs)

PASS if:
- `uninstall-post.txt` contains the expected FieldWorks ARP entries for the scenario.
- Expected prereqs are present after install when they were absent before.

Important nuance:
- **Versions are expected to differ** when comparing “old release baseline” vs “new candidate build”.
- If the goal is “same product version, different toolchain”, then the baseline and candidate must be built from the same source version and should match on DisplayVersion.

### G) Known/expected warnings

Some warnings are acceptable if consistently explained and do not affect outcome.

Example (commonly seen in VMs):
- “Could not create system restore point … Continuing…”

These should be:
- Present in both runs (or justified if only in one), and
- Not accompanied by actual failures.

## What counts as a discrepancy

A discrepancy is anything that changes behavior or user-facing outcome, including:
- Different packages planned/executed (beyond expected modernization).
- Silent failures hidden behind `ExitCode=0` (e.g., MSI rollback in log).
- Missing ARP entries, missing shortcuts, missing registry values, missing protocol/env vars (when those are part of the scenario under test).

## Minimum pass bar for a “clean machine parity lane”

For the parity lane to be meaningful, each run must satisfy:
- Sections A–E are PASS.
- Section F is PASS for at least the core ARP/prereq expectations.

Higher bar (recommended for sign-off) extends Section F to include:
- Install location(s), registry values, shortcuts, URL protocol, env vars, and upgrade/uninstall behavior per specs/001-wix-v6-migration/verification-matrix.md.
