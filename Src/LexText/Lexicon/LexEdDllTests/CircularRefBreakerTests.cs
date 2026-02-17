// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel;
using SIL.FieldWorks.XWorks.LexEd;
using System.Collections.Generic;

namespace LexEdDllTests
{
	public class CircularRefBreakerTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void BreakCircularEntryRefs()
		{
			var a = TestUtils.MakeEntry(Cache, "a", "a");
			var b = TestUtils.MakeEntry(Cache, "b", "b");
			var c = TestUtils.MakeEntry(Cache, "c", "c");
			var d = TestUtils.MakeEntry(Cache, "d", "d");
			var ab = TestUtils.MakeEntry(Cache, "ab", "ab");
			var ac = TestUtils.MakeEntry(Cache, "ac", "ac");
			var abcd = TestUtils.MakeEntry(Cache, "abcd", "abcd");
			// Create some reasonable component references
			TestUtils.AddComplexFormComponents(Cache, ab, new List<ICmObject> {a, b});
			TestUtils.AddComplexFormComponents(Cache, ac, new List<ICmObject> {a.SensesOS[0], c.SensesOS[0]});
			TestUtils.AddComplexFormComponents(Cache, abcd, new List<ICmObject> {a, b, c, d.SensesOS[0]});
			// Create circular component references
			TestUtils.AddComplexFormComponents(Cache, a, new List<ICmObject> {ab.SensesOS[0], ac});
			TestUtils.AddComplexFormComponents(Cache, b, new List<ICmObject> {ab});
			TestUtils.AddComplexFormComponents(Cache, c, new List<ICmObject> {ac.SensesOS[0]});
			// SUT
			var breaker = new CircularRefBreaker();
			Assert.DoesNotThrow(() => breaker.Process(Cache), "The BreakCircularRefs.Process(cache) method does not throw an exception");
			Assert.That(a.EntryRefsOS.Count, Is.EqualTo(0), "Invalid LexEntryRef should be be removed from 'a'");
			Assert.That(b.EntryRefsOS.Count, Is.EqualTo(0), "Invalid LexEntryRef should be be removed from 'b'");
			Assert.That(c.EntryRefsOS.Count, Is.EqualTo(0), "Invalid LexEntryRef should be be removed from 'c'");
			Assert.That(d.EntryRefsOS.Count, Is.EqualTo(0), "'d' should never have had any LexEntryRef objects");
			Assert.That(ab.EntryRefsOS.Count, Is.EqualTo(1), "'ab' should have a single LexEntryRef");
			Assert.That(ac.EntryRefsOS.Count, Is.EqualTo(1), "'ac' should have a single LexEntryRef");
			Assert.That(abcd.EntryRefsOS.Count, Is.EqualTo(1), "'abcd' should have a single LexEntryRef");
			Assert.That(breaker.Count, Is.EqualTo(6), "There should have been 6 LexEntryRef objects to process for this test");
			Assert.That(breaker.Circular, Is.EqualTo(5), "There should have been 5 circular references fixed");
			Assert.DoesNotThrow(() => breaker.Process(Cache), "The BreakCircularRefs.Process(cache) method still does not throw an exception");
			Assert.That(a.EntryRefsOS.Count, Is.EqualTo(0), "'a' should still not have any LexEntryRef objects");
			Assert.That(b.EntryRefsOS.Count, Is.EqualTo(0), "'b' should still not have any LexEntryRef objects");
			Assert.That(c.EntryRefsOS.Count, Is.EqualTo(0), "'c' should still not have any LexEntryRef objects");
			Assert.That(d.EntryRefsOS.Count, Is.EqualTo(0), "'d' should still not have any LexEntryRef objects");
			Assert.That(ab.EntryRefsOS.Count, Is.EqualTo(1), "'ab' should still have a single LexEntryRef");
			Assert.That(ac.EntryRefsOS.Count, Is.EqualTo(1), "'ac' should still have a single LexEntryRef");
			Assert.That(abcd.EntryRefsOS.Count, Is.EqualTo(1), "'abcd' should still have a single LexEntryRef");
			Assert.That(breaker.Count, Is.EqualTo(3), "There should have been 3 LexEntryRef objects to process for this test");
			Assert.That(breaker.Circular, Is.EqualTo(0), "There should have been 0 circular references fixed");
		}
	}
}
