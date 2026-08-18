---
name: pr-pitch
description: "NOT an entrypoint -- pr-preflight calls this for the write-up step; use pr-preflight for a fresh 'write/make/open a PR' request. Invoke this directly only to redo the write-up on a PR that already exists. Composes a PR body as a pitch that answers the unknowns a reviewer arrives with, with the branch's decisions, provenance, and paths-not-taken folded into collapsed accordions below it, while evicting those files from the repo."
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

Two artifacts, always together, never one without the other:

1. **The PR body**, in two zones:
   - **The pitch** -- 200-400 words, above the fold, uncollapsed.
   - **The accordions** -- collapsed `<details>` sections below it, holding
     the decisions, evidence, and paths not taken.
2. **A commit** that deletes the provenance sources from the branch, so
   research and working notes inform the reviewer without merging.

**Everything goes in the body -- never a separate comment.** The PR
description is the one place a reader always looks, it is inherently sticky
(editing it is always in place, the URL never changes), and it is the only
part of the PR that survives being read a year later without scrolling a
thread. A second comment splits the record for no gain: nothing here needs a
comment's affordances, and no CI job posts alongside it.

If you write the accordions but skip the deletion, the branch merges its own
scaffolding. If you delete without the accordions, the reasoning is lost. Do
both or do neither.

## The rule that drives everything

A reviewer arrives at the PR with the **same unknowns the author started
with**, and no time to rediscover them. The pitch closes that gap; the
accordions hold what a future maintainer -- not a reviewer -- will want when
they ask "why is it like this?" a year from now.

So the test for every sentence:

- A reviewer needs it to say yes or no today -> **the pitch**.
- A reviewer would want it only to *check* a claim the pitch makes ->
  **an accordion**. State the claim above the fold, put the proof below it.
  This is where most over-long bodies go wrong: the proof is real and
  interesting and belongs on the PR, just not above the fold.
- Nobody needs it today, but someone will in a year -> **an accordion**.
- Neither -> **delete it**.

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

## Phase 3 -- Write the pitch (the top zone)

**Budget: 200-400 words, and it must fit on one screen without scrolling.**
That is the binding constraint, not a target. Reviewers skim the description
and leave for the diff; a body that runs past a screen buries the one
paragraph that would have saved them an hour, and the length itself reads as
"the author could not say what this does."

Scale by risk, not by diff size. An 82,000-line branch whose whole story is
"one flag, defaulted off" gets a *shorter* body than a 200-line change to a
payment path, because there is less a reviewer must hold in their head.

The budget applies to the top zone only. The accordions below it are as long
as the branch's reasoning deserves -- a closed `<details>` costs a reader
nothing. There is no tension between "short" and "complete" here; there is
only the question of which zone a sentence belongs in.

A reviewer arrives with two kinds of gap. **Known unknowns** -- the questions
they already know they have on opening this diff -- and **unknown unknowns**,
the things only you can see because you built it. The top zone answers those
two and nothing else. Everything you happen to know that answers neither goes
below the fold, or nowhere.

The overflow rule: when a section will not fit the budget, that section was
accordion material. Move it down and leave one line up top. Never compress by
deleting the qualifiers that make a claim honest.

The top zone, in this order.

### 1. Lead with what it does (2-3 sentences)

Open with the concrete thing, not the framing. A screenshot or GIF if the
change is visible; otherwise one sentence naming what a user or caller can
now do that they could not before. Never open with "This PR refactors...".

### 2. The unknown the reviewer starts with (one paragraph)

State the question the reviewer will actually have on opening a diff this
size -- usually "what breaks?" or "why is this so big?" -- answer it, and
then reframe the review around the question that *is* worth their time. Do
not make them read to find it.

### 3. Where to look (at most five bullets, one line each)

The load-bearing section. The failure points a domain expert would
anticipate, ordered by what would sink the PR -- not by what was hardest to
build. One line each: the risk, and the thing that pins it -- the gate, the
test, the invariant.

The evidence for each line goes in an accordion below. A reviewer who wants
to check rather than trust opens it; one who trusts the summary never pays
for it. Do not inline the proof here -- that is what blew the budget on every
over-long body this skill exists to prevent.

### 4. What is deliberately not here (one list, one line each)

Deferrals, parity gaps, known-narrower paths. A reviewer who finds an
unlisted gap stops trusting the whole pitch, so listing them buys more than
it costs. Name the in-code marker or the follow-up PR, not the reasoning --
reasoning goes in an accordion.

### 5. Stack and verification (a few lines)

If the PR is stacked, say what it merges into and in what order. Then build,
tests, manual checks -- and what was *not* run. Anything currently red or
known-broken goes here in plain words; a reviewer must never learn of a red
job from the checks tab after reading a body that implied green.

Pitch rules:

- No process narration. "We then discovered...", "after several
  iterations..." -- cut. The reviewer is approving a result, not a journey.
- No apology, no hedging, no "should be fine".
- No section that exists only to demonstrate rigor. Depth belongs below the
  fold, where it costs the reviewer nothing.
