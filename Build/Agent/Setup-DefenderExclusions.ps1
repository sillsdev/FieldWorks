<#
.SYNOPSIS
    Configures Windows Defender exclusions for FieldWorks development.

.DESCRIPTION
    This script adds Windows Defender exclusions for paths and processes used
    during FieldWorks development. Without these exclusions, real-time scanning
    can cause significant slowdowns during builds, NuGet restores, and IDE usage.

    MUST BE RUN AS ADMINISTRATOR.

    Exclusions added:
    - Repository and worktree paths (source code, build outputs)
    - NuGet package caches (global, per-repo, per-agent)
    - Build tool processes (MSBuild, cl.exe, dotnet.exe, etc.)
    - IDE processes (VS Code, Visual Studio, language servers)
    - Docker paths and processes
    - Temp folders used during package extraction

.PARAMETER RepoRoot
    Path to the FieldWorks repository. Default: auto-detected from script location.

.PARAMETER DryRun
    Show what would be added without making changes.

.PARAMETER Remove
    Remove the exclusions instead of adding them.

.EXAMPLE
    # Run from Admin PowerShell
    .\Build\Agent\Setup-DefenderExclusions.ps1

.EXAMPLE
    # Preview changes without applying
    .\Build\Agent\Setup-DefenderExclusions.ps1 -DryRun

.EXAMPLE
    # Remove all FieldWorks exclusions
    .\Build\Agent\Setup-DefenderExclusions.ps1 -Remove

.NOTES
    Requires Administrator privileges.
    Exclusions take effect immediately - no restart required.

    Security note: These exclusions reduce protection for development folders.
    This is standard practice for developer workstations but should not be
    applied to production or shared systems.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$RepoRoot,
    [switch]$DryRun,
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'

# =============================================================================
# Administrator Check
# =============================================================================

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin -and -not $DryRun) {
    Write-Host ""
    Write-Host "ERROR: This script must be run as Administrator." -ForegroundColor Red
    Write-Host ""
    Write-Host "Please:" -ForegroundColor Yellow
    Write-Host "  1. Right-click PowerShell or Windows Terminal"
    Write-Host "  2. Select 'Run as Administrator'"
    Write-Host "  3. Navigate to this repo and run the script again"
    Write-Host ""
    Write-Host "Or use -DryRun to preview changes without Administrator." -ForegroundColor Gray
    Write-Host ""
    exit 1
}

# =============================================================================
# Detect Repository Root
# =============================================================================

if (-not $RepoRoot) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSCommandPath))
}

if (-not (Test-Path (Join-Path $RepoRoot "FieldWorks.sln"))) {
    throw "Could not find FieldWorks.sln in '$RepoRoot'. Specify -RepoRoot explicitly."
}

$RepoRoot = (Resolve-Path $RepoRoot).Path
$UserProfile = $env:USERPROFILE
$WorktreesRoot = Split-Path -Parent $RepoRoot | Join-Path -ChildPath "fw-worktrees"

Write-Host ""
Write-Host "FieldWorks Defender Exclusions Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Repository: $RepoRoot"
Write-Host "User Profile: $UserProfile"
Write-Host ""

# =============================================================================
# Define Exclusions
# =============================================================================

