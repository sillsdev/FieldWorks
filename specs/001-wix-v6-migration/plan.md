# Implementation Plan: WiX v6 Migration

**Branch**: 001-wix-v6-migration | **Date**: 2025-12-11 | **Spec**: [specs/001-wix-v6-migration/spec.md](specs/001-wix-v6-migration/spec.md)
**Input**: Feature specification from specs/001-wix-v6-migration/spec.md

**Note**: This template is filled in by the /speckit.plan command. See .specify/templates/commands/plan.md for the execution workflow.

## Summary

Migrate the FieldWorks installer from WiX 3.11 to WiX Toolset v6. This involves converting project files to SDK-style, replacing the genericinstaller submodule with in-tree code, modernizing the build process to use MSBuild targets instead of batch files, and ensuring functional parity including offline support, dual-directory UI, and seamless upgrades.

To keep the migration maintainable, we also standardize on C# 8 language features for managed code (while keeping nullable reference type analysis disabled initially to avoid warnings-as-errors churn).

For **clean-machine installer verification**, we will:
- Use a **deterministic Hyper-V checkpoint runner** as the **parity gate** for WiX3 vs WiX6 behavior.

## Technical Context

**Language/Version**: WiX Toolset v6, MSBuild (VS 2022), C# 8 (Custom Actions; nullable analysis disabled initially)
**Primary Dependencies**: WixToolset.Sdk, WixToolset.UI.wixext, WixToolset.Util.wixext, WixToolset.NetFx.wixext, WixToolset.Bal.wixext
**Storage**: N/A (Installer)
**Testing**: Snapshot/compare-based evidence collection; deterministic Hyper-V checkpoint parity runs; WixToolset.Heat for harvesting (if used)
**Target Platform**: Windows (x64/x86)
**Project Type**: Installer (MSI & Bundle)
**Performance Goals**: N/A
**Constraints**: Offline capability, Dual Directory UI (App + Project Data), Major Upgrade support
**Scale/Scope**: ~12 features, multiple locales, ~110 projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Data Integrity**: Installer must perform Major Upgrade to prevent side-by-side version conflicts and potential data corruption. (FR-Upgrade)
- **II. Test and Review Discipline**: Changes to installer require validation. User stories include independent tests for Developer Build, CI Build, Online Install, and Offline Install.
- **III. Internationalization**: Installer must support multiple locales (FR-005).
- **IV. User-Centered Stability**: Offline installation support is critical (FR-004).
- **V. Licensing**: WiX is open source (MS-RL/MIT).
- **VI. Documentation Fidelity**: COPILOT.md in FLExInstaller and PatchableInstaller must be updated.

## Project Structure

### Documentation (this feature)

`	ext
specs/001-wix-v6-migration/
 plan.md              # This file
 research.md          # Phase 0 output
 data-model.md        # Phase 1 output (N/A for installer)
 quickstart.md        # Phase 1 output
 contracts/           # Phase 1 output (N/A for installer)
 tasks.md             # Phase 2 output
`

### Source Code (repository root)

`	ext
FLExInstaller/
 *.wxs                # Main installer source (to be migrated)
 *.wxi                # Includes
 ...

PatchableInstaller/    # Likely to be consolidated or refactored
 BaseInstallerBuild/
 ...

Build/
 Installer.targets    # New MSBuild targets for WiX v6
 ...
`

**Structure Decision**: The existing FLExInstaller directory will remain the primary source. PatchableInstaller contents (specifically BaseInstallerBuild and CustomActions) will be analyzed and migrated into FLExInstaller or a new Installer.Shared project to remove the genericinstaller dependency.

## Complexity Tracking

N/A - No constitution violations.

## Clean-Machine Verification: Hyper-V Checkpoint Runner (Parity Gate)

### Why Hyper-V (vs Windows Sandbox)
Windows Sandbox has shown non-deterministic startup behavior for automation (e.g., sandbox UI starts but configured automation does not run). Hyper-V avoids this class of failure by restoring a VM checkpoint to a known state and driving the test from the host.

This path relies on:
- Hyper-V being available on Windows (Pro/Enterprise) and enabled.
- PowerShell-based automation using the Hyper-V module (`Checkpoint-VM`, `Restore-VMSnapshot`, `Start-VM`, `Get-VM`) plus PowerShell Direct (`Invoke-Command -VMName`, `New-PSSession -VMName`, `Copy-Item -ToSession/-FromSession`).

### Goals
- **Deterministic**: every run begins from the same VM checkpoint.
- **Comparable**: WiX3 installer and WiX6 installer runs produce the same evidence schema.
- **Automatable**: run from a single host script with minimal manual steps.

### Inputs / outputs
**Inputs**
- `BaselineInstaller`: the WiX3 installer to compare against (bundle EXE and/or MSI).
- `CandidateInstaller`: the WiX6 installer under test (bundle EXE and/or MSI).
- `VMName`: Hyper-V VM to use.
- `CheckpointName`: checkpoint to restore before each run.
- `GuestCredential`: local admin credentials inside the VM (for PowerShell Direct).

**Outputs**
- Evidence folders on the host:
	- `evidence/wix3/<run-id>/...`
	- `evidence/wix6/<run-id>/...`
- A parity report artifact that summarizes differences (expected vs unexpected).

### High-level runner flow
1) Validate host prerequisites
	 - Hyper-V enabled and Hyper-V PowerShell module available.
	 - VM exists, is local, and checkpoint exists.

2) For each installer under test (WiX3 baseline then WiX6 candidate):
	 - Restore checkpoint.
	 - Start VM and wait for PowerShell Direct readiness.
	 - Copy payload (installer + any supporting files) into VM.
	 - Run installer unattended (capture bundle/MSI logs).
	 - Collect evidence:
		 - exit code + logs
		 - uninstall registry keys snapshot
		 - key file/shortcut/service presence checks
		 - optional smoke launch
	 - Copy evidence bundle back to host.
	 - Power off VM (optional; next restore will reset state anyway).

3) Compare evidence
	 - Run the existing snapshot/compare tooling on the two evidence folders.

### Infrastructure changes (planned)
- Add a host-side script module (PowerShell) to:
	- restore checkpoints
	- start/stop VM
	- establish PowerShell Direct sessions
	- copy files in/out
	- orchestrate “baseline vs candidate” runs
- Add a guest-side runner script to:
	- run the installer(s) with known flags
	- emit evidence files to a single folder
- Extend the evidence schema and parity compare to include:
	- consistent log filenames and exit code capture
	- registry export snapshots
	- normalized paths (avoid machine-specific paths)

### Research notes (Hyper-V)
Key references used for this plan:
- Hyper-V enablement/system requirements (Windows 10/11 Pro/Enterprise, SLAT, VT-x/AMD-V): https://learn.microsoft.com/en-us/windows-server/virtualization/hyper-v/get-started/install-hyper-v
- PowerShell Direct requirements and usage (`Invoke-Command -VMName`, `New-PSSession`, `Copy-Item -ToSession/-FromSession`): https://learn.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/powershell-direct
- Hyper-V PowerShell module cmdlets (`Checkpoint-VM`, `Restore-VMSnapshot`, `Copy-VMFile`, etc.): https://learn.microsoft.com/en-us/powershell/module/hyper-v/
- Hyper-V docs and samples repository (maps learn.microsoft.com content + scripts): https://github.com/MicrosoftDocs/Virtualization-Documentation
- General background on Hyper-V platform availability and requirements: https://en.wikipedia.org/wiki/Hyper-V
