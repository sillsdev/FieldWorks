# Model Context Protocol helpers

FieldWorks ships a small workspace `.vscode/mcp.json` so Model Context Protocol clients can spin up three
servers automatically:

- **GitHub server** via the hosted GitHub MCP endpoint (`https://api.githubcopilot.com/mcp/`).
- **Serena server** for accelerated navigation of this large mono-repo.
- **WinForms MCP server** for background UIA2 automation of the FieldWorks WinForms desktop app.

## Prerequisites

| Component      | Purpose                                   | Install guidance                                      |
| -------------- | ----------------------------------------- | ----------------------------------------------------- |
| Serena CLI     | Provides Serena search/navigation        | `pipx install serena-cli` or `uv tool install serena` |
| `uvx`          | Launches Serena from workspace `.vscode/mcp.json` | https://github.com/astral-sh/uv                       |
| Node.js/npm    | Launches `@fnrhombus/winforms-mcp` with `npx` | https://nodejs.org                                    |

> **Note**: Serena **auto-downloads** its language servers on first use:
> - **C# (`csharp`)**: Microsoft.CodeAnalysis.LanguageServer (Roslyn) from Azure NuGet + .NET 9 runtime
> - **C++ (`cpp`)**: clangd 19.1.2 from GitHub releases (Windows/Mac); Linux requires `apt install clangd`
>
> No manual language server installation needed!

Authentication:

- GitHub MCP uses OAuth through your normal VS Code GitHub/Copilot sign-in.
- `SERENA_API_KEY` remains optional and is only needed when your Serena deployment requires it.
- WinForms MCP is launched locally with `npx -y @fnrhombus/winforms-mcp`; no service login is required.

## How it works

1. `.vscode/mcp.json` defines a hosted GitHub MCP server plus local Serena and WinForms MCP stdio servers.
2. GitHub MCP uses VS Code OAuth authentication for repository operations.
3. Serena starts from `uvx --from git+https://github.com/oraios/serena serena start-mcp-server --context ide --project ${workspaceFolder}`.
4. WinForms MCP starts from `npx -y @fnrhombus/winforms-mcp` with `HEADLESS=true`, `TFM=net48`, and `TELEMETRY_OPTOUT=true`.
5. In chat, use tool sets / tool picker to keep active tool counts low and focused.

## WinForms MCP for FieldWorks UI automation

FieldWorks is currently a WinForms desktop app, so prefer WinForms MCP for most runtime UI walks and screenshots. The workspace config enables headless mode so `winforms_launch_app` starts FieldWorks on a hidden desktop instead of stealing focus from the developer's visible desktop.

Tool names in this section use the WinForms MCP `winforms_*` namespace as shown in the client tool picker. If a client shortens a name to the underlying action, map it back to the same WinForms MCP command.

Use WinForms MCP when:

- launching a fresh FieldWorks instance for manual verification;
- capturing screenshots without bringing FieldWorks to the foreground;
- inspecting standard WinForms controls, menus, and dialogs by AutomationId, name, class, or control type;
- using `winforms_render_form` for a `.Designer.cs` preview.

Use the UIA3 WinApp MCP tools when:

- WinForms MCP is unavailable in the active client;
- you need to attach to a user-visible process and inspect the desktop/window list;
- a route needs UIA3-specific behavior or a control is not exposed correctly through WinForms MCP;
- troubleshooting focus, foreground-window behavior, or non-WinForms surfaces.

Headless WinForms MCP limitations: `winforms_send_keys`, drag/drop, and double-click paths use input simulation and require the visible desktop. Prefer `winforms_type_text`, `winforms_set_value`, `winforms_select_item`, `winforms_click_element`, and `winforms_click_menu_item` for headless work.

## Running the servers manually

If you want to test outside an MCP-aware editor:

```powershell
# GitHub server is hosted; test by adding it to VS Code MCP and invoking a GitHub tool.

# Serena server
uvx --from git+https://github.com/oraios/serena serena start-mcp-server --context ide --project .

# WinForms MCP server
$env:HEADLESS = "true"
$env:TFM = "net48"
$env:TELEMETRY_OPTOUT = "true"
npx -y @fnrhombus/winforms-mcp
```

