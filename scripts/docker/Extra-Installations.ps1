# Extra-Installations.ps1
# Downloads and installs additional tools needed for FieldWorks development.
# This script is primarily used by Dockerfile.windows for container builds.
#
# For developer workstation setup, use Setup-Developer-Machine.ps1 instead,
# which provides better defaults, winget integration, and verification.
#
# Usage (Docker/CI):
#   .\Extra-Installations.ps1                  # Install everything to C:\ paths
#   .\Extra-Installations.ps1 -SkipIfExists    # Skip if already installed

param(
    [switch]$SkipIfExists  # Skip installation if the tool already exists
)

$ErrorActionPreference = 'Stop'

Write-Host "=== Starting Extra Installations ==="

# Ensure TEMP directory exists
if (-not (Test-Path 'C:\TEMP')) {
    New-Item -ItemType Directory -Path 'C:\TEMP' -Force | Out-Null
}

# 1. .NET Framework 4.8.1 Developer Pack
Write-Host "`n--- .NET Framework 4.8.1 Developer Pack ---"
$netfxMarker = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8.1'
if ($SkipIfExists -and (Test-Path $netfxMarker)) {
    Write-Host ".NET Framework 4.8.1 Developer Pack already installed, skipping"
} else {
    Write-Host "Downloading .NET Framework 4.8.1 Developer Pack..."
    Invoke-WebRequest -Uri 'https://download.microsoft.com/download/8/1/8/81877d8b-a9b2-4153-9ad2-63a6441d11dd/NDP481-DevPack-ENU.exe' `
        -OutFile 'C:\TEMP\NDP481-DevPack-ENU.exe'
    Write-Host "Installing .NET Framework 4.8.1 Developer Pack..."
    Start-Process -FilePath 'C:\TEMP\NDP481-DevPack-ENU.exe' -ArgumentList '/q', '/norestart' -Wait
    Remove-Item 'C:\TEMP\NDP481-DevPack-ENU.exe' -Force
    Write-Host ".NET Framework 4.8.1 Developer Pack installed"
}

# 2. WiX Toolset 3.14.1
Write-Host "`n--- WiX Toolset 3.14.1 ---"
$wixDir = 'C:\Wix314'
if ($SkipIfExists -and (Test-Path $wixDir)) {
    Write-Host "WiX Toolset already installed at $wixDir, skipping"
} else {
    Write-Host "Downloading WiX Toolset 3.14.1..."
    Invoke-WebRequest -Uri 'https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314-binaries.zip' `
        -OutFile 'C:\TEMP\wix314.zip'
    Write-Host "Extracting WiX Toolset..."
    Expand-Archive -LiteralPath 'C:\TEMP\wix314.zip' -DestinationPath $wixDir -Force
    Remove-Item 'C:\TEMP\wix314.zip' -Force
    Write-Host "WiX Toolset installed to $wixDir"
}

# 3. .NET SDK 8.0 (for dotnet restore targets)
Write-Host "`n--- .NET SDK 8.0 ---"
$dotnetDir = 'C:\dotnet'
if ($SkipIfExists -and (Test-Path "$dotnetDir\dotnet.exe")) {
    Write-Host ".NET SDK already installed at $dotnetDir, skipping"
} else {
    Write-Host "Downloading .NET SDK installer script..."
    Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile 'C:\TEMP\dotnet-install.ps1'
    Write-Host "Installing .NET SDK 8.0..."
    & 'C:\TEMP\dotnet-install.ps1' -Channel 8.0 -Quality GA -InstallDir $dotnetDir -NoPath
    Remove-Item 'C:\TEMP\dotnet-install.ps1' -Force
    Write-Host ".NET SDK installed to $dotnetDir"
}

# 4. .NET 9 Runtime (required by Serena's C# language server - Microsoft.CodeAnalysis.LanguageServer)
Write-Host "`n--- .NET 9 Runtime (for Serena C# language server) ---"
$dotnet9Dir = 'C:\dotnet9'
if ($SkipIfExists -and (Test-Path "$dotnet9Dir\dotnet.exe")) {
    Write-Host ".NET 9 Runtime already installed at $dotnet9Dir, skipping"
} else {
    Write-Host "Downloading .NET 9 Runtime installer script..."
    Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile 'C:\TEMP\dotnet-install.ps1'
    Write-Host "Installing .NET 9 Runtime..."
    & 'C:\TEMP\dotnet-install.ps1' -Channel 9.0 -Quality GA -Runtime dotnet -InstallDir $dotnet9Dir -NoPath
    Remove-Item 'C:\TEMP\dotnet-install.ps1' -Force
    Write-Host ".NET 9 Runtime installed to $dotnet9Dir"
}

# 5. clangd (C++ language server for Serena MCP)
# Pre-install to avoid Serena downloading on every container start
# Using same version Serena would download (19.1.2)
Write-Host "`n--- clangd (C++ language server) ---"
$clangdDir = 'C:\clangd'
if ($SkipIfExists -and (Test-Path "$clangdDir\bin\clangd.exe")) {
    Write-Host "clangd already installed at $clangdDir, skipping"
} else {
    Write-Host "Downloading clangd 19.1.2..."
    Invoke-WebRequest -Uri 'https://github.com/clangd/clangd/releases/download/19.1.2/clangd-windows-19.1.2.zip' `
        -OutFile 'C:\TEMP\clangd.zip'
    Write-Host "Extracting clangd..."
    Expand-Archive -LiteralPath 'C:\TEMP\clangd.zip' -DestinationPath 'C:\TEMP\clangd-extract' -Force
    # The zip contains a subfolder like clangd_19.1.2, move contents to C:\clangd
    $extractedFolder = Get-ChildItem 'C:\TEMP\clangd-extract' -Directory | Select-Object -First 1
    if ($extractedFolder) {
        Move-Item -Path $extractedFolder.FullName -Destination $clangdDir -Force
    } else {
        Move-Item -Path 'C:\TEMP\clangd-extract' -Destination $clangdDir -Force
    }
    Remove-Item 'C:\TEMP\clangd.zip' -Force
    Remove-Item 'C:\TEMP\clangd-extract' -Force -Recurse -ErrorAction SilentlyContinue
    Write-Host "clangd installed to $clangdDir"
}

# 6. NuGet CLI
Write-Host "`n--- NuGet CLI ---"
$nugetPath = 'C:\nuget.exe'
if ($SkipIfExists -and (Test-Path $nugetPath)) {
    Write-Host "NuGet CLI already exists at $nugetPath, skipping"
} else {
    Write-Host "Downloading NuGet CLI..."
    Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nugetPath
    Write-Host "NuGet CLI downloaded to $nugetPath"
}

# Note: Serena's C# language server (Microsoft.CodeAnalysis.LanguageServer) auto-downloads
# from Azure NuGet on first use. We pre-install .NET 9 runtime and clangd to speed up
# container startup. The Roslyn server itself is small and downloads quickly.

Write-Host "`n=== Extra Installations Completed Successfully ==="
