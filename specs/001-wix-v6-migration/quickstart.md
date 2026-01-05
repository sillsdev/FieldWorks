# Quickstart: Building the Installer

## Prerequisites

1.  **Visual Studio 2022** with ".NET Desktop Development" workload.
2.  **WiX Toolset v6** (installed via .NET tool or NuGet, handled by build).
3.  **Internet Connection** (for downloading NuGet packages and prerequisites).

## Building Locally

To build the installer, run the following command from the repository root:

```powershell
# Build the installer (Debug configuration)
msbuild FieldWorks.proj /t:BuildInstaller

# Build Release version
msbuild FieldWorks.proj /t:BuildInstaller /p:Configuration=Release
```

## Artifacts

The build produces the following artifacts in `Output/Installer`:

- `FieldWorks.msi`: The main MSI package.
- `FieldWorks.exe`: The bootstrapper bundle (includes prerequisites).
- `FieldWorks.wixpdb`: Debug symbols for the installer.

## Troubleshooting

- **Missing Prerequisites**: Ensure you have internet access. The build attempts to download required redistributables.
- **Signing Errors**: If `SIGN_INSTALLER` is set but no certificate is provided, the build will fail. Unset the variable for local testing.
- **WiX Errors**: Check the build log for specific WiX error codes (e.g., `WIX0001`).
