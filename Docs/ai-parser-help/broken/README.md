# HermitCrab correctness gotchas

Part of the [HermitCrab-for-LLMs reference](../README.md). One file per gotcha — each is
self-contained, cites the specific `machine` source file(s) (and, where the trap originates in how
FieldWorks compiles a grammar into HermitCrab's XML, the specific `FieldWorks` source file(s)) that
implement the relevant behavior, and gives a fix. This section covers *correctness* problems — wrong
parses, missing parses, crashes, and silent misconfigurations — as opposed to the sibling
[`speed/`](../speed/README.md) section, which covers performance.

Each file starts with a metadata header:

```yaml
---
title: <short name of the gotcha>
implements: <machine-repo (and/or FieldWorks-repo) source file(s) that implement the relevant behavior>
category: <morphotactics | allomorphy | phonology | compounding | lexicon | feature-system | loader | ui-workflow>
symptom: <wrong-parse | missing-parse | crash | silent-misconfiguration | rejects-everything>
grammar_visible: <yes | no | partially>
---
```

## Index

| Gotcha | Category | Symptom | Description |
|---|---|---|---|
| [A StemName's partsOfSpeech is required — omitting it crashes, it doesn't silently reject everything](stemname-partsofspeech-required-not-silent.md) | loader | crash | An omitted `partsOfSpeech` on `<StemName>` fails the grammar load outright (DTD validation or a `NullReferenceException`), not a silent empty-POS constraint. |
| [A rule's requiredStemName is checked in the forward (synthesis) pass only — including during parsing](stem-name-affix-requirement-trace-misreading.md) | lexicon | silent-misconfiguration | `AffixProcessRule.RequiredStemName` is never read during analysis-side unapplication; it's enforced later in the mandatory resynthesis/confirmation pass, so a trace reader looking only at analysis-rule entries sees no rejection. |
| [LexFamily suppletion blocking silently discards a regular derivation, and only during generation](lexfamily-blocking-generation-only.md) | lexicon | missing-parse | `Blockable` rules can substitute a suppletive family member's form for a regularly-derived one via feature-structure `Subsumes`, which is checked only on synthesis and can fire far more broadly than one paradigm cell if the irregular entry's own feature structure is under-specified. |
| [An MPR feature group's match type defaults to Any (OR), not All](mpr-group-matchtype-default-is-any.md) | feature-system | silent-misconfiguration | Omitting `matchType` on a feature group gives OR semantics (any one listed feature suffices), not AND — the opposite of what an author wanting conjunctive gating might assume. |
| [An MPR feature group's output type defaults to Overwrite, not Append](mpr-group-output-default-overwrite.md) | feature-system | silent-misconfiguration | Omitting `outputType` gives non-monotone "last rule wins" behavior by default, silently dropping an earlier rule's tag from the same group. |
| [A stem with no inflection class and no configured default silently fails every class-restricted rule](inflection-class-no-default-silent-gating.md) | feature-system | missing-parse | An untagged stem with no `DefaultInflectionClassRA` configured anywhere up its POS hierarchy gets no inflection-class MPR feature at all, so every rule gated on any inflection class fails for it — not "matches the wrong class," matches no class. |
| [RealizationalRule can only spell out features already present — it cannot assign new ones the way AffixProcessRule can](realizational-rule-cannot-add-features.md) | morphotactics | silent-misconfiguration | `RealizationalRule` has no `OutputHeadFeatures`/`outputPartOfSpeech` equivalent; it can only merge a `RealizationalFeatureStruct` into a word, never introduce a feature value the word doesn't already carry in some form. |
| [RealizationalAffixProcessRule has no MaxApplicationCount backstop, unlike AffixProcessRule](realizational-rule-no-application-cap.md) | morphotactics | wrong-parse | Unlike `AffixProcessRule`/`CompoundingRule`, this rule type has no per-rule application cap at all — only a feature-based `IsBlocked` check stands between it and reapplying indefinitely in one derivation. |
| [A null/zero-realization affix is a real rule application, not a free default value](null-affix-cannot-express-default.md) | morphotactics | wrong-parse | A "null suffix" modeling a default value still goes through the same required-feature/MPR/environment gates as any other rule in a mandatory slot — if those gates aren't unconditioned, the derivation dead-ends instead of falling back to the intended default. |
| [Independent optional slots in one template can license a combination two templates would have prevented](single-template-independent-slots-illegal-combos.md) | morphotactics | wrong-parse | Each optional slot's apply-or-skip choice is explored independently of every other slot in the same template, so two affixes meant to always co-occur can each fire without the other unless a grammar author adds an explicit feature gate or splits them into separate templates. |
| [An unclassified (or under-specified) affix rule bypasses normal template-ordering discipline](unclassified-affix-bypasses-template-ordering.md) | morphotactics | wrong-parse | FLEx's "unclassified" affix status (and an inflectional affix left with no assigned slot) compiles to `IsPartial = true`, which is specifically permitted to attach after a final template where a normal classified rule would be refused. |
| [A multi-morpheme co-occurrence exclusion requires all listed morphemes together — not any one of them](coocurrence-rule-requires-all-not-any.md) | morphotactics | wrong-parse | An ad hoc prohibition listing several "other" morphemes only blocks the key morpheme when *all* of them co-occur together in the same word, not when any single one does — a much weaker constraint than a list of independent pairwise exclusions. |
| [Modeling a discontinuous morpheme as two independent affixes loses the engine's atomic-circumfix guarantee](circumfix-as-two-affixes-loses-atomicity.md) | morphotactics | wrong-parse | HC's native circumfix process applies both parts atomically or not at all; modeling the same discontinuous morpheme as two independent affixes in two template slots gives up that guarantee and can license a one-sided (half-realized) form. |
| [Compounding is capped at one application per derivation by default, and exocentric compounding cannot be configured otherwise](compounding-max-application-count-default.md) | compounding | missing-parse | `CompoundingRule.MaxApplicationCount` defaults to 1; FieldWorks's loader only ever raises it for endocentric compound rules, so a recursive (three-or-more-element) compounding pattern modeled as exocentric can never be configured to recurse. |
| [A stem-name-restricted allomorph needs the feature explicitly assigned, not just compatible](stem-name-explicit-feature-requirement.md) | lexicon | silent-misconfiguration | `StemName.IsRequiredMatch` tests whether a feature is *explicitly present* on the word, not whether the word is merely compatible with (doesn't conflict with) the region — an unmarked form fails a stem-name-restricted allomorph even trivially. |

## How to use this with an LLM

Paste the raw URL of the specific gotcha file that matches your symptom, e.g.:

```
https://raw.githubusercontent.com/sillsdev/machine/master/docs/ai-parser-help/broken/lexfamily-blocking-generation-only.md
```

If you're not sure which one applies, paste this index's raw URL first and describe your grammar's
structure (not its actual rules — see the privacy note in the top-level [`README.md`](../README.md))
and symptom (e.g. "this word parses when it shouldn't," "the grammar won't load," "an affix I marked
inflectional shows up in a position I didn't expect").
