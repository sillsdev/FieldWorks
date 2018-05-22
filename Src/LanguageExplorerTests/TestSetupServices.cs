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
		public static void SetupTestPubSubSystem(out IPublisher publisher, out ISubscriber subscriber)
		{
			subscriber = new Subscriber();
			publisher = new Publisher(subscriber);
		}

		public static IPropertyTable SetupTestPropertyTable()
		{
			ISubscriber subscriber;
			IPublisher publisher;
			return SetupTestTriumvirate(out publisher, out subscriber);
		}

		public static IPropertyTable SetupTestTriumvirate(out IPublisher publisher, out ISubscriber subscriber)
		{
			SetupTestPubSubSystem(out publisher, out subscriber);
			return new PropertyTable(publisher);
		}

		internal static FlexComponentParameters SetupEverything(LcmCache cache, bool includeStylesheet = true)
		{
			SetupCache(cache);

			ISubscriber subscriber;
			IPublisher publisher;
			var propertyTable = SetupTestTriumvirate(out publisher, out subscriber);
			propertyTable.SetProperty("cache", cache);
			var flexComponentParameters = new FlexComponentParameters(propertyTable, publisher, subscriber);
			if (includeStylesheet)
			{
				var styleSheet = new LcmStyleSheet();
				styleSheet.Init(cache, cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
				flexComponentParameters.PropertyTable.SetProperty("FlexStyleSheet", styleSheet);
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
			DirectoryUtilities.CopyDirectoryContents(Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "DictionaryConfiguration", "TestData"), baseDir);
		}
	}
}