[CmdletBinding()]
param(
	[string]$ArtifactUrl = $env:ARTIFACT_URL,
	[string]$CommentPath,
	[string]$FailureCount = $env:FAILURE_COUNT,
	[string]$EventPath = $env:GITHUB_EVENT_PATH,
	[string]$GitHubApiUrl = $env:GITHUB_API_URL,
	[string]$GitHubOutputPath = $env:GITHUB_OUTPUT,
	[string]$GitHubToken = $env:GITHUB_TOKEN,
	[string]$HeadRef = $env:GITHUB_HEAD_REF,
	[string]$HasArtifacts = $env:HAS_ARTIFACTS,
	[string]$PreviousCommentBody,
	[string]$PullRequestNumber = $env:PULL_REQUEST_NUMBER,
	[string]$Repository = $env:GITHUB_REPOSITORY,
	[string]$RunAttempt = $env:GITHUB_RUN_ATTEMPT,
	[string]$RunId = $env:GITHUB_RUN_ID,
	[string]$ServerUrl = $env:GITHUB_SERVER_URL,
	[string]$Sha = $env:GITHUB_SHA,
	[string]$WorkflowRef = $env:GITHUB_WORKFLOW_REF
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$StickyCommentMarker = '<!-- Sticky Pull Request Commentfieldworks-render-comparison-artifacts -->'
$FailureRunLabelMarker = '<!-- fieldworks-render-failure-run-label: '
$FailureRunUrlMarker = '<!-- fieldworks-render-failure-run-url: '

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrWhiteSpace($CommentPath)) {
	$CommentPath = Join-Path (Join-Path $repoRoot 'Output\RenderArtifacts') 'render-comment.md'
}

if ([string]::IsNullOrWhiteSpace($GitHubApiUrl)) {
	$GitHubApiUrl = 'https://api.github.com'
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

function Invoke-GitHubApi {
	param(
		[Parameter(Mandatory = $true)]
		[ValidateSet('Get')]
		[string]$Method,
		[Parameter(Mandatory = $true)]
		[string]$Uri
	)

	$headers = @{
		Accept = 'application/vnd.github+json'
		'X-GitHub-Api-Version' = '2022-11-28'
	}
	if (-not [string]::IsNullOrWhiteSpace($GitHubToken)) {
		$headers.Authorization = "Bearer $GitHubToken"
	}

	return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers
}

function Get-PullRequestNumber {
	if (-not [string]::IsNullOrWhiteSpace($PullRequestNumber)) {
		return [int]$PullRequestNumber
	}

	if ([string]::IsNullOrWhiteSpace($EventPath) -or -not (Test-Path -LiteralPath $EventPath)) {
		return $null
	}

	$eventPayload = Get-Content -LiteralPath $EventPath -Raw | ConvertFrom-Json
	if ($null -eq $eventPayload.pull_request -or $null -eq $eventPayload.pull_request.number) {
		return $null
	}

	return [int]$eventPayload.pull_request.number
}

function Get-PreviousCommentBody {
	if (-not [string]::IsNullOrWhiteSpace($PreviousCommentBody)) {
		return $PreviousCommentBody
	}

	$pullRequestNumber = Get-PullRequestNumber
	if ($null -eq $pullRequestNumber) {
		return $null
	}

	$repositoryName = Get-RequiredValue -Name 'Repository' -Value $Repository
	$repositoryParts = $repositoryName.Split('/')
	if ($repositoryParts.Count -ne 2) {
		throw "Invalid GitHub repository value: $repositoryName"
	}

	$owner = $repositoryParts[0]
	$repo = $repositoryParts[1]
	$commentsUri = "$GitHubApiUrl/repos/$owner/$repo/issues/$pullRequestNumber/comments?per_page=100"
	$comments = @(Invoke-GitHubApi -Method Get -Uri $commentsUri)
	$latestMatchingCommentBody = $null
	foreach ($comment in $comments) {
		if ($comment.body -and $comment.body.Contains($StickyCommentMarker)) {
			$latestMatchingCommentBody = [string]$comment.body
		}
	}

	return $latestMatchingCommentBody
}

function Get-MetadataValue {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Body,
		[Parameter(Mandatory = $true)]
		[string]$Prefix
	)

	$pattern = [regex]::Escape($Prefix) + '(?<value>.*?) -->'
	$match = [regex]::Match($Body, $pattern)
	if (-not $match.Success) {
		return $null
	}

	return $match.Groups['value'].Value.Trim()
}