$pathExclusions = @(
    # -------------------------------------------------------------------------
    # Repository paths
    # -------------------------------------------------------------------------
    $RepoRoot,                                          # Main repo (covers Src/, Output/, Obj/, packages/)
    (Join-Path $RepoRoot ".nuget"),                     # Per-agent NuGet caches
    (Join-Path $RepoRoot "Output"),                     # Build outputs
    (Join-Path $RepoRoot "Obj"),                        # Intermediate files
    (Join-Path $RepoRoot "packages"),                   # NuGet packages (host builds)

    # -------------------------------------------------------------------------
    # Worktrees (if using multi-agent setup)
    # -------------------------------------------------------------------------
    $WorktreesRoot,                                     # All worktrees

    # -------------------------------------------------------------------------
    # NuGet caches
    # -------------------------------------------------------------------------
    (Join-Path $UserProfile ".nuget"),                  # Global NuGet cache
    (Join-Path $UserProfile ".nuget\packages"),         # Global packages folder
    "C:\.nuget",                                        # Container NuGet path
    (Join-Path $UserProfile "AppData\Local\NuGet"),     # NuGet local data (http-cache, plugins-cache)
    (Join-Path $UserProfile "AppData\Local\NuGet\v3-cache"),  # HTTP cache
    (Join-Path $UserProfile "AppData\Local\NuGet\plugins-cache"),  # Plugins cache
    (Join-Path $UserProfile "AppData\Local\Temp\NuGetScratch"),    # NuGet temp/scratch

    # -------------------------------------------------------------------------
    # Docker
    # -------------------------------------------------------------------------
    "C:\ProgramData\Docker",                            # Docker data
    "C:\ProgramData\Docker\windowsfilter",              # Windows container layers (CRITICAL for image pulls)
    "C:\ProgramData\Docker\image",                      # Docker image metadata
    "C:\ProgramData\Docker\containers",                 # Running container data
    "C:\Program Files\Docker",                          # Docker installation
    (Join-Path $UserProfile ".docker"),                 # Docker user config
    (Join-Path $UserProfile "AppData\Local\Docker"),    # Docker local data

    # -------------------------------------------------------------------------
    # VS Code
    # -------------------------------------------------------------------------
    (Join-Path $UserProfile "AppData\Local\Programs\Microsoft VS Code"),  # VS Code installation
    (Join-Path $UserProfile ".vscode"),                 # VS Code extensions
    (Join-Path $UserProfile ".vscode-server"),          # VS Code Server (remote)
    (Join-Path $UserProfile "AppData\Roaming\Code"),    # VS Code user data

    # -------------------------------------------------------------------------
    # Serena language servers
    # -------------------------------------------------------------------------
    (Join-Path $UserProfile ".serena"),                 # Serena MCP (OmniSharp, clangd)

    # -------------------------------------------------------------------------
    # Visual Studio
    # -------------------------------------------------------------------------
    "C:\Program Files\Microsoft Visual Studio",         # VS installation
    "C:\Program Files (x86)\Microsoft Visual Studio",   # VS x86 components

    # -------------------------------------------------------------------------
    # .NET / dotnet
    # -------------------------------------------------------------------------
    "C:\Program Files\dotnet",                          # .NET SDK

    # -------------------------------------------------------------------------
    # FieldWorks app data (runtime data)
    # -------------------------------------------------------------------------
    (Join-Path $UserProfile "AppData\Local\SIL"),       # FieldWorks user data

    # -------------------------------------------------------------------------
    # Developer tools (Setup-Developer-Machine.ps1 location)
    # -------------------------------------------------------------------------
    (Join-Path $UserProfile "AppData\Local\FieldWorksTools"),  # WiX, etc. on dev machines

    # -------------------------------------------------------------------------
    # WiX Toolset
    # -------------------------------------------------------------------------
    "C:\Wix314",                                        # WiX on containers/CI

    # -------------------------------------------------------------------------
    # Container-specific paths (VS Build Tools, .NET SDK, clangd)
    # -------------------------------------------------------------------------
    "C:\BuildTools",                                    # VS Build Tools in container
    "C:\dotnet",                                        # .NET SDK in container
    "C:\dotnet9",                                       # .NET 9 Runtime (Serena C# server)
    "C:\clangd",                                        # clangd in container

    # -------------------------------------------------------------------------
    # Temp folders (package extraction)
    # -------------------------------------------------------------------------
    (Join-Path $UserProfile "AppData\Local\Temp"),      # User temp folder
    "C:\Temp",                                          # Container temp (includes C:\Temp\Obj)
    "C:\TEMP",                                          # Container temp (case variant)
    "C:\Windows\Temp",                                  # System temp folder

    # -------------------------------------------------------------------------
    # Symbol cache and debug symbols
    # -------------------------------------------------------------------------
    (Join-Path $UserProfile "AppData\Local\Microsoft\VisualStudio"),  # VS local data, symbol cache
    (Join-Path $UserProfile "AppData\Local\Microsoft\VSCommon"),      # VS common data
    (Join-Path $UserProfile "AppData\Roaming\Microsoft\VisualStudio"), # VS roaming data

    # -------------------------------------------------------------------------
    # Windows SDK and build tools
    # -------------------------------------------------------------------------
    "C:\Program Files (x86)\Windows Kits",              # Windows SDK
    "C:\Program Files (x86)\Microsoft SDKs",            # Microsoft SDKs
    "C:\Program Files\Microsoft SDKs"                   # Microsoft SDKs (x64)
)

