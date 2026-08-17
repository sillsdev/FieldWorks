<#
.SYNOPSIS
	Attributes every full-repo comment-hygiene violation to its introducing commit.

.DESCRIPTION
	Runs Get-CommentHygieneViolations in full-repo mode, then git-blames each
	violating line to find who introduced it and when, so a human can triage
	existing debt deliberately instead of the ratchet gate accepting it
	silently forever. Writes one JSON array to -OutputPath.

.PARAMETER OutputPath
	Path to write the JSON report.

.EXAMPLE
	Build/Agent/comment-hygiene-blame.ps1 -OutputPath triage.json
#>
[CmdletBinding()]
param(
	[Parameter(Mandatory)][string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Import-Module (Join-Path $PSScriptRoot 'CommentHygiene.psm1') -Force

function Test-ExcludedPath {
	param([string] $Path)
	return ($Path -match '\.g\.cs$') -or ($Path -match 'Designer\.cs$')
}

function ConvertTo-RepoPath {
	param([string] $RelativePath)
	return Join-Path $repoRoot ($RelativePath -replace '/', [IO.Path]::DirectorySeparatorChar)
}

function Get-BlameInfo {
	param([string] $File, [int] $Line)

	$porcelain = git blame -L "$Line,$Line" --porcelain -- $File 2>$null
	if (-not $porcelain) { return $null }

	$sha = ($porcelain[0] -split ' ')[0]
	$authorLine = $porcelain | Where-Object { $_ -like 'author *' } | Select-Object -First 1
	$emailLine = $porcelain | Where-Object { $_ -like 'author-mail *' } | Select-Object -First 1
	$timeLine = $porcelain | Where-Object { $_ -like 'author-time *' } | Select-Object -First 1

	$author = if ($authorLine) { $authorLine.Substring(7) } else { 'unknown' }
	$email = if ($emailLine) { $emailLine.Substring(12).Trim('<', '>') } else { '' }
	$epoch = if ($timeLine) { [int64]($timeLine.Substring(12)) } else { 0 }
	$date = if ($epoch -gt 0) { [DateTimeOffset]::FromUnixTimeSeconds($epoch).UtcDateTime.ToString('yyyy-MM-dd') } else { '' }

	$subject = (git log -1 --format=%s $sha 2>$null)
	$bodyLines = git log -1 --format=%B $sha 2>$null
	$body = ($bodyLines -join "`n")
	$hasAiTrailer = $body -match '(?i)co-authored-by:.*claude|generated with claude'

	return [PSCustomObject]@{
		CommitSha            = $sha
		Author               = "$author <$email>"
		Date                 = $date
		Subject              = $subject
		HasAiCoAuthorTrailer = [bool]$hasAiTrailer
	}
}

$scopedGlobs = @(
	'*.cs', '*.ps1', '*.psm1',
	'*.cpp', '*.cxx', '*.cc', '*.c', '*.h', '*.hpp', '*.idl',
	'*.csproj', '*.vcxproj', '*.vcproj', '*.props', '*.targets', '*.proj', '*.axaml', '*.xaml'
)
$files = git ls-files $scopedGlobs | ForEach-Object { ConvertTo-RepoPath $_ } | Where-Object { -not (Test-ExcludedPath $_) }
Write-Host "comment-hygiene-blame: scanning $($files.Count) file(s)..."

$violations = Get-CommentHygieneViolations -Files $files
Write-Host "comment-hygiene-blame: $($violations.Count) violation(s) found; resolving blame..."

$report = New-Object System.Collections.ArrayList
$index = 0

foreach ($v in $violations) {
	$index++
	if ($index % 50 -eq 0) { Write-Host "  ...$index/$($violations.Count)" }

	$blame = Get-BlameInfo -File $v.File -Line $v.Line
	$relative = $v.File.Substring($repoRoot.Length + 1) -replace '\\', '/'

	[void]$report.Add([PSCustomObject]@{
		file                 = $relative
		line                 = $v.Line
		category             = $v.Category
		text                 = $v.Text
		commitSha            = if ($blame) { $blame.CommitSha } else { $null }
		author               = if ($blame) { $blame.Author } else { $null }
		date                 = if ($blame) { $blame.Date } else { $null }
		subject              = if ($blame) { $blame.Subject } else { $null }
		hasAiCoAuthorTrailer = if ($blame) { $blame.HasAiCoAuthorTrailer } else { $false }
	})
}

Set-CommentHygieneFileContent -Path $OutputPath -Lines @($report | ConvertTo-Json -Depth 4) -Utf8Bom $false
Write-Host "comment-hygiene-blame: wrote $($report.Count) record(s) to $OutputPath"
