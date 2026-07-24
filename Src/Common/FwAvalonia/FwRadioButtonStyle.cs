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
	/// The radio-button counterpart of <see cref="FwCheckBoxStyle"/>: the ONE DETERMINISTIC, GLOBAL
	/// RadioButton style, so every Avalonia surface (dialogs, region, bulk-edit bar) renders radios at a FIXED
	/// size derived from <see cref="FwAvaloniaDensity.RadioBoxSize"/> (the same 14px the checkbox uses, a
	/// function of the 12px surface font), so a radio NEVER inflates a row past the text line.
	///
	/// WHY A WHOLE TEMPLATE (not a selector tweak or a RenderTransform): same reason as the checkbox — the
	/// Fluent 11.3 RadioButton template hardcodes its ~20px ellipse (<c>OuterEllipse</c>/<c>CheckOuterEllipse</c>)
	/// on a tall (~32px) layout slot as LOCAL VALUES in the template, which OUTRANK any style setter (Avalonia
	/// precedence: LocalValue &gt; Style), so a selector cannot shrink them, and a ScaleTransform shrinks only the
	/// PAINT and leaves the tall layout slot (the row-inflation the requirement rejects). The robust deterministic
	/// fix is to REPLACE the template with a compact one whose outer ellipse and layout footprint ARE
	/// <see cref="FwAvaloniaDensity.RadioBoxSize"/>. This is a <see cref="ControlTheme"/> (applied via a
	/// <c>Theme</c> setter) carrying that template plus the checked/disabled state styles.
	///
	/// AUTHORITATIVE SOURCE: this C# builder is the single definition, mirroring <see cref="FwCheckBoxStyle"/>.
	/// <see cref="FwSurfaceStyles"/> (browse / region / bulk-bar path) adds it; the dialog path adds it via
	/// <c>DialogThemeBootstrap.Apply</c> (called by every dialog ctor, in BOTH the runtime host and the headless
	/// dialog tests). One helper, both paths.
	/// </summary>
	public static class FwRadioButtonStyle
	{
		// Concrete brushes (NOT Fluent DynamicResources, which do not resolve in the headless test app — the
		// established "hard rule 1"): a WinForms-ish radio. White circle, mid-gray border, blue accent dot when
		// checked, gray when disabled. The same palette as FwCheckBoxStyle so radios and checkboxes match.
		private static readonly IBrush BoxFill = Brushes.White;
		private static readonly IBrush BoxStroke = new SolidColorBrush(Color.FromRgb(0x7A, 0x7A, 0x7A));
		private static readonly IBrush CheckedStroke = new SolidColorBrush(Color.FromRgb(0x00, 0x5F, 0xB8));
		private static readonly IBrush DotFill = new SolidColorBrush(Color.FromRgb(0x00, 0x5F, 0xB8));
		private static readonly IBrush DisabledFill = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
		private static readonly IBrush DisabledStroke = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));

		/// <summary>
		/// The deterministic RadioButton styles, ready to add to a surface's <see cref="StyledElement.Styles"/>.
		/// One style that points every RadioButton at the compact <see cref="ControlTheme"/>.
		/// </summary>
		public static IEnumerable<IStyle> Build()
		{
			yield return new Style(s => s.OfType<RadioButton>())
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

		// A compact, self-contained RadioButton ControlTheme: the outer ellipse and its layout slot are
		// RadioBoxSize, so the control's footprint is the font-proportional circle (never the Fluent tall slot).
		// Nested pseudo-class styles drive the checked/disabled visuals (reproduced concretely so they render
		// headlessly).
		private static ControlTheme BuildTheme()
		{
			var box = FwAvaloniaDensity.RadioBoxSize;

			var theme = new ControlTheme(typeof(RadioButton))
			{
				Setters =
				{
					new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
					// No box→label gap here: the gap is the StackPanel Spacing in BuildTemplate (deterministic,
					// CheckboxLabelGap — the same gap the checkbox uses, so radios and checkboxes line up).
					new Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
					new Setter(Layoutable.MinHeightProperty, 0d),
					new Setter(Layoutable.MinWidthProperty, 0d),
					new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center),
					new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<RadioButton>((_, __) => BuildTemplate(box)))
				}
			};

			// Base (unchecked) visuals — set via STYLES, not local template values, so the state styles below
			// can override them (a local value would outrank a style setter). The circle reads white with a gray
			// border; the dot starts hidden. These must precede the state styles so a later matching state style
			// wins by ordering.
			theme.Add(new Style(s => s.Nesting().Template().OfType<Ellipse>().Name("FwRadio_Box"))
			{
				Setters =
				{
					new Setter(Ellipse.FillProperty, BoxFill),
					new Setter(Ellipse.StrokeProperty, BoxStroke)
				}
			});
			theme.Add(new Style(s => s.Nesting().Template().OfType<Ellipse>().Name("FwRadio_Dot"))
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) }
			});

			// :checked — accent the ring and reveal the filled dot.
			theme.Add(new Style(s => s.Nesting().Class(":checked").Template().OfType<Ellipse>().Name("FwRadio_Box"))
			{
				Setters =
				{
					new Setter(Ellipse.FillProperty, BoxFill),
					new Setter(Ellipse.StrokeProperty, CheckedStroke)
				}
			});
			theme.Add(new Style(s => s.Nesting().Class(":checked").Template().OfType<Ellipse>().Name("FwRadio_Dot"))
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) }
			});

			// :disabled — gray the ring so a disabled radio reads inert.
			theme.Add(new Style(s => s.Nesting().Class(":disabled").Template().OfType<Ellipse>().Name("FwRadio_Box"))
			{
				Setters =
				{
					new Setter(Ellipse.FillProperty, DisabledFill),
					new Setter(Ellipse.StrokeProperty, DisabledStroke)
				}
			});

			return theme;
		}

		// The compact template: an outer ellipse (the ring) with an inner filled dot, then the content presenter
		// for any label. The ring and the surrounding StackPanel are sized to `box`, so the layout footprint is
		// the font-proportional circle, not the Fluent tall slot. The dot is ~40% of the box, centered.
		private static Control BuildTemplate(double box)
		{
			var dotSize = box * 0.45;

			// NOTE: Fill/Stroke are NOT set locally — a local value would outrank the :checked / :disabled state
			// Style setters (LocalValue > Style). The unchecked fill/stroke come from the theme's base nested
			// style; the states recolor through styles (see BuildTheme).
			var ring = new Ellipse
			{
				Name = "FwRadio_Box",
				Width = box,
				Height = box,
				StrokeThickness = 1,
				VerticalAlignment = VerticalAlignment.Center
			};

			// NOTE: do NOT set Opacity locally here — a local value outranks a Style setter, so the :checked
			// state style could not reveal it. The dot is hidden by the theme's base nested style and revealed
			// by the :checked state style (see BuildTheme).
			var dot = new Ellipse
			{
				Name = "FwRadio_Dot",
				Width = dotSize,
				Height = dotSize,
				Fill = DotFill,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			var boxPanel = new Panel
			{
				Width = box,
				Height = box,
				VerticalAlignment = VerticalAlignment.Center,
				Children = { ring, dot }
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
				// Deterministic ring→label gap so the words never butt against the circle. The same gap the
				// checkbox uses (CheckboxLabelGap), so a radio group and a checkbox group line up.
				Spacing = FwAvaloniaDensity.CheckboxLabelGap,
				Children = { boxPanel, content }
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
