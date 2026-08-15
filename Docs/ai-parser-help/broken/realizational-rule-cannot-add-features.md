---
title: "RealizationalRule can only spell out features already present — it cannot assign new ones the way AffixProcessRule can"
implements: src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/RealizationalAffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisRealizationalAffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd
category: morphotactics
symptom: silent-misconfiguration
grammar_visible: yes
---

## What it is

`AffixProcessRule` and `RealizationalAffixProcessRule` look like two flavors of the same thing (both
compile from a `<MorphologicalRule>`-family element, both hold a list of `AffixProcessAllomorph`
subrules, both apply through a Synthesis/Analysis rule pair), but they differ in a way that isn't
obvious from either the C# API surface or a first read of the DTD: `RealizationalRule` has no
mechanism at all for introducing a new syntactic feature or part of speech into the word. It can only
"realize" (spell out morphologically) a feature bundle the word already carries.

## The mechanism

`AffixProcessRule` has `OutSyntacticFeatureStruct` (`MorphologicalRules/AffixProcessRule.cs:64`),
populated on synthesis via `outWord.SyntacticFeatureStruct.PriorityUnion(_rule.OutSyntacticFeatureStruct)`
(`SynthesisAffixProcessRule.cs:182`) — this is the mechanism an ordinary inflectional or derivational
affix uses to introduce a feature value (or change part of speech) that wasn't there before.

`RealizationalAffixProcessRule` has no equivalent property. Its DTD element confirms this is not an
oversight in the C# class alone:

```
<!ELEMENT RealizationalRule (Name, MorphologicalSubrules, RealizationalFeatures?,
    RequiredHeadFeatures?, RequiredFootFeatures?, MorphemeId?, Gloss?, Properties?) >
```

(`HermitCrabInput.dtd:362`) — there is no `outputPartOfSpeech` attribute and no `OutputHeadFeatures`
child element, unlike the ordinary `MorphologicalRule` element. What it has instead is
`RealizationalFeatures`, loaded into `RealizationalFeatureStruct`
(`RealizationalAffixProcessRule.cs:54`), which `SynthesisRealizationalAffixProcessRule.Apply` merges
via `outWord.SyntacticFeatureStruct.PriorityUnion(_rule.RealizationalFeatureStruct)`
(`SynthesisRealizationalAffixProcessRule.cs:122`) — syntactically this looks like the same kind of
merge an `AffixProcessRule` does, but semantically it is gated very differently: the rule first
requires `_rule.RealizationalFeatureStruct.Subsumes(input.RealizationalFeatureStruct)`
(`SynthesisRealizationalAffixProcessRule.cs:46`) and then refuses to apply at all if
`IsBlocked(_rule.RealizationalFeatureStruct, input.SyntacticFeatureStruct, ...)` finds every one of
the rule's realizational features already present in the word's accumulated syntactic feature
structure (`SynthesisRealizationalAffixProcessRule.cs:49-59, 168-195`). A `RealizationalRule` is
designed to spell out a feature bundle some other mechanism (typically an inflectional
`AffixProcessRule`'s `OutputHeadFeatures`, or `InflFeatsOA` on the MSA in the FieldWorks compiler) has
already assigned abstractly — not to be the thing that assigns it.

## Why this is easy to miss

Both rule types are exposed in FieldWorks as morphological-rule-like entries with a features section
in the UI, and both end up merging a feature structure into the word during synthesis via what reads,
in the compiled C# alone, like the same `PriorityUnion` idiom. A grammar author modeling an agreement
affix as a `RealizationalRule` because "it realizes agreement features" can reasonably expect it to
also be able to introduce a feature the word didn't have yet (e.g. assigning `case=nom` for the first
time) the way an ordinary affix would — but there is no attribute or element in the `RealizationalRule`
schema that does that, and no code path in `SynthesisRealizationalAffixProcessRule` that adds a
feature the accumulated structure doesn't already carry in some form. The rule will still compile and
load without error; it simply never introduces the feature the author expected, and unification against
`RequiredHeadFeatures`/`RequiredFootFeatures` downstream continues to fail silently for words that
were supposed to receive it from this rule.

## Concrete example

A grammar models subject-agreement suffixes as `RealizationalRule`s, expecting each suffix to assign
the relevant person/number combination to a word that previously had no agreement features at all
(e.g. a bare verb stem). Because `RealizationalRule` has no output-feature mechanism, the suffix rule
requires `RequiredHeadFeatures` to already be compatible and merges only `RealizationalFeatureStruct`
— which, per `IsBlocked`, only proceeds when the target features are not already fully present, but
also never actually assigns them if nothing upstream (e.g. an inflectional `AffixProcessRule`'s
`OutputHeadFeatures`) already put an abstract agreement feature bundle onto the word first. A word
that never went through such an upstream rule never receives any agreement value from the
realizational suffix, no matter how many realizational rules are chained.

## Fix

Use `RealizationalRule` only to spell out features an inflectional `AffixProcessRule` (or another
mechanism) has already assigned abstractly elsewhere in the same derivation — never as the sole
mechanism responsible for introducing a feature value. If an affix genuinely needs to introduce a new
syntactic feature or part of speech, model it as an ordinary `AffixProcessRule` with
`OutputHeadFeatures`/`outputPartOfSpeech`, not as a `RealizationalRule`.
