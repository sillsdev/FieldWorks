---
title: "Optional slots, null affixes, and multiple templates model different facts"
implements: src/SIL.Machine.Morphology.HermitCrab/AffixTemplateSlot.cs, src/SIL.Machine.Morphology.HermitCrab/SynthesisAffixTemplateRule.cs, src/SIL.Machine.Morphology.HermitCrab/AffixTemplate.cs, src/SIL.Machine.Morphology.HermitCrab/LexEntry.cs
black_sections: "2.1.2.2 Optional affix slots; 2.1.2.3 Multiple templates; 3.6 Morphemes that may be null; 4.1.1 Null allomorphs"
category: morphotactics
when_to_use: "a paradigm cell has no overt marker in some forms and you're choosing how to model the absence"
---

Black lays out three options for "this paradigmatic position sometimes has nothing overt" (§2.1.2.2):
mark the slot optional, split into multiple templates, or give the affix a null allomorph. These
aren't interchangeable defaults — each licenses a different set of word forms, and picking the
wrong one either blocks legitimate words or accepts illegitimate ones. This page is about which fact
each choice actually encodes; for the *performance* cost of optional slots specifically, see
[`speed/affix-template-optional-slots.md`](../speed/affix-template-optional-slots.md) — that page's
`O(2^n)` analysis isn't repeated here.

## Optional slot: "this position can be genuinely absent"

Marking a slot optional means the template can produce a well-formed word with nothing in that slot
at all — the feature that slot would have set is never assigned, not "set to a default." An
optional slot is the right choice only when nothing downstream needs a definite value for whatever
feature that slot would carry, and when the slot's absence is unconditional (not tied to what other
slots in the same template did or didn't fill).

That last condition is easy to violate silently. Black's own worked example (Orizaba Nahuatl present
intransitive, §2.1.2.3) shows the failure directly: a subject prefix and a plural suffix look, at
first glance, like a single template with the suffix slot optional (subject prefixes are obligatory,
number is optional, defaulting to singular). But `ti-` is genuinely ambiguous on its own between
2sg.subject and 1pl.subject. If Number is just one optional slot in the same template as Subject,
the engine has no way to make "the plural suffix is required *whenever the subject prefix chosen was
one of the plural set*" — an optional slot is a single, global toggle; it can't condition its own
optionality on which specific rule filled a *different* slot in the same derivation. The result is
that `timiki` (`ti-miki`, "you(sg) die") would also parse as `1pl.sbj-die`, and `timikih` would
additionally parse as `2sg.sbj-die-pl` — both spurious.

## Multiple templates: "two branches of the paradigm have different obligation structure"

The fix is not a smarter single slot — it's two templates, one used only for singular subjects, one
only for plural, each with its *own* slot-obligation pattern: the plural template makes the Number
slot obligatory, so a plural subject prefix can never surface without the plural suffix; the
singular template has no Number slot to omit at all. This is the general shape of the choice: reach
for multiple templates when what differs between two cells of the paradigm isn't just *which* affix
fills a slot, but *whether a slot is optional or obligatory in this branch*. A single template with
independently optional slots cannot express that distinction — it can only license or forbid a slot
uniformly across every word that reaches it, which is exactly what lets an illegitimate combination
through.

The same reasoning applies to Black's discontinuous-morpheme case (§2.1.2.4): a future tense
realized as a prefix *and* a suffix that must co-occur is modeled as one template with both slots
obligatory, forcing "if the future prefix appears, so must the future suffix, and vice versa" — a
fact one optional slot has no way to state, because optionality is a per-slot property, not a
per-combination one. (For the alternative — modeling the discontinuous exponent as a single
morpheme instead of two co-occurring ones — see
[`circumfixes-and-discontinuous-morphemes.md`](circumfixes-and-discontinuous-morphemes.md).)

## Null allomorph: "this cell has a feature value that must be set, spelled with zero segments"

A null allomorph is a real lexical entry (or a real allomorph of one) whose phonetic shape is empty,
occupying an otherwise-obligatory slot. Choose this over an optional slot precisely when something
*downstream* needs the feature that slot carries to have an actual, assigned value — because an
optional slot that's skipped leaves the feature completely unset, and unset is not the same as "set
to the default value" anywhere else in the engine. This is the same distinction covered from the
allomorph side in
[`broken/stem-name-explicit-feature-requirement.md`](../broken/stem-name-explicit-feature-requirement.md):
a stem-name region (or any rule with a `RequiredHeadFeatures`/`RequiredSyntacticFeatureStruct` gate)
tests whether a feature is *explicitly present*, not whether the word is merely compatible with it.
If number agreement, a stem-name region, or a later rule needs to see `num=sg` as an actual value,
an optional-and-skipped Number slot never produces that value — only a null singular suffix
occupying a non-optional slot does.

Black is explicit that this comes at a real running cost (§4.1.1: null allomorphs "can make the
parser run rather slowly," with `MaxNulls` as the throttle) and recommends constraining a null
allomorph with as specific an environment as possible so it doesn't get tried in places it
shouldn't. Black also notes the alternative some grammarians reach for — treating an always-null
affix as a default feature value instead of a real morpheme — isn't representable in the current
parsers at all; there is no "default feature value" construct, only rules that either assign a
feature or don't.

## A quick decision guide

- The position can truly have nothing there, and nothing downstream cares whether the feature was
  ever assigned → optional slot.
- Two branches of the paradigm need genuinely different slot-obligation patterns, not just
  different fillers for the same slot → multiple templates.
- The cell always has *some* value for a feature that something downstream must see explicitly, even
  though it surfaces as zero segments → null allomorph in a non-optional slot, tightly
  environment-constrained.
