<#
.SYNOPSIS
	Renders the comment-hygiene pull request comment from a scan report.

.DESCRIPTION
	Turns the report comment-hygiene.ps1 writes under -ReportPath into a markdown
	body for a sticky pull request comment, and publishes comment_path and
	has_violations as GitHub step outputs so the workflow can pick between posting
	and clearing. Violations are listed as a table of file links, categories, and
	comment text, capped at -MaxListed rows.

.PARAMETER ReportPath
	The JSON report comment-hygiene.ps1 writes. A missing file is an error, not an
	empty report: a scan always writes one.

.PARAMETER CommentPath
	Where to write the markdown body. Defaults beside the report.

.PARAMETER MaxListed
	How many violations the table carries before it summarizes the rest as a
	count.

.PARAMETER Sha
	Commit the file links resolve against. Pass a pull request's head commit;
	links fall back to plain text when this or Repository is unset.

.EXAMPLE
	Build/Agent/Build-CommentHygieneComment.ps1 -ReportPath Output/CommentHygiene/report.json
#>
[CmdletBinding()]
param(
	[string]$CommentPath,
	[string]$GitHubOutputPath = $env:GITHUB_OUTPUT,
	[int]$MaxListed = 25,
	[string]$ReportPath,
	[string]$Repository = $env:GITHUB_REPOSITORY,
	[string]$RunId = $env:GITHUB_RUN_ID,
	[string]$ServerUrl = $env:GITHUB_SERVER_URL,
	[string]$Sha = $env:HEAD_SHA
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrWhiteSpace($ReportPath)) {
	$ReportPath = Join-Path (Join-Path $repoRoot 'Output\CommentHygiene') 'comment-hygiene-report.json'
}
if ([string]::IsNullOrWhiteSpace($CommentPath)) {
	$CommentPath = Join-Path (Split-Path -Path $ReportPath -Parent) 'comment-hygiene-comment.md'
}
if ([string]::IsNullOrWhiteSpace($ServerUrl)) {
	$ServerUrl = 'https://github.com'
}
if ([string]::IsNullOrWhiteSpace($Sha)) {
	$Sha = $env:GITHUB_SHA
}

# PowerShell's current location and .NET's working directory can differ; this script
# reads through the first and writes through the second, so anchor both paths once.
if (-not [System.IO.Path]::IsPathRooted($ReportPath)) {
	$ReportPath = Join-Path (Get-Location).Path $ReportPath
}
if (-not [System.IO.Path]::IsPathRooted($CommentPath)) {
	$CommentPath = Join-Path (Get-Location).Path $CommentPath
}

$SkillPath = '.claude/skills/fieldworks-code-commenting/SKILL.md'

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

	# UTF8Encoding($false): a byte-order mark part way through the output file breaks
	# the runner's parse of every value after it.
	$encoding = New-Object System.Text.UTF8Encoding($false)
	[System.IO.File]::AppendAllText($GitHubOutputPath, "$Name=$Value$([System.Environment]::NewLine)", $encoding)
}

function Format-TableCell {
	<#
	.SYNOPSIS
		Renders comment text as a single-line markdown code span, truncated to
		-MaxLength characters.
	#>
	param(
		[Parameter(Mandatory = $true)]
		[AllowEmptyString()]
		[string]$Value,
		[int]$MaxLength = 90
	)

	$collapsed = ($Value -replace '\s+', ' ').Trim()
	if ([string]::IsNullOrEmpty($collapsed)) {
		return ''
	}
	if ($collapsed.Length -gt $MaxLength) {
		$collapsed = $collapsed.Substring(0, $MaxLength).TrimEnd() + '...'
	}

	# GitHub splits a table row on every unescaped pipe, inside a code span as much
	# as outside one. A fence has to outnumber any backtick it wraps.
	$escaped = $collapsed -replace '\|', '\|'
	$fence = if ($escaped.Contains('`')) { '``' } else { '`' }
	if ($escaped.StartsWith('`') -or $escaped.EndsWith('`')) {
		$escaped = " $escaped "
	}

	return "$fence$escaped$fence"
}

function Format-FileLink {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path,
		[Parameter(Mandatory = $true)]
		[int]$Line
	)

	$label = "${Path}:$Line"
	if ([string]::IsNullOrWhiteSpace($Repository) -or [string]::IsNullOrWhiteSpace($Sha)) {
		return "``$label``"
	}

	return "[$label]($ServerUrl/$Repository/blob/$Sha/$Path#L$Line)"
}

function Get-RunReference {
	if ([string]::IsNullOrWhiteSpace($Repository) -or [string]::IsNullOrWhiteSpace($RunId)) {
		return 'this check''s log'
	}

	return "[this check's log]($ServerUrl/$Repository/actions/runs/$RunId)"
}

if (-not (Test-Path -LiteralPath $ReportPath)) {
	throw "Comment-hygiene report not found: $ReportPath"
}

$report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
foreach ($field in @('base', 'violationCount', 'violations')) {
	if ($null -eq $report.PSObject.Properties[$field]) {
		throw "Comment-hygiene report is missing the '$field' field: $ReportPath"
	}
}

$violationCount = [int]$report.violationCount
$baseLabel = [string]$report.base
$hasViolations = $violationCount -gt 0

if ($hasViolations) {
	$listed = @(@($report.violations) | Select-Object -First $MaxListed)
	$commentLines = @(
		'### Comment hygiene (advisory)'
		''
		"$violationCount comment-style violation(s) in the lines this branch adds since ``$baseLabel``."
		'Advisory only -- no check fails on these, and the same violations appear as inline warnings on the Files changed tab.'
		''
		'| File | Category | Comment |'
		'| --- | --- | --- |'
	)

	foreach ($violation in $listed) {
		$commentLines += ('| {0} | `{1}` | {2} |' -f
			(Format-FileLink -Path ([string]$violation.file) -Line ([int]$violation.line)),
			([string]$violation.category),
			(Format-TableCell -Value ([string]$violation.text)))
	}

	$commentLines += ''
	if ($violationCount -gt $listed.Count) {
		$commentLines += "$($violationCount - $listed.Count) more not listed here -- see $(Get-RunReference)."
		$commentLines += ''
	}

	$commentLines += "Fix them per ``$SkillPath``."
	$commentLines += 'Running `.\build.ps1 -CommentHygiene` (or `.\test.ps1 -CommentHygiene`) enforces them locally, and'
	$commentLines += 're-wraps over-wide lines and repairs non-ASCII punctuation as it goes.'
}
else {
	$commentLines = @(
		'### Comment hygiene (advisory)'
		''
		"No comment-style violations in the lines this branch adds since ``$baseLabel``."
	)
}

$commentDirectory = Split-Path -Path $CommentPath -Parent
if (-not [string]::IsNullOrWhiteSpace($commentDirectory) -and -not (Test-Path -LiteralPath $commentDirectory)) {
	New-Item -ItemType Directory -Path $commentDirectory -Force | Out-Null
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($CommentPath, ($commentLines -join [System.Environment]::NewLine), $utf8NoBom)

$resolvedCommentPath = [System.IO.Path]::GetFullPath($CommentPath)
Write-GitHubOutputValue -Name 'comment_path' -Value $resolvedCommentPath
Write-GitHubOutputValue -Name 'has_violations' -Value ($hasViolations.ToString().ToLowerInvariant())
Write-GitHubOutputValue -Name 'violation_count' -Value ([string]$violationCount)

Write-Output ([pscustomobject]@{
	CommentPath = $resolvedCommentPath
	HasViolations = $hasViolations
	ViolationCount = $violationCount
})
