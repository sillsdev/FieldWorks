---
title: "Independent optional slots in one template can license a combination two templates would have prevented"
implements: src/SIL.Machine.Morphology.HermitCrab/SynthesisAffixTemplateRule.cs, src/SIL.Machine.Morphology.HermitCrab/SynthesisAffixTemplatesRule.cs, src/SIL.Machine.Morphology.HermitCrab/AffixTemplate.cs
category: morphotactics
symptom: wrong-parse
grammar_visible: partially
---

## What it is

H. Andrew Black's FLEx/HC conceptual intro (§2.1.2.2 and its follow-up discussion of using separate
templates to force affix co-occurrence) describes a modeling choice: two affixes that must always
co-occur (or never co-occur) can be forced into that relationship by putting them in two mutually
exclusive templates (gated by disjoint required feature structures), where putting them in
independent optional slots of *one* template cannot express that coupling — each slot's
apply-or-skip choice is made independently of every other slot in the same template.

## The mechanism

Within one template, `SynthesisAffixTemplateRule.ApplySlots` recurses slot by slot, and for every
*optional* slot it explores both branches — apply the slot's rule(s), and also fall through to the
next slot without applying it:

```csharp
private void ApplySlots(Word input, int index, HashSet<Word> output)
{
    for (int i = index; i < _rules.Count; i++)
    {
        foreach (Word outWord in _rules[i].Apply(input))
            ApplySlots(outWord, i + 1, output);
        if (!_template.Slots[i].Optional)
            return;
    }
    output.Add(input);
}
```

(`SynthesisAffixTemplateRule.cs:37-55`). Nothing here couples slot `i`'s apply/skip choice to slot
`j`'s — each optional slot's two branches are explored independently, so with `n` independent
optional slots in one template, all `2^n` combinations of "applied at this slot or not" are
reachable (modulo each rule's own required-feature/environment gates), including combinations the
grammar author never intended to co-occur.

Templates themselves, by contrast, *are* mutually exclusive per derivation in the sense that a
template is chosen by its own gate: `SynthesisAffixTemplatesRule.Apply` only enters a template when
`input.SyntacticFeatureStruct.IsUnifiable(_templates[i].RequiredSyntacticFeatureStruct)`
(`SynthesisAffixTemplatesRule.cs:37`) — so a grammar author who wants "affix A and affix B only ever
co-occur, never apply independently" can express that by putting A in one template (with a
distinguishing required feature) and B in a separate template gated on the same required feature,
rather than as two optional slots of one shared template. Note both templates that satisfy their gate
still both get tried in the same call (the loop at `SynthesisAffixTemplatesRule.cs:33-54` does not
`break` after the first applicable template) — the mutual exclusion Black describes comes from making
the templates' `RequiredSyntacticFeatureStruct`s disjoint, not from the engine picking only one
template automatically.

## Concrete example

A stratum's single template has two independent optional slots: slot 1 for an applicative marker
`-ap`, slot 2 for an object-agreement suffix that is only supposed to be licensed *together with*
`-ap` (the language only marks object agreement on applicativized verbs). Modeled as two independent
optional slots in one template, the engine happily explores: neither fires, only `-ap` fires, only
the agreement suffix fires, or both fire — all four are separately reachable derivations unless each
rule's own `RequiredSyntacticFeatureStruct`/MPR-feature gates independently rule out the "just
agreement, no applicative" case. If the grammar author didn't add such a gate (having assumed the
template structure itself expressed the dependency), the illegal "bare object agreement with no
applicative" form parses and generates successfully.

## Fix

Do not rely on template/slot placement alone to express a required co-occurrence between two
affixes. Either add explicit `RequiredMprFeatures`/`RequiredSyntacticFeatureStruct` gates so the
dependent affix's rule cannot apply without the feature the other affix's rule sets, or split the
co-occurring pair into their own template with a disjoint required feature structure from the
template(s) where they don't apply, following Black's separate-templates pattern.
