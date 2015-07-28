// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;

using SidebarLibrary.General;
using SidebarLibrary.Win32;

namespace SidebarLibrary.WinControls
{
	/// <summary>
	/// Summary description for TextComboBox.
	/// </summary>
	[ToolboxItem(false)]
	public class TextComboBox : ComboBoxBase
	{

		#region Constructors
		// For use when hosted by a toolbar
		public TextComboBox(bool toolBarUse) : base(toolBarUse)
		{
			// Override parent, we don't want to do all the painting ourselves
			// since we want to let the edit control deal with the text for editing
			// the parent class ComboBoxBase knows to do the right stuff with
			// non-editable comboboxes as well as editable comboboxes as long
			// as we change these flags below
			SetStyle(ControlStyles.AllPaintingInWmPaint
				|ControlStyles.UserPaint|ControlStyles.Opaque, false);
		}

		public TextComboBox()
		{
			// Override parent, we don't want to do all the painting ourselves
			// since we want to let the edit control deal with the text for editing
			// the parent class ComboBoxBase knows to do the right stuff with
			// non-editable comboboxes as well as editable comboboxes as long
			// as we change these flags below
			SetStyle(ControlStyles.AllPaintingInWmPaint
				|ControlStyles.UserPaint|ControlStyles.Opaque, false);
		}

		#endregion

		#region Overrides
		protected override void DrawComboBoxItem(Graphics g, Rectangle bounds, int Index, bool selected, bool editSel)
		{
			// Call base class to do the "Flat ComboBox" drawing
			base.DrawComboBoxItem(g, bounds, Index, selected, editSel);
			if (Index != -1)
			{
				using (SolidBrush brush = new SolidBrush(SystemColors.MenuText))
				{
					Size textSize = TextUtil.GetTextSize(g, Items[Index].ToString(), Font);
					int top = bounds.Top + (bounds.Height - textSize.Height) / 2;

					// Check if the combobox is bound to a Data Source
					if (DataSource != null)
						DrawDataBoundMember(g, brush, Index, new Point(bounds.Left + 1, top));
					else
						g.DrawString(Items[Index].ToString(), Font, brush, new Point(bounds.Left + 1, top));
				}
			}
		}

		protected override void DrawComboBoxItemEx(Graphics g, Rectangle bounds, int Index, bool selected, bool editSel)
		{
			// This "hack" is necessary to avoid a clipping bug that comes from the fact that sometimes
			// we are drawing using the Graphics object for the edit control in the combobox and sometimes
			// we are using the graphics object for the combobox itself. If we use the same function to do our custom
			// drawing it is hard to adjust for the clipping because of what was said about
			base.DrawComboBoxItemEx(g, bounds, Index, selected, editSel);
			if (Index != -1)
			{
				using (SolidBrush brush = new SolidBrush(SystemColors.MenuText))
				{
					Size textSize = TextUtil.GetTextSize(g, Items[Index].ToString(), Font);
					int top = bounds.Top + (bounds.Height - textSize.Height) / 2;
					// Clipping rectangle
					Rectangle clipRect = new Rectangle(bounds.Left + 4, top, bounds.Width - ARROW_WIDTH - 4, top + textSize.Height);
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
			int index = SelectedIndex;
			if ( index == -1 ) return;

			using ( Graphics g = CreateGraphics() )
			{
				using ( Brush b = new SolidBrush(SystemColors.ControlDark) )
				{
					Rectangle rc = ClientRectangle;
					Size textSize = TextUtil.GetTextSize(g, Items[index].ToString(), Font);

					// Clipping rectangle
					int top = rc.Top + (rc.Height - textSize.Height)/2;
					Rectangle clipRect = new Rectangle(rc.Left + 4,
						top, rc.Width - 4 - ARROW_WIDTH - 4, top+textSize.Height);
					g.DrawString(Items[index].ToString(), Font, b, clipRect);

				}
			}
		}

		protected void DrawDataBoundMember(Graphics g, Brush brush, int index, Point point)
		{
			string text = string.Empty;
			if ( DataSource != null )
			{
				IList list = null;
				// We coud be bound to an array list which implement the IList interface
				// or we could be bound to a DataSet which implements the IListSource from which
				// we can get the IList interface
				if ( DataSource is IList)
				{
					// Assume this is an array object and try to get the data based
					// on this assumption
					list = (IList)DataSource;
					Debug.Assert(list != null, "ComboBox is bound to unrecognized DataSource");
					// Now extract the actual text to be displayed
					object o = list[index];
					Type objectType = o.GetType();
					// Now invoke the method that is associate with the
					// Display name property of the ComboBox
					PropertyInfo pi = objectType.GetProperty(DisplayMember);
					text = (string)pi.GetValue(o, null);
				}
				else
				{
					// Data set object
					if ( DataSource is IListSource )
					{
						IListSource ls = (IListSource)DataSource;
						list = ls.GetList();
						Debug.Assert(list != null, "ComboBox is bound to unrecognized DataSource");
						// This is a data set object, get the value under that assumption
						DataRowView dataRowView = (DataRowView)list[index];
						DataRow dataRow = dataRowView.Row;
						object o = dataRow[DisplayMember];
						text = o.ToString();

					}
				}
				g.DrawString(text, Font, brush, point);
			}
		}

		#endregion


	}
}
