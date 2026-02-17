<#
.SYNOPSIS
    Copies locally-built LCM assemblies from an adjacent liblcm folder into the FieldWorks output directory.

.DESCRIPTION
    This script enables developers to test local liblcm fixes without publishing a NuGet package.
    It builds liblcm from a local checkout (by default ../liblcm) and copies the resulting
    assemblies into FieldWorks' Output/<Configuration> folder, overwriting the NuGet versions.

.PARAMETER LcmRoot
    Path to the liblcm repository root. Defaults to ../liblcm (relative to FieldWorks repo).

.PARAMETER FwOutputDir
    Path to FieldWorks output directory. Defaults to Output/<Configuration>.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Debug.

.PARAMETER BuildLcm
    If set, builds liblcm before copying. If not set, just copies existing DLLs.

.PARAMETER SkipConfirm
    If set, skips the confirmation prompt.

.EXAMPLE
    .\Copy-LocalLcm.ps1 -BuildLcm
    Builds liblcm from ../liblcm and copies DLLs to Output/Debug.

.EXAMPLE
    .\Copy-LocalLcm.ps1 -Configuration Release -BuildLcm
    Builds liblcm for Release and copies to Output/Release.

.NOTES
    Use this for local debugging of liblcm issues. Never commit code that depends on this.
#>
[CmdletBinding()]
param(
    [string]$LcmRoot,
    [string]$FwOutputDir,
    [string]$Configuration = "Debug",
    [switch]$BuildLcm,
    [switch]$SkipConfirm
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")

if (-not $LcmRoot) {
    $LcmRoot = Join-Path (Split-Path $repoRoot -Parent) "liblcm"
}

if (-not (Test-Path $LcmRoot)) {
    Write-Error "liblcm not found at '$LcmRoot'. Clone it there or specify -LcmRoot."
    exit 1
}

$lcmSolution = Join-Path $LcmRoot "LCM.sln"
if (-not (Test-Path $lcmSolution)) {
    Write-Error "LCM.sln not found at '$lcmSolution'. Is '$LcmRoot' a valid liblcm checkout?"
    exit 1
}

if (-not $FwOutputDir) {
    $FwOutputDir = Join-Path $repoRoot "Output\$Configuration"
}

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  Local LCM Copy Utility" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  LCM Source:  $LcmRoot" -ForegroundColor White
Write-Host "  FW Output:   $FwOutputDir" -ForegroundColor White
Write-Host "  Config:      $Configuration" -ForegroundColor White
Write-Host "  Build LCM:   $($BuildLcm.IsPresent)" -ForegroundColor White
Write-Host ""

if (-not $SkipConfirm) {
    Write-Host "This will OVERWRITE NuGet LCM DLLs in the FW output directory." -ForegroundColor Yellow
    Write-Host "Only use for local debugging - never commit changes that depend on this." -ForegroundColor Yellow
    Write-Host ""
    $confirm = Read-Host "Continue? [y/N]"
    if ($confirm -notin @('y', 'Y', 'yes', 'Yes')) {
        Write-Host "Aborted." -ForegroundColor Red
        exit 0
    }
}

# Build liblcm if requested
if ($BuildLcm) {
    Write-Host ""
    Write-Host "Building liblcm ($Configuration)..." -ForegroundColor Cyan
    
    Push-Location $LcmRoot
    try {
        # liblcm uses build.cmd for building
        $buildScript = Join-Path $LcmRoot "build.cmd"
        if (-not (Test-Path $buildScript)) {
            throw "build.cmd not found at '$buildScript'. Is '$LcmRoot' a valid liblcm checkout?"
        }
        
        Write-Host "Running: $buildScript" -ForegroundColor Gray
        & cmd /c $buildScript
        if ($LASTEXITCODE -ne 0) {
            throw "liblcm build failed with exit code $LASTEXITCODE"
        }
        Write-Host "[OK] liblcm build complete." -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}

# Find the LCM output directory
# liblcm builds to: artifacts/<Configuration>/net462/<dll>
$lcmBinDir = Join-Path $LcmRoot "artifacts\$Configuration\net462"
if (-not (Test-Path $lcmBinDir)) {
    # Try net472 (alternative target)
    $lcmBinDir = Join-Path $LcmRoot "artifacts\$Configuration\net472"
}
if (-not (Test-Path $lcmBinDir)) {
    # Try net48 (newer versions)
    $lcmBinDir = Join-Path $LcmRoot "artifacts\$Configuration\net48"
}
if (-not (Test-Path $lcmBinDir)) {
    Write-Error "LCM bin directory not found. Build liblcm first with -BuildLcm or manually run 'build.cmd' in '$LcmRoot'."
    exit 1
}

Write-Host ""
Write-Host "Copying LCM assemblies..." -ForegroundColor Cyan
Write-Host "  From: $lcmBinDir" -ForegroundColor Gray

# List of LCM assemblies to copy
$lcmAssemblies = @(
    "SIL.LCModel.dll",
    "SIL.LCModel.pdb",
    "SIL.LCModel.Core.dll",
    "SIL.LCModel.Core.pdb",
    "SIL.LCModel.Utils.dll",
    "SIL.LCModel.Utils.pdb"
)

$copied = 0
$missing = @()

foreach ($asm in $lcmAssemblies) {
    $sourcePath = Join-Path $lcmBinDir $asm
    $destPath = Join-Path $FwOutputDir $asm
    
    if (Test-Path $sourcePath) {
        if (-not (Test-Path $FwOutputDir)) {
            New-Item -Path $FwOutputDir -ItemType Directory -Force | Out-Null
        }
        Copy-Item -Path $sourcePath -Destination $destPath -Force
        Write-Host "  [COPIED] $asm" -ForegroundColor Green
        $copied++
    }
    else {
        # PDB files are optional
        if ($asm -match '\.pdb$') {
            Write-Host "  [SKIP]   $asm (not found)" -ForegroundColor Gray
        }
        else {
            $missing += $asm
            Write-Host "  [WARN]   $asm not found!" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
if ($missing.Count -gt 0) {
    Write-Host "WARNING: Some assemblies were not found:" -ForegroundColor Yellow
    $missing | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    Write-Host ""
    Write-Host "This may indicate liblcm wasn't built, or has a different output structure." -ForegroundColor Yellow
    Write-Host "Try running: dotnet build LCM.sln -c $Configuration in '$LcmRoot'" -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] Copied $copied LCM assembly file(s) to FW output." -ForegroundColor Green
Write-Host ""
Write-Host "IMPORTANT: These local DLLs will be overwritten on next clean build." -ForegroundColor Yellow
Write-Host "To persist the fix, it must be merged to liblcm and a new NuGet package published." -ForegroundColor Yellow
