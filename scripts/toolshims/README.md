# Toolshims: purpose and usage

This folder contains small, repo-local shims to make developer workflows and assistant
invocations more reliable across different machines. They are intentionally conservative
and non-invasive: they do not modify system settings and only affect shells started
from this workspace (VS Code integrated terminals will get this folder at the front of
`PATH` because of `.vscode/settings.json`).

## Files

- **`py.cmd`, `py.ps1`** — a shim and PowerShell wrapper for Python. The wrapper:
  - prefers `python` (CPython) if available, otherwise `py`.
  - supports a simple heredoc emulation when an argument like `<<MARKER` is passed; it will
    read lines from stdin until a line exactly matching `MARKER` and send that content to
    `python -` (stdin).

- **`pwsh.cmd`** — prefers PowerShell Core (`pwsh`) and falls back to Windows PowerShell.

## Notes on heredocs

`python - <<'PY'` is a shell feature commonly available in bash. On Windows that exact
syntax is not native to `cmd.exe` or PowerShell. The `py` wrapper offers a pragmatic
fallback: if your command includes an argument like `<<MARKER`, the wrapper will read
lines from the terminal until it sees `MARKER` on its own line and forward those lines
to `python -`.

## Examples

Run a small inline script in the integrated terminal (paste the lines and finish with `PY`):

```cmd
python <<PY
print('hello from heredoc')
PY
```

Standard invocation (prefers CPython):

```cmd
py -m pip install --user requests
```

## Why this approach?

- It avoids changing global PATH or system configuration for other projects.
- It gives predictable behavior for automated tools and assistants that assume commands
  like `py` or `pwsh` exist.
