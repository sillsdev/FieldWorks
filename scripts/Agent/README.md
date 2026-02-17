# Agent Wrapper Scripts

PowerShell scripts for Copilot auto-approval. Use these instead of commands with pipes (`|`), `&&`, or `2>&1`.

## Scripts

| Script | Purpose |
|--------|---------|
| `Git-Search.ps1` | Git operations (show, diff, log, grep, blame) |
| `Read-FileContent.ps1` | Read files with head/tail/pattern filtering |
| `Invoke-Installer.ps1` | Run MSI or bundle and collect logs under Output/InstallerEvidence |
| `Collect-InstallerSnapshot.ps1` | Capture a machine snapshot (registry/files/uninstall entries) for installer verification |
| `Compare-InstallerSnapshots.ps1` | Compare two snapshots and write a plain-text diff report |
| `Invoke-InstallerCheck.ps1` | Orchestrate snapshot → install → snapshot (+ optional uninstall) for installer verification |
| `New-HyperVTestVm.ps1` | Create a Hyper-V Windows test VM from an ISO (for deterministic parity runs) |
| `Get-WindowsIso.ps1` | Download a Windows ISO to a shared cache (optionally using an external Fido.ps1 to obtain the URL) |
| `New-HyperVCleanCheckpoint.ps1` | Create a named clean checkpoint on a Hyper-V VM |
| `New-HyperVCleanBaseline.ps1` | Recreate the clean baseline checkpoint (optionally removes automatic checkpoints) |
| `Get-HyperVTestVmInfo.ps1` | List Hyper-V VMs or show snapshots for a specific VM |
| `Set-HyperVGuestCredential.ps1` | Prompt once and cache the VM admin credential locally (DPAPI encrypted) |

## Usage

```powershell
Get-Help .\scripts\Agent\Git-Search.ps1 -Full
```
