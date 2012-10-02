using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;


using SidebarLibrary.General;
using SidebarLibrary.Win32;


namespace SidebarLibrary.WinControls
{

	#region Helper Classes
	internal class EditCtrlHook : System.Windows.Forms.NativeWindow
	{

		#region Class Variables
		ComboBoxBase comboBox = null;
		bool ignoreNextPaintMessage = false;
		#endregion

		#region Constructors
		public EditCtrlHook(ComboBoxBase cb)
		{
			comboBox = cb;
		}
		#endregion

		#region Overrides
		protected override  void WndProc(ref Message m)
		{
			Msg currentMessage = (Msg)m.Msg;

			switch(m.Msg)
			{
				case ((int)Msg.WM_PAINT):
					if ( ignoreNextPaintMessage )
					{
						ignoreNextPaintMessage = false;
						return;
					}
					if ( comboBox.forceUpdate &&  comboBox.DroppedDown == false )
					{
						comboBox.forceUpdate = false;
						comboBox.ForceUpdate();
					}

					if ( comboBox.Enabled == false )
					{
						// Let the edit control do its thing first
						base.WndProc(ref m);
						// This is going to generate another paint message
						// ignore it
						ignoreNextPaintMessage = true;
						DrawDisableState();
						return;
					}

					break;
				case ((int)Msg.WM_MOUSEMOVE):
					RequestMouseLeaveMessage(m.HWnd);
					if ( comboBox.highlighted == false)
					{
						using (Graphics g = Graphics.FromHwnd(comboBox.Handle))
						{
							comboBox.DrawComboBoxBorder(g, ColorUtil.VSNetBorderColor);
							comboBox.DrawComboBoxArrowSelected(g, false);
						}
					}
					break;
				case ((int)Msg.WM_MOUSELEAVE):
					if ( comboBox.highlighted == true && !comboBox.ContainsFocus)
					{
						using (Graphics g = Graphics.FromHwnd(comboBox.Handle))
						{
							comboBox.DrawComboBoxBorder(g, SystemColors.Window);
							comboBox.DrawComboBoxArrowNormal(g, false);
						}
					}
					break;
				default:
					break;
			}

			base.WndProc(ref m);

		}

		#endregion

		#region Implementation
		void RequestMouseLeaveMessage(IntPtr hWnd)
		{
			// Crea the structure needed for WindowsAPI call
			Win32.TRACKMOUSEEVENTS tme = new Win32.TRACKMOUSEEVENTS();

			// Fill in the structure
			tme.cbSize = 16;
			tme.dwFlags = (uint)Win32.TrackerEventFlags.TME_LEAVE;
			tme.hWnd = hWnd;
			tme.dwHoverTime = 0;

			// Request that a message gets sent when mouse leaves this window
			WindowsAPI.TrackMouseEvent(ref tme);
		}

		void DrawDisableState()
		{
			// we are just going to fill the edit area
			// with a white background, the combobox
			// already does the hard part
			using (Graphics g = Graphics.FromHwnd(Handle))
			{
				RECT rc = new RECT();
				WindowsAPI.GetClientRect(Handle, ref rc);
				Rectangle clientSize = new Rectangle(rc.left, rc.top, rc.right-rc.left, rc.bottom-rc.top);
				g.FillRectangle(Brushes.White, clientSize);

				// Draw text
				STRINGBUFFER buffer;
				WindowsAPI.GetWindowText(Handle, out buffer, 512);
				using ( Brush b = new SolidBrush(SystemColors.ControlDark) )
				{
					string text = buffer.szText;
					Size textSize = TextUtil.GetTextSize(g, text, SystemInformation.MenuFont);
					Point pt = new Point(rc.left, rc.top + ( rc.bottom-textSize.Height )/2);
					g.DrawString(text, comboBox.Font, b, pt);
				}
			}
		}

		#endregion

	}

	#endregion

	/// <summary>
	/// Summary description for ComboBoxBase.
	/// </summary>
	[ToolboxItem(false)]
	public class ComboBoxBase : System.Windows.Forms.ComboBox
	{
		#region Class Variables
		private IntPtr mouseHookHandle = IntPtr.Zero;
		private GCHandle mouseProcHandle;
		private bool toolBarUse = false;
		private bool hooked = false;
		public const int ARROW_WIDTH = 12;
		private EditCtrlHook editHook = null;
		internal bool forceUpdate = false;
		internal bool highlighted = false;
		bool fireOnSelectedIndexChanged = true;
		#endregion

