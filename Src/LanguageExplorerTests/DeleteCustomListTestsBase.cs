// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests
{
	/// <summary>
	/// Some methods and fields refactored out so they can be shared with other unit tests.
	/// </summary>
	public class DeleteCustomListTestsBase : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Member Variables

		protected ICmPossibilityListRepository m_listRepo;
		protected ICmPossibilityFactory m_possFact;
		protected ICmPossibilityList m_testList;
		internal DeleteListHelper m_helper;
		protected int m_userWs;

		#endregion

		protected override void CreateTestData()
		{
			base.CreateTestData();
			var servLoc = Cache.ServiceLocator;
			m_listRepo = servLoc.GetInstance<ICmPossibilityListRepository>();
			m_possFact = servLoc.GetInstance<ICmPossibilityFactory>();
			m_userWs = Cache.DefaultUserWs;
			CreateCustomList();
			m_helper = new DeleteListHelper(Cache);
		}

		protected void CreateCustomList()
		{
			const string name = "Test Custom List";
			const string description = "Test Custom list description";
			var listName = TsStringUtils.MakeString(name, m_userWs);
			var listDesc = TsStringUtils.MakeString(description, m_userWs);
			m_testList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(listName.Text, m_userWs);
			m_testList.Name.set_String(m_userWs, listName);
			m_testList.Description.set_String(m_userWs, listDesc);
			// Set various properties of CmPossibilityList
			m_testList.DisplayOption = (int)PossNameType.kpntNameAndAbbrev;
			m_testList.PreventDuplicates = true;
			m_testList.IsSorted = true;
			m_testList.WsSelector = WritingSystemServices.kwsAnals;
			m_testList.IsVernacular = false;
			m_testList.Depth = 127;
		}

		protected ICmCustomItem CreateCustomItemAddToList(ICmPossibilityList owningList, string itemName)
		{
			var item = Cache.ServiceLocator.GetInstance<ICmCustomItemFactory>().Create();
			owningList.PossibilitiesOS.Add(item);
			item.Name.set_String(m_userWs, TsStringUtils.MakeString(itemName, m_userWs));
			return item;
		}
	}
}