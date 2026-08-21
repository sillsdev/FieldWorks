---
name: jira-bugfix
description: >
  End-to-end JIRA bugfix workflow: fetch issue, assign, branch,
  TDD, fix, test, update AGENTS.md, commit, PR, and update JIRA.
  Use when the user says "fix LT-XXXXX" or references a JIRA bug
  to resolve.
license: MIT
compatibility: Requires atlassian-skills, atlassian-readonly-skills.
metadata:
  author: FieldWorks team
  version: "2.0"
---

# JIRA Bugfix

Fixing a bug sourced from an LT ticket, from triage to PR.

Tell the user the steps before starting, then work them in order. Pause at the
four decision points below; everything else proceeds.

## Steps

| # | Step | Notes |
| --- | --- | --- |
| 0 | Fetch the issue | Summary, status, components, comments. Comments often carry the repro |
| 1 | Assign, move to In Progress | Skip if already there |
| 2 | Branch | `LT-XXXXX-short-slug` off fresh `origin/main` |
| 3 | **Reproduce with a failing test** | The most important step. See below |
| 4 | Fix | The minimal change that makes the test pass |
| 5 | Widen coverage | Edge cases, other call sites of the changed code, backward compatibility |
| 6 | Devil's advocate | Challenge the fix and the tests before anyone else does |
| 7 | AGENTS.md | Update only if a contract or behaviour documented there changed |
| 8 | Commit | `commit-messages` skill. gitlint runs in CI |
| 9 | PR | `pr-preflight` -- the required entrypoint. Never hand-write a PR body |
| 10 | Comment on the ticket | Root cause, fix, PR link, tests added |

**Arriving from `jira-issue`?** If that skill just filed the ticket and the
user chose "start now", steps 0-2 are already done -- the issue is fetched,
assigned, In Progress, and the branch and worktree are named in a ticket
comment. Begin at step 3.

## Step 3 -- TDD, and the escape hatch

Write a failing test that captures the bug before writing any fix. Name it
`MethodName_Scenario_ExpectedBehavior`. Confirm it fails, then fix, then
confirm it passes.

```powershell
.\test.ps1 -TestFilter "Name~TestMethodName" -CommentHygiene
```

A filter that matches nothing still exits 0 and prints PASS. Check
`Total tests: N` is greater than zero.

If a test is genuinely impossible -- purely visual, needs an external service,
or lives in build/packaging -- say **which** of those it is, then ask whether
to proceed without one. Do not skip silently, and do not invent a test that
passes either way.

## Step 6 -- Devil's advocate

Before finalising, argue against your own work:

- Is there a simpler or more robust fix? If several are reasonable, present
  them with trade-offs and let the user choose.
- Does it regress anything, or change performance or compatibility?
- Is the scope right -- not so narrow it misses sibling cases, not so broad it
  becomes a refactor?
- Does the diff carry dead code, debug artefacts, or stray whitespace?

Surface any uncertainty to the user rather than resolving it silently.

## Branching and worktrees

`LT-XXXXX-short-slug`, for example `LT-22715-nc-delete-warning`. The number
keeps step 2's "contains the LT number" check working and makes the branch
greppable; the slug is what makes a list of a dozen worktrees readable.

Do not create a worktree without asking -- and do not refuse to create one
either. `scripts/Worktree-CreateFromBranch.ps1` and the `Worktree:` VS Code
tasks exist. If `.claude/.jira-issue-prefs.json` records a `workspace`
preference, follow it and say so. The script places worktrees under
`../<repo>.worktrees/` while the ones on disk are under `.tmp/worktrees/` --
match what is already there.

## JIRA mechanics

All of it -- create, comment, attach, link, transition, and the `custom_fields`
back doors for assignee and Affects Version -- is in
`.claude/skills/jira-issue/references/publish.md`. Do not duplicate it here.

**Never transition to Done or Resolved.** That follows a merged PR, and it is
not this skill's call.

## Pause for the user

1. The current branch does not match the ticket.
2. A failing test cannot be written.
3. Devil's advocate surfaced a real alternative or uncertainty.
4. The fix reveals a larger problem than the ticket describes.

## When things fail

- **JIRA unreachable** -- ask for the details, carry on, update the ticket at the end.
- **Tests fail after the fix** -- show the failures and ask; do not tune the test to pass.
- **Push rejected** -- pull with rebase, resolve, retry.

## Composes with

`jira-issue` (files the ticket, hands off here at step 3) ·
`atlassian-skills` (JIRA writes) · `commit-messages` (step 8) ·
`pr-preflight` (step 9) · `fieldworks-test-coverage` (step 5)
