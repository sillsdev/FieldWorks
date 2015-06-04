using System;
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

		private const string ProjectsDir = "ProjectsDir";
		private const string RootDataDir = "RootDataDir";
		private const string RootCodeDir = "RootCodeDir";
		private const string Projects = "Projects";
		private const string Templates = "Templates";
		private const string FieldWorksDir = "FieldWorks";

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

		public static IFdoDirectories FdoDirectories
		{
			get { return s_fdoDirs; }
		}

		public static string DataDirectory
		{
			get { return GetDirectory(RootDataDir, DirectoryFinder.CommonAppDataFolder(FieldWorksDir)); }
		}

		public static string DataDirectoryLocalMachine
		{
			get { return GetDirectoryLocalMachine(RootDataDir, DirectoryFinder.CommonAppDataFolder(FieldWorksDir)); }
		}

		public static string CodeDirectory
		{
			get { return GetDirectory(RootCodeDir, Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase))); }
		}

		private static string GetDirectory(string registryValue, string defaultDir)
		{
			using (RegistryKey userKey = ParatextLexiconPluginRegistryHelper.FieldWorksRegistryKey)
			using (RegistryKey machineKey = ParatextLexiconPluginRegistryHelper.FieldWorksRegistryKeyLocalMachine)
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
			using (RegistryKey machineKey = ParatextLexiconPluginRegistryHelper.FieldWorksRegistryKeyLocalMachine)
			{
				return GetDirectory(machineKey, registryValue, defaultDir);
			}
		}

		private class ParatextLexiconPluginFdoDirectories : IFdoDirectories
		{
			string IFdoDirectories.ProjectsDirectory
			{
				get { return ProjectsDirectory; }
			}

			string IFdoDirectories.DefaultProjectsDirectory
			{
				get { return ProjectsDirectoryLocalMachine; }
			}

			string IFdoDirectories.TemplateDirectory
			{
				get { return TemplateDirectory; }
			}
		}
	}
}
