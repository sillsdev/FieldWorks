# FieldWorks Avalonia style system (density + borders, per surface)

The single source of truth for **font/density tokens** and the **field-border rule** across every
FieldWorks Avalonia surface. The goal is WinForms density (tight, Segoe-UI-9pt-ish) — NOT the roomy
Fluent defaults — with each surface matching its *own* WinForms predecessor rather than one uniform look.

## Why a global token system (and why it renders headlessly)

`FwAvaloniaApp` adds `new FluentTheme()`, which ships ~14px fonts and ~32px control floors app-wide.
Density/borders must be layered ON TOP of Fluent. The reliable mechanism — proven by a prior agent —
is **per-control-tree `.Styles`**, added to each surface's own `Styles` collection by its view ctor.
App-level `Application.Styles` did **not** apply in the headless test app, so that path is rejected.

Two hard rules that fall out of headless rendering:

1. **Concrete values, not Fluent `DynamicResource`s, for anything that must render headlessly.** The old
   `BorderBrush="{DynamicResource ControlStrokeColorDefault}"` resolved to nothing in the headless app, so
   the field-host borders were invisible there (and too faint at runtime). Use a concrete color.
2. **One source per surface family.** Dialog density/borders live in `DialogTheme.axaml`; region/browse
   font lives in `FwSurfaceStyles`. Don't scatter font/padding literals across views.

## Per-surface intent (do NOT make them uniform)

| Surface | WinForms analogue | Look | Where it's set |
|---|---|---|---|
| **Dialog text inputs** (`TextBox`/`ComboBox`, and the `PART_*Host` boxes wrapping owned multistring/option editors) | classic WinForms dialog inputs | **clearly boxed** — visible 1px gray border + tight padding | `DialogTheme.axaml` (`fwFieldHost` style + `TextBox`/`ComboBox` setters) |
| **Detail / region view** (`LexicalEditRegionView`) | the DataTree | **flat** with subtle 1px field separators — do NOT box every value | flat structure via `FwAvaloniaDensity` literals; font via `FwSurfaceStyles` |
| **Browse table** (`LexicalBrowseView`) | XMLViews browse | **grid** — keep column/row lines + bold headers, just denser font | grid via `FwAvaloniaDensity`; font via `FwSurfaceStyles` |

The key distinction: **dialog inputs get a box; detail values do not.** The detail view's owned editors are
deliberately borderless/transparent (flat like the legacy RootSite). Only in a *dialog* does an owned editor
sit inside a `Border.fwFieldHost` that supplies the box.

## The tokens / values (the calibrated numbers)

