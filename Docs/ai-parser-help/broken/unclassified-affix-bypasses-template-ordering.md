---
title: "An unclassified (or under-specified) affix rule bypasses normal template-ordering discipline"
implements: src/SIL.Machine.Morphology.HermitCrab/Morpheme.cs, src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/SynthesisAffixProcessRule.cs, src/SIL.Machine.Morphology.HermitCrab/SynthesisAffixTemplatesRule.cs, FieldWorks Src/LexText/ParserCore/HCLoader.cs
category: morphotactics
symptom: wrong-parse
grammar_visible: partially
---

## What it is

FieldWorks distinguishes an affix's morphosyntactic status as inflectional, derivational, or
unclassified (H. Andrew Black's FLEx/HC conceptual intro, §2.1.1 "Unclassified affixes," discusses
how an unclassified affix is "relatively unconstrained as to where it can appear" and can cause
spurious parses). This is not just a FLEx-side labeling convention: it compiles down to a real HC
engine property, `Morpheme.IsPartial` (`Morpheme.cs:45`), and the engine gives an `IsPartial` rule
genuinely different template-ordering permissions than an ordinary classified rule.

## The mechanism

`SynthesisAffixProcessRule.Apply` gates whether a non-template rule may apply after a template has
already fired, and the gate is different depending on `IsPartial`:

```csharp
// if a final template was last applied,
// do not allow a non-partial rule to apply unless the input is partial
if (!_rule.IsTemplateRule && (input.IsLastAppliedRuleFinal ?? false)
    && !input.IsPartial && !_rule.IsPartial)
    return Enumerable.Empty<Word>();   // FailureReason.NonPartialRuleProhibitedAfterFinalTemplate

// if a non-final template was last applied,
// only allow a non-partial rule to apply unless the input is partial
if (!_rule.IsTemplateRule && input.IsLastAppliedRuleFinal.HasValue
    && !input.IsLastAppliedRuleFinal.Value && !input.IsPartial && _rule.IsPartial)
    return Enumerable.Empty<Word>();   // FailureReason.NonPartialRuleRequiredAfterNonFinalTemplate
```

(`MorphologicalRules/SynthesisAffixProcessRule.cs:61-104`). In plain terms: an ordinary
(non-partial) rule cannot apply after a *final* template unless the word itself is already partial,
and a *partial* rule specifically can. `SynthesisAffixTemplatesRule.Apply` also skips template
matching for a partial root entirely: `&& !input.RootAllomorph.Morpheme.IsPartial`
(`SynthesisAffixTemplatesRule.cs:38`) — a partial morpheme's derivation doesn't go through the
normal template-selection gate other roots go through.

The FieldWorks loader sets `IsPartial` from the FLEx affix classification: an inflectional affix's
`IsPartial` is `msa.SlotsRC.Count == 0` (no slot assigned — effectively still under-specified even
though nominally "inflectional"), and an unclassified affix is *always* loaded with `IsPartial =
true` regardless of anything else about it (both in `HCLoader.cs`, in the inflectional- and
unclassified-affix loading methods respectively).

## Why this matters

A classified, fully slotted inflectional or derivational affix is confined to the normal
template-ordering discipline: once a final template applies, no further non-partial affixation is
allowed except from other partial rules. An unclassified affix sidesteps that discipline on both
ends — it's allowed to apply where a normal rule would be refused (after a final template, if the
word itself isn't already partial... note it still needs the word to satisfy the *other* branch, but
the two branches together give a partial rule strictly more freedom around template boundaries than
a non-partial one gets). A grammar author who leaves an affix unclassified because its
morphosyntactic status genuinely doesn't matter to them can end up with an affix that combines with
stems in positions relative to a stratum's affix templates that a fully classified affix of the same
shape never could — producing analyses (or generable words) the author did not intend to license.

## Concrete example

A grammar has one inflectional affix template per POS, marked `final="true"` (nothing may apply
after it). A derivational suffix `-caus` is left unclassified in FLEx instead of tagged derivational
(perhaps because the modeler wasn't sure or didn't think it mattered). Because `IsPartial` is forced
`true` for any unclassified affix, `-caus` is now specifically *permitted* to apply after the final
inflectional template — stacking on top of a fully-inflected word — where a properly classified
derivational affix would have been rejected by `NonPartialRuleProhibitedAfterFinalTemplate`. The
result: words like a fully-inflected verb with `-caus` appended afterward parse (or generate)
successfully, when the grammar's actual structure (derivation happens before inflection, never
after) never intended to allow that ordering.

## Fix

Classify every affix as inflectional or derivational explicitly, and for inflectional affixes assign
a real template slot (`msa.SlotsRC`) rather than leaving it empty — both avoid `IsPartial` being set
in a way you didn't intend. Reserve "unclassified" for affixes that genuinely should be exempt from
template-ordering constraints (rare), and check any unclassified affix's actual generable/parseable
combinations against the templates you expect it to interact with, since it is not gated the same
way the rest of the grammar's affixes are.
