# Post-Install-Setup.ps1
# Consolidates all post-installation setup steps that involve complex paths
# to avoid Docker command-line parsing issues.

$ErrorActionPreference = 'Stop'

Write-Host "=== Starting Post-Installation Setup ==="

# 1. Create Visual Studio directory structure using full path
Write-Host "Creating Visual Studio directory structure..."
$vsPath = 'C:\Program Files (x86)\Microsoft Visual Studio\2022'
if (-not (Test-Path $vsPath)) {
    Write-Host "Creating directory: $vsPath"
    New-Item -ItemType Directory -Path $vsPath -Force | Out-Null
}

# 2. Verify BuildTools exists
Write-Host "Verifying BuildTools installation..."
if (-not (Test-Path 'C:\BuildTools')) {
    throw 'BuildTools directory not found at C:\BuildTools'
}

# 3. Create junction to BuildTools
Write-Host "Creating BuildTools junction..."
$junctionPath = Join-Path $vsPath 'BuildTools'
if (-not (Test-Path $junctionPath)) {
    Write-Host "Creating junction: $junctionPath -> C:\BuildTools"
    & "$env:SystemRoot\System32\cmd.exe" /c mklink /J "$junctionPath" "C:\BuildTools"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create junction with exit code $LASTEXITCODE"
    }
} else {
    Write-Host "Junction already exists: $junctionPath"
}

# 4. Create NuGet packages cache directory
Write-Host "Creating NuGet packages cache directory..."
$nugetPath = 'C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages'
if (-not (Test-Path $nugetPath)) {
    Write-Host "Creating directory: $nugetPath"
    New-Item -ItemType Directory -Path $nugetPath -Force | Out-Null
}

# 5. Download NuGet CLI
Write-Host "Downloading NuGet CLI..."
if (-not (Test-Path 'C:\nuget.exe')) {
    Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile 'C:\nuget.exe'
    Write-Host "NuGet CLI downloaded successfully"
} else {
    Write-Host "NuGet CLI already exists"
}

# 6. Configure MSBuild to find .NET SDK
Write-Host "Configuring MSBuild .NET SDK resolver..."
$msbuildSdkPath = 'C:\BuildTools\MSBuild\Sdks'
# Find the latest .NET SDK version installed
$sdkVersions = Get-ChildItem 'C:\dotnet\sdk' -Directory | Where-Object { $_.Name -match '^\d+\.\d+\.\d+$' } | Sort-Object Name -Descending
$latestSdk = $sdkVersions | Select-Object -First 1
$dotnetSdkPath = Join-Path $latestSdk.FullName 'Sdks'
Write-Host "Using .NET SDK: $($latestSdk.Name)"
if (Test-Path $dotnetSdkPath) {
    if (-not (Test-Path $msbuildSdkPath)) {
        Write-Host "Creating MSBuild Sdks directory..."
        New-Item -ItemType Directory -Path $msbuildSdkPath -Force | Out-Null
    }
    if (-not (Test-Path "$msbuildSdkPath\Microsoft.NET.Sdk")) {
        Write-Host "Creating symbolic links for .NET SDK..."
        $sdks = @(
            'Microsoft.NET.Sdk',
            'Microsoft.NET.Sdk.Web',
            'Microsoft.NET.Sdk.Worker'
        )
        foreach ($sdk in $sdks) {
            $source = Join-Path $dotnetSdkPath $sdk
            $target = Join-Path $msbuildSdkPath $sdk
            if ((Test-Path $source) -and (-not (Test-Path $target))) {
                & "$env:SystemRoot\System32\cmd.exe" /c mklink /D "$target" "$source" | Out-Null
            }
        }

        # Create stub for WorkloadAutoImportPropsLocator (not present in .NET 8.0.100)
        $workloadLocator = Join-Path $msbuildSdkPath 'Microsoft.NET.SDK.WorkloadAutoImportPropsLocator\Sdk'
        if (-not (Test-Path $workloadLocator)) {
            New-Item -ItemType Directory -Path $workloadLocator -Force | Out-Null
            # Create empty Sdk.props, Sdk.targets, and AutoImport.props
            Set-Content -Path (Join-Path $workloadLocator 'Sdk.props') -Value '<?xml version="1.0" encoding="utf-8"?><Project />'
            Set-Content -Path (Join-Path $workloadLocator 'Sdk.targets') -Value '<?xml version="1.0" encoding="utf-8"?><Project />'
            Set-Content -Path (Join-Path $workloadLocator 'AutoImport.props') -Value '<?xml version="1.0" encoding="utf-8"?><Project />'
        }

        # Create stub for WorkloadManifestTargetsLocator (not present in .NET 8.0.100)
        $manifestLocator = Join-Path $msbuildSdkPath 'Microsoft.NET.SDK.WorkloadManifestTargetsLocator\Sdk'
        if (-not (Test-Path $manifestLocator)) {
            New-Item -ItemType Directory -Path $manifestLocator -Force | Out-Null
            # Create empty Sdk.props and Sdk.targets
            Set-Content -Path (Join-Path $manifestLocator 'Sdk.props') -Value '<?xml version="1.0" encoding="utf-8"?><Project />'
            Set-Content -Path (Join-Path $manifestLocator 'Sdk.targets') -Value '<?xml version="1.0" encoding="utf-8"?><Project />'
        }

        Write-Host ".NET SDK symbolic links created"
    }
}

# 7. Configure Machine PATH
Write-Host "Configuring Machine PATH..."

$netfxTools = 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools'
if (-not (Test-Path $netfxTools)) {
    throw "NETFX 4.8.1 tools not found at $netfxTools"
}

$msbuildPath = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin'
if (-not (Test-Path $msbuildPath)) {
    throw "MSBuild not found at $msbuildPath"
}

$paths = @(
    'C:\Windows\System32\WindowsPowerShell\v1.0',
    'C:\Wix311',
    'C:\dotnet',
    $netfxTools,
    $msbuildPath
)

$existing = [Environment]::GetEnvironmentVariable('PATH', 'Machine')

foreach ($p in $paths) {
    if ($existing -notlike "*$p*") {
        Write-Host "Adding to PATH: $p"
        $existing = "$existing;$p"
    } else {
        Write-Host "Already in PATH: $p"
    }
}

[Environment]::SetEnvironmentVariable('PATH', $existing, 'Machine')
Write-Host "Machine PATH updated successfully"

# 8. Configure Machine Environment Variables
Write-Host "Configuring Machine Environment Variables..."

$envVars = @{
    'WIX'            = 'C:\Wix311'
    'DOTNET_ROOT'    = 'C:\dotnet'
    'NUGET_PACKAGES' = 'C:\.nuget\packages'
    'VCTargetsPath'  = 'C:\BuildTools\MSBuild\Microsoft\VC\v170'
    'VSINSTALLDIR'   = 'C:\BuildTools\'
    'VCINSTALLDIR'   = 'C:\BuildTools\VC\'
}

foreach ($key in $envVars.Keys) {
    $val = $envVars[$key]
    Write-Host "Setting $key = $val"
    [Environment]::SetEnvironmentVariable($key, $val, 'Machine')
}
Write-Host "Machine Environment Variables updated successfully"

Write-Host "=== Post-Installation Setup Completed Successfully ==="
