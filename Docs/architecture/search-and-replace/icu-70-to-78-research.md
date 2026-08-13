# ICU 70 to 78 upgrade research

Status: working evidence, not a normative product contract.

Research date: 2026-08-13.

This note investigates the FieldWorks ICU 70 pin, the upstream changes through
ICU 78, and the effect an upgrade could have on the search-and-replace
correctness work. It distinguishes upstream ICU from the SIL-maintained native
packages and the `icu.net` managed wrapper. The links to source files and
commits are evidence, not approval to change the product contract.

## Findings in brief

- FieldWorks has a deliberate, repository-wide ICU 70 integration rather than
  an isolated NuGet pin. The native package, versioned C++ namespace, custom
  normalization data, test initialization, installer environment, and managed
  wrapper all encode 70.
- The last major upgrade in the repository history moved from checked-in ICU 54
  headers to SIL's ICU 70 packages in 2022. Later package-management and
  `icu.net` changes did not advance the native ICU major version. The history
  does not record a product decision that ICU 70 must remain forever; it shows a
  downstream compatibility surface that has not been migrated.
- ICU 71--78 update Unicode, CLDR locale data, collation, segmentation, script
  and property data, and security data. They also change native build and ABI
  requirements. These changes can alter literal search and word-boundary
  characterization, but they do not supply canonical-equivalence regular
  expressions.
- The current NFC/NFD regex asymmetry is in FieldWorks: it NFD-normalizes the
  pattern and copies the source text directly. ICU's regex documentation still
  says `UREGEX_CANON_EQ` is not implemented. Upgrading ICU alone will not fix
  that finding.
- A safe upgrade needs a matching SIL ICU package and liblcm/ICU data package,
  an all-native rebuild before managed projects, installer and test-path
  changes, and a differential characterization run against ICU 70.

## Why the repository remains on ICU 70

The current version graph has several independent-looking values that must be
changed together:

