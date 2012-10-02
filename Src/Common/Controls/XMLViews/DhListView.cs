using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Resources;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{

	/// <summary>
	/// Derived from ListView, this class supports notifying the browse view
	/// when resizing the columns.
	/// </summary>
	public class DhListView : ListView, IFWDisposable
	{
		private BrowseViewer m_bv;
		private ImageList m_imgList;
		private bool m_fInAdjustWidth = false; // used to ignore recursive calls to AdjustWidth.
		private Button m_checkMarkButton; // button shown over check mark column if any.
		Timer m_configDisplayTimer = null; // See comment on UpdateConfigureButton
		private bool m_fColumnDropped = false;	// set this after we've drag and dropped a column
		bool m_suppressColumnWidthChanges;
//		int m_checkColWidth;

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
		public event ConfigIconClickHandler CheckIconClick;
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

#if __MonoCS__ // FWNX-131
			SmallImageList = m_imgList;
#endif

			m_checkMarkButton = new Button();
			m_checkMarkButton.Click += new EventHandler(m_checkMarkButton_Click);
			m_checkMarkButton.Image = ResourceHelper.CheckMarkHeader;
			m_checkMarkButton.Width = m_checkMarkButton.Image.Width + 4;
			m_checkMarkButton.FlatStyle = FlatStyle.Flat;
			m_checkMarkButton.BackColor = Color.FromKnownColor(KnownColor.ControlLight);
			m_checkMarkButton.ForeColor = Color.FromKnownColor(KnownColor.ControlLight);
			m_checkMarkButton.Top = 0;
			m_checkMarkButton.Left = -1;

#if __MonoCS__ // FWNX-224
			ColumnWidthChanged += ListView_ColumnWidthChanged;

			ColumnClick += HandleColumnClick;
#endif
		}

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
				if (m_configDisplayTimer != null)
				{
					m_configDisplayTimer.Stop();
					m_configDisplayTimer.Tick -= new EventHandler(m_configDisplayTimer_Tick);
					m_configDisplayTimer.Dispose();
				}
				if (m_checkMarkButton != null)
				{
					m_checkMarkButton.Click -= new EventHandler(m_checkMarkButton_Click);
					if (!Controls.Contains(m_checkMarkButton))
						m_checkMarkButton.Dispose();
				}
				if (m_imgList != null)
					m_imgList.Dispose();
			}
			m_configDisplayTimer = null;
			m_imgList = null;
			m_checkMarkButton = null;
			m_bv = null;

			base.Dispose (disposing);
		}

		/// <summary>
		/// Disabled property: We have disabled the CheckMarkHeader in DhListView because
		/// it is not reliably receiving system Paint messages after an Invalidate(), Refresh(), or Update().
		/// Until we find a solution to that problem, the parent of DhListView (e.g. BrowseViewer)
		/// will have to be responsible for setting up this button.(cf. LT-4473)
		/// </summary>
		public bool DisplayCheckMarkHeader
		{
			get
			{
				CheckDisposed();
				return Controls.Contains(m_checkMarkButton);
			}
			set
			{
				CheckDisposed();

				Debug.Assert(value != true, "CheckMarkButton currently disabled. See comment for DisplayCheckMarkHeader");
				//if (value == DisplayCheckMarkHeader)
				//	return;
				//if (value)
				//    Controls.Add(m_checkMarkButton);
				//else
				//    Controls.Remove(m_checkMarkButton);
			}
		}

		// no use because it never gets called
		//		protected override void OnPaint(PaintEventArgs e)
		//		{
		//			base.OnPaint (e);
		//			Image blueArrow = ResourceHelper.BlueCircleDownArrow;
		//			int xPos = this.Width - blueArrow.Width - 4;
		//			e.Graphics.DrawImage(blueArrow, new Rectangle(xPos, 0, blueArrow.Width, blueArrow.Height));
		//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Layout"/> event.
		/// </summary>
		/// <param name="levent">A <see cref="T:System.Windows.Forms.LayoutEventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs levent)
		{
			base.OnLayout (levent);
			// -6 seems to be about right to allow for various borders and prevent the button from
			// overwriting the bottom border of the header bar.

			// disposing and ListView Constructor can call OnLayout on mono
			if (m_checkMarkButton != null)
				m_checkMarkButton.Height = this.Height - 6;
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

#if !__MonoCS__ // FWNX-131
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			m_suppressColumnWidthChanges = true;
			base.OnHandleCreated (e);
			SetHeaderImageList(m_imgList);
			m_suppressColumnWidthChanges = false;
		}
