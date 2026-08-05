# Rule-formula and supporting grammar editors

Status: hypotheses and behavioral constraints requiring revalidation;
implementation retired
Sources: retired PR #966 and commit `2f81e9183`; PR #964 current base
Human review: PR #964

## Question tested

Can six grammar tool routes share an Avalonia formula-editing model while
preserving the distinct semantics of regular, metathesis, compound,
environment, natural-class, phoneme, and co-occurrence editing?

## Observations

- The six tool routes were grouped for delivery but do not necessarily form one
  indivisible product feature.
- Formula-string oracles captured domain structure more precisely than visual
  similarity alone.
- Atomic-to-sequence context changes had ordering and ownership invariants;
  assigning a feature structure before owning the new context could fail.
- Projection, write-back, plugin selection, and the framework-neutral editor
  were useful separate responsibilities.
- A catalog activation could expose an entire Options group in addition to
  changing product routing.

## What failed or was retired

The rule models, commands, projectors, sinks, plugins, supporting editors,
tests, strings, and activation changes were removed. The stack also inherited
interlinear work even though no domain dependency requires rule work to follow
interlinear work.

## Durable lessons

1. Define parity with domain oracles and transformations, not screenshots
   alone.
2. Pin context ownership and atomic/sequence transition ordering before editing
   nested rule structures.
3. Keep route selection, domain projection/write-back, and UI editing concerns
   independently testable.
4. Make cancel/no-op, invalid-target rejection, one-step undo, and round-trip
   behavior explicit for every supported route.
5. Activate only routes whose real plugin, lifecycle, localization, legacy
   fallback, and product workflow have all been proven.

## Evidence needed next time

- A human-reviewed parity matrix for each of the six tool routes.
- Canonical formula oracles for regular, metathesis, compound, and environment
  structures, including empty and malformed cases.
- Insert/delete/move and atomic-to-sequence transition tests with undo.
- Environment and Basic IPA commit/clear/description behavior confirmed against
  the legacy product.
- Real-project, keyboard, focus, accessibility, teardown, and routing evidence
  in addition to headless model tests.

## Decision boundary

This record constrains domain characterization, transition safety, test
oracles, layering, and activation. A human chooses route grouping, UX,
recursive depth, deferred behaviors, and whether routes ship independently.

## Do not infer

- Do not port or cherry-pick the retired implementation or test suite.
- Do not make rule work depend on interlinear work because the old PRs stacked
  that way.
- Do not treat leaf editing as recursive parity.
- Do not restore an entire feature group from a historical active-tool list.
- Do not treat a passing headless suite as proof of product activation.