| Surface | Current evidence | Upgrade implication |
| --- | --- | --- |
| SIL package version | [`Build/SilVersions.props`](../../../Build/SilVersions.props#L24) sets `IcuNugetVersion` to `70.1.152`. | Obtain a matching `Icu4c.Win.Fw.Lib` and `Icu4c.Win.Fw.Bin` package; this is not the same thing as selecting an upstream ICU tarball. |
| Native ICU major | [`Build/SetupInclude.targets`](../../../Build/SetupInclude.targets#L17) and [`Build/Installer.targets`](../../../Build/Installer.targets#L18) set `IcuVersion` to 70. [`Include/IcuCommon.h`](../../../Include/IcuCommon.h#L42) defines `ICU_VERSION` as `"70"`. | Headers, libraries, DLL names, versioned C++ symbols, data directories, and tests must agree. |
| Custom data | [`Build/PackageRestore.targets`](../../../Build/PackageRestore.targets#L91) expects `nfc_fw.nrm` and `nfkc_fw.nrm`; the same file copies `icudt70l` content and stages `DistFiles/Icu70`. | Regenerate or obtain the matching custom data package, then update every `icudt70l`/`Icu70` path. A native DLL from one major and data from another is not a supported combination. |
| Managed/native startup | [`FieldWorks.cs`](../../../Src/Common/FieldWorks/FieldWorks.cs#L158) calls `ConfineIcuVersions(70)`; both test assembly attributes initialize ICU 70 ([tests](../../../Src/AssemblyInfoForTests.cs#L44), [UI-independent tests](../../../Src/AssemblyInfoForUiIndependentTests.cs#L26)). | Update `icu.net` compatibility and all initialization/binding paths as one change. |
| Installer/test deployment | The installer sets `ICU_DATA` to `Icu70` ([`FLExInstaller/CustomComponents.wxi`](../../../FLExInstaller/CustomComponents.wxi#L39)); the C++ test script also chooses `Icu70` ([`Invoke-CppTest.ps1`](../../../Build/scripts/Invoke-CppTest.ps1#L516)). | Verify installed and worktree test runs, x86 and x64, rather than only compiling. |

The native namespace is also explicit: [`IcuCommon.h`](../../../Include/IcuCommon.h#L245)
imports `icu_70`, and [`StrUtil.cpp`](../../../Src/Generic/StrUtil.cpp#L40)
builds the ICU-versioned registry key from `ICU_VERSION`. The Unicode Character
Editor launch overrides set `ICU_DATA` to `Icu70` in both installer variants
([WiX 3](../../../FLExInstaller/Overrides.wxi#L16),
[WiX 6](../../../FLExInstaller/wix6/Overrides.wxi#L16)). `Build/Windows.targets`
still contains an older explicit `icudt68.dll` entry; this is a reminder to
audit every version token and not blindly replace only the current 70 paths.

The 2022 commit ["Use icu version 70 from Nuget package"](https://github.com/sillsdev/FieldWorks/commit/7f52d9cdbcb7f0d8b38744b45c91694012653f1c)
changed `SetupInclude`, `IcuCommon.h`, native package restore, and the
architecture-specific package paths from ICU 54 to ICU 70. It also removed the
checked-in ICU headers in favor of headers copied from `Icu4c.Win.Fw.Lib`. The
SIL package's own nuspec identifies the package as a FieldWorks build of ICU4C,
and says its major version corresponds to the ICU release ([library nuspec](../../../packages/icu4c.win.fw.lib/70.1.152/icu4c.win.fw.lib.nuspec),
[binary nuspec](../../../packages/icu4c.win.fw.bin/70.1.152/icu4c.win.fw.bin.nuspec)).
The `70.1.152` suffix is therefore a SIL package/build version around ICU 70,
not ICU 70.1.152 as an upstream ICU major release.

The later history supports "not migrated" rather than "intentionally held by a
new ICU decision":

- Commit [`5711bf6be`](https://github.com/sillsdev/FieldWorks/commit/5711bf6bedf311f8cbe45637c73d9f7adb5ef716) centralized SIL versions in
  `Build/SilVersions.props`, but retained `70.1.152`.
- Commit [`1d77f0700`](https://github.com/sillsdev/FieldWorks/commit/1d77f0700a5033d1912cab73f2fbac9e628c1a48) moved package handling while retaining the same native package.
- Commit [`5543cf1c0`](https://github.com/sillsdev/FieldWorks/commit/5543cf1c070e363c243a40f391537a16b94727d7) updated other SIL tooling and still left the ICU package at `70.1.152`.

The public [SIL `Icu4c.Win.Fw.Lib` package history](https://www.nuget.org/packages/Icu4c.Win.Fw.Lib/)
currently ends at `70.1.153`; it lists no SIL FieldWorks package for ICU 71
through 78. FieldWorks is one SIL package revision behind that public ICU 70
package, but moving to `.153` would still be ICU 70. Advancing the major
requires SIL to build or adopt a matching ICU 78 library, binary-tools, and
data package set rather than changing the existing version property alone.

There is a second, separate version axis. The repository currently uses
`icu.net` 3.0.1 ([`Directory.Packages.props`](../../../Directory.Packages.props#L147)).
Its local README describes a C# wrapper for a subset of the ICU C API and says
that native Windows ICU packages must also be present
([`packages/icu.net/3.0.1/README.md`](../../../packages/icu.net/3.0.1/README.md)).
The wrapper is not the ICU data or native C++ distribution. The history shows
wrapper binding changes independently: [2.8.0-beta.10](https://github.com/sillsdev/FieldWorks/commit/7fa07ec2ad111a815f9c98e4ca033a70f29c8cc0),
[2.8.1-beta.1](https://github.com/sillsdev/FieldWorks/commit/f98a189ab99b545cc5bf19b50949a89d7d5ecf69),
and [binding updates](https://github.com/sillsdev/FieldWorks/commit/4549fe5c41095aabbf572ded7387f7066751b38b).
The ICU native-major migration and the managed assembly/binding migration
should consequently be planned and tested separately, even if released
together.

## What upstream ICU 71--78 changes

The ICU release pages are the authoritative summaries for the release-to-data
mapping: ICU 70 corresponds to Unicode 14 and CLDR 40; ICU 71 to CLDR 41; ICU
72 to Unicode 15 and CLDR 42; later releases advance to Unicode 17 and CLDR 48
in ICU 78 ([ICU downloads](https://unicode-org.github.io/icu/download/)). The
following are the changes with plausible FieldWorks impact; Java-only API
changes are not treated as FieldWorks behavior.

| Release | Upstream changes relevant to this audit |
| --- | --- |
| [71](https://icu.unicode.org/download/71) | CLDR 41 locale data, phrase-based Japanese line breaking, Hindi Latin locale data, and a timezone-data update. The release still supports C++11 and Windows 7. |
| [72](https://icu.unicode.org/download/72) | Unicode 15 and CLDR 42 add scripts, emoji, and data. Word segmentation changed for letters joined after `:` and `@`; locale-data lookup was improved. This can change whole-word boundaries and locale fallback. |
| [73](https://icu.unicode.org/download/73) | CLDR 43 and Japanese/Korean short-text line breaking. Chinese GB18030-2022 support changes short collation behavior. CLDR root collation also improves handling of quote-like punctuation. ICU 73.2 reverses the ICU 72 `@` word-segmentation behavior. |
| [74](https://icu.unicode.org/download/74) | Unicode 15.1 and CLDR 44; line-breaking updates for some Southeast Asian scripts; security-spec updates; new identifier-related properties; NFKC simple case-folding support; Locale/LocaleBuilder C wrappers; and an ExternalBreakEngine preview. |
| [75](https://icu.unicode.org/download/75) | CLDR 45; `Identifier_Status` and `Identifier_Type` APIs from UTS #39; robust string/buffer API work. ICU 4C now requires C11 and C++17, which is a build prerequisite for a FieldWorks native migration. |
| [76](https://unicode-org.github.io/icu/download/76.html) | Unicode 16 and CLDR 46; new scripts/emoji and `Indic_Conjunct_Break`; IDNA changes; and a significant root-collation realignment (including punctuation/digit ordering). Segmentation changes were deliberately deferred to 77. Header-only C++ APIs and C++17 are part of the new build surface. |
| [77](https://unicode-org.github.io/icu/download/77.html) | CLDR 47 and segmentation-conformance fixes. Root-colon tailoring and Swedish/Finnish changes introduced in 72 were reverted; Indic grapheme clusters use current `Indic_Conjunct_Break`; line-break behavior and C/C++ USet/UCollator APIs received fixes. |
| [78](https://unicode-org.github.io/icu/download/78.html) | Unicode 17 and CLDR 48; new scripts/CJK/emoji, word/line segmentation improvements, and changed default recommendations for `Identifier_Status`/`Identifier_Type`. Locale was optimized and new UTF-8/16/32 code-point iterator headers were added. ICU 78 requires C++17 and the current stable maintenance release is 78.3 ([release history](https://unicode-org.github.io/icu/download/)). ICU 78.2 fixed an uninitialized-memory issue for a bogus locale passed to `ucasemap_setLocale`; 78.3 fixed an MSVC warning in `utfiterator.h`. |

These are upstream ICU changes. A SIL `Icu4c.Win.Fw.*` package may carry a
patch, a reduced or full data build, a package-only revision, or a different
compiler/runtime choice. The exact SIL package and its build recipe must be
audited before treating an upstream release note as a FieldWorks guarantee.

## Effect on the current correctness findings

### NFC/NFD regex asymmetry: no automatic fix

FieldWorks converts the pattern to NFD in [`VwPattern::Compile`](../../../Src/views/VwPattern.cpp#L1955),
then the regex path copies the source text directly into an ICU
`RegexMatcher` ([regex path](../../../Src/views/VwPattern.cpp#L1500)). The
current capability inventory records the resulting finding: an NFC pattern can
match an equivalent NFD source range but not the equivalent NFC source
([capability inventory](capability-inventory.md)).

ICU's regular-expression guide states that ICU implements UTS #18 level 1 and
selected level-2 behavior, but `UREGEX_CANON_EQ` is "not yet implemented"
([ICU regex user guide](https://unicode-org.github.io/icu/userguide/strings/regexp.html)).
That remains true in the ICU 78 documentation. Therefore ICU 70 to 78 is not a
canonical-equivalence regex upgrade. The product fix, if approved, belongs in
the FieldWorks boundary: normalize the source and/or use an explicitly
canonical matching adapter, while preserving UTF-16 index mapping and capture
ranges. Keep the current NFC/NFD matrix as a regression test.

Unicode normalization itself has a stronger stability guarantee: once a
character is assigned, its canonical combining class and decomposition mappings
are stable, and normalized text remains normalized in future versions
([Unicode normalization stability policy](https://www.unicode.org/policies/stability_policy.html),
[UAX #15](https://unicode.org/reports/tr15/)). New characters can of course
gain data in a newer ICU. Custom `nfc_fw.nrm` and `nfkc_fw.nrm` are downstream
data and still need to be regenerated and compared.

### Literal collation and diacritics: likely behavior changes

FieldWorks' literal path uses ICU `StringSearch`, `BreakIterator`, and
`Collator`, and explicitly enables `USEARCH_CANONICAL_MATCH` in
[`VwPattern.cpp`](../../../Src/views/VwPattern.cpp#L1379). ICU StringSearch
defines a match through collator equality; canonical matching, strength,
locale, tailoring, and whole-word boundaries are separate inputs ([StringSearch
guide](https://unicode-org.github.io/icu/userguide/collation/string-search.html)).
Changes in CLDR collation data from 40 to 48, ICU 73's punctuation/GB18030
updates, and ICU 76's root-collation realignment can therefore change match
acceptance or sort keys even when the application code is unchanged.

The existing FieldWorks contract says a writing system's standard or custom
collation is authoritative, and tests must set a locale rather than inherit the
host locale ([historical evidence](historical-evidence.md)). An upgrade should
compare ICU 70 and the candidate with explicit locales and custom rules; it
should not silently bless changed results as defects or compatibility changes.
The `UCHAR_DIACRITIC`/`UCHAR_EXTENDER` checks in the FieldWorks search code are
also data-dependent for newly assigned characters, so retain accented and
combining-mark vectors.

### UTF-16 offsets and zero-width matches: mostly FieldWorks behavior

ICU regex and StringSearch expose native UTF-16 indexes. `RegexMatcher.start()`
and `.end()` report the match range, with `end()` just after the match; ICU does
not define FieldWorks' replacement iteration policy. FieldWorks maps those
indexes and special-cases zero-length matches such as `^`, `\n*`, and `\s*` in
its own `FindIn` path. A major-version upgrade should not be expected to change
that policy. Keep tests for supplementary-plane text, zero-width matches at
the beginning/middle/end, forward and backward search, Replace Once/All, and
the invariant that no operation splits a surrogate pair. Review any changed
offset only after checking whether it is an upstream matcher change or a local
range-advance rule.

### Segmentation: yes, especially whole-word and grapheme behavior

Literal whole-word search obtains a locale-specific `BreakIterator`. ICU 72,
73.2, 76, and 77 include notable word/line/grapheme boundary changes, with
especially visible transitions around `:`/`@`, Indic conjuncts, and some
Southeast Asian scripts. ICU regex `\b` uses its regex word-boundary rules; the
optional `(?w)` mode is a separate UAX #29-style behavior described by the
regex guide. FieldWorks does not translate its literal whole-word option into a
regex word-boundary option. Characterize both engines separately.

### Unicode property and script data: yes for new data

The regex guide says Unicode property classes such as `\p{...}` use the
properties known by the ICU data installed at runtime. New scripts, properties,
and code points from Unicode 15--17 can change `\p`/`\P` results. The same
applies to native `u_hasBinaryProperty` calls for diacritics and extenders.
Normalization stability does not imply that every property or collation result
is stable. Add vectors for existing assigned characters, new scripts, default
ignorables/unassigned code points, supplementary-plane values, and every
property used by FieldWorks.

### Locale handling and security: data/API changes, not a search contract

CLDR updates and ICU 72 locale lookup changes can affect writing-system locale
fallback, collation, and break iteration. FieldWorks must continue to pass an
explicit writing-system locale and custom rules. ICU 74--78 add or change
identifier/security properties and improve malformed-locale handling, but
FieldWorks' current search path does not become a security policy merely by
upgrading. The regex guide recommends a time limit for untrusted patterns; the
current inventory finds no visible ICU regex time/stack limit. Add a resource
policy test (time/heap/cancellation) rather than relying on a version bump.

### Phonology: table filters use ICU; rule grammar remains separate

There are two different paths. Phonological environments are parsed by
`PhonEnvRecognizer` and loaded into HermitCrab as typed domain grammar, not
regular expressions ([phonology boundary](phonology-boundary.md)). A new ICU
major must not redefine that grammar without an explicit external dependency
change and domain-owner decision.

The Phonemes, Phonological Features, Bulk Edit Phoneme Features, and Natural
Classes browse tables are different. Their configured columns are rendered by
`LayoutFinder`, filtered by `FilterBarCellFilter` and managed matchers, and
matched by native `VwPattern`. Normal filtering therefore uses ICU collation
and regular-expression filtering uses ICU regex. The reported `+`, `-`,
`<ipa>`, and `Labial` cases belong in the ICU 70-versus-78 differential suite,
including an end-to-end check that the configured cell exposes the text the
user sees. Bare `+` must be treated as invalid regex syntax while `\+` is the
literal-plus expression.

## ABI, data, and interop constraints

ICU's design documentation explains that public C++ classes are in a versioned
namespace such as `icu_70`, and C++ ABI compatibility is not promised across
major releases; native C++ consumers must be rebuilt. The C APIs have a more
stable binary-compatibility policy, but they still depend on matching DLL
names, data, and runtime behavior ([ICU design](https://unicode-org.github.io/icu/userguide/icu/design),
[ICU packaging](https://unicode-org.github.io/icu/userguide/icu4c/packaging.html)).
This matters directly to FieldWorks because `IcuCommon.h` includes package
headers, native projects link `.lib` files, and managed startup calls
`ConfineIcuVersions` before `icu.net` initialization.

ICU data contains character properties, normalization tables, collation data,
break-iterator rules, and locale data. The default data-file naming and
`ICU_DATA` lookup rules are documented in [ICU data management](https://unicode-org.github.io/icu/userguide/icu_data/).
FieldWorks adds custom normalization resources and stages them beneath
`DistFiles/Icu$(IcuVersion)`. Consequently, all of the following must move as
one deployment unit:

1. SIL native libraries, import libraries, headers, and binary tools.
2. The matching `icudt78l` data file and FieldWorks custom `.nrm` resources.
3. x86/x64 output, the versioned C++ namespace, and `icu.net` native-library
   discovery/configuration.
4. Installer `ICU_DATA`, registry names, test environment setup, and any
   hard-coded DLL names. The repository also contains older ICU version tokens
   (for example `icudt68` in `Build/Windows.targets`) that need an audit rather
   than a blind global replacement.

ICU 75's C++17 requirement is an explicit toolchain gate. It does not mean that
the managed projects must target a new .NET runtime, but it does mean that the
native build image, compiler flags, and all C++ consumers must be validated
before package restore can be considered successful.

## Recommended sequencing

1. **Freeze and measure ICU 70.** Record native literal and regex results,
   canonical normalization, collation sort keys, locale fallback, break
   boundaries, Unicode properties, UTF-16 ranges, zero-width replacement, x86
   and x64 startup, and installer/test data discovery. Keep the baseline
   artifacts with the characterization tests.
2. **Resolve SIL dependencies first.** Obtain or publish matching
   `Icu4c.Win.Fw.Lib/Bin` packages for the selected ICU 78 maintenance release,
   plus a matching `SIL.LCModel.Core`/liblcm data package containing
   `icudt78l/nfc_fw.nrm/nfkc_fw.nrm`. Verify whether the current `icu.net`
   release supports that native build; update binding redirects and the
   `Microsoft.Extensions.DependencyModel` pin only when the wrapper/package
   compatibility matrix requires it.
3. **Change the version graph together.** Update the central package version,
   every `IcuVersion`/`ICU_VERSION`/`ConfineIcuVersions` and test attribute,
   data paths, installer environment, DLL names, package-restore paths, and
   generated ICU headers. Rebuild native C++ first, then managed projects, as
   required by the repository build order.
4. **Run differential characterization.** Execute the ICU 70 and candidate
   suites with fixed locales and fixed custom collation rules. Classify each
   changed result as expected upstream data movement, a FieldWorks regression,
   or an unresolved product-contract decision. Do not use a new ICU result as
   an implicit contract change.
5. **Decide rollout/rollback.** If old collation or segmentation results must
   remain bug-for-bug compatible, retain a pinned side-by-side process or defer
   that surface while the product/domain owners approve a new contract. Keep
   package, installer, and data rollback artifacts together.

## Required characterization tests

| Area | Minimum vectors |
| --- | --- |
| Normalization | NFC/NFD/NFKC and NFKC simple case-folding; canonical ordering; assigned versus newly assigned code points; FieldWorks `nfc_fw.nrm` and `nfkc_fw.nrm`. |
| Native regex | NFC/NFD pattern/source matrix, multiple combining marks, `UREGEX_CANON_EQ` unsupported behavior, case-fold expansions such as `ß`/`ss`, captures and UTF-16 ranges, invalid UTF-16/lone surrogates, `\p`/`\P` scripts/properties, invalid patterns, and resource-limit/cancellation behavior. |
| Literal search/collation | Root and each representative writing-system locale; standard and custom rules; case/diacritic strengths; canonical match; punctuation/quote variants; contractions/expansions; sort keys; supplementary-plane and combining-mark offsets. |
| Segmentation | Literal whole-word and grapheme boundaries around combining marks, `:`/`@`, Indic conjuncts, Southeast Asian scripts, and locale-specific words; compare regex `\b` with `(?w)\b` where supported. |
| Zero-width/replacement | `^`, `$`, `\b`, `\n*`, `\s*`, lookahead, start/middle/end positions, surrogate pairs, forward/backward search, Replace Once/All, growing replacement text, and no split of original formatted runs. |
| Managed interop/deployment | `icu.net` init/cleanup, `ConfineIcuVersions`, binding redirects and assembly identity, native DLL discovery, `ICU_DATA`, registry/installer staging, clean worktree tests, x86, and x64. |
| Phonology browse tables | Real configured-cell extraction and retained-object results for normal and regex `+`, `-`, `<ipa>`, and `Labial` filters under ICU 70 and the candidate. |
| Phonological grammar | Separate parser/HermitCrab vectors, including combining marks and malformed input. Do not assert typed grammar behavior through an ICU regex test. |

The central acceptance point is narrow: ICU 78 may be a worthwhile data,
security, and maintenance upgrade, but it is not the fix for FieldWorks'
canonical-equivalence regex asymmetry. That behavior requires a separately
owned FieldWorks change and its own characterization/approval cycle.
