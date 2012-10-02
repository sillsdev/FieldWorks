using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SidebarLibrary.Collections;
using SidebarLibrary.Win32;
using SidebarLibrary.General;

namespace SidebarLibrary.CommandBars
{
	/// <summary>
	/// Summary description for Rebar.
	/// </summary>
	public class ReBar : Control, IMessageFilter
	{
		RebarBandCollection bands = new RebarBandCollection();
		static bool needsColorUpdate = false;
		bool bGotIsCommonCtrl6 = false;
		bool isCommonCtrl6 = false;

		public ReBar()
		{
			SetStyle(ControlStyles.UserPaint, false);
			TabStop = false;
			Dock = DockStyle.Top;
			bands.Changed += new EventHandler(Bands_Changed);

		}

		private bool IsCommonCtrl6()
		{
			// Cache this value for efficenty
			if ( bGotIsCommonCtrl6 == false )
			{
				DLLVERSIONINFO dllVersion = new DLLVERSIONINFO();
				// We are assummng here that anything greater or equal than 6
				// will have the new XP theme drawing enable
				dllVersion.cbSize = Marshal.SizeOf(typeof(DLLVERSIONINFO));
				WindowsAPI.GetCommonControlDLLVersion(ref dllVersion);
				bGotIsCommonCtrl6 = true;
				isCommonCtrl6 = (dllVersion.dwMajorVersion >= 6);
			}
			return isCommonCtrl6;
		}

		public void UpdateBackgroundColor()
		{
			for ( int i = 0; i < bands.Count; i++ )
			{
				// Update Rebar band information
				// This make sure that the background color and foreground color
				// of the bands are set to the new system colors
				UpdateBand(i);
				ToolBarEx toolBar = (ToolBarEx)bands[i];
				toolBar.Invalidate();
			}
			Invalidate();
		}

		private void PaintBackground()
		{
			if ( needsColorUpdate)
			{
				needsColorUpdate = false;
				for ( int i = 0; i < bands.Count; i++ )
				{
					// Update toolbar specific information
					// This update is to guarantee that the toolbar can resize
					// the buttons appropiately in case the SystemMenuFont was
					// changed along with the system colors
					ToolBarEx toolBar = (ToolBarEx)bands[i];
					toolBar.UpdateToolBarItems();
				}
				for ( int i = 0; i < bands.Count; i++ )
				{
					// Update Rebar band information
					// This make sure that the background color and foreground color
					// of the bands are set to the new system colors
					UpdateBand(i);
				}
			}

			// We don't need to paint the gripper if we are going
			// to let the operating system do the painting
			if ( IsCommonCtrl6() )
				return;

			Control c = null;
			Rectangle rc;
			for ( int i = 0; i < bands.Count; i++ )
			{
				using (Graphics g = CreateGraphics())
				{
					c = bands[i];
					RectangleF rf = g.ClipBounds;
					rc = c.Bounds;
					if ( rf.Contains(rc) )
					{
						ToolBarEx toolBar = (ToolBarEx)bands[i];
						if ( toolBar.BarType == BarType.MenuBar )
						{
							// The menu bar height is smaller that the other toolbars
							// and if the menubar is in the same row with a toolbar that is bigger in height
							// the toolbar gripper will not be painted correctly if we use the actual height
							// of the menubar. Instead ajust the rectangle to compensate for the actual height
							// of the band
										Rectangle menuRect = GetBandRect(i);
							int offset = (menuRect.Height - rc.Height)/2-1;
							rc = new Rectangle(rc.Left, rc.Top-offset, menuRect.Width, menuRect.Height-2);
						}
						DrawGripper(g, rc);
					}
				}
			}
		}

		private void DrawGripper(Graphics g, Rectangle bounds)
		{
			bounds.X = bounds.Left - 7;
			bounds.Width = 7;

			g.FillRectangle(new SolidBrush(ColorUtil.VSNetControlColor), bounds);
			int nHeight = bounds.Height;
			for ( int i = 2; i < nHeight-1; i++)
			{
				if ( ColorUtil.UsingCustomColor )
					g.DrawLine(new Pen(ColorUtil.VSNetBorderColor, 1), bounds.Left, bounds.Top+i, bounds.Left+3, bounds.Top+i);
				else
					g.DrawLine(new Pen(SystemColors.ControlDark, 1), bounds.Left, bounds.Top+i, bounds.Left+3, bounds.Top+i);
				i++;
			}
		}

