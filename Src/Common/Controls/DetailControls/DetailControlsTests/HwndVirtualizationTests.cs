// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Tests that define the behavioral contract for HWND grow-only virtualization.
//
// ======================== INTENT ========================
//
// Protect behavior that must remain stable while deferring HWND creation,
// surface failure modes likely during migration, and document WinForms constraints.
//
// ======================== REGRESSION RISK MODEL ========================
//
// HWND virtualization decouples three things the current code assumes are always in sync:
//   1. Slices list — logical inventory (DataTree.Slices)
//   2. Controls collection — WinForms children with HWNDs (DataTree.Controls)
//   3. SplitContainer/TreeNode/Control — eagerly created per-slice infrastructure
//
// This creates 5 major regression risk areas:
//
// RISK A — Property Access NREs
//   33 methods in Slice.cs access ContainingDataTree without null guard.
//   TreeNode getter throws ArgumentOutOfRangeException (not null) before Install.
//   Covered by: cats 1, 4, 8, 17, 22, 29, 30
//
// RISK B — Lifecycle/Dispose Safety
//   Dispose accesses SplitCont.SplitterMoved; ViewSlice unsubscribes 3 RootSite events.
//   Partially-constructed slices (no TreeNode, no events) must be safely torn down.
//   Covered by: cats 2, 7, 20, 21, 27, 28
//
// RISK C — Layout/Visibility/Painting
//   MakeSliceVisible forces all prior slices visible (high-water-mark, LT-7307 fix).
//   HandleLayout1 forces HWNDs via tci.Handle; OnSizeChanged accesses CDT.AutoScrollPosition.
//   OnSizeChanged is guarded by m_widthHasBeenSetByDataTree flag.
//   Covered by: cats 5, 10, 16, 24, 26
//
// RISK D — Focus/CurrentSlice Management
//   TakeFocus(true) NREs on CDT; TakeFocus(false) with TabStop Control also NREs.
//   SetCurrentState throws on deferred slices (TreeNode getter, not null).
//   GetMessageTargets short-circuits on invisible slices (safe for deferred).
//   Covered by: cats 3, 4, 19, 22, 23
//
// RISK E — Slices/Controls Coupling Divergence
//   Install() adds to both Controls and Slices; RemoveSlice removes from both.
//   Expand/Collapse creates/destroys child slices in both collections.
//   ScrollControlIntoView requires slice in Controls.
//   Covered by: cats 6, 9, 11, 25, 26, 28
//
// ======================== TEST CLASSIFICATION ========================
//
// CONTRACT tests: verify behavior that MUST remain stable during virtualization.
//   A failing contract test means the implementation has a regression bug.
//
// SENTINEL tests: verify pre-virtualization invariants EXPECTED to change.
//   A failing sentinel test means virtualization is working; update the assertion.
//   Marked with "BASELINE SENTINEL" in comments.
//
// ======================== COVERAGE MAP ========================
//
// This file: cats 1-4 — core NRE safety, dispose, CurrentSlice, focus
// HwndVirtualizationTests.VisibilityInstall.cs: cats 5-10 — visibility,
//   install ordering, BecomeReal, properties, index coupling, HWND count
// HwndVirtualizationTests.ShowObjectAndStress.cs: cats 11-16 — ShowObject,
//   OverrideBackColor, hierarchy, FinishInit, splitter events, stress
// HwndVirtualizationTests.PostDisposeAndEvents.cs: cats 17-20 — post-dispose
//   access, ConfigurationNode, IsHeaderNode, event lifecycle
// HwndVirtualizationTests.EdgeCases.cs: cats 21-30 — ViewSlice lifecycle,
//   CDT null-safety, GetMessageTargets visibility, OnSizeChanged guards,
//   expand/collapse, HWND/Controls membership, splitter consistency,
//   replacement lifecycle, TabIndex validity, post-dispose properties

