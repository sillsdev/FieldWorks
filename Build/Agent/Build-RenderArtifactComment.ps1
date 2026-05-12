[CmdletBinding()]
param(
	[string]$ArtifactUrl = $env:ARTIFACT_URL,
	[string]$CommentPath,
	[string]$FailureCount = $env:FAILURE_COUNT,
	[string]$GitHubOutputPath = $env:GITHUB_OUTPUT,
	[string]$HasArtifacts = $env:HAS_ARTIFACTS,
	[string]$Repository = $env:GITHUB_REPOSITORY,
	[string]$RunAttempt = $env:GITHUB_RUN_ATTEMPT,
	[string]$RunId = $env:GITHUB_RUN_ID,
	[string]$ServerUrl = $env:GITHUB_SERVER_URL,
	[string]$Sha = $env:GITHUB_SHA
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrWhiteSpace($CommentPath)) {
	$CommentPath = Join-Path (Join-Path $repoRoot 'Output\RenderArtifacts') 'render-comment.md'
}

function Get-RequiredValue {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Name,
		[AllowEmptyString()]
		[string]$Value
	)

	if ([string]::IsNullOrWhiteSpace($Value)) {
		throw "Required value missing: $Name"
	}

	return $Value
}

function Write-GitHubOutputValue {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Name,
		[Parameter(Mandatory = $true)]
		[string]$Value
	)

	if ([string]::IsNullOrWhiteSpace($GitHubOutputPath)) {
		return
	}

	$encoding = New-Object System.Text.UTF8Encoding($false)
	[System.IO.File]::AppendAllText($GitHubOutputPath, "$Name=$Value$([System.Environment]::NewLine)", $encoding)
}

$commentDirectory = Split-Path -Path $CommentPath -Parent
if (-not (Test-Path -LiteralPath $commentDirectory)) {
	New-Item -ItemType Directory -Path $commentDirectory -Force | Out-Null
}

$runLabel = "$(Get-RequiredValue -Name 'RunId' -Value $RunId).$(Get-RequiredValue -Name 'RunAttempt' -Value $RunAttempt)"
$runUrl = "$(Get-RequiredValue -Name 'ServerUrl' -Value $ServerUrl)/$(Get-RequiredValue -Name 'Repository' -Value $Repository)/actions/runs/$RunId"
$shortSha = (Get-RequiredValue -Name 'Sha' -Value $Sha).Substring(0, [System.Math]::Min(12, $Sha.Length))
$renderArtifactsDetected = [System.String]::Equals([string]$HasArtifacts, 'true', [System.StringComparison]::OrdinalIgnoreCase)

if ($renderArtifactsDetected) {
	$downloadLine = if ([string]::IsNullOrWhiteSpace($ArtifactUrl)) {
		'Artifact upload URL was not available for this run.'
	}
	else {
		"- [Download render comparison artifacts]($ArtifactUrl)"
	}

	$commentLines = @(
		'### Render comparison artifacts'
		''
		"[$shortSha run $runLabel]($runUrl) detected $(Get-RequiredValue -Name 'FailureCount' -Value $FailureCount) render snapshot failure(s)."
		''
		'- The artifact includes `index.html`, expected images, actual images, diff images, and metadata.'
		$downloadLine
		''
		'This comment is updated in place by CI for the latest run.'
	)
}
else {
	$commentLines = @(
		'### Render comparison artifacts'
		''
		'No render snapshot failures were captured in the latest successful CI run.'
		''
		"Latest run: [$shortSha run $runLabel]($runUrl)."
	)
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($CommentPath, ($commentLines -join [System.Environment]::NewLine), $utf8NoBom)
Write-GitHubOutputValue -Name 'comment_path' -Value ([System.IO.Path]::GetFullPath($CommentPath))

Write-Output ([pscustomobject]@{
	CommentPath = [System.IO.Path]::GetFullPath($CommentPath)
	HasArtifacts = $renderArtifactsDetected
	FailureCount = $FailureCount
})