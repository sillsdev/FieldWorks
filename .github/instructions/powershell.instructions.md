---
applyTo: "**/*.ps1"
name: "powershell.instructions"
description: "PowerShell best practices for scripts used in FieldWorks (dev scripts & CI helpers)"
---

# PowerShell development and script usage

## Purpose & Scope
- Provide conventions and safety patterns for PowerShell scripts in `scripts/` and CI.

## Style and Linting
- Use `pwsh`/PowerShell Core syntax where possible and `Set-StrictMode -Version Latest`.
- Use `Write-Host` sparingly; prefer `Write-Output` and `Write-Error` for correct streams.
- Use `-ErrorAction Stop` in helper functions when errors should abort execution.

## Security
- Avoid embedding secrets in scripts; read from env vars and prefer platform secret stores.
- Do not commit credential tokens in any scripts or docs.

## Testing and Execution
- Use `pwsh -NoProfile -ExecutionPolicy Bypass -File` in CI wrappers.
- Add small smoke test steps to validate paths and required tools are present.

## Auto-Approval Patterns
Copilot's terminal security blocks complex commands. For auto-approval, avoid:
- Pipes (`|`)
- Semicolons (`;`) or `&&`
- Redirection (`2>&1`)
- Nested quotes in `docker exec`

**For complex operations, check `scripts/Agent/` for wrapper scripts** that encapsulate the logic in auto-approvable form. Use `Get-Help .\scripts\Agent\<script>.ps1` to see usage.

## Examples
```powershell
# Good: simple commands auto-approve
.\build.ps1
git status

# Good: use script parameters instead of pipes
.\build.ps1 -TailLines 150

# Good: use wrapper scripts for complex operations
.\scripts\Agent\Git-Search.ps1 -Action show -Ref "release/9.3" -Path "file.h" -HeadLines 20
```
