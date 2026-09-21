Set-StrictMode -Version Latest

# Later patches must keep every component a published patch on the base shipped, or Windows
# Installer silently installs nothing. The ledger records those components.

$script:UpdateBucket = 'https://flex-updates.s3.amazonaws.com'
$script:PatchPrefix = 'jobs/FieldWorks-Win-all-Release-Patch/'
$script:LedgerColumns = @('ComponentId', 'Component', 'File', 'Feature', 'FirstShipped', 'LastShipped')

function Invoke-MsiQuery {
	param(
		[Parameter(Mandatory = $true)]$Database,
		[Parameter(Mandatory = $true)][string]$Sql,
		[Parameter(Mandatory = $true)][int]$Columns
	)
	$view = $Database.GetType().InvokeMember('OpenView', 'InvokeMethod', $null, $Database, @($Sql))
	$viewType = $view.GetType()
	$viewType.InvokeMember('Execute', 'InvokeMethod', $null, $view, $null) | Out-Null
	$rows = New-Object System.Collections.Generic.List[object]
	while ($true) {
		$record = $viewType.InvokeMember('Fetch', 'InvokeMethod', $null, $view, $null)
		if ($null -eq $record) { break }
		$values = New-Object string[] $Columns
		for ($i = 1; $i -le $Columns; $i++) {
			$values[$i - 1] = $record.GetType().InvokeMember('StringData', 'GetProperty', $null, $record, @($i))
		}
		$rows.Add($values)
	}
	$viewType.InvokeMember('Close', 'InvokeMethod', $null, $view, $null) | Out-Null
	return , $rows
}

function Get-MsiComponents {
	<#
	.SYNOPSIS
	Returns the components of an MSI keyed by ComponentId, each with its key-path file name and
	feature.
	#>
	param([Parameter(Mandatory = $true)][string]$MsiPath)

	$installer = New-Object -ComObject WindowsInstaller.Installer
	# 0 = msiOpenDatabaseModeReadOnly
	$db = $installer.GetType().InvokeMember('OpenDatabase', 'InvokeMethod', $null, $installer, @((Resolve-Path -LiteralPath $MsiPath).Path, 0))

	$fileNames = @{}
	foreach ($row in (Invoke-MsiQuery -Database $db -Sql 'SELECT `File`,`FileName` FROM `File`' -Columns 2)) {
		# FileName is "SHORT|Long name" when a short name exists.
		$fileNames[$row[0]] = ($row[1] -split '\|')[-1]
	}
	$features = @{}
	foreach ($row in (Invoke-MsiQuery -Database $db -Sql 'SELECT `Feature_`,`Component_` FROM `FeatureComponents`' -Columns 2)) {
		$features[$row[1]] = $row[0]
	}
	$components = @{}
	foreach ($row in (Invoke-MsiQuery -Database $db -Sql 'SELECT `Component`,`ComponentId`,`KeyPath` FROM `Component`' -Columns 3)) {
		if ([string]::IsNullOrEmpty($row[1])) { continue }
		$file = ''
		if ($fileNames.ContainsKey($row[2])) { $file = $fileNames[$row[2]] }
		$feature = ''
		if ($features.ContainsKey($row[0])) { $feature = $features[$row[0]] }
		$components[$row[1].ToUpperInvariant()] = [pscustomobject]@{
			ComponentId = $row[1].ToUpperInvariant()
			Component   = $row[0]
			File        = $file
			Feature     = $feature
		}
	}
	[System.Runtime.InteropServices.Marshal]::ReleaseComObject($db) | Out-Null
	return $components
}

function Get-MsiProperty {
	param(
		[Parameter(Mandatory = $true)][string]$MsiPath,
		[Parameter(Mandatory = $true)][string]$Name
	)
	$installer = New-Object -ComObject WindowsInstaller.Installer
	$db = $installer.GetType().InvokeMember('OpenDatabase', 'InvokeMethod', $null, $installer, @((Resolve-Path -LiteralPath $MsiPath).Path, 0))
	$rows = Invoke-MsiQuery -Database $db -Sql "SELECT ``Value`` FROM ``Property`` WHERE ``Property`` = '$Name'" -Columns 1
	[System.Runtime.InteropServices.Marshal]::ReleaseComObject($db) | Out-Null
	if ($rows.Count -eq 0) { return $null }
	return $rows[0][0]
}

