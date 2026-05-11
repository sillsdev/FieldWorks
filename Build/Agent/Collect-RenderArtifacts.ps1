[CmdletBinding()]
param(
	[string]$SearchRoot,
	[string]$OutputDirectory,
	[string]$GitHubOutputPath = $env:GITHUB_OUTPUT
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrWhiteSpace($SearchRoot)) {
	$SearchRoot = $repoRoot
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
	$OutputDirectory = Join-Path (Join-Path $repoRoot 'Output') 'RenderArtifacts'
}

function Get-FullPath {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path
	)

	return [System.IO.Path]::GetFullPath($Path)
}

function Format-RelativePath {
	param(
		[Parameter(Mandatory = $true)]
		[string]$BasePath,
		[Parameter(Mandatory = $true)]
		[string]$TargetPath
	)

	$baseFullPath = Get-FullPath $BasePath
	$targetFullPath = Get-FullPath $TargetPath
	if (-not $baseFullPath.EndsWith([System.IO.Path]::DirectorySeparatorChar.ToString(), [System.StringComparison]::Ordinal)) {
		$baseFullPath += [System.IO.Path]::DirectorySeparatorChar
	}

	$baseUri = New-Object System.Uri($baseFullPath)
	$targetUri = New-Object System.Uri($targetFullPath)
	return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

function Format-ArtifactPath {
	param(
		[string]$Path
	)

	if ([string]::IsNullOrWhiteSpace($Path)) {
		return $null
	}

	return $Path.Replace([System.IO.Path]::DirectorySeparatorChar, '/')
}

function Copy-RenderArtifactFile {
	param(
		[Parameter(Mandatory = $true)]
		[string]$SourcePath,
		[Parameter(Mandatory = $true)]
		[string]$DestinationPath,
		[Parameter(Mandatory = $true)]
		[string]$OutputRoot
	)

	if (-not (Test-Path -LiteralPath $SourcePath)) {
		return $null
	}

	$destinationDirectory = Split-Path -Parent $DestinationPath
	if (-not (Test-Path -LiteralPath $destinationDirectory)) {
		New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
	}

	Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force
	return Format-ArtifactPath (Format-RelativePath -BasePath $OutputRoot -TargetPath $DestinationPath)
}

function Write-GitHubOutputValue {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Name,
		[Parameter(Mandatory = $true)]
		[string]$Value
	)

	if ([string]::IsNullOrWhiteSpace($GitHubOutputPath)) {
		return
	}

	$encoding = New-Object System.Text.UTF8Encoding($false)
	[System.IO.File]::AppendAllText($GitHubOutputPath, "$Name=$Value$([System.Environment]::NewLine)", $encoding)
}

function New-MarkdownReport {
	param(
		[Parameter(Mandatory = $true)]
		[object[]]$Entries,
		[Parameter(Mandatory = $true)]
		[string]$OutputRoot
	)

	$readmePath = Join-Path $OutputRoot 'README.md'
	$lines = @(
		'# Render comparison artifacts',
		'',
		"Render snapshot failures: $($Entries.Count)",
		'',
		'Open `index.html` after downloading this artifact to inspect expected, actual, and diff images.',
		'',
		'| Snapshot | Expected | Actual | Diff |',
		'|---|---|---|---|'
	)

	foreach ($entry in $Entries) {
		$expected = if ($entry.ExpectedPath) { "[expected]($($entry.ExpectedPath))" } else { 'missing' }
		$actual = if ($entry.ActualPath) { "[actual]($($entry.ActualPath))" } else { 'missing' }
		$diff = if ($entry.DiffPath) { "[diff]($($entry.DiffPath))" } else { 'missing' }
		$lines += "| $($entry.SourcePath) | $expected | $actual | $diff |"
	}

	Set-Content -Path $readmePath -Value $lines -Encoding UTF8
}

function New-HtmlReport {
	param(
		[Parameter(Mandatory = $true)]
		[object[]]$Entries,
		[Parameter(Mandatory = $true)]
		[string]$OutputRoot
	)

	$indexPath = Join-Path $OutputRoot 'index.html'
	$builder = New-Object System.Text.StringBuilder
	[void]$builder.AppendLine('<!doctype html>')
	[void]$builder.AppendLine('<html lang="en">')
	[void]$builder.AppendLine('<head>')
	[void]$builder.AppendLine('<meta charset="utf-8">')
	[void]$builder.AppendLine('<title>Render comparison artifacts</title>')
	[void]$builder.AppendLine('<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#1f2328}table{border-collapse:collapse;width:100%}th,td{border:1px solid #d0d7de;padding:8px;vertical-align:top}th{background:#f6f8fa;text-align:left}img{max-width:320px;max-height:240px;border:1px solid #d0d7de;background:#fff}.path{font-family:Consolas,monospace;font-size:12px;word-break:break-all}</style>')
	[void]$builder.AppendLine('</head>')
	[void]$builder.AppendLine('<body>')
	[void]$builder.AppendLine('<h1>Render comparison artifacts</h1>')
	[void]$builder.AppendLine("<p>Render snapshot failures: $($Entries.Count)</p>")
	[void]$builder.AppendLine('<table>')
	[void]$builder.AppendLine('<thead><tr><th>Snapshot</th><th>Expected</th><th>Actual</th><th>Diff</th></tr></thead>')
	[void]$builder.AppendLine('<tbody>')

	foreach ($entry in $Entries) {
		$snapshot = [System.Net.WebUtility]::HtmlEncode($entry.SourcePath)
		[void]$builder.AppendLine('<tr>')
		[void]$builder.AppendLine(('<td class="path">{0}</td>' -f $snapshot))
		foreach ($artifact in @(
			@{ PropertyName = 'ExpectedPath'; Description = 'Expected render' },
			@{ PropertyName = 'ActualPath'; Description = 'Actual render' },
			@{ PropertyName = 'DiffPath'; Description = 'Diff image' }
		)) {
			$propertyName = $artifact.PropertyName
			$path = $entry.$propertyName
			if ([string]::IsNullOrWhiteSpace($path)) {
				[void]$builder.AppendLine('<td>missing</td>')
				continue
			}

			$encodedPath = [System.Net.WebUtility]::HtmlEncode($path)
			$encodedAltText = [System.Net.WebUtility]::HtmlEncode(('{0} for {1}' -f $artifact.Description, $entry.SourcePath))
			[void]$builder.AppendLine(('<td><a href="{0}"><img src="{0}" alt="{1}"></a></td>' -f $encodedPath, $encodedAltText))
		}
		[void]$builder.AppendLine('</tr>')
	}

	[void]$builder.AppendLine('</tbody>')
	[void]$builder.AppendLine('</table>')
	[void]$builder.AppendLine('</body>')
	[void]$builder.AppendLine('</html>')

	Set-Content -Path $indexPath -Value $builder.ToString() -Encoding UTF8
}

