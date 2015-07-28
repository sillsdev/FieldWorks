// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using NUnit.Framework;
using System;
using SIL.FieldWorks.Views;
using System.Windows.Forms;
using SIL.Utils;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Language
{
	[TestFixture]
	public class ManagedVwWindowTests : BaseTest
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

				Assert.AreEqual(c.ClientRectangle.Left, temp.left, "Left not the same");
				Assert.AreEqual(c.ClientRectangle.Right, temp.right, "Right not the same");
				Assert.AreEqual(c.ClientRectangle.Top, temp.top, "Top not the same");
				Assert.AreEqual(c.ClientRectangle.Bottom, temp.bottom, "Bottom not the same");
			}
		}

		[Test]
		[ExpectedException(typeof(ApplicationException)) ]
		public void NotSettingWindowTest()
		{
			var wrappedWindow = new ManagedVwWindow();
			Rect temp;
			wrappedWindow.GetClientRectangle(out temp);
		}
	}
}
