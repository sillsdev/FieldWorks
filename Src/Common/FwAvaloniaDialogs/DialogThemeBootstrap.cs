// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// Applies the shared dialog theme (<c>DialogTheme.axaml</c> — the spacing tokens + the host-border /
	/// dialog-root base styles) to a dialog body. Every dialog view constructor calls <see cref="Apply"/> on
	/// itself, so the theme is present whether the dialog is shown through the runtime host
	/// (<c>AvaloniaDialogHost.ShowModal</c>) or realized directly in a headless test — no separate app-builder
	/// wiring is needed, and a new dialog inherits the tokens + the structural border/padding fix just by being
	/// a dialog.
	///
	/// The theme is added to the dialog body's own <see cref="StyledElement.Styles"/> (the proven pattern used
	/// by <c>CompactDialogStyles</c>): that both (a) applies the <c>fwDialogRoot</c> / <c>fwFieldHost</c> styles
	/// AND the density (font/min-height/padding) setters to the dialog's control tree and (b) puts the token
	/// resources in scope so the view's <c>{StaticResource DialogControlGap}</c> (etc.) references resolve from
	/// inside the dialog subtree — independent of any application-level resource wiring. Because the density
	/// setters live in the theme XAML, the headless dialog tests (which call ONLY this bootstrap, not the
	/// runtime <c>AvaloniaDialogHost</c> chokepoint) render at the same WinForms density as the live dialogs.
	/// </summary>
	public static class DialogThemeBootstrap
	{
		private const string ThemeUri = "avares://FwAvaloniaDialogs/DialogTheme.axaml";

		/// <summary>
		/// Marks a dialog body whose <see cref="StyledElement.Styles"/> already carry the theme, so a second
		/// call (e.g. re-hosting) is a genuine no-op rather than appending a duplicate include.
		/// </summary>
		private static readonly AttachedProperty<bool> AppliedProperty =
			AvaloniaProperty.RegisterAttached<Control, bool>("DialogThemeApplied", typeof(DialogThemeBootstrap));

		/// <summary>
		/// Idempotently adds the dialog theme to <paramref name="dialogBody"/>'s styles. Null-tolerant so a
		/// view constructor can call it unconditionally.
		/// </summary>
		public static void Apply(Control dialogBody)
		{
			if (dialogBody == null || dialogBody.GetValue(AppliedProperty))
				return;

			// Every dialog view ctor calls Apply() before InitializeComponent(), so this is the
			// earliest point ANY dialog touches Avalonia XAML — earlier than AvaloniaDialogHost.ShowModal.
			// A caller that builds the view before calling ShowModal (the documented "view + VM + ShowModal"
			// shape) would otherwise hit "Could not create IAssetLoader" here if this is the first Avalonia
			// surface constructed in the process. EnsureInitialized() is idempotent, so this doesn't
			// duplicate the ShowModal call — it just moves the guarantee earlier in the same chain.
			SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaRuntime.EnsureInitialized();

			dialogBody.SetValue(AppliedProperty, true);
			dialogBody.Styles.Add(new StyleInclude(new Uri(ThemeUri, UriKind.Absolute))
			{
				Source = new Uri(ThemeUri, UriKind.Absolute)
			});

			// The ONE deterministic, font-proportional CheckBox style (FwCheckBoxStyle — the single authority
			// shared with the browse/region lane). Added here so BOTH dialog paths get it: the headless dialog
			// tests (which call only this bootstrap) AND the runtime host. It cannot live in DialogTheme.axaml
			// because the compact box requires REPLACING the Fluent CheckBox template (whose hardcoded 20×20 box
			// / 32px slot are local values a style selector cannot override), which the C# ControlTheme does.
			foreach (var checkBoxStyle in SIL.FieldWorks.Common.FwAvalonia.FwCheckBoxStyle.Build())
				dialogBody.Styles.Add(checkBoxStyle);

			// The radio-button counterpart (FwRadioButtonStyle — the single authority shared with the
			// browse/region lane), added here for the same reason and via the same path as the checkbox: it
			// REPLACES the Fluent RadioButton template (whose hardcoded ~20px ellipse on a tall slot are local
			// values a style selector cannot override), so it cannot live in DialogTheme.axaml. Added in BOTH
			// dialog paths — the headless tests and the runtime host.
			foreach (var radioStyle in SIL.FieldWorks.Common.FwAvalonia.FwRadioButtonStyle.Build())
				dialogBody.Styles.Add(radioStyle);

			// A control's own Styles target its DESCENDANTS, not itself, so the `fwDialogRoot` window-padding
			// style cannot reach the dialog body from here. Apply that one structurally in code: every dialog
			// body IS the root, so it must carry DialogWindowPadding — even a view that omits its own padding.
			// (The `fwFieldHost` border style DOES reach the descendant host borders through the include above.)
			if (dialogBody is TemplatedControl templated && templated.Padding == default(Thickness)
				&& dialogBody.TryGetResource("DialogWindowPadding", null, out var pad) && pad is Thickness window)
			{
				templated.Padding = window; // currently DialogWindowPadding = 10 (see DialogTheme.axaml)
			}
		}
	}
}
