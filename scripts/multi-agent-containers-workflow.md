# Multi-agent local development (Windows containers + Git worktrees) for FieldWorks

This workflow enables 2-5 concurrent "agents," each with:
- Its own Git branch in a dedicated worktree
- Its own Windows container for build/test so COM and registry writes stay isolated
- Its own VS Code window (Copilot still runs per window)

Git happens on the host; builds and tests happen inside containers.

## Prerequisites

- Windows 11/Server with Docker Desktop in Windows-containers mode
- One local clone of FieldWorks (for example `C:\dev\FieldWorks`)
- PowerShell 5+
- 32 GB RAM recommended for 2-5 agents (~3-4 GB per container)

## Repo files

- `Dockerfile.windows` - builds the image with .NET Framework 4.8 SDK, VS Build Tools, and C++
- `scripts/spin-up-agents.ps1` - creates worktrees and starts containers
- `scripts/templates/tasks.template.json` - VS Code task template written per worktree
- `scripts/tear-down-agents.ps1` - stops/removes containers and optionally worktrees

Place these at the repo root under the paths shown above.

## Quick start

1. Optional: choose where worktrees live (default is `<RepoRoot>\worktrees`)
   ```powershell
   $env:FW_WORKTREES_ROOT = "C:\dev\FieldWorks\worktrees"
   ```

2) Build the image and spin up 3 agents (adjust `Count` as 2–5)
```
.\scripts\spin-up-agents.ps1 `
  -RepoRoot "C:\dev\FieldWorks" `
  -Count 3 `
  -BaseRef origin/release/9.3 `
  -CreateVsCodeTasks `
  -OpenVSCode
```

- `-RepoRoot` (required): Path to your FieldWorks clone
- `-Count` (required): Number of agents (2-5 recommended)
- `-BaseRef`: Base branch (default: `origin/release/9.3`)
- `-SolutionRelPath`: Solution file path (default: `FieldWorks.sln`)

The script will:
   ```powershell
   .\scripts\spin-up-agents.ps1 `
     -RepoRoot "C:\dev\FieldWorks" `
     -Count 3 `
     -BaseRef origin/release/9.3 `
     -SolutionRelPath "Build\FW.sln" `
     -CreateVsCodeTasks `
     -OpenVSCode
   ```

   - `-BaseRef` - base branch for agent branches (release branches like `origin/release/9.3` are common, but change if needed)
   - `-SolutionRelPath` - solution path relative to each worktree root
   - `-CreateVsCodeTasks` - writes `.vscode/tasks.json` plus color-coded `settings.json` per agent
   - `-OpenVSCode` - launches a VS Code window per agent with `FW_AGENT_CONTAINER` set

The script will:
- Create worktrees at `...\worktrees\agent-1..N` with branches `agents/agent-1..N`
- Build or reuse the `fw-build:ltsc2022` Windows image
- Start containers `fw-agent-1..N` with per-agent NuGet caches
- Generate `.vscode/tasks.json` wired to the matching container
- Generate `.vscode/settings.json` with unique colors to keep agent windows visually distinct
- Optionally open each worktree in a new VS Code window

3. Work per agent

- Open the worktree folder in VS Code (one window per agent)
- Use Copilot, edit code, and run Git commands on the host worktree directory
- Build or test via **Terminal -> Run Task -> Restore + Build Debug** (runs inside the matching container)

All registry and COM operations occur inside the container, isolated from your host and other agents.

## Git workflow

- Run Git commands on the host from the worktree directory:
  - `git status`, `git commit`, `git push`
  - `gh pr create -H agents/agent-1 -B release/9.3 -t "..." -b "..."`
- Avoid running Git inside the container for that worktree to prevent file locking.

## Performance and limits

- Avoid building all 5 agents simultaneously on a 32 GB machine; stagger builds or reduce msbuild parallelism
- Each container defaults to a 4 GB memory cap (adjust in `spin-up-agents.ps1` if required)
- Keep the repo and Docker data on SSD/Dev Drive for best performance

## Teardown

Stop and remove containers (keeps worktrees/branches):
```powershell
.\scripts\tear-down-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -Count 3
```

Remove containers, worktrees, agent branches, and per-agent NuGet caches:
```powershell
.\scripts\tear-down-agents.ps1 `
  -RepoRoot "C:\dev\FieldWorks" `
  -Count 3 `
  -RemoveWorktrees
```

You can pass `-WorktreesRoot` if you placed worktrees outside the default path; otherwise the script respects `FW_WORKTREES_ROOT` when present.

## Notes

## Configuration options

### Environment variables
- `FW_WORKTREES_ROOT`: Override default worktree location (default: `<RepoRoot>\worktrees`)

### Script parameters
See `-Help` on each script for full parameter lists:
```powershell
Get-Help .\scripts\spin-up-agents.ps1 -Detailed
Get-Help .\scripts\tear-down-agents.ps1 -Detailed
```

### Customizing colors
Edit the `Get-AgentColors` function in `spin-up-agents.ps1` to change color schemes. Current palette uses Tailwind CSS color tokens for consistency.

### Resource limits
Adjust container memory in `spin-up-agents.ps1` (line ~133):
```powershell
"--memory","4g",  # Change to "8g" for larger builds
```

### Base image compatibility
Docker image tag uses `ltsc2022`. If your Windows version is different, update `Dockerfile.windows`:
- Windows Server 2019 / Windows 10 1809: `ltsc2019`
- Windows Server 2022 / Windows 11: `ltsc2022` (default)

## Troubleshooting

### "Docker is not in Windows containers mode"
Switch Docker Desktop to Windows containers:
1. Right-click Docker Desktop system tray icon
2. Select "Switch to Windows containers..."
3. Wait for restart, then retry

### Container fails to start
Check available resources:
```powershell
docker info  # Check available memory
docker ps -a  # List all containers
docker logs fw-agent-1  # Check container logs
```

### Worktree already exists
The script safely reuses existing worktrees and containers. To start fresh:
```powershell
.\scripts\tear-down-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -Count 3 -RemoveWorktrees
```

### Build fails inside container
Verify the solution path is correct and accessible:
```powershell
docker exec fw-agent-1 powershell -c "Test-Path 'Build\FW.sln'"
```

## Notes

- The repo root is bind-mounted at the same absolute path inside each container to keep Git worktree pointers valid (even if you rarely use Git inside containers).
- Each container has a separate NuGet cache at `C:\.nuget\packages` mapped to a per-agent host folder to avoid cross-agent churn.
- Containers run indefinitely (sleep 1 year) so they persist across reboots; use `docker start fw-agent-N` to restart after host reboot.
