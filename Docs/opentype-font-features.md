# OpenType Font Features

FieldWorks stores font options as renderer-neutral feature strings such as `smcp=1`, `kern=0`, and `cv01=2`. The same value is used by writing-system default fonts, style font settings, rendering, and export paths.

In the current WinForms UI, use the Font Options button in font controls to choose the configurable features exposed by the selected font. Graphite remains available for now, but the Font Options UI is no longer limited to Graphite fonts.

Graphite feature IDs are still converted only at the Graphite renderer boundary. OpenType feature tags stay as four-character tags and are passed to the Uniscribe OpenType path when Graphite is not enabled.

For export, CSS output maps these values to `font-feature-settings`, and Notebook export preserves writing-system default font features in `DefaultFontFeatures`.

Word DOCX export preserves the subset of OpenType features that Microsoft WordprocessingML can represent with Office 2010 `w14` typography elements:

- `liga`, `clig`, `hlig`, and `dlig` map to Word ligature settings.
- `lnum` and `onum` map to lining and old-style number forms.
- `pnum` and `tnum` map to proportional and tabular number spacing.
- `calt` maps to contextual alternatives.
- `ss01` through `ss20` map to Word stylistic sets.

Other tags, including character variants such as `cv01`, small-cap features such as `smcp`, kerning, swashes, and private or vendor tags, do not have a documented arbitrary DOCX feature-tag representation. Word export ignores those unsupported tags while preserving supported tags from the same feature string.
