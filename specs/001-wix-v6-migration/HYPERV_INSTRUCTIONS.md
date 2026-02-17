# Hyper-V Installer Parity: Manual Staging + Evidence (Guest Service Interface)

This is the simplest “I only need a few runs” path for comparing the WiX3 baseline installer vs the WiX6 candidate installer.

Instead of PowerShell Direct (`New-PSSession -VMName ...`), it uses the Hyper-V **Guest Service Interface** to copy files into the guest, and then you run the installer + evidence capture script manually inside the VM.

## Prerequisites

- Hyper-V VM: `FwInstallerTest`
- Clean checkpoint: your clean baseline checkpoint (use `Get-VMSnapshot -VMName FwInstallerTest` to list names)
- Hyper-V Integration Service enabled: **Guest Service Interface**
- WiX6 candidate bundle built on the host:
  - `FLExInstaller\bin\x64\Release\FieldWorksBundle.exe`
- WiX3 baseline installer present on the host (default expected path):
  - `C:\ProgramData\FieldWorks\HyperV\Installers\Wix3Baseline\FieldWorks_9.2.11.1_Online_x64.exe`

## What the helper scripts do

- Host-side copier: `scripts\Agent\Copy-HyperVParityPayload.ps1`
  - Copies the payload files into the VM:
    - WiX3 baseline EXE
    - WiX6 candidate EXE
    - Guest evidence scripts (single-entry + double-click wrappers)
- Guest evidence scripts:
  - Single-entry (CLI-friendly): `scripts\Agent\Guest\Invoke-InstallerParityEvidence.ps1`
  - Double-click friendly:
    - `scripts\Agent\Guest\Invoke-InstallerParityEvidence-Wix3.ps1`
    - `scripts\Agent\Guest\Invoke-InstallerParityEvidence-Wix6.ps1`
  - Shared implementation: `scripts\Agent\Guest\InstallerParityEvidence.Common.ps1`

## Run loop (repeat per attempt)

Fast path (host helper):

```powershell
./scripts/Agent/Start-HyperVManualParityRun.ps1 -VMName FwInstallerTest -CheckpointName initial -ForceRestore

# If your checkpoint is named differently (e.g., "installer"), use that name:
# ./scripts/Agent/Start-HyperVManualParityRun.ps1 -VMName FwInstallerTest -CheckpointName installer -ForceRestore
```

This restores the checkpoint, boots the VM, and stages the payload into the guest.

## One-time VM prep (recommended): allow local PowerShell scripts

If you see errors like “running scripts is disabled on this system” when you Shift+Right-click → **Run with PowerShell**, that’s Windows PowerShell’s **execution policy**.

Do this once inside the VM, then take (or update) your clean checkpoint so you don’t have to fight it again.

Inside the VM (run an elevated PowerShell):

```powershell
Get-ExecutionPolicy -List

# Recommended: allow local scripts; still blocks unsigned scripts that came from the internet.
Set-ExecutionPolicy -Scope LocalMachine -ExecutionPolicy RemoteSigned -Force

# Optional: also set CurrentUser (helps if MachinePolicy is not in play)
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned -Force

Get-ExecutionPolicy -List
```

Notes:

- `RemoteSigned` is usually the best tradeoff for a test VM: locally-created/copied scripts run; “downloaded from the internet” scripts still require unblocking/signing.
- If `MachinePolicy` is set, Group Policy is enforcing it; you’ll need to change the policy (or set it to Not Configured) to make `Set-ExecutionPolicy` stick.
- ExecutionPolicy is not a security boundary, but it *does* stop double-click/Run-with-PowerShell flows unless you set it.

After setting the policy:

1. Apply your clean checkpoint baseline (or clean the VM)
2. Create/update the clean checkpoint (so the policy change is preserved)

For your setup, this is typically a checkpoint like `initial` or `installer`.

### 1) Restore the checkpoint and start the VM

In Hyper-V Manager:

