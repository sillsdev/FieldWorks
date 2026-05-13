Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Equal {
	param(
		[Parameter(Mandatory = $true)]$Expected,
		[Parameter(Mandatory = $true)]$Actual,
		[Parameter(Mandatory = $true)][string]$Message
	)

	if ($Expected -ne $Actual) {
		throw "$Message Expected '$Expected', got '$Actual'."
	}
}

function Assert-True {
	param(
		[Parameter(Mandatory = $true)][bool]$Condition,
		[Parameter(Mandatory = $true)][string]$Message
	)

	if (-not $Condition) {
		throw $Message
	}
}

function Invoke-Git {
	param(
		[Parameter(Mandatory = $true)][string]$RepositoryPath,
		[Parameter(Mandatory = $true)][string[]]$Arguments
	)

	$output = @(& git -C $RepositoryPath @Arguments)
	if ($LASTEXITCODE -ne 0) {
		$joinedArgs = [string]::Join(" ", $Arguments)
		$joinedOutput = [string]::Join([Environment]::NewLine, $output)
		throw "git $joinedArgs failed in $RepositoryPath.$([Environment]::NewLine)$joinedOutput"
	}

	return $output
}

function Get-WorkspaceColorIndex {
	param([Parameter(Mandatory = $true)][string]$WorkspacePath)

	$raw = Get-Content -LiteralPath $WorkspacePath -Raw -Encoding UTF8
	$json = ConvertFrom-Json -InputObject $raw
	return [int]$json.settings."fieldworks.worktreeColorIndex"
}

function Get-SerenaLocalProjectName {
	param([Parameter(Mandatory = $true)][string]$ProjectLocalPath)

	$raw = Get-Content -LiteralPath $ProjectLocalPath -Raw -Encoding UTF8
	if ($raw -match '(?m)^\s*project_name:\s*(.+?)\s*$') {
		return $matches[1].Trim().Trim('"', "'")
	}

	throw "No project_name found in $ProjectLocalPath"
}

$repoScriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) "Setup-WorktreeColor.ps1"
$tempRoot = Join-Path $env:TEMP ("fw-worktree-color-test-" + [Guid]::NewGuid().ToString("N"))
$repoDir = Join-Path $tempRoot "FieldWorks"
$scriptsDir = Join-Path $repoDir "scripts"
$serenaDir = Join-Path $repoDir ".serena"
$worktreesDir = Join-Path $tempRoot "worktrees"

try {
	$null = New-Item -ItemType Directory -Path $scriptsDir -Force
	$null = New-Item -ItemType Directory -Path $serenaDir -Force
	$null = New-Item -ItemType Directory -Path $worktreesDir -Force
	Copy-Item -LiteralPath $repoScriptPath -Destination $scriptsDir

	Invoke-Git -RepositoryPath $repoDir -Arguments @("init")
	Invoke-Git -RepositoryPath $repoDir -Arguments @("config", "user.email", "test@example.com")
	Invoke-Git -RepositoryPath $repoDir -Arguments @("config", "user.name", "Test User")

	Set-Content -LiteralPath (Join-Path $repoDir "README.md") -Value "init" -Encoding UTF8
	Set-Content -LiteralPath (Join-Path $serenaDir "project.yml") -Value "project_name: FieldWorks`nlanguages:`n- csharp_omnisharp`n- cpp`n" -Encoding UTF8
	Set-Content -LiteralPath (Join-Path $serenaDir ".gitignore") -Value "/project.local.yml`n" -Encoding UTF8
	Invoke-Git -RepositoryPath $repoDir -Arguments @("add", "README.md")
	Invoke-Git -RepositoryPath $repoDir -Arguments @("add", ".serena/project.yml", ".serena/.gitignore")
	Invoke-Git -RepositoryPath $repoDir -Arguments @("commit", "-m", "init")

	$setupScript = Join-Path $scriptsDir "Setup-WorktreeColor.ps1"
	$alphaDir = Join-Path $worktreesDir "alpha"
	$betaDir = Join-Path $worktreesDir "beta"

	Invoke-Git -RepositoryPath $repoDir -Arguments @("worktree", "add", $alphaDir, "-b", "alpha", "HEAD")
	Invoke-Git -RepositoryPath $repoDir -Arguments @("worktree", "add", $betaDir, "-b", "beta", "HEAD")

	& $setupScript -Action Apply -WorktreePath $alphaDir
	& $setupScript -Action Apply -WorktreePath $betaDir

	$alphaWorkspace = Join-Path $alphaDir "alpha.code-workspace"
	$betaWorkspace = Join-Path $betaDir "beta.code-workspace"
	$alphaSerenaLocal = Join-Path $alphaDir ".serena\project.local.yml"
	$betaSerenaLocal = Join-Path $betaDir ".serena\project.local.yml"
	$alphaIndex = Get-WorkspaceColorIndex -WorkspacePath $alphaWorkspace
	$betaFirstIndex = Get-WorkspaceColorIndex -WorkspacePath $betaWorkspace

	Assert-Equal -Expected 0 -Actual $alphaIndex -Message "First worktree should use the first palette slot."
	Assert-Equal -Expected 1 -Actual $betaFirstIndex -Message "Second worktree should use the next palette slot."
	Assert-True -Condition (Test-Path $alphaSerenaLocal -PathType Leaf) -Message "Alpha worktree should get a Serena local override file."
	Assert-True -Condition (Test-Path $betaSerenaLocal -PathType Leaf) -Message "Beta worktree should get a Serena local override file."
	Assert-Equal -Expected "alpha" -Actual (Get-SerenaLocalProjectName -ProjectLocalPath $alphaSerenaLocal) -Message "Alpha worktree should set the Serena local project name from the worktree branch."
	Assert-Equal -Expected "beta" -Actual (Get-SerenaLocalProjectName -ProjectLocalPath $betaSerenaLocal) -Message "Beta worktree should set the Serena local project name from the worktree branch."

	Set-Content -LiteralPath $alphaSerenaLocal -Value "project_name: custom-alpha`n" -Encoding UTF8
	& $setupScript -Action Apply -WorktreePath $alphaDir
	Assert-Equal -Expected "custom-alpha" -Actual (Get-SerenaLocalProjectName -ProjectLocalPath $alphaSerenaLocal) -Message "Existing Serena local overrides should not be overwritten."

	Invoke-Git -RepositoryPath $repoDir -Arguments @("worktree", "remove", $betaDir, "--force")
	Invoke-Git -RepositoryPath $repoDir -Arguments @("worktree", "add", $betaDir, "beta")

	& $setupScript -Action Apply -WorktreePath $betaDir

	$betaSecondIndex = Get-WorkspaceColorIndex -WorkspacePath $betaWorkspace

	Assert-Equal -Expected 2 -Actual $betaSecondIndex -Message "Recreated worktree should advance to the next palette slot."
	Assert-True -Condition ($betaSecondIndex -ne $betaFirstIndex) -Message "Recreated worktree should not reuse its previous color slot."
	Assert-True -Condition (Test-Path $betaSerenaLocal -PathType Leaf) -Message "Recreated worktree should recreate the Serena local override file."
	Assert-Equal -Expected "beta" -Actual (Get-SerenaLocalProjectName -ProjectLocalPath $betaSerenaLocal) -Message "Recreated worktree should recreate the Serena local project name."

	Write-Output "PASS: worktree color rotation and Serena local override creation behave as expected."
}
finally {
	if (Test-Path $tempRoot) {
		Remove-Item -Path $tempRoot -Recurse -Force
	}
}