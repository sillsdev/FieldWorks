# Task Completion Checklist

1. Run the relevant MSBuild traversal (`.uild.ps1` or equivalent VS Code task) plus any targeted test suites touched by the change.
2. Execute whitespace and commit message parity checks via `./Build/Agent/check-and-fix-whitespace.ps1` and `./Build/Agent/commit-messages.ps1` (or the combined "local CI check" task) to match CI expectations.
3. If installer/config files were edited, perform the WiX validation steps from `FLExInstaller` instructions before submitting.
4. Update COPILOT documentation per `.github/update-copilot-summaries.md` when code behavior or folder contracts change, keeping `last-reviewed-tree` current.
5. Ensure git status is clean aside from intentional changes, confirm `.editorconfig` formatting, then prepare concise commits (<72-char subject) referencing any linked specs/issues.