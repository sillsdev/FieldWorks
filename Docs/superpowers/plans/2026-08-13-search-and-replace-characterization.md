# Search and Replace Characterization Implementation Plan

Tracking issue: [LT-22696](https://jira.sil.org/browse/LT-22696).

> **For agentic workers:** REQUIRED SUB-SKILL: Use
> superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Goal:** Record current search, regular expression, replacement, indexed
discovery, and adjacent phonology behavior without changing product semantics.

**Architecture:** Add small characterization fixtures at the engine and
consumer boundaries that already own each behavior. Reuse input dimensions,
but keep adapters local so incompatible engines do not acquire a false shared
contract. Prove each new fixture is sensitive with one deliberate
wrong-expectation run, then restore the observed expectation and leave
production code unchanged.

**Tech Stack:** C++ Unit++, C# 8.0, NUnit, `IVwPattern`, ICU, .NET regular
expressions, LCModel memory test backends, `build.ps1`, and `test.ps1`.

---

## File structure

- Create
  `Src/xWorks/xWorksTests/Search/VwPatternSearchCharacterizationTests.cs` for
  managed tests of the real native literal and ICU regex engines.
- Create
  `Src/xWorks/xWorksTests/Search/BulkEditReplaceCharacterizationTests.cs` for
  preview and apply behavior through the real `ReplaceWithMethod` loop.
- Modify
  `Src/FwCoreDlgs/FwCoreDlgsTests/FwFindReplaceDlgTests.cs` only for dialog
  option wiring that cannot be asserted below the user-interface boundary.
- Modify
  `Src/Common/Controls/XMLViews/XMLViewsTests/SearchEngineTests.cs` for indexed
  prefix-search normalization and diacritic behavior.
- Modify
  `Src/LexText/Interlinear/ITextDllTests/ConcordanceControlTests.cs` for final
  regex matcher behavior over concordance custom fields.
- Modify
  `Src/Utilities/AlloVarGen/AlloGenService/AlloGenServiceTests/ReplacerTests.cs`
  for the current .NET replacement contract.
- Modify
  `Src/LexText/ParserCore/ParserCoreTests/HCLoaderTests.cs` for the separate
  phonology and combining-mark boundary.

No production file changes in this plan.

### Task 1: Characterize native literal search through `IVwPattern`

**Files:**

- Create:
  `Src/xWorks/xWorksTests/Search/VwPatternSearchCharacterizationTests.cs`
- Evidence:
  `Src/views/VwPattern.cpp:1301-1462,1955-2088`
- Historical fixture:
  `a5a380e85:Src/xWorks/xWorksTests/Avalonia/Performance/VwPatternSearchCharacterisationTests.cs`

- [x] **Step 1: Add a locale-pinned fixture over the real engine**

Create a fixture deriving from `MemoryOnlyBackendProviderTestBase`. Its pattern
factory must set all relevant options explicitly and set `IcuLocale = "root"`:

```csharp
private IVwPattern MakePattern(string text, bool matchCase = false,
	bool matchDiacritics = false, bool matchWholeWord = false)
{
	var pattern = VwPatternClass.Create();
	pattern.Pattern = TsStringUtils.MakeString(text, Cache.DefaultVernWs);
	pattern.MatchCase = matchCase;
	pattern.MatchDiacritics = matchDiacritics;
	pattern.MatchWholeWord = matchWholeWord;
	pattern.IcuLocale = "root";
	return pattern;
}
```

Use `VwStringTextSourceClass.Create()` for sources and a `Find` helper that
returns `(int Start, int Limit)`.

- [x] **Step 2: Add the literal behavior matrix**

Add focused tests asserting these observed results:

```text
NFC "caf\u00e9" pattern vs NFD "cafe\u0301" text -> (0, 5)
NFD pattern vs NFC text -> (0, 4)
"\u1ebf" pattern vs "e\u0302\u0301" -> (0, 3)
"\u1e09" pattern vs "c\u0301\u0327" -> no match
case=false, diacritics=false over "caf\u00e9 CAFE cafe" -> (0, 4)
case=false, diacritics=true -> (5, 9)
case=true, diacritics=false -> (10, 14)
whole-word=false over "cafeteria cafe" -> (0, 4)
whole-word=true -> (10, 14)
range [0,4) over "cafe\u0301" with diacritics=false -> (0, 4)
the same range with diacritics=true -> no match
U+1F600 in "ab\U0001F600cd" -> (2, 4)
reverse search over "cafe x cafe" -> (7, 11)
restart at offset 1 over "cafe cafe" -> (5, 9)
```

- [x] **Step 3: Prove fixture sensitivity**

Temporarily change the first expected limit from `5` to `4` and run:

```powershell
.\test.ps1 -TestProject "Src/xWorks/xWorksTests" `
  -TestFilter "FullyQualifiedName~VwPatternLiteralSearchCharacterizationTests" `
  -Verbosity minimal -StartedBy agent
```

Expected: one assertion failure reporting actual limit `5`. Restore `5` before
continuing. Do not commit the deliberately wrong expectation.

- [x] **Step 4: Run the literal fixture with observed expectations**

Run the same command. Expected: all literal characterization tests pass.

- [x] **Step 5: Commit the literal fixture**

```text
test: characterize native literal search behavior

Pin canonical matching, option interactions, ranges, locale, and UTF-16
offsets through the real IVwPattern engine without changing production code.
```

### Task 2: Characterize native ICU regex behavior

**Files:**

- Modify:
  `Src/xWorks/xWorksTests/Search/VwPatternSearchCharacterizationTests.cs`
- Evidence:
  `Src/views/VwPattern.cpp:1465-1565,1955-2031`

- [x] **Step 1: Add a regex factory and result helper**

The factory must set every option explicitly so the fixture does not inherit
constructor defaults:

```csharp
private IVwPattern MakeRegex(string text, bool matchCase = true,
	bool matchDiacritics = true, bool matchWholeWord = false)
{
	var pattern = MakePattern(text, matchCase, matchDiacritics,
		matchWholeWord);
	pattern.UseRegularExpressions = true;
	return pattern;
}
```

- [x] **Step 2: Add the regex behavior matrix**

Record current outcomes for:

```text
NFC pattern "\u00e9" against NFC source "\u00e9" -> no match
NFC pattern against NFD source "e\u0301" -> (0, 2)
literal "e" against NFD source "e\u0301" -> (0, 1)
MatchDiacritics true and false -> identical regex result
MatchWholeWord true and false in "cafeteria" -> identical (0, 4)
MatchCase false finds "Cafe"; MatchCase true skips it
"^" against nonempty source -> (0, 0)
"$" against nonempty source -> (length, length)
"(c)(afe)" exposes groups 0, 1, and 2 and replacement "$2$1"
forward and reverse search choose the first and last occurrence
a regex match ignores writing-system differences even when
MatchOldWritingSystem is true
```

If a probe contradicts an expected row, record the observed current result in
the test and update the working capability inventory in the same commit. Do not
change production code.

- [x] **Step 3: Prove regex fixture sensitivity**

Invert the expected result of the NFC-against-NFC case for one run. Use:

```powershell
.\test.ps1 -TestProject "Src/xWorks/xWorksTests" `
  -TestFilter "FullyQualifiedName~VwPatternRegexSearchCharacterizationTests" `
  -Verbosity minimal -StartedBy agent
```

Expected: the inverted assertion fails. Restore the observed expectation.

- [x] **Step 4: Run the regex fixture**

Run the same command. Expected: all regex characterization tests pass.

- [x] **Step 5: Commit the regex fixture**

```text
test: characterize native regular expression behavior

Record normalization, option, capture, zero-width, range, and writing-system
behavior at the ICU regex boundary without approving semantic changes.
```

### Task 3: Characterize Bulk Edit replacement

**Files:**

- Create:
  `Src/xWorks/xWorksTests/Search/BulkEditReplaceCharacterizationTests.cs`
- Evidence:
  `Src/Common/Controls/XMLViews/BulkEditBar.cs:5065-5165`
- Historical fixtures: commits `5555730e9` and `a5a380e85`

- [x] **Step 1: Build the fixture on the existing Bulk Edit test base**

Derive from `BulkEditBarTestsBase`. Build a `FieldReadWriter` for
`LexEntry.CitationForm`, an explicitly configured `IVwPattern`, and a real
`ReplaceWithMethod`. Use `FakeDoit` to read preview text from
`XMLViewsDataCache.ktagAlternateValue`, and `Doit` to read the applied citation
form.

- [x] **Step 2: Add preview and apply cases**

Characterize:

```text
literal replacement of all three occurrences
NFC pattern replacing NFD text and returning NFD output
replacement text longer than the match
replacement text shorter than the match
regex capture replacement
"^" zero-width insertion terminates with one insertion at the start
"$" zero-width insertion terminates with one insertion at the end
preview text equals apply text for every case
an unmatched row is disabled and has no preview replacement
```

Assert the text and writing system of the result. Do not add timing
assertions.

- [x] **Step 3: Prove Bulk Edit fixture sensitivity**

Change one preview expectation to an incorrect string for one filtered run:

```powershell
.\test.ps1 -TestProject "Src/xWorks/xWorksTests" `
  -TestFilter "FullyQualifiedName~BulkEditReplaceCharacterizationTests" `
  -Verbosity minimal -StartedBy agent
```

Expected: one text assertion fails. Restore the observed result.

- [x] **Step 4: Run the Bulk Edit fixture**

Run the same command. Expected: all Bulk Edit characterization tests pass.

- [x] **Step 5: Commit the Bulk Edit fixture**

```text
test: characterize bulk edit replacement behavior

Exercise the real preview and apply paths for literal, canonical, capture,
length-changing, and zero-width replacements without changing product code.
```

### Task 4: Characterize dialog, indexed search, and concordance wiring

**Files:**

- Modify:
  `Src/FwCoreDlgs/FwCoreDlgsTests/FwFindReplaceDlgTests.cs`
- Modify:
  `Src/Common/Controls/XMLViews/XMLViewsTests/SearchEngineTests.cs`
- Modify:
  `Src/LexText/Interlinear/ITextDllTests/ConcordanceControlTests.cs`

- [x] **Step 1: Expose only option state needed by dialog tests**

Add read-only test accessors for `Enabled` on Match Diacritics, Match Whole
Word, and Match Writing System to `DummyFwFindReplaceDlg`. Add one test that
sets all three options, enables regex, and records that all three become
unchecked and disabled while Match Case remains available. Disable regex and
record that the controls become enabled without restoring their old checked
values.

- [x] **Step 2: Add indexed-search normalization cases**

In `SearchEngineTests`, create entries whose lexeme forms are NFC, NFD,
unaccented, and differently accented. Search with both NFC and NFD prefixes in
the default vernacular writing system and record the exact HVO result sets.
Keep the assertions local to `StringSearcher<T>` behavior; do not claim parity
with `VwPattern`.

- [x] **Step 3: Add concordance regex cases**

Refactor the existing custom-field setup into a small helper, then record final
occurrences for a case-insensitive regex, NFC pattern against NFC and NFD
field values, and a pattern containing a capture. Assert segment identity, not
database candidate ordering.

- [x] **Step 4: Prove one fixture in each project is sensitive**

For each project, invert one expected boolean or count, run its filtered test,
observe the expected assertion failure, and restore it.

```powershell
.\test.ps1 -TestProject "Src/FwCoreDlgs/FwCoreDlgsTests" `
  -TestFilter "FullyQualifiedName~RegexOptions" -Verbosity minimal `
  -StartedBy agent
.\test.ps1 -TestProject "Src/Common/Controls/XMLViews/XMLViewsTests" `
  -TestFilter "FullyQualifiedName~IndexedPrefixSearchIgnoresAccentDifferences" -Verbosity minimal `
  -StartedBy agent
.\test.ps1 -TestProject "Src/LexText/Interlinear/ITextDllTests" `
  -TestFilter "FullyQualifiedName~ConcordanceRegex" -Verbosity minimal `
  -StartedBy agent
```

- [x] **Step 5: Run all three focused fixtures with observed expectations**

Expected: all selected tests pass.

- [x] **Step 6: Commit consumer wiring characterization**

```text
test: characterize search consumer option wiring

Record regex dialog controls, indexed Unicode discovery, and concordance
matching without asserting that the three surfaces share one contract.
```

### Task 5: Characterize AlloVarGen replacement semantics

**Files:**

- Modify:
  `Src/Utilities/AlloVarGen/AlloGenService/AlloGenServiceTests/ReplacerTests.cs`
- Evidence:
  `Src/Utilities/AlloVarGen/AlloGenService/Replacer.cs:25-59`

- [x] **Step 1: Add a direct operation builder**

Create a private helper that returns a `Replacer` with one active regex
operation for writing system `en`. Use the existing AlloGen model types rather
than XML fixtures so each behavior is isolated.

- [x] **Step 2: Add .NET replacement cases**

Record these outputs:

```text
capture swap with "$2$1"
literal dollar with "$$"
backslash behavior
missing optional capture
NFC pattern against NFC and NFD input
invalid pattern returns the unmodified value
an invalid later operation returns the result of earlier operations
empty final result becomes U+00A0
inactive and wrong-writing-system operations are skipped
```

- [x] **Step 3: Prove and restore fixture sensitivity**

Invert the capture-swap expectation, run the fixture, observe an assertion
failure, and restore it.

```powershell
.\test.ps1 -TestProject `
  "Src/Utilities/AlloVarGen/AlloGenService/AlloGenServiceTests" `
  -TestFilter "FullyQualifiedName~ReplacerTests" -Verbosity minimal `
  -StartedBy agent
```

- [x] **Step 4: Run and commit**

Expected: all selected replacement tests pass.

```text
test: characterize AlloVarGen replacement semantics

Record .NET capture, escaping, normalization, invalid-pattern, and
writing-system behavior without claiming parity with native ICU matching.
```

### Task 6: Characterize the phonology Unicode boundary

**Files:**

- Modify:
  `Src/LexText/ParserCore/ParserCoreTests/HCLoaderTests.cs`
- Evidence:
  `Src/LexText/ParserCore/HCLoader.cs:2313-2457,2532-2561,2669-2743`

- [x] **Step 1: Add domain-grammar cases**

Using the existing `AddEntry`, `AddEnvironment`, `LoadLanguage`, and
`m_loadErrors` helpers, add these focused cases:

```text
UnicodeEnvironment_BaseAndCombiningMarkLoadsAsOneSegment:
  enable AcceptUnspecifiedGraphemes, add a stem containing "a\u0307", load,
  and assert one character definition named "a\u0307" and one entry

UnicodeEnvironment_DottedCircleIsRemovedBeforeSegmentation:
  enable AcceptUnspecifiedGraphemes, add a stem containing
  "\u25CCa\u0307", load, and assert the table contains "a\u0307" but not
  "\u25CCa\u0307"

UnicodeEnvironment_OptionalCombiningSegmentLoads:
  add a phoneme represented by "a\u0307", add the environment
  "/ _ (a\u0307)", load, and assert one parsed optional environment and no
  load error
```

Keep the existing unmatched-index test and add a multi-element rewrite-rule
test to the focused verification filter so Jira LT-18767 and LT-22353 remain
represented.
Assert HermitCrab pattern or environment output and logger category. Do not
reuse regex assertions or terminology.

- [x] **Step 2: Prove fixture sensitivity**

Change one expected environment string or error category, run the focused
fixture, observe the assertion failure, and restore it.

```powershell
.\test.ps1 -TestProject "Src/LexText/ParserCore/ParserCoreTests" `
  -TestFilter "FullyQualifiedName~UnicodeEnvironment|FullyQualifiedName~InvalidPartialReduplicationEnvironment|FullyQualifiedName~MultiElementRewriteRules" `
  -Verbosity minimal `
  -StartedBy agent
```

- [x] **Step 3: Run and commit**

Expected: all selected phonology characterization tests pass.

```text
test: characterize phonology Unicode boundaries

Record combining-mark segmentation, malformed references, and multi-element
rule loading at the HermitCrab adapter without treating the grammar as regex.
```

### Task 7: Verify the characterization tranche

**Files:**

- Verify all files changed by Tasks 1 through 6.

- [x] **Step 1: Confirm production code is untouched**

Run:

```powershell
git diff --name-only main...HEAD
```

Expected: only `Docs/**` and the named test files appear. No production source
file appears.

- [x] **Step 2: Run the relevant repository build**

Run:

```powershell
.\build.ps1
```

Expected: exit 0.

- [x] **Step 3: Run the affected test projects through the repository script**

Run each project command from Tasks 1 through 6 without the narrow test filter.
Expected: zero failures in every affected project.

- [x] **Step 4: Validate commits and workspace**

Run:

```powershell
.\Build\Agent\commit-messages.ps1
git diff --check main...HEAD
git status --short --branch
```

Expected: commit validation and whitespace validation exit 0; the worktree is
clean on `regex-correctness`.

- [x] **Step 5: Update the working evidence with observed surprises**

If any probe contradicted the planned result, update only the working
inventory and recommendation decision list. Do not convert the surprise into
an approved contract or production fix.

- [x] **Step 6: Commit any evidence corrections**

```text
docs: record observed search characterization results

Update the working evidence where executable characterization contradicted
the initial source-based hypothesis. Keep unresolved product choices open.
```

If no hypothesis changed, record that fact in the handoff and do not create an
empty documentation commit.

### Task 8: Characterize phonology browse-table filtering

**Files:**

- Create:
  `Src/Common/Filters/FiltersTests/FilterStringMatcherCharacterizationTests.cs`
- Modify:
  `Docs/architecture/search-and-replace/capability-inventory.md`
- Modify:
  `Docs/architecture/search-and-replace/phonology-boundary.md`
- Modify:
  `Docs/architecture/search-and-replace/test-strategy.md`
- Evidence:
  `Src/Common/Controls/XMLViews/FilterBar.cs:1010-1075,2686-2750`
- Evidence:
  `Src/Common/Controls/XMLViews/SimpleMatchDlg.cs:143-204`
- Evidence:
  `Src/Common/Filters/RecordFilter.cs:662-1278,2387-2452`
- Configuration:
  `DistFiles/Language Explorer/Configuration/Grammar/Edit/toolConfiguration.xml:74-210`

- [x] **Step 1: Separate table filtering from phonological-rule parsing**

Record that the Phonemes, Phonological Features, and Natural Classes browse
tables use the generic filter-bar pipeline. Their normal and regular-expression
searches are not parsed by `PhonEnvRecognizer` or HermitCrab.

- [x] **Step 2: Characterize the reported search tokens**

At the managed filter-matcher boundary, record normal Anywhere matching and
regular-expression matching for `+`, `-`, `<ipa>`, and `Labial`:

```text
normal Anywhere "+" against "+" -> match
normal Anywhere "-" against "-" -> match
normal Anywhere "<ipa>" against "<ipa>" -> match
normal Anywhere "Labial" against "labial", MatchCase=false -> match
regex "+" -> invalid expression with an actionable error
regex "\+" against "+" -> match
regex "-" against "-" -> match
regex "<ipa>" against "<ipa>" -> match
regex "Labial" against "labial", MatchCase=false -> match
```

These cases distinguish standard regex syntax from a table extraction or
filter-application defect. They do not establish that `LayoutFinder` extracts
the displayed value correctly for every generated phonological-feature
column.

- [x] **Step 3: Prove fixture sensitivity and run it**

Invert one expected match, observe the assertion failure, restore it, and run:

```powershell
.\test.ps1 -SkipNative -TestProject "Src/Common/Filters/FiltersTests" `
  -TestFilter "FullyQualifiedName~FilterStringMatcherCharacterizationTests" `
  -Verbosity minimal -StartedBy agent
```

Verification on 2026-08-13 used the separately reviewed VS environment
normalization from PR 1062 because this branch intentionally does not include
that workspace fix. The deliberate wrong expectation produced one failure in
`RegExpMatcher_BarePlusIsInvalid`; after restoration the focused fixture passed
9/9, the full FiltersTests project passed 35/35, and the repository build
reported zero warnings and zero errors.

- [x] **Step 4: Record the remaining end-to-end gap**

Add planned consumer tests that build real phoneme, feature, and natural-class
rows, extract each configured cell through `LayoutFinder`, apply the installed
`FilterBarCellFilter`, and assert the exact retained objects. Cover generated
`+` and `-` feature-value columns, IPA symbols including literal angle
brackets, and `Labial` in names, values, descriptions, and feature summaries.
Keep this gap open until those tests run through the real browse configuration.

- [ ] **Step 5: Commit the characterization and evidence**

```text
test: characterize phonology table filter syntax

Record literal and ICU regular-expression behavior for reported phonology
filter tokens and identify the remaining configured-table integration gap.
```

### Task 9: Prepare the ICU 78 migration decision

**Files:**

- Research:
  `Docs/architecture/search-and-replace/icu-70-to-78-research.md`
- Modify:
  `Docs/architecture/search-and-replace/recommended-contracts.md`
- Modify:
  `Docs/architecture/search-and-replace/test-strategy.md`

- [x] **Step 1: Trace the current version graph and history**

Record the SIL native packages, `IcuVersion`, versioned C++ namespace, custom
normalization data, `icu.net`, initialization, installer, registry, and test
paths that bind FieldWorks to ICU 70. Identify the 2022 ICU 54 -> 70 migration
and later commits that retained the pin without treating inertia as product
intent.

- [x] **Step 2: Review upstream ICU 71 through 78**

Use official ICU, Unicode, and CLDR sources to record changes in Unicode data,
collation, segmentation, regex properties, locale handling, security data,
native ABI, and the C++17 toolchain requirement.

- [x] **Step 3: Classify impact on current findings**

Record explicitly that ICU 78 does not implement regex canonical equivalence
and will not repair FieldWorks' pattern-only normalization. Treat collation,
whole-word segmentation, locale data, and Unicode properties as likely
differential results. Treat UTF-16 zero-width advancement as primarily owned
by FieldWorks.

- [ ] **Step 4: Build an ICU 70-versus-78 differential gate**

Before changing package versions, run the same pinned corpus against ICU 70
and a candidate ICU 78 SIL package. Include literal and regex search,
normalization, custom collation, segmentation, properties, resource bounds,
deployment, and both phonology browse-table filters and typed grammar tests.
Classify every changed result before accepting it.

- [ ] **Step 5: Migrate the complete deployment unit**

After the differential gate and owner review, update matching SIL native
libraries, headers, import libraries, `icu.net` compatibility, ICU data,
FieldWorks custom `.nrm` resources, namespace/version constants, installer and
registry paths, test startup, and x86/x64 deployment. Build native C++ before
managed projects and keep the complete ICU 70 rollback unit until release
validation passes.
