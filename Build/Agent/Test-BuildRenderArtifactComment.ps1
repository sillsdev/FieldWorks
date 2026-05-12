[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot 'Build-RenderArtifactComment.ps1'
$workspace = Join-Path ([System.IO.Path]::GetTempPath()) ('BuildRenderArtifactCommentTest-' + [guid]::NewGuid().ToString('N'))

function Assert-True {
	param(
		[Parameter(Mandatory = $true)]
		[bool]$Condition,
		[Parameter(Mandatory = $true)]
		[string]$Message
	)

	if (-not $Condition) {
		throw $Message
	}
}

try {
	$artifactsDirectory = Join-Path $workspace 'with-artifacts'
	$noArtifactsDirectory = Join-Path $workspace 'without-artifacts'
	New-Item -ItemType Directory -Path $artifactsDirectory -Force | Out-Null
	New-Item -ItemType Directory -Path $noArtifactsDirectory -Force | Out-Null

	$withArtifactsCommentPath = Join-Path $artifactsDirectory 'render-comment.md'
	$withArtifactsOutputPath = Join-Path $artifactsDirectory 'github-output.txt'
	$withArtifactsParameters = @{
		ArtifactUrl = 'https://example.test/artifact'
		CommentPath = $withArtifactsCommentPath
		FailureCount = '2'
		GitHubOutputPath = $withArtifactsOutputPath
		HasArtifacts = 'true'
		Repository = 'sillsdev/FieldWorks'
		RunAttempt = '1'
		RunId = '123456'
		ServerUrl = 'https://github.com'
		Sha = '0123456789abcdef0123456789abcdef01234567'
	}
	$withArtifactsResult = & $scriptPath @withArtifactsParameters

	Assert-True (Test-Path -LiteralPath $withArtifactsCommentPath) 'Expected helper to write the artifact comment markdown file.'
	Assert-True (Test-Path -LiteralPath $withArtifactsOutputPath) 'Expected helper to write the GitHub output file for the artifact case.'
	Assert-True ($withArtifactsResult.HasArtifacts -eq $true) 'Expected helper to report artifact presence.'

	$withArtifactsComment = Get-Content -LiteralPath $withArtifactsCommentPath -Raw
	Assert-True ($withArtifactsComment.Contains('detected 2 render snapshot failure(s).')) 'Expected artifact comment to include the failure count.'
	Assert-True ($withArtifactsComment.Contains('[Download render comparison artifacts](https://example.test/artifact)')) 'Expected artifact comment to include the artifact link.'

	$withArtifactsOutput = Get-Content -LiteralPath $withArtifactsOutputPath -Raw
	Assert-True ($withArtifactsOutput.Contains('comment_path=')) 'Expected artifact case to publish comment_path to GitHub output.'

	$withoutArtifactsCommentPath = Join-Path $noArtifactsDirectory 'render-comment.md'
	$withoutArtifactsOutputPath = Join-Path $noArtifactsDirectory 'github-output.txt'
	$withoutArtifactsParameters = @{
		CommentPath = $withoutArtifactsCommentPath
		GitHubOutputPath = $withoutArtifactsOutputPath
		HasArtifacts = 'false'
		Repository = 'sillsdev/FieldWorks'
		RunAttempt = '2'
		RunId = '123457'
		ServerUrl = 'https://github.com'
		Sha = 'fedcba9876543210fedcba9876543210fedcba98'
	}
	$withoutArtifactsResult = & $scriptPath @withoutArtifactsParameters

	Assert-True (Test-Path -LiteralPath $withoutArtifactsCommentPath) 'Expected helper to write the clean-run comment markdown file.'
	Assert-True (Test-Path -LiteralPath $withoutArtifactsOutputPath) 'Expected helper to write the GitHub output file for the clean-run case.'
	Assert-True ($withoutArtifactsResult.HasArtifacts -eq $false) 'Expected helper to report no artifacts for a clean run.'

	$withoutArtifactsComment = Get-Content -LiteralPath $withoutArtifactsCommentPath -Raw
	Assert-True ($withoutArtifactsComment.Contains('No render snapshot failures were captured in the latest successful CI run.')) 'Expected clean-run comment text.'
	Assert-True ($withoutArtifactsComment.Contains('Latest run: [fedcba987654 run 123457.2](https://github.com/sillsdev/FieldWorks/actions/runs/123457).')) 'Expected clean-run comment to include the latest run link.'
}
finally {
	if (Test-Path -LiteralPath $workspace) {
		Remove-Item -LiteralPath $workspace -Recurse -Force
	}
}

Write-Output '[OK] Build-RenderArtifactComment smoke test passed.'