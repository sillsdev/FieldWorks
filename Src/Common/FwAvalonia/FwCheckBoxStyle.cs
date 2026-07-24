// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// The ONE DETERMINISTIC, GLOBAL CheckBox style: every Avalonia surface (dialogs, browse table,
	/// chooser flat list + tree, configure-columns, options, find/replace, insert-entry, region) renders
	/// checkboxes at a FIXED size derived from <see cref="FwAvaloniaDensity.CheckboxBoxSize"/> (a function of
	/// the 12px surface font), so a checkbox NEVER inflates a table/list/tree row past the text-row height.
	///
	/// WHY A WHOLE TEMPLATE (not a selector tweak or a RenderTransform): the Fluent 11.3 CheckBox template
	/// hardcodes the box as a 20×20 <c>Border</c> (<c>NormalRectangle</c>) inside an unnamed inner <c>Grid</c>
	/// pinned to <c>Height="32"</c> — both as LOCAL VALUES in the template, which OUTRANK any style setter
	/// (Avalonia precedence: LocalValue &gt; Style), so a <c>CheckBox /template/ Border#NormalRectangle</c>
	/// selector cannot shrink them. A <c>ScaleTransform</c> shrinks only the PAINT and leaves the 32px layout
	/// slot — the row-inflation the requirement rejects. The robust deterministic fix is to REPLACE the
	/// template with a compact one whose box and layout footprint ARE <see cref="FwAvaloniaDensity.CheckboxBoxSize"/>.
	/// This is a <see cref="ControlTheme"/> (applied via a <c>Theme</c> setter) carrying that template plus the
	/// checked/indeterminate/disabled state styles — a self-contained, content-independent definition, so the
	/// rendered size is identical on every surface.
	///
	/// AUTHORITATIVE SOURCE: this C# builder is the single definition. <see cref="FwSurfaceStyles"/> (browse /
	/// region / bulk-bar path) adds it; the dialog path adds it via <c>DialogThemeBootstrap.Apply</c> (called by
	/// every dialog ctor, in BOTH the runtime host and the headless dialog tests). One helper, both paths.
	/// </summary>
	public static class FwCheckBoxStyle
	{
		// The legacy Fluent checkmark geometry (Controls/CheckBox.xaml), drawn in a Viewbox so it scales to
		// whatever box size we choose — keeping the deterministic box font-proportional without redrawing.
		private const string CheckGeometry =
			"M5.5 10.586 1.707 6.793A1 1 0 0 0 .293 8.207l4.5 4.5a 1 1 0 0 0 1.414 0l11-11A1 1 0 0 0 15.793.293L5.5 10.586Z";
		private const string IndeterminateGeometry = "M1536 1536v-1024h-1024v1024h1024z";

		// Concrete brushes (NOT Fluent DynamicResources, which do not resolve in the headless test app — the
		// established "hard rule 1"): a WinForms-ish checkbox. White box, mid-gray border, blue accent when
		// checked, gray when disabled.
		private static readonly IBrush BoxFill = Brushes.White;
		private static readonly IBrush BoxStroke = new SolidColorBrush(Color.FromRgb(0x7A, 0x7A, 0x7A));
		private static readonly IBrush CheckedFill = new SolidColorBrush(Color.FromRgb(0x00, 0x5F, 0xB8));
		private static readonly IBrush CheckedStroke = new SolidColorBrush(Color.FromRgb(0x00, 0x5F, 0xB8));
		private static readonly IBrush GlyphForeground = Brushes.White;
		private static readonly IBrush DisabledFill = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
		private static readonly IBrush DisabledStroke = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));

		/// <summary>
		/// The deterministic CheckBox styles, ready to add to a surface's <see cref="StyledElement.Styles"/>.
		/// One style that points every CheckBox at the compact <see cref="ControlTheme"/>.
		/// </summary>
		public static IEnumerable<IStyle> Build()
		{
			yield return new Style(s => s.OfType<CheckBox>())
			{
				Setters =
				{
					new Setter(StyledElement.ThemeProperty, BuildTheme()),
					new Setter(Layoutable.MinHeightProperty, 0d),
					new Setter(Layoutable.MinWidthProperty, 0d),
					new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center)
				}
			};
		}

		// A compact, self-contained CheckBox ControlTheme: the box and its layout slot are CheckboxBoxSize, so
		// the control's footprint is the font-proportional box (never the Fluent 32px slot). The glyph rides a
		// Viewbox so it auto-fits the box. Nested pseudo-class styles drive the checked/indeterminate/disabled
		// visuals (the part of the Fluent theme we still need, reproduced concretely so it renders headlessly).
		private static ControlTheme BuildTheme()
		{
			var box = FwAvaloniaDensity.CheckboxBoxSize;

			var theme = new ControlTheme(typeof(CheckBox))
			{
				Setters =
				{
					new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
					// No box→label gap here: the gap is the StackPanel Spacing in BuildTemplate (deterministic,
					// CheckboxLabelGap). Padding stays 0 so a content-less select checkbox adds no width either.
					new Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
					new Setter(Layoutable.MinHeightProperty, 0d),
					new Setter(Layoutable.MinWidthProperty, 0d),
					new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center),
					new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<CheckBox>((_, __) => BuildTemplate(box)))
				}
			};

			// Base (unchecked) visuals — set via STYLES, not local template values, so the state styles below
			// can override them (a local value would outrank a style setter). The box reads white with a gray
			// border; both glyphs start hidden. These must precede the state styles so a later matching state
			// style wins by ordering.
			theme.Add(new Style(s => s.Nesting().Template().OfType<Border>().Name("FwCheckBox_Box"))
			{
				Setters =
				{
					new Setter(Border.BackgroundProperty, BoxFill),
					new Setter(Border.BorderBrushProperty, BoxStroke)
				}
			});
			theme.Add(new Style(s => s.Nesting().Template().OfType<Path>().Name("FwCheckBox_CheckGlyph"))
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) }
			});
			theme.Add(new Style(s => s.Nesting().Template().OfType<Path>().Name("FwCheckBox_IndeterminateGlyph"))
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) }
			});

			// :checked — accent-fill the box and reveal the checkmark glyph.
			theme.Add(BoxFillStyle(":checked", CheckedFill, CheckedStroke));
			theme.Add(GlyphOpacityStyle(":checked", "FwCheckBox_CheckGlyph"));

			// :indeterminate — accent-fill the box and reveal the square indeterminate glyph.
			theme.Add(BoxFillStyle(":indeterminate", CheckedFill, CheckedStroke));
			theme.Add(GlyphOpacityStyle(":indeterminate", "FwCheckBox_IndeterminateGlyph"));

			// :disabled — gray the box so a disabled checkbox reads inert.
			theme.Add(BoxFillStyle(":disabled", DisabledFill, DisabledStroke));

			return theme;
		}

		private static Style BoxFillStyle(string pseudo, IBrush fill, IBrush stroke)
			=> new Style(s => s.Nesting().Class(pseudo).Template().OfType<Border>().Name("FwCheckBox_Box"))
			{
				Setters =
				{
					new Setter(Border.BackgroundProperty, fill),
					new Setter(Border.BorderBrushProperty, stroke)
				}
			};

		private static Style GlyphOpacityStyle(string pseudo, string glyphName)
			=> new Style(s => s.Nesting().Class(pseudo).Template().OfType<Path>().Name(glyphName))
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) }
			};

		// The compact template: a box Border + the check/indeterminate glyphs in a Viewbox, then the content
		// presenter for any label. Box and the surrounding StackPanel are sized to `box`, so the layout
		// footprint is the font-proportional box, not the Fluent 32px slot.
		private static Control BuildTemplate(double box)
		{
			// NOTE: do NOT set Opacity locally here — a local value outranks a Style setter (Avalonia
			// precedence LocalValue > Style), so the :checked state style could not reveal it. The glyphs are
			// hidden by the theme's base nested style and revealed by the state styles (see BuildTheme).
			var checkGlyph = new Path
			{
				Name = "FwCheckBox_CheckGlyph",
				Data = Geometry.Parse(CheckGeometry),
				Fill = GlyphForeground,
				Stretch = Stretch.Uniform
			};
			var indeterminateGlyph = new Path
			{
				Name = "FwCheckBox_IndeterminateGlyph",
				Data = Geometry.Parse(IndeterminateGeometry),
				Fill = GlyphForeground,
				Stretch = Stretch.Uniform
			};
			var glyphBox = new Viewbox
			{
				Width = box,
				Height = box,
				Child = new Panel { Children = { checkGlyph, indeterminateGlyph } }
			};

			// NOTE: Background/BorderBrush are NOT set locally — a local value would outrank the :checked /
			// :disabled state Style setters (LocalValue > Style). The unchecked fill/stroke come from the
			// theme's base nested style; the states recolor through styles (see BuildTheme).
			var boxBorder = new Border
			{
				Name = "FwCheckBox_Box",
				Width = box,
				Height = box,
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(2),
				VerticalAlignment = VerticalAlignment.Center,
				Child = glyphBox
			};

			var content = new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				VerticalAlignment = VerticalAlignment.Center
			};
			content.Bind(ContentPresenter.ContentProperty,
				new Avalonia.Data.Binding("Content") { RelativeSource = TemplatedParentSource });
			content.Bind(ContentPresenter.ContentTemplateProperty,
				new Avalonia.Data.Binding("ContentTemplate") { RelativeSource = TemplatedParentSource });
			content.Bind(Layoutable.MarginProperty,
				new Avalonia.Data.Binding("Padding") { RelativeSource = TemplatedParentSource });

			var layout = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				VerticalAlignment = VerticalAlignment.Center,
				// Deterministic box→label gap so the words never butt against the box (e.g. FilterFor's
				// "Match case"). A content-less select checkbox (browse/list/tree) gets only this small trailing
				// gap, which sits inside the fixed checkbox column, so it still adds no row height.
				Spacing = FwAvaloniaDensity.CheckboxLabelGap,
				Children = { boxBorder, content }
			};

			return new Border
			{
				Name = "PART_Border",
				Background = Brushes.Transparent,
				Child = layout
			};
		}

		private static readonly Avalonia.Data.RelativeSource TemplatedParentSource =
			new Avalonia.Data.RelativeSource(Avalonia.Data.RelativeSourceMode.TemplatedParent);
	}
}
