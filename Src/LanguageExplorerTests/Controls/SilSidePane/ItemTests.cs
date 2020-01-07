// SilSidePane, Copyright 2009-2020 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using LanguageExplorer.Controls.SilSidePane;
using NUnit.Framework;

namespace LanguageExplorerTests.Controls.SilSidePane
{
	[TestFixture]
	public class ItemTests
	{
		[Test]
		public void ItemTest_basic()
		{
			Assert.DoesNotThrow(()=> new Item(string.Empty));
			Assert.DoesNotThrow(() => new Item("itemname"));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ItemTest_null()
		{
			var item = new Item(null);
		}
	}
}