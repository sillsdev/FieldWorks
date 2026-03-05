// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public partial class HwndVirtualizationTests
	{
		#region Category 5: Visibility and high-water-mark

		/// <summary>
		/// BASELINE: Slices are created invisible (Visible = false).
		/// </summary>
		[Test]
		public void Slices_StartInvisible()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.Visible, Is.False,
					"All slices must start invisible to prevent reordering by WinForms.");
			}
		}

		/// <summary>
		/// BASELINE: MakeSliceVisible sets all prior slices visible (high-water-mark pattern).
		/// This is the key pattern that virtualization must adapt — when slices aren't in
		/// Controls, setting Visible would be meaningless or harmful.
		/// NOTE: Requires Form.Show() because WinForms Visible property only returns true
		/// when the parent Form has been shown.
		/// </summary>
		[Test]
		public void MakeSliceVisible_SetsAllPriorSlicesVisible()
		{
			var dtree = new DataTree();
			var mediator = new Mediator();
			var propTable = new PropertyTable(mediator);
			dtree.Init(mediator, propTable, null);
			var parentForm = new Form();
			parentForm.Controls.Add(dtree);
			try
			{
				// Form.Show() is required: WinForms Control.Visible getter returns false
				// for controls inside un-shown Forms, even after setting Visible = true.
				parentForm.Show();
				dtree.Initialize(Cache, false, m_layouts, m_parts);

				var slice0 = new Slice { Label = "Slice0" };
				var slice1 = new Slice { Label = "Slice1" };
				var slice2 = new Slice { Label = "Slice2" };

				slice0.Install(dtree);
				slice1.Install(dtree);
				slice2.Install(dtree);

				Assert.That(slice0.Visible, Is.False, "Precondition: slice0 starts invisible.");
				Assert.That(slice1.Visible, Is.False, "Precondition: slice1 starts invisible.");
				Assert.That(slice2.Visible, Is.False, "Precondition: slice2 starts invisible.");

				// Making slice2 visible should also make slice0 and slice1 visible
				dtree.MakeSliceVisible(slice2);

				Assert.That(slice0.Visible, Is.True,
					"High-water-mark: making slice[2] visible must also make slice[0] visible.");
				Assert.That(slice1.Visible, Is.True,
					"High-water-mark: making slice[2] visible must also make slice[1] visible.");
				Assert.That(slice2.Visible, Is.True);
			}
			finally
			{
				parentForm.Close();
				parentForm.Dispose();
				propTable.Dispose();
				mediator.Dispose();
			}
		}

		/// <summary>
		/// BASELINE: MakeSliceVisible is idempotent — calling it twice doesn't hurt.
		/// Requires Form.Show() for Visible property to work correctly.
		/// </summary>
		[Test]
		public void MakeSliceVisible_CalledTwice_IsIdempotent()
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
				var slice = new Slice { Label = "TestSlice" };
				slice.Install(dtree);

				dtree.MakeSliceVisible(slice);
				Assert.That(slice.Visible, Is.True);

				Assert.DoesNotThrow(() => dtree.MakeSliceVisible(slice),
					"MakeSliceVisible must be idempotent.");
				Assert.That(slice.Visible, Is.True);
			}
			finally
			{
				parentForm.Close();
				parentForm.Dispose();
				propTable.Dispose();
				mediator.Dispose();
			}
		}

		/// <summary>
		/// BASELINE: IndexInContainer matches position in Controls collection.
		/// After MakeSliceVisible, the index should not change.
		/// </summary>
		[Test]
		public void IndexInContainer_StableAfterMakeVisible()
		{
			var dtree = CreatePopulatedDataTree();
			Assert.That(dtree.Controls.Count, Is.GreaterThanOrEqualTo(2));
			var slice1 = dtree.Controls[1] as Slice;
			int indexBefore = slice1.IndexInContainer;

			dtree.MakeSliceVisible(slice1);

			Assert.That(slice1.IndexInContainer, Is.EqualTo(indexBefore),
				"IndexInContainer must not change after MakeSliceVisible — " +
				"this is the LT-7307 invariant that the high-water-mark enforces.");
		}

		#endregion

		#region Category 6: Install ordering and SplitContainer setup

		/// <summary>
		/// BASELINE: Install() creates a SliceTreeNode in Panel1 and configures the splitter.
		/// Calling Install is what transitions a Slice from "constructed" to "installed".
		/// </summary>
		[Test]
		public void Install_CreatesTreeNodeInPanel1()
		{
			var dtree = CreatePopulatedDataTree();
			using (var slice = new Slice())
			{
				Assert.That(slice.SplitCont.Panel1.Controls.Count, Is.EqualTo(0),
					"Precondition: no TreeNode before Install.");

				slice.Install(dtree);

				Assert.That(slice.SplitCont.Panel1.Controls.Count, Is.EqualTo(1),
					"Install must add SliceTreeNode to Panel1.");
				Assert.That(slice.TreeNode, Is.Not.Null);
				Assert.That(slice.TreeNode, Is.InstanceOf<SliceTreeNode>());
			}
		}

		/// <summary>
		/// BASELINE: Install() adds the Slice to the DataTree's Controls and Slices.
		/// This is the step that creates the HWND (via Controls.Add → CreateHandle).
		/// Virtualization will defer this step.
		/// </summary>
		[Test]
		public void Install_AddsSliceToDataTreeControls()
		{
			var dtree = CreatePopulatedDataTree();
			int countBefore = dtree.Controls.Count;

			using (var slice = new Slice())
			{
				slice.Install(dtree);
				Assert.That(dtree.Controls.Count, Is.EqualTo(countBefore + 1),
					"Install must add the Slice to DataTree.Controls.");
				Assert.That(dtree.Slices, Does.Contain(slice),
					"Install must add the Slice to DataTree.Slices.");
			}
		}

		/// <summary>
		/// BASELINE: Install() requires a non-null parent.
		/// </summary>
		[Test]
		public void Install_NullParent_ThrowsInvalidOperationException()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.Throws<InvalidOperationException>(() => slice.Install(null),
					"Install must throw if parent is null.");
			}
		}

		/// <summary>
		/// BASELINE: A re-used slice (Panel1 already has a TreeNode) does not create a second SliceTreeNode.
		/// </summary>
		[Test]
		public void Install_Reuse_DoesNotDuplicateTreeNode()
		{
			var dtree = CreatePopulatedDataTree();
			using (var slice = new Slice())
			{
				slice.Install(dtree);
				Assert.That(slice.SplitCont.Panel1.Controls.Count, Is.EqualTo(1));

				// Simulate re-install (as happens in slice reuse)
				// Remove from DataTree first
				dtree.Slices.Remove(slice);
				dtree.Controls.Remove(slice);

				// Re-install
				slice.Install(dtree);
				Assert.That(slice.SplitCont.Panel1.Controls.Count, Is.EqualTo(1),
					"Re-installing a slice must not duplicate the SliceTreeNode.");
			}
		}

		#endregion

		#region Category 7: BecomeReal / BecomeRealInPlace lifecycle

		/// <summary>
		/// BASELINE: Base Slice.IsRealSlice returns true.
		/// Only ViewSlice overrides this to return false until BecomeRealInPlace is called.
		/// </summary>
		[Test]
		public void IsRealSlice_BaseSlice_AlwaysTrue()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.IsRealSlice, Is.True,
					"Base Slice.IsRealSlice should always return true.");
			}
		}

		/// <summary>
		/// BASELINE: Base Slice.BecomeRealInPlace returns false (no-op for base slices).
		/// </summary>
		[Test]
		public void BecomeRealInPlace_BaseSlice_ReturnsFalse()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.BecomeRealInPlace(), Is.False,
					"Base Slice.BecomeRealInPlace should return false.");
			}
		}

		/// <summary>
		/// BASELINE: Base Slice.BecomeReal returns the same slice (identity).
		/// </summary>
		[Test]
		public void BecomeReal_BaseSlice_ReturnsSelf()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.BecomeReal(0), Is.SameAs(slice),
					"Base Slice.BecomeReal should return the same instance.");
			}
		}

		#endregion

		#region Category 8: Property access on non-installed slices

		/// <summary>
		/// BASELINE: Label can be set and retrieved on a standalone (non-installed) slice.
		/// This is safe because it only touches m_strLabel.
		/// Virtualization relies on this being safe for deferred slices.
		/// </summary>
		[Test]
		public void Label_CanBeSetOnStandaloneSlice()
		{
			using (var slice = CreateStandaloneSlice())
			{
				slice.Label = "Test Label";
				Assert.That(slice.Label, Is.EqualTo("Test Label"));
			}
		}

		/// <summary>
		/// BASELINE: Indent can be set and retrieved on a standalone slice.
		/// </summary>
		[Test]
		public void Indent_CanBeSetOnStandaloneSlice()
		{
			using (var slice = CreateStandaloneSlice())
			{
				slice.Indent = 3;
				Assert.That(slice.Indent, Is.EqualTo(3));
			}
		}

		/// <summary>
		/// BASELINE: Weight can be set and retrieved on a standalone slice.
		/// </summary>
		[Test]
		public void Weight_CanBeSetOnStandaloneSlice()
		{
			using (var slice = CreateStandaloneSlice())
			{
				slice.Weight = ObjectWeight.normal;
				Assert.That(slice.Weight, Is.EqualTo(ObjectWeight.normal));
			}
		}

		/// <summary>
		/// BASELINE: Expansion can be set and retrieved on a standalone slice.
		/// </summary>
		[Test]
		public void Expansion_CanBeSetOnStandaloneSlice()
		{
			using (var slice = CreateStandaloneSlice())
			{
				slice.Expansion = DataTree.TreeItemState.ktisExpanded;
				Assert.That(slice.Expansion, Is.EqualTo(DataTree.TreeItemState.ktisExpanded));
			}
		}

		/// <summary>
		/// BASELINE: Object (ICmObject) can be set on a standalone slice.
		/// </summary>
		[Test]
		public void Object_CanBeSetOnStandaloneSlice()
		{
			using (var slice = CreateStandaloneSlice())
			{
				slice.Object = m_entry;
				Assert.That(slice.Object, Is.SameAs(m_entry));
			}
		}

		/// <summary>
		/// BASELINE: Cache can be set on a standalone slice.
		/// </summary>
		[Test]
		public void Cache_CanBeSetOnStandaloneSlice()
		{
			using (var slice = CreateStandaloneSlice())
			{
				slice.Cache = Cache;
				Assert.That(slice.Cache, Is.SameAs(Cache));
			}
		}

		/// <summary>
		/// BASELINE: ContainingDataTree is null for a standalone slice.
		/// It returns Parent as DataTree, and Parent is null before Controls.Add.
		/// </summary>
		[Test]
		public void ContainingDataTree_IsNullForStandaloneSlice()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.ContainingDataTree, Is.Null,
					"ContainingDataTree should be null before the slice is installed in a DataTree.");
			}
		}

		/// <summary>
		/// BASELINE: ContainingDataTree is set after Install().
		/// </summary>
		[Test]
		public void ContainingDataTree_IsSetAfterInstall()
		{
			var dtree = CreatePopulatedDataTree();
			var slice = dtree.Controls[0] as Slice;
			Assert.That(slice.ContainingDataTree, Is.SameAs(dtree),
				"ContainingDataTree should reference the parent DataTree after install.");
		}

		/// <summary>
		/// BASELINE: Mediator can be set on a standalone slice without accessing SplitCont.
		/// However, it does access Control — which accesses SplitCont.Panel2.
		/// </summary>
		[Test]
		public void Mediator_SetOnStandaloneSlice_DoesNotThrow()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.DoesNotThrow(() => { slice.Mediator = m_mediator; },
					"Setting Mediator on a standalone slice must not throw.");
			}
		}

		#endregion

		#region Category 9: IndexInContainer and Slices/Controls consistency

		/// <summary>
		/// BASELINE SENTINEL: After ShowObject, Slices.Count matches Controls.Count
		/// (they are the same set of objects).
		/// Virtualization is expected to intentionally break this invariant.
		/// </summary>
		[Test]
		public void SlicesCount_MatchesControlsCount_AfterShowObject()
		{
			var dtree = CreatePopulatedDataTree();
			Assert.That(dtree.Slices.Count, Is.EqualTo(dtree.Controls.Count),
				"Before virtualization, Slices.Count must equal Controls.Count.");
		}

		/// <summary>
		/// BASELINE SENTINEL: Each slice's IndexInContainer matches its position in Controls/Slices.
		/// </summary>
		[Test]
		public void IndexInContainer_ConsistentForAllSlices()
		{
			var dtree = CreatePopulatedDataTree();
			for (int i = 0; i < dtree.Slices.Count; i++)
			{
				Assert.That(dtree.Slices[i].IndexInContainer, Is.EqualTo(i),
					$"Slice at position {i} should report IndexInContainer == {i}.");
			}
		}

		/// <summary>
		/// BASELINE SENTINEL: Slices[i] identity matches Controls[i] identity.
		/// Virtualization is expected to decouple these two collections.
		/// </summary>
		[Test]
		public void SlicesAndControls_SameIdentity()
		{
			var dtree = CreatePopulatedDataTree();
			for (int i = 0; i < dtree.Slices.Count; i++)
			{
				Assert.That(dtree.Slices[i], Is.SameAs(dtree.Controls[i]),
					$"Slices[{i}] and Controls[{i}] must be the same object before virtualization.");
			}
		}

		#endregion

		#region Category 10: Slice construction HWND count

		/// <summary>
		/// BASELINE: A constructed (but not installed) Slice has child controls that will
		/// create HWNDs. Count them to establish the baseline HWND cost.
		/// </summary>
		[Test]
		public void Construction_CreatesExpectedChildControls()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.Controls.Count, Is.EqualTo(1),
					"Slice should have exactly 1 child control (the SplitContainer).");
				Assert.That(slice.Controls[0], Is.InstanceOf<SplitContainer>());

				var sc = slice.SplitCont;
				Assert.That(sc.Panel1, Is.Not.Null, "SplitContainer.Panel1 exists.");
				Assert.That(sc.Panel2, Is.Not.Null, "SplitContainer.Panel2 exists.");
			}
		}

		/// <summary>
		/// BASELINE: After Install, a Slice also has a SliceTreeNode in Panel1,
		/// adding at least 1 more HWND.
		/// </summary>
		[Test]
		public void Install_AddsSliceTreeNodeHWND()
		{
			var dtree = CreatePopulatedDataTree();
			using (var slice = new Slice())
			{
				int panel1CountBefore = slice.SplitCont.Panel1.Controls.Count;
				Assert.That(panel1CountBefore, Is.EqualTo(0));

				slice.Install(dtree);

				Assert.That(slice.SplitCont.Panel1.Controls.Count, Is.EqualTo(1),
					"Install should add exactly 1 SliceTreeNode to Panel1.");
			}
		}

		/// <summary>
		/// BASELINE: When a Slice has a content control (e.g., TextBox), it's in Panel2.
		/// This adds 1+ HWNDs.
		/// </summary>
		[Test]
		public void ContentControl_InPanel2_AddsHWND()
		{
			using (var slice = CreateStandaloneSliceWithControl())
			{
				Assert.That(slice.SplitCont.Panel2.Controls.Count, Is.EqualTo(1),
					"Content control should be in Panel2.");
				Assert.That(slice.Control, Is.InstanceOf<TextBox>());
			}
		}

		#endregion
	}
}
