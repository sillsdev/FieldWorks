---
title: "Kitchen-sink natural classes widen a rule's environment, not just its own cost"
implements: src/SIL.Machine/FeatureModel/FeatureStruct.cs, src/SIL.Machine/FeatureModel/SymbolicFeatureValue.cs, src/SIL.Machine.Morphology.HermitCrab/SegmentNaturalClass.cs, src/SIL.Machine.Morphology.HermitCrab/SimpleContext.cs, src/SIL.Machine.Morphology.HermitCrab/NaturalClass.cs
category: feature-system
cost: "unification itself is cheap (near-constant in feature count); the real cost is a widened match set downstream"
grammar_visible: yes
---

## What it is

A natural class (e.g. "vowels", "voiceless obstruents") is a named feature structure used as a
matching constraint in rule environments. `SegmentNaturalClass` builds its feature structure by
unioning every member segment's own feature structure; `SimpleContext` wraps a natural class's
feature structure (plus any pattern variables) as the actual constraint object tested against
each string position during phonological pattern matching.

## Where the cost is, and where it isn't

Unification recurses over each feature structure's feature dictionary, following nested complex
features and re-entrancies through a visited-node map that also acts as a cycle guard — cost is
proportional to the number of distinct features involved (typically tens), not exponential in
disjunction size. Within one feature, a symbolic value's disjunctive value-set is backed by a
bitset, and intersect/union/overlap checks are bitwise ops on that flag set. **Unifying two
symbolic values, even with large disjunctive sets, is cheap** — this is not the mechanism that
makes broad natural classes slow.

## The actual gotcha: how a natural class is built

The feature-structure union used to build a class from its member segments only keeps a feature
key that's present in **both** operands, narrowing the surviving value via bitset union where a
key is kept. So a natural class spanning segments that don't share many features (e.g.
"everything that isn't a vowel," lumping bilabial stops in with sibilants and laterals)
converges toward an **emptier** feature structure as more dissimilar segments are added — and an
empty feature structure renders (and behaves) as unconstrained: HC's own feature-structure
stringifier literally prints an empty structure as `"ANY"`. A pattern context built from that
natural class then matches at essentially every string position, because the constraint it wraps
has lost the features that would have restricted it.

## Gotcha, concretely

Defining a kitchen-sink natural class (e.g. lumping every consonant in the inventory into one
`AnyC` class instead of the specific place/manner classes a rule's environment actually needs)
does not make *unification* slow — it makes the *rule* less selective, so its environment
matches far more positions than intended, and every one of those spurious matches is a candidate
the rest of the engine (rule ordering, template slots, other phonological rules downstream) now
has to process. The blowup shows up as more candidate match/rewrite sites explored, not as
expensive feature-structure comparisons.

## Fix

Define natural classes at the granularity the rule's environment actually needs — a class stated
directly in terms of the relevant features is usually more precise and more legible than one
built by listing many segments and hoping their union keeps the right features — and check what
feature structure a segment-listed natural class actually reduces to when new segments are added
to it.
