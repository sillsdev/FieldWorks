# OpenType Font Features

FieldWorks stores font options as renderer-neutral feature strings such as `smcp=1`, `kern=0`, and `cv01=2`. The same value is used by writing-system default fonts, style font settings, rendering, and export paths.

In the current WinForms UI, use the Font Options button in font controls to choose the configurable features exposed by the selected font. Graphite remains available for now, but the Font Options UI is no longer limited to Graphite fonts.

## Feature names and values

Feature labels are resolved in order: the name the font supplies for a stylistic set (`ssXX`) or character variant (`cvXX`) via its GSUB `featureParams` and `name` table; a localized name from `FwCoreDlgControls.resx`; the English name from the registered-feature catalog; and finally a `Stylistic Set N` / `Character Variant N` / `Feature #<tag>` fallback. Labels show the name only, matching the Graphite presentation; four-character tags appear only in the last-resort fallback. `OpenTypeFontFeatureInfoReader` in `FwUtils` reads this information from GSUB and GPOS through a table-source delegate (GDI `GetFontData` in the app, font-file bytes in tests) and degrades to a tag-only record if a table is malformed.

A character variant that names its options is presented as a multi-valued submenu — `None` plus each named option — and stored as `cv43=2`, where the value is the 1-based option index. Character variants and stylistic sets without font-supplied strings fall back to an on/off toggle.

`OpenTypeFeatureCatalog` in `FwUtils` classifies registered features. Features it marks hidden (shaping-required features such as `mark`, `ccmp`, `init`; and glyph-palette features such as `aalt`) are discovered but not shown as toggles. Features it marks default-on (`liga`, `clig`, `calt`, `kern`) display as enabled when the stored feature string does not mention them; the string gains an explicit `tag=0` only when the user turns such a feature off. Unset features are never written.

Graphite feature IDs are still converted only at the Graphite renderer boundary. OpenType feature tags stay as four-character tags and are passed to the Uniscribe OpenType path when Graphite is not enabled.

For export, CSS output maps these values to `font-feature-settings`, and Notebook export preserves writing-system default font features in `DefaultFontFeatures`.

Word DOCX export preserves the subset of OpenType features that Microsoft WordprocessingML can represent with Office 2010 `w14` typography elements:

- `liga`, `clig`, `hlig`, and `dlig` map to Word ligature settings.
- `lnum` and `onum` map to lining and old-style number forms.
- `pnum` and `tnum` map to proportional and tabular number spacing.
- `calt` maps to contextual alternatives.
- `ss01` through `ss20` map to Word stylistic sets.

Other tags, including character variants such as `cv01`, small-cap features such as `smcp`, kerning, swashes, and private or vendor tags, do not have a documented arbitrary DOCX feature-tag representation. Word export ignores those unsupported tags while preserving supported tags from the same feature string.
