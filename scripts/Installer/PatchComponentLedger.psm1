Set-StrictMode -Version Latest

# File-backed components under MSI APPFOLDER must remain present across the base
# and immediate previous patch.

$script:UpdateBucket = 'https://flex-updates.s3.amazonaws.com'
$script:PatchPrefix = 'jobs/FieldWorks-Win-all-Release-Patch/'
$script:LedgerColumns = @('ComponentId', 'Component', 'File', 'Feature')

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

function Get-MsiDirectoryName {
	<#
	.SYNOPSIS
	Extracts the target directory name from an MSI DefaultDir value.
	#>
	param(
		[Parameter(Mandatory = $true)][AllowEmptyString()][string]$DefaultDir
	)
	$target = ($DefaultDir -split ':', 2)[0]
	if ([string]::IsNullOrEmpty($target) -or $target -eq '.') { return '' }
	return ($target -split '\|', 2)[-1]
}

function Get-RelativeMsiFilePath {
	<#
	.SYNOPSIS
	Resolves a file path relative to the MSI APPFOLDER directory.
	#>
	param(
		[Parameter(Mandatory = $true)][hashtable]$Directories,
		[Parameter(Mandatory = $true)][string]$DirectoryId,
		[Parameter(Mandatory = $true)][AllowEmptyString()][string]$FileName
	)
	if ([string]::IsNullOrWhiteSpace($FileName)) { return $null }
	if ($DirectoryId -ieq 'APPFOLDER') { return $FileName }

	$segments = New-Object System.Collections.Generic.List[string]
	$visited = @{}
	$current = $DirectoryId
	while ($current -and $current -ine 'APPFOLDER') {
		if ($visited.ContainsKey($current) -or -not $Directories.ContainsKey($current)) { return $null }
		$visited[$current] = $true
		$directory = $Directories[$current]
		if (-not [string]::IsNullOrWhiteSpace($directory.Name)) { $segments.Insert(0, $directory.Name) }
		$current = $directory.Parent
	}
	if ($current -ine 'APPFOLDER') { return $null }
	$segments.Add($FileName)
	return ($segments -join '/')
}

