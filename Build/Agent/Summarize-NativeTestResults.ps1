[CmdletBinding()]
param(
	[string]$Configuration = 'Debug',
	[string]$StepSummaryPath = $env:GITHUB_STEP_SUMMARY
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$nativeLogs = @(
	(Join-Path $repoRoot "Output/$Configuration/testGenericLib.exe.log"),
	(Join-Path $repoRoot "Output/$Configuration/TestViews.exe.log")
)

$tableRows = @()
foreach ($logPath in $nativeLogs) {
	$displayPath = Resolve-Path -LiteralPath $logPath -ErrorAction SilentlyContinue
	if (-not $displayPath) {
		Write-Host "::warning title=Native log missing::$logPath not found"
		$tableRows += "| $logPath | - | - | - | MISSING |"
		continue
	}

	$summaryLine = Get-Content -Path $logPath -ErrorAction SilentlyContinue |
		Select-String -Pattern 'Tests \[Ok-Fail-Error\]: \[(\d+)-(\d+)-(\d+)\]' |
		Select-Object -Last 1

	if (-not $summaryLine) {
		Write-Host "::warning title=Native summary missing::$logPath does not contain Unit++ summary line"
		$tableRows += "| $logPath | - | - | - | UNKNOWN |"
		continue
	}

	$match = [regex]::Match($summaryLine.Line, 'Tests \[Ok-Fail-Error\]: \[(\d+)-(\d+)-(\d+)\]')
	$okCount = [int]$match.Groups[1].Value
	$failCount = [int]$match.Groups[2].Value
	$errorCount = [int]$match.Groups[3].Value
	$status = if ($failCount -gt 0 -or $errorCount -gt 0) { 'FAIL' } else { 'PASS' }

	if ($status -eq 'FAIL') {
		Write-Host "::error title=Native test failure::$logPath => Ok=$okCount Fail=$failCount Error=$errorCount"
	}
	else {
		Write-Host "::notice title=Native test pass::$logPath => Ok=$okCount Fail=$failCount Error=$errorCount"
	}

	$tableRows += "| $logPath | $okCount | $failCount | $errorCount | $status |"
}

if (-not [string]::IsNullOrWhiteSpace($StepSummaryPath)) {
	Add-Content -Path $StepSummaryPath -Value '### Native test summary'
	Add-Content -Path $StepSummaryPath -Value ''
	Add-Content -Path $StepSummaryPath -Value '| Log | Ok | Fail | Error | Status |'
	Add-Content -Path $StepSummaryPath -Value '|---|---:|---:|---:|---|'
	foreach ($row in $tableRows) {
		Add-Content -Path $StepSummaryPath -Value $row
	}
}
