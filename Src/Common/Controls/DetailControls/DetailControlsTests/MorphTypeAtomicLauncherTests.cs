// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Regression tests for the DoNotRefresh + m_postponePropChanged interaction (LT-22414).
	///
	/// Root cause: when <c>m_postponePropChanged=true</c> (default since LT-22018),
	/// <see cref="DataTree.PropChanged"/> defers refresh via <c>BeginInvoke</c>.
	/// This means <see cref="DataTree.RefreshListNeeded"/> is never set during a
	/// <c>DoNotRefresh</c> window, so releasing <c>DoNotRefresh</c> does not trigger
	/// a synchronous refresh. Code that brackets LCModel changes with DoNotRefresh
	/// (like <c>MorphTypeAtomicLauncher.SwapValues</c>) must explicitly set
	/// <c>RefreshListNeeded=true</c> before releasing <c>DoNotRefresh</c>.
	/// </summary>
	[TestFixture]
	public class MorphTypeSwapRefreshTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Inventory m_parts;
		private Inventory m_layouts;
		private ILexEntry m_entry;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private DataTree m_dtree;
		private Form m_parent;

		#region Fixture Setup and Teardown

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_layouts = DataTreeTests.GenerateLayouts();
			m_parts = DataTreeTests.GenerateParts();
		}

		#endregion

		#region Test Setup and Teardown

		public override void TestSetup()
		{
			base.TestSetup();

			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			m_entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("test", Cache.DefaultVernWs);
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("bib content");
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("bib content");

			m_dtree = new DataTree();
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent = new Form();
			m_parent.Controls.Add(m_dtree);
		}

		public override void TestTearDown()
		{
			if (m_parent != null)
			{
				m_parent.Close();
				m_parent.Dispose();
			}
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}
			if (m_mediator != null)
			{
				m_mediator.Dispose();
				m_mediator = null;
			}

			base.TestTearDown();
		}

		#endregion

		/// <summary>
		/// LT-22414 regression: after clearing bibliography data inside a
		/// DoNotRefresh window, the ifdata bibliography slice should disappear.
		///
		/// With m_postponePropChanged=true (default since LT-22018), PropChanged
		/// defers via BeginInvoke and never sets RefreshListNeeded during the
		/// DoNotRefresh window. Callers (like SwapValues) must explicitly set
		/// RefreshListNeeded=true before releasing DoNotRefresh.
		///
		/// RED phase:  comment out RefreshListNeeded=true → test FAILS (stale slices).
		/// GREEN phase: RefreshListNeeded=true present → test PASSES.
		/// </summary>
		[Test]
		public void DoNotRefresh_SlicesMustReflectChanges_AfterRelease_LT22414()
		{
			// Arrange: show entry with CfAndBib layout (CitationForm + Bibliography ifdata)
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

			// Verify initial state: both slices visible (bib has data)
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2),
				"Setup: should have CitationForm + Bibliography");

			// Act: simulate the DoNotRefresh pattern from SwapValues
			m_dtree.DoNotRefresh = true;

			// Make changes that should affect visible slices:
			// clearing bibliography data should cause the ifdata slice to disappear on refresh
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("");
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");

			// LT-22414 FIX: callers must explicitly set RefreshListNeeded=true.
			// Without this line, the test FAILS (proving the bug exists).
			// >>> COMMENT OUT THIS LINE TO SEE THE BUG <<<
			m_dtree.RefreshListNeeded = true;

			m_dtree.DoNotRefresh = false;

			// Assert: after refresh, bibliography slice should be gone (no data → ifdata hides it)
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(1),
				"LT-22414: After DoNotRefresh=false, slices should reflect data changes. " +
				"Bibliography has no data so ifdata should hide it. " +
				"If this fails with count=2, no refresh occurred.");
			Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("CitationForm"));
		}

		/// <summary>
		/// Complementary test: verify that WITHOUT RefreshListNeeded=true,
		/// releasing DoNotRefresh does NOT trigger a refresh (the bug behavior).
		/// This documents the root cause of LT-22414.
		/// </summary>
		[Test]
		public void DoNotRefresh_WithoutRefreshListNeeded_DoesNotRefresh_LT22414_BugDemo()
		{
			// Arrange: show entry with CfAndBib layout
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2));

			var originalSlice = m_dtree.Slices[0];

			// Act: DoNotRefresh without setting RefreshListNeeded
			m_dtree.DoNotRefresh = true;
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("");
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
			// Intentionally NOT setting RefreshListNeeded (simulates buggy SwapValues)
			m_dtree.DoNotRefresh = false;

			// Assert: slices are STALE — bibliography still visible despite no data
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2),
				"Without RefreshListNeeded, DoNotRefresh=false does not trigger refresh; " +
				"slices remain stale (bibliography still visible despite no data).");
		}
	}
}
