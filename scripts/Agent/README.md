# Agent Wrapper Scripts

This folder contains PowerShell scripts designed for Copilot agent auto-approval.

## Why These Exist

Copilot's terminal security blocks commands with pipes (`|`), semicolons (`;`), `&&`, and `2>&1`. These wrapper scripts encapsulate complex operations into simple, auto-approvable commands.

## Available Scripts

| Script | Purpose |
|--------|---------|
| `Git-Search.ps1` | Git operations (show, diff, log, search, blame) with built-in output limiting |
| `Read-FileContent.ps1` | Read files with head/tail limiting and pattern filtering |
| `Test-ContainerEnv.ps1` | Verify VS build environment in Docker containers |
| `Invoke-InContainer.ps1` | Run approved scripts inside containers |
| `Invoke-AgentTask.ps1` | Build/clean/test via container-aware task runner |

## Modules (Internal)

| Module | Purpose |
|--------|---------|
| `AgentInfrastructure.psm1` | Docker, git, and path utilities for agent scripts |
| `VsCodeControl.psm1` | VS Code window management for multi-agent workflows |

## Usage

Use `Get-Help` to see full documentation:

```powershell
Get-Help .\scripts\Agent\Git-Search.ps1 -Full
Get-Help .\scripts\Agent\Read-FileContent.ps1 -Examples
```

## Quick Examples

```powershell
# Git: show file from another branch (first 20 lines)
.\scripts\Agent\Git-Search.ps1 -Action show -Ref "release/9.3" -Path "file.h" -HeadLines 20

# Git: search for pattern in files
.\scripts\Agent\Git-Search.ps1 -Action search -Pattern "IVwGraphics" -Path "Src/"

# File: read first 50 lines with line numbers
.\scripts\Agent\Read-FileContent.ps1 -Path "src/file.cs" -HeadLines 50 -LineNumbers

# File: read last 100 lines of a log
.\scripts\Agent\Read-FileContent.ps1 -Path "build.log" -TailLines 100

# File: find lines matching a pattern
.\scripts\Agent\Read-FileContent.ps1 -Path "src/file.cs" -Pattern "class\s+\w+" -LineNumbers

# Container: test VS environment
.\scripts\Agent\Test-ContainerEnv.ps1

# Container: run build in container
.\scripts\Agent\Invoke-InContainer.ps1 -Script build
```

## Adding New Scripts

When adding a new wrapper script:

1. Use standard PowerShell help (`.SYNOPSIS`, `.DESCRIPTION`, `.PARAMETER`, `.EXAMPLE`)
2. Keep the command invocation simple (no pipes in the final execution)
3. Add parameters like `-HeadLines`/`-TailLines` instead of requiring pipe to `Select-Object`
4. Update this README's script table

The scripts are self-documenting via `Get-Help`, so detailed examples here are optional.
