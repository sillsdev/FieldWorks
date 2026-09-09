# Implementation findings: improve-opentype-feature-ui

## Verified font parsing (task 1.3, 3.4)

Reader validated against two downloaded SIL fonts via `OpenTypeFontFeatureInfoReader` and a file-based table source.

**CharisSIL-Regular 6.200** (Latin) — exact values asserted in tests:
- `cv43` "Capital Eng" → Lowercase no descender / Capital form / Lowercase short stem
- `cv25` "Lowercase rams horn" → Large bowl / Small gamma
- `cv13` "Capital B hook" → Single bowl
- `ss01` "Single-story a and g"
- `liga`, `smcp` discovered with no font label; `mark`, `mkmk` discovered from GPOS

**ScheherazadeNew-Regular 4.500** (Arabic) — manual sanity scan:
- `cv70` "Damma" → Filled / Short / Crossed (directly answers the ticket's "what is cv70?")
- `cv48` "Heh" → Sindhi-style / Urdu-style / Kurdish-style
- `cv82` "Eastern digits" → 5 options
- Shaping features `fina`, `init`, `medi`, `rlig`, `rtlm`, `ccmp`, `mark`, `mkmk` present and hidden by the catalog; `kern` discovered from GPOS.

The Arabic set confirms multi-option character variants and correct hidden-feature filtering on a complex script.

## liblcm round-trip (task 2.5)

Multi-values such as `cv43=2` round-trip through the FieldWorks storage/render/export layers:
- `FontFeatureSettings.Parse`/`Normalize` preserve `cv01=2` (asserted in `FontFeatureSettingsTests`).
- `RenderEngineFactory` and `DefaultFontsControl` pass `ws.DefaultFontFeatures` through `NormalizePreservingLegacy`, which keeps the value verbatim.
- `CssGenerator.ConvertToCssFeatures` and the C++ Uniscribe path (`UniscribeSegment.cpp`) already emit/consume arbitrary non-negative values.
- The value is an opaque string to liblcm's writing-system LDML serialization; there is no per-value clamping on the FieldWorks side.
- The existing `FwFontDialogTests`/`FwFontTabTests` round-trip `smcp=1` through `FontInfo.m_features`/`ktptFontVariations` and the WS default; the path is value-agnostic, so `cv43=2` follows the same round-trip. Those suites pass.

Conclusion: no storage change needed; the multi-value round-trip is sound.

## Test results

- FwUtilsTests: 33 passed (reader vs Charis, synthetic robustness, catalog, existing FontFeatureSettings)
- FwCoreDlgControlsTests: 17 passed (provider values/labels/filtering, resx consistency, existing button tests)
- FwCoreDlgsTests: 19 passed (FontTab, FontDialog, StyleInfo integration — no regression)

## Catalog audit deviations from Paratext

Beyond the planned corrections (`dlig` visible, `aalt` hidden, `kern` default-on), fixed Paratext's 5-character `stchc` typo to the registered tag `stch`. All other entries match Paratext, including the legacy 33-tag shaping blocklist which remains hidden (asserted by `OpenTypeFeatureCatalogTests`).

## Font survey (2026-09, one machine)

Measured with the reader over 192 font files: the Windows font directory, the per-user
font store, `DistFiles` and `Downloads`. Machine-local and not reproducible from the
repository, which is why the numbers live here rather than in shipping comments. An
earlier run over 178 files reported 173 duplicate tags; the doc and the `Read` remark
carried those figures until this change.

**Script sensitivity.** 176 `cv`/`ss` tags appear more than once in a flat feature list,
and none of the duplicates differ in derived label or options, so `IsRicher` always
chooses between equal records. This agrees with the specification rather than merely
happening to hold: the registry lists *Script/language sensitivity: None* for both
`cv01`-`cv99` and `ss01`-`ss20`. The tooling follows the registry — feaLib keys
`featureNames` and `cvParameters` by tag alone, so fontmake, ufo2ft and Glyphs pipelines
cannot author per-script labels for a shared tag, and HarfBuzz documents the duplicate
script/langsys entries as raw repetition for the caller to collapse. Word, InDesign and
Paratext's own reader were not checked, so this is strong evidence rather than proof.

**Named options.** 477 cvNN features declare named options, across 15 fonts, and every
slot resolved. Their option counts:

| Options | Features |
| ------- | -------- |
| 1       | 420      |
| 2       | 40       |
| 3       | 16       |
| 4       | 1        |

The maximum anywhere is four, `ScheherazadeNew cv82`. Two decisions rest on this
distribution: the 32-option ceiling is exposed through diagnostics rather than raised,
since a font would have to ship 32 named alternates of one character to reach it; and a
feature whose single option fails to decode keeps the binary on/off fallback, because 420
of the 477 have exactly one option and a "None / Option 1" dropdown would be worse than a
checkbox.

Narrowed to the shipped SIL fonts alone, 275 cvNN features declare named options and
248 of them declare exactly one, the same shape at a different scope.

**`size`.** No font in the set declares it, so the dead `size` checkbox the catalog now
hides was never reachable in practice either.
