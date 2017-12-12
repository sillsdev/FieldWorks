// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer;
using LanguageExplorer.Impls;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorerTests.Impls
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
			TestSetupServices.SetupTestPubSubSystem(out publisher, out subscriber);
			using (var propertyTable = TestSetupServices.SetupTestPropertyTable(publisher))
			using (var dummyWindow = new DummyFwMainWnd())
			using (var statusbar = new StatusBar())
			using (IRecordListRepository recordListRepository = new RecordListRepository(Cache, new FlexComponentParameters(propertyTable, publisher, subscriber)))
			{
				propertyTable.SetProperty("cache", Cache, SettingsGroup.BestSettings, false, false);
				propertyTable.SetProperty("window", dummyWindow, SettingsGroup.BestSettings, false, false);

				// Test 1. Make sure a bogus clerk isn't in the repository.
				Assert.IsNull(recordListRepository.GetRecordList("bogusClerkId"));
				// Test 2. Make sure there is no active clerk.
				Assert.IsNull(recordListRepository.ActiveRecordList);

				// Test 3. New clerk is added.
				var recordList = new RecordList("records", statusbar, Cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), false, new VectorPropertyParameterObject(Cache.LanguageProject.ResearchNotebookOA, "AllRecords", Cache.MetaDataCacheAccessor.GetFieldId2(RnResearchNbkTags.kClassId, "AllRecords", false)));
				recordList.InitializeFlexComponent(new FlexComponentParameters(propertyTable, publisher, subscriber));

				recordListRepository.AddRecordList(recordList);
				Assert.AreSame(recordList, recordListRepository.GetRecordList("records"));
				Assert.IsNull(recordListRepository.ActiveRecordList);

				// Test 4. Check out active clerk
				Assert.IsNull(recordListRepository.ActiveRecordList);
				recordListRepository.ActiveRecordList = recordList;
				Assert.AreSame(recordList, recordListRepository.ActiveRecordList);

				// Test 5. Remove clerk.
				recordListRepository.RemoveRecordList(recordList);
				Assert.IsNull(recordListRepository.ActiveRecordList);
			}
		}
	}
}