function Get-PreviousFailureRunInfo {
	param([string]$Body)

	if ([string]::IsNullOrWhiteSpace($Body)) {
		return $null
	}

	$metadataLabel = Get-MetadataValue -Body $Body -Prefix $FailureRunLabelMarker
	$metadataUrl = Get-MetadataValue -Body $Body -Prefix $FailureRunUrlMarker
	if (-not [string]::IsNullOrWhiteSpace($metadataLabel) -and -not [string]::IsNullOrWhiteSpace($metadataUrl)) {
		return [pscustomobject]@{
			Label = $metadataLabel
			Url = $metadataUrl
		}
	}

	$patterns = @(
		'\[(?<label>[^\]]+ run \d+\.\d+)\]\((?<url>[^)]+/actions/runs/\d+)\) detected \d+ render snapshot failure\(s\)\.',
		'Render snapshot failures were reported in \[(?<label>[^\]]+)\]\((?<url>[^)]+)\), but the latest run \[[^\]]+\]\([^)]+\) is clean\.'
	)

	foreach ($pattern in $patterns) {
		$match = [regex]::Match($Body, $pattern)
		if ($match.Success) {
			return [pscustomobject]@{
				Label = $match.Groups['label'].Value
				Url = $match.Groups['url'].Value
			}
		}
	}

	return $null
}

function Get-WorkflowIdentifier {
	if ([string]::IsNullOrWhiteSpace($WorkflowRef)) {
		return 'CI.yml'
	}

	$atIndex = $WorkflowRef.IndexOf('@')
	$workflowRefPrefix = if ($atIndex -ge 0) {
		$WorkflowRef.Substring(0, $atIndex)
	}
	else {
		$WorkflowRef
	}

	$repositoryName = Get-RequiredValue -Name 'Repository' -Value $Repository
	$expectedPrefix = "$repositoryName/"
	if ($workflowRefPrefix.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
		return $workflowRefPrefix.Substring($expectedPrefix.Length)
	}

	return 'CI.yml'
}

function Get-PreviousFailureRunInfoFromWorkflowRuns {
	if ([string]::IsNullOrWhiteSpace($HeadRef)) {
		return $null
	}

	$repositoryName = Get-RequiredValue -Name 'Repository' -Value $Repository
	$repositoryParts = $repositoryName.Split('/')
	if ($repositoryParts.Count -ne 2) {
		throw "Invalid GitHub repository value: $repositoryName"
	}

	$owner = $repositoryParts[0]
	$repo = $repositoryParts[1]
	$workflowIdentifier = [System.Uri]::EscapeDataString((Get-WorkflowIdentifier))
	$encodedHeadRef = [System.Uri]::EscapeDataString($HeadRef)
	$runsUri = "$GitHubApiUrl/repos/$owner/$repo/actions/workflows/$workflowIdentifier/runs?branch=$encodedHeadRef&event=pull_request&per_page=20"
	$response = Invoke-GitHubApi -Method Get -Uri $runsUri
	$runs = @($response.workflow_runs)
	$currentRunId = if ([string]::IsNullOrWhiteSpace($RunId)) { $null } else { [string]$RunId }

	foreach ($run in $runs) {
		if ($null -eq $run) {
			continue
		}

		$runIdValue = [string]$run.id
		if (-not [string]::IsNullOrWhiteSpace($currentRunId) -and $runIdValue -eq $currentRunId) {
			continue
		}

		if (-not [System.String]::Equals([string]$run.status, 'completed', [System.StringComparison]::OrdinalIgnoreCase)) {
			continue
		}

		if (-not [System.String]::Equals([string]$run.conclusion, 'failure', [System.StringComparison]::OrdinalIgnoreCase)) {
			continue
		}

		$runSha = [string]$run.head_sha
		$runAttemptValue = [string]$run.run_attempt
		if ([string]::IsNullOrWhiteSpace($runSha) -or [string]::IsNullOrWhiteSpace($runIdValue) -or [string]::IsNullOrWhiteSpace($runAttemptValue)) {
			continue
		}

		$runShortSha = $runSha.Substring(0, [System.Math]::Min(12, $runSha.Length))
		return [pscustomobject]@{
			Label = "$runShortSha run $runIdValue.$runAttemptValue"
			Url = [string]$run.html_url
		}
	}

	return $null
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
		"$FailureRunLabelMarker$shortSha run $runLabel -->"
		"$FailureRunUrlMarker$runUrl -->"
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
	$previousFailureRun = Get-PreviousFailureRunInfo -Body (Get-PreviousCommentBody)
	if ($null -eq $previousFailureRun) {
		$previousFailureRun = Get-PreviousFailureRunInfoFromWorkflowRuns
	}
	$commentLines = @()
	if ($null -ne $previousFailureRun) {
		$commentLines += "@$FailureRunLabelMarker$($previousFailureRun.Label) -->".Substring(1)
		$commentLines += "@$FailureRunUrlMarker$($previousFailureRun.Url) -->".Substring(1)
		$commentLines += @(
			'### Render comparison artifacts'
			''
			"Render snapshot failures were reported in [$($previousFailureRun.Label)]($($previousFailureRun.Url)), but the latest run [$shortSha run $runLabel]($runUrl) is clean."
			''
			'This comment will be replaced if a future run produces render snapshot failures again.'
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
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($CommentPath, ($commentLines -join [System.Environment]::NewLine), $utf8NoBom)
Write-GitHubOutputValue -Name 'comment_path' -Value ([System.IO.Path]::GetFullPath($CommentPath))

Write-Output ([pscustomobject]@{
	CommentPath = [System.IO.Path]::GetFullPath($CommentPath)
	HasArtifacts = $renderArtifactsDetected
	FailureCount = $FailureCount
})