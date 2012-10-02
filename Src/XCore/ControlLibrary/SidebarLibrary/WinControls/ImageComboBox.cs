using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using SidebarLibrary.General;

namespace SidebarLibrary.WinControls
{
	/// <summary>
	/// Summary description for ImageComboBox.
	/// </summary>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(SidebarLibrary.WinControls.ImageComboBox),
	 "SidebarLibrary.WinControls.ImageComboBox.bmp")]
	public class ImageComboBox : ComboBoxBase
	{
		#region Class Variables
		Bitmap[] bitmapsArray;
		string[] bitmapsNames;
		ImageList imageList;
		bool useImageList = false;
		private const int PREVIEW_BOX_WIDTH = 20;
		#endregion

		#region Constructors
		public ImageComboBox()
		{
			InitializeImageComboBox(null, true, null, null);

		}

		public ImageComboBox(ImageList imageList)
		{
			InitializeImageComboBox(imageList, true, null, null);
		}

		// Run time support only
		public ImageComboBox(Bitmap[] bitmapsArray, String[] bitmapsNames)
		{
			InitializeImageComboBox(null, false, bitmapsArray, bitmapsNames);
		}

		// Run time support only
		public ImageComboBox(Bitmap[] bitmapsArray, String[] bitmapsNames, bool toolBarUse): base(toolBarUse)
		{
			// To be used when using the combobox in as a ToolBarItem in a ToolBarEx control
			InitializeImageComboBox(null, false, bitmapsArray, bitmapsNames);
		}

		void InitializeImageComboBox(ImageList list, bool useImageList, Bitmap[] bitmapsArray, String[] bitmapsNames)
		{
			DropDownStyle = ComboBoxStyle.DropDownList;
			imageList = list;
			this.useImageList = useImageList;
			if ( bitmapsArray != null && bitmapsNames != null && useImageList == false )
			{
				this.bitmapsArray = bitmapsArray;
				this.bitmapsNames = bitmapsNames;
				for ( int i = 0; i < bitmapsArray.Length; i++ )
				{
					Items.Add(bitmapsNames[i]);
				}
			}
		}

		#endregion

		#region Overrides
		protected override void DrawComboBoxItem(Graphics g, Rectangle bounds, int Index, bool selected, bool editSel)
		{
			// Call base class to do the "Flat ComboBox" drawing
			// Draw bitmap
			base.DrawComboBoxItem(g, bounds, Index, selected, editSel);
			if (Index != -1)
			{
				using (Brush brush = new SolidBrush(SystemColors.MenuText))
				{
					if (useImageList == false)
						g.DrawImage(bitmapsArray[Index], bounds.Left + 2, bounds.Top + 2, PREVIEW_BOX_WIDTH, bounds.Height - 4);
					else
						g.DrawImage(imageList.Images[Index], bounds.Left + 2, bounds.Top + 2, PREVIEW_BOX_WIDTH, bounds.Height - 4);

					using (Pen blackPen = new Pen(new SolidBrush(Color.Black), 1))
						g.DrawRectangle(blackPen, new Rectangle(bounds.Left + 1, bounds.Top + 1, PREVIEW_BOX_WIDTH + 1, bounds.Height - 3));

					Size textSize = TextUtil.GetTextSize(g, Items[Index].ToString(), Font);
					int top = bounds.Top + (bounds.Height - textSize.Height) / 2;
					g.DrawString(Items[Index].ToString(), Font, brush,
					new Point(bounds.Left + 28, top));
				}
			}
		}

		protected override void DrawComboBoxItemEx(Graphics g, Rectangle bounds, int Index, bool selected, bool editSel)
		{
			// This "hack" is necessary to avoid a clipping bug that comes from the fact that sometimes
			// we are drawing using the Graphics object for the edit control in the combobox and sometimes
			// we are using the graphics object for the combobox itself. If we use the same function to do our custom
			// drawing it is hard to adjust for the clipping because of these limitations
			base.DrawComboBoxItemEx(g, bounds, Index, selected, editSel);
			if (Index != -1)
			{
				using (SolidBrush brush = new SolidBrush(SystemColors.MenuText))
				{
					Rectangle rc = bounds;
					rc.Inflate(-3, -3);
					using (Pen blackPen = new Pen(new SolidBrush(Color.Black), 1))
						g.DrawRectangle(blackPen, new Rectangle(rc.Left + 1, rc.Top + 1, PREVIEW_BOX_WIDTH + 1, rc.Height - 3));

					if (useImageList == false)
						g.DrawImage(bitmapsArray[Index], rc.Left + 2, rc.Top + 2, PREVIEW_BOX_WIDTH, rc.Height - 4);
					else
						g.DrawImage(imageList.Images[Index], rc.Left + 2, rc.Top + 2, PREVIEW_BOX_WIDTH, rc.Height - 4);

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

		#region Properties
		public ImageList Images
		{
			get{ return imageList;}
			set{ imageList = value;}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public new ComboBox.ObjectCollection Items
		{
			get { return base.Items; }
		}

		// Only Run time support
		[Browsable(false)]
		public Bitmap[] Bitmaps
		{
			set { bitmapsArray = value; }
			get { return bitmapsArray; }
		}

		// Only Run time support
		[Browsable(false)]
		public string[] BitmapNames
		{
			set
			{
				bitmapsNames = value;
				if ( bitmapsNames != null )
				{
					// Add empty element so that we can get call to draw
					// the bitmaps items
					for ( int i = 0; i < bitmapsNames.Length; i++ )
					{
						Items.Add(bitmapsNames[i]);
					}
				}
			}
			get { return bitmapsNames; }
		}

		#endregion

		#region Methods
		// Designer support
		public void PassMsg(ref Message m)
		{
			base.WndProc(ref m);
		}
		#endregion

	}
}
