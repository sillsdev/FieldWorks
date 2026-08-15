---
title: "Pattern-shaped root allomorphs bypass the trie and pay a linear-scan cost"
implements: src/SIL.Machine.Morphology.HermitCrab/LexEntry.cs, src/SIL.Machine.Morphology.HermitCrab/RootAllomorph.cs, src/SIL.Machine.Morphology.HermitCrab/RootAllomorphTrie.cs, src/SIL.Machine.Morphology.HermitCrab/Morpher.cs
category: lexicon
cost: "O(word length) via the trie for literal-shape allomorphs; O(number of pattern allomorphs) linear scan, per analysis attempt, for the rest"
grammar_visible: yes
---

## What it is

A lexical entry holds one or more root allomorphs — suppletive or phonologically-conditioned
surface forms of the same morpheme. Root lookup is normally near-free regardless of lexicon
size, but any allomorph whose own phonetic shape is written with a broad, underspecified segment
sequence (rather than a literal string) falls out of that fast path entirely.

## Trie-indexed lookup, and what defeats it

The engine builds one root-allomorph trie per stratum from every root allomorph in that
stratum's lexicon. The trie is a segment-by-segment automaton built by chaining each allomorph's
literal shape into shared states keyed on exact feature-structure equality per node; searching it
transduces the input shape against it, giving lookup cost proportional to the word's length, not
the number of lexical entries.

But **not every allomorph goes into the trie**: a root allomorph is marked as a "pattern" if any
node in its shape is iterative, or is an optional, non-boundary annotation — i.e. any root
declared with a wildcard-like shape (a segment sequence using an unbounded/optional quantifier
rather than literal segments) instead of a fixed literal string. Every such pattern allomorph is
routed into a separate flat list instead of the trie. Lexical lookup then searches that flat list
with an explicit linear loop over every entry in it, once per analysis attempt, **in addition to**
(not instead of) the ordinary trie lookup for literal entries.

## Gotcha

An allomorph whose own shape is written with a broad, underspecified segment sequence (e.g.
matching "any string of segments" to model a maximally general root shape, as opposed to the
root's actual literal string) does not get the trie's near-free lookup — every such allomorph is
checked against every analysis input via a separate linear scan. A grammar with many such
"pattern" roots (as opposed to a handful of genuinely templatic ones, e.g. reduplication bases)
pays that linear cost on every word analyzed, on top of the normal trie lookup for its literal
entries.

## Fix

Reserve pattern-shaped root allomorphs for cases that are genuinely templatic (true
reduplication/prosodic templates); give ordinary roots their literal phonemic shape so they land
in the trie instead of the linear-scan fallback.
