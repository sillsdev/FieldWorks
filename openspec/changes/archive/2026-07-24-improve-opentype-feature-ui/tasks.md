# Tasks: improve-opentype-feature-ui

All work is managed C# (no native changes). Groups are ordered by dependency: the FwUtils reader/catalog must exist before the provider integration, which must exist before verification.

## 1. Feature-info reader and catalog (Src/Common/FwUtils)

- [x] 1.1 Add `OpenTypeFontFeatureInfoReader` to `Src/Common/FwUtils`: parse GSUB/GPOS featureLists plus `featureParams` (ssXX `UINameID`; cvXX `featUiLabelNameId` and named parameter name IDs) and the `name` table with ranked record selection (Windows-English, then Unicode/Windows, then Mac Roman), all reads bounds-checked, input via table-source delegate (four-character tag → bytes); return typed records (tag, font label, option labels). Port parsing logic from Paratext `OpenTypeFeatures.Ttf`, keeping GPOS and adding the cv label read.
- [x] 1.2 Add `OpenTypeFeatureCatalog` to `Src/Common/FwUtils` mapping registered tags to `Hidden`/`DefaultOn`/`DefaultOff`, seeded from Paratext's `RegisteredFeatureCatalog` and audited against the OpenType registry: `dlig` visible, `aalt` hidden, every tag from the old `s_nonUserConfigurableTags` blocklist hidden, `liga`/`clig`/`calt`/`kern` default-on. (Catalog also carries the English fallback name so the resx only holds the localizable subset.)
- [x] 1.3 Wire `CharisSIL-Regular.ttf` (already downloaded by `Build/PackageRestore.targets`) into `FwUtilsTests` output the way `TestViews.vcxproj` consumes it, and add reader tests with exact-value assertions: cv43 "Capital Eng" with 3 options in order, cv25 with 2, cv13 with 1, ss01 "Single-story a and g", `liga`/`smcp` present without params, `mark`/`mkmk` discovered from GPOS.
- [x] 1.4 Add synthetic malformed-table tests in `FwUtilsTests` (truncated featureList, featureParams offset past table end, name record outside storage, undecodable platform/encoding, zero-length names): each degrades to tag-only records with no exception.
- [x] 1.5 Add catalog consistency tests: old blocklist tags are all `Hidden` (FwUtilsTests); every `kstidOpenTypeFeature_<tag>` resx key maps to a visible feature (FwCoreDlgControlsTests).

## 2. Provider integration (Src/FwCoreDlgs/FwCoreDlgControls)

- [x] 2.1 Rewrite `OpenTypeFontFeatureProvider` inside `FontFeaturesButton.cs` to consume reader records: `GetFeatureValues` returns `{0..N}` with catalog-driven defaults (`DefaultOn` → 1), named cvXX value labels ("None" + option names), hidden filtering at the provider, LOGFONT-keyed cache now storing the typed records.
- [x] 2.2 Label composition in the provider: font-supplied name first, then `kstidOpenTypeFeature_<tag>` resx name, then catalog English name, then "Stylistic Set {0}" / "Character Variant {0}" / existing "Feature #{0}" fallbacks; name-only (no tag suffix); Graphite label behavior untouched.
- [x] 2.3 Update `FwCoreDlgControls.resx`: add "None" and the numbered stylistic-set / character-variant fallback strings, remove the dead `aalt` and `ccmp` label resources (both now hidden).
- [x] 2.4 Extend `TestFontFeaturesButton.cs`: multi-option cvXX exposes None + named options and stores `cv43=2`, `cv43=2` round-trips through the renderer-neutral string, unset `liga` initializes on / `smcp` off, unnamed cvXX falls back to binary, hidden features filtered, unknown vendor tag stays visible, resx labels map to visible features.
- [x] 2.5 Verify liblcm round-trip: `DefaultFontFeatures` flows through `NormalizePreservingLegacy` (preserves `cv01=2`, tested) and is stored as an opaque LDML string with no per-value clamping; existing FwFontDialog/FwFontTab round-trip suites pass. Documented in `research.md`.

## 3. Verification and documentation

- [x] 3.1 Build with `.\build.ps1` and run `.\test.ps1` for FwUtilsTests (33 pass), FwCoreDlgControlsTests (17 pass), and FwCoreDlgsTests (19 pass: FontTab, FontDialog, StyleInfo — no regression).
- [x] 3.2 Coverage (test.ps1 -Coverage): OpenTypeFeatureCatalog 100%, OpenTypeFontFeatureInfo 100%, OpenTypeFontFeatureInfoReader 86% (uncovered = Mac Roman fallback / platform-1 decode). Provider exercised by the 17 FwCoreDlgControlsTests.
- [x] 3.3 Manual acceptance per LT-22638 in the live app (fieldworks-winapp): Format > Set up Vernacular Writing Systems > Font tab > Charis > Font Features shows meaningful names; cv sub-options selectable and rendering changes confirmed. Manual testing passed (confirmed by Jason).
- [x] 3.4 Sanity-check the reader against `ScheherazadeNew-Regular.ttf` (Arabic): cv70 "Damma", cv82 "Eastern digits" (5 options), shaping features hidden. Recorded in `research.md`.
- [x] 3.5 Update `Docs/opentype-font-features.md`: names come from the font/catalog, cvXX multi-values, catalog-based hidden/default classification, unchanged export behavior.
