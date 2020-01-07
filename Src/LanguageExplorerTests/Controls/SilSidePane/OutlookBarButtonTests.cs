// SilSidePane, Copyright 2009-2020 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System.Drawing;
using LanguageExplorer.Controls.SilSidePane;
using NUnit.Framework;

namespace LanguageExplorerTests.Controls.SilSidePane
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
			_button = null;
		}

		[Test]
		public void Constructor()
		{
#pragma warning disable 0219
			using (Image image = new Bitmap("./DefaultIcon.ico"))
			using (var button = new OutlookBarButton("text", image))
			{ }
#pragma warning restore 0219
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
			var someObject = new object();
			_button.Tag = someObject;
			Assert.AreSame(someObject, _button.Tag);
		}

		[Test]
		public void Name()
		{
			const string name = "buttonname";
			_button.Name = name;
			Assert.AreEqual(_button.Name, name);
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