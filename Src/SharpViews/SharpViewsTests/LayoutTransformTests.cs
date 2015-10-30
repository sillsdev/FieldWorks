// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class LayoutTransformTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		[Test]
		public void ToRoot()
		{
			LayoutTransform transform = new LayoutTransform(2, 3, 96, 96);
			Point p = new Point(10,15);
			Point p2 = transform.ToRoot(p);
			Assert.AreEqual(12, p2.X);
			Assert.AreEqual(18, p2.Y);
		}

		[Test]
		public void MpToPixels()
		{
			LayoutTransform transform = new LayoutTransform(2, 3, 96, 120);
			int dx = transform.MpToPixelsX(72000);
			Assert.AreEqual(96, dx);
			int dy = transform.MpToPixelsY(36000);
			Assert.AreEqual(60, dy);
			// Just a little larger should round down
			dx = transform.MpToPixelsX(72005);
			Assert.AreEqual(96, dx);
			// More than half a pixel larger should round up.
			dx = transform.MpToPixelsX(72376);
			Assert.AreEqual(97, dx);
			// Close to half should round down
			dx = transform.MpToPixelsX(72373);
			Assert.AreEqual(96, dx);
			// Should work with negative numbers too.
			dx = transform.MpToPixelsX(-72376);
			Assert.AreEqual(-97, dx);
			// Close to half should round down
			dx = transform.MpToPixelsX(-72373);
			Assert.AreEqual(-96, dx);
			// Less than a half pix rounds down to zero
			dx = transform.MpToPixelsX(373);
			Assert.AreEqual(0, dx);
			// More than a half pix rounds up
			dx = transform.MpToPixelsX(377);
			Assert.AreEqual(1, dx);
			dx = transform.MpToPixelsX(-373);
			Assert.AreEqual(0, dx);

			// When computing the thickness of a border, we get similar results except
			// we never round down to zero.
			dx = transform.MpToBorderPixelsX(373);
			Assert.AreEqual(1, dx);
			dx = transform.MpToBorderPixelsX(377);
			Assert.AreEqual(1, dx);
			dx = transform.MpToBorderPixelsX(72373);
			Assert.AreEqual(96, dx);
			dx = transform.MpToBorderPixelsX(-373);
			Assert.AreEqual(-1, dx);
			dy = transform.MpToBorderPixelsY(380);
			Assert.AreEqual(1, dy);
		}
	}
}
