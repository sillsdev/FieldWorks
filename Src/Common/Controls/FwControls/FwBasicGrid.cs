// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using SIL.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides an implementation of the DataGridView that uses a different color scheme for
	/// selected rows and cells from the default.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwBasicGrid : DataGridView
	{
		//private bool m_overrideBorderDrawing = true;
		private bool m_drawSelectedCellFocusRect = false;
		private Color m_selCellBackColor;
		private Color m_selRowBackColor;
		private Color m_selCellForeColor;
		private Color m_selRowForeColor;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwBasicGrid"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwBasicGrid()
		{
			m_selCellBackColor =
				ColorHelper.CalculateColor(SystemColors.Window, SystemColors.Highlight, 200);

			m_selRowBackColor =
				ColorHelper.CalculateColor(SystemColors.Window, SystemColors.Highlight, 150);

			m_selCellForeColor = SystemColors.WindowText;
			m_selRowForeColor = SystemColors.WindowText;

			DefaultCellStyle.SelectionForeColor = m_selRowForeColor;
			DefaultCellStyle.SelectionBackColor = m_selRowBackColor;
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to draw a focus rectangle around
		/// the selected cell.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DrawSelectedCellFocusRect
		{
			get { return m_drawSelectedCellFocusRect; }
			set
			{
				m_drawSelectedCellFocusRect = value;
				Invalidate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get rid of focus rectangle if applicable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
		{
			bool doPaint = false;

			if (e.ColumnIndex == CurrentCellAddress.X && e.RowIndex == CurrentCellAddress.Y)
			{
				e.CellStyle.SelectionBackColor = m_selCellBackColor;
				e.CellStyle.SelectionForeColor = m_selCellForeColor;

				if (!m_drawSelectedCellFocusRect)
				{
					DataGridViewPaintParts parts = e.PaintParts;
					parts &= ~DataGridViewPaintParts.Focus;

					// Grr. PaintParts is readonly. So I have to brute force it.
					LCModel.Utils.ReflectionHelper.SetField(e, "paintParts", parts);
					doPaint = true;
				}
			}

			base.OnCellPainting(e);

			if (!e.Handled && doPaint)
			{
				e.Paint(e.ClipBounds, e.PaintParts);
				e.Handled = true;
			}
		}
	}
}