**Font:** `12` px app-wide on the Avalonia surfaces (down from Fluent's ~14). One value: `DialogFontSize`
in `DialogTheme.axaml`, `FwSurfaceStyles.SurfaceFontSize`, and `CompactDialogStyles.DialogFontSize` are all 12
and must stay equal.

**Control height:** `TextBox`/`ComboBox`/`Button` `MinHeight = 22` (WinForms ~22px, not Fluent ~32px).
`CheckBox`/`TabItem`/`ListBoxItem` drop the min-height floor to 0 and size to content.

**Paddings:** `TextBox 4,2` · `ComboBox 6,1` · `Button 8,2` · `TabItem 8,3` · `ListBoxItem 4,1`.

**Checkboxes (the ONE global, deterministic rule):** checkboxes are **font-proportional** and **never add row
height**. `FwAvaloniaDensity.CheckboxBoxSize = 14` (a fixed function of the 12px surface font) is the glyph-box
size on *every* surface — dialogs, browse table, chooser flat list + tree, configure-columns, options,
find/replace, insert-entry, region. The size is **deterministic** (a concrete px size applied to the template,
identical regardless of content) — **not** a `RenderTransform`/`ScaleTransform` (a scale shrinks the paint but
leaves the tall layout slot, which still inflates the row — the rejected hack, now removed). The single builder
`FwCheckBoxStyle.Build()` restyles the Fluent 11.3 `CheckBox` template: it (a) sets `MinHeight=0`/`MinWidth=0`/
`Padding=4,0,0,0`/`VerticalAlignment=Center` on the `CheckBox`, (b) pins the box `Border#NormalRectangle` to
`14×14`, and (c) collapses the inner template `Grid` (the Fluent `Height=32` slot, reached via
`CheckBox /template/ Grid#RootGrid > Grid`) to `14` — so the layout footprint, not just the paint, is the box.
The `CheckGlyph` rides a `Viewbox` inside that grid and auto-scales to the new box. Net: a row with a checkbox
is no taller than a text row (`BrowseRowMinHeight = 18`). This is **global, both lanes**: `FwSurfaceStyles`
(region/browse) and `CompactDialogStyles` (dialog runtime) both call `FwCheckBoxStyle.Build()`, and
`DialogTheme.axaml` mirrors the SAME selectors as XAML for the headless dialog tests — the `14` in the XAML must
stay equal to `CheckboxBoxSize`. Part names verified against `Avalonia.Themes.Fluent 11.3.6`
(`Controls/CheckBox.xaml`: `RootGrid`, the inner `Grid Height=32`, `NormalRectangle`, `CheckGlyph`).

**Radio buttons (the checkbox's counterpart — same global, deterministic rule):** radios are
**font-proportional** and **never add row height**, exactly like checkboxes. `FwAvaloniaDensity.RadioBoxSize`
(= `CheckboxBoxSize` = 14) is the outer-circle size on *every* surface (dialogs, region, bulk-edit bar). The
single builder `FwRadioButtonStyle.Build()` REPLACES the Fluent 11.3 `RadioButton` template (whose ~20px ellipse
on a tall ~32px slot are LOCAL values a style selector cannot override — same precedence trap as the checkbox)
with a compact `ControlTheme`: an outer `Ellipse#FwRadio_Box` pinned to `14×14` + an inner filled
`Ellipse#FwRadio_Dot` (~45% of the box) revealed on `:checked`, the label after a `CheckboxLabelGap` (6px)
`StackPanel.Spacing`, `MinHeight=0`/`MinWidth=0`, `VerticalAlignment=Center`. Concrete brushes (white fill, gray
`#7A7A7A` stroke, blue `#005FB8` accent stroke + dot when checked, gray when disabled) — NOT Fluent
`DynamicResource`s (hard rule 1). **Global, both lanes**, wired in the SAME two places as the checkbox:
`FwSurfaceStyles.Build()` (region/browse/bulk-bar) and `DialogThemeBootstrap.Apply` (dialogs — runtime host AND
headless tests). It is NOT in `DialogTheme.axaml` (the template replace must be a C# `ControlTheme`) and NOT in
`CompactDialogStyles` (the bootstrap already covers both dialog paths). The headless no-inflation test mirrors
the checkbox's: `RadioButton_OnStyledSurface_IsFontProportional_AndDoesNotExceedTheTextRowHeight` in
`LexicalBrowseDensityTests.cs` asserts the ring is exactly `RadioBoxSize`, the control is ≤ `BrowseRowMinHeight`,
and the dot opacity goes 0 → 1 on `:checked`.

**Group separation:** adjacent logical control GROUPS (e.g. a radio group followed by a checkbox group, as in
`FilterForDialogView`) get a little visual distance so they read as distinct rather than butting together:
`FwAvaloniaDensity.GroupSeparation` (8px) + the dialog tokens `DialogGroupSeparation` (`Thickness 0,8,0,0`) and
`DialogGroupSeparatorBrush` (concrete `LightGray`) in `DialogTheme.axaml`. `FilterForDialogView` draws a thin
1px `Border` hairline with that brush + margin between its match-style radio group and its regex/Match-case
checkbox group. Use the thin line + whitespace for the clearest cases; whitespace alone where a line would be
too heavy.

**Dialog spacing tokens (`DialogTheme.axaml` `Styles.Resources`):**

| Token | Value | Was |
|---|---|---|
| `DialogWindowPadding` | `10` | 12 |
| `DialogControlGap` | `6` | 8 |
| `DialogLabelFieldGap` | `4` | 6 |
| `DialogMinControlHeight` | `22` | 24 |
| `DialogFieldPadding` | `3` | 4 |
| `DialogButtonStripGap` | `8` | 8 |
| `DialogGroupSeparation` | `0,8,0,0` | (new) |
| `DialogIconGap` | `0,0,12,0` | — |

The message box (`MessageBoxView.axaml`) uses the standard layout: the severity glyph on the LEFT,
vertically centered against the message text block, `DialogIconGap` (12px) between icon and text, the message
wrapping in the remaining `*` column. Mirrors the conventional Windows MessageBox arrangement.

**Field border rule:** `Border.fwFieldHost` → `BorderThickness 1`, `Padding 3`, and
`BorderBrush #FF7A7A7A` — a **concrete mid-gray** that reads as a real input border at the snapshot DPI.
`DialogFieldBorderBrush`/`DialogFieldBorderColor` carry the same `#FF7A7A7A` for views that bind it. NOT a
Fluent `DynamicResource` (see hard rule 1).

## Where each piece is applied (the bootstraps)

- **Dialogs** — `DialogThemeBootstrap.Apply(this)` in every dialog view ctor adds `DialogTheme.axaml`
  (tokens + density setters + `fwFieldHost`/`fwDialogRoot` styles) to the dialog body's `Styles`. This is
  the path the headless dialog tests use, so they render at the same density as runtime. At runtime
  `AvaloniaDialogHost.ShowModal` additionally calls `CompactDialogStyles.Apply` — a belt-and-suspenders C#
  duplicate of the same values (both idempotent; keep the two numerically identical).
- **Region / browse** — `FwSurfaceStyles.Apply(this)` in `LexicalEditRegionView` and `LexicalBrowseView`
  ctors adds the **font-only** baseline (TextBlock/TextBox → 12px). The flat-with-separators (region) and
  grid (browse) structure come from `FwAvaloniaDensity` literals, which are concrete and already render
  headlessly; `FwSurfaceStyles` exists only to drop the Fluent default font those literals don't touch.

## Changing the density

Change the number in **`DialogTheme.axaml`** (and the mirrored constant in `CompactDialogStyles` /
`FwSurfaceStyles` if it's the font or min-height), rebuild the dialog + visual test projects, re-capture, and
**Read the PNGs** (`Output/Snapshots/` — one flat folder) to confirm the surfaces still read dense + properly bordered.
Never tune density by editing a single view — that breaks the single-source rule.
