# Search and replace test strategy

Status: working plan, not a normative product contract.

## Test roles

Characterization tests record current behavior. They must pass against the
unchanged implementation. Names and assertion messages describe the observed
result without presenting it as an approved product decision.

Acceptance tests state approved behavior. For a correction, each acceptance
test must fail for the expected reason before production code changes and pass
after the minimal fix.

Safety invariants may be asserted immediately when product evidence and a
finite reproducer establish them: no data loss, no crash, valid UTF-16 ranges,
and no split surrogate pairs. Hang prevention and deterministic completion are
goals, but a resource-bound acceptance test waits for an approved measurable
bound.

## Layered harness

1. Native `VwPattern` tests establish literal and ICU regex engine behavior.
2. Managed matcher tests establish option wiring and error translation.
3. Consumer tests establish Find and Replace, Bulk Edit, filters,
   concordance, indexed discovery, and AlloVarGen behavior.
4. Focused integration tests establish preservation of formatted strings,
   writing systems, annotations, preview/apply parity, and undo boundaries.

Do not force one test base across engines with incompatible result types.
Share case data and expected-result records while keeping small adapters close
to each consumer.

## Characterization matrix

Every applicable surface records:

- found or not found;
- UTF-16 start and limit;
- forward and reverse behavior;
- restart behavior after a match;
- capture groups;
- replacement text and text properties;
- error category;
- cancellation or bounded-completion outcome.

| Dimension | Minimum cases |
| --- | --- |
| Normalization | NFC pattern against NFD text and the reverse; NFD against NFD; compatibility forms recorded separately |
| Marks | One and two combining marks; canonical and noncanonical mark order; leading and trailing marks; unknown marks |
| Ignorables and extenders | Soft hyphen, combining grapheme joiner, zero-width joiner, Mongolian variation selector, Arabic tatweel, Hangul filler, Thai nikhahit |
| Options | Case by diacritics matrix; whole word; writing system; style, tag, and object runs where supported |
| Locale | `root`, a named language locale, custom tailoring, shifted punctuation, and other-language rules |
| Ranges | Empty source, nonzero start, range ending before a mark, supplementary-plane text, forward and reverse restart |
| Regex | Anchors, alternation, captures, empty match, invalid groups and classes, Unicode properties, long nested quantifiers |
| Replacement | Capture references, literal dollar and backslash, missing groups, empty replacement, growing and shrinking text, mixed writing-system runs |
| Safety | Replace All data preservation, deterministic termination, cancellation, stale indexed results, and actionable errors |

## Existing assets to preserve

The active `table-speedup` branch contains useful tests that are not yet on
`main`. Only these committed hashes are inputs to this plan:

- Commit `a5a380e85` adds `VwPatternSearchCharacterisationTests.cs`, which
  records normalization, mark order,
  option strength, whole word, ranges, restart offsets, and pattern reuse.
- Commits `9dc07d8d8`, `80d6fc2b2`, and `60ecfe2d6` add and refine
  `VwPatternCollationRegressionTests.cs`, which records shifted punctuation, custom
  tailoring, normalization, locale-specific ignorables, and range behavior.
- Commit `5555730e9` adds `BulkEditOuterLoopCostTests.cs`, which exercises the
  real preview and apply path and
  counts `FindIn` calls.

Uncommitted changes in that worktree are non-normative context and are not a
dependency of this plan.

Adapt the semantic fixtures into appropriately named correctness suites. Do
not copy performance timing assertions or assume experimental fast-path
expectations are desired behavior.

## Initial implementation order

1. Port the committed native-pattern characterization cases to the
   `regex-correctness` branch with locale pinned.
2. Add ICU regex normalization, option, capture, zero-width, and range cases.
3. Add Bulk Edit preview/apply cases that reuse the real `ReplaceWithMethod`.
4. Add Find and Replace option-wiring and replacement-preservation cases.
5. Add indexed-search and concordance cases for normalization and diacritics.
6. Restore direct AlloVarGen matcher coverage or document the fixture blocker;
   add .NET replacement behavior cases either way.
7. Add phonology characterization for combining-mark segments, malformed
   references, and merge or split rules without sharing regex assertions.
8. Add resource-policy acceptance tests only after an owner approves a bound.

## Verification

Use repository scripts only:

```powershell
.\build.ps1
.\test.ps1 -TestProject TestViews
.\test.ps1 -TestProject Src/xWorks/xWorksTests
.\test.ps1 -TestProject Src/FwCoreDlgs/FwCoreDlgsTests
.\test.ps1 -TestProject Src/Common/Controls/XMLViews/XMLViewsTests
.\test.ps1 -TestProject Src/LexText/Interlinear/ITextDllTests
```

The exact test project selector must be verified with `test.ps1 -ListTests`
before relying on a filtered invocation.
