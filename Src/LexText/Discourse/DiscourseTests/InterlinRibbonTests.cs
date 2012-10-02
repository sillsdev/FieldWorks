using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Windows.Forms;
using System.Drawing;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;

using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// Tests for the Constituent chart.
	/// </summary>
	[TestFixture]
	public class InterlinRibbonTests : InMemoryDiscourseTestBase
	{
		TestInterlinRibbon m_ribbon;

		public InterlinRibbonTests()
		{
		}
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
			Cache.VwCacheDaAccessor.CacheVecProp(m_stText.Hvo, m_ribbon.AnnotationListId, new int[0], 0);
		}

		public override void Exit()
		{
			if (m_ribbon != null)
			{
				m_ribbon.Dispose();
				m_ribbon = null;
			}
			m_ribbon = null;
			base.Exit();
		}

		#endregion

		#region test data creation

		#endregion

		#region tests

		[Test]
		public void RibbonLayout()
		{
			m_ribbon.MakeRoot();
			m_ribbon.CallLayout();
			Assert.IsNotNull(m_ribbon.RootBox, "layout should produce some root box");
			int widthEmpty = m_ribbon.RootBox.Width;
			int heightEmpty = m_ribbon.RootBox.Height;

			int[] glosses = MakeAnnotations(m_firstPara);
			int[] firstGloss = new int[] { glosses[0] };
			Cache.VwCacheDaAccessor.CacheVecProp(m_stText.Hvo, m_ribbon.AnnotationListId, firstGloss, 1);
			Cache.PropChanged(m_stText.Hvo, m_ribbon.AnnotationListId, 0, 1, 0);
			m_ribbon.CallLayout();
			int widthOne = m_ribbon.RootBox.Width;
			int heightOne = m_ribbon.RootBox.Height;
			Assert.IsTrue(widthOne > widthEmpty, "adding a wordform should make the root box wider");

			Cache.VwCacheDaAccessor.CacheVecProp(m_stText.Hvo, m_ribbon.AnnotationListId, glosses, glosses.Length);
			Cache.PropChanged(m_stText.Hvo, m_ribbon.AnnotationListId, 0, glosses.Length - 1, 0);
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
			int[] glosses = MakeAnnotations(m_firstPara);
			Cache.VwCacheDaAccessor.CacheVecProp(m_stText.Hvo, m_ribbon.AnnotationListId, glosses, glosses.Length);
			m_ribbon.MakeRoot();
			m_ribbon.RootBox.Reconstruct(); // forces it to really be constructed
			m_ribbon.CallOnLoad(new EventArgs());
			Assert.AreEqual(new int[] { glosses[0] }, m_ribbon.SelectedAnnotations, "should have selection even before any click");

			Rectangle rcSrc, rcDst;
			m_ribbon.CallGetCoordRects(out rcSrc, out rcDst);
			m_ribbon.RootBox.MouseDown(1, 1, rcSrc, rcDst);
			m_ribbon.RootBox.MouseUp(1, 1, rcSrc, rcDst);
			Assert.AreEqual(new int[] { glosses[0] }, m_ribbon.SelectedAnnotations);

			Rectangle location = m_ribbon.GetSelLocation();
			Assert.IsTrue(m_ribbon.RootBox.Selection.IsRange, "single click selection should expand to range");
			int width = location.Width;

			// Clicking just right of that should add the second one. We need to allow for the gap between
			// (about 10 pixels) and at the left of the view.
			m_ribbon.RootBox.MouseDown(width + 15, 5, rcSrc, rcDst);
			m_ribbon.RootBox.MouseUp(width + 15, 5, rcSrc, rcDst);
			Assert.AreEqual(new int[] { glosses[0], glosses[1] }, m_ribbon.SelectedAnnotations);

			// And a shift-click back near the start should go back to just one of them.
			m_ribbon.RootBox.MouseDownExtended(1, 1, rcSrc, rcDst);
			m_ribbon.RootBox.MouseUp(1, 1, rcSrc, rcDst);
			Assert.AreEqual(new int[] { glosses[0] }, m_ribbon.SelectedAnnotations);
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
			base.GetCoordRects(out rcSrcRoot, out rcDstRoot);
		}

		internal Rectangle GetSelLocation()
		{
			using (new HoldGraphics(this))
			{
				Rectangle rcSrcRoot, rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				Rect rcPrimary, rcSec, rcPrimary2;
				bool fSplit, fEndBeforeAnchor;
				IVwSelection anchor = RootBox.Selection.EndPoint(false);
				IVwSelection end = RootBox.Selection.EndPoint(true);
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
