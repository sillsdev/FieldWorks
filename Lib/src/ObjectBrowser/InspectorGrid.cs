// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace SIL.ObjectBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InspectorGrid : DataGridView
	{
		private const int m_kLeftVLineMargin = 5;

		private int m_dxVLine;
		private Size m_szHotSpot;
		private Color m_clrGrid;
		private Color m_clrShading;
		private int m_firstRowInShadind = -1;
		private int m_lastRowInShading = -1;
		private IInspectorList m_list;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="InspectorGrid"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public InspectorGrid()
		{
			DoubleBuffered = true;
			AllowUserToAddRows = false;
			AllowUserToDeleteRows = false;
			MultiSelect = false;
			ReadOnly = false;
			SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			RowHeadersVisible = false;
			// TODO-Linux: VirtualMode is not supported on Mono.
			VirtualMode = true;
			AllowUserToResizeRows = false;
			Font = SystemFonts.MenuFont;
			DefaultCellStyle.ForeColor = SystemColors.WindowText;
			GridColor = DefaultCellStyle.BackColor;
			m_clrGrid = Color.FromArgb(40, ForeColor);
			m_clrShading = CalculateColor(DefaultCellStyle.ForeColor,
				DefaultCellStyle.BackColor, 25);

			m_list = new GenericInspectorObjectList();

			using (Image img = Properties.Resources.kimidExpand)
			{
				m_szHotSpot = new Size(img.Width, img.Height);
				m_dxVLine = (int)(m_szHotSpot.Width * 1.5);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="InspectorGrid"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InspectorGrid(IInspectorList list) : this()
		{
			List = list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the list of IInspectorObject objects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IInspectorList List
		{
			get { return m_list; }
			set
			{
				m_list = value;
				RowCount = 0;
				if (m_list != null)
					RowCount = m_list.Count;

				Invalidate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current inspector object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IInspectorObject CurrentObject
		{
			get
			{
				int irow = CurrentCellAddress.Y;
				return (irow >= 0 ? m_list[irow] : null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the color of the shading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Color ShadingColor
		{
			get { return m_clrShading; }
			set
			{
				m_clrShading = value;
				Invalidate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rows in block.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void GetRowsInBlock(int currRow, out int firstRowInBlock, out int lastRowInBlock)
		{
			int currLevel = m_list[currRow].Level;
			firstRowInBlock = lastRowInBlock = currRow;

			for (int i = firstRowInBlock + 1; i < RowCount && m_list[i].Level > currLevel; i++)
				lastRowInBlock = i;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rectangle inside of which is the shaded block of rows.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Rectangle GetBlockRectangle()
		{
			if (m_firstRowInShadind < 0 || m_lastRowInShading < 0 || m_firstRowInShadind == m_lastRowInShading)
				return Rectangle.Empty;

			int maxRow = RowCount - 1;
			Rectangle rc = ClientRectangle;
			Rectangle rcFirst = GetCellDisplayRectangle(0, Math.Min(maxRow, m_firstRowInShadind), false);
			Rectangle rcLast = GetCellDisplayRectangle(0, Math.Min(maxRow, m_lastRowInShading), false);

			if (rcFirst.Height > 0)
				rc.Y = rcFirst.Y;

			if (rcLast.Height == 0)
				rc.Height -= rc.Y;
			else
				rc.Height = (rcLast.Bottom - rc.Y);

			return rc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the row change.
		/// </summary>
		/// <param name="currRow">The curr row.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessRowChange(int currRow)
		{
			if (!m_list.IsExpanded(currRow))
			{
				int parentRow;
				m_list.GetParent(currRow, out parentRow);
				currRow = parentRow;
			}

			Rectangle rc;

			if (currRow < 0)
			{
				rc = GetBlockRectangle();
				if (rc != Rectangle.Empty)
					Invalidate(rc);

				m_firstRowInShadind = -1;
				m_lastRowInShading = -1;
				return;
			}

			int firstRowInBlock, lastRowInBlock;
			GetRowsInBlock(currRow, out firstRowInBlock, out lastRowInBlock);

			if (firstRowInBlock == m_firstRowInShadind && lastRowInBlock == m_lastRowInShading)
				return;

			rc = GetBlockRectangle();
			if (rc != Rectangle.Empty)
				Invalidate(rc);

			m_firstRowInShadind = -1;
			m_lastRowInShading = -1;

			if (firstRowInBlock >= 0 && lastRowInBlock > firstRowInBlock)
			{
				m_firstRowInShadind = firstRowInBlock;
				m_lastRowInShading = lastRowInBlock;
				rc = GetBlockRectangle();
				if (rc != Rectangle.Empty)
					Invalidate(rc);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.DataGridView.RowEnter"/> event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnRowEnter(DataGridViewCellEventArgs e)
		{
			base.OnRowEnter(e);
			ProcessRowChange(e.RowIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellPainting event of the dataGridView1 control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
		{
			base.OnCellPainting(e);

			if (e.Handled || e.RowIndex < 0 || e.ColumnIndex < 0 || m_list == null ||
				m_list.Count == 0 || m_list[e.RowIndex] == null)
			{
				return;
			}

			e.Handled = true;

			// Paint everything but the focus rectangle, foreground and background.
			// I'm not sure what's left, but just in case...
			DataGridViewPaintParts parts = e.PaintParts;
			parts &= ~DataGridViewPaintParts.Focus;
			parts &= ~DataGridViewPaintParts.Background;
			parts &= ~DataGridViewPaintParts.ContentForeground;
			e.Paint(e.CellBounds, parts);
			e.PaintBackground(e.CellBounds, false);

			IInspectorObject io = m_list[e.RowIndex];
			Rectangle rcText = e.CellBounds;
			Rectangle rcHotSpot = Rectangle.Empty;
			bool isSelected = ((e.State & DataGridViewElementStates.Selected) > 0);
			bool isInBlock = (!isSelected && e.RowIndex >= m_firstRowInShadind && e.RowIndex <= m_lastRowInShading);
			bool isIndentedCell = (e.ColumnIndex == 0);

			if (isIndentedCell)
			{
				// Calculate the location and size of the rectangle into which text will be drawn.
				// Adjust the text rectangle to account for the +/- image and the proper indent level.
				rcHotSpot = GetExpandCollapseRect(e.CellBounds, io.Level);
				int dx = ((rcHotSpot.Right - rcText.X) + 5);
				rcText.X += dx;
				rcText.Width -= dx;
			}

			// Draw the background color for the cell.
			using (SolidBrush br = new SolidBrush(DefaultCellStyle.BackColor))
			{
				if (isSelected)
					br.Color = DefaultCellStyle.SelectionBackColor;
				else if (isInBlock && m_clrShading != Color.Empty)
					br.Color = m_clrShading;

				e.Graphics.FillRectangle(br, rcText);
			}

			Color clrFore = (isSelected ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor);
			TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
				TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;

			TextRenderer.DrawText(e.Graphics, e.FormattedValue as string,
				e.CellStyle.Font, rcText, clrFore, flags);

			DrawBorders(e, isIndentedCell,
				(isInBlock && m_clrShading != Color.Empty ? m_clrShading : GridColor),
				(isInBlock ? rcText.X: rcHotSpot.X));

			if (!isIndentedCell)
				return;

			DrawTreeLines(e, rcHotSpot, io.Level);

			if (io.HasChildren)
			{
				// Draw the expand or collapse (+/-) image.
				e.Graphics.DrawImage(m_list.IsExpanded(e.RowIndex) ?
					Properties.Resources.kimidCollapse : Properties.Resources.kimidExpand, rcHotSpot);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the borders.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawBorders(DataGridViewCellPaintingEventArgs e, bool isIndentedCell,
			Color clrFadedBottomBorder, int dxBottomBorder)
		{
			if (e.RowIndex < 0)
				return;

			Point pt1;
			Point pt2;

			using (Pen pen = new Pen(m_clrGrid))
			{
				// Draw the cell's right border
				pt1 = new Point(e.CellBounds.Right - 1, e.CellBounds.Y);
				pt2 = new Point(e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
				e.Graphics.DrawLine(pen, pt1, pt2);

				if (e.ColumnIndex > 0)
				{
					// For cells other than those in the first column, draw the
					// cell's bottom border.
					pt1 = new Point(e.CellBounds.X, e.CellBounds.Bottom - 1);
					e.Graphics.DrawLine(pen, pt1, pt2);
				}
			}

			if (isIndentedCell)
			{
				// Draw the bottom border of the cell. It will fade away from right to left.
				pt1 = new Point(dxBottomBorder, e.CellBounds.Bottom - 1);
				pt2 = new Point(e.CellBounds.Right, pt1.Y);

				// Using a rectangle for the linear gradient brush will prevent an
				// strange artifact bug in the .Net code when the line is painted.
				Rectangle rc = new Rectangle(pt1.X, pt1.Y, pt2.X - pt1.X + 1, 1);
				using (LinearGradientBrush br = new LinearGradientBrush(rc, clrFadedBottomBorder,
					m_clrGrid, (float)0.0))
				using (Pen pen = new Pen(br))
					e.Graphics.DrawLine(pen, pt1, pt2);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the lines.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawTreeLines(DataGridViewCellPaintingEventArgs e, Rectangle rcHotSpot, int level)
		{

			using (Pen pen = new Pen(SystemColors.GrayText))
			{
				pen.DashStyle = DashStyle.Dot;

				// Draw horizontal line.
				Point pt1 = new Point(rcHotSpot.X + rcHotSpot.Width / 2, rcHotSpot.Y + rcHotSpot.Height / 2);
				Point pt2 = new Point(rcHotSpot.Right + 4, pt1.Y);
				e.Graphics.DrawLine(pen, pt2, pt1);

				// Draw the vertical line at the deepest level.
				pt2.X = pt1.X;
				pt2.Y++;
				pt1.Y = e.CellBounds.Top;
				if (!m_list.IsTerminus(e.RowIndex))
					pt2.Y = e.CellBounds.Bottom;

				e.Graphics.DrawLine(pen, pt1, pt2);

				// Draw the rest of the vertical lines going out to the shallowest level.
				pt2.Y = e.CellBounds.Bottom;
				for (int i = level - 1; i >= 0; i--)
				{
					pt1.X -= (int)(m_szHotSpot.Width * 1.5);
					pt2.X = pt1.X;

					if (m_list.HasFollowingUncleAtLevel(e.RowIndex, i))
						e.Graphics.DrawLine(pen, pt1, pt2);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellValueNeeded event of the gridInspector control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
		{
			e.Value = null;
			base.OnCellValueNeeded(e);

			if (e.Value != null || m_list == null || m_list.Count <= e.RowIndex)
				return;

			IInspectorObject io = m_list[e.RowIndex];
			if (io == null)
				e.Value = "null";
			else
			{
				if (e.ColumnIndex == 0)
					e.Value = io.DisplayName;
				else if (e.ColumnIndex == 1)
					e.Value = io.DisplayValue;
				else
					e.Value = io.DisplayType;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// In order to achieve double buffering without the problem that arises from having
		/// double buffering on while sizing rows and columns or dragging columns around,
		/// monitor when the mouse goes down and turn off double buffering when it goes down
		/// on a column heading or over the dividers between rows or the dividers between
		/// columns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
		{
			if (e.RowIndex == -1 || (e.ColumnIndex == -1 && Cursor == Cursors.SizeNS))
				DoubleBuffered = false;

			base.OnCellMouseDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When double buffering is off, it means it was turned off in the cell mouse down
		/// event. Therefore, turn it back on.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellMouseUp(DataGridViewCellMouseEventArgs e)
		{
			if (!DoubleBuffered)
				DoubleBuffered = true;

			base.OnCellMouseUp(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expand and collapse items when the right or left arrow keys are pressed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			int i = CurrentCellAddress.Y;

			if (i >= 0 && i < m_list.Count && m_list[i].HasChildren)
			{
				if ((e.KeyCode == Keys.Left && m_list.IsExpanded(i)) ||
					(e.KeyCode == Keys.Right && !m_list.IsExpanded(i)))
				{
					ToggleExpand(i);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.DataGridView.CellClick"/> event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellClick(DataGridViewCellEventArgs e)
		{
			base.OnCellClick(e);

			if (e.ColumnIndex != 0)
				return;

			IInspectorObject io = m_list[e.RowIndex];
			Rectangle rc = GetCellDisplayRectangle(0, e.RowIndex, true);
			Rectangle rcHotSpot = GetExpandCollapseRect(rc, io.Level);

			if (rcHotSpot.Contains(PointToClient(MousePosition)))
				ToggleExpand(e.RowIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggles the expansion state of the specified row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ToggleExpand(int irow)
		{
			if (m_list.ToggleObjectExpansion(irow))
			{
				// Adjust the number of rows in the grid after the expanding or collapsing.
				SuspendLayout();

				// It turns out removing all the rows first is a lot faster than
				// changing the row count when it's greater than zero.
				int i = FirstDisplayedScrollingRowIndex;
				RowCount = 0;
				RowCount = m_list.Count;

				// Make sure to restore the selected row.
				CurrentCell = this[0, irow];
				if (FirstDisplayedScrollingRowIndex != i)
					FirstDisplayedScrollingRowIndex = i;

				ResumeLayout();

				ProcessRowChange(irow);
			}

			Rectangle rc = GetCellDisplayRectangle(0, irow, true);
			int dy = ClientRectangle.Bottom - rc.Bottom;
			rc.Y = rc.Bottom;
			rc.Height = dy;
			rc.Width = ClientSize.Width;
			Invalidate(rc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the expand collapse rect.
		/// </summary>
		/// <param name="rcCell">The rc cell.</param>
		/// <param name="obj">The obj.</param>
		/// ------------------------------------------------------------------------------------
		private Rectangle GetExpandCollapseRect(Rectangle rcCell, int level)
		{
			Rectangle rc = rcCell;
			rc.X += (m_kLeftVLineMargin + (level * m_dxVLine));
			rc.Y += ((rc.Height - m_szHotSpot.Height) / 2);
			rc.Width = m_szHotSpot.Width;
			rc.Height = m_szHotSpot.Height;
			return rc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the default columns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CreateDefaultColumns()
		{
			DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
			col.HeaderText = "Object";
			col.Name = "colObject";
			col.ReadOnly = true;
			col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			col.FillWeight = 33;
			Columns.Add(col);

			col = new DataGridViewTextBoxColumn();
			col.HeaderText = "Value";
			col.Name = "colValue";
			if (ObjectBrowser.m_updateFlag == true)
				col.ReadOnly = false;
			col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			col.FillWeight = 33;
			Columns.Add(col);

			col = new DataGridViewTextBoxColumn();
			col.HeaderText = "Type";
			col.Name = "colType";
			col.ReadOnly = true;
			col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			col.FillWeight = 33;
			Columns.Add(col);
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates a color by applying the specified alpha value to the specified front
		/// color, assuming the color behind the front color is the specified back color. The
		/// returned color has the alpha channel set to completely opaque, but whose alpha
		/// channel value appears to be the one specified.
		/// </summary>
		///-------------------------------------------------------------------------------------
		public static Color CalculateColor(Color front, Color back, int alpha)
		{
			// Use alpha blending to brigthen the colors but don't use it
			// directly. Instead derive an opaque color that we can use.
			// -- if we use a color with alpha blending directly we won't be able
			// to paint over whatever color was in the background and there
			// would be shadows of that color showing through
			Color frontColor = Color.FromArgb(255, front);
			Color backColor = Color.FromArgb(255, back);

			float frontRed = frontColor.R;
			float frontGreen = frontColor.G;
			float frontBlue = frontColor.B;
			float backRed = backColor.R;
			float backGreen = backColor.G;
			float backBlue = backColor.B;

			float fRed = frontRed * alpha / 255 + backRed * ((float)(255 - alpha) / 255);
			byte newRed = (byte)fRed;
			float fGreen = frontGreen * alpha / 255 + backGreen * ((float)(255 - alpha) / 255);
			byte newGreen = (byte)fGreen;
			float fBlue = frontBlue * alpha / 255 + backBlue * ((float)(255 - alpha) / 255);
			byte newBlue = (byte)fBlue;

			return Color.FromArgb(255, newRed, newGreen, newBlue);
		}
	}
}
