---
name: jira-issue
description: "Write and file a well-shaped LT Jira issue -- bug or feature. Use whenever asked to file, raise, report, or create a Jira issue or LT ticket, to turn a user report or a finding into a ticket, or to restructure a ticket that buries its point. Interviews for who/when/where/how, hunts duplicates before drafting, gets the three-line lede approved, keeps the description short because Jira Data Center cannot collapse anything, and moves the analysis to a comment."
argument-hint: "Optional: the raw report, a finding, or an existing LT-XXXXX to restructure"
user-invocable: true
---

# Jira Issue

Read `.claude/references/compact-style.md` first. Every rule below assumes it.

Related skills: `atlassian-readonly-skills` (search, read), `atlassian-skills`
(create, comment, link, transition), `jira-bugfix` (takes over at Phase 8).

## The constraint that shapes everything

Jira Data Center has no `{expand}` macro. Nothing in a description can be
folded away, so length in the description is length on the screen. The
description stays short; the depth goes in the first comment.

## Preferences

Read `.claude/.jira-issue-prefs.json` (gitignored). On first run, ask the two
questions once and write it:

```json
{
  "jiraUsername": "",
  "workspace": "worktree | branch",
  "branchStyle": "LT-XXXXX-slug"
}
```

Thereafter do not ask. State which preference was used when acting on it, so a
wrong one is visible and correctable.

## Phase 0 -- Type, and the one-problem test

Bug or feature. Then count the problems in the raw material.

**More than one problem means more than one ticket.** LT-22715 announces that
it contains "four separate user-visible problems"; that is four tickets and a
set of links, and splitting is far cheaper now than after triage. Propose the
split, name each ticket in one line, and get agreement before drafting.

## Phase 0b -- Relevance

**Ask what kind of thing this ticket is about before asking anything else.**
Not every LT ticket is about FLEx the product. A ticket about developer
tooling, an agent skill, the build, CI, or documentation has no FLEx version,
no project file, no menu path, and no reproduction inside the application.

| Subject | Environment questions that apply |
| --- | --- |
| FLEx product | Version and build, OS, project, menu path, keyboard/IME |
| Developer tooling, agent skills, docs | Repo, branch, which skill or script. No FLEx version |
| Build, CI, installer | Branch, runner, toolchain, which script. No project |

**Never emit a section that does not apply.** A template dutifully filled with
"N/A" is worse than a short ticket: it costs the reader the same scan and
returns nothing. Drop the heading instead.

The same test governs the interview. Asking a developer which FLEx build they
were running, for a ticket about a Markdown reference file, wastes one of the
six questions and signals that the ticket was generated rather than written.

## Phase 1 -- Interview

One question at a time. **Hard cap of six.** Quote the answers; do not
paraphrase them into confidence. Skip any row Phase 0b ruled irrelevant --
the cap is a budget, and a wasted question is one you do not get back.

| | Bug | Feature |
| --- | --- | --- |
| Who | Which user, what role, how many affected | Who wants it, and what they are actually trying to accomplish |
| When | FLEx version and build, date, first time or recurring, did it work before | How often it comes up |
| Where | Tool, window, menu path, which project | Which area of FLEx |
| How | Exact actions -- typed, pasted, dragged, IME, Send/Receive | What they do today instead |
| How bad | Data loss, workaround, blocking | What "done" looks like |

Two rules outrank the questions:

- **"I don't know" is an answer and it goes in the ticket** as a `*Not known:*`
  line. A recorded gap tells the next reader where to dig; a guess becomes
  folklore.
- **Nothing enters the description that the reporter did not say or that we
  did not verify.** Inferred mechanism goes to the analysis comment, labelled
  inferred.

If the developer is relaying a second-hand report, ask which parts they
witnessed. Second-hand detail is recorded as second-hand.

## Phase 2 -- Duplicates and related issues, before drafting

Search first. If the ticket exists, the work is a comment on it, and finding
that out after drafting wastes the drafting.

Four passes, ten results each:

```powershell
python -c @'
import sys; sys.path.insert(0, ".claude/skills/atlassian-readonly-skills/scripts")
from jira_search import jira_search
jql = "project = LT AND text ~ \"natural class\" AND status != Closed ORDER BY updated DESC"
print(jira_search(jql, fields="key,summary,status,updated", limit=10))
'@
```

1. **Symptom words** -- the user's vocabulary, not ours.
2. **Area** -- `project = LT AND component = "..." AND status != Closed`.
3. **Link walk** -- for every ticket already cited, read its links and follow
   one hop.
4. **Mechanism** -- the type or method name, when the code location is known.

Present at most five candidates:

| Key | Summary | Why it might be the same | Verdict |
| --- | --- | --- | --- |

