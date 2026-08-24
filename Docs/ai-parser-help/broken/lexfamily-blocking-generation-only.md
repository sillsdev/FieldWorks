---
title: "LexFamily suppletion blocking silently discards a regular derivation, and only during generation"
implements: src/SIL.Machine.Morphology.HermitCrab/Word.cs, src/SIL.Machine.Morphology.HermitCrab/LexFamily.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisAffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisCompoundingRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisRealizationalAffixProcessRule.cs
category: lexicon
symptom: missing-parse
grammar_visible: partially
---

## What it is

HermitCrab supports "blocking": when a regularly-derived word would be replaced by a suppletive
family member's own irregular form (classic "*goed* is blocked by *went*"). This mechanism is
controlled by `AffixProcessRule.Blockable` / `RealizationalAffixProcessRule.Blockable` /
`CompoundingRule` (each initialized to `true` in its constructor — e.g.
`MorphologicalRules/AffixProcessRule.cs:29`). It has no corresponding check on the analysis side at
all, and its trigger condition is a feature-structure subsumption test that's easy to get broader
than intended.

## The mechanism

After a rule derives a word, `Word.CheckBlocking` (`Word.cs:472-497`) runs:

```csharp
LexFamily family = ((LexEntry)RootAllomorph.Morpheme).Family;
if (family == null) return false;
foreach (LexEntry entry in family.Entries)
{
    if (entry != RootAllomorph.Morpheme
        && entry.Stratum == Stratum
        && SyntacticFeatureStruct.Subsumes(entry.SyntacticFeatureStruct))
    {
        word = new Word(entry.PrimaryAllomorph, RealizationalFeatureStruct.Clone()) { ... };
        return true;
    }
}
```

If the just-derived word's own feature structure `Subsumes` some other family member's feature
structure, the derived word is discarded outright and replaced by that other entry's primary
allomorph. This is called from `SynthesisAffixProcessRule.cs:198`,
`SynthesisCompoundingRule.cs:192`, and `SynthesisRealizationalAffixProcessRule.cs:127`, each gated
by `if (_rule.Blockable && outWord.CheckBlocking(out Word newWord))` — and `Blockable` defaults to
`true` for every rule type unless a grammar author explicitly turns it off.

Two things make this a correctness trap rather than just "suppletion working as intended":

1. **It's synthesis-only.** There is no equivalent check anywhere in `AnalysisAffixProcessRule.cs`,
   `AnalysisCompoundingRule.cs`, or `LexEntry.cs`/`LexFamily.cs`. Blocking suppresses a candidate
   during generation but has no bearing on what parses during analysis — the two directions are not
   symmetric for a family with a suppletive member.
2. **Subsumption, not equality, decides it.** `Subsumes` succeeds whenever the derived word's
   feature structure is *at least as specific as* the family member's — so a family member with a
   deliberately broad or under-specified feature structure (e.g. left with an unassigned feature
   that was meant to narrow it to one paradigm cell) can end up blocking derivations well beyond the
   single irregular cell the grammar author intended to model.

## Concrete example

A `LexFamily` groups `go` (regular root, produces `go+ed` via a regular past-tense
`AffixProcessRule`) and `went` (irregular root, `partOfSpeech=posV`, own feature structure just
`{tense=past}`, no other features assigned). Both entries share a stratum. Any regularly-derived
past-tense form of `go` — whatever its full feature structure ends up being — subsumes `went`'s bare
`{tense=past}` requirement (an unspecified feature structure is a subsumer of anything more specific
in HC's feature system), so `Word.CheckBlocking` fires on every one of them: the regular
`go`+`-ed` derivation is silently discarded and replaced by `went` every single time, not just for
the one paradigm cell the author meant to override.

## Fix

- Set `blockable="false"` on a rule if you want its output to survive regardless of family
  suppletion.
- If suppletion should only block one specific paradigm cell, give the irregular family member's
  own feature structure exactly that cell's features (not a bare/under-specified structure) so
  `Subsumes` can't match broader derivations than intended.
- Do not assume blocking constrains parsing — a form that blocking would suppress during generation
  can still be accepted as a valid analysis when parsing the same surface string, because
  `CheckBlocking` never runs on the analysis side.
