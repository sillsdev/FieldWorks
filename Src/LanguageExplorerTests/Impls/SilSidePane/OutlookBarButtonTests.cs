// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System.Drawing;
using LanguageExplorer.Impls.SilSidePane;
using NUnit.Framework;

namespace LanguageExplorerTests.Impls.SilSidePane
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
			using (new OutlookBarButton("text", image))
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