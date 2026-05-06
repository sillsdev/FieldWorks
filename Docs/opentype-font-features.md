# OpenType Font Features

FieldWorks stores font options as renderer-neutral feature strings such as `smcp=1`, `kern=0`, and `cv01=2`. The same value is used by writing-system default fonts, style font settings, rendering, and export paths.

In the current WinForms UI, use the Font Options button in font controls to choose the configurable features exposed by the selected font. Graphite remains available for now, but the Font Options UI is no longer limited to Graphite fonts.

Graphite feature IDs are still converted only at the Graphite renderer boundary. OpenType feature tags stay as four-character tags and are passed to the Uniscribe OpenType path when Graphite is not enabled.

For export, CSS output maps these values to `font-feature-settings`, and Notebook export preserves writing-system default font features in `DefaultFontFeatures`. Word export does not currently have a verified OpenType feature mapping and should be treated as follow-up work if Word parity is required.