// Copyright (c) 2016-2022 SIL International
// SilOutlookBar is licensed under the MIT license.
using System;
using LanguageExplorer.Impls.SilSidePane;
using NUnit.Framework;

namespace LanguageExplorerTests.Impls.SilSidePane
{
	[TestFixture]
	public class ItemTests
	{
		[TestCase("")]
		[TestCase("itemname")]
		public void ItemTest_basic(string itemName)
		{
			Assert.That(()=> new Item(itemName), Throws.Nothing);
		}

		[Test]
		public void ItemTest_null()
		{
			Assert.That(() => { var item = new Item(null); }, Throws.TypeOf<ArgumentNullException>());
		}
	}
}
