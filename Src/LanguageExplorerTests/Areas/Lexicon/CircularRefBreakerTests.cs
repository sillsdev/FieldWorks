// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer.Areas.Lexicon;
using NUnit.Framework;
using SIL.LCModel;

namespace LanguageExplorerTests.Areas.Lexicon
{
	public class CircularRefBreakerTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void BreakCircularEntryRefs()
		{
			var a = LexiconTestUtils.MakeEntry(Cache, "a", "a");
			var b = LexiconTestUtils.MakeEntry(Cache, "b", "b");
			var c = LexiconTestUtils.MakeEntry(Cache, "c", "c");
			var d = LexiconTestUtils.MakeEntry(Cache, "d", "d");
			var ab = LexiconTestUtils.MakeEntry(Cache, "ab", "ab");
			var ac = LexiconTestUtils.MakeEntry(Cache, "ac", "ac");
			var abcd = LexiconTestUtils.MakeEntry(Cache, "abcd", "abcd");
			// Create some reasonable component references
			LexiconTestUtils.AddComplexFormComponents(Cache, ab, new List<ICmObject> {a, b});
			LexiconTestUtils.AddComplexFormComponents(Cache, ac, new List<ICmObject> {a.SensesOS[0], c.SensesOS[0]});
			LexiconTestUtils.AddComplexFormComponents(Cache, abcd, new List<ICmObject> {a, b, c, d.SensesOS[0]});
			// Create circular component references
			LexiconTestUtils.AddComplexFormComponents(Cache, a, new List<ICmObject> {ab.SensesOS[0], ac});
			LexiconTestUtils.AddComplexFormComponents(Cache, b, new List<ICmObject> {ab});
			LexiconTestUtils.AddComplexFormComponents(Cache, c, new List<ICmObject> {ac.SensesOS[0]});
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
	}
}
