// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary />
	[TestFixture]
	public class LcmExtensionsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary />
		[Test]
		public void CmPossibilityCloneTest()
		{
			var factory = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			var testList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned("TestList", Cache.DefaultUserWs);
			var confidenceLevels = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned("ConfidenceLevels", Cache.DefaultUserWs);
			var originalParentPossibility = factory.Create(Guid.NewGuid(), testList);
			originalParentPossibility.Name.set_String(Cache.DefaultUserWs, "testing");
			var conf = factory.Create(Guid.NewGuid(), confidenceLevels);
			conf.Name.set_String(Cache.DefaultUserWs, "confidence");
			originalParentPossibility.ConfidenceRA = conf;
			var originalChildPossibility = factory.Create(Guid.NewGuid(), originalParentPossibility);
			originalChildPossibility.Name.set_String(Cache.DefaultUserWs, "testing child");
			var originalGrandchildPossibility = factory.Create(Guid.NewGuid(), originalChildPossibility);
			originalGrandchildPossibility.Name.set_String(Cache.DefaultUserWs, "testing grandchild");
			originalGrandchildPossibility.Description.set_String(Cache.DefaultUserWs, "young");

			//SUT
			Assert.AreEqual(testList.PossibilitiesOS.Count, 1); // Make sure list only has one possibility at the start.
			var clone = originalParentPossibility.Clone();
			Assert.AreEqual(testList.PossibilitiesOS.Count, 2); //Make sure item was duplicated once and its sub-items were added as sub-items of the duplicate and not siblings of the original
			Assert.AreSame(clone, testList.PossibilitiesOS[1]);
			Assert.AreEqual("testing (Copy) (1)", clone.Name.UiString);
			Assert.AreEqual("confidence", clone.ConfidenceRA.Name.UiString);
			Assert.AreEqual(1, clone.SubPossibilitiesOS.Count);
			var cloneChild = clone.SubPossibilitiesOS[0];
			Assert.AreEqual("testing child", cloneChild.Name.UiString);
			Assert.AreEqual(1, cloneChild.SubPossibilitiesOS.Count);
			var cloneGrandchild = cloneChild.SubPossibilitiesOS[0];
			Assert.AreEqual("testing grandchild", cloneGrandchild.Name.UiString);
			Assert.AreEqual("young", cloneGrandchild.Description.UiString);
		}
	}
}