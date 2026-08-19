---
title: "RealizationalAffixProcessRule has no MaxApplicationCount backstop, unlike AffixProcessRule"
implements: src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/RealizationalAffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisRealizationalAffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AnalysisRealizationalAffixProcessRule.cs
category: morphotactics
symptom: wrong-parse
grammar_visible: partially
---

## What it is

An ordinary `AffixProcessRule` is capped at one application per derivation by default —
`MaxApplicationCount = 1` is set in its constructor (`MorphologicalRules/AffixProcessRule.cs:28`)
and enforced on every application: `if (input.GetApplicationCount(_rule) >= _rule.MaxApplicationCount)`
(`MorphologicalRules/SynthesisAffixProcessRule.cs:46`, mirrored on the analysis side at
`AnalysisAffixProcessRule.cs:45`). `RealizationalAffixProcessRule` — the rule type meant for pure
feature-realization/spellout rules rather than form-changing affixation — has no such property at
all. Reading the whole file confirms it: no `MaxApplicationCount` field, no constructor default, no
check anywhere in `RealizationalAffixProcessRule.cs`, `SynthesisRealizationalAffixProcessRule.cs`, or
`AnalysisRealizationalAffixProcessRule.cs`.

## The mechanism

The only guard against a `RealizationalAffixProcessRule` reapplying itself indefinitely in the same
derivation is a feature-based recursive check, `IsBlocked`
(`SynthesisRealizationalAffixProcessRule.cs:168-195`), gated by:

```csharp
if (!_rule.RealizationalFeatureStruct.IsEmpty
    && IsBlocked(_rule.RealizationalFeatureStruct, input.SyntacticFeatureStruct, ...))
```

`IsBlocked` walks the rule's `RealizationalFeatureStruct` recursively and returns true only when
every one of the rule's realizational features is *already present* in the word's accumulated
syntactic feature structure. If a rule's realizational features are only a partial or ambiguous
subset of what's already assigned — or if the rule sets a feature that the accumulated structure
doesn't yet carry in a form `IsBlocked` recognizes as "already there" — this guard does not fire, and
nothing else stops the rule from applying again in a cyclic derivation. Unlike an
`AffixProcessRule`, there is no numeric backstop underneath the feature check.

## Why this is easy to miss

A grammar author who reasons "every morphological rule type gets a default `MaxApplicationCount` of
1 unless I explicitly raise it" (true for `AffixProcessRule` and `CompoundingRule`, whose
constructors both set `MaxApplicationCount = 1`, e.g. `MorphologicalRules/CompoundingRule.cs:19`)
will not find that assumption holds for `RealizationalAffixProcessRule` at all — there is no
attribute to raise or lower, because the rule type doesn't have the property. Nothing in a grammar's
XML for a `RealizationalRule` (which has no `multipleApplication` attribute in the schema for this
rule kind) hints that the usual per-rule application cap doesn't exist here.

## Concrete example

A `RealizationalRule` meant to spell out a single agreement feature (`agr=match`) as a floating tone
or zero-marking, with a `RequiredSyntacticFeatureStruct` that doesn't itself exclude re-application
(e.g. it only requires `posV`, not "agr is not yet match"). A stratum's own rule cascade
(`LinearRuleCascade`/`CombinationRuleCascade`, see `speed/stratum-rule-ordering.md`) tracks rules
"not yet used on the current derivation path" within a single pass through that cascade, so within
one straightforward pass the rule fires at most once. But whenever a derivation reaches the same
rule's application point a second time by a different route — e.g. compounding
(`SynthesisCompoundingRule`) combines two independently-derived sub-words, each of which already ran
the realizational rule once on its own branch, or a longer, multi-stratum derivation feeds a word
back through the same stratum again later — nothing revokes or dedupes a prior application, and
`IsBlocked`'s feature-subset test is the only thing standing between that and the realizational
spellout being expressed twice in the combined result. A grammar author reasoning from
`AffixProcessRule`'s default `MaxApplicationCount = 1` would not expect this rule type to need any
extra guard against it.

## Fix

For a `RealizationalAffixProcessRule`, make sure `RequiredSyntacticFeatureStruct` (or the
accumulated `RealizationalFeatureStruct` state it depends on) is specific enough that a second
application is infeasible on its own terms — e.g. require the *absence* of the feature the rule sets,
not merely the presence of the features that license it. Don't rely on an implicit per-rule
application cap for this rule type; unlike `AffixProcessRule`/`CompoundingRule`, none exists.
