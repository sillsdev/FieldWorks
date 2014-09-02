using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal static class ParatextLexiconPluginDirectoryFinder
	{
		private static readonly IFdoDirectories s_fdoDirs = new ParatextLexiconPluginFdoDirectories();

		static ParatextLexiconPluginDirectoryFinder()
		{
			RegistryHelper.CompanyName = DirectoryFinder.CompanyName;
			RegistryHelper.ProductName = ProductName;
		}

		private const string ProjectsDir = "ProjectsDir";
		private const string ProductName = "FieldWorks";
		private const string RootDataDir = "RootDataDir";
		private const string RootCodeDir = "RootCodeDir";
		private const string Projects = "Projects";
		private const string Templates = "Templates";
		private const string FdoVersion = "8";

		public static string ProjectsDirectory
		{
			get { return GetDirectory(ProjectsDir, Path.Combine(DataDirectory, Projects)); }
		}

		public static string ProjectsDirectoryLocalMachine
		{
			get { return GetDirectoryLocalMachine(ProjectsDir, Path.Combine(DataDirectoryLocalMachine, Projects)); }
		}

		public static string TemplateDirectory
		{
			get { return Path.Combine(CodeDirectory, Templates); }
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

		public static IFdoDirectories FdoDirectories
		{
			get { return s_fdoDirs; }
		}

		public static string DataDirectory
		{
			get { return GetDirectory(RootDataDir, DirectoryFinder.CommonAppDataFolder(ProductName)); }
		}

		public static string DataDirectoryLocalMachine
		{
			get { return GetDirectoryLocalMachine(RootDataDir, DirectoryFinder.CommonAppDataFolder(ProductName)); }
		}

		public static string CodeDirectory
		{
			get { return GetDirectory(RootCodeDir, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)); }
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Disposed in caller.")]
		private static RegistryKey FieldWorksRegistryKey
		{
			get { return RegistryHelper.SettingsKey(FdoVersion); }
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Disposed in caller.")]
		private static RegistryKey FieldWorksRegistryKeyLocalMachine
		{
			get { return RegistryHelper.SettingsKeyLocalMachine(FdoVersion); }
		}

		private static string GetDirectory(string registryValue, string defaultDir)
		{
			using (RegistryKey userKey = FieldWorksRegistryKey)
			using (RegistryKey machineKey = FieldWorksRegistryKeyLocalMachine)
			{
				var registryKey = userKey;
				if (userKey == null || userKey.GetValue(registryValue) == null)
				{
					registryKey = machineKey;
				}

				return GetDirectory(registryKey, registryValue, defaultDir);
			}
		}

		private static string GetDirectory(RegistryKey registryKey, string registryValue, string defaultDir)
		{
			string rootDir = (registryKey == null) ? null : registryKey.GetValue(registryValue, null) as string;

			if (string.IsNullOrEmpty(rootDir) && !string.IsNullOrEmpty(defaultDir))
				rootDir = defaultDir;
			if (string.IsNullOrEmpty(rootDir))
			{
				throw new ApplicationException();
			}
			// Hundreds of callers of this method are using Path.Combine with the results.
			// Combine only works with a root directory if it is followed by \ (e.g., c:\)
			// so we don't want to trim the \ in this situation.
			string dir = rootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return dir.Length > 2 ? dir : dir + Path.DirectorySeparatorChar;
		}

		private static string GetDirectoryLocalMachine(string registryValue, string defaultDir)
		{
			using (RegistryKey machineKey = FieldWorksRegistryKeyLocalMachine)
			{
				return GetDirectory(machineKey, registryValue, defaultDir);
			}
		}

		private class ParatextLexiconPluginFdoDirectories : IFdoDirectories
		{
			public string ProjectsDirectory
			{
				get { return ParatextLexiconPluginDirectoryFinder.ProjectsDirectory; }
			}
			public string TemplateDirectory
			{
				get { return ParatextLexiconPluginDirectoryFinder.TemplateDirectory; }
			}
		}
	}
}
