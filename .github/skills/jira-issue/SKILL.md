---
name: jira-issue
description: >
  General FieldWorks JIRA issue workflow for LT-prefixed tickets: fetch and
  summarize the issue, classify bug/feature/task/research work, reproduce or
  validate the requested behavior, gather screenshots or image-sequence
  evidence when useful, implement or document the change, test, commit, open a
  PR, and update JIRA. Use this skill whenever the user references LT-XXXXX,
  a jira.sil.org issue, or asks to work on, fix, investigate, verify, or
  document a JIRA issue.
license: MIT
compatibility: Requires atlassian-readonly-skills for reading JIRA and atlassian-skills for JIRA writes.
metadata:
  author: FieldWorks team
  version: "2.0"
---

# JIRA Issue Workflow

Use this skill for FieldWorks work sourced from SIL JIRA LT tickets. It covers
bugs, features, tasks, documentation work, research, and verification requests.
For visual or desktop UI issues, compose it with `fieldworks-winapp` to capture
reproduction and fix evidence from the live application.

## Start By Reading The Issue

Use `atlassian-readonly-skills` for LT-prefixed tickets. Do not browse directly
to `jira.sil.org` or use generic web fetch tools for JIRA.

```powershell
python -c "import sys; sys.path.insert(0, '.agents/skills/atlassian-readonly-skills/scripts'); from jira_issues import jira_get_issue; print(jira_get_issue('LT-XXXXX'))"
```

Extract and summarize:

- summary and description;
- issue type, status, priority, assignee, affected/fix versions;
- components, labels, attachments, and recent comments;
- reproduction steps, acceptance criteria, screenshots, or links;
- any explicit request for before/after evidence.

If JIRA access is denied, continue from the context already available in the
workspace or user prompt and clearly record that limitation in the work notes.

## Classify The Work

Choose the workflow shape before editing:

- Bug: reproduce first, preferably with a failing automated test or a manual
  reproduction screenshot.
- Feature or improvement: confirm desired behavior, update OpenSpec or design
  notes when the change is more than a tiny code edit.
- Task or cleanup: define the expected final state and validation signals.
- Research: collect findings, trade-offs, and recommendations without making
  production edits unless the user asks.
- Verification: run the app/tests, capture evidence, and report pass/fail.

## Branch And Scope

Check the current branch. If it does not match the ticket, ask before creating
or switching branches. Do not create a new worktree automatically unless the
user explicitly asks; this repository has VS Code tasks for worktree setup.

Keep changes scoped to the ticket. If the issue reveals unrelated problems,
record them as follow-ups rather than folding them into the current fix.

## Reproduce Or Establish Baseline Evidence

For bugs, reproduce before fixing whenever feasible:

- Automated reproduction: add or identify a failing test and run it.
- UI reproduction: use `fieldworks-winapp` to launch FieldWorks, navigate to
  the affected screen, and capture the broken state.
- Visual or timing bugs: capture an image sequence or GIF if a single screenshot
  does not show the issue clearly.

Evidence folder convention:

`Output/ManualEvidence/LT-XXXXX-<short-name>/`

For OpenSpec or docs evidence that should be committed, copy selected files into
the relevant change folder, for example:

`openspec/changes/<change-id>/evidence/manual-winapp/`

Name evidence files so the story is obvious:

- `01-before-<screen>.png`
- `02-after-<screen>.png`
- `03-after-<related-screen>.png`
- `sequence-<scenario>-001.png`, `sequence-<scenario>-002.png`, ...

When a true before-state cannot be captured because only a fixed build is
available, document the reason and include exact steps for recreating the
before-state from an unfixed build or separate worktree.

## Implement Or Document

For code changes:

1. Follow the repository instructions for the touched area.
2. Prefer the existing architecture and helper APIs.
3. Add focused tests that cover the ticket behavior.
4. Preserve user data and avoid project mutations during manual UI evidence
   unless the user requested a data-changing workflow.

For documentation, OpenSpec, or investigation-only tickets:

1. Add the smallest durable note that future reviewers need.
2. Include links or relative paths to committed evidence.
3. Separate confirmed facts from assumptions and follow-ups.

## Verify

Use the repository scripts:

```powershell
.\build.ps1
.\test.ps1
```

For targeted validation, run the smallest meaningful test set first, then widen
as risk increases. For native-only FieldWorks tests, use:

```powershell
.\test.ps1 -SkipManaged -TestProject TestViews -StartedBy agent
```

For UI issues, verify with both the app screenshot evidence and the relevant
automated tests when feasible.

## Review And Commit

Review the diff as a code reviewer:

- root cause addressed;
- evidence matches the issue claim;
- no unrelated refactors or generated churn;
- tests and manual evidence are named clearly;
- no secrets or private data in screenshots.

Commit at sensible boundaries. Use an imperative subject and include the LT
ticket when the branch/task is ticket-specific, for example:

```text
Fix LT-XXXXX: enable font options without Graphite
```

Do not stage unrelated files such as local tool state.

## Pull Request And JIRA Update

Create a PR with:

- problem/request summary;
- root cause or design rationale;
- implementation summary;
- automated test results;
- manual evidence paths or uploaded screenshots/GIFs;
- known limitations or follow-ups.

If JIRA write access is available, add a concise ticket comment with the PR link
and evidence summary. Do not transition to Done/Resolved unless the team process
or user explicitly asks.

## Skill Composition

- Use `atlassian-readonly-skills` to read issue details.
- Use `atlassian-skills` to assign, transition, or comment when requested.
- Use `fieldworks-winapp` for live FieldWorks reproduction and screenshots.
- Use `execute-implement` for implementation conventions.
- Use `verify-test` and `rubric-verify` for validation.
- Use `review` before finalizing changes or PR text.
