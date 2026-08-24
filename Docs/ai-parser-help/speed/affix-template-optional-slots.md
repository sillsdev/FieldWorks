---
title: Independently-optional affix-template slots cause exponential blowup
implements: src/SIL.Machine.Morphology.HermitCrab/AffixTemplateSlot.cs, src/SIL.Machine.Morphology.HermitCrab/SynthesisAffixTemplateRule.cs, src/SIL.Machine.Morphology.HermitCrab/AnalysisAffixTemplateRule.cs, src/SIL.Machine/Rules/RuleBatch.cs
category: morphotactics
cost: O(2^n) in the number of independently-optional slots
grammar_visible: yes
---

## What it is

An affix template models a word's inflectional morphology (e.g. a noun's number and case) as
an ordered sequence of "slots," each slot holding the rule(s) that can fill that position. If a
grammar models several mutually-exclusive realizations of the same paradigmatic position (say,
ten cases crossed with singular/plural, several of them null) as separate, independently
optional slots rather than one slot with several alternatives, parse time blows up
exponentially in the slot count.

## The data model

```csharp
// AffixTemplateSlot.cs
public class AffixTemplateSlot
{
    // a slot holds a LIST of rules — these are tried as alternatives, not composed
    public ReadOnlyCollection<MorphemicMorphologicalRule> Rules { get; }

    // if true, the slot may be skipped entirely (no rule in it fires)
    public bool Optional { get; set; }
}
```

An `AffixTemplate` is just an ordered list of these slots, applied to a stem in sequence.

## The traversal algorithm

```csharp
// SynthesisAffixTemplateRule.cs
_rules = template.Slots.Select(slot => new RuleBatch<Word, ShapeNode>(
    slot.Rules.Select(mr => mr.CompileSynthesisRule(morpher)),
    false, // disjunctive = false: try ALL rules in the slot, union their outputs
    FreezableEqualityComparer<Word>.Default
));

private void ApplySlots(Word input, int index, HashSet<Word> output)
{
    for (int i = index; i < _rules.Count; i++)
    {
        foreach (Word outWord in _rules[i].Apply(input))   // batch = all rules in slot i
            ApplySlots(outWord, i + 1, output);             // recurse per surviving output

        if (!_template.Slots[i].Optional)
            return;   // mandatory slot: stop here either way

        // slot IS optional: loop continues to slot i+1 using the ORIGINAL `input`,
        // i.e. "skip this slot" is tried as a separate path in addition to "apply it"
    }
    output.Add(input);
}
```

And the batch itself:

```csharp
// RuleBatch.cs
public virtual IEnumerable<TData> Apply(TData input)
{
    var output = new HashSet<TData>(_comparer);
    foreach (var rule in _rules)
    {
        output.UnionWith(rule.Apply(input));
        if (_disjunctive && output.Count > 0)
            return output;   // (not our case — slots use disjunctive: false)
    }
    return output;   // unions ALL matching rules' outputs
}
```

Two facts fall out of this:

1. **Across slots**, the recursion is per-slot, not per-rule: `ApplySlots` recurses once for
   each slot's *combined* batch output, not once per individual rule inside a slot.
2. **Within a slot**, every rule in the slot is tried and *all* that structurally unify
   contribute an output (union, not first-match). So a slot with `k` mutually-exclusive rules
   costs `O(k)` — not `O(2^k)` — *provided* the rules' required feature structures are actually
   mutually exclusive, so normally only one (or a few, for genuine ambiguity) unifies per
   analysis.

## Pattern A: one slot per affix, each optional — exponential

Model 20 independent prefix positions (ten grammatical cases × singular/plural, several
realized as null) as 20 slots, each `optional="true"`, each holding one rule. Every optional
slot independently contributes an "applied" branch and a "skipped" branch, because the
recursion has no way to know that a null-realized rule firing looks identical, on the surface,
to the slot never having fired. Cost for a single stem: **O(2ⁿ)** — for n = 20, roughly 10⁶
candidate paths, before any other rule interactions. A minimal worked example: 12 independent
optional slots that each insert the same single character produces `C(12,k)` equally-valid,
byte-identical-surface analyses for a form with `k` of those characters present — `C(12,6) =
924` for the midpoint case — even though nothing in the surface string distinguishes which
subset of slots actually fired.

## Pattern B: one slot, many mutually-exclusive rules, non-optional — linear

The fix is to recognize that the 20 cells are not 20 independent binary toggles — they are
**one paradigmatic position** (a "number+case prefix") with 20 alternative realizations, exactly
one of which applies to any given noun. Model that as:

- **One** `AffixTemplateSlot`, containing all 20 `MorphologicalRule`s.
- Each rule gated by a required inflectional feature structure (e.g. `num=sg, case=erg` vs
  `num=pl, case=abs`, ...) so the rules' domains are pairwise disjoint.
- The slot marked **non-optional** — the noun always has *some* number+case value, it's never
  truly absent, it's just sometimes spelled with zero segments. Marking it optional adds a
  spurious extra "skip" branch for a cell that linguistically can't actually be skipped.

Toy example (invented `casA`/`casB`/`casC` × `sg`/`pl`, not any real language) — six
mutually-exclusive rules in one slot instead of six optional slots:

```xml
<AffixTemplate requiredPartsOfSpeech="posN">
  <Name>numberCasePrefix</Name>
  <Slot optional="false" morphologicalRules="mrSgA mrPlA mrSgB mrPlB mrSgC mrPlC">
    <Name>numCase</Name>
  </Slot>
</AffixTemplate>
```

where e.g. `mrPlB` requires `[num:pl case:casB]` and its subrule's output is simply a copy of
the input with no inserted segments (the null realization) — the rule still "fires" and tags
the word with `[num:pl case:casB]`, it just doesn't change the string.

With rules' feature requirements pairwise disjoint, at most one of the six unifies per analysis
→ **O(6)** for that slot, not `O(2^6)`.

## If some null cells are genuinely homophonous

Suppose 3 of the 20 cells really do share an identical null exponent and are truly
indistinguishable on the surface (not just under-specified features — actually ambiguous).
Analysis of such a word correctly yields 3 candidate parses, each tagged with a different
`num`/`case` feature bundle, all with identical surface text. That is **O(3)** residual
ambiguity at that node — real linguistic ambiguity, not a performance bug. It should be resolved
(if at all) by agreement elsewhere in the clause (verb/adjective concord, syntax-level
unification), not by trying to make the morphological parser guess.

## Fix

Collapse independently-optional slots that are really alternative fillers of one paradigmatic
position into a single non-optional slot holding all the alternatives, each gated by a distinct,
mutually-exclusive required feature structure. This changes the asymptotic behavior (`O(2ⁿ)` →
`O(n)`), not just the constant factor.
