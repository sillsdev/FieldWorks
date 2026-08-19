---
title: "A multi-morpheme co-occurrence exclusion requires all listed morphemes together — not any one of them"
implements: src/SIL.Machine.Morphology.HermitCrab/MorphCoOccurrenceRule.cs, src/SIL.Machine.Morphology.HermitCrab/Allomorph.cs
category: morphotactics
symptom: wrong-parse
grammar_visible: partially
---

## What it is

FLEx's "ad hoc co-occurrence" mechanism lets a grammar author pick a key morpheme (or allomorph) and
list one or more *other* morphemes/allomorphs that must or must not co-occur with it
(`IMoMorphAdhocProhib`/`IMoAlloAdhocProhib` in FieldWorks, compiled to
`MorphemeCoOccurrenceRule`/`AllomorphCoOccurrenceRule` in the HC engine). When that "others" list has
more than one entry, a grammar author modeling "the key morpheme must not co-occur with A, or with B,
or with C" (three separate, independent exclusions) by listing A, B, and C in one rule gets something
much weaker: the exclusion only fires when **all** of A, B, and C appear together with the key
morpheme in the same word. Any word containing the key morpheme plus only one or two of the listed
others is not excluded at all.

## The mechanism

`MorphCoOccurrenceRule<T>.IsWordValid` (`MorphCoOccurrenceRule.cs:82-87`) is:

```csharp
public bool IsWordValid(T key, Word word)
{
    if (_type == ConstraintType.Exclude)
        return !CoOccurs(key, word);
    return CoOccurs(key, word);
}
```

and `CoOccurs` (`MorphCoOccurrenceRule.cs:92-170`) walks the word's morphs (in the adjacency order
the rule specifies — `Anywhere`, `SomewhereToLeft/Right`, or `AdjacentToLeft/Right`) removing each
`others` entry from a working copy of the list as it's matched, and returns `others.Count == 0` at
the end — true only if **every** entry in `_others` was found. For `ConstraintType.Exclude`,
`IsWordValid` negates that: the word is invalid only when `CoOccurs` returns true, i.e. only when
*all* of the listed morphemes were present together with the key. If the list has three entries and
a word contains the key plus only one of them, `CoOccurs` returns false (not every entry was matched),
so `IsWordValid` (Exclude) returns `true` — the word is accepted, not rejected.

This is not a quirk of how the rule happens to get built from a single-item FieldWorks list — it's
the same class regardless of list length, and FieldWorks does build multi-item lists directly into
one rule's `others` set. `HCLoader.LoadAllomorphCoOccurrenceRules`/`LoadMorphemeCoOccurrenceRules`
each take one `IMoAlloAdhocProhib`/`IMoMorphAdhocProhib`'s entire `RestOfAllosRS`/`RestOfMorphsRS`
reference collection (FieldWorks's "Rest of Allomorphs"/"Rest of Morphemes" field) and pass it as the
`others` list to a single `new AllomorphCoOccurrenceRule(ConstraintType.Exclude, others, adjacency)`/
`new MorphemeCoOccurrenceRule(...)` call — one rule per adhoc-prohibition entry, not one rule per
listed morpheme (`HCLoader.cs:2163-2239`). A FieldWorks user who adds three morphemes to one
prohibition's "Rest of" field, expecting three independent exclusions, gets one rule whose `others`
list has three entries and whose `Exclude` semantics only fire on their joint co-occurrence.

## Why this is easy to miss

The FLEx UI field is a plural reference list ("Rest of Allomorphs"), which reads naturally as "any of
these" when the grammar author is modeling several independent things the key morpheme shouldn't
combine with — the field's own name doesn't distinguish "all of these together" from "any one of
these." Nothing in the exported HC XML makes the distinction more obvious either: the rule just lists
several morpheme IDs, and a reader has to already know `MorphCoOccurrenceRule`'s all-or-nothing
matching semantics to recognize that a three-item exclusion list is far weaker than three separate
one-item lists would be.

## Concrete example

A grammar wants to say "the passive suffix `-en` cannot co-occur with any of the three
object-agreement suffixes `-a`, `-i`, `-u`" — three independent, pairwise exclusions. Modeled as one
`IMoMorphAdhocProhib` with `FirstMorphemeRA = -en` and `RestOfMorphsRS = {-a, -i, -u}`, the compiled
rule's `others` list is `[-a, -i, -u]`, and `CoOccurs` only returns true (triggering the `Exclude`)
when a word contains `-en` together with `-a` *and* `-i` *and* `-u` all at once — a combination that
may never even be otherwise derivable. Any word with `-en` plus just `-a` (the actually-intended,
common case to reject) passes this rule with no complaint, because two of the three required
`others` entries are missing from that particular word.

## Fix

Model each independent pairwise (or n-ary "must not co-occur with this specific one") exclusion as
its own separate ad hoc prohibition entry, with exactly one morpheme/allomorph in its "Rest
of"/`RestOfAllosRS`/`RestOfMorphsRS` list. Only put more than one entry in a single prohibition's
"Rest of" list when the intent genuinely is "excluded only when all of these co-occur together" —
that is the only semantics `MorphCoOccurrenceRule` gives a multi-item list.