		#region Constructors
		// I wanted to make this constructor either protected
		// or internal so that the ComboBoxBase could not be used
		// as a stand alone class, however the IDE designer complains about it
		// if the constructors are not public
		public ComboBoxBase(bool toolBarUse)
		{
			// Flag to indicate that combo box will be used
			// in a toolbar --which means we will use some window hooks
			// to reset the focus of the combobox--
			this.toolBarUse = toolBarUse;
			DrawMode = DrawMode.OwnerDrawFixed;
			SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.DoubleBuffer, true);
			// Use Menu font so that the combobox uses the same font as the toolbar buttons
			Font = SystemInformation.MenuFont;
			// When use in a toolbar we don't need tab stop
			TabStop = false;
		 }

		// I wanted to make this constructor either protected
		// or internal so that the ComboBoxBase could not be used
		// as a stand alone class, however the IDE designer complains about it
		// if the constructors are not public
		public ComboBoxBase()
		{
			DrawMode = DrawMode.OwnerDrawFixed;
			SetStyle(ControlStyles.AllPaintingInWmPaint
				|ControlStyles.UserPaint|ControlStyles.Opaque|ControlStyles.DoubleBuffer, true);
		}

		#endregion

		#region Overrides
		protected override void OnPaint(PaintEventArgs pe)
		{
			// This on paint is only going to happen for the combobox if
			// the combobox has been set the style to do all painting
			// in the OnPaint event
			base.OnPaint(pe);
			if ( DropDownStyle == ComboBoxStyle.DropDown )
			{
				// We will handle the painting from WM_PAINT
				return;
			}

			if (Enabled == false)
			{
				DrawDisableState();
				return;
			}

			PaintComboBoxBackground(pe.Graphics, SystemColors.Window);
			Rectangle rc = ClientRectangle;
			if ( SelectedIndex == -1 )
			{
				if ( Items.Count > 0 )
				{
					// Select first item as the current item
					fireOnSelectedIndexChanged  = false;
					SelectedIndex = 0;
				}
			}

			DrawComboBoxItemEx( pe.Graphics, rc, SelectedIndex, false, true);
			if ( !ContainsFocus )
			{
				DrawComboBoxBorder(pe.Graphics, SystemColors.Window);
				DrawComboBoxArrowNormal(pe.Graphics, true);
			}
			else
			{
				DrawComboBoxBorder(pe.Graphics, ColorUtil.VSNetBorderColor);
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			// Hook the edit control
			if ( DropDownStyle == ComboBoxStyle.DropDown )
			{
				IntPtr hEditControl = WindowsAPI.GetDlgItem(Handle, 0x3E9);
				Debug.Assert(hEditControl != IntPtr.Zero, "Fail to get ComboBox's Edit Control Handle...");
				editHook = new EditCtrlHook(this);
				editHook.AssignHandle(hEditControl);
			}
		}

		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			// Draw bitmap strech to the size of the size of the combobox
			Graphics g = e.Graphics;
			Rectangle bounds = e.Bounds;
			bool selected = (e.State & DrawItemState.Selected) > 0;
			bool editSel = (e.State & DrawItemState.ComboBoxEdit ) > 0;
			if ( e.Index != -1 )
				DrawComboBoxItem(g, bounds, e.Index, selected, editSel);
		}

		protected override void WndProc(ref Message m)
		{
			bool doPainting =  false;
			switch(m.Msg)
			{
				case (int)Msg.WM_PAINT:
					doPainting = true;
					break;
				default:
					break;
			}

			base.WndProc(ref m);

			// Now let's do our own painting
			// we have to do it after the combox
			// does its own painting so that we can
			// let the edit control in the combobox
			// take care of the text
			if ( doPainting )
				ForcePaint(ref m);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			Graphics g = CreateGraphics();
			DrawComboBoxBorder(g,ColorUtil.VSNetBorderColor);
			DrawComboBoxArrowSelected(g, false);
			g.Dispose();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			if ( !ContainsFocus )
			{
				Graphics g = CreateGraphics();
				DrawComboBoxBorder(g, SystemColors.Window);
				DrawComboBoxArrowNormal(g, false);
				g.Dispose();
			}
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			Graphics g = CreateGraphics();
			DrawComboBoxBorder(g, SystemColors.Window);
			DrawComboBoxArrowNormal(g, false);
			g.Dispose();
			if ( toolBarUse && hooked)
			{
				hooked = false;
				EndHook();
			}
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			Graphics g = CreateGraphics();
			DrawComboBoxBorder(g, ColorUtil.VSNetBorderColor);
			DrawComboBoxArrowSelected(g, false);
			g.Dispose();
			if ( toolBarUse && !hooked)
			{
				hooked = true;
				StartHook();
			}
		}

