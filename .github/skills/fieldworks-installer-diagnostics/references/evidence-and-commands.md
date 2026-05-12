# FieldWorks Installer Evidence And Commands

## Artifact Paths

WiX 3 fallback/current default:

- `FLExInstaller/bin/x64/Debug/en-US/FieldWorks.msi`
- `FLExInstaller/bin/x64/Debug/FieldWorksBundle.exe`

WiX 6 migration path:

- `FLExInstaller/wix6/bin/x64/Debug/en-US/FieldWorks.msi`
- `FLExInstaller/wix6/bin/x64/Debug/FieldWorksBundle.exe`
- `FLExInstaller/wix6/bin/x64/Debug/FieldWorksOfflineBundle.exe`

Adjust `Debug` to `Release` as needed.

## Helper Script Notes

- `scripts/Agent/Invoke-Installer.ps1` creates `Output/InstallerEvidence/<RunId>/` by default.
- The helper's default artifact resolver historically points at `FLExInstaller/bin/...`; pass `-InstallerPath` explicitly for WiX 6 artifacts.
- Use `-IncludeTempLogs` for Burn package logs and chained package logs.
- Use `-SummarizeMsiFileAccess` when validating installed file payloads from MSI logs.

## Common Runs

Interactive WiX 6 bundle:

```powershell
./scripts/Agent/Invoke-Installer.ps1 -InstallerType Bundle -InstallerPath '.\FLExInstaller\wix6\bin\x64\Debug\FieldWorksBundle.exe' -IncludeTempLogs
```

Passive WiX 6 bundle:

```powershell
./scripts/Agent/Invoke-Installer.ps1 -InstallerType Bundle -InstallerPath '.\FLExInstaller\wix6\bin\x64\Debug\FieldWorksBundle.exe' -Arguments @('/passive') -IncludeTempLogs
```

Direct MSI full UI:

```powershell
msiexec /i .\FLExInstaller\wix6\bin\x64\Debug\en-US\FieldWorks.msi /qf /l*v C:\Temp\FwInstallerEvidence\msi-ui.log
```

Uninstall by product code:

```powershell
msiexec /x {PRODUCT-CODE} /l*v C:\Temp\FwInstallerEvidence\msi-uninstall.log
```

## Crash Evidence

- Event Viewer -> Windows Logs -> Application: `.NET Runtime`, `Application Error`, and `MsiInstaller`.
- Crash dumps: `%LOCALAPPDATA%\CrashDumps\` for bundle, custom BA, or `msiexec.exe` crashes.
- FieldWorks debug traces can be enabled with `FieldWorks.Diagnostics.config` as described in `.github/instructions/debugging.instructions.md`.
