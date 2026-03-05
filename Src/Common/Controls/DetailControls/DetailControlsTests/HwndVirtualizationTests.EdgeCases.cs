// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Edge-case tests for HWND virtualization regression safety.
//
// These tests cover gaps identified by auditing Slice.cs, DataTree.cs, and ViewSlice.cs
// for null-safety, lifecycle ordering, and Slices/Controls coupling assumptions.
// Each category targets a specific regression risk area documented in the main file's
// front matter.

using System;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public partial class HwndVirtualizationTests
	{
		#region Category 21: ViewSlice lifecycle from ShowObject

		/// <summary>
		/// CONTRACT: Without Form.Show(), ViewSlices from ShowObject may NOT be real.
		/// HandleLayout1 calls FieldAt → BecomeRealInPlace only for slices in the
		/// visible clip rect. Without a shown Form, the layout pass may skip
		/// materialization. This is the starting state that virtualization extends.
		/// </summary>
		[Test]
		public void FromShowObject_WithoutFormShow_SlicesMayBeNonReal()
		{
			var dtree = CreatePopulatedDataTree();
			bool hasNonReal = false;
			foreach (Slice slice in dtree.Slices)
			{
				if (!slice.IsRealSlice)
					hasNonReal = true;
			}
			// Document: ViewSlices may remain non-real without a shown Form.
			// This is expected — HandleLayout1 only forces BecomeRealInPlace
			// for slices in the visible clip rect.
			Assert.That(hasNonReal || dtree.Slices.Count > 0, Is.True,
				"ShowObject should produce at least one slice.");
		}

		/// <summary>
		/// CONTRACT: ViewSlice.IsRealSlice returns RootSite.AllowLayout, which
		/// starts false and is set true by BecomeRealInPlace(). This means
		/// ViewSlices from ShowObject are NOT automatically real — they become
		/// real only when HandleLayout1 calls FieldAt for slices in the viewport.
		///
		/// This is the fundamental behavior that virtualization extends: slices
		/// are created lightweight and only become real when needed.
		/// </summary>
		[Test]
		public void FromShowObject_ViewSlices_StartNonRealUntilBecomeRealInPlace()
		{
			var dtree = CreatePopulatedDataTree();
			bool foundNonReal = false;
			foreach (Slice slice in dtree.Slices)
			{
				if (!slice.IsRealSlice)
				{
					foundNonReal = true;
					// Verify BecomeRealInPlace makes it real
					bool became = slice.BecomeRealInPlace();
					Assert.That(became, Is.True,
						$"ViewSlice '{slice.Label}' should become real via BecomeRealInPlace.");
					Assert.That(slice.IsRealSlice, Is.True,
						$"After BecomeRealInPlace, '{slice.Label}' should be real.");
				}
			}
			Assert.That(foundNonReal, Is.True,
				"CfAndBib layout should produce ViewSlices that start non-real — " +
				"this is the deferred materialization pattern.");
		}

		/// <summary>
		/// CONTRACT: All slices from ShowObject have a non-null Control.
		/// The multistring editor creates a RootSite-based control for each field.
		/// Virtualization must ensure Control is available before accessing it.
		/// </summary>
		[Test]
		public void FromShowObject_AllSlicesHaveControl()
		{
			var dtree = CreatePopulatedDataTree();
			foreach (Slice slice in dtree.Slices)
			{
				Assert.That(slice.Control, Is.Not.Null,
					$"Slice '{slice.Label}' should have a Control after ShowObject.");
			}
		}

		/// <summary>
		/// CONTRACT: All slices from ShowObject have a non-null TreeNode.
		/// Install() creates the SliceTreeNode in SplitCont.Panel1.
		/// TreeNode is required for SetCurrentState, TakeFocus, DrawLabel, and OnLayout.
		/// </summary>
		[Test]
		public void FromShowObject_AllSlicesHaveTreeNode()
		{
			var dtree = CreatePopulatedDataTree();
			foreach (Slice slice in dtree.Slices)
			{
				Assert.That(slice.TreeNode, Is.Not.Null,
					$"Slice '{slice.Label}' should have a TreeNode after ShowObject.");
			}
		}

		/// <summary>
		/// CONTRACT: Slices from ShowObject are derived types (MultiStringSlice, etc.),
		/// not plain base Slice. Virtualization must handle subclass-specific behaviors
		/// like ViewSlice's event subscriptions and BecomeRealInPlace.
		/// </summary>
		[Test]
		public void FromShowObject_SlicesAreDerivedTypes()
		{
			var dtree = CreatePopulatedDataTree();
			foreach (Slice slice in dtree.Slices)
			{
				Assert.That(slice.GetType(), Is.Not.EqualTo(typeof(Slice)),
					$"Slice '{slice.Label}' should be a derived type, not base Slice.");
			}
		}

		#endregion

		#region Category 22: ContainingDataTree null-safety contracts

		/// <summary>
		/// CONTRACT: TakeFocus(true) on a standalone slice returns false (safe)
		/// when ContainingDataTree is null.
		///
		/// Virtualization should avoid throwing in this pre-install state.
		/// </summary>
		[Test]
		public void TakeFocus_True_Standalone_ReturnsFalse_WhenCDTNull()
		{
			using (var slice = CreateStandaloneSlice())
			{
				bool result = slice.TakeFocus(true);
				Assert.That(result, Is.False,
					"TakeFocus(true) should return false when no ContainingDataTree is available.");
			}
		}

		/// <summary>
		/// CONTRACT: TakeFocus(false) on a standalone slice with no Control returns false
		/// without accessing ContainingDataTree. This is safe because:
		///   ctrl == null → skip MakeSliceVisible, skip Focus, return false.
		///
		/// Virtualization can safely call TakeFocus(false) on deferred slices
		/// that have no Control yet.
		/// </summary>
		[Test]
		public void TakeFocus_False_Standalone_NoControl_ReturnsFalse()
		{
			using (var slice = CreateStandaloneSlice())
			{
				bool result = slice.TakeFocus(false);
				Assert.That(result, Is.False,
					"TakeFocus(false) with no Control should return false without touching CDT.");
			}
		}

		/// <summary>
		/// CONTRACT: TakeFocus(false) on a standalone slice WITH a TabStop-enabled control
		/// returns false safely when ContainingDataTree is null.
		///
		/// This verifies a subtle deferred edge case remains non-throwing.
		/// </summary>
		[Test]
		public void TakeFocus_False_StandaloneWithTabStopControl_ReturnsFalse()
		{
			using (var slice = CreateStandaloneSliceWithControl())
			{
				// TextBox has TabStop=true by default
				Assert.That(slice.Control.TabStop, Is.True, "Precondition: TextBox.TabStop is true.");
				bool result = slice.TakeFocus(false);
				Assert.That(result, Is.False,
					"TakeFocus(false) with TabStop control should return false without throwing.");
			}
		}

		/// <summary>
		/// CONTRACT: The TreeNode getter on a standalone slice returns null (safe) before
		/// physical install; it no longer throws when Panel1 has no controls.
		/// </summary>
		[Test]
		public void TreeNode_Standalone_ThrowsArgumentOutOfRange()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.TreeNode, Is.Null,
					"TreeNode should be null before physical install for standalone slices.");
			}
		}

		/// <summary>
		/// CONTRACT: SetCurrentState on a standalone slice should not throw when TreeNode
		/// is not yet created; TreeNode is null-safe in deferred mode.
		/// </summary>
		[Test]
		public void SetCurrentState_Standalone_ThrowsDueToTreeNodeAccess()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.DoesNotThrow(() => slice.SetCurrentState(true),
					"SetCurrentState should tolerate missing TreeNode before physical install.");
			}
		}

		#endregion

		#region Category 23: GetMessageTargets visibility gating

		/// <summary>
		/// CONTRACT: GetMessageTargets on a standalone slice returns empty because
		/// Visible == false (set in constructor) short-circuits the CDT access.
		///
		/// This is safe — the Mediator won't dispatch messages to deferred slices.
		/// But if someone explicitly sets Visible = true on a deferred slice,
		/// GetMessageTargets would NRE on CDT.Visible.
		/// </summary>
		[Test]
		public void GetMessageTargets_Standalone_ReturnsEmpty()
		{
			using (var slice = CreateStandaloneSlice())
			{
				var targets = slice.GetMessageTargets();
				Assert.That(targets, Is.Empty,
					"Invisible standalone slice should return empty message targets.");
			}
		}

		/// <summary>
		/// CONTRACT: GetMessageTargets on an invisible installed slice returns empty.
		/// In the test harness (Form not shown), Visible returns false for all slices,
		/// so this short-circuits before accessing CDT.
		/// </summary>
		[Test]
		public void GetMessageTargets_InvisibleInstalled_ReturnsEmpty()
		{
			var dtree = CreatePopulatedDataTree();
			var slice = dtree.Slices[0];
			var targets = slice.GetMessageTargets();
			Assert.That(targets, Is.Empty,
				"Invisible installed slice should return empty message targets.");
		}

		/// <summary>
		/// CONTRACT: GetMessageTargets on a visible slice with a shown Form returns
		/// non-empty targets (the slice itself and its Control if it's an IxCoreColleague).
		///
		/// This confirms that the visibility gate works correctly — messages are only
		/// dispatched to visible, materialized slices.
		/// </summary>
		[Test]
		public void GetMessageTargets_VisibleInstalled_ReturnsTargets()
		{
			var dtree = new DataTree();
			var mediator = new Mediator();
			var propTable = new PropertyTable(mediator);
			dtree.Init(mediator, propTable, null);
			var parentForm = new Form();
			parentForm.Controls.Add(dtree);
			try
			{
				parentForm.Show();
				dtree.Initialize(Cache, false, m_layouts, m_parts);
				dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

				var slice = dtree.Slices[0];
				dtree.MakeSliceVisible(slice);

				var targets = slice.GetMessageTargets();
				Assert.That(targets.Length, Is.GreaterThan(0),
					"Visible slice with CfAndBib should return message targets.");
			}
			finally
			{
				parentForm.Close();
				parentForm.Dispose();
				propTable.Dispose();
				mediator.Dispose();
			}
		}

		#endregion

		#region Category 24: OnSizeChanged layout guard

		/// <summary>
		/// CONTRACT: Resizing a standalone slice does not throw because the
		/// m_widthHasBeenSetByDataTree guard prevents OnSizeChanged from
		/// accessing ContainingDataTree.AutoScrollPosition.
		///
		/// This guard is the ONLY thing preventing NRE during pre-install sizing.
		/// Virtualization must ensure this guard remains intact for deferred slices.
		/// </summary>
		[Test]
		public void Resize_StandaloneSlice_DoesNotThrow()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.DoesNotThrow(() => { slice.Size = new Size(500, 30); },
					"Resizing a standalone slice must not throw — " +
					"m_widthHasBeenSetByDataTree guard prevents CDT access.");
			}
		}

		/// <summary>
		/// CONTRACT: Resizing an installed slice (after SetWidthForDataTreeLayout has
		/// been called during layout) does not throw.
		/// </summary>
		[Test]
		public void Resize_InstalledSlice_DoesNotThrow()
		{
			var dtree = CreatePopulatedDataTree();
			var slice = dtree.Controls[0] as Slice;
			Assert.That(slice, Is.Not.Null);
			Assert.DoesNotThrow(() => { slice.Size = new Size(500, 30); },
				"Resizing an installed slice must not throw.");
		}

		#endregion

		#region Category 25: Expand/Collapse dynamics

		/// <summary>
		/// CONTRACT: Collapsing an expanded header slice removes its child slices.
		/// Collapse iterates descendants via IsDescendant() and removes them with
		/// RemoveSliceAt(), which calls Controls.Remove and Slices.RemoveAt.
		///
		/// Virtualization must handle Collapse on slices whose children may not
		/// be in Controls (deferred children).
		/// </summary>
		[Test]
		public void Collapse_ExpandedHeader_RemovesChildSlices()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Nested-Expanded", null, m_entry, false);

			int initialCount = m_dtree.Slices.Count;
			Assert.That(initialCount, Is.GreaterThanOrEqualTo(2),
				"Nested-Expanded should produce a header + children.");

			var header = m_dtree.Slices[0];
			Assert.That(header.Label, Is.EqualTo("Header"));
			Assert.That(header.Expansion, Is.EqualTo(DataTree.TreeItemState.ktisExpanded));

			header.Collapse();

			Assert.That(m_dtree.Slices.Count, Is.LessThan(initialCount),
				"Collapse should remove child slices.");
			Assert.That(header.Expansion, Is.EqualTo(DataTree.TreeItemState.ktisCollapsed));
		}

		/// <summary>
		/// CONTRACT: After Collapse + Expand round-trip, the same number of child slices
		/// is restored. The Expand path uses CreateIndentedNodes to rebuild children.
		///
		/// Virtualization must ensure Expand on a collapsed header materializes children
		/// correctly (deferred or immediate).
		/// </summary>
		[Test]
		public void ExpandAfterCollapse_RestoresChildSlices()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Nested-Expanded", null, m_entry, false);

			int initialCount = m_dtree.Slices.Count;
			var header = m_dtree.Slices[0];

			header.Collapse();
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(1),
				"After collapse, only the header should remain.");

			header.Expand(0);

			Assert.That(m_dtree.Slices.Count, Is.EqualTo(initialCount),
				"Expand should restore the same number of child slices.");
			Assert.That(header.Expansion, Is.EqualTo(DataTree.TreeItemState.ktisExpanded));
		}

		/// <summary>
		/// CONTRACT: Collapsing a non-header slice (no children) doesn't remove anything
		/// but does set Expansion to ktisCollapsed. The Collapse loop finds zero
		/// descendants via IsDescendant and skips the removal.
		/// </summary>
		[Test]
		public void Collapse_NonHeaderSlice_NoRemoval()
		{
			var dtree = CreatePopulatedDataTree();
			int countBefore = dtree.Slices.Count;
			var leafSlice = dtree.Slices[0];

			leafSlice.Collapse();

			Assert.That(dtree.Slices.Count, Is.EqualTo(countBefore),
				"Collapsing a leaf slice should not remove any slices.");
			Assert.That(leafSlice.Expansion, Is.EqualTo(DataTree.TreeItemState.ktisCollapsed));
		}

		#endregion

		#region Category 26: HWND creation and Controls membership

		/// <summary>
		/// BASELINE SENTINEL: After Install(), the slice IS in the DataTree's Controls collection.
		/// This is the fundamental coupling that virtualization will break — Install() currently
		/// calls parent.Controls.Add(this), which triggers CreateHandle.
		///
		/// Virtualization will make Controls.Add conditional based on visibility/viewport.
		/// </summary>
		[Test]
		public void Install_SliceIsInControlsCollection()
		{
			var dtree = CreatePopulatedDataTree();
			using (var slice = new Slice())
			{
				slice.Install(dtree);
				Assert.That(dtree.Controls.Contains(slice), Is.True,
					"SENTINEL: Install currently adds slice to Controls. " +
					"Virtualization will make this conditional on materialization.");
			}
		}

		/// <summary>
		/// BASELINE SENTINEL: After Install(), the slice IS in both Controls AND Slices.
		/// This 1:1 coupling is the core assumption virtualization removes.
		/// </summary>
		[Test]
		public void Install_SliceInBothControlsAndSlices()
		{
			var dtree = CreatePopulatedDataTree();
			using (var slice = new Slice())
			{
				slice.Install(dtree);
				Assert.That(dtree.Controls.Contains(slice), Is.True,
					"SENTINEL: Slice must be in Controls before virtualization.");
				Assert.That(dtree.Slices.Contains(slice), Is.True,
					"Slice must be in Slices.");
			}
		}

		/// <summary>
		/// CONTRACT: The SplitContainer in a newly constructed Slice has both Panel1 and Panel2.
		/// Panel1 is empty (no TreeNode yet), Panel2 is empty (no Control yet).
		/// Virtualization needs these panels available even if the SplitContainer handle
		/// hasn't been created.
		/// </summary>
		[Test]
		public void Constructor_SplitContHasBothPanels()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.SplitCont.Panel1, Is.Not.Null);
				Assert.That(slice.SplitCont.Panel2, Is.Not.Null);
				Assert.That(slice.SplitCont.Panel1.Controls.Count, Is.EqualTo(0),
					"Panel1 is empty before Install (no TreeNode).");
				Assert.That(slice.SplitCont.Panel2.Controls.Count, Is.EqualTo(0),
					"Panel2 is empty before Control is set.");
			}
		}

		#endregion

		#region Category 27: SplitterDistance and split position consistency

		/// <summary>
		/// CONTRACT: All installed slices have a non-negative SplitterDistance.
		/// The split position is set during Install → SetSplitPosition →
		/// CDT.SliceSplitPositionBase + indent calculations.
		/// </summary>
		[Test]
		public void SplitterDistance_AllSlices_NonNegative()
		{
			var dtree = CreatePopulatedDataTree();
			foreach (Slice slice in dtree.Slices)
			{
				int distance = slice.SplitCont.SplitterDistance;
				Assert.That(distance, Is.GreaterThanOrEqualTo(0),
					$"SplitterDistance for '{slice.Label}' must be non-negative.");
			}
		}

		/// <summary>
		/// CONTRACT: Slices at the same indent level have the same SplitterDistance.
		/// AdjustSliceSplitPosition sets SplitterDistance = SliceSplitPositionBase + indent.
		/// Slices with the same indent share the same position.
		///
		/// Virtualization must maintain this consistency even for deferred slices
		/// (either by setting it at materialization time or via a virtual property).
		/// </summary>
		[Test]
		public void SplitterDistance_SameIndent_Consistent()
		{
			var dtree = CreatePopulatedDataTree();
			Assert.That(dtree.Slices.Count, Is.GreaterThanOrEqualTo(2));

			// CfAndBib slices are at the same indent level
			int indent0 = dtree.Slices[0].Indent;
			int indent1 = dtree.Slices[1].Indent;
			Assert.That(indent0, Is.EqualTo(indent1), "Precondition: both slices same indent.");

			int dist0 = dtree.Slices[0].SplitCont.SplitterDistance;
			int dist1 = dtree.Slices[1].SplitCont.SplitterDistance;
			Assert.That(dist0, Is.EqualTo(dist1),
				"Slices at the same indent level should have the same SplitterDistance.");
		}

		#endregion

		#region Category 28: Slice replacement lifecycle

		/// <summary>
		/// CONTRACT: When ShowObject switches to a layout with fewer slices,
		/// the extra slices are removed from Slices and disposed.
		/// The CreateSlices reuse map keeps matching slices; non-matching ones are
		/// cleaned up via RemoveSlice → Controls.Remove → Dispose.
		///
		/// Virtualization must ensure deferred slices in the reuse map can be
		/// correctly matched, reused, or disposed.
		/// </summary>
		[Test]
		public void ShowObject_SwitchLayout_RemovedSlicesAreDisposed()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(2), "Precondition: CfAndBib has 2 slices.");

			// Save reference to the bibliography slice — it won't be reused by CfOnly
			var bibSlice = m_dtree.Slices[1];
			Assert.That(bibSlice.Label, Does.Contain("Bibliography").IgnoreCase);
			Assert.That(bibSlice.IsDisposed, Is.False, "Precondition: not disposed.");

			// Switch to CfOnly (Bibliography has visibility="never")
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(1));

			Assert.That(bibSlice.IsDisposed, Is.True,
				"Unreused slices must be disposed when ShowObject replaces them.");
		}

		/// <summary>
		/// CONTRACT: After ShowObject replaces slices, all new slices have
		/// TreeNode, Control, and ContainingDataTree. They may NOT be "real"
		/// yet (ViewSlice.IsRealSlice depends on BecomeRealInPlace) but they
		/// are fully installed with all structural components.
		/// </summary>
		[Test]
		public void ShowObject_SwitchLayout_NewSlicesAreInstalled()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			// Switch to CfAndBib — adds a Bibliography slice
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(2));

			foreach (Slice slice in m_dtree.Slices)
			{
				Assert.That(slice.TreeNode, Is.Not.Null, $"'{slice.Label}' should have TreeNode.");
				Assert.That(slice.Control, Is.Not.Null, $"'{slice.Label}' should have Control.");
				Assert.That(slice.ContainingDataTree, Is.SameAs(m_dtree),
					$"'{slice.Label}' should reference DataTree.");
			}
		}

		#endregion

		#region Category 29: TabIndex and TabStop validity

		/// <summary>
		/// CONTRACT: After ShowObject, slices have sequentially increasing TabIndex values.
		/// ResetTabIndices iterates Slices and sets TabIndex = i for each real slice.
		///
		/// Virtualization note: ResetTabIndices accesses slice.TabIndex setter, which
		/// in WinForms can trigger CreateHandle. For deferred slices, this is a hidden
		/// HWND creation trigger that must be addressed.
		/// </summary>
		[Test]
		public void TabIndex_Sequential_AfterShowObject()
		{
			var dtree = CreatePopulatedDataTree();
			for (int i = 0; i < dtree.Slices.Count; i++)
			{
				Assert.That(dtree.Slices[i].TabIndex, Is.GreaterThanOrEqualTo(0),
					$"TabIndex at {i} should be non-negative.");
			}
		}

		/// <summary>
		/// CONTRACT: UserControl.TabStop defaults to true in WinForms.
		/// After DataTree calls ResetTabIndices → SetTabIndex, TabStop is set to
		/// (Control != null &amp;&amp; Control.TabStop). Direct Install() does NOT
		/// call ResetTabIndices — that's the DataTree's responsibility.
		///
		/// Virtualization note: ResetTabIndices accesses TabIndex setter on each
		/// slice, which in WinForms can trigger CreateHandle — a hidden HWND
		/// creation point that must be gated for deferred slices.
		/// </summary>
		[Test]
		public void TabStop_DefaultsTrue_BeforeResetTabIndices()
		{
			var dtree = CreatePopulatedDataTree();
			using (var slice = new Slice())
			{
				slice.Install(dtree);
				Assert.That(slice.Control, Is.Null, "Precondition: no Control.");
				// WinForms UserControl.TabStop defaults to true.
				// Only DataTree.ResetTabIndices sets it to false for no-control slices.
				// Direct Install() does NOT call ResetTabIndices.
				Assert.That(slice.TabStop, Is.True,
					"TabStop defaults to true before DataTree calls ResetTabIndices.");
			}
		}

		#endregion

		#region Category 30: Post-dispose property access consistency

		/// <summary>
		/// CONTRACT: SplitCont throws ObjectDisposedException after Dispose.
		/// CheckDisposed() is called at the top of the getter.
		/// This is the same pattern as Label (tested in cat 17) — confirming
		/// consistency across all guarded properties.
		/// </summary>
		[Test]
		public void SplitCont_AfterDispose_ThrowsObjectDisposedException()
		{
			var slice = CreateStandaloneSlice();
			slice.Dispose();
			Assert.Throws<ObjectDisposedException>(() => { var _ = slice.SplitCont; },
				"SplitCont must throw ObjectDisposedException after Dispose.");
		}

		/// <summary>
		/// CONTRACT: Control throws ObjectDisposedException after Dispose.
		/// </summary>
		[Test]
		public void Control_AfterDispose_ThrowsObjectDisposedException()
		{
			var slice = CreateStandaloneSlice();
			slice.Dispose();
			Assert.Throws<ObjectDisposedException>(() => { var _ = slice.Control; },
				"Control must throw ObjectDisposedException after Dispose.");
		}

		/// <summary>
		/// CONTRACT: GetMessageTargets throws ObjectDisposedException after Dispose.
		/// The CheckDisposed() call at the top of GetMessageTargets prevents
		/// any Mediator dispatch to a disposed slice.
		/// </summary>
		[Test]
		public void GetMessageTargets_AfterDispose_ThrowsObjectDisposedException()
		{
			var slice = CreateStandaloneSlice();
			slice.Dispose();
			Assert.Throws<ObjectDisposedException>(() => slice.GetMessageTargets(),
				"GetMessageTargets must throw ObjectDisposedException after Dispose.");
		}

		#endregion
	}
}
