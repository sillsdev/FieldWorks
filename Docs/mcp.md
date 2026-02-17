# Model Context Protocol helpers

FieldWorks ships a small workspace `mcp.json` so Model Context Protocol clients can spin up two
servers automatically:

- **GitHub server** via the hosted GitHub MCP endpoint (`https://api.githubcopilot.com/mcp/`).
- **Serena server** for accelerated navigation of this large mono-repo.

## Prerequisites

| Component      | Purpose                                   | Install guidance                                      |
| -------------- | ----------------------------------------- | ----------------------------------------------------- |
| VS Code 1.101+ | Remote MCP + OAuth support               | https://code.visualstudio.com                         |
| Serena CLI     | Provides Serena search/navigation        | `pipx install serena-cli` or `uv tool install serena` |
| `uvx`          | Launches Serena from workspace `mcp.json` | https://github.com/astral-sh/uv                       |

> **Note**: Serena **auto-downloads** its language servers on first use:
> - **C# (`csharp`)**: Microsoft.CodeAnalysis.LanguageServer (Roslyn) from Azure NuGet + .NET 9 runtime
> - **C++ (`cpp`)**: clangd 19.1.2 from GitHub releases (Windows/Mac); Linux requires `apt install clangd`
>
> No manual language server installation needed!

Authentication:

- GitHub MCP uses OAuth through your normal VS Code GitHub/Copilot sign-in.
- `SERENA_API_KEY` remains optional and is only needed when your Serena deployment requires it.

## How it works

1. `mcp.json` defines a hosted GitHub MCP server and a local Serena stdio server.
2. GitHub MCP uses VS Code OAuth authentication for repository operations.
3. Serena starts from `uvx oraios-serena --project-root ${workspaceFolder}`.
4. In chat, use tool sets / tool picker to keep active tool counts low and focused.

## Running the servers manually

If you want to test outside an MCP-aware editor:

```powershell
# GitHub server is hosted; test by adding it to VS Code MCP and invoking a GitHub tool.

# Serena server
uvx oraios-serena --project-root .
```

The Serena process runs until you press `Ctrl+C`. When invoked through an MCP host, it stops
when the client disconnects.

## Multiple Worktrees and Serena Conflicts

When working with multiple git worktrees (e.g., `fw-worktrees/agent-1`, `agent-2`, etc.),
each worktree contains its own `.serena/project.yml` file (shared via git). This can cause
issues when Serena auto-discovers projects:

### Symptoms
- `get_current_config` shows multiple projects named "FieldWorks"
- Language server errors that don't match your current worktree
- Serena loads projects from worktrees you're not currently working in

### Cause
VS Code's user-level MCP config (`%APPDATA%\Code\User\mcp.json`) may have a Serena
server that auto-discovers projects by scanning for `.serena` folders. Combined with
workspace-level `mcp.json`, this creates duplicate project registrations.

### Solution
**Use only workspace-level Serena**

Remove or disable the Serena entry from your user-level MCP config:
```powershell
# View current user MCP config
code "$env:APPDATA\Code\User\mcp.json"
```
Remove the `"oraios/serena"` entry. The workspace `mcp.json` provides Serena
with explicit project targeting.

## Best-practice profile for this repo

- Keep repo-level MCP servers minimal: `github` + `serena` only.
- Keep workflow/task conventions in skills/prompt files, not additional MCP servers.
- Enable extra MCP servers only per-task via the tool picker, then disable again.
- If tool list feels noisy, reset cached tools with **MCP: Reset Cached Tools**.

## Worktree best practices

- Open one VS Code window per worktree; let that window use its own workspace `mcp.json`.
- Keep only one Serena server definition active (workspace-level), and remove user-level Serena.
- Keep Serena pinned to the active workspace via `--project-root ${workspaceFolder}` (already configured).
- After switching worktrees, run **MCP: Reset Cached Tools** if tool lists or capabilities look stale.
- No extra GitHub MCP worktree settings are required beyond OAuth sign-in.

## Troubleshooting

- **GitHub tools fail with auth errors** – sign out/in of GitHub in VS Code and restart MCP servers.
- **`uvx` was not found on PATH** – install `uv` and reopen your shell.
- **Unable to locate Serena CLI** – install Serena CLI (via `pipx`/`uv tool install`) so `oraios-serena` resolves.
- **Language server download fails (network error)** – Serena auto-downloads C# (Roslyn) and C++ (clangd)
  language servers on first use. Check network connectivity to Azure NuGet and GitHub releases.
  The download is cached, so subsequent starts are fast.
- **Linux: clangd not found** – on Linux, install clangd manually: `sudo apt-get install clangd`
- **"Language server manager is not initialized"** – restart VS Code; Serena may still be downloading
  language servers on first startup (can take 1-2 minutes for ~250MB of binaries).
