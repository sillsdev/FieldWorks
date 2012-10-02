using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using SIL.Utils;
using System.Windows.Forms.VisualStyles;

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

			//BorderStyle = (Application.VisualStyleState == VisualStyleState.NoneEnabled ?
			//    BorderStyle.Fixed3D : BorderStyle.FixedSingle);
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

		///// ------------------------------------------------------------------------------------
		///// <summary>
		/////
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//public new BorderStyle BorderStyle
		//{
		//    get { return base.BorderStyle; }
		//    set
		//    {
		//        base.BorderStyle = value;

		//        m_overrideBorderDrawing = (value == BorderStyle.FixedSingle &&
		//            (Application.VisualStyleState == VisualStyleState.NonClientAreaEnabled ||
		//            Application.VisualStyleState == VisualStyleState.ClientAndNonClientAreasEnabled));
		//    }
		//}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		/////
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//public bool DrawVisualStyleBorder
		//{
		//    get { return m_overrideBorderDrawing; }
		//    set { m_overrideBorderDrawing = value; }
		//}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// After the panel has been resized, force the border to be repainted. I found that
		///// often, after resizing the panel at runtime (e.g. when it's docked inside a
		///// splitter panel and the splitter moved), the portion of the border that was newly
		///// repainted didn't show the overriden border color handled by the WndProc above.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//protected override void OnClientSizeChanged(EventArgs e)
		//{
		//    base.OnClientSizeChanged(e);

		//    if (m_overrideBorderDrawing)
		//        Utils.Win32.SendMessage(Handle, PaintingHelper.WM_NCPAINT, 1, 0);
		//}

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
					ReflectionHelper.SetField(e, "paintParts", parts);
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

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Catch the non client area paint message so we can paint a border around the
		///// explorer bar that isn't black.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//protected override void WndProc(ref Message m)
		//{
		//    base.WndProc(ref m);

		//    if (m.Msg == PaintingHelper.WM_NCPAINT && m_overrideBorderDrawing)
		//    {
		//        PaintingHelper.DrawCustomBorder(this);
		//        m.Result = IntPtr.Zero;
		//        m.Msg = 0;
		//    }
		//}
	}
}