		protected bool HitGripper(MouseEventArgs e)
		{
			// Find out if we hit the gripper
			Point mousePoint = new Point(e.X, e.Y);
			Control c = null;
			Rectangle bounds;
			for ( int i = 0; i < bands.Count; i++ )
			{
				c = bands[i];
				bounds = c.Bounds;

				// adjust to gripper area
				bounds.X = bounds.Left - 7;
				bounds.Width = 7;

				if ( bounds.Contains(mousePoint) )
					return true;
			}
			return false;

		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if ( Capture )
				return;

			bool hit = HitGripper(e);
			if ( hit )
			{
				if ( ShowMoveCursor(e) )
					Cursor.Current = Cursors.SizeAll;
				else
					Cursor.Current = Cursors.SizeWE;
			}
			else
				Cursor.Current = Cursors.Default;
		}

		private bool ShowMoveCursor(MouseEventArgs e)
		{
			// Even though we can actually move the toolbars around always
			// sometimes it is more intuive to show the "Move" cursor depending
			// how many bars are in the same row that always showing the resize cursor
			Point mousePoint = new Point(e.X, e.Y);
			Control c = null;
			Rectangle bounds;
			for ( int i = 0; i < bands.Count; i++ )
			{
				c = bands[i];
				bounds = c.Bounds;

				// adjust to gripper area
				bounds.X = bounds.Left - 7;
				bounds.Width = 7;

				if ( bounds.Contains(mousePoint) )
				{
					if ( bounds.Left <= 5 )
					{   // The left value would be actually at least 2 if the toolbar
						// is on the edge of the main window as opossed to be somewhere in the middle of the
						// strip. The assumption here is that the gripper starts approximately 2 pixel from the edge
						return true;
					}
				}
			}

			return false;
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			bool hit = HitGripper(e);
			if ( hit )
			{
					Capture = true;
				if ( ShowMoveCursor(e) )
					Cursor.Current = Cursors.SizeAll;
				else
					Cursor.Current = Cursors.VSplit;
			}
			else
				Cursor.Current = Cursors.Default;

		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			bool hit = HitGripper(e);
			Capture = false;
			if ( hit )
			{
				if ( ShowMoveCursor(e) )
					Cursor.Current = Cursors.SizeAll;
				else
					Cursor.Current = Cursors.SizeWE;
			}
			else
				Cursor.Current = Cursors.Default;

		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			bands.Changed -= new EventHandler(Bands_Changed);
		}

		public RebarBandCollection Bands
		{
			get { return bands; }
		}

		public override bool PreProcessMessage(ref Message msg)
		{
			foreach (Control band in bands)
			{
				if (band.PreProcessMessage(ref msg))
					return true;
			}
			return false;
		}

		protected override void OnParentChanged(EventArgs e)
		{
			if (Parent != null)
				Application.AddMessageFilter(this);
			else
				Application.RemoveMessageFilter(this);
		}

		protected override Size DefaultSize
		{
			get { return new Size(100, 22 * 2); }
		}

		protected override void CreateHandle()
		{
			if (!RecreatingHandle)
			{
				INITCOMMONCONTROLSEX icex = new INITCOMMONCONTROLSEX();
				icex.dwSize = Marshal.SizeOf(typeof(INITCOMMONCONTROLSEX));
				icex.dwICC = (int)(CommonControlInitFlags.ICC_BAR_CLASSES | CommonControlInitFlags.ICC_COOL_CLASSES);
				WindowsAPI.InitCommonControlsEx(icex);
			}
			base.CreateHandle();
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ClassName = WindowsAPI.REBARCLASSNAME;
				createParams.Style = (int)(WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE
					| WindowStyles.WS_CLIPCHILDREN | WindowStyles.WS_CLIPSIBLINGS);
				createParams.Style |= (int)(CommonControlStyles.CCS_NODIVIDER | CommonControlStyles.CCS_NOPARENTALIGN);

				createParams.Style |= (int)(RebarStyles.RBS_VARHEIGHT | RebarStyles.RBS_AUTOSIZE);

				return createParams;
			}
		}

