# Agent Wrapper Scripts

PowerShell scripts for Copilot auto-approval. Use these instead of commands with pipes (`|`), `&&`, or `2>&1`.

## Scripts

| Script | Purpose |
|--------|---------|
| `Git-Search.ps1` | Git operations (show, diff, log, grep, blame) |
| `Read-FileContent.ps1` | Read files with head/tail/pattern filtering |
| `Invoke-InContainer.ps1` | Run scripts inside Docker containers |
| `Invoke-AgentTask.ps1` | Container-aware build/clean/test |

## Usage

```powershell
Get-Help .\scripts\Agent\Git-Search.ps1 -Full
```

## Modules (Internal)

- `AgentInfrastructure.psm1` — Docker, git, and path utilities
- `VsCodeControl.psm1` — VS Code window management