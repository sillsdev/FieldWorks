// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.XWorks.LexEd;

namespace LexEdDllTests
{
	[TestFixture]
	public class LexEntryChangeHandlerTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void FixupRemovesDanglingLexEntryRefs()
		{
			var a = TestUtils.MakeEntry(Cache, "a", "a");
			var b = TestUtils.MakeEntry(Cache, "b", "b");
			TestUtils.AddComplexFormComponents(Cache, a, new List<ICmObject> { b });
			TestUtils.AddComplexFormComponents(Cache, a, new List<ICmObject>());
			TestUtils.AddComplexFormComponents(Cache, a, new List<ICmObject>(),
				new List<ILexEntryType> { Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Cast<ILexEntryType>().First() });
			using (var changeHandler = new LexEntryChangeHandler())
			{
				changeHandler.Setup(a, null, Cache);
				changeHandler.Fixup(false); // SUT
			}
			var remainingRefs = a.EntryRefsOS;
			Assert.AreEqual(1, remainingRefs.Count, "Dangling References should have been removed");
			var referees = remainingRefs.First().ComponentLexemesRS;
			Assert.AreEqual(1, referees.Count, "The remaining LexEntryRef should have a Component");
			Assert.AreSame(b, referees.First(), "The remaining ref should still point to the same Component");
		}
	}
}
