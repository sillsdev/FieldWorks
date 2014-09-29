using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using SIL.Utils;
using System.Linq;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Derived from ListView, this class supports notifying the browse view
	/// when resizing the columns.
	/// This class holds the headers that show above columns of data that BrowseViewer knows about.
	/// </summary>
	public class DhListView : ListView, IFWDisposable
	{
		private BrowseViewer m_bv;
		private ImageList m_imgList;
		private bool m_fInAdjustWidth = false; // used to ignore recursive calls to AdjustWidth.
		private bool m_fColumnDropped = false;	// set this after we've drag and dropped a column
		bool m_suppressColumnWidthChanges;
		private ToolTip m_tooltip;

#if __MonoCS__	// FWNX-224
			// on Mono, when a right click is pressed, this class emits a RightClick event
			// followed by a generic click event.
			// This flag allows use to generate a LeftClick event if we previously didn't
			// receive a RightClick.
			private bool m_fIgnoreNextClick = false;
#endif

		/// <summary></summary>
		public event ColumnRightClickEventHandler ColumnRightClick;
#if __MonoCS__	// FWNX-224
		/// <summary>event for 'left click'</summary>
		public event ColumnClickEventHandler ColumnLeftClick;
#endif

		/// <summary></summary>
		public event ColumnDragDropReorderedHandler ColumnDragDropReordered;

		internal bool AdjustingWidth
		{
			get
			{
				CheckDisposed();
				return m_fInAdjustWidth;
			}
			set
			{
				CheckDisposed();
				m_fInAdjustWidth = value;
			}
		}

		int kHalfArrowSize = 6;

#if __MonoCS__ // FWNX-646: missing column headings
		/// <summary/>
		protected override void OnParentVisibleChanged(EventArgs e)
		{
			// Force a call to internal mono method LayoutDetails().
			BeginUpdate();
			EndUpdate();
			base.OnParentVisibleChanged(e);
		}
#endif

		/// <summary>
		/// Create one and set the browse view it belongs to.
		/// </summary>
		/// <param name="bv"></param>
		public DhListView(BrowseViewer bv)
		{
			m_bv = bv;

			m_imgList = new ImageList();
			m_imgList.ImageSize = new Size(kHalfArrowSize * 2, kHalfArrowSize * 2);
			m_imgList.TransparentColor = Color.FromKnownColor(KnownColor.ControlLight);

			m_imgList.Images.Add(GetArrowBitmap(ArrowType.Ascending, ArrowSize.Large));		// Add ascending arrow
			m_imgList.Images.Add(GetArrowBitmap(ArrowType.Ascending, ArrowSize.Medium));		// Add ascending arrow
			m_imgList.Images.Add(GetArrowBitmap(ArrowType.Ascending, ArrowSize.Small));		// Add ascending arrow
			m_imgList.Images.Add(GetArrowBitmap(ArrowType.Descending, ArrowSize.Large));		// Add descending arrow
			m_imgList.Images.Add(GetArrowBitmap(ArrowType.Descending, ArrowSize.Medium));		// Add descending arrow
			m_imgList.Images.Add(GetArrowBitmap(ArrowType.Descending, ArrowSize.Small));		// Add descending arrow

			ColumnWidthChanged += ListView_ColumnWidthChanged;
#if __MonoCS__ // FWNX-224
			ColumnClick += HandleColumnClick;
#endif
			ColumnWidthChanging += ListView_ColumnWidthChanging;
			ColumnReordered += HandleColumnReordered;
		}

		/// <summary>
		/// Get the columns in the order they display. This is USUALLY the same as the order of items in Columns,
		/// but when the user drags a column to re-order it, the system does not re-order the Columns
		/// collection, but just changes the DisplayOrder of the affected items. So we must sort by that to get
		/// them in the order they are actually seen, which is the order we wish to have the View data columns.
		///
		/// Still more pathologically, in HandleColumnReordered, the event handler for the end of the drag,
		/// the system passes us an argument telling us which column was dragged and what its new DisplayIndex
		/// is, but it has NOT yet updated them. So during this method, a sort by DisplayIndex produces the OLD
		/// order. But we want to update the rest of the display to match the NEW order. So for the durarion of
		/// that method, we override the results of this property to reflect the modified order that is about to happen.
		/// </summary>
		public List<ColumnHeader> ColumnsInDisplayOrder
		{
			get
			{
				var result = new List<ColumnHeader>(Columns.Cast<ColumnHeader>());
				result.Sort((x, y) => x.DisplayIndex.CompareTo(y.DisplayIndex));
				return result;
			}
		}

		private List<int> m_orderForColumnsDisplay = new List<int>();
		/// <summary>
		/// This is used to keep track of the positions of columns after they have been dragged and dropped.
		/// ColumnsDisplayOrder[i] is the index of the position where the column at
		/// position i in the orginal Columns collection is actually displayed.
		/// </summary>
		private List<int> OrderForColumnsDisplay
		{
			get
			{
				m_orderForColumnsDisplay.Clear();
				foreach (ColumnHeader col in Columns)
				{
					m_orderForColumnsDisplay.Add(col.DisplayIndex);
				}
				return m_orderForColumnsDisplay;
			}
		}

		private void HandleColumnReordered(object sender, ColumnReorderedEventArgs e)
		{
			// Disallow reordering the checkbox column, either by moving it or moving something into its place
			if (HasCheckBoxColumn && (e.OldDisplayIndex == 0 || e.NewDisplayIndex == 0))
			{
				e.Cancel = true;
				return;
			}
			// At this point, we want to re-arrange the dependent parts of the display (the filter bar and the main data area)
			// to reflect the re-ordering of the columns.
			// However, the system has not actually re-ordered them. So we must simulate the order it is going to change
			// them to.

			// Now we want an array of integers showing how they are re-ordered.
			// columnDisplayOrder[i] is the position that element i in the old (previous, not original) order will have in the new order.
			// Note that we cannot depend on e.OldDisplayIndex. In Windows, this is the position in the
			// most recent previous display order; in Mono, it is the position in the original sequence.
			// However, both systems seem to pass us the columns with each header having its pre-change
			// DisplayIndex intact.
			var columnDisplayOrder = Enumerable.Range(0, Columns.Count).ToList();
			var reorderedColumnHeader = columnDisplayOrder[e.Header.DisplayIndex];
			columnDisplayOrder.Remove(reorderedColumnHeader);
			columnDisplayOrder.Insert(e.NewDisplayIndex, reorderedColumnHeader);

			// Let affected browse view update its columns of data
			if (ColumnDragDropReordered != null)
				ColumnDragDropReordered(this, new ColumnDragDropReorderedEventArgs(columnDisplayOrder));

			//Adjust the browseViewer column ordering whenever columns are moved.
			m_bv.OrderForColumnsDisplay = m_orderForColumnsDisplay;

		}

		#region Support for context menu and column size on header

		private const int WM_CONTEXTMENU = 0x007B;
		private const int WM_NOTIFY = 0x004E;

		[StructLayout(LayoutKind.Sequential)]
		private struct NMHDR
		{
			/// <summary>
			/// The HWND from
			/// </summary>
			public IntPtr hwndFrom;
			/// <summary>
			/// The identifier from
			/// </summary>
			public int idFrom;
			/// <summary>
			/// The code
			/// </summary>
			public int code;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct NMHEADER
		{
			/// <summary>
			/// The HWND from
			/// </summary>
			public NMHDR hdr;
			/// <summary>
			/// The identifier from
			/// </summary>
			public int iItem;
			/// <summary>
			/// The code
			/// </summary>
			public int iButton;

			/// <summary>
			/// The pitem
			/// </summary>
			public IntPtr pitem;
		}

		/// <summary>
		/// Override of WndProc to handle the context menu or column resize on column headers.
		/// Necessary because ListView eats almost all mouse events, and does not provide any that can be used to handling right
		/// clicks in the header.
		/// Also couldn't use class wizard to handle WM_NOTIFY because it won't add HDN_ITEMCLICKA and HDN_DIVIDERDBLCLICKW
		/// </summary>
		/// <param name="m">The Windows Message to process</param>
		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case WM_CONTEXTMENU:
					Point pointClicked = PointToClient(MousePosition);
					int index = GetColumnIndexFromMousePosition(pointClicked);
					if (index >= 0)
					{
						OnColumnRightClick(index, pointClicked);
					}
					break;
				case WM_NOTIFY:
					var nm = (NMHDR)m.GetLParam(typeof(NMHDR));

					// Notification names: standard, ANSI (A), and Unicode (W)
					const int HDN_FIRST = 0 - 300;
					const int HDN_DIVIDERDBLCLICKA = HDN_FIRST - 5;
					const int HDN_DIVIDERDBLCLICKW = HDN_FIRST - 25;

					// Handle notifications for header column divider
					if (nm.code == HDN_DIVIDERDBLCLICKA || nm.code == HDN_DIVIDERDBLCLICKW)
					{
						// Resize the column based on the header width and then pass that size to AutoResizeColumn.
						// nmheader.iItem is in the original column order, but the resizes are done in display order.
						var nmheader = (NMHEADER)m.GetLParam(typeof(NMHEADER));
						int inDisplayOrder = m_orderForColumnsDisplay[nmheader.iItem];
						AutoResizeColumn(inDisplayOrder, ColumnHeaderAutoResizeStyle.HeaderSize);

						int headerWidth = ColumnsInDisplayOrder[inDisplayOrder].Width;
						m_bv.AdjustColumnWidthToMatchContents(inDisplayOrder, headerWidth);
					}
					// Handle all other notifications
					else
					{
						base.WndProc(ref m);
					}
					break;
				default:
					base.WndProc(ref m);
					break;
			}
		}

		private int GetColumnIndexFromMousePosition(Point pt)
		{
			int width = 0;
			for (int col = 0; col < Columns.Count; ++col)
			{
				width += Columns[col].Width;
				// Review: is there some way we can check for a valid pt.Y value?
				// It's probably okay as is, since the body of the list view intercepts
				// mouse clicks before they get here.
				if (pt.X <= width)
				{
					return col;
				}
			}
			return -1;
		}

		#endregion