		protected override void OnSelectedIndexChanged(EventArgs e)
		{
			if ( fireOnSelectedIndexChanged )
			{
				base.OnSelectedIndexChanged(e);
			}

			fireOnSelectedIndexChanged = true;

		}

		#endregion

		#region Properties
		public bool ToolBarUse
		{
			set { toolBarUse = value; }
			get { return toolBarUse; }
		}
		#endregion

		#region Methods
		public void SetFontHeight(int newHeight)
		{
			FontHeight = newHeight;
		}

		#endregion

		#region Implementation
		void PaintComboBoxBackground(Graphics g, Color backColor)
		{
			Rectangle rc = ClientRectangle;
			rc.Inflate(-1, -1);
			g.FillRectangle(new SolidBrush(backColor), rc);
		}

		internal void DrawComboBoxBorder(Graphics g, Color color)
		{
			// Keep track of what we painted last
			if ( color.Equals(ColorUtil.VSNetBorderColor) )
			{
				highlighted =  true;
			}
			else
			{
				highlighted = false;
			}

			Pen pen = new Pen(new SolidBrush(color), 1);
			g.DrawRectangle(pen, ClientRectangle.Left, ClientRectangle.Top, ClientRectangle.Width-1, ClientRectangle.Height-1);

			// We need to draw an extra "inner" border to erase the ugly 3D look of  the combobox
			g.DrawRectangle(Pens.White, ClientRectangle.Left+1, ClientRectangle.Top+1,
				ClientRectangle.Width-SystemInformation.VerticalScrollBarWidth, ClientRectangle.Height-3);


		}

		internal void DrawComboBoxArrowNormal(Graphics g, bool disable)
		{
			int left, top, arrowWidth, height;
			CalculateArrowBoxCoordinates(out left, out top, out arrowWidth, out height);

			// We are not going to draw the arrow background using the total
			// width of the arrow button in the combobox because it is too wide
			// and it does not look nice. However, we need to paint over the section
			// that correspond to the "original" arrow button dimension to avoid
			// clipping or painting problems
			Brush stripeColorBrush = new SolidBrush(ColorUtil.VSNetControlColor);
			if ( Enabled )
			{
				int width = SystemInformation.VerticalScrollBarWidth - ARROW_WIDTH;
				g.FillRectangle(Brushes.White, new Rectangle(left-width, top,
					SystemInformation.VerticalScrollBarWidth, height));
			}

			if ( !disable)
			{
				// Erase previous selected rectangle first
				DrawComboBoxArrowSelected(g, true);
				// Now draw the unselected background
				g.FillRectangle(stripeColorBrush, left, top, arrowWidth, height);
			}
			else
			{
				// Now draw the unselected background
				g.FillRectangle(stripeColorBrush, left-1, top-1, arrowWidth+2, height+2);
			}

			DrawArrowGlyph(g, disable);

		}

