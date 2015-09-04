// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System.Drawing;
using LanguageExplorer.Controls.SilSidePane;
using NUnit.Framework;

namespace LanguageExplorerTests.SilSidePaneTests
{
	[TestFixture]
	public class OutlookBarButtonTests
	{
		private OutlookBarButton _button;

		[SetUp]
		public void SetUp()
		{
			_button = new OutlookBarButton();
		}

		[TearDown]
		public void TearDown()
		{
			_button.Dispose();
		}

		[Test]
		public void Constructor()
		{
			using (Image image = new Bitmap("./DefaultIcon.ico"))
			{
#pragma warning disable 0219
				using (OutlookBarButton button = new OutlookBarButton("text", image))
					{}
#pragma warning restore 0219
			}
		}

		[Test]
		public void Enabled()
		{
			_button.Enabled = true;
			Assert.IsTrue(_button.Enabled);
			_button.Enabled = false;
			Assert.IsFalse(_button.Enabled);
		}

		[Test]
		public void Tag()
		{
			object someObject = new object();
			_button.Tag = someObject;
			Assert.AreSame(someObject, _button.Tag);
		}

		[Test]
		public void Name()
		{
			string name = "buttonname";
			_button.Name = name;
			string result = _button.Name;
			Assert.AreEqual(result, name);
		}

		[Test]
		public void SupportsImageType()
		{
			using (Image image = new Bitmap("./DefaultIcon.ico"))
			{
				_button.Image = image;
				Assert.AreSame(image, _button.Image);
			}
		}
	}
}
