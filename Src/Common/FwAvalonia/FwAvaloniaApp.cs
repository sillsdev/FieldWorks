// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Globalization;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using SemiCore = Semi.Avalonia.SemiTheme;
using SemiUrsa = Ursa.Themes.Semi.SemiTheme;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Minimal Avalonia <see cref="Application"/> for the shared FieldWorks Avalonia controls.
	/// Adds the Semi theme (core plus Ursa's Semi-styled controls) so the pure-C# controls
	/// receive templates in the Preview Host and in headless tests alike.
	/// </summary>
	public sealed class FwAvaloniaApp : Application
	{
		public override void Initialize()
		{
			// Light is the only reviewed variant; Dark is a first-pass placeholder.
			// Unset, this follows the OS theme, and Dark being complete means it
			// would resolve silently rather than fail.
			RequestedThemeVariant = ThemeVariant.Light;
			Styles.Add(new SemiCore { Locale = FwSemiLocale.ForSemi(CultureInfo.CurrentUICulture) });
			Styles.Add(new SemiUrsa { Locale = FwSemiLocale.ForUrsa(CultureInfo.CurrentUICulture) });
			FwSemiDensity.ApplyTo(this);
			// Shared color/brush + font-size token tier (Src/Common/FwAvaloniaTheme), so the
			// foundation's Fw*Brush/FwSurfaceFontSize resources resolve app-wide.
			MergeTokens("avares://FwAvaloniaTheme/Tokens/FwColorTokens.axaml");
			// DataTree/detail-view layout dimensions, so FwAvaloniaDensity.cs's DataTree.* keys
			// resolve app-wide.
			MergeTokens("avares://FwAvaloniaTheme/Tokens/DataTree/DataTreeTokens.axaml");
			// Shared dialog spacing/border tokens, so the Dialog* keys this assembly
			// publishes as generated constants are actually registered here too.
			MergeTokens("avares://FwAvaloniaTheme/Tokens/DialogTokens.axaml");
		}

		private void MergeTokens(string avaresUri)
		{
			var tokenUri = new System.Uri(avaresUri);
			Resources.MergedDictionaries.Add(new ResourceInclude(tokenUri) { Source = tokenUri });
		}
	}

	/// <summary>
	/// AppBuilder configuration for shared FieldWorks Avalonia hosting. Desktop/Win32 platform
	/// detection is used for the in-process embedding path beside WinForms; headless tests configure
	/// their own platform.
	/// </summary>
	public static class FwAvaloniaHost
	{
		/// <summary>Builds the AppBuilder for desktop (Win32) hosting.</summary>
		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<FwAvaloniaApp>()
				.UsePlatformDetect()
				.LogToTrace();
	}
}
