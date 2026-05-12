[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot 'Collect-RenderArtifacts.ps1'
$workspace = Join-Path ([System.IO.Path]::GetTempPath()) ('CollectRenderArtifactsTest-' + [guid]::NewGuid().ToString('N'))
$searchRoot = Join-Path $workspace 'repo'
$outputDirectory = Join-Path $workspace 'render-artifacts'
$githubOutputPath = Join-Path $workspace 'github-output.txt'

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
	New-Item -ItemType Directory -Path $searchRoot -Force | Out-Null

	$baselineDirectory = Join-Path $searchRoot 'Src/Common/RootSite/RootSiteTests/Baselines'
	New-Item -ItemType Directory -Path $baselineDirectory -Force | Out-Null

	foreach ($name in @('alpha', 'beta')) {
		Set-Content -Path (Join-Path $baselineDirectory "$name.verified.png") -Value "expected-$name"
		Set-Content -Path (Join-Path $baselineDirectory "$name.received.png") -Value "actual-$name"
		Set-Content -Path (Join-Path $baselineDirectory "$name.diff.png") -Value "diff-$name"
		Set-Content -Path (Join-Path $baselineDirectory "$name.diff.json") -Value "{`"SnapshotName`":`"$name`"}"
	}

	$result = & $scriptPath -SearchRoot $searchRoot -OutputDirectory $outputDirectory -GitHubOutputPath $githubOutputPath

	Assert-True ($result.HasArtifacts -eq $true) 'Expected render artifacts to be detected.'
	Assert-True ($result.FailureCount -eq 2) 'Expected two render failures to be reported.'
	Assert-True (Test-Path (Join-Path $outputDirectory 'index.html')) 'Expected an HTML report.'
	Assert-True (Test-Path (Join-Path $outputDirectory 'README.md')) 'Expected a Markdown report.'
	Assert-True (Test-Path (Join-Path $outputDirectory 'manifest.json')) 'Expected a manifest file.'
	Assert-True (Test-Path (Join-Path $outputDirectory 'Src/Common/RootSite/RootSiteTests/Baselines/alpha.expected.png')) 'Expected alpha baseline image to be copied.'
	Assert-True (Test-Path (Join-Path $outputDirectory 'Src/Common/RootSite/RootSiteTests/Baselines/alpha.actual.png')) 'Expected alpha received image to be copied.'
	Assert-True (Test-Path (Join-Path $outputDirectory 'Src/Common/RootSite/RootSiteTests/Baselines/alpha.diff.png')) 'Expected alpha diff image to be copied.'

	$githubOutputBytes = [System.IO.File]::ReadAllBytes($githubOutputPath)
	$hasUtf8Bom = $githubOutputBytes.Length -ge 3 -and $githubOutputBytes[0] -eq 0xEF -and $githubOutputBytes[1] -eq 0xBB -and $githubOutputBytes[2] -eq 0xBF
	Assert-True (-not $hasUtf8Bom) 'Expected GitHub output file to be written without a UTF-8 BOM.'

	$githubOutput = [System.IO.File]::ReadAllText($githubOutputPath)
	Assert-True ($githubOutput.Contains('has_artifacts=true')) 'Expected has_artifacts GitHub output.'
	Assert-True ($githubOutput.Contains('failure_count=2')) 'Expected failure_count GitHub output.'

	$readme = Get-Content -LiteralPath (Join-Path $outputDirectory 'README.md') -Raw
	Assert-True ($readme.Contains('[expected](')) 'Expected Markdown report to use expected link labels.'
	Assert-True ($readme.Contains('[actual](')) 'Expected Markdown report to use actual link labels.'
	Assert-True ($readme.Contains('[diff](')) 'Expected Markdown report to use diff link labels.'

	$html = Get-Content -LiteralPath (Join-Path $outputDirectory 'index.html') -Raw
	Assert-True ($html.Contains('alt="Expected render for Src/Common/RootSite/RootSiteTests/Baselines/alpha.received.png"')) 'Expected descriptive alt text for expected render image.'
}
finally {
	if (Test-Path -LiteralPath $workspace) {
		Remove-Item -LiteralPath $workspace -Recurse -Force
	}
}

Write-Output '[OK] Collect-RenderArtifacts smoke test passed.'