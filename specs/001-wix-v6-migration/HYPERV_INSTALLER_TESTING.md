# Hyper-V Installer Parity Testing (Deterministic)

This document describes the deterministic clean-machine verification lane for comparing WiX3 vs WiX6 installer behavior using a Hyper-V VM restored from a checkpoint.

## Prerequisites

- Windows Pro/Enterprise with Hyper-V enabled.
- A local Hyper-V VM (Windows 10/11 Pro/Enterprise or Server) that supports PowerShell Direct.
- A checkpoint created for a clean baseline state (no FieldWorks installed).
- Local admin credentials inside the VM (PowerShell Direct).

Host permissions:

- Your Windows user should be in the local `Hyper-V Administrators` group to manage VMs without running everything elevated.
- Some host-side disk diagnostics (e.g., `Mount-VHD` while debugging a broken VM boot) require an elevated PowerShell.

To set up the recommended host permissions (idempotent):

```powershell
./scripts/Agent/Setup-HyperVHostPermissions.ps1
```

If it adds you to a group, log off/on (or reboot) before rerunning parity, otherwise existing processes may not see the new membership.

## One-time VM setup

If you do not already have a Hyper-V VM, you can create one from a Windows ISO.

Note: there is no built-in way to obtain a "windows-latest" image locally; you must provide a Windows 10/11 ISO (or an evaluation ISO) and complete Windows setup in the VM.

### Optional: download + cache an ISO

If you already have a direct ISO URL (for example, from a Microsoft download flow), you can cache it locally:

```powershell
./scripts/Agent/Get-WindowsIso.ps1 -IsoUrl "https://example.invalid/Windows.iso"
```

If you use Fido to obtain the download URL, do not vendor it into this repo (Fido is GPLv3). Instead, download it separately and point the helper at your local copy:

```powershell
./scripts/Agent/Get-WindowsIso.ps1 -FidoPath "C:\Tools\Fido.ps1" -Win "Windows 11" -Rel Latest -Lang "English" -Arch x64
```

Alternatively, you can let the helper auto-download Fido into a local cache folder (not committed). This requires an explicit GPL acknowledgement:

```powershell
./scripts/Agent/Get-WindowsIso.ps1 -AutoDownloadFido -AllowGplFido -Win "Windows 11" -Rel Latest -Lang "English" -Arch x64
```

Tip: run Fido with `-Lang List` to see valid language values.

By default, ISOs are cached under `%ProgramData%\FieldWorks\HyperV\ISOs`.

```powershell
# Create the VM from an ISO (Generation 2)
./scripts/Agent/New-HyperVTestVm.ps1 -VMName FWInstallerTest -IsoPath "C:\Path\To\Windows.iso" -VhdSizeGB 80 -MemoryStartupBytes 4294967296 -ProcessorCount 2 -StartVm
```

Alternatively, if you have a direct ISO URL, you can have the script download into the cache automatically:

```powershell
./scripts/Agent/New-HyperVTestVm.ps1 -VMName FWInstallerTest -IsoUrl "https://example.invalid/Windows.iso" -StartVm
```

If you do not have a direct ISO URL, you can have the VM creation script use Fido to obtain one and download the ISO (Fido is GPLv3; not vendored here). This requires an explicit acknowledgement:

```powershell
./scripts/Agent/New-HyperVTestVm.ps1 -VMName FWInstallerTest -UseFido -AllowGplFido -StartVm
```

Note: do not delete the ISO until Windows installation is complete and the VM no longer needs to boot from DVD.

1) Create a Windows VM in Hyper-V.
2) Boot it and complete OOBE.
3) Ensure WinRM/PowerShell is usable (PowerShell Direct does not require networking, but the guest must be running Windows).
4) Create a clean checkpoint:

- Example VM name: `FWInstallerTest`
- Example checkpoint name: `FWInstallerTest_Clean`

You can create the checkpoint using the helper script (recommended after Windows setup is complete and before installing FieldWorks):

```powershell
./scripts/Agent/New-HyperVCleanCheckpoint.ps1 -VMName FWInstallerTest -CheckpointName FWInstallerTest_Clean -StopVmFirst
```

If the VM boots to the Hyper-V UEFI screen with **"No operating system was loaded"**, your OS disk is not bootable (often it is still RAW/unpartitioned because Windows was never installed). Fix by completing Windows installation inside the VM, then recreate the clean checkpoint.

### Recreate the clean baseline checkpoint

If the baseline checkpoint gets "dirty" (e.g., the VM was changed outside of the parity run), recreate it.

1) Restore the VM to a known-good state (manually, via Hyper-V Manager), and ensure FieldWorks is not installed.
2) Then run:

