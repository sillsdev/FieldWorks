// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using L10NSharp;
using L10NSharp.Windows.Forms;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Test-only <see cref="IHelpTopicProvider"/> that has no help file. The product app sets the
	/// PropertyTable "HelpTopicProvider" to the real <c>FwApp</c>, which delegates to a non-null
	/// inner provider; the test <c>MockFwXApp</c> is built with a null inner provider, so its
	/// <c>GetHelpString</c> throws an NRE the moment the legacy DataTree's Help command queries it
	/// (DTMenuHandler.OnDisplayDataTreeHelp) while building the menu. Returning null here makes the
	/// Help item simply invisible/disabled — the same as the product when a slice has no help
	/// topic — instead of crashing menu materialization.
	/// </summary>
	internal sealed class TestNullHelpTopicProvider : IHelpTopicProvider
	{
		public string GetHelpString(string ksPropName) => null;
		public string HelpFile => string.Empty;
	}
	/// <summary>
	/// Test-only harness bootstrap (test-harness hook): registers a minimal English
	/// <c>LocalizationManager</c> so legacy DataTree slices that localize strings can be built
	/// headlessly.
	///
	/// The lexicon "detail" layout includes the <c>MessageSlice</c> (a Chorus NotesBar). When the
	/// hidden command-routing DataTree adapter builds that slice, <c>NotesBarView</c> calls
	/// <c>LocalizationManager.GetString</c>, which throws
	/// "You must create at least one LocalizationManager before trying to localize any strings"
	/// when NO manager of any kind exists in the process. The product app never hits this because
	/// <c>FieldWorks.InitializeLocalizationManager</c> creates the managers at startup; the
	/// composer-based xWorks test harness does not run that startup, so we mirror just enough of it
	/// here.
	///
	/// Prefer the shared Avalonia bootstrap first so tests that exercise Avalonia chrome strings see
	/// the real Palaso/Chorus catalogs. If that cannot find the installed XLIFF files, fall back to
	/// a minimal temp-dir Chorus manager so legacy DataTree localization still stops throwing. It is
	/// idempotent and best-effort: a failure to set up localization must not mask the real assertion
	/// under test.
	/// </summary>
	internal static class TestLocalizationManagerBootstrap
	{
		private static bool s_initialized;
		private static readonly object s_lock = new object();

		public static void EnsureInitialized()
		{
			if (s_initialized)
				return;
			lock (s_lock)
			{
				if (s_initialized)
					return;
				try
				{
					if (!HasAnyManager())
					{
						FwAvaloniaLocalizationBootstrap.EnsureInitialized();
					}

					if (!HasAnyManager())
					{
						// directoryOfInstalledFiles is an absolute (writable) temp dir; the
						// relativeSettingPathForLocalizationFolder MUST be a relative path (the
						// product passes "CommonLocalizations"). Create asserts non-rooted there.
						var baseDir = Path.Combine(Path.GetTempPath(),
							"FwXWorksTestLocalizations");
						Directory.CreateDirectory(baseDir);

						// One English XLiff manager is enough for GetStringFromAnyLocalizationManager
						// to stop throwing and fall back to the in-code English default strings;
						// mirror the Chorus manager the product creates first at startup.
						LocalizationManagerWinforms.Create("en", "Chorus", "Chorus", "1.0.0",
							baseDir, "CommonLocalizations", null, new[] { "Chorus", "LibChorus" });
					}
				}
				catch (Exception)
				{
					// Best-effort: if localization setup fails the test will surface its own real
					// failure rather than this harness gap.
				}
				finally
				{
					s_initialized = true;
				}
			}
		}

		/// <summary>
		/// Replace the PropertyTable "HelpTopicProvider" (the product points it at the FwApp, whose
		/// inner provider is null under the test mock) with a null-returning stub so the legacy
		/// DataTree's Help command can be queried during menu materialization without an NRE.
		/// </summary>
		public static void EnsureHelpTopicProvider(PropertyTable propertyTable)
		{
			if (propertyTable == null)
				return;
			propertyTable.SetProperty("HelpTopicProvider", new TestNullHelpTopicProvider(), true);
			propertyTable.SetPropertyPersistence("HelpTopicProvider", false);
		}

		private static bool HasAnyManager()
		{
			try
			{
				// GetUILanguages(true) enumerates loaded managers' available languages; if any
				// manager exists this returns a non-empty set without throwing.
				return LocalizationManager.GetUILanguages(true) != null
					&& System.Linq.Enumerable.Any(LocalizationManager.GetUILanguages(true));
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
