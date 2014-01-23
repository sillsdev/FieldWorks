// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SimpleRootSiteTests_IsSelectionVisibleTests.cs
// Responsibility:

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using NUnit.Framework;
using NMock;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	#region DummyRootSite
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyRootSite : SimpleRootSite
	{
		private Point m_position = new Point(0, 0);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyRootSite() : base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new IVwRootBox RootBox
		{
			set
			{
				CheckDisposed();
				m_rootb = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Point ScrollPosition
		{
			set
			{
				CheckDisposed();
				m_position = new Point(-value.X, -value.Y);
			}
			get
			{
				CheckDisposed();
				return m_position;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We don't do horizontal scrolling
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool DoAutoHScroll
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a DummyEditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper CreateEditingHelper()
		{
			return new DummyEditingHelper();
		}
	}
	#endregion DummyRootSite

	#region ScrollTestsBase class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for tests testing scroll changes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test. Variable disposed in Teardown method")]
	public class ScrollTestsBase
	{
		private readonly string[] kLocationArgs = new string[]{typeof(IVwGraphics).FullName,
																  typeof(Rect).FullName, typeof(Rect).FullName, typeof(Rect).FullName + "&",
																  typeof(Rect).FullName + "&", typeof(bool).FullName + "&", typeof(bool).FullName + "&"};
		internal IVwRootBox m_rootb;
		internal DummyRootSite m_site;
		internal DynamicMock m_mockSelection;
		internal IVwSelection m_selection;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_site = new DummyRootSite();

			DynamicMock rootb = new DynamicMock(typeof(IVwRootBox));
			rootb.SetupResult("Height", 10000);
			rootb.SetupResult("Width", m_site.ClientRectangle.X);
			rootb.SetupResult("IsPropChangedInProgress", false);
			m_rootb = (IVwRootBox)rootb.MockInstance;

			m_site.RootBox = m_rootb;

			m_mockSelection = new DynamicMock(typeof(IVwSelection));
			m_mockSelection.SetupResult("IsValid", true);
			m_site.CreateControl();
			m_site.ScrollMinSize = new Size(m_site.ClientRectangle.Width, 10000);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			m_site.Dispose();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the location of the m_selection
		/// </summary>
		/// <param name="rcPrimary">m_selection rectangle (from top of view)</param>
		/// <param name="fEndBeforeAnchor"><c>true</c> if end is before anchor</param>
		/// <param name="scrollPos">The scroll position</param>
		/// <param name="fIsRange"><c>true</c> if it is a range m_selection</param>
		/// ------------------------------------------------------------------------------------
		protected void SetLocation(Rect rcPrimary, bool fEndBeforeAnchor, Point scrollPos,
			bool fIsRange)
		{
			m_mockSelection.SetupResult("Location", null, kLocationArgs,
				new object[]{null, null, null, new Rect(rcPrimary.left - scrollPos.X,
								rcPrimary.top - scrollPos.Y, rcPrimary.right - scrollPos.X,
								rcPrimary.bottom - scrollPos.Y), new Rect(0, 0, 0, 0), false,
								fEndBeforeAnchor});
			m_mockSelection.SetupResult("IsRange", fIsRange);
			m_mockSelection.SetupResult("SelType", VwSelType.kstText);
			m_mockSelection.SetupResult("EndBeforeAnchor", fEndBeforeAnchor);
			m_selection = (IVwSelection)m_mockSelection.MockInstance;
			m_site.ScrollPosition = scrollPos;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if the end point of the m_selection is inside of the client window
		/// </summary>
		/// <param name="endPos">Location of the end point</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool IsInClientWindow(int endPos)
		{
			bool fRange = m_selection.IsRange;
			bool fEndBeforeAnchor = m_selection.EndBeforeAnchor;

			int yScrollPos = -m_site.ScrollPosition.Y;
			return endPos >= yScrollPos + (fRange && !fEndBeforeAnchor ? 2 : 1) * m_site.LineHeight &&
				endPos <= yScrollPos + m_site.ClientHeight - (fRange && fEndBeforeAnchor ? 2 : 1) * m_site.LineHeight;
		}
	}
	#endregion

	#region IsSelectionVisibleTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for SimpleRootSite.IsSelectionVisible
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class IsSelectionVisibleTests: ScrollTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// IP is visible at the top of the window, so result should be true
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPVisibleTopWindow()
		{
			SetLocation(new Rect(0, 0, 0, m_site.LineHeight), false, new Point(0, 0), false);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsTrue(visible, "Selection should be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// IP is not visible at the top of the window when the flag to have at least one line
		/// of text is set to true, so result should be false
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPNotVisibleWhenFlagIsSetTopWindow()
		{
			SetLocation(new Rect(0, 0, 0, m_site.LineHeight), false, new Point(0, 0), false);

			bool visible = m_site.IsSelectionVisible(m_selection, true);
			Assert.IsFalse(visible, "Selection should not be visible if flag is set");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// IP is visible at bottom of window, so result should be true
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPVisibleBottomWindow()
		{
			SetLocation(new Rect(0, m_site.ClientHeight - m_site.LineHeight, 0, m_site.ClientHeight),
				false, new Point(0, 0), false);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsTrue(visible, "Selection should be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// IP is not visible at bottom of window when the flag to have at least one line of
		/// text is set to true, so result should be false
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPNotVisibleWhenFlagIsSetBottomWindow()
		{
			SetLocation(new Rect(0, m_site.ClientHeight - m_site.LineHeight, 0, m_site.ClientHeight),
				false, new Point(0, 0), false);

			bool visible = m_site.IsSelectionVisible(m_selection, true);
			Assert.IsFalse(visible, "Selection should not be visible if flag is set");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// IP is visible in the middle of window when the flag to have at least one line of
		/// text is set to true, so result should be true
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPVisibleWhenFlagIsSetMiddleWindow()
		{
			SetLocation(new Rect(0, 50, 0, 50 + m_site.LineHeight), false, new Point(0, 0), false);

			bool visible = m_site.IsSelectionVisible(m_selection, true);
			Assert.IsTrue(visible, "Selection should be visible if in the middle of the window");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// IP is below client window, so result should be false
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPBelowWindow()
		{
			SetLocation(new Rect(0, 5000, 0, 5000 + m_site.LineHeight), false, new Point(0, 0), false);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// IP is almost completely below client window, so result should be false
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAlmostBelowWindow()
		{
			SetLocation(new Rect(0, 5000, 0, 5000 + m_site.LineHeight), false,
				new Point(0, 5001), false);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// IP is partly completely below client window, so result should be false
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPPartlyBelowWindow()
		{
			SetLocation(new Rect(0, 5000, 0, 5000 + m_site.LineHeight), false,
				new Point(0, 4999 + m_site.LineHeight), false);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// IP is above client window, so result should be false
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAboveWindow()
		{
			SetLocation(new Rect(0, 1000, 0, 1000 + m_site.LineHeight), false, new Point(0, 2000), false);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// IP is partly above client window, so result should be false
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPPartlyAboveWindow()
		{
			SetLocation(new Rect(0, 1000, 0, 1000 + m_site.LineHeight), false,
				new Point(0, 1001), false);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// IP is almost entirely above client window, so result should be false
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAlmostAboveWindow()
		{
			SetLocation(new Rect(0, 1000, 0, 1000 + m_site.LineHeight), false,
				new Point(0, 999 + m_site.LineHeight), false);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We have a range m_selection that is entirely visible, so result should be true
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelAllVisible()
		{
			SetLocation(new Rect(30, 1020, 60, 1100), false, new Point(0, 1000), true);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsTrue(visible, "Selection should be all visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We have a range m_selection that is completely outside of the client window, so result
		/// should be false
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelAllNotVisible()
		{
			SetLocation(new Rect(30, 1020, 60, 1100), false, new Point(0, 400), true);

			m_site.ScrollMinSize = new Size(m_site.ClientRectangle.Width, 10000);
			m_site.ScrollPosition = new Point(0, 400);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We have a range m_selection that is completely outside of the client window, so result
		/// should be false. This tests when the end is before the anchor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelAllNotVisibleEndBeforeAnchor()
		{
			SetLocation(new Rect(30, 1020, 60, 1100), true, new Point(0, 400), true);

			m_site.ScrollMinSize = new Size(m_site.ClientRectangle.Width, 10000);
			m_site.ScrollPosition = new Point(0, 400);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We have a range m_selection where the anchor is above the client window, the end
		/// is inside of the client window. Result should be true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelAnchorAboveWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), false, new Point(0, 1000), true);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsTrue(visible, "Selection should be considered visible if end is showing");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We have a range m_selection where the end is above the client window, the anchor is
		/// inside of the client window. Result should be false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelEndAboveWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), true, new Point(0, 1000), true);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be considered visible if end is not showing");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We have a range m_selection where the anchor is below the client window, the end is
		/// inside of the client window. Result should be true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelAnchorBelowWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), true, new Point(0, 850), true);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsTrue(visible, "Selection should be considered visible if end is showing");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We have a range m_selection where the end is below the client window, the anchor is
		/// inside of the client window. Result should be false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelEndBelowWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), false, new Point(0, 850), true);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be considered visible if end is not showing");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We have a range m_selection where the anchor is above the client window, the end is
		/// below of the client window. Result should be false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelBothOutsideWindow()
		{
			SetLocation(new Rect(30, 900, 60, 1300), false, new Point(0, 1000), true);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be considered visible if end is not showing");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We have a range m_selection where the anchor is below the client window, the end is
		/// partially inside of the client window. Result should be false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelEndAlmostBelowWindowAnchorBelow()
		{
			SetLocation(new Rect(30, 900, 60, 1100), true, new Point(0, 901 - m_site.ClientHeight),
				true);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be considered visible if end is not completely showing");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We have a range m_selection where the anchor is above the client window, the end is
		/// partially inside of the client window. Result should be false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RangeSelEndAlmostAboveWindowAnchorAbove()
		{
			SetLocation(new Rect(30, 900, 60, 1100), false, new Point(0, 1099),
				true);

			bool visible = m_site.IsSelectionVisible(m_selection);
			Assert.IsFalse(visible, "Selection should not be considered visible if end is not completely showing");
		}
	}
	#endregion IsSelectionVisibleTests
}
