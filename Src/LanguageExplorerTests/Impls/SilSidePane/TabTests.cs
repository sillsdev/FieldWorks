// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using LanguageExplorer.Impls.SilSidePane;
using NUnit.Framework;

namespace LanguageExplorerTests.Impls.SilSidePane
{
	[TestFixture]
	public class TabTests
	{
		[Test]
		public void TabTest_basic()
		{
			Assert.DoesNotThrow(() => new Tab("name"));
			Assert.DoesNotThrow(() => new Tab(string.Empty));
		}
	}
}