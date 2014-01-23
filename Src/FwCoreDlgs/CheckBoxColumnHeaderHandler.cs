// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CheckBoxColumnHeader.cs
// Responsibility: Olson
//
// <remarks>
// The original version of this code was written for the SayMore project.  It didn't display a
// label, centering the checkbox in the header instead.  It also treated clicking anywhere in
// the header as clicking in the checkbox.
// </remarks>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class draws a checkbox in a column header and lets the user click/unclick the
	/// check box, firing an event when they do so. IMPORTANT: This class must be instantiated
	/// after the column has been added to a DataGridView control.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CheckBoxColumnHeaderHandler : IDisposable
	{
		/// <summary></summary>
		public delegate bool CheckChangeHandler(CheckBoxColumnHeaderHandler sender,
			CheckState oldState);

		/// <summary></summary>
		public event CheckChangeHandler CheckChanged;

		private DataGridViewColumn m_col;
		private DataGridView m_grid;
		private Size m_szCheckBox = Size.Empty;
		private CheckState m_state = CheckState.Checked;
		private StringFormat m_stringFormat;

		/// <summary>
		/// Get/set the label to be used in addition to the checkbox.
		/// </summary>
		public string Label { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckBoxColumnHeaderHandler(DataGridViewColumn col)
		{
			Debug.Assert(col != null);
			Debug.Assert(col is DataGridViewCheckBoxColumn);
			Debug.Assert(col.DataGridView != null);

			m_col = col;
			m_grid = col.DataGridView;
			m_grid.HandleDestroyed += HandleHandleDestroyed;
			m_grid.CellPainting += HandleHeaderCellPainting;
			m_grid.CellMouseMove += HandleHeaderCellMouseMove;
			m_grid.ColumnHeaderMouseClick += HandleHeaderCellMouseClick;
			m_grid.CellContentClick += HandleDataCellCellContentClick;
			m_grid.Scroll += HandleGridScroll;
			m_grid.RowsAdded += HandleGridRowsAdded;
			m_grid.RowsRemoved += HandleGridRowsRemoved;

			if (!Application.RenderWithVisualStyles)
			{
				m_szCheckBox = new Size(13, 13);
			}
			else
			{
				var element = VisualStyleElement.Button.CheckBox.CheckedNormal;
				var renderer = new VisualStyleRenderer(element);
				using (var g = m_grid.CreateGraphics())
					m_szCheckBox = renderer.GetPartSize(g, ThemeSizeType.True);
			}

			m_stringFormat = new StringFormat(StringFormat.GenericTypographic);
			m_stringFormat.Alignment = StringAlignment.Center;
			m_stringFormat.LineAlignment = StringAlignment.Center;
			m_stringFormat.Trimming = StringTrimming.EllipsisCharacter;
			m_stringFormat.FormatFlags |= StringFormatFlags.NoWrap;
		}

#if DEBUG
		/// <summary>Finalizer</summary>
		~CheckBoxColumnHeaderHandler()
		{
			Dispose(false);
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// StringFormat implements IDisposable, so we better do the same.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// In addition to disposing m_stringFormat, we should also clear out all the event
		/// handlers  we added to m_grid in the constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (!IsDisposed)
			{
				if (fDisposing)
				{
					if (m_stringFormat != null)
						m_stringFormat.Dispose();
					if (m_grid != null && !m_grid.IsDisposed)
					{
						m_grid.HandleDestroyed -= HandleHandleDestroyed;
						m_grid.CellPainting -= HandleHeaderCellPainting;
						m_grid.CellMouseMove -= HandleHeaderCellMouseMove;
						m_grid.ColumnHeaderMouseClick -= HandleHeaderCellMouseClick;
						m_grid.CellContentClick -= HandleDataCellCellContentClick;
						m_grid.Scroll -= HandleGridScroll;
						m_grid.RowsAdded -= HandleGridRowsAdded;
						m_grid.RowsRemoved -= HandleGridRowsRemoved;
					}
				}
				Label = null;
				m_stringFormat = null;
				m_grid = null;
				m_col = null;
				IsDisposed = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the state of the column header's check box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckState HeadersCheckState
		{
			get
			{
				CheckDisposed();
				return m_state;
			}
			set
			{
				CheckDisposed();
				m_state = value;
				m_grid.InvalidateCell(m_col.HeaderCell);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		private void HandleHandleDestroyed(object sender, EventArgs e)
		{
			m_grid.HandleDestroyed -= HandleHandleDestroyed;
			m_grid.CellPainting -= HandleHeaderCellPainting;
			m_grid.CellMouseMove -= HandleHeaderCellMouseMove;
			m_grid.ColumnHeaderMouseClick -= HandleHeaderCellMouseClick;
			m_grid.CellContentClick -= HandleDataCellCellContentClick;
			m_grid.Scroll -= HandleGridScroll;
			m_grid.RowsAdded -= HandleGridRowsAdded;
			m_grid.RowsRemoved -= HandleGridRowsRemoved;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		private void HandleGridRowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
		{
			UpdateHeadersCheckStateFromColumnsValues();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		private void HandleGridRowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
		{
			UpdateHeadersCheckStateFromColumnsValues();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		private void HandleGridScroll(object sender, ScrollEventArgs e)
		{
			if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
			{
				var rc = m_grid.ClientRectangle;
				rc.Height = m_grid.ColumnHeadersHeight;
				m_grid.Invalidate(rc);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateHeadersCheckStateFromColumnsValues()
		{
			bool foundOneChecked = false;
			bool foundOneUnChecked = false;

			foreach (DataGridViewRow row in m_grid.Rows)
			{
				object cellValue = row.Cells[m_col.Index].Value;
				if (!(cellValue is bool))
					continue;

				bool chked = (bool)cellValue;
				if (!foundOneChecked && chked)
					foundOneChecked = true;
				else if (!foundOneUnChecked && !chked)
					foundOneUnChecked = true;

				if (foundOneChecked && foundOneUnChecked)
				{
					HeadersCheckState = CheckState.Indeterminate;
					return;
				}
			}

			HeadersCheckState = (foundOneChecked ? CheckState.Checked : CheckState.Unchecked);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateColumnsDataValuesFromHeadersCheckState()
		{
			foreach (DataGridViewRow row in m_grid.Rows)
			{
				if (row.Cells[m_col.Index] == m_grid.CurrentCell && m_grid.IsCurrentCellInEditMode)
					m_grid.EndEdit();

				row.Cells[m_col.Index].Value = (m_state == CheckState.Checked);
			}
		}

		#region Mouse move and click handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles toggling the selected state of an item in the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleDataCellCellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0 && e.ColumnIndex == m_col.Index)
			{
				bool currCellValue = (bool)m_grid[e.ColumnIndex, e.RowIndex].Value;
				m_grid[e.ColumnIndex, e.RowIndex].Value = !currCellValue;
				UpdateHeadersCheckStateFromColumnsValues();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		private void HandleHeaderCellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.RowIndex >= 0 || e.ColumnIndex != m_col.Index)
				return;
			if (!IsClickInCheckBox(e))
				return;

			CheckState oldState = HeadersCheckState;

			if (HeadersCheckState == CheckState.Checked)
				HeadersCheckState = CheckState.Unchecked;
			else
				HeadersCheckState = CheckState.Checked;
			m_grid.InvalidateCell(m_col.HeaderCell);
			bool updateValues = true;
			if (CheckChanged != null)
				updateValues = CheckChanged(this, oldState);

			if (updateValues)
				UpdateColumnsDataValuesFromHeadersCheckState();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		private void HandleHeaderCellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.ColumnIndex == m_col.Index && e.RowIndex < 0)
				m_grid.InvalidateCell(m_col.HeaderCell);
		}

		#endregion

		#region Painting methods
		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		private void HandleHeaderCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			if (e.RowIndex >= 0 || e.ColumnIndex != m_col.Index)
				return;
			var rcCell = HeaderRectangle;
			if (rcCell.IsEmpty)
				return;
			var rcBox = GetCheckBoxRectangle(rcCell);
			if (Application.RenderWithVisualStyles)
			{
				DrawVisualStyleCheckBox(e.Graphics, rcBox);
			}
			else
			{
				var state = ButtonState.Checked;
				if (HeadersCheckState == CheckState.Unchecked)
					state = ButtonState.Normal;
				else if (HeadersCheckState == CheckState.Indeterminate)
					state |= ButtonState.Inactive;
				ControlPaint.DrawCheckBox(e.Graphics, rcBox, state | ButtonState.Flat);
			}
			if (String.IsNullOrEmpty(Label))
				return;

			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			using (var brush = new SolidBrush(m_grid.ForeColor))
			{
				var sz = e.Graphics.MeasureString(Label, m_grid.Font, new Point(0, 0), m_stringFormat).ToSize();
				var dy2 = (int)Math.Floor((rcCell.Height - sz.Height) / 2f);
				if (dy2 < 0)
					dy2 = 0;
				var rcText = new Rectangle(rcBox.X + rcBox.Width + 3, rcCell.Y + dy2,
					rcCell.Width - (rcBox.Width + 6), Math.Min(sz.Height, rcCell.Height));

				e.Graphics.DrawString(Label, m_grid.Font, brush, rcText, m_stringFormat);
			}
		}

		private Rectangle HeaderRectangle
		{
			get
			{
				var rcCell = m_grid.GetCellDisplayRectangle(m_col.Index, -1, false);
				if (rcCell.IsEmpty)
					return rcCell;

				// At this point, we know at least part of the header cell is visible, therefore,
				// force the rectangle's width to that of the column's.
				rcCell.X = rcCell.Right - m_col.Width;

				// Subtract one so as not to include the left border in the width.
				rcCell.Width = m_col.Width - 1;
				return rcCell;
			}
		}

		private Rectangle GetCheckBoxRectangle(Rectangle rcCell)
		{
			var dx = 3;
			if (String.IsNullOrEmpty(Label))
				dx = (int)Math.Floor((rcCell.Width - m_szCheckBox.Width) / 2f);
			var dy = (int)Math.Floor((rcCell.Height - m_szCheckBox.Height) / 2f);
			return new Rectangle(rcCell.X + dx, rcCell.Y + dy, m_szCheckBox.Width, m_szCheckBox.Height);
		}

		/// ------------------------------------------------------------------------------------
		///<summary>
		/// Check whether this mouse click was inside our checkbox display rectangle.
		///</summary>
		/// ------------------------------------------------------------------------------------
		public bool IsClickInCheckBox(DataGridViewCellMouseEventArgs e)
		{
			CheckDisposed();
			if (e.ColumnIndex != m_col.Index || e.RowIndex >= 0)
				return false;
			var rcCell = HeaderRectangle;
			if (rcCell.IsEmpty)
				return false;
			var rcBox = GetCheckBoxRectangle(rcCell);
			var minX = rcBox.X - rcCell.X;
			var maxX = minX + rcBox.Width;
			var minY = rcBox.Y - rcCell.Y;
			var maxY = minY + rcBox.Height;
			return (e.X >= minX && e.X < maxX && e.Y >= minY && e.Y < maxY);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		private void DrawVisualStyleCheckBox(IDeviceContext g, Rectangle rcBox)
		{
			var isHot = rcBox.Contains(m_grid.PointToClient(Control.MousePosition));
			var element = VisualStyleElement.Button.CheckBox.CheckedNormal;

			if (HeadersCheckState == CheckState.Unchecked)
			{
				element = (isHot ? VisualStyleElement.Button.CheckBox.UncheckedHot :
					VisualStyleElement.Button.CheckBox.UncheckedNormal);
			}
			else if (HeadersCheckState == CheckState.Indeterminate)
			{
				element = (isHot ? VisualStyleElement.Button.CheckBox.MixedHot :
					VisualStyleElement.Button.CheckBox.MixedNormal);
			}
			else if (isHot)
				element = VisualStyleElement.Button.CheckBox.CheckedHot;

			var renderer = new VisualStyleRenderer(element);
			renderer.DrawBackground(g, rcBox);
		}

		#endregion
	}
}
