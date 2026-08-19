---
title: "Compounding enumerates every split point, and only the head re-derives"
implements: src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/CompoundingRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AnalysisCompoundingRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisCompoundingRule.cs, src/SIL.Machine.Morphology.HermitCrab/Morpher.cs
category: compounding
cost: "scales with word length x stem count on the analysis side"
grammar_visible: "partially — MaxStemCount is a host-application setting, not grammar XML"
---

## What it is

A compounding rule combines a **head** word with a **non-head** word into one compound.
Analysis-direction compounding doesn't guess where a compound's head/non-head boundary is — it
tries every position a compounding subrule's head/non-head patterns can match against the
observed shape, at every stratum where a compounding rule is declared, and only then filters by
whether the resulting non-head substring actually resolves to a real lexical root.

## Overview of the construct

A compounding rule carries separate required-feature-structures for each side (head vs.
non-head), separate MPR-feature productivity restrictions per side, a maximum application count
defaulting to **1** (a compounding rule fires at most once per word by default), and is
blockable by default (so a more specific compound can block a more general one, the same
mechanism used by ordinary affix rules).

## How the two combinatorics-limiting choices show up in the analysis-side code

Splitting an unknown surface string into head+non-head has no a-priori split point, so the
analysis-side compounding rule compiles each subrule's combined head+non-head pattern to
enumerate every split point consistent with the pattern (anchored to both the start and end of
the input), not just one. Two explicit guards bound how far this can grow: the rule bails out if
the word already has as many stems as the host-configured stem cap allows, or if it's already
been applied as many times as its own maximum-application-count permits, or if its output
features aren't unifiable with the input's — checked before the expensive split-search work.

The stem cap (`Morpher.MaxStemCount`) defaults to **2** — by default a word may contain at most a
head and one non-head stem; deeper nominal/verbal compounding (three or more stems) requires
explicitly raising this host-application setting, which re-opens combinatorial cost the default
exists to cap.

## The more important, and less obvious, limit

Analysis-direction compounding restricts each candidate non-head to a **bare lexical root** — it
looks up root allomorphs directly via a trie search, not a fully re-derived (affixed) sub-word.
The engine's own source comment states this is a deliberate complexity decision:

> for computational complexity reasons, we ensure that the non-head is a root, otherwise we
> assume it is not a valid analysis and throw it away

This means compounding's combinatorics are **not** symmetric — it is not
`parses(stem1) × parses(stem2)` recursively for both sides. The **head** genuinely re-enters the
normal stratum/affix-template pipeline (it's the same word continuing its ordinary derivation, so
it can carry its own inflectional morphology), but the **non-head** is capped to whatever root
allomorphs literally match its shape via the trie — no recursive re-affixing of the non-head is
attempted at all. Same-shape/same-allomorph candidates are also explicitly deduplicated before
continuing, purely to reduce the search space.

## Gotcha and fix

For a grammar with a large compounding lexicon and permissive head/non-head patterns (broad
optional-segment spans rather than tightly anchored shapes), the number of split points examined
before the root-lookup filter prunes them still scales with word length — and if the stem cap is
raised above its default of 2 to permit N-ary compounds, each additional stem multiplies the
number of split combinations considered, since every accepted binary split becomes a new input
that the same rule is tried against again on the next outer application (bounded by the rule's
own maximum-application-count).

Grammars that need genuinely recursive compounding (a compound whose non-head is itself a fully
affixed compound or derived stem) run up against the root-only restriction on the analysis side
by design — raising the stem cap widens how many *flat* stems a word can contain, it does not
enable recursive non-head derivation. If deep recursive compounding is linguistically required,
expect that HC's analysis-side compounding is structured to avoid it, and budget for handling
deeply nested compounds outside the compounding mechanism (e.g. as separate lexicalized entries)
rather than assuming the grammar will discover arbitrary-depth nesting on its own. Within the
grammar itself, prefer head/non-head patterns anchored as tightly as the language allows
(fixed-length or feature-narrow contexts) over broad optional-segment spans, since a tighter
pattern reduces the number of positions the split-search has to enumerate before the root-lookup
filter gets a chance to prune them. Keep the host-configured stem cap as low as the language's
actual compounding depth requires — raising it beyond what's linguistically needed directly
multiplies the split-point search.
