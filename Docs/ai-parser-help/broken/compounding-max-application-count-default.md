---
title: "Compounding is capped at one application per derivation by default, and exocentric compounding cannot be configured otherwise"
implements: src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/CompoundingRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisCompoundingRule.cs
category: compounding
symptom: missing-parse
grammar_visible: partially
---

## What it is

`CompoundingRule.MaxApplicationCount` defaults to `1` (`CompoundingRule.cs:19`), enforced the same
way an `AffixProcessRule`'s cap is: `SynthesisCompoundingRule.Apply` refuses to apply the rule again
once `input.GetApplicationCount(_rule) >= _rule.MaxApplicationCount`
(`SynthesisCompoundingRule.cs:50-62`). A grammar author modeling a language that allows more than two
elements in a compound (e.g. a three-noun compound built by applying the same compounding rule
twice) gets exactly one level of compounding per derivation unless something explicitly raises this
cap — and FieldWorks's compiler only gives *endocentric* compound rules a way to do that.

## The mechanism

FieldWorks's `HCLoader.LoadEndoCompoundingRule` looks up a per-rule maximum application count from a
separate `parserParams` XML document (not the main HC grammar XML), keyed by the compound rule's
GUID:

```csharp
int maxApps = 1;
if (m_CompoundRuleLookup.TryGetValue(compoundRule.Guid.ToString(), out maxApps))
    hcCompoundRule.MaxApplicationCount = maxApps;
```

(`HCLoader.cs:1894-1896`), populated earlier from a `<CompoundRules>` element's `maxApps` attributes
(`HCLoader.cs:104-111`). `LoadExoCompoundingRule` (`HCLoader.cs:1922` onward) has no equivalent
lookup anywhere in its body — every `CompoundingRule` it builds keeps the C# constructor's default of
`MaxApplicationCount = 1`, with no code path that could raise it. An exocentric compound rule (the
kind that produces a category not inherited from either the head or non-head component, e.g. a
noun-noun compound that becomes a distinct new nominal category) can therefore never recurse more
than once per derivation in a FieldWorks-generated grammar, regardless of any setting a grammar
author might look for.

## Why this is easy to miss

The cap is invisible from the main HC grammar XML — `MaxApplicationCount` shows up on the
`CompoundingRule` element the same way it would for any other rule, and a reader checking "did the
author configure this to recurse" has to know to look in a *different*, parser-parameters document
(the one `m_CompoundRuleLookup` is built from) rather than the grammar itself, and to know that this
lookup is only ever consulted for endocentric rules. A grammar author who wants three-way (or deeper)
compounding, and who successfully configures a higher `maxApps` for an endocentric rule, can
reasonably expect the same configuration surface to work for an exocentric rule modeling the same
kind of recursive compounding — it does not, because the loading code for exocentric rules never
reads that lookup at all.

## Concrete example

A language allows noun compounds of arbitrary length (`N N N ... N`), modeled as one exocentric
compounding rule (the output category isn't simply "the same as the head," so it's modeled as
exocentric) meant to apply repeatedly, left-associatively, to build up longer compounds one pair at a
time. Because `LoadExoCompoundingRule` never overrides `MaxApplicationCount`, the compiled rule stays
capped at `1` — only genuine two-element compounds are generable or parseable; any three-or-more-noun
compound the grammar author expected to derive by reapplying the same rule silently fails to parse or
generate, with no configuration surface (short of switching the rule to an endocentric model, if the
category semantics allow it) to fix it.

## Fix

If a compounding pattern needs to recurse more than once per derivation, model it as an *endocentric*
compound rule (where the output category is inherited from the head) so FieldWorks's `maxApps`
lookup applies, and configure that count explicitly in the parser-parameters settings rather than
assuming the default is unlimited or that it matches whatever an affix rule's default would be. For a
genuinely exocentric recursive-compounding pattern, there is currently no configuration surface in
the FieldWorks-to-HC compiler to raise the cap above `1` — the underlying `CompoundingRule.MaxApplicationCount`
property itself has no such restriction (a hand-edited HC XML grammar can set it directly), only the
FieldWorks loader path for exocentric rules never populates it from anything but the default.
