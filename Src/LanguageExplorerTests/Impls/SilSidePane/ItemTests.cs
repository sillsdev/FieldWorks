<<<<<<< HEAD:Src/LanguageExplorerTests/Impls/SilSidePane/ItemTests.cs
// SilSidePane, Copyright 2009-2020 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.
||||||| f013144d5:Src/XCore/SilSidePane/SilSidePaneTests/ItemTests.cs
// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.
=======
// Copyright (c) 2016-2021 SIL International
// SilOutlookBar is licensed under the MIT license.
>>>>>>> develop:Src/XCore/SilSidePane/SilSidePaneTests/ItemTests.cs

<<<<<<< HEAD:Src/LanguageExplorerTests/Impls/SilSidePane/ItemTests.cs
using System;
using LanguageExplorer.Impls.SilSidePane;
||||||| f013144d5:Src/XCore/SilSidePane/SilSidePaneTests/ItemTests.cs
using System;
=======
>>>>>>> develop:Src/XCore/SilSidePane/SilSidePaneTests/ItemTests.cs
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
<<<<<<< HEAD:Src/LanguageExplorerTests/Impls/SilSidePane/ItemTests.cs
			Assert.That(() => { var item = new Item(null); }, Throws.TypeOf<ArgumentNullException>());
||||||| f013144d5:Src/XCore/SilSidePane/SilSidePaneTests/ItemTests.cs
#pragma warning disable 0219
			Item item = new Item(null);
#pragma warning restore 0219
=======
#pragma warning disable 0219
			Assert.That(() => { Item item = new Item(null); }, Throws.ArgumentNullException);
#pragma warning restore 0219
>>>>>>> develop:Src/XCore/SilSidePane/SilSidePaneTests/ItemTests.cs
		}
	}
}
