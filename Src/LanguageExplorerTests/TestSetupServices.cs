// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using LanguageExplorer.Impls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.IO;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests
{
	public static class TestSetupServices
	{
		public static void SetupTestPubSubSystem(out IPublisher publisher)
		{
			publisher = new Publisher();
		}
		public static void SetupTestPubSubSystem(out IPublisher publisher, out ISubscriber subscriber)
		{
			subscriber = new Subscriber();
			publisher = new Publisher(subscriber);
		}

		public static IPropertyTable SetupTestPropertyTable(IPublisher publisher)
		{
			return new PropertyTable(publisher);
		}

		internal static FlexComponentParameters SetupEverything(LcmCache cache, bool includeStylesheet = true)
		{
			SetupCache(cache);

			var subscriber = new Subscriber();
			var publisher = new Publisher(subscriber);
			var propertyTable = SetupTestPropertyTable(publisher);
			propertyTable.SetProperty("cache", cache, SettingsGroup.BestSettings, false, false);
			var flexComponentParameters = new FlexComponentParameters(propertyTable, publisher, subscriber);
			if (includeStylesheet)
			{
				var styleSheet = new LcmStyleSheet();
				styleSheet.Init(cache, cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
				flexComponentParameters.PropertyTable.SetProperty("FlexStyleSheet", styleSheet, SettingsGroup.BestSettings, false, false);
			}
			return flexComponentParameters;
		}

		internal static void SetupCache(LcmCache cache)
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
			DirectoryUtilities.CopyDirectoryContents(Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Works", "TestData"), baseDir);
		}
	}
}
