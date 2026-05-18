[CmdletBinding()]
param(
	[string]$ArtifactUrl = $env:ARTIFACT_URL,
	[string]$EventPath = $env:GITHUB_EVENT_PATH,
	[string]$FailureCount = $env:FAILURE_COUNT,
	[string]$GitHubApiUrl = $env:GITHUB_API_URL,
	[string]$GitHubToken = $env:GITHUB_TOKEN,
	[string]$HasArtifacts = $env:HAS_ARTIFACTS,
	[string]$Repository = $env:GITHUB_REPOSITORY,
	[string]$RunAttempt = $env:GITHUB_RUN_ATTEMPT,
	[string]$RunId = $env:GITHUB_RUN_ID,
	[string]$ServerUrl = $env:GITHUB_SERVER_URL,
	[string]$Sha = $env:GITHUB_SHA,
	[string]$StepSummaryPath = $env:GITHUB_STEP_SUMMARY,
	[switch]$SkipComment
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Marker = '<!-- fieldworks-render-comparison-artifacts -->'
if ([string]::IsNullOrWhiteSpace($GitHubApiUrl)) {
	$GitHubApiUrl = 'https://api.github.com'
}
if ([string]::IsNullOrWhiteSpace($ServerUrl)) {
	$ServerUrl = 'https://github.com'
}

function Add-Utf8NoBomLine {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path,
		[Parameter(Mandatory = $true)]
		[AllowEmptyString()]
		[string]$Value
	)

	$encoding = New-Object System.Text.UTF8Encoding($false)
	[System.IO.File]::AppendAllText($Path, "$Value$([System.Environment]::NewLine)", $encoding)
}

