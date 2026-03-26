#!/usr/bin/env pwsh
# Formats commit-message lint results for GitHub Actions summary/output usage.

param(
	[Parameter(Mandatory = $true)]
	[string]$LintOutcome,

	[string]$ResultsLogPath = 'check_results.log',

	[string]$RepositoryOwner = '',

	[string]$RepositoryName = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-CommitMessageSummaryContent {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Outcome,

		[Parameter(Mandatory = $true)]
		[string]$LogPath,

		[string]$Owner,

		[string]$RepoName
	)

	$content = $null
	$lintSucceeded = $Outcome -eq 'success'

	if (Test-Path -LiteralPath $LogPath) {
		$content = Get-Content -LiteralPath $LogPath -Raw
	}

	if ([string]::IsNullOrWhiteSpace($content)) {
		if ($lintSucceeded) {
			return 'No problems found.'
		}

		return 'Commit message checker failed before producing details.'
	}

	if (-not [string]::IsNullOrWhiteSpace($Owner) -and -not [string]::IsNullOrWhiteSpace($RepoName)) {
		$commitUrl = "https://github.com/$Owner/$RepoName/commit/"
		$content = [regex]::Replace($content, 'Commit ([0-9a-f]{7,40})', {
			param($match)
			$sha = $match.Groups[1].Value
			return "[commit $sha]($commitUrl$sha)"
		})
	}

	Set-Content -LiteralPath $LogPath -Value $content -NoNewline
	return $content
}

$summaryContent = Get-CommitMessageSummaryContent -Outcome $LintOutcome -LogPath $ResultsLogPath -Owner $RepositoryOwner -RepoName $RepositoryName

if ($env:GITHUB_STEP_SUMMARY) {
	Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summaryContent
}

if ($env:GITHUB_OUTPUT) {
	Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value 'check_results<<###LINT_DELIMITER###'
	Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value $summaryContent
	Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value '###LINT_DELIMITER###'
}