# Design: the `jira-issue` skill

Working document. When this becomes a PR, `pr-pitch` triages it as RESEARCH and
evicts it into the PR body. It is not meant to merge.

## Problem

Agent-authored LT tickets carry good analysis in the wrong shape. LT-22715 is
the reference failure: its first rendered line is `h3. The underlying problem`,
its description runs past a thousand words, and it announces that it contains
"four separate user-visible problems". A triager scanning a queue cannot use it.

Jira Data Center has no `{expand}` macro, so nothing can be folded away. Length
in the description is length on the screen. That single constraint drives the
whole design: the description stays short and the depth moves to a comment.

## Shape

One skill, `.claude/skills/jira-issue/`, two tracks (bug and feature) sharing
one format contract, one style reference, one publish path. Nine phases.

| Phase | Name | Gate |
| --- | --- | --- |
| 0 | Type and one-problem test | Split before drafting |
| 1 | Interview: who/when/where/how/how-bad | Cap of six questions |
| 2 | Duplicate and related hunt | Candidate table, developer says yes |
| 3 | Lede | Developer approves three lines |
| 4 | Track sections | - |
| 5 | Permission | Hard stop on user data |
| 6 | Publish | Issue, analysis comment, links |
| 7 | Report | Key, URL, one `Next:` line |
| 8 | Start now? | Four exits, hands off to `jira-bugfix` |

## Decisions

**Search before drafting, not before posting.** If the ticket already exists,
the work is a comment on it. Discovering that after twenty minutes of drafting
wastes the drafting.

**The lede is approved before anything else is written.** Three labelled lines,
rendered and shown. Up to three revisions, then the skill asks which line is
wrong rather than guessing a fourth time.

**"I don't know" is recorded, not resolved.** Missing facts go into the ticket
as `*Not known:*`. Invented ones are damage. Nothing enters the description
that the reporter did not say or that we did not verify; inferred mechanism
goes to the analysis comment, labelled as inferred.

**Preferences are a file, not memory.** `.claude/.jira-issue-prefs.json`,
gitignored, written on first run. Portable to any agent this repo supports,
per-clone, and one developer's `worktree` never becomes another's default.

**Branches are `LT-XXXXX-short-slug`.** Greppable by ticket and readable in a
worktree list of sixteen. `jira-bugfix`'s existing "branch name contains the LT
number" check keeps working.

## Rejected

**A second skill pair mirroring `pr-preflight` / `pr-pitch`.** Two entrypoints
to keep straight for a workflow that is mostly linear. The PR pair earns its
split because the write-up is re-run on existing PRs; a Jira description is
rewritten far less often.

**Collapsible sections in the description.** `{expand}` is a Confluence macro.
Verified absent before relying on the comment split.

**A shared `compact-writing` skill.** A fourth skill in the chain, loadable
when nobody asked for it. A reference file both skills point at costs less.

## Sources

`compact-style.md` adapts the MIT-licensed `i-have-adhd` skill
(https://github.com/ayghri/i-have-adhd) from chat turns to written artifacts.

## Split

PR 1, this branch: skill, style reference, prefs, `jira-bugfix` and `pr-pitch`
edits. Touches no image behaviour.

PR 2, stacked: evidence framework, `jira_add_attachment`, `pr-pitch` evidence
section, `fieldworks-avalonia-ui` capture step. Its publish step probes for
`gh --attach`, falls back to `gh image`, then to an orphan evidence branch, so
it does not depend on the gh release landing.
