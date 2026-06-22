// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Resources;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// Localized product-facing strings for the Avalonia MVVM dialogs. Standard .resx-backed resources
	/// (Crowdin-compatible via the project's &lt;RootNamespace&gt;); automation ids stay nonlocalized
	/// constants in XAML, never resource lookups. Each Avalonia UI project owns its own product-message
	/// resources — this is the dialogs' equivalent of the foundation's <c>FwAvaloniaStrings</c>. Bind from
	/// XAML with <c>{x:Static …}</c>.
	/// </summary>
	public static class FwAvaloniaDialogsStrings
	{
		private static readonly ResourceManager Resources =
			new ResourceManager("FwAvaloniaDialogs.FwAvaloniaDialogsStrings", typeof(FwAvaloniaDialogsStrings).Assembly);

		public static string OptionsTitle => Resources.GetString("ksOptionsTitle");

		// Tab headers (the four real Options tabs).
		public static string GeneralTab => Resources.GetString("ksGeneralTab");
		public static string PluginsTab => Resources.GetString("ksPluginsTab");
		public static string PrivacyTab => Resources.GetString("ksPrivacyTab");
		public static string UpdatesTab => Resources.GetString("ksUpdatesTab");

		// General tab.
		public static string UiLanguageLabel => Resources.GetString("ksUiLanguageLabel");
		public static string UiLanguageNote => Resources.GetString("ksUiLanguageNote");
		public static string LexicalEditUiLabel => Resources.GetString("ksLexicalEditUiLabel");
		public static string UiModeLegacy => Resources.GetString("ksUiModeLegacy");
		public static string UiModeNew => Resources.GetString("ksUiModeNew");
		public static string Apply => Resources.GetString("ksApply");
		public static string AutoOpenLastProject => Resources.GetString("ksAutoOpenLastProject");

		// Plugins tab.
		public static string PluginsUnavailableNote => Resources.GetString("ksPluginsUnavailableNote");

		// Privacy tab.
		public static string PrivacyNote => Resources.GetString("ksPrivacyNote");
		public static string OkToPing => Resources.GetString("ksOkToPing");

		// Updates tab.
		public static string AutoUpdate => Resources.GetString("ksAutoUpdate");
		public static string UpdateChannelLabel => Resources.GetString("ksUpdateChannelLabel");

		public static string Ok => Resources.GetString("ksOk");
		public static string Cancel => Resources.GetString("ksCancel");
	}
}
