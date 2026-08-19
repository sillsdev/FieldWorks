---
title: "MPR features and co-occurrence rules filter late, they don't prune early"
implements: src/SIL.Machine.Morphology.HermitCrab/MprFeature.cs, src/SIL.Machine.Morphology.HermitCrab/MprFeatureGroup.cs, src/SIL.Machine.Morphology.HermitCrab/MprFeatureSet.cs, src/SIL.Machine.Morphology.HermitCrab/MorphCoOccurrenceRule.cs, src/SIL.Machine.Morphology.HermitCrab/ConstraintType.cs, src/SIL.Machine.Morphology.HermitCrab/Allomorph.cs, src/SIL.Machine.Morphology.HermitCrab/Morpher.cs, src/SIL.Machine.Morphology.HermitCrab/AllomorphEnvironment.cs
category: morphotactics
cost: "each check is cheap in isolation (O(features) or O(word length)); the gotcha is when it runs, not how much it costs"
grammar_visible: "partially — the rules are visible, their late timing is not"
---

## What it is

MPR features are boolean tags (e.g. a noun-class or conjugation-class agreement tag) attached to
a lexical entry and referenced from rule/allomorph gates. Morpheme/allomorph co-occurrence rules
are a separate mechanism for "morpheme X requires (or excludes) morpheme(s) Y elsewhere in the
word," with a positional-adjacency setting (anywhere / somewhere-to-left / somewhere-to-right /
adjacent-to-left / adjacent-to-right). Both mechanisms are individually cheap to evaluate — but
both are evaluated **after** a complete candidate word already exists, not as an early prune
during the search.

## Cost mechanics

MPR-feature matching (required/excluded checks, grouped by feature group with any/all matching
semantics) is a hash-set-style lookup — essentially free per allomorph. Co-occurrence-rule
checking takes the word's full morpheme list and does one linear scan of that list per rule
check — `O(m)` where `m` is the number of morphemes already in the word, not the size of the
grammar. Total co-occurrence cost for a candidate word is `O(rules-on-its-allomorphs ×
word-length)` — cheap in isolation.

## The real gotcha: these are a late filter, not an early prune

Co-occurrence rules and allomorph environment checks are invoked from a word-validity check that
is only run as a final `.Where(...)` filter *after* the entire synthesis rule cascade has already
produced a complete candidate word. So a grammar author who adds obligatory co-occurrence rules
expecting them to *cut down* the combinatorial fan-out from templates, strata, or allomorph
disjunction will not see that benefit — the full candidate set is generated first, at whatever
cost the upstream combinatorics already impose, and co-occurrence rules only reject invalid
members of that already-built set. If a grammar is slow because of combinatorial
rule/template/allomorph interaction, adding co-occurrence constraints will fix *correctness*
(spurious analyses disappearing) but will not by itself fix *performance* — the fix for
performance has to happen at the source of the combinatorics (see the affix-template,
stratum-ordering, and disjunctive-allomorph gotchas). The same applies to root-allomorph
environment constraints: a broad or underspecified environment doesn't slow down unification per
se, it just means more candidates survive to be checked later — and if the environment is *too*
narrow or wrong, legitimate words silently fail to parse/generate, surfaced only through a
trace-manager failure reason rather than an upfront rejection.

## Toy example

A fictional toy language: a suffix rule excludes lexical entries tagged with an MPR feature.

```xml
<MorphologicalPhonologicalRuleFeatures>
  <MorphologicalPhonologicalRuleFeature id="mprException">Exception</MorphologicalPhonologicalRuleFeature>
</MorphologicalPhonologicalRuleFeatures>
...
<MorphologicalSubrule id="subSuf">
  <MorphologicalInput excludedMPRFeatures="mprException">...</MorphologicalInput>
  <MorphologicalOutput>...<InsertSegments><PhoneticShape>an</PhoneticShape></InsertSegments></MorphologicalOutput>
</MorphologicalSubrule>
...
<LexicalEntry id="eVokad" partOfSpeech="posMPR" ruleFeatures="mprException">
  <Allomorphs><Allomorph id="aVokad"><PhoneticShape>vokad</PhoneticShape></Allomorph></Allomorphs>
  ...
</LexicalEntry>
```

`eVokad`'s tag means the `-an` subrule's exclusion check fails for it — this root simply doesn't
take that suffix, an irregular-exception pattern, cheaply gated (a single tag lookup),
independent of the rest of the grammar's size. This mechanism is a fine, cheap way to encode
irregular exceptions; the gotcha above is specifically about expecting it to also bound search
cost, which it does not.

## Fix

Use MPR features and co-occurrence rules for what they're good at — cheaply encoding
correctness constraints and irregular exceptions on an already-generated candidate. To actually
reduce search cost, address the combinatorics at their source: collapse independently-optional
affix-template slots that are really one paradigmatic position, keep morphological-rule ordering
`linear` where possible, and minimize environment-conditioned (as opposed to MPR-feature-gated)
allomorph disjunction.
