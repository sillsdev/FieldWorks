# Quickstart: Building the Installer

## Prerequisites

1.  **Visual Studio 2022** with ".NET Desktop Development" workload.
2.  **WiX Toolset v6** (installed via .NET tool or NuGet, handled by build).
3.  **Internet Connection** (for downloading NuGet packages and prerequisites).

## Building Locally

To build the installer, run the following command from the repository root:

```powershell
# Build the installer (Debug configuration)
./build.ps1 -BuildInstaller

# Or run MSBuild directly (matches build.ps1 -BuildInstaller)
msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release

# Build Release version
./build.ps1 -BuildInstaller -Configuration Release

# Or run MSBuild directly
msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Release /p:Platform=x64 /p:config=release
```

## Artifacts

The build produces artifacts under `FLExInstaller/bin/<platform>/<configuration>/` (bundle outputs are culture-specific under `en-US/`).

- `FieldWorks.msi`: The main MSI package.
- `FieldWorksBundle.exe`: The bootstrapper bundle (includes prerequisites).
- `*.wixpdb`: Debug symbols for MSI/bundle.

### Artifact checklist (x64/Debug)

- [ ] `FLExInstaller/bin/x64/Debug/en-US/FieldWorks.msi`
- [ ] `FLExInstaller/bin/x64/Debug/en-US/FieldWorks.wixpdb`
- [ ] `FLExInstaller/bin/x64/Debug/FieldWorksBundle.exe`
- [ ] `FLExInstaller/bin/x64/Debug/FieldWorksBundle.wixpdb`

## Troubleshooting

- **Missing Prerequisites**: Ensure you have internet access. The build attempts to download required redistributables.
- **Signing Errors**: If `SIGN_INSTALLER` is set but no certificate is provided, the build will fail. Unset the variable for local testing.
- **WiX Errors**: Check the build log for specific WiX error codes (e.g., `WIX0001`).
