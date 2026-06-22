// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Characterization tests that lock down the CURRENT disposal, event-unsubscription, focus, and
	/// accessibility behavior of <see cref="DataTree"/> and <see cref="Slice"/> BEFORE the Avalonia
	/// refactor (task 2.7 of lexical-edit-avalonia-migration). These protect the refactor: an Avalonia
	/// adapter selected by the two-adapter flag must preserve this observable behavior.
	///
	/// They assert observable behavior only (IsDisposed, no-throw after Dispose, AccessibleName,
	/// focus order) so they remain robust across the refactor rather than pinning private internals.
	/// </summary>
	[TestFixture]
	public class DataTreeDisposalCharacterizationTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
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
				TsStringUtils.MakeString("citation", Cache.DefaultVernWs);
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

			base.TestTearDown();
		}

		#endregion

		private void ShowCfAndBib()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
		}

		/// <summary>
		/// Disposing the DataTree (via its parent form) disposes all realized slices and marks the
		/// tree disposed. Locks the cascade in <see cref="DataTree.Dispose"/>.
		/// </summary>
		[Test]
		public void Dispose_AfterShowObject_DisposesAllSlicesAndTree()
		{
			ShowCfAndBib();
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2));
			var slice0 = (Slice)m_dtree.Controls[0];
			var slice1 = (Slice)m_dtree.Controls[1];

			m_parent.Dispose();

			Assert.That(m_dtree.IsDisposed, Is.True, "DataTree should be disposed via its parent form.");
			Assert.That(slice0.IsDisposed, Is.True, "Slice 0 should be disposed with the tree.");
			Assert.That(slice1.IsDisposed, Is.True, "Slice 1 should be disposed with the tree.");
		}

		/// <summary>
		/// After disposal the DataTree has removed its LCModel PropChanged notification, so a later
		/// model change does not call back into the disposed tree and does not throw.
		/// </summary>
		[Test]
		public void Dispose_ThenLcModelChange_DoesNotThrow()
		{
			ShowCfAndBib();
			m_parent.Dispose();

			// A model change after disposal must not reach the disposed tree (RemoveNotification(this)).
			Assert.That(() =>
			{
				m_entry.CitationForm.VernacularDefaultWritingSystem =
					TsStringUtils.MakeString("changed after dispose", Cache.DefaultVernWs);
			}, Throws.Nothing);
		}

		/// <summary>Disposing twice is safe (the guard in Dispose makes it idempotent).</summary>
		[Test]
		public void Dispose_IsIdempotent()
		{
			ShowCfAndBib();
			m_parent.Dispose();
			Assert.That(() => m_dtree.Dispose(), Throws.Nothing, "Second Dispose must be a no-op.");
			Assert.That(m_dtree.IsDisposed, Is.True);
		}

		/// <summary>
		/// Disposing after assigning a current slice exercises the SetCurrentState(false) path during
		/// <see cref="DataTree.Dispose"/>; it must not throw. (Note: without a shown form the current
		/// slice does not latch, which this test documents rather than fights.)
		/// </summary>
		[Test]
		public void Dispose_AfterAssigningCurrentSlice_DoesNotThrow()
		{
			ShowCfAndBib();
			m_dtree.CurrentSlice = (Slice)m_dtree.Controls[0];

			Assert.That(() => m_parent.Dispose(), Throws.Nothing);
			Assert.That(m_dtree.IsDisposed, Is.True);
		}

		/// <summary>
		/// Each realized slice exposes its label as the accessible name of its control. This is the
		/// in-process accessibility "reachability" baseline the Avalonia editors must match.
		/// </summary>
		[Test]
		public void Slices_ExposeAccessibleNameMatchingLabel()
		{
			ShowCfAndBib();

			var cf = (Slice)m_dtree.Controls[0];
			var bib = (Slice)m_dtree.Controls[1];
			Assert.That(cf.Control.AccessibleName, Is.EqualTo("CitationForm"));
			Assert.That(bib.Control.AccessibleName, Is.EqualTo("Bibliography"));
		}

		/// <summary>
		/// The realized slice order (focus order) follows the layout order: CitationForm then
		/// Bibliography. Locks the baseline focus order.
		/// </summary>
		[Test]
		public void Slices_FocusOrderFollowsLayoutOrder()
		{
			ShowCfAndBib();

			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2));
			Assert.That(((Slice)m_dtree.Controls[0]).Label, Is.EqualTo("CitationForm"));
			Assert.That(((Slice)m_dtree.Controls[1]).Label, Is.EqualTo("Bibliography"));
			Assert.That(m_dtree.Controls.IndexOf((Control)m_dtree.Controls[0]), Is.EqualTo(0));
		}
	}
}
