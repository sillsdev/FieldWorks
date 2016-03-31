// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using System.Collections.Generic;
using LanguageExplorer.Areas.Lexicon;

namespace LanguageExplorerTests.Lexicon
{
	public class CircularRefBreakerTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void BreakCircularEntryRefs()
		{
			var a = MakeEntry("a", "a");
			var b = MakeEntry("b", "b");
			var c = MakeEntry("c", "c");
			var d = MakeEntry("d", "d");
			var ab = MakeEntry("ab", "ab");
			var ac = MakeEntry("ac", "ac");
			var abcd = MakeEntry("abcd", "abcd");
			// Create some reasonable component references
			AddComplexFormComponents(ab, new List<ICmObject> {a, b});
			AddComplexFormComponents(ac, new List<ICmObject> {a.SensesOS[0], c.SensesOS[0]});
			AddComplexFormComponents(abcd, new List<ICmObject> {a, b, c, d.SensesOS[0]});
			// Create circular component references
			AddComplexFormComponents(a, new List<ICmObject> {ab.SensesOS[0], ac});
			AddComplexFormComponents(b, new List<ICmObject> {ab});
			AddComplexFormComponents(c, new List<ICmObject> {ac.SensesOS[0]});
			// SUT
			var breaker = new CircularRefBreaker();
			Assert.DoesNotThrow(() => breaker.Process(Cache), "The BreakCircularRefs.Process(cache) method does not throw an exception");
			Assert.AreEqual(0, a.EntryRefsOS.Count, "Invalid LexEntryRef should be be removed from 'a'");
			Assert.AreEqual(0, b.EntryRefsOS.Count, "Invalid LexEntryRef should be be removed from 'b'");
			Assert.AreEqual(0, c.EntryRefsOS.Count, "Invalid LexEntryRef should be be removed from 'c'");
			Assert.AreEqual(0, d.EntryRefsOS.Count, "'d' should never have had any LexEntryRef objects");
			Assert.AreEqual(1, ab.EntryRefsOS.Count, "'ab' should have a single LexEntryRef");
			Assert.AreEqual(1, ac.EntryRefsOS.Count, "'ac' should have a single LexEntryRef");
			Assert.AreEqual(1, abcd.EntryRefsOS.Count, "'abcd' should have a single LexEntryRef");
			Assert.AreEqual(6, breaker.Count, "There should have been 6 LexEntryRef objects to process for this test");
			Assert.AreEqual(5, breaker.Circular, "There should have been 5 circular references fixed");
			Assert.DoesNotThrow(() => breaker.Process(Cache), "The BreakCircularRefs.Process(cache) method still does not throw an exception");
			Assert.AreEqual(0, a.EntryRefsOS.Count, "'a' should still not have any LexEntryRef objects");
			Assert.AreEqual(0, b.EntryRefsOS.Count, "'b' should still not have any LexEntryRef objects");
			Assert.AreEqual(0, c.EntryRefsOS.Count, "'c' should still not have any LexEntryRef objects");
			Assert.AreEqual(0, d.EntryRefsOS.Count, "'d' should still not have any LexEntryRef objects");
			Assert.AreEqual(1, ab.EntryRefsOS.Count, "'ab' should still have a single LexEntryRef");
			Assert.AreEqual(1, ac.EntryRefsOS.Count, "'ac' should still have a single LexEntryRef");
			Assert.AreEqual(1, abcd.EntryRefsOS.Count, "'abcd' should still have a single LexEntryRef");
			Assert.AreEqual(3, breaker.Count, "There should have been 3 LexEntryRef objects to process for this test");
			Assert.AreEqual(0, breaker.Circular, "There should have been 0 circular references fixed");
		}

		private void AddComplexFormComponents(ILexEntry entry, List<ICmObject> list)
		{
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler, () =>
			{
				var dummy = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var ler = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
				dummy.EntryRefsOS.Add(ler);
				ler.RefType = LexEntryRefTags.krtComplexForm;
				foreach (var item in list)
				{
					ler.ComponentLexemesRS.Add(item);
					ler.PrimaryLexemesRS.Add(item);
				}
				// Change the owner to the real entry: this bypasses the check for circular references in FdoList.Add().
				entry.EntryRefsOS.Add(ler);
				dummy.Delete();
			});
		}

		private ILexEntry MakeEntry(string lf, string gloss)
		{
			ILexEntry entry = null;
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				entry.LexemeFormOA = form;
				form.Form.VernacularDefaultWritingSystem =
					Cache.TsStrFactory.MakeString(lf, Cache.DefaultVernWs);
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss, Cache.DefaultAnalWs);
			});
			return entry;
		}

	}
}
