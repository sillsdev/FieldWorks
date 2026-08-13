---
name: commit-messages
description: MUST use before writing any git commit message in this repository. Covers the gitlint rules CI enforces (title/body length, blank line, trailing punctuation) and how to write a compliant message on the first try.
---

# Commit Messages

CI (`.github/workflows/CommitMessage.yml`) runs `gitlint` against every
commit on a PR and fails the build on any violation. `.gitlint` in the repo
root only exempts `Agent-Logs-Url:` and the Copilot Autofix co-author
trailer from the body-length rule -- every other line, in every commit,
is checked. There is no leniency for "just the summary" or "just this once."

## The rules that actually fire in practice

| Rule | Limit | Notes |
|---|---|---|
| Title length (T1) | 72 characters | Counts the whole subject line, including the `type:` prefix. |
| Body line length (B1) | 80 characters | Per line, not per paragraph. A heredoc does NOT auto-wrap -- you must break lines yourself. |
| Blank line after title (B4) | required | One empty line between the subject and the body. |
| Title trailing punctuation (T3) | none | No period, no colon, at the end of the subject line. |
| Trailing whitespace (T2, B2) | none | Watch for trailing spaces left by hand-wrapped lines. |
| Hard tabs (B3) | none | Use spaces in the body. |

## Writing a compliant message the first time

- Count the title before committing to it. "docs: remove doc-pointers and
  provenance narration from PR #964" is 63 characters; adding a scope like
  "and cleanup notes" on top of an already-full title is how T1 fails.
- Wrap body prose by hand at well under 80 characters per line -- a heredoc
  passed to `git commit -F -`/`-m` reproduces exactly the line breaks you
  typed, it does not reflow them. Aim for ~70 so a `Co-Authored-By:` trailer
  or an indented list item added later doesn't push a line over.
- Prefer several short lines over one long one, and several short
  paragraphs over one dense one -- a commit message is read in a `git log`
  pane, not a text editor with wrapping.

## Verify before considering a commit done

Run the same check CI runs, scoped to the current branch:

```powershell
gitlint --ignore body-is-missing --commits main..HEAD
```

(Substitute the actual base branch if not `main`.) A clean run prints
nothing and exits 0. `Build/Agent/commit-messages.ps1` (or `.sh`) wraps this
with the same base-ref auto-detection CI uses, if you want the base
resolved for you instead of naming it.

## Fixing a violation after the fact

If a commit already landed non-compliant and hasn't been pushed to a shared
branch, reword it without a full interactive rebase:

```bash
git rebase <target>^ --exec "if [ \"\$(git rev-parse HEAD)\" = \"<target-full-sha>\" ]; then git commit --amend -F <message-file>; fi"
```

This replays history non-interactively (no editor, no `-i` prompt) and
amends only the one commit whose SHA matches, at the point in the replay
where it is HEAD. Never do this on a branch that has already been pushed
and could have a PR or other work based on it -- check
`git rev-parse --abbrev-ref --symbolic-full-name @{u}` and
`git ls-remote --heads origin <branch>` first, and confirm with the user if
either shows the branch is shared.
