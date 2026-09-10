---
name: pr-pitch
description: "NOT an entrypoint -- pr-preflight calls this for the write-up step; use pr-preflight for a fresh 'write/make/open a PR' request. Invoke this directly only to redo the write-up on a PR that already exists. Composes a PR body as a pitch that answers the unknowns a reviewer arrives with, with the branch's decisions, provenance, and paths-not-taken folded into collapsed accordions below it, while evicting those files from the repo."
argument-hint: "Optional PR number (defaults to the PR for the current branch)"
---

# PR Pitch

**Not an entrypoint.** `pr-preflight` calls this for the write-up. Invoke it
directly only to redo the write-up on an existing PR.

Read `.claude/references/compact-style.md` before writing, and
`.claude/references/evidence.md` before publishing any screenshot.

## What this produces

Two artifacts, always together:

1. **The PR body** -- a pitch of 200-400 words above the fold, then collapsed
   `<details>` accordions holding decisions, evidence and paths not taken.
2. **A commit deleting the provenance sources** from the branch, so research
   and working notes inform the reviewer without merging.

Do both or neither. Accordions without the deletion merges the scaffolding;
deletion without accordions loses the reasoning.

**Everything goes in the body, never a separate comment.** The description is
the one place a reader always looks, editing is in place, and it survives being
read a year later without scrolling a thread.

## The rule that drives everything

A reviewer arrives with the **same unknowns the author started with**, and no
time to rediscover them. So, for every sentence:

| Test | Zone |
| --- | --- |
| A reviewer needs it to say yes or no today | The pitch |
| They would want it only to *check* a claim the pitch makes | An accordion |
| Nobody needs it today, but someone will in a year | An accordion |
| Neither | Delete it |

## Phase 1 -- Triage every markdown file on the branch

```
git diff --name-status main...HEAD -- '*.md'
```

Judge by what the file **is**, not where it lives.

| Bucket | Test | Destination |
| --- | --- | --- |
| DURABLE | Someone changing this code next year must read it to change it correctly | Stays -- align it in Phase 2 |
| RESEARCH | A one-time investigation whose conclusion is now baked into the code | Accordion, then delete |
| NOT-TAKEN | Options considered and rejected | Accordion, then delete |
| PROCESS | Checklists and working notes tied to *doing* the work | Accordion (durable findings only), then delete |
| STALE | Describes code that no longer exists | Delete. Salvage only a real reversal |

Two traps: **a spec is not automatically durable** -- one that narrates what
the code plainly says is RESEARCH; durable means constraints the code cannot
express. And **skills are usually durable, references usually not.**

Present the triage as a table and get confirmation. **Deletion is the
developer's to approve.**

## Phase 2 -- Align what stays

**Always trust the written code.** Where a doc and the code disagree, the doc
is wrong -- never the reverse, and never "reconcile" by going vague.

Verify every concrete noun a DURABLE file names -- type, method, path, setting
key, test name -- with `git grep -l "<Name>" -- 'Src/*'`. Zero hits means the
doc is wrong; find the current name. Recount every **count** it asserts; counts
rot silently. This is a correction pass, not a rewrite.

## Phase 3 -- The pitch

**200-400 words, fitting one screen without scrolling.** That is binding, not
a target. Scale by *risk*, not diff size: an 82,000-line branch whose story is
"one flag, defaulted off" gets a shorter body than a 200-line payment change.

When a section will not fit, it was accordion material -- move it down and
leave one line up top. Never compress by deleting the qualifiers that make a
claim honest.

The `**Start here:**` line leads the body -- the first file to read, and why.
That is the *entry point*, not the riskiest thing; a reviewer not told where to
begin reads the diff alphabetically, which is nobody's reading order. Then, in
order:

**0. Status** (optional, one line) -- only for a PR open a while. What it is
waiting on, and anything red. *"Ready for review. CI green except the
known-flaky interlinear test."*

**1. What it does** (2-3 sentences) -- the concrete thing, not the framing.
Never open with "This PR refactors". If the change is visible, **a picture is
expected, not optional**: trimmed, captioned with what to look at, and
labelled headless / live / mockup. The image supports the claim; the test pins
it.

**2. The unknown they start with** (one paragraph) -- usually "what breaks?"
or "why is this so big?". Answer it, then reframe the review around the
question worth their time.

**3. Where to look** (at most five bullets) -- the failure points a domain
expert would anticipate, ordered by what would sink the PR -- one line each:
the risk, and the thing that pins it. **The proof goes in an accordion**;
inlining it is what blows the budget.

**4. What is deliberately not here** -- deferrals, parity gaps, narrower
paths. A reviewer who finds an unlisted gap stops trusting the whole pitch.
Name the marker or follow-up PR, not the reasoning.

**5. Stack and verification** -- what it merges into and in what order. Build,
tests, manual checks, and what was *not* run. Anything red goes here in plain
words; a reviewer must never learn of a red job from the checks tab. Give
anything they must run themselves as numbered, copy-pasteable steps -- prose
verification instructions get skipped.

**6. What you want from them** (one line) -- *"Next: approve, or tell me to
split the installer change out."* A pitch ending on a verification paragraph
leaves the reviewer guessing whether they are approving, splitting or blocking.

### Pitch rules

- No process narration. "We then discovered", "after several iterations" -- cut.
- No apology, no hedging, no "should be fine".
- No section that exists only to demonstrate rigor.
- Every claim with a name in it must be true of the current tree. Re-verify
  claims carried over from an earlier version of the body.
- Word-count before publishing. Over 400, cut -- do not rationalize.

## Phases 4 and 5

Accordions: `references/accordions.md`. Applying and the final checklist:
`references/publishing.md`.

Related skills this calls: `fieldworks-code-commenting` (when doc text is
inlined into source), `fieldworks-migration-scope-review` (large migration
branches).
