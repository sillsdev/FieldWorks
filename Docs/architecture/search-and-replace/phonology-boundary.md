# Phonology and Unicode boundary

Status: working recommendation requiring owner feedback.

## Two different phonology boundaries

### Phonological rule and environment grammar

FieldWorks phonological environments are not regular expressions. The
external `PhonEnvRecognizer` validates a domain grammar, and `HCLoader`
translates valid environments and rules into HermitCrab pattern, group,
quantifier, and feature-constraint nodes
(`Src/LexText/ParserCore/HCLoader.cs:2313-2457`).

Regex correctness changes must not silently alter this grammar. The shared
concerns are Unicode segmentation, diacritics, malformed-input diagnostics,
bounded execution, and tests that preserve linguistic intent.

### Phonology browse-table search

Searching the Phonemes, Phonological Features, Bulk Edit Phoneme Features, or
Natural Classes tables is not domain-grammar parsing. Those tables use the
generic browse filter pipeline:

```text
configured column
  -> LayoutFinder renders cell text
  -> SimpleMatchDlg selects normal or regular expression
  -> FilterBarCellFilter
  -> Anywhere/Exact/Begin/End/RegExpMatcher
  -> native VwPattern
  -> ICU collation or ICU regex
```

This path belongs in the shared search architecture. Its user-visible
linguistic matching must use ICU. It needs separate configured-table tests for
literal and regular-expression filtering on `+`, `-`, `<ipa>`, and `Labial`.
The first characterization establishes matcher syntax only: bare `+` is
invalid regex and literal plus is `\+`; the other reported tokens are literal
in normal mode and valid literal regex text outside a character class.

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

## Test boundaries

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

Keep browse-table filtering in the shared search tests, not the parser suite.
The required consumer fixture must create real phoneme, feature, and
natural-class objects; render the configured columns through `LayoutFinder`;
install the selected matcher through `FilterBarCellFilter`; and assert exact
retained object identities. It must distinguish invalid regex feedback from a
failure to extract or match displayed cell text.
