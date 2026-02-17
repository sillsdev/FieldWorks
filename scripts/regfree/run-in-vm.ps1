[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$VmName,

    [Parameter(Mandatory = $true)]
    [string]$ExecutablePath,

    [string[]]$ExtraPayload = @(),

    [string[]]$Arguments = @(),

    [string]$CheckpointName = "regfree-clean",

    [string]$GuestWorkingDirectory = "C:\\RegFreePayload",

    [string]$OutputDirectory = "specs/003-convergence-regfree-com-coverage/artifacts/vm-output",

    [switch]$NoCheckpointRestore,

    [switch]$SkipStopVm
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Output "[$timestamp] $Message"
}

function Resolve-PathStrict {
    param([string]$Path)
    $resolved = Resolve-Path -Path $Path -ErrorAction Stop
    return $resolved.ProviderPath
}

if (-not (Get-Command Get-VM -ErrorAction SilentlyContinue)) {
    throw "Hyper-V PowerShell module is required. Install the Hyper-V feature and rerun this script."
}

$exePath = Resolve-PathStrict -Path $ExecutablePath
$payloadPaths = @($exePath)

$manifestPath = "$exePath.manifest"
if (Test-Path -Path $manifestPath) {
    $payloadPaths += (Resolve-PathStrict -Path $manifestPath)
} else {
    Write-Log "Manifest not found next to executable ($manifestPath). Continuing without manifest copy."
}

foreach ($item in $ExtraPayload) {
    $payloadPaths += (Resolve-PathStrict -Path $item)
}

$vm = Get-VM -Name $VmName -ErrorAction Stop

if (-not $NoCheckpointRestore) {
    $checkpoint = Get-VMCheckpoint -VMName $VmName -Name $CheckpointName -ErrorAction SilentlyContinue
    if ($null -eq $checkpoint) {
        Write-Log "Checkpoint '$CheckpointName' not found; continuing without restore."
    } else {
        Write-Log "Restoring checkpoint '$CheckpointName'."
        Restore-VMCheckpoint -VMCheckpoint $checkpoint -Confirm:$false | Out-Null
    }
}

if ($vm.State -ne 'Running') {
    Write-Log "Starting VM '$VmName'."
    Start-VM -VM $vm | Out-Null
}

$payloadRoot = Join-Path -Path $env:TEMP -ChildPath ("regfree-" + [Guid]::NewGuid())
New-Item -ItemType Directory -Path $payloadRoot | Out-Null

try {
    foreach ($path in $payloadPaths) {
        Copy-Item -Path $path -Destination $payloadRoot -Force
    }

    $hostOutputDirectory = Resolve-Path -Path $OutputDirectory -ErrorAction SilentlyContinue
    if (-not $hostOutputDirectory) {
        $hostOutputDirectory = New-Item -ItemType Directory -Path $OutputDirectory -Force
    }

    $outputFile = Join-Path -Path $hostOutputDirectory -ChildPath ("${VmName}-" + (Split-Path -Leaf $exePath) + "-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")

    Write-Log "Copying payload files to VM '$VmName'."
    foreach ($file in Get-ChildItem -Path $payloadRoot) {
        Copy-VMFile -VMName $VmName -SourcePath $file.FullName -DestinationPath (Join-Path $GuestWorkingDirectory $file.Name) -CreateFullPath -FileSource Host -ErrorAction Stop
    }

    $guestExePath = Join-Path $GuestWorkingDirectory (Split-Path -Leaf $exePath)
    $scriptBlock = @"
        param(
            [string]`$CommandPath,
            [string[]]`$Args,
            [string]`$WorkingDir
        )
        Set-Location -Path `$WorkingDir
        & `$CommandPath @Args
"@

    Write-Log "Launching executable inside VM via PowerShell Direct."
    $invokeResult = Invoke-Command -VMName $VmName -ScriptBlock ([ScriptBlock]::Create($scriptBlock)) -ArgumentList $guestExePath, $Arguments, $GuestWorkingDirectory -ErrorAction Stop
    $invokeResult | Out-File -FilePath $outputFile -Encoding utf8
    Write-Log "VM execution complete. Log saved to $outputFile"
}
finally {
    Remove-Item -Path $payloadRoot -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
    if (-not $SkipStopVm) {
        Write-Log "Stopping VM '$VmName'."
        Stop-VM -VM $vm -Force -TurnOff:$false | Out-Null
    }
}
