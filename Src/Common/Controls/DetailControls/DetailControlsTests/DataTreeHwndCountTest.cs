// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.TestUtilities;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	[TestFixture]
	public class DataTreeHwndCountTest : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Inventory m_parts;
		private Inventory m_layouts;
		private ILexEntry m_entry;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private DataTree m_dtree;
		private Form m_parent;

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_layouts = DataTreeTests.GenerateLayouts();
			m_parts = DataTreeTests.GenerateParts();
		}

		public override void TestSetup()
		{
			base.TestSetup();
			m_entry = CreateEntryWithSenses(40);
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_dtree = new DataTree();
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
			m_dtree = null;
			base.TestTearDown();
		}

		[Test]
		public void ShowObject_RecordsUserHandleDeltaAndInstallCount()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			int userHandlesBefore = HwndDiagnostics.GetCurrentProcessUserHandleCount();

			m_dtree.ShowObject(m_entry, "Normal", null, m_entry, false);
			Application.DoEvents();

			int userHandlesAfter = HwndDiagnostics.GetCurrentProcessUserHandleCount();
			int userHandleDelta = userHandlesAfter - userHandlesBefore;

			Assert.That(m_dtree.Slices.Count, Is.GreaterThan(20),
				"Normal layout with 40 senses should produce a large slice set for baseline diagnostics.");
			Assert.That(m_dtree.SliceInstallCreationCount, Is.EqualTo(m_dtree.Slices.Count),
				"Baseline behavior installs all slices and creates hosted controls for each slice.");
			Assert.That(userHandleDelta, Is.GreaterThanOrEqualTo(0),
				"USER handle delta should be non-negative for this single-ShowObject baseline probe.");

			int threshold = m_dtree.Slices.Count * 8;
			Assert.That(userHandleDelta, Is.LessThanOrEqualTo(threshold),
				$"USER-handle growth should stay within baseline threshold of <= 8 per slice. " +
				$"Delta={userHandleDelta}, Slices={m_dtree.Slices.Count}, Threshold={threshold}.");
		}

		[Test]
		public void ShowObject_SecondCallWithSameRoot_RebuildsSlices_Baseline()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Normal", null, m_entry, false);
			int installCountAfterFirstShow = m_dtree.SliceInstallCreationCount;
			int firstSliceCount = m_dtree.Slices.Count;

			m_dtree.ShowObject(m_entry, "Normal", null, m_entry, false);
			Application.DoEvents();

			Assert.That(installCountAfterFirstShow, Is.GreaterThan(0),
				"First ShowObject should install slices.");
			Assert.That(firstSliceCount, Is.GreaterThan(0),
				"Baseline should have created at least one slice on first ShowObject.");
			Assert.That(m_dtree.SliceInstallCreationCount, Is.EqualTo(m_dtree.Slices.Count),
				"Baseline behavior rebuilds/reinstalls slices on same-root ShowObject refresh.");
		}

		[Test]
		public void ShowObject_WithDeferredCreation_NotAllSlicesArePhysicallyInstalled()
		{
			m_dtree.DeferSliceHwndCreationEnabled = true;
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			m_parent.Show();
			m_dtree.ShowObject(m_entry, "Normal", null, m_entry, false);
			Application.DoEvents();

			int totalSlices = m_dtree.Slices.Count;
			int physicallyInstalled = m_dtree.Slices.Count(slice => slice.Parent == m_dtree);
			int slicesWithTreeNode = m_dtree.Slices.Count(slice => slice.TreeNode != null);
			int slicesWithHandles = m_dtree.Slices.Count(slice => slice.IsHandleCreated);

			Assert.That(totalSlices, Is.GreaterThan(20),
				"Test requires a substantial slice set to verify deferred physical installation behavior.");
			Assert.That(physicallyInstalled, Is.GreaterThan(0),
				"Visible/active slices should still be physically installed when deferred mode is enabled.");
			Assert.That(physicallyInstalled, Is.LessThan(totalSlices),
				"Deferred mode should avoid physically installing every slice immediately.");
			Assert.That(slicesWithTreeNode, Is.EqualTo(physicallyInstalled),
				"TreeNode creation should align with physical install status in deferred mode.");
			Assert.That(slicesWithHandles, Is.EqualTo(physicallyInstalled),
				"Only physically installed slices should own HWND handles immediately after initial layout.");
		}

		[Test]
		public void DeferredLayout_AfterAllSlicesMaterialized_VirtualHeightMatchesActualHeight()
		{
			m_dtree.DeferSliceHwndCreationEnabled = true;
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			m_parent.Show();
			m_dtree.ShowObject(m_entry, "Normal", null, m_entry, false);
			Application.DoEvents();

			int totalSlices = m_dtree.Slices.Count;
			Assert.That(totalSlices, Is.GreaterThan(20),
				"Test requires a substantial slice set to validate deferred virtual layout convergence.");

			for (int index = 0; index < totalSlices; index++)
			{
				m_dtree.MakeSliceVisible(m_dtree.Slices[index], index);
				Application.DoEvents();
			}

			m_dtree.PerformLayout();
			Application.DoEvents();

			int physicallyInstalled = m_dtree.Slices.Count(slice => slice.Parent == m_dtree);
			Assert.That(physicallyInstalled, Is.EqualTo(totalSlices),
				"After materializing all slices, every logical slice should be physically installed.");

			int minHeight = m_dtree.GetMinFieldHeight();
			int scrollOffset = -m_dtree.AutoScrollPosition.Y;
			int actualBottom = m_dtree.Slices.Max(slice =>
				slice.Top + scrollOffset + Math.Max(minHeight, slice.Height) + 1);
			int virtualBottom = m_dtree.AutoScrollMinSize.Height;

			Assert.That(Math.Abs(virtualBottom - actualBottom), Is.LessThanOrEqualTo(1),
				$"Virtual layout bottom should converge to actual realized layout bottom within 1px. " +
				$"Virtual={virtualBottom}, Actual={actualBottom}, Slices={totalSlices}.");
		}

		private ILexEntry CreateEntryWithSenses(int senseCount)
		{
			if (senseCount < 1)
				throw new ArgumentOutOfRangeException(nameof(senseCount));

			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("hwnd baseline", Cache.DefaultVernWs);
			for (int index = 0; index < senseCount; index++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(
					$"gloss-{index}", Cache.DefaultAnalWs);
			}
			return entry;
		}
	}
}
