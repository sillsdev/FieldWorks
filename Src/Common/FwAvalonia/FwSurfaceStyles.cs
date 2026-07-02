// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// The shared WinForms-density FONT baseline for the owned NON-dialog surfaces — the lexical-edit
	/// detail/region view and the browse table. Applied to each surface's own control subtree (added to its
	/// <c>Styles</c>) by its view constructor, the proven per-control-tree mechanism: it renders the same in
	/// the runtime host (<c>FwAvaloniaHost</c>/<c>FwAvaloniaApp</c>) and the headless Skia tests, INDEPENDENT
	/// of application-level resource wiring (a prior agent confirmed app-level <c>Application.Styles</c> do not
	/// apply in the headless test app, so this scoped path is the reliable one).
	///
	/// WHY ONLY THE FONT (not the boxing/padding the dialogs get): these surfaces are intentionally NOT boxed.
	/// The region/detail view is FLAT with subtle 1px field separators like the WinForms DataTree, and the
	/// browse table draws its own grid lines (<see cref="FwAvaloniaDensity.BrowseGridLineBrush"/>) — both own
	/// their structural look through <see cref="FwAvaloniaDensity"/> literals (concrete values that already
	/// render headlessly). The one thing those literals do NOT override is the Fluent ~14px default font on a
	/// plain <see cref="TextBlock"/>/<see cref="TextBox"/>; this drops it to the WinForms ~12px so the detail
	/// and browse text reads at the same density as the dialogs. The dialog density lives in
	/// <c>DialogTheme.axaml</c> / <see cref="CompactDialogStyles"/>; this is its region/browse counterpart, so
	/// font density has ONE WinForms value (12) across every Avalonia surface.
	/// </summary>
	public static class FwSurfaceStyles
	{
		/// <summary>The WinForms surface font (Segoe UI 9pt ≈ 12px), matching the dialog density font.</summary>
		public const double SurfaceFontSize = 12.0;

		/// <summary>
		/// Marks a surface whose subtree already carries the styles, so a second call is a genuine no-op
		/// rather than appending a duplicate style set.
		/// </summary>
		private static readonly AttachedProperty<bool> AppliedProperty =
			AvaloniaProperty.RegisterAttached<Control, bool>("FwSurfaceStylesApplied", typeof(FwSurfaceStyles));

		/// <summary>
		/// Idempotently adds the surface font baseline to <paramref name="surface"/>'s styles. Null-tolerant so
		/// a view constructor can call it unconditionally.
		/// </summary>
		public static void Apply(Control surface)
		{
			if (surface == null || surface.GetValue(AppliedProperty))
				return;
			surface.SetValue(AppliedProperty, true);
			foreach (var style in Build())
				surface.Styles.Add(style);
		}

		private static IEnumerable<IStyle> Build()
		{
			// TextBlock — the region labels/values and browse cells/headers. A per-control FontSize (e.g. the
			// smaller WS abbreviation, the bold browse header) is a LOCAL value that outranks this style setter,
			// so those keep their explicit size; only the unset Fluent default is replaced.
			yield return new Style(s => s.OfType<TextBlock>())
			{
				Setters = { new Setter(TextBlock.FontSizeProperty, SurfaceFontSize) }
			};
			// TextBox — the editable detail values / browse cells. Font only: the editors are deliberately
			// borderless/transparent (flat like the legacy RootSite views), so we do NOT add a border or a
			// min-height floor here the way the boxed dialog inputs get.
			yield return new Style(s => s.OfType<TextBox>())
			{
				Setters = { new Setter(TemplatedControl.FontSizeProperty, SurfaceFontSize) }
			};
			// TabItem — the bulk-edit bar's mode tabs. Fluent's default tab headers are large (~16px semibold
			// with tall padding); with several tabs on a narrow bar they WRAP to a second row, eating the bar
			// height and pushing the tab CONTENT (the Target/Apply row) off the bottom. Drop them to the surface
			// font with compact padding and no tall min-height floor so the modes sit in ONE compact row, like
			// the legacy WinForms bulk-edit bar's tab strip.
			yield return new Style(s => s.OfType<TabItem>())
			{
				Setters =
				{
					new Setter(TemplatedControl.FontSizeProperty, SurfaceFontSize),
					new Setter(TemplatedControl.FontWeightProperty, FontWeight.Normal),
					// Bottom padding (6) leaves room UNDER the label for the Fluent selected-tab underline so the
					// blue indicator sits below the text instead of on top of it; top 3 keeps the tab compact.
					new Setter(TemplatedControl.PaddingProperty, new Thickness(8, 3, 8, 6)),
					new Setter(Layoutable.MinHeightProperty, 0d)
				}
			};

			// The ONE deterministic, font-proportional CheckBox style (the same definition the dialog lane
			// gets), so a browse/table/tree select checkbox is sized to FwAvaloniaDensity.CheckboxBoxSize and
			// never inflates a row past BrowseRowMinHeight.
			foreach (var checkBoxStyle in FwCheckBoxStyle.Build())
				yield return checkBoxStyle;

			// The ONE deterministic, font-proportional RadioButton style (its checkbox counterpart, the same
			// definition the dialog lane gets), so a radio is sized to FwAvaloniaDensity.RadioBoxSize and never
			// inflates a row past the text line.
			foreach (var radioStyle in FwRadioButtonStyle.Build())
				yield return radioStyle;
		}
	}
}