		internal void DrawComboBoxArrowSelected(Graphics g, bool erase)
		{

			int left, top, arrowWidth, height;
			CalculateArrowBoxCoordinates(out left, out top, out arrowWidth, out height);

			// We are not going to draw the arrow background using the total
			// width of the arrow button in the combobox because it too wide
			// and it does not look nice. However, we need to paint over the section
			// that correspond to the "original" arrow button dimension to avoid
			// clipping or painting problems
			if ( Enabled )
			{
				int width = SystemInformation.VerticalScrollBarWidth - ARROW_WIDTH;
				g.FillRectangle(Brushes.White, new Rectangle(left-width, top,
					SystemInformation.VerticalScrollBarWidth, height));
			}

			if ( !erase )
			{
				if (DroppedDown)
				{
					// If showing the list portion of the combo box, draw the arrow portion background using
					// the highlight color with some transparency
					// Don't use the graphics object that we get passed because that is associated
					// to the edit control of the combobox and we actually want to paint on the combobox area
					// and not be clipped only to the edit control client area
					Graphics cbg = CreateGraphics();
					cbg.FillRectangle(new SolidBrush(ColorUtil.VSNetPressedColor), left-1, top-1, arrowWidth+2, height+2);
					cbg.DrawRectangle(new Pen( new SolidBrush(ColorUtil.VSNetBorderColor), 1), left-1, top-1, arrowWidth+2, height+3);
					// The DrawArrow won't work with the passed Graphics object since it is the one for
					// the edit control and we need to paint on the combobox
					DrawArrowGlyph(cbg, false);
					cbg.Dispose();
					forceUpdate = true;
					return;
				}
				else
				{
					g.FillRectangle(new SolidBrush(ColorUtil.VSNetSelectionColor), left-1, top-1, arrowWidth+2, height+2);
					g.DrawRectangle(new Pen( new SolidBrush(ColorUtil.VSNetBorderColor), 1), left-1, top-2, arrowWidth+2, height+3);
				}

			}
			else
			{
				g.FillRectangle(Brushes.White, left-1, top-1, arrowWidth+2, height+2);
			}

			DrawArrowGlyph(g, false);

		}

		void CalculateArrowBoxCoordinates(out int left, out int top, out int width, out int height)
		{
			Rectangle rc = ClientRectangle;
			width = ARROW_WIDTH;
			left = rc.Right - width - 2;
			top = rc.Top + 2;
			height = rc.Height - 4;
		}

		void DrawArrowGlyph(Graphics g, bool disable)
		{
			int left, top, arrowWidth, height;
			CalculateArrowBoxCoordinates(out left, out top, out arrowWidth, out height);

			// Draw arrow glyph
			Point[] pts = new Point[3];
			pts[0] = new Point(left + arrowWidth/2 - 2, top + height/2-1);
			pts[1] = new Point(left + arrowWidth/2 + 3,  top + height/2-1);
			pts[2] = new Point(left + arrowWidth/2, (top + height/2-1) + 3);

			if ( disable )
			{
				g.FillPolygon(new SolidBrush(SystemColors.ControlDark), pts);
			}
			else
			{
				g.FillPolygon(Brushes.Black, pts);
			}
		}

		void ForcePaint(ref Message m)
		{
			// Similar code to the OnPaint handler
			Graphics g = Graphics.FromHwnd(Handle);
			if (Enabled == false)
			{
				DrawDisableState();
				return;
			}

			Rectangle rc = ClientRectangle;
			if ( SelectedIndex == -1 )
				if ( Items.Count > 0 )
				{
					fireOnSelectedIndexChanged = false;
					SelectedIndex = 0;
				}

			DrawComboBoxItemEx( g, rc, SelectedIndex, false, true);

			if ( !ContainsFocus )
			{
				DrawComboBoxBorder(g, SystemColors.Window);
				DrawComboBoxArrowNormal(g, false);
			}
			else
			{
				DrawComboBoxBorder(g, ColorUtil.VSNetBorderColor);
			}
		}

		internal void ForceUpdate()
		{
			using (	Graphics g = CreateGraphics())
			{
				if ( ContainsFocus )
					DrawComboBoxArrowSelected(g, false);
			}
		}

		void StartHook()
		{
			// Mouse hook
			WindowsAPI.HookProc mouseHookProc = new WindowsAPI.HookProc(MouseHook);
			mouseProcHandle = GCHandle.Alloc(mouseHookProc);
			mouseHookHandle = WindowsAPI.SetWindowsHookEx((int)WindowsHookCodes.WH_MOUSE,
				mouseHookProc, IntPtr.Zero, WindowsAPI.GetCurrentThreadId());
			if ( mouseHookHandle == IntPtr.Zero)
				throw new SecurityException();
		}

		void EndHook()
		{
			// Unhook
			WindowsAPI.UnhookWindowsHookEx(mouseHookHandle);
			mouseProcHandle.Free();
			mouseHookHandle = IntPtr.Zero;

		}

