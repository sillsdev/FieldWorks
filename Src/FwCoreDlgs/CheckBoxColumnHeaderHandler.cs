// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// This class draws a checkbox in a column header and lets the user check/uncheck the
	/// check box, firing an event when they do so. IMPORTANT: This class must be instantiated
	/// after the column has been added to a DataGridView control.
	/// </summary>
	public sealed class CheckBoxColumnHeaderHandler : IDisposable
	{
		/// <summary />
		public delegate bool CheckChangeHandler(CheckBoxColumnHeaderHandler sender, CheckState oldState);

		/// <summary />
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

		/// <summary />
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
				{
					m_szCheckBox = renderer.GetPartSize(g, ThemeSizeType.True);
				}
			}

			m_stringFormat = new StringFormat(StringFormat.GenericTypographic)
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center,
				Trimming = StringTrimming.EllipsisCharacter
			};
			m_stringFormat.FormatFlags |= StringFormatFlags.NoWrap;
		}

		/// <summary />
		~CheckBoxColumnHeaderHandler()
		{
			Dispose(false);
		}

		/// <summary />
		private bool IsDisposed { get; set; }

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// In addition to disposing m_stringFormat, we should also clear out all the event
		/// handlers  we added to m_grid in the constructor.
		/// </summary>
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				m_stringFormat?.Dispose();
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

		/// <summary>
		/// Gets or sets the state of the column header's check box.
		/// </summary>
		public CheckState HeadersCheckState
		{
			get
			{
				return m_state;
			}
			set
			{
				m_state = value;
				m_grid.InvalidateCell(m_col.HeaderCell);
			}
		}

		/// <summary />
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

		/// <summary />
		private void HandleGridRowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
		{
			UpdateHeadersCheckStateFromColumnsValues();
		}

		/// <summary />
		private void HandleGridRowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
		{
			UpdateHeadersCheckStateFromColumnsValues();
		}

		/// <summary />
		private void HandleGridScroll(object sender, ScrollEventArgs e)
		{
			if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
			{
				var rc = m_grid.ClientRectangle;
				rc.Height = m_grid.ColumnHeadersHeight;
				m_grid.Invalidate(rc);
			}
		}

		/// <summary />
		private void UpdateHeadersCheckStateFromColumnsValues()
		{
			var foundOneChecked = false;
			var foundOneUnChecked = false;
			foreach (DataGridViewRow row in m_grid.Rows)
			{
				var cellValue = row.Cells[m_col.Index].Value;
				if (!(cellValue is bool))
				{
					continue;
				}
				var chked = (bool)cellValue;
				if (!foundOneChecked && chked)
				{
					foundOneChecked = true;
				}
				else if (!foundOneUnChecked && !chked)
				{
					foundOneUnChecked = true;
				}
				if (foundOneChecked && foundOneUnChecked)
				{
					HeadersCheckState = CheckState.Indeterminate;
					return;
				}
			}

			HeadersCheckState = foundOneChecked ? CheckState.Checked : CheckState.Unchecked;
		}

		/// <summary />
		private void UpdateColumnsDataValuesFromHeadersCheckState()
		{
			foreach (DataGridViewRow row in m_grid.Rows)
			{
				if (row.Cells[m_col.Index] == m_grid.CurrentCell && m_grid.IsCurrentCellInEditMode)
				{
					m_grid.EndEdit();
				}
				row.Cells[m_col.Index].Value = (m_state == CheckState.Checked);
			}
		}

		#region Mouse move and click handlers

		/// <summary>
		/// Handles toggling the selected state of an item in the list.
		/// </summary>
		private void HandleDataCellCellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0 && e.ColumnIndex == m_col.Index)
			{
				var currCellValue = (bool)m_grid[e.ColumnIndex, e.RowIndex].Value;
				m_grid[e.ColumnIndex, e.RowIndex].Value = !currCellValue;
				UpdateHeadersCheckStateFromColumnsValues();
			}
		}

		/// <summary />
		private void HandleHeaderCellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.RowIndex >= 0 || e.ColumnIndex != m_col.Index)
			{
				return;
			}
			if (!IsClickInCheckBox(e))
			{
				return;
			}
			var oldState = HeadersCheckState;
			if (HeadersCheckState == CheckState.Checked)
			{
				HeadersCheckState = CheckState.Unchecked;
			}
			else
			{
				HeadersCheckState = CheckState.Checked;
			}
			m_grid.InvalidateCell(m_col.HeaderCell);
			var updateValues = true;
			if (CheckChanged != null)
			{
				updateValues = CheckChanged(this, oldState);
			}
			if (updateValues)
			{
				UpdateColumnsDataValuesFromHeadersCheckState();
			}
		}

		/// <summary />
		private void HandleHeaderCellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.ColumnIndex == m_col.Index && e.RowIndex < 0)
			{
				m_grid.InvalidateCell(m_col.HeaderCell);
			}
		}

		#endregion

		#region Painting methods

		/// <summary />
		private void HandleHeaderCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			if (e.RowIndex >= 0 || e.ColumnIndex != m_col.Index)
			{
				return;
			}
			var rcCell = HeaderRectangle;
			if (rcCell.IsEmpty)
			{
				return;
			}
			var rcBox = GetCheckBoxRectangle(rcCell);
			if (Application.RenderWithVisualStyles)
			{
				DrawVisualStyleCheckBox(e.Graphics, rcBox);
			}
			else
			{
				var state = ButtonState.Checked;
				if (HeadersCheckState == CheckState.Unchecked)
				{
					state = ButtonState.Normal;
				}
				else if (HeadersCheckState == CheckState.Indeterminate)
				{
					state |= ButtonState.Inactive;
				}
				ControlPaint.DrawCheckBox(e.Graphics, rcBox, state | ButtonState.Flat);
			}
			if (string.IsNullOrEmpty(Label))
			{
				return;
			}
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			using (var brush = new SolidBrush(m_grid.ForeColor))
			{
				var sz = e.Graphics.MeasureString(Label, m_grid.Font, new Point(0, 0), m_stringFormat).ToSize();
				var dy2 = (int)Math.Floor((rcCell.Height - sz.Height) / 2f);
				if (dy2 < 0)
				{
					dy2 = 0;
				}
				var rcText = new Rectangle(rcBox.X + rcBox.Width + 3, rcCell.Y + dy2, rcCell.Width - (rcBox.Width + 6), Math.Min(sz.Height, rcCell.Height));
				e.Graphics.DrawString(Label, m_grid.Font, brush, rcText, m_stringFormat);
			}
		}

		private Rectangle HeaderRectangle
		{
			get
			{
				var rcCell = m_grid.GetCellDisplayRectangle(m_col.Index, -1, false);
				if (rcCell.IsEmpty)
				{
					return rcCell;
				}
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
			if (string.IsNullOrEmpty(Label))
			{
				dx = (int)Math.Floor((rcCell.Width - m_szCheckBox.Width) / 2f);
			}
			var dy = (int)Math.Floor((rcCell.Height - m_szCheckBox.Height) / 2f);
			return new Rectangle(rcCell.X + dx, rcCell.Y + dy, m_szCheckBox.Width, m_szCheckBox.Height);
		}

		///<summary>
		/// Check whether this mouse click was inside our checkbox display rectangle.
		///</summary>
		public bool IsClickInCheckBox(DataGridViewCellMouseEventArgs e)
		{
			if (e.ColumnIndex != m_col.Index || e.RowIndex >= 0)
			{
				return false;
			}
			var rcCell = HeaderRectangle;
			if (rcCell.IsEmpty)
			{
				return false;
			}
			var rcBox = GetCheckBoxRectangle(rcCell);
			var minX = rcBox.X - rcCell.X;
			var maxX = minX + rcBox.Width;
			var minY = rcBox.Y - rcCell.Y;
			var maxY = minY + rcBox.Height;
			return e.X >= minX && e.X < maxX && e.Y >= minY && e.Y < maxY;
		}

		/// <summary />
		private void DrawVisualStyleCheckBox(IDeviceContext g, Rectangle rcBox)
		{
			var isHot = rcBox.Contains(m_grid.PointToClient(Control.MousePosition));
			var element = VisualStyleElement.Button.CheckBox.CheckedNormal;
			switch (HeadersCheckState)
			{
				case CheckState.Unchecked:
					element = isHot ? VisualStyleElement.Button.CheckBox.UncheckedHot : VisualStyleElement.Button.CheckBox.UncheckedNormal;
					break;
				case CheckState.Indeterminate:
					element = isHot ? VisualStyleElement.Button.CheckBox.MixedHot : VisualStyleElement.Button.CheckBox.MixedNormal;
					break;
				default:
				{
					if (isHot)
					{
						element = VisualStyleElement.Button.CheckBox.CheckedHot;
					}
					break;
				}
			}

			var renderer = new VisualStyleRenderer(element);
			renderer.DrawBackground(g, rcBox);
		}

		#endregion
	}
}