$processExclusions = @(
    # -------------------------------------------------------------------------
    # Build tools (C++/C#)
    # -------------------------------------------------------------------------
    "MSBuild.exe",
    "dotnet.exe",
    "cl.exe",                                           # C++ compiler
    "link.exe",                                         # Linker
    "lib.exe",                                          # Library manager
    "ml64.exe",                                         # MASM assembler
    "nmake.exe",
    "csc.exe",                                          # C# compiler
    "csc.dll",
    "VBCSCompiler.exe",                                 # Roslyn compiler server
    "nuget.exe",
    "midl.exe",                                         # IDL compiler (COM interfaces)

    # -------------------------------------------------------------------------
    # WiX Toolset (installer builds)
    # -------------------------------------------------------------------------
    "candle.exe",                                       # WiX compiler
    "light.exe",                                        # WiX linker
    "heat.exe",                                         # WiX harvester

    # -------------------------------------------------------------------------
    # Test runners
    # -------------------------------------------------------------------------
    "vstest.console.exe",                               # VS Test runner
    "testhost.exe",                                     # .NET test host
    "nunit3-console.exe",                               # NUnit (if used)

    # -------------------------------------------------------------------------
    # VS Code
    # -------------------------------------------------------------------------
    "Code.exe",
    "cpptools.exe",
    "cpptools-srv.exe",
    "Microsoft.VisualStudio.Code.ServiceHost.exe",

    # -------------------------------------------------------------------------
    # Language servers
    # -------------------------------------------------------------------------
    "OmniSharp.exe",
    "clangd.exe",
    "Microsoft.CodeAnalysis.LanguageServer.exe",        # Roslyn C# server (Serena)

    # -------------------------------------------------------------------------
    # Visual Studio
    # -------------------------------------------------------------------------
    "devenv.exe",
    "PerfWatson2.exe",
    "ServiceHub.Host.CLR.x64.exe",                      # VS service hub
    "ServiceHub.Host.dotnet.x64.exe",                   # VS dotnet service hub
    "ServiceHub.IdentityHost.exe",
    "ServiceHub.VSDetouredHost.exe",
    "ServiceHub.RoslynCodeAnalysisService.exe",         # Roslyn analysis
    "ServiceHub.ThreadedWaitDialog.exe",
    "Microsoft.ServiceHub.Controller.exe",

    # -------------------------------------------------------------------------
    # Docker
    # -------------------------------------------------------------------------
    "docker.exe",
    "dockerd.exe",
    "com.docker.backend.exe",
    "com.docker.build.exe",                             # Docker build process
    "com.docker.service.exe",
    "Docker Desktop.exe",
    "docker-language-server-windows-amd64.exe",         # VS Code Docker extension

    # -------------------------------------------------------------------------
    # Windows Containers (Hyper-V isolation)
    # -------------------------------------------------------------------------
    "vmcompute.exe",                                    # Hyper-V compute service
    "vmwp.exe",                                         # VM worker process
    "CExecSvc.exe",                                     # Container execution service
    "hcsdiag.exe",                                      # Host compute diagnostics
    "containerd.exe",                                   # Container daemon
    "hcsshim.exe",                                      # Host compute shim
    "smss.exe",                                         # Windows Session Manager (container)
    "wininit.exe",                                      # Windows initialization

    # -------------------------------------------------------------------------
    # Other common dev tools
    # -------------------------------------------------------------------------
    "git.exe",
    "node.exe",
    "npm.exe",
    "java.exe",
    "javac.exe",
    "python.exe",
    "python3.exe",
    "msedgewebview2.exe",
    "powershell.exe",
    "pwsh.exe",
    "conhost.exe",
    "OpenConsole.exe",                                  # Windows Terminal
    "WindowsTerminal.exe",
    "explorer.exe",
    "cmd.exe",

    # -------------------------------------------------------------------------
    # Remote development
    # -------------------------------------------------------------------------
    "remoting_host.exe",                                # Chrome Remote Desktop
    "chrome.exe",
    "msedge.exe"
)

