# Enabling WinForms MCP control of FieldWorks (Claude Code)

This is the one-time setup that makes the `winforms_*` tools appear in a Claude Code session so the
navigation routes in this skill can drive the live FieldWorks app.

## Why the tools may be missing ("the MCP server isn't working")

The winforms-mcp server (`@fnrhombus/winforms-mcp`) is configured in **two different files for two
different clients**, and they are NOT interchangeable:

- `.vscode/mcp.json` — read by **VS Code**'s MCP client. (Has the `"servers"` key.)
- `.mcp.json` (repo root) — read by **Claude Code**'s MCP client. (Has the `"mcpServers"` key.)

If only `.vscode/mcp.json` exists, VS Code can drive FieldWorks but **Claude Code sees no `winforms_*`
tools** — `ToolSearch "winforms"` returns nothing. The fix is the repo-root `.mcp.json` (committed in this
change). Claude Code loads MCP servers **at session start**, so after `.mcp.json` is added you must:

1. **Reconnect / restart Claude Code** (or run `/mcp` to reload), and
2. **Approve the project MCP server** when prompted (project-scoped servers require explicit approval the
   first time — a security gate).

Verify it took: `claude mcp list` should show `winforms-mcp`, and `ToolSearch "winforms"` should surface
`winforms_launch_app`, `winforms_take_screenshot`, etc.

## The `.mcp.json` entry (Windows-robust form)

```jsonc
{
  "mcpServers": {
    "winforms-mcp": {
      "command": "cmd",                                  // see note
      "args": ["/c", "npx", "-y", "@fnrhombus/winforms-mcp"],
      "env": { "HEADLESS": "true", "TFM": "net48", "TELEMETRY_OPTOUT": "true" }
    }
  }
}
```

Note — **`cmd /c npx`, not bare `npx`**: on Windows, Claude Code spawns the command without a shell, and
`npx` is a `.cmd`/`.ps1` shim that fails to spawn directly (and PowerShell mangles the leading `@` of the
scoped package name). Wrapping in `cmd /c` is the reliable form. `HEADLESS=true` runs FieldWorks on a
hidden desktop so it does not steal the foreground; `TFM=net48` matches the FieldWorks managed target.

## Preflight

Before a session, run the preflight to confirm prerequisites and get the exact exe path:

```powershell
.\.claude\skills\fieldworks-winapp\scripts\Preflight-WinFormsMcp.ps1            # Debug
.\.claude\skills\fieldworks-winapp\scripts\Preflight-WinFormsMcp.ps1 -PrewarmPackage
```

It checks node/npx, that `@fnrhombus/winforms-mcp` resolves, that `Output/Debug/FieldWorks.exe` exists
(else points at `build.ps1`), ICU data, and that `.mcp.json` registers the server. The MCP **server is
launched by the client**, not by this script — the script only verifies the launch will succeed.

## First launch

`winforms_launch_app` with `path = <repo>\Output\Debug\FieldWorks.exe` (the preflight prints it). Then
follow `navigation/launch-or-attach.md`. For the WinForms↔Avalonia parity flow see
`navigation/winforms-avalonia-parity.md`.

## Troubleshooting

- **No `winforms_*` tools after adding `.mcp.json`** → you did not reconnect, or declined the approval
  prompt. Reconnect; run `/mcp`; approve the project server.
- **Server shows "failed" in `claude mcp list`** → run the preflight; most often node/npx is not on the
  Claude Code process PATH, or the bare-`npx` form was used instead of `cmd /c npx`.
- **FieldWorks launches but has no project** → restore/open a project per `navigation/project-loading.md`
  (parity work needs a project with parsed wordform analyses for the Words ▸ Analyses interlinear).
- **Screenshots capture the wrong window** → keep `HEADLESS=true` (WinForms MCP) and pass the FieldWorks
  `pid`; for the visible-desktop WinApp MCP fallback, bring FieldWorks forward (see `mcp-selection.md`).
