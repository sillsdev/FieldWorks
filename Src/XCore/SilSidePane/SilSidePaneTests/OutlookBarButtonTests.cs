// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System.Drawing;
using NUnit.Framework;

namespace SIL.SilSidePane
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
			using (Image image = new Bitmap("DefaultIcon.ico"))
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
			Assert.That(_button.Enabled, Is.True);
			_button.Enabled = false;
			Assert.That(_button.Enabled, Is.False);
		}

		[Test]
		public void Tag()
		{
			object someObject = new object();
			_button.Tag = someObject;
			Assert.That(_button.Tag, Is.SameAs(someObject));
		}

		[Test]
		public void Name()
		{
			string name = "buttonname";
			_button.Name = name;
			string result = _button.Name;
			Assert.That(name, Is.EqualTo(result));
		}

		[Test]
		public void SupportsImageType()
		{
			using (Image image = new Bitmap("DefaultIcon.ico"))
			{
				_button.Image = image;
				Assert.That(_button.Image, Is.SameAs(image));
			}
		}
	}
}
