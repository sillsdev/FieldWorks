// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using NUnit.Framework;

namespace SIL.SilSidePane
{


	[TestFixture]
	public class TabTests
	{

		[Test]
		public void TabTest_basic()
		{
#pragma warning disable 0219
			Tab tab1 = new Tab("name");
			Tab tab2 = new Tab("");
#pragma warning restore 0219
		}
	}
}
