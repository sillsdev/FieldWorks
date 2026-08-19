---
title: "Modeling a discontinuous morpheme as two independent affixes loses the engine's atomic-circumfix guarantee"
implements: src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AffixProcessRule.cs, FieldWorks Src/LexText/ParserCore/HCLoader.cs
category: morphotactics
symptom: wrong-parse
grammar_visible: yes
---

## What it is

H. Andrew Black's FLEx/HC conceptual intro (§4.3 "Circumfixes"; process-rule mechanics in §6.1.1.3
"Circumfixation as a process") describes two ways to model a discontinuous morpheme whose prefix
part and suffix part always co-occur as a single meaning: (a) one atomic circumfix lexical entry, or
(b) two independent affix entries, one placed in a prefix slot and one in a suffix slot of the same
template. HC's engine genuinely supports circumfixation as one of its native affix processes
(`AffixProcessRule.cs:14-17` documents "prefixation, suffixation, infixation, circumfixation,
simulfixation, reduplication, and truncation" as the process types one rule can express), and
FieldWorks's loader compiles a circumfix lexical entry into a single, atomic `AffixProcessAllomorph`
whose pattern spans the stem with the prefix part on one side and the suffix part on the other
(`LoadCircumfixAffixProcessAllomorph`, FieldWorks `Src/LexText/ParserCore/HCLoader.cs:1273`). Option
(b) — two separate affixes — does not get this atomicity, and the engine has no other mechanism that
supplies it.

## The mechanism

A circumfix loaded as one `AffixProcessAllomorph` (option a) either applies as a whole — inserting
both the prefix and suffix material together, gated by one shared set of required
features/environment/MPR conditions — or does not apply at all. There is no way for "half a
circumfix" to appear in a derivation, because the engine only ever sees one rule/allomorph unit for
it.

Modeled as two ordinary `AffixProcessRule`s in independent template slots (option b), each half is
an entirely separate `Morpheme`/`AffixProcessRule` with its own `RequiredSyntacticFeatureStruct`,
its own environment, its own MPR-feature gates — nothing in the engine ties their applicability
together. If the two rules' gates aren't kept in exact lockstep by the grammar author (same required
features, same MPR-feature requirements, same environment conditions restated on both), the engine
can apply one half without the other: the prefix rule's slot fires while the suffix rule's slot (in
the same optional-slot cross-multiplication described in the companion single-template gotcha) does
not, or vice versa — see `single-template-independent-slots-illegal-combos.md` for the exact
mechanism by which independent optional slots explore every combination, including one-sided ones.

## Concrete example

A causative meaning is realized as a circumfix `ka-...-an` in one language design. Modeled as two
independent affixes — a prefix rule for `ka-` and a suffix rule for `-an`, each in its own optional
template slot, each carrying the same `cause=true` required/output feature by the author's intent —
a later grammar edit that updates the prefix rule's required feature structure (e.g. narrowing it to
a particular verb subclass) without making the identical edit to the suffix rule's required feature
structure silently breaks the coupling: forms with `ka-` but no `-an`, or `-an` but no `ka-`, become
derivable/parseable, even though the language never realizes the causative as anything but the whole
circumfix. Modeled instead as one atomic circumfix entry, the same edit to "narrow which subclass
gets the causative" only has one place to make it, and there is no way to get a half-realized
circumfix out of the engine.

## Fix

Model a true discontinuous morpheme (one meaning realized as two non-adjacent surface parts that
always co-occur) as a single circumfix lexical entry/`AffixProcessAllomorph`, not as two independent
affixes in two template slots. If two independent affixes are used anyway (e.g. because the two
parts have genuinely independent distributions in some contexts), keep every gating condition
(required features, MPR features, environment) that couples them duplicated exactly and re-verified
on every edit to either rule — the engine provides no shared-identity mechanism to keep them in sync
automatically.