- Every claim with a name in it must be true of the current tree. Re-verify
  claims carried over from an earlier version of the body; long-lived PRs
  accumulate stale ones.
- Word-count the result before publishing. Over 400, cut -- do not rationalize.

## Phase 4 -- Write the accordions (the bottom zone)

**Synthesize; do not paste.** Dumping the specs into `<details>` blocks is
the failure mode this skill exists to prevent. The accordions carry what
someone will care about later, in your words, with the reasoning intact and
the scaffolding gone.

They go directly below the pitch in the same PR body, after a `---` rule.
Open with a short orienting accordion that says what this record is and why
it exists outside the tree -- a reader a year out needs to know the working
documents were deliberately deleted, not lost.

```markdown
---

<details>
<summary><b>Reading this a year from now</b> -- start here</summary>

What this record is, and why the reasoning lives here instead of in the
tree.

</details>

<details>
<summary><b>Decisions, and why</b></summary>

...

</details>
```

Sections worth writing, when the branch has them:

- **Reading this a year from now** -- the orienting preamble. First.
- **The layer cake** -- for a branch that introduces an architecture, the
  path from input to output with the real type at each hop. This is the
  single most useful thing for someone arriving cold.
- **Decisions, and why** -- the choice, the alternatives, what tipped it.
  Prefer decisions where the code looks arbitrary until you know the reason.
- **Paths not taken** -- what was tried or seriously considered and
  rejected, and the reason. The single most valuable section; it is what
  stops the next person re-proposing a dead end.
- **Reversals** -- things built and then removed, with why. Note which ones
  are invisible in `git log` because they happened inside a squash.
- **Surprising findings** -- what the investigation turned up that
  contradicted the initial assumption.
- **What this does NOT authorize** -- for a foundational branch, the limits.
  A later reader will otherwise cite the branch as precedent for more than
  it decided.
- **Deferred, and what would unblock it** -- what was scoped out and what
  would need to be true to pick it up.
- **Evidence** -- the proof behind the pitch's "Where to look" bullets: the
  predicate quoted, the call-site count, the equivalence argument, the test
  that pins it. Written for a reviewer rather than a maintainer, so it can
  sit last; the pitch already carries the claim.

Rules:

- Each section stands alone. Nobody reads these top to bottom.
- Attribute nothing to a person; describe the decision, not the deciders.
- GitHub caps a PR body at 65,536 characters. If you are near it, you are
  pasting, not synthesizing. Cut harder.
- Reasoning recoverable *only* from git history -- because the branch
  deleted the document that argued it -- is the highest-value content here.
  Prefer it over anything a reader could derive by reading the tree.

## Phase 5 -- Apply

In this order:

1. Delete the RESEARCH / NOT-TAKEN / PROCESS / STALE files (`git rm`), after
   the developer has confirmed the Phase 1 triage.
2. Commit the Phase 2 alignment edits and the deletions together, with a
   message saying the reasoning moved to the PR.
3. Push.
4. Update the PR body -- pitch and accordions, one write.

Write the body to a file first -- never pass it inline. Then:

```powershell
# gh pr edit is fine when the token has the scope; when it does not
# (it wants org:read), go straight to REST.
gh pr edit <n> --body-file body.md
gh api -X PATCH repos/<owner>/<repo>/pulls/<n> -F body=@body.md --jq '.body|length'
```

The body is inherently sticky: editing it is always in place and the PR URL
never changes, so nothing needs marker-matching or an existence check. That
is the main reason the record lives here rather than in a comment.

If an earlier run of this skill left a separate provenance comment, fold its
content into the accordions and delete it, so there is exactly one record:

```powershell
gh api -X DELETE repos/<owner>/<repo>/issues/comments/<id>
```

Deleting is destructive and public -- confirm with the developer first, and
only after its content is verifiably in the published body.

Anything in the tree that pointed at that comment now dangles. Check before
you delete, and repoint at the PR description:

```
git grep -n "provenance comment\|issuecomment" -- openspec/ Docs/ Src/
```

Note that a wrapped line will defeat a naive grep for a two-word phrase --
search for each word.

Before finishing, confirm:

- [ ] The pitch zone is 200-400 words and fits one screen. Count, do not
      estimate.
- [ ] Every `<details>` is closed -- count `<details>` against `</details>`.
- [ ] The whole body is under 65,536 characters.
- [ ] No content lives in a PR comment; the description is the only record.
- [ ] Nothing in the tree references a comment that was deleted.
- [ ] No deleted file's content was lost -- each is represented in an
      accordion or was deliberately dropped as STALE.
- [ ] Every name in the body resolves in the current tree -- accordions rot
      the same way the pitch does, and a rename sweep late in a branch will
      have stranded names the earlier reasoning used.
- [ ] Every count was recounted.
- [ ] The pitch does not repeat what an accordion already says.
- [ ] Working notes are gitignored (`Docs/migration/working/`), not merged.

Do not mark this complete on unverified claims. If a claim could not be
checked, say so in the report rather than asserting it.
