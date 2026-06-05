#!/usr/bin/env pwsh
[CmdletBinding()]
param(
	[string]$Configuration = "Debug",
	[string]$CoverageXmlPath = "",
	[string]$FocusPath = "Src\\Common\\Controls\\DetailControls\\",
	[string]$FocusClassRegex = "SIL\.FieldWorks\.Common\.Framework\.DetailControls\.(DataTree|Slice|ObjSeqHashMap)$",
	[string]$OutputMarkdownPath = "",
	[string]$OutputJsonPath = ""
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../../../..")
if ([string]::IsNullOrWhiteSpace($CoverageXmlPath)) {
	$CoverageXmlPath = Join-Path $repoRoot "Output/$Configuration/Coverage/coverage.cobertura.xml"
}
if ([string]::IsNullOrWhiteSpace($OutputMarkdownPath)) {
	$OutputMarkdownPath = Join-Path $repoRoot "Output/$Configuration/Coverage/coverage-gap-assessment.md"
}
if ([string]::IsNullOrWhiteSpace($OutputJsonPath)) {
	$OutputJsonPath = Join-Path $repoRoot "Output/$Configuration/Coverage/coverage-gap-assessment.json"
}

if (-not (Test-Path -LiteralPath $CoverageXmlPath)) {
	throw "Coverage XML not found: $CoverageXmlPath"
}

function Classify-Resolution {
	param(
		[string]$MethodName,
		[double]$LineRate,
		[string]$ClassName
	)

	$method = $MethodName.ToLowerInvariant()
	if ($method -match 'trace|debug|report') {
		return 'dead-code-or-debug-path-review'
	}
	if ($method -match 'onpaint|onlayout|onsizechanged|mouse|contextmenu|focus|splitter|begininvoke') {
		return 'simplify-architecture-or-add-ui-harness'
	}
	if ($method -match '^get_|^set_|checkdisposed|startswith|extraindent|callernodeequal|getflidifpossible|monitored|dorefresh') {
		return 'add-unit-tests'
	}
	if ($method -match 'handle.*command|getcan.*|insertobject|merge|split|delete') {
		return 'add-functional-tests'
	}
	if ($LineRate -eq 0) {
		return 'add-tests-or-evaluate-relevance'
	}
	return 'add-targeted-tests'
}

function ToPercent([double]$v) {
	return [math]::Round($v * 100, 2)
}

[xml]$coverage = Get-Content -LiteralPath $CoverageXmlPath

$classes = @()
$methods = @()

foreach ($pkg in $coverage.coverage.packages.package) {
	if ($null -eq $pkg.classes) { continue }
	foreach ($cls in $pkg.classes.class) {
		$file = [string]$cls.filename
		$className = [string]$cls.name
		$lineRate = ToPercent([double]$cls.'line-rate')
		$branchRate = ToPercent([double]$cls.'branch-rate')

		if ($file -like "*$FocusPath*" -or $className -match $FocusClassRegex) {
			$classes += [PSCustomObject]@{
				File = $file
				Class = $className
				LineRate = $lineRate
				BranchRate = $branchRate
			}

			if ($null -ne $cls.methods) {
				foreach ($m in $cls.methods.method) {
					$ml = ToPercent([double]$m.'line-rate')
					$mb = ToPercent([double]$m.'branch-rate')
					if ($ml -lt 100) {
						$recommendation = Classify-Resolution -MethodName ([string]$m.name) -LineRate $ml -ClassName $className
						$methods += [PSCustomObject]@{
							Class = $className
							Method = [string]$m.name
							LineRate = $ml
							BranchRate = $mb
							Resolution = $recommendation
						}
					}
				}
			}
		}
	}
}

$classesSorted = $classes | Sort-Object LineRate, File, Class
$methodsSorted = $methods | Sort-Object LineRate, Class, Method

$resolutionSummary = $methodsSorted |
	Group-Object Resolution |
	Sort-Object Count -Descending |
	ForEach-Object {
		[PSCustomObject]@{ Resolution = $_.Name; Count = $_.Count }
	}

$topGaps = $methodsSorted | Select-Object -First 60

$result = [PSCustomObject]@{
	GeneratedAt = (Get-Date).ToString('o')
	Configuration = $Configuration
	CoverageXml = $CoverageXmlPath
	FocusPath = $FocusPath
	FocusClassRegex = $FocusClassRegex
	ClassCoverage = $classesSorted
	MethodGaps = $topGaps
	ResolutionSummary = $resolutionSummary
}

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputJsonPath) -Force | Out-Null
$result | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $OutputJsonPath -Encoding utf8

$md = @()
$md += "# Coverage Gap Assessment"
$md += ""
$md += "- Generated: $($result.GeneratedAt)"
$md += "- Configuration: $Configuration"
$md += "- FocusPath: $FocusPath"
$md += ""
$md += "## Resolution Summary"
$md += ""
$md += "| Resolution | Count |"
$md += "|-----------|------:|"
+($resolutionSummary | ForEach-Object { $md += "| $($_.Resolution) | $($_.Count) |" })
$md += ""
$md += "## Focused Class Coverage"
$md += ""
$md += "| Line % | Branch % | File | Class |"
$md += "|-------:|---------:|------|-------|"
+($classesSorted | Select-Object -First 40 | ForEach-Object { $md += "| $($_.LineRate) | $($_.BranchRate) | $($_.File) | $($_.Class) |" })
$md += ""
$md += "## Top Method Gaps"
$md += ""
$md += "| Line % | Branch % | Class | Method | Suggested Resolution |"
$md += "|-------:|---------:|-------|--------|----------------------|"
+($topGaps | ForEach-Object { $md += "| $($_.LineRate) | $($_.BranchRate) | $($_.Class) | $($_.Method) | $($_.Resolution) |" })
$md += ""
$md += "## Suggested Next Actions"
$md += ""
$md += '1. Add deterministic unit tests for low-cost `add-unit-tests` gaps.'
$md += "2. For `simplify-architecture-or-add-ui-harness`, prefer extracting pure logic before adding fragile UI harnesses."
$md += "3. Review `dead-code-or-debug-path-review` items with maintainers before removing any code."

$md -join [Environment]::NewLine | Out-File -LiteralPath $OutputMarkdownPath -Encoding utf8

Write-Host "[OK] Coverage gap assessment written" -ForegroundColor Green
Write-Host "  Markdown: $OutputMarkdownPath" -ForegroundColor Gray
Write-Host "  JSON:     $OutputJsonPath" -ForegroundColor Gray
