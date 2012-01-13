// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScreenUtilsTest.cs
// Responsibility: DavidO
// --------------------------------------------------------------------------------------------
using System.Windows.Forms;
using NUnit.Framework;
using System.Drawing;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for screen utils.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScreenUtilsTest // can't derive from BaseTest because of dependencies
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureVisibleRect method of the <see cref="ScreenUtils"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureVisibleRectTest()
		{
			foreach (Screen scrn in Screen.AllScreens)
			{
				// We want to skip a screen if it is vitual
				if (ScreenUtils.ScreenIsVirtual(scrn))
					continue;

				Rectangle realScreenArea = ScreenUtils.AdjustedWorkingArea(scrn);

				// Make test rectangle half the height and width of the screen's working area.
				Rectangle rcAdjusted = scrn.WorkingArea;
				rcAdjusted.Width /= 2;
				rcAdjusted.Height /= 2;

				// Move rectangle so right edge is off the screen and test that our rectangle
				// comes back fully contained within the screen.
				rcAdjusted.X = scrn.WorkingArea.Right - rcAdjusted.Width + 15;
				ScreenUtils.EnsureVisibleRect(ref rcAdjusted);
				Assert.IsTrue(realScreenArea.Contains(rcAdjusted),
					"Right edge test failed:" + scrn.DeviceName);

				// Move rectangle so bottom edge is off the screen and test that our rectangle
				// comes back fully contained within the screen.
				rcAdjusted.Y = scrn.WorkingArea.Bottom - rcAdjusted.Height + 15;
				ScreenUtils.EnsureVisibleRect(ref rcAdjusted);
				Assert.IsTrue(realScreenArea.Contains(rcAdjusted),
					"Bottom edge test failed: " + scrn.DeviceName);

				// Move rectangle so right and bottom edges are off the screen and test that
				// our rectangle comes back fully contained within the screen.
				rcAdjusted.X = scrn.WorkingArea.Right - rcAdjusted.Width + 15;
				rcAdjusted.Y = scrn.WorkingArea.Bottom - rcAdjusted.Height + 15;
				ScreenUtils.EnsureVisibleRect(ref rcAdjusted);
				Assert.IsTrue(realScreenArea.Contains(rcAdjusted),
					"Right & bottom edge test failed: " + scrn.DeviceName);

				// Move rectangle so left edge is off the screen and test that
				// our rectangle comes back fully contained within the screen.
				rcAdjusted.X = scrn.Bounds.Left - 15;
				ScreenUtils.EnsureVisibleRect(ref rcAdjusted);
				Assert.IsTrue(realScreenArea.Contains(rcAdjusted),
					"Left edge test failed: " + scrn.DeviceName);

				// Move rectangle so top edge is off the screen and test that our
				// rectangle comes back fully contained within the screen.
				rcAdjusted.Y = scrn.Bounds.Top - 15;
				ScreenUtils.EnsureVisibleRect(ref rcAdjusted);
				Assert.IsTrue(realScreenArea.Contains(rcAdjusted),
					"Top edge test failed: " + scrn.DeviceName);

				// Move rectangle so left and top edges are off the screen and test that
				// our rectangle comes back fully contained within the screen.
				rcAdjusted.X = scrn.Bounds.Left - 15;
				rcAdjusted.Y = scrn.Bounds.Top - 15;
				ScreenUtils.EnsureVisibleRect(ref rcAdjusted);
				Assert.IsTrue(realScreenArea.Contains(rcAdjusted),
					"Left & top edge test failed: " + scrn.DeviceName);

				// Make rectangle larger than the screen and move it up and to the left of
				// the top, left corner. Then verify that it shrinks and gets moved to fit
				// exactly in the screen's working area.
				rcAdjusted = scrn.WorkingArea;
				rcAdjusted.Inflate(100, 100);
				ScreenUtils.EnsureVisibleRect(ref rcAdjusted);
				Assert.AreEqual(realScreenArea, rcAdjusted,
					"Shrink Rectangle to working area test failed: ");
			}
		}
	}
}
