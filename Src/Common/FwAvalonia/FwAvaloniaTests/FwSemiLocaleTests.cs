// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaTests
{
	/// <summary>
	/// FwSemiLocale's locale lists are transcribed by hand from the vendor themes, and both
	/// themes reset to zh-CN on a locale they do not have -- so a list that drifts ahead of the
	/// packages silently gives some users Chinese chrome. These tests read the locales out of the
	/// shipped assemblies and compare, so a package upgrade that adds or drops one fails here
	/// instead.
	/// </summary>
	[TestFixture]
	public class FwSemiLocaleTests
	{
		/// <summary>
		/// Both vendor themes ship one resource dictionary per supported locale, named
		/// /Locale/xx-yy.axaml inside the assembly's Avalonia resource blob.
		/// </summary>
		private static IReadOnlyCollection<string> PackagedLocales(string assemblyName)
		{
			var asm = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(a => a.GetName().Name == assemblyName) ?? Assembly.Load(assemblyName);
			var names = asm.GetManifestResourceNames()
				.Where(n => n.EndsWith("!AvaloniaResources", StringComparison.Ordinal));
			var found = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var resource in names)
			{
				using (var stream = asm.GetManifestResourceStream(resource))
				{
					var buffer = new byte[stream.Length];
					var read = 0;
					while (read < buffer.Length)
					{
						var n = stream.Read(buffer, read, buffer.Length - read);
						if (n == 0)
							break;
						read += n;
					}
					foreach (Match m in Regex.Matches(Encoding.UTF8.GetString(buffer, 0, read),
						@"/Locale/([A-Za-z]{2}-[A-Za-z]{2})\.axaml"))
					{
						found.Add(m.Groups[1].Value.ToLowerInvariant());
					}
				}
			}
			Assert.That(found, Is.Not.Empty,
				"found no /Locale/*.axaml in " + assemblyName + "; the vendor's resource layout "
				+ "changed and this test needs updating before it can guard anything");
			return found;
		}

		private static IReadOnlyCollection<string> DeclaredLocales(string fieldName)
		{
			var field = typeof(FwSemiLocale).GetField(fieldName,
				BindingFlags.NonPublic | BindingFlags.Static);
			Assert.That(field, Is.Not.Null, fieldName + " must exist for this test to guard it");
			var set = (HashSet<string>)field.GetValue(null);
			return new SortedSet<string>(set.Select(s => s.ToLowerInvariant()),
				StringComparer.OrdinalIgnoreCase);
		}

		[TestCase("Semi.Avalonia", "SemiExact")]
		[TestCase("Ursa.Themes.Semi", "UrsaExact")]
		public void DeclaredLocales_MatchTheShippedPackage(string assemblyName, string fieldName)
		{
			var packaged = PackagedLocales(assemblyName);
			var declared = DeclaredLocales(fieldName);

			Assert.That(declared, Is.EquivalentTo(packaged),
				fieldName + " has drifted from " + assemblyName + ". Missing from the list: "
				+ string.Join(",", packaged.Except(declared)) + "; listed but not shipped: "
				+ string.Join(",", declared.Except(packaged)));
		}

		/// <summary>
		/// Ursa supports a strict subset of Semi's locales, which is what lets one UI culture
		/// resolve to two different theme locales without either resetting.
		/// </summary>
		[Test]
		public void UrsaLocales_AreASubsetOfSemis()
		{
			Assert.That(DeclaredLocales("UrsaExact"),
				Is.SubsetOf(DeclaredLocales("SemiExact")));
		}

		/// <summary>
		/// The bug this class exists to prevent: an unrecognized culture must never come back as
		/// zh-CN, or an Arabic user gets Chinese context menus.
		/// </summary>
		[TestCase("ar-SA")]
		[TestCase("he-IL")]
		[TestCase("sw-KE")]
		[TestCase("th-TH")]
		public void UnsupportedCulture_FallsBackToEnglish_NotChinese(string culture)
		{
			var ui = new CultureInfo(culture);

			Assert.That(FwSemiLocale.ForSemi(ui).Name, Is.EqualTo("en-US"));
			Assert.That(FwSemiLocale.ForUrsa(ui).Name, Is.EqualTo("en-US"));
		}

		/// <summary>
		/// A culture Semi supports but Ursa does not must resolve per theme, not to the lowest
		/// common denominator and not to zh-CN.
		/// </summary>
		[Test]
		public void CultureSupportedBySemiOnly_ResolvesPerTheme()
		{
			var german = new CultureInfo("de-DE");

			Assert.That(FwSemiLocale.ForSemi(german).Name, Is.EqualTo("de-DE"),
				"Semi ships de-DE, so it should be used");
			Assert.That(FwSemiLocale.ForUrsa(german).Name, Is.EqualTo("en-US"),
				"Ursa has no de-DE; English is correct and zh-CN would be the bug");
		}

		/// <summary>A regional variant with no exact dictionary falls back by language.</summary>
		[TestCase("fr-CA", "fr-FR")]
		[TestCase("de-AT", "de-DE")]
		[TestCase("es-MX", "es-ES")]
		public void RegionalVariant_FallsBackByLanguage(string culture, string expected)
		{
			Assert.That(FwSemiLocale.ForSemi(new CultureInfo(culture)).Name, Is.EqualTo(expected));
		}

		/// <summary>Chinese is the one culture that should legitimately get zh-CN.</summary>
		[Test]
		public void ChineseCulture_StillGetsChinese()
		{
			Assert.That(FwSemiLocale.ForSemi(new CultureInfo("zh-CN")).Name, Is.EqualTo("zh-CN"));
			Assert.That(FwSemiLocale.ForUrsa(new CultureInfo("zh-CN")).Name, Is.EqualTo("zh-CN"));
		}
	}
}
