---
applyTo: "**/*.cs, **/*.cpp, **/*.h"
name: "coding-standard.instructions"
description: "Coding standards for FieldWorks development"
---

# Coding Standards

## Purpose & Scope

This document defines the coding standards for FieldWorks. These standards ensure consistency, readability, and maintainability across the codebase.

## Commit Messages

### Format

Commit messages should follow this structure:

```
<subject line - max 62 characters>

<body - lines < 75 characters>
```

### Subject Line Rules

- Maximum 62 characters (75 if single line)
- Do NOT end with a period
- Use imperative mood: "Add feature" not "Added feature" or "Adds feature"
- Be concise but descriptive

### Body Rules

- Separate from subject with a blank line
- Keep lines under 75 characters
- Explain **what** and **why**, not how
- Include issue references (e.g., "Fixes LT-12345")

### Good Examples

```
Add export dialog for lexicon entries

Implement a new dialog that allows users to export selected
lexicon entries to various formats including LIFT and CSV.

Fixes LT-12345
```

```
Fix crash when opening projects with missing files

The application crashed when a project referenced files that
had been moved or deleted. Now shows a warning dialog instead.
```

### Bad Examples

```
Fixed the bug.              # Too vague, past tense, period
```

```
Adding some changes to the export functionality that were requested by users  # Too long, present participle
```

## Whitespace

### Indentation

- **Use tabs, not spaces**
- Tab width: 4 characters
- Configure your editor to insert tabs, not spaces

### Trailing Whitespace

- **No trailing whitespace** on any line
- Files should end with a single newline

### Configuration

These rules are enforced in `.editorconfig`. Ensure your editor respects EditorConfig files.

## Enforcement

- **Commit message format** is checked by gitlint during CI
- **Whitespace rules** are checked by `git log --check` during CI
- **EditorConfig** is read by Visual Studio and VS Code

Run the CI parity checks locally before committing:
```powershell
# Check whitespace
git diff --check

# Lint commit messages
gitlint --commits origin/release/9.3..HEAD
```

## Language-Specific Standards

For detailed language-specific guidelines, see:

- [managed.instructions.md](managed.instructions.md) - C# specific guidelines
- [native.instructions.md](native.instructions.md) - C++/C++CLI guidelines
- [csharp.instructions.md](csharp.instructions.md) - Additional C# patterns

## External References

- [Palaso Coding Standards](https://docs.google.com/document/d/1t4QVHWwGnrUi036lOXM-hnHVn15BbJkuGVKGLnbo4qk/edit#heading=h.oqp1hgsjndtl) - Additional coding conventions (FieldWorks works closely with Palaso code)

## Quick Reference

| Rule | Requirement |
|------|-------------|
| Indentation | Tabs (4 spaces wide) |
| Trailing whitespace | None |
| Line endings | LF (Unix-style) |
| Commit subject | ≤62 chars, imperative, no period |
| Commit body | Lines ≤75 chars, explain why |
| File encoding | UTF-8 |
