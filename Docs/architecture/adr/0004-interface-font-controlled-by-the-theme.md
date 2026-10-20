# The Avalonia interface font is controlled by the theme; data text keeps its writing-system font

LT-22854. FieldWorks controls its interface font instead of leaving it to the platform
default. One theme token, `FwUiFontFamily` in `FwColorTokens.axaml`, names the font that every
Avalonia surface (dialogs, detail view chrome, menus, flyouts) draws its interface text in. The
font is bundled in `FwAvaloniaTheme.dll` and registered at startup as the private font
collection `fonts:FieldWorks`, with per-language fallbacks, so every machine and every test run
looks the same, and changing the font later is a one-token change plus the font files. The
proposed default is Andika 7.000 (`fonts:FieldWorks#Andika`; see "Alternatives for the
default"). `FwUiFont.ApplyTo`, called from both
`Application.Initialize` overrides, registers the collection and points Semi's
`DefaultFontFamily`, `ContentControlThemeFontFamily` and `SemiFontFamilyRegular` resources at
the token, so text inherits it from the root. Data text keeps the font of its writing system.
The family is a stack: the default font, then the fallback fonts of the UI language the application
started in (see "Fallback fonts" below).

**Why a bundled collection rather than an installed font**: the installer ships Andika 6.101
as a system font and a developer machine may carry any build or none. A bundled 7.000 gives
the same glyphs and kerning in the installed product, the Preview Host and headless tests,
which would otherwise vary by machine. A writing system that names "Andika" still resolves to
the system copy through the plain family name, because a private collection key never captures
it, so data rendering is unchanged.

**Why Semi's resource keys**: Semi sets the family on its own root and popup themes from those
three resources, and the WinForms-hosted root uses the same theme. Setting only
`FontManagerOptions.DefaultFamilyName` would leave Semi's stack (landing on Segoe UI) in
charge, and the headless test builders bypass that option anyway.

**Five faces of the default**: Regular, Italic, Bold, BoldItalic and SemiBold, the weights and styles the
interface uses, about 4.1 MB. Avalonia matches the nearest weight for anything else.

**Sizes stay at 11 px for surfaces and 13 px for labels.** Andika's x-height (0.508 em) matches
Segoe UI's (0.500 em), so legibility holds. Its line height is 1.61 em against 1.33 em, so
a line grows about 3 px at 11 px (14.6 to 17.7) and rows and dialogs get taller. This is
visible in the refreshed baseline screenshots. Line-height tokens to recover WinForms row
height are a separate decision.

**Fallback fonts**: Andika lacks the scripts of five of the sixteen UI languages shipped in
`Localizations/l10ns` (az es fa fr hi hu id ko ms pt ru rw te tr vi zh-CN). Persian (fa), Hindi
(hi), Korean (ko), Telugu (te) and Simplified Chinese (zh-CN) therefore get a named fallback
after Andika in the family stack, chosen from the UI culture by `FwUiFont.FallbackFamiliesFor`.
Each list names a Windows system font first (Segoe UI, Nirmala UI, Malgun Gothic, Microsoft
YaHei UI), so the fallback is present on every Windows machine, and then the SIL or Noto family
that an SIL per-language font table names (Scheherazade New, Annapurna SIL, Noto Sans CJK KR, Noto
Sans Telugu, Noto Sans CJK SC), which is used where installed and skipped otherwise. Avalonia
tries the families in the stack per character, so Latin text stays in Andika. The table holds
only these five languages and each has a test; a language added to Crowdin later needs a new
entry and test. No fallback fonts are bundled. The other eleven languages are drawn in Andika
alone, which a test confirms with a sample string each.

**What this deliberately does not do**:

- It does not set `FontManagerOptions.DefaultFamilyName`, which would change the fallback for
  data text whose writing-system font is missing.
- It does not bump the installer's Andika 6.101 to 7.000 (tracked under LT-19288).
- It does not bundle any fallback font; the fallbacks are Windows system fonts and installed
  SIL or Noto families.
- It does not follow a UI language change after start-up: the stack is chosen once, as the
  UI language itself is only read at start-up.
- It does not add FontFamily setters to dialog or surface styles; inheritance carries the font.

**Revisit when**: the installer ships Andika 7.000 as a system font (the bundled copy could
then be dropped), line-height tokens are introduced, or testers report unreadable text on a
machine that has none of the named fallback families.

**Alternatives for the default**: Andika is the proposed default, not a requirement of this
decision. Swapping it means replacing the font files in `Src/Common/FwAvaloniaTheme/Assets/Fonts`
(with their licence and notice) and changing the value of `FwUiFontFamily`; no code changes. Its
line height (1.61 em) is taller than Segoe UI's (1.33 em), so a font with tighter vertical
metrics would keep rows closer to the WinForms height.

- The platform UI font (Segoe UI on Windows) is the closest to today's look (x-height 0.500,
  line height 1.33). Its licence does not allow bundling, so other platforms and test machines
  would differ. The token can still name it if the team prefers today's look.
- Sophia Nubian 1.0 (SIL, OFL, 2009; https://software.sil.org/sophianubian/) is a Univers-like
  sans whose vertical metrics are closest to Segoe UI (x-height 0.500, line height 1.385). It has
  no Cyrillic, so it cannot draw the Russian UI. It also has no SemiBold and little Greek and
  IPA, and its last release was in 2009.
- Padauk 6.000 (SIL, OFL, 2025; https://software.sil.org/padauk/) is a Myanmar family with a
  Latin sans. It has a SemiBold weight. Its x-height is 0.435, so it would need a size about 15%
  larger, and it has no Cyrillic, IPA or Italic.

Andika 7.000 is the only SIL Latin sans with full Latin, Cyrillic, Greek and IPA and all the
weights FieldWorks uses. Its cost is the taller line (1.61 em against 1.33 em). Metrics are per
em.