		IntPtr MouseHook(int code, IntPtr wparam, IntPtr lparam)
		{
			MOUSEHOOKSTRUCT mh = (MOUSEHOOKSTRUCT )Marshal.PtrToStructure(lparam, typeof(MOUSEHOOKSTRUCT));
			if ( mh.hwnd != Handle && !DroppedDown
				&& (wparam == (IntPtr)Msg.WM_LBUTTONDOWN || wparam == (IntPtr)Msg.WM_RBUTTONDOWN)
				|| wparam == (IntPtr)Msg.WM_NCLBUTTONDOWN )
			{
				// Loose focus
				WindowsAPI.SetFocus(IntPtr.Zero);
			}
			else if ( mh.hwnd != Handle && !DroppedDown
				&& (wparam == (IntPtr)Msg.WM_LBUTTONUP || wparam == (IntPtr)Msg.WM_RBUTTONUP)
				|| wparam == (IntPtr)Msg.WM_NCLBUTTONUP )
			{
				WindowsAPI.SetFocus(IntPtr.Zero);

			}
			return WindowsAPI.CallNextHookEx(mouseHookHandle, code, wparam, lparam);
		}

		protected virtual void DrawDisableState()
		{
			Graphics g = CreateGraphics();
			PaintComboBoxBackground(g, SystemColors.Window);
			DrawComboBoxBorder(g, SystemColors.ControlDark);
			DrawComboBoxArrowNormal(g, true);
			g.Dispose();
		}

		protected virtual void DrawComboBoxItem(Graphics g, Rectangle bounds, int Index, bool selected, bool editSel)
		{
			// Draw the the combo item
			using ( Brush b = new SolidBrush(SystemColors.Window) )
			{
				g.FillRectangle( b, bounds.Left, bounds.Top, bounds.Width, bounds.Height);
			}

			if ( selected && !editSel)
			{
				// Draw highlight rectangle
				Pen p1 = new Pen(ColorUtil.VSNetBorderColor);
				Brush b1 = new SolidBrush(ColorUtil.VSNetSelectionColor);
				g.FillRectangle(b1, bounds.Left, bounds.Top, bounds.Width, bounds.Height);
				g.DrawRectangle(p1, bounds.Left, bounds.Top, bounds.Width-1, bounds.Height-1);
				p1.Dispose();
				b1.Dispose();
			}
			else
			{
				// Erase highlight rectangle
				g.FillRectangle(new SolidBrush(SystemColors.Window), bounds.Left, bounds.Top, bounds.Width, bounds.Height);

				if ( editSel && ContainsFocus )
				{
					// Draw higlighted arrow
					Debug.WriteLine("Drawing ComboBox Arrow...");
					DrawComboBoxArrowSelected(g, false);
				}
			}
		}

		protected virtual void DrawComboBoxItemEx(Graphics g, Rectangle bounds, int Index, bool selected, bool editSel)
		{
			// This function is only called form the OnPaint handler and the Graphics object passed is the one
			// for the combobox itself as opossed to the one for the edit control in the combobox
			// doing this allows us to be able to avoid clipping problems with text strings

			// Draw the the combo item
			bounds.Inflate(-3, -3);
			using ( Brush b = new SolidBrush(SystemColors.Window) )
			{
				g.FillRectangle( b, bounds.Left, bounds.Top, bounds.Width, bounds.Height);
			}

			if ( selected && !editSel)
			{
				// Draw highlight rectangle
				Pen p1 = new Pen(ColorUtil.VSNetBorderColor);
				Brush b1 = new SolidBrush(ColorUtil.VSNetSelectionColor);
				g.FillRectangle(b1, bounds.Left, bounds.Top, bounds.Width, bounds.Height);
				g.DrawRectangle(p1, bounds.Left, bounds.Top, bounds.Width-1, bounds.Height-1);
				p1.Dispose();
				b1.Dispose();
			}
			else
			{
				// Erase highlight rectangle
				g.FillRectangle(new SolidBrush(SystemColors.Window), bounds.Left, bounds.Top, bounds.Width, bounds.Height);
				if ( editSel && ContainsFocus )
				{
					// Draw higlighted arrow
					DrawComboBoxArrowSelected(g, false);
				}
			}
		}

		#endregion

	}

}
