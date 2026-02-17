[CmdletBinding()]
param(
    [ValidateSet('Bundle', 'Msi')]
    [string]$InstallerType = 'Bundle',
    [string]$InstallerPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-InstallerPath {
    param(
        [string]$SearchRoot,
        [string]$Type
    )

    if ($Type -eq 'Bundle') {
        $candidate = Get-ChildItem -Path $SearchRoot -Filter '*Bundle*.exe' -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($null -ne $candidate) {
            return $candidate.FullName
        }
        return $null
    }

    $candidate = Get-ChildItem -Path $SearchRoot -Filter '*.msi' -File -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($null -ne $candidate) {
        return $candidate.FullName
    }

    return $null
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
    $InstallerPath = Get-InstallerPath -SearchRoot $scriptDir -Type $InstallerType
}

if ([string]::IsNullOrWhiteSpace($InstallerPath) -or -not (Test-Path -LiteralPath $InstallerPath)) {
    throw "Installer not found. Provide -InstallerPath or place the installer next to this script."
}

$installerDir = Split-Path -Parent $InstallerPath
$installerName = [System.IO.Path]::GetFileNameWithoutExtension($InstallerPath)
$logPath = Join-Path $installerDir ("{0}.log" -f $installerName)

Write-Output "Installer: $InstallerPath"
Write-Output "Log: $logPath"

if ($InstallerType -eq 'Bundle') {
    & $InstallerPath '/log' $logPath
    $exitCode = $LASTEXITCODE
}
else {
    $process = Start-Process -FilePath 'msiexec.exe' -ArgumentList @('/i', $InstallerPath, '/l*v', $logPath) -Wait -PassThru
    $exitCode = $process.ExitCode
}

if ($exitCode -ne 0) {
    Write-Error "Installer returned exit code $exitCode. See log: $logPath"
    exit $exitCode
}

Write-Output "[OK] Installer completed. Log saved to $logPath"
