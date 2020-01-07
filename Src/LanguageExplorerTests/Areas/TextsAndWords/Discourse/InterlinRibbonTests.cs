// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using FieldWorks.TestUtilities.Attributes;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorerTests.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// Tests for the Constituent chart.
	/// </summary>
	[TestFixture]
	[InitializeRealKeyboardController]
	public class InterlinRibbonTests : InMemoryDiscourseTestBase
	{
		private TestInterlinRibbon _ribbon;
		private FlexComponentParameters _flexComponentParameters;

		#region Test setup

		/// <summary>
		/// Create minimal test data required for every test.
		/// </summary>
		protected override void CreateTestData()
		{
			base.CreateTestData();
			_ribbon = new TestInterlinRibbon(Cache, m_stText.Hvo)
			{
				Width = 100,
				Height = 40
			};
			_flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			_ribbon.InitializeFlexComponent(_flexComponentParameters);
			Assert.IsNotNull(_ribbon.Decorator, "Don't have correct access here.");
			_ribbon.CacheRibbonItems(new List<AnalysisOccurrence>());
		}

		public override void TestTearDown()
		{
			try
			{
				_ribbon?.Dispose();
				TestSetupServices.DisposeTrash(_flexComponentParameters);
				_ribbon = null;
				_flexComponentParameters = null;
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} TestTearDown method.", err);
			}
			finally
			{
				base.TestTearDown();
			}
		}

	#endregion

	#region Helper methods

		private static AnalysisOccurrence[] GetParaAnalyses(IStTxtPara para)
		{
			var result = new List<AnalysisOccurrence>();
			var point1 = new AnalysisOccurrence(para.SegmentsOS[0], 0);
			if (!point1.IsValid)
			{
				return result.ToArray();
			}
			do
			{
				if (point1.HasWordform)
				{
					result.Add(point1);
				}
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
			_ribbon.MakeRoot();
			_ribbon.CallLayout();
			Assert.IsNotNull(_ribbon.RootBox, "layout should produce some root box");
			var widthEmpty = _ribbon.RootBox.Width;
			var glosses = new AnalysisOccurrence[0];

			// SUT#2 This changes data! Use a UOW.
			UndoableUnitOfWorkHelper.Do("RibbonLayoutUndo", "RibbonLayoutRedo", Cache.ActionHandlerAccessor, () => glosses = GetParaAnalyses(m_firstPara));

			Assert.Greater(glosses.Length, 0);
			var firstGloss = new List<AnalysisOccurrence> { glosses[0] };

			// SUT#3 This changes some internal data! Use a UOW.
			UndoableUnitOfWorkHelper.Do("CacheAnnsUndo", "CacheAnnsRedo", Cache.ActionHandlerAccessor, () => _ribbon.CacheRibbonItems(firstGloss));
			_ribbon.CallLayout();

			int widthOne = _ribbon.RootBox.Width;
			int heightOne = _ribbon.RootBox.Height;
			Assert.IsTrue(widthOne > widthEmpty, "adding a wordform should make the root box wider");

			var glossList = new List<AnalysisOccurrence>();
			glossList.AddRange(glosses);

			// SUT#4 This changes some internal data! Use a UOW.
			UndoableUnitOfWorkHelper.Do("CacheAnnsUndo", "CacheAnnsRedo", Cache.ActionHandlerAccessor, () => _ribbon.CacheRibbonItems(glossList));
			_ribbon.CallLayout();
			int widthMany = _ribbon.RootBox.Width;
			int heightMany = _ribbon.RootBox.Height;
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
			int labelOffset = 150;
			glossList.AddRange(glosses);
			EndSetupTask();

			//SUT
			UndoableUnitOfWorkHelper.Do("CacheAnnUndo", "CacheAnnRedo", m_actionHandler, () => _ribbon.CacheRibbonItems(glossList));

			_ribbon.MakeRoot();
			_ribbon.RootBox.Reconstruct(); // forces it to really be constructed
			_ribbon.CallOnLoad(new EventArgs());
			Assert.AreEqual(new [] { glosses[0] }, _ribbon.SelectedOccurrences, "should have selection even before any click");

			Rectangle rcSrc, rcDst;
			_ribbon.CallGetCoordRects(out rcSrc, out rcDst);

			// SUT #2?!
			_ribbon.RootBox.MouseDown(labelOffset, 1, rcSrc, rcDst);
			_ribbon.RootBox.MouseUp(labelOffset, 1, rcSrc, rcDst);
			Assert.AreEqual(new[] { glosses[0] }, _ribbon.SelectedOccurrences);

			var location = _ribbon.GetSelLocation();
			Assert.IsTrue(_ribbon.RootBox.Selection.IsRange, "single click selection should expand to range");
			var offset = location.Width + labelOffset;

			// SUT #3?!
			// Clicking just right of that should add the second one. We need to allow for the gap between
			// (about 15 pixels) and at the left of the view.
			_ribbon.RootBox.MouseDown(offset + 15, 5, rcSrc, rcDst);
			_ribbon.RootBox.MouseUp(offset + 15, 5, rcSrc, rcDst);
			Assert.AreEqual(new[] { glosses[0], glosses[1] }, _ribbon.SelectedOccurrences);

			// SUT #4?!
			// And a shift-click back near the start should go back to just one of them.
			_ribbon.RootBox.MouseDownExtended(1, 1, rcSrc, rcDst);
			_ribbon.RootBox.MouseUp(1, 1, rcSrc, rcDst);
			Assert.AreEqual(new[] { glosses[0] }, _ribbon.SelectedOccurrences);
		}
		#endregion

		/// <summary>
		/// Makes some protected methods available for testing.
		/// </summary>
		private class TestInterlinRibbon : InterlinRibbon
		{
			public TestInterlinRibbon(LcmCache cache, int hvoStText)
				: base(cache, hvoStText)
			{
			}

			/// <summary>
			/// Call the OnLayout methods (test-only)
			/// </summary>
			public void CallLayout()
			{
				OnLayout(new LayoutEventArgs(this, string.Empty));
			}

			/// <summary>
			/// Make method available for testing
			/// </summary>
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
					anchor.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary, out rcSec, out fSplit, out fEndBeforeAnchor);
					end.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary2, out rcSec, out fSplit, out fEndBeforeAnchor);
					var left = Math.Min(rcPrimary.left, rcPrimary2.left);
					var top = Math.Min(rcPrimary.top, rcPrimary2.top);
					var width = Math.Max(rcPrimary.right, rcPrimary2.right) - left;
					var height = Math.Max(rcPrimary.bottom, rcPrimary2.bottom) - top;
					return new Rectangle(left, top, width, height);
				}
			}

			internal void CallOnLoad(EventArgs eventArgs)
			{
				base.OnLoad(eventArgs);
			}

			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
				base.Dispose(disposing);
			}
		}
	}
}
