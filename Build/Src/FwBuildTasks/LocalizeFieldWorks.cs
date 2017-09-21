// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using SIL.FieldWorks.Build.Tasks.Localization;
using Task = Microsoft.Build.Utilities.Task;

namespace FwBuildTasks
{
	/// <summary>
	/// This class implements a complex process required to generate various resource DLLs for
	/// the localization of fieldworks. The main input of the process is a set of files kept
	/// under Localizations following the pattern messages.[locale].po, which contain
	/// translations of the many English strings in FieldWorks for the specified [locale]
	/// (e.g., fr, es).
	///
	/// We first apply some sanity checks, making sure String.Format markers like {0}, {1} etc
	/// have not been obviously mangled in translation.
	///
	/// The first stage of the process applies these substitutions to certain strings in
	/// DistFiles/Language Explorer/Configuration/strings-en.txt to produce
	/// a strings-[locale].txt for each locale (in the same place).
	///
	/// The second stage generates a resources.dll for each (non-test) project under Src, in
	/// Output/[config]/[locale]/[project].resources.dll. For example, in a release build
	/// (config), for the FDO project, for the French locale, it will generate
	/// Output/release/fr/FDO.resources.dll.
	///
	/// The second stage involves multiple steps to get to the resources DLL (done in parallel
	/// for each locale).
	/// - First we generate a lookup table (for an XSLT) which contains a transformation of the
	///   .PO file, Output/[locale].xml, e.g., Output/fr.xml.
	/// - For each non-test project,
	///     - for each .resx file in the project folder or a direct subfolder
	///         - apply an xslt, Build/LocalizeResx.xsl, to [path]/File.resx, producing
	///           Output[path]/[namespace].File.Strings.[locale].resx. (resx files are
	///           internally xml)
	///           (LocalizeResx.xsl includes the Output/fr.xml file created in the first step
	///           and uses it to translate appropriate items in the resx.)
	///           (Namespace is the fully qualified namespace of the project, which is made
	///           part of the filename.)
	///         - run an external program, resgen, which converts the resx to a .resources file
	///           in the same location, with otherwise the same name.
	///     - run an external program, al (assembly linker) which assembles all the .resources
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
		/// The directory in which it all happens, corresponding to the main ww directory in source control.
		/// </summary>
		[Required]
		public string RootDirectory { get; set; }

		/// <summary>
		/// What to build: Valid values are: SourceOnly, BinaryOnly, All
		/// </summary>
		public string Build { get; set; }

		/// <summary>
		/// The configuration we want to build (typically Release or Debug).
		/// </summary>
		public string Config { get; set; }

		internal static readonly string PoFileRelative = "Localizations"; // relative to root directory.

		internal string PoFileDirectory => Path.Combine(RootDirectory, PoFileRelative);

		internal static readonly string PoFileLeadIn = "messages.";
		internal static readonly string PoFileExtension = ".po";

		internal static readonly string DistFilesFolderName = "DistFiles";
		internal static readonly string LExFolderName = "Language Explorer";
		internal static readonly string ConfigFolderName = "Configuration";
		internal static readonly string OutputFolderName = "Output";
		internal static readonly string SrcFolderName = "Src";
		internal static readonly string BldFolderName = "Build";

		internal static readonly string AssemblyInfoName = "CommonAssemblyInfo.cs";

		internal string AssemblyInfoPath => Path.Combine(SrcFolder, AssemblyInfoName);

		internal string ConfigurationFolder => Path.Combine(Path.Combine(Path.Combine(RootDirectory, DistFilesFolderName), LExFolderName),
			ConfigFolderName);

		internal string OutputFolder => Path.Combine(RootDirectory, OutputFolderName);

		internal string SrcFolder => Path.Combine(RootDirectory, SrcFolderName);

		internal string RealBldFolder => Path.Combine(RealFwRoot, BldFolderName);

		internal bool BuildSource => Build != "BinaryOnly";

		internal bool BuildBinaries => Build != "SourceOnly";

		/// <summary>
		/// It happens to be the ConfigurationFolder, but let's keep things flexible. This one is the one where
		/// strings.en.xml (and the output strings.X.xml) live.
		/// </summary>
		internal string StringsXmlFolder => ConfigurationFolder;

		internal static readonly string EnglishLocale = "en";

		internal static readonly string StringsXmlPattern = "strings-{0}.xml";
		internal Localizer[] m_localizers;

		internal string StringsEnPath => StringsXmlPath(EnglishLocale);

		/// <summary>
		/// The path where wer expect to store a file like strings-es.xml for a given locale.
		/// </summary>
		/// <param name="locale"></param>
		/// <returns></returns>
		internal string StringsXmlPath(string locale)
		{
			return Path.Combine(StringsXmlFolder, string.Format(StringsXmlPattern, locale));
		}


		/// <summary>
		/// The path where we expect to store a temporary form of the .PO file for a particular locale,
		/// such as Output/es.xml.
		/// </summary>
		internal string XmlPoFilePath(string locale)
		{
			return Path.Combine(OutputFolder, Path.ChangeExtension(locale, ".xml"));
		}

		private Localizer CreateLocalizer(string currentFile, LocalizeFieldWorks parent)
		{
			var localizer = Activator.CreateInstance(LocalizerType) as Localizer;
			localizer.Initialize(currentFile, new LocalizerOptions(this));
			return localizer;
		}

		/// <summary>
		/// The main entry point invoked by a line in the build script.
		/// </summary>
		/// <returns></returns>
		public override bool Execute()
		{
#if DEBUG
			string test = RealFwRoot;
			Debug.WriteLine("RealFwRoot => '{0}'", test);	// keeps compiler from complaining.
#endif
			Log.LogMessage(MessageImportance.Low, "PoFileDirectory is set to {0}.", PoFileDirectory);

			// Get all the .po files paths:
			string[] poFiles = Directory.GetFiles(PoFileDirectory, PoFileLeadIn + "*" + PoFileExtension);

			Log.LogMessage(MessageImportance.Low, "{0} .po files found.", poFiles.Length);

			// Prepare to get responses from processing each .po file
			m_localizers = new Localizer[poFiles.Length];

			Log.LogMessage(MessageImportance.Normal, "Generating localization assemblies...");

			// Main loop for each language:
			Parallel.ForEach(poFiles, currentFile =>
			{
				// Process current .po file:
				var localizer = CreateLocalizer(currentFile, this);
				localizer.ProcessFile();

				// Slot current localizer into array at index matching current language.
				// This allows us to output any errors in a coherent manner.
				int index = Array.FindIndex(poFiles, poFile => poFile == currentFile);
				if (index != -1)
					m_localizers[index] = localizer;
			}
			);

			bool buildFailed = false;
			// Output all processing results to console:
			for (int i = 0; i < poFiles.Length; i++)
			{
				if (m_localizers[i] == null)
				{
					LogError("ERROR: localization of " + poFiles[i] + " was not done!");
					buildFailed = true;
				}
				else
				{
					foreach (string message in m_localizers[i].Errors)
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
		internal virtual void LogError(string message)
		{
			Log.LogError(message);
		}

		/// <summary>
		/// In normal operation, this is the same as RootDirectory. In test, we find the real one, to allow us to
		/// find fixed files like LocalizeResx.xml
		/// </summary>
		internal virtual string RealFwRoot => RootDirectory;

		// for testing only: get the project folders of the first Localizer
		internal List<string> GetProjectFolders()
		{
			List<string> result;
			m_localizers[0].GetProjectFolders(out result);
			return result;
		}
	}
}
