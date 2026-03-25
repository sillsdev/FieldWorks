# Runs the commit-message lint used by CI and local validation.
# Exits with gitlint's exit code.

$ErrorActionPreference = 'Stop'

$resultsLogPath = 'check_results.log'

# Import shared git helpers
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $ScriptDir 'GitHelpers.ps1')

function Install-GitLint {
	$pythonCommand = $null
	foreach ($candidate in @('python', 'python3')) {
		if (Get-Command $candidate -ErrorAction SilentlyContinue) {
			$pythonCommand = $candidate
			break
		}
	}

	if (-not $pythonCommand) {
		throw 'Unable to locate python or python3 to install gitlint.'
	}

	& $pythonCommand -m pip install --upgrade gitlint
}

if (-not (Get-Command gitlint -ErrorAction SilentlyContinue)) {
	Install-GitLint
}

# Ensure we have up-to-date refs
git fetch origin 2>$null | Out-Null

$baseRef = $env:GITHUB_BASE_REF
if (-not $baseRef -or $baseRef -eq '') {
	$baseRef = (Get-DefaultBranchName)
}

$range = $null
if ($baseRef) { $range = "origin/$baseRef.." }
else {
	Write-Host 'No base ref found; linting last 20 commits as a fallback.'
	$range = 'HEAD~20..HEAD'
}

# Run gitlint and tee output to a file for CI summaries and local inspection.
$cmd = @('gitlint', '--ignore', 'body-is-missing', '--commits', $range)
Write-Host "Running: $($cmd -join ' ')"
$proc = & gitlint --ignore body-is-missing --commits $range 2>&1 | Tee-Object -FilePath $resultsLogPath
$exit = $LASTEXITCODE
$proc | Out-Host
exit $exit
