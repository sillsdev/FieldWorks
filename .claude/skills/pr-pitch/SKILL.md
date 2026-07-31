---
name: pr-pitch
description: Compose a PR body as a pitch that answers the unknowns a reviewer arrives with, and roll the branch's research, provenance, and paths-not-taken into collapsed PR comments while evicting those files from the repo. Use when writing or refreshing a PR description, when a branch carries working markdown that should not merge, or when pr-preflight reaches its PR step.
argument-hint: "Optional PR number (defaults to the PR for the current branch)"
---

# PR Pitch

**Not an entrypoint.** `pr-preflight` is the single entrypoint for making a
PR; it calls this skill for the write-up step. Invoke this directly only to
redo the write-up on a PR that already exists.

Related skills this one calls: `fieldworks-code-commenting` (when doc text
is inlined into source), `fieldworks-migration-scope-review` (when the PR is
a large migration branch and the pitch must justify its scope).

## What this produces

Three artifacts, always together, never one without the others:

1. **The PR body** -- a pitch. Not a changelog, not an audit.
2. **One or more PR comments** -- provenance, in collapsed `<details>`.
3. **A commit** that deletes the provenance sources from the branch, so
   research and working notes inform the reviewer without merging.

If you produce the comment but skip the deletion, the branch merges its own
scaffolding. If you delete without the comment, the reasoning is lost. Do
both or do neither.

## The rule that drives everything

A reviewer arrives at the PR with the **same unknowns the author started
with**, and no time to rediscover them. The pitch closes that gap; the
provenance comment holds what a future maintainer -- not a reviewer -- will
want when they ask "why is it like this?" a year from now.

So the test for every sentence is: *does a reviewer need this to say yes or
no today?* Yes -> pitch. No, but someone will want it later -> provenance
comment. Neither -> delete it.

## Phase 1 -- Triage every markdown file on the branch

List what the branch adds or changes:

```
git diff --name-status main...HEAD -- '*.md'
```

Classify each file into exactly one bucket. Judge by what the file **is**,
not where it lives.

| Bucket | Test | Destination |
| --- | --- | --- |
| DURABLE | Someone changing this code next year must read it to change it correctly | Stays in repo -- align it in Phase 2 |
| RESEARCH | A one-time investigation, audit, census, or survey whose conclusion is now baked into the code | Provenance comment, then delete |
| NOT-TAKEN | Options considered and rejected, alternatives, tradeoffs, abandoned approaches | Provenance comment, then delete |
| PROCESS | Task checklists, burn-down guides, review checklists, working notes tied to *doing* the work | Provenance comment (only the durable findings), then delete |
| STALE | Describes code that no longer exists | Delete. Salvage to provenance only if it records a real reversal |

Two traps:

- **A spec is not automatically durable.** A spec that merely narrates what
  the code now plainly says is RESEARCH. Durable means it carries
  constraints the code cannot express: invariants, contracts with other
  subsystems, rejected designs that will otherwise be re-proposed.
- **Skills are usually durable, references are usually not.** A
  `SKILL.md` that guides future work stays. A `references/*.md` capturing
  one migration's findings is RESEARCH.

Present the triage to the developer as a table and get confirmation before
deleting anything. Deletion is theirs to approve.

## Phase 2 -- Align what stays with the code as it is

**Always trust the written code.** When a doc and the code disagree, the
code is right and the doc is wrong -- never the reverse, and never
"reconcile" by softening the doc into vagueness.

For each DURABLE file, verify every concrete noun it names -- type,
interface, method, namespace, file path, setting key, test name -- against
the tree:

```
git grep -l "<Name>" -- 'Src/*'
```

Zero hits means the doc is wrong. Find what the thing is called now and fix
the doc. Renames, folder moves, and split interfaces are the usual cause.

Also re-check every **count** a doc asserts ("~8 call sites", "12 markers").
Counts rot silently. Recount from the tree and correct.

Do not rewrite prose that is still accurate. This phase is a correction
pass, not a rewrite.

## Phase 3 -- Write the pitch

The body, in this order. Keep it to what a reviewer needs.

