// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using LanguageExplorer.Impls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.IO;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.TestUtilities
{
	public static class TestSetupServices
	{
		public static FlexComponentParameters SetupTestTriumvirate()
		{
			var subscriber = new Subscriber();
			var publisher = new Publisher(subscriber);
			return new FlexComponentParameters(new PropertyTable(publisher), publisher, subscriber);
		}

		internal static FlexComponentParameters SetupEverything(LcmCache cache, bool includeStylesheet = true)
		{
			Guard.AgainstNull(cache, nameof(cache));

			var baseDir = Path.Combine(Path.GetTempPath(), cache.ProjectId.Name);
			if (!Directory.Exists(baseDir))
			{
				Directory.CreateDirectory(baseDir);
			}
			cache.ProjectId.Path = Path.Combine(baseDir, cache.ProjectId.Name + @".junk");
			foreach (var gonerDir in new HashSet<string> { "AudioFiles", "ConfigurationSettings" })
			{
				if (Directory.Exists(gonerDir))
				{
					Directory.Delete(gonerDir, true);
				}
			}
			DirectoryUtilities.CopyDirectoryContents(Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorer.TestUtilities", "DictionaryConfiguration", "TestData"), baseDir);
			var flexComponentParameters = SetupTestTriumvirate();
			flexComponentParameters.PropertyTable.SetProperty("cache", cache);
			if (includeStylesheet)
			{
				var styleSheet = new LcmStyleSheet();
				styleSheet.Init(cache, cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
				flexComponentParameters.PropertyTable.SetProperty(FwUtils.FlexStyleSheet, styleSheet);
			}
			return flexComponentParameters;
		}

		public static void DisposeTrash(FlexComponentParameters flexComponentParameters)
		{
			flexComponentParameters?.PropertyTable.Dispose();
		}
	}
}