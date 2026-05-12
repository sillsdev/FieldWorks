---
name: fieldworks-installer-diagnostics
description: Use this skill for FieldWorks installer logging, diagnostics, evidence collection, and failure triage: double-click does nothing, bundle exits or hangs, uninstall hangs, ARP issues, prerequisite failures, custom action failures, MSI Return value 3, Burn detect/plan/apply analysis, Event Viewer/crash dumps, VM evidence folders, and before/after installer snapshots. Trigger for any FieldWorks installer debugging or diagnostic request.
---

# FieldWorks Installer Diagnostics

This skill is evidence-first. Installer problems are often ambiguous until Burn logs, MSI logs, and machine state snapshots are collected.

## Load References When Needed

- Read `references/evidence-and-commands.md` for exact commands and evidence locations.
- Read `references/log-triage.md` for Burn/MSI/custom action triage recipes.

## First Moves

1. Read `.github/instructions/installer.instructions.md` and `.github/instructions/debugging.instructions.md`.
2. Read `scripts/Agent/Invoke-Installer.ps1`, `Invoke-InstallerCheck.ps1`, `Collect-InstallerSnapshot.ps1`, and `Compare-InstallerSnapshots.ps1` if changing or using diagnostics scripts.
3. Read `specs/001-wix-v6-migration/verification-matrix.md`, `golden-install-checklist.md`, and `REMAINING_WIX6_ISSUES.md` for the expected evidence shape.
4. Identify which artifact is under test: WiX 3 fallback/current default or WiX 6 migration path. Do not assume helper defaults point at WiX 6.

## Evidence-First Workflow

1. Create or choose an evidence folder. Repo convention: `Output/InstallerEvidence/<RunId>/` for agent scripts, or `C:\Temp\FwInstallerEvidence\YYYY-MM-DD\` for VM/manual runs.
2. Capture the command, artifact path, version, SHA256 if relevant, and machine snapshot/VM state.
3. Run the bundle or MSI with logging. For WiX 6 artifacts, pass explicit `-InstallerPath` when using helper scripts.
4. Include temp logs when chained packages or Burn package logs may be separate.
5. If the UI exits silently, capture Event Viewer entries and crash dumps.
6. Summarize the first failing layer: Burn bootstrapper, chained package/MSI, custom action, Windows Installer engine, or environment.

## WinApp MCP Runtime Evidence

When the WinApp MCP server is available, use it to make UI diagnostics repeatable:

- Launch or attach to installer windows and wait for input idle before diagnosing UI state.
- If the app snapshot is empty, list visible desktop windows and use `get_focused_element` plus keyboard navigation to identify and exercise controls.
- Record the observed focus path and control names in the evidence notes, especially for silent/blank UI, MSI handoff, uninstall prompts, and ARP uninstall hangs.
- For safe UI-only smoke tests, stop after proving the license checkbox/Install/Cancel path; do not proceed into install unless the scenario requires machine changes.
- For hangs, combine WinApp focused-element/window evidence with bundle/MSI logs, before/after snapshots, Event Viewer entries, and process lists.
- If the bundle reaches elevated MSI internal UI, a non-elevated WinApp host may be limited to observation. Treat visible-but-non-clickable MSI controls as an automation integrity boundary until proven otherwise; capture the focused element, MSI log action, screenshots, process integrity/window ownership evidence, and rerun elevated for full manual UI control.

## Commands

Prefer the repo helper for repeatable runs:

```powershell
./scripts/Agent/Invoke-Installer.ps1 -InstallerType Bundle -InstallerPath '.\FLExInstaller\wix6\bin\x64\Debug\FieldWorksBundle.exe' -IncludeTempLogs
./scripts/Agent/Invoke-Installer.ps1 -InstallerType Msi -InstallerPath '.\FLExInstaller\wix6\bin\x64\Debug\en-US\FieldWorks.msi' -IncludeTempLogs
```

Use explicit manual commands when needed:

```powershell
.\FLExInstaller\wix6\bin\x64\Debug\FieldWorksBundle.exe /log C:\Temp\FwInstallerEvidence\bundle.log
msiexec /i .\FLExInstaller\wix6\bin\x64\Debug\en-US\FieldWorks.msi /l*v C:\Temp\FwInstallerEvidence\msi-install.log
msiexec /x {PRODUCT-CODE} /l*v C:\Temp\FwInstallerEvidence\msi-uninstall.log
```

For before/after evidence:

```powershell
./scripts/Agent/Collect-InstallerSnapshot.ps1 -OutputPath C:\Temp\FwInstallerEvidence\before.json -Name before
./scripts/Agent/Collect-InstallerSnapshot.ps1 -OutputPath C:\Temp\FwInstallerEvidence\after.json -Name after
./scripts/Agent/Compare-InstallerSnapshots.ps1 -BeforeSnapshotPath C:\Temp\FwInstallerEvidence\before.json -AfterSnapshotPath C:\Temp\FwInstallerEvidence\after.json
```

## Triage Priorities

- Bundle log first for Burn detection, prerequisite planning, cache/download failures, related bundle handling, and package log paths.
- MSI verbose log first for directory properties, feature states, standard action sequencing, and custom action failures.
- Search MSI logs for `Return value 3`, then walk upward to the failing action and property context.
- Search bundle logs for `Detected package`, `Planned package`, `Applying execute package`, `Error 0x`, and final result code.
- For custom actions, find the WiX action ID in `Framework.wxs`, then map it to the DllEntry in `CustomAction.cs`.
- For hangs, identify the last completed phase/action and whether a UI prompt, process-close prompt, files-in-use dialog, or elevation prompt is waiting.

## Report Format

When reporting diagnostics, include:

- Artifact tested and command used.
- Evidence folder and primary logs.
- Failure layer and first meaningful error.
- Relevant Burn/MSI action/package names.
- Machine state: clean, prereqs present, old FieldWorks installed, offline, upgrade, repair, or uninstall.
- Next smallest fix or next evidence needed.
