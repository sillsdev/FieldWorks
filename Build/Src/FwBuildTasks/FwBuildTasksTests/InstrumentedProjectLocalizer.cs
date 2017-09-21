// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Build.Tasks.Localization;

namespace FwBuildTasks
{
	internal class InstrumentedProjectLocalizer: ProjectLocalizer
	{
		public InstrumentedProjectLocalizer(string projectFolder, ProjectLocalizerOptions options):
			base(projectFolder, options)
		{
		}

		public static List<string> LinkerPath = new List<string>();
		public static List<string> LinkerCulture = new List<string>();
		public static List<string> LinkerFileVersion = new List<string>();
		public static List<string> LinkerProductVersion = new List<string>();
		public static List<string> LinkerVersion = new List<string>();
		public static List<List<EmbedInfo>> LinkerResources = new List<List<EmbedInfo>>();
		public static List<string> LinkerAlArgs = new List<string>();
		public static List<string> ResGenOutputPaths = new List<string>();
		public static List<string> ResGenResxPaths = new List<string>();
		public static List<string> ResGenOriginalFolders = new List<string>();

		public static void Reset()
		{
			LinkerPath = new List<string>();
			LinkerCulture = new List<string>();
			LinkerFileVersion = new List<string>();
			LinkerProductVersion = new List<string>();
			LinkerVersion = new List<string>();
			LinkerResources = new List<List<EmbedInfo>>();
			LinkerAlArgs = new List<string>();
			ResGenOutputPaths = new List<string>();
			ResGenResxPaths = new List<string>();
			ResGenOriginalFolders = new List<string>();
		}

		protected override void RunAssemblyLinker(string outputDllPath, string culture,
			string fileversion, string productVersion, string version, List<EmbedInfo> resources)
		{
			LinkerPath.Add(outputDllPath);
			LinkerCulture.Add(culture);
			LinkerFileVersion.Add(fileversion);
			LinkerProductVersion.Add(productVersion);
			LinkerVersion.Add(version);
			LinkerResources.Add(resources);
			LinkerAlArgs.Add(BuildLinkerArgs(outputDllPath, culture, fileversion, productVersion, version, resources));
		}

		protected override void RunResGen(string outputResourcePath, string resxPath, string originalFolder)
		{
			ResGenOutputPaths.Add(outputResourcePath);
			ResGenResxPaths.Add(resxPath);
			ResGenOriginalFolders.Add(originalFolder);
		}
	}
}
