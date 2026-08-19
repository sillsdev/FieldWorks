---
title: "Decide the typological frame and class system before authoring rules"
implements: src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd, src/SIL.Machine.Morphology.HermitCrab/AffixTemplate.cs, src/SIL.Machine.Morphology.HermitCrab/MprFeatureGroup.cs, src/SIL.Machine.Morphology.HermitCrab/SyntacticFeatureSystem.cs
black_sections: "1.1 Key issues; 2.1.2.5 Inflection and categories considerations; 2.1.2.6.2 Inflection classes and category organization"
category: build-order
when_to_use: "starting a new grammar, or an LLM is asked to add rules to a grammar that has none yet"
---

## The three things to commit to, in order

Before authoring a single morphological or phonological rule, three questions are worth answering
in this order, because each one constrains the HC constructs the next phase will use:

1. **What's the typological frame?** Degree of synthesis, prefixing vs. suffixing vs. both,
   vowel harmony, nasal assimilation, tone, noun-class/gender, case, head- vs. dependent-marking,
   articles, infixation, reduplication. This is cheap to determine (a handful of paradigms usually
   settles it) and it tells you which HC mechanisms you'll need at all — a language with no
   noun-class agreement has no reason to design an MPR-feature-group schema; a language with no
   infixation never needs to think about writing an infixation environment.
2. **What's the class system?** The noun/verb class inventory (declension classes, conjugation
   classes, genders) and how membership propagates to other words via agreement (an adjective or
   article tracking its head noun's class). This is a schema every later rule's vocabulary depends
   on — which class IDs exist, which feature groups they belong to, which categories can carry them
   — so it is worth designing deliberately rather than discovering piecemeal as each new rule turns
   out to need "one more inflection class."
3. **What are the rules, in ordered default+exception blocks?** See
   [`ordered-rule-exception-blocks.md`](ordered-rule-exception-blocks.md) for how that ordering maps
   onto HC's actual mechanisms.

This isn't a HermitCrab-specific idea — it's a general discipline for building any rule-based
grammar with lexically-conditioned exceptions. What follows is what each phase actually commits
you to in HC/FLEx terms, and where the commitment gets locked in.

## Phase 1 commits you to: parts of speech and natural classes

The typological frame determines your **part-of-speech inventory** and your **natural class
inventory** — both flat, ID-referenced lists at the engine level. Confirm this from the schema
itself: `HermitCrabInput.dtd` declares `PartsOfSpeech` as `(PartOfSpeech+)`, each `PartOfSpeech`
just an `id` and a `Name` — there is no parent/child element anywhere in that declaration. Every
place a rule or template restricts itself to a category (`requiredPartsOfSpeech` on a
`MorphologicalRule`, an `AffixTemplate`, a `PhonologicalSubrule`, `headPartsOfSpeech` on a
compounding rule, etc.) references this flat list directly, by ID.

FieldWorks lets you organize categories hierarchically (verb, with intransitive verb and
transitive verb nested under it) and per Black §2.1.2.5/§2.1.2.6.2, a template or inflection class
defined at the parent level is inherited by every nested subcategory. That inheritance is a
FieldWorks Language Explorer authoring convenience — the exported grammar you're handed has no
category hierarchy construct to inherit through. By the time a grammar reaches the engine, the
inheritance has already been resolved: each generated rule/template's `requiredPartsOfSpeech`
already lists whatever flat set of categories it applies to. If you're reading an exported grammar
and see the same category ID (or the same list of several category IDs) repeated across many
rules, that's very likely this flattening at work, not an authoring mistake — but it also means the
hierarchy discipline (put shared structure at the highest common category) has to happen on the
FieldWorks side, before export, because there's nowhere to express it afterward.

Natural classes (vowel, consonant, sonorant, etc.) work the same way structurally — they're a flat,
named, ID-referenced set that every environment and phonological rule pulls from. Committing to a
stable natural-class inventory during the frame phase avoids the failure mode covered in the
sibling performance reference
[`speed/natural-class-feature-widening.md`](../speed/natural-class-feature-widening.md): natural
classes invented ad hoc per-rule tend to be either too narrow (missing a segment a later rule
needs) or too broad (a "kitchen sink" class that widens every rule that references it).

## Phase 2 commits you to: MPR feature groups and syntactic features

The class system is realized by two genuinely different HC mechanisms, and picking the right one
per phenomenon is exactly Black's Table 9 (§2.1.2.8) decision:

- **Inflection classes** (arbitrary lexical conditioning, no semantic difference, not visible to
  agreement) → `MprFeature`/`MprFeatureGroup`. See
  [`inflection-classes-and-mpr-features.md`](inflection-classes-and-mpr-features.md).
- **Agreement/inflection features** (semantically real, syntactically visible categories like
  gender, person, number, case) → values in the `SyntacticFeatureSystem`, carried on
  `RequiredSyntacticFeatureStruct`/`RequiredHeadFeatures` and the word's own
  `SyntacticFeatureStruct`.

The reason to design this schema before writing rules, rather than after: `MprFeatureSet` (see
`MprFeatureSet.cs`) checks membership against whichever `MprFeatureGroup` a feature happens to
belong to, and a group's `MatchType` (`Any`/`All`) and `Output` (`Overwrite`/`Append`) are
properties of the *group*, not of any individual rule that references it. Every rule that gates on
a feature in that group inherits whatever matching/output behavior the group was given. Retrofitting
the group's semantics after several rules already depend on it (for instance, discovering you need
`Overwrite` semantics — see
[`speed/mpr-overwrite-order-dependence.md`](../speed/mpr-overwrite-order-dependence.md) — after
rules were written assuming `Append`) means re-auditing every rule that touches the group, not just
adding one new rule.

Syntactic (agreement) features are similarly global: a feature is declared once per category
(Black §2.1.2.7, "add the feature to the category's set of inflectable features") and every rule
for that category shares the same value space. A class system designed after the fact tends to
produce features invented per-rule with slightly different value sets for what's really the same
category-wide feature — the exact mismatch Black's Spanish gender example (§2.1.2.7) exists to
prevent, just discovered late instead of designed early.

## Phase 3: rules and exceptions

Once the frame and class schema exist, rule authoring becomes populating ordered blocks against a
fixed vocabulary of POS IDs, natural classes, and MPR features/syntactic features — rather than
inventing new ad hoc gates per rule. See
[`ordered-rule-exception-blocks.md`](ordered-rule-exception-blocks.md) for how "default, then
exception classes, then individual exceptions, most-specific-first" maps onto HC's actual
allomorph-ordering and stratum mechanisms.

## The failure mode this order prevents

Authoring rules opportunistically — adding a class ID the moment one affix seems to need it, adding
a natural class scoped to exactly the segments one rule cares about — tends to produce: MPR
features that are really one class split into several near-duplicates because nobody designed the
group first; natural classes that silently diverge from each other by one segment; and rules whose
`requiredPartsOfSpeech` lists were hand-copied between similar rules and drift out of sync as the
category inventory grows. None of these show up as an error — they show up later as a rule that
"mysteriously" doesn't fire for one lexical entry, or a class that behaves inconsistently between
two rules that were supposed to share it.
