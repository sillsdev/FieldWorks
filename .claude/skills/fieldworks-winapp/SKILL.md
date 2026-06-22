---
name: fieldworks-winapp
description: >
  Control and document the FieldWorks desktop application with WinForms MCP or WinApp MCP.
  Use this skill whenever a task requires launching FieldWorks, restoring or
  opening a FieldWorks project, walking WinForms UI flows, collecting manual
  screenshots, reproducing UI bugs, or verifying a fix inside the live FLEx
  application.
license: MIT
compatibility: >
  Requires a Windows/x64 FieldWorks workspace. Prefer WinForms MCP
  for hidden-desktop UIA2 automation when available; use WinApp MCP as the UIA3
  fallback and for visible desktop/window diagnostics.
metadata:
  author: FieldWorks team
  version: "1.0"
---

# FieldWorks WinForms/WinApp Automation

Use this skill to launch, inspect, navigate, and capture evidence from the
FieldWorks Language Explorer desktop app through WinForms MCP or WinApp MCP.
Keep the workflow grounded in the live UI tree: inspect first, interact second,
capture evidence after the target state is visible.

This skill is intentionally organized as a small index plus route-specific
navigation files. Read only the route files needed for the task.

## Setup First (if `winforms_*` tools are missing)

If `ToolSearch "winforms"` finds no tools, the MCP server is not registered for
**Claude Code** — most often because it is only in `.vscode/mcp.json` (VS Code's
file), not the repo-root `.mcp.json` (Claude Code's file). Read
`references/mcp-setup.md`, then:

1. Ensure repo-root `.mcp.json` registers `winforms-mcp` (committed here; the
   Windows-robust `cmd /c npx` form).
2. Run `scripts/Preflight-WinFormsMcp.ps1` to confirm node/npx, the package, and
   a built `Output/Debug/FieldWorks.exe`.
3. **Reconnect/restart Claude Code** (MCP servers load at session start) and
   **approve** the project server when prompted. Confirm with `claude mcp list`
   and `ToolSearch "winforms"`.

The MCP **client** launches the server — no script starts it; the preflight only
verifies that launch will succeed.

## Core Rules

- Prefer WinForms MCP UIA2 tools for fresh FieldWorks runtime automation because
  FieldWorks is currently WinForms and the workspace config runs WinForms MCP in
  hidden-desktop mode.
- Use WinApp MCP UIA3 tools when WinForms MCP is unavailable, when a task needs
  visible desktop/window diagnostics, or when UIA2 cannot see the target
  surface.
- Prefer UI Automation IDs and names over coordinates. Use coordinates only
  after snapshots and element searches fail.
- Always inspect the current tree with `winforms_get_element_tree`,
  `mcp_winapp_get_snapshot`, or targeted element search before assuming a
  dialog structure.
- Treat FieldWorks UI automation as stateful. After opening menus, dialogs, or
  popups, re-query the snapshot or element list.
- Capture screenshots into a task-specific evidence folder before closing the
  app or dialog.
- Close evidence-only dialogs with `Cancel` or `Escape` unless the user asked
  to change project data.
- Do not globally register COM or add registry hacks. FieldWorks uses reg-free
  COM from the build output.

## MCP Selection

- Default to WinForms MCP for launch/test/screenshot flows. Use
  `winforms_launch_app`, `winforms_wait_for_element`,
  `winforms_get_element_tree`, `winforms_find_element`,
  `winforms_click_element`, `winforms_type_text`, `winforms_set_value`,
  `winforms_select_item`, `winforms_click_menu_item`, and
  `winforms_take_screenshot`.
- Use WinForms MCP headless-safe operations only. Avoid `winforms_send_keys`,
  drag/drop, and double-clicks on hidden-desktop processes.
- Use WinApp MCP for visible desktop/window diagnostics, UIA3 comparison,
  fallback screenshots, or controls that WinForms MCP cannot operate.
- Read `references/mcp-selection.md` before changing a route from one MCP driver
  to the other.

## How This Skill Is Organized

- `SKILL.md`: trigger metadata, global safety rules, and route index.
- `navigation/*.md`: one goal-oriented navigation path per file. These are the
  FieldWorks equivalent of Page Objects: each file owns the menu path,
  automation IDs, verification cues, and safe exit path for one user-facing
  destination or workflow.
- `references/how-to-update.md`: rules for adding or revising navigation paths.
- `references/mcp-selection.md`: rules for choosing WinForms MCP or WinApp MCP.
- `references/mcp-setup.md`: enabling the `winforms_*` tools for Claude Code (`.mcp.json`, reconnect).
- `references/research.md`: source-backed rationale for this structure.
- `scripts/Preflight-WinFormsMcp.ps1`: pre-session check (node/npx, package, build, `.mcp.json`).

When a task names a destination, read the matching navigation file. When a task
discovers a better route or a fragile selector, read and update
`references/how-to-update.md` before changing the route file.

## Navigation Path Index

- Launch or attach to FieldWorks: `navigation/launch-or-attach.md`
- Confirm or restore a sample project: `navigation/project-loading.md`
- Writing System Properties > Font tab: `navigation/writing-system-font-options.md`
- Styles dialog > Font tab: `navigation/styles-font-tab.md`
- Manual screenshot evidence: `navigation/screenshot-evidence.md`
- WinForms ↔ Avalonia parity capture: `navigation/winforms-avalonia-parity.md`
- Enable the MCP tools for Claude Code: `references/mcp-setup.md`
- MCP driver selection: `references/mcp-selection.md`

## Screenshot Evidence

For evidence tasks, read `navigation/screenshot-evidence.md`. Prefer committed
OpenSpec evidence only when the screenshot should be part of review history;
otherwise use `Output/ManualEvidence/<ticket-or-change-id>/`.

For target detection, descriptive filenames, inline review, annotation, and
retake quality gates, also use `../smart-screenshot-capture/SKILL.md`.

## How To Update This Skill

Keep improving this skill as you explore FieldWorks and discover the most
efficient ways to do things. When you find a reliable automation ID, keyboard
route, menu path, restore flow, modal-dialog workaround, or screenshot trick,
update the relevant route file in the same change set or propose the update to
the user. Prefer short, verified notes over broad guesses.

Use `references/how-to-update.md` for the exact update checklist. In short:

- add one navigation file per distinct destination or workflow;
- keep each file task-focused and action-oriented;
- record stable automation IDs, entry state, verification cues, and exit path;
- move shared rules back to this index only when at least two route files need
  the same guidance;
- remove or revise stale routes when WinApp snapshots prove they changed.
