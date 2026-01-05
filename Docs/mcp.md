# Model Context Protocol helpers

FieldWorks ships a small `mcp.json` so Model Context Protocol clients can spin up two
servers automatically:

- **GitHub server** via `@modelcontextprotocol/server-github` for read/write access to
  `sillsdev/FieldWorks`.
- **Serena server** for accelerated navigation of this large mono-repo.

## Prerequisites

| Component           | Purpose                                    | Install guidance                                      |
| ------------------- | ------------------------------------------ | ----------------------------------------------------- |
| Node.js 18+ (`npx`) | Launches the GitHub MCP server package     | https://nodejs.org                                    |
| Serena CLI          | Provides Serena search/navigation          | `pipx install serena-cli` or `uv tool install serena` |
| `uvx` (optional)    | Used as a fallback launcher for Serena     | https://github.com/astral-sh/uv                       |
| PowerShell 5.1+     | Both helper scripts run through PowerShell | Preinstalled on Windows                               |

> **Note**: Serena **auto-downloads** its language servers on first use:
> - **C# (`csharp`)**: Microsoft.CodeAnalysis.LanguageServer (Roslyn) from Azure NuGet + .NET 9 runtime
> - **C++ (`cpp`)**: clangd 19.1.2 from GitHub releases (Windows/Mac); Linux requires `apt install clangd`
>
> No manual language server installation needed!

Required environment variables:

- `GITHUB_TOKEN`: PAT with at least `repo` scope so the MCP GitHub server can read issues,
  pull requests, and apply patches.
- `SERENA_API_KEY` (optional): Needed when your Serena deployment requires authentication.

## How it works

1. `mcp.json` points at two helper scripts under `scripts/mcp/`.
2. `start-github-server.ps1` validates `GITHUB_TOKEN`, confirms `npx` is available, and
   executes `npx --yes @modelcontextprotocol/server-github --repo sillsdev/FieldWorks`.
3. `start-serena-server.ps1` locates the Serena CLI (`serena`, `uvx serena`, or `uv run serena`),
   then runs `serena serve --project .serena/project.yml` so MCP clients can issue Serena searches.

Because the scripts perform their own validation, failures are easier to diagnose than if the
MCP client invoked the raw binaries.

## Running the servers manually

If you want to test outside an MCP-aware editor:

```powershell
# GitHub server
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/mcp/start-github-server.ps1

# Serena server (override host/port example)
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/mcp/start-serena-server.ps1 -BindHost localhost -Port 3334
```

The scripts run until you press `Ctrl+C`. When invoked through an MCP host, they automatically
stop when the client disconnects.

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
Remove the `"oraios/serena"` entry. The workspace `mcp.json` will provide Serena
with explicit project targeting.

## Troubleshooting

- **`GITHUB_TOKEN is not set`** – export a PAT (`setx GITHUB_TOKEN <token>` or use a
  secrets manager) before starting the GitHub server.
- **`npx was not found on PATH`** – install Node.js and reopen your shell.
- **`Unable to locate the Serena CLI`** – install the Serena CLI (via `pipx`, `uv tool install`,
  or ensure `uvx` is available) so the helper can find at least one launcher.
- **Port already in use** – pass `-Port <number>` to `start-serena-server.ps1` to pick an open port.
- **Language server download fails (network error)** – Serena auto-downloads C# (Roslyn) and C++ (clangd)
  language servers on first use. Check network connectivity to Azure NuGet and GitHub releases.
  The download is cached, so subsequent starts are fast.
- **Linux: clangd not found** – On Linux, install clangd manually: `sudo apt-get install clangd`
- **"Language server manager is not initialized"** – restart VS Code; Serena may still be downloading
  language servers on first startup (can take 1-2 minutes for ~250MB of binaries).
