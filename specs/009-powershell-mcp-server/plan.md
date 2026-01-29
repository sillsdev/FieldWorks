# Implementation Plan: PowerShell MCP Server

**Branch**: `009-powershell-mcp-server` | **Date**: 2025-12-08 | **Spec**: specs/009-powershell-mcp-server/spec.md
**Input**: Feature specification from `/specs/009-powershell-mcp-server/spec.md`

## Summary

Build a Python-based MCP server that dynamically surfaces `scripts/Agent/*.ps1` and key root scripts (`build.ps1`, `test.ps1`) as MCP tools. Error handling must return structured outputs (`stdout`, `stderr`, `exitCode`) on non-zero exits. Update `terminal.instructions.md` and workspace MCP config so agents default to these tools for safe, auto-approved operations.

## Technical Context

**Language/Version**: Python 3.11; PowerShell 5.1/7 for script execution
**Primary Dependencies**: MCP Python SDK (modelcontext-protocol) or lightweight JSON-RPC implementation; use existing PowerShell scripts under `scripts/Agent/`
**Storage**: N/A (no persistent data)
**Testing**: pytest-based unit/integration for server routing; smoke calls to representative `.ps1` wrappers
**Target Platform**: Windows (worktrees/agent containers included)
**Project Type**: CLI/service (local MCP server process)
**Performance Goals**: Low latency for single-command invocations; must avoid blocking on long-running PS commands by streaming or timeouts where appropriate
**Constraints**: Must preserve container/host routing logic encapsulated in existing scripts; no additional elevation/registry changes
**Scale/Scope**: Local developer use (single-user, single-machine MCP server)
**Defaults**: Dynamic tool discovery with allowlist/denylist, default timeout 120s, default output cap 1MB, working directory = repo root; all overrideable via config file.

## Constitution Check

- Data integrity: No schema or data migration; N/A but ensure no destructive defaults.
- Tests required for risk areas: Provide automated tests for tool dispatch, argument mapping, and error reporting (FR-009).
- I18n/rendering: Not applicable (no UI), but error text should pass through without truncation.
- Documentation fidelity: Update `terminal.instructions.md` and any new MCP config docs to match behavior.
- Licensing: Verify any new Python dependency licenses are compatible (LGPL-safe).

## Project Structure

### Documentation (this feature)

```text
specs/009-powershell-mcp-server/
├── plan.md          # This file
├── research.md      # Phase 0 (to be created)
├── data-model.md    # Phase 1 (to be created)
├── quickstart.md    # Phase 1 (to be created)
├── contracts/       # Phase 1 (to be created)
└── tasks.md         # Phase 2 (/speckit.tasks, not in this phase)
```

### Source Code (repository root)

```text
scripts/
├── Agent/                  # Existing PS wrappers (tools source)
├── mcp/                    # New: Python MCP server package
│   ├── __init__.py
│   ├── server.py           # MCP server entry, JSON-RPC handling
│   ├── ps_tools.py         # Tool discovery + invocation helpers
│   └── config.py           # Tool allowlist/denylist, timeouts
└── toolshims/              # Existing PATH shims (unchanged)

mcp.json (or .vscode settings)   # Workspace MCP client config pointing to server
.github/instructions/terminal.instructions.md  # Updated guidance to prefer MCP tools
```

**Structure Decision**: Single local MCP service under `scripts/mcp/` that discovers tools from `scripts/Agent/` and selected root scripts.

## Complexity Tracking

No constitution violations anticipated; complexity tracking table not required.
