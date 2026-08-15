---
title: "Iterative phonological rules rescan mutated output; simultaneous rules don't"
implements: src/SIL.Machine.Morphology.HermitCrab/PhonologicalRules/RewriteRule.cs, src/SIL.Machine.Morphology.HermitCrab/PhonologicalRules/SimultaneousPhonologicalPatternRule.cs, src/SIL.Machine.Morphology.HermitCrab/PhonologicalRules/IterativePhonologicalPatternRule.cs, src/SIL.Machine.Morphology.HermitCrab/HCFeatureSystem.cs
category: phonology
cost: "O(#matches) either way in the ordinary case; the difference is what a rule can see, not how many times it runs"
grammar_visible: yes
---

## What it is

A phonological rewrite rule has an application mode — simultaneous or iterative (the default) —
and the choice determines whether later matches in the same rule application can see the effects
of earlier ones. This is not just a style preference: it changes what inputs a rule's own
environment can match against, and it's the mechanism behind a self-feeding crash risk
documented separately (see the epenthesis/metathesis gotcha).

A rewrite rule can have multiple subrules, each with its own left/right environment; like affix
template slots, all subrules are tried and each one that matches contributes an output — `O(k)`
for `k` subrules, not exponential, provided the subrules' environments are close to mutually
exclusive.

## The algorithmic difference that matters

- **Simultaneous** application collects **all** matches from the matcher against the original,
  unmodified word first, then applies every matched subrule's output afterward in a second pass.
  No match ever sees the effect of any other match from the same application — one pass,
  `O(#matches)`, no rescanning.
- **Iterative** application (the default) is a `while` loop: match, apply (or advance past a
  non-match), then re-match starting just past the just-consumed match **on the already-mutated
  word**. Content the rule just inserted or modified is still ahead of the scan position and can
  be matched again on the next loop iteration.

## The guard against immediate self-reapplication

An iterative rule whose own output re-satisfies its own trigger environment can reapply into
content it just produced. HermitCrab's guard against this within a single rule application is a
feature, not a counter: a `Modified` feature with values `Dirty`/`Clean` (default `Clean`). A
rule's own left-hand-side pattern is compiled with an added requirement that the target be
`Clean`, and every node a rule inserts or modifies is marked `Dirty` immediately — so the *same
rule* cannot immediately rematch a node it just touched, within one application. The `Dirty`
marks persist for the whole scan and are only reset once, at the very end of that application —
so this protection covers one full rule application, not across separate stratum/rule
applications, and it protects against the *same rule* re-triggering on its own output, not a
*different* rule doing so.

## Fix / practical takeaway

If a phonological pattern is meant to look at the *original* string only (a static assimilation
table, for instance) rather than a genuinely cascading process, prefer simultaneous application
— it's a strictly one-pass operation and cannot loop by construction. Reserve iterative
application for rules that are genuinely meant to rescan (e.g. iterative stress assignment
across a whole word), and be aware that its safety against self-reapplication depends entirely
on the Clean/Dirty tagging described above, not on anything the grammar author writes explicitly.
