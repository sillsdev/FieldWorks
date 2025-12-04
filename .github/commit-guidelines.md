# Commit message guidelines (CI-enforced)

These align with the gitlint rules run in CI.

## Subject (first line)

- Max 72 characters.
- Use imperative mood when reasonable (e.g., "Fix crash on startup").
- No trailing punctuation (e.g., don't end with a period).
- No tabs, no leading/trailing whitespace.

## Body (optional)

- Blank line after the subject.
- Wrap lines at 80 characters.
- Explain what and why over how; link issues like "Fixes #1234" when applicable.
- No hard tabs, no trailing whitespace.

## Helpful commands (Windows PowerShell)

```powershell
python -m pip install --upgrade gitlint
git fetch origin
gitlint --ignore body-is-missing --commits origin/<base>..
```

Replace `<base>` with your target branch (e.g., `release/9.3`, `develop`).

## Common examples

- Good: "Refactor XCore event dispatch to avoid deadlock"
- Good: "Fix: avoid trailing whitespace in generated XSLT layouts"
- Avoid: "Fixes stuff." (too vague, trailing period)
- Avoid: "WIP: temp" (unclear intent, typically avoided in shared history)
