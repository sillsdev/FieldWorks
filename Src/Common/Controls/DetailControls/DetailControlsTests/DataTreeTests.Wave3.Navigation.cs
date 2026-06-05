// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	[TestFixture]
	public partial class DataTreeTests
	{
		#region Wave 3 — Navigation Additions

		[Test]
		public void GotoFirstSlice_WithNoSlices_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => m_dtree.GotoFirstSlice());
			Assert.That(m_dtree.CurrentSlice, Is.Null);
		}

		[Test]
		public void GotoNextSlice_WithNoCurrentSlice_LeavesCurrentNull()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

			Assert.That(m_dtree.CurrentSlice, Is.Null);
			m_dtree.GotoNextSlice();
			Assert.That(m_dtree.CurrentSlice, Is.Null);
		}

		[Test]
		public void GotoNextSlice_WithCurrentAtLast_DoesNotAdvance()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			var onlySlice = m_dtree.Slices[0];
			SetCurrentSliceFieldForTest(onlySlice);

			m_dtree.GotoNextSlice();

			Assert.That(m_dtree.CurrentSlice, Is.SameAs(onlySlice));
		}

		[Test]
		public void IndexOfSliceAtY_WithNoSlices_ReturnsMinusOne()
		{
			Assert.That(m_dtree.IndexOfSliceAtY(0), Is.EqualTo(-1));
			Assert.That(m_dtree.IndexOfSliceAtY(42), Is.EqualTo(-1));
		}

		[Test]
		public void GotoPreviousSliceBeforeIndex_WithNoSlices_ReturnsFalse()
		{
			bool moved = m_dtree.GotoPreviousSliceBeforeIndex(0);
			Assert.That(moved, Is.False);
		}

		#endregion
	}
}
