// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Views;

namespace SIL.FieldWorks.Language
{
	[TestFixture]
	public class ManagedVwWindowTests
	{
		[Test]
		public void SimpleWindowTest()
		{
			using (var c = new Control())
			{
				var wrappedWindow = new ManagedVwWindow();
				wrappedWindow.Window = (uint)c.Handle;
				Rect temp;
				wrappedWindow.GetClientRectangle(out temp);

				Assert.That(temp.left, Is.EqualTo(c.ClientRectangle.Left), "Left not the same");
				Assert.That(temp.right, Is.EqualTo(c.ClientRectangle.Right), "Right not the same");
				Assert.That(temp.top, Is.EqualTo(c.ClientRectangle.Top), "Top not the same");
				Assert.That(temp.bottom, Is.EqualTo(c.ClientRectangle.Bottom), "Bottom not the same");
			}
		}

		[Test]
		public void NotSettingWindowTest()
		{
			var wrappedWindow = new ManagedVwWindow();
			Rect temp;
			Assert.That(() => wrappedWindow.GetClientRectangle(out temp), Throws.TypeOf<ApplicationException>());
		}
	}
}
