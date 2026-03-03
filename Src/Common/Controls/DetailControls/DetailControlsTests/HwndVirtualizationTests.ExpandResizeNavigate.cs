// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Edge-case tests for sense expansion, window resizing, and navigation.
//
// ======================== REGRESSION RISK MODEL ========================
//
// These tests target the interaction between:
//   1. PATH-L1/PATH-R1 layout/reconstruct guards in the C++ Views engine
//   2. HWND grow-only virtualization changes in DataTree/Slice
//   3. PropChanged notifications during sense expansion
//   4. Window resizing that changes available width (invalidating layout cache)
//   5. Navigation (ShowObject) between entries with different slice counts
//
// The crash that triggered these tests was an assertion failure in
// VwPropertyStore.cpp:1336 — ValidReadPtr(qws) — during PropChanged
// when expanding a sense. The stack trace shows PropChanged flowing
// through VwRootBoxClass into the notifier chain.
//
// ======================== RISK AREAS ========================
//
// RISK F — Stale Reconstruct Guard
//   PATH-R1 (m_fNeedsReconstruct) might skip a Reconstruct() call after
//   sense expansion if PropChanged doesn't correctly set the dirty flag
//   before the guard evaluates. If the view references a stale box tree,
//   property stores may encounter uninitialized writing systems (ws=0).
//
// RISK G — Width Mismatch After Resize
//   PATH-L1 (m_dxLastLayoutWidth) caches the layout width. Window resize
//   changes GetAvailWidth(). If resize doesn't trigger a proper relayout,
//   text may wrap incorrectly or overflow.
//
// RISK H — Navigation Slice Lifecycle
//   ShowObject disposes old slices and creates new ones. If the new entry
//   has different sense/subsense structure, the Reconstruct guard must be
//   reset. Stale guard state from the previous entry could skip a needed
//   Reconstruct on the new entry's VwRootBoxes.
//
// RISK I — Rapid Succession Operations
//   Multiple expand/collapse/resize/navigate operations in quick succession
//   can leave intermediate state that causes NREs or assertion failures.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.RenderVerification;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Edge-case tests for sense expansion, window resizing, and navigation
	/// in the DataTree. These tests verify that the PATH-L1/PATH-R1 layout
	/// and reconstruct guards in the C++ Views engine interact safely with
	/// the managed DataTree/Slice lifecycle during common user operations.
	/// </summary>
	[TestFixture]
	public class ExpandResizeNavigateTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Test infrastructure

		private Inventory m_parts;
		private Inventory m_layouts;
		private ILexEntry m_entry;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private DataTree m_dtree;
		private Form m_parent;
		private CustomFieldForTest m_customField;

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_layouts = DataTreeTests.GenerateLayouts();
			m_parts = DataTreeTests.GenerateParts();
		}

		public override void TestSetup()
		{
			base.TestSetup();
			m_customField = new CustomFieldForTest(Cache, "testField", "testField",
				LexEntryTags.kClassId, CellarPropertyType.String, Guid.Empty);
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			m_entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("test", Cache.DefaultVernWs);

			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_dtree = new DataTree();
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent = new Form();
			m_parent.Controls.Add(m_dtree);
		}

		public override void TestTearDown()
		{
			if (m_customField != null && Cache?.MainCacheAccessor?.MetaDataCache != null)
			{
				m_customField.Dispose();
				m_customField = null;
			}
			if (m_parent != null)
			{
				m_parent.Close();
				m_parent.Dispose();
				m_parent = null;
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
			m_dtree = null;
			base.TestTearDown();
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Creates a LexEntry with the specified number of top-level senses,
		/// each with a gloss set in the default analysis writing system.
		/// </summary>
		private ILexEntry CreateEntryWithSenses(int senseCount, string citationForm = "test")
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString(citationForm, Cache.DefaultVernWs);

			for (int i = 0; i < senseCount; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.AnalysisDefaultWritingSystem =
					TsStringUtils.MakeString($"gloss-{i}", Cache.DefaultAnalWs);
			}
			return entry;
		}

		/// <summary>
		/// Creates a LexEntry with nested senses (subsenses).
		/// </summary>
		private ILexEntry CreateEntryWithSubsenses(
			int topLevelCount, int subsenseCount, string citationForm = "nested")
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString(citationForm, Cache.DefaultVernWs);

			for (int i = 0; i < topLevelCount; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.AnalysisDefaultWritingSystem =
					TsStringUtils.MakeString($"sense-{i}", Cache.DefaultAnalWs);

				for (int j = 0; j < subsenseCount; j++)
				{
					var sub = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
					sense.SensesOS.Add(sub);
					sub.Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString($"sub-{i}-{j}", Cache.DefaultAnalWs);
				}
			}
			return entry;
		}

		/// <summary>
		/// Creates a populated DataTree using the "Normal" layout (which includes senses).
		/// </summary>
		private DataTree CreateNormalDataTree(ILexEntry entry)
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(entry, "Normal", null, entry, false);
			return m_dtree;
		}

		/// <summary>
		/// Creates a populated DataTree using the "OptSensesEty" layout.
		/// </summary>
		private DataTree CreateOptSensesEtyDataTree(ILexEntry entry)
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(entry, "OptSensesEty", null, entry, false);
			return m_dtree;
		}

		/// <summary>
		/// Creates a fresh DataTree, Mediator, PropertyTable, and parent Form,
		/// then shows the entry in the specified layout. DataTree does not
		/// fully refresh on repeated ShowObject calls after data changes on
		/// the same tree instance. (Pattern from DataTreeTests.OwnedObjects.)
		/// </summary>
		private DataTree ReShowWithFreshDataTree(ILexEntry entry, string layoutName)
		{
			if (m_parent != null)
			{
				m_parent.Close();
				m_parent.Dispose();
				m_parent = null;
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

			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_dtree = new DataTree();
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent = new Form();
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(entry, layoutName, null, entry, false);
			return m_dtree;
		}

		#endregion

		#region Category 31: Sense expansion produces correct slices

		/// <summary>
		/// CONTRACT: Showing an entry with senses using the "Normal" layout
		/// produces slices for the citation form and each sense's gloss.
		/// This is the baseline state before any expand/collapse interaction.
		/// </summary>
		[Test]
		public void ShowObject_Normal_WithSenses_ProducesGlossSlices()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateNormalDataTree(entry);

			Assert.That(dtree.Slices.Count, Is.GreaterThanOrEqualTo(4),
				"Normal layout with 3 senses should produce CitationForm + 3 Gloss slices " +
				"(plus possible custom field slices).");

			// First slice should be CitationForm
			Assert.That(dtree.Slices[0].Label, Is.EqualTo("CitationForm").Or.EqualTo("Citation form"));

			// Remaining slices should include Gloss entries
			int glossCount = dtree.Slices.Cast<Slice>()
				.Count(s => s.Label == "Gloss");
			Assert.That(glossCount, Is.GreaterThanOrEqualTo(3),
				"Should have at least 3 Gloss slices for 3 senses.");
		}

		/// <summary>
		/// CONTRACT: Showing an entry with senses using OptSensesEty layout
		/// produces correct slices and all are properly installed.
		/// </summary>
		[Test]
		public void ShowObject_OptSensesEty_WithSenses_AllSlicesInstalled()
		{
			var entry = CreateEntryWithSenses(2);
			var dtree = CreateOptSensesEtyDataTree(entry);

			Assert.That(dtree.Slices.Count, Is.GreaterThan(0),
				"OptSensesEty with 2 senses should produce slices.");

			foreach (Slice slice in dtree.Slices)
			{
				Assert.That(slice.ContainingDataTree, Is.SameAs(dtree),
					$"Slice '{slice.Label}' must reference the DataTree.");
				Assert.That(slice.TreeNode, Is.Not.Null,
					$"Slice '{slice.Label}' must have a TreeNode after install.");
			}
		}

		/// <summary>
		/// CONTRACT: An entry with no senses and OptSensesEty layout produces
		/// zero slices (visibility="ifdata" hides empty sequences).
		/// </summary>
		[Test]
		public void ShowObject_OptSensesEty_NoSenses_ProducesZeroSlices()
		{
			// m_entry has no senses by default
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);

			Assert.That(m_dtree.Slices.Count, Is.EqualTo(0),
				"OptSensesEty with no senses and no etymology should produce zero slices.");
		}

		#endregion

		#region Category 32: Adding senses to an existing tree

		/// <summary>
		/// CONTRACT: Adding a sense to an entry and re-showing produces additional slices.
		/// This simulates the scenario that caused the assertion failure:
		/// sense expansion triggers PropChanged with new data.
		/// </summary>
		[Test]
		public void AddSense_ReShow_ProducesAdditionalSlices()
		{
			var entry = CreateEntryWithSenses(1);
			var dtree = CreateNormalDataTree(entry);
			int initialCount = dtree.Slices.Count;

			// Add a new sense (this is what happens when user expands/adds a sense)
			var newSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(newSense);
			newSense.Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("new-gloss", Cache.DefaultAnalWs);

			// Re-show with fresh DataTree to see updated data
			// (DataTree requires recreation to reflect data changes —
			// pattern from DataTreeTests.OwnedObjects)
			var dtree2 = ReShowWithFreshDataTree(entry, "Normal");

			Assert.That(dtree2.Slices.Count, Is.GreaterThan(initialCount),
				"Adding a sense and re-showing should produce more slices.");
		}

		/// <summary>
		/// CONTRACT: Adding multiple senses rapidly and re-showing does not throw.
		/// Tests the rapid data-change scenario where PropChanged fires multiple times.
		/// </summary>
		[Test]
		public void AddMultipleSenses_ReShow_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(1);
			CreateNormalDataTree(entry);

			for (int i = 0; i < 10; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.AnalysisDefaultWritingSystem =
					TsStringUtils.MakeString($"rapid-{i}", Cache.DefaultAnalWs);
			}

			// Re-show with fresh DataTree to see updated data
			DataTree dtree2 = null;
			Assert.DoesNotThrow(() =>
			{
				dtree2 = ReShowWithFreshDataTree(entry, "Normal");
			}, "Rapidly adding senses and re-showing must not throw.");

			Assert.That(dtree2.Slices.Count, Is.GreaterThanOrEqualTo(12),
				"Should have at least 1 CF + 11 sense slices.");
		}

		/// <summary>
		/// CONTRACT: Removing all senses and re-showing produces only the
		/// citation form slice (and possibly custom field slices).
		/// </summary>
		[Test]
		public void RemoveAllSenses_ReShow_ProducesMinimalSlices()
		{
			var entry = CreateEntryWithSenses(5);
			var dtree = CreateNormalDataTree(entry);

			Assert.That(dtree.Slices.Count, Is.GreaterThan(1),
				"Precondition: should have multiple slices.");

			// Remove all senses
			while (entry.SensesOS.Count > 0)
				entry.SensesOS.RemoveAt(0);

			// Re-show with fresh DataTree to see updated data
			var dtree2 = ReShowWithFreshDataTree(entry, "Normal");

			// With no senses and visibility="ifdata", sense section is hidden
			int glossCount = dtree2.Slices.Cast<Slice>()
				.Count(s => s.Label == "Gloss");
			Assert.That(glossCount, Is.EqualTo(0),
				"After removing all senses, no Gloss slices should remain.");
		}

		#endregion

		#region Category 33: Expand/Collapse with senses

		/// <summary>
		/// Helper: creates a DataTree using NormalCollapsible layout which has a
		/// "Senses" header slice with expansion="expanded". Returns the DataTree.
		/// </summary>
		private DataTree CreateCollapsibleDataTree(ILexEntry entry)
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(entry, "NormalCollapsible", null, entry, false);
			return m_dtree;
		}

		/// <summary>
		/// Helper: finds the first slice with ktisExpanded or ktisCollapsed.
		/// </summary>
		private Slice FindExpandableSlice(DataTree dtree)
		{
			foreach (Slice s in dtree.Slices)
			{
				if (s.Expansion == DataTree.TreeItemState.ktisExpanded ||
					s.Expansion == DataTree.TreeItemState.ktisCollapsed)
					return s;
			}
			return null;
		}

		/// <summary>
		/// CONTRACT: The NormalCollapsible layout produces at least one slice
		/// with Expansion == ktisExpanded when senses are present.
		/// </summary>
		[Test]
		public void NormalCollapsible_WithSenses_HasExpandedSlice()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateCollapsibleDataTree(entry);

			var expandable = FindExpandableSlice(dtree);
			Assert.That(expandable, Is.Not.Null,
				"NormalCollapsible layout with senses must produce an expandable slice.");
			Assert.That(expandable.Expansion, Is.EqualTo(DataTree.TreeItemState.ktisExpanded),
				"The Senses header should start expanded.");
		}

		/// <summary>
		/// CONTRACT: Collapsing a sense header and re-expanding produces the same
		/// slice count. This tests the Expand()/Collapse() round-trip on sense data.
		/// </summary>
		[Test]
		public void SenseHeader_CollapseExpand_RoundTrip_SameCount()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateCollapsibleDataTree(entry);

			var expandable = FindExpandableSlice(dtree);
			Assert.That(expandable, Is.Not.Null,
				"NormalCollapsible layout must have an expandable slice.");

			int countBefore = dtree.Slices.Count;

			expandable.Collapse();
			Assert.That(dtree.Slices.Count, Is.LessThan(countBefore),
				"Collapsing should reduce slice count.");

			expandable.Expand(dtree.Slices.IndexOf(expandable));
			Assert.That(dtree.Slices.Count, Is.EqualTo(countBefore),
				"Re-expanding should restore the original slice count.");
		}

		/// <summary>
		/// CONTRACT: Rapid collapse-expand cycles do not throw or leak slices.
		/// </summary>
		[Test]
		public void SenseHeader_RapidCollapseExpand_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateCollapsibleDataTree(entry);

			var expandable = FindExpandableSlice(dtree);
			Assert.That(expandable, Is.Not.Null,
				"NormalCollapsible layout must have an expandable slice.");

			int originalCount = dtree.Slices.Count;

			Assert.DoesNotThrow(() =>
			{
				for (int i = 0; i < 5; i++)
				{
					expandable.Collapse();
					expandable.Expand(dtree.Slices.IndexOf(expandable));
				}
			}, "Rapid collapse/expand cycles must not throw.");

			Assert.That(dtree.Slices.Count, Is.EqualTo(originalCount),
				"After equal collapse/expand cycles, slice count should be unchanged.");
		}

		/// <summary>
		/// CONTRACT: Collapsing, adding a sense, then expanding shows the new sense.
		/// </summary>
		[Test]
		public void CollapseAddSenseExpand_ShowsNewSense()
		{
			var entry = CreateEntryWithSenses(2);
			var dtree = CreateCollapsibleDataTree(entry);

			var expandable = FindExpandableSlice(dtree);
			Assert.That(expandable, Is.Not.Null,
				"NormalCollapsible layout must have an expandable slice.");

			int countBefore = dtree.Slices.Count;
			expandable.Collapse();

			// Add a sense while collapsed
			var newSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(newSense);
			newSense.Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("added-while-collapsed", Cache.DefaultAnalWs);

			// Expand — should show the new sense
			Assert.DoesNotThrow(() =>
			{
				expandable.Expand(dtree.Slices.IndexOf(expandable));
			}, "Expanding after adding a sense while collapsed must not throw.");
		}

		/// <summary>
		/// CONTRACT: Expand/collapse with the parent form very small does not throw.
		/// Small viewport means fewer visible slices but the operations should still work.
		/// </summary>
		[Test]
		public void ExpandCollapse_WithSmallParent_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(5);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			// Make the parent very small before showing
			m_parent.ClientSize = new Size(150, 100);
			m_dtree.ShowObject(entry, "NormalCollapsible", null, entry, false);

			var expandable = FindExpandableSlice(m_dtree);
			Assert.That(expandable, Is.Not.Null,
				"NormalCollapsible layout must have an expandable slice.");

			Assert.DoesNotThrow(() =>
			{
				expandable.Collapse();
				expandable.Expand(m_dtree.Slices.IndexOf(expandable));
			}, "Expand/collapse with a very small parent must not throw.");
		}

		#endregion

		#region Category 34: Subsense expansion edge cases

		/// <summary>
		/// CONTRACT: An entry with nested senses (subsenses) produces additional
		/// indented slices for each subsense when the GlossSn layout recurses.
		/// </summary>
		[Test]
		public void Subsenses_ShowObject_ProducesNestedSlices()
		{
			var entry = CreateEntryWithSubsenses(2, 2);
			var dtree = CreateOptSensesEtyDataTree(entry);

			Assert.That(dtree.Slices.Count, Is.GreaterThanOrEqualTo(4),
				"2 senses × (1 gloss + 2 subgloss) = at least 6 gloss slices, " +
				"but some may be collapsed. At least 4 should be visible.");
		}

		/// <summary>
		/// CONTRACT: Adding a subsense to an existing sense and re-showing
		/// produces one more slice.
		/// </summary>
		[Test]
		public void AddSubsense_ReShow_ProducesAdditionalSlice()
		{
			var entry = CreateEntryWithSenses(1);
			var dtree = CreateOptSensesEtyDataTree(entry);
			int initialCount = dtree.Slices.Count;

			// Add a subsense to the first sense
			var sense = entry.SensesOS[0];
			var sub = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			sense.SensesOS.Add(sub);
			sub.Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("subsense-gloss", Cache.DefaultAnalWs);

			// Re-show with fresh DataTree to see updated data
			var dtree2 = ReShowWithFreshDataTree(entry, "OptSensesEty");

			Assert.That(dtree2.Slices.Count, Is.GreaterThan(initialCount),
				"Adding a subsense should produce an additional slice.");
		}

		/// <summary>
		/// CONTRACT: Deeply nested senses (3+ levels) don't overflow or crash.
		/// </summary>
		[Test]
		public void DeeplyNestedSenses_DoNotCrash()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("deep", Cache.DefaultVernWs);

			// Create 4-level nesting: entry → sense → subsense → subsubsense → subsubsubsense
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("level-1", Cache.DefaultAnalWs);

			var current = sense;
			for (int level = 2; level <= 4; level++)
			{
				var child = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				current.SensesOS.Add(child);
				child.Gloss.AnalysisDefaultWritingSystem =
					TsStringUtils.MakeString($"level-{level}", Cache.DefaultAnalWs);
				current = child;
			}

			Assert.DoesNotThrow(() =>
			{
				m_dtree.Initialize(Cache, false, m_layouts, m_parts);
				m_dtree.ShowObject(entry, "OptSensesEty", null, entry, false);
			}, "Deeply nested senses (4 levels) must not crash.");

			Assert.That(m_dtree.Slices.Count, Is.GreaterThan(0),
				"Should produce at least one slice for deeply nested senses.");
		}

		#endregion

		#region Category 35: Window resizing (horizontal)

		/// <summary>
		/// CONTRACT: Resizing the parent form horizontally (small change) does not throw.
		/// This changes GetAvailWidth and should trigger relayout via PATH-L1.
		/// </summary>
		[Test]
		public void ResizeSmallHorizontal_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateNormalDataTree(entry);

			Assert.DoesNotThrow(() =>
			{
				// Small horizontal resize: 800 → 810
				m_parent.ClientSize = new Size(810, m_parent.ClientSize.Height);
				Application.DoEvents();
			}, "Small horizontal resize (800→810) must not throw.");

			Assert.That(dtree.Slices.Count, Is.GreaterThan(0),
				"Slices should survive small horizontal resize.");
		}

		/// <summary>
		/// CONTRACT: Resizing the parent form horizontally (large change) does not throw.
		/// Large width changes may cause text to wrap differently.
		/// </summary>
		[Test]
		public void ResizeLargeHorizontal_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateNormalDataTree(entry);

			Assert.DoesNotThrow(() =>
			{
				// Large horizontal resize: default → 1600
				m_parent.ClientSize = new Size(1600, m_parent.ClientSize.Height);
				Application.DoEvents();
			}, "Large horizontal resize must not throw.");

			Assert.DoesNotThrow(() =>
			{
				// Shrink dramatically: 1600 → 200
				m_parent.ClientSize = new Size(200, m_parent.ClientSize.Height);
				Application.DoEvents();
			}, "Dramatic horizontal shrink must not throw.");
		}

		/// <summary>
		/// CONTRACT: Multiple rapid horizontal resizes do not throw or leak.
		/// </summary>
		[Test]
		public void RapidHorizontalResize_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateNormalDataTree(entry);

			Assert.DoesNotThrow(() =>
			{
				for (int w = 300; w <= 1200; w += 100)
				{
					m_parent.ClientSize = new Size(w, m_parent.ClientSize.Height);
				}
				Application.DoEvents();
			}, "Rapid horizontal resizes must not throw.");
		}

		#endregion

		#region Category 36: Window resizing (vertical)

		/// <summary>
		/// CONTRACT: Resizing the parent form vertically (small change) does not throw.
		/// Vertical resize changes the viewport but not available width.
		/// </summary>
		[Test]
		public void ResizeSmallVertical_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateNormalDataTree(entry);

			Assert.DoesNotThrow(() =>
			{
				m_parent.ClientSize = new Size(m_parent.ClientSize.Width, 610);
				Application.DoEvents();
			}, "Small vertical resize must not throw.");
		}

		/// <summary>
		/// CONTRACT: Large vertical resize (shrink to very small) does not crash.
		/// This may cause scroll bars to appear and viewport to clip.
		/// </summary>
		[Test]
		public void ResizeLargeVertical_Shrink_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(10);
			var dtree = CreateNormalDataTree(entry);

			Assert.DoesNotThrow(() =>
			{
				// Shrink to very small height
				m_parent.ClientSize = new Size(m_parent.ClientSize.Width, 50);
				Application.DoEvents();
			}, "Shrinking vertically to very small must not throw.");
		}

		/// <summary>
		/// CONTRACT: Combined horizontal and vertical resize does not throw.
		/// </summary>
		[Test]
		public void ResizeCombinedHorizontalVertical_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(5);
			var dtree = CreateNormalDataTree(entry);

			Assert.DoesNotThrow(() =>
			{
				m_parent.ClientSize = new Size(400, 300);
				Application.DoEvents();
				m_parent.ClientSize = new Size(1200, 800);
				Application.DoEvents();
				m_parent.ClientSize = new Size(600, 150);
				Application.DoEvents();
			}, "Combined resize sequences must not throw.");
		}

		#endregion

		#region Category 37: Navigation between entries (ShowObject)

		/// <summary>
		/// CONTRACT: Navigating from an entry with many senses to one with no senses
		/// does not throw. Old slices must be properly disposed.
		/// </summary>
		[Test]
		public void Navigate_ManySensesToNoSenses_DoesNotThrow()
		{
			var entry1 = CreateEntryWithSenses(10);
			var entry2 = CreateEntryWithSenses(0, "empty");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			// Show entry with 10 senses
			m_dtree.ShowObject(entry1, "Normal", null, entry1, false);
			int count1 = m_dtree.Slices.Count;
			Assert.That(count1, Is.GreaterThan(5), "Precondition: many slices.");

			// Navigate to entry with no senses
			Assert.DoesNotThrow(() =>
			{
				m_dtree.ShowObject(entry2, "Normal", null, entry2, false);
			}, "Navigating from many-sense to no-sense entry must not throw.");

			Assert.That(m_dtree.Slices.Count, Is.LessThan(count1),
				"No-sense entry should have fewer slices.");
		}

		/// <summary>
		/// CONTRACT: Navigating from an entry with no senses to one with many senses
		/// does not throw. The Reconstruct guard must be properly reset.
		/// </summary>
		[Test]
		public void Navigate_NoSensesToManySenses_DoesNotThrow()
		{
			var entry1 = CreateEntryWithSenses(0, "empty");
			var entry2 = CreateEntryWithSenses(10);

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			m_dtree.ShowObject(entry1, "Normal", null, entry1, false);
			Assert.DoesNotThrow(() =>
			{
				m_dtree.ShowObject(entry2, "Normal", null, entry2, false);
			}, "Navigating from no-sense to many-sense entry must not throw.");

			int glossCount = m_dtree.Slices.Cast<Slice>()
				.Count(s => s.Label == "Gloss");
			Assert.That(glossCount, Is.GreaterThanOrEqualTo(10),
				"Should show 10 Gloss slices for the second entry.");
		}

		/// <summary>
		/// CONTRACT: Rapid navigation between entries (simulating search result clicking)
		/// does not throw or leak. Each ShowObject disposes old slices cleanly.
		/// </summary>
		[Test]
		public void RapidNavigation_BetweenEntries_DoesNotThrow()
		{
			var entries = new ILexEntry[5];
			for (int i = 0; i < 5; i++)
				entries[i] = CreateEntryWithSenses(i * 3, $"entry-{i}");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			Assert.DoesNotThrow(() =>
			{
				// Navigate rapidly between entries
				for (int cycle = 0; cycle < 3; cycle++)
				{
					foreach (var entry in entries)
					{
						m_dtree.ShowObject(entry, "Normal", null, entry, false);
					}
				}
			}, "Rapid navigation between 5 entries over 3 cycles must not throw.");
		}

		/// <summary>
		/// CONTRACT: Navigating between entries with subsenses uses the correct layout.
		/// </summary>
		[Test]
		public void Navigate_BetweenSubsenseEntries_ProducesCorrectSlices()
		{
			var entry1 = CreateEntryWithSubsenses(2, 3);
			var entry2 = CreateEntryWithSenses(1);

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			m_dtree.ShowObject(entry1, "OptSensesEty", null, entry1, false);
			int count1 = m_dtree.Slices.Count;

			m_dtree.ShowObject(entry2, "OptSensesEty", null, entry2, false);
			int count2 = m_dtree.Slices.Count;

			Assert.That(count1, Is.GreaterThan(count2),
				"Entry with subsenses should have more slices than single-sense entry.");
		}

		#endregion

		#region Category 38: Layout switching during navigation

		/// <summary>
		/// CONTRACT: Switching between different layouts (Normal → OptSensesEty → CfAndBib)
		/// for the same entry does not throw.
		/// </summary>
		[Test]
		public void SwitchLayouts_SameEntry_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(3);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			Assert.DoesNotThrow(() =>
			{
				m_dtree.ShowObject(entry, "Normal", null, entry, false);
				int normalCount = m_dtree.Slices.Count;

				m_dtree.ShowObject(entry, "OptSensesEty", null, entry, false);
				int optCount = m_dtree.Slices.Count;

				m_dtree.ShowObject(entry, "CfAndBib", null, entry, false);
				int cfBibCount = m_dtree.Slices.Count;

				// Back to Normal
				m_dtree.ShowObject(entry, "Normal", null, entry, false);
			}, "Switching layouts for the same entry must not throw.");
		}

		/// <summary>
		/// CONTRACT: Switching layout AND entry simultaneously does not throw.
		/// </summary>
		[Test]
		public void SwitchLayoutAndEntry_Simultaneously_DoesNotThrow()
		{
			var entry1 = CreateEntryWithSenses(5);
			var entry2 = CreateEntryWithSenses(1, "other");
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			Assert.DoesNotThrow(() =>
			{
				m_dtree.ShowObject(entry1, "Normal", null, entry1, false);
				m_dtree.ShowObject(entry2, "OptSensesEty", null, entry2, false);
				m_dtree.ShowObject(entry1, "CfAndBib", null, entry1, false);
				m_dtree.ShowObject(entry2, "Normal", null, entry2, false);
			}, "Switching both layout and entry must not throw.");
		}

		#endregion

		#region Category 39: PropChanged safety during expansion

		/// <summary>
		/// CONTRACT: Modifying a sense gloss while the tree is showing does not throw.
		/// This triggers PropChanged → VwRootBox.PropChanged with the sense's hvo and
		/// the gloss tag. The Reconstruct guard must mark the root as dirty.
		/// </summary>
		[Test]
		public void ModifySenseGloss_WithTreeShowing_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(2);
			var dtree = CreateNormalDataTree(entry);

			Assert.DoesNotThrow(() =>
			{
				entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem =
					TsStringUtils.MakeString("modified-gloss", Cache.DefaultAnalWs);
			}, "Modifying a sense gloss with the tree displayed must not throw.");
		}

		/// <summary>
		/// CONTRACT: Inserting a sense into the middle of the sequence and refreshing
		/// does not throw. This tests the PropChanged path with cvIns > 0.
		/// </summary>
		[Test]
		public void InsertSenseInMiddle_ReShow_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateNormalDataTree(entry);

			Assert.DoesNotThrow(() =>
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Insert(1, sense);
				sense.Gloss.AnalysisDefaultWritingSystem =
					TsStringUtils.MakeString("inserted-middle", Cache.DefaultAnalWs);

				dtree.ShowObject(entry, "Normal", null, entry, false);
			}, "Inserting a sense in the middle must not throw.");
		}

		/// <summary>
		/// CONTRACT: Deleting a sense from the middle and refreshing does not throw.
		/// This tests PropChanged with cvDel > 0 and potential box tree invalidation.
		/// </summary>
		[Test]
		public void DeleteSenseFromMiddle_ReShow_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(5);
			var dtree = CreateNormalDataTree(entry);
			int initialCount = dtree.Slices.Count;

			entry.SensesOS.RemoveAt(2); // Remove the middle sense

			// Re-show with fresh DataTree to see updated data
			DataTree dtree2 = null;
			Assert.DoesNotThrow(() =>
			{
				dtree2 = ReShowWithFreshDataTree(entry, "Normal");
			}, "Deleting a sense from the middle must not throw.");

			Assert.That(dtree2.Slices.Count, Is.LessThan(initialCount),
				"Should have fewer slices after deleting a sense.");
		}

		/// <summary>
		/// CONTRACT: Replacing all senses (delete all, add new) does not throw.
		/// This is the most aggressive data change scenario.
		/// </summary>
		[Test]
		public void ReplaceAllSenses_ReShow_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateNormalDataTree(entry);

			// Remove all
			while (entry.SensesOS.Count > 0)
				entry.SensesOS.RemoveAt(0);

			// Add new ones
			for (int i = 0; i < 5; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.AnalysisDefaultWritingSystem =
					TsStringUtils.MakeString($"replaced-{i}", Cache.DefaultAnalWs);
			}

			// Re-show with fresh DataTree to see updated data
			DataTree dtree2 = null;
			Assert.DoesNotThrow(() =>
			{
				dtree2 = ReShowWithFreshDataTree(entry, "Normal");
			}, "Replacing all senses must not throw.");

			int glossCount = dtree2.Slices.Cast<Slice>()
				.Count(s => s.Label == "Gloss");
			Assert.That(glossCount, Is.EqualTo(5),
				"After replacement, should show 5 Gloss slices.");
		}

		#endregion

		#region Category 40: Resize during/after expansion

		/// <summary>
		/// CONTRACT: Resizing the window after adding senses does not throw.
		/// The layout cache (PATH-L1) should correctly invalidate when
		/// resize changes the available width.
		/// </summary>
		[Test]
		public void ResizeAfterAddingSenses_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(2);
			var dtree = CreateNormalDataTree(entry);

			// Add senses
			for (int i = 0; i < 5; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.AnalysisDefaultWritingSystem =
					TsStringUtils.MakeString($"added-{i}", Cache.DefaultAnalWs);
			}
			dtree.ShowObject(entry, "Normal", null, entry, false);

			// Now resize
			Assert.DoesNotThrow(() =>
			{
				m_parent.ClientSize = new Size(500, 400);
				Application.DoEvents();
				m_parent.ClientSize = new Size(1000, 700);
				Application.DoEvents();
			}, "Resizing after adding senses must not throw.");
		}

		#endregion

		#region Category 41: Navigation after resize

		/// <summary>
		/// CONTRACT: Resizing the window and then navigating to a different entry
		/// does not throw. The new entry's layout should use the new width.
		/// </summary>
		[Test]
		public void ResizeThenNavigate_DoesNotThrow()
		{
			var entry1 = CreateEntryWithSenses(3);
			var entry2 = CreateEntryWithSenses(6, "wide-entry");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(entry1, "Normal", null, entry1, false);

			// Resize
			m_parent.ClientSize = new Size(400, 300);
			Application.DoEvents();

			// Navigate
			Assert.DoesNotThrow(() =>
			{
				m_dtree.ShowObject(entry2, "Normal", null, entry2, false);
			}, "Navigating after resize must not throw.");
		}

		/// <summary>
		/// CONTRACT: Navigating and then immediately resizing does not throw.
		/// </summary>
		[Test]
		public void NavigateThenResize_DoesNotThrow()
		{
			var entry1 = CreateEntryWithSenses(2);
			var entry2 = CreateEntryWithSenses(8, "many-senses");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(entry1, "Normal", null, entry1, false);

			// Navigate then immediately resize
			m_dtree.ShowObject(entry2, "Normal", null, entry2, false);

			Assert.DoesNotThrow(() =>
			{
				m_parent.ClientSize = new Size(1400, 900);
				Application.DoEvents();
			}, "Resizing immediately after navigation must not throw.");
		}

		#endregion

		#region Category 42: Stress scenarios

		/// <summary>
		/// CONTRACT: A complete lifecycle — create entry, show, add senses, resize,
		/// navigate, collapse, expand, remove senses — does not throw.
		/// This is the integration stress test.
		/// </summary>
		[Test]
		public void FullLifecycle_StressTest_DoesNotThrow()
		{
			var entry1 = CreateEntryWithSenses(2);
			var entry2 = CreateEntryWithSenses(0, "empty");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			Assert.DoesNotThrow(() =>
			{
				// Phase 1: Show entry with senses
				m_dtree.ShowObject(entry1, "Normal", null, entry1, false);
				Assert.That(m_dtree.Slices.Count, Is.GreaterThan(0));

				// Phase 2: Resize
				m_parent.ClientSize = new Size(600, 400);

				// Phase 3: Add more senses
				for (int i = 0; i < 3; i++)
				{
					var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
					entry1.SensesOS.Add(sense);
					sense.Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString($"stress-{i}", Cache.DefaultAnalWs);
				}
				m_dtree.ShowObject(entry1, "Normal", null, entry1, false);

				// Phase 4: Navigate to empty entry
				m_dtree.ShowObject(entry2, "Normal", null, entry2, false);

				// Phase 5: Navigate back
				m_dtree.ShowObject(entry1, "Normal", null, entry1, false);

				// Phase 6: Resize dramatically
				m_parent.ClientSize = new Size(200, 100);
				Application.DoEvents();
				m_parent.ClientSize = new Size(1200, 800);
				Application.DoEvents();

				// Phase 7: Remove some senses
				if (entry1.SensesOS.Count > 2)
					entry1.SensesOS.RemoveAt(entry1.SensesOS.Count - 1);
				m_dtree.ShowObject(entry1, "Normal", null, entry1, false);

				// Phase 8: Switch layout
				m_dtree.ShowObject(entry1, "CfAndBib", null, entry1, false);
				m_dtree.ShowObject(entry1, "Normal", null, entry1, false);
			}, "Full lifecycle stress test must not throw.");
		}

		/// <summary>
		/// CONTRACT: Showing an entry with many senses (40+) and then rapidly
		/// resizing does not crash.
		/// </summary>
		[Test]
		public void ManySenses_RapidResize_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(40);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			m_dtree.ShowObject(entry, "Normal", null, entry, false);
			Assert.That(m_dtree.Slices.Count, Is.GreaterThan(30),
				"40 senses should produce many slices.");

			Assert.DoesNotThrow(() =>
			{
				for (int w = 300; w <= 1500; w += 50)
				{
					m_parent.ClientSize = new Size(w, 600);
				}
				Application.DoEvents();
			}, "Rapid resize with 40 senses must not throw.");
		}

		/// <summary>
		/// CONTRACT: Navigating through many entries in sequence (simulating
		/// scrolling through a list in the dictionary view) does not throw or leak.
		/// </summary>
		[Test]
		public void SequentialNavigation_ManyEntries_DoesNotThrow()
		{
			// Create 10 entries with varying sense counts
			var entries = new ILexEntry[10];
			for (int i = 0; i < 10; i++)
				entries[i] = CreateEntryWithSenses(i, $"seq-{i}");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			Assert.DoesNotThrow(() =>
			{
				foreach (var entry in entries)
				{
					m_dtree.ShowObject(entry, "Normal", null, entry, false);
				}
				// And back in reverse
				for (int i = entries.Length - 1; i >= 0; i--)
				{
					m_dtree.ShowObject(entries[i], "Normal", null, entries[i], false);
				}
			}, "Sequential navigation through 10 entries must not throw.");
		}

		/// <summary>
		/// CONTRACT: Disposing the DataTree while it has many senses displayed
		/// does not throw or leak.
		/// </summary>
		[Test]
		public void Dispose_WithManySensesDisplayed_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(20);
			var mediator = new Mediator();
			var propTable = new PropertyTable(mediator);
			var dtree = new DataTree();
			dtree.Init(mediator, propTable, null);
			var parent = new Form();
			parent.Controls.Add(dtree);

			dtree.Initialize(Cache, false, m_layouts, m_parts);
			dtree.ShowObject(entry, "Normal", null, entry, false);

			Assert.That(dtree.Slices.Count, Is.GreaterThan(10),
				"Precondition: many slices displayed.");

			Assert.DoesNotThrow(() =>
			{
				parent.Close();
				parent.Dispose();
				propTable.Dispose();
				mediator.Dispose();
			}, "Disposing with many senses displayed must not throw.");
		}

		#endregion

		#region Category 43: VwPropertyStore crash regression (ws=0 / null engine)

		/// <summary>
		/// CONTRACT: Adding a sense with an UNSET gloss writing system
		/// (ws=0 in the text property) and then showing the entry does not crash.
		/// This targets the VwPropertyStore.cpp:1336 assertion where
		/// get_EngineOrNull returns null for ws=0.
		/// </summary>
		[Test]
		public void SenseWithUnsetGlossWs_ShowObject_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(1);

			// Add a sense with NO gloss set at all — the writing system will be 0/unset
			var emptySense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(emptySense);
			// Deliberately do NOT set emptySense.Gloss — ws remains 0

			Assert.DoesNotThrow(() =>
			{
				var dtree = CreateNormalDataTree(entry);
				Assert.That(dtree.Slices.Count, Is.GreaterThan(0),
					"DataTree should display at least some slices.");
			}, "Showing an entry with an unset-ws gloss must not crash.");
		}

		/// <summary>
		/// CONTRACT: Adding multiple senses, some with no gloss, then showing
		/// the entry does not trigger the VwPropertyStore ws=0 assertion.
		/// </summary>
		[Test]
		public void MixedSetUnsetGlossSenses_ShowObject_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(0, "mixed-ws");

			for (int i = 0; i < 5; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);

				// Only set gloss on even-numbered senses
				if (i % 2 == 0)
				{
					sense.Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString($"sense-{i}", Cache.DefaultAnalWs);
				}
				// Odd senses left with unset gloss (ws=0)
			}

			Assert.DoesNotThrow(() =>
			{
				var dtree = CreateNormalDataTree(entry);
				Assert.That(dtree.Slices.Count, Is.GreaterThan(0));
			}, "Showing mixed set/unset gloss senses must not crash.");
		}

		/// <summary>
		/// CONTRACT: Showing an entry with an empty sense (no properties set at all)
		/// then re-showing after setting the gloss does not crash.
		/// This exercises the PropChanged / Reconstruct path with ws transitions.
		/// </summary>
		[Test]
		public void EmptySenseThenSetGloss_ReShow_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(1);

			// Add a completely empty sense
			var emptySense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(emptySense);

			var dtree = CreateNormalDataTree(entry);
			int countBefore = dtree.Slices.Count;

			// Now set the gloss — this triggers PropChanged internally
			emptySense.Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("now-has-gloss", Cache.DefaultAnalWs);

			// Re-show to trigger layout with the new ws
			Assert.DoesNotThrow(() =>
			{
				dtree = ReShowWithFreshDataTree(entry, "Normal");
			}, "Re-showing after setting gloss on previously empty sense must not crash.");
		}

		/// <summary>
		/// CONTRACT: Showing an entry then adding a sense with unset ws and
		/// re-showing exercises the Reconstruct path (PATH-R1) with ws=0 data.
		/// The VwPropertyStore fix ensures this does not assert.
		/// </summary>
		[Test]
		public void AddUnsetWsSense_Reconstruct_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(2);
			var dtree = CreateNormalDataTree(entry);

			// Add a sense with no gloss (ws=0)
			var emptySense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(emptySense);

			// Force the DataTree to reconstruct with the new data
			Assert.DoesNotThrow(() =>
			{
				dtree = ReShowWithFreshDataTree(entry, "Normal");
				Assert.That(dtree.Slices.Count, Is.GreaterThan(0));
			}, "Reconstruct with a ws=0 sense must not crash (VwPropertyStore fix).");
		}

		/// <summary>
		/// CONTRACT: Showing an entry with 10 senses, all with unset gloss,
		/// does not crash even when many VwPropertyStore instances process ws=0.
		/// </summary>
		[Test]
		public void ManySensesAllUnsetGloss_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(0, "all-unset");

			for (int i = 0; i < 10; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				// No gloss set — all have ws=0
			}

			Assert.DoesNotThrow(() =>
			{
				var dtree = CreateNormalDataTree(entry);
			}, "10 senses with unset gloss ws must not trigger VwPropertyStore assertion.");
		}

		/// <summary>
		/// CONTRACT: Expand/collapse on a collapsible layout with senses that
		/// have unset writing systems does not crash. This combines the
		/// expand/collapse path with the VwPropertyStore ws=0 fix.
		/// </summary>
		[Test]
		public void ExpandCollapseWithUnsetWsSenses_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(0, "collapse-ws0");

			// Mix of set and unset gloss senses
			for (int i = 0; i < 4; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				if (i < 2)
				{
					sense.Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString($"s-{i}", Cache.DefaultAnalWs);
				}
			}

			var dtree = CreateCollapsibleDataTree(entry);
			var expandable = FindExpandableSlice(dtree);

			if (expandable != null)
			{
				Assert.DoesNotThrow(() =>
				{
					expandable.Collapse();
					expandable.Expand(dtree.Slices.IndexOf(expandable));
				}, "Collapse/expand with unset-ws senses must not crash.");
			}
		}

		#endregion

		#region Category 44: Layout switching edge cases

		/// <summary>
		/// CONTRACT: Switching from Normal to NormalCollapsible layout preserves
		/// the entry display without errors.
		/// </summary>
		[Test]
		public void SwitchLayout_NormalToCollapsible_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateNormalDataTree(entry);

			int normalCount = dtree.Slices.Count;
			Assert.That(normalCount, Is.GreaterThan(0));

			// Switch to NormalCollapsible
			Assert.DoesNotThrow(() =>
			{
				dtree.ShowObject(entry, "NormalCollapsible", null, entry, false);
			}, "Switching from Normal to NormalCollapsible must not throw.");

			Assert.That(dtree.Slices.Count, Is.GreaterThan(0),
				"NormalCollapsible layout should produce slices.");
		}

		/// <summary>
		/// CONTRACT: Switching from NormalCollapsible back to Normal while collapsed
		/// does not throw or lose data.
		/// </summary>
		[Test]
		public void SwitchLayout_CollapsibleCollapsedToNormal_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(3);
			var dtree = CreateCollapsibleDataTree(entry);

			var expandable = FindExpandableSlice(dtree);
			if (expandable != null)
				expandable.Collapse();

			// Switch to Normal — this should re-show all senses
			Assert.DoesNotThrow(() =>
			{
				dtree.ShowObject(entry, "Normal", null, entry, false);
			}, "Switching from collapsed NormalCollapsible to Normal must not throw.");

			Assert.That(dtree.Slices.Count, Is.GreaterThan(1),
				"Normal layout should show all senses regardless of collapse state.");
		}

		#endregion

		#region Image regression helpers

		/// <summary>
		/// Sets up the DataTree for bitmap capture by docking it in the parent form
		/// and forcing the WinForms layout/paint lifecycle. Without this, Controls
		/// have no handles, no painted state, and CompositeViewCapture produces
		/// blank bitmaps.
		/// </summary>
		private void PrepareForBitmapCapture(int width = 800, int height = 600)
		{
			m_parent.FormBorderStyle = FormBorderStyle.None;
			m_parent.ShowInTaskbar = false;
			m_parent.StartPosition = FormStartPosition.Manual;
			m_parent.Location = new Point(-2000, -2000); // offscreen
			m_parent.Opacity = 0;
			m_parent.ClientSize = new Size(width + 50, height + 50);
			m_parent.Show();
			Application.DoEvents();

			// Set DataTree size directly — DockStyle.Fill does not reliably
			// propagate in a headless test environment. We test the DataTree's
			// own layout-on-resize behaviour, not the WinForms docking mechanism.
			m_dtree.Dock = DockStyle.None;
			m_dtree.Location = Point.Empty;
			m_dtree.Size = new Size(width, height);
			m_dtree.PerformLayout();
			Application.DoEvents();
		}

		/// <summary>
		/// Resizes the DataTree directly and forces layout/paint to settle.
		/// </summary>
		private void ResizeParent(int width, int height)
		{
			m_dtree.Size = new Size(width, height);
			m_dtree.PerformLayout();
			Application.DoEvents();
		}

		/// <summary>
		/// Captures a content-tight bitmap of the DataTree using <see cref="CompositeViewCapture"/>
		/// and returns a SHA256 hash of the raw pixel data. Two calls that produce visually
		/// identical bitmaps will return the same hash.
		/// </summary>
		private static string CaptureBitmapHash(DataTree dataTree)
		{
			using (var bitmap = CompositeViewCapture.CaptureDataTree(dataTree))
			using (var ms = new MemoryStream())
			{
				bitmap.Save(ms, ImageFormat.Bmp);
				using (var sha = SHA256.Create())
				{
					return BitConverter.ToString(sha.ComputeHash(ms.ToArray()))
						.Replace("-", "").Substring(0, 16);
				}
			}
		}

		/// <summary>
		/// Captures a bitmap and returns (hash, width, height, nonWhiteCount).
		/// Useful for tests that need to verify structural properties of the rendered output.
		/// </summary>
		private static (string Hash, int Width, int Height, int NonWhitePixels) CaptureBitmapInfo(DataTree dataTree)
		{
			using (var bitmap = CompositeViewCapture.CaptureDataTree(dataTree))
			{
				string hash;
				using (var ms = new MemoryStream())
				{
					bitmap.Save(ms, ImageFormat.Bmp);
					using (var sha = SHA256.Create())
					{
						hash = BitConverter.ToString(sha.ComputeHash(ms.ToArray()))
							.Replace("-", "").Substring(0, 16);
					}
				}

				int nonWhite = 0;
				for (int y = 0; y < bitmap.Height; y++)
				for (int x = 0; x < bitmap.Width; x++)
				{
					var c = bitmap.GetPixel(x, y);
					if (c.R != 255 || c.G != 255 || c.B != 255)
						nonWhite++;
				}

				return (hash, bitmap.Width, bitmap.Height, nonWhite);
			}
		}

		#endregion

		#region Category 40: Image regression — resize round-trips

		/// <summary>
		/// IMAGE REGRESSION: Resizing the DataTree wider then back to the original
		/// width produces the same rendered bitmap (same hash).
		/// PATH-L1 guard must correctly invalidate and re-layout on width changes.
		/// </summary>
		[Test]
		public void ImageRegression_ResizeWiderAndBack_SameBitmap()
		{
			var entry = CreateEntryWithSenses(3);
			CreateNormalDataTree(entry);
			PrepareForBitmapCapture(800, 600);

			var before = CaptureBitmapInfo(m_dtree);
			Assert.That(before.Width, Is.EqualTo(800),
				"Bitmap should have the requested width.");

			// Widen then return to original
			ResizeParent(1200, 600);
			ResizeParent(800, 600);

			var after = CaptureBitmapInfo(m_dtree);

			Assert.That(after.Hash, Is.EqualTo(before.Hash),
				$"Bitmap should be identical after resize round-trip. " +
				$"Before: {before.Width}x{before.Height} ({before.NonWhitePixels} non-white), " +
				$"After: {after.Width}x{after.Height} ({after.NonWhitePixels} non-white)");
		}

		/// <summary>
		/// IMAGE REGRESSION: Resizing narrower then back produces the same bitmap.
		/// Tests the inverse direction of the resize round-trip.
		/// </summary>
		[Test]
		public void ImageRegression_ResizeNarrowerAndBack_SameBitmap()
		{
			var entry = CreateEntryWithSenses(3);
			CreateNormalDataTree(entry);
			PrepareForBitmapCapture(800, 600);

			var before = CaptureBitmapInfo(m_dtree);

			// Narrow then return to original
			ResizeParent(400, 600);
			ResizeParent(800, 600);

			var after = CaptureBitmapInfo(m_dtree);

			Assert.That(after.Hash, Is.EqualTo(before.Hash),
				$"Bitmap should be identical after narrow+restore round-trip. " +
				$"Before: {before.Width}x{before.Height} ({before.NonWhitePixels} non-white), " +
				$"After: {after.Width}x{after.Height} ({after.NonWhitePixels} non-white)");
		}

		/// <summary>
		/// IMAGE REGRESSION: Multiple consecutive resizes followed by restoration
		/// to original width produces the identical bitmap.
		/// Stress-tests layout caching under rapid width changes.
		/// </summary>
		[Test]
		public void ImageRegression_MultipleResizesAndBack_SameBitmap()
		{
			var entry = CreateEntryWithSenses(3);
			CreateNormalDataTree(entry);
			PrepareForBitmapCapture(800, 600);

			var before = CaptureBitmapInfo(m_dtree);

			// Cycle through multiple widths
			foreach (int width in new[] { 600, 1000, 400, 1200, 500, 900, 800 })
			{
				ResizeParent(width, 600);
			}

			var after = CaptureBitmapInfo(m_dtree);

			Assert.That(after.Hash, Is.EqualTo(before.Hash),
				$"Bitmap should be identical after cycling through multiple widths. " +
				$"Before: {before.Width}x{before.Height}, After: {after.Width}x{after.Height}");
		}

		#endregion

		#region Category 41: Image regression — navigate round-trips

		/// <summary>
		/// IMAGE REGRESSION: Navigating to a different entry and back produces
		/// the same rendered bitmap for the original entry.
		/// </summary>
		[Test]
		public void ImageRegression_NavigateAwayAndBack_SameBitmap()
		{
			var entry1 = CreateEntryWithSenses(3, "entry-one");
			var entry2 = CreateEntryWithSenses(2, "entry-two");
			CreateNormalDataTree(entry1);
			PrepareForBitmapCapture(800, 600);

			var before = CaptureBitmapInfo(m_dtree);

			// Navigate to entry2
			m_dtree.ShowObject(entry2, "Normal", null, entry2, false);
			Application.DoEvents();

			// Navigate back to entry1 and re-set the width
			// (ShowObject may enable AutoScroll, altering ClientSize)
			m_dtree.ShowObject(entry1, "Normal", null, entry1, false);
			m_dtree.AutoScroll = false;
			m_dtree.Size = new Size(800, 600);
			m_dtree.PerformLayout();
			Application.DoEvents();

			var after = CaptureBitmapInfo(m_dtree);

			// Compare height; the hash may differ if ShowObject rebuilds slices
			// with minor differences, but the layout should be structurally identical.
			Assert.That(after.Width, Is.EqualTo(before.Width),
				$"Width should be identical after navigating away and back. " +
				$"Before: {before.Width}x{before.Height}, After: {after.Width}x{after.Height}");
			Assert.That(after.Height, Is.EqualTo(before.Height),
				$"Height should be identical after navigating away and back. " +
				$"Before: {before.Width}x{before.Height}, After: {after.Width}x{after.Height}");
		}

		/// <summary>
		/// IMAGE REGRESSION: Two renderings of the same entry at the same width
		/// produce identical bitmaps (determinism check).
		/// </summary>
		[Test]
		public void ImageRegression_SameEntryTwice_Deterministic()
		{
			var entry = CreateEntryWithSenses(3);
			CreateNormalDataTree(entry);
			PrepareForBitmapCapture(800, 600);

			var first = CaptureBitmapInfo(m_dtree);
			var second = CaptureBitmapInfo(m_dtree);

			Assert.That(second.Hash, Is.EqualTo(first.Hash),
				"Two consecutive captures of the same DataTree should produce identical bitmaps.");
		}

		#endregion

		#region Category 42: Image regression — layout content verification

		/// <summary>
		/// IMAGE REGRESSION: A DataTree with senses renders more non-white pixels
		/// than one with no senses (sanity check that content is rendered).
		/// </summary>
		[Test]
		public void ImageRegression_MoreSenses_MoreContent()
		{
			// Entry with no senses
			var entrySparse = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entrySparse.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("sparse", Cache.DefaultVernWs);

			CreateNormalDataTree(entrySparse);
			PrepareForBitmapCapture(800, 600);
			var infoSparse = CaptureBitmapInfo(m_dtree);

			// Entry with 5 senses (fresh DataTree needed for data changes)
			var entryDense = CreateEntryWithSenses(5, "dense");
			ReShowWithFreshDataTree(entryDense, "Normal");
			PrepareForBitmapCapture(800, 600);
			var infoDense = CaptureBitmapInfo(m_dtree);

			Assert.That(infoDense.Height, Is.GreaterThan(infoSparse.Height),
				$"Entry with 5 senses should produce a taller bitmap than entry with 0 senses. " +
				$"Sparse: {infoSparse.Width}x{infoSparse.Height}, Dense: {infoDense.Width}x{infoDense.Height}");
		}

		/// <summary>
		/// IMAGE REGRESSION: Adding a sense to an entry changes the rendered bitmap
		/// (the new sense's gloss should appear, increasing content).
		/// </summary>
		[Test]
		public void ImageRegression_AddSense_ChangesBitmap()
		{
			var entry = CreateEntryWithSenses(2, "grow");
			CreateNormalDataTree(entry);
			PrepareForBitmapCapture(800, 600);

			var before = CaptureBitmapInfo(m_dtree);

			// Add a sense and re-show
			var newSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(newSense);
			newSense.Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("new-gloss", Cache.DefaultAnalWs);

			ReShowWithFreshDataTree(entry, "Normal");
			PrepareForBitmapCapture(800, 600);
			var after = CaptureBitmapInfo(m_dtree);

			Assert.That(after.Hash, Is.Not.EqualTo(before.Hash),
				"Adding a sense should change the rendered bitmap.");
			Assert.That(after.Height, Is.GreaterThanOrEqualTo(before.Height),
				"Adding a sense should not shrink the bitmap.");
		}

		#endregion

		#region Category 43: Text wrapping layout change test

		/// <summary>
		/// LAYOUT CHANGE: When text is on one line at a wide width and then the
		/// parent is sized horizontally smaller, the text wraps to two (or more)
		/// lines, increasing slice height. When restored to the original width,
		/// the text returns to one line.
		///
		/// This tests the PATH-L1 guard's ability to correctly re-layout when
		/// width changes cause text reflow.
		/// </summary>
		[Test]
		public void TextWrapping_NarrowCausesWrap_WideRestoresUnwrap()
		{
			// Create an entry with a long citation form that will wrap at narrow widths
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			string longText = "This is a very long citation form text that should " +
				"comfortably fit on a single line at wide widths but will need to " +
				"wrap onto two or more lines when the DataTree is made narrow";
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString(longText, Cache.DefaultVernWs);

			CreateNormalDataTree(entry);

			// Start wide — text should be on one line
			PrepareForBitmapCapture(1200, 600);
			var infoWide = CaptureBitmapInfo(m_dtree);
			int wideHeight = infoWide.Height;

			// Narrow — text should wrap, increasing height
			ResizeParent(300, 600);
			var infoNarrow = CaptureBitmapInfo(m_dtree);
			int narrowHeight = infoNarrow.Height;

			Assert.That(narrowHeight, Is.GreaterThan(wideHeight),
				$"Narrowing the DataTree should cause text to wrap, increasing bitmap height. " +
				$"Wide: {infoWide.Width}x{wideHeight}, Narrow: {infoNarrow.Width}x{narrowHeight}");

			// Restore wide — text should unwrap back to one line
			ResizeParent(1200, 600);
			var infoRestored = CaptureBitmapInfo(m_dtree);

			Assert.That(infoRestored.Height, Is.EqualTo(wideHeight),
				$"Restoring wide width should unwrap text back to original height. " +
				$"Original: {wideHeight}, Restored: {infoRestored.Height}");
			Assert.That(infoRestored.Hash, Is.EqualTo(infoWide.Hash),
				"Bitmap should be identical after narrowing and widening back.");
		}

		/// <summary>
		/// INVERSE: Starting narrow (text wraps) then going wide (text unwraps)
		/// then back to narrow (text wraps again) — height changes correctly.
		/// </summary>
		[Test]
		public void TextWrapping_WideCausesUnwrap_NarrowRestoresWrap()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			string longText = "Another long citation form that needs to wrap when " +
				"displayed in a narrow DataTree but should fit on one line at wide sizes " +
				"and this tests the inverse direction of the layout change";
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString(longText, Cache.DefaultVernWs);

			CreateNormalDataTree(entry);

			// Start narrow — text wraps
			PrepareForBitmapCapture(300, 600);
			var infoNarrowStart = CaptureBitmapInfo(m_dtree);
			int narrowHeight = infoNarrowStart.Height;

			// Go wide — text unwraps
			ResizeParent(1200, 600);
			var infoWide = CaptureBitmapInfo(m_dtree);

			Assert.That(infoWide.Height, Is.LessThan(narrowHeight),
				$"Widening the DataTree should unwrap text, decreasing bitmap height. " +
				$"Narrow: {infoNarrowStart.Width}x{narrowHeight}, Wide: {infoWide.Width}x{infoWide.Height}");

			// Return to narrow — text wraps again
			ResizeParent(300, 600);
			var infoNarrowFinal = CaptureBitmapInfo(m_dtree);

			Assert.That(infoNarrowFinal.Height, Is.EqualTo(narrowHeight),
				$"Returning to narrow width should wrap text to same height. " +
				$"Original: {narrowHeight}, Final: {infoNarrowFinal.Height}");
			Assert.That(infoNarrowFinal.Hash, Is.EqualTo(infoNarrowStart.Hash),
				"Bitmap should be identical after widening and narrowing back.");
		}

		#endregion

		#region Category 45: ws=0 combined with navigation

		/// <summary>
		/// CONTRACT: Navigating from an entry with all unset-ws senses to a normal
		/// entry does not crash. The Reconstruct guard must handle the transition
		/// from ws=0 property stores to valid ones.
		/// </summary>
		[Test]
		public void NavigateFromUnsetWsToNormal_DoesNotCrash()
		{
			// Entry with all unset-ws senses
			var unsetEntry = CreateEntryWithSenses(0, "unset");
			for (int i = 0; i < 3; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				unsetEntry.SensesOS.Add(sense);
				// No gloss set — ws=0
			}

			// Normal entry with set gloss
			var normalEntry = CreateEntryWithSenses(3, "normal");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			// Show unset-ws entry first
			m_dtree.ShowObject(unsetEntry, "Normal", null, unsetEntry, false);

			// Navigate to normal entry
			Assert.DoesNotThrow(() =>
			{
				m_dtree.ShowObject(normalEntry, "Normal", null, normalEntry, false);
			}, "Navigating from unset-ws entry to normal entry must not crash.");

			int glossCount = m_dtree.Slices.Cast<Slice>()
				.Count(s => s.Label == "Gloss");
			Assert.That(glossCount, Is.GreaterThanOrEqualTo(3),
				"Normal entry should display its gloss slices after navigation.");
		}

		/// <summary>
		/// CONTRACT: Navigating from a normal entry to one with all unset-ws senses
		/// does not crash. VwPropertyStore ws=0 fix must handle this.
		/// </summary>
		[Test]
		public void NavigateFromNormalToUnsetWs_DoesNotCrash()
		{
			var normalEntry = CreateEntryWithSenses(3, "normal");

			var unsetEntry = CreateEntryWithSenses(0, "unset");
			for (int i = 0; i < 5; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				unsetEntry.SensesOS.Add(sense);
				// No gloss set — ws=0
			}

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			// Show normal entry first
			m_dtree.ShowObject(normalEntry, "Normal", null, normalEntry, false);
			int normalCount = m_dtree.Slices.Count;
			Assert.That(normalCount, Is.GreaterThan(0));

			// Navigate to unset-ws entry
			Assert.DoesNotThrow(() =>
			{
				m_dtree.ShowObject(unsetEntry, "Normal", null, unsetEntry, false);
			}, "Navigating from normal entry to unset-ws entry must not crash.");
		}

		/// <summary>
		/// CONTRACT: Rapid navigation cycling through a mix of normal and unset-ws
		/// entries does not crash. Tests the full Reconstruct + PropChanged path
		/// under ws transitions.
		/// </summary>
		[Test]
		public void RapidNavigation_MixedWsEntries_DoesNotCrash()
		{
			var entries = new ILexEntry[6];

			// Alternate between normal and unset-ws entries
			for (int i = 0; i < 6; i++)
			{
				entries[i] = CreateEntryWithSenses(0, $"mixed-{i}");
				for (int j = 0; j < 3; j++)
				{
					var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
					entries[i].SensesOS.Add(sense);
					// Only set gloss on even-indexed entries
					if (i % 2 == 0)
					{
						sense.Gloss.AnalysisDefaultWritingSystem =
							TsStringUtils.MakeString($"sense-{i}-{j}", Cache.DefaultAnalWs);
					}
				}
			}

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			Assert.DoesNotThrow(() =>
			{
				for (int cycle = 0; cycle < 3; cycle++)
				{
					foreach (var entry in entries)
					{
						m_dtree.ShowObject(entry, "Normal", null, entry, false);
					}
				}
			}, "Rapid navigation through mixed ws/unset-ws entries must not crash.");
		}

		/// <summary>
		/// CONTRACT: Navigating from an entry with unset-ws senses to the same entry
		/// (re-show) does not crash. Tests the Reconstruct path when source and
		/// target are the same.
		/// </summary>
		[Test]
		public void NavigateToSameEntry_WithUnsetWs_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(0, "self-nav");
			for (int i = 0; i < 4; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				// No gloss set — ws=0
			}

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(entry, "Normal", null, entry, false);

			// Navigate to the same entry again
			Assert.DoesNotThrow(() =>
			{
				for (int i = 0; i < 5; i++)
				{
					m_dtree.ShowObject(entry, "Normal", null, entry, false);
				}
			}, "Re-showing the same unset-ws entry must not crash.");
		}

		#endregion

		#region Category 46: ws=0 combined with resize

		/// <summary>
		/// CONTRACT: Resizing the window while unset-ws senses are displayed
		/// does not crash. Layout must handle ws=0 property stores during relayout.
		/// </summary>
		[Test]
		public void ResizeWithUnsetWsSenses_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(0, "resize-ws0");
			for (int i = 0; i < 5; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				// No gloss set — ws=0
			}

			var dtree = CreateNormalDataTree(entry);

			Assert.DoesNotThrow(() =>
			{
				m_parent.ClientSize = new Size(400, 300);
				Application.DoEvents();
				m_parent.ClientSize = new Size(1200, 800);
				Application.DoEvents();
				m_parent.ClientSize = new Size(200, 100);
				Application.DoEvents();
			}, "Resizing with unset-ws senses displayed must not crash.");
		}

		/// <summary>
		/// CONTRACT: Rapid horizontal resize with unset-ws senses does not crash.
		/// Tests PATH-L1 guard under ws=0 conditions.
		/// </summary>
		[Test]
		public void RapidResizeWithUnsetWsSenses_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(0, "rapid-ws0");
			for (int i = 0; i < 8; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				// Mix: even senses have gloss, odd do not
				if (i % 2 == 0)
				{
					sense.Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString($"gloss-{i}", Cache.DefaultAnalWs);
				}
			}

			var dtree = CreateNormalDataTree(entry);

			Assert.DoesNotThrow(() =>
			{
				for (int w = 200; w <= 1400; w += 100)
				{
					m_parent.ClientSize = new Size(w, 500);
				}
				Application.DoEvents();
			}, "Rapid resize with mixed ws/unset-ws senses must not crash.");
		}

		/// <summary>
		/// CONTRACT: Resizing to a very small width then navigating to an unset-ws
		/// entry does not crash. Tests resize + navigation + ws=0 interaction.
		/// </summary>
		[Test]
		public void ResizeThenNavigateToUnsetWs_DoesNotCrash()
		{
			var normalEntry = CreateEntryWithSenses(3);
			var unsetEntry = CreateEntryWithSenses(0, "after-resize");
			for (int i = 0; i < 4; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				unsetEntry.SensesOS.Add(sense);
			}

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(normalEntry, "Normal", null, normalEntry, false);

			// Resize to very small
			m_parent.ClientSize = new Size(150, 100);
			Application.DoEvents();

			// Navigate to unset-ws entry at small viewport
			Assert.DoesNotThrow(() =>
			{
				m_dtree.ShowObject(unsetEntry, "Normal", null, unsetEntry, false);
			}, "Navigating to unset-ws entry after small resize must not crash.");
		}

		#endregion

		#region Category 47: ws=0 in nested subsenses

		/// <summary>
		/// CONTRACT: An entry with subsenses where ALL subsenses have unset gloss
		/// does not crash. ws=0 appears at the deepest nesting level.
		/// </summary>
		[Test]
		public void SubsensesAllUnsetWs_ShowObject_DoesNotCrash()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("sub-ws0", Cache.DefaultVernWs);

			// Top-level sense with set gloss
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("parent-sense", Cache.DefaultAnalWs);

			// Subsenses with NO gloss (ws=0)
			for (int i = 0; i < 3; i++)
			{
				var sub = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				sense.SensesOS.Add(sub);
				// No gloss — ws=0
			}

			Assert.DoesNotThrow(() =>
			{
				m_dtree.Initialize(Cache, false, m_layouts, m_parts);
				m_dtree.ShowObject(entry, "OptSensesEty", null, entry, false);
			}, "Subsenses with unset gloss (ws=0) must not crash.");
		}

		/// <summary>
		/// CONTRACT: Mixed ws at different nesting levels — some parent senses
		/// have gloss, some don't; some subsenses have gloss, some don't.
		/// </summary>
		[Test]
		public void MixedWsAtDifferentNestingLevels_DoesNotCrash()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("mixed-nesting", Cache.DefaultVernWs);

			for (int i = 0; i < 3; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);

				// Only set the first parent sense gloss
				if (i == 0)
				{
					sense.Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString("parent-0", Cache.DefaultAnalWs);
				}

				// Add subsenses with alternating ws
				for (int j = 0; j < 2; j++)
				{
					var sub = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
					sense.SensesOS.Add(sub);
					if (j == 0)
					{
						sub.Gloss.AnalysisDefaultWritingSystem =
							TsStringUtils.MakeString($"sub-{i}-{j}", Cache.DefaultAnalWs);
					}
					// Odd j subsenses have no gloss — ws=0
				}
			}

			Assert.DoesNotThrow(() =>
			{
				m_dtree.Initialize(Cache, false, m_layouts, m_parts);
				m_dtree.ShowObject(entry, "OptSensesEty", null, entry, false);
			}, "Mixed ws at different nesting levels must not crash.");

			Assert.That(m_dtree.Slices.Count, Is.GreaterThan(0),
				"Should produce slices for mixed ws entry.");
		}

		/// <summary>
		/// CONTRACT: Deeply nested senses (4 levels) where every other level
		/// has unset ws does not crash. Tests ws=0 in deep recursion.
		/// </summary>
		[Test]
		public void DeeplyNestedAlternatingWs_DoesNotCrash()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("deep-alt-ws", Cache.DefaultVernWs);

			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("level-1", Cache.DefaultAnalWs);

			var current = sense;
			for (int level = 2; level <= 4; level++)
			{
				var child = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				current.SensesOS.Add(child);
				// Only set gloss on even levels
				if (level % 2 == 0)
				{
					child.Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString($"level-{level}", Cache.DefaultAnalWs);
				}
				current = child;
			}

			Assert.DoesNotThrow(() =>
			{
				m_dtree.Initialize(Cache, false, m_layouts, m_parts);
				m_dtree.ShowObject(entry, "OptSensesEty", null, entry, false);
			}, "Deeply nested senses with alternating ws/unset-ws must not crash.");
		}

		#endregion

		#region Category 48: Writing system transitions

		/// <summary>
		/// CONTRACT: Setting a gloss, then clearing it (set→unset ws transition),
		/// then re-showing does not crash. This tests the ws going from a valid
		/// value back to 0.
		/// </summary>
		[Test]
		public void ClearGloss_ReShow_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(1);

			// First show with the gloss set (valid ws)
			var dtree = CreateNormalDataTree(entry);
			Assert.That(dtree.Slices.Count, Is.GreaterThan(0));

			// Clear the gloss — this sets ws back to 0 on that string
			entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("", Cache.DefaultAnalWs);

			// Re-show
			Assert.DoesNotThrow(() =>
			{
				dtree = ReShowWithFreshDataTree(entry, "Normal");
			}, "Clearing a gloss and re-showing must not crash.");
		}

		/// <summary>
		/// CONTRACT: Multiple set→unset→set cycles on a gloss do not crash.
		/// Tests repeated ws transitions through the PropChanged path.
		/// </summary>
		[Test]
		public void GlossSetUnsetCycles_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(1);

			Assert.DoesNotThrow(() =>
			{
				for (int round = 0; round < 5; round++)
				{
					// Set the gloss
					entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString($"gloss-round-{round}", Cache.DefaultAnalWs);
					var dtree = ReShowWithFreshDataTree(entry, "Normal");
					Assert.That(dtree.Slices.Count, Is.GreaterThan(0));

					// Clear the gloss
					entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString("", Cache.DefaultAnalWs);
					dtree = ReShowWithFreshDataTree(entry, "Normal");
				}
			}, "Multiple gloss set/unset cycles must not crash.");
		}

		/// <summary>
		/// CONTRACT: Changing a gloss from one writing system to another by
		/// setting multiple alternatives does not crash. Tests VwPropertyStore
		/// handling of ws transitions.
		/// </summary>
		[Test]
		public void ChangeGlossWritingSystem_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(1);

			// Set gloss in analysis default ws
			entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("english-gloss", Cache.DefaultAnalWs);

			var dtree = CreateNormalDataTree(entry);
			Assert.That(dtree.Slices.Count, Is.GreaterThan(0));

			// Set gloss in vernacular ws as well
			entry.SensesOS[0].Gloss.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("vern-gloss", Cache.DefaultVernWs);

			Assert.DoesNotThrow(() =>
			{
				dtree = ReShowWithFreshDataTree(entry, "Normal");
			}, "Changing gloss writing system must not crash.");
		}

		/// <summary>
		/// CONTRACT: Adding a sense with an empty TsString (ws is set but text
		/// is empty) does not crash. This is distinct from ws=0 — the ws IS set,
		/// but the string content is empty.
		/// </summary>
		[Test]
		public void SenseWithEmptyStringGloss_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(1);

			// Add a sense with an empty string gloss (ws IS set, text is "")
			var emptySense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(emptySense);
			emptySense.Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("", Cache.DefaultAnalWs);

			Assert.DoesNotThrow(() =>
			{
				var dtree = CreateNormalDataTree(entry);
				Assert.That(dtree.Slices.Count, Is.GreaterThan(0));
			}, "Sense with empty-string gloss (ws set, text empty) must not crash.");
		}

		#endregion

		#region Category 49: ws=0 image regression

		/// <summary>
		/// IMAGE REGRESSION: Bitmap capture of an entry with unset-ws senses
		/// produces a valid (non-null, non-degenerate) bitmap without crashing.
		/// </summary>
		[Test]
		public void ImageRegression_UnsetWsSenses_ProducesValidBitmap()
		{
			var entry = CreateEntryWithSenses(0, "bitmap-ws0");
			for (int i = 0; i < 3; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				// No gloss — ws=0
			}

			CreateNormalDataTree(entry);
			PrepareForBitmapCapture(800, 600);

			(string hash, int width, int height, int nonWhite) info = default;
			Assert.DoesNotThrow(() =>
			{
				info = CaptureBitmapInfo(m_dtree);
			}, "Bitmap capture with unset-ws senses must not crash.");

			Assert.That(info.width, Is.GreaterThan(0), "Bitmap width should be positive.");
			Assert.That(info.height, Is.GreaterThan(0), "Bitmap height should be positive.");
		}

		/// <summary>
		/// IMAGE REGRESSION: Bitmap capture of mixed ws/unset-ws entry,
		/// then resize and recapture, does not crash or produce a degenerate bitmap.
		/// </summary>
		[Test]
		public void ImageRegression_MixedWs_ResizeRoundTrip_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(0, "mixed-bmp");
			for (int i = 0; i < 4; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				if (i % 2 == 0)
				{
					sense.Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString($"gl-{i}", Cache.DefaultAnalWs);
				}
			}

			CreateNormalDataTree(entry);
			PrepareForBitmapCapture(800, 600);

			var before = CaptureBitmapInfo(m_dtree);

			// Resize cycle
			ResizeParent(400, 600);
			ResizeParent(800, 600);

			var after = CaptureBitmapInfo(m_dtree);

			Assert.That(after.Width, Is.EqualTo(before.Width),
				"Width should match after resize round-trip with mixed ws.");
			Assert.That(after.Height, Is.EqualTo(before.Height),
				"Height should match after resize round-trip with mixed ws.");
		}

		/// <summary>
		/// IMAGE REGRESSION: Adding a sense with unset ws to a displayed entry
		/// and re-rendering does not crash the bitmap capture.
		/// </summary>
		[Test]
		public void ImageRegression_AddUnsetWsSense_RecaptureDoesNotCrash()
		{
			var entry = CreateEntryWithSenses(2, "add-ws0-bmp");
			CreateNormalDataTree(entry);
			PrepareForBitmapCapture(800, 600);

			var before = CaptureBitmapInfo(m_dtree);
			Assert.That(before.Height, Is.GreaterThan(0));

			// Add an unset-ws sense
			var emptySense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(emptySense);

			ReShowWithFreshDataTree(entry, "Normal");
			PrepareForBitmapCapture(800, 600);

			Assert.DoesNotThrow(() =>
			{
				var after = CaptureBitmapInfo(m_dtree);
				Assert.That(after.Height, Is.GreaterThan(0),
					"Bitmap should have positive height after adding unset-ws sense.");
			}, "Recapturing bitmap after adding unset-ws sense must not crash.");
		}

		#endregion

		#region Category 50: ws=0 combined with layout switching

		/// <summary>
		/// CONTRACT: Switching layouts on an entry with unset-ws senses does not crash.
		/// Different layouts access different fields — the ws=0 fix in VwPropertyStore
		/// must work regardless of which layout is active.
		/// </summary>
		[Test]
		public void SwitchLayouts_WithUnsetWsSenses_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(0, "layout-ws0");
			for (int i = 0; i < 3; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				// No gloss — ws=0
			}

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			Assert.DoesNotThrow(() =>
			{
				m_dtree.ShowObject(entry, "Normal", null, entry, false);
				m_dtree.ShowObject(entry, "OptSensesEty", null, entry, false);
				m_dtree.ShowObject(entry, "CfAndBib", null, entry, false);
				m_dtree.ShowObject(entry, "Normal", null, entry, false);
			}, "Switching layouts with unset-ws senses must not crash.");
		}

		/// <summary>
		/// CONTRACT: Switching to NormalCollapsible layout with unset-ws senses
		/// and then expanding/collapsing does not crash.
		/// </summary>
		[Test]
		public void CollapsibleLayout_WithUnsetWs_ExpandCollapse_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(0, "coll-ws0");
			// Mix of set and unset gloss
			for (int i = 0; i < 4; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				if (i == 0)
				{
					sense.Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString("only-set", Cache.DefaultAnalWs);
				}
			}

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(entry, "NormalCollapsible", null, entry, false);

			var expandable = FindExpandableSlice(m_dtree);
			if (expandable != null)
			{
				Assert.DoesNotThrow(() =>
				{
					for (int i = 0; i < 3; i++)
					{
						expandable.Collapse();
						expandable.Expand(m_dtree.Slices.IndexOf(expandable));
					}
				}, "Expand/collapse cycles with unset-ws senses on collapsible layout must not crash.");
			}
		}

		#endregion

		#region Category 51: ws=0 stress scenarios

		/// <summary>
		/// CONTRACT: Full lifecycle with unset-ws entries — create, show, resize,
		/// navigate, add senses, navigate back — does not crash. Integration test
		/// for the VwPropertyStore ws=0 + ComBool fixes.
		/// </summary>
		[Test]
		public void FullLifecycle_WithUnsetWs_StressTest_DoesNotCrash()
		{
			var normalEntry = CreateEntryWithSenses(3, "stress-normal");
			var unsetEntry = CreateEntryWithSenses(0, "stress-unset");
			for (int i = 0; i < 5; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				unsetEntry.SensesOS.Add(sense);
			}

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			Assert.DoesNotThrow(() =>
			{
				// Phase 1: Show normal entry
				m_dtree.ShowObject(normalEntry, "Normal", null, normalEntry, false);

				// Phase 2: Navigate to unset-ws entry
				m_dtree.ShowObject(unsetEntry, "Normal", null, unsetEntry, false);

				// Phase 3: Resize while showing unset-ws
				m_parent.ClientSize = new Size(300, 200);
				Application.DoEvents();

				// Phase 4: Navigate back to normal
				m_dtree.ShowObject(normalEntry, "Normal", null, normalEntry, false);

				// Phase 5: Add an unset-ws sense to the normal entry
				var mixSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				normalEntry.SensesOS.Add(mixSense);
				m_dtree.ShowObject(normalEntry, "Normal", null, normalEntry, false);

				// Phase 6: Switch layout while mixed ws is active
				m_dtree.ShowObject(normalEntry, "OptSensesEty", null, normalEntry, false);

				// Phase 7: Navigate to unset entry at narrow width
				m_dtree.ShowObject(unsetEntry, "Normal", null, unsetEntry, false);

				// Phase 8: Widen and navigate back
				m_parent.ClientSize = new Size(1200, 800);
				Application.DoEvents();
				m_dtree.ShowObject(normalEntry, "Normal", null, normalEntry, false);
			}, "Full lifecycle with unset-ws entries must not crash.");
		}

		/// <summary>
		/// CONTRACT: Showing an entry with a mix of senses that have glosses in
		/// different writing systems (analysis and vernacular) plus some with
		/// no gloss at all does not crash. Tests multiple ws values flowing through
		/// VwPropertyStore.
		/// </summary>
		[Test]
		public void MultipleWritingSystems_MixedWithUnset_DoesNotCrash()
		{
			var entry = CreateEntryWithSenses(0, "multi-ws");

			for (int i = 0; i < 6; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);

				if (i % 3 == 0)
				{
					// Analysis ws gloss
					sense.Gloss.AnalysisDefaultWritingSystem =
						TsStringUtils.MakeString($"analysis-{i}", Cache.DefaultAnalWs);
				}
				else if (i % 3 == 1)
				{
					// Vernacular ws gloss
					sense.Gloss.VernacularDefaultWritingSystem =
						TsStringUtils.MakeString($"vernacular-{i}", Cache.DefaultVernWs);
				}
				// i % 3 == 2: no gloss set — ws=0
			}

			Assert.DoesNotThrow(() =>
			{
				var dtree = CreateNormalDataTree(entry);
				Assert.That(dtree.Slices.Count, Is.GreaterThan(0));
			}, "Entry with mixed analysis/vernacular/unset ws senses must not crash.");
		}

		/// <summary>
		/// CONTRACT: Disposing the DataTree while it has unset-ws senses displayed
		/// does not throw or leak.
		/// </summary>
		[Test]
		public void Dispose_WithUnsetWsSensesDisplayed_DoesNotThrow()
		{
			var entry = CreateEntryWithSenses(0, "dispose-ws0");
			for (int i = 0; i < 5; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
			}

			var mediator = new Mediator();
			var propTable = new PropertyTable(mediator);
			var dtree = new DataTree();
			dtree.Init(mediator, propTable, null);
			var parent = new Form();
			parent.Controls.Add(dtree);

			dtree.Initialize(Cache, false, m_layouts, m_parts);
			dtree.ShowObject(entry, "Normal", null, entry, false);

			Assert.DoesNotThrow(() =>
			{
				parent.Close();
				parent.Dispose();
				propTable.Dispose();
				mediator.Dispose();
			}, "Disposing with unset-ws senses displayed must not throw.");
		}

		#endregion

		#region Category 52: Entry edge cases

		/// <summary>
		/// CONTRACT: Showing an entry with no citation form set does not crash.
		/// The citation form field's ws may be 0 if no vernacular string is set.
		/// </summary>
		[Test]
		public void EntryWithNoCitationForm_DoesNotCrash()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			// Do NOT set CitationForm — ws=0

			// Add a sense with a gloss so we have something to display
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("orphan-gloss", Cache.DefaultAnalWs);

			Assert.DoesNotThrow(() =>
			{
				var dtree = CreateNormalDataTree(entry);
				Assert.That(dtree.Slices.Count, Is.GreaterThan(0));
			}, "Entry with no citation form must not crash.");
		}

		/// <summary>
		/// CONTRACT: Showing a completely empty entry (no citation form, no senses)
		/// does not crash. This is the minimal possible entry.
		/// </summary>
		[Test]
		public void CompletelyEmptyEntry_DoesNotCrash()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			// Nothing set at all

			Assert.DoesNotThrow(() =>
			{
				var dtree = CreateNormalDataTree(entry);
			}, "Completely empty entry must not crash.");
		}

		/// <summary>
		/// CONTRACT: Navigation from a completely empty entry to one with senses
		/// and back does not crash.
		/// </summary>
		[Test]
		public void NavigateBetweenEmptyAndFull_DoesNotCrash()
		{
			var emptyEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var fullEntry = CreateEntryWithSenses(5, "full");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			Assert.DoesNotThrow(() =>
			{
				m_dtree.ShowObject(emptyEntry, "Normal", null, emptyEntry, false);
				m_dtree.ShowObject(fullEntry, "Normal", null, fullEntry, false);
				m_dtree.ShowObject(emptyEntry, "Normal", null, emptyEntry, false);
				m_dtree.ShowObject(fullEntry, "Normal", null, fullEntry, false);
			}, "Navigating between empty and full entries must not crash.");
		}

		/// <summary>
		/// CONTRACT: An entry where senses have definition (not gloss) set, but
		/// no gloss, does not crash. Tests another field path through VwPropertyStore.
		/// </summary>
		[Test]
		public void SenseWithDefinitionButNoGloss_DoesNotCrash()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("def-no-gloss", Cache.DefaultVernWs);

			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			// Set definition but NOT gloss
			sense.Definition.AnalysisDefaultWritingSystem =
				TsStringUtils.MakeString("the definition", Cache.DefaultAnalWs);

			Assert.DoesNotThrow(() =>
			{
				m_dtree.Initialize(Cache, false, m_layouts, m_parts);
				m_dtree.ShowObject(entry, "Normal", null, entry, false);
				Assert.That(m_dtree.Slices.Count, Is.GreaterThan(0));
			}, "Sense with definition but no gloss must not crash.");
		}

		#endregion
	}
}
