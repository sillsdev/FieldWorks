// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Reflection;

namespace SIL.FieldWorks.FDO.FDOTests
{
	internal static class TestDirectoryFinder
	{
		public static string CodeDirectory
		{
			get
			{
				Uri uriBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
				return Path.GetDirectoryName(Uri.UnescapeDataString(uriBase.AbsolutePath));
			}
		}

		private static string RootDirectory
		{
			get
			{
				// we'll assume the executing assembly is <root>/artifacts/Debug/FDOTests.dll,
				string dir = CodeDirectory;
				dir = Path.GetDirectoryName(dir);       // strip the parent directory name (Debug)
				dir = Path.GetDirectoryName(dir);       // strip the parent directory again (artifacts)
				return dir;
			}
		}

		public static string ProjectsDirectory => Path.Combine(CodeDirectory, "Projects");

		public static string TestDataDirectory => Path.Combine(RootDirectory, "tests", "FDOTests", "TestData");

		public static string TemplateDirectory => Path.Combine(CodeDirectory, "Templates");

		public static IFdoDirectories FdoDirectories { get; } = new TestFdoDirectories(ProjectsDirectory);
	}
}
