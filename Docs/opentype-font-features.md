# OpenType Font Features

FieldWorks stores font options as renderer-neutral feature strings such as `smcp=1`, `kern=0`, and `cv01=2`. The same value is used by writing-system default fonts, style font settings, rendering, and export paths.

In the current WinForms UI, use the Font Options button in font controls to choose the configurable features exposed by the selected font. Graphite remains available for now, but the Font Options UI is no longer limited to Graphite fonts.

## Feature names and values

Feature labels are resolved in order: the name the font supplies for a stylistic set (`ssXX`) or character variant (`cvXX`) via its GSUB `featureParams` and `name` table; a localized name from `FwCoreDlgControls.resx`; the English name from the registered-feature catalog; and finally a `Stylistic Set N` / `Character Variant N` / `Feature #<tag>` fallback. Labels show the name only, matching the Graphite presentation; four-character tags appear only in the last-resort fallback. `OpenTypeFontFeatureInfoReader` in `FwUtils` reads this information from GSUB and GPOS through a table-source delegate (GDI `GetFontData` in the app, font-file bytes in tests) and degrades to a tag-only record if a table is malformed.

A character variant that names its options is presented as a multi-valued submenu — `None` plus each named option — and stored as `cv43=2`, where the value is the 1-based option index. Character variants and stylistic sets without font-supplied strings fall back to an on/off toggle.

`OpenTypeFeatureCatalog` in `FwUtils` classifies registered features. Features it marks hidden (shaping-required features such as `mark`, `ccmp`, `init`; and glyph-palette features such as `aalt`) are discovered but not shown as toggles. Features it marks default-on (`liga`, `clig`, `calt`, `kern`) display as enabled when the stored feature string does not mention them; the string gains an explicit `tag=0` only when the user turns such a feature off. Unset features are never written.

`IsDefaultOn` is the one place the OpenType provider is not equivalent to the Graphite one, and the difference matters before changing the set. `GraphiteFontFeatureProvider` gets `defaultValue` from the font: `IRenderingFeatures.GetFeatureValues` reads the font's own feature table, where the designer declared a default per feature, and that same table drives the Graphite shaper. The checkbox and the renderer cannot disagree. OpenType has nowhere to record this. A GSUB/GPOS `FeatureRecord` has no default-value field, and only `cvNN`/`ssNN` carry `featureParams`, which hold labels rather than defaults. `OpenTypeFontFeatureProvider` therefore fills the same out-parameter from `OpenTypeFeatureCatalog.IsDefaultOn`, a static table. That makes it an assertion about what Uniscribe does, not a fact read from the font, and the two providers share a signature that hides the difference.

Marking a tag default-on only sets the initial checkbox state for a feature the writing system does not mention; nothing reaches the renderer either way. Being wrong is not symmetric. A tag wrongly marked on displays as enabled while the text is unaffected, and the user's first click writes `tag=0`, so the gesture meant to enable it explicitly disables it. A tag wrongly marked off displays as disabled while the text is affected, and the first click writes `tag=1`, a no-op that matches reality, with `tag=0` one click further. Under uncertainty, off is the recoverable error.

That is why the set is narrower than the OpenType registry suggests. The registry recommends `cpsp` and `rand` be on by default, and `chws` and `halt` for horizontal CJK layout in applications that do not implement CLREQ/JLREQ/KLREQ. Those are instructions to an application to *apply* a feature, which here would mean writing `tag=1`; marking such a tag default-on does the opposite, leaving the string silent so the feature is never requested. `chws` and `halt` could not both be default-on regardless, since each is mutually exclusive with the other and with every other horizontal glyph-width feature, and the catalog carries no per-script state. LT-22774 measures what Uniscribe actually applies, which is the only thing that can close this loop the way the Graphite font table closes it.

Feature discovery is script-blind, which is a known limitation rather than an oversight. `OpenTypeFontFeatureInfoReader` walks the feature list flat and deduplicates by tag, so a font that gave the same tag different labels or options under different scripts would show one script's strings to another. No font does: of 178 installed fonts, 173 `cv`/`ss` tags appear more than once and every duplicate carries identical metadata, and the SIL fonts covering Latin, Cyrillic and Greek together register each feature once under every script. Making it script-aware requires passing the writing system's script into the reader; do that when a font appears that needs it.

Graphite feature IDs are still converted only at the Graphite renderer boundary. OpenType feature tags stay as four-character tags and are passed to the Uniscribe OpenType path when Graphite is not enabled.

For export, CSS output maps these values to `font-feature-settings`, and Notebook export preserves writing-system default font features in `DefaultFontFeatures`.

Word DOCX export preserves the subset of OpenType features that Microsoft WordprocessingML can represent with Office 2010 `w14` typography elements:

- `liga`, `clig`, `hlig`, and `dlig` map to Word ligature settings.
- `lnum` and `onum` map to lining and old-style number forms.
- `pnum` and `tnum` map to proportional and tabular number spacing.
- `calt` maps to contextual alternatives.
- `ss01` through `ss20` map to Word stylistic sets.

Other tags, including character variants such as `cv01`, small-cap features such as `smcp`, kerning, swashes, and private or vendor tags, do not have a documented arbitrary DOCX feature-tag representation. Word export ignores those unsupported tags while preserving supported tags from the same feature string.
