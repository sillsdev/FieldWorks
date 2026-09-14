# Proposal: improve-opentype-feature-ui

Jira: [LT-22638](https://jira.sil.org/browse/LT-22638) (Critical, FW 9.3) — follow-up to LT-22324 / `add-opentype-font-features`.

## Why

Font Options currently shows OpenType features as cryptic four-character tags (what is `cv70`?) with only On/Off checkboxes, even though SIL fonts such as Charis SIL 6.x publish human-readable names and multi-option character variants inside the font (GSUB `featureParams` + name table). Users cannot understand or reach most features, so OpenType support is not yet useful; the ticket asks for parity with the Graphite feature experience.

## What Changes

- Add an OpenType feature-info reader in `Src/Common/FwUtils` that parses GSUB/GPOS feature lists **plus** `featureParams` and the `name` table, producing typed records: tag, font-supplied label, named cvXX options, default value. Table access goes through a delegate (`GetFontData` on the button's HDC in production; raw file bytes in tests). Parsing logic is ported from Paratext's `OpenTypeFeatures.Ttf` (the PT9.6 code the ticket references), keeping our GPOS coverage and reading `featUiLabelNameId` for cv labels (which Paratext skips).
- Add a registered-feature catalog (tag → `Hidden`/`DefaultOn`/`DefaultOff`) in FwUtils, seeded from Paratext's catalog and audited against the OpenType registry (`dlig` stays visible, `aalt` becomes hidden). Friendly names extend the existing `kstidOpenTypeFeature_<tag>` resx entries in `FwCoreDlgControls`. The catalog replaces the hardcoded 33-tag blocklist; hidden-filtering moves from the reader to the provider.
- Rewrite the OpenType provider inside `FontFeaturesButton`: cvXX features with named options render as radio submenus (None + option names, storing `cv43=2` etc.); ssXX and registered features render as checkboxes; labels are name-only in the current FLEx Graphite style; fallbacks are "Stylistic Set N", "Character Variant N", and the existing "Feature '<tag>'" format. Default-on features (`liga`, `calt`, `kern`, `clig`) initialize checked from catalog flags via the provider's existing `defaultValue` out-param.
- No changes to the menu machinery, the `tag=value` storage format, rendering, or exports — all already handle multi-valued settings (verified in `UniscribeSegment.cpp` and `CssGenerator`).
- Tests: reader tests in FwUtilsTests against the build-downloaded CharisSIL 6.200 with exact-value assertions, synthetic malformed-table fixtures that must degrade silently, `TestFontFeaturesButton` extensions for menu semantics, and a catalog/resx/old-blocklist consistency test.

## Capabilities

### New Capabilities

_None — this change extends an existing capability._

### Modified Capabilities

- `font-feature-settings`: the "OpenType feature discovery supports UI selection" requirement is extended — feature labels SHALL come from the font's `featureParams`/name table when present (catalog/resx names otherwise), cvXX features SHALL expose multiple named values instead of On/Off, and hidden/default classification SHALL come from a registered-feature catalog rather than a hardcoded blocklist. (This capability currently lives as a delta spec in `add-opentype-font-features`, not yet synced to `openspec/specs/`.)

## Impact

- **Managed C# only; no native changes.** The C++ Uniscribe path already parses arbitrary `tag=value` records.
- Affected code: `Src/Common/FwUtils` (new reader + catalog + tests), `Src/FwCoreDlgs/FwCoreDlgControls` (`FontFeaturesButton` provider, resx labels, tests), `Docs/opentype-font-features.md`.
- Test infrastructure: reuses the existing `PackageRestore.targets` Charis SIL download; no new checked-in assets.
- No storage, LDML, or data-model changes; verify at implementation time that liblcm round-trips feature strings untouched.
- Both host dialogs (Writing System Properties `DefaultFontsControl`, Styles `FwFontTab`) inherit the improvement with no changes to them.

## Non-goals

- No new dialog, grouping, tooltips, or sample glyphs (tier "b" presentation upgrade — candidate follow-up ticket).
- No localization of font-supplied names in this change (English-ranked name selection, with the ranking function isolated so a UI-language parameter can be added later without rework).
- No tri-state unset/on/off UI and no canonical stripping of explicit values (decided policy: toggles write explicit values; unset features are never written).
- No cmap or lookup-coverage parsing (only needed for sample glyphs).
- No changes to Graphite feature discovery, labels, or precedence behavior.
- No Word DOCX export changes (cvXX remains unsupported there, as documented).
