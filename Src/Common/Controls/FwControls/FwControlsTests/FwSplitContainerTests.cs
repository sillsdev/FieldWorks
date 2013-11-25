// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwSplitContainerTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwSplitContainerTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting (and obeying) a max percentage for a horizontal split container
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HorizontalGreaterThenMaxPercentage()
		{
			using (var splitContainer = new FwSplitContainer())
				{
					splitContainer.Orientation = Orientation.Horizontal;
					splitContainer.Bounds = new Rectangle(0, 0, 100, 100);
					splitContainer.SplitterDistance = 50;

					splitContainer.MaxFirstPanePercentage = 0.7f;

					// Moving splitter to 90% should leave splitter at 70%
					SplitterCancelEventArgs e = new SplitterCancelEventArgs(50, 90, 50, 90);
					splitContainer.OnSplitterMoving(e);

					Assert.AreEqual((int)(splitContainer.Height * splitContainer.MaxFirstPanePercentage),
						e.SplitY);
				}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting (and obeying) a max percentage for a horizontal split container.
		/// Moving the splitter to exactly max percentage is allowed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HorizontalEqualsMaxPercentage()
		{
			using (FwSplitContainer splitContainer = new FwSplitContainer())
			{
				splitContainer.Orientation = Orientation.Horizontal;
				splitContainer.Bounds = new Rectangle(0, 0, 100, 100);
				splitContainer.SplitterDistance = 50;

				splitContainer.MaxFirstPanePercentage = 0.7f;

				// Moving splitter to 70% should be allowed
				SplitterCancelEventArgs e = new SplitterCancelEventArgs(50, 70, 50, 70);
				splitContainer.OnSplitterMoving(e);

				Assert.IsFalse(e.Cancel);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting (and obeying) a max percentage for a vertical split container
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerticalGreaterThenMaxPercentage()
		{
			using (FwSplitContainer splitContainer = new FwSplitContainer())
			{
				splitContainer.Orientation = Orientation.Vertical;
				splitContainer.Bounds = new Rectangle(0, 0, 100, 100);
				splitContainer.SplitterDistance = 50;

				splitContainer.MaxFirstPanePercentage = 0.7f;

				// Moving splitter to 90% should leave splitter at 70%
				SplitterCancelEventArgs e = new SplitterCancelEventArgs(90, 50, 90, 50);
				splitContainer.OnSplitterMoving(e);

				Assert.AreEqual((int)(splitContainer.Width * splitContainer.MaxFirstPanePercentage),
					e.SplitX);
			}
		}
	}
}
