---
title: "Unordered strata are combinatorial; linear only fixes generation, not parsing"
implements: src/SIL.Machine.Morphology.HermitCrab/Stratum.cs, src/SIL.Machine.Morphology.HermitCrab/SynthesisStratumRule.cs, src/SIL.Machine.Morphology.HermitCrab/AnalysisStratumRule.cs, src/SIL.Machine/Rules/LinearRuleCascade.cs, src/SIL.Machine/Rules/CombinationRuleCascade.cs, src/SIL.Machine/Rules/PermutationRuleCascade.cs, src/SIL.Machine/Rules/RuleCascade.cs
category: morphotactics
cost: "O(n) generation / O(2^n)-ish parsing for linear; O(n!) both ways for unordered"
grammar_visible: yes
---

## What it is

A stratum groups a character-definition table, a lexicon, phonological rules, morphological
rules, and affix templates that all apply together. The per-stratum
`MorphologicalRuleOrder` attribute (`linear` or `unordered`, default `linear`) controls how the
stratum's morphological rules are applied relative to each other, and the two settings have very
different, and asymmetric, cost profiles on the generation vs. parsing side.

## Mechanics

The stratum's constructor picks a rule-cascade implementation from the enum:

```csharp
case MorphologicalRuleOrder.Linear:
    _mrulesRule = new LinearRuleCascade<Word, ShapeNode>(mrules, true, ...);
case MorphologicalRuleOrder.Unordered:
    _mrulesRule = new CombinationRuleCascade<Word, ShapeNode>(mrules, true, ...);
```

`LinearRuleCascade.ApplyRules` walks the rule list in fixed index order; at the first index `i`
where a rule actually produces output, it recurses on that output starting at `i+1` and then
**stops trying any other rule at this level** (`if (applied) return false;`). That is a genuine
single fixed pipeline: `O(n)` rule-application attempts per derivation, not `2ⁿ` or `n!`.

`CombinationRuleCascade.ApplyRules` instead loops over **every** rule not yet used on the current
derivation path and recurses into each one — i.e. it explores every ordering of every subset of
the stratum's morphological rules that structurally apply. Worst case that's `O(n!)`
rule-application attempts for `n` mutually-applicable rules, bounded in practice only by a
configurable alternatives cap (an exception is thrown once the alternative count exceeds the
configured limit — unbounded by default). The analysis side uses a parallelized version of the
same search for this case — same algorithm, spread across threads, not a cheaper one.

## The gotcha most authors miss

Switching a slow stratum from `unordered` to `linear` only fixes the *generation* (synthesis)
side. On the **analysis** (parsing) side, `linear` does not compile to a fixed-pipeline cascade
at all — it uses a permutation-style cascade instead, with the engine's own reasoning stated
directly in its source comment:

> Use `PermutationRuleCascade` instead of `LinearRuleCascade` because morphological rules should
> be considered optional during unapplication (they are obligatory during application, but we
> don't know they have been applied during unapplication).

`PermutationRuleCascade.ApplyRules` loops over every rule index from the current position onward
and recurses into **each one independently against the same input**, with no "stop after first
success" — i.e., during parsing, HC must consider every subset of a stratum's rules as a
candidate explanation for the surface string, because a rule that wasn't applied looks identical
(from the surface form alone) to one that doesn't fire. Because recursion only ever moves the
index forward, this restricts the search to *subsets taken in listed order* rather than full
`n!` permutations — cheaper than the unordered case's any-order-any-subset search, but still
combinatorial (`O(2ⁿ)`-ish rather than `O(n)`).

## Practical implication

For a stratum with more than a handful of morphological rules that can structurally co-apply,
`unordered` is expensive to parse *and* generate; `linear` is cheap to generate but still
combinatorial to parse, because parsing must guess which rules applied. There is no separate
ordering setting for phonological rules — those are always compiled into a plain fixed-pipeline
cascade regardless of stratum settings; only morphological-rule ordering is a grammar-author
choice with this cost profile.

## Fix

If a stratum's rules genuinely have no fixed relative order (rare, and typically small `n`),
`unordered` is correct. For anything larger, give the rules a real order and use `linear` — it
won't make parsing `O(n)`, but it keeps generation linear and keeps the parse-side search to
ordered subsets instead of full permutations.
