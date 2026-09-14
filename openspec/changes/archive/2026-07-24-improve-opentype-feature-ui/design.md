# Design: improve-opentype-feature-ui

Jira: [LT-22638](https://jira.sil.org/browse/LT-22638). Decisions below were resolved in a design review with Jason Naylor (2026-07-23) against the guidance at writingsystems.info (opentype, feature-ui, and user-interface-strings topics) and Paratext 9.6's implementation (`OpenTypeFeatures.Ttf` in the Paratext repo).

## Context

`FontFeaturesButton` (Src/FwCoreDlgs/FwCoreDlgControls) drives the Font Options menu used by both Writing System Properties (`DefaultFontsControl`) and the Styles Font tab (`FwFontTab`). Since LT-22324 it discovers OpenType feature tags via GDI `GetFontData` over GSUB/GPOS, but:

- It reads only the four-character tags from the FeatureList, never the `featureParams` offset — so font-supplied names for ssXX/cvXX (the only place those names exist, per the OpenType spec) are never seen.
- `OpenTypeFontFeatureProvider.GetFeatureValues` hardcodes `{0,1}` with default 0, so every feature renders as an On/Off checkbox; cvXX multi-option variants are unreachable.
- Labels come from a 21-entry hardcoded resx map (ss01–ss05 only, no cvXX); everything else shows a bare tag.
- Filtering uses a hardcoded 33-tag blocklist; default-on shaping features (`liga`, `calt`, `kern`, `clig`) display unchecked when unset, which misrepresents actual rendering.

Everything below the UI already supports the fix: `FontFeatureSettings` stores any non-negative `tag=value`, the C++ Uniscribe path (`UniscribeSegment.cpp: TryParseFontFeatureRecords`) passes arbitrary values into `OPENTYPE_FEATURE_RECORD.lParameter`, CSS export emits numeric values, and the menu machinery in `OnClick` already renders multi-value radio submenus (that is how Graphite features work today). Only the discovery/provider layer lies.

Verified ground truth (CharisSIL-Regular 6.200, the font the build already downloads via `PackageRestore.targets`): 34 cvXX features all carrying featureParams — e.g. cv43 "Capital Eng" with 3 named options, cv25 "Lowercase rams horn" with 2, cv13 "Capital B hook" with 1 — plus 5 named ssXX (ss01 "Single-story a and g") and GPOS-only `mark`/`mkmk`.

## Goals / Non-Goals

**Goals:**

- Feature names come from the font (`featureParams` → name table) for ssXX/cvXX, from a catalog/resx for registered tags; bare tags appear only inside fallback strings.
- cvXX features with named options render as multi-value radio submenus (None + option names), storing `cv43=2` etc.
- Registered-feature catalog supplies `Hidden` (replaces the blocklist) and `DefaultOn`/`DefaultOff` (honest initial check state).
- Strict parity with the existing Graphite menu experience; both host dialogs inherit the change with zero modifications.
- Reader logic is a standalone, unit-testable FwUtils class, exercised against the real Charis font and synthetic malformed fixtures.

**Non-Goals:**

- New dialog, grouping, tooltips, sample glyphs (candidate follow-up ticket; requires cmap/lookup parsing deliberately excluded here).
- Localization of names (name-record ranking prefers English; the ranking function stays isolated so a UI-language parameter can be added later).
- Tri-state unset/on/off UI or canonical stripping of explicit values (policy: toggles write explicit values; unset features are never written — the existing `Int32.MaxValue` convention).
- Graphite discovery/label/precedence changes; Word DOCX export changes; storage or LDML format changes.

## Decisions

1. **Discovery architecture: port Paratext's parsing logic onto our table acquisition** (rejected: extending the tag-only parser in place; adopting Paratext's file-based reader wholesale). A new reader in `Src/Common/FwUtils` (beside `FontFeatureSettings.cs`) takes a table-source delegate `Func<uint tag, byte[]>` and returns typed records `{ tag, fontLabel, options[], isFromFont }`. Production feeds it `GetFontData` on the button's HDC — the only acquisition guaranteed to describe the face GDI actually renders (substitution, styles, CFF fonts, TTC members). Tests feed it bytes sliced from a font file. Paratext's file-based reader was rejected because FieldWorks has no font-file locator, file-vs-GDI face selection can disagree, it reads GSUB only (loses `kern` and other GPOS features), and it rejects CFF (`OTTO`) fonts.
2. **Read `featUiLabelNameId` (featureParams offset +2) for cv feature labels.** Paratext skips this field and substitutes catalog descriptions; the Charis scan proved it carries the good names ("Capital Eng"). ssXX uses `UINameID` (offset +2 in the stylistic-set params). Name-record selection ports Paratext's ranked scheme (Windows-English first, then any Unicode/Windows record, then Mac Roman).
3. **Catalog split: flags in FwUtils, names in resx** (rejected: everything in the UI assembly; everything in FwUtils). A static catalog table (tag → `Hidden`/`DefaultOn`/`DefaultOff`) lives in FwUtils where the reader/provider layers can use it; human names extend the existing `kstidOpenTypeFeature_<tag>` entries in `FwCoreDlgControls.resx` so Crowdin localization keeps working and the 21 existing (possibly translated) entries are preserved. Seeded from Paratext's ~120-entry `RegisteredFeatureCatalog` but audited against the OpenType registry: `dlig` stays visible (Paratext wrongly hides it; hiding it would regress our current UI), `aalt` becomes hidden (glyph-palette feature, meaningless as a toggle), and the dead `ccmp` resx label is removed. Every tag in the old 33-tag blocklist must be `Hidden` in the catalog.
4. **Hidden-filtering moves from reader to provider** (rejected: keep filtering at read time). The reader reports every feature the font declares; the UI layer decides visibility. This keeps the model truthful and lets a future "show advanced features" UI reuse the same reader. The reader keeps the existing LOGFONT-keyed 32-entry cache, now storing the richer records.
5. **Value model.** cvXX with N named options → values `{0..N}`, 0 labeled "None" (resx), value *i* = option *i* (OpenType cvXX semantics: value is a 1-based alternate index). cvXX without featureParams → binary On/Off fallback (enumerating alternates would require lookup parsing — tier-b territory; fonts with multiple alternates ship names in practice). ssXX and registered tags → binary. Default value comes from catalog `DefaultOn`, flowing through the provider's existing `defaultValue` out-param, which the menu already honors — c1 costs zero menu changes; unchecking a default-on `liga` writes `liga=0`, which genuinely disables it in Uniscribe/CSS/Word semantics.
6. **Labels are name-only, FLEx-Graphite style** (rejected: Paratext's "Name (tag)" suffix — reversed by Jason during review). Tags surface only in fallbacks: existing "Feature '<tag>'" format for unknown/vendor tags (which stay visible — hiding them would regress capability), plus new "Stylistic Set N" / "Character Variant N" resx fallbacks for ss/cv features without font strings. The provider composes final labels itself and never returns empty.
7. **UI form: keep the existing dropdown menu** (rejected: Paratext-style dialog with checkbox+combo rows — deferred to a follow-up ticket as the "tier b" presentation upgrade). The menu machinery is provider-agnostic and already renders everything required; a dialog adds Designer/test surface without serving LT-22638's acceptance criteria.
8. **Tests use the real downloaded Charis 6.200 plus synthetic fixtures** (rejected: checking in a purpose-built fixture font). Exact-value assertions (cv43 = "Capital Eng" + 3 options in order; cv25 = 2; ss01 name; `liga` present with no params; `mark`/`mkmk` from GPOS) guard against plausible-but-wrong parsing — the failure mode binary parsers invite. Synthetic truncated/corrupt table fixtures assert silent degradation, never exceptions, since this code runs against arbitrary user-installed fonts.

## Risks / Trade-offs

- [Malformed or hostile font tables crash discovery] → Every read is bounds-checked; parse failures degrade to tag-only records; synthetic-fixture tests cover truncation at each structural boundary. Existing behavior (tag-only) is the floor.
- [Catalog default flags disagree with a shaper's actual per-script defaults] → Flags affect only the initial check state, never what is written for untouched features; worst case matches today's behavior for that feature. No auto-stripping means no data is rewritten based on flag data.
- [Fonts with unusual name-table encodings show garbled names] → Ranked name selection restricted to decodable platform/encoding pairs (Windows/Unicode, Mac Roman); undecodable records are skipped and fallback labels apply.
- [Binary fallback for unnamed multi-alternate cvXX hides alternates beyond the first] → Accepted; rare in practice (SIL fonts ship names), upgradeable later via lookup parsing without storage changes.
- [FwUtilsTests gains a dependency on the font download step] → Precedent exists (native tests, RenderComparison); restore already provisions the font; synthetic fixtures keep parser logic testable even without it.
- [liblcm or LDML round-trip could normalize/clamp multi-values like `cv43=2`] → Open question below; verified during implementation before the UI writes such values.

## Migration Plan

No data migration. Existing stored strings (`smcp=1`, legacy numeric Graphite strings) parse unchanged; new multi-values are additive and already accepted by `FontFeatureSettings.Parse`, rendering, and CSS export. Rollback = revert the PR; stored `cvXX=N` values would simply display via the old binary UI without data loss.

## Open Questions

- Verify liblcm round-trips `DefaultFontFeatures` / LDML `<font features="...">` without clamping values > 1 (expected: opaque string; confirm before enabling multi-value writes).
- Confirm with a second SIL font (e.g. Scheherazade New, already downloaded) that the reader behaves on an Arabic-script feature set; not a gate, but cheap extra coverage.
