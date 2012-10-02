// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SimpleRootSiteTests_ScrollSelectionIntoView.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	#region MakeSelectionVisibleTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for SimpleRootSite.ScrollSelectionIntoView with parameter
	/// VwScrollSelOpts.kssoDefault.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrollSelectionIntoView_Default : ScrollTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If IP is already visible the scroll position should not change
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPVisible()
		{
			SetLocation(new Rect(0, 1000, 0, 1000 + m_site.LineHeight), false,
				new Point(0, 950), false);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			Assert.AreEqual(new Point(0,- 950), m_site.ScrollPosition,
				"Scroll position should not change if IP is already visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If IP is below client window we expect the window to scroll
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPBelowWindow()
		{
			SetLocation(new Rect(0, 5000, 0, 5000 + m_site.LineHeight), false, new Point(0,0),
				false);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the IP to be somewhere inside of the client window.
			Assert.IsTrue(IsInClientWindow(5000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If IP is almost completely below client window we expect the window to scroll
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAlmostBelowWindow()
		{
			SetLocation(new Rect(0, 5000, 0, 5000 + m_site.LineHeight), false,
				new Point(0, 5001), false);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the IP to be somewhere inside of the client window.
			Assert.IsTrue(IsInClientWindow(5000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If IP is partly below client window we expect the window to scroll
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPPartlyBelowWindow()
		{
			SetLocation(new Rect(0, 5000, 0, 5000 + m_site.LineHeight), false,
				new Point(0, 4999 + m_site.LineHeight), false);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the IP to be somewhere inside of the client window.
			Assert.IsTrue(IsInClientWindow(5000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If IP is above client window we expect the window to scroll
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAboveWindow()
		{
			SetLocation(new Rect(0, 1000, 0, 1000 + m_site.LineHeight), false,
				new Point(0, 2000), false);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the IP to be somewhere inside of the client window.
			Assert.IsTrue(IsInClientWindow(1000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If IP is partly above client window we expect the window to scroll
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPPartlyAboveWindow()
		{
			SetLocation(new Rect(0, 1000, 0, 1000 + m_site.LineHeight), false,
				new Point(0, 1001), false);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the IP to be somewhere inside of the client window.
			Assert.IsTrue(IsInClientWindow(1000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If IP is almost entirely above client window we expect the window to scroll
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAlmostAboveWindow()
		{
			SetLocation(new Rect(0, 1000, 0, 1000 + m_site.LineHeight), false,
				new Point(0, 999 + m_site.LineHeight), false);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the IP to be somewhere inside of the client window.
			Assert.IsTrue(IsInClientWindow(1000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If range selection is entirely visible the scroll position should not change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelAllVisible()
		{
			SetLocation(new Rect(30, 1020, 60, 1100), false, new Point(0, 1000), true);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			Assert.AreEqual(new Point(0, -1000), m_site.ScrollPosition,
				"Scroll position should not change if selection is already visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If range selection isn't visible at all we expect the end of the selection to scroll
		/// into view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelAllNotVisible()
		{
			SetLocation(new Rect(30, 1020, 60, 1100), false, new Point(0, 400), true);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the end of the selection to be somewhere inside of the client window
			Assert.IsTrue(IsInClientWindow(1100));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If range selection isn't visible at all we expect the end of the selection to scroll
		/// into view. This tests when the end is before the anchor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelAllNotVisibleEndBeforeAnchor()
		{
			SetLocation(new Rect(30, 1020, 60, 1100), true, new Point(0, 400), true);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the end of the selection to be somewhere inside of the client window
			Assert.IsTrue(IsInClientWindow(1020));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the anchor of the range selection is above the client window and the end of the
		/// selection is inside of the client window we expect the scroll position not to change
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelAnchorAboveWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), false, new Point(0, 1000), true);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			Assert.AreEqual(new Point(0, -1000), m_site.ScrollPosition,
				"Scroll position should not change if end of selection is already visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the end of the range selection is above the window and the anchor is inside of
		/// the client window we expect scrolling.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelEndAboveWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), true, new Point(0, 1000), true);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the end of the selection to be somewhere inside of the client window
			Assert.IsTrue(IsInClientWindow(900));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If end of range selection is partly above client window we expect the window to
		/// scroll
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelEndPartlyAboveWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), true, new Point(0, 901), true);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the end of the selection to be somewhere inside of the client window.
			Assert.IsTrue(IsInClientWindow(900));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If end of range selection is almost entirely above client window we expect the
		/// window to scroll
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelEndAlmostAboveWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), true, new Point(0, 899 + m_site.LineHeight),
				true);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the end of the selection to be somewhere inside of the client window.
			Assert.IsTrue(IsInClientWindow(900));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the anchor of the range selection is below the window and the end is inside of
		/// the client window the scroll position should not change
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelAnchorBelowWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), true, new Point(0, 850), true);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			Assert.AreEqual(new Point(0, -850), m_site.ScrollPosition,
				"Scroll position should not change if end of selection is already visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the anchor of the range selection is below the window and the end is partially in
		/// the client window the scroll position should change
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelEndAlmostBelowWindowAnchorBelow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), true,
				new Point(0, 901 - m_site.ClientHeight), true);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the end of the selection to be somewhere inside of the client window
			Assert.IsTrue(IsInClientWindow(900));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the end of the selection is below the window and the anchor is inside of the
		/// client window, we expect the scroll position to change
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelEndBelowWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), false, new Point(0, 850), true);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the end of the selection to be somewhere inside of the client window
			Assert.IsTrue(IsInClientWindow(1100));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If both the anchor and the end of the selection are outside of the client window
		/// we expect the end of the selection to scroll into view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelBothOutsideWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1300), false, new Point(0, 1000), true);

			m_site.ScrollSelectionIntoView(m_selection, VwScrollSelOpts.kssoDefault);

			// We expect the end of the selection to be somewhere inside of the client window
			Assert.IsTrue(IsInClientWindow(1300));
		}
	}
	#endregion MakeSelectionVisibleTests

	// ENHANCE: tests for
	// - split selection
	// - kssoNearTop
	// - kssoTop
}
