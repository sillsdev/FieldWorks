<#
.SYNOPSIS
Fails a patch build when the new patch drops a component that a patch published on the same base
shipped.

.DESCRIPTION
Compares the patch's upgraded MSI image against the ledger of every component that
earlier patches on this base added: the committed seed ledger plus the per-patch
ledgers published next to each .msp. Writes this patch's own ledger for publishing.
pyro already rejects dropping a component the base itself ships (PYRO0305), so only
patch-added components are checked here.

.PARAMETER MasterMsi
The base (Master) MSI rebuilt by the patch build.

.PARAMETER UpdateMsi
The upgraded (Update) MSI the patch was diffed from.

.PARAMETER PatchVersion
The new patch's product version, e.g. 9.3.12.2761.

.PARAMETER SeedLedger
Committed ledger covering patches published before per-patch ledgers existed. Optional.

.PARAMETER OutLedger
Where to write this patch's ledger.

.PARAMETER SkipPublished
Check against the seed ledger only, without reading published ledgers from the update bucket.
#>
[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)][string]$MasterMsi,
	[Parameter(Mandatory = $true)][string]$UpdateMsi,
	[Parameter(Mandatory = $true)][string]$BaseBuildNumber,
	[Parameter(Mandatory = $true)][string]$PatchVersion,
	[string]$SeedLedger,
	[Parameter(Mandatory = $true)][string]$OutLedger,
	[switch]$SkipPublished
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'PatchComponentLedger.psm1') -Force

$master = Get-MsiComponents -MsiPath $MasterMsi
$update = Get-MsiComponents -MsiPath $UpdateMsi

$added = New-Object System.Collections.Generic.List[object]
foreach ($id in $update.Keys) {
	if (-not $master.ContainsKey($id)) {
		$entry = $update[$id]
		$added.Add([pscustomobject]@{
				ComponentId  = $entry.ComponentId
				Component    = $entry.Component
				File         = $entry.File
				Feature      = $entry.Feature
				FirstShipped = $PatchVersion
				LastShipped  = $PatchVersion
			})
	}
}
Write-ComponentLedger -Path $OutLedger -Entries $added.ToArray() -Heading "Components patch $PatchVersion adds to base $BaseBuildNumber"
Write-Output "Patch $PatchVersion adds $($added.Count) components to base $BaseBuildNumber; ledger written to $OutLedger"

$ledgerFiles = New-Object System.Collections.Generic.List[string]
if ($SeedLedger -and (Test-Path -LiteralPath $SeedLedger)) { $ledgerFiles.Add($SeedLedger) }
if (-not $SkipPublished) {
	$downloads = Join-Path ([IO.Path]::GetTempPath()) "fw-patch-ledgers-b$BaseBuildNumber"
	New-Item -ItemType Directory -Force -Path $downloads | Out-Null
	foreach ($key in (Get-PublishedPatchKeys -BaseBuildNumber $BaseBuildNumber -Wildcard '*_components.tsv')) {
		$ledgerFiles.Add((Save-PublishedFile -Key $key -Directory $downloads))
	}
}
$ledger = Read-ComponentLedger -Path $ledgerFiles.ToArray()
Write-Output "Checking against $($ledger.Count) components from $($ledgerFiles.Count) ledger files"

$dropped = @($ledger.Values | Where-Object { -not $update.ContainsKey($_.ComponentId) } | Sort-Object File)
if ($dropped.Count -eq 0) {
	Write-Output '[OK] The patch keeps every component published patches on this base have shipped.'
	exit 0
}

$message = Format-DroppedComponentMessage -PatchVersion $PatchVersion -BaseBuildNumber $BaseBuildNumber -Dropped $dropped
Write-Output $message
if ($env:GITHUB_ACTIONS -eq 'true') {
	foreach ($entry in $dropped) {
		Write-Output "::error title=Patch drops a shipped component::Patch $PatchVersion drops $($entry.File) (component $($entry.ComponentId), feature $($entry.Feature)), shipped by patches $($entry.FirstShipped) to $($entry.LastShipped) on base $BaseBuildNumber. See Docs/workflows/patch-component-removal.md."
	}
}
exit 1
