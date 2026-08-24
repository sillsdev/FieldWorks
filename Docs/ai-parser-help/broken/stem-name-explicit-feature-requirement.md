---
title: "A stem-name-restricted allomorph needs the feature explicitly assigned, not just compatible"
implements: src/SIL.Machine.Morphology.HermitCrab/StemName.cs, src/SIL.Machine.Morphology.HermitCrab/RootAllomorph.cs
category: lexicon
symptom: silent-misconfiguration
grammar_visible: "no — produces silent, correct-per-the-code parse failures with no obvious cause in the XML"
---

## What it is

Stem names restrict a root allomorph to only be valid when the word's accumulated syntactic
feature structure falls inside one of the stem name's declared regions; any *other* allomorph of
the same lexical entry that has no stem name (or a different one) is only valid **outside** that
region. This is the mechanism behind "principal parts" — a root with an irregular form for one
paradigm cell and a regular form everywhere else.

## The mechanism

A stem name's required-match check tests the word's *current* feature structure against the
stem name's declared region. Because that check tests the feature structure as it actually
stands at the point of checking — not "is this compatible with the region," but "is this
explicitly inside the region" — a bare stem (before any rule has assigned the relevant feature)
does not automatically satisfy a stem name's region, even if the region only mentions one
feature. A form with that feature totally unassigned is outside the region, not inside it.

## Toy example

A root `tam`/`kap` where `kap` is stem-name-restricted to require `pers=1`:

```xml
<StemNames>
  <StemName id="snP1" partsOfSpeech="posV">
    <Regions><Region><AssignedHeadFeatures><FeatureValue feature="featPers" symbolValues="symP1" /></AssignedHeadFeatures></Region></Regions>
  </StemName>
</StemNames>
...
<LexicalEntry id="eRoot" partOfSpeech="posV">
  <Allomorphs>
    <Allomorph id="aDefault"><PhoneticShape>tam</PhoneticShape></Allomorph>
    <Allomorph id="aRestricted" stemName="snP1"><PhoneticShape>kap</PhoneticShape></Allomorph>
  </Allomorphs>
</LexicalEntry>
```

The bare root `kap` alone (no person feature assigned at all) has **zero** valid parses — the
stem-name-restricted allomorph requires `pers=1` to be explicitly present, and an unmarked form
doesn't count, even trivially. Only a rule that actually assigns `pers=1` (e.g. a first-person
agreement rule) makes `kap`-derived forms valid; the bare root, and any form built with a rule
assigning `pers=2`, can only surface as `tam`.

## Gotcha and fix

A grammar author who expects a stem-name-restricted allomorph to be usable "whenever nothing
else says otherwise" will see silent, correct-per-the-code parse failures instead — nothing in a
trace beyond a generic environment/region mismatch points at "you forgot to assign the feature
this region requires." The fix is to make sure every morphological rule that's supposed to
license a restricted stem name actually assigns the specific feature value the region requires,
not just a value that happens to be compatible with it.
