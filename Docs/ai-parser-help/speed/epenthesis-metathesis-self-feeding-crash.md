---
title: "Iterative epenthesis can crash the engine; metathesis has no numeric backstop at all"
implements: src/SIL.Machine.Morphology.HermitCrab/PhonologicalRules/EpenthesisSynthesisRewriteSubruleSpec.cs, src/SIL.Machine.Morphology.HermitCrab/PhonologicalRules/IterativePhonologicalPatternRule.cs, src/SIL.Machine.Morphology.HermitCrab/InfiniteLoopException.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisMetathesisRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisMetathesisRuleSpec.cs, src/SIL.Machine.Morphology.HermitCrab.Tool/SignatureFormat.cs
category: phonology
cost: "runs to a hard 256-shape-node cap, then throws (epenthesis); unbounded, no cap at all (metathesis)"
grammar_visible: "partially — visible only if you reason about whether a rule's own output can re-satisfy its own trigger"
---

## What it is

An iterative-mode epenthesis rule (segment insertion with nothing consumed on the input side)
that inserts material matching its own trigger environment can re-match the segment it just
inserted on its very next scan iteration, cascading until a hardcoded safety cap fires and the
engine throws. Metathesis (swapping two matched spans) has an equivalent self-feeding risk but
**no such cap at all** — it can hang rather than crash.

## The engine mechanism: epenthesis

Epenthesis insertion is handled by a dedicated rewrite subrule spec that inserts the new
segment(s) directly into the word's shape. Beyond the Clean/Dirty guard that protects a rule
from re-matching a node it just touched within one rule application (see the
simultaneous-vs-iterative gotcha), there is one additional hard backstop — the **only** throw
site of `InfiniteLoopException` in the entire HermitCrab source: a cap of 256 total shape nodes
on the word. This fires if repeated epenthesis (across iterations of the enclosing iterative
loop, or via chains where each epenthesized segment satisfies a *different* subrule/rule than
the one that just inserted it, sidestepping the per-rule Clean/Dirty check) keeps growing the
word indefinitely. **There is no configurable override for this cap.**

A minimal worked example: a rule that inserts a high-front-unrounded vowel after *any* high
vowel, with no right-environment restriction, applied to a word already containing high vowels.
Under simultaneous application this converges in one pass (every match is computed against the
original string). The same rule under iterative application risks each newly-inserted vowel
re-creating a new "after a high vowel" context for the next pass — exactly the case the
Clean/Dirty guard and the 256-node cap exist for.

## The engine mechanism: metathesis

Metathesis has no application-mode choice of its own — it always runs through the iterative
pattern-rule machinery, so it is always scanned iterative-style. The metathesis rule spec swaps
the two captured spans and marks every moved segment `Dirty`, and its switch-group left-hand-side
constraints are likewise cloned with a `Clean` requirement — so a rule that swaps segments A and
B cannot immediately re-match the now-`Dirty` result to swap them back within the same
application. **Unlike epenthesis, metathesis has no numeric backstop** — there is no
`InfiniteLoopException` throw anywhere in the metathesis rule classes. Oscillation prevention for
metathesis relies entirely on the Clean/Dirty mechanism; there is no cap analogous to
epenthesis's 256-node ceiling to fall back on if a grammar somehow produces a metathesis
environment the Dirty tagging doesn't cover.

## Practical takeaway

If a metathesis rule appears to hang rather than throw `InfiniteLoopException`, that specific
error is not the applicable diagnostic (it's epenthesis-only) — look instead at whether the
rule's environment can be satisfied again by the result of its own swap, since the only
protection there is the one-application-scoped `Dirty` tag.

## Operational corollary: one crashing word aborts the whole batch

This is not a parse-time complexity gotcha but a direct operational consequence of the crash
above, worth knowing before it surprises you in a batch run. The tool-level per-word parsing
entry point wraps only a single catch for the "invalid shape" exception around a word parse —
its own doc comment states explicitly that any other exception propagates, since that's treated
as a genuine engine crash, not a normal per-word outcome. `InfiniteLoopException` is exactly such
an exception. Practically: if a word list or a batch parse run hits one pathological word that
trips the 256-node epenthesis cap (or any other engine-level crash), that exception unwinds out
of the per-word handling and terminates processing of every *subsequent* word in the same run —
it does not get recorded as a per-word failure and skipped. If you're batch-parsing a large
corpus and the run stops partway with no per-word error for the remaining words, suspect a crash
on the word where it actually stopped, not silent success on everything after.

## Fix

For any epenthesis (insertion-only) rewrite rule whose inserted segment's own feature bundle
could satisfy the rule's own left/right environment, prefer simultaneous application over the
default iterative mode — simultaneous mode's "collect all matches against the original input,
then apply them all" semantics is structurally immune to this specific self-feeding cascade. If
iterative mode is required for some other reason (e.g. a rule that genuinely needs to see its own
prior applications to converge on a correct output), make sure the environment can never be
re-satisfied by the rule's own insertion — e.g. by writing the environment to require a feature
the inserted segment doesn't carry. For metathesis, since no numeric cap exists at all, this
verification matters even more: a self-feeding metathesis rule has no automatic backstop.
