// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using L10NSharp;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogsTests
{
	[TestFixture]
	public sealed class AvaloniaLocalizationTests
	{
		[Test]
		public void GetPalasoString_ReturnsTranslatedValue_WhenCatalogAndLanguageExist()
		{
			FwAvaloniaLocalizationBootstrap.EnsureInitialized();
			var original = LocalizationManager.UILanguageId;

			try
			{
				LocalizationManager.SetUILanguage("es");
				Assert.That(
					FwAvaloniaLocalization.GetPalasoString("AboutDialog.NoUpdates", "No Updates"),
					Is.EqualTo("No hay actualizaciones"));
			}
			finally
			{
				LocalizationManager.SetUILanguage(original);
			}
		}

		[Test]
		public void GetChorusString_ReturnsTranslatedValue_WhenCatalogAndLanguageExist()
		{
			FwAvaloniaLocalizationBootstrap.EnsureInitialized();
			var original = LocalizationManager.UILanguageId;

			try
			{
				LocalizationManager.SetUILanguage("es");
				Assert.That(
					FwAvaloniaLocalization.GetChorusString("Common.Help", "Help"),
					Is.EqualTo("Ayuda"));
			}
			finally
			{
				LocalizationManager.SetUILanguage(original);
			}
		}

		[Test]
		public void GetString_ReturnsEnglishFallback_WhenAppManagerIsMissing()
		{
			Assert.That(
				FwAvaloniaLocalization.GetString("Missing.Avalonia.Manager", "FwAvalonia.Test.Fallback", "English fallback"),
				Is.EqualTo("English fallback"));
		}
	}
}