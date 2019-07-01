// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer;
using LanguageExplorer.Impls;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorerTests.Impls
{
	[TestFixture]
	public class RecordListRepositoryTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void RecordListRepository_CompleteWorkout_IsHappyAsAClamInTheMud()
		{
			// Setup
			var flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			try
			{
				using (var dummyWindow = new DummyFwMainWnd())
				using (var statusBar = new StatusBar())
				using (IRecordListRepository recordListRepository = new RecordListRepository(Cache, flexComponentParameters))
				{
					flexComponentParameters.PropertyTable.SetProperty(LanguageExplorerConstants.RecordListRepository, recordListRepository, settingsGroup: SettingsGroup.GlobalSettings);
					flexComponentParameters.PropertyTable.SetProperty(FwUtils.cache, Cache);
					flexComponentParameters.PropertyTable.SetProperty(FwUtils.window, dummyWindow);

					// Test 1. Make sure a bogus record list isn't in the repository.
					Assert.IsNull(recordListRepository.GetRecordList("bogusRecordListId"));
					// Test 2. Make sure there is no active clerk.
					Assert.IsNull(recordListRepository.ActiveRecordList);

					// Test 3. New record list is added.
					var recordList = new RecordList("records", statusBar, Cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), false, new VectorPropertyParameterObject(Cache.LanguageProject.ResearchNotebookOA, "AllRecords", Cache.MetaDataCacheAccessor.GetFieldId2(RnResearchNbkTags.kClassId, "AllRecords", false)));
					recordList.InitializeFlexComponent(flexComponentParameters);

					recordListRepository.AddRecordList(recordList);
					Assert.AreSame(recordList, recordListRepository.GetRecordList("records"));
					Assert.IsNull(recordListRepository.ActiveRecordList);

					// Test 4. Check out active record list
					Assert.IsNull(recordListRepository.ActiveRecordList);
					recordListRepository.ActiveRecordList = recordList;
					Assert.AreSame(recordList, recordListRepository.ActiveRecordList);

					// Test 5. Remove record list.
					recordListRepository.RemoveRecordList(recordList);
					Assert.IsNull(recordListRepository.ActiveRecordList);
				}
			}
			finally
			{
				flexComponentParameters.PropertyTable.Dispose();
			}
		}
	}
}