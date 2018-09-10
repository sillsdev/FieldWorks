// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
#if DEBUG
using System.Reflection;
#endif
using Microsoft.Win32;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal partial class ParatextLexiconPluginRegistryHelper
	{

		static ParatextLexiconPluginRegistryHelper()
		{
			// copy the FW registry values to PT registry
			if (MiscUtils.IsUnix)
			{
				const string fwRegKey = "LocalMachine/software/sil";
#if DEBUG
				// On a developer Linux machine these are kept under output/registry. Since the program is running at output/{debug|release},
				// one level up should find the registry folder.
				string fwRegLoc = Path.Combine(Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase)) ?? ".",
					"../registry", fwRegKey);
				// On a developer Linux machine, PT appends the version number to its config directory, which contains the registry
				string paratextVersion = "7.6";
#else
				string fwRegLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".config/fieldworks/registry", fwRegKey);
				string paratextVersion = "";
#endif
				string ptRegLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
					string.Format(".config/paratext{0}/registry", paratextVersion), fwRegKey);

				if (Directory.Exists(fwRegLoc))
					DirectoryUtils.CopyDirectory(fwRegLoc, ptRegLoc, true, true);
			}
		}

		/// <summary>
		/// Is Fieldworks installed, based on querying the registry.
		/// </summary>
		public static bool IsFieldWorksInstalled
		{
			get
			{
				using (RegistryKey machineKey = FieldWorksRegistryKeyLocalMachine)
				{
					return machineKey != null;
				}
			}
		}

		public static RegistryKey FieldWorksRegistryKey
		{
			get { return RegistryHelper.SettingsKey(FWMajorVersion); }
		}

		public static RegistryKey FieldWorksRegistryKeyLocalMachine
		{
			get { return RegistryHelper.SettingsKeyLocalMachine(FWMajorVersion); }
		}
	}
}
