// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using SIL.Utils;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class ParatextLexiconPluginRegistryHelper
	{
		private const string FdoVersion = "8";

		static ParatextLexiconPluginRegistryHelper()
		{
			// copy the FW registry values to PT registry
			if (MiscUtils.IsUnix)
			{
				const string fwRegKey = "LocalMachine/software/sil";

				string ptRegLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".config/paratext7.6/registry", fwRegKey);

#if DEBUG
				// On a developer Linux machine these are kept under output/registry. Since the program is running at output/{debug|release},
				// one level up should find the registry folder.
				string fwRegLoc = Path.Combine(Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase)) ?? ".",
					"../registry", fwRegKey);
#else
				string fwRegLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".config/fieldworks/registry", fwRegKey);
#endif

				if (Directory.Exists(fwRegLoc))
					DirectoryUtils.CopyDirectory(fwRegLoc, ptRegLoc, true, true);
			}
		}

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

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Disposed in caller.")]
		public static RegistryKey FieldWorksRegistryKey
		{
			get { return RegistryHelper.SettingsKey(FdoVersion); }
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Disposed in caller.")]
		public static RegistryKey FieldWorksRegistryKeyLocalMachine
		{
			get { return RegistryHelper.SettingsKeyLocalMachine(FdoVersion); }
		}
	}
}
