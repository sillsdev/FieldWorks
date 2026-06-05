#!/usr/bin/env pwsh
[CmdletBinding()]
param(
	[string]$Configuration = "Debug",
	[string]$TestFilter = "",
	[string]$TestProject = "",
	[switch]$NoBuild,
	[string]$OutputSubDir = "Coverage",
	[string]$FocusPath = "Src\\Common\\Controls\\DetailControls\\"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$testScript = Join-Path $repoRoot "test.ps1"
if (-not (Test-Path -LiteralPath $testScript)) {
	Write-Host "[ERROR] Could not find test.ps1 at $testScript" -ForegroundColor Red
	exit 1
}

$outputRoot = Join-Path $repoRoot "Output/$Configuration/$OutputSubDir"
$htmlDir = Join-Path $outputRoot "html"
$coverageXml = Join-Path $outputRoot "coverage.cobertura.xml"
$summaryJson = Join-Path $outputRoot "coverage-summary.json"
$summaryMd = Join-Path $outputRoot "coverage-summary.md"

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

$toolsDir = Join-Path $repoRoot ".tools"
New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null

function Ensure-Tool {
	param(
		[string]$ExeName,
		[string]$PackageId
	)

	$exePath = Join-Path $toolsDir "$ExeName.exe"
	if (-not (Test-Path -LiteralPath $exePath)) {
		Write-Host "Installing $PackageId to $toolsDir..." -ForegroundColor Cyan
		$null = & dotnet tool install --tool-path $toolsDir $PackageId
		if ($LASTEXITCODE -ne 0) {
			throw "Failed to install tool '$PackageId'"
		}
	}

	if (-not (Test-Path -LiteralPath $exePath)) {
		throw "Tool executable not found after install: $exePath"
	}

	return $exePath
}

function To-Percent {
	param([double]$Value)
	if ($Value -le 1.0) {
		return [math]::Round($Value * 100, 2)
	}
	return [math]::Round($Value, 2)
}

$dotnetCoverageExe = Ensure-Tool -ExeName "dotnet-coverage" -PackageId "dotnet-coverage"
$reportGeneratorExe = Ensure-Tool -ExeName "reportgenerator" -PackageId "dotnet-reportgenerator-globaltool"

Write-Host "Collecting coverage..." -ForegroundColor Cyan
$testArgs = @(
	"powershell.exe",
	"-NoProfile",
	"-ExecutionPolicy",
	"Bypass",
	"-File",
	$testScript,
	"-Configuration",
	$Configuration
)

if ($NoBuild) {
	$testArgs += "-NoBuild"
}
if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
	$testArgs += "-TestFilter"
	$testArgs += $TestFilter
}
if (-not [string]::IsNullOrWhiteSpace($TestProject)) {
	$testArgs += "-TestProject"
	$testArgs += $TestProject
}

$collectArgs = @(
	"collect",
	"--output-format",
	"cobertura",
	"--output",
	$coverageXml,
	"--"
) + $testArgs

& $dotnetCoverageExe $collectArgs
if ($LASTEXITCODE -ne 0) {
	throw "Coverage collection failed"
}

if (-not (Test-Path -LiteralPath $coverageXml)) {
	throw "Coverage file was not produced: $coverageXml"
}

Write-Host "Generating report artifacts..." -ForegroundColor Cyan
& $reportGeneratorExe "-reports:$coverageXml" "-targetdir:$htmlDir" "-reporttypes:Html;MarkdownSummary;JsonSummary"
if ($LASTEXITCODE -ne 0) {
	throw "ReportGenerator failed"
}

[xml]$coverage = Get-Content -LiteralPath $coverageXml

$lineRate = To-Percent([double]$coverage.coverage.'line-rate')
$branchRate = To-Percent([double]$coverage.coverage.'branch-rate')
$linesCovered = [int]$coverage.coverage.'lines-covered'
$linesValid = [int]$coverage.coverage.'lines-valid'
$branchesCovered = [int]$coverage.coverage.'branches-covered'
$branchesValid = [int]$coverage.coverage.'branches-valid'

