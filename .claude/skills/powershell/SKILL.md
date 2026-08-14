---
name: powershell
description: >
  PowerShell best practices for scripts used in FieldWorks (dev scripts & CI helpers).
  Use when writing or modifying PowerShell scripts in scripts/ or Build/Agent/.
allowed-tools: "Read,PowerShell"
version: "1.0.0"
---

# PowerShell Development Skill

Conventions and safety patterns for PowerShell scripts in `scripts/` and CI.

## Style and Linting

- Scripts in `Build/Agent/`, and anything else reached from `build.ps1` or `test.ps1`,
  must run under **both** Windows PowerShell 5.1 and PowerShell 7. CI executes the
  build and test steps under 5.1, so 6+-only syntax that parses cleanly on 7 can
  still fail or silently misbehave there: the backtick u{} escape resolves to
  literal text under 5.1 instead of a code point, and `-Encoding utf8BOM`/`utf8NoBOM`
  throw a parameter-binding error. Run `Build/Agent/powershell-compat.ps1` to check,
  and prefer syntax both engines share over anything PowerShell Core adds.
- Use `Set-StrictMode -Version Latest`.
- Use `Write-Host` sparingly; prefer `Write-Output` and `Write-Error` for correct streams.
- Use `-ErrorAction Stop` in helper functions when errors should abort execution.
- **No Unicode icons or emojis** in output messages (e.g., `✓`, `✗`, `⚠`, `🔧`). Use plain ASCII text like `[OK]`, `[FAIL]`, `[WARN]`, `ERROR:` instead. Unicode causes encoding issues in CI logs.

## Traps that produce a wrong answer instead of an error

The first one below is the dangerous one: it yields a plausible result with no
warning, so nothing prompts you to look. The others fail loudly, but only under
`Set-StrictMode -Version Latest`, which this repo requires.

### An operator after a bare function call binds as an argument

`-replace`, `-split`, `-match`, and friends written after an unparenthesized
function call are parsed as further *arguments* to that call, not applied to its
result. The operator is silently ignored.

```powershell
# BAD: -replace and '\s+' become arguments 2 and 3 of Norm; nothing is replaced
$key = Norm ($text) -replace '\s+', ''

# GOOD: parenthesize the call, then apply the operator to its result
$key = (Norm $text) -replace '\s+', ''
```

### Measure-Object -Sum over an empty collection returns $null

Reading `.Sum` (or `.Maximum`, `.Average`) off that result then throws
"The property 'Sum' cannot be found on this object" -- which surfaces far from
the empty input that caused it.

```powershell
# BAD: throws whenever $items happens to be empty
$total = ($items | Measure-Object -Property Length -Sum).Sum

# GOOD
$total = 0
foreach ($item in $items) { $total += $item.Length }
```

### Returning a collection from a function unrolls it

`return $list` enumerates into the pipeline: an empty collection becomes `$null`
and a single element becomes a scalar, so the caller's `.Count` throws. Prefix
with a comma to return the collection itself.

```powershell
# BAD: (Get-Ids).Count throws when the list is empty
function Get-Ids { $ids = New-Object System.Collections.Generic.List[int]; return $ids }

# GOOD
function Get-Ids { $ids = New-Object System.Collections.Generic.List[int]; return ,$ids }
```

### The stop-parsing token consumes the rest of the line

`--%` passes everything after it to the native command verbatim, including any
closing bracket you meant PowerShell to read. It cannot appear inside `@(...)`,
`$(...)`, or any other expression that has to be closed.

```powershell
# BAD: --% swallows the closing paren; parse error, not a runtime error
$msg = @(git --% log -1 --format=%B)

# GOOD: keep --% on a statement of its own, or drop it when it is not needed
$msg = @(git log -1 --pretty=%B)
```

## Security

- Avoid embedding secrets in scripts; read from env vars and prefer platform secret stores.
- Do not commit credential tokens in any scripts or docs.

## Testing and Execution

- Use `pwsh -NoProfile -ExecutionPolicy Bypass -File` in CI wrappers.
- Add small smoke test steps to validate paths and required tools are present.

## Auto-Approval Patterns

**CRITICAL**: Agent terminal security blocks complex commands. The following require manual approval:
- Pipes (`|`)
- Semicolons (`;`) or `&&`
- Redirection (`2>&1`)

**ALWAYS use `scripts/Agent/` wrapper scripts for these operations.** Do not attempt raw commands.

See [terminal.instructions.md](../../instructions/terminal.instructions.md) for the complete transformation table.

## Examples

```powershell
# Good: simple commands auto-approve
.\build.ps1
git status

# Good: use wrapper scripts (ALWAYS for git with pipes)
.\scripts\Agent\Git-Search.ps1 -Action show -Ref "release/9.3" -Path "file.h" -HeadLines 20
.\scripts\Agent\Git-Search.ps1 -Action log -HeadLines 20
.\scripts\Agent\Read-FileContent.ps1 -Path "file.cs" -HeadLines 50 -LineNumbers

# BAD: these require manual approval - NEVER USE
# git log --oneline | head -20
# Get-Content file.cs | Select-Object -First 50
```
