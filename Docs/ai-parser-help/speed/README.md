# HermitCrab performance gotchas

Part of the [AI Parser Help reference](../README.md). One file per gotcha — each is
self-contained, cites the specific `machine` source file(s) that implement the relevant
algorithm, and gives a fix. Every gotcha here is grounded in reading the actual HermitCrab
engine source (`src/SIL.Machine.Morphology.HermitCrab/` and `src/SIL.Machine/`) — none of it
depends on any other repo or on this documentation project's own history.

Each file starts with a metadata header:

```yaml
---
title: <short name of the gotcha>
implements: <machine-repo source file(s) that implement the relevant algorithm>
category: <morphotactics | allomorphy | phonology | compounding | lexicon | feature-system>
cost: <asymptotic shape or a short cost description>
grammar_visible: <yes | no | partially — can a grammar author see this from the XML alone?>
---
```

## Index

| Gotcha | Category | Cost |
|---|---|---|
| [Independently-optional affix-template slots cause exponential blowup](affix-template-optional-slots.md) | morphotactics | `O(2^n)` in slot count |
| [Unordered strata are combinatorial; linear only fixes generation, not parsing](stratum-rule-ordering.md) | morphotactics | `O(n)` gen / `O(2^n)`-ish parse (linear); `O(n!)` both ways (unordered) |
| [Kitchen-sink natural classes widen a rule's environment, not just its own cost](natural-class-feature-widening.md) | feature-system | cheap unification, widened match set downstream |
| [Environment-conditioned allomorphs aren't short-circuited until a surface form exists](disjunctive-allomorph-deferred-recheck.md) | allomorphy | multiplicative per environment-constrained allomorph |
| [MPR features and co-occurrence rules filter late, they don't prune early](mpr-cooccurrence-late-filter.md) | morphotactics | cheap per check, but runs after the full candidate is built |
| [MPR feature groups set to Overwrite are order-dependent, not accumulating](mpr-overwrite-order-dependence.md) | feature-system | blocks collapsing order-equivalent candidate derivations |
| [Iterative phonological rules rescan mutated output; simultaneous rules don't](phonological-simultaneous-vs-iterative.md) | phonology | `O(#matches)` either way; the difference is what a rule can see |
| [Iterative epenthesis can crash the engine; metathesis has no numeric backstop at all](epenthesis-metathesis-self-feeding-crash.md) | phonology | hard 256-node cap (epenthesis); unbounded (metathesis) |
| [Compounding enumerates every split point, and only the head re-derives](compounding-split-point-enumeration.md) | compounding | scales with word length × stem count |
| [Pattern-shaped root allomorphs bypass the trie and pay a linear-scan cost](root-allomorph-trie-vs-pattern.md) | lexicon | `O(word length)` trie vs. `O(#pattern allomorphs)` linear scan |

## How to use this with an LLM

Paste the raw URL of the specific gotcha file that matches your symptom, e.g.:

```
https://raw.githubusercontent.com/sillsdev/machine/master/docs/ai-parser-help/speed/affix-template-optional-slots.md
```

If you're not sure which one applies, paste this index's raw URL first and describe your
grammar's structure (not its actual rules — see the privacy note in the top-level
[`README.md`](../README.md)) and symptom (e.g. "parsing is slow," "I get way more analyses than
expected," "the engine crashes on one word").
