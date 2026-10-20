// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Makes the font named by the FwUiFontFamily token, bundled in FwAvaloniaTheme, the
	/// interface
	/// font of every Avalonia surface, followed by the fallback fonts the UI language needs for
	/// scripts that font lacks. The platform default font is never used for interface text, so
	/// every machine and test run draws the same. Data text is untouched: it keeps the font of
	/// its writing system. Semi reads its text font from overridable application resources, so
	/// overriding those keys is enough and no FontManagerOptions default is set (that would also
	/// change the fallback for data text).
	/// </summary>
	public static class FwUiFont
	{
		/// <summary>Key of the private font collection holding the bundled interface
		/// font.</summary>
		public const string CollectionKey = "fonts:FieldWorks";

		/// <summary>Location of the bundled interface font files inside
		/// FwAvaloniaTheme.dll.</summary>
		public const string AssetsUri = "avares://FwAvaloniaTheme/Assets/Fonts";

		private const string DefaultFontFamilyKey = "DefaultFontFamily";

		/// <summary>The fallback list of each FieldWorks UI language whose script the bundled
		/// font lacks, in
		/// the order tried. A family that is not installed is skipped, so each list names a
		/// Windows system font first and an SIL or Noto family second. Keyed by language code, so
		/// every Chinese culture shares one list.</summary>
		private static readonly Dictionary<string, string[]> FallbacksByLanguage =
			new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["fa"] = new[] { "Segoe UI", "Scheherazade New" },
			["hi"] = new[] { "Nirmala UI", "Annapurna SIL" },
			["ko"] = new[] { "Malgun Gothic", "Noto Sans CJK KR" },
			["te"] = new[] { "Nirmala UI", "Noto Sans Telugu" },
			["zh"] = new[] { "Microsoft YaHei UI", "Noto Sans CJK SC" },
		};

		private static FontManager s_registeredWith;

		/// <summary>The interface font family in effect: the bundled font followed by the
		/// fallbacks of the UI language the application started in.</summary>
		public static FontFamily UiFontFamily
			=> FwThemeResources.RequireFontFamily(DefaultFontFamilyKey);

		/// <summary>The fallback family names, in order, that <paramref name="uiCulture"/> needs
		/// after the bundled font. Empty when it covers the language's script.</summary>
		public static IReadOnlyList<string> FallbackFamiliesFor(CultureInfo uiCulture)
		{
			var tag = (uiCulture ?? CultureInfo.InvariantCulture).Name;
			var subtags = tag.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
			if (subtags.Length == 0)
				return new string[0];
			return FallbacksByLanguage.TryGetValue(subtags[0], out var families)
				? families : new string[0];
		}

		/// <summary>The family stack for <paramref name="uiCulture"/>: the bundled font from
		/// the FwUiFontFamily token, then that language's fallbacks.</summary>
		public static FontFamily FamilyFor(CultureInfo uiCulture)
		{
			var names = new List<string>
			{
				FwThemeResources.RequireFontFamily(GeneratedTokenKeys.FwUiFontFamily).ToString()
			};
			names.AddRange(FallbackFamiliesFor(uiCulture));
			return FontFamily.Parse(string.Join(", ", names));
		}

		/// <summary>Registers the bundled font and points Semi's text-font resources at the
		/// interface family stack for the current UI language. Calling it again for the same
		/// process re-registers nothing. The FwAvaloniaTheme token dictionary must already be
		/// merged into <paramref name="app"/>.</summary>
		public static void ApplyTo(Application app)
		{
			var fonts = FontManager.Current;
			if (!ReferenceEquals(s_registeredWith, fonts))
			{
				var collectionUri = new Uri(CollectionKey);
				fonts.AddFontCollection(new EmbeddedFontCollection(collectionUri, new Uri(AssetsUri)));
				s_registeredWith = fonts;
			}

			var family = FamilyFor(CultureInfo.CurrentUICulture);
			app.Resources[DefaultFontFamilyKey] = family;
			app.Resources["ContentControlThemeFontFamily"] = family;
			app.Resources["SemiFontFamilyRegular"] = family;
		}
	}
}
