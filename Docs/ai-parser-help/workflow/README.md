# HermitCrab grammar-authoring workflow

Part of the [HermitCrab-for-LLMs reference](../README.md). Where [`speed/`](../speed/README.md)
covers performance gotchas and `broken/` covers correctness gotchas, this directory covers *how to
model a language well* — the modeling decisions to make, in what order, and which HermitCrab/
FieldWorks constructs each decision commits you to. Every mechanism described as "how HC actually
behaves" is grounded in reading the engine source under `src/SIL.Machine.Morphology.HermitCrab/`,
not just restated from the methodology guide it's paired with.

## Primary source

These files draw on, and cite by section number, H. Andrew Black's *A Conceptual Introduction to
Morphological Parsing for FieldWorks Language Explorer* — the methodology guide FLEx/HermitCrab
modeling follows — plus a companion workshop transcript covering the same ground in the order a
person actually works through it. Both are reproduced verbatim in
[`sources/`](sources/black-flex-conceptual-intro-fulltext.txt) for anyone who wants Black's exact
original text; the files here are original guidance that cites specific sections rather than
quoting them at length.

Each file starts with a metadata header:

```yaml
---
title: <short name of the topic>
implements: <machine-repo source file(s) this guidance is grounded against>
black_sections: <Black conceptual-intro section number(s) for the full original treatment>
category: <build-order | morphotactics | feature-system | allomorphy>
when_to_use: <the symptom or authoring moment that makes this page relevant>
---
```

## Index

| Topic | Category | Use when |
|---|---|---|
| [Decide the typological frame and class system before authoring rules](build-order.md) | build-order | starting a new grammar, or extending one that has no rules yet |
| [Affix status (inflectional/derivational/unclassified) is a modeling commitment](affix-status-and-spurious-parses.md) | morphotactics | an affix behaves as if it has no constraints |
| [Optional slots, null affixes, and multiple templates model different facts](optional-slots-null-affixes-multiple-templates.md) | morphotactics | a paradigm cell has no overt marker in some forms |
| [Inflection classes, subclasses, and the default class are MPR features](inflection-classes-and-mpr-features.md) | feature-system | an affix's allomorph choice is lexically arbitrary |
| [Author default/exception blocks most-specific-first](ordered-rule-exception-blocks.md) | allomorphy | modeling a default form plus layered exceptions for one morpheme |
| [Circumfixes and discontinuous morphemes: one entry or two slots](circumfixes-and-discontinuous-morphemes.md) | morphotactics | a single meaning surfaces as material on both sides of the stem |

## How to use this with an LLM

Paste the raw URL of the specific topic file that matches your modeling question, e.g.:

```
https://raw.githubusercontent.com/sillsdev/machine/master/docs/ai-parser-help/workflow/build-order.md
```

If you're not sure which topic applies, paste this index's raw URL first and describe the modeling
decision you're facing (not your actual grammar's rules — see the privacy note in the top-level
[`README.md`](../README.md)), e.g. "I have a paradigm cell with no overt marker in half the cells,
how should I model it" or "I don't know if this affix is inflectional or derivational."

If your question is about *why parsing is slow* or *why a parse is wrong/missing*, this isn't the
right directory — see [`speed/`](../speed/README.md) or `broken/` instead. This directory assumes
the grammar runs; it's about whether it models the language correctly.
