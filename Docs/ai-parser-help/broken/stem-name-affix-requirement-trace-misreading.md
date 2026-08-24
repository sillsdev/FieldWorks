---
title: "A rule's requiredStemName is checked in the forward (synthesis) pass only — including during parsing"
implements: src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisAffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AnalysisAffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/Morpher.cs
category: lexicon
symptom: silent-misconfiguration
grammar_visible: partially
---

## What it is

`AffixProcessRule.RequiredStemName` (`MorphologicalRules/AffixProcessRule.cs:66`) lets a grammar
author say "this affix rule may only apply to a root that already carries stem name X." This is a
different mechanism from a stem-name-restricted *allomorph* (`RootAllomorph.StemName`, checked in
`RootAllomorph.CheckAllomorphConstraints`, `RootAllomorph.cs:65-70` — see the companion gotcha in
[`stem-name-explicit-feature-requirement.md`](stem-name-explicit-feature-requirement.md) for that
mechanism). This one is a rule-level gate, and it is enforced in exactly one place in the whole
engine.

## The mechanism

`SynthesisAffixProcessRule.Apply` checks it directly:

```csharp
if (_rule.RequiredStemName != null && _rule.RequiredStemName != input.RootAllomorph.StemName)
```

(`MorphologicalRules/SynthesisAffixProcessRule.cs:106`). This compares the rule's required stem
name against `input.RootAllomorph.StemName` — a property that is only meaningful once a specific
root allomorph has been chosen, which is a synthesis-direction concept: synthesis builds a word
forward from a chosen lexical entry and allomorph.

`AnalysisAffixProcessRule.Apply` (`MorphologicalRules/AnalysisAffixProcessRule.cs`) never
references `RequiredStemName` at all. This isn't an oversight — it's structural: analysis unapplies
an affix rule by stripping it off a surface form *before* lexical lookup identifies which root (and
therefore which root allomorph) produced the stem. There is nothing to compare against yet at that
point in the pipeline.

That does not mean parsing ignores `RequiredStemName` end to end. `Morpher.ParseWord` (`Morpher.cs`)
does not return raw backward-unapplication results — it feeds every analysis candidate through
`Synthesize(word, analyses)` (`Morpher.cs:283-299`), which performs lexical lookup and then reapplies
the same forward `SynthesisAffixProcessRule.Apply` used for generation, filtering the result through
`IsWordValid`. So the check at `SynthesisAffixProcessRule.cs:106` *does* run during parsing — just in
the resynthesis/confirmation half of the pipeline, not the backward-stripping half.

## Why this is easy to miss

A grammar author or anyone reading a parser trace one rule-application at a time sees the backward
`AnalysisAffixProcessRule` step for a `requiredStemName`-bearing rule succeed unconditionally — it
never looks at stem names at all. The actual rejection only shows up later, tagged
`FailureReason.RequiredStemName`, in what a trace reader would think of as the generation/synthesis
half of a "parse." Debugging "why didn't this word parse" by reading only the analysis-rule trace
entries shows nothing wrong; the answer is in the resynthesis-confirmation entries instead.

## Concrete example

POS `posV`; feature `pers` with values `sym1`/`sym2`/`sym3`. Lexical entry `eRoot` has two
allomorphs: `kap` (no stem name) and `tam` (stem name `snP1`, region `pers=sym1`). Suffix rule
`rSuf1` (spells out `-xi`) declares `requiredStemName="snP1"`.

Parsing the surface form `kap-xi`: the backward `AnalysisAffixProcessRule` pass for `rSuf1` strips
`-xi` and reports success regardless of which root eventually gets matched. Lexical lookup then
matches the residual shape `kap` to the `kap` allomorph (no stem name). Resynthesis reapplies
`rSuf1` forward with `input.RootAllomorph` pointing at the `kap` allomorph, hits
`RequiredStemName != input.RootAllomorph.StemName` (`snP1 != null`), and the candidate is dropped —
never rejected in the initial unapplication step, only in the later confirmation step.

## Fix

When a parse unexpectedly fails (or unexpectedly succeeds) for a rule with `requiredStemName`, check
the resynthesis/confirmation trace entries (`FailureReason.RequiredStemName`), not just the backward
rule-application trace. If you want a stem-name requirement to behave symmetrically and visibly in
both directions, prefer modeling it as a stem-name-restricted *allomorph* region instead (or in
addition) — that mechanism is checked through the shared `Allomorph.IsWordValid` path and is at
least consistently a "final validity" check on both sides, rather than a rule-only gate that is
silently absent from one half of the pipeline's own rule trace.
