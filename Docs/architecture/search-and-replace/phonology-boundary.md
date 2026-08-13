# Phonology and Unicode boundary

Status: working recommendation requiring owner feedback.

## Boundary

FieldWorks phonological environments are not regular expressions. The
external `PhonEnvRecognizer` validates a domain grammar, and `HCLoader`
translates valid environments and rules into HermitCrab pattern, group,
quantifier, and feature-constraint nodes
(`Src/LexText/ParserCore/HCLoader.cs:2313-2457`).

Regex correctness changes must not silently alter this grammar. The shared
concerns are Unicode segmentation, diacritics, malformed-input diagnostics,
bounded execution, and tests that preserve linguistic intent.

## Recommended contract

- Segment input by the domain's grapheme rules, including combining marks and
  dotted-circle representations.
- Resolve natural classes and indexed reduplication references explicitly.
- Report malformed or unresolved input without a yellow-box crash.
- Do not reject a valid merge or split rule merely because either side has
  multiple elements.
- State whether a malformed rule is skipped completely or partially loaded;
  never leave the outcome implicit in exception handling.
- Keep parser warnings and errors actionable and tied to the offending domain
  object.

## Historical constraints

- Jira [LT-18767](https://jira.sil.org/browse/LT-18767) establishes that an
  unresolved XAmple-style reduplication index produces a diagnostic rather
  than a crash.
- Jira [LT-21585](https://jira.sil.org/browse/LT-21585) led to a temporary
  restriction on multi-element rewrite rules.
- Jira [LT-22353](https://jira.sil.org/browse/LT-22353) and PR 646 establish
  that merge and split rules with multiple elements are valid after the
  HermitCrab update. The earlier restriction is superseded.

## Test boundary

Keep phonology cases in parser tests rather than a shared regex suite. Reuse
Unicode input vectors where useful:

- combining marks and dotted-circle segments;
- malformed brackets, parentheses, and optional contexts;
- missing natural classes and reduplication indices;
- merge and split rules;
- logger category and partial-load behavior;
- end-to-end Try a Word acceptance cases when fixtures are available.

The implementation of `PhonEnvRecognizer` is supplied by an external
dependency. If its grammar or normalization behavior becomes the subject of a
change, inspect the corresponding dependency source and tests before changing
FieldWorks adapters.