$resolvedSearchRoot = Get-FullPath $SearchRoot
$resolvedOutputDirectory = Get-FullPath $OutputDirectory

if (-not (Test-Path -LiteralPath $resolvedSearchRoot)) {
	throw "Search root not found: $resolvedSearchRoot"
}

if (Test-Path -LiteralPath $resolvedOutputDirectory) {
	Remove-Item -LiteralPath $resolvedOutputDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

$receivedFiles = @(Get-ChildItem -Path $resolvedSearchRoot -Filter '*.received.png' -File -Recurse | Where-Object {
	-not $_.FullName.StartsWith($resolvedOutputDirectory, [System.StringComparison]::OrdinalIgnoreCase)
} | Sort-Object FullName)

$entries = @()
foreach ($receivedFile in $receivedFiles) {
	$receivedPath = $receivedFile.FullName
	$snapshotBasePath = $receivedPath.Substring(0, $receivedPath.Length - '.received.png'.Length)
	$snapshotName = [System.IO.Path]::GetFileName($snapshotBasePath)
	$relativeDirectory = Format-RelativePath -BasePath $resolvedSearchRoot -TargetPath (Split-Path -Parent $receivedPath)
	$destinationDirectory = Join-Path $resolvedOutputDirectory $relativeDirectory

	$expectedPath = Copy-RenderArtifactFile -SourcePath ($snapshotBasePath + '.verified.png') -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.expected.png')) -OutputRoot $resolvedOutputDirectory
	$actualPath = Copy-RenderArtifactFile -SourcePath $receivedPath -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.actual.png')) -OutputRoot $resolvedOutputDirectory
	$diffPath = Copy-RenderArtifactFile -SourcePath ($snapshotBasePath + '.diff.png') -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.diff.png')) -OutputRoot $resolvedOutputDirectory
	$expectedMetadataPath = Copy-RenderArtifactFile -SourcePath ($snapshotBasePath + '.verified.json') -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.expected.json')) -OutputRoot $resolvedOutputDirectory
	$actualMetadataPath = Copy-RenderArtifactFile -SourcePath ($snapshotBasePath + '.received.json') -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.actual.json')) -OutputRoot $resolvedOutputDirectory
	$diffMetadataPath = Copy-RenderArtifactFile -SourcePath ($snapshotBasePath + '.diff.json') -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.diff.json')) -OutputRoot $resolvedOutputDirectory

	$entries += [pscustomobject]@{
		SnapshotName = $snapshotName
		SourcePath = Format-ArtifactPath (Format-RelativePath -BasePath $resolvedSearchRoot -TargetPath $receivedPath)
		ExpectedPath = $expectedPath
		ActualPath = $actualPath
		DiffPath = $diffPath
		ExpectedMetadataPath = $expectedMetadataPath
		ActualMetadataPath = $actualMetadataPath
		DiffMetadataPath = $diffMetadataPath
	}
}

$hasArtifacts = $entries.Count -gt 0
Write-GitHubOutputValue -Name 'has_artifacts' -Value ($hasArtifacts.ToString().ToLowerInvariant())
Write-GitHubOutputValue -Name 'failure_count' -Value $entries.Count.ToString([System.Globalization.CultureInfo]::InvariantCulture)
Write-GitHubOutputValue -Name 'artifact_path' -Value $resolvedOutputDirectory

if (-not $hasArtifacts) {
	Write-Output ([pscustomobject]@{
		HasArtifacts = $false
		FailureCount = 0
		OutputDirectory = $resolvedOutputDirectory
	})
	return
}

$manifest = [pscustomobject]@{
	GeneratedAtUtc = [System.DateTime]::UtcNow.ToString('O', [System.Globalization.CultureInfo]::InvariantCulture)
	FailureCount = $entries.Count
	Artifacts = $entries
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $resolvedOutputDirectory 'manifest.json') -Encoding UTF8
New-MarkdownReport -Entries $entries -OutputRoot $resolvedOutputDirectory
New-HtmlReport -Entries $entries -OutputRoot $resolvedOutputDirectory

Write-Output ([pscustomobject]@{
	HasArtifacts = $true
	FailureCount = $entries.Count
	OutputDirectory = $resolvedOutputDirectory
})

Write-Host "::notice title=Render comparison artifacts::$($entries.Count) render comparison artifact set(s) collected."