[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[string]$VMName,

	[Parameter(Mandatory = $true)]
	[string]$CheckpointName,

	[Parameter(Mandatory = $true)]
	[pscredential]$GuestCredential,

	[Parameter(Mandatory = $true)]
	[string]$InstallerPath,

	[Parameter(Mandatory = $true)]
	[ValidateSet('Wix3', 'Wix6')]
	[string]$Mode,

	[Parameter(Mandatory = $false)]
	[ValidateSet('Auto', 'Bundle', 'Msi')]
	[string]$InstallerType = 'Auto',

	[Parameter(Mandatory = $false)]
	[string]$RunId,

	[Parameter(Mandatory = $false)]
	[string]$GuestWorkDir = 'C:\\FWInstallerTest',

	[Parameter(Mandatory = $false)]
	[string]$HostEvidenceRoot,

	[Parameter(Mandatory = $false)]
	[string[]]$InstallArguments = @(),

	[Parameter(Mandatory = $false)]
	[int]$MaxFileCount = 20000,

	[Parameter(Mandatory = $false)]
	[int]$PowerShellDirectTimeoutSeconds = 900,

	[Parameter(Mandatory = $false)]
	[switch]$NoStopVm
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
	$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
	return $repoRoot.Path
}

function Ensure-Directory {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path)) {
		$null = New-Item -ItemType Directory -Path $Path -Force
	}
}

function Assert-FileExists {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path -PathType Leaf)) {
		throw "File not found: $Path"
	}
}

function Wait-ForPowerShellDirect {
	param(
		[Parameter(Mandatory = $true)][string]$VMName,
		[Parameter(Mandatory = $true)][pscredential]$Credential,
		[Parameter(Mandatory = $true)][int]$TimeoutSeconds
	)

	$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
	$lastError = $null
	$lastHeartbeatStatus = $null
	$lastVmState = $null
	$lastUptime = $null
	$nextVmStatusLog = Get-Date

	function Get-VmStatusLine {
		param([Parameter(Mandatory = $true)][string]$Name)
		try {
			$vmNow = Get-VM -Name $Name -ErrorAction Stop
			$hbNow = $null
			try { $hbNow = Get-VMIntegrationService -VMName $Name -Name 'Heartbeat' -ErrorAction Stop } catch { }
			$hbText = if ($null -ne $hbNow) { [string]$hbNow.PrimaryStatusDescription } else { 'Unknown' }
			return "State=$($vmNow.State) Uptime=$($vmNow.Uptime) CPU=$($vmNow.CPUUsage)% Mem=$($vmNow.MemoryAssigned) Heartbeat=$hbText"
		} catch {
			return "(Failed to query VM status: $_)"
		}
	}

	while ((Get-Date) -lt $deadline) {
		# Periodically log VM status so timeouts are actionable.
		if ((Get-Date) -ge $nextVmStatusLog) {
			Write-Verbose ("VM status: " + (Get-VmStatusLine -Name $VMName))
			$nextVmStatusLog = (Get-Date).AddSeconds(30)
		}

		# Heartbeat is a useful hint but not a hard gate (it can remain 'No Contact' early in boot).
		try {
			$hb = Get-VMIntegrationService -VMName $VMName -Name 'Heartbeat' -ErrorAction Stop
			if ($null -ne $hb) {
				$lastHeartbeatStatus = [string]$hb.PrimaryStatusDescription
			}
		} catch {
			# Ignore; integration services may not be queryable in some host states.
		}

		try {
			$session = New-PSSession -VMName $VMName -Credential $Credential -ErrorAction Stop
			return $session
		} catch {
			$lastError = $_
			try {
				$vmNow = Get-VM -Name $VMName -ErrorAction Stop
				$lastVmState = [string]$vmNow.State
				$lastUptime = [string]$vmNow.Uptime
			} catch {
				# Ignore; best-effort.
			}
			Write-Verbose "PowerShell Direct not ready yet: $lastError"
			Start-Sleep -Seconds 5
		}
	}

	$vmState = $null
	try { $vmState = (Get-VM -Name $VMName -ErrorAction Stop).State } catch { }
	throw "Timed out waiting for PowerShell Direct session to VM '$VMName' (state: $vmState, lastHeartbeat: $lastHeartbeatStatus, lastUptime: $lastUptime). Last error: $lastError"
}

$repoRoot = Resolve-RepoRoot

Assert-FileExists -Path $InstallerPath
$InstallerPath = (Resolve-Path -LiteralPath $InstallerPath).Path

if ([string]::IsNullOrWhiteSpace($HostEvidenceRoot)) {
	$HostEvidenceRoot = Join-Path $repoRoot 'Output\InstallerEvidence\HyperV'
}

if ([string]::IsNullOrWhiteSpace($RunId)) {
	$RunId = '{0}-{1}' -f ($Mode.ToLowerInvariant()), (Get-Date -Format 'yyyyMMdd-HHmmss')
}

$hostModeDir = Join-Path $HostEvidenceRoot $Mode
$hostRunDir = Join-Path $hostModeDir $RunId
Ensure-Directory -Path $hostRunDir

$startedUtc = (Get-Date).ToUniversalTime()

try {
	Import-Module Hyper-V -ErrorAction Stop
} catch {
	throw "Hyper-V PowerShell module is required. Enable Hyper-V and ensure the Hyper-V module is installed. Error: $_"
}

$vm = Get-VM -Name $VMName -ErrorAction Stop
$checkpoint = Get-VMSnapshot -VMName $VMName -Name $CheckpointName -ErrorAction Stop

