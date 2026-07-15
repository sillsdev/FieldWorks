// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using L10NSharp;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Runtime localization helper for Avalonia chrome strings. Product hosts are expected to initialize
	/// the existing Palaso/Chorus LocalizationManagers first; preview/test hosts can use the lightweight
	/// bootstrap below. If the requested manager is unavailable, callers fall back to the compiled English
	/// seed text passed in.
	/// </summary>
	public static class FwAvaloniaLocalization
	{
		public const string PalasoAppId = "Palaso";
		public const string ChorusAppId = "Chorus";

		public static string GetPalasoString(string stringId, string englishText, string comment = null)
			=> GetString(PalasoAppId, stringId, englishText, comment);

		public static string GetChorusString(string stringId, string englishText, string comment = null)
			=> GetString(ChorusAppId, stringId, englishText, comment);

		public static string GetString(string appId, string stringId, string englishText, string comment = null)
		{
			if (string.IsNullOrEmpty(englishText))
				return englishText;

			try
			{
				var localized = string.IsNullOrEmpty(comment)
					? LocalizationManager.GetDynamicString(appId, stringId, englishText)
					: LocalizationManager.GetDynamicString(appId, stringId, englishText, comment);

				return string.IsNullOrEmpty(localized) || localized == stringId
					? englishText
					: localized;
			}
			catch (ArgumentException)
			{
				return englishText;
			}
			catch (InvalidOperationException)
			{
				return englishText;
			}
		}
	}

	/// <summary>
	/// Minimal bootstrap for non-product hosts that need the existing Palaso/Chorus XLIFF catalogs.
	/// It is idempotent and best-effort so preview/headless test hosts can opt in without paying a
	/// per-test setup cost.
	/// </summary>
	public static class FwAvaloniaLocalizationBootstrap
	{
		private static readonly object s_gate = new object();
		private static bool s_initialized;

		public static void EnsureInitialized()
		{
			if (s_initialized)
				return;

			lock (s_gate)
			{
				if (s_initialized)
					return;

				try
				{
					var installedLocalizationDir = ResolveInstalledLocalizationDirectory();
					if (installedLocalizationDir != null)
					{
						LocalizationManager.Create("en",
							FwAvaloniaLocalization.ChorusAppId,
							"Chorus",
							"1.0.0",
							installedLocalizationDir,
							"FieldWorksAvalonia",
							new[] { "Chorus", "LibChorus" });

						LocalizationManager.Create("en",
							FwAvaloniaLocalization.PalasoAppId,
							"Palaso",
							"1.0.0",
							installedLocalizationDir,
							"FieldWorksAvalonia",
							new[] { "SIL.Windows.Forms", "SIL.FieldWorks.Common.FwAvalonia", "FwAvaloniaDialogs" });

						var uiCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
						if (LocalizationManager.GetUILanguages(true).Any(lang => lang.TwoLetterISOLanguageName == uiCulture))
						{
							LocalizationManager.SetUILanguage(uiCulture);
						}
					}
				}
				catch (Exception)
				{
					// Best effort: preview and tests can fall back to the compiled English text.
				}
				finally
				{
					s_initialized = true;
				}
			}
		}

		internal static string ResolveInstalledLocalizationDirectory()
		{
			var baseDir = AppDomain.CurrentDomain.BaseDirectory;
			if (TryResolveFrom(baseDir, out var resolved))
				return resolved;

			var current = new DirectoryInfo(baseDir);
			while (current != null)
			{
				if (TryResolveFrom(current.FullName, out resolved))
					return resolved;
				current = current.Parent;
			}

			return null;
		}

		private static bool TryResolveFrom(string root, out string resolved)
		{
			var candidates = new[]
			{
				Path.Combine(root, "CommonLocalizations"),
				Path.Combine(root, "DistFiles", "CommonLocalizations")
			};

			resolved = candidates.FirstOrDefault(Directory.Exists);
			return resolved != null;
		}
	}
}