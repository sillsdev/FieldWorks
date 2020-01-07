// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.XMLViews;
using NUnit.Framework;

namespace LanguageExplorerTests.Controls.XMLViews
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
			var info1 = new NeededPropertyInfo(1);
			var info2 = info1.AddObjField(2, true);
			var info2b = info1.AddObjField(2, true);
			Assert.AreSame(info2, info2b); // didn't make a duplicate

			var info3 = info1.AddObjField(3, true);
			info2b = info1.AddObjField(2, true);
			Assert.AreSame(info2, info2b); // can still find (2)

			var info3b = info1.AddObjField(3, true);
			Assert.AreSame(info3, info3b); // also rediscovers ones that aren't first

			var info4 = info1.AddObjField(4, true);
			info2b = info1.AddObjField(2, true);
			Assert.AreSame(info2, info2b); // can still find (2) with 3 items
			info3b = info1.AddObjField(3, true);
			Assert.AreSame(info3, info3b); // can rediscover mid-seq
			var info4b = info1.AddObjField(4, true);
			Assert.AreSame(info4, info4b); // also rediscovers ones that aren't first

			// Now recursive
			var info5 = info2.AddObjField(5, true);
			var info5b = info1.AddObjField(2, true).AddObjField(5, true);
			Assert.AreSame(info5, info5b); // recursive works too.
		}
	}
}