The local stdio processes run until you press `Ctrl+C`. When invoked through an MCP host, they stop
when the client disconnects.

## Multiple Worktrees and Serena Conflicts

When working with multiple git worktrees (e.g., `fw-worktrees/agent-1`, `agent-2`, etc.),
each worktree contains its own `.serena/project.yml` file (shared via git). This can cause
issues when Serena auto-discovers projects:

### Symptoms
- `get_current_config` shows multiple FieldWorks worktrees registered, or a project name that does not match the current folder
- Language server errors that don't match your current worktree
- Serena loads projects from worktrees you're not currently working in

### Cause
VS Code's user-level MCP config (`%APPDATA%\Code\User\mcp.json`) may have a Serena
server that auto-discovers projects by scanning for `.serena` folders. Combined with
workspace-level `mcp.json`, this creates duplicate project registrations.

This repo's shared `.serena/project.yml` intentionally leaves `project_name` unset so
Serena falls back to the worktree folder name by default. That keeps worktrees easier
to distinguish when multiple registrations exist.

### Solution
**Use only workspace-level Serena, and keep project names local to the worktree**

Remove or disable the Serena entry from your user-level MCP config:
```powershell
# View current user MCP config
code "$env:APPDATA\Code\User\mcp.json"
```
Remove the `"oraios/serena"` entry. The workspace `.vscode/mcp.json` provides Serena
with explicit project targeting.

If you want a friendlier project name than the folder name, set it in
`.serena/project.local.yml` for that worktree instead of committing a shared
`project_name` in `.serena/project.yml`.

## Best-practice profile for this repo

- Keep repo-level MCP servers focused: `github`, `serena`, and `winforms-mcp`.
- Keep workflow/task conventions in skills/prompt files, not additional MCP servers.
- Enable WinForms MCP only for UI automation tasks via the tool picker when the client supports per-task tool selection.
- If tool list feels noisy, reset cached tools with **MCP: Reset Cached Tools**.

## Worktree best practices

- Open one VS Code window per worktree; let that window use its own workspace `.vscode/mcp.json`.
- Keep only one Serena server definition active (workspace-level), and remove user-level Serena.
- Keep Serena pinned to the active workspace via `--project ${workspaceFolder}` (already configured).
- If you want an explicit Serena project name, set it in `.serena/project.local.yml`; do not commit a shared `project_name` in `.serena/project.yml`.
- After switching worktrees, run **MCP: Reset Cached Tools** if tool lists or capabilities look stale.
- No extra GitHub MCP worktree settings are required beyond OAuth sign-in.

## Troubleshooting

- **GitHub tools fail with auth errors** – sign out/in of GitHub in VS Code and restart MCP servers.
- **`uvx` was not found on PATH** – install `uv` and reopen your shell.
- **`npx` was not found on PATH** – install Node.js/npm and reopen VS Code.
- **WinForms MCP package cannot be resolved** – run `npm view @fnrhombus/winforms-mcp version` to check npm registry access.
- **Headless text input does nothing** – use WinForms MCP `winforms_type_text` or `winforms_set_value`, not `winforms_send_keys`.
- **Headless screenshot is blank or stale** – pass the FieldWorks process ID to `winforms_take_screenshot` when the client exposes that parameter.
- **Unable to start Serena MCP** – ensure `uvx` can reach GitHub and run:
  `uvx --from git+https://github.com/oraios/serena serena start-mcp-server --help`
- **Language server download fails (network error)** – Serena auto-downloads C# (Roslyn) and C++ (clangd)
  language servers on first use. Check network connectivity to Azure NuGet and GitHub releases.
  The download is cached, so subsequent starts are fast.
- **Linux: clangd not found** – on Linux, install clangd manually: `sudo apt-get install clangd`
- **"Language server manager is not initialized"** – restart VS Code; Serena may still be downloading
  language servers on first startup (can take 1-2 minutes for ~250MB of binaries).
