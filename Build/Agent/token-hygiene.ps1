<#
.SYNOPSIS
	Full-conformance token-hygiene check for the FieldWorks Avalonia surface.

.DESCRIPTION
	Enforces that every color and spacing/dimension value under every root
	Get-TokenHygieneScopeRoots lists (Src/Common/FwAvalonia/**,
	Src/Common/FwAvaloniaDialogs/**, Src/Common/FwAvaloniaTheme/**,
	Src/Common/FwAvaloniaPreviewHost/**, Src/LexText/LexTextControls/Avalonia/**,
	and Src/xWorks/Avalonia/**) routes through the shared FwAvaloniaTheme token system
	(Src/Common/FwAvaloniaTheme/Tokens/) instead of a hand-picked literal.

	Deliberately different from comment-hygiene.ps1: there is NO diff/
	added-lines mode and NO grandfathering. Every invocation scans the
	ENTIRE scoped tree (what comment-hygiene calls -Full) and fails non-zero
	on any violation by default. The scoped tree was fully cleaned up before
	this check was wired into build.ps1/test.ps1, so a clean run is the
	expected baseline, not an aspiration -- a check that starts red on day one
	because of pre-existing literals would just get bypassed.

.PARAMETER List
	Accepted for parity with comment-hygiene.ps1's -List flag. Every
	violation is always printed on a failing or advisory run, so this switch
	has no additional effect today.

.PARAMETER Advisory
	Report violations and exit 0 instead of failing. Under GitHub Actions
	each violation is also emitted as a warning annotation, so it lands on
	the pull request's diff without breaking the build. build.ps1 and
	test.ps1 pass this whenever -TokenHygiene was not requested.

.EXAMPLE
	Build/Agent/token-hygiene.ps1 -List
	Report every hardcoded-color/hardcoded-spacing violation in the whole
	scoped tree and fail if any exist.

.EXAMPLE
	Build/Agent/token-hygiene.ps1 -Advisory
	Report violations without failing (used by build.ps1/test.ps1 in CI when
	-TokenHygiene was not passed).
#>
[CmdletBinding()]
param(
	[switch] $List,
	[switch] $Advisory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Import-Module (Join-Path $PSScriptRoot 'TokenHygiene.psm1') -Force

function Write-Violation {
	param($Violation)
	$relative = $Violation.File.Substring($repoRoot.Length + 1)
	Write-Host ("  {0}:{1} [{2}] {3}" -f $relative, $Violation.Line, $Violation.Category, $Violation.Text)

	# An annotation puts the violation on the pull request's diff, where a
	# reviewer sees it without the job failing. Forward slashes: GitHub matches
	# annotation paths against the repository's own separator.
	if ($Advisory -and $env:GITHUB_ACTIONS -eq 'true' -and -not $script:suppressDuplicateAnnotations) {
		Write-Host ("::warning file={0},line={1},title=token-hygiene ({2})::{3}" -f `
			($relative -replace '\\', '/'), $Violation.Line, $Violation.Category, $Violation.Text)
	}
}

# Both CI steps scan the same tree; the first marks the job so later runs skip
# duplicate annotations. Never the scan: that would tie enforcement to step order.
$suppressDuplicateAnnotations = $false
if ($env:GITHUB_ACTIONS -eq 'true') {
	if ($env:FW_TOKEN_HYGIENE_REPORTED -eq '1') {
		Write-Host 'token-hygiene: annotations already emitted earlier in this job.'
		$suppressDuplicateAnnotations = $true
	}
	if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_ENV)) {
		# UTF8Encoding($false): appending a BOM mid-file would corrupt the
		# environment file the runner parses when the step ends.
		[System.IO.File]::AppendAllText($env:GITHUB_ENV, "FW_TOKEN_HYGIENE_REPORTED=1`n",
			(New-Object System.Text.UTF8Encoding($false)))
	}
}

$files = Get-TokenHygieneScopedFiles -RepoRoot $repoRoot
Write-Host "token-hygiene: scanning $($files.Count) file(s) under the Avalonia surface"

$violations = Get-TokenHygieneViolations -Files $files

if ($violations.Count -eq 0) {
	Write-Host 'token-hygiene: clean.'
	exit 0
}

Write-Host ''

if ($Advisory) {
	Write-Host "token-hygiene: $($violations.Count) advisory violation(s)" -ForegroundColor Yellow
	foreach ($v in $violations) { Write-Violation $v }
	Write-Host ''
	Write-Host 'Advisory only. Pass -TokenHygiene to build.ps1 or test.ps1 to enforce these.' -ForegroundColor Yellow
	exit 0
}

Write-Host "token-hygiene: $($violations.Count) violation(s)" -ForegroundColor Red
foreach ($v in $violations) { Write-Violation $v }
Write-Host ''
Write-Host 'Route every color/spacing value through the FwAvaloniaTheme token system (Src/Common/FwAvaloniaTheme/Tokens/) -- see FwAvaloniaDensity.cs and FwThemeResources.cs for the point-of-use pattern.' -ForegroundColor Red
exit 1