```powershell
./scripts/Agent/New-HyperVCleanBaseline.ps1 -VMName FWInstallerTest -CheckpointName FWInstallerTest_Clean -StopVmFirst -Replace -RemoveAutomaticCheckpoints -DisableAutomaticCheckpoints
```

Note: `New-HyperVCleanCheckpoint.ps1` / `New-HyperVCleanBaseline.ps1` now perform a host-side OS disk sanity check (and will refuse to checkpoint a RAW/uninitialized disk). If you are not running elevated, they will warn and skip that check.

If you need to set up host permissions:

```powershell
./scripts/Agent/Setup-HyperVHostPermissions.ps1
```

Notes:
- This script only creates/removes checkpoints; it cannot automate Windows OOBE or ensure your VM is "clean".
- If you prefer to keep automatic checkpoints, omit `-RemoveAutomaticCheckpoints`.

## Run parity (WiX3 baseline vs WiX6 candidate)

If you only need a handful of runs (e.g., ~1–5) and PowerShell Direct authentication is getting in the way, use the manual Guest Service Interface workflow instead:

- `specs/001-wix-v6-migration/HYPERV_INSTRUCTIONS.md`

That workflow uses `Copy-VMFile` (Guest Service Interface) to stage installers + scripts into the VM and includes a one-time VM step to set ExecutionPolicy so Shift+Right-click → Run with PowerShell works reliably.

Create a config file (sample):

- specs/001-wix-v6-migration/contracts/hyperv-runner.sample.json

Notes on the **WiX3 baseline**:

- The parity runner can auto-download the FieldWorks 9.2.11 (WiX3-era) Windows installer from https://software.sil.org/fieldworks/download/fw-92/fw-9211/.
- The sample config sets `baseline.downloadPageUrl` and caches the EXE under `%ProgramData%\FieldWorks\HyperV\Installers\Wix3Baseline`.
- If you already have the installer EXE, you can point `baseline.installerPath` at your local copy and omit the download fields.

Then run:

```powershell
# From repo root
# Optional: avoid interactive credential prompts by setting the guest password in an env var.
# The config can reference it via guestCredential.passwordEnvVar.
$env:FW_HYPERV_GUEST_PASSWORD = '<guest password>'

./scripts/Agent/Invoke-HyperVInstallerParity.ps1 -ConfigPath specs/001-wix-v6-migration/contracts/hyperv-runner.sample.json -PublishToSpecEvidence -GenerateFixPlan
```

Note on elevation:

- Parity runs should not require starting VS Code as Administrator if your user is in `Hyper-V Administrators`.
- If you need to debug VM boot failures by mounting the VHD on the host (`Mount-VHD`), use an elevated PowerShell / elevated VS Code.

If the env var is not set, the runner will prompt via `Get-Credential` (interactive). For CI/non-interactive runs, pass `-RequireGuestPasswordEnvVar` to fail fast instead of prompting.

Credential caching (recommended for local/dev):

- By default, the runner will cache the guest credential locally (DPAPI-encrypted, CurrentUser scope) under `%LOCALAPPDATA%\FieldWorks\HyperV\Secrets`.
- This avoids hardcoding a password anywhere and avoids storing a plaintext password in a persistent environment variable.
- Disable with `-RememberGuestCredential:$false`.

One-time setup helper (recommended):

```powershell
./scripts/Agent/Set-HyperVGuestCredential.ps1 -VMName FWInstallerTest
```

Tip: when prompted for the username, use a local account format like `.\<UserName>` (or `FwInstallerTest\<UserName>`). The account should be a local administrator in the guest.

### Validate configuration (no VM changes)

Use this to confirm the VM/checkpoint and installer paths are correct before doing a real run:

```powershell
./scripts/Agent/Invoke-HyperVInstallerParity.ps1 -ConfigPath specs/001-wix-v6-migration/contracts/hyperv-runner.sample.json -ValidateOnly
```

Note: `-ValidateOnly` does not auto-download the baseline installer unless you also pass `-ForceDownloadWix3Baseline`.

### Outputs

- Host evidence:
  - `Output/InstallerEvidence/HyperV/Wix3/<runId>/...`
  - `Output/InstallerEvidence/HyperV/Wix6/<runId>/...`
- Published spec evidence (optional):
  - `specs/001-wix-v6-migration/evidence/Wix3/<runId>/...`
  - `specs/001-wix-v6-migration/evidence/Wix6/<runId>/...`
- Diff report:
  - under a `compare/` folder next to the WiX6 evidence directory
- Generated fix plan (optional):
  - `specs/001-wix-v6-migration/wix6-parity-fix-plan-<timestamp>.md`
