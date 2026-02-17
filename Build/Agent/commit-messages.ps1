# Mirrored from .github/workflows/CommitMessage.yml, ported to PowerShell
# Installs gitlint and lints commit messages since PR base (or origin default branch)

$ErrorActionPreference = 'Stop'

# Import shared git helpers
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $ScriptDir 'GitHelpers.ps1')

# Ensure Python/pip can install gitlint
python -m pip install --upgrade gitlint

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

# Run gitlint and tee to check_results.log like CI
# Note: PowerShell uses Tee-Object instead of POSIX tee
$cmd = @('gitlint', '--ignore', 'body-is-missing', '--commits', $range)
Write-Host "Running: $($cmd -join ' ')"
$proc = & gitlint --ignore body-is-missing --commits $range 2>&1 | Tee-Object -FilePath check_results.log
$exit = $LASTEXITCODE
$proc | Out-Host
exit $exit
