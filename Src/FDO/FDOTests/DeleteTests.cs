// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests to do with deleting objects.
	/// </summary>
	[TestFixture]
	public class DeleteTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Tests that incoming references to something deleted get cleared.
		/// </summary>
		[Test]
		public void ClearIncomingRefs()
		{
			var servLoc = Cache.ServiceLocator;

			var leFact = servLoc.GetInstance<ILexEntryFactory>();
			var le1 = leFact.Create();

			var lsFact = servLoc.GetInstance<ILexSenseFactory>();
			var ls1 = lsFact.Create();
			le1.SensesOS.Add(ls1);
			var ls2 = lsFact.Create();
			le1.SensesOS.Add(ls2);

			if (Cache.LangProject.StatusOA == null)
			{
				Cache.LangProject.StatusOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
			}
			var list = Cache.LangProject.StatusOA;
			var possFact = servLoc.GetInstance<ICmPossibilityFactory>();
			var status1 = possFact.Create();
			list.PossibilitiesOS.Add(status1);
			var status2 = possFact.Create();
			list.PossibilitiesOS.Add(status2);

			ls1.StatusRA = status1;
			ls1.SenseTypeRA = status1; // pathological, but we want two atomic refs from same source to same dest.
			ls2.StatusRA = status1;

			// Test 1: delete target with multiple incoming atomic refs
			list.PossibilitiesOS.Remove(status1);

			Assert.IsNull(ls1.StatusRA);
			Assert.IsNull(ls2.StatusRA);
			Assert.IsNull(ls1.SenseTypeRA);

			le1.MainEntriesOrSensesRS.Add(ls1);
			le1.MainEntriesOrSensesRS.Add(ls2);
			le1.MainEntriesOrSensesRS.Add(ls1);
			var ls3 = lsFact.Create();
			ls1.SensesOS.Add(ls3);
			le1.MainEntriesOrSensesRS.Add(ls3);

			// Test 2: remove two objects, references to both of them should be cleared.
			le1.SensesOS.Remove(ls1);
			Assert.AreEqual(1, le1.MainEntriesOrSensesRS.Count); // removed ls1 twice, also ls3.
			Assert.AreEqual(ls2, le1.MainEntriesOrSensesRS[0]);

			ls2.DomainTypesRC.Add(status2);

			// Test3: clear ref from ref collection
			list.PossibilitiesOS.Remove(status2);

			Assert.AreEqual(0, ls2.DomainTypesRC.Count);

		}

	}
}
