// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SplitGridViewTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Controls.SplitGridView
{
	#region DummySplitGrid
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummySplitGrid : SplitGrid
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class DummyCollapsibleDataGridView : CollapsibleDataGridView
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Calls the on shown.
			/// </summary>
			/// <param name="sender">The sender.</param>
			/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
			/// --------------------------------------------------------------------------------
			public void CallOnShown(object sender, EventArgs e)
			{
				OnShown(sender, e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummySplitGrid"/> class.
		/// </summary>
		/// <param name="rows">The number of rows.</param>
		/// <param name="cols">The number of columns.</param>
		/// ------------------------------------------------------------------------------------
		public DummySplitGrid(int rows, int cols)
			: base(null, null, rows, cols)
		{
			Visible = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the data grid view.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override CollapsibleDataGridView CreateDataGridView()
		{
			return new DummyCollapsibleDataGridView();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the columns.
		/// </summary>
		/// <value>The columns.</value>
		/// ------------------------------------------------------------------------------------
		public DataGridViewColumnCollection Columns
		{
			get { return m_grid.Columns; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rows.
		/// </summary>
		/// <value>The rows.</value>
		/// ------------------------------------------------------------------------------------
		public DataGridViewRowCollection Rows
		{
			get { return m_grid.Rows; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the on shown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallOnShown()
		{
			OnShown(this, EventArgs.Empty);
			((DummyCollapsibleDataGridView)m_grid).CallOnShown(this, EventArgs.Empty);
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SplitGridViewTests
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the height of the rows is about the same if don't specify any height.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RowHeightDistributedEvenly()
		{
			DummySplitGrid grid = new DummySplitGrid(3, 3);
			grid.Size = new Size(100, 100);
			grid.CreateControl();
			grid.AddControl(null, 0, 0, null);
			grid.AddControl(null, 0, 1, null);
			grid.AddControl(null, 0, 2, null);
			grid.AddControl(null, 1, 0, null);
			grid.AddControl(null, 1, 1, null);
			grid.AddControl(null, 1, 2, null);
			grid.AddControl(null, 2, 0, null);
			grid.AddControl(null, 2, 1, null);
			grid.AddControl(null, 2, 2, null);

			foreach (DataGridViewColumn column in grid.Columns)
				column.Visible = true;
			foreach (DataGridViewRow row in grid.Rows)
				row.Visible = true;

			grid.CallOnShown();

			// Grid height is 100 with 3 rows (i.e. 2 splitters). With a 1px border this leaves 98.
			Assert.AreEqual(32, grid.Rows[0].Height);
			Assert.AreEqual(32, grid.Rows[1].Height);
			Assert.AreEqual(34, grid.Rows[2].Height);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// If only one row is visible that row should get the entire space.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void OneVisibleRowGetsAll()
		{
			DummySplitGrid grid = new DummySplitGrid(3, 3);
			grid.Size = new Size(100, 100);
			grid.CreateControl();
			grid.AddControl(null, 0, 0, null);
			grid.AddControl(null, 0, 1, null);
			grid.AddControl(null, 0, 2, null);
			grid.AddControl(null, 1, 0, null);
			grid.AddControl(null, 1, 1, null);
			grid.AddControl(null, 1, 2, null);
			grid.AddControl(null, 2, 0, null);
			grid.AddControl(null, 2, 1, null);
			grid.AddControl(null, 2, 2, null);

			foreach (DataGridViewColumn column in grid.Columns)
				column.Visible = true;
			grid.Rows[1].Visible = true;

			grid.CallOnShown();

			Assert.AreEqual(100, grid.Rows[1].Height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If we're displaying two rows with a weight specified the height of the rows should
		/// be according to the ratio of the weights.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TwoRowsWithWeight()
		{
			DummySplitGrid grid = new DummySplitGrid(3, 3);
			grid.Size = new Size(101, 101);
			grid.CreateControl();
			grid.AddControl(null, 0, 0, null);
			grid.AddControl(null, 1, 0, null);
			grid.AddControl(null, 2, 0, null);

			grid.Columns[0].Visible = true;
			grid.Rows[0].Visible = true;
			grid.Rows[1].Visible = true;
			grid.Rows[2].Visible = false;
			grid.GetRow(0).FillWeight = 100;
			grid.GetRow(1).FillWeight = 17;

			grid.CallOnShown();

			// Rows should occupy 85%/15% of the available height.
			Assert.AreEqual(85, grid.Rows[0].Height);
			Assert.AreEqual(15, grid.Rows[1].Height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If we're displaying three rows with a weight specified and one of the
		/// rows is fixed, the height of the rows should be be according to the ratio of the
		/// weights of the two non-fixed rows.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ThreeRowsWithWeightAndFixedRow()
		{
			DummySplitGrid grid = new DummySplitGrid(3, 3);
			grid.Size = new Size(122, 122);
			grid.CreateControl();
			grid.AddControl(null, 0, 0, null);
			grid.AddControl(null, 1, 0, null);
			grid.AddControl(null, 2, 0, null);

			grid.Columns[0].Visible = true;
			grid.Rows[0].Visible = true;
			grid.Rows[1].Visible = true;
			grid.Rows[2].Visible = true;
			grid.GetRow(0).Height = 20;
			grid.GetRow(0).IsAutoFill = false;
			grid.GetRow(1).FillWeight = 100;
			grid.GetRow(2).FillWeight = 17;

			grid.CallOnShown();

			// First row should have fixed height of 20
			Assert.AreEqual(20, grid.Rows[0].Height);
			// Rows should occupy 85%/15% of the available height.
			Assert.AreEqual(85, grid.Rows[1].Height);
			Assert.AreEqual(15, grid.Rows[2].Height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changing the height of a row should change its weight.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangingHeightChangesWeight()
		{
			DummySplitGrid grid = new DummySplitGrid(3, 1);
			grid.Size = new Size(102, 102);
			grid.CreateControl();
			grid.AddControl(null, 0, 0, null);
			grid.AddControl(null, 1, 0, null);
			grid.AddControl(null, 2, 0, null);

			grid.Columns[0].Visible = true;
			grid.Rows[0].Visible = true;
			grid.Rows[1].Visible = true;
			grid.Rows[2].Visible = true;
			grid.GetRow(0).FillWeight = 100;
			grid.GetRow(1).FillWeight = 200;
			grid.GetRow(2).FillWeight = 100;

			grid.CallOnShown();

			// first and third row occupy 25% each
			Assert.AreEqual(25, grid.Rows[0].Height);
			Assert.AreEqual(25, grid.Rows[2].Height);
			// second row occupies 50%.
			Assert.AreEqual(50, grid.Rows[1].Height);

			// Now change the height of the first row to occupy 50%
			grid.Rows[0].Height = 50;

			Assert.AreEqual(50, grid.Rows[0].Height);
			// second now occupies 2/3 of the remaining 50%, i.e. 33% total
			Assert.AreEqual(33, grid.Rows[1].Height);
			// third row occupies the rest (1/3 of the remaining 50%)
			Assert.AreEqual(17, grid.Rows[2].Height);
		}
	}
}