function Add-StepSummaryLine {
	param([string]$Value = '')

	if ([string]::IsNullOrWhiteSpace($StepSummaryPath)) {
		return
	}

	Add-Utf8NoBomLine -Path $StepSummaryPath -Value $Value
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

function Invoke-GitHubApi {
	param(
		[Parameter(Mandatory = $true)]
		[ValidateSet('Get', 'Post', 'Patch')]
		[string]$Method,
		[Parameter(Mandatory = $true)]
		[string]$Uri,
		[object]$Body
	)

	$headers = @{
		Accept = 'application/vnd.github+json'
		Authorization = "Bearer $GitHubToken"
		'X-GitHub-Api-Version' = '2022-11-28'
	}

	if ($PSBoundParameters.ContainsKey('Body')) {
		$jsonBody = $Body | ConvertTo-Json -Depth 8
		return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers -Body $jsonBody -ContentType 'application/json'
	}

	return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers
}

function Get-PullRequestNumber {
	$eventPayload = Get-Content -LiteralPath (Get-RequiredValue -Name 'EventPath' -Value $EventPath) -Raw | ConvertFrom-Json
	if ($null -eq $eventPayload.pull_request -or $null -eq $eventPayload.pull_request.number) {
		throw 'GitHub event payload does not contain a pull request number.'
	}

	return [int]$eventPayload.pull_request.number
}

function Get-RenderComment {
	param(
		[Parameter(Mandatory = $true)]
		[string]$CommentsUri,
		[Parameter(Mandatory = $true)]
		[string]$Owner,
		[Parameter(Mandatory = $true)]
		[string]$Repo,
		[Parameter(Mandatory = $true)]
		[int]$PullRequestNumber
	)

	$comments = @(Invoke-GitHubApi -Method Get -Uri $CommentsUri)
	foreach ($comment in $comments) {
		if ($comment.body -and $comment.body.Contains($Marker)) {
			return $comment
		}
	}

	$query = @'
query($owner: String!, $repo: String!, $number: Int!) {
  repository(owner: $owner, name: $repo) {
    pullRequest(number: $number) {
      comments(first: 100) {
        nodes {
          databaseId
          body
        }
      }
    }
  }
}
'@
	$result = Invoke-GitHubApi -Method Post -Uri "$GitHubApiUrl/graphql" -Body @{
		query = $query
		variables = @{
			owner = $Owner
			repo = $Repo
			number = $PullRequestNumber
		}
	}

	$pullRequestComments = @($result.data.repository.pullRequest.comments.nodes)
	foreach ($comment in $pullRequestComments) {
		if ($comment.body -and $comment.body.Contains($Marker)) {
			return [pscustomobject]@{
				id = $comment.databaseId
				body = $comment.body
			}
		}
	}

	return $null
}

$hasRenderArtifacts = [System.String]::Equals($HasArtifacts, 'true', [System.StringComparison]::OrdinalIgnoreCase)
$repositoryName = Get-RequiredValue -Name 'Repository' -Value $Repository
$repositoryParts = $repositoryName.Split('/')
if ($repositoryParts.Count -ne 2) {
	throw "Invalid GitHub repository value: $repositoryName"
}

$owner = $repositoryParts[0]
$repo = $repositoryParts[1]
$runLabel = "$(Get-RequiredValue -Name 'RunId' -Value $RunId).$(Get-RequiredValue -Name 'RunAttempt' -Value $RunAttempt)"
$runUrl = "$ServerUrl/$owner/$repo/actions/runs/$RunId"
$shortSha = (Get-RequiredValue -Name 'Sha' -Value $Sha).Substring(0, [System.Math]::Min(12, $Sha.Length))

if ($hasRenderArtifacts) {
	$artifactLink = Get-RequiredValue -Name 'ArtifactUrl' -Value $ArtifactUrl
	$failureTotal = Get-RequiredValue -Name 'FailureCount' -Value $FailureCount
	Add-StepSummaryLine '### Render comparison artifacts'
	Add-StepSummaryLine
	Add-StepSummaryLine "$failureTotal render snapshot failure(s) produced expected, actual, and diff images."
	Add-StepSummaryLine
	Add-StepSummaryLine "[Download render comparison artifacts]($artifactLink)"
}

if ($SkipComment) {
	return
}

Get-RequiredValue -Name 'GitHubToken' -Value $GitHubToken | Out-Null
$pullRequestNumber = Get-PullRequestNumber
$commentsUri = "$GitHubApiUrl/repos/$owner/$repo/issues/$pullRequestNumber/comments?per_page=100"
$previous = Get-RenderComment -CommentsUri $commentsUri -Owner $owner -Repo $repo -PullRequestNumber $pullRequestNumber

if ($hasRenderArtifacts) {
	$entry = "- [$shortSha run $runLabel]($runUrl) - $FailureCount render snapshot failure(s) - [download artifact]($ArtifactUrl)"
	$previousEntries = @()
	if ($null -ne $previous) {
		$previousEntries = @($previous.body -split '\r?\n' | Where-Object { $_.StartsWith('- [', [System.StringComparison]::Ordinal) })
	}

	$entries = @($entry) + @($previousEntries | Where-Object { -not $_.Contains("run $runLabel") })
	$bodyLines = @(
		$Marker,
		'### Render comparison artifacts',
		'',
		'Render snapshot failure artifacts from recent CI runs. Each artifact contains `index.html`, expected images, actual images, diff images, and metadata.',
		''
	) + @($entries | Select-Object -First 25) + @('')
}
else {
	if ($null -eq $previous) {
		Write-Output 'No existing render artifact comment found to mark resolved.'
		return
	}

	$bodyLines = @(
		$Marker,
		'### Render comparison artifacts',
		'',
		'No render snapshot failures were captured in the latest successful CI run.',
		'',
		"Latest run: [$shortSha run $runLabel]($runUrl).",
		''
	)
}

$body = $bodyLines -join "`n"
if ($null -ne $previous) {
	Invoke-GitHubApi -Method Patch -Uri "$GitHubApiUrl/repos/$owner/$repo/issues/comments/$($previous.id)" -Body @{ body = $body } | Out-Null
	Write-Output "Updated render artifact comment $($previous.id)."
}
else {
	Invoke-GitHubApi -Method Post -Uri "$GitHubApiUrl/repos/$owner/$repo/issues/$pullRequestNumber/comments" -Body @{ body = $body } | Out-Null
	Write-Output "Created render artifact comment on PR $pullRequestNumber."
}