// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.LCModel;
using SIL.FieldWorks.XWorks.LexEd;

namespace LexEdDllTests
{
	[TestFixture]
	public class LexEntryChangeHandlerTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void FixupKeepDanglingLexEntryRefsWhenComplexEntryTypeExists()
		{
			var a = TestUtils.MakeEntry(Cache, "a", "a");
			var b = TestUtils.MakeEntry(Cache, "b", "b");
			var t = Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Cast<ILexEntryType>().ElementAt(2);
			TestUtils.AddComplexFormComponents(Cache, a, new List<ICmObject> { b });
			TestUtils.AddComplexFormComponents(Cache, a, new List<ICmObject>());
			TestUtils.AddComplexFormComponents(Cache, a, new List<ICmObject>(), new List<ILexEntryType> { t });
			using (var changeHandler = new LexEntryChangeHandler())
			{
				changeHandler.Setup(a, null, Cache);
				changeHandler.Fixup(false); // SUT
			}
			var remainingRefs = a.EntryRefsOS;
			Assert.AreEqual(2, remainingRefs.Count, "Dangling References should be removed");

			var referees = remainingRefs.First().ComponentLexemesRS;
			Assert.AreEqual(1, referees.Count, "The remaining typeless LexEntryRef should have a Component");
			Assert.AreSame(b, referees.First(), "The remaining typeless ref should still point to the same Component");
			var complexEntryTypes = remainingRefs.First().ComplexEntryTypesRS;
			Assert.AreEqual(1, complexEntryTypes.Count, "The remaining typeless ref should have been given Unspecified Complex Form Type");
			Assert.AreEqual(Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Cast<ILexEntryType>()
					.First(u => u.Guid == LexEntryTypeTags.kguidLexTypeUnspecifiedComplexForm),
				complexEntryTypes.First(), "The remaining typeless ref should have been given Unspecified Complex Form Type");

			referees = remainingRefs.ElementAt(1).ComponentLexemesRS;
			Assert.AreEqual(0, referees.Count, "The remaining componentless LexEntryRef should not have a Component");
			complexEntryTypes = remainingRefs.ElementAt(1).ComplexEntryTypesRS;
			Assert.AreEqual(1, complexEntryTypes.Count, "The remaining componentless ref should still point to a Complex Entry Type");
			Assert.AreEqual(t, complexEntryTypes.First(), "The remaining componentles ref should still point to the same Complex Entry Type");
		}

		[Test]
		public void FixupRemovesDanglingLexEntryRefs()
		{
			var a = TestUtils.MakeEntry(Cache, "a", "a");
			TestUtils.AddComplexFormComponents(Cache, a, new List<ICmObject>());
			using (var changeHandler = new LexEntryChangeHandler())
			{
				changeHandler.Setup(a, null, Cache);
				changeHandler.Fixup(false); // SUT
			}
			var remainingRefs = a.EntryRefsOS;
			Assert.AreEqual(0, remainingRefs.Count, "Dangling References should have been removed");
		}
	}
}
