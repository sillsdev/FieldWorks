// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// This class implements a complex process required to generate various resource DLLs for
	/// the localization of fieldworks. The main input of the process is a set of files kept
	/// under Localizations/l10ns/[locale], which are translated versions of resx and other files
	/// in FieldWorks for the specified [locale] (e.g., fr, es, zh-CN).
	///
	/// We first apply some sanity checks, making sure String.Format markers like {0}, {1}, etc.
	/// have not been obviously mangled in translation. TODO (Hasso) 2019.11: test and reimplement these checks if necessary
	///
	/// The first stage of the process copies localized strings-[locale].xml files to
	/// DistFiles/Language Explorer/Configuration (the same place as strings-en.xml). TODO (Hasso) 2019.11: reimplement
	///
	/// The second stage generates a resources.dll for each (non-test) project under Src, in
	/// Output/[config]/[locale]/[project].resources.dll. For example, in a release build
	/// (config), for the FDO project, for the French locale, it will generate
	/// Output/release/fr/FDO.resources.dll.
	///
	/// The second stage involves multiple steps to get to the resources DLL (done in parallel
	/// for each locale).
	/// - For each non-test project,
	///     - for each .resx file in the project folder or a direct subfolder
	///         - copy the localized resx from
	///           Localizations/l10ns/[locale]/[path]/[filename].[locale].resx to
	///           Output[path]/[namespace].File.Strings.[locale].resx
	///           (Namespace is the fully qualified namespace of the project, which is made
	///           part of the filename.)
	///         - run an external program, resgen, which converts the resx to a .resources file
	///           in the same location, with otherwise the same name.
	///     - run an external program, al (assembly linker), which assembles all the .resources
	///       files into the final localized resources.dll
	/// </summary>
	public class LocalizeFieldWorks: Task
	{
		public LocalizeFieldWorks()
		{
			Config = "Release"; // a suitable default.
			Build = "All";
		}

		internal Type LocalizerType = typeof(Localizer);

		internal object SyncObj = new object();

		/// <summary>
		/// The directory in which it all happens, corresponding to the main fw directory in source control.
		/// </summary>
		[Required]
		public string RootDirectory { get; set; }

		[Required]
		public string SrcFolder { get; set; }

		/// <summary>
		/// What to build: Valid values are: SourceOnly, BinaryOnly, All
		/// </summary>
		public string Build { get; set; }

		/// <summary>
		/// The configuration we want to build (typically Release or Debug).
		/// </summary>
		public string Config { get; set; }

		[Required]
		public string OutputFolder { get; set; }


		[Required]
		public string L10nFileDirectory { get; set; }

		internal const string DistFilesFolderName = "DistFiles";
		internal const string LExFolderName = "Language Explorer";
		internal const string ConfigFolderName = "Configuration";

		internal static readonly string AssemblyInfoName = "CommonAssemblyInfo.cs";

		internal string AssemblyInfoPath => Path.Combine(SrcFolder, AssemblyInfoName);

		internal string ConfigurationFolder => Path.Combine(RootDirectory, DistFilesFolderName, LExFolderName, ConfigFolderName);

		internal bool BuildSource => Build != "BinaryOnly";

		internal bool BuildBinaries => Build != "SourceOnly";

		/// <summary>
		/// It happens to be the ConfigurationFolder, but let's keep things flexible. This one is the one where
		/// strings.en.xml (and the output strings.X.xml) live.
		/// </summary>
		internal string StringsXmlFolder => ConfigurationFolder;

		internal const string EnglishLocale = "en";

		internal const string StringsXmlPattern = "strings-{0}.xml";
		internal Localizer[] m_localizers;

		internal string StringsEnPath => StringsXmlPath(EnglishLocale);

		/// <summary>
		/// The destination path for localized strings-[locale].xml files (DistFiles\Language Explorer\Configuration\strings-[locale].xml)
		/// </summary>
		internal string StringsXmlPath(string locale)
		{
			return Path.Combine(StringsXmlFolder, string.Format(StringsXmlPattern, locale));
		}

		/// <summary>
		/// The source path for localized strings-[locale].xml files (Localizations\l10ns\[locale]\strings-[locale].xml)
		/// </summary>
		internal string StringsXmlSourcePath(string locale)
		{
			return Path.Combine(L10nFileDirectory, locale, string.Format(StringsXmlPattern, locale));
		}

		private Localizer CreateLocalizer(string currentDir)
		{
			var localizer = (Localizer) Activator.CreateInstance(LocalizerType);
			localizer.Initialize(currentDir, new LocalizerOptions(this));
			return localizer;
		}

		/// <summary>
		/// The main entry point invoked by a line in the build script.
		/// </summary>
		public override bool Execute()
		{
#if DEBUG
			string test = RealFwRoot;
			Debug.WriteLine("RealFwRoot => '{0}'", test);   // keeps compiler from complaining.
#endif
			Log.LogMessage(MessageImportance.Low, "L10nFileDirectory is set to {0}.", L10nFileDirectory);

			// Get all the directories containing localized .resx files:
			string[] l10nDirs = Directory.GetDirectories(L10nFileDirectory);

			Log.LogMessage(MessageImportance.Low, "{0} l10n dirs found.", l10nDirs.Length);

			// Prepare to get responses from processing each locale
			m_localizers = new Localizer[l10nDirs.Length];

			Log.LogMessage(MessageImportance.Normal, "Generating localization assemblies...");

			// Main loop for each language:
			Parallel.ForEach(l10nDirs, currentDir =>
			{
				// Process current l10n dir:
				var localizer = CreateLocalizer(currentDir);
				localizer.ProcessFile();

				// Slot current localizer into array at index matching current language.
				// This allows us to output any errors in a coherent manner.
				var index = Array.FindIndex(l10nDirs, dir => dir == currentDir);
				if (index != -1)
					m_localizers[index] = localizer;
			});

			var buildFailed = false;
			// Output all processing results to console:
			for (var i = 0; i < l10nDirs.Length; i++)
			{
				if (m_localizers[i] == null)
				{
					LogError($"ERROR: localization of {l10nDirs[i]} was not done!");
					buildFailed = true;
				}
				else if (m_localizers[i].Errors.Count > 0)
				{
					LogError($"Got Errors localizing {l10nDirs[i]}:");
					foreach (var message in m_localizers[i].Errors)
					{
						LogError(message);
						buildFailed = true;  // an error was reported, e.g., from Assembly Linker, that we didn't manage to make cause a return false.
					}
				}
			}

			// Decide if we succeeded or not:
			if (buildFailed)
				LogError("STOPPING BUILD - at least one localization build failed.");
			else
				Log.LogMessage(MessageImportance.Normal, "Finished generating localization assemblies.");

			return !buildFailed;
		}

		// overridden in tests to trap errors.
		protected virtual void LogError(string message)
		{
			Log.LogError(message);
		}

		/// <summary>
		/// In normal operation, this is the same as RootDirectory. In test, we find the real one, to allow us to
		/// find fixed files like LocalizeResx.xml
		/// </summary>
		protected virtual string RealFwRoot => RootDirectory;

		/// <remarks>for testing only: get the project folders of the first Localizer</remarks>
		internal List<string> GetProjectFolders()
		{
			m_localizers[0].GetProjectFolders(out var result);
			return result;
		}
	}
}
