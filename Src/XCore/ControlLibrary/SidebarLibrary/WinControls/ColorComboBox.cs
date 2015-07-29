// Original author or copyright holder unknown.

#if USE_THIS
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using SidebarLibrary.Win32;
using SidebarLibrary.General;
using SidebarLibrary.Menus;

namespace SidebarLibrary.WinControls
{
	/// <summary>
	/// Summary description for ColorComboBox.
	/// </summary>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(SidebarLibrary.WinControls.ColorComboBox),
	 "SidebarLibrary.WinControls.ColorComboBox.bmp")]
	public class ColorComboBox : ComboBoxBase
	{

		#region Class Variables
		private const int PREVIEW_BOX_WIDTH = 20;
		#endregion

		#region Constructos
		// For use when hosted by a toolbar
		public ColorComboBox(bool toolBarUse) : base(toolBarUse)
		{
			DropDownStyle = ComboBoxStyle.DropDownList;
			Items.AddRange(ColorUtil.KnownColorNames);
		}
		public ColorComboBox()
		{
			DropDownStyle = ComboBoxStyle.DropDownList;
			Items.AddRange(ColorUtil.KnownColorNames);
		}
		#endregion

		#region Overrides
		protected override void DrawComboBoxItem(Graphics g, Rectangle bounds, int Index, bool selected, bool editSel)
		{

			// Call base class to do the "Flat ComboBox" drawing
			base.DrawComboBoxItem(g, bounds, Index, selected, editSel);
			if (Index != -1)
			{
				string item = Items[Index].ToString();
				Color currentColor = Color.FromName(item);
				using (Brush brush = new SolidBrush(SystemColors.MenuText))
				{
					using (Brush currentColorBrush = new SolidBrush(currentColor))
						g.FillRectangle(currentColorBrush, bounds.Left + 2, bounds.Top + 2, PREVIEW_BOX_WIDTH, bounds.Height - 4);
					using (Pen blackPen = new Pen(Brushes.Black, 1))
						g.DrawRectangle(blackPen, new Rectangle(bounds.Left + 1, bounds.Top + 1, PREVIEW_BOX_WIDTH + 1, bounds.Height - 3));

					Size textSize = TextUtil.GetTextSize(g, Items[Index].ToString(), Font);
					int top = bounds.Top + (bounds.Height - textSize.Height) / 2;
					g.DrawString(item, Font, brush, new Point(bounds.Left + 28, top));
				}

			}
		}

		protected override void DrawComboBoxItemEx(Graphics g, Rectangle bounds, int Index, bool selected, bool editSel)
		{

			// This "hack" is necessary to avoid a clipping bug that comes from the fact that sometimes
			// we are drawing using the Graphics object for the edit control in the combobox and sometimes
			// we are using the graphics object for the combobox itself. If we use the same function to do our custom
			// drawing it is hard to adjust for the clipping because of this situation
			base.DrawComboBoxItemEx(g, bounds, Index, selected, editSel);
			if (Index != -1)
			{
				string item = Items[Index].ToString();
				Color currentColor = Color.FromName(item);
				using (SolidBrush brush = new SolidBrush(SystemColors.MenuText))
				{
					Rectangle rc = bounds;
					rc.Inflate(-3, -3);
					using (Brush currentColorBrush = new SolidBrush(currentColor))
						g.FillRectangle(currentColorBrush, rc.Left + 2, rc.Top + 2, PREVIEW_BOX_WIDTH, rc.Height - 4);
					using (Pen blackPen = new Pen(Brushes.Black, 1))
						g.DrawRectangle(blackPen, new Rectangle(rc.Left + 1, rc.Top + 1, PREVIEW_BOX_WIDTH + 1, rc.Height - 3));

					Size textSize = TextUtil.GetTextSize(g, Items[Index].ToString(), Font);
					int top = bounds.Top + (bounds.Height - textSize.Height) / 2;

					// Clipping rectangle
					Rectangle clipRect = new Rectangle(bounds.Left + 31, top, bounds.Width - 31 - ARROW_WIDTH - 4, top + textSize.Height);
					g.DrawString(Items[Index].ToString(), Font, brush, clipRect);
				}
			}
		}

		protected override void DrawDisableState()
		{
			// Draw the combobox state disable
			base.DrawDisableState();

			// Draw the specific disable state to
			// this derive class
			using ( Graphics g = CreateGraphics() )
			{
				using ( Brush b = new SolidBrush(SystemColors.ControlDark) )
				{
					Rectangle rc = ClientRectangle;
					Rectangle bounds = new Rectangle(rc.Left, rc.Top, rc.Width, rc.Height);
					bounds.Inflate(-3, -3);
					g.DrawRectangle(SystemPens.ControlDark, new Rectangle(bounds.Left+2,
						bounds.Top+2, PREVIEW_BOX_WIDTH, bounds.Height-4));

					int index = SelectedIndex;
					Size textSize = TextUtil.GetTextSize(g, Items[index].ToString(), Font);

					// Clipping rectangle
					int top = rc.Top + (rc.Height - textSize.Height)/2;
					Rectangle clipRect = new Rectangle(rc.Left + 31,
						top, rc.Width - 31 - ARROW_WIDTH - 4, top+textSize.Height);
					g.DrawString(Items[index].ToString(), Font, b, clipRect);

				}
			}
		}
		#endregion

		#region Methods
		public void PassMsg(ref Message m)
		{
			base.WndProc(ref m);
		}
		#endregion

	}


}
#endif