#endif

		internal void RecordCheckWidth(int val)
		{
			CheckDisposed();

//			m_checkColWidth = val;
		}

		// Certain events seem to hide the configuration button. Invalidating it is enough to get
		// it repainted, but invalidating immediately doesn't work. My (JohnT's) desperate solution
		// is to invalidate it 1/100 second after the situations that make it disappear.
		// If you don't believe me, comment out the body of this method and watch the blue circle
		// icon disappear when you drag a column separator!
		internal void UpdateConfigureButton()
		{
			CheckDisposed();

			if (m_configDisplayTimer != null)
				m_configDisplayTimer.Stop(); // abort any previous notification
			else
			{
				m_configDisplayTimer = new Timer();
				m_configDisplayTimer.Interval = 10;
				m_configDisplayTimer.Tick += new EventHandler(m_configDisplayTimer_Tick);
			}
			m_configDisplayTimer.Start();
		}
		const int WM_NOTIFY = 0x004E;
		const int HDN_FIRST = -300;
		const int HDN_BEGINTRACKA = (HDN_FIRST - 6);
		const int HDN_BEGINTRACKW = (HDN_FIRST - 26);
		const int HDN_ENDTRACK = HDN_FIRST - 27; // strictly HDN_ENDTRACKW
		const int HDN_TRACK = HDN_FIRST - 28; // strictly HDN_TRACKW (does not seem to occur)
		const int HDN_ITEMCHANGED = HDN_FIRST - 21;		// HDN_ITEMCHANGEDW
		const int HDN_BEGINDRAG = HDN_FIRST - 10;	// Drag
		const int HDN_ENDDRAG = HDN_FIRST - 11;	// Drop
		const int HDN_ITEMDBLCLICKW = (HDN_FIRST - 23);
		const int HDN_DIVIDERDBLCLICKW = (HDN_FIRST - 25);

		const int NM_FIRST = 0;
#if !__MonoCS__
		const int NM_RCLICK = NM_FIRST - 5;
#else // FWNX-224
		const int WM_CONTEXTMENU = 0x007B; // replaces NM_RCLICK
		const int WM_MOUSE_ENTER = 0x0401;
		const int WM_MOUSE_LEAVE = 0x0402;
#endif
		const int NM_LCLICK = NM_FIRST - 12;	// Left Click Mouse Down
		const int NM_LCLICKUP = NM_FIRST - 16;	// Left Click Mouse Up

