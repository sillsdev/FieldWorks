---
title: "Author default/exception blocks most-specific-first, the way HC actually orders them"
implements: src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisAffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/Allomorph.cs, src/SIL.Machine.Morphology.HermitCrab/MprFeatureSet.cs, src/SIL.Machine.Morphology.HermitCrab/Stratum.cs
black_sections: "4.1.2 Order of allomorphs within a lexical entry; 2.1.6 Exception features; 3.1.4 Allomorph ordering"
category: allomorphy
when_to_use: "modeling a default form plus several layers of exceptions for one morpheme"
---

## The general shape: default, then exception classes, then individual exceptions

A recurring, well-grounded pattern for morphophonology is to model a rule as an ordered block: a
default (elsewhere) realization, then classes of exceptions (each with a reason — a phonological
environment, a lexical stratum, a semantic or lexically-arbitrary grouping), then individual lexical
exceptions, ordered most-specific-first — the Elsewhere principle. HermitCrab has a direct, literal
realization of exactly this ordering at the allomorph level, and a coarser one at the stratum level.
This page is about the authoring discipline of using them that way; the mechanics themselves are
covered in the sibling performance reference and not re-derived here.

## Allomorph order is the literal mechanism

Within one lexical entry, allomorph order is not cosmetic. `Allomorph.cs` tracks an `Index` per
allomorph, and the synthesis rule only lets an earlier allomorph pre-empt a later one, never the
reverse (see
[`speed/disjunctive-allomorph-deferred-recheck.md`](../speed/disjunctive-allomorph-deferred-recheck.md)
for the exact final-validity mechanism — the short version is that the engine's own source comment
states it can't check environment constraints until a surface form exists, so it defers the
disjunctive check and then rejects a later-indexed candidate if an earlier, more specific allomorph
should have won instead). Black's own text (§4.1.2) independently describes the same behavior from
the authoring side, and the two agree exactly: "FieldWorks Language Explorer applies the condition
of [an] Allomorph Form and, at the same time, negates the conditions of all preceding Allomorph
Forms." Concretely, Black's English plural example —

1. `-ɪz` after strident segments
2. `-z` after voiced (non-strident) segments
3. `-s` elsewhere

— only works because it's listed narrowest-condition-first, elsewhere-last. List them in the other
order and the elsewhere allomorph (now first) would apply everywhere, including after stridents,
before the narrower ones ever get a chance. This is the Elsewhere principle in its most literal HC
form: **most specific first, unconstrained/elsewhere last** — and it's exactly what makes the Lexeme
Form always sort last (Black notes this explicitly: the Lexeme Form field is automatically ordered
after every Allomorph Form), since the lexeme form is definitionally the least-constrained,
"nothing else applies" case.

The same ordering discipline applies whether the "exception" is phonological (an environment
pattern), lexically arbitrary (an `MprFeature` gate — see
[`inflection-classes-and-mpr-features.md`](inflection-classes-and-mpr-features.md)), or both at
once: whichever allomorph has the narrowest, most specific gate belongs earliest in the list.

## MPR-feature-gated exception classes follow the same discipline

Black's "exception features" (§2.1.6) are the class-of-exceptions-with-a-reason layer: tag an affix
with an `MprFeature`, tag only the stems that are genuine exceptions with the same feature, and the
affix is now restricted to that subset instead of applying everywhere it structurally could. Layering
several such tags is exactly "classes of exceptions, most specific first" translated into MPR terms:
a narrowly-tagged allomorph (a specific loanword stratum, a specific irregular subclass) should
precede a more broadly-tagged or untagged one in the allomorph list, for the same reason the English
plural example orders `-ɪz` before `-s` — an earlier allomorph's match pre-empts a later one's,
never the reverse.

An individual lexical exception (one specific root that behaves irregularly, not a whole class) is
the same mechanism taken to its narrowest case: a dedicated `MprFeature` (or, for stem-conditioned
allomorphy specifically, a stem name — see the stem-name gotcha referenced below) that only that one
lexical entry carries, gating an allomorph that exists only for it.

## What ordering discipline buys you, and what it doesn't

Getting the order right is a **correctness** discipline, not a performance optimization. Both the
environment/allomorph disjunctive check and the MPR-feature check are late filters — they run after
a candidate has already been constructed, not before
([`speed/mpr-cooccurrence-late-filter.md`](../speed/mpr-cooccurrence-late-filter.md),
[`speed/disjunctive-allomorph-deferred-recheck.md`](../speed/disjunctive-allomorph-deferred-recheck.md)).
Ordering exceptions most-specific-first doesn't make the engine skip work for the broader cases —
it determines *which candidate wins* once all of them have been tried. Getting the order backwards
doesn't just cost more; per the mechanism above, it silently changes which allomorph a form resolves
to, or produces spurious ambiguity where a listed-first "elsewhere" case pre-empts what should have
been the narrower winner.

## Strata as the coarser block

If an entire class of exceptions corresponds to a distinct historical layer of the lexicon (a
loanword stratum with its own phonology and morphology, rather than a handful of MPR-tagged
exceptions within one rule set), model it as a separate `Stratum` rather than MPR-gating every rule
in a single stratum to account for it. Strata are applied in a fixed sequence (each stratum's output
feeding the next), which is the natural HC analog of "this whole layer of the grammar behaves
differently, categorically, not just for a few tagged exceptions." See
[`speed/stratum-rule-ordering.md`](../speed/stratum-rule-ordering.md) for how a stratum's own
morphological-rule ordering setting (`Linear`/`Unordered`) interacts with this, which is a separate
axis from the cross-stratum sequence.
