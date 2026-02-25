---
applyTo: "**/*"
name: "commit-messages.instructions"
description: "Commit message formatting rules enforced by CI (gitlint)"
---

# Commit Message Format (CI-enforced)

## Purpose & Scope
Ensure every commit message passes the gitlint checks run by CI
(`Build/Agent/commit-messages.ps1`). Violations block merges.

## Rules

### Subject line (first line)
- **Max 72 characters.** This is a hard limit.
- Use imperative mood ("Fix crash", not "Fixed crash").
- No trailing punctuation (no period, exclamation, etc.).
- No tabs, no leading/trailing whitespace.

### Body (optional but encouraged for non-trivial changes)
- **Blank line** between subject and body.
- **Wrap every line at 80 characters.** This is a hard limit.
- Explain *what* and *why*, not *how*.
- Reference issues: "Fixes LT-12345" or "See LT-12345".
- No hard tabs, no trailing whitespace.

## Validation
```powershell
.\Build\Agent\commit-messages.ps1
```

## Examples

Good:
```
Fix LT-22427: honor Show Hidden Fields for entry details

Unify DataTree show-hidden key resolution across
ShowObject, menu display, and property change handling.
Use currentContentControl when present; fall back to
lexiconEdit for LexEntry.
```

Bad (body lines exceed 80 chars):
```
Fix LT-22427: honor Show Hidden Fields key for Lexicon entry details

- unify DataTree show-hidden key resolution across ShowObject, menu display, and property change handling
```

## Reference
- Human-facing guide: `.github/commit-guidelines.md`
- CI script: `Build/Agent/commit-messages.ps1`
