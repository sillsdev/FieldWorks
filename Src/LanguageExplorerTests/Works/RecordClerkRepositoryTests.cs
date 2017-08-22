// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Works;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorerTests.Works
{
	[TestFixture]
	public class RecordClerkRepositoryTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void ClerkRepository_CompleteWorkout_IsHappyAsAClamInTheMud()
		{
			// Setup
			IPublisher publisher;
			ISubscriber subscriber;
			PubSubSystemFactory.CreatePubSubSystem(out publisher, out subscriber);
			using (var propertyTable = PropertyTableFactory.CreatePropertyTable(publisher))
			using (var dummyWindow = new DummyFwMainWnd())
			using (var statusbar = new StatusBar())
			using (IRecordClerkRepository recordClerkRepository = new RecordClerkRepository(Cache, new FlexComponentParameters(propertyTable, publisher, subscriber)))
			{
				propertyTable.SetProperty("cache", Cache, SettingsGroup.BestSettings, false, false);
				propertyTable.SetProperty("window", dummyWindow, SettingsGroup.BestSettings, false, false);

				// Test 1. Make sure a bogus clerk isn't in the repository.
				Assert.IsNull(recordClerkRepository.GetRecordClerk("bogusClerkId"));
				// Test 2. Make sure there is no active clerk.
				Assert.IsNull(recordClerkRepository.ActiveRecordClerk);

				// Test 3. New clerk is added.
				var recordList = new RecordList(Cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), false, Cache.MetaDataCacheAccessor.GetFieldId2(Cache.LanguageProject.ResearchNotebookOA.ClassID, "AllRecords", false), Cache.LanguageProject.ResearchNotebookOA, "AllRecords");
				var clerk = new RecordClerk("records", statusbar, recordList, new PropertyRecordSorter("ShortName"), "Default", null, false, false);
				clerk.InitializeFlexComponent(new FlexComponentParameters(propertyTable, publisher, subscriber));

				recordClerkRepository.AddRecordClerk(clerk);
				Assert.AreSame(clerk, recordClerkRepository.GetRecordClerk("records"));
				Assert.IsNull(recordClerkRepository.ActiveRecordClerk);

				// Test 4. Check out active clerk
				Assert.IsNull(recordClerkRepository.ActiveRecordClerk);
				recordClerkRepository.ActiveRecordClerk = clerk;
				Assert.AreSame(clerk, recordClerkRepository.ActiveRecordClerk);

				// Test 5. Remove clerk.
				recordClerkRepository.RemoveRecordClerk(clerk);
				Assert.IsNull(recordClerkRepository.ActiveRecordClerk);
			}
		}
	}
}