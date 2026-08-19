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
	/// Compact density for Avalonia dialogs -- the design baseline so migrated dialogs match the
	/// legacy
	/// WinForms dialog density (small font, tight padding, no Fluent min-height floors) rather than the
	/// roomy Fluent defaults. Applied once by <see cref="AvaloniaDialogHost"/> to every hosted
	/// dialog
	/// body, so EVERY dialog shown through the host inherits it automatically -- new dialogs need
	/// no
	/// per-dialog density work. Scoped to the dialog's control subtree (added to its <c>Styles</c>), so
	/// it never affects the detail/table views, which own their own density (<see cref="FwAvaloniaDensity"/>).
	///
	/// The same density also lives in <c>DialogTheme.axaml</c>, which the headless dialog tests apply instead
	/// of this runtime chokepoint, so both paths must carry the same numbers: CHANGE BOTH
	/// TOGETHER. Font size resolves the single FwSurfaceFontSize token from
	/// Src/Common/FwAvaloniaTheme (see <see cref="DialogFontSize"/>); the padding Thickness
	/// literals resolve <see cref="GeneratedTokenKeys"/>' baked copies of DialogTheme.axaml's own
	/// Dialog*Padding tokens, since Avalonia's compiled XAML rejects <c>x:Static</c> as a
	/// resource
	/// declaration and this file cannot read them via <c>{StaticResource}</c> at runtime.
	/// <see cref="LineControlMinHeight"/> stays an independent literal: it mirrors
	/// DialogMinControlHeight, which is itself an alias onto a Semi resource with no token text
	/// to
	/// bake.
	/// </summary>
	public static class CompactDialogStyles
	{
		/// <summary>Dialog body font, tuned against the legacy WinForms dialogs (Segoe UI 9pt) and well
		/// below the ~14px Fluent default. Resolved from the shared FwAvaloniaTheme token
		/// dictionary
		/// (FwSurfaceFontSize), the same key DialogTheme.axaml's <c>DialogFontSize</c> now points
		/// at.
		/// A property, not a field: resolved at point-of-use, after the Application has
		/// started.</summary>
		public static double DialogFontSize => FwThemeResources.RequireDouble(GeneratedTokenKeys.FwSurfaceFontSize);

		/// <summary>Min height for compact line controls (buttons/combos/text boxes), vs the Fluent ~32px floor.
		/// Genuine WinForms line controls run 20-23px, but that falls below the ~24px desktop pointer-target
		/// accessibility floor, so 24 is used instead (see the note on <c>DialogMinControlHeight</c> in
		/// DialogTheme.axaml).
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
			// GeneratedTokenKeys' baked copies of DialogTheme.axaml's Dialog*Padding tokens:
			// compiled XAML rejects x:Static, so this C# path cannot read them via
			// {StaticResource}.
			yield return Templated<Button>(GeneratedTokenKeys.DialogButtonPaddingValue, LineControlMinHeight);
			yield return Templated<ComboBox>(GeneratedTokenKeys.DialogComboBoxPaddingValue, LineControlMinHeight);
			yield return Templated<TextBox>(GeneratedTokenKeys.DialogTextBoxPaddingValue, LineControlMinHeight);
			// Tabs size to content (drop the Fluent min-height floor) for compact rows.
			yield return Templated<TabItem>(GeneratedTokenKeys.DialogTabItemPaddingValue, 0);

			// CheckBox/RadioButton density is not set here: Semi sizes them from overridable
			// resources that FwSemiDensity retargets once at Application level.

			yield return new Style(s => s.OfType<TextBlock>())
			{
				Setters = { new Setter(TextBlock.FontSizeProperty, DialogFontSize) }
			};
			yield return new Style(s => s.OfType<ListBoxItem>())
			{
				Setters =
				{
					new Setter(Layoutable.MinHeightProperty, 0.0),
					new Setter(TemplatedControl.PaddingProperty, GeneratedTokenKeys.DialogListBoxItemPaddingValue)
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
