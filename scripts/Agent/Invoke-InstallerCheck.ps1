[CmdletBinding()]
param(
	[Parameter(Mandatory = $false)]
	[ValidateSet('Auto', 'Bundle', 'Msi')]
	[string]$InstallerType = 'Bundle',

	[Parameter(Mandatory = $false)]
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Debug',

	[Parameter(Mandatory = $false)]
	[ValidateSet('x64', 'x86')]
	[string]$Platform = 'x64',

	[Parameter(Mandatory = $false)]
	[string]$InstallerPath,

	[Parameter(Mandatory = $false)]
	[string]$LogRoot,

	[Parameter(Mandatory = $false)]
	[string]$RunId,

	[Parameter(Mandatory = $false)]
	[string[]]$InstallArguments = @(),

	[Parameter(Mandatory = $false)]
	[switch]$RunUninstall,

	[Parameter(Mandatory = $false)]
	[string[]]$UninstallArguments = @('/uninstall'),

	[Parameter(Mandatory = $false)]
	[switch]$AssertUninstallClean,

	[Parameter(Mandatory = $false)]
	[int]$MaxFileCount = 20000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
	$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
	return $repoRoot.Path
}

function New-DirectoryIfMissing {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path)) {
		$null = New-Item -ItemType Directory -Path $Path -Force
	}
}

$repoRoot = Resolve-RepoRoot

if ([string]::IsNullOrWhiteSpace($LogRoot)) {
	$LogRoot = Join-Path $repoRoot 'Output\InstallerEvidence'
}

if ([string]::IsNullOrWhiteSpace($RunId)) {
	$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss')
}

$evidenceDir = Join-Path $LogRoot $RunId
New-DirectoryIfMissing -Path $evidenceDir

$beforePath = Join-Path $evidenceDir 'snapshot-before-install.json'
$afterInstallPath = Join-Path $evidenceDir 'snapshot-after-install.json'
$afterUninstallPath = Join-Path $evidenceDir 'snapshot-after-uninstall.json'

Write-Output "Evidence dir: $evidenceDir"

Write-Output "Collecting snapshot: before install"
& "$repoRoot\scripts\Agent\Collect-InstallerSnapshot.ps1" -OutputPath $beforePath -Name 'before-install' -MaxFileCount $MaxFileCount

Write-Output "Running installer"
$installResult = & "$repoRoot\scripts\Agent\Invoke-Installer.ps1" -InstallerType $InstallerType -Configuration $Configuration -Platform $Platform -InstallerPath $InstallerPath -LogRoot $LogRoot -RunId $RunId -Arguments $InstallArguments -IncludeTempLogs -PassThru -NoExit

if ($null -eq $installResult) {
	throw "Invoke-Installer did not return a result."
}

$exitCode = [int]$installResult.ExitCode
Write-Output "Installer exit code: $exitCode"

Write-Output "Collecting snapshot: after install"
& "$repoRoot\scripts\Agent\Collect-InstallerSnapshot.ps1" -OutputPath $afterInstallPath -Name 'after-install' -MaxFileCount $MaxFileCount

Write-Output "Comparing snapshots: before vs after install"
$installReportPath = Join-Path $evidenceDir 'diff-before-vs-after-install.txt'
& "$repoRoot\scripts\Agent\Compare-InstallerSnapshots.ps1" -BeforeSnapshotPath $beforePath -AfterSnapshotPath $afterInstallPath -ReportPath $installReportPath

if ($RunUninstall) {
	if ($InstallerType -ne 'Bundle') {
		Write-Output "WARNING: RunUninstall is currently only supported for InstallerType=Bundle. Skipping uninstall."
	} else {
		Write-Output "Running uninstall"
		$uninstallRunId = "$RunId-uninstall"
		$uninstallResult = & "$repoRoot\scripts\Agent\Invoke-Installer.ps1" -InstallerType 'Bundle' -Configuration $Configuration -Platform $Platform -InstallerPath $InstallerPath -LogRoot $LogRoot -RunId $uninstallRunId -Arguments $UninstallArguments -IncludeTempLogs -PassThru -NoExit
		$uninstallExit = [int]$uninstallResult.ExitCode
		Write-Output "Uninstall exit code: $uninstallExit"

		Write-Output "Collecting snapshot: after uninstall"
		& "$repoRoot\scripts\Agent\Collect-InstallerSnapshot.ps1" -OutputPath $afterUninstallPath -Name 'after-uninstall' -MaxFileCount $MaxFileCount

		Write-Output "Comparing snapshots: before install vs after uninstall"
		$uninstallReportPath = Join-Path $evidenceDir 'diff-before-install-vs-after-uninstall.txt'
		$compareArgs = @{
			BeforeSnapshotPath = $beforePath
			AfterSnapshotPath = $afterUninstallPath
			ReportPath = $uninstallReportPath
		}
		if ($AssertUninstallClean) { $compareArgs.FailOnDifferences = $true }
		& "$repoRoot\scripts\Agent\Compare-InstallerSnapshots.ps1" @compareArgs
	}
}

exit $exitCode