function Get-MsiComponents {
	<#
	.SYNOPSIS
	Returns file-backed components under MSI APPFOLDER keyed by ComponentId, each
	with their relative path and feature.
	#>
	param([Parameter(Mandatory = $true)][string]$MsiPath)

	$installer = New-Object -ComObject WindowsInstaller.Installer
	# 0 = msiOpenDatabaseModeReadOnly
	$db = $installer.GetType().InvokeMember('OpenDatabase', 'InvokeMethod', $null, $installer, @((Resolve-Path -LiteralPath $MsiPath).Path, 0))

	$directories = @{}
	foreach ($row in (Invoke-MsiQuery -Database $db -Sql 'SELECT `Directory`,`Directory_Parent`,`DefaultDir` FROM `Directory`' -Columns 3)) {
		$name = Get-MsiDirectoryName -DefaultDir ([string]$row[2])
		$directories[$row[0]] = [pscustomobject]@{ Name = $name; Parent = $row[1] }
	}

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
	foreach ($row in (Invoke-MsiQuery -Database $db -Sql 'SELECT `Component`,`ComponentId`,`Directory_`,`KeyPath` FROM `Component`' -Columns 4)) {
		if ([string]::IsNullOrEmpty($row[1]) -or [string]::IsNullOrEmpty($row[2]) -or -not $fileNames.ContainsKey($row[3])) { continue }
		$file = Get-RelativeMsiFilePath -Directories $directories -DirectoryId $row[2] -FileName $fileNames[$row[3]]
		if ($null -eq $file) { continue }
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

function Read-ComponentLedger {
	<#
	.SYNOPSIS
	Reads a ledger of file-backed components under MSI APPFOLDER keyed by ComponentId.
	#>
	param([Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$Path)

	$ledger = @{}
	foreach ($file in $Path) {
		if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
			throw "Component ledger file not found: $file"
		}
		$lineNumber = 0
		foreach ($line in (Get-Content -LiteralPath $file)) {
			$lineNumber++
			if ($line -match '^\s*(#|$)') { continue }
			$cells = [regex]::Split($line, [char]9)
			if ($cells.Count -ne $script:LedgerColumns.Count -or @($cells | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0) {
				throw "Malformed component ledger row in '$file' at line $lineNumber. Expected four non-empty tab-separated fields."
			}
			$id = $cells[0].ToUpperInvariant()
			if ($ledger.ContainsKey($id)) {
				throw "Duplicate component ID '$id' in component ledger '$file' at line $lineNumber."
			}
			$ledger[$id] = [pscustomobject]@{
				ComponentId = $id
				Component   = $cells[1]
				File        = $cells[2]
				Feature     = $cells[3]
			}
		}
	}
	return $ledger
}

function Write-ComponentLedger {
	<#
	.SYNOPSIS
	Writes file-backed components under MSI APPFOLDER to a tab-separated ledger.
	#>
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
	<#
	.SYNOPSIS
	Downloads one published file to a local directory.
	#>
	param(
		[Parameter(Mandatory = $true)][string]$Key,
		[Parameter(Mandatory = $true)][string]$Directory
	)
	$target = Join-Path $Directory (Split-Path $Key -Leaf)
	Invoke-WebRequest -Uri "$script:UpdateBucket/$Key" -OutFile $target -UseBasicParsing
	return $target
}

function Get-PatchVersionFromKey {
	<#
	.SYNOPSIS
	Parses a patch version from a published object key.
	#>
	param([Parameter(Mandatory = $true)][string]$Key)
	# FieldWorks_<version>_b<base>_<arch>.<ext>
	return [version]((Split-Path $Key -Leaf) -split '_')[1]
}

function Select-PreviousPublishedPatch {
	<#
	.SYNOPSIS
	Selects the previous patch and matching ledger for a base and version.
	#>
	param(
		[Parameter(Mandatory = $true)][string[]]$PatchKeys,
		[string[]]$LedgerKeys = @(),
		[Parameter(Mandatory = $true)][string]$BaseBuildNumber,
		[Parameter(Mandatory = $true)][string]$PatchVersion
	)
	$baseMarker = "_b${BaseBuildNumber}_"
	$targetVersion = [version]$PatchVersion
	$eligiblePatches = New-Object System.Collections.Generic.List[object]
	foreach ($key in $PatchKeys) {
		if (-not $key.Contains($baseMarker)) { continue }
		try { $version = Get-PatchVersionFromKey -Key $key }
		catch { continue }
		if ($version -lt $targetVersion) {
			$eligiblePatches.Add([pscustomobject]@{ Key = $key; Version = $version })
		}
	}
	if ($eligiblePatches.Count -eq 0) {
		return [pscustomobject]@{
			PatchKey       = $null
			LedgerKey      = $null
		}
	}
	$orderedPatches = @($eligiblePatches | Sort-Object Version)
	$previous = $orderedPatches[$orderedPatches.Count - 1]
	$expectedLedgerKey = $previous.Key -replace '\.msp$', '_components.tsv'
	$matchingLedger = @($LedgerKeys | Where-Object { $_ -eq $expectedLedgerKey })
	if ($matchingLedger.Count -gt 0) {
		return [pscustomobject]@{
			PatchKey       = $previous.Key
			LedgerKey      = $matchingLedger[0]
		}
	}
	$earlierLedgerPair = $false
	foreach ($candidate in $orderedPatches) {
		if ($candidate.Version -ge $previous.Version) { continue }
		$candidateLedger = $candidate.Key -replace '\.msp$', '_components.tsv'
		if (@($LedgerKeys | Where-Object { $_ -eq $candidateLedger }).Count -gt 0) {
			$earlierLedgerPair = $true
			break
		}
	}
	if ($earlierLedgerPair) {
		throw "Bootstrap has ended: published patch $($previous.Key) has no matching ledger $expectedLedgerKey. Publish the ledger beside that patch before building another patch."
	}
	return [pscustomobject]@{
		PatchKey       = $previous.Key
		LedgerKey      = $null
	}
}

function Format-RemovedComponentRemediation {
	<#
	.SYNOPSIS
	Formats remediation for file-backed components under MSI APPFOLDER missing from a patch.
	#>
	param([Parameter(Mandatory = $true)][object[]]$Dropped)
	$lines = New-Object System.Collections.Generic.List[string]
	$lines.Add('Remediation:')
	$lines.Add('Add the output path for each missing file to RemovedSinceLastBase in Build/Installer.legacy.targets; preserve the relative output path when one exists:')
	foreach ($entry in ($Dropped | Sort-Object File, ComponentId)) {
		$path = '$(dir-outputBase)/' + $entry.File
		$lines.Add(('  <RemovedSinceLastBase Include="' + $path + '" />'))
	}
	$lines.Add('Create an issue to remove the placeholder before the next base build.')
	return ($lines -join [Environment]::NewLine)
}

function Get-UpdateMinusBaseLedgerEntries {
	<#
	.SYNOPSIS
	Returns update file-backed components under MSI APPFOLDER absent from the base MSI.
	#>
	param(
		[Parameter(Mandatory = $true)][hashtable]$Master,
		[Parameter(Mandatory = $true)][hashtable]$Update
	)
	$entries = New-Object System.Collections.Generic.List[object]
	foreach ($id in $Update.Keys) {
		if ($Master.ContainsKey($id)) { continue }
		$entry = $Update[$id]
		$entries.Add([pscustomobject]@{
			ComponentId = $entry.ComponentId
			Component   = $entry.Component
			File        = $entry.File
			Feature     = $entry.Feature
		})
	}
	return $entries.ToArray()
}

function Get-MissingComponents {
	<#
	.SYNOPSIS
	Finds required file-backed components under MSI APPFOLDER absent from the
	available set.
	#>
	param(
		[Parameter(Mandatory = $true)][hashtable]$Required,
		[Parameter(Mandatory = $true)][hashtable]$Available
	)
	$missing = New-Object System.Collections.Generic.List[object]
	foreach ($id in $Required.Keys) {
		if (-not $Available.ContainsKey($id)) { $missing.Add($Required[$id]) }
	}
	return ($missing | Sort-Object File, ComponentId)
}

function Format-DroppedComponentMessage {
	<#
	.SYNOPSIS
	Formats a diagnostic for file-backed components under MSI APPFOLDER
	dropped by a patch.
	#>
	param(
		[Parameter(Mandatory = $true)][string]$PatchVersion,
		[Parameter(Mandatory = $true)][string]$BaseBuildNumber,
		[Parameter(Mandatory = $true)][object[]]$Dropped
	)
	$lines = New-Object System.Collections.Generic.List[string]
	foreach ($entry in $Dropped) {
		$lines.Add("Patch $PatchVersion drops file-backed component $($entry.ComponentId)")
		$lines.Add("  file:    $($entry.File)   (feature $($entry.Feature))")
		$lines.Add("  base:    $BaseBuildNumber")
	}
	$lines.Add((Format-RemovedComponentRemediation -Dropped $Dropped))
	return ($lines -join [Environment]::NewLine)
}

Export-ModuleMember -Function Get-MsiDirectoryName, Get-RelativeMsiFilePath, Get-MsiComponents, Read-ComponentLedger, Write-ComponentLedger, Get-UpdateBucketKeys, Get-PublishedPatchKeys, Save-PublishedFile, Get-PatchVersionFromKey, Select-PreviousPublishedPatch, Get-UpdateMinusBaseLedgerEntries, Get-MissingComponents, Format-RemovedComponentRemediation, Format-DroppedComponentMessage
