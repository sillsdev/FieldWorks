// Copyright (c) 2016-2021 SIL International
// SilOutlookBar is licensed under the MIT license.

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
		public void ItemTest_null()
		{
#pragma warning disable 0219
			Assert.That(() => { Item item = new Item(null); }, Throws.ArgumentNullException);
#pragma warning restore 0219
		}
	}
}
