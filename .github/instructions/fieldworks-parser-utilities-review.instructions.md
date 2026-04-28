---
applyTo: "Src/Utilities/pcpatrflex/**,Src/Utilities/AlloVarGen/**,Src/LexText/ParserCore/**,Src/LexText/Morphology/**,Src/LexText/Interlinear/**"
name: "fieldworks-parser-utilities-review"
description: "Copilot code review checks for parser, morphology, interlinear, PCPATR, and Allomorph Generator changes"
---

# Parser and Linguistic Utilities Review Checks

## Purpose

Use these checks for parser utilities, morphology, interlinear analysis, PCPATR,
TonePars, Allomorph Generator, and related test data.

## Correctness and regression coverage

- Parser, morphology, grammar, or transform changes should include acceptance
  examples or regression tests that show the intended linguistic behavior.
- For media-line, interlinear display, stale filtering, and preview-pane changes,
  verify the changed model state triggers display refresh and persistence updates.
- When large `.fwdata`, grammar, XML, or expected-output files change, verify the
  diff is intentional and tied to a specific parser or fixture behavior.
- Flag changes that alter matching, regex, grammar, or word-analysis behavior
  without edge-case tests for empty values, multiple writing systems, and
  ambiguous analyses.

## Allomorph and variant generator safety

- Dialog and chooser changes must handle empty collections, deleted items,
  missing writing systems, and null cast results.
- Selection state should be restored by stable identifiers or names when object
  instances can be recreated.
- New UI text or documentation for these utilities should use resources and
  remain localizable.

## Test data discipline

- Keep fixture updates scoped: do not refresh broad expected-output files unless
  the changed behavior requires it.
- Review paired before/after fixture files together and verify they remain
  consistent.