#if __MonoCS__ // FWNX-224
		// Key track if mouse is over the list header
		private bool m_MouseOverListHeader = false;

		private void ListView_ColumnWidthChanged(Object sender, ColumnWidthChangedEventArgs e)
		{
			if (m_MouseOverListHeader)
			{
				m_bv.AdjustColumnWidths(true);
				if (this.ColumnOrdersOutOfSync)
				{
					this.MaintainSelectColumnPosition();
				}
			}
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides <see cref="M:System.Windows.Forms.Control.WndProc(System.Windows.Forms.Message@)"/>.
		/// </summary>
		/// <param name="m">The Windows <see cref="T:System.Windows.Forms.Message"/> to process.</param>
		/// ------------------------------------------------------------------------------------
		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
#if __MonoCS__	// FWNX-224 : We don't GET WM_NOTIFY on mono (r152577)
			case WM_MOUSE_ENTER: //
				m_MouseOverListHeader = true;
				break;
			case WM_MOUSE_LEAVE:
				m_MouseOverListHeader = false;
				break;
			case WM_CONTEXTMENU: // replaces WM_NOTIFY -> NM_RCLICK
				Point pt_rclick = this.PointToClient(Control.MousePosition);
				int icol_rclick = GetColumnIndexFromMousePosition(pt_rclick);
				if (icol_rclick >= 0)
				{
					OnColumnRightClick(icol_rclick, pt_rclick);
					return;
				}
				break;
#else // FWNX-224 : We don't GET WM_NOTIFY on mono (r152577)

				case WM_NOTIFY:
					Win32.NMHEADER nmhdr = (Win32.NMHEADER)m.GetLParam(typeof(Win32.NMHEADER));
					switch (nmhdr.hdr.code)
					{
						case HDN_DIVIDERDBLCLICKW: // double-click on line between column headers.
							// adjust width of column to match item of greatest length.
							m_bv.AdjustColumnWidthToMatchContents(nmhdr.iItem);
							break;
						default:
							base.WndProc(ref m);
							break;
					}
					//Debug.WriteLine("DhListView.WndProc: WM_NOTIFY code = " + nmhdr.hdr.code);
				switch(nmhdr.hdr.code)
				{
					case HDN_BEGINTRACKA:
					case HDN_BEGINTRACKW:
						if (m_bv.m_xbv.Vc.HasSelectColumn)
						{
							int iCol = nmhdr.iItem;
							// We want to prevent the user from resizing the checkbox column (LT-3938)
							if (iCol == 0)
								m.Result = (IntPtr)1;	// Disable tracking.
						}
						m_suppressColumnWidthChanges = true;
						break;
					case HDN_ITEMDBLCLICKW: // double-click on line between items collapses it.
						// However, we never get this notification, because we're not treating the
						// column headers as buttons. Sigh.
						break;
					case HDN_ENDTRACK:
						m_bv.AdjustColumnWidths(true);
						m_suppressColumnWidthChanges = false;
						break;
					case HDN_ITEMCHANGED:
						// No longer needed. HDN_DIVIDERDBLCLICKW above helps to
						// avoid the the problem of someone double-clicking the
						// separator and messing up the check mark column irretrievably.
						if (!m_suppressColumnWidthChanges && !m_fInAdjustWidth)
							m_bv.AdjustColumnWidths(true);
						break;
					case NM_RCLICK:
						// HDN_ITEMCLICK never shows up for a right click, although it does for a
						// left click.
						// NM_RCLICK shows up okay, but gives no useful information for where the
						// right click occurred!  We have to get the current mouse position, and
						// compare it to the column boundaries across the window.
						Point pt_rclick = this.PointToClient(Control.MousePosition);
						int icol_rclick = GetColumnIndexFromMousePosition(pt_rclick);
						if (icol_rclick >= 0)
						{
							OnColumnRightClick(icol_rclick, pt_rclick);
							return;
						}
						break;
					case HDN_ENDDRAG:
						// We don't have a new display order until AFTER the HDN_ENDDRAG event.
						// so flag for further handling (on NM_CLICKUP).
						m_fColumnDropped = true;  // flag for further handling during NM_LCLICKUP.
						return;
					case NM_LCLICKUP:
						// first handle the special case where the user may have dragged and dropped
						// something into the location of the "select column".
						if (m_bv.m_xbv.Vc.HasSelectColumn &&
							m_fColumnDropped && this.ColumnOrdersOutOfSync)
						{
							this.MaintainSelectColumnPosition();
						}
						// if displayed columns are (still) out of sync with Columns collection,
						// then we re-sync with that reordering and let the browse view know the new order.
						if (m_fColumnDropped && this.ColumnOrdersOutOfSync)
						{
							this.OnColumnDisplayReordered();
							// reset our drag-n-drop event flag.
							m_fColumnDropped = false;
						}
						return;
					default:
						break;
				}
					break;
#endif // FWNX-224 : We don't GET WM_NOTIFY on mono (r152577)

				default:
					base.WndProc(ref m);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If user has changed the position of the SelectColumn through drag &amp; drop,
		/// move it back in its place, bumping everything else to the right to fill its gap.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MaintainSelectColumnPosition()
		{
			if (m_bv.m_xbv.Vc.HasSelectColumn == false || (int)this.ColumnDisplayOrder[0] == 0)
				return;

			List<int> displayOrder = this.ColumnDisplayOrder;
			List<int> newOrder = new List<int>(displayOrder.ToArray());
			// start shifting the indices to the right until we replace '0'
			for (int i = 0; i < displayOrder.Count && displayOrder[i] != 0; ++i)
			{
				newOrder[i + 1] = displayOrder[i];
			}
			// now put the SelectColumn back where it belongs.
			newOrder[0] = 0;

			this.ColumnDisplayOrder = newOrder;
		}

		/// <summary>
		/// True if Column collection and the displayed order is out of sync.
		/// This can occur when our AllowColumnReorder is set to true, and
		/// the user re-orders the columns by dragging and dropping.
		/// </summary>
		private bool ColumnOrdersOutOfSync
		{
			get
			{
				List<int> displayColumns = this.ColumnDisplayOrder;
				for (int i = 0; i < this.Columns.Count; ++i)
				{
					if (displayColumns[i] != i)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Reorder the Column collection according to our display order.
		/// </summary>
		private void SyncColumnOrder()
		{
			// return if we're already sync'd
			if (this.ColumnOrdersOutOfSync == false)
				return;
			List<int> displayOrder = this.ColumnDisplayOrder;
			ColumnHeader[] columnsReordered = new ColumnHeader[this.Columns.Count];
			int originalNumberOfColumns = this.Columns.Count;
			// save the original columns in the new order
			for (int i = 0; i < originalNumberOfColumns; ++i)
			{
				columnsReordered[i] = this.Columns[displayOrder[i]];
			}
			// delete the old columns
			this.Columns.Clear();
			//// Restore the columns in their new order
			//// Note: we do not want to use AddRange because when we have the checkbox column as in
			// BulkEdit we can end up having the column headers aligned improperly.
			this.BeginUpdate();
			for (int i = 0; i < originalNumberOfColumns; ++i)
			{
				Columns.Insert(i, columnsReordered[i]);
			}
			this.EndUpdate();
			Debug.Assert(this.ColumnOrdersOutOfSync == false);
		}

		private void OnColumnDisplayReordered()
		{
			Debug.Assert(this.ColumnOrdersOutOfSync == true);

			// Save our displayed column order
			List<int> newColumnOrder = this.ColumnDisplayOrder;

			// Sync our Columns collection items with the ColumnDisplayOrder
			this.SyncColumnOrder();

			// Let affected browse view update their stuff
			if (ColumnDragDropReordered != null)
				ColumnDragDropReordered(this, new ColumnDragDropReorderedEventArgs(newColumnOrder));
		}

#if !__MonoCS__
		// Courtesy of Kevin Tao from http://dotnet247.com/247reference/msgs/12/60008.aspx
		// Thread "Column indexes do not change when reordered in ListView"
		[DllImport("user32", CharSet=CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, Int32 msg, Int32 wParam, Int32[] lParam);
#endif

		[StructLayoutAttribute(LayoutKind.Sequential)]
			struct LV_COLUMN
		{
			public UInt32 mask;
			public Int32 fmt;
			public Int32 cx;
			public String pszText;
			public Int32 cchTextMax;
			public Int32 iSubItem;
			public Int32 iImage;
			public Int32 iOrder;
		}

		/// <summary>
		/// Set or Get the current display order for column headers.
		/// The "get" accessor returns a list of indices for the ListView column order, as it is currently displayed.
		/// Needed when AllowColumnReorder == true, since Columns collection will not be ordered
		/// the same as the display order after the user reorders the columns by drag and drop.
		/// The "set" accessor changes the display order for our column headers using an ArrayList of indices
		/// for the Column collection.
		/// </summary>
		private List<int> ColumnDisplayOrder
		{
			set
			{
				if (AllowColumnReorder == true && (value != null))
				{
					Debug.Assert(value.Count == this.Columns.Count, "List count must match Columns.Count");
#if !__MonoCS__
					SendMessage(this.Handle, LVM_SETCOLUMNORDERARRAY, value.Count, value.ToArray());
#endif
				}
			}
			get
			{
				List<int> result = new List<int>();
				// If AllowColumnReorder property is false, return current order.
				if (this.AllowColumnReorder == false)
				{
					for (int i = 0; i < this.Columns.Count; ++i)
						result.Add(i);
				}
				else
				{

					// Return the actual display order
					int columnCount = this.Columns.Count;
					int[] columnOrders = new int[columnCount];
#if !__MonoCS__
					SendMessage(this.Handle, LVM_GETCOLUMNORDERARRAY, columnCount, columnOrders);
#endif
					result.AddRange(columnOrders);
				}
				return result;
			}
		}

		private int GetColumnIndexFromMousePosition(Point pt)
		{
			int nWidth = 0;
			for (int icol = 0; icol < this.Columns.Count; ++icol)
			{
				nWidth += Columns[icol].Width;
				// Review: is there some way we can check for a valid pt.Y value?
				// It's probably okay as is, since the body of the list view intercepts
				// mouse clicks before they get here.
				if (pt.X <= nWidth)
				{
					return icol;
				}
			}
			return -1;
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
		internal const int kMinColWidth = 25;
		/// <summary>
		/// This attempts to prevent the header turning into a scroll bar by adjusting the widths
		/// of columns as necessary so they fit.
		///
		/// </summary>
		/// <param name="iLastAdjustItem">Index of the last column we are allowed to adjust the
		/// width of; typically 0 or the column the user is dragging.</param>
		internal void AdjustWidth(int iLastAdjustItem)
		{
			CheckDisposed();

			if (m_fInAdjustWidth)
				return;
			bool fNeedResume = false;
			try
			{
				m_fInAdjustWidth = true;
				int count = Columns.Count;
				int widthTotal = 0;

				for (int i = 0; i < count; i++)
				{
					int width = Columns[i].Width;
					widthTotal += width;
				}
				int goalWidth = Width - kgapForScrollBar;
				if (widthTotal <= goalWidth)
					return; // nothing to do.
				// Currently we never make any adjustments here.
				//				this.SuspendLayout();
				//				fNeedResume = true;
				//				// If it's gotten too wide, shrink the last column(s). First pass tries to make them no less than min.
				//				while (widthTotal > goalWidth)
				//				{
				//					bool fGotOne = false; // any column we can shrink at all?
				//					// Find last column we can shrink.
				//					for (int icol = Columns.Count; --icol >= iLastAdjustItem; )
				//					{
				//						int widthCurrent = Columns[icol].Width;
				//						int newWidth = Math.Max(widthCurrent - (widthTotal - goalWidth), kMinColWidth); // don't make column less than 1 pixel.
				//						if (newWidth < widthCurrent)
				//						{
				//							widthTotal -= widthCurrent - newWidth;
				//							Columns[icol].Width = newWidth;
				//							fGotOne = true;
				//							break;
				//						}
				//					}
				//					if (!fGotOne)
				//						break;
				//				}
				//				while (widthTotal > goalWidth)
				//				{
				//					bool fGotOne = false; // any column we can shrink at all?
				//					// Find last column we can shrink.
				//					for (int icol = Columns.Count; --icol >=0; )
				//					{
				//						int widthCurrent = Columns[icol].Width;
				//						int newWidth = Math.Max(widthCurrent - (widthTotal - goalWidth), 1); // don't make column less than 1 pixel.
				//						if (newWidth < widthCurrent)
				//						{
				//							widthTotal -= widthCurrent - newWidth;
				//							Columns[icol].Width = newWidth;
				//							fGotOne = true;
				//							break;
				//						}
				//					}
				//					if (!fGotOne)
				//						break;
				//				}
				//				// If any column right of the one dragged is less than kMinColWidth pixels wide, assign some extra to it.
				//				for (int i = iLastAdjustItem; i < count -1 && widthTotal < goalWidth; i++)
				//				{
				//					int currentWidth = Columns[i].Width;
				//					if (currentWidth < kMinColWidth)
				//					{
				//						// Change the width to be at most kMinColWidth, but no more than the
				//						// amount we want to grow plus the current size.
				//						int newWidth = Math.Min(kMinColWidth, currentWidth + goalWidth - widthTotal);
				//						Columns[i].Width = newWidth;
				//						widthTotal += newWidth - currentWidth;
				//					}
				//				}
				//				// If anything is left assign it to the last column.
				//				if (widthTotal < goalWidth)
				//				{
				//					// May as well allocate the extra to the last column
				//					Columns[count - 1].Width += (goalWidth - widthTotal);
				//				}
			}
			finally
			{
				if (fNeedResume)
					this.ResumeLayout();
				m_fInAdjustWidth = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.SizeChanged"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);
		}

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

#if !__MonoCS__
		// Todo JohnT: this could be added to the many overloads of SendMessage in Win32Wrappers.
		[DllImport("user32", EntryPoint="SendMessage")]
		static extern IntPtr SendMessage2(IntPtr Handle, Int32 msg, IntPtr wParam, ref HDITEM lParam);

		// This possibly could also...though the current version of this overload is specified to return bool.
		[DllImport("user32")]
		static extern IntPtr SendMessage(IntPtr Handle, Int32 msg, IntPtr wParam, IntPtr lParam);
#endif

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

			if (columnIndex < 0 || columnIndex >= this.Columns.Count)
				return;
#if !__MonoCS__ // FWNX-131

			IntPtr hHeader = SendMessage(this.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);

			ColumnHeader colHdr = this.Columns[columnIndex];

			// Setting these icons happens during view startup BEFORE we restore the saved settings,
			// so saving the current ones overwrites the saved ones. Anyway, modifying the icon
			// doesn't modif the width, so doing anything about column widths would be a waste.
			bool oldSuppress = SuppressColumnWidthChanges;
			SuppressColumnWidthChanges = true;
			// the following commented out code uses the default visual style sort arrows,
			// we are not using them so that we can use different size arrows
			//if (Application.RenderWithVisualStyles)
			//{
			//    IntPtr colPtr = new IntPtr(columnIndex);
			//    HDITEM hd = new HDITEM();
			//    hd.mask = HDI_FORMAT;
			//    SendMessage2(hHeader, HDM_GETITEM, colPtr, ref hd);
			//    if (sortOrder != SortOrder.None)
			//    {
			//        switch (sortOrder)
			//        {
			//            case SortOrder.Ascending:
			//                hd.fmt &= ~HDF_SORTDOWN;
			//                hd.fmt |= HDF_SORTUP;
			//                break;

			//            case SortOrder.Descending:
			//                hd.fmt &= ~HDF_SORTUP;
			//                hd.fmt |= HDF_SORTDOWN;
			//                break;
			//        }
			//    }
			//    else
			//    {
			//        hd.fmt &= ~HDF_SORTDOWN & ~HDF_SORTUP;
			//    }

			//    SendMessage2(hHeader, HDM_SETITEM, colPtr, ref hd);
			//}
			//else
			//{
			HDITEM hd = new HDITEM();
			hd.mask = HDI_IMAGE | HDI_FORMAT;

			HorizontalAlignment align = colHdr.TextAlign;

			if (align == HorizontalAlignment.Left)
				hd.fmt = HDF_LEFT | HDF_STRING | HDF_BITMAP_ON_RIGHT;

			else if (align == HorizontalAlignment.Center)
				hd.fmt = HDF_CENTER | HDF_STRING | HDF_BITMAP_ON_RIGHT;

			else	// HorizontalAlignment.Right
				hd.fmt = HDF_RIGHT | HDF_STRING;

			if (sortOrder != SortOrder.None)
			{
				hd.fmt |= HDF_IMAGE;
			}

			if (sortOrder == SortOrder.None)
				hd.iImage = -1;
			else
			{
				// There are 3 potential sizes: Large, Medium, and Small with 2 sort orders, Ascending and Descending
				// The images are stored in that order, so the following works
				hd.iImage = (int)size + (3 * ((int)sortOrder - 1));
			}

			SendMessage2(hHeader, HDM_SETITEM, new IntPtr(columnIndex), ref hd);
			//}
			SuppressColumnWidthChanges = oldSuppress;

			Update();
#else // FWNX-131
			ColumnHeader colHdr = this.Columns[columnIndex];

			bool oldSuppress = SuppressColumnWidthChanges;
			SuppressColumnWidthChanges = true;

			if (sortOrder == SortOrder.None)
				colHdr.ImageIndex = -1;
			else
			{
				// There are 3 potential sizes: Large, Medium, and Small with 2 sort orders, Ascending and Descending
				// The images are stored in that order, so the following works
				colHdr.ImageIndex = (int)size + (3 * ((int)sortOrder - 1));
			}

			SuppressColumnWidthChanges = oldSuppress;

			Update();
#endif
		}

#if !__MonoCS__ // FWNX-131
		private void SetHeaderImageList(ImageList imgList)
		{
			IntPtr hHeader = SendMessage(this.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
			SendMessage(hHeader, HDM_SETIMAGELIST, IntPtr.Zero, imgList.Handle);
		}
#endif

		// Todo JohnT: These could possibly move to Win32Wrappers.
		[StructLayout(LayoutKind.Sequential)]
			private struct HDITEM
		{
			public Int32     mask;
			public Int32     cxy;
			[MarshalAs(UnmanagedType.LPTStr)]
			public String    pszText;
			public IntPtr	 hbm;
			public Int32     cchTextMax;
			public Int32     fmt;
			public Int32     lParam;
			public Int32     iImage;
			public Int32     iOrder;
		};

		const Int32 LVM_FIRST           = 0x1000;		// List messages
		const Int32 LVM_GETHEADER		= LVM_FIRST + 31;
		const Int32 LVM_GETCOLUMN		= LVM_FIRST + 95;
		const Int32 LVM_SETCOLUMNORDERARRAY = LVM_FIRST + 58;
		const Int32 LVM_GETCOLUMNORDERARRAY = LVM_FIRST + 59;

		const Int32 HDI_FORMAT			= 0x0004;
		const Int32 HDI_IMAGE			= 0x0020;

		const Int32 HDF_LEFT			= 0x0000;
		const Int32 HDF_RIGHT			= 0x0001;
		const Int32 HDF_CENTER			= 0x0002;
		const Int32 HDF_SORTDOWN		= 0x0200;
		const Int32 HDF_SORTUP			= 0x0400;
		const Int32 HDF_IMAGE			= 0x0800;
		const Int32 HDF_BITMAP_ON_RIGHT = 0x1000;
		const Int32 HDF_STRING			= 0x4000;

		const Int32 HDM_FIRST           = 0x1200;		// Header messages
		const Int32 HDM_SETIMAGELIST	= HDM_FIRST + 8;
		const Int32 HDM_GETITEM			= HDM_FIRST + 11;
		const Int32 HDM_SETITEM			= HDM_FIRST + 12;

		private void m_configDisplayTimer_Tick(object sender, EventArgs e)
		{
			m_configDisplayTimer.Stop();
			m_checkMarkButton.Invalidate();
		}

		private void m_checkMarkButton_Click(object sender, EventArgs e)
		{
			if (CheckIconClick != null)
				CheckIconClick(this, new ConfigIconClickEventArgs(
					this.RectangleToClient(m_checkMarkButton.RectangleToScreen(m_checkMarkButton.ClientRectangle))));

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