		static public void UpdateBandsColors(object sender, EventArgs e)
		{
			needsColorUpdate = true;
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			RealizeBands();
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			switch (m.Msg)
			{
				case (int)Msg.WM_PAINT:
					PaintBackground();
					break;
				case (int)Msg.WM_NOTIFY:
				case (int)((int)Msg.WM_NOTIFY + (int)Msg.WM_REFLECT):
				{
					NMHDR note = (NMHDR)m.GetLParam(typeof(NMHDR));
					switch (note.code)
					{
						case (int)RebarNotifications.RBN_HEIGHTCHANGE:
							UpdateSize();
							break;

						case (int)RebarNotifications.RBN_CHEVRONPUSHED:
							NotifyChevronPushed(ref m);
							break;

						case (int)RebarNotifications.RBN_CHILDSIZE:
							NotifyChildSize();
							break;
						case (int)NotificationMessages.NM_NCHITTEST:
							break;
					}
				}
					break;
			}
		}

		void NotifyChevronPushed(ref Message m)
		{
			NMREBARCHEVRON nrch = (NMREBARCHEVRON) m.GetLParam(typeof(NMREBARCHEVRON));
			REBARBANDINFO rb;
			int bandIndex = nrch.uBand;
			rb = GetRebarInfo(bandIndex);
			int actualIndex = rb.wID;
			Control band = (Control)bands[actualIndex];
			Point point = new Point(nrch.rc.left, nrch.rc.bottom);
			(band as IChevron).Show(this, point);

		}

		void NotifyChildSize()
		{
			for ( int i = 0; i < bands.Count; i++ )
			{
				// Update toolbar specific information
				// This update is to guarantee that the toolbar can resize
				// the buttons appropiately in case the SystemMenuFont was
				// changed along with the system colors
				ToolBarEx toolBar = (ToolBarEx)bands[i];
				toolBar.ToolbarSizeChanged();
			}
		}

		void BeginUpdate()
		{
			WindowsAPI.SendMessage(Handle, (int)Msg.WM_SETREDRAW, 0, 0);
		}

		void EndUpdate()
		{
			WindowsAPI.SendMessage(Handle, (int)Msg.WM_SETREDRAW, 1, 0);
		}

		bool IMessageFilter.PreFilterMessage(ref Message message)
		{
			ArrayList handles = new ArrayList();
			IntPtr handle = Handle;
			while (handle != IntPtr.Zero)
			{
				handles.Add(handle);
				handle = WindowsAPI.GetParent(handle);
			}

			handle = message.HWnd;
			while (handle != IntPtr.Zero)
			{
				//Msg currentMessage = (Msg)message.Msg;
				if (handles.Contains(handle))
					return PreProcessMessage(ref message);
				handle = WindowsAPI.GetParent(handle);
			}

			return false;
		}

		void RealizeBands()
		{
			ReleaseBands();
			BeginUpdate();

			for (int i = 0; i < bands.Count; i++)
			{
				REBARBANDINFO bandInfo = GetBandInfo(i);
				WindowsAPI.SendMessage(Handle, (int)RebarMessages.RB_INSERTBANDW, i, ref bandInfo);
			}

			UpdateSize();
			EndUpdate();
			CaptureBands();
		}

		void UpdateBand(int index)
		{
			if (!IsHandleCreated) return;

			BeginUpdate();

			// Make sure we get the right index according to the band position in the rebar
			// and not to the index in the toolbar collections which can or cannot match the order
			// in the rebar control
			int actualIndex = GetBandActualIndex(index);

			REBARBANDINFO rbbi = GetBandInfo(actualIndex);
			ToolBarEx tb = (ToolBarEx)bands[actualIndex];
			int idealSize = tb.GetIdealSize();
			rbbi.cxIdeal = idealSize;
			WindowsAPI.SendMessage(Handle, (int)RebarMessages.RB_SETBANDINFOW, index, ref rbbi);

			UpdateSize();
			EndUpdate();
		}

		int GetBandActualIndex(int bandIndex)
		{
			// This maps between the indexes in the band collection and the actual
			// indexes in the rebar that can actually change as the user moves
			// the bands around
			REBARBANDINFO rb;
			rb = GetRebarInfo(bandIndex);
			return rb.wID;
		}

		void UpdateSize()
		{
			Height = WindowsAPI.SendMessage(Handle, (int)RebarMessages.RB_GETBARHEIGHT, 0, 0) + 1;
		}

		public int GetRebarHeight()
		{
			int height = WindowsAPI.SendMessage(Handle, (int)RebarMessages.RB_GETBARHEIGHT, 0, 0) + 1;
			return height;
		}

