// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;

namespace XMLViewsTests
{
	/// <summary>
	/// Test the NeededPropertyInfo.
	/// </summary>
	[TestFixture]
	public class TestNeededPropertyInfo: SIL.FieldWorks.Test.TestUtils.BaseTest
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
			Assert.AreSame(info2, info2b); // did't make a duplicate

			NeededPropertyInfo info3 = info1.AddObjField(3, true);
			info2b = info1.AddObjField(2, true);
			Assert.AreSame(info2, info2b); // can still find (2)

			NeededPropertyInfo info3b = info1.AddObjField(3, true);
			Assert.AreSame(info3, info3b); // also rediscovers ones that aren't first

			NeededPropertyInfo info4 = info1.AddObjField(4, true);
			info2b = info1.AddObjField(2, true);
			Assert.AreSame(info2, info2b); // can still find (2) with 3 items
			info3b = info1.AddObjField(3, true);
			Assert.AreSame(info3, info3b); // can rediscover mid-seq
			NeededPropertyInfo info4b = info1.AddObjField(4, true);
			Assert.AreSame(info4, info4b); // also rediscovers ones that aren't first

			// Now recursive
			NeededPropertyInfo info5 = info2.AddObjField(5, true);
			NeededPropertyInfo info5b = info1.AddObjField(2, true).AddObjField(5, true);
			Assert.AreSame(info5, info5b); // recursive works too.
		}
	}
}
