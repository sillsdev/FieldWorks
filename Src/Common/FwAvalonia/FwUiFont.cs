// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Applies the interface font token and UI-language fallbacks to Semi's resources.
	/// Data text retains its writing-system font.
	/// </summary>
	public static class FwUiFont
	{
		private const string DefaultFontFamilyKey = "DefaultFontFamily";

		/// <summary>Named UI-language fallbacks, in the order tried. Each list names a Windows
		/// font first and an SIL or Noto family second. Keyed by language code, so
		/// every Chinese culture shares one list.</summary>
		private static readonly Dictionary<string, string[]> FallbacksByLanguage =
			new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["ar"] = new[] { "Segoe UI", "Scheherazade New" },
			["bn"] = new[] { "Nirmala UI", "Noto Sans Bengali" },
			["fa"] = new[] { "Segoe UI", "Scheherazade New" },
			["hi"] = new[] { "Nirmala UI", "Annapurna SIL" },
			["km"] = new[] { "Leelawadee UI", "Khmer UI", "Noto Sans Khmer" },
			["ko"] = new[] { "Malgun Gothic", "Noto Sans CJK KR" },
			["ml"] = new[] { "Nirmala UI", "Noto Sans Malayalam" },
			["my"] = new[] { "Myanmar Text", "Padauk" },
			["ne"] = new[] { "Nirmala UI", "Annapurna SIL" },
			["ta"] = new[] { "Nirmala UI", "Noto Sans Tamil" },
			["te"] = new[] { "Nirmala UI", "Noto Sans Telugu" },
			["th"] = new[] { "Leelawadee UI", "Noto Sans Thai" },
			["ur"] = new[] { "Segoe UI", "Scheherazade New" },
			["zh"] = new[] { "Microsoft YaHei UI", "Noto Sans CJK SC" },
		};

		/// <summary>
		/// The interface font family in effect for the UI language at application startup.
		/// </summary>
		public static FontFamily UiFontFamily
			=> FwThemeResources.RequireFontFamily(DefaultFontFamilyKey);

		/// <summary>The fallback family names, in order, that <paramref name="uiCulture"/> needs
		/// after the theme font.</summary>
		public static IReadOnlyList<string> FallbackFamiliesFor(CultureInfo uiCulture)
		{
			var tag = (uiCulture ?? CultureInfo.InvariantCulture).Name;
			var subtags = tag.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
			if (subtags.Length == 0)
				return new string[0];
			return FallbacksByLanguage.TryGetValue(subtags[0], out var families)
				? families : new string[0];
		}

		/// <summary>
		/// The theme font, UI-language fallbacks, and platform default for
		/// <paramref name="uiCulture"/>.
		/// </summary>
		public static FontFamily FamilyFor(CultureInfo uiCulture)
		{
			var names = new List<string>
			{
				FwThemeResources.RequireFontFamily(GeneratedTokenKeys.FwUiFontFamily).ToString()
			};
			names.AddRange(FallbackFamiliesFor(uiCulture));
			names.Add(FontManager.Current.DefaultFontFamily.ToString());
			return FontFamily.Parse(string.Join(", ", names));
		}

		/// <summary>Points Semi's text-font resources at the interface family stack for the
		/// current UI language. The FwAvaloniaTheme token dictionary must already be merged
		/// into <paramref name="app"/>.</summary>
		public static void ApplyTo(Application app)
		{
			var family = FamilyFor(CultureInfo.CurrentUICulture);
			app.Resources[DefaultFontFamilyKey] = family;
			app.Resources["ContentControlThemeFontFamily"] = family;
			app.Resources["SemiFontFamilyRegular"] = family;
		}
	}
}
