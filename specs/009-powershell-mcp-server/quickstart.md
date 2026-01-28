# Quickstart: PowerShell MCP Server

## Prerequisites

- Python 3.11+
- PowerShell available on PATH (5.1+)
- Repo root on disk (worktree/container aware scripts already exist)

## Install

```powershell
python -m pip install -r scripts/mcp/requirements.txt
```

## Run the server (defaults)

```powershell
python -m scripts.mcp.server --serve --host 127.0.0.1 --port 5000
```

- Defaults: dynamic discovery of `scripts/Agent/*.ps1`, plus `build.ps1` and `test.ps1`.
- Default allowlist: `Git-Search`, `Read-FileContent`, `Invoke-InContainer`, `Invoke-AgentTask`, `build`, `test`, `Copilot-Detect`, `Copilot-Plan`, `Copilot-Apply`, `Copilot-Validate`.
- Limits: timeout 120s, output cap 1MB, working dir = repo root.
- Overrides: provide a JSON config file (see example below) and pass `--config path/to/config.json`.

### Start/stop

- Start: run the command above (optionally add `--config <path>`).
- Stop: `Ctrl+C` in the server terminal.
- CLI mode: `python -m scripts.mcp.server --tool <name> --args ...` runs a single tool without MCP client.

### Example config overrides

```json
{
  "timeout_seconds": 300,
  "output_cap_bytes": 2097152,
  "tools": {
    "allow": ["build", "test", "Git-Search", "Read-FileContent"],
    "deny": ["dangerous-script"]
  },
  "extra_tools": {
    "copilot-detect": "scripts/Agent/Git-Search.ps1"
  }
}
```

## Use with MCP client

Create `mcp.json` in the repo root (or update `.vscode/settings.json` MCP entry):

```json
{
  "mcpServers": {
    "ps-tools": {
      "url": "http://127.0.0.1:5000/sse"
    }
  }
}
```

Restart Copilot / MCP client after starting the server.

- Confirm wiring by running a sample tool (see verification below).

### Safe terminal operations (US1)

These tools map parameters directly to the PowerShell scripts. Examples:

```jsonc
// Git log (head-limited)
{
  "tool": "Git-Search",
  "args": {
    "action": "log",
    "repoPath": "C:/path/to/repo",
    "headLines": 10
  }
}

// File read with head lines
{
  "tool": "Read-FileContent",
  "args": {
    "path": "C:/path/to/file.txt",
    "headLines": 20,
    "lineNumbers": true
  }
}

// Copilot detect (US3)
{
  "tool": "Copilot-Detect",
  "args": {
    "base": "release/9.3",
    "out": ".cache/copilot/detect.json"
  }
}

// Copilot plan (US3)
{
  "tool": "Copilot-Plan",
  "args": {
    "detectJson": ".cache/copilot/detect.json",
    "out": ".cache/copilot/diff-plan.json",
    "base": "release/9.3"
  }
}

// Copilot apply (US3)
{
  "tool": "Copilot-Apply",
  "args": {
    "plan": ".cache/copilot/diff-plan.json",
    "folders": "Src/Common"
  }
}

// Copilot validate (US3)
{
  "tool": "Copilot-Validate",
  "args": {
    "base": "release/9.3",
    "paths": "Src/Common/COPILOT.md"
  }
}
```

## Run a single tool via CLI (no MCP client)

```powershell
python -m scripts.mcp.server --tool build --args "-Configuration" "Debug"
```

## Structured error output (FR-009)

All executions return:

```json
{
  "stdout": "...",
  "stderr": "...",
  "exitCode": 0,
  "truncated": false,
  "timedOut": false
}
```

Non-zero exit codes and timeouts are returned in the body (no protocol error).

## Worktrees/containers

The server calls existing wrapper scripts; host/container routing is preserved automatically by those scripts. No new routing logic is added.

## Troubleshooting

- Missing tool name → response: `{ "stderr": "Tool '<name>' not found", "exitCode": -1 }`.
- Timeout → `{ "timedOut": true, "exitCode": -1 }`; increase `timeout_seconds` in config if needed.
- Large output truncated → `truncated: true`; rerun with larger `output_cap_bytes` or narrow `headLines`/`tailLines`/`pattern`.
- If the server refuses to start, ensure `pip install -r scripts/mcp/requirements.txt` succeeded.

## Verification checkpoint (T025)

1) Start server with defaults: `python -m scripts.mcp.server --serve`.
2) Ensure `mcp.json` is present (or `.vscode/settings.json` MCP entry) pointing to `http://127.0.0.1:5000/sse`.
3) From MCP client, run `Git-Search` with `{ "action": "log", "headLines": 5 }` and expect recent commits returned with `exitCode: 0`.
4) Optionally run `Copilot-Detect` with `{ "out": ".cache/copilot/detect.json" }` to confirm Copilot maintenance wiring.
