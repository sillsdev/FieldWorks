---
title: "Inflection classes, subclasses, and the default class are MPR features"
implements: src/SIL.Machine.Morphology.HermitCrab/MprFeature.cs, src/SIL.Machine.Morphology.HermitCrab/MprFeatureGroup.cs, src/SIL.Machine.Morphology.HermitCrab/MprFeatureSet.cs, src/SIL.Machine.Morphology.HermitCrab/LexEntry.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AffixProcessAllomorph.cs, src/SIL.Machine.Morphology.HermitCrab/Word.cs
black_sections: "2.1.2.6 Inflection classes; 2.1.2.6.1 Inflection subclasses; 2.1.2.8 Inflection classes versus inflection features; 2.1.6 Exception features"
category: feature-system
when_to_use: "an affix's allomorph choice is lexically arbitrary — not phonologically or syntactically motivated"
---

## One mechanism, several authoring names

Black introduces "inflection classes" (§2.1.2.6, for allomorphy that tracks the lexical stem rather
than phonology or agreement) and "exception features" (§2.1.6, for blocking an affix from stems that
lack a designated tag) as if they were separate FieldWorks features. At the engine level they are
the same primitive: `MprFeature`, grouped into `MprFeatureGroup`s, checked via `MprFeatureSet`.
A stem's tag set lives on `LexEntry.MprFeatures`; an allomorph's gates are
`RequiredMprFeatures`/`ExcludedMprFeatures` (see `AffixProcessAllomorph.cs`, and the analogous fields
on phonological subrules and compounding rules). Whether you're modeling Yalálag Zapotec's two
future-tense allomorph classes, Latin's five declensions, or Orizaba Nahuatl's absolutive-suffix
exception list, you're populating the same `MprFeature`/`MprFeatureGroup` machinery — just for a
different authoring purpose. Recognizing that they're one mechanism matters because it means the
same design discipline applies to both: decide the group's semantics once, before tagging stems and
allomorphs against it.

## The matching mechanics, precisely

`MprFeatureSet.IsMatchRequired` and `IsMatchExcluded` (in `MprFeatureSet.cs`) group the checked
features by their `MprFeatureGroup` and apply the group's `MatchType`:

- `MatchType.All` (or a feature with no group at all): every feature in the group that's on the
  gate must be present on the stem (for `IsMatchRequired`) or absent (for `IsMatchExcluded`).
- `MatchType.Any`: at least one feature in the group must be present/absent.

This is the mechanical form of Black's inflection-class constraint rules (§2.1.2.6.1, rule 29): an
allomorph's `RequiredMprFeatures` is checked against the stem's `MprFeatures` set directly — there's
no separate "compatible with" notion, only "does the required set of features actually appear."

`MprFeatureGroup.Output` (`Overwrite`/`Append`) governs what happens when a rule's `OutMprFeatures`
gets folded into a word's accumulated tag set (`MprFeatureSet.AddOutput`) — `Overwrite` removes any
sibling feature from the same group before adding the new one, `Append` just unions. This only
matters when a *rule* assigns MPR features as an output (not for a bare lexical stem's static tags),
and it's order-dependent in exactly the way
[`speed/mpr-overwrite-order-dependence.md`](../speed/mpr-overwrite-order-dependence.md) describes —
not re-derived here.

## Subclasses: flattened, not hierarchical, at the engine level

`MprFeatureGroup` has no parent/child relationship between its member features — it's a flat set
plus one `MatchType`. There is no engine construct corresponding to "this feature is a subclass of
that one." Black's inflection-subclass behavior (§2.1.2.6.1) — an allomorph tagged only at the main
class level also matches any stem tagged with a *subclass* of that class — therefore cannot be a
runtime hierarchy lookup; it has to be realized as multiple flat tags. Concretely, this means a stem
belonging to "Class 2, Subclass 2A" needs *both* MPR features actually present in its `MprFeatures`
set (the subclass tag and the main-class tag) for a main-class-level `RequiredMprFeatures` gate to
match it, per the mechanics above. If you're reading an exported grammar and a stem carries several
MPR-feature tags that look redundant with each other, that's very likely this flattening — the
"hierarchy" is encoded as co-occurring flat tags, not as a lookup the engine performs.

One practical corollary: when an inflectional affix entry has allomorphs where *some* are tagged
with a subclass and *others* only with a main-level class, every allomorph in that entry needs
tagging consistently (main-level tags need to cover every subclass they're meant to reach) — an
untagged or main-level-only allomorph doesn't "inherit" narrower coverage automatically the way an
untagged natural class inherits nothing narrower either. This is Black's own warning (§2.1.2.6.1,
end) restated in engine terms: it isn't the engine being inconsistent, it's the flat-tag mechanism
having no notion of automatic narrowing.

## The "default" class is resolved before the grammar reaches HermitCrab

FieldWorks lets you designate one inflection class as the default, applied to any stem "not overtly
tagged." That resolution happens on the FieldWorks side, not in the engine: the exporter walks up a
part of speech's own hierarchy looking for a `DefaultInflectionClass` to fall back to for an
untagged stem (this is FieldWorks-side behavior, not part of `sillsdev/machine` — see
`GetDefaultInflClass` in FieldWorks' `Src/LexText/ParserCore/HCLoader.cs`). By the time a stem
reaches the engine, it either already carries an explicit `MprFeature` tag, or it carries none at
all — there's no "untagged means whichever class was marked default" behavior anywhere in
`MprFeatureSet`. If you're authoring a HermitCrab grammar directly (not exporting from FieldWorks),
there is no default-class shortcut available to you: model a "default"/"regular" class as a real,
explicit `MprFeature` and tag every stem that isn't a genuine exception with it, the same way you'd
tag any other class. `RequiredMprFeatures`/`ExcludedMprFeatures` are positive checks against
whatever's actually present — "nothing else claimed this stem" is not a condition the engine can
test for you.

## Inflection classes vs. inflection (agreement) features

Black's Table 9 (§2.1.2.8) reduces to one load-bearing distinction, and it's verifiable directly
from where each mechanism's data lives: `MprFeatureSet` is a field on `Word`/`LexEntry` entirely
separate from `SyntacticFeatureStruct` — an MPR feature never appears in the word's syntactic
feature structure, which is the only thing later syntactic/agreement reasoning (and, within
HermitCrab, any `RequiredSyntacticFeatureStruct`/`RequiredHeadFeatures` gate) can see. So:

- If the distinction is purely about which allomorph an affix uses, with no meaning difference and
  no syntax needs to know about it (declension class, conjugation class, arbitrary allomorphy) →
  `MprFeature`/`MprFeatureGroup`. It's invisible outside rule-internal gating, by construction.
- If the distinction has real meaning, changes which affixes can co-occur via *agreement*, or needs
  to be visible for downstream syntactic processing (gender, person, number, noun class as a
  syntactic category) → a real value in the `SyntacticFeatureSystem`, carried on
  `SyntacticFeatureStruct` and checked via `RequiredHeadFeatures`/`RequiredSyntacticFeatureStruct`.

Picking `MprFeature` for something that actually needs to participate in agreement will parse
correctly in isolation but leaves nothing for a later syntax-level or cross-word agreement check to
read — the tag is real to the rule that gated on it, and invisible to everything else.
