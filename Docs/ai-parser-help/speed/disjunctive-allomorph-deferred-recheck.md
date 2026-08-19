---
title: "Environment-conditioned allomorphs aren't short-circuited until a surface form exists"
implements: src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisAffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/Allomorph.cs, src/SIL.Machine.Morphology.HermitCrab/Word.cs
category: allomorphy
cost: "multiplicative per environment-constrained allomorph on a rule; stacks across derivation steps"
grammar_visible: "no — invisible from the grammar's own XML"
---

## What it is

A morpheme with several allomorphs, where more than one allomorph is environment-constrained
(not the unconstrained "elsewhere" case), causes the *synthesis* search to carry more live
candidate words forward than the final grammar will actually accept — the rejection happens
later, in a second full pass, not by narrowing the search up front.

## The engine mechanism

A morphological rule's subrules become allomorphs, each tried in listed order. The loop over a
rule's allomorphs only `break`s out — stopping earlier alternatives from also firing — when
three conditions all hold: the allomorph isn't the last one, it does *not* free-fluctuate with
the next allomorph, it has *zero* environments, and it has an empty required syntactic feature
structure. An environment-constrained allomorph fails the "zero environments" condition, so the
loop **does not break** — every later allomorph is also tried and also contributes an output
candidate. The engine's own source comment states the reasoning directly:

> return all word syntheses that match subrules that are constrained by environments, HC
> violates the disjunctive property of allomorphs here because it cannot check the
> environmental constraints until it has a surface form, we will enforce the disjunctive
> property of allomorphs at that time

The actual disjunctive filtering — "did an earlier-indexed allomorph's environment also match
here, in which case this later one shouldn't have been used" — happens only once the word's full
surface form exists, in a final-validity check: it walks the set of allomorphs that were "passed
over" during synthesis and rejects the candidate if any earlier, non-free-fluctuating,
environment-satisfied allomorph should have won instead. The escape hatch: when two allomorphs'
constraint sets compare exactly equal, the loop treats them as free variation rather than
disjunctive alternatives, and neither is retroactively rejected.

## Why this matters for performance

For a morpheme with `k` allomorphs where `m` of the first `k-1` are environment-constrained (not
the elsewhere case), synthesis carries up to `m+1` live candidate words forward from that single
rule application — each one fully expanded through every subsequent affix template slot and
phonological rule — before the bulk of them are pruned back out at final validity. This cost is
not exponential by itself, but it **stacks multiplicatively** with every other rule application
downstream of it in the same derivation, and it is invisible from the grammar alone: nothing in
the XML says "this environment gate delays rejection instead of preventing generation."

## Rule order is also semantically load-bearing here

Each rule's required syntactic features must unify against whatever the *previously applied*
rules left in the accumulated feature structure, and its own output features feed forward for
the next rule to require against. So within a linearly-ordered stratum, a rule that requires a
feature another rule sets must be listed after it — a correctness dependency the engine has a
separate mechanism to catch (a word where a promised feature was never actually set by the end
of the derivation is flagged as invalid).

## Toy example

A rule with several phonologically-conditioned allomorphs (e.g. "insert `-i` after a consonant,
`-ni` after a vowel, elsewhere `-mi`") looks disjunctive to a grammar author — exactly one
allomorph *should* apply to any given stem — but is not treated as disjunctive internally until
each candidate surface form is checked against its environment later. If several such rules
stack across a derivation, the independently-propagated candidates multiply.

## Fix

This is largely an engine cost a grammar author cannot design around directly — unlike the
affix-template case, there's no "collapse into one slot" fix, because environment-conditioned
allomorphy inherently can't be resolved until synthesis produces a surface string. What *is*
actionable:

- Minimize the number of allomorphs per morpheme that carry a genuine environment constraint, as
  opposed to using a broader natural class match that could instead be pushed into a
  phonological rule later in the derivation.
- Put the unconstrained "elsewhere" allomorph last, which keeps the passed-over set small per
  application.
- Add MPR-feature gates (`RequiredMprFeatures`/`ExcludedMprFeatures`) to environment-conditioned
  allomorphs where the choice can be made from a cheap tag lookup instead of a phonological
  environment — MPR-feature gates are checked first and skip non-matching allomorphs cheaply
  before the environment/pattern match is even attempted, so adding them turns wasted
  pattern-matching work into a fast `continue`, not an added cost.
