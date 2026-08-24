---
title: "A stem with no inflection class and no configured default silently fails every class-restricted rule"
implements: src/SIL.Machine.Morphology.HermitCrab/MprFeatureSet.cs, FieldWorks Src/LexText/ParserCore/HCLoader.cs
category: feature-system
symptom: missing-parse
grammar_visible: partially
---

## What it is

FieldWorks lets a grammar author set a "default inflection class" for a part of speech; H. Andrew
Black's conceptual intro (around the discussion of default inflection class, roughly line 1224 in
the full-text source) states that when this default is configured, "the FieldWorks Language Explorer
parser will use this default inflection class for any stem that is not overtly tagged" — implying
that when no default is configured, an untagged stem gets nothing, silently. FieldWorks's HC loader
confirms exactly this fallback chain, and the HC engine's MPR-feature matching confirms exactly what
happens downstream when it comes up empty.

## The mechanism

FieldWorks's loader resolves a stem's inflection class with:

```csharp
protected static IMoInflClass GetInflClass(IMoStemMsa msa)
{
    if (msa.InflectionClassRA != null)
        return msa.InflectionClassRA;
    if (msa.PartOfSpeechRA != null)
        return GetDefaultInflClass(msa.PartOfSpeechRA);
    return null;
}
```

walking up the part-of-speech hierarchy for a configured `DefaultInflectionClassRA` if the stem
itself isn't tagged, and returning `null` if neither the stem nor any ancestor POS has one. When
`GetInflClass` returns `null`, the corresponding `MprFeatures.Add(...)` call for inflection class is
simply skipped — the compiled HC lexical entry gets **no** inflection-class MPR feature at all, not
a "default" placeholder feature.

On the engine side, a rule restricted to specific inflection classes expresses that via
`RequiredMprFeatures`, checked through `MprFeatureSet.IsMatchRequired`
(`MprFeatureSet.cs:46-70`): for an ungrouped required feature (or a group with `MatchType.All`), the
check is `group.Any(mf => !mprFeats.Contains(mf))` — if the stem's compiled MPR-feature set doesn't
contain the required class feature at all (because it was never added), the match fails
unconditionally. There is no "unmarked stems match everything" or "unmarked stems get treated as the
elsewhere case" behavior anywhere in this check — a stem with no inflection-class feature simply
fails every rule gated on any inflection class.

## Why this is easy to miss

Nothing in the grammar's rule XML says "this rule requires the stem to have been assigned an
inflection class" in those words — it just lists `RequiredMprFeatures` referencing specific class
IDs, which reads the same whether or not every stem in the lexicon is guaranteed to carry one of
them. A grammar author who sets up inflection classes for the *irregular* subset of a POS and assumes
"everything else falls through to some sensible default" gets that behavior only if they remembered
to configure `DefaultInflectionClassRA` on the POS (or an ancestor POS) — if they didn't, every stem
they didn't explicitly tag silently fails every class-restricted rule, with no trace signal beyond an
ordinary MPR-feature mismatch that looks identical to "this stem really is the wrong class."

## Concrete example

A POS `posN` has inflection classes `classI` (a handful of irregular nouns, explicitly tagged) and no
configured default. A plural-suffix rule requires `classI` OR requires "not classI" via
`ExcludedMprFeatures`, depending on how the two paradigms were modeled; either way, the majority of
`posN` stems were never tagged with any inflection class at all (the author assumed "untagged = the
regular pattern"). Every one of those untagged stems fails the class-gated rule that was supposed to
be their regular paradigm, because their compiled MPR-feature set has no inflection-class feature to
match against — not because they matched the wrong class, but because they matched no class.

## Fix

Either configure an explicit default inflection class on the POS (or the relevant ancestor POS) so
every untagged stem actually receives a class feature, or design class-restricted rules so the
"regular"/default paradigm's rule has no inflection-class requirement at all (only the irregular
classes are gated), so an absent class feature can't accidentally exclude the majority case.
