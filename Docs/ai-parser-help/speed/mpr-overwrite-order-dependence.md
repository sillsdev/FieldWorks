---
title: "MPR feature groups set to Overwrite are order-dependent, not accumulating"
implements: src/SIL.Machine.Morphology.HermitCrab/MprFeatureSet.cs, src/SIL.Machine.Morphology.HermitCrab/MprFeatureGroup.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisAffixProcessAllomorphRuleSpec.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisCompoundingRule.cs
category: feature-system
cost: "not exponential by itself, but blocks collapsing otherwise-equivalent candidate derivations that differ only in rule order"
grammar_visible: "no — reads like a plain accumulating tag set from the XML alone"
---

## What it is

An MPR feature group can be declared with output policy `Overwrite` instead of the default
`Append`. Where `Append` behaves like an ordinary accumulating tag set (every rule's output MPR
features just get added to the word's running set), `Overwrite` does not accumulate — it makes
the word's final state for that group depend on which rule touched it *last*, silently dropping
any earlier rule's contribution to that same group.

## The mechanism

Adding a rule's output MPR features to a word's accumulated set is implemented as: for every
feature group the new output touches that is set to `Overwrite`, first remove every existing
member of that group from the word's current set unless the new output restates it, and only
then union in the new output. Concretely — if rule A sets group G to `{x}`, and a later rule B
in the same derivation sets group G to `{y}`, the word's final state for G is `{y}` alone; `x` is
gone, not merged. This is a genuinely order-dependent, non-monotone update, not an accumulation —
and it's called once per rule application (both ordinary affix-process allomorphs and
compounding subrules feed their output MPR features through the same mechanism), in whatever
order those rules actually fire during a derivation.

## Why this is easy to miss, and why it matters beyond correctness

It's easy to assume MPR features behave uniformly across a grammar — like plain boolean tags that
simply accumulate as rules apply, the way `RequiredMprFeatures`/`ExcludedMprFeatures` checks treat
them everywhere else. An `Overwrite` group breaks that assumption silently: nothing in a rule's
own XML declaration says "this group resets instead of accumulates" — that's a property of the
*group*, declared once, potentially far away from any of the rules that touch it.

This also has a subtler consequence for the engine's search, not just for grammar-author
intuition. HC's search can legitimately produce multiple candidate derivations that apply the
*same set* of rules in a *different relative order* — for example, an unordered stratum's
rule-combination search, or two rules that are each independently eligible to fire in either
order relative to each other. Two such derivations would ordinarily be collapsible, or at least
treated as equivalent in their morphosyntactic outcome, if every feature mechanism accumulated
monotonically. But because an `Overwrite` group's final content depends on *which rule touched it
last*, two derivations that apply the same rules in different orders can end up with genuinely
different final MPR-feature states for that group — meaning the engine cannot treat them as
interchangeable. Downstream checks gated on that group (required/excluded MPR-feature matching,
or a compounding rule's MPR-feature productivity restriction) can then behave differently for
what looks, from the morpheme sequence alone, like "the same word."

## Fix

Prefer `Append` for MPR feature groups unless "last rule wins" is specifically the intended
semantics (e.g. a group modeling a paradigm cell that later derivation steps are meant to
override outright, such as a valence-changing operation resetting an argument-structure tag). If
`Overwrite` is required, keep the rules that touch that group in a fixed relative order — e.g. by
keeping the containing stratum's morphological-rule order `linear` with those rules ordered
correctly relative to each other — so the non-monotone behavior can't silently vary between
logically-equivalent derivations, and so a grammar author reasoning about the group's final state
only has to reason about one order, not every order the search might explore.
