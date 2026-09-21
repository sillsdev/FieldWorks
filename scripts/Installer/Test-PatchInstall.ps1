<#
.SYNOPSIS
Installs the published base, applies the latest published patch, then applies the new patch the
way a user would, and fails unless the new patch really installs.

.DESCRIPTION
Windows Installer's default reaction to a patch that drops a component is to log SELMGR,
install nothing, and still return success. This test applies the new patch with
MSIENFORCEUPGRADECOMPONENTRULES=1, which turns that into error 2771, and also checks that
FieldWorks.exe on disk carries the patch's version. Needs administrator rights and a machine
without FieldWorks installed; a CI runner qualifies.

.PARAMETER Patch
The new .msp.

.PARAMETER PatchVersion
The new patch's product version, e.g. 9.3.12.2761.

.PARAMETER UpdateMsi
The upgraded (Update) MSI, which supplies the product code and the FieldWorks.exe component.

.PARAMETER LedgerPaths
Ledger files that map the component GUIDs Windows Installer reports to file names.

.PARAMETER WorkDir
Where downloads and install logs go.
#>
[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)][string]$Patch,
	[Parameter(Mandatory = $true)][string]$PatchVersion,
	[Parameter(Mandatory = $true)][string]$BaseBuildNumber,
	[Parameter(Mandatory = $true)][string]$UpdateMsi,
	[string[]]$LedgerPaths = @(),
	[Parameter(Mandatory = $true)][string]$WorkDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
Import-Module (Join-Path $PSScriptRoot 'PatchComponentLedger.psm1') -Force

New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null
$failures = New-Object System.Collections.Generic.List[string]

function Invoke-Logged {
	param([string]$FilePath, [string]$Arguments, [string]$Step)
	Write-Output "${Step}: $FilePath $Arguments"
	$process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -Wait -PassThru
	# 3010 = success, restart required.
	if ($process.ExitCode -ne 0 -and $process.ExitCode -ne 3010) {
		throw "$Step failed with exit code $($process.ExitCode)"
	}
}

$baseKey = @(Get-UpdateBucketKeys -Prefix "jobs/FieldWorks-Win-all-Release-Base/$BaseBuildNumber/" -Like '*_Online_x64.exe') | Select-Object -First 1
if (-not $baseKey) { throw "No published base installer found for base $BaseBuildNumber" }
$baseInstaller = Save-PublishedFile -Key $baseKey -Directory $WorkDir
Invoke-Logged -FilePath $baseInstaller -Arguments "/quiet /norestart /log `"$WorkDir\01-base.log`"" -Step 'Install base'

$newVersion = [version]$PatchVersion
$previousKey = @(Get-PublishedPatchKeys -BaseBuildNumber $BaseBuildNumber -Wildcard '*.msp' |
		Where-Object { (Get-PatchVersionFromKey $_) -lt $newVersion } |
		Sort-Object { Get-PatchVersionFromKey $_ }) | Select-Object -Last 1
if ($previousKey) {
	$previousPatch = Save-PublishedFile -Key $previousKey -Directory $WorkDir
	Invoke-Logged -FilePath 'msiexec.exe' -Arguments "/p `"$previousPatch`" /qn /norestart AUTOUPDATE=True /l*vx `"$WorkDir\02-previous-patch.log`"" -Step "Apply published patch $(Get-PatchVersionFromKey $previousKey)"
}
else {
	Write-Output "No earlier patch is published on base $BaseBuildNumber; applying the new patch to the base alone."
}

$newLog = Join-Path $WorkDir '03-new-patch.log'
$process = Start-Process -FilePath 'msiexec.exe' -ArgumentList "/p `"$Patch`" /qn /norestart AUTOUPDATE=True MSIENFORCEUPGRADECOMPONENTRULES=1 /l*vx `"$newLog`"" -Wait -PassThru
Write-Output "Apply new patch ${PatchVersion}: exit code $($process.ExitCode)"
if ($process.ExitCode -ne 0 -and $process.ExitCode -ne 3010) {
	$failures.Add("Applying patch $PatchVersion failed with exit code $($process.ExitCode).")
}

# SELMGR names each component a feature has lost; the ledgers turn those GUIDs into files.
$dropped = New-Object System.Collections.Generic.List[object]
if (Test-Path -LiteralPath $newLog) {
	$ledger = Read-ComponentLedger -Path $LedgerPaths
	$pattern = "SELMGR: ComponentId '(\{[0-9A-Fa-f-]+\})' is registered to feature '([^']+)'"
	foreach ($match in (Select-String -LiteralPath $newLog -Pattern $pattern -AllMatches)) {
		foreach ($m in $match.Matches) {
			$id = $m.Groups[1].Value.ToUpperInvariant()
			if ($ledger.ContainsKey($id)) {
				$dropped.Add($ledger[$id])
			}
			else {
				$dropped.Add([pscustomobject]@{ ComponentId = $id; File = '(not in any ledger)'; Feature = $m.Groups[2].Value; FirstShipped = '?'; LastShipped = '?' })
			}
		}
	}
}
if ($dropped.Count -gt 0) {
	$failures.Add((Format-DroppedComponentMessage -PatchVersion $PatchVersion -BaseBuildNumber $BaseBuildNumber -Dropped $dropped.ToArray()))
}

# The registered version advances even when no files are copied, so read the binary itself.
$productCode = Get-MsiProperty -MsiPath $UpdateMsi -Name 'ProductCode'
$exeComponent = @((Get-MsiComponents -MsiPath $UpdateMsi).Values | Where-Object { $_.File -eq 'FieldWorks.exe' }) | Select-Object -First 1
if ($productCode -and $exeComponent) {
	$installer = New-Object -ComObject WindowsInstaller.Installer
	$exePath = $installer.GetType().InvokeMember('ComponentPath', 'GetProperty', $null, $installer, @($productCode, $exeComponent.ComponentId))
	$onDisk = if ($exePath -and (Test-Path -LiteralPath $exePath)) { (Get-Item -LiteralPath $exePath).VersionInfo.FileVersion } else { '(missing)' }
	Write-Output "FieldWorks.exe on disk: $onDisk (expected $PatchVersion)"
	if ($onDisk -ne $PatchVersion) {
		$failures.Add("FieldWorks.exe on disk is $onDisk after applying patch $PatchVersion; the patch did not install its files.")
	}
}
else {
	$failures.Add('Could not find the product code or the FieldWorks.exe component in the Update MSI.')
}

if ($failures.Count -eq 0) {
	Write-Output "[OK] Patch $PatchVersion installs on base $BaseBuildNumber over the latest published patch."
	exit 0
}
foreach ($failure in $failures) {
	Write-Output "ERROR: $failure"
	if ($env:GITHUB_ACTIONS -eq 'true') {
		Write-Output ('::error title=Patch install test failed::' + ($failure -replace "`r?`n", ' '))
	}
}
exit 1
