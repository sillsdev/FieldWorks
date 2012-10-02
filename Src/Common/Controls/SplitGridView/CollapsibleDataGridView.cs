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
// File: CollapsibleDataGridView.cs
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
using System.Reflection;
using Microsoft.Win32;

namespace SIL.FieldWorks.Common.Controls.SplitGridView
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A DataGridView that collapses a column by hiding it if it reaches the minimum possible
	/// size (2 pixels).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CollapsibleDataGridView: DataGridView
	{
		#region Member variables
		private bool m_fFullyInitialized;
		private List<DataGridViewBand> m_ColumnsAndRowsToHide = new List<DataGridViewBand>();
		private bool m_fResizeInProgress;
		#endregion

		#region Disposed stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this before doing anything else.
		/// </summary>
		/// <remarks>This method is thread safe.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
			}

			base.Dispose(disposing);
		}
		#endregion

		#region Event handlers and overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when initially shown.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnShown(object sender, EventArgs e)
		{
			m_fFullyInitialized = true;
			ResizeRows(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.ParentChanged"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);

			Parent_ParentChanged(this, e);

			if (Parent != null)
				Parent.ParentChanged += new EventHandler(Parent_ParentChanged);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.DataGridView.DataError"></see> event.
		/// </summary>
		/// <param name="displayErrorDialogIfNoHandler">true to display an error dialog box if
		/// there is no handler for the <see cref="E:System.Windows.Forms.DataGridView.DataError">
		/// </see> event.</param>
		/// <param name="e">A <see cref="T:System.Windows.Forms.DataGridViewDataErrorEventArgs">
		/// </see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDataError(bool displayErrorDialogIfNoHandler, DataGridViewDataErrorEventArgs e)
		{
			displayErrorDialogIfNoHandler = false;
			base.OnDataError(displayErrorDialogIfNoHandler, e);
			throw e.Exception;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ParentChanged event of the Parent control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void Parent_ParentChanged(object sender, EventArgs e)
		{
			if (FindForm() != null)
				FindForm().Shown += new EventHandler(OnShown);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.DataGridView.ColumnWidthChanged"/>
		/// event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.DataGridViewColumnEventArgs"/>
		/// that contains the event data.</param>
		/// <exception cref="T:System.ArgumentException">The column indicated by the
		/// <see cref="P:System.Windows.Forms.DataGridViewColumnEventArgs.Column"/> property of
		/// e does not belong to this <see cref="T:System.Windows.Forms.DataGridView"></see>
		/// control.</exception>
		/// ------------------------------------------------------------------------------------
		protected override void OnColumnWidthChanged(DataGridViewColumnEventArgs e)
		{
			base.OnColumnWidthChanged(e);

			if (e.Column.Width == DataGridViewControlColumn.kMinimumValue && m_fFullyInitialized &&
				((DataGridViewControlColumn)e.Column).IsCollapsible)
			{
				// user dragged the divider all the way to the left, so we will hide the column
				m_ColumnsAndRowsToHide.Add(e.Column);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.DataGridView.RowHeightChanged"></see>
		/// event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.DataGridViewRowEventArgs"></see>
		/// that contains the event data.</param>
		/// <exception cref="T:System.ArgumentException">The row indicated by the
		/// <see cref="P:System.Windows.Forms.DataGridViewRowEventArgs.Row"></see> property of
		/// e does not belong to this <see cref="T:System.Windows.Forms.DataGridView"></see>
		/// control.</exception>
		/// ------------------------------------------------------------------------------------
		protected override void OnRowHeightChanged(DataGridViewRowEventArgs e)
		{
			if (e.Row.Height == DataGridViewControlColumn.kMinimumValue && m_fFullyInitialized)
			{
				// user dragged the divider all the way to the top, so we hide the row
				e.Row.Visible = false;
			}
			base.OnRowHeightChanged(e);

			if (!m_fResizeInProgress)
				AdjustRowWeight(e.Row as DataGridViewControlRow);

			ResizeRows(false);

			// Adjust the positions of the views in rows following the row that changed height
			int iLastRow = LastVisibleRow;
			for (int iRow = e.Row.Index + 1; iRow <= iLastRow; iRow++)
			{
				DataGridViewRow row = Rows[iRow];
				if (!row.Visible)
					continue;

				for (int iColumn = 0; iColumn < ColumnCount; iColumn++)
				{
					if (!Columns[iColumn].Visible)
						continue;

					DataGridViewControlCell cell = row.Cells[iColumn] as DataGridViewControlCell;
					if (cell.Control != null)
						cell.Control.Location = cell.CellContentDisplayRectangle.Location;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.SizeChanged"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			// Re-adjust the heights of the rows.
			ResizeRows(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.MouseUp"></see> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs"></see> that
		/// contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			HitTestInfo info = HitTest(e.X, e.Y);
			if (info.Type == DataGridViewHitTestType.Cell)
			{
				DataGridViewControlColumn column = Columns[info.ColumnIndex] as DataGridViewControlColumn;
				column.StandardWidth = column.Width;
				if (e.X < column.ThresholdWidth && e.X > DataGridViewControlColumn.kMinimumValue)
					e = new MouseEventArgs(e.Button, e.Clicks, column.ThresholdWidth, e.Y, e.Delta);
			}

			base.OnMouseUp(e);

			// Hide all the rows and columns that are to small now
			foreach (DataGridViewBand band in m_ColumnsAndRowsToHide)
				band.Visible = false;
			m_ColumnsAndRowsToHide.Clear();

			for (int iColumn = 0; iColumn < ColumnCount; iColumn++)
			{
				DataGridViewControlColumn col = Columns[iColumn] as DataGridViewControlColumn;

				// The current column can occupy a maximum percentage of the remaining width
				// (calculated as width from the left edge of the column to the right edge of
				// the grid) - except the last column which can occupy max percentage of the
				// entire width.
				Rectangle colRect = GetColumnDisplayRectangle(iColumn, false);
				int remainingWidth = (iColumn == ColumnCount - 1) ? Width : Width - colRect.Left;
				if (col.MaxPercentage > 0 && col.MaxPercentage < (float)col.Width / (float)remainingWidth)
					col.Width = (int)(col.MaxPercentage * remainingWidth);
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Previews a keyboard message.
		/// </summary>
		/// <param name="m">A <see cref="T:System.Windows.Forms.Message"></see>, passed by
		/// reference, that represents the window message to process.</param>
		/// <returns>Always false.</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool ProcessKeyPreview(ref Message m)
		{
			return false;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last visible column.
		/// </summary>
		/// <value>The last visible column.</value>
		/// ------------------------------------------------------------------------------------
		internal int LastVisibleColumn
		{
			get
			{
				CheckDisposed();
				for (int i = ColumnCount - 1; i >= 0; i--)
				{
					if (Columns[i].Visible)
						return i;
				}
				// if no column is visible we just take the last one.
				return ColumnCount - 1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last visible row.
		/// </summary>
		/// <value>The last visible row.</value>
		/// ------------------------------------------------------------------------------------
		internal int LastVisibleRow
		{
			get
			{
				CheckDisposed();
				for (int i = RowCount - 1; i >= 0; i--)
				{
					if (Rows[i].Visible)
						return i;
				}
				// if no row is visible we just take the last one.
				return RowCount - 1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last visible resizable row, or -1 if no visible row is resizable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int LastVisibleResizableRow
		{
			get
			{
				CheckDisposed();
				for (int i = RowCount - 1; i >= 0; i--)
				{
					DataGridViewControlRow row = (DataGridViewControlRow)Rows[i];
					if (row.Visible && (row.InternalResizable == DataGridViewTriState.True ||
						(row.InternalResizable == DataGridViewTriState.NotSet && AllowUserToResizeRows)))
						return i;
				}
				return -1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the width of the border.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int BorderWidth
		{
			get { return 1; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the width available for columns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int AvailableWidth
		{
			get { return ClientRectangle.Width - BorderWidth * (VisibleColumnCount - 1); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height available for rows.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int AvailableHeight
		{
			get { return ClientRectangle.Height - BorderWidth * (VisibleRowCount - 1); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the visible column count.
		/// </summary>
		/// <value>The visible column count.</value>
		/// ------------------------------------------------------------------------------------
		protected int VisibleColumnCount
		{
			get
			{
				CheckDisposed();
				int nVisibleCols = 0;
				for (int i = 0; i < ColumnCount; i++)
				{
					if (Columns[i].Visible)
						nVisibleCols++;
				}
				return nVisibleCols;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of visible rows.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected int VisibleRowCount
		{
			get
			{
				CheckDisposed();
				int nVisibleRows = 0;
				for (int i = 0; i < RowCount; i++)
				{
					if (Rows[i].Visible)
						nVisibleRows++;
				}
				return nVisibleRows;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether layout is suspended.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is layout suspended; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		internal bool IsLayoutSuspended
		{
			get
			{
				// Unfortunately Control.IsLayoutSuspended is internal, so we have to use
				// reflection to get to it.
				Type t = typeof(Control);
				return (bool)t.InvokeMember("IsLayoutSuspended", BindingFlags.DeclaredOnly
					| BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.Instance,
					null, this, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this instance is fully initialized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool IsFullyInitialized
		{
			get { return m_fFullyInitialized; }
			set { m_fFullyInitialized = value; }
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the heights of all rows to fit the contents of all their cells.
		/// </summary>
		/// <param name="fForceHeightChanged"><c>true</c> to fire a RowHeightChanged event even
		/// when nothing changed</param>
		/// ------------------------------------------------------------------------------------
		internal void ResizeRows(bool fForceHeightChanged)
		{
			// ClientRectangle is empty if we're minimized
			if (m_fResizeInProgress || VisibleRowCount == 0 || !m_fFullyInitialized ||
				ClientRectangle.IsEmpty)
			{
				return;
			}

			m_fResizeInProgress = true;
			try
			{
				float sumRowWeight = 0;
				int sumNoAutoFill = 0; // height of rows that have auto fill turned off
				foreach (DataGridViewControlRow row in Rows)
				{
					if (!row.Visible)
						continue;

					if (row.IsAutoFill)
						sumRowWeight += row.FillWeight;
					else
						sumNoAutoFill += row.Height;
				}

				int autoFillHeight = AvailableHeight - sumNoAutoFill;
				int sumFilled = 0;
				foreach (DataGridViewControlRow row in Rows)
				{
					if (!row.Visible)
						continue;

					if (row.IsAutoFill)
					{
						row.Height = (int)(autoFillHeight * row.FillWeight / sumRowWeight);
						sumFilled += row.Height;
					}
					if (fForceHeightChanged)
						base.OnRowHeightChanged(new DataGridViewRowEventArgs(row));
				}

				// last row gets extra pixels that are left over because of rounding
				int iLastRow = LastVisibleRow;
				if (iLastRow > -1 && !GetRowDisplayRectangle(iLastRow, true).IsEmpty)
					Rows[iLastRow].Height += autoFillHeight - sumFilled;
			}
			finally
			{
				m_fResizeInProgress = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the row weight based on the new height of the row.
		/// </summary>
		/// <param name="row">The row.</param>
		/// ------------------------------------------------------------------------------------
		private void AdjustRowWeight(DataGridViewControlRow row)
		{
			if (!row.IsAutoFill || VisibleRowCount == 1)
				return;

			// user changed the height of the row, so we have to adjust the weight of the cell.
			float rowPercentage = (float)row.Height / AvailableHeight;
			if (rowPercentage >= 1.0f)
			{
				// This row occupies all available space. Hide all other rows.
				foreach (DataGridViewControlRow otherRow in Rows)
				{
					if (otherRow != row)
					{
						otherRow.Height = 0;
						otherRow.Visible = false;
					}
				}
				return;
			}

			float otherRowWeights = 0;
			foreach (DataGridViewControlRow otherRow in Rows)
			{
				if (otherRow == row || !otherRow.IsAutoFill || !otherRow.Visible)
					continue;
				otherRowWeights += otherRow.FillWeight;
			}
			row.FillWeight = otherRowWeights * rowPercentage / (1 - rowPercentage);
		}
	}
}
