# Interlinear analysis detail editing

Status: hypotheses and behavioral constraints requiring revalidation;
implementation retired
Sources: retired PR #965 and commit `bebe3473e`; PR #964 current base
Human review: PR #964

## Question tested

Can the Words > Analyses detail pane render and edit interlinear morph-bundle
data through the shared Avalonia detail infrastructure while preserving legacy
domain behavior and undo semantics?

## Observations

- The useful layering kept LCModel projection and write-back at the product
  edge while the view consumed an LCModel-free representation.
- The historical view aligned Wordform, Morphemes, Lexical Entries, Lexical
  Gloss, and Lexical Grammatical Information across morphemes. Bare wordforms,
  multiple analyses, RTL, and different vernacular/analysis writing systems
  were meaningful cases.
- Legacy configuration distinguished editable human-approved analyses from
  parser or disapproved analyses.
- Sense, MSA, and same-form lexical-entry choices have cascading domain effects.
  The historical choices for first-sense selection, independent MSA changes,
  stale chooser keys, and MSA pruning were not sufficiently established as
  current domain contracts.
- Activating `Analyses` affected more than one detail slice: it also affected
  feature-catalog and Options exposure while its browse pane remained legacy.

## What failed or was retired

The interlinear model, view, projector, write-back, plugin, dedicated tests,
and activation changes were removed from the current base. They use obsolete
region vocabulary and predate current detail, multi-writing-system, and host
infrastructure.

## Durable lessons

1. Keep the UI representation LCModel-free; projection and write-back own the
   domain boundary.
2. Treat one user gesture, its cascading model updates, cleanup, and undo as
   one fenced unit of work.
3. Characterize approval state, candidate identity, clear/error behavior, and
   prune rules from legacy and liblcm before designing edits.
4. Preserve writing-system runs, direction, shaping, caret behavior, and fonts;
   a single default string/font representation is insufficient.
5. Gate the exact detail route being proven. Do not activate a whole tool,
   browse surface, or Options row as an accidental side effect.

## Evidence needed next time

- Seeded legacy cases for bare wordforms, multiple analyses, multiple
  morphemes, alternate same-form entries, sense/MSA changes, parser analyses,
  RTL/mixed writing systems, stale candidates, and undo/redo.
- Domain-owner decisions for first-sense behavior, independent MSA choices,
  explicit clear versus invalid identity, and exact orphan-pruning rules.
- Projection identity and round-trip tests, including surviving references.
- Real gesture tests proving a single undo step and refresh settlement.
- Semantic, rendered, keyboard, UIA, and in-product parity for the detail pane.

## Decision boundary

This record constrains domain discovery, transactionality, text fidelity, and
activation scope. A human chooses which edit gesture to build first and whether
the feature should remain read-only or legacy when a domain decision is open.

## Do not infer

- Do not port or cherry-pick the retired implementation or its tests.
- Do not assume the first sense is the correct default.
- Do not clear a relationship merely because a historical chooser key is
  missing or stale.
- Do not delete an MSA based on the retired candidate heuristic.
- Do not include segmentation, the Analyses browse list, or Options exposure
  in a detail-slice proposal unless separately approved.
