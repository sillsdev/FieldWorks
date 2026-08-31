---
name: jira-issue
description: "Write and file an LT Jira issue -- bug, feature or task -- that a triager can act on from the first line. Use whenever asked to file, raise, report or create a Jira issue or LT ticket, to turn a user report or a finding into a ticket, or to restructure a ticket that buries its point. Interviews, hunts duplicates before drafting, gets a three-line lede approved, and moves the analysis to a comment."
argument-hint: "Optional: the raw report, a finding, or an existing LT-XXXXX to restructure"
user-invocable: true
---

# Jira Issue

Jira Data Center has no `{expand}`. Nothing folds away, so length in the
description is length on the screen for every reader, permanently. Keep the
description short and put the depth in the first comment.

Style contract: `.claude/references/compact-style.md`. Read it first.
Screenshots: `.claude/references/evidence.md`, before publishing any image.

## Phases

| # | Phase | Rule |
| --- | --- | --- |
| 0 | Type | Bug, feature or task. **More than one problem means more than one ticket** |
| 0b | Relevance | Not every ticket is about FLEx. Drop sections that do not apply; never fill them with "N/A" |
| 1 | Interview | Who / when / where / how / how bad. One at a time, **max 6**. "I don't know" is recorded, not guessed |
| 2 | Duplicates | Search **before** drafting. Show at most 5 candidates as a table with verdicts |
| 3 | Lede | Three labelled lines, **approved before anything else is written**. Max 3 revisions, then ask which line is wrong |
| 4 | Body | Track file, plus the budgets in `references/format.md` |
| 5 | Permission | "Do you have permission to post this?" Hard stop. A screenshot of a live project counts |
| 6 | Publish | `references/publish.md` |
| 7 | Report | Key, URL, one `Next:` line. Nothing else |
| 8 | Start now? | Assign, transition, comment the branch and worktree, hand to `jira-bugfix` at its Step 3 |

## The gates

Nothing is filed until the developer has answered all three:

1. the duplicate table (Phase 2)
2. the three lede lines (Phase 3)
3. permission to post (Phase 5)

If any can be skipped quietly, the skill is decoration.

## Non-negotiable

- **Affects Version on every new ticket.** `FW <FWMAJOR>.<FWMINOR>` from
  `Src/MasterVersionInfo.txt` -- `FW 9.3` at the time of writing. A filing
  convention, not a claim about a build, so Phase 0b does not exempt tooling or
  docs tickets.
- **Nothing above the fold that the reporter did not say or you did not
  verify.** Inference goes in the comment, labelled inferred.
- Budgets: summary 80 characters, lede 60 words, description 250 words ending
  in one `*Next:*` line. Overflow goes to the comment.

## Lede labels

| Type | Labels |
| --- | --- |
| Bug | `*What happens:*` `*Who hits it:*` `*How bad:*` |
| Feature | `*What they want:*` `*Who wants it:*` `*Why it matters:*` |
| Task | `*What this is:*` `*Who it affects:*` `*Why it matters:*` |

## Traps that have already bitten

- **Read link types; never guess.** There is no `Relates` here. Falling back to
  the first name in the list once produced four bogus `Cloners` links.
- **`resolution` cannot be set by an update** -- only by a transition.
- **A private URL is broken evidence.** A Gmail or Drive link renders for
  nobody. Attach the file.
- **Rewriting a ticket: post the original as a comment first**, then replace
  the description.

## Preferences

`.claude/.jira-issue-prefs.json` (gitignored): `jiraUsername`, `workspace`
(`worktree` or `branch`), `branchStyle`. Ask the two questions once on first
run, then act on them silently -- but say which preference was used, so a
wrong one is visible.

## References

| File | For |
| --- | --- |
| `references/bug-track.md` | Interview, repro rules, sample-project permission |
| `references/duplicates.md` | The four search passes and the candidate table |
| `references/feature-track.md` | User story, workarounds, cost shape |
| `references/format.md` | Budgets, required fields, Jira markup |
| `references/publish.md` | The API calls and their gotchas |
| `references/examples.md` | LT-22715 before and after |

## Before finishing

- [ ] All three gates answered by the developer.
- [ ] Affects Version set.
- [ ] Description 250 words or fewer, ending in one `*Next:*` line.
- [ ] Every unknown is a `*Not known:*` line rather than a guess.
- [ ] No section emitted that does not apply.
- [ ] Every image trimmed, captioned, and labelled headless / live / mockup.
- [ ] The title and the last line alone tell the reader what is wrong and what
      happens next.
