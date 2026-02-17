<#
.SYNOPSIS
    Start MCP Agent Mail server in a separate Git Bash window.

.DESCRIPTION
    Checks for an existing mcp_agent_mail server process and starts it if not already running.
    Uses the companion repository script: ../mcp_agent_mail/scripts/run_server_with_token.sh

.PARAMETER RepoRoot
    Optional repo root override. Defaults to the FieldWorks repo root.

.PARAMETER ServerScriptPath
    Optional path to run_server_with_token.sh. Defaults to ../mcp_agent_mail/scripts/run_server_with_token.sh
#>

[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$ServerScriptPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Status {
    param([string]$Message, [string]$Status = "INFO")
    $prefix = switch ($Status) {
        "OK"    { "[OK]   " }
        "WARN"  { "[WARN] " }
        "ERROR" { "[FAIL] " }
        default { "       " }
    }
    Write-Output "$prefix$Message"
}

function Find-GitBash {
    $paths = @(
        "C:\\Program Files\\Git\\bin\\bash.exe",
        "C:\\Program Files\\Git\\usr\\bin\\bash.exe"
    )
    foreach ($path in $paths) {
        if (Test-Path $path) { return $path }
    }
    return $null
}

function Convert-ToGitBashPath {
    param([string]$Path)
    $normalized = [string]$Path
    if ($normalized.Length -ge 2 -and $normalized[1] -eq ':') {
        $drive = $normalized.Substring(0, 1).ToLowerInvariant()
        $rest = $normalized.Substring(2) -replace "\\", "/"
        return "/$drive$rest"
    }
    return ($normalized -replace "\\", "/")
}

function Get-RepoRootInfo {
    param([string]$Path)
    $gitPath = Join-Path $Path ".git"
    $gitItem = Get-Item $gitPath -ErrorAction SilentlyContinue
    if (-not $gitItem) { return @{ IsWorktree = $false; RepoRoot = $Path } }

    if ($gitItem.PSIsContainer) {
        return @{ IsWorktree = $false; RepoRoot = $Path }
    }

    $gitDirLine = Get-Content $gitPath -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($gitDirLine -and $gitDirLine -match "^gitdir:\s*(.+)$") {
        $gitDir = $matches[1].Trim()
        if (-not [System.IO.Path]::IsPathRooted($gitDir)) {
            $gitDir = Join-Path $Path $gitDir
        }
        $gitDir = (Resolve-Path $gitDir).Path
        if ($gitDir -match "\\worktrees\\") {
            $commonGitDir = (Resolve-Path (Join-Path $gitDir "..\\..") ).Path
            $repoRoot = Split-Path -Parent $commonGitDir
            return @{ IsWorktree = $true; RepoRoot = $repoRoot }
        }
    }

    return @{ IsWorktree = $false; RepoRoot = $Path }
}

function Find-Or-CloneAgentMailRepo {
    param([string]$FieldWorksRoot)
    $repoInfo = Get-RepoRootInfo -Path $FieldWorksRoot
    $rootParent = Split-Path -Parent $FieldWorksRoot
    $candidates = @(
        (Join-Path $rootParent "mcp_agent_mail"),
        (Join-Path $rootParent "mcp-agent-mail")
    )

    if ($repoInfo.IsWorktree) {
        $parentParent = Split-Path -Parent $rootParent
        $candidates += (Join-Path $parentParent "mcp_agent_mail")
        $candidates += (Join-Path $parentParent "mcp-agent-mail")
    }

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return (Resolve-Path $candidate).Path }
    }

    $cloneBase = $rootParent
    if ($repoInfo.IsWorktree) {
        $cloneBase = Split-Path -Parent $rootParent
    }

    $cloneTarget = Join-Path $cloneBase "mcp_agent_mail"
    Write-Status "mcp_agent_mail repo not found. Cloning to $cloneTarget" "WARN"

    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) {
        Write-Status "git not found. Install Git for Windows to clone the repo." "ERROR"
        exit 1
    }

    & $git.Source clone "https://github.com/Dicklesworthstone/mcp_agent_mail.git" $cloneTarget
    if (-not (Test-Path $cloneTarget)) {
        Write-Status "Clone failed or repo not found at $cloneTarget" "ERROR"
        exit 1
    }

    return (Resolve-Path $cloneTarget).Path
}

