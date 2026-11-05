# The Avalonia interface font is controlled by the theme

LT-22854. The `FwUiFontFamily` token names Segoe UI as the default interface
font on Windows. Segoe UI preserves the density of the current interface; the
proposed Andika default has taller line metrics. Line-height tokens remain a
separate decision.

`FwUiFont.ApplyTo` registers the bundled Andika collection and assigns the
interface font stack to Semi's `DefaultFontFamily`,
`ContentControlThemeFontFamily`, and `SemiFontFamilyRegular` resources. The
stack combines the theme default, fallbacks for the UI language, and the
platform default. It is chosen at startup and applies across the application:
menus and other interface chrome should not change when the project changes.

The language project's `UiElement` style continues to control UI-writing-system
text drawn in Views; Avalonia interface chrome does not read that style. Data
text continues to use its writing-system font. Andika 7.000 remains bundled
in a private collection; a plain `Andika` family still names the system font
for writing-system data.

Windows validation must measure density, clipping and script glyph fallback.
Screenshot baselines still depict Andika and must be refreshed on Windows.
