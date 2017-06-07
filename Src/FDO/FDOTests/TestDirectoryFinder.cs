using System;
using System.IO;
using System.Reflection;

namespace SIL.FieldWorks.FDO.FDOTests
{
	internal static class TestDirectoryFinder
	{
		private static string RootDirectory
		{
			get
			{
				// we'll assume the executing assembly is $FW/Output/Debug/FDOTests.dll,
				Uri uriBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
				string dir = Path.GetDirectoryName(Uri.UnescapeDataString(uriBase.AbsolutePath));
				dir = Path.GetDirectoryName(dir);       // strip the parent directory name (Debug)
				return Path.GetDirectoryName(dir);       // strip the parent directory again (Output)
			}
		}

		public static string SourceDirectory => Path.Combine(RootDirectory, "Src");

		public static string CodeDirectory => Path.Combine(RootDirectory, "DistFiles");

		public static string DataDirectory => CodeDirectory;

		public static string ProjectsDirectory => Path.Combine(CodeDirectory, "Projects");

		public static string TemplateDirectory => Path.Combine(CodeDirectory, "Templates");

		public static IFdoDirectories FdoDirectories { get; } = new TestdoDirectories();

		private class TestdoDirectories : IFdoDirectories
		{
			string IFdoDirectories.ProjectsDirectory => ProjectsDirectory;
			string IFdoDirectories.DefaultProjectsDirectory => ProjectsDirectory;
			string IFdoDirectories.TemplateDirectory => TemplateDirectory;
		}
	}
}
