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

$repoScriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) "Setup-WorktreeColor.ps1"
$tempRoot = Join-Path $env:TEMP ("fw-worktree-color-test-" + [Guid]::NewGuid().ToString("N"))
$repoDir = Join-Path $tempRoot "FieldWorks"
$scriptsDir = Join-Path $repoDir "scripts"
$worktreesDir = Join-Path $tempRoot "worktrees"

try {
	$null = New-Item -ItemType Directory -Path $scriptsDir -Force
	$null = New-Item -ItemType Directory -Path $worktreesDir -Force
	Copy-Item -LiteralPath $repoScriptPath -Destination $scriptsDir

	Invoke-Git -RepositoryPath $repoDir -Arguments @("init")
	Invoke-Git -RepositoryPath $repoDir -Arguments @("config", "user.email", "test@example.com")
	Invoke-Git -RepositoryPath $repoDir -Arguments @("config", "user.name", "Test User")

	Set-Content -LiteralPath (Join-Path $repoDir "README.md") -Value "init" -Encoding UTF8
	Invoke-Git -RepositoryPath $repoDir -Arguments @("add", "README.md")
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
	$alphaIndex = Get-WorkspaceColorIndex -WorkspacePath $alphaWorkspace
	$betaFirstIndex = Get-WorkspaceColorIndex -WorkspacePath $betaWorkspace

	Assert-Equal -Expected 0 -Actual $alphaIndex -Message "First worktree should use the first palette slot."
	Assert-Equal -Expected 1 -Actual $betaFirstIndex -Message "Second worktree should use the next palette slot."

	Invoke-Git -RepositoryPath $repoDir -Arguments @("worktree", "remove", $betaDir, "--force")
	Invoke-Git -RepositoryPath $repoDir -Arguments @("worktree", "add", $betaDir, "beta")

	& $setupScript -Action Apply -WorktreePath $betaDir

	$betaSecondIndex = Get-WorkspaceColorIndex -WorkspacePath $betaWorkspace

	Assert-Equal -Expected 2 -Actual $betaSecondIndex -Message "Recreated worktree should advance to the next palette slot."
	Assert-True -Condition ($betaSecondIndex -ne $betaFirstIndex) -Message "Recreated worktree should not reuse its previous color slot."

	Write-Output "PASS: worktree color rotation advances after delete and recreate."
}
finally {
	if (Test-Path $tempRoot) {
		Remove-Item -Path $tempRoot -Recurse -Force
	}
}