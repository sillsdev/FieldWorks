<#
.SYNOPSIS
	One-time non-ascii-punctuation comment repair sweep for an explicit file list.

.DESCRIPTION
	Runs Get-CommentHygieneViolations against the given files, filtered to
	the non-ascii-punctuation category, and applies Repair-CommentLine to each hit,
	writing fixes back to disk. Reports how many lines were fixed, in how
	many files, and lists any (file, line, text) that could not be
	auto-fixed because the line carries a character outside
	Get-NonAsciiReplacementMap, so a human can handle those individually.

	Accepts an explicit file list rather than a commit SHA or -Full scope,
	since the caller is expected to already know which files are in scope
	(e.g. from a comment-hygiene-blame.ps1 triage report).

.PARAMETER Files
	Paths (absolute or relative to the current directory) to .cs files to sweep.

.EXAMPLE
	Build/Agent/comment-hygiene-repair.ps1 -Files (Get-Content scoped-files.txt)
#>
[CmdletBinding()]
param(
	[Parameter(Mandatory)][string[]] $Files
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'CommentHygiene.psm1') -Force

function Test-Utf8Bom {
	param([string] $Path)
	$bytes = [System.IO.File]::ReadAllBytes($Path)
	return ($bytes.Length -ge 3) -and ($bytes[0] -eq 0xEF) -and ($bytes[1] -eq 0xBB) -and ($bytes[2] -eq 0xBF)
}

$resolvedFiles = $Files | ForEach-Object { (Resolve-Path -LiteralPath $_).Path }

# Assign before piping -- Get-CommentHygieneViolations's comma-return makes a direct pipe into Where-Object deliver one bundled array instead of filtering per-element.
$allViolations = Get-CommentHygieneViolations -Files $resolvedFiles
$violations = $allViolations | Where-Object { $_.Category -eq 'non-ascii-punctuation' }

$fixedFiles = @{}
$fixedCount = 0
$unrepairable = New-Object System.Collections.ArrayList

foreach ($v in $violations) {
	$hadBom = Test-Utf8Bom -Path $v.File
	$fileLines = @(Get-Content -LiteralPath $v.File -Encoding UTF8)
	$fixedLine = Repair-CommentLine -Line $fileLines[$v.Line - 1]
	if ($null -eq $fixedLine) {
		[void]$unrepairable.Add($v)
		continue
	}

	$fileLines[$v.Line - 1] = $fixedLine
	Set-CommentHygieneFileContent -Path $v.File -Lines $fileLines -Utf8Bom $hadBom
	$fixedFiles[$v.File] = $true
	$fixedCount++
}

Write-Host "comment-hygiene-repair: fixed $fixedCount non-ascii-punctuation comment line(s) in $($fixedFiles.Keys.Count) file(s)."

if ($unrepairable.Count -gt 0) {
	Write-Host ''
	Write-Host "comment-hygiene-repair: $($unrepairable.Count) violation(s) could NOT be auto-fixed (unmapped character(s)); handle these individually:" -ForegroundColor Yellow
	foreach ($v in $unrepairable) {
		Write-Host ("  {0}:{1} {2}" -f $v.File, $v.Line, $v.Text)
	}
}
