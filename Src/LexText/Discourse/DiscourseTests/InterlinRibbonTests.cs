// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Windows.Forms;
using System.Drawing;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.Utils.Attributes;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// Tests for the Constituent chart.
	/// </summary>
	[TestFixture]
	[InitializeRealKeyboardController(InitDummyAfterTests = true)]
	public class InterlinRibbonTests : InMemoryDiscourseTestBase
	{
		private TestInterlinRibbon m_ribbon;

		#region Test setup

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create minimal test data required for every test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_ribbon = new TestInterlinRibbon(Cache, m_stText.Hvo);
			m_ribbon.Width = 100;
			m_ribbon.Height = 40;
			Assert.IsNotNull(m_ribbon.Decorator, "Don't have correct access here.");
			m_ribbon.CacheRibbonItems(new List<AnalysisOccurrence>());
		}

		public override void TestTearDown()
		{
			m_ribbon.Dispose();
			base.TestTearDown();
		}

		#endregion

		#region Helper methods

		private static AnalysisOccurrence[] GetParaAnalyses(IStTxtPara para)
		{
			var result = new List<AnalysisOccurrence>();
			var point1 = new AnalysisOccurrence(para.SegmentsOS[0], 0);
			if (!point1.IsValid)
				return result.ToArray();
			do
			{
				if (point1.HasWordform)
					result.Add(point1);
				point1 = point1.NextWordform();
			} while (point1 != null && point1.IsValid);
			return result.ToArray();
		}

		#endregion

		#region tests

		[Test]
		public void RibbonLayout()
		{
			EndSetupTask();
			// SUT#1 (but not one that changes data)
			m_ribbon.MakeRoot();
			m_ribbon.CallLayout();
			Assert.IsNotNull(m_ribbon.RootBox, "layout should produce some root box");
			var widthEmpty = m_ribbon.RootBox.Width;
			var glosses = new AnalysisOccurrence[0];

			// SUT#2 This changes data! Use a UOW.
			UndoableUnitOfWorkHelper.Do("RibbonLayoutUndo", "RibbonLayoutRedo",
										Cache.ActionHandlerAccessor, () => glosses = GetParaAnalyses(m_firstPara));

			Assert.Greater(glosses.Length, 0);
			var firstGloss = new List<AnalysisOccurrence> { glosses[0] };

			// SUT#3 This changes some internal data! Use a UOW.
			UndoableUnitOfWorkHelper.Do("CacheAnnsUndo", "CacheAnnsRedo", Cache.ActionHandlerAccessor,
										() => m_ribbon.CacheRibbonItems(firstGloss));
			m_ribbon.CallLayout();

			int widthOne = m_ribbon.RootBox.Width;
			int heightOne = m_ribbon.RootBox.Height;
			Assert.IsTrue(widthOne > widthEmpty, "adding a wordform should make the root box wider");

			var glossList = new List<AnalysisOccurrence>();
			glossList.AddRange(glosses);

			// SUT#4 This changes some internal data! Use a UOW.
			UndoableUnitOfWorkHelper.Do("CacheAnnsUndo", "CacheAnnsRedo", Cache.ActionHandlerAccessor,
										() => m_ribbon.CacheRibbonItems(glossList));
			m_ribbon.CallLayout();
			int widthMany = m_ribbon.RootBox.Width;
			int heightMany = m_ribbon.RootBox.Height;
			Assert.IsTrue(widthMany > widthOne, "adding more wordforms should make the root box wider");
			// In a real view they might not be exactly equal due to subscripts and the like, but our
			// text and anaysis are very simple.
			Assert.AreEqual(heightOne, heightMany, "ribbon should not wrap!");
		}

		[Test]
		public void ClickExpansion()
		{
			var glosses = GetParaAnalyses(m_firstPara);
			var glossList = new List<AnalysisOccurrence>();
			glossList.AddRange(glosses);
			EndSetupTask();

			//SUT
			UndoableUnitOfWorkHelper.Do("CacheAnnUndo", "CacheAnnRedo", m_actionHandler, () =>
																						 m_ribbon.CacheRibbonItems(glossList));

			m_ribbon.MakeRoot();
			m_ribbon.RootBox.Reconstruct(); // forces it to really be constructed
			m_ribbon.CallOnLoad(new EventArgs());
			Assert.AreEqual(new [] { glosses[0] }, m_ribbon.SelectedOccurrences, "should have selection even before any click");

			Rectangle rcSrc, rcDst;
			m_ribbon.CallGetCoordRects(out rcSrc, out rcDst);

			// SUT #2?!
			m_ribbon.RootBox.MouseDown(1, 1, rcSrc, rcDst);
			m_ribbon.RootBox.MouseUp(1, 1, rcSrc, rcDst);
			Assert.AreEqual(new [] { glosses[0] }, m_ribbon.SelectedOccurrences);

			Rectangle location = m_ribbon.GetSelLocation();
			Assert.IsTrue(m_ribbon.RootBox.Selection.IsRange, "single click selection should expand to range");
			int width = location.Width;

			// SUT #3?!
			// Clicking just right of that should add the second one. We need to allow for the gap between
			// (about 10 pixels) and at the left of the view.
			m_ribbon.RootBox.MouseDown(width + 15, 5, rcSrc, rcDst);
			m_ribbon.RootBox.MouseUp(width + 15, 5, rcSrc, rcDst);
			Assert.AreEqual(new [] { glosses[0], glosses[1] }, m_ribbon.SelectedOccurrences);

			// SUT #4?!
			// And a shift-click back near the start should go back to just one of them.
			m_ribbon.RootBox.MouseDownExtended(1, 1, rcSrc, rcDst);
			m_ribbon.RootBox.MouseUp(1, 1, rcSrc, rcDst);
			Assert.AreEqual(new [] { glosses[0] }, m_ribbon.SelectedOccurrences);
		}
		#endregion
	}

	/// <summary>
	/// Makes some protected methods available for testing.
	/// </summary>
	internal class TestInterlinRibbon : InterlinRibbon
	{
		public TestInterlinRibbon(FdoCache cache, int hvoStText)
			: base(cache, hvoStText)
		{
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the OnLayout methods (test-only)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallLayout()
		{
			OnLayout(new LayoutEventArgs(this, string.Empty));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make method available for testing
		/// </summary>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		internal void CallGetCoordRects(out Rectangle rcSrcRoot, out Rectangle rcDstRoot)
		{
			GetCoordRects(out rcSrcRoot, out rcDstRoot);
		}

		internal Rectangle GetSelLocation()
		{
			using (new HoldGraphics(this))
			{
				Rectangle rcSrcRoot, rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				Rect rcPrimary, rcSec, rcPrimary2;
				bool fSplit, fEndBeforeAnchor;
				var anchor = RootBox.Selection.EndPoint(false);
				var end = RootBox.Selection.EndPoint(true);
				anchor.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
								out rcSec, out fSplit, out fEndBeforeAnchor);
				end.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary2,
							 out rcSec, out fSplit, out fEndBeforeAnchor);
				int left = Math.Min(rcPrimary.left, rcPrimary2.left);
				int top = Math.Min(rcPrimary.top, rcPrimary2.top);
				int width = Math.Max(rcPrimary.right, rcPrimary2.right) - left;
				int height = Math.Max(rcPrimary.bottom, rcPrimary2.bottom) - top;

				return new Rectangle(left, top, width, height);
			}
		}

		internal void CallOnLoad(EventArgs eventArgs)
		{
			base.OnLoad(eventArgs);
		}
	}
}
