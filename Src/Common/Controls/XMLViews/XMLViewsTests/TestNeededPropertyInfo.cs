// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;

namespace XMLViewsTests
{
	/// <summary>
	/// Test the NeededPropertyInfo.
	/// </summary>
	[TestFixture]
	public class TestNeededPropertyInfo
	{
		/// <summary>
		/// Try adding sequence props recursively.
		/// </summary>
		[Test]
		public void TestSeqProps()
		{
			NeededPropertyInfo info1 = new NeededPropertyInfo(1);
			NeededPropertyInfo info2 = info1.AddObjField(2, true);
			NeededPropertyInfo info2b = info1.AddObjField(2, true);
			Assert.That(info2b, Is.SameAs(info2)); // did't make a duplicate

			NeededPropertyInfo info3 = info1.AddObjField(3, true);
			info2b = info1.AddObjField(2, true);
			Assert.That(info2b, Is.SameAs(info2)); // can still find (2)

			NeededPropertyInfo info3b = info1.AddObjField(3, true);
			Assert.That(info3b, Is.SameAs(info3)); // also rediscovers ones that aren't first

			NeededPropertyInfo info4 = info1.AddObjField(4, true);
			info2b = info1.AddObjField(2, true);
			Assert.That(info2b, Is.SameAs(info2)); // can still find (2) with 3 items
			info3b = info1.AddObjField(3, true);
			Assert.That(info3b, Is.SameAs(info3)); // can rediscover mid-seq
			NeededPropertyInfo info4b = info1.AddObjField(4, true);
			Assert.That(info4b, Is.SameAs(info4)); // also rediscovers ones that aren't first

			// Now recursive
			NeededPropertyInfo info5 = info2.AddObjField(5, true);
			NeededPropertyInfo info5b = info1.AddObjField(2, true).AddObjField(5, true);
			Assert.That(info5b, Is.SameAs(info5)); // recursive works too.
		}
	}
}