### 1. Lead with what it does

Open with the concrete thing, not the framing. A screenshot or GIF if the
change is visible; otherwise one sentence naming what a user or caller can
now do that they could not before. Never open with "This PR refactors...".

### 2. The unknown the reviewer starts with

State the question the reviewer will actually have on opening a diff this
size -- usually "what breaks?" or "why is this so big?" -- and answer it in
the first paragraph. Do not make them read to find it.

### 3. What an expert would worry about, answered

The load-bearing section. List the failure points a domain expert would
anticipate, each with the evidence that it was handled. Not "this is safe"
but *why* it is safe and what pins it there -- the gate, the test, the
invariant.

Order by what would sink the PR, not by what was hardest to build.

### 4. What is deliberately not here

Deferrals, parity gaps, known-narrower paths. A reviewer who finds an
unlisted gap stops trusting the whole pitch, so listing them buys more than
it costs. Name where each is marked in-code.

### 5. How it was verified

Build, tests, manual checks -- and what was *not* run. State skipped
validation plainly.

Pitch rules:

- No process narration. "We then discovered...", "after several
  iterations..." -- cut. The reviewer is approving a result, not a journey.
- No apology, no hedging, no "should be fine".
- Every claim with a name in it must be true of the current tree. Re-verify
  claims carried over from an earlier version of the body; long-lived PRs
  accumulate stale ones.
- If the PR is stacked, say what it merges into and in what order.

## Phase 4 -- Write the provenance comments

**Synthesize; do not paste.** Dumping the specs into a comment is the
failure mode this skill exists to prevent. The comment carries what someone
will care about later, in your words, with the reasoning intact and the
scaffolding gone.

Structure: a one-line preamble, then collapsed sections. Multiple
`<details>` blocks per comment; multiple comments when needed.

```markdown
<!-- pr-pitch:provenance:start:<slug> -->
Background for this PR, kept out of the tree. Nothing here is required to
review the change -- it is here for whoever asks "why is it like this?"
later.

<details>
<summary><b>Decisions and why</b></summary>

...

</details>

<details>
<summary><b>Paths not taken</b></summary>

...

</details>
<!-- pr-pitch:provenance:end:<slug> -->
```

Sections worth writing, when the branch has them:

- **Decisions and why** -- the choice, the alternatives, what tipped it.
- **Paths not taken** -- what was tried or seriously considered and
  rejected, and the reason. This is the single most valuable section; it is
  what stops the next person re-proposing a dead end.
- **Surprising findings** -- what the investigation turned up that
  contradicted the initial assumption.
- **Reversals** -- things built and then removed, with why. Future
  archaeology on the commit history lands here.
- **Deferred with rationale** -- what was scoped out and what would need to
  be true to pick it up.

Rules:

- Each section stands alone. Nobody reads these top to bottom.
- Attribute nothing to a person; describe the decision, not the deciders.
- GitHub caps a comment at 65,536 characters. Split by theme, not by
  arbitrary cut -- and if you are near the cap, you are pasting, not
  synthesizing. Cut harder.
- Use the start/end markers so a re-run replaces the comment instead of
  stacking a duplicate.

## Phase 5 -- Apply

In this order:

1. Delete the RESEARCH / NOT-TAKEN / PROCESS / STALE files (`git rm`), after
   the developer has confirmed the Phase 1 triage.
2. Commit the Phase 2 alignment edits and the deletions together, with a
   message saying the reasoning moved to the PR.
3. Push.
4. Update the PR body with the pitch (`gh pr edit <n> --body-file`).
5. Post or update the provenance comments, matching on the markers.

Before finishing, confirm:

- [ ] No deleted file's content was lost -- each is represented in a
      provenance section or was deliberately dropped as STALE.
- [ ] Every name in the pitch resolves in the current tree.
- [ ] Every count in the pitch was recounted.
- [ ] The body does not duplicate the provenance comments.
- [ ] Working notes are gitignored (`Docs/migration/working/`), not merged.

Do not mark this complete on unverified claims. If a claim could not be
checked, say so in the report rather than asserting it.