if ($vm.State -ne 'Off') {
	Write-Output "Stopping VM '$VMName' (state: $($vm.State))"
	Stop-VM -Name $VMName -Force -TurnOff -ErrorAction Stop
}

Write-Output "Restoring checkpoint '$CheckpointName' on VM '$VMName'"
Restore-VMSnapshot -VMName $VMName -Name $CheckpointName -Confirm:$false -ErrorAction Stop

Write-Output "Starting VM '$VMName'"
Start-VM -Name $VMName -ErrorAction Stop | Out-Null

Write-Output "Waiting for PowerShell Direct readiness"
$session = Wait-ForPowerShellDirect -VMName $VMName -Credential $GuestCredential -TimeoutSeconds $PowerShellDirectTimeoutSeconds

$guestRepoRoot = Join-Path $GuestWorkDir 'repo'
$guestAgentDir = Join-Path $guestRepoRoot 'scripts\Agent'
$guestPayloadDir = Join-Path $GuestWorkDir 'payload'
$guestOutRoot = Join-Path $GuestWorkDir 'out'
$guestInstallerPath = Join-Path $guestPayloadDir (Split-Path -Leaf $InstallerPath)
$guestRunDir = Join-Path $guestOutRoot $RunId

try {
	Invoke-Command -Session $session -ScriptBlock {
		param($GuestWorkDir, $GuestAgentDir, $GuestPayloadDir, $GuestOutRoot)

		$paths = @($GuestWorkDir, $GuestAgentDir, $GuestPayloadDir, $GuestOutRoot)
		foreach ($p in $paths) {
			if (!(Test-Path -LiteralPath $p)) {
				$null = New-Item -ItemType Directory -Path $p -Force
			}
		}
	} -ArgumentList $GuestWorkDir, $guestAgentDir, $guestPayloadDir, $guestOutRoot -ErrorAction Stop | Out-Null

	$requiredScripts = @(
		'Collect-InstallerSnapshot.ps1',
		'Compare-InstallerSnapshots.ps1',
		'Invoke-Installer.ps1',
		'Invoke-InstallerCheck.ps1'
	)

	foreach ($scriptName in $requiredScripts) {
		$src = Join-Path $repoRoot (Join-Path 'scripts\Agent' $scriptName)
		Assert-FileExists -Path $src

		$dst = Join-Path $guestAgentDir $scriptName
		Copy-Item -LiteralPath $src -Destination $dst -ToSession $session -Force -ErrorAction Stop
	}

	Copy-Item -LiteralPath $InstallerPath -Destination $guestInstallerPath -ToSession $session -Force -ErrorAction Stop

	Write-Output "Running installer check inside VM (RunId: $RunId)"

	$exitCode = Invoke-Command -Session $session -ScriptBlock {
		param(
			$GuestWorkDir,
			$InstallerType,
			$InstallerPath,
			$RunId,
			[string[]]$InstallArguments,
			$MaxFileCount
		)

		$checkScript = Join-Path $GuestWorkDir 'repo\scripts\Agent\Invoke-InstallerCheck.ps1'
		if (!(Test-Path -LiteralPath $checkScript)) {
			throw "Missing guest script: $checkScript"
		}

		$logRoot = Join-Path $GuestWorkDir 'out'

		$args = @(
			'-NoProfile',
			'-ExecutionPolicy', 'Bypass',
			'-File', $checkScript,
			'-InstallerType', $InstallerType,
			'-InstallerPath', $InstallerPath,
			'-LogRoot', $logRoot,
			'-RunId', $RunId,
			'-MaxFileCount', $MaxFileCount
		)

		if ($null -ne $InstallArguments -and $InstallArguments.Count -gt 0) {
			$args += @('-InstallArguments')
			$args += $InstallArguments
		}

		& 'powershell.exe' @args
		return [int]$LASTEXITCODE
	} -ArgumentList $GuestWorkDir, $InstallerType, $guestInstallerPath, $RunId, $InstallArguments, $MaxFileCount -ErrorAction Stop

	Write-Output "Guest installer check exit code: $exitCode"

	Write-Output "Copying evidence from guest: $guestRunDir -> host: $hostRunDir"
	Copy-Item -FromSession $session -Path (Join-Path $guestRunDir '*') -Destination $hostRunDir -Recurse -Force -ErrorAction Stop

	$finishedUtc = (Get-Date).ToUniversalTime()
	$meta = [pscustomobject]@{
		Mode = $Mode
		RunId = $RunId
		StartedUtc = $startedUtc.ToString('o')
		FinishedUtc = $finishedUtc.ToString('o')
		ExitCode = [int]$exitCode
		VMName = $VMName
		CheckpointName = $CheckpointName
		HostInstallerPath = $InstallerPath
		GuestInstallerPath = $guestInstallerPath
		InstallArguments = $InstallArguments
		HostEvidenceDir = $hostRunDir
		GuestEvidenceDir = $guestRunDir
	}
	$meta | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $hostRunDir 'hyperv-run.json') -Encoding UTF8

	return [pscustomobject]@{
		Mode = $Mode
		RunId = $RunId
		ExitCode = [int]$exitCode
		HostEvidenceDir = $hostRunDir
	}
} finally {
	if ($null -ne $session) {
		try { Remove-PSSession -Session $session -ErrorAction SilentlyContinue } catch { }
	}

	if (-not $NoStopVm) {
		try {
			Write-Output "Stopping VM '$VMName'"
			Stop-VM -Name $VMName -Force -TurnOff -ErrorAction Stop
		} catch {
			Write-Output "WARNING: Failed to stop VM '$VMName': $_"
		}
	}
}
