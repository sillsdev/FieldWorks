// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Areas;
using NUnit.Framework;
using SIL.LCModel;

namespace LanguageExplorerTests.Areas
{
	[TestFixture]
	class TreeBarHandlerUtilsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void DuplicateTest()
		{
			var factory = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			var testList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned("TestList", Cache.DefaultUserWs);
			var confidenceLevels = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned("ConfidenceLevels", Cache.DefaultUserWs);
			var testListItem = factory.Create(Guid.NewGuid(), testList);
			testListItem.Name.set_String(Cache.DefaultUserWs, "testing");
			ICmPossibility conf = factory.Create(Guid.NewGuid(), confidenceLevels);
			conf.Name.set_String(Cache.DefaultUserWs, "confidence");
			testListItem.ConfidenceRA = conf;

			factory.Create(Guid.NewGuid(), testListItem);
			testListItem.SubPossibilitiesOS[0].Name.set_String(Cache.DefaultUserWs, "testing child");

			factory.Create(Guid.NewGuid(), testListItem.SubPossibilitiesOS[0]);
			testListItem.SubPossibilitiesOS[0].SubPossibilitiesOS[0].Name.set_String(Cache.DefaultUserWs, "testing grandchild");
			testListItem.SubPossibilitiesOS[0].SubPossibilitiesOS[0].Description.set_String(Cache.DefaultUserWs, "young");

			//SUT
			TreeBarHandlerUtils.Tree_Duplicate(testListItem, 0, Cache);
			Assert.AreEqual(testList.PossibilitiesOS[1].Name.UiString, "testing (Copy) (1)");
			Assert.AreEqual(testList.PossibilitiesOS[1].ConfidenceRA.Name.UiString, "confidence");
			Assert.AreEqual(testList.PossibilitiesOS.Count, 2); //Make sure item was duplicated once and its subitems were added as subitems and not siblings
			Assert.AreEqual(testList.PossibilitiesOS[1].SubPossibilitiesOS[0].Name.UiString, "testing child");
			Assert.AreEqual(testList.PossibilitiesOS[1].SubPossibilitiesOS.Count, 1);
			Assert.AreEqual(testList.PossibilitiesOS[1].SubPossibilitiesOS[0].SubPossibilitiesOS[0].Name.UiString, "testing grandchild");
			Assert.AreEqual(testList.PossibilitiesOS[1].SubPossibilitiesOS[0].SubPossibilitiesOS.Count, 1);
			Assert.AreEqual(testList.PossibilitiesOS[1].SubPossibilitiesOS[0].SubPossibilitiesOS[0].Description.UiString, "young");

		}
	}
}
