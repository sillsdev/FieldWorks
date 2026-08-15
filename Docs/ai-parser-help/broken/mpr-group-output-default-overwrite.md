---
title: "An MPR feature group's output type defaults to Overwrite, not Append"
implements: src/SIL.Machine.Morphology.HermitCrab/MprFeatureGroup.cs, src/SIL.Machine.Morphology.HermitCrab/MprFeatureSet.cs, src/SIL.Machine.Morphology.HermitCrab/XmlLanguageLoader.cs, src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd
category: feature-system
symptom: silent-misconfiguration
grammar_visible: partially
---

## What it is

`MprFeatureGroup.Output` (`MprFeatureGroup.cs:79`) is an `MprFeatureGroupOutput` with values
`Overwrite` ("overwrites all existing features in the same group") and `Append` ("appends
features") (`MprFeatureGroup.cs:26-37`). When a group's XML omits the `outputType` attribute, both
the DTD and the C# loader default it to `Overwrite`, not `Append` — the non-monotone, order-dependent
behavior described in `speed/mpr-overwrite-order-dependence.md` is what an omitted attribute actually
gets, not the safer accumulating behavior a reader of that file's own wording ("declared with output
policy Overwrite instead of the default Append") might assume the loader falls back to.

## The mechanism

The DTD states the default explicitly:

```
<!ATTLIST MorphologicalPhonologicalRuleFeatureGroup
  isActive (yes | no) "yes"
  matchType (any | all) "any"
  outputType (overwrite | append) "overwrite"
  features IDREFS #REQUIRED
>
```

(`HermitCrabInput.dtd:82-87`). The loader's fallback agrees:

```csharp
private static MprFeatureGroupOutput GetGroupOutput(string outputTypeStr)
{
    switch (outputTypeStr)
    {
        case "overwrite": return MprFeatureGroupOutput.Overwrite;
        case "append": return MprFeatureGroupOutput.Append;
    }
    return MprFeatureGroupOutput.Overwrite;
}
```

(`XmlLanguageLoader.cs:108-119`) — an omitted, empty, or unrecognized `outputType` string falls
through to `Overwrite`. That default feeds directly into `MprFeatureSet.AddOutput`
(`MprFeatureSet.cs:29-44`), which is what every rule application runs on its output MPR features
(both ordinary affix-process allomorphs and compounding subrules): for a group whose `Output` is
`Overwrite`, applying a new rule's output silently drops any of that group's features the word
already carried unless the new output restates them — "last rule to touch this group wins," not
accumulation.

## Why this is easy to miss

A grammar author who reasons "MPR features are just tags, and tags accumulate unless I say
otherwise" gets the *opposite* of that reasonable assumption the moment they define a
`MorphologicalPhonologicalRuleFeatureGroup` and don't set `outputType` — which is the common case,
since `outputType` only matters once a grammar author is deliberately grouping features, at which
point `Append` (the semantically "boring," accumulating choice) looks like it should need no
attribute at all. Nothing about the group's own declaration signals this; the non-monotone behavior
only becomes visible once two different rules in a derivation touch the same group and a grammar
author notices one rule's tag vanished from the word's final state. See
`speed/mpr-overwrite-order-dependence.md` for the full consequences of `Overwrite` semantics once
they're in effect (order-dependence, and its interaction with the engine's ability to collapse
otherwise-equivalent candidate derivations) — this gotcha is specifically about how easy it is to end
up with `Overwrite` unintentionally, by omission, rather than the mechanism's downstream effects.

## Concrete example

A grammar groups MPR features `tagX` and `tagY` under one `MorphologicalPhonologicalRuleFeatureGroup`
with no `outputType` attribute, intending them as independent accumulating flags checked later by an
`ExcludedMprFeatures` gate elsewhere. Rule A applies first and outputs `tagX`; rule B applies later
in the same derivation and outputs `tagY`. Because the group defaults to `Overwrite`, rule B's
application removes `tagX` from the word's MPR-feature set before adding `tagY` — the final word
carries only `tagY`, even though both rules fired. A downstream gate checking for `tagX` fails
silently, exactly as if rule A had never applied.

## Fix

Set `outputType="append"` explicitly on any `MorphologicalPhonologicalRuleFeatureGroup` whose
features are meant to accumulate across a derivation. Only rely on the omitted-attribute default when
"last rule to touch this group wins" is the actually-intended semantics (e.g. a group modeling a
paradigm cell that a later derivation step is meant to reset outright).
