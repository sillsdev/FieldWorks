# The Avalonia interface font is controlled by the theme

LT-22854. The `FwUiFontFamily` token names Segoe UI as the default interface
font on Windows. Segoe UI preserves the density of the current interface; the
proposed Andika default has taller line metrics. Line-height tokens remain a
separate decision.

`FwUiFont.ApplyTo` registers the bundled Andika collection and assigns the
interface font family to Semi's `DefaultFontFamily`,
`ContentControlThemeFontFamily`, and `SemiFontFamilyRegular` resources. The
family stack ends with the platform default, so a system without Segoe UI can
use its own default. Data text continues to use its writing-system font.
Andika 7.000 remains bundled in a private collection; a plain `Andika` family
still names the system font for writing-system data.

The current implementation still adds five language-specific fallback lists.
Reading the language project's `UiElement` style and replacing that table are
not implemented yet. Project-specific font selection needs to be applied to the
corresponding Avalonia host without changing other open project windows. The
factory `UiElement` entries for en, es, fr, id and pt specify Microsoft Sans
Serif; their treatment must be reconciled with the chosen Segoe UI default.

Windows validation must measure density, clipping, script glyph fallback and
project-specific settings. Screenshot baselines still depict Andika and must be
refreshed on Windows after the implementation is complete.
