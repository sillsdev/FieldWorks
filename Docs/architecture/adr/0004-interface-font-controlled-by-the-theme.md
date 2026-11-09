# The Avalonia interface font is controlled by the theme

LT-22854. The `FwUiFontFamily` token names Segoe UI as the default interface
font on Windows. Segoe UI preserves the density of the current interface; the
proposed Andika default has taller line metrics. Line-height tokens remain a
separate decision.

`FwUiFont.ApplyTo` assigns the interface font stack to Semi's
`DefaultFontFamily`, `ContentControlThemeFontFamily`, and
`SemiFontFamilyRegular` resources. The stack combines the theme default,
fallbacks for the UI language, and the platform default. It is chosen at
startup and applies across the application: menus and other interface chrome
should not change when the project changes.

Why Segoe UI: its line is about 1.33 em against Andika's 1.61 em, so rows and
dialogs keep today's height. The fallbacks are named, one list per UI language
the installer ships, because Segoe UI lacks those scripts and a named Windows
font and SIL or Noto font draws them predictably; the platform default is last.
Andika was bundled at first and removed: nothing used it once Segoe UI became
the default, and it cost 4 MB in `FwAvaloniaTheme.dll`. Revisit if a theme
needs a bundled font, or when the line-height tokens land.

Text that follows the `UiElement` style in the WinForms detail view keeps
following it. The detail view's section headings (the `UiElement` runs in the
`*-Jt-Summary` parts) take the font the project's `UiElement` style gives the UI
writing system; the composer reads it and the model carries it, so each project
window has its own and a second window never changes the first. Menus, dialog
controls and field labels use the theme font: nothing in them reads `UiElement`
today (slice labels are fixed 10 pt sans serif). The browse filter bar's
`UiElement` text has no Avalonia counterpart yet and follows when one exists.
Data text continues to use its writing-system font.

Windows validation must measure density, clipping and script glyph fallback.
The dialog reference images show Segoe UI.
