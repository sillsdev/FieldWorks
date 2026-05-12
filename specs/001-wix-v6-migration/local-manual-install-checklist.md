# Local Manual Installer Validation Checklist

**Purpose**: Capture repeatable local development PC evidence for WiX 3 and WiX 6 installer runs when no clean VM or sandbox is available.

Use this checklist only for local-dev validation. Clean-VM evidence is still required before release decisions.

## Evidence Folder

Use `Output/InstallerEvidence/<RunId>/` for repo-local evidence. Recommended run ID format:

```powershell
20260430-winapp-mcp-manual-wix6-install
```

For every run, capture:

- `before-install.json` from `scripts/Agent/Collect-InstallerSnapshot.ps1`.
- Bundle log from the tested bundle, for example `bundle.log`.
- MSI verbose log discovered from `WixBundleLog_AppMsiPackage` or written by `msiexec /l*v`.
- Screenshots of bundle welcome/license, MSI welcome, directory selection, feature selection, progress, and completion where reachable.
- `blocked-state.json` or `after-install.json`, depending on whether the run blocks or completes.
- Snapshot comparison output from `scripts/Agent/Compare-InstallerSnapshots.ps1`.

## WinApp MCP Rules

WinApp MCP is useful for local installer evidence, but process elevation matters.

- Non-elevated VS Code/WinApp MCP can drive the WiX 6 bundle before elevation: license checkbox, Install, Cancel, screenshots, focused element checks.
- Once the bundle launches the elevated MSI internal UI, a non-elevated automation host may only observe the MSI dialog. It may report focus on `&Next` and capture screenshots, while click, Enter, coordinate click, and direct Win32 button messages do not advance the dialog.
- For full manual MSI UI validation through WinApp MCP, run the automation host elevated before launching the bundle, or use a VM where the automation host and installer run at compatible integrity levels.
- If full UI input is blocked, stop the run before MSI execute when possible, capture `blocked-state.json`, and compare snapshots so machine changes are explicit.
- For install/uninstall completion without full UI, use the quiet evidence wrapper: `scripts/Agent/Invoke-InstallerCheck.ps1` with explicit `-InstallerToolset Wix6`, quiet arguments, uninstall arguments, and timeouts.

## WiX 6 Local Full UI Attempt

1. Create evidence folder:

```powershell
$runId = 'YYYYMMDD-winapp-mcp-manual-wix6-install'
$evidenceDir = Join-Path 'Output\InstallerEvidence' $runId
New-Item -ItemType Directory -Force -Path $evidenceDir
```

2. Capture baseline snapshot:

```powershell
./scripts/Agent/Collect-InstallerSnapshot.ps1 -OutputPath (Join-Path $evidenceDir 'before-install.json') -Name 'before-winapp-manual-wix6-install'
```

3. Launch the Release WiX 6 bundle with logging:

```powershell
./FLExInstaller/wix6/bin/x64/Release/FieldWorksBundle.exe /log "$((Resolve-Path $evidenceDir).Path)\bundle.log"
```

4. Use WinApp MCP to capture and drive:

- Bundle license screen.
- License checkbox accepted.
- Install button reached and activated.
- MSI welcome screen after internal UI handoff.
- Directory and feature pages if the automation host can drive elevated MSI UI.

5. If blocked at MSI full UI, capture:

```powershell
./scripts/Agent/Collect-InstallerSnapshot.ps1 -OutputPath (Join-Path $evidenceDir 'blocked-state.json') -Name 'blocked-winapp-manual-wix6-install'
./scripts/Agent/Compare-InstallerSnapshots.ps1 -BeforeSnapshotPath (Join-Path $evidenceDir 'before-install.json') -AfterSnapshotPath (Join-Path $evidenceDir 'blocked-state.json')
```

6. If completed, capture after-install and after-uninstall snapshots and compare both transitions.

## Current Evidence

- `Output/InstallerEvidence/20260430-winapp-mcp-manual-wix6-install/`: WiX 6 Release bundle launched with WinApp MCP, bundle license accepted, Install activated, FLExBridge prerequisite installed, MSI internal UI reached `GIWelcomeDlg`, but non-elevated automation could not advance the elevated MSI `&Next` button. The aborted run left one added Burn dependency provider, `{BDFC2A1E-094B-45A3-ADC4-B681631B5828}_v9.3.9.1`, with no tracked file, uninstall entry, or registry-value changes.