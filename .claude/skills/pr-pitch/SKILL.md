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

1. **The PR body** -- a pitch, 200-400 words. Not a changelog, not an audit.
2. **One sticky PR comment** -- evidence and provenance in collapsed
   `<details>`, edited in place on every re-run so its permalink never rots.
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

So the test for every sentence:

- A reviewer needs it to say yes or no today -> **pitch**.
- A reviewer would want it only to *check* a claim the pitch makes ->
  **provenance comment, evidence section**. Summarize the claim in the body,
  link to the proof. This is where most over-long bodies go wrong: the proof
  is real and interesting and belongs on the PR, just not in the body.
- Nobody needs it today, but someone will in a year -> **provenance comment**.
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

## Phase 3 -- Write the pitch

**Budget: 200-400 words, and it must fit on one screen without scrolling.**
That is the binding constraint, not a target. Reviewers skim the description
and leave for the diff; a body that runs past a screen buries the one
paragraph that would have saved them an hour, and the length itself reads as
"the author could not say what this does."

Scale by risk, not by diff size. An 82,000-line branch whose whole story is
"one flag, defaulted off" gets a *shorter* body than a 200-line change to a
payment path, because there is less a reviewer must hold in their head.

A reviewer arrives with two kinds of gap. **Known unknowns** -- the questions
they already know they have on opening this diff -- and **unknown unknowns**,
the things only you can see because you built it. The body answers those two
and nothing else. Everything you happen to know that answers neither is
cruft: it goes in the provenance comment, or nowhere.

The overflow rule: when a section will not fit the budget, that section was
provenance-comment material. Move it and leave one line plus a link. Never
compress by deleting the qualifiers that make a claim honest.

The body, in this order.

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

The evidence for each line goes in the provenance comment, under one
`<details>` section, and this section ends with a link to it. A reviewer who
wants to check rather than trust follows the link; one who trusts the summary
never pays for it. Do not inline the proof here -- that is what blew the
budget on every over-long body this skill exists to prevent.

### 4. What is deliberately not here (one list, one line each)

Deferrals, parity gaps, known-narrower paths. A reviewer who finds an
unlisted gap stops trusting the whole pitch, so listing them buys more than
it costs. Name the in-code marker or the follow-up PR, not the reasoning --
reasoning is provenance.

### 5. Stack and verification (a few lines)

If the PR is stacked, say what it merges into and in what order. Then build,
tests, manual checks -- and what was *not* run. Anything currently red or
known-broken goes here in plain words; a reviewer must never learn of a red
job from the checks tab after reading a body that implied green.

Pitch rules:

- No process narration. "We then discovered...", "after several
  iterations..." -- cut. The reviewer is approving a result, not a journey.
- No apology, no hedging, no "should be fine".
- No section that exists only to demonstrate rigor. Depth belongs in the
  provenance comment, where it costs the reviewer nothing.
- Every claim with a name in it must be true of the current tree. Re-verify
  claims carried over from an earlier version of the body; long-lived PRs
  accumulate stale ones.
- Word-count the result before publishing. Over 400, cut -- do not rationalize.

## Phase 4 -- Write the provenance comments

**Synthesize; do not paste.** Dumping the specs into a comment is the
failure mode this skill exists to prevent. The comment carries what someone
will care about later, in your words, with the reasoning intact and the
scaffolding gone.

**One sticky comment.** There is exactly one provenance comment per PR, and
every re-run edits that same comment in place -- same ID, same permalink, so
the link in the body never rots and the thread never fills with superseded
copies. Marker-matched edit, never a fresh post. Phase 5 has the mechanics.
Prefer a second sticky comment only when a single one nears GitHub's 65,536
character cap; give it its own slug.

Structure: a preamble, then collapsed sections. The first section holds the
evidence behind the body's "Where to look" bullets -- that is the one part a
reviewer may actually open, so it goes first and the preamble says so.
Everything after it is for the future maintainer.

```markdown
<!-- pr-pitch:provenance:start:<slug> -->
The long form behind the PR description. The first section is the evidence
for the claims the description makes -- open it if you want to check them
rather than take them. Everything after it is background for whoever asks
"why is it like this?" later, and is not needed to review the change.

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

- **Evidence** -- the proof behind each "Where to look" bullet in the body:
  the predicate quoted, the call-site count, the equivalence argument, the
  test that pins it. First section, because it is the only one written for a
  reviewer rather than a future maintainer.
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
- Always carry the start/end markers. They are what makes the comment
  sticky -- without them a re-run cannot find its own previous comment and
  posts a duplicate.

## Phase 5 -- Apply

In this order:

1. Delete the RESEARCH / NOT-TAKEN / PROCESS / STALE files (`git rm`), after
   the developer has confirmed the Phase 1 triage.
2. Commit the Phase 2 alignment edits and the deletions together, with a
   message saying the reasoning moved to the PR.
3. Push.
4. Update the PR body with the pitch.
5. Update the sticky provenance comment.

Write both to files first -- never pass a body inline. Then:

```powershell
# 4. Body. gh pr edit is fine when the token has the scope; when it does not
#    (it wants org:read), go straight to REST.
gh pr edit <n> --body-file pitch.md
gh api -X PATCH repos/<owner>/<repo>/pulls/<n> -F body=@pitch.md --jq '.body|length'
```

```powershell
# 5. Sticky comment: find by marker, edit in place, create only if absent.
$id = gh api --paginate repos/<owner>/<repo>/issues/<n>/comments `
        --jq '[.[] | select(.body | contains("<!-- pr-pitch:provenance:start:<slug> -->")) | .id] | last'

if ($id) {
    gh api -X PATCH repos/<owner>/<repo>/issues/comments/$id -F body=@provenance.md --jq '.html_url'
} else {
    gh api -X POST repos/<owner>/<repo>/issues/<n>/comments -F body=@provenance.md --jq '.html_url'
}
```

Match on the marker, not on author and not on "the last comment I posted" --
`gh pr comment --edit-last` picks the most recent comment by the current
user, which on a live PR is as likely to be an unrelated reply. `| last`
handles the case where a pre-sticky run already left duplicates: it edits the
newest and you delete the older ones by hand.

The permalink is `<pr-url>#issuecomment-<id>`. It is stable across edits, so
the body may link to it -- and must, once the "Where to look" evidence lives
there. On the first run, post the comment before writing that link into the
body.

Before finishing, confirm:

- [ ] The body is 200-400 words and fits one screen. Count, do not estimate.
- [ ] The body links to the provenance comment, and the link resolves.
- [ ] The sticky comment kept its ID -- the response URL matches the one the
      body links to. A new ID means the marker did not match; find and delete
      the duplicate.
- [ ] No deleted file's content was lost -- each is represented in a
      provenance section or was deliberately dropped as STALE.
- [ ] Every name in the pitch resolves in the current tree.
- [ ] Every count in the pitch was recounted.
- [ ] The body does not duplicate the provenance comments.
- [ ] Working notes are gitignored (`Docs/migration/working/`), not merged.

Do not mark this complete on unverified claims. If a claim could not be
checked, say so in the report rather than asserting it.
