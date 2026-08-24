---
title: "An MPR feature group's match type defaults to Any (OR), not All — the opposite of what a gating author might assume"
implements: src/SIL.Machine.Morphology.HermitCrab/MprFeatureGroup.cs, src/SIL.Machine.Morphology.HermitCrab/MprFeatureSet.cs, src/SIL.Machine.Morphology.HermitCrab/XmlLanguageLoader.cs, src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd
category: feature-system
symptom: silent-misconfiguration
grammar_visible: partially
---

## What it is

`MprFeatureGroup.MatchType` (`MprFeatureGroup.cs:73`) is an `MprFeatureGroupMatchType` with values
`Any` ("when any features match within the group") and `All` ("only if all features match within the
group") (`MprFeatureGroup.cs:10-21`). When a grammar's `MorphologicalPhonologicalRuleFeatureGroup`
XML omits the `matchType` attribute, the loader — and the format's own DTD — both default it to
`Any`, not `All`. A grammar author reasoning "an unmarked/omitted setting should be the strict,
conjunctive one" gets the loose, disjunctive one instead, silently.

## The mechanism

The DTD itself states the default explicitly:

```
<!ATTLIST MorphologicalPhonologicalRuleFeatureGroup
  isActive (yes | no) "yes"
  matchType (any | all) "any"
  outputType (overwrite | append) "overwrite"
  features IDREFS #REQUIRED
>
```

(`HermitCrabInput.dtd:82-87`). The C# loader's fallback agrees:

```csharp
private static MprFeatureGroupMatchType GetGroupMatchType(string matchTypeStr)
{
    switch (matchTypeStr)
    {
        case "all": return MprFeatureGroupMatchType.All;
        case "any": return MprFeatureGroupMatchType.Any;
    }
    return MprFeatureGroupMatchType.Any;
}
```

(`XmlLanguageLoader.cs:95-106`) — any omitted, empty, or unrecognized `matchType` string falls
through to `Any`. This matches the enum's own declared order (`Any` is the first, zero-valued
member), so even a hypothetical uninitialized `MprFeatureGroup` would default the same way.

The consequence at match time, `MprFeatureSet.IsMatchRequired` (`MprFeatureSet.cs:46-70`):

```csharp
if (group.Key == null || group.Key.MatchType == MprFeatureGroupMatchType.All)
{
    if (group.Any(mf => !mprFeats.Contains(mf))) { mismatchGroup = group.Key; return false; }
}
else // Any
{
    if (group.All(mf => !mprFeats.Contains(mf))) { mismatchGroup = group.Key; return false; }
}
```

For an `All`-type group, the check fails as soon as *any one* referenced feature is missing from the
word's accumulated set — every listed feature must be present. For an `Any`-type group (the default),
the check only fails if *every* referenced feature is missing — a single one present is enough to
satisfy the whole group. (Note this also means an MPR feature with no group at all — `group.Key ==
null` — is always treated with `All` semantics, i.e. as its own singleton conjunctive requirement;
the `Any` default only applies to features that are actually placed in a declared group.)

## Why this is the opposite trap from the intuitive one

It would be easy to assume the risk runs the other way — that a grammar author wanting OR semantics
("any one of these MPR features suffices to license this rule") has to opt in and might forget to,
silently landing on stricter AND gating instead. The verified default runs the other direction: the
default is already `Any`. The actual silent-misconfiguration risk is for an author who *wants*
conjunctive gating — "all of these MPR features must be present together for this rule to apply" —
and doesn't realize that leaving `matchType` unset does not give them that. Any single feature in the
group being present is enough, which can silently *under*-constrain a rule that was meant to require
several co-occurring MPR features at once.

## Concrete example

A grammar groups MPR features `hasPrefixA` and `hasPrefixB` into one group meant to gate a rule that
should only apply when a stem carries *both* prefixes' tags (e.g. a portmanteau-blocking condition).
The group's XML omits `matchType`. Because the default is `Any`, a stem carrying only `hasPrefixA`
(not `hasPrefixB`) already satisfies `RequiredMprFeatures` for that group — the rule applies to stems
the author only meant to license when both tags co-occurred.

## Fix

Set `matchType="all"` explicitly whenever a group is meant to require every one of its features
together — do not rely on the omitted-attribute default, which is `any`. Conversely, if `any`
(OR) semantics really is what you want, it's already the default and you don't need to state it, but
stating it explicitly still makes the grammar's intent legible to the next person reading the XML.