function Read-ComponentLedger {
	<#
	.SYNOPSIS
	Merges ledger files into one table keyed by ComponentId, widening each entry's shipped range.
	#>
	param([string[]]$Path)

	$ledger = @{}
	foreach ($file in $Path) {
		if (-not (Test-Path -LiteralPath $file)) { continue }
		foreach ($line in (Get-Content -LiteralPath $file)) {
			if ($line -match '^\s*(#|$)') { continue }
			$cells = $line -split "`t"
			if ($cells.Count -lt $script:LedgerColumns.Count) { continue }
			$id = $cells[0].ToUpperInvariant()
			$entry = [pscustomobject]@{
				ComponentId  = $id
				Component    = $cells[1]
				File         = $cells[2]
				Feature      = $cells[3]
				FirstShipped = $cells[4]
				LastShipped  = $cells[5]
			}
			if ($ledger.ContainsKey($id)) {
				$known = $ledger[$id]
				if ([version]$entry.FirstShipped -lt [version]$known.FirstShipped) { $known.FirstShipped = $entry.FirstShipped }
				if ([version]$entry.LastShipped -gt [version]$known.LastShipped) { $known.LastShipped = $entry.LastShipped }
			}
			else {
				$ledger[$id] = $entry
			}
		}
	}
	return $ledger
}

function Write-ComponentLedger {
	param(
		[Parameter(Mandatory = $true)][string]$Path,
		[Parameter(Mandatory = $true)][object[]]$Entries,
		[string]$Heading
	)
	$lines = New-Object System.Collections.Generic.List[string]
	if ($Heading) { $lines.Add("# $Heading") }
	$lines.Add('# ' + ($script:LedgerColumns -join "`t"))
	foreach ($entry in ($Entries | Sort-Object File, ComponentId)) {
		$lines.Add((($script:LedgerColumns | ForEach-Object { $entry.$_ }) -join "`t"))
	}
	# ASCII keeps the file byte-identical under Windows PowerShell 5.1 and PowerShell 7.
	Set-Content -LiteralPath $Path -Value $lines -Encoding Ascii
}

function Get-UpdateBucketKeys {
	<#
	.SYNOPSIS
	Lists the update-bucket keys under a prefix whose names match a wildcard.
	#>
	param(
		[Parameter(Mandatory = $true)][string]$Prefix,
		[Parameter(Mandatory = $true)][string]$Like
	)
	$keys = New-Object System.Collections.Generic.List[string]
	$token = $null
	do {
		$url = "$script:UpdateBucket/?list-type=2&prefix=$Prefix"
		if ($token) { $url += '&continuation-token=' + [uri]::EscapeDataString($token) }
		[xml]$page = (Invoke-WebRequest -Uri $url -UseBasicParsing).Content
		foreach ($item in @($page.ListBucketResult.Contents)) {
			if ($null -ne $item -and $item.Key -like $Like) { $keys.Add($item.Key) }
		}
		$token = $null
		if ($page.ListBucketResult.IsTruncated -eq 'true') { $token = $page.ListBucketResult.NextContinuationToken }
	} while ($token)
	return , $keys
}

function Get-PublishedPatchKeys {
	<#
	.SYNOPSIS
	Lists the patch-job keys published for one base whose names end with a wildcard.
	#>
	param(
		[Parameter(Mandatory = $true)][string]$BaseBuildNumber,
		[Parameter(Mandatory = $true)][string]$Wildcard
	)
	return , (Get-UpdateBucketKeys -Prefix $script:PatchPrefix -Like "*_b${BaseBuildNumber}_$Wildcard")
}

function Save-PublishedFile {
	param(
		[Parameter(Mandatory = $true)][string]$Key,
		[Parameter(Mandatory = $true)][string]$Directory
	)
	$target = Join-Path $Directory (Split-Path $Key -Leaf)
	Invoke-WebRequest -Uri "$script:UpdateBucket/$Key" -OutFile $target -UseBasicParsing
	return $target
}

function Get-PatchVersionFromKey {
	param([Parameter(Mandatory = $true)][string]$Key)
	# FieldWorks_<version>_b<base>_<arch>.<ext>
	return [version]((Split-Path $Key -Leaf) -split '_')[1]
}

function Format-DroppedComponentMessage {
	param(
		[Parameter(Mandatory = $true)][string]$PatchVersion,
		[Parameter(Mandatory = $true)][string]$BaseBuildNumber,
		[Parameter(Mandatory = $true)][object[]]$Dropped
	)
	$lines = New-Object System.Collections.Generic.List[string]
	foreach ($entry in $Dropped) {
		$lines.Add("Patch $PatchVersion drops component $($entry.ComponentId)")
		$lines.Add("  file:    $($entry.File)   (feature $($entry.Feature))")
		$lines.Add("  shipped: patches $($entry.FirstShipped) to $($entry.LastShipped) on base $BaseBuildNumber")
	}
	$lines.Add('Fix: restore the file to the output, or add a component stand-in (see')
	$lines.Add('     Docs/workflows/patch-component-removal.md), as well as an issue to remove')
	$lines.Add('     the file before cutting the next base build.')
	return ($lines -join [Environment]::NewLine)
}

Export-ModuleMember -Function Get-MsiComponents, Get-MsiProperty, Read-ComponentLedger, Write-ComponentLedger, Get-UpdateBucketKeys, Get-PublishedPatchKeys, Save-PublishedFile, Get-PatchVersionFromKey, Format-DroppedComponentMessage
