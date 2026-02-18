#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

# Import shared git helpers
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $ScriptDir 'GitHelpers.ps1')

function Get-BaseRef {
	if ($env:GITHUB_BASE_REF) { return "origin/$($env:GITHUB_BASE_REF)" }
	return (Get-DefaultBranchRef)
}

function Format-FileWhitespace {
	param([Parameter(Mandatory)][string]$Path)
	if (-not (Test-Path -LiteralPath $Path)) { return }
	try {
		$raw = Get-Content -LiteralPath $Path -Raw -Encoding utf8
	}
 catch {
		Write-Host "Skipping non-UTF8 or binary file: $Path"
		return
	}
	$orig = $raw
	# Normalize newlines to real LF characters for processing
	$normalized = $raw -replace '\r\n', "`n" -replace '\r', "`n"
	# Build a mutable list of lines and trim trailing spaces/tabs
	$lines = New-Object System.Collections.Generic.List[string]
	foreach ($line in ($normalized -split "`n")) {
		$lines.Add(($line -replace '[ \t]+$', ''))
	}
	# Remove trailing blank lines
	while ($lines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($lines[$lines.Count - 1])) { $lines.RemoveAt($lines.Count - 1) }
	# Join back and ensure exactly one trailing newline
	$new = ($lines -join "`n") + "`n"
	if ($new -ne $orig) {
		Set-Content -LiteralPath $Path -Value $new -Encoding utf8 -NoNewline
		Write-Host "Fixed whitespace: $Path"
	}
}

# Prefer files from the most recent check run (check-results.log), else fall back to diff vs base
$fixFiles = @()
if (Test-Path -LiteralPath 'check-results.log') {
	$fixFiles = Get-Content -LiteralPath 'check-results.log' | Where-Object { $_ -match '^[^:]+:[1-9][0-9]*:' } | ForEach-Object { ($_ -split ':')[0] } | Select-Object -Unique
	if ($fixFiles.Count -gt 0) {
		Write-Host "Fixing whitespace for files listed in check-results.log"
	}
}
if (-not $fixFiles -or $fixFiles.Count -eq 0) {
	$base = Get-BaseRef
	Write-Host "Fixing whitespace for files changed since $base..HEAD"
	$fixFiles = git diff --name-only "$base"..HEAD
}

$files = $fixFiles | Where-Object { $_ -and (Test-Path $_) }

foreach ($f in $files) { Format-FileWhitespace -Path $f }

Write-Host "Whitespace fix completed. Review changes, commit, and rebase as needed."
exit 0