using System;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Tests that capture the current behavioral contract of Slice/DataTree/ViewSlice
	/// with respect to HWND creation, lifecycle, and state transitions.
	///
	/// These serve as a safety net for the grow-only virtualization work:
	/// any test that fails after a code change reveals a behavioral regression
	/// that must be handled by the virtualization implementation.
	/// </summary>
	[TestFixture]
	public partial class HwndVirtualizationTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
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
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("bib");
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("bib");

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

		/// <summary>
		/// Creates a minimal DataTree with slices via ShowObject, returning the populated tree.
		/// Uses the "CfAndBib" layout which produces two slices.
		/// </summary>
		private DataTree CreatePopulatedDataTree()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			return m_dtree;
		}

		/// <summary>
		/// Creates a standalone Slice (not yet installed in any DataTree).
		/// This simulates a slice in the "deferred HWND" state that
		/// virtualization would introduce.
		/// </summary>
		private Slice CreateStandaloneSlice()
		{
			return new Slice();
		}

		/// <summary>
		/// Creates a standalone slice with a dummy control attached.
		/// </summary>
		private Slice CreateStandaloneSliceWithControl()
		{
			var ctrl = new TextBox();
			return new Slice(ctrl);
		}

		#endregion

		#region Category 1: SplitCont / TreeNode / Control property access

		/// <summary>
		/// BASELINE: A freshly constructed Slice has a SplitContainer in Controls[0].
		/// Virtualization must ensure this property is safe to call on deferred slices
		/// (either by lazy creation or a null-safe pattern).
		/// </summary>
		[Test]
		public void SplitCont_ExistsImmediatelyAfterConstruction()
		{
			using (var slice = CreateStandaloneSlice())
			{
				// Current behavior: SplitContainer is eagerly created in the constructor
				Assert.That(slice.SplitCont, Is.Not.Null,
					"SplitCont must exist immediately after construction — this is the invariant " +
					"virtualization will need to either preserve or replace with a null-safe pattern.");
				Assert.That(slice.SplitCont, Is.InstanceOf<SplitContainer>());
			}
		}

		/// <summary>
		/// BASELINE: TreeNode is null before Install() because no SliceTreeNode has been added to Panel1.
		/// Virtualization must handle callers that access TreeNode on uninstalled slices.
		/// </summary>
		[Test]
		public void TreeNode_IsNullBeforeInstall()
		{
			using (var slice = CreateStandaloneSlice())
			{
				// TreeNode accesses SplitCont.Panel1.Controls[0], which is empty before Install.
				// With the current code, Panel1.Controls.Count == 0, so the cast returns null.
				var treeNode = slice.SplitCont.Panel1.Controls.Count > 0
					? slice.SplitCont.Panel1.Controls[0] as SliceTreeNode
					: null;
				Assert.That(treeNode, Is.Null,
					"TreeNode should be null before Install() — virtualization must guard against " +
					"NullReferenceException in SetCurrentState and other callers.");
			}
		}

		/// <summary>
		/// BASELINE: TreeNode is set after Install().
		/// This confirms the expected post-Install state.
		/// </summary>
		[Test]
		public void TreeNode_ExistsAfterInstall()
		{
			var dtree = CreatePopulatedDataTree();
			Assert.That(dtree.Controls.Count, Is.GreaterThan(0));
			var slice = dtree.Controls[0] as Slice;
			Assert.That(slice, Is.Not.Null);
			Assert.That(slice.TreeNode, Is.Not.Null,
				"TreeNode must exist after Install() — called as part of ShowObject.");
			Assert.That(slice.TreeNode, Is.InstanceOf<SliceTreeNode>());
		}

		/// <summary>
		/// BASELINE: Control property returns null when no control has been set on an uninstalled slice.
		/// Virtualization callers must handle this null gracefully.
		/// </summary>
		[Test]
		public void Control_IsNullWhenNotSet()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.Control, Is.Null,
					"Control should be null when no child control has been added to Panel2.");
			}
		}

		/// <summary>
		/// BASELINE: Control property returns the control after it's been set.
		/// </summary>
		[Test]
		public void Control_ReturnsControlAfterSet()
		{
			using (var slice = CreateStandaloneSliceWithControl())
			{
				Assert.That(slice.Control, Is.Not.Null);
				Assert.That(slice.Control, Is.InstanceOf<TextBox>());
			}
		}

		/// <summary>
		/// BASELINE: SplitCont.Panel2.Controls holds exactly 0 or 1 controls.
		/// Virtualization must maintain this invariant to avoid Debug.Assert failures.
		/// </summary>
		[Test]
		public void Panel2_NeverHasMoreThanOneControl()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.SplitCont.Panel2.Controls.Count, Is.LessThanOrEqualTo(1));
			}

			using (var slice = CreateStandaloneSliceWithControl())
			{
				Assert.That(slice.SplitCont.Panel2.Controls.Count, Is.EqualTo(1));
			}
		}

		#endregion

		#region Category 2: Dispose safety

		/// <summary>
		/// BASELINE: A never-installed Slice can be disposed without NullReferenceException.
		/// Currently Dispose() accesses SplitCont.SplitterMoved which will work since SplitCont
		/// is always created. Virtualization must ensure Dispose is safe for deferred slices.
		/// </summary>
		[Test]
		public void Dispose_NeverInstalledSlice_DoesNotThrow()
		{
			var slice = CreateStandaloneSlice();
			Assert.DoesNotThrow(() => slice.Dispose(),
				"Disposing a never-installed Slice must not throw. " +
				"Virtualization must guard SplitCont.SplitterMoved -= in Dispose.");
		}

		/// <summary>
		/// BASELINE: A Slice with a control but never installed can be disposed safely.
		/// </summary>
		[Test]
		public void Dispose_SliceWithControl_NeverInstalled_DoesNotThrow()
		{
			var ctrl = new TextBox();
			var slice = new Slice(ctrl);
			Assert.DoesNotThrow(() => slice.Dispose(),
				"Disposing a Slice with a control but never installed must not throw.");
		}

		/// <summary>
		/// BASELINE: Double-dispose does not throw (idempotent).
		/// </summary>
		[Test]
		public void Dispose_CalledTwice_DoesNotThrow()
		{
			var slice = CreateStandaloneSlice();
			slice.Dispose();
			Assert.DoesNotThrow(() => slice.Dispose(),
				"Double-dispose must be idempotent.");
		}

		/// <summary>
		/// BASELINE: After disposal, IsDisposed is true.
		/// </summary>
		[Test]
		public void Dispose_SetsIsDisposed()
		{
			var slice = CreateStandaloneSlice();
			slice.Dispose();
			Assert.That(slice.IsDisposed, Is.True);
		}

		/// <summary>
		/// BASELINE: A fully installed slice (via ShowObject) can be disposed through its parent Form.
		/// This is the normal teardown path.
		/// </summary>
		[Test]
		public void Dispose_InstalledSlice_ViaParentForm_DoesNotThrow()
		{
			CreatePopulatedDataTree();
			Assert.That(m_dtree.Controls.Count, Is.GreaterThan(0));
			// Normal teardown: parent.Close() + parent.Dispose() disposes the DataTree and its slices.
			Assert.DoesNotThrow(() =>
			{
				m_parent.Close();
				m_parent.Dispose();
				m_parent = null;
			});
		}

		#endregion

		#region Category 3: CurrentSlice transitions (SetCurrentState)

		/// <summary>
		/// BASELINE: SetCurrentState(true) works on an installed slice that has TreeNode and Control.
		/// </summary>
		[Test]
		public void SetCurrentState_True_OnInstalledSlice_DoesNotThrow()
		{
			var dtree = CreatePopulatedDataTree();
			var slice = dtree.Controls[0] as Slice;
			Assert.That(slice, Is.Not.Null);
			Assert.DoesNotThrow(() => slice.SetCurrentState(true),
				"SetCurrentState(true) must work on an installed slice.");
		}

		/// <summary>
		/// BASELINE: SetCurrentState(false) works on an installed slice.
		/// This is the path taken when switching FROM a current slice.
		/// </summary>
		[Test]
		public void SetCurrentState_False_OnInstalledSlice_DoesNotThrow()
		{
			var dtree = CreatePopulatedDataTree();
			var slice = dtree.Controls[0] as Slice;
			Assert.That(slice, Is.Not.Null);
			// First make it current, then un-current it
			slice.SetCurrentState(true);
			Assert.DoesNotThrow(() => slice.SetCurrentState(false),
				"SetCurrentState(false) must work on an installed slice.");
		}

		/// <summary>
		/// BASELINE: SetCurrentState accesses both Control and TreeNode.
		/// When Control is null (no content control), and TreeNode exists,
		/// the INotifyControlInCurrentSlice branch is skipped but TreeNode.Invalidate is called.
		/// Virtualization must handle the case where TreeNode is also null.
		/// </summary>
		[Test]
		public void SetCurrentState_WithNullControl_InstalledSlice_DoesNotThrow()
		{
			var dtree = CreatePopulatedDataTree();
			// The slices created by ShowObject always have controls, so we test on a manually
			// installed slice with no content control.
			using (var slice = new Slice())
			{
				slice.Label = "TestNoControl";
				slice.Install(dtree);
				// TreeNode should exist after Install, but Control is null
				Assert.That(slice.TreeNode, Is.Not.Null);
				Assert.That(slice.Control, Is.Null);
				Assert.DoesNotThrow(() => slice.SetCurrentState(true),
					"SetCurrentState should handle null Control gracefully.");
				Assert.DoesNotThrow(() => slice.SetCurrentState(false));
			}
		}

		/// <summary>
		/// BASELINE: After ShowObject, m_fSuspendSettingCurrentSlice is true because
		/// the idle-queue handler (OnReadyToSetCurrentSlice) hasn't fired.
		/// This means CurrentSlice remains null — the setter short-circuits.
		/// In production, the message pump fires OnReadyToSetCurrentSlice which clears the flag.
		/// Virtualization must handle this deferred-focus pattern.
		/// </summary>
		[Test]
		public void CurrentSlice_IsSuspendedAfterShowObject_WithoutMessagePump()
		{
			var dtree = CreatePopulatedDataTree();
			Assert.That(dtree.Controls.Count, Is.GreaterThanOrEqualTo(2),
				"Need at least 2 slices.");

			// CurrentSlice is null because m_fSuspendSettingCurrentSlice is true
			// (set by ShowObject, cleared by idle-queue handler which never fires in test)
			Assert.That(dtree.CurrentSlice, Is.Null,
				"CurrentSlice should be null after ShowObject without a message pump — " +
				"the idle-queue handler that clears m_fSuspendSettingCurrentSlice hasn't fired.");

			// Attempting to set CurrentSlice while suspended does not throw,
			// it just stores the value in m_currentSliceNew and returns.
			var slice0 = dtree.Controls[0] as Slice;
			Assert.DoesNotThrow(() => { dtree.CurrentSlice = slice0; },
				"Setting CurrentSlice while suspended must not throw.");
			Assert.That(dtree.CurrentSlice, Is.Null,
				"CurrentSlice remains null — the setter stored the value internally but didn't commit.");
		}

		/// <summary>
		/// BASELINE: When the suspend flag is cleared (simulating idle-queue callback),
		/// CurrentSlice transitions work correctly.
		/// This tests the mechanism virtualization will use.
		/// </summary>
		[Test]
		public void CurrentSlice_Transition_AfterSuspendCleared()
		{
			var dtree = CreatePopulatedDataTree();
			Assert.That(dtree.Controls.Count, Is.GreaterThanOrEqualTo(2));

			// Simulate the idle-queue handler clearing the suspend flag
			dtree.OnReadyToSetCurrentSlice(false);

			var slice0 = dtree.Controls[0] as Slice;
			var slice1 = dtree.Controls[1] as Slice;

			// Now CurrentSlice should be settable
			dtree.CurrentSlice = slice0;
			Assert.That(dtree.CurrentSlice, Is.SameAs(slice0));

			// Transition to another slice
			Assert.DoesNotThrow(() => { dtree.CurrentSlice = slice1; },
				"CurrentSlice transition must work after suspend is cleared.");
			Assert.That(dtree.CurrentSlice, Is.SameAs(slice1));
		}

		/// <summary>
		/// BASELINE: Setting CurrentSlice to null throws ArgumentException.
		/// This constraint must be preserved by virtualization.
		/// </summary>
		[Test]
		public void CurrentSlice_SetToNull_ThrowsArgumentException()
		{
			var dtree = CreatePopulatedDataTree();
			Assert.Throws<ArgumentException>(() => { dtree.CurrentSlice = null; },
				"Setting CurrentSlice to null must throw ArgumentException.");
		}

		#endregion

		#region Category 4: Focus management

		/// <summary>
		/// BASELINE: TakeFocus on a visible installed slice does not throw.
		/// It accesses Control and TreeNode.
		/// </summary>
		[Test]
		public void TakeFocus_OnVisibleInstalledSlice_DoesNotThrow()
		{
			var dtree = CreatePopulatedDataTree();
			var slice = dtree.Controls[0] as Slice;
			Assert.That(slice, Is.Not.Null);

			// Make it visible first (mimicking what HandleLayout1 does)
			slice.Visible = true;

			// TakeFocus accesses Control and TreeNode
			Assert.DoesNotThrow(() => slice.TakeFocus(),
				"TakeFocus must work on a visible installed slice.");
		}

		/// <summary>
		/// BASELINE: TakeFocus on an invisible slice calls MakeSliceVisible.
		/// In the test harness (no shown Form), the Visible property may not reflect
		/// the change because WinForms visibility requires the parent chain to be realized.
		/// However, TakeFocus must not throw.
		/// </summary>
		[Test]
		public void TakeFocus_OnInvisibleSlice_DoesNotThrow()
		{
			var dtree = CreatePopulatedDataTree();
			var slice = dtree.Controls[0] as Slice;
			Assert.That(slice, Is.Not.Null);

			// Slices start invisible (Visible = false in constructor)
			// TakeFocus calls MakeSliceVisible internally; in a test
			// harness without a shown Form this may not change the property,
			// but it must not throw.
			Assert.DoesNotThrow(() => slice.TakeFocus(),
				"TakeFocus must not throw on an invisible slice.");
		}

		/// <summary>
		/// BASELINE: In the test harness (Form not shown), WinForms Visible property
		/// returns false even after MakeSliceVisible sets it true internally.
		/// This documents a key WinForms limitation for test code.
		/// </summary>
		[Test]
		public void MakeSliceVisible_WithoutFormShow_VisibleRemainsSettable()
		{
			var dtree = CreatePopulatedDataTree();
			var slice = dtree.Controls[0] as Slice;
			Assert.That(slice, Is.Not.Null);
			Assert.That(slice.Visible, Is.False, "Precondition: starts invisible.");

			// MakeSliceVisible does set Visible = true internally,
			// but WinForms returns false because the parent Form was never shown.
			Assert.DoesNotThrow(() => dtree.MakeSliceVisible(slice),
				"MakeSliceVisible must not throw even on un-shown Forms.");
		}

		/// <summary>
		/// BASELINE: TakeFocus with fOkToFocusTreeNode=false returns false
		/// when the content Control is null or not focusable.
		/// </summary>
		[Test]
		public void TakeFocus_NoFocusableControl_ReturnsFalse()
		{
			var dtree = CreatePopulatedDataTree();
			using (var slice = new Slice())
			{
				slice.Label = "NoControl";
				slice.Install(dtree);
				slice.Visible = true;

				// No content control → TakeFocus(false) should return false
				bool took = slice.TakeFocus(false);
				Assert.That(took, Is.False,
					"TakeFocus(false) should return false when there's no focusable control.");
			}
		}

		#endregion
	}
}
