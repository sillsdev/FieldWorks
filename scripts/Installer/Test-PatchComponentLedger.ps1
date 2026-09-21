<#
.SYNOPSIS
Fails a patch build when the new Update MSI drops a component from the base or previous patch.

.DESCRIPTION
Compares the new Update MSI with the base MSI and the complete ledger from the immediately
previous published patch. Writes the complete update-minus-base ledger for this patch.

.PARAMETER MasterMsi
The base (Master) MSI rebuilt by the patch build.

.PARAMETER UpdateMsi
The upgraded (Update) MSI the patch was diffed from.

.PARAMETER PatchVersion
The new patch's product version, e.g. 9.3.12.2761.

.PARAMETER SeedLedger
Committed snapshot supplies the initial previous-patch component set for a base.

.PARAMETER OutLedger
Where to write this patch's ledger.

#>
[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)][string]$MasterMsi,
	[Parameter(Mandatory = $true)][string]$UpdateMsi,
	[Parameter(Mandatory = $true)][string]$BaseBuildNumber,
	[Parameter(Mandatory = $true)][string]$PatchVersion,
	[string]$SeedLedger,
	[Parameter(Mandatory = $true)][string]$OutLedger
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'PatchComponentLedger.psm1') -Force

$master = Get-MsiComponents -MsiPath $MasterMsi
$update = Get-MsiComponents -MsiPath $UpdateMsi

$publishedPatchKeys = @(Get-PublishedPatchKeys -BaseBuildNumber $BaseBuildNumber -Wildcard '*.msp')
$publishedLedgerKeys = @(Get-PublishedPatchKeys -BaseBuildNumber $BaseBuildNumber -Wildcard '*_components.tsv')
$previous = Select-PreviousPublishedPatch `
	-PatchKeys $publishedPatchKeys `
	-LedgerKeys $publishedLedgerKeys `
	-BaseBuildNumber $BaseBuildNumber `
	-PatchVersion $PatchVersion

$ledgerFiles = New-Object System.Collections.Generic.List[string]
if ($previous.LedgerKey) {
	$downloads = Join-Path ([IO.Path]::GetTempPath()) "fw-patch-ledgers-b$BaseBuildNumber"
	New-Item -ItemType Directory -Force -Path $downloads | Out-Null
	$ledgerFiles.Add((Save-PublishedFile -Key $previous.LedgerKey -Directory $downloads))
}
elseif ($SeedLedger -and (Test-Path -LiteralPath $SeedLedger)) {
	$ledgerFiles.Add($SeedLedger)
}
elseif ($previous.PatchKey) {
	throw "Published patch $($previous.PatchKey) has no matching ledger, and the bootstrap ledger '$SeedLedger' is unavailable."
}

$previousLedger = Read-ComponentLedger -Path $ledgerFiles.ToArray()
$required = @{}
foreach ($entry in $master.Values) { $required[$entry.ComponentId] = $entry }
foreach ($entry in $previousLedger.Values) {
	if (-not $required.ContainsKey($entry.ComponentId)) { $required[$entry.ComponentId] = $entry }
}
$dropped = @(Get-MissingComponents -Required $required -Available $update)
$newLedgerEntries = @(Get-UpdateMinusBaseLedgerEntries -Master $master -Update $update)
Write-ComponentLedger -Path $OutLedger -Entries $newLedgerEntries -Heading "Complete update-minus-base ledger for patch $PatchVersion on base $BaseBuildNumber"
Write-Output "Patch $PatchVersion ledger contains $($newLedgerEntries.Count) update-minus-base components; ledger written to $OutLedger"
Write-Output "Checking against $($required.Count) base and previous-patch components"

if ($dropped.Count -eq 0) {
	Write-Output '[OK] The patch keeps every base and immediately previous-patch component.'
	exit 0
}

$message = Format-DroppedComponentMessage -PatchVersion $PatchVersion -BaseBuildNumber $BaseBuildNumber -Dropped $dropped
Write-Output $message
if ($env:GITHUB_ACTIONS -eq 'true') {
	Write-Output ('::error title=Patch component removal::' + ($message -replace "`r?`n", ' '))
}
exit 1