		public Rectangle GetBandRect(int bandIndex)
		{
			RECT rect = new RECT();
			//int index = GetBandActualIndex(bandIndex);
			WindowsAPI.SendMessage(Handle, (int)RebarMessages.RB_GETRECT, bandIndex, ref rect);
			return new Rectangle(rect.left, rect.top, rect.right-rect.left, rect.bottom-rect.top);
		}


		REBARBANDINFO GetRebarInfo(int index)
		{
			REBARBANDINFO rbbi = new REBARBANDINFO();
			rbbi.cbSize = Marshal.SizeOf(typeof(REBARBANDINFO));
			rbbi.fMask = (int)(RebarInfoMask.RBBIM_ID|RebarInfoMask.RBBIM_IDEALSIZE);
			WindowsAPI.SendMessage(Handle, (int)RebarMessages.RB_GETBANDINFOW, index, ref rbbi);
			return rbbi;
		}

		REBARBANDINFO GetBandInfo(int index)
		{
			Control band = bands[index];
			REBARBANDINFO rbbi = new REBARBANDINFO();
			rbbi.cbSize = Marshal.SizeOf(typeof(REBARBANDINFO));

			if ( !IsCommonCtrl6() )
			{
				rbbi.fMask = (int)RebarInfoMask.RBBIM_COLORS;
				rbbi.clrBack = (int)ColorUtil.RGB(ColorUtil.VSNetControlColor.R,
					ColorUtil.VSNetControlColor.G, ColorUtil.VSNetControlColor.B);

				rbbi.clrFore = (int)ColorUtil.RGB(255,0,255);
			}

			rbbi.iImage = 0;
			rbbi.hbmBack = IntPtr.Zero;
			rbbi.lParam = 0;
			rbbi.cxHeader = 0;

			rbbi.fMask |= (int)RebarInfoMask.RBBIM_ID;
			rbbi.wID = index;

			if ((band.Text != null) && (band.Text != string.Empty))
			{
				rbbi.fMask |= (int)RebarInfoMask.RBBIM_TEXT;
				rbbi.lpText = Marshal.StringToHGlobalAnsi(band.Text);
				rbbi.cch = (band.Text == null) ? 0 : band.Text.Length;
			}

			rbbi.fMask |= (int)RebarInfoMask.RBBIM_STYLE;
			rbbi.fStyle = (int)(RebarStylesEx.RBBS_CHILDEDGE | RebarStylesEx.RBBS_FIXEDBMP | RebarStylesEx.RBBS_GRIPPERALWAYS);
			ToolBarEx tb = (ToolBarEx)band;
			if ( tb.UseNewRow == true)
				rbbi.fStyle |= (int)(RebarStylesEx.RBBS_BREAK);
			rbbi.fStyle |= (band is IChevron) ? (int)RebarStylesEx.RBBS_USECHEVRON : 0;

			rbbi.fMask |= (int)(RebarInfoMask.RBBIM_CHILD);
			rbbi.hwndChild = band.Handle;

			rbbi.fMask |= (int)(RebarInfoMask.RBBIM_CHILDSIZE);
			rbbi.cyMinChild = band.Height;
			rbbi.cxMinChild = 0;
			rbbi.cyChild = 0;
			rbbi.cyMaxChild = 0;
			rbbi.cyIntegral = 0;

			rbbi.fMask |= (int)(RebarInfoMask.RBBIM_SIZE);
			rbbi.cx = band.Width;
			rbbi.fMask |= (int)(RebarInfoMask.RBBIM_IDEALSIZE);
			rbbi.cxIdeal = band.Width;

			return rbbi;
		}

		void UpdateBands()
		{
			if (IsHandleCreated) RecreateHandle();
		}

		void Bands_Changed(Object s, EventArgs e)
		{
			UpdateBands();
		}

		void Band_HandleCreated(Object s, EventArgs e)
		{
			ReleaseBands();

			Control band = (Control) s;
			UpdateBand(bands.IndexOf(band));

			CaptureBands();
		}

		void Band_TextChanged(Object s, EventArgs e)
		{
			Control band = (Control) s;
			UpdateBand(bands.IndexOf(band));
		}

		void CaptureBands()
		{
			foreach (Control band in bands)
			{
				band.HandleCreated += new EventHandler(Band_HandleCreated);
				band.TextChanged += new EventHandler(Band_TextChanged);
			}
		}

		void ReleaseBands()
		{
			foreach (Control band in bands)
			{
				band.HandleCreated -= new EventHandler(Band_HandleCreated);
				band.TextChanged -= new EventHandler(Band_TextChanged);
			}
		}
	}

}