# =============================================================================
# Get Current Exclusions (if admin)
# =============================================================================

$currentPathExclusions = @()
$currentProcessExclusions = @()

if ($isAdmin) {
    try {
        $prefs = Get-MpPreference -ErrorAction SilentlyContinue
        if ($prefs.ExclusionPath) { $currentPathExclusions = $prefs.ExclusionPath }
        if ($prefs.ExclusionProcess) { $currentProcessExclusions = $prefs.ExclusionProcess }
    }
    catch {
        Write-Host "  (Could not read current exclusions)" -ForegroundColor DarkGray
    }
}

# =============================================================================
# Apply or Remove Exclusions
# =============================================================================

$action = if ($Remove) { "Removing" } else { "Adding" }
$verb = if ($Remove) { "Remove" } else { "Add" }

Write-Host "$action Path Exclusions:" -ForegroundColor Yellow
Write-Host "-" * 50

foreach ($path in $pathExclusions) {
    $alreadyExists = $currentPathExclusions -contains $path

    if ($DryRun) {
        if ($alreadyExists -and -not $Remove) {
            Write-Host "  [EXISTS] $path" -ForegroundColor DarkGreen
        }
        else {
            Write-Host "  [DryRun] Would $verb`: $path" -ForegroundColor Gray
        }
    }
    else {
        try {
            if ($Remove) {
                Remove-MpPreference -ExclusionPath $path -ErrorAction SilentlyContinue
            }
            else {
                Add-MpPreference -ExclusionPath $path -ErrorAction SilentlyContinue
            }
            if ($alreadyExists -and -not $Remove) {
                Write-Host "  [EXISTS] $path" -ForegroundColor DarkGreen
            }
            else {
                Write-Host "  [OK] $path" -ForegroundColor Green
            }
        }
        catch {
            Write-Host "  [SKIP] $path - $($_.Exception.Message)" -ForegroundColor DarkGray
        }
    }
}

Write-Host ""
Write-Host "$action Process Exclusions:" -ForegroundColor Yellow
Write-Host "-" * 50

foreach ($proc in $processExclusions) {
    $alreadyExists = $currentProcessExclusions -contains $proc

    if ($DryRun) {
        if ($alreadyExists -and -not $Remove) {
            Write-Host "  [EXISTS] $proc" -ForegroundColor DarkGreen
        }
        else {
            Write-Host "  [DryRun] Would $verb`: $proc" -ForegroundColor Gray
        }
    }
    else {
        try {
            if ($Remove) {
                Remove-MpPreference -ExclusionProcess $proc -ErrorAction SilentlyContinue
            }
            else {
                Add-MpPreference -ExclusionProcess $proc -ErrorAction SilentlyContinue
            }
            if ($alreadyExists -and -not $Remove) {
                Write-Host "  [EXISTS] $proc" -ForegroundColor DarkGreen
            }
            else {
                Write-Host "  [OK] $proc" -ForegroundColor Green
            }
        }
        catch {
            Write-Host "  [SKIP] $proc - $($_.Exception.Message)" -ForegroundColor DarkGray
        }
    }
}

# =============================================================================
# Summary
# =============================================================================

Write-Host ""
Write-Host "=" * 50 -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "DRY RUN COMPLETE - No changes were made." -ForegroundColor Yellow
    if (-not $isAdmin) {
        Write-Host "[EXISTS] markers require Administrator to check current exclusions." -ForegroundColor DarkGray
    }
    Write-Host "Run without -DryRun (as Admin) to apply exclusions."
}
elseif ($Remove) {
    Write-Host "EXCLUSIONS REMOVED" -ForegroundColor Green
    Write-Host "Windows Defender will now scan these locations."
}
else {
    Write-Host "EXCLUSIONS APPLIED" -ForegroundColor Green
    Write-Host "[EXISTS] = already configured, [OK] = newly added"
    Write-Host "Changes take effect immediately - no restart required."
}

Write-Host ""
