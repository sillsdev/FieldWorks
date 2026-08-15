---
title: "Circumfixes and discontinuous morphemes: one entry, two-sided output, or two slots"
implements: src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AffixProcessAllomorph.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/InsertSegments.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/MorphologicalOutputAction.cs, src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd
black_sections: "1.1.5 Discontinuous morphemes; 2.1.2.4 Discontinuous morpheme; 4.3 Circumfixes; 6.1.1.3 Circumfixation as a process"
category: morphotactics
when_to_use: "a single meaning is realized as material on both sides of the stem at once"
---

## Two genuinely different strategies, not one

Both a discontinuous tense marker (a prefix and a suffix that must co-occur, e.g. Caquinte's future
`n-...-e`) and a circumfix (Indonesian `ke-...-an` nominalizer) put material on both sides of the
stem. Black models them differently (§2.1.2.4 vs. §4.3/§6.1.1.3), and the difference is real, not
stylistic — it comes down to whether the two pieces are one morpheme or two.

### Strategy 1: two slots, both obligatory, in one template

For Caquinte's future tense, Black's approach is pure morphotactics: a template with a Future prefix
slot and a Future suffix slot, both non-optional. Nothing ties the two allomorphs together except
that the template requires both to be filled — they remain two separate morphemes in the analysis
(two glosses, two lexical entries), and the fact that they always co-occur is a fact about the
template, not about either morpheme's own identity. This is the same tool used for forcing
co-occurrence generally — see
[`optional-slots-null-affixes-multiple-templates.md`](optional-slots-null-affixes-multiple-templates.md)
— applied specifically to a pair of affixes that jointly spell one grammatical value.

### Strategy 2: one lexical entry, one rule, output on both sides

For a true circumfix — one meaning, one gloss, conventionally treated as a single morpheme — the
HC-native mechanism is a single affix-process allomorph whose output (`Rhs`) inserts segments before
*and* after the copied stem material, in the same rule. This is directly grounded in the engine:
`AffixProcessAllomorph.Rhs` is a plain `IList<MorphologicalOutputAction>` (see
`AffixProcessAllomorph.cs`), and `InsertSegments` (`InsertSegments.cs`) is just one such action that
inserts a fixed shape at whatever position it occupies in that list. Nothing prevents a `Rhs` from
containing `InsertSegments("ke")`, then a copy of the stem's own matched material, then
`InsertSegments("an")` — which is exactly Black's own description of the mechanism (§6.1.1.3): "the
pattern is just whatever material is currently present (the X); the result is the prefix phonemes,
the 1, and then the suffix phonemes." The stem is one morpheme with one `MorphemeId`/gloss; the
"two parts" are just two output actions in the same rule's `Rhs`.

Note the DTD's own morpheme-type enumeration excludes `circumfix` as a type the engine directly
recognizes (Black's footnote at §4.3 says this outright: the parser recognizes every morpheme type
"except for discontiguous phrase, simulfix, suprafix, and circumfix"). FieldWorks' lexical-entry-level
"circumfix" morpheme type (two allomorphs keyed together, one prefix-shaped and one suffix-shaped) is
therefore a FieldWorks authoring convenience that has to be translated into the single-`Rhs`,
two-sided-output form above before it means anything to the engine — if you're reading an exported
grammar and looking for "the circumfix," look for a single affix-process rule whose output list has
an insertion before *and* after the stem-copy action, not a lexical entry literally tagged
`circumfix`.

## Deciding which one you have

- Can the two pieces be glossed independently, and would a linguist reasonably call them two
  morphemes that happen to always co-occur in this cell? → two obligatory slots in one template
  (Strategy 1). The co-occurrence is a template-level fact.
- Is it conventionally analyzed as one morpheme with one meaning, whose exponent simply happens to
  wrap the stem? → one lexical entry, one affix-process rule, two-sided `Rhs` (Strategy 2).

Getting this wrong in the "should be one morpheme" direction (modeling a true circumfix as two
slots) produces a grammar that reports two morphemes and two glosses for what a reader expects to
see analyzed as one meaning unit — not a parse failure, but a wrong-shaped analysis. Getting it
wrong in the other direction (forcing two genuinely independent, separately-glossable affixes into a
single two-sided `Rhs`) loses the ability to have either piece occur without the other in some other
combination, if the language ever needs that.

## Infixation is the same output-action mechanism, aimed inward

The same `Rhs` list mechanism handles infixation — the inserted material lands *inside* the copied
stem's matched shape rather than outside it, using the same output-action composition, with the
insertion point defined by an infixation environment pattern (Black §3.3.1's schematic
`# [C] _ [V]`, anchoring the insertion point relative to the stem's own segments rather than to the
whole word's edges). It's the same construct as the circumfix case — output actions placed relative
to a copied-input action — just with the insertion point moved inward instead of split to both
edges.
