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

## Examples
```powershell
# Good: validate tools before running
if (-not (Get-Command "git" -ErrorAction SilentlyContinue)) { throw "git not found" }
Get-Content README.md | Select-Object -First 1
```
