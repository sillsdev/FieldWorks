// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MoreRootSiteTests.cs
// Responsibility: FW team
// --------------------------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using NMock;
using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.CoreImpl;
using System;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// More unit tests for <see cref="RootSite">RootSite</see> that use
	/// <see cref="DummyBasicView">DummyBasicView</see> view to perform tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MoreRootSiteTests : RootsiteBasicViewTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixture setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			// Because these tests use ScrFootnotes with multiple paragraphs, we need to allow
			// the use.
			ReflectionHelper.SetField(Type.GetType("SIL.FieldWorks.FDO.DomainImpl.ScrFootnote,FDO", true),
				"s_maxAllowedParagraphs", 5);
		}

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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);

			Point pt = m_basicView.IPLocation;
			Assert.IsTrue(m_basicView.ClientRectangle.Contains(pt),
				"IP is not in Draft View's client area.");

			Assert.IsFalse(pt == new Point(0, 0), "IP is at 0, 0");
		}

		#endregion

		#region Scroll range tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RootSite.UpdateScrollRange method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustScrollRange_InResponseToScrollingAndResizing()
		{
			// JohnT: this test is unfortunately very sensitive to the height of the window. Too large, and some of the lazy stuff
			// gets expanded during the initial ShowForm, due to automatically scrolling to show the automatic default selection,
			// which now makes sure one screenful is expanded. Too small, and ScrollToEnd only has to expand the last lazy box rather
			// than both of them. This value works on Windows, but something different might be needed on Linux. Roughly, it should
			// be a little smaller than the first two (non-lazy) paragraphs, but definitely bigger than just one.
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll, 150);
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests RootSite's ScrollToEnd() function
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void AnotherScrollToEnd()
		{
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: This test is too dependent on mono ScrollableControl behaving the sames as .NET")]
		public void AdjustScrollRange_VScroll_PosAtTop()
		{
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);
			DummyBasicView view = m_basicView;
#pragma warning disable 219
			int dydWindheight = view.ClientRectangle.Height;
#pragma warning restore 219
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
#if __MonoCS__
		[Ignore("// TODO-Linux: This test is too dependent on mono ScrollableControl behaving the sames as .NET")]
#endif
		public void AdjustScrollRange_VScroll_PosInMiddle()
		{
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);
			DummyBasicView view = m_basicView;
#pragma warning disable
			int dydWindheight = view.ClientRectangle.Height;
#pragma warning restore
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
#if __MonoCS__
		[Ignore("// TODO-Linux: This test is too dependent on mono ScrollableControl behaving the sames as .NET")]
#endif
		public void AdjustScrollRange_VScroll_PosAlmostAtEnd()
		{
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll, 150);
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

			fRet = view.AdjustScrollRange(0, 0, - (dydWindheight + 30), 0);
			//nHeight -= dydWindheight + 30;
			nPos = Math.Max(0, nPos - dydWindheight - 30); // JohnT: also can't be less than zero.
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsFalse(fRet, "3. test");

			fRet = view.AdjustScrollRange(0, 0, dydWindheight + 30, nPos - 1);
			//nHeight += dydWindheight + 30;
			nPos = maxScrollPos; // nPos += dydWindheight + 30; // JohnT: can't exceed max.
			Assert.AreEqual(nPos, -view.ScrollPosition.Y, "3. test");
			Assert.AreEqual(nHeight, view.DisplayRectangle.Height, "3. test");
			Assert.IsFalse(fRet, "3. test");

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
#if __MonoCS__
		[Ignore("// TODO-Linux: This test is too dependent on mono ScrollableControl behaving the sames as .NET")]
#endif
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
#if __MonoCS__
		[Ignore("// TODO-Linux: This test is too dependent on mono ScrollableControl behaving the sames as .NET")]
#endif
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: Test Too Dependent DisplayRectangle being updated by mono the same ways as .NET")]
		public void AdjustScrollRangeTestHScroll()
		{
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: Test Too Dependent DisplayRectangle being updated by mono the same ways as .NET")]
		public void AdjustScrollRangeTestHVScroll()
		{
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
			using (ArrayPtr rgvsliTemp = MarshalEx.ArrayToNative<SelLevInfo>(clev))
			{
				sel.AllTextSelInfo(out ihvoRoot, clev, rgvsliTemp, out tag, out cpropPrevious,
					out ichAnchor, out ichEnd, out ws, out fAssocPrev, out ihvoEnd1, out ttp);
				SelLevInfo[] rgvsli = MarshalEx.NativeToArray<SelLevInfo>(rgvsliTemp, clev);
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
			using (ArrayPtr rgvsliTemp = MarshalEx.ArrayToNative<SelLevInfo>(clev))
			{
				sel.AllTextSelInfo(out ihvoRoot, clev, rgvsliTemp, out tag, out cpropPrevious,
					out ichAnchor, out ichEnd, out ws, out fAssocPrev, out ihvoEnd1, out ttp);
				SelLevInfo[] rgvsli = MarshalEx.NativeToArray<SelLevInfo>(rgvsliTemp, clev);
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
			ITsStrFactory factory = Cache.TsStrFactory;
			string filename;
			if (Environment.OSVersion.Platform == PlatformID.Unix)
				filename = "/junk.jpg";
			else
				filename = "c:\\junk.jpg";
			ICmPicture pict = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create(filename,
				factory.MakeString("Test picture", Cache.DefaultVernWs),
				CmFolderTags.LocalPictures);
			Assert.IsNotNull(pict);

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);
			DynamicMock mockedSelection = new DynamicMock(typeof(IVwSelection));
			mockedSelection.ExpectAndReturn("IsValid", true);
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
				new object[] { false, 0, pict.Hvo, CmPictureTags.kflidCaption, 0, 0, null });
			mockedSelection.ExpectAndReturn("PropInfo", null,
				new object[] { true, 0, null, null, null, null, null },
				new string[] {typeof(bool).FullName, sIntType, intRef, intRef, intRef,
					intRef, typeof(IVwPropertyStore).FullName + "&"},
				new object[] { true, 0, pict.Hvo, CmPictureTags.kflidCaption, 0, 0, null });
			mockedSelection.ExpectAndReturn(2, "EndBeforeAnchor", false);

			DummyBasicView.DummyEditingHelper editingHelper =
				(DummyBasicView.DummyEditingHelper)m_basicView.EditingHelper;
			editingHelper.m_mockedSelection = (IVwSelection)mockedSelection.MockInstance;
			editingHelper.m_fOverrideGetParaPropStores = true;

			IVwSelection vwsel;
			int hvoText, tagText, ihvoFirst, ihvoLast;
			IVwPropertyStore[] vvps;
			ITsTextProps[] vttp;

			Assert.IsTrue(m_basicView.GetParagraphProps(out vwsel, out hvoText, out tagText,
				out vvps, out ihvoFirst, out ihvoLast, out vttp));

			Assert.AreEqual(CmPictureTags.kflidCaption, tagText);
			Assert.AreEqual(1, vttp.Length);
			Assert.AreEqual("Figure caption",
				vttp[0].GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
		#endregion

		#region Merge multiple types of translations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which causes two paragraphs to get joined also preserves the
		/// back translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeTranslationssWhenParasMerge_BothParasHaveTranslations()
		{
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);

			// We'll just re-use our "vernacular" WS for the translations, for testing purposes.
			ICmTranslation bt1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(bt1, m_wsEng, "BT1", null);
			ICmTranslation bt2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(bt2, m_wsEng, "BT2", null);

			m_basicView.RootBox.Reconstruct();

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", bt1.Translation.get_String(m_wsEng).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);

			// We'll just re-use our "vernacular" WS for the translations, for testing purposes.
			ICmTranslation bt2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(bt2, m_wsEng, "BT2", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo, StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null, true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			ICmTranslation bt1 = para1.GetBT();
			Assert.AreEqual("BT2", bt1.Translation.get_String(m_wsEng).Text);
		}
		#endregion

		#region Split BT tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which splits a paragraph gets split into two paragraphs with
		/// the segments divided correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitBTs_BothParasHaveBt()
		{
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add two segments to the first text and create Back Translations
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = book.FootnotesOS[0];
			IScrTxtPara para = (IScrTxtPara)text1.ParagraphsOS[0];
			AddRunToMockedPara(para, DummyBasicView.kSecondParaEng, Cache.DefaultVernWs);
			AddSegmentFt(para, 0, "BT1", Cache.DefaultAnalWs);
			AddSegmentFt(para, 1, "BT2", Cache.DefaultAnalWs);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 0, 1, 1);

			// Make a selection after the first segment.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, ich, 0, true, -1, null, true);
			TypeEnter();

			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = (IStTxtPara)text1.ParagraphsOS[1];
			Assert.AreEqual(DummyBasicView.kFirstParaEng, para1.Contents.Text);
			Assert.AreEqual(DummyBasicView.kSecondParaEng, para2.Contents.Text);
			Assert.AreEqual("BT1", para1.SegmentsOS[0].FreeTranslation.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("BT2", para2.SegmentsOS[0].FreeTranslation.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a key press which splits a paragraph mid-segment gets split into two
		/// paragraphs with the segments divided correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitBTs_MidSegment_BothParasHaveBt()
		{
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add two segments to the first text and create Back Translations
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = book.FootnotesOS[0];
			IScrTxtPara para = (IScrTxtPara)text1.ParagraphsOS[0];
			AddRunToMockedPara(para, DummyBasicView.kSecondParaEng, Cache.DefaultVernWs);
			AddRunToMockedPara(para, DummyBasicView.kSecondParaEng, Cache.DefaultVernWs);
			AddSegmentFt(para, 0, "BT1", Cache.DefaultAnalWs);
			AddSegmentFt(para, 1, "BT2", Cache.DefaultAnalWs);
			AddSegmentFt(para, 2, "BT3", Cache.DefaultAnalWs);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 0, 1, 1);

			// Make a selection after the first segment.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length + 5;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, ich, 0, true, -1, null, true);
			TypeEnter();

			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = (IStTxtPara)text1.ParagraphsOS[1];
			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng.Substring(0, 5),
				para1.Contents.Text);
			Assert.AreEqual(DummyBasicView.kSecondParaEng.Substring(5) + DummyBasicView.kSecondParaEng,
				para2.Contents.Text);
			Assert.AreEqual("BT1", para1.SegmentsOS[0].FreeTranslation.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("BT2", para1.SegmentsOS[1].FreeTranslation.AnalysisDefaultWritingSystem.Text);
			Assert.IsNull(para2.SegmentsOS[0].FreeTranslation.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("BT3", para2.SegmentsOS[1].FreeTranslation.AnalysisDefaultWritingSystem.Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.get_String(m_wsEng).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create a Back Translations on
			// only the second paragraph
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			using (IEnumerator<ICmTranslation> translations = para1.TranslationsOC.GetEnumerator())
			{
				translations.MoveNext();
				ICmTranslation transl = translations.Current;
				Assert.AreEqual("BT2", transl.Translation.get_String(m_wsEng).Text);
			}
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create a Back Translations on
			// only the first paragraph
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1", trans1.Translation.get_String(m_wsEng).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);

			int wsfr = m_wsf.GetWsFromStr("fr");
			trans1.Translation.set_String(wsfr, Cache.TsStrFactory.MakeString("BT1fr", wsfr));

			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);
			trans2.Translation.set_String(wsfr, Cache.TsStrFactory.MakeString("BT2fr", wsfr));

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.get_String(m_wsEng).Text);
			Assert.AreEqual("BT1fr BT2fr", trans1.Translation.get_String(wsfr).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			int wsfr = m_wsf.GetWsFromStr("fr");
			trans2.Translation.set_String(wsfr, Cache.TsStrFactory.MakeString("BT2fr", wsfr));

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.get_String(m_wsEng).Text);
			Assert.AreEqual("BT2fr", trans1.Translation.get_String(wsfr).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);
			int wsfr = m_wsf.GetWsFromStr("fr");
			trans1.Translation.set_String(wsfr, Cache.TsStrFactory.MakeString("BT1fr", wsfr));

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.get_String(m_wsEng).Text);
			Assert.AreEqual("BT1fr", trans1.Translation.get_String(wsfr).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the end of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ichEndPara1 = DummyBasicView.kFirstParaEng.Length;
			int ichEndPara2 = DummyBasicView.kSecondParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ichEndPara1,
				ichEndPara2, 0, true, 1, null, true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng, para1.Contents.Text);
			Assert.AreEqual("BT1", trans1.Translation.get_String(m_wsEng).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the end of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 1;
			int ichEndPara1 = DummyBasicView.kFirstParaEng.Length;
			int ichEndPara2 = DummyBasicView.kSecondParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ichEndPara2,
				ichEndPara1, 0, true, 0, null, true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng, para1.Contents.Text);
			Assert.AreEqual("BT1", trans1.Translation.get_String(m_wsEng).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the start of first paragraph to the start of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, 0, 0, 0,
				true, 1, null, true);
			TypeBackspace();

			// we don't know which paragraph survived, so get it from text
			para1 = (IStTxtPara)text1.ParagraphsOS[0];

			Assert.AreEqual(DummyBasicView.kSecondParaEng, para1.Contents.Text);
			using (IEnumerator<ICmTranslation> translations = para1.TranslationsOC.GetEnumerator())
			{
				translations.MoveNext();
				ICmTranslation transl = translations.Current;
				Assert.AreEqual("BT2", transl.Translation.get_String(m_wsEng).Text);
			}
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection at the end of first paragraph
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 1;
			//int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, 0, 0, 0,
				true, -1, null, true);
			TypeBackspace();

			// we don't know which paragraph survived, so get it from text
			para1 = (IStTxtPara)text1.ParagraphsOS[0];

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.get_String(m_wsEng).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, "Just some garbage", m_wsEng);
			IStTxtPara para3 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para3, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);
			ICmTranslation trans3 = AddBtToMockedParagraph(para3, m_wsEng);
			AddRunToMockedTrans(trans3, m_wsEng, "BT3", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 2, 0);

			// Make a selection from the end of first paragraph to the beginning of the third.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 2, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT3", trans1.Translation.get_String(m_wsEng).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, "Just some garbage", m_wsEng);
			IStTxtPara para3 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para3, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			// extra space intentionally added after BT1 to test TE-4075 (that too many spaces are not added)
			AddRunToMockedTrans(trans1, m_wsEng, "BT1 ", null);
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);
			ICmTranslation trans3 = AddBtToMockedParagraph(para3, m_wsEng);
			AddRunToMockedTrans(trans3, m_wsEng, "BT3", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 2, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ichAnchor = DummyBasicView.kFirstParaEng.Length - 2;
			int ichEnd = 2;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ichAnchor, ichEnd, 0, true, 2, null,
				true);
			TypeBackspace();

			Assert.AreEqual(DummyBasicView.kFirstParaEng.Substring(0, ichAnchor) +
				DummyBasicView.kSecondParaEng.Substring(ichEnd), para1.Contents.Text);
			Assert.AreEqual("BT1 BT3", trans1.Translation.get_String(m_wsEng).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			// extra space intentionally added before BT2 to test TE-4075 (that too many spaces are not added)
			AddRunToMockedTrans(trans2, m_wsEng, " BT2", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeDelete();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.get_String(m_wsEng).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create a Back Translations on
			// only the second paragraph
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = DummyBasicView.kFirstParaEng.Length;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ich, 0, 0, true, 1, null,
				true);
			TypeDelete();

			Assert.AreEqual(DummyBasicView.kFirstParaEng + DummyBasicView.kSecondParaEng,
				para1.Contents.Text);
			using (IEnumerator<ICmTranslation> translations = para1.TranslationsOC.GetEnumerator())
			{
				translations.MoveNext();
				ICmTranslation transl = translations.Current;
				Assert.AreEqual("BT2", transl.Translation.get_String(m_wsEng).Text);
			}
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Add a second paragraph to the first text and create some Back Translations on
			// both paragraphs
			IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(m_hvoRoot);
			IStText text1 = (IStText)book.FootnotesOS[0];
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = AddParaToMockedText(text1, "TestStyle");
			AddRunToMockedPara(para2, DummyBasicView.kSecondParaEng, m_wsEng);
			// We'll just re-use our "vernacular" WS for the back translation, for testing purposes.
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, m_wsEng);
			AddRunToMockedTrans(trans1, m_wsEng, "BT1", null);
			ICmTranslation trans2 = AddBtToMockedParagraph(para2, m_wsEng);
			AddRunToMockedTrans(trans2, m_wsEng, "BT2", null);

			// Because of the complexities of this test, we need to fire all of the propchanges
			// from the setup to update the view.
			m_actionHandler.BreakUndoTask("I want my mommy", "I want my mommy");

			rootBox.PropChanged(text1.Hvo, StTextTags.kflidParagraphs, 1, 1, 0);

			// Make a selection from the end of first paragraph to the beginning of the second.
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ichAnchor = DummyBasicView.kFirstParaEng.Length - 2;
			int ichEnd = 2;
			IVwSelection sel = rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, ichAnchor, ichEnd, 0, true, 1, null, true);
			TypeChar('a');

			Assert.AreEqual(DummyBasicView.kFirstParaEng.Substring(0, ichAnchor) + "a" +
				DummyBasicView.kSecondParaEng.Substring(ichEnd), para1.Contents.Text);
			Assert.AreEqual("BT1 BT2", trans1.Translation.get_String(m_wsEng).Text);
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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;
			rootBox.Activate(VwSelectionState.vssEnabled);

			// Select the first four characters in the first paragraph
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, 0, 3, 0, true, 0, null, true);

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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;
			rootBox.Activate(VwSelectionState.vssEnabled);

			// Select the first four characters in the first paragraph
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, 0, 3, 0, true, 0, null, true);

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
			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;
			rootBox.Activate(VwSelectionState.vssEnabled);
			m_basicView.ShowRangeSelAfterLostFocus = true;

			// Select the first four characters in the first paragraph
			SelLevInfo[] levelInfo = new SelLevInfo[2];
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			rootBox.MakeTextSelection(0, 2, levelInfo,
				StTxtParaTags.kflidContents, 0, 0, 3, 0, true, 0, null, true);

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
			TypeChar((char)VwSpecialChars.kscBackspace);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate the user typing a delete
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void TypeDelete()
		{
			TypeChar((char)VwSpecialChars.kscDelForward);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate the user typing the enter key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void TypeEnter()
		{
			TypeChar('\r');
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
				bool fWasComplex;
				rootBox.DeleteRangeIfComplex(vg, out fWasComplex);

				// Attempt to act like the real program in the case of complex deletions
				if (fWasComplex)
					rootBox.DataAccess.GetActionHandler().BreakUndoTask("complex deletion", "complex deletion");

				if (!fWasComplex || (ch != (char)VwSpecialChars.kscBackspace && ch != (char)VwSpecialChars.kscDelForward))
					rootBox.OnTyping(vg, ch.ToString(), VwShiftStatus.kfssNone, ref wsTemp);
			}
			finally
			{
				m_basicView.ReleaseGraphics(rootBox, vg);
			}
		}
		#endregion
	}
}
