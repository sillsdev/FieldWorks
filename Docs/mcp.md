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
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/mcp/start-serena-server.ps1 -Host localhost -Port 3334
```

The scripts run until you press `Ctrl+C`. When invoked through an MCP host, they automatically
stop when the client disconnects.

## Troubleshooting

- **`GITHUB_TOKEN is not set`** – export a PAT (`setx GITHUB_TOKEN <token>` or use a
  secrets manager) before starting the GitHub server.
- **`npx was not found on PATH`** – install Node.js and reopen your shell.
- **`Unable to locate the Serena CLI`** – install the Serena CLI (via `pipx`, `uv tool install`,
  or ensure `uvx` is available) so the helper can find at least one launcher.
- **Port already in use** – pass `-Port <number>` to `start-serena-server.ps1` to pick an open port.
