// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Drawing.Design;

using SidebarLibrary.General;
using System.Diagnostics.CodeAnalysis;

namespace SidebarLibrary.WinControls
{
	/// <summary>
	/// Summary description for ColorListBox.
	/// </summary>
	[ToolboxBitmap(typeof(SidebarLibrary.WinControls.ColorListBox),
		 "SidebarLibrary.WinControls.ColorListBox.bmp")]
	public class ColorListBox : System.Windows.Forms.ListBox
	{

		#region Class variables
		string[] colorArray = null;
		#endregion

		#region Constructors
		public ColorListBox()
		{
			DrawMode = DrawMode.OwnerDrawFixed;
			ItemHeight = ItemHeight + 1;
		}
		#endregion

		#region Overrides
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "g is a reference")]
		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			Graphics g = e.Graphics;
			Rectangle bounds = e.Bounds;
			bool selected = (e.State & DrawItemState.Selected) > 0;
			bool editSel = (e.State & DrawItemState.ComboBoxEdit ) > 0;
			if ( e.Index != -1 )
				DrawListBoxItem(g, bounds, e.Index, selected, editSel);

		}
		#endregion

		#region Properties
		public  new ListBox.ObjectCollection Items
		{
			get{ return base.Items; }
		}

		public String[] ColorArray
		{
			get
			{
				return colorArray;
			}
			set
			{
				colorArray = value;
				if ( colorArray != null )
					Items.AddRange(value);
			}
		}
		#endregion

		#region Methods
		public void PassMsg(ref Message m)
		{
			base.WndProc(ref m);
		}
		#endregion

		#region Implementation
		protected void DrawListBoxItem(Graphics g, Rectangle bounds, int Index, bool selected, bool editSel)
		{
			// Draw List box item
			if ( Index != -1)
			{
				if ( selected )
				{
					// Draw highlight rectangle
					using ( Brush b = new SolidBrush(ColorUtil.VSNetSelectionColor) )
					{
						g.FillRectangle(b, bounds.Left, bounds.Top, bounds.Width, bounds.Height);
					}
					using ( Pen p = new Pen(ColorUtil.VSNetBorderColor) )
					{
						g.DrawRectangle(p, bounds.Left, bounds.Top, bounds.Width-1, bounds.Height-1);
					}
				}
				else
				{
					// Erase highlight rectangle
					g.FillRectangle(SystemBrushes.Window, bounds.Left, bounds.Top, bounds.Width, bounds.Height);
				}

				string item = (string)Items[Index];
				Color currentColor = Color.FromName(item);

				using ( Brush b = new SolidBrush(currentColor) )
				{
					g.FillRectangle(new SolidBrush(currentColor), bounds.Left+2, bounds.Top+2, 20, bounds.Height-4);
				}
				g.DrawRectangle(Pens.Black, new Rectangle(bounds.Left+1, bounds.Top+1, 21, bounds.Height-3));
				g.DrawString(item, SystemInformation.MenuFont, SystemBrushes.ControlText, new Point(bounds.Left + 28, bounds.Top));

			}
		}
		#endregion

	}
}
