// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Globalization;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Maps the current UI culture onto the locale each Semi theme actually supports. Both
	/// Semi.Avalonia.SemiTheme and Ursa.Themes.Semi.SemiTheme default to (and reset to) zh-CN on
	/// an unrecognized Locale, so callers must always pass one of these results, never the raw
	/// UI culture.
	/// </summary>
	public static class FwSemiLocale
	{
		private const string Fallback = "en-US";

		// Semi.Avalonia.SemiTheme's 16 supported locales (v11.3.14), keyed by two-letter language
		// for fallback matching (e.g. fr-CA -> fr-FR).
		private static readonly Dictionary<string, string> SemiByLanguage = new Dictionary<string, string>
		{
			{ "zh", "zh-CN" }, { "en", "en-US" }, { "it", "it-IT" }, { "nl", "nl-NL" },
			{ "ja", "ja-JP" }, { "ko", "ko-KR" }, { "uk", "uk-UA" }, { "ru", "ru-RU" },
			{ "de", "de-DE" }, { "es", "es-ES" }, { "pl", "pl-PL" }, { "fr", "fr-FR" }
		};

		private static readonly HashSet<string> SemiExact = new HashSet<string>
		{
			"zh-CN", "en-US", "en-GB", "it-IT", "it-CH", "nl-BE", "nl-NL", "ja-JP",
			"ko-KR", "uk-UA", "ru-RU", "zh-TW", "de-DE", "es-ES", "pl-PL", "fr-FR"
		};

		// Ursa.Themes.Semi.SemiTheme supports only 4 locales (v1.15.1), a strict subset of
		// Semi's.
		private static readonly Dictionary<string, string> UrsaByLanguage = new Dictionary<string, string>
		{
			{ "zh", "zh-CN" }, { "en", "en-US" }, { "fr", "fr-FR" }, { "ru", "ru-RU" }
		};

		private static readonly HashSet<string> UrsaExact = new HashSet<string>
		{
			"zh-CN", "en-US", "fr-FR", "ru-RU"
		};

		/// <summary>Locale to hand Semi.Avalonia.SemiTheme.Locale; never null, never zh-CN unless
		/// the culture is Chinese.</summary>
		public static CultureInfo ForSemi(CultureInfo uiCulture) => Resolve(uiCulture, SemiExact, SemiByLanguage);

		/// <summary>Locale to hand Ursa.Themes.Semi.SemiTheme.Locale; never null, never zh-CN
		/// unless the culture is Chinese.</summary>
		public static CultureInfo ForUrsa(CultureInfo uiCulture) => Resolve(uiCulture, UrsaExact, UrsaByLanguage);

		private static CultureInfo Resolve(CultureInfo uiCulture, HashSet<string> exact, Dictionary<string, string> byLanguage)
		{
			var name = uiCulture?.Name;
			if (!string.IsNullOrEmpty(name) && exact.Contains(name))
				return uiCulture;

			var language = uiCulture?.TwoLetterISOLanguageName;
			if (!string.IsNullOrEmpty(language) && byLanguage.TryGetValue(language, out var match))
				return new CultureInfo(match);

			return new CultureInfo(Fallback);
		}
	}
}
