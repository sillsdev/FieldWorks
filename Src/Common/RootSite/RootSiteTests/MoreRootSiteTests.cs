// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2003' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MoreRootSiteTests.cs
// Responsibility: Eberhard Beilharz
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using NMock;
using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;


namespace SIL.FieldWorks.Common.RootSites
{
	#region Tests with real cache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// More unit tests for <see cref="RootSite">RootSite</see> that use
	/// <see cref="DummyBasicView">DummyBasicView</see> view to perform tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MoreRootSiteTestsRealCache : RootsiteBasicViewTestsBaseRealCache
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RootSite.UpdateScrollRange method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateScrollRange()
		{
			CheckDisposed();

			ShowForm();
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			m_basicView.GetCoordRects(out rcSrcRoot, out rcDstRoot);
			int currentHeight = m_basicView.RootBox.Height;
			// Initially we have 2 expanded boxes with 6 lines each, and 2 lazy boxes with
			// 2 fragments each
			int expectedHeight = 2 * (6 * m_basicView.SelectionHeight
				+ DummyBasicViewVc.kMarginTop * rcSrcRoot.Height / DummyBasicViewVc.kdzmpInch)
				+ 2 * (2 * DummyBasicViewVc.kEstimatedParaHeight * rcSrcRoot.Height / 72);
			Assert.AreEqual(expectedHeight, currentHeight, "Unexpected initial height");

			m_basicView.ScrollToEnd();
			currentHeight = m_basicView.RootBox.Height;
			// we have 4 paragraphs with 6 lines each, and a margin before each paragraph
			expectedHeight = 4 * (6 * m_basicView.SelectionHeight
				+ DummyBasicViewVc.kMarginTop * rcSrcRoot.Height / DummyBasicViewVc.kdzmpInch);
			Assert.AreEqual(expectedHeight, currentHeight, "Unexpected height after scrolling");

			// Determine width of one line, so that we can make the window smaller.
			m_basicView.ScrollToTop();
			SelectionHelper selHelper = SelectionHelper.Create(m_basicView);
			selHelper.IchEnd = DummyBasicView.kFirstParaEng.Length;
			selHelper.SetSelection(true);
			// Set Form width to 3/4 of line width, so that we have twice the number of lines
			int nNewWidth = m_basicView.SelectionWidth * 3 / 4;
			m_basicView.Width = nNewWidth;
			Application.DoEvents();

			selHelper.IchEnd = 0;
			selHelper.SetSelection(true);
			currentHeight = m_basicView.RootBox.Height;
			// we have 4 paragraphs with 12 lines each, and a margin before each paragraph
			expectedHeight = 4 * (12 * m_basicView.SelectionHeight
				+ DummyBasicViewVc.kMarginTop * rcSrcRoot.Height / DummyBasicViewVc.kdzmpInch);
			Assert.AreEqual(expectedHeight, currentHeight, "Unexpected height after resizing");
		}
	}

	#endregion

	#region Tests with mocked cache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// More unit tests for <see cref="RootSite">RootSite</see> that use
	/// <see cref="DummyBasicView">DummyBasicView</see> view to perform tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MoreRootSiteTests : RootsiteBasicViewTestsBase
	{
		#region Misc tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IPLocation property to make sure it returns a point that's in the root
		/// site's client rectangle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPLocationTest()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);

			Point pt = m_basicView.IPLocation;
			Assert.IsTrue(m_basicView.ClientRectangle.Contains(pt),
				"IP is not in Draft View's client area.");

			Assert.IsFalse(pt == new Point(0, 0), "IP is at 0, 0");
		}

		#endregion

		#region Scroll range tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests RootSite's ScrollToEnd() function
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void AnotherScrollToEnd()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);
			m_basicView.ScrollToEnd();

			Point pt = m_basicView.ScrollPosition;
			Rectangle rect = m_basicView.DisplayRectangle;
			Assert.AreEqual(-pt.Y + m_basicView.ClientRectangle.Height, rect.Height,
				"Scroll position is not at the very end");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the RootSite.AdjustScrollRange1 method for the vertical scroll bar where the
		/// thumb position is at the top.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustScrollRange_VScroll_PosAtTop()
		{
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);
			DummyBasicView view = m_basicView;
			int dydWindheight = view.ClientRectangle.Height;
			view.SkipLayout = true;
			view.HScroll = false;

			// 1. Test: Thumb position is at top
			view.ScrollPosition = new Point(0, 0);
			int nPos = -view.ScrollPosition.Y;
			int nHeight = view.DisplayRectangle.Height;

			bool fRet = view.AdjustScrollRange(0, 0, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "1. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "1. test");
			Assert.IsFalse(fRet, "1. test");

			fRet = view.AdjustScrollRange(0, 0, 0, 10);
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "1. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "1. test");
			Assert.IsFalse(fRet, "1. test");

			fRet = view.AdjustScrollRange(0, 0, 30, 0);
			// JohnT: AdjustScrollRange now adjust the view height to the current actual height;
			// it doesn't just add the increment if the contents haven't really changed.
			// Review TE team (JohnT): should this test be enhanced to actually resize some
			// internal box?
			//nHeight += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "1. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "1. test");
			Assert.IsFalse(fRet, "1. test");

			fRet = view.AdjustScrollRange(0, 0, -30, 0);
			//nHeight -= 30; // JohnT: see above.
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "1. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "1. test");
			Assert.IsFalse(fRet, "1. test");

			fRet = view.AdjustScrollRange(0, 0, 30, 10);
			//nHeight += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "1. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "1. test");
			Assert.IsFalse(fRet, "1. test");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the RootSite.AdjustScrollRange1 method for the vertical scroll bar when the
		/// thumb positioin is somewhere in the middle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustScrollRange_VScroll_PosInMiddle()
		{
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);
			DummyBasicView view = m_basicView;
			int dydWindheight = view.ClientRectangle.Height;
			view.SkipLayout = true;
			view.HScroll = false;

			// Thumb position is somewhere in the middle
			view.ScrollPosition = new Point(0, 100);
			int nPos = -view.ScrollPosition.Y;
			int nHeight = view.DisplayRectangle.Height;

			bool fRet = view.AdjustScrollRange(0, 0, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "2. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(0, 0, 0, 10);
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "2. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(0, 0, 30, 0);
			//nHeight += 30; // JohnT: see above
			nPos += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "2. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(0, 0, -30, 0);
			//nHeight -= 30; // JohnT: see above
			nPos -= 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "2. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(0, 0, 30, nPos - 1);
			//nHeight += 30;
			nPos += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "2. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(0, 0, 30, nPos);
			//nHeight += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "2. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(0, 0, 30, nPos + 1);
			//nHeight += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "2. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "2. test");
			Assert.IsFalse(fRet, "2. test");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the RootSite.AdjustScrollRange1 method for the vertical scroll bar when the
		/// thumb position is almost at the end.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustScrollRange_VScroll_PosAlmostAtEnd()
		{
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);
			DummyBasicView view = m_basicView;
			int dydWindheight = view.ClientRectangle.Height;
			view.SkipLayout = true;
			view.HScroll = false;

			// The largest scroll position allowed (one screen less than the total content height).
			int maxScrollPos = view.DisplayRectangle.Height - dydWindheight;

			// Thumb position is almost at the end
			view.ScrollPosition = new Point(0, maxScrollPos - 15);
			int nPos = -view.ScrollPosition.Y;
			int nHeight = view.DisplayRectangle.Height;

			bool fRet = view.AdjustScrollRange(0, 0, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(0, 0, 0, 10);
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(0, 0, 30, 0);
			//nHeight += 30;
			// nPos += 30;
			nPos = maxScrollPos; // JohnT: since we didn't really increase the range, the position can't be more than this.
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsTrue(fRet, "3. test"); // JohnT: because scroll pos change was impossible

			fRet = view.AdjustScrollRange(0, 0, -30, 0);
			//nHeight -= 30;
			nPos -= 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(0, 0, 30, nPos - 1);
			//nHeight += 30;
			nPos += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(0, 0, 30, nPos);
			//nHeight += 30;
			// JohnT: originally, I think, meant to test that it won't increase scroll position
			// if the fourth argument is large enough. Now, however, it won't anyway because
			// it's already at max for the fixed view size.
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(0, 0, dydWindheight + 30, 0);
			//nHeight += dydWindheight + 30;
			// nPos += dydWindheight + 30; //JohnT: can't exceed height.
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsTrue(fRet, "3. test"); // JohnT; because adjust scroll pos suppressed.

			fRet = view.AdjustScrollRange(0, 0, -(dydWindheight + 30), 0);
			//nHeight -= dydWindheight + 30;
			nPos = 0; // nPos -= dydWindheight + 30; // JohnT: also can't be less than zero.
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsTrue(fRet, "3. test"); // JohnT: scroll position impossible

			fRet = view.AdjustScrollRange(0, 0, dydWindheight + 30, nPos - 1);
			//nHeight += dydWindheight + 30;
			nPos = maxScrollPos; // nPos += dydWindheight + 30; // JohnT: can't exceed max.
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsTrue(fRet, "3. test");

			fRet = view.AdjustScrollRange(0, 0, dydWindheight + 30, nPos);
			//nHeight += dydWindheight + 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsFalse(fRet, "3. test");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the RootSite.AdjustScrollRange1 method for the vertical scroll bar when the
		/// thumb position is at the end.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustScrollRange_VScroll_PosAtEnd()
		{
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);
			DummyBasicView view = m_basicView;
			int dydWindheight = view.ClientRectangle.Height;
			view.SkipLayout = true;
			view.HScroll = false;

			// The largest scroll position allowed (one screen less than the total content height).
			int maxScrollPos = view.DisplayRectangle.Height - dydWindheight;

			// Thumb position is at the end
			view.ScrollPosition = new Point(0, maxScrollPos);
			int nPos = -view.ScrollPosition.Y;
			int nHeight = view.DisplayRectangle.Height;

			bool fRet = view.AdjustScrollRange(0, 0, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "4. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "4. test");
			Assert.IsFalse(fRet, "4. test");

			fRet = view.AdjustScrollRange(0, 0, 0, 10);
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "4. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "4. test");
			Assert.IsFalse(fRet, "4. test");

			fRet = view.AdjustScrollRange(0, 0, 30, 0);
			//nHeight += 30;
			// nPos += 30; // JohnT: can't exceed max
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "4. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "4. test");
			Assert.IsTrue(fRet, "4. test");

			fRet = view.AdjustScrollRange(0, 0, -30, 0);
			//nHeight -= 30;
			nPos -= 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "4. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "4. test");
			Assert.IsFalse(fRet, "4. test");

			fRet = view.AdjustScrollRange(0, 0, 30, nPos - 1);
			//nHeight += 30;
			nPos += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "4. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "4. test");
			Assert.IsFalse(fRet, "4. test");

			fRet = view.AdjustScrollRange(0, 0, 30, nPos);
			//nHeight += 30;
			// JohnT: again increase is blocked by max as well as intended limit.
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "4. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "4. test");
			Assert.IsFalse(fRet, "4. test");

			fRet = view.AdjustScrollRange(0, 0, dydWindheight + 30, nPos);
			//nHeight += dydWindheight + 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "4. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "4. test");
			Assert.IsFalse(fRet, "4. test");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the RootSite.AdjustScrollRange1 method for the vertical scroll bar when the
		/// scroll range is less than the window height.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustScrollRange_VScroll_ScrollRangeLessThanClientRectangle()
		{
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);
			DummyBasicView view = m_basicView;
			int dydWindheight = view.ClientRectangle.Height;
			view.SkipLayout = true;
			view.HScroll = false;

			// The largest scroll position allowed (one screen less than the total content height).
			int maxScrollPos = view.DisplayRectangle.Height - dydWindheight;

			// Now test scroll range < ClientRectangle
			int dydSomewhere = maxScrollPos;
			view.ScrollPosition = new Point(0, maxScrollPos);
			int nChange = maxScrollPos;
			int nHeight = view.DisplayRectangle.Height;

			bool fRet = view.AdjustScrollRange(0, 0, -nChange, 0);
			int nPos = 0;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "5. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "5. test");
			Assert.IsFalse(fRet, "5. test: scroll position not forced to change"); // JohnT: no problem since window didn't shrink.
			Assert.IsTrue(view.VScroll, "5. test: scrollbar still visible"); // JohnT: we don't change the range.

			RestorePreviousYScrollRange(nChange, dydSomewhere);
			nChange = view.DisplayRectangle.Height - dydWindheight;

			fRet = view.AdjustScrollRange(0, 0, -nChange, 0);
			//nHeight = dydWindheight;
			nPos = 0;
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "5. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "5. test");
			// JohnT: fiddled with next two lines because height does not change.
			Assert.IsFalse(fRet, "5. test: scroll position has not changed");
			Assert.IsTrue(view.VScroll, "5. test: scrollbar still visible");

			RestorePreviousYScrollRange(nChange, maxScrollPos);
			nChange = view.DisplayRectangle.Height - dydWindheight / 2;

			fRet = view.AdjustScrollRange(0, 0, -nChange, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "5. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "5. test");
			Assert.IsTrue(fRet, "5. test: scroll position has not changed");
			Assert.IsTrue(view.VScroll, "5. test: scrollbar still visible"); // JohnT: no change to height.

			RestorePreviousYScrollRange(nChange, dydSomewhere);
			nChange = view.DisplayRectangle.Height - dydWindheight / 2;

			fRet = view.AdjustScrollRange(0, 0, -nChange, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "5. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "5. test");
			Assert.IsTrue(fRet, "5. test: scroll position has not changed");
			Assert.IsTrue(view.VScroll, "5. test: scrollbar still visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the RootSite.AdjustScrollRange1 method for the horizontal scroll bar
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustScrollRangeTestHScroll()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);
			DummyBasicView view = m_basicView;
			int dxdWindwidth = view.ClientRectangle.Width;
			view.SkipLayout = true;
			view.HScroll = true;
			view.AutoScrollMinSize = new Size(dxdWindwidth, view.AutoScrollMinSize.Height);

			// 1. Test: Thumb position is at left
			view.ScrollPosition = new Point(0, 0);
			int nPos = -view.ScrollPosition.X;
			int nWidth = view.DisplayRectangle.Width;

			bool fRet = view.AdjustScrollRange(0, 0, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "1. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "1. test");
			Assert.IsFalse(fRet, "1. test");
			Assert.IsFalse(view.HScroll, "1. test: Scrollbar still visible");

			view.HScroll = true;
			fRet = view.AdjustScrollRange(0, 10, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "1. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "1. test");
			Assert.IsFalse(fRet, "1. test");
			Assert.IsFalse(view.HScroll, "1. test: Scrollbar still visible");

			view.HScroll = true;
			fRet = view.AdjustScrollRange(2 * dxdWindwidth, 0, 0, 0);
			nWidth += 2 * dxdWindwidth;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "1. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "1. test");
			Assert.IsFalse(fRet, "1. test");
			Assert.IsTrue(view.HScroll, "1. test: Scrollbar not visible");

			fRet = view.AdjustScrollRange(-30, 0, 0, 0);
			nWidth -= 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "1. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "1. test");
			Assert.IsFalse(fRet, "1. test");

			fRet = view.AdjustScrollRange(30, 10, 0, 0);
			nWidth += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "1. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "1. test");
			Assert.IsFalse(fRet, "1. test");

			// Thumb position is somewhere in the middle
			view.ScrollPosition = new Point(100, 0);
			nPos = -view.ScrollPosition.X;
			nWidth = view.DisplayRectangle.Width;

			fRet = view.AdjustScrollRange(0, 0, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "2. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(0, 10, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "2. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(30, 0, 0, 0);
			nWidth += 30;
			nPos += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "2. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(-30, 0, 0, 0);
			nWidth -= 30;
			nPos -= 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "2. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(30, nPos - 1, 0, 0);
			nWidth += 30;
			nPos += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "2. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(30, nPos, 0, 0);
			nWidth += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "2. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "2. test");
			Assert.IsFalse(fRet, "2. test");

			fRet = view.AdjustScrollRange(30, nPos + 1, 0, 0);
			nWidth += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "2. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "2. test");
			Assert.IsFalse(fRet, "2. test");

			int scrollMax = view.DisplayRectangle.Width - dxdWindwidth;

			// Thumb position is almost at the far right
			view.ScrollPosition = new Point(scrollMax - 15, 0);
			nPos = -view.ScrollPosition.X;
			nWidth = view.DisplayRectangle.Width;

			fRet = view.AdjustScrollRange(0, 0, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "3. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(0, 10, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "3. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(30, 0, 0, 0);
			nWidth += 30;
			nPos += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "3. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(-30, 0, 0, 0);
			nWidth -= 30;
			nPos -= 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "3. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(30, nPos - 1, 0, 0);
			nWidth += 30;
			nPos += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "3. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(30, nPos, 0, 0);
			nWidth += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "3. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(dxdWindwidth + 30, 0, 0, 0);
			nWidth += dxdWindwidth + 30;
			nPos += dxdWindwidth + 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "3. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(- (dxdWindwidth + 30), 0, 0, 0);
			nWidth -= dxdWindwidth + 30;
			nPos -= dxdWindwidth + 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "3. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(dxdWindwidth + 30, nPos - 1, 0, 0);
			nWidth += dxdWindwidth + 30;
			nPos += dxdWindwidth + 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "3. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(dxdWindwidth + 30, nPos, 0, 0);
			nWidth += dxdWindwidth + 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "3. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "3. test");
			Assert.IsFalse(fRet, "3. test");

			// Thumb position is at the far right
			view.ScrollPosition = new Point(scrollMax, 0);
			nPos = -view.ScrollPosition.X;
			nWidth = view.DisplayRectangle.Width;

			fRet = view.AdjustScrollRange(0, 0, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "4. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "4. test");
			Assert.IsFalse(fRet, "4. test");

			fRet = view.AdjustScrollRange(0, 10, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "4. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "4. test");
			Assert.IsFalse(fRet, "4. test");

			fRet = view.AdjustScrollRange(30, 0, 0, 0);
			nWidth += 30;
			nPos += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "4. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "4. test");
			Assert.IsFalse(fRet, "4. test");

			fRet = view.AdjustScrollRange(-30, 0, 0, 0);
			nWidth -= 30;
			nPos -= 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "4. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "4. test");
			Assert.IsFalse(fRet, "4. test");

			fRet = view.AdjustScrollRange(30, nPos - 1, 0, 0);
			nWidth += 30;
			nPos += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "4. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "4. test");
			Assert.IsFalse(fRet, "4. test");

			fRet = view.AdjustScrollRange(30, nPos, 0, 0);
			nWidth += 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "4. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "4. test");
			Assert.IsFalse(fRet, "4. test");

			fRet = view.AdjustScrollRange(dxdWindwidth + 30, nPos, 0, 0);
			nWidth += dxdWindwidth + 30;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "4. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "4. test");
			Assert.IsFalse(fRet, "4. test");

			// Now test scroll range < ClientRectangle
			int dxdSomewhere = nPos;
			view.ScrollPosition = new Point(scrollMax, 0);
			int nChange = view.DisplayRectangle.Width - dxdWindwidth;
			fRet = view.AdjustScrollRange(-nChange, 0, 0, 0);
			nWidth = dxdWindwidth;
			nPos = 0;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "5. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "5. test");
			Assert.IsTrue(fRet,"5. test: scroll position forced to change");
			Assert.IsFalse(view.HScroll, "5. test: scrollbar still visible");

			RestorePreviousXScrollRange(nChange, dxdSomewhere);
			nChange = view.DisplayRectangle.Width - dxdWindwidth;

			fRet = view.AdjustScrollRange(-nChange, 0, 0, 0);
			nWidth = dxdWindwidth;
			nPos = 0;
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "5. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "5. test");
			Assert.IsTrue(fRet,"5. test: scroll position has not changed");
			Assert.IsFalse(view.HScroll, "5. test: scrollbar still visible");

			RestorePreviousXScrollRange(nChange, view.DisplayRectangle.Width);
			nChange = view.DisplayRectangle.Width - dxdWindwidth / 2;

			fRet = view.AdjustScrollRange(-nChange, 0, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "5. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "5. test");
			Assert.IsTrue(fRet,"5. test: scroll position has not changed");
			Assert.IsFalse(view.HScroll, "5. test: scrollbar still visible");

			RestorePreviousXScrollRange(nChange, dxdSomewhere);
			nChange = view.DisplayRectangle.Width - dxdWindwidth / 2;

			fRet = view.AdjustScrollRange(-nChange, 0, 0, 0);
			Assert.AreEqual(nPos, -view.ScrollPosition.X, "5. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "5. test");
			Assert.IsTrue(fRet,"5. test: scroll position has not changed");
			Assert.IsFalse(view.HScroll, "5. test: scrollbar still visible");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the RootSite.AdjustScrollRange1 method for simultanious use of the horizontal
		/// and vertical scroll bar
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustScrollRangeTestHVScroll()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);
			DummyBasicView view = m_basicView;
			int dxdWindwidth = view.ClientRectangle.Width;
			view.SkipLayout = true;
			view.HScroll = true;

			view.ScrollMinSize = new Size(2 * dxdWindwidth, view.AutoScrollMinSize.Height);

			int maxYScroll = view.DisplayRectangle.Height - view.ClientRectangle.Height;
			int maxXScroll = view.DisplayRectangle.Width - view.ClientRectangle.Width;

			// 1. Test: Thumb position is at bottom left
			view.ScrollPosition = new Point(10, maxYScroll);
			int nXPos = -view.ScrollPosition.X;
			int nYPos = -view.ScrollPosition.Y;
			int nWidth = view.DisplayRectangle.Width;
			int nHeight = view.DisplayRectangle.Height;

			bool fRet = view.AdjustScrollRange(30, 0, -40, 0);
			nWidth += 30;
			nXPos += 30;
			//nHeight -= 40; JohnT: height doesn't really change.
			nYPos -= 40;
			Assert.AreEqual(nXPos, -view.ScrollPosition.X, "1. test");
			Assert.AreEqual(nYPos, -view.ScrollPosition.Y, "1. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "1. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "1. test");
			Assert.IsFalse(fRet, "1. test");
			Assert.IsTrue(view.HScroll, "1. test: Scrollbar not visible");

			// 2. Test: Thumb position is at top right
			view.ScrollPosition = new Point(maxXScroll, 10);
			nXPos = -view.ScrollPosition.X;
			nYPos = -view.ScrollPosition.Y;
			fRet = view.AdjustScrollRange(-30, 0, 40, 0);
			nWidth -= 30;
			nXPos -= 30;
			//nHeight += 40;
			nYPos += 40;
			Assert.AreEqual(nXPos, -view.ScrollPosition.X, "2. test");
			Assert.AreEqual(nYPos, -view.ScrollPosition.Y, "2. test");
			Assert.AreEqual(nWidth, view.DisplayRectangle.Width, "2. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "2. test");
			Assert.IsFalse(fRet, "2. test");
			Assert.IsTrue(view.HScroll, "2. test: Scrollbar not visible");
		}
		#endregion

		#region Para props tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the IsParagraphProps method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsParagraphProps_Basic()
		{
			CheckDisposed();

			// we want more paragraphs with different Hvos
			MakeEnglishParagraphs();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);
			IVwRootBox rootBox = m_basicView.RootBox;
			IVwSelection vwsel;
			int hvoText, tagText, ihvoAnchor, ihvoEnd;
			IVwPropertyStore[] vqvps;

			// Test 1: selection in one paragraph
			rootBox.MakeSimpleSel(false, true, false, true);
			IVwSelection sel = rootBox.Selection;
			ITsString tss;
			int ich, hvoObj, tag, ws;
			bool fAssocPrev;
			sel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out tag, out ws);
			int clev = sel.CLevels(true);
			clev--; // result it returns is one more than what the AllTextSelInfo routine wants.
			int ihvoRoot;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ihvoEnd1;
			ITsTextProps ttp;
			using (ArrayPtr rgvsliTemp = MarshalEx.ArrayToNative(clev, typeof(SelLevInfo)))
			{
				sel.AllTextSelInfo(out ihvoRoot, clev, rgvsliTemp, out tag, out cpropPrevious,
					out ichAnchor, out ichEnd, out ws, out fAssocPrev, out ihvoEnd1, out ttp);
				SelLevInfo[] rgvsli = (SelLevInfo[])MarshalEx.NativeToArray(rgvsliTemp, clev,
					typeof(SelLevInfo));
				int ichInsert = 0;
				rootBox.MakeTextSelection(ihvoRoot, clev, rgvsli, tag, cpropPrevious, ichInsert,
					ichInsert + 5, ws, fAssocPrev, ihvoEnd1, ttp, true);

				bool fRet = m_basicView.IsParagraphProps(out vwsel, out hvoText, out tagText,
					out vqvps, out ihvoAnchor, out ihvoEnd);

				Assert.AreEqual(true, fRet, "1. test:");
				Assert.AreEqual(ihvoAnchor, ihvoEnd, "1. test:");

				// Test 2: selection across two sections
				SelLevInfo[] rgvsliEnd = new SelLevInfo[clev];
				rgvsli.CopyTo(rgvsliEnd, 0);
				rgvsli[0].ihvo = 0; // first paragraph
				rgvsli[clev-1].ihvo = 2; // third section
				rootBox.MakeTextSelInObj(ihvoRoot, clev, rgvsli, clev, rgvsliEnd, false, true, true, true,
					true);

				fRet = m_basicView.IsParagraphProps(out vwsel, out hvoText, out tagText,
					out vqvps, out ihvoAnchor, out ihvoEnd);

				Assert.AreEqual(false, fRet, "2. test:");
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the IsParagraphProps method when the selection crosses paragraph boundary
		/// and selection levels differ.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void IsParagraphProps_WholeFootnoteParaSelected()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kBookFootnoteDetailsSinglePara);

			IVwRootBox rootBox = m_basicView.RootBox;
			IVwSelection vwsel;
			int hvoText, tagText, ihvoAnchor, ihvoEnd;
			IVwPropertyStore[] vqvps;

			IVwSelection selAnchor = rootBox.MakeSimpleSel(true, false, false, true);
			m_basicView.CallOnKeyDown(new KeyEventArgs(Keys.End));
			IVwSelection selEnd = rootBox.Selection;
			rootBox.MakeRangeSelection(selAnchor, selEnd, true);

			Assert.IsTrue(m_basicView.IsParagraphProps(out vwsel, out hvoText, out tagText,
				out vqvps, out ihvoAnchor, out ihvoEnd));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the RootSite.GetParagraphProps method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParagraphProps()
		{
			CheckDisposed();

			// we want more paragraphs with different Hvos
			MakeEnglishParagraphs();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);
			IVwRootBox rootBox = m_basicView.RootBox;
			IVwSelection vwsel;
			int hvoText, tagText, ihvoFirst, ihvoLast;
			IVwPropertyStore[] vqvps;
			ITsTextProps[] vqttp;

			// Test 1: selection in one paragraph
			rootBox.MakeSimpleSel(false, true, false, true);
			IVwSelection sel = rootBox.Selection;
			ITsString tss;
			int ich, hvoObj, tag, ws;
			bool fAssocPrev;
			sel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out tag, out ws);
			int clev = sel.CLevels(true);
			clev--; // result it returns is one more than what the AllTextSelInfo routine wants.
			int ihvoRoot;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ihvoEnd1;
			ITsTextProps ttp;
			using (ArrayPtr rgvsliTemp = MarshalEx.ArrayToNative(clev, typeof(SelLevInfo)))
			{
				sel.AllTextSelInfo(out ihvoRoot, clev, rgvsliTemp, out tag, out cpropPrevious,
					out ichAnchor, out ichEnd, out ws, out fAssocPrev, out ihvoEnd1, out ttp);
				SelLevInfo[] rgvsli = (SelLevInfo[])MarshalEx.NativeToArray(rgvsliTemp, clev,
					typeof(SelLevInfo));
				int ichInsert = 0;
				rootBox.MakeTextSelection(ihvoRoot, clev, rgvsli, tag, cpropPrevious, ichInsert,
					ichInsert + 5, ws, fAssocPrev, ihvoEnd1, ttp, true);

				bool fRet = m_basicView.GetParagraphProps(out vwsel, out hvoText, out tagText,
					out vqvps, out ihvoFirst, out ihvoLast, out vqttp);

				Assert.IsTrue(fRet, "Test 1 ");
				Assert.AreEqual(ihvoFirst, ihvoLast, "Test 1 ");
				Assert.AreEqual(1, vqttp.Length, "Test 1 ");

				// Test 2: selection across two sections
				SelLevInfo[] rgvsliEnd = new SelLevInfo[clev];
				rgvsli.CopyTo(rgvsliEnd, 0);
				rgvsli[0].ihvo = 0; // first paragraph
				rgvsli[clev-1].ihvo = 2; // third section
				rootBox.MakeTextSelInObj(ihvoRoot, clev, rgvsli, clev, rgvsliEnd, false, true, true, true,
					true);

				fRet = m_basicView.GetParagraphProps(out vwsel, out hvoText, out tagText,
					out vqvps, out ihvoFirst, out ihvoLast, out vqttp);

				Assert.IsFalse(fRet, "Test 2 ");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the RootSite.GetParagraphProps method when the selection is in a picture
		/// caption.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParagraphProps_InPictureCaption()
		{
			CheckDisposed();
			ITsStrFactory factory = TsStrFactoryClass.Create();
			CmPicture pict = new CmPicture(Cache, "c:\\junk.jpg",
				factory.MakeString("Test picture", Cache.DefaultVernWs),
				StringUtils.LocalPictures);
			Assert.IsNotNull(pict);
			//CmPicture pic = new CmPicture();
			//ICmFolder folder = new CmFolder();
			//Cache.LangProject.PicturesOC.Add(folder);

			//m_basicView.EditingHelper.MakePictureFromText(Cache,
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);
			DynamicMock mockedSelection = new DynamicMock(typeof(IVwSelection));
			mockedSelection.ExpectAndReturn("IsValid", true);
			mockedSelection.ExpectAndReturn("Commit", true);
			VwChangeInfo changeInfo = new VwChangeInfo();
			changeInfo.hvo = 0;
			mockedSelection.ExpectAndReturn("CompleteEdits", true, new object[] {changeInfo},
				new string[] {typeof(VwChangeInfo).FullName + "&"}, new object[] {changeInfo});
			mockedSelection.ExpectAndReturn("CLevels", 2, false);
			mockedSelection.ExpectAndReturn("CLevels", 2, true);
			string sIntType = typeof(int).FullName;
			string intRef = sIntType + "&";
			mockedSelection.ExpectAndReturn("PropInfo", null,
				new object[] { false, 0, null, null, null, null, null },
				new string[] {typeof(bool).FullName, sIntType, intRef, intRef, intRef,
					intRef, typeof(IVwPropertyStore).FullName + "&"},
				new object[] { false, 0, pict.Hvo, (int)CmPicture.CmPictureTags.kflidCaption, 0, 0, null });
			mockedSelection.ExpectAndReturn("PropInfo", null,
				new object[] { true, 0, null, null, null, null, null },
				new string[] {typeof(bool).FullName, sIntType, intRef, intRef, intRef,
					intRef, typeof(IVwPropertyStore).FullName + "&"},
				new object[] { true, 0, pict.Hvo, (int)CmPicture.CmPictureTags.kflidCaption, 0, 0, null });
			mockedSelection.ExpectAndReturn(2, "EndBeforeAnchor", false);

			DummyBasicView.DummyEditingHelper editingHelper =
				(DummyBasicView.DummyEditingHelper)m_basicView.EditingHelper;
			editingHelper.m_mockedSelection = (IVwSelection)mockedSelection.MockInstance;
			editingHelper.m_fOverrideGetParaPropStores = true;

			IVwRootBox rootBox = m_basicView.RootBox;
			IVwSelection vwsel;
			int hvoText, tagText, ihvoFirst, ihvoLast;
			IVwPropertyStore[] vvps;
			ITsTextProps[] vttp;

			Assert.IsTrue(m_basicView.GetParagraphProps(out vwsel, out hvoText, out tagText,
				out vvps, out ihvoFirst, out ihvoLast, out vttp));

			Assert.AreEqual((int)CmPicture.CmPictureTags.kflidCaption, tagText);
			Assert.AreEqual(1, vttp.Length);
			Assert.AreEqual("Figure caption",
				vttp[0].GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
		#endregion

		#region Merge multiple types of translations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translations, free translations and literal translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeTranslationssWhenParasMerge_BothParasHaveThreeTypesofTranslations()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_inMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_inMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);

			// We'll just re-use our "vernacular" WS for the translations, for testing purposes.
			ICmTranslation bt1 = m_inMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(bt1, m_wsEng, "BT1", null);
			ICmTranslation bt2 = m_inMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(bt2, m_wsEng, "BT2", null);

			ICmTranslation free1 = m_inMemoryCache.AddTransToMockedParagraph(para1,
				LangProject.kguidTranFreeTranslation, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(free1, m_wsEng, "Free1", null);
			ICmTranslation free2 = m_inMemoryCache.AddTransToMockedParagraph(para2,
				LangProject.kguidTranFreeTranslation, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(free2, m_wsEng, "Free2", null);

			ICmTranslation lit1 = m_inMemoryCache.AddTransToMockedParagraph(para1,
				LangProject.kguidTranLiteralTranslation, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(lit1, m_wsEng, "Lit1", null);
			ICmTranslation lit2 = m_inMemoryCache.AddTransToMockedParagraph(para2,
				LangProject.kguidTranLiteralTranslation, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(lit2, m_wsEng, "Lit2", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", bt1.Translation.GetAlternative(m_wsEng).Text);
			Assert.AreEqual("Free1 Free2", free1.Translation.GetAlternative(m_wsEng).Text);
			Assert.AreEqual("Lit1 Lit2", lit1.Translation.GetAlternative(m_wsEng).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// translations of the second one even if the first one has no translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeTranslationsWhenParasMerge_FirstParaHasNoTranslations()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_inMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_inMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);

			// We'll just re-use our "vernacular" WS for the translations, for testing purposes.
			ICmTranslation bt2 = m_inMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(bt2, m_wsEng, "BT2", null);

			ICmTranslation free2 = m_inMemoryCache.AddTransToMockedParagraph(para2,
				LangProject.kguidTranFreeTranslation, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(free2, m_wsEng, "Free2", null);

			ICmTranslation lit2 = m_inMemoryCache.AddTransToMockedParagraph(para2,
				LangProject.kguidTranLiteralTranslation, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(lit2, m_wsEng, "Lit2", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			ICmTranslation bt1 = para1.GetBT();
			Assert.AreEqual("BT2", bt1.Translation.GetAlternative(m_wsEng).Text);
			ICmTranslation free1 = para1.GetTrans(LangProject.kguidTranFreeTranslation);
			Assert.AreEqual("Free2", free1.Translation.GetAlternative(m_wsEng).Text);
			ICmTranslation lit1 = para1.GetTrans(LangProject.kguidTranLiteralTranslation);
			Assert.AreEqual("Lit2", lit1.Translation.GetAlternative(m_wsEng).Text);
		}
		#endregion

		#region Merge BT tests - Backspace
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translations, merging them into one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_BothParasHaveBt()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_scrInMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_scrInMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = m_scrInMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_scrInMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.GetAlternative(m_wsEng).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translation of the second one even if the first one has no BT.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_FirstParaHasNoBt()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create a Back Translations on
			// only the second paragraph
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans2 = m_scrInMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_scrInMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			IEnumerator<ICmTranslation> translations = para1.TranslationsOC.GetEnumerator();
			translations.MoveNext();
			ICmTranslation transl = translations.Current;
			Assert.AreEqual("BT2", transl.Translation.GetAlternative(m_wsEng).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translation of the first one even if the second one has no BT.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_SecondParaHasNoBt()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create a Back Translations on
			// only the first paragraph
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_scrInMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_scrInMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1", trans1.Translation.GetAlternative(m_wsEng).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translations, merging them into one when both paragraphs have back translations
		/// in multiple writing systems
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_BothParasHaveBtMultiWs()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_scrInMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_scrInMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);

			int wsfr = m_wsf.GetWsFromStr("fr");
			trans1.Translation.SetAlternative("BT1fr", wsfr);

			ICmTranslation trans2 = m_scrInMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_scrInMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);
			trans2.Translation.SetAlternative("BT2fr", wsfr);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.GetAlternative(m_wsEng).Text);
			Assert.AreEqual("BT1fr BT2fr", trans1.Translation.GetAlternative(wsfr).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translations, merging them into one when the first paragraph has a back
		/// translation in only one writing system, but the second paragraph has back
		/// translations in multiple writing systems
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_FirstParaHasSingleWsBtSecondHasMultiWs()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_scrInMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_scrInMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = m_scrInMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_scrInMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			int wsfr = m_wsf.GetWsFromStr("fr");
			trans2.Translation.SetAlternative("BT2fr", wsfr);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.GetAlternative(m_wsEng).Text);
			Assert.AreEqual("BT2fr", trans1.Translation.GetAlternative(wsfr).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translations, merging them into one when the second paragraph has a back
		/// translation in only one writing system, but the first paragraph has back
		/// translations in multiple writing systems
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_SecondParaHasSingleWsBtFirstHasMultiWs()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_scrInMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_scrInMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = m_scrInMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_scrInMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);
			int wsfr = m_wsf.GetWsFromStr("fr");
			trans1.Translation.SetAlternative("BT1fr", wsfr);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.GetAlternative(m_wsEng).Text);
			Assert.AreEqual("BT1fr", trans1.Translation.GetAlternative(wsfr).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes the second paragraph to get deleted doesn't join
		/// the back translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoMergeBTsWhenSecondParaDeleted_AnchorInSurvivingPara()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the end of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ichEndPara1 = DummyBasicView.kFirstParaEng.Length;
			int ichEndPara2 = DummyBasicView.kSecondParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ichEndPara1,
				ichEndPara2, 0, true, 1, null, true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng, para1.Contents.Text);
			Assert.AreEqual("BT1", trans1.Translation.GetAlternative(m_wsEng).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes the second paragraph to get deleted doesn't join
		/// the back translations. The selection extends from the end of the second paragraph
		/// to the end of the first paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoMergeBTsWhenSecondParaDeleted_AnchorInDyingPara()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the end of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 1;
			int ichEndPara1 = DummyBasicView.kFirstParaEng.Length;
			int ichEndPara2 = DummyBasicView.kSecondParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ichEndPara2,
				ichEndPara1, 0, true, 0, null, true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng, para1.Contents.Text);
			Assert.AreEqual("BT1", trans1.Translation.GetAlternative(m_wsEng).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes the first paragraph to get deleted preserves the
		/// back translation from the second paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PreserveSecondBTWhenFirstParaDeleted()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the start of first paragraph to the start of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, 0, 0,
				true, 1, null, true);
			TypeBackspace();

			// we don't know which paragraph survived, so get it from text
			para1 = (StTxtPara)text1.ParagraphsOS[0];

			Assert.AreEqual(DummyBasicView.kSecondParaEng, para1.Contents.Text);
			IEnumerator<ICmTranslation> translations = para1.TranslationsOC.GetEnumerator();
			translations.MoveNext();
			ICmTranslation transl = translations.Current;
			Assert.AreEqual("BT2", transl.Translation.GetAlternative(m_wsEng).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translations, merging them into one when we have an IP only
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_BothParasHaveBt_IP()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection at the end of first paragraph
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 1;
			//int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, 0, 0,
				true, -1, null, true);
			TypeBackspace();

			// we don't know which paragraph survived, so get it from text
			para1 = (StTxtPara)text1.ParagraphsOS[0];

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.GetAlternative(m_wsEng).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translations, merging them into one. In this test case we have three
		/// paragraphs, the middle paragraph gets deleted. The merged back translations should
		/// consist of those of the first and last paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_ThreeParasWithBt()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "Just some garbage", m_wsEng);
			StTxtPara para3 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para3, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);
			ICmTranslation trans3 = m_inMemoryCache.AddBtToMockedParagraph(para3, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans3, m_wsEng, "BT3", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 2, 0);

			// Make a selection from the end of first paragraph to the beginning of the third.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 2, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT3", trans1.Translation.GetAlternative(m_wsEng).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translations, merging them into one. In this test case we have three
		/// paragraphs, the middle paragraph gets deleted. The merged back translations should
		/// consist of those of the first and last paragraphs. We further complicate things
		/// by having our selection start in the middle of the first para and end in the middle
		/// of the last one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_ThreeParas_FromMiddleOfPara1ToMiddleOfPara2()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "Just some garbage", m_wsEng);
			StTxtPara para3 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para3, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			// extra space intentionally added after BT1 to test TE-4075 (that too many spaces are not added)
			m_inMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1 ", null);
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);
			ICmTranslation trans3 = m_inMemoryCache.AddBtToMockedParagraph(para3, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans3, m_wsEng, "BT3", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 2, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ichAnchor = DummyBasicView.kFirstParaEng.Length - 2;
			int ichEnd = 2;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ichAnchor, ichEnd, 0, true, 2, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng.Substring(0, ichAnchor) +
				DummyBasicView.kSecondParaEng.Substring(ichEnd), para1.Contents.Text);
			Assert.AreEqual("BT1 BT3", trans1.Translation.GetAlternative(m_wsEng).Text);
		}
		#endregion

		#region Merge BT tests - Delete
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that pressing the delete key which causes two paragraphs to get joined also
		/// preserves the back translations, merging them into one.
		/// TE-4075: Make sure that white space is included between joined paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_UseDeleteKey()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			// extra space intentionally added before BT2 to test TE-4075 (that too many spaces are not added)
			m_scrInMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, " BT2", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeDelete();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.GetAlternative(m_wsEng).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translation of the second one even if the first one has no BT.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_FirstParaHasNoBt_DelKey()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create a Back Translations on
			// only the second paragraph
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeDelete();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			IEnumerator<ICmTranslation> translations = para1.TranslationsOC.GetEnumerator();
			translations.MoveNext();
			ICmTranslation transl = translations.Current;
			Assert.AreEqual("BT2", transl.Translation.GetAlternative(m_wsEng).Text);
		}
		#endregion

		#region Merge BT tests - Other key
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that pressing the 'a' key, which causes two paragraphs to get joined, also
		/// preserves the back translations, merging them into one.
		/// TE-4075: Make sure that white space is included between joined paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeBTsWhenParasMerge_FromMiddleOfPara1ToMiddleOfPara2_aKey()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			ScrBook book = new ScrBook(Cache, m_hvoRoot);
			StText text1 = (StText)book.FootnotesOS[0];
			StTxtPara para1 = (StTxtPara)text1.ParagraphsOS[0];
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(text1.Hvo, string.Empty);
			m_scrInMemoryCache.AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para1, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(para2, m_wsEng);
			m_inMemoryCache.AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, text1.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ichAnchor = DummyBasicView.kFirstParaEng.Length - 2;
			int ichEnd = 2;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ichAnchor, ichEnd, 0, true, 1, null,
				true);
			TypeChar('a');

			Assert.AreEqual(DummyBasicView.kFirstParaEng.Substring(0, ichAnchor) + "a" +
				DummyBasicView.kSecondParaEng.Substring(ichEnd), para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.GetAlternative(m_wsEng).Text);
		}
		#endregion

		#region Tests for displaying/hiding selection when losing focus
		// These tests should probably be in SimpleRootSiteTests, but it's easier to do them
		// here.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that losing the focus to a non-view window still displays the selection for a
		/// range selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoseFocusToNonView_RangeSel()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;
			rootBox.Activate(VwSelectionState.vssEnabled);

			// Select the first four characters in the first paragraph
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, 3, 0, true, 0, null, true);

			// We have to set up a form that contains the view and the control that we pretend
			// gets focus.
			using (Form parentForm = new Form())
			using (Control control = new Control())
			{
				m_basicView.Parent = parentForm;
				control.Parent = parentForm;
				// Lets pretend we a non-view gets the focus (although it's the same)
				m_basicView.KillFocus(control);

				Assert.IsTrue(rootBox.Selection.IsEnabled,
					"Selection should still be enabled if non-view window got focus");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that losing the focus to a view window hides the selection for a range
		/// selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoseFocusToView_RangeSel()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;
			rootBox.Activate(VwSelectionState.vssEnabled);

			// Select the first four characters in the first paragraph
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, 3, 0, true, 0, null, true);

			// Lets pretend we a different view gets the focus (although it's the same)
			m_basicView.KillFocus(m_basicView);

			Assert.IsFalse(rootBox.Selection.IsEnabled,
				"Selection should not be enabled if other view window got focus");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that losing the focus to a view window when the ShowRangeSelAfterLostFocus
		/// flag is set, still shows the selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoseFocusToView_RangeSel_FlagSet()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;
			rootBox.Activate(VwSelectionState.vssEnabled);
			m_basicView.ShowRangeSelAfterLostFocus = true;

			// Select the first four characters in the first paragraph
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, 3, 0, true, 0, null, true);

			// Lets pretend we a different view gets the focus (although it's the same)
			m_basicView.KillFocus(m_basicView);

			Assert.IsTrue(rootBox.Selection.IsEnabled,
				"Selection should still be enabled if the ShowRangeSelAfterLostFocus flag is set");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that losing the focus to a non-view window doesn't display the selection for
		/// an IP
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoseFocusToNonView_IP()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;
			rootBox.Activate(VwSelectionState.vssEnabled);

			// Make IP selection at the beginning of the text
			rootBox.MakeSimpleSel(true, true, false, true);

			// We have to set up a form that contains the view and the control that we pretend
			// gets focus.
			using (Form parentForm = new Form())
			using (Control control = new Control())
			{
				m_basicView.Parent = parentForm;
				control.Parent = parentForm;
				// Lets pretend we a non-view gets the focus (although it's the same)
				m_basicView.KillFocus(control);

				Assert.IsFalse(rootBox.Selection.IsEnabled,
					"Selection should not be enabled if non-view window got focus if we have an IP");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that losing the focus to a view window hides the selection (IP)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoseFocusToView_IP()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;
			rootBox.Activate(VwSelectionState.vssEnabled);

			// Make IP selection at the beginning of the text
			rootBox.MakeSimpleSel(true, true, false, true);

			// Lets pretend we a different view gets the focus (although it's the same)
			m_basicView.KillFocus(m_basicView);

			Assert.IsFalse(rootBox.Selection.IsEnabled,
				"Selection should not be enabled if other view window got focus");
		}
		#endregion

		#region helper methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restores the previous scroll range
		/// </summary>
		/// <param name="nChange">What get's added to the current range</param>
		/// <param name="dydScrollPos">The new scroll position</param>
		/// ------------------------------------------------------------------------------------
		private void RestorePreviousYScrollRange(int nChange, int dydScrollPos)
		{
			DummyBasicView view = m_basicView;
			view.VScroll = true;
			view.AdjustScrollRange(0, 0, nChange, 0); // restore previous size
			view.ScrollPosition = new Point(0, dydScrollPos);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restores the previous scroll range
		/// </summary>
		/// <param name="nChange">What get's added to the current range</param>
		/// <param name="dxdScrollPos">The new scroll position</param>
		/// ------------------------------------------------------------------------------------
		private void RestorePreviousXScrollRange(int nChange, int dxdScrollPos)
		{
			DummyBasicView view = m_basicView;
			view.HScroll = true;
			view.AdjustScrollRange(nChange, 0, 0, 0); // restore previous size
			view.ScrollPosition = new Point(dxdScrollPos, 0);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate the user typing a backspace
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void TypeBackspace()
		{
			IVwRootBox rootBox = m_basicView.RootBox;
			IVwGraphics vg = m_basicView.get_ScreenGraphics(rootBox);
			try
			{
				int wsTemp = -1;
				rootBox.OnTyping(vg, string.Empty, 1, 0,
					(char)(int)VwSpecialChars.kscBackspace, ref wsTemp);
			}
			finally
			{
				m_basicView.ReleaseGraphics(rootBox, vg);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate the user typing a delete
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void TypeDelete()
		{
			IVwRootBox rootBox = m_basicView.RootBox;
			IVwGraphics vg = m_basicView.get_ScreenGraphics(rootBox);
			try
			{
				int wsTemp = -1;
				rootBox.OnTyping(vg, string.Empty, 0, 1,
					(char)(int)VwSpecialChars.kscDelForward, ref wsTemp);
			}
			finally
			{
				m_basicView.ReleaseGraphics(rootBox, vg);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate the user typing a character
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void TypeChar(char ch)
		{
			IVwRootBox rootBox = m_basicView.RootBox;
			IVwGraphics vg = m_basicView.get_ScreenGraphics(rootBox);
			try
			{
				int wsTemp = -1;
				rootBox.OnTyping(vg, new string(ch, 1), 0, 0, ch, ref wsTemp);
			}
			finally
			{
				m_basicView.ReleaseGraphics(rootBox, vg);
			}
		}
		#endregion
	}
	#endregion
}
