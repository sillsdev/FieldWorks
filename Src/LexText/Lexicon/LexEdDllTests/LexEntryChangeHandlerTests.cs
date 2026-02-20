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
			Assert.That(remainingRefs.Count, Is.EqualTo(2), "Dangling References should be removed");

			var referees = remainingRefs.First().ComponentLexemesRS;
			Assert.That(referees.Count, Is.EqualTo(1), "The remaining typeless LexEntryRef should have a Component");
			Assert.That(referees.First(), Is.SameAs(b), "The remaining typeless ref should still point to the same Component");
			var complexEntryTypes = remainingRefs.First().ComplexEntryTypesRS;
			Assert.That(complexEntryTypes.Count, Is.EqualTo(1), "The remaining typeless ref should have been given Unspecified Complex Form Type");
			Assert.That(complexEntryTypes.First(), Is.EqualTo(Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Cast<ILexEntryType>()
					.First(u => u.Guid == LexEntryTypeTags.kguidLexTypeUnspecifiedComplexForm)), "The remaining typeless ref should have been given Unspecified Complex Form Type");

			referees = remainingRefs.ElementAt(1).ComponentLexemesRS;
			Assert.That(referees.Count, Is.EqualTo(0), "The remaining componentless LexEntryRef should not have a Component");
			complexEntryTypes = remainingRefs.ElementAt(1).ComplexEntryTypesRS;
			Assert.That(complexEntryTypes.Count, Is.EqualTo(1), "The remaining componentless ref should still point to a Complex Entry Type");
			Assert.That(complexEntryTypes.First(), Is.EqualTo(t), "The remaining componentles ref should still point to the same Complex Entry Type");
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
			Assert.That(remainingRefs.Count, Is.EqualTo(0), "Dangling References should have been removed");
		}
	}
}