$classRows = @()
foreach ($pkg in $coverage.coverage.packages.package) {
	if (-not $pkg.classes) {
		continue
	}
	foreach ($cls in $pkg.classes.class) {
		$classRows += [PSCustomObject]@{
			Package = [string]$pkg.name
			Class = [string]$cls.name
			File = [string]$cls.filename
			LineRate = To-Percent([double]$cls.'line-rate')
			BranchRate = To-Percent([double]$cls.'branch-rate')
		}
	}
}

$lowestCoverageClasses = $classRows |
	Where-Object { $_.LineRate -lt 100 } |
	Sort-Object LineRate, File, Class |
	Select-Object -First 25

$focusedCoverageClasses = $classRows |
	Where-Object { $_.File -like "*$FocusPath*" -and $_.LineRate -lt 100 } |
	Sort-Object LineRate, File, Class |
	Select-Object -First 25

$summaryObject = [PSCustomObject]@{
	GeneratedAt = (Get-Date).ToString("o")
	Configuration = $Configuration
	TestFilter = $TestFilter
	TestProject = $TestProject
	Coverage = [PSCustomObject]@{
		LineRate = $lineRate
		BranchRate = $branchRate
		LinesCovered = $linesCovered
		LinesValid = $linesValid
		BranchesCovered = $branchesCovered
		BranchesValid = $branchesValid
	}
	LowestCoverageClasses = $lowestCoverageClasses
	FocusedCoverageClasses = $focusedCoverageClasses
	Artifacts = [PSCustomObject]@{
		CoberturaXml = $coverageXml
		HtmlIndex = (Join-Path $htmlDir "index.html")
		MarkdownSummary = (Join-Path $htmlDir "Summary.md")
		JsonSummary = (Join-Path $htmlDir "Summary.json")
	}
}

$summaryObject | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $summaryJson -Encoding utf8

$md = @()
$md += "# Coverage Summary"
$md += ""
$md += "- Generated: $($summaryObject.GeneratedAt)"
$md += "- Configuration: $Configuration"
$md += "- TestFilter: $TestFilter"
$md += "- TestProject: $TestProject"
$md += ""
$md += "## Overall"
$md += ""
$md += "- Line coverage: $lineRate% ($linesCovered / $linesValid)"
$md += "- Branch coverage: $branchRate% ($branchesCovered / $branchesValid)"
$md += ""
$md += "## Lowest Coverage Classes (Top 25)"
$md += ""
$md += "| File | Class | Line % | Branch % |"
$md += "|------|-------|--------|----------|"
foreach ($row in $lowestCoverageClasses) {
	$md += "| $($row.File) | $($row.Class) | $($row.LineRate) | $($row.BranchRate) |"
}
$md += ""
$md += "## Artifacts"
$md += ""
$md += "- Cobertura XML: $coverageXml"
$md += "- HTML report: $(Join-Path $htmlDir 'index.html')"
$md += "- ReportGenerator markdown: $(Join-Path $htmlDir 'Summary.md')"
$md += "- ReportGenerator json: $(Join-Path $htmlDir 'Summary.json')"
$md += ""
$md += "## Focused Coverage Classes (Top 25)"
$md += ""
$md += "- FocusPath: $FocusPath"
$md += ""
$md += "| File | Class | Line % | Branch % |"
$md += "|------|-------|--------|----------|"
foreach ($row in $focusedCoverageClasses) {
	$md += "| $($row.File) | $($row.Class) | $($row.LineRate) | $($row.BranchRate) |"
}

$md -join [Environment]::NewLine | Out-File -LiteralPath $summaryMd -Encoding utf8

Write-Host "[OK] Coverage collection complete" -ForegroundColor Green
Write-Host "  Summary markdown: $summaryMd" -ForegroundColor Gray
Write-Host "  Summary json:     $summaryJson" -ForegroundColor Gray
Write-Host "  HTML report:      $(Join-Path $htmlDir 'index.html')" -ForegroundColor Gray