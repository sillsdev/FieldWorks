#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

# Import shared git helpers
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $ScriptDir 'GitHelpers.ps1')

function Get-BaseRef {
	if ($env:GITHUB_BASE_REF) { return "origin/$($env:GITHUB_BASE_REF)" }
	return (Get-DefaultBranchRef)
}

function Test-HasUtf8Bom {
	param([Parameter(Mandatory)][string]$Path)

	# Read only the first three bytes to check for a UTF-8 BOM to avoid loading the entire file.
	$buffer = [byte[]]::new(3)
	$stream = [System.IO.File]::OpenRead($Path)
	try {
		$bytesRead = $stream.Read($buffer, 0, 3)
	}
	finally {
		$stream.Dispose()
	}

	if ($bytesRead -lt 3) { return $false }
	return $buffer[0] -eq 0xEF -and $buffer[1] -eq 0xBB -and $buffer[2] -eq 0xBF
}

function Write-Utf8Text {
	param(
		[Parameter(Mandatory)][string]$Path,
		[Parameter(Mandatory)][string]$Content,
		[Parameter(Mandatory)][bool]$EmitBom
	)

	$encoding = New-Object System.Text.UTF8Encoding($EmitBom)
	[System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Get-OriginalLineEnding {
	param([Parameter(Mandatory)][string]$Content)

	if ($Content.Contains("`r`n")) { return "`r`n" }
	if ($Content.Contains("`n")) { return "`n" }
	if ($Content.Contains("`r")) { return "`r" }
	return [Environment]::NewLine
}

function Read-Utf8Text {
	param([Parameter(Mandatory)][string]$Path)

	$stream = [System.IO.File]::OpenRead($Path)
	try {
		$encoding = New-Object System.Text.UTF8Encoding($false, $true)
		$reader = New-Object System.IO.StreamReader($stream, $encoding, $true)
		try {
			return $reader.ReadToEnd()
		}
		finally {
			$reader.Dispose()
		}
	}
	finally {
		$stream.Dispose()
	}
}

function Get-TextFilesFromDiff {
	param([Parameter(Mandatory)][string]$Range)

	$files = New-Object System.Collections.Generic.List[string]
	foreach ($line in (git diff --numstat $Range)) {
		$parts = $line -split "`t", 3
		if ($parts.Length -eq 3 -and $parts[0] -ne '-' -and $parts[1] -ne '-') {
			$files.Add($parts[2])
		}
	}

	return $files.ToArray()
}

function Format-FileWhitespace {
	param([Parameter(Mandatory)][string]$Path)
	if (-not (Test-Path -LiteralPath $Path)) { return }
	try {
		$hasUtf8Bom = Test-HasUtf8Bom -Path $Path
		$raw = Read-Utf8Text -Path $Path
	}
 catch {
		Write-Host "Skipping non-UTF8 or binary file: $Path"
		return
	}
	$orig = $raw
	$lineEnding = Get-OriginalLineEnding -Content $raw
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
	$new = ($lines -join $lineEnding) + $lineEnding
	if ($new -ne $orig) {
		Write-Utf8Text -Path $Path -Content $new -EmitBom $hasUtf8Bom
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
	$range = "$base..HEAD"
	$fixFiles = Get-TextFilesFromDiff -Range $range
}

$files = $fixFiles | Where-Object { $_ -and (Test-Path $_) }

foreach ($f in $files) { Format-FileWhitespace -Path $f }

Write-Host "Whitespace fix completed. Review and stage the updated files before committing."
Write-Host "If check-whitespace reported an older commit in origin/main..HEAD, rewrite history with amend, squash, or rebase so that offending commit is no longer part of the branch."
exit 0
