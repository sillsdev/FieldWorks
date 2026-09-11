<#
.SYNOPSIS
	Static compatibility check for PowerShell that must run under both 5.1 and 7.

.DESCRIPTION
	Applies to any script this repo ships to developers or CI. Two independent
	static layers, neither of which requires more than one PowerShell engine to
	actually be installed:

	1. A dependency-free regex scan for known gotchas where 5.1 and 7 both
	   parse the same text successfully but disagree on its meaning, so no
	   AST-based tool can see the difference: the backtick u{} escape
	   (5.1 silently drops it instead of resolving the code point) and the
	   utf8BOM/utf8NoBOM -Encoding values (5.1 does not recognize them at
	   all). This layer always runs and never needs installing anything.

	2. PSScriptAnalyzer's PSUseCompatibleSyntax rule, which catches
	   structural grammar additions (ternary, null-coalescing, the
	   null-conditional operators, pipeline chain operators, and more) by
	   checking the parsed script against Microsoft's maintained per-version
	   grammar profiles -- it does not need 5.1 itself to be installed,
	   only its own module. This layer is best-effort: if the module is
	   missing and cannot be installed (offline, restricted network), this
	   script warns and skips it rather than failing the build over a
	   missing optional dependency.

	Neither layer is a substitute for actually running under both engines:
	that is the only way to catch every possible semantic difference, and
	it requires both engines to be present, which this script does not
	assume. CI (windows-2022 runners ship both powershell.exe and pwsh) runs
	CommentHygiene.Tests.ps1 under both as that real-runtime check; this
	script is the cheaper static complement, runnable anywhere.

.PARAMETER Full
	Report every hit without failing.

.EXAMPLE
	Build/Agent/powershell-compat.ps1
#>
[CmdletBinding()]
param(
	[switch] $Full
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'CommentHygiene.psm1') -Force

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
# Every PowerShell file under Build, not just Build/Agent: build.ps1 loads
# modules from there under Windows PowerShell 5.1 in CI.
$scanRoots = @($PSScriptRoot, (Join-Path $repoRoot 'Build'))
$targetFiles = @($scanRoots | ForEach-Object {
		Get-ChildItem -LiteralPath $_ -Recurse -File -ErrorAction SilentlyContinue
	} |
	Where-Object { $_.Extension -eq '.ps1' -or $_.Extension -eq '.psm1' } |
	ForEach-Object { $_.FullName } | Sort-Object -Unique)

$violations = New-Object System.Collections.ArrayList

# Layer 1: dependency-free regex scan for known parse-both-ways-differently gotchas.
$gotchaPatterns = [ordered]@{
	'backtick-unicode-escape' = @{
		Pattern = '`[uU]\{[0-9a-fA-F]+\}'
		Message = 'Backtick u{} escape is PowerShell 6+ only; 5.1 drops the backtick and keeps the literal text. Use [char] / [char]::ConvertFromUtf32 instead.'
	}
	'ps7-only-encoding-value' = @{
		Pattern = '-Encoding\s+([''"]?)(utf8BOM|utf8NoBOM)\1\b'
		Message = '-Encoding utf8BOM/utf8NoBOM is PowerShell 6+ only and throws a parameter-binding error under 5.1. Use System.Text.UTF8Encoding directly for explicit BOM control.'
	}
}

# Excludes itself: $gotchaPatterns must spell out each pattern's literal
# text, which would otherwise flag the definition line as an instance of it.
$regexScanFiles = $targetFiles | Where-Object { $_ -ne (Join-Path $repoRoot 'Build/Agent/powershell-compat.ps1') }

foreach ($file in $regexScanFiles) {
	$lines = @(Get-Content -LiteralPath $file -Encoding UTF8)
	# Comment lines are excluded: this tooling's own doc comments describe the
	# gotcha patterns in prose, which would otherwise self-match as a violation.
	$classification = Get-CommentLineClassification -Lines $lines -Language 'PowerShell'
	for ($i = 0; $i -lt $lines.Count; $i++) {
		if ($null -ne $classification.Kinds[$i]) { continue }
		foreach ($gotchaName in $gotchaPatterns.Keys) {
			if ($lines[$i] -match $gotchaPatterns[$gotchaName].Pattern) {
				[void]$violations.Add([PSCustomObject]@{
					File    = $file.Substring($repoRoot.Length + 1)
					Line    = $i + 1
					Message = $gotchaPatterns[$gotchaName].Message
				})
			}
		}
	}
}

# Layer 2: PSScriptAnalyzer's PSUseCompatibleSyntax, best-effort. $Global:
# persists across a session, so a failed install costs one timeout, not one
# per build. Seeded first: StrictMode throws on unset.
if (-not (Test-Path Variable:Global:FwPowerShellCompatAnalyzerUnavailable)) {
	$Global:FwPowerShellCompatAnalyzerUnavailable = $false
}

if ((-not (Get-Module -ListAvailable -Name PSScriptAnalyzer)) -and -not $Global:FwPowerShellCompatAnalyzerUnavailable) {
	try {
		Write-Host 'powershell-compat: installing PSScriptAnalyzer (first run only)...'
		Install-Module -Name PSScriptAnalyzer -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop | Out-Null
	}
	catch {
		Write-Host "powershell-compat: PSScriptAnalyzer unavailable and could not be installed ($($_.Exception.Message)); skipping the PSUseCompatibleSyntax layer for the rest of this session." -ForegroundColor Yellow
		$Global:FwPowerShellCompatAnalyzerUnavailable = $true
	}
}

if (Get-Module -ListAvailable -Name PSScriptAnalyzer) {
	Import-Module PSScriptAnalyzer -Force
	$settings = @{
		IncludeRules = @('PSUseCompatibleSyntax')
		Rules        = @{
			PSUseCompatibleSyntax = @{
				Enable         = $true
				TargetVersions = @('5.1', '7.0')
			}
		}
	}
	foreach ($file in $targetFiles) {
		$results = Invoke-ScriptAnalyzer -Path $file -Settings $settings
		foreach ($r in $results) {
			[void]$violations.Add([PSCustomObject]@{
				File    = $file.Substring($repoRoot.Length + 1)
				Line    = $r.Line
				Message = $r.Message
			})
		}
	}
}

if ($violations.Count -eq 0) {
	Write-Host 'powershell-compat: clean (5.1 and 7.0).'
	exit 0
}

Write-Host "powershell-compat: $($violations.Count) syntax incompatibility(ies) found" -ForegroundColor Red
foreach ($v in $violations) {
	Write-Host ("  {0}:{1} {2}" -f $v.File, $v.Line, $v.Message)
}

if ($Full) { exit 0 }
exit 1
