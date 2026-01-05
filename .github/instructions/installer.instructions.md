---
applyTo: "FLExInstaller/**"
name: "installer.instructions"
description: "FieldWorks installer (WiX) development guidelines"
---
# Installer development guidelines (WiX v6)

## Purpose & Scope
Guidance for building, validating, and debugging the FieldWorks installer (WiX v6): MSI (`FieldWorks.msi`) and bootstrapper bundle (`FieldWorksBundle.exe`).

## Key facts (FieldWorks repo)
- WiX projects live in `FLExInstaller/`:
	- `FieldWorks.Installer.wixproj` (MSI)
	- `FieldWorks.Bundle.wixproj` (bundle)
- Builds are orchestrated via `Build/Orchestrator.proj` (preferred entrypoints are `./build.ps1` and `./test.ps1`).
- Default installer artifacts (Debug) land under:
	- `FLExInstaller\bin\x64\Debug\en-US\FieldWorks.msi`
	- `FLExInstaller\bin\x64\Debug\FieldWorksBundle.exe`

## Build commands
```powershell
# Preferred: traversal build wrapper (Debug)
./build.ps1 -BuildInstaller

# Direct MSBuild (equivalent to the orchestration target)
msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64
```

## Debugging flow: “double-click does nothing”
When a bundle/MSI exits with no UI, assume one of:
1) immediate process exit (argument parsing, prerequisite detection, policy)
2) crash before UI (bad BA init, missing dependency, load failure)
3) UI suppressed (quiet/passive mode, elevation issue)

Work from most observable → deepest inspection:

### 1) Run via the repo helper script (recommended)
Use the agent-friendly wrapper script to run the installer and collect logs in a deterministic evidence folder:
```powershell
# Bundle (default path for the chosen configuration/platform)
.\scripts\Agent\Invoke-Installer.ps1 -InstallerType Bundle -Configuration Debug -Platform x64

# Bundle with extra args and additional temp-log capture (useful for chained package logs)
.\scripts\Agent\Invoke-Installer.ps1 -InstallerType Bundle -Configuration Release -Arguments @('/passive') -IncludeTempLogs

# MSI (direct Windows Installer engine, with /l*v logging)
.\scripts\Agent\Invoke-Installer.ps1 -InstallerType Msi -Configuration Debug -Platform x64

# Custom path (if you're testing a copied artifact)
.\scripts\Agent\Invoke-Installer.ps1 -InstallerType Bundle -InstallerPath 'C:\Path\To\FieldWorksBundle.exe'
```

Notes:
- The script writes evidence under `Output\InstallerEvidence\<timestamp>\` and prints the primary log path.
- `-IncludeTempLogs` copies common related logs from `%TEMP%` that were written during the run.

### 1a) Manual commands (when you need full control)
Create an evidence folder and run explicitly:
```powershell
$e = Join-Path $env:TEMP ('FwInstallerEvidence\\' + (Get-Date -Format yyyy-MM-dd))
$null = New-Item -ItemType Directory -Force -Path $e

# Bundle (preferred for end-user scenario)
.\FLExInstaller\bin\x64\Debug\FieldWorksBundle.exe /log "$e\bundle.log"

# MSI (direct Windows Installer engine)
msiexec /i .\FLExInstaller\bin\x64\Debug\en-US\FieldWorks.msi /l*v "$e\msi-install.log"
```

Notes:
- Windows Installer requires the target log directory to exist; if logging fails, create the folder first.

### 2) Check Windows Event Viewer and crash dumps
If there is still “nothing”, check for an early crash:
- Event Viewer → Windows Logs → Application
	- `.NET Runtime` and `Application Error`
	- `MsiInstaller` events for MSI failures
- Crash dumps: `%LOCALAPPDATA%\CrashDumps\` for `*.dmp` related to the bundle or `msiexec.exe`.

### 3) Re-run with reduced noise (optional)
MSI UI levels can hide dialogs; explicitly request UI:
```powershell
# Full UI (if authored):
msiexec /i .\FLExInstaller\bin\x64\Debug\en-US\FieldWorks.msi /qf /l*v C:\Temp\FwInstallerEvidence\msi-ui.log

# Basic UI (progress only):
msiexec /i .\FLExInstaller\bin\x64\Debug\en-US\FieldWorks.msi /qb /l*v C:\Temp\FwInstallerEvidence\msi-basic.log
```

### 4) If the bundle starts but install fails: differentiate bundle vs MSI
- Bundle log shows prerequisite detection, downloads, and chaining decisions.
- MSI log shows property resolution, component/file install, registry writes, custom action execution.

### 5) If custom actions are suspected
- Search the MSI log for:
	- `Return value 3` (classic MSI failure marker)
	- `CustomAction` lines and the action name that failed
- Prefer reproducing with a single variable change at a time (different App/Data dirs; feature set; upgrade vs clean).

## WiX v6 build-time diagnostics (authoring/build)
- WiX v6 is used via SDK-style `.wixproj` with MSBuild properties.
- Useful knobs live on the project (or can be passed on the command line) such as:
	- `DefineConstants` (preprocessor variables)
	- `SuppressIces` / `Ices` / `SuppressValidation` (validation control)
	- `VerboseOutput` (more build output)
	- `*AdditionalOptions` properties to pass arbitrary `wix.exe` args

## Harvesting note (WiX v6)
- FieldWorks currently uses Heat (via `WixToolset.Heat` NuGet) to generate harvested `.wxs` inputs.
- Heat emits a deprecation warning (HEAT5149) and is expected to go away in WiX v7; treat this as technical debt to retire.

## Spec-backed verification (what to capture)
- Verification matrix: `specs/001-wix-v6-migration/verification-matrix.md`
- Golden install checklist: `specs/001-wix-v6-migration/golden-install-checklist.md`
- Recommended evidence folder convention: `C:\Temp\FwInstallerEvidence\YYYY-MM-DD\`

## References
- Windows Installer logging & command line:
	- https://learn.microsoft.com/windows/win32/msi/windows-installer-logging
	- https://learn.microsoft.com/windows/win32/msi/command-line-options
- WiX v6 MSBuild SDK concepts/properties:
	- https://docs.firegiant.com/wix/tools/msbuild/
