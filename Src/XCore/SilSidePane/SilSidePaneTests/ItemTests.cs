// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System;
using NUnit.Framework;

namespace SIL.SilSidePane
{


	[TestFixture]
	public class ItemTests
	{
		[Test]
		public void ItemTest_basic()
		{
#pragma warning disable 0219
			Item item1 = new Item("");
			Item item2 = new Item("itemname");
#pragma warning restore 0219
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ItemTest_null()
		{
#pragma warning disable 0219
			Item item = new Item(null);
#pragma warning restore 0219
		}
	}
}
