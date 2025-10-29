## Summary
<!-- Briefly describe what changed and why. Link issues if applicable (e.g., Fixes #1234). -->

## CI-ready checklist

- [ ] Commit messages follow `.github/commit-guidelines.md` (subject ≤ 72 chars, no trailing punctuation; if body present, blank line then ≤ 80-char lines).
- [ ] No whitespace warnings locally:
  ```powershell
  git fetch origin
  git log --check --pretty=format:"---% h% s" origin/<base>..
  git diff --check --cached
  ```
- [ ] Builds/tests pass locally (or I've run the CI-style build via Bash script or MSBuild).

## Notes for reviewers (optional)
<!-- Risks, roll-out, docs/tests touched, special validation steps, etc. -->