#if __MonoCS__ // FWNX-224
		internal void HandleColumnClick (object sender, ColumnClickEventArgs e)
		{
			if (m_fIgnoreNextClick)
			{
				m_fIgnoreNextClick = false;
				return;
			}

			if (ColumnLeftClick != null && m_fIgnoreNextClick == false)
				ColumnLeftClick(this, e);
		}
#endif

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.ListView"/> and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_imgList != null)
					m_imgList.Dispose();
				if (m_timer != null)
				{
					m_timer.Dispose();
					m_timer = null;
				}
				if (m_tooltip != null)
				{
					m_tooltip.Dispose();
					m_tooltip = null;
				}
			}
			m_imgList = null;
			m_bv = null;

			base.Dispose (disposing);
		}

		/// <summary>
		/// If there is a checkbox column
		/// </summary>
		public virtual bool HasCheckBoxColumn
		{
			get
			{
				return m_bv.m_xbv.Vc.HasSelectColumn;
			}
		}

		/// <summary>
		/// May be used to suppress the normal behavior of adjusting things and saving column widths
		/// when a header column width changes.
		/// </summary>
		internal bool SuppressColumnWidthChanges
		{
			get { return m_suppressColumnWidthChanges; }
			set { m_suppressColumnWidthChanges = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			m_suppressColumnWidthChanges = true;
			base.OnHandleCreated (e);
			m_suppressColumnWidthChanges = false;
			this.OwnerDraw = true;
			this.DrawColumnHeader += DhListView_DrawColumnHeader;
		}

		// Set when we paint with the background color that indicates the mouse is inside.
		// For some reason Windows knows to paint the header when we move into a column but not when we move out of it.
		private Rectangle m_hotRectangle;

		/// <summary>
		/// This seems to be the only way to find out when the mouse moves out of one of our column headers. Ugh!!
		/// </summary>
		private Timer m_timer;
		private int m_cTicks;
		private string m_tooltipText;
		// even if there is a fresh paint (e.g., because the tooltip went away) we don't want to show the same one again
		// during the same visit to the same header.
		private string m_lastTooltipText;

		void m_timer_Tick(object sender, EventArgs e)
		{
			var cursorLocation = PointToClient(Cursor.Position);
			if (m_hotRectangle.Height > 0 && !m_hotRectangle.Contains(cursorLocation))
			{
				m_timer.Stop();
				m_timer.Dispose();
				m_timer = null;
				Invalidate(true);
				m_hotRectangle = Rectangle.Empty;
				if (m_tooltip != null)
				{
					m_tooltip.Dispose();
					m_tooltip = null;
					m_lastTooltipText = null;
				}
				return;
			}
			m_cTicks++;
			if (m_cTicks == 10 && m_lastTooltipText != m_tooltipText)
			{
				m_lastTooltipText = m_tooltipText;
				if (m_tooltip == null)
				{
					m_tooltip = new ToolTip();
					m_tooltip.InitialDelay = 10;
					m_tooltip.ReshowDelay = 10;
					m_tooltip.SetToolTip(this, m_tooltipText);
				}
				m_tooltip.Show(m_tooltipText, this, cursorLocation.X, cursorLocation.Y, 2000);
			}
		}

		void DhListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
		{
			var paleBlue = Color.FromArgb(187, 235, 254);
			var veryPaleBlue = Color.FromArgb(227, 247, 255);
			var lightBlue = Color.LightBlue;
			var drawRect = e.Bounds;
			// For some reason Bounds.Height is much too big. Bigger than our whole control!
			drawRect.Height = Height - 4;
			int imageIndex;
			//e.Header.Index is it's index in the original, un-dragged list of columns.
			//m_columnIconIndexes is indexed by the current display position of columns that require the icon to be displayed.
			var currentDisplayPositionOfColumn = OrderForColumnsDisplay[e.Header.Index];
			if (!m_columnIconIndexes.TryGetValue(currentDisplayPositionOfColumn, out imageIndex))
				imageIndex = -1;
			if (imageIndex >= 0)
				drawRect.Width -= m_imgList.ImageSize.Width;

			var topHeight = drawRect.Height / 2 - 1;
			var drawText = e.Header.Text;
			var realSize = e.Graphics.MeasureString(drawText, e.Font);
			if ((e.State & ListViewItemStates.Selected) != 0)
			{
				using (var brush = new SolidBrush(paleBlue))
				{
					e.Graphics.FillRectangle(brush, new Rectangle(e.Bounds.Left, e.Bounds.Top, e.Bounds.Width, topHeight));
				}
				using (var brush = new SolidBrush(lightBlue))
				{
					e.Graphics.FillRectangle(brush, new Rectangle(e.Bounds.Left, e.Bounds.Top + topHeight, e.Bounds.Width, e.Bounds.Height - topHeight));
				}
			}
			else if (e.Bounds.Contains(PointToClient(Cursor.Position))) // seems to be no state that indicates mouse is in it...Hot should but doesn't.
			{
				m_hotRectangle = e.Bounds;
				if (m_timer == null)
				{
					m_timer = new Timer();
					m_timer.Interval = 50;
					m_timer.Tick += m_timer_Tick;
					m_timer.Start();
				}
				m_cTicks = 0;
				if (realSize.Width > drawRect.Width)
				{
					m_tooltipText = drawText;
				}
				else
				{
					m_tooltipText = null;
				}
				using (var brush = new SolidBrush(veryPaleBlue))
				{
					e.Graphics.FillRectangle(brush, new Rectangle(e.Bounds.Left, e.Bounds.Top, e.Bounds.Width, topHeight));
				}
				using (var brush = new SolidBrush(paleBlue))
				{
					e.Graphics.FillRectangle(brush, new Rectangle(e.Bounds.Left, e.Bounds.Top + topHeight, e.Bounds.Width, e.Bounds.Height - topHeight));
				}
			}
			else
			{
				e.DrawBackground(); // standard background
			}
			// Draw the header text.
			if (realSize.Width > drawRect.Width)
			{
				// Guess how much we can fit.
				int len = (int)(drawText.Length * drawRect.Width / realSize.Width) + 1;

				// Subtract more until it fits.
				do
				{
					len = len - 1;
					Debug.Assert(len >= 0, "length should not be negative");
					drawText = drawText.Substring(0, len) + "\x2026"; // ellipsis
					realSize = e.Graphics.MeasureString(drawText, e.Font);
				} while (len > 0 && realSize.Width > drawRect.Width);

				// Add any more that fits.
				while (len < e.Header.Text.Length - 1)
				{
					len = len + 1;
					var possibleText = e.Header.Text.Substring(0, len) + "\x2026"; // ellipsis
					realSize = e.Graphics.MeasureString(possibleText, e.Font);
					if (realSize.Width > drawRect.Width)
						break; // we can't add this one more character.
					drawText = possibleText; // we can fit this much at least.
				}
			}
			using (StringFormat sf = new StringFormat())
			{
				sf.Trimming = StringTrimming.Character;
				sf.FormatFlags = StringFormatFlags.NoWrap;
				sf.LineAlignment = StringAlignment.Far; // bottom

				//if (drawRect.Height > realSize.Height - 2)
				//{
				//    drawRect = new Rectangle(drawRect.X, drawRect.Top + drawRect.Height - (int)realSize.Height - 2, drawRect.Width, (int)realSize.Height + 2);
				//}
				e.Graphics.DrawString(drawText, e.Font,
					Brushes.Black, drawRect, sf);
			}
			if (imageIndex >= 0)
			{
				var size = m_imgList.ImageSize;
				m_imgList.Draw(e.Graphics, e.Bounds.Right - 2 - size.Width, e.Bounds.Top + 2, imageIndex);
			}
		}

		private void ListView_ColumnWidthChanged(Object sender, ColumnWidthChangedEventArgs e)
		{
			// These two tests seem to suppress all the cases where we are changing the width during setup
			// and while making secondary adjustments, and allow us only to make a new call to AdjustColumns
			// when the user has actually dragged to change a column width.
			if (!AdjustingWidth && !SuppressColumnWidthChanges)
				m_bv.AdjustColumnWidths(true);
		}

		/// <summary>
		/// Reject inappropriate column width changes.
		/// Helps to fix FWNX-1018.
		/// No column should be so small that compensating for ascending/descending icon size
		/// (m_imgList.ImageSize.Width) in DhListView_DrawColumnHeader
		/// would result in a negative width.
		/// </summary>
		private void ListView_ColumnWidthChanging(Object sender, ColumnWidthChangingEventArgs e)
		{
			if (!IsThisColumnChangeAllowable(e.ColumnIndex, Columns[e.ColumnIndex].Width, e.NewWidth))
				e.Cancel = true;
		}

		/// <summary>
		/// Determine if a column width change is acceptable.
		/// </summary>
		private bool IsThisColumnChangeAllowable(int columnIndex, int currentWidth, int newWidth)
		{
			// Don't allow resizing the checkbox column, if present.
			if (HasCheckBoxColumn && columnIndex == 0)
				return false;
			// Don't allow resizing if the new width is less than it should be.
			// But it's okay if the new width is less than the minimum if it's at least
			// bigger than the current width. Otherwise if a column were somehow
			// smaller than the minimum, a user couldn't ever make it bigger.
			if (newWidth >= currentWidth)
				return true;
			if (newWidth < kMinColWidth)
				return false;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a right click event in a column header.
		/// </summary>
		/// <param name="iItem">The i item.</param>
		/// <param name="ptLoc">The pt loc.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnColumnRightClick(int iItem, Point ptLoc)
		{
#if __MonoCS__ // FWNX-224
			// set flag so next ColumnClick event doesn't generate a left click
			m_fIgnoreNextClick = true;
#endif

			if (ColumnRightClick != null)
				ColumnRightClick(this, new ColumnRightClickEventArgs(iItem, ptLoc));
		}

		internal const int kgapForScrollBar = 23;
		/// <summary>
		/// Minimum width a normal column can be (other than the checkbox column)
		/// </summary>
		internal const int kMinColWidth = 25;

		enum ArrowType { Ascending, Descending }
		/// <summary></summary>
		public enum ArrowSize {
			/// <summary></summary>
			Large = 0,
			/// <summary></summary>
			Medium = 1,
			/// <summary></summary>
			Small = 2
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create up/down icon (Adapted from a post by Eddie Velasquez.)
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="size">The size.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		Bitmap GetArrowBitmap(ArrowType type, ArrowSize size)
		{
			int offset;
			switch(size)
			{
				case ArrowSize.Large:
					offset = 0;
					break;
				case ArrowSize.Medium:
					offset = 2;
					break;
				case ArrowSize.Small:
					offset = 3;
					break;
				default:
					offset = 0;
					break;
			}

			Bitmap bmp = new Bitmap(kHalfArrowSize * 2, kHalfArrowSize * 2);
			using (Graphics gfx = Graphics.FromImage(bmp))
			{
				Brush brush = new SolidBrush(Color.FromArgb(215,230,255));
				Pen pen = new Pen(Color.FromArgb(49,106,197));

				gfx.FillRectangle(new SolidBrush(Color.FromKnownColor(KnownColor.ControlLight)), 0, 0, kHalfArrowSize * 2, kHalfArrowSize * 2);

				Point[] points = null;
				if(type == ArrowType.Ascending)
				{
//					Point left = new Point(kHalfArrowSize, 0);
//					Point right = new Point(kHalfArrowSize, 0);
//					for (int i = 0; i < kHalfArrowSize; i++)
//					{
//						left.X -= 1;
//						right.X += 1;
//						gfx.DrawLine(pen, left, right);
//						left.Y += 1;
//						right.Y += 1;
//						gfx.DrawLine(pen, left, right);
//						left.Y += 1;
//						right.Y += 1;
//					}
					points = new Point[] { new Point(kHalfArrowSize, offset), new Point(kHalfArrowSize * 2 - 1 - offset, kHalfArrowSize * 2 - 1 - offset),
						new Point(offset,kHalfArrowSize * 2 - 1 - offset)};
					gfx.FillPolygon(brush, points);
					gfx.DrawPolygon(pen, points);
				}
				else if(type == ArrowType.Descending)
				{
					points = new Point[] { new Point(offset,offset), new Point(kHalfArrowSize * 2 - 1 - offset, offset), new Point(kHalfArrowSize,kHalfArrowSize * 2 - 1 - offset)};
					gfx.FillPolygon(brush, points);
					gfx.DrawPolygon(pen, points);
				}
			}

			return bmp;
		}

		Dictionary<int, int> m_columnIconIndexes = new Dictionary<int, int>();
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the header icon. (Thanks to a post by Eddie Velasquez.)
		/// </summary>
		/// <param name="columnIndex">Index of the column.</param>
		/// <param name="sortOrder">The sort order.</param>
		/// <param name="size">The size.</param>
		/// ------------------------------------------------------------------------------------
		public void ShowHeaderIcon(int columnIndex, SortOrder sortOrder, ArrowSize size)
		{
			CheckDisposed();

			if (columnIndex < 0 || columnIndex >= Columns.Count)
				return;

			if (sortOrder == SortOrder.None)
			{
				m_columnIconIndexes.Remove(columnIndex);
			}
			else
			{
				// There are 3 potential sizes: Large, Medium, and Small with 2 sort orders, Ascending and Descending
				// The images are stored in that order, so the following works
				m_columnIconIndexes[columnIndex] = (int)size + (3 * ((int)sortOrder - 1));
			}
		}
	}

	/// <summary>
	/// Specialized event handler for right mouse button clicks in list view column headers.
	/// </summary>
	public delegate void ColumnRightClickEventHandler(object sender,
	ColumnRightClickEventArgs e);

	/// <remarks>
	/// Specialized event argument for right mouse button clicks in list view column headers.
	/// </remarks>
	public class ColumnRightClickEventArgs : EventArgs
	{
		private int m_icol;
		private Point m_ptLocation;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ColumnRightClickEventArgs(int icol, Point ptLoc)
		{
			m_icol = icol;
			m_ptLocation = ptLoc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the column.
		/// </summary>
		/// <value>The column.</value>
		/// ------------------------------------------------------------------------------------
		public int Column
		{
			get { return m_icol; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the location.
		/// </summary>
		/// <value>The location.</value>
		/// ------------------------------------------------------------------------------------
		public Point Location
		{
			get { return m_ptLocation; }
		}
	}

	/// <summary>
	/// Handles clicking on ConfigIcon
	/// </summary>
	public delegate void ConfigIconClickHandler(object sender,
		ConfigIconClickEventArgs e);

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Arguments for clicking on ConfigIcon
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ConfigIconClickEventArgs : EventArgs
	{
		private Rectangle m_buttonLocation;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="location">The location.</param>
		/// ------------------------------------------------------------------------------------
		public ConfigIconClickEventArgs(Rectangle location)
		{
			m_buttonLocation = location;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the location of the button relative to the DhListView's client area.
		/// It provides a good idea of where to display a popup.
		/// </summary>
		/// <value>The location.</value>
		/// ------------------------------------------------------------------------------------
		public Rectangle Location
		{
			get { return m_buttonLocation; }
		}
	}

	/// <summary>
	/// Handles drag-n-drop for reordering columns
	/// </summary>
	public delegate void ColumnDragDropReorderedHandler(object sender,
		ColumnDragDropReorderedEventArgs e);

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ColumnDragDropReorderedEventArgs : EventArgs
	{
		private List<int> m_displayedColumnOrder;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ColumnDragDropReorderedEventArgs(List<int> newColumnOrder)
		{
			m_displayedColumnOrder = newColumnOrder;
		}

		/// <summary>
		/// Contains indices for the Columns collection in the order they are displayed.
		/// </summary>
		public List<int> DragDropColumnOrder
		{
			get
			{
				if (m_displayedColumnOrder == null)
					m_displayedColumnOrder = new List<int>();	// empty list

				return m_displayedColumnOrder;
			}
		}
	}
}
