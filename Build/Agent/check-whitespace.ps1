# Runs the whitespace check used by CI and local validation.
# Exit codes:
#   0 = checker completed and found no whitespace issues
#   2 = checker completed and found whitespace issues
#   1 = checker could not complete successfully

$ErrorActionPreference = 'Stop'

$resultsLogPath = 'check-results.log'

# Import shared git helpers
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $ScriptDir 'GitHelpers.ps1')

function Get-RepoPath {
	$url = git remote get-url origin 2>$null
	if (-not $url) { return $null }
	if ($url -match 'github\.com[:/]([^/]+/[^/\.]+)(?:\.git)?$') { return $Matches[1] }
	return $null
}

# Ensure we have up-to-date refs
git fetch origin 2>$null | Out-Null

$baseRef = $env:GITHUB_BASE_REF
if (-not $baseRef -or $baseRef -eq '') {
	$baseRef = (Get-DefaultBranchName)
}
if (-not $baseRef) {
	Write-Error 'Unable to determine base branch (GITHUB_BASE_REF not set and origin HEAD unknown).'
	exit 1
}

$baseSha = (git rev-parse "origin/$baseRef" 2>$null)
if (-not $baseSha) {
	Write-Error "Unable to resolve base SHA for origin/$baseRef"
	exit 1
}

Write-Host "Base ref: $baseRef ($baseSha)"

# Run git log --check and tee output to a file for later inspection.
$log = git log --check --pretty=format:'---% h% s' "$baseSha.." 2>&1
$null = $log | Tee-Object -FilePath $resultsLogPath
$log | Out-Host

$problems = New-Object System.Collections.Generic.List[string]
$commit = ''
$commitText = ''
$commitTextmd = ''
$repoPath = Get-RepoPath
$headRef = (git rev-parse --abbrev-ref HEAD 2>$null)

$log | ForEach-Object {
	$line = $_
	switch -regex ($line) {
		'^---\s' {
			# Format: --- <sha> <subject>
			$parts = $line -split ' ', 3
			if ($parts.Length -ge 3) {
				$commit = $parts[1]
				$commitText = $parts[2]
				if ($repoPath) {
					$commitTextmd = "[$commit](https://github.com/$repoPath/commit/$commit) $commitText"
				}
				else {
					$commitTextmd = "$commit $commitText"
				}
			}
		}
		'^[^:]+:[1-9][0-9]*:' {
			# contains file and line number info -> indicates whitespace error
			$idx = $line.IndexOf(':')
			$file = $line.Substring(0, $idx)
			$afterFile = $line.Substring($idx + 1)
			$lineNumber = $afterFile.Substring(0, $afterFile.IndexOf(':'))
			if ($commitTextmd) { $problems.Add($commitTextmd) }
			if ($repoPath -and $headRef) {
				$problems.Add("[$line](https://github.com/$repoPath/blob/$headRef/$file#L$lineNumber)")
			}
			else {
				$problems.Add($line)
			}
			$problems.Add('')
		}
		default { }
	}
}

if ($problems.Count -gt 0) {
	Write-Host '[WARN] Please review the output for further information.'
	Write-Host '### A whitespace issue was found in one or more of the commits.'
	Write-Host 'This check validates commit history from origin/main..HEAD, not just the current working tree.'
	Write-Host 'If the report names an older commit, fix the file and then amend, squash, or rebase so that commit no longer appears in the branch history.'
	Write-Host ''
	Write-Host 'Errors:'
	$problems | ForEach-Object { Write-Host $_ }
	exit 2
}

Write-Host 'No problems found'
exit 0
