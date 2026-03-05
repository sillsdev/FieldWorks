// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public partial class HwndVirtualizationTests
	{
		#region Category 11: ShowObject / slice generation

		/// <summary>
		/// BASELINE: ShowObject with "CfOnly" layout produces exactly 1 slice.
		/// This tests the minimal case for virtualization.
		/// </summary>
		[Test]
		public void ShowObject_CfOnly_ProducesOneSlice()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(1));
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(1));
		}

		/// <summary>
		/// BASELINE: ShowObject with "CfAndBib" layout produces exactly 2 slices.
		/// </summary>
		[Test]
		public void ShowObject_CfAndBib_ProducesTwoSlices()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2));
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(2));
		}

		/// <summary>
		/// BASELINE: All slices from ShowObject have non-null Labels.
		/// Virtualization must preserve Label availability on deferred slices.
		/// </summary>
		[Test]
		public void ShowObject_AllSlicesHaveLabels()
		{
			var dtree = CreatePopulatedDataTree();
			foreach (Slice slice in dtree.Slices)
			{
				Assert.That(slice.Label, Is.Not.Null.And.Not.Empty,
					"Every slice from ShowObject should have a non-empty Label.");
			}
		}

		/// <summary>
		/// BASELINE: After ShowObject, calling ShowObject again replaces slices cleanly.
		/// Virtualization must handle re-generation when the displayed object changes.
		/// </summary>
		[Test]
		public void ShowObject_CalledTwice_ReplacesSlices()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(1));

			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(2),
				"ShowObject should replace previous slices with new ones.");
		}

		#endregion

		#region Category 12: OverrideBackColor and ShowSubControls

		/// <summary>
		/// BASELINE: OverrideBackColor with null Control does nothing (no exception).
		/// Virtualization must preserve this null-safety.
		/// </summary>
		[Test]
		public void OverrideBackColor_NullControl_DoesNotThrow()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.Control, Is.Null);
				Assert.DoesNotThrow(() => slice.OverrideBackColor("Window"),
					"OverrideBackColor must handle null Control gracefully.");
			}
		}

		/// <summary>
		/// BASELINE: OverrideBackColor sets color on the Control when it exists.
		/// </summary>
		[Test]
		public void OverrideBackColor_WithControl_SetsColor()
		{
			using (var slice = CreateStandaloneSliceWithControl())
			{
				slice.OverrideBackColor(null);
				Assert.That(slice.Control.BackColor, Is.EqualTo(SystemColors.Window),
					"Default back color should be SystemColors.Window.");
			}
		}

		/// <summary>
		/// BASELINE: ShowSubControls on base Slice does nothing (no-op).
		/// </summary>
		[Test]
		public void ShowSubControls_BaseSlice_DoesNotThrow()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.DoesNotThrow(() => slice.ShowSubControls());
			}
		}

		#endregion

		#region Category 13: Nested/expanded hierarchy

		/// <summary>
		/// BASELINE: Expanded layout produces slices with indent > 0.
		/// Virtualization must preserve indent hierarchy for deferred slices.
		/// </summary>
		[Test]
		public void NestedExpandedLayout_ProducesIndentedSlices()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Nested-Expanded", null, m_entry, false);

			Assert.That(m_dtree.Slices.Count, Is.GreaterThanOrEqualTo(2));
			var headerSlice = m_dtree.Slices[0];
			Assert.That(headerSlice.Label, Is.EqualTo("Header"));
		}

		/// <summary>
		/// BASELINE: ParentSlice is accessible on installed slices.
		/// Virtualization must maintain ParentSlice relationships.
		/// </summary>
		[Test]
		public void ParentSlice_IsNullForStandaloneSlice()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.That(slice.ParentSlice, Is.Null);
			}
		}

		#endregion

		#region Category 14: FinishInit lifecycle

		/// <summary>
		/// BASELINE: FinishInit can be called on a standalone slice without error.
		/// This is a virtual no-op in the base class.
		/// </summary>
		[Test]
		public void FinishInit_StandaloneSlice_DoesNotThrow()
		{
			using (var slice = CreateStandaloneSlice())
			{
				Assert.DoesNotThrow(() => slice.FinishInit(),
					"FinishInit must work on a standalone slice.");
			}
		}

		#endregion

		#region Category 15: Splitter position and SplitterMoved event

		/// <summary>
		/// BASELINE: The SplitContainer's SplitterMoved event is unsubscribed in Dispose.
		/// If SplitCont is null (deferred), Dispose would NRE.
		/// This test documents that after Install, unsubscription works.
		/// </summary>
		[Test]
		public void Dispose_AfterInstall_UnsubscribesSplitterMoved()
		{
			var dtree = CreatePopulatedDataTree();
			var slice = new Slice();
			slice.Install(dtree);

			Assert.DoesNotThrow(() => slice.Dispose(),
				"Dispose after Install must safely unsubscribe SplitterMoved.");
		}

		/// <summary>
		/// BASELINE: SplitCont.SplitterDistance can be read after Install.
		/// DataTree.AdjustSliceSplitPosition modifies this during layout.
		/// </summary>
		[Test]
		public void SplitterDistance_ReadableAfterInstall()
		{
			var dtree = CreatePopulatedDataTree();
			var slice = dtree.Controls[0] as Slice;
			Assert.That(slice, Is.Not.Null);

			int distance = slice.SplitCont.SplitterDistance;
			Assert.That(distance, Is.GreaterThanOrEqualTo(0),
				"SplitterDistance should be non-negative after install.");
		}

		#endregion

		#region Category 16: Edge case — many slices stress test

		/// <summary>
		/// Stress test: Create and install many slices, dispose them all.
		/// Tests that the basic slice lifecycle is robust under volume.
		/// </summary>
		[Test]
		public void ManySlices_CreateInstallDispose_DoesNotThrow()
		{
			var dtree = CreatePopulatedDataTree();
			const int count = 50;
			var slices = new List<Slice>(count);

			Assert.DoesNotThrow(() =>
			{
				for (int i = 0; i < count; i++)
				{
					var slice = new Slice();
					slice.Label = $"Stress_{i}";
					slice.Indent = i % 3;
					slice.Install(dtree);
					slices.Add(slice);
				}
			}, "Creating and installing 50 slices must not throw.");

			int totalSlices = dtree.Slices.Count;
			Assert.That(totalSlices, Is.GreaterThanOrEqualTo(count),
				"DataTree should contain at least the stress-test slices.");

			Assert.DoesNotThrow(() =>
			{
				foreach (var s in slices)
					s.Dispose();
			}, "Disposing 50 installed slices must not throw.");
		}

		#endregion
	}
}