function Ensure-AgentMailEnv {
    param([string]$RepoPath)

    $envPath = Join-Path $RepoPath ".env"
    if (Test-Path $envPath) { return $envPath }

    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
    }
    finally {
        if ($rng) { $rng.Dispose() }
    }
    $token = ($bytes | ForEach-Object { $_.ToString("x2") }) -join ""

    @(
        "HTTP_BEARER_TOKEN=$token",
        "HTTP_ALLOW_LOCALHOST_UNAUTHENTICATED=true"
    ) | Set-Content -Path $envPath -Encoding UTF8

    Write-Status "Created .env with HTTP_BEARER_TOKEN in $RepoPath" "OK"
    return $envPath
}

function Ensure-AgentMailDependencies {
    param([string]$RepoPath)

    if (Test-Path (Join-Path $RepoPath ".venv")) { return }

    $uv = Get-Command uv -ErrorAction SilentlyContinue
    if (-not $uv) {
        Write-Status "uv not found; cannot auto-create venv. Install uv or run setup in the repo." "WARN"
        return
    }

    Write-Status "Creating venv and syncing dependencies with uv..."
    Push-Location $RepoPath
    try {
        & $uv.Source sync
    }
    finally {
        Pop-Location
    }
}

function Test-AgentMailHttp {
    param([string]$Url)

    try {
        $null = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 3 -UseBasicParsing -ErrorAction Stop
        return $true
    }
    catch {
        $response = $_.Exception.Response
        if ($response -and $response.StatusCode) {
            $statusCode = [int]$response.StatusCode
            if ($statusCode -ge 200 -and $statusCode -lt 500) {
                return $true
            }
        }
        return $false
    }
}

if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\\..") ).Path
}

if (-not $ServerScriptPath) {
    $agentMailRepo = Find-Or-CloneAgentMailRepo -FieldWorksRoot $RepoRoot
    $ServerScriptPath = Join-Path $agentMailRepo "scripts\\run_server_with_token.sh"
}

if (-not $ServerScriptPath -or -not (Test-Path $ServerScriptPath)) {
    Write-Status "Server script not found. Expected: $RepoRoot\\..\\mcp_agent_mail\\scripts\\run_server_with_token.sh" "ERROR"
    exit 1
}

$existing = Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -and $_.CommandLine -match "mcp_agent_mail"
}

if ($existing) {
    $pids = ($existing | Select-Object -ExpandProperty ProcessId) -join ", "
    $httpOk = Test-AgentMailHttp -Url "http://127.0.0.1:8765/mail"
    if ($httpOk) {
        Write-Status "mcp_agent_mail already running. PID(s): $pids" "OK"
        exit 0
    }

    Write-Status "mcp_agent_mail process detected (PID(s): $pids) but HTTP check failed. Attempting start." "WARN"
}

$bash = Find-GitBash
if (-not $bash) {
    Write-Status "Git Bash not found. Install Git for Windows or add bash.exe to PATH." "ERROR"
    exit 1
}


$serverRepo = Split-Path -Parent (Split-Path -Parent $ServerScriptPath)
$null = Ensure-AgentMailEnv -RepoPath $serverRepo
Ensure-AgentMailDependencies -RepoPath $serverRepo
$launchCommand = "./scripts/run_server_with_token.sh"

Write-Status "Starting mcp_agent_mail in Git Bash..."
Start-Process -FilePath $bash -WorkingDirectory $serverRepo -ArgumentList "-lc", "$launchCommand; exec bash"
Start-Sleep -Seconds 2

$started = Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -and $_.CommandLine -match "mcp_agent_mail"
}

if (-not $started) {
    Write-Status "No mcp_agent_mail process detected yet. Check the Git Bash window for errors." "WARN"

    $uv = Get-Command uv -ErrorAction SilentlyContinue
    if ($uv) {
        Write-Status "Attempting fallback via Windows uv.exe..." "WARN"
        Start-Process -FilePath $uv.Source -WorkingDirectory $serverRepo -ArgumentList "run", "python", "-m", "mcp_agent_mail.cli", "serve-http"
        Start-Sleep -Seconds 2
        $started = Get-CimInstance Win32_Process | Where-Object {
            $_.CommandLine -and $_.CommandLine -match "mcp_agent_mail"
        }
        if (-not $started) {
            Write-Status "Fallback start did not detect a running server yet." "WARN"
        }
    }
}
Write-Status "mcp_agent_mail launch requested." "OK"
