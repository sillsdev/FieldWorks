// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FDO.FDOTests
{
	internal class TestFdoDirectories : IFdoDirectories
	{
		public TestFdoDirectories(string projectsDirectory)
		{
			ProjectsDirectory = projectsDirectory;
		}

		public string ProjectsDirectory { get; }

		public string DefaultProjectsDirectory => ProjectsDirectory;

		public string TemplateDirectory => TestDirectoryFinder.TemplateDirectory;
	}
}