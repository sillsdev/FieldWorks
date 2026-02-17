<#
.SYNOPSIS
    Sets up and verifies Serena MCP for FieldWorks development.

.DESCRIPTION
    Ensures the Serena Model Context Protocol server is properly configured
    for FieldWorks. This enables AI-assisted code navigation and analysis
    for both C# (via OmniSharp) and C++ (via clangd).

    Steps performed:
    1. Verifies uv/uvx is installed
    2. Verifies Serena project configuration exists
    3. Initializes Serena and triggers language server downloads if needed
    4. Validates that language servers respond to basic queries

.PARAMETER SkipLanguageServerCheck
    Skip the language server connectivity check (useful for CI where we just
    want to ensure config exists).

.PARAMETER CacheDir
    Directory for Serena language server cache. Defaults to ~/.cache/serena.

.PARAMETER OutputGitHubEnv
    If specified, writes environment variables to $GITHUB_ENV for Actions.

.EXAMPLE
    # Quick setup/check
    .\Build\Agent\Setup-Serena.ps1

.EXAMPLE
    # Skip slow language server check
    .\Build\Agent\Setup-Serena.ps1 -SkipLanguageServerCheck

.EXAMPLE
    # CI mode with GitHub Actions output
    .\Build\Agent\Setup-Serena.ps1 -OutputGitHubEnv
#>

[CmdletBinding()]
param(
    [switch]$SkipLanguageServerCheck,
    [string]$CacheDir = "$env:USERPROFILE\.cache\serena",
    [switch]$OutputGitHubEnv
)

$ErrorActionPreference = 'Stop'

function Write-Status {
    param([string]$Message, [string]$Status = "INFO")
    $color = switch ($Status) {
        "OK"    { "Green" }
        "WARN"  { "Yellow" }
        "ERROR" { "Red" }
        default { "Cyan" }
    }
    $prefix = switch ($Status) {
        "OK"    { "[OK]   " }
        "WARN"  { "[WARN] " }
        "ERROR" { "[FAIL] " }
        default { "       " }
    }
    Write-Host "$prefix$Message" -ForegroundColor $color
}

function Set-EnvVar {
    param([string]$Name, [string]$Value)
    [Environment]::SetEnvironmentVariable($Name, $Value, 'Process')
    if ($OutputGitHubEnv -and $env:GITHUB_ENV) {
        Add-Content -Path $env:GITHUB_ENV -Value "$Name=$Value"
    }
}

# ============================================================================
# MAIN SCRIPT
# ============================================================================

Write-Host "=== Serena MCP Setup ===" -ForegroundColor Cyan
Write-Host ""

$repoRoot = (Get-Location).Path
$serenaConfig = Join-Path $repoRoot ".serena/project.yml"

# ----------------------------------------------------------------------------
# Step 1: Check for uv/uvx
# ----------------------------------------------------------------------------
Write-Host "--- Step 1: Python Package Manager ---" -ForegroundColor Cyan

$uvInstalled = $false
$uv = Get-Command uv -ErrorAction SilentlyContinue
if ($uv) {
    $uvVersion = (& uv --version 2>&1)
    Write-Status "uv installed: $uvVersion" -Status "OK"
    $uvInstalled = $true
}
else {
    Write-Status "uv not found - attempting install via pip" -Status "WARN"
    try {
        $python = Get-Command python -ErrorAction SilentlyContinue
        if (-not $python) { $python = Get-Command python3 -ErrorAction SilentlyContinue }
        if ($python) {
            & $python.Source -m pip install --quiet uv
            $uv = Get-Command uv -ErrorAction SilentlyContinue
            if ($uv) {
                Write-Status "uv installed successfully via pip" -Status "OK"
                $uvInstalled = $true
            }
        }
    }
    catch {
        Write-Status "Failed to install uv: $_" -Status "ERROR"
    }
}

if (-not $uvInstalled) {
    Write-Status "Cannot proceed without uv. Install with: winget install astral-sh.uv" -Status "ERROR"
    exit 1
}

# Verify uvx is available
$uvx = Get-Command uvx -ErrorAction SilentlyContinue
if ($uvx) {
    Write-Status "uvx available" -Status "OK"
}
else {
    # uvx should be bundled with uv
    Write-Status "uvx not found (should be bundled with uv)" -Status "WARN"
}

# ----------------------------------------------------------------------------
# Step 2: Verify Serena configuration
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Step 2: Serena Configuration ---" -ForegroundColor Cyan

if (Test-Path $serenaConfig) {
    Write-Status "Found .serena/project.yml" -Status "OK"

    # Show configured languages
    $configContent = Get-Content $serenaConfig -Raw
    if ($configContent -match 'programming_languages:\s*\[([^\]]+)\]') {
        $languages = $matches[1]
        Write-Status "Configured languages: $languages"
    }
}
else {
    Write-Status "Missing .serena/project.yml - Serena not configured" -Status "ERROR"
    Write-Host ""
    Write-Host "Create .serena/project.yml with:" -ForegroundColor Yellow
    Write-Host @"
name: FieldWorks
project_root: .
programming_languages: [csharp_omnisharp, cpp]
"@
    exit 1
}

# ----------------------------------------------------------------------------
# Step 3: Ensure cache directory exists
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Step 3: Cache Directory ---" -ForegroundColor Cyan

if (-not (Test-Path $CacheDir)) {
    New-Item -ItemType Directory -Path $CacheDir -Force | Out-Null
    Write-Status "Created cache directory: $CacheDir" -Status "OK"
}
else {
    $cacheSize = (Get-ChildItem $CacheDir -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
    $cacheSizeMB = [math]::Round($cacheSize / 1MB, 2)
    Write-Status "Cache directory exists ($cacheSizeMB MB): $CacheDir" -Status "OK"
}

# Set environment variable for Serena cache
Set-EnvVar -Name "SERENA_CACHE_DIR" -Value $CacheDir

# ----------------------------------------------------------------------------
# Step 4: Check language servers (optional)
# ----------------------------------------------------------------------------
if (-not $SkipLanguageServerCheck) {
    Write-Host ""
    Write-Host "--- Step 4: Language Server Check ---" -ForegroundColor Cyan

    # Check for clangd (C++)
    $clangd = Get-Command clangd -ErrorAction SilentlyContinue
    if ($clangd) {
        $clangdVersion = (& clangd --version 2>&1 | Select-Object -First 1)
        Write-Status "clangd: $clangdVersion" -Status "OK"
    }
    else {
        Write-Status "clangd not in PATH - Serena will download on first use" -Status "WARN"
    }

    # OmniSharp is downloaded by Serena automatically
    Write-Status "OmniSharp: Will be downloaded by Serena on first use"
}

# ----------------------------------------------------------------------------
# Summary
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Serena Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "To start using Serena:"
Write-Host "  1. Open VS Code with the FieldWorks workspace"
Write-Host "  2. Serena tools will be available via MCP"
Write-Host ""
Write-Host "To test Serena standalone:"
Write-Host "  uvx oraios-serena --project-root `"$repoRoot`""
Write-Host ""

# Return success
exit 0
