# Search and replace capability inventory

Status: working evidence, not a normative product contract.

## Capability map

| Surface | Engine and owner | Important current semantics | Primary tests |
| --- | --- | --- | --- |
| Native literal search | ICU `StringSearch`, `BreakIterator`, and `Collator` in `VwPattern` | Case, diacritics, whole word, writing system, style, tags, locale, custom collation, and canonical matching | `Src/views/Test/TestVwPattern.h` |
| Native regular expression search | ICU `RegexMatcher` in `VwPattern` | Case-insensitive is the only option translated to an ICU regex flag; pattern text is converted to NFD | `Src/views/Test/TestVwPattern.h` |
| Managed matchers and filters | `RegExpMatcher` and `SimpleStringMatcher` over `IVwPattern` | Persist case and diacritic options; derive locale and collation rules from the pattern writing system | `Src/Common/Filters/RecordFilter.cs` and filter tests |
| Find and Replace | `FwFindReplaceDlg` over `IVwPattern` | Regex mode disables diacritics, whole word, and writing-system options; replacement preserves formatted text runs | `Src/FwCoreDlgs/FwCoreDlgsTests/FwFindReplaceDlgTests.cs` |
| Bulk Edit replace | `ReplaceWithMethod` over `IVwPattern` | Preview and apply use the native search engine; replacement iteration and normalization affect data safety and ranges | `Src/xWorks/xWorksTests/BulkEditBarTests.cs`; additional committed characterization assets are pinned in the test strategy |
| Concordance | `RegExpMatcher` or literal matchers with a database candidate prefilter | Regex bypasses the accent-aware literal prefilter; the final matcher remains authoritative | `Src/LexText/Interlinear/ITextDllTests/ConcordanceControlTests.cs` |
| Indexed Find and Go | LCModel `StringSearcher<T>` | Prefix or full-text discovery over indexed visible fields; the characterized default prefix search returns NFC, NFD, unaccented, and differently accented forms for NFC, NFD, or unaccented queries; not an `IVwPattern` or regex contract | `Src/Common/Controls/XMLViews/XMLViewsTests/SearchEngineTests.cs` |
| AlloVarGen matching | Managed `RegExpMatcher` over native `IVwPattern` | Delegates to the native ICU engine | Matcher tests are currently disabled |
| AlloVarGen replacement | .NET `Regex.Replace` | Different engine, syntax, replacement rules, error handling, and resource policy from native matching | AlloGen service tests and XML fixtures |
| Phonological environments | `PhonEnvRecognizer` and HermitCrab pattern nodes | Domain grammar rather than regex; shares Unicode and malformed-input concerns | `Src/LexText/ParserCore/ParserCoreTests/HCLoaderTests.cs` |
| `xp_IsMatch` | ICU lowercasing followed by `wcsstr` | Case-insensitive substring search despite the pattern-like name | Native database extension code |

`xp_IsMatch` is documentation-only in the initial characterization tranche.
Its current call sites and supported status must be established before tests or
renaming are proposed.

## Native engine boundary

Literal and regex search share the `IVwPattern` object but not the same
semantic pipeline. Literal search applies collation, canonical matching,
diacritic checks, word boundaries, and text-property checks. Regex search uses
ICU `RegexMatcher`, checks result bounds, and bypasses the literal candidate
checks. The source records that diacritic and property filtering are not
practical after a regex candidate is returned
(`Src/views/VwPattern.cpp:1465-1565`).

Compilation also has an important asymmetry. The pattern is converted to NFD
before compiling either search engine (`Src/views/VwPattern.cpp:1955-2015`),
while the regex source buffer is copied directly
(`Src/views/VwPattern.cpp:1487-1502`). This is an observed implementation fact;
tests must determine its visible consequences before any correction is
proposed.

Characterization confirms the visible regex asymmetry: an NFC pattern matches
an equivalent NFD source range but not the equivalent NFC source. Concordance
inherits the same final-matcher behavior. This is a compatibility finding, not
an approved product contract.

## Option ownership

The option values are not centrally owned:

- Native `VwPattern` initializes `MatchDiacritics` to true
  (`Src/views/VwPattern.cpp:32-39`).
- Persisted filter XML treats a missing `matchDiacritics` value as false
  (`Src/Common/Filters/RecordFilter.cs:976-991`).
- Find and Replace clears and disables diacritics, whole word, and writing
  system when regex mode is enabled
  (`Src/FwCoreDlgs/FwFindReplaceDlg.cs:1421-1441`).
- Search consumers set locale and custom collation rules with different
  degrees of explicitness.

These differences must be classified as intent-specific defaults,
compatibility constraints, or defects. They must not be unified merely because
they share an interface.

## Replacement boundary

Matching and replacement are separate contracts. Native replacement exposes
ICU capture groups and formatted `ITsString` results. Find and Replace and Bulk
Edit iterate matches while preserving selections, text properties, related
annotations, and undo behavior. AlloVarGen instead uses .NET replacement
syntax. Cross-engine parity is not currently established.

## Resource boundary

View traversal supports cooperative cancellation through `IVwSearchKiller`,
but an individual ICU regex operation has no visible time or stack limit.
AlloVarGen calls .NET `Regex.Replace` without a timeout. This establishes a
missing resource-policy owner, not proof of a current exploitable failure or a
testable completion bound.
