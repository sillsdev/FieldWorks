---
applyTo: "**/*"
name: "terminal.instructions"
description: "Terminal command patterns for auto-approval in FieldWorks"
---

# Terminal Commands

Commands with pipes (`|`), `&&`, or `2>&1` require manual approval. Use `scripts/Agent/` wrappers instead.

**MCP-first:** When the `ps-tools` MCP server is running, prefer MCP tools (`Git-Search`, `Read-FileContent`, `Invoke-AgentTask`, `build`, `test`, Copilot tools) instead of direct terminal commands. Use wrappers only when MCP is unavailable.

## Transformations

| ❌ Blocked | ✅ Use Instead |
|-----------|----------------|
| `git log \| head -20` | `.\scripts\Agent\Git-Search.ps1 -Action log -HeadLines 20` |
| `git show ref:file \| head -50` | `.\scripts\Agent\Git-Search.ps1 -Action show -Ref "ref" -Path "file" -HeadLines 50` |
| `git diff ref -- path` | `.\scripts\Agent\Git-Search.ps1 -Action diff -Ref "ref" -Path "path"` |
| `Get-Content file \| Select -First N` | `.\scripts\Agent\Read-FileContent.ps1 -Path "file" -HeadLines N` |
| `Get-Content file \| Select-String pat` | `.\scripts\Agent\Read-FileContent.ps1 -Path "file" -Pattern "pat"` |

## Scripts

| Script | Purpose |
|--------|---------|
| `Git-Search.ps1` | git show/diff/log/grep/blame |
| `Read-FileContent.ps1` | File reading with filtering |

**Build/test**: Run `.\build.ps1` or `.\test.ps1` directly—they're auto-approvable.