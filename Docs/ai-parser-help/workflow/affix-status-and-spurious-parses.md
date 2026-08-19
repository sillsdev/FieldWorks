---
title: "Affix status (inflectional/derivational/unclassified) is a modeling commitment"
implements: src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AffixProcessAllomorph.cs, src/SIL.Machine.Morphology.HermitCrab/AffixTemplate.cs, src/SIL.Machine.Morphology.HermitCrab/SynthesisStratumRule.cs, src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd
black_sections: "2.1.1 Unclassified affixes; 2.1.2.9 Underspecified inflectional affixes; 2.1.3.7 Underspecified derivational affixes; 2.1.4 Derivation outside of inflection; 2.1.5 Derivation versus inflection"
category: morphotactics
when_to_use: "an affix is behaving as if it has no constraints, or you're deciding how to classify a new affix"
---

## There is no "unclassified" at the engine level

FieldWorks lets you label an affix inflectional, derivational, or unclassified (Black §2.1.1), and
treats a *partially specified* inflectional or derivational affix the same as unclassified (§2.1.2.9,
§2.1.3.7). The HermitCrab engine itself has no such attribute at all — check
`HermitCrabInput.dtd`: there is no `affixType`/`inflectionType` element or attribute anywhere in the
`MorphologicalRule`/`RealizationalRule` declarations. "Affix status" is a FieldWorks-side authoring
distinction that cashes out, by export time, into two independent engine-level facts:

1. **Whether the rule sits in an `AffixTemplate` slot at all.** Inflectional affixes go in a slot
   (order- and co-occurrence-constrained by the template); derivational affixes are stratum-level
   `MorphologicalRule`s applied outside any template, gated only by their own
   `RequiredSyntacticFeatureStruct`/`OutSyntacticFeatureStruct` pair.
2. **How constrained that rule's `RequiredSyntacticFeatureStruct` is.** Both `AffixProcessRule`
   (see `AffixProcessRule.cs`) and `AffixProcessAllomorph` initialize this to
   `FeatureStruct.New().Value` — an empty, unconstrained feature structure — unless the grammar
   sets it. An "unclassified" affix, in engine terms, is simply a rule that was never given a
   tighter `RequiredSyntacticFeatureStruct` and was never placed in a template slot.

## What that actually costs at parse time

An unconstrained `RequiredSyntacticFeatureStruct` unifies against *any* stem's feature structure —
`Subsumes` on an empty structure is trivially true. So a rule left unclassified structurally
applies to every stem whose phonological shape matches, regardless of category: a nominal-looking
suffix will be tried against verb stems, a valence-changing suffix will be tried against nouns,
and so on. This is exactly Black's own diagnosis (§2.1.1): "the affix is relatively unconstrained as
to where it can appear," producing extra incorrect parses for any word that happens to contain a
matching character sequence.

The performance angle compounds this rather than replacing it. An unclassified rule sitting in an
`unordered` stratum multiplies into the combinatorial search described in
[`speed/stratum-rule-ordering.md`](../speed/stratum-rule-ordering.md), and if its natural-class
patterns are broad (which they often are, precisely because nobody has narrowed the rule to a
category yet), it also widens the match set the way
[`speed/natural-class-feature-widening.md`](../speed/natural-class-feature-widening.md) describes.
An unclassified affix isn't a cheap placeholder you pay for later — it's an unconstrained rule
contributing to every derivation it can structurally reach, from the moment it's added.

## Deciding inflectional vs. derivational

Black's criteria (§2.1.5, elaborated further in the companion workshop transcript) reduce to four
questions, and "yes" to all four means inflectional:

- Does it belong to a set of affixes where exactly one member is used (a paradigm cell)?
- Can you state its position relative to other affixes in the word?
- Does it sit outside the less-constrained (derivational) affixes?
- Is its meaning grammatical rather than lexical/category-changing?

If the answer trends "no," treat it as derivational: a stratum-level `MorphologicalRule` with its
own required and output syntactic feature structures, applied freely (subject to the stratum's
`Linear`/`Unordered` cascade) rather than pinned to a slot position.

Treat "unclassified" as a temporary bookkeeping state while you're still gathering paradigm data —
not a resting state for a shipped grammar. Every unclassified affix left in place is, concretely, an
unconstrained rule contributing to every parse attempt it can reach.

## Derivation outside of inflection

Some languages inflect a stem, derive a new category from the inflected form, then inflect again
(Black §2.1.4's Huallaga Quechua example: verb inflected for aspect+object, nominalized, then the
resulting noun inflected for possessor+purpose). This is handled by marking the *inner* inflectional
template as one that "requires more derivation" — in engine terms, `AffixTemplate.IsFinal` is `false`
for that template instead of its default `true`.

This is grounded directly in `SynthesisStratumRule.ApplyMorphologicalRules`/`ApplyTemplates`: after a
template applies, the stratum rule checks `mruleOutWord.IsLastAppliedRuleFinal`. If the template that
just applied is non-final, the output doesn't get treated as a complete word for this stratum —
`ApplyTemplates` recurses it back through the stratum's morphological rules (looking for the
category-changing derivational affix), and if a word reaches the end of the stratum still needing a
non-final template's obligations satisfied, `Word.HasRemainingRulesFromStratum` catches it and the
engine reports a `PartialParse` failure rather than silently accepting an under-derived form (e.g.
Black's example: a bare `see-ipfv-1.obj` verb form with no subject suffix, missing the derivation
step that would let it skip subject agreement). Black's text adds a caveat worth taking at face
value even though it wasn't independently re-traced here: a template marked as requiring more
derivation must have at least one *obligatory* slot — an all-optional template can be satisfied by
skipping every slot, which would make "this template's obligations are met" trivially true and
defeat the "must be followed by more derivation" requirement entirely.
