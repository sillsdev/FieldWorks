---
title: "A null/zero-realization affix is a real rule application, not a free default value"
implements: src/SIL.Machine.Morphology.HermitCrab/AffixTemplateSlot.cs, src/SIL.Machine.Morphology.HermitCrab/SynthesisAffixTemplateRule.cs
category: morphotactics
symptom: wrong-parse
grammar_visible: yes
---

## What it is

H. Andrew Black's FLEx/HC conceptual intro (§2.1.2.2, "Optional affix slots") points out that some
categories have a "default" value realized by zero marking (e.g. singular number, unmarked when
plural is overtly suffixed) and states plainly that "the current parsers do not allow us to mark
such default features" — a null/zero affix is not the same thing as an actual default. This is
verifiable directly in the engine's slot-optionality logic.

## The mechanism

`AffixTemplateSlot.Optional` only auto-derives `true` when the slot has **no rules at all**:

```csharp
public bool Optional
{
    get
    {
        if (_rules.Count == 0)
            return true;
        return _isOptional;
    }
    set { _isOptional = value; }
}
```

(`AffixTemplateSlot.cs:35-45`). A slot holding one rule — even a rule whose only allomorph spells
out nothing overt (a "null suffix") — is **not** automatically optional; its `Optional` flag is
whatever the grammar author set explicitly, and if the author left it non-optional (the natural
choice for "this category is always marked, sometimes by zero"), `SynthesisAffixTemplateRule.ApplySlots`
requires that rule to actually apply, successfully, before the derivation can proceed past that slot:

```csharp
foreach (Word outWord in _rules[i].Apply(input))
    ApplySlots(outWord, i + 1, output);
if (!_template.Slots[i].Optional)
    return;   // no successful application at this (mandatory) slot -> dead end
```

(`SynthesisAffixTemplateRule.cs:39-49`). A null-realization rule still goes through the same
application machinery as any other affix rule — its `RequiredSyntacticFeatureStruct` must unify, any
`RequiredMprFeatures`/environment/stem-name gates on it must be satisfied, and so on. If the
conditions that license the null affix's rule aren't met for some input, the mandatory slot has
nothing to apply and the whole derivation dead-ends at that slot — it does not fall through to "just
assume the default value," because there is no such fallback in the engine.

## Why this is easy to miss

The zero-marking case *looks* like exactly what a default feature value should be: nothing is
written, nothing seems constrained. But because the null affix is implemented as an ordinary rule
occupying a non-optional slot, every one of that rule's own gates (required features, MPR features,
environment) still has to pass. A form that should trivially take "the default" because none of the
overt alternatives apply can instead fail to derive at all, if the null rule's own required
conditions don't happen to be satisfied for that particular stem — there is no separate "otherwise
assume singular" fallback path.

## Concrete example

A noun category has number marked by `-s` (plural) vs. a null suffix (singular), both rules placed
in one non-optional template slot. If the null-suffix rule is written with its own
`RequiredSyntacticFeatureStruct` (e.g. requiring `num=sg` to already be set elsewhere) rather than as
the true, condition-free elsewhere case, any noun stem for which `num` hasn't already been
independently assigned by an earlier rule fails at this slot entirely — neither the `-s` rule nor the
null rule can apply, and the whole derivation dies, when the grammar author's mental model was "no
suffix just means singular by default."

## Fix

If a category's "default" is a zero realization, still model it as a real, unconditioned rule (no
`requiredSyntacticFeatureStruct` beyond the bare POS, no MPR-feature gate that could fail) so it acts
as a true elsewhere case within its slot — or, per Black's suggested workaround, mark the slot
optional and have every overt affix *set* the non-default feature value explicitly, leaving the
feature unset (rather than null-affixed) when the slot doesn't fire, and have downstream rules or
default unification handle the "otherwise assume the default" logic instead of relying on the
template slot mechanism to express it.
