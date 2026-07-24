// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Styling;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Compact density for Avalonia dialogs — the design baseline so migrated dialogs match the legacy
	/// WinForms dialog density (small font, tight padding, no Fluent min-height floors) rather than the
	/// roomy Fluent defaults. Applied once by <see cref="AvaloniaDialogHost"/> to every hosted dialog
	/// body, so EVERY dialog shown through the kit inherits it automatically — new dialogs need no
	/// per-dialog density work. Scoped to the dialog's control subtree (added to its <c>Styles</c>), so
	/// it never affects the region/table surfaces, which own their own density (<see cref="FwAvaloniaDensity"/>).
	///
	/// AUTHORITATIVE SOURCE: the same density setters now also live in <c>DialogTheme.axaml</c> (applied by
	/// <c>DialogThemeBootstrap.Apply</c> in every dialog view ctor), so the density renders in the headless
	/// dialog tests too — which apply only the theme, never this runtime chokepoint. The values here are kept
	/// numerically identical to the theme's so the runtime and headless paths can never diverge; change BOTH
	/// together. This runtime apply is a belt-and-suspenders duplicate of the theme's (both are idempotent).
	/// </summary>
	public static class CompactDialogStyles
	{
		/// <summary>Dialog body font (≈ the legacy Segoe UI 9pt WinForms dialog font, vs the ~14px Fluent
		/// default). Mirrors <c>DialogFontSize</c> in DialogTheme.axaml.</summary>
		public const double DialogFontSize = 12.0;

		/// <summary>Min height for compact line controls (buttons/combos/text boxes), vs the Fluent ~32px floor.
		/// Genuine WinForms line controls run 20-23px, but that falls below the ~24px desktop pointer-target
		/// accessibility floor, so 24 is used instead (see the note on <c>DialogMinControlHeight</c> in
		/// DialogTheme.axaml and the "Dialog spacing tokens" note in style-system.md).
		/// Mirrors <c>DialogMinControlHeight</c> in DialogTheme.axaml.</summary>
		public const double LineControlMinHeight = 24.0;

		/// <summary>
		/// Marks a control whose subtree already has the compact styles, so <see cref="Apply"/> is genuinely
		/// idempotent (a second call is a no-op rather than appending a duplicate style set).
		/// </summary>
		private static readonly AttachedProperty<bool> AppliedProperty =
			AvaloniaProperty.RegisterAttached<Control, bool>("CompactDialogStylesApplied", typeof(CompactDialogStyles));

		/// <summary>
		/// Adds the compact dialog styles to a dialog body's control subtree. Idempotent: calling it again on
		/// the same control does nothing (the styles are added at most once), so re-hosting or a double call
		/// can't stack duplicate styles.
		/// </summary>
		public static void Apply(Control dialogBody)
		{
			if (dialogBody == null || dialogBody.GetValue(AppliedProperty))
				return;
			dialogBody.SetValue(AppliedProperty, true);
			foreach (var style in Build())
				dialogBody.Styles.Add(style);
		}

		private static IEnumerable<IStyle> Build()
		{
			yield return Templated<Button>(new Thickness(8, 2), LineControlMinHeight);
			yield return Templated<ComboBox>(new Thickness(6, 1), LineControlMinHeight);
			yield return Templated<TextBox>(new Thickness(4, 2), LineControlMinHeight);
			// Tabs size to content (drop the Fluent min-height floor) for compact rows.
			yield return Templated<TabItem>(new Thickness(8, 3), 0);

			// NOTE: the deterministic CheckBox style (FwCheckBoxStyle) is NOT added here. It is applied once,
			// to every dialog body, by DialogThemeBootstrap.Apply (called from each dialog ctor in BOTH the
			// runtime host and the headless dialog tests), so it reaches the headless path that never runs this
			// runtime chokepoint — and stays a single application rather than a double one.

			yield return new Style(s => s.OfType<TextBlock>())
			{
				Setters = { new Setter(TextBlock.FontSizeProperty, DialogFontSize) }
			};
			yield return new Style(s => s.OfType<ListBoxItem>())
			{
				Setters =
				{
					new Setter(Layoutable.MinHeightProperty, 0.0),
					new Setter(TemplatedControl.PaddingProperty, new Thickness(4, 1))
				}
			};
		}

		private static Style Templated<T>(Thickness padding, double minHeight) where T : TemplatedControl
		{
			var style = new Style(s => s.OfType<T>());
			style.Setters.Add(new Setter(TemplatedControl.FontSizeProperty, DialogFontSize));
			style.Setters.Add(new Setter(TemplatedControl.PaddingProperty, padding));
			style.Setters.Add(new Setter(Layoutable.MinHeightProperty, minHeight));
			return style;
		}
	}
}