1. Select `FwInstallerTest`
2. Apply checkpoint `fresh-install`
3. Start the VM

Wait until Windows reaches the logon screen.

### 2) Copy baseline + candidate + script into the guest (host → guest)

From repo root on the host:

```powershell
./scripts/Agent/Copy-HyperVParityPayload.ps1 -VMName FwInstallerTest
```

This prints a `GuestRunRoot` like:

- `C:\FWInstallerTest\ParityPayload\<runId>`

and places these files there:

- `Wix3Baseline.exe`
- `Wix6Candidate.exe`
- `Invoke-InstallerParityEvidence.ps1`
- `Invoke-InstallerParityEvidence-Wix3.ps1`
- `Invoke-InstallerParityEvidence-Wix6.ps1`
- `InstallerParityEvidence.Common.ps1`

If your installer paths differ, pass overrides:

```powershell
./scripts/Agent/Copy-HyperVParityPayload.ps1 `
  -VMName FwInstallerTest `
  -Wix3BaselineInstallerPath "C:\Path\To\FieldWorks_9.2.11.1_Online_x64.exe" `
  -Wix6CandidateInstallerPath "FLExInstaller\bin\x64\Release\FieldWorksBundle.exe"
```

### 3) Run the evidence script inside the VM

You have two ways to run inside the VM:

1) Shift+Right-click a mode-specific script and choose **Run with PowerShell** (it pauses at the end)
2) Run from an elevated PowerShell prompt

Adjust `<runId>` to the folder printed by the copy script:

Option A (Shift+Right-click → Run with PowerShell):

- `C:\FWInstallerTest\ParityPayload\<runId>\Invoke-InstallerParityEvidence-Wix3.ps1`
- `C:\FWInstallerTest\ParityPayload\<runId>\Invoke-InstallerParityEvidence-Wix6.ps1`

Option B (elevated PowerShell):

```powershell
$payload = 'C:\FWInstallerTest\ParityPayload\<runId>'
Set-ExecutionPolicy -Scope Process Bypass -Force

& "$payload\Invoke-InstallerParityEvidence-Wix3.ps1"

# For clean parity, restore the checkpoint again before the Wix6 run.

& "$payload\Invoke-InstallerParityEvidence-Wix6.ps1"
```

Each run prints a zip path like:

- `EvidenceZip=C:\FWInstallerTest\evidence-<Mode>-<timestamp>.zip`

### 4) Copy evidence out of the VM

Hyper-V Guest Services are host→guest only (`Copy-VMFile`), so copy the zip back to the host using one of:

- Enhanced Session Mode (clipboard + drive redirection)
- Remote Desktop (clipboard + drive redirection)
- SMB share (if you enabled networking)
- A “transfer disk” VHDX mounted in guest then mounted on host

#### Clipboard / drive sharing prerequisites

- Use a **Windows Pro** (or Enterprise) guest. Windows Home cannot act as an RDP host, which makes Remote Desktop (and thus easy clipboard/drive redirection) unavailable.
- Enable **Enhanced Session Mode** on the host:
  - Hyper-V Manager → Hyper-V Settings… → Enhanced Session Mode Policy → **Allow enhanced session mode**
  - Hyper-V Manager → Hyper-V Settings… → Enhanced Session Mode → **Use enhanced session mode**
- Enable **Remote Desktop** in the guest:
  - Settings → System → Remote Desktop → **Enable Remote Desktop**
  - Ensure the user you log in with is allowed to connect (Administrators are allowed by default)
  - If prompted, allow Remote Desktop through Windows Firewall
- Windows Hello Conflict: If using Windows 10/11, go to Settings > Accounts > Sign-in options and turn off the requirement for "Windows Hello sign-in for Microsoft accounts".

Suggested host destination:

- `Output\InstallerEvidence\HyperV\Manual\<runId>\evidence-*.zip`

## Notes

- For true “clean machine” parity, restore `fresh-install` before each run.
- This manual path does not uninstall FieldWorks automatically.