Verdicts are duplicate, related, or unrelated. **Never file without showing
this table and getting a yes.** If a duplicate exists, stop and offer to
comment on it instead; if the developer still wants a new ticket, file it and
link it as a duplicate.

## Phase 3 -- The lede, approved before anything else is written

Draft exactly three lines and stop:

```
*What happens:* Deleting a feature-based natural class empties every rule
formula that used it. No warning, no prompt.
*Who hits it:* Anyone whose phonological rules reference Natural Classes.
*How bad:* Silent data loss. Rules must be rebuilt by hand.
```

For a feature track the labels are `*What they want:*`, `*Who wants it:*`,
`*Why it matters:*`.

Show it rendered. The developer approves, edits, or adds detail. Loop at most
three times; if it is still wrong, ask which line is wrong rather than guessing
a fourth. **Nothing else is drafted until these three lines are theirs.**

## Phase 4 -- The rest of the ticket

Follow `references/bug-track.md` or `references/feature-track.md`. Field
budgets and Jira markup are in `references/format.md`. Worked before-and-after
examples are in `references/examples.md`.

Budgets, since nothing can be folded: summary 80 characters, lede 60 words,
whole description 250 words ending in one `*Next:*` line. Overflow goes to the
analysis comment.

## Phase 5 -- Permission

**Hard stop.** Before any attachment, sample project, log, or screenshot of
real data leaves the machine:

> Do you have permission to post this?

Language data is frequently unpublished and community-owned -- vernacular
text, speaker names, unreleased lexicons. Never attach a project or capture
the agent found on disk without being told to. If the answer is no or unclear,
describe the data instead of attaching it and record that in the ticket.

## Phase 6 -- Publish

Order matters: issue, then comment, then links.

Write the description and the comment to files first; never inline multi-line
Jira markup into a command.

```powershell
python -c @'
import sys; sys.path.insert(0, ".claude/skills/atlassian-skills/scripts")
from jira_issues import jira_create_issue
desc = open("desc.txt", encoding="utf-8").read()
print(jira_create_issue("LT", "<summary>", "Bug", description=desc))
'@
```

Then the analysis comment via `jira_add_comment(issue_key, comment)`, then one
link per related ticket. Read the real link type names rather than guessing:

```powershell
python -c @'
import sys; sys.path.insert(0, ".claude/skills/atlassian-readonly-skills/scripts")
from jira_links import jira_get_link_types
print(jira_get_link_types())
'@
```

Then `jira_create_issue_link(link_type, inward_issue_key, outward_issue_key)`.

SIL Jira is Data Center: `assignee` takes a username, not an account ID,
despite what the docstring says. If assignment fails, fall back to
`custom_fields={"assignee": {"name": "<username>"}}`.

## Phase 7 -- Report

Three lines: the key, the URL, and one `Next:` line. Nothing else.

## Phase 8 -- "Do you want to start this now?"

Ask. Four exits:

| Answer | Actions |
| --- | --- |
| Start now | Assign, transition to In Progress, comment the branch and worktree, create the workspace, hand to `jira-bugfix` at its Step 3 |
| Mine, later | Assign only. Status untouched, no branch |
| Leave for triage | Nothing. This is the default |
| Someone else's | Assign to the named person, comment, stop |

On "start now":

1. Branch `LT-XXXXX-short-slug` off fresh `origin/main`.
2. Workspace per the saved preference -- worktree or branch in place. Say
   which preference was used.
3. Comment on the ticket, naming both:

   > Taken. Working on branch `LT-22715-nc-delete-warning`, worktree
   > `.tmp/worktrees/nc-delete-warning`.

   This is what makes the ticket the index into a worktree list of sixteen.
4. Hand off to `jira-bugfix` at Step 3. Do not repeat its Steps 0-2; the issue
   is already in hand, assigned and in progress.

Resolve transitions by ID from `jira_get_transitions`, never by guessing a
name. If the transition is refused for permissions, say "assigned, please move
it to In Progress yourself" and continue rather than aborting. **Never
transition to Done or Resolved** -- that follows a merged PR.

If Phase 2 found the work already exists, Phase 8 targets that existing key.

## Before finishing

- [ ] Summary is 80 characters or fewer and names the symptom, not the cause.
- [ ] The three lede lines are the developer's, not the first draft.
- [ ] Description is 250 words or fewer and ends in one `*Next:*` line.
- [ ] Every claim above the fold was said by the reporter or verified by us.
- [ ] Every unknown is a `*Not known:*` line rather than a guess.
- [ ] No section was emitted that does not apply -- no "N/A" environment, no
      empty repro steps on a ticket with nothing to reproduce.
- [ ] The duplicate table was shown and answered.
- [ ] Links created for every candidate marked duplicate or related.
- [ ] Nothing was attached without the permission question being answered.
- [ ] The reader knows what is wrong and what happens next from the title and
      the last line alone.
