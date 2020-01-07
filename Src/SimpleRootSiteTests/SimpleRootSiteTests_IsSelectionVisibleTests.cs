// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using FieldWorks.TestUtilities.Attributes;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Tests for SimpleRootSite.IsSelectionVisible
	/// </summary>
	[TestFixture]
	[InitializeRealKeyboardController]
	public class IsSelectionVisibleTests : ScrollTestsBase
	{
		/// <summary>
		/// IP is visible at the top of the window, so result should be true
		/// </summary>
		[Test]
		public void IPVisibleTopWindow()
		{
			SetLocation(new Rect(0, 0, 0, m_site.LineHeight), false, new Point(0, 0), false);
			Assert.IsTrue(m_site.IsSelectionVisible(m_selection), "Selection should be visible");
		}

		/// <summary>
		/// IP is not visible at the top of the window when the flag to have at least one line
		/// of text is set to true, so result should be false
		/// </summary>
		[Test]
		public void IPNotVisibleWhenFlagIsSetTopWindow()
		{
			SetLocation(new Rect(0, 0, 0, m_site.LineHeight), false, new Point(0, 0), false);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection, true), "Selection should not be visible if flag is set");
		}

		/// <summary>
		/// IP is visible at bottom of window, so result should be true
		/// </summary>
		[Test]
		public void IPVisibleBottomWindow()
		{
			SetLocation(new Rect(0, m_site.ClientHeight - m_site.LineHeight, 0, m_site.ClientHeight), false, new Point(0, 0), false);
			Assert.IsTrue(m_site.IsSelectionVisible(m_selection), "Selection should be visible");
		}

		/// <summary>
		/// IP is not visible at bottom of window when the flag to have at least one line of
		/// text is set to true, so result should be false
		/// </summary>
		[Test]
		public void IPNotVisibleWhenFlagIsSetBottomWindow()
		{
			SetLocation(new Rect(0, m_site.ClientHeight - m_site.LineHeight, 0, m_site.ClientHeight), false, new Point(0, 0), false);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection, true), "Selection should not be visible if flag is set");
		}

		/// <summary>
		/// IP is visible in the middle of window when the flag to have at least one line of
		/// text is set to true, so result should be true
		/// </summary>
		[Test]
		public void IPVisibleWhenFlagIsSetMiddleWindow()
		{
			SetLocation(new Rect(0, 50, 0, 50 + m_site.LineHeight), false, new Point(0, 0), false);
			Assert.IsTrue(m_site.IsSelectionVisible(m_selection, true), "Selection should be visible if in the middle of the window");
		}

		/// <summary>
		/// IP is below client window, so result should be false
		/// </summary>
		[Test]
		public void IPBelowWindow()
		{
			SetLocation(new Rect(0, 5000, 0, 5000 + m_site.LineHeight), false, new Point(0, 0), false);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be visible");
		}

		/// <summary>
		/// IP is almost completely below client window, so result should be false
		/// </summary>
		[Test]
		public void IPAlmostBelowWindow()
		{
			SetLocation(new Rect(0, 5000, 0, 5000 + m_site.LineHeight), false, new Point(0, 5001), false);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be visible");
		}

		/// <summary>
		/// IP is partly completely below client window, so result should be false
		/// </summary>
		[Test]
		public void IPPartlyBelowWindow()
		{
			SetLocation(new Rect(0, 5000, 0, 5000 + m_site.LineHeight), false, new Point(0, 4999 + m_site.LineHeight), false);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be visible");
		}

		/// <summary>
		/// IP is above client window, so result should be false
		/// </summary>
		[Test]
		public void IPAboveWindow()
		{
			SetLocation(new Rect(0, 1000, 0, 1000 + m_site.LineHeight), false, new Point(0, 2000), false);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be visible");
		}

		/// <summary>
		/// IP is partly above client window, so result should be false
		/// </summary>
		[Test]
		public void IPPartlyAboveWindow()
		{
			SetLocation(new Rect(0, 1000, 0, 1000 + m_site.LineHeight), false, new Point(0, 1001), false);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be visible");
		}

		/// <summary>
		/// IP is almost entirely above client window, so result should be false
		/// </summary>
		[Test]
		public void IPAlmostAboveWindow()
		{
			SetLocation(new Rect(0, 1000, 0, 1000 + m_site.LineHeight), false, new Point(0, 999 + m_site.LineHeight), false);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be visible");
		}

		/// <summary>
		/// We have a range m_selection that is entirely visible, so result should be true
		/// </summary>
		[Test]
		public void RangeSelAllVisible()
		{
			SetLocation(new Rect(30, 1020, 60, 1100), false, new Point(0, 1000), true);
			Assert.IsTrue(m_site.IsSelectionVisible(m_selection), "Selection should be all visible");
		}

		/// <summary>
		/// We have a range m_selection that is completely outside of the client window, so result
		/// should be false
		/// </summary>
		[Test]
		public void RangeSelAllNotVisible()
		{
			SetLocation(new Rect(30, 1020, 60, 1100), false, new Point(0, 400), true);
			m_site.ScrollMinSize = new Size(m_site.ClientRectangle.Width, 10000);
			m_site.ScrollPosition = new Point(0, 400);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be visible");
		}

		/// <summary>
		/// We have a range m_selection that is completely outside of the client window, so result
		/// should be false. This tests when the end is before the anchor
		/// </summary>
		[Test]
		public void RangeSelAllNotVisibleEndBeforeAnchor()
		{
			SetLocation(new Rect(30, 1020, 60, 1100), true, new Point(0, 400), true);
			m_site.ScrollMinSize = new Size(m_site.ClientRectangle.Width, 10000);
			m_site.ScrollPosition = new Point(0, 400);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be visible");
		}

		/// <summary>
		/// We have a range m_selection where the anchor is above the client window, the end
		/// is inside of the client window. Result should be true.
		/// </summary>
		[Test]
		public void RangeSelAnchorAboveWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), false, new Point(0, 1000), true);
			Assert.IsTrue(m_site.IsSelectionVisible(m_selection), "Selection should be considered visible if end is showing");
		}

		/// <summary>
		/// We have a range m_selection where the end is above the client window, the anchor is
		/// inside of the client window. Result should be false.
		/// </summary>
		[Test]
		public void RangeSelEndAboveWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), true, new Point(0, 1000), true);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be considered visible if end is not showing");
		}

		/// <summary>
		/// We have a range m_selection where the anchor is below the client window, the end is
		/// inside of the client window. Result should be true.
		/// </summary>
		[Test]
		public void RangeSelAnchorBelowWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), true, new Point(0, 850), true);
			Assert.IsTrue(m_site.IsSelectionVisible(m_selection), "Selection should be considered visible if end is showing");
		}

		/// <summary>
		/// We have a range m_selection where the end is below the client window, the anchor is
		/// inside of the client window. Result should be false.
		/// </summary>
		[Test]
		public void RangeSelEndBelowWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), false, new Point(0, 850), true);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be considered visible if end is not showing");
		}

		/// <summary>
		/// We have a range m_selection where the anchor is above the client window, the end is
		/// below of the client window. Result should be false.
		/// </summary>
		[Test]
		public void RangeSelBothOutsideWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1300), false, new Point(0, 1000), true);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be considered visible if end is not showing");
		}

		/// <summary>
		/// We have a range m_selection where the anchor is below the client window, the end is
		/// partially inside of the client window. Result should be false.
		/// </summary>
		[Test]
		public void RangeSelEndAlmostBelowWindowAnchorBelow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), true, new Point(0, 901 - m_site.ClientHeight), true);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be considered visible if end is not completely showing");
		}

		/// <summary>
		/// We have a range m_selection where the anchor is above the client window, the end is
		/// partially inside of the client window. Result should be false.
		/// </summary>
		[Test]
		public void RangeSelEndAlmostAboveWindowAnchorAbove()
		{
			SetLocation(new Rect(30, 900, 60, 1100), false, new Point(0, 1099), true);
			Assert.IsFalse(m_site.IsSelectionVisible(m_selection), "Selection should not be considered visible if end is not completely showing");
		}
	}
}