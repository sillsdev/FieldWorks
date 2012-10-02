using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Text;
using System.Diagnostics;
using System.Security;
using SidebarLibrary.Collections;
using SidebarLibrary.Win32;
using SidebarLibrary.General;
using SidebarLibrary.WinControls;
using SidebarLibrary.Menus;

namespace SidebarLibrary.CommandBars
{

	public enum BarType
	{
		ToolBar = 0,
		MenuBar = 1
	}

	/// <summary>
	/// Summary description for ToolBarEx.
	/// </summary>
	public class ToolBarEx : System.Windows.Forms.Control, IChevron
	{

		ToolBarItemCollection items = new ToolBarItemCollection();
		ToolBarItem[] handledItems = new ToolBarItem[0];
		bool[] handledItemsVisible = new bool[0];
		ChevronMenu chevronMenu = new ChevronMenu();
		BarType barType = BarType.ToolBar;
		const int DROWPDOWN_ARROW_WIDTH = 14;
		const int MARGIN = 3;
		const int MENUTEXT_MARGIN = 8;
		ImageList imageList = null;
		bool bGotIsCommonCtrl6 = false;
		bool isCommonCtrl6 = false;

		// To be used only when we have a menubar type
		enum State
		{
			None,
			Hot,
			HotTracking
		}
		State state = State.None;
		// State lastState = State.None;
		Point lastMousePosition = new Point(0, 0);
		int trackHotItem = -1;
		int trackNextItem = -1;
		bool trackEscapePressed = false;
		IntPtr hookHandle = IntPtr.Zero;
		bool doKeyboardSelect = false;
		bool useNewRow = true;

		public ToolBarEx()
		{
			InitializeToolBar();
		}

		public ToolBarEx(bool useNewRow)
		{
			InitializeToolBar();
			this.useNewRow = useNewRow;
		}

		public ToolBarEx(BarType type)
		{
			barType = type;
			InitializeToolBar();
		}

		public ToolBarEx(BarType type, bool useNewRow)
		{
			barType = type;
			InitializeToolBar();
			this.useNewRow = useNewRow;
		}

		private void InitializeToolBar()
		{
			// We'll let the toolbar to send us messages for drawing
			SetStyle(ControlStyles.UserPaint, false);
			TabStop = false;
			// Always on top
			Dock = DockStyle.Top;
			Attach();
		}


		/// <summary>
		/// Override base implementation, to detach the object.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
				return;

			Detach();
			base.Dispose (disposing);
		}

		void Attach()
		{
			items.Changed += new EventHandler(Items_Changed);

			int count = Items.Count;
			handledItems = new ToolBarItem[count];
			handledItemsVisible = new bool[count];

			for (int i = 0; i < count; i++)
			{
				ToolBarItem item = Items[i];
				item.Changed += new EventHandler(Item_Changed);
				handledItems[i] = item;
				handledItemsVisible[i] = item.Visible;
			}
		}

		void Detach()
		{
			foreach (ToolBarItem item in handledItems)
			{
				item.Changed -= new EventHandler(Item_Changed);
			}

			handledItems = null;
			handledItemsVisible = null;
			items.Changed -= new EventHandler(Items_Changed);

		}

		public bool UseNewRow
		{
			get { return useNewRow; }
		}

		public ToolBarItemCollection Items
		{
			get { return items; }
		}

		protected override Size DefaultSize
		{
			get { return new Size(1, 1); }
		}

		public BarType BarType
		{
			get { return barType; }

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


		protected override void CreateHandle()
		{
			// Make sure common control library initilizes toolbars and rebars
			if ( !RecreatingHandle )
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
				createParams.ClassName = WindowsAPI.TOOLBARCLASSNAME;
				createParams.ExStyle = 0;
				// Windows specific flags
				createParams.Style = (int)(WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE |
					WindowStyles.WS_CLIPCHILDREN | WindowStyles.WS_CLIPSIBLINGS);
				// Common Control specific flags
				createParams.Style |= (int)(CommonControlStyles.CCS_NODIVIDER | CommonControlStyles.CCS_NORESIZE |
					CommonControlStyles.CCS_NOPARENTALIGN);
				// ToolBar specific flags
				createParams.Style |= (int)(ToolBarStyles.TBSTYLE_TOOLTIPS | ToolBarStyles.TBSTYLE_FLAT | ToolBarStyles.TBSTYLE_TRANSPARENT);
				if (HasText()) createParams.Style |= (int)ToolBarStyles.TBSTYLE_LIST;

				return createParams;
			}
		}

		private bool HasText()
		{

			for (int i = 0; i < items.Count; i++)
			{
				// check if we need to make this toolbar TBSTYLE_LIST
				if ( items[i].Text != null && items[i].Text != string.Empty )
					return true;
			}
			return false;

		}

		protected override void OnHandleCreated(EventArgs e)
		{
			// Send message needed for the toolbar to work properly before any other messages are sent
			WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_BUTTONSTRUCTSIZE, Marshal.SizeOf(typeof(TBBUTTON)), 0);
			// Setup extended styles
			int extendedStyle = (int)(ToolBarExStyles.TBSTYLE_EX_HIDECLIPPEDBUTTONS |
				ToolBarExStyles.TBSTYLE_EX_DOUBLEBUFFER );
			if ( BarType == BarType.ToolBar ) extendedStyle |= (int)ToolBarExStyles.TBSTYLE_EX_DRAWDDARROWS;
			WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_SETEXTENDEDSTYLE, 0, extendedStyle);
			RealizeItems();
			base.OnHandleCreated(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if ( barType == BarType.MenuBar)
			{
				Point point = new Point(e.X, e.Y);
				if ( state == State.Hot )
				{
					int index = HitTest(point);
					if ((IsValid(index)) && ( point != lastMousePosition))
						SetHotItem(index);
					return;
				}
				lastMousePosition = point;
			}

			base.OnMouseMove(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if ( barType == BarType.MenuBar)
			{
				if ((e.Button == MouseButtons.Left) && (e.Clicks == 1))
				{
					Point point = new Point(e.X, e.Y);
					int index = HitTest(point);
					if (IsValid(index))
					{
						TrackDropDown(index);
						return;
					}
				}
			}

			base.OnMouseDown(e);
		}

		bool IsValid(int index)
		{
			int count = WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_BUTTONCOUNT, 0, 0);
			return ((index >= 0) && (index < count));
		}

		int HitTest(Point point)
		{
			POINT pt = new POINT();
			pt.x = point.X;
			pt.y = point.Y;
			int hit = WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_HITTEST, 0, ref pt);
			if (hit > 0)
			{
				point = PointToScreen(point);
				Rectangle bounds = RectangleToScreen(new Rectangle(0, 0, Width, Height));
				if ( !bounds.Contains(point) ) return -1;
			}
			return hit;
		}

		int GetNextItem(int index)
		{
			if (index == -1) throw new Exception();
			int count = WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_BUTTONCOUNT, 0, 0);
			index++;
			if (index >= count) index = 0;
			return index;
		}

		int GetPreviousItem(int index)
		{
			if (index == -1) throw new Exception();
			int count = WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_BUTTONCOUNT, 0, 0);
			index--;
			if (index < 0) index = count - 1;
			return index;
		}

//		int GetHotItemIndex()
//		{
//			return WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_GETHOTITEM, 0, 0);
//		}

		void SetHotItem(int index)
		{
			WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_SETHOTITEM, index, 0);
		}

		void TrackDropDown(int index)
		{

			while ( index >= 0 )
			{
				trackNextItem = -1;

				BeginUpdate();

				// Raise event
				ToolBarItem item = (ToolBarItem)items[index];
				item.RaiseDropDown();
				// Item state
				WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_PRESSBUTTON, index, -1);

				// Trick to get the first menu item selected
				if ( doKeyboardSelect )
				{
					WindowsAPI.PostMessage(Handle, (int)Msg.WM_KEYDOWN, (int) Keys.Down, 1);
					WindowsAPI.PostMessage(Handle, (int)Msg.WM_KEYUP, (int) Keys.Down, 1);
				}
				doKeyboardSelect = false;
				SetState(State.HotTracking, index);

				// Hook
				WindowsAPI.HookProc hookProc = new WindowsAPI.HookProc(DropDownHook);
				GCHandle hookProcHandle = GCHandle.Alloc(hookProc);
				hookHandle = WindowsAPI.SetWindowsHookEx((int)WindowsHookCodes.WH_MSGFILTER,
					hookProc, IntPtr.Zero, WindowsAPI.GetCurrentThreadId());
				if ( hookHandle == IntPtr.Zero ) throw new SecurityException();

				// Ask for position
				RECT rect = new RECT();
				WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_GETRECT, index, ref rect);
				Point position = new Point(rect.left, rect.bottom-1);

				EndUpdate();
				Update();
				CommandBarMenu menu = item.ToolBarItemMenu;
				if ( menu == null ) return;
				menu.Show(this, position);

				// Unhook
				WindowsAPI.UnhookWindowsHookEx(hookHandle);
				hookProcHandle.Free();
				hookHandle = IntPtr.Zero;

				// Item state
				WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_PRESSBUTTON, index, 0);
				SetState(trackEscapePressed ? State.Hot : State.None, index);

				index = trackNextItem;
			}
		}

		void TrackDropDownNext(int index)
		{
			if (index != trackHotItem)
			{
				WindowsAPI.PostMessage(Handle, (int)Msg.WM_CANCELMODE, 0, 0);
				trackNextItem = index;
			}
		}

		IntPtr DropDownHook(int code, IntPtr wparam, IntPtr lparam)
		{
			if (code == (int)MouseHookFilters.MSGF_MENU)
			{
				MSG msg = (MSG) Marshal.PtrToStructure(lparam, typeof(MSG));
				Message message = Message.Create(msg.hwnd, msg.message, msg.wParam, msg.lParam);
				if ( DropDownFilter(ref message) )
					return (IntPtr) 1;
			}
			return WindowsAPI.CallNextHookEx(hookHandle, code, wparam, lparam);
		}

		bool DropDownFilter(ref Message message)
		{
			if (state != State.HotTracking) throw new Exception();

			// comctl32 sometimes steals the hot item for unknown reasons.
			SetHotItem(trackHotItem);

			if (message.Msg == (int)Msg.WM_KEYDOWN)
			{
				Keys keyData = (Keys)(int) message.WParam | ModifierKeys;
				if ( keyData == Keys.Left || keyData == Keys.Right )
					doKeyboardSelect = true;

				if (keyData == Keys.Left)
				{
					TrackDropDownNext(GetPreviousItem(trackHotItem));
					return true;
				}

				// Only move right if there is no submenu on the current selected item.
				ToolBarItem item = items[trackHotItem];
				if ((keyData == Keys.Right) && ((item.ToolBarItemMenu.SelectedMenuItem == null)
					|| (item.ToolBarItemMenu.SelectedMenuItem.MenuItems.Count == 0)))
				{
					TrackDropDownNext(GetNextItem(trackHotItem));
					return true;
				}

				if (keyData == Keys.Escape)
				{
					trackEscapePressed = true;
				}
			}
			else if ((message.Msg == (int)Msg.WM_MOUSEMOVE) || (message.Msg == (int)Msg.WM_LBUTTONDOWN))
			{
				Point point = new Point(((int) message.LParam) & 0xffff, ((int) message.LParam) >> 16);
				point = this.PointToClient(point);

				if (message.Msg == (int)Msg.WM_MOUSEMOVE)
				{
					if (point != lastMousePosition)
					{
						int index = HitTest(point);
						if ((IsValid(index)) && (index != trackHotItem))
							TrackDropDownNext(index);
						lastMousePosition = point;
					}
				}
				else if (message.Msg == (int)Msg.WM_LBUTTONDOWN)
				{
					if (HitTest(point) == trackHotItem)
					{
						TrackDropDownNext(-1);
						return true;
					}
				}
			}

			return false;

		}

		void SetState(State state, int index)
		{
			if (this.state != state)
			{
				if (state == State.None)
					index = -1;

				SetHotItem(index);

				if (state == State.HotTracking)
				{
					trackEscapePressed = false;
					trackHotItem = index;
				}
			}

			//this.lastState = this.state;
			this.state = state;
		}

		void IChevron.Show(Control control, Point point)
		{
			ToolBarItemCollection chevronItems = new ToolBarItemCollection();
			Size size = ClientSize;
			int currentCount = 0;
			bool addItem = true;
			ToolBarItem lastItem;
			bool hasComboBox = false;

			for (int i = 0; i < items.Count; i++)
			{
				bool IsSeparator = false;
				RECT rect = new RECT();
				WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_GETITEMRECT, i, ref rect);
				if (rect.right > size.Width)
				{
					ToolBarItem item = items[i];
					if ( item.ComboBox != null )
						hasComboBox = true;
					IsSeparator = (item.Style == ToolBarItemStyle.Separator);
					if ( item.Visible )
						if ( (!IsSeparator ) || (chevronItems.Count != 0) )
						{
							// don't add it if previous item was a separator
							currentCount = chevronItems.Count;
							if ( currentCount > 0 )
							{
								lastItem = chevronItems[currentCount-1];
								if ( lastItem.Style == ToolBarItemStyle.Separator && IsSeparator )
								{
									addItem = false;
								}
							}

							if ( addItem )
								chevronItems.Add(item);
							addItem = true;
						}
				}
			}

			// Don't show a separator as the last item of the context menu
			int itemsCount = chevronItems.Count;
			if ( itemsCount > 0 )
			{
				lastItem = chevronItems[itemsCount-1];
				if ( lastItem.Style == ToolBarItemStyle.Separator )
					chevronItems.RemoveAt(itemsCount-1);
			}

			chevronMenu.Items = chevronItems;
			chevronMenu.Style = VisualStyle.IDE;
			chevronMenu.TrackPopup(control.PointToScreen(point));

			// Need to reparent the combobox to this toolbar in case
			// there was a combobox that was displayed by the popup menu
			if ( hasComboBox )
			{
				// Run the logic for combobox visibility before reposition it
				ToolbarSizeChanged();

				for (int i = 0; i < items.Count; i++)
				{
					ToolBarItem item = items[i];
					if ( item.Style == ToolBarItemStyle.ComboBox )
					{
						WindowsAPI.SetParent(item.ComboBox.Handle, Handle);
						ComboBoxBase cbb = (ComboBoxBase)item.ComboBox;
						cbb.ToolBarUse = true;
						UpdateItem(i);
						cbb.Invalidate();
					}
				}
			}
		}

		internal void ToolbarSizeChanged()
		{
			if ( BarType == BarType.MenuBar )
				return;

			// Make sure that comboboxes are either visible
			// or invisible depending wheater they are showing
			// all of the client area or they are partially hidden
			Size size = ClientSize;
			for (int i = 0; i < items.Count; i++)
			{
				ToolBarItem item = items[i];
				if ( item.Style == ToolBarItemStyle.ComboBox )
				{
					RECT rect = new RECT();
					WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_GETITEMRECT, i, ref rect);
					if (rect.right > size.Width)
						item.ComboBox.Visible = false;
					else
						item.ComboBox.Visible = true;
				}
			}
		}

		protected override void WndProc(ref Message message)
		{
			base.WndProc(ref message);
			int index = -1;
			ToolBarItem item = null;

			switch (message.Msg)
			{
				case (int)ReflectedMessages.OCM_COMMAND:
					index = (int) message.WParam & 0xFFFF;
					item = items[index];
					item.RaiseClick();
					base.WndProc(ref message);
					ResetMouseEventArgs();
					break;

				case (int)Msg.WM_MENUCHAR:
					MenuChar(ref message);
					break;

				case (int)Msg.WM_ERASEBKGND:
					break;

				case (int)Msg.WM_NOTIFY:
				case (int)ReflectedMessages.OCM_NOTIFY:
					NMHDR nm = (NMHDR) message.GetLParam(typeof(NMHDR));
					switch (nm.code)
					{
						case (int)ToolBarNotifications.TTN_NEEDTEXTA:
							NotifyNeedTextA(ref message);
							break;

						case (int)ToolBarNotifications.TTN_NEEDTEXTW:
							NotifyNeedTextW(ref message);
							break;

						case (int)ToolBarNotifications.TBN_QUERYINSERT:
							message.Result = (IntPtr) 1;
							break;

						case (int)ToolBarNotifications.TBN_DROPDOWN:
							NMTOOLBAR nmt = (NMTOOLBAR) message.GetLParam(typeof(NMTOOLBAR));
							index = nmt.iItem;
							item = items[index];
							item.Dropped = true;
							item.RaiseDropDown();
							break;

						case (int)NotificationMessages.NM_CUSTOMDRAW:
							NotifyCustomDraw(ref message);
							break;

						case (int)ToolBarNotifications.TBN_HOTITEMCHANGE:
							break;
					}
					break;
			}
		}

		void MenuChar(ref Message message)
		{

			ToolBarItem item = items[trackHotItem];
			Menu menu = item.ToolBarItemMenu.FindMenuItem(MenuItem.FindHandle, message.LParam);
			if (item.ToolBarItemMenu.Handle == message.LParam) menu = item.ToolBarItemMenu;

			if (menu != null)
			{
				char key = char.ToUpper((char) ((int) message.WParam & 0x0000FFFF));
				int index = 0;
				foreach (MenuItem menuItem in menu.MenuItems)
				{
					if ((menuItem != null) && (menuItem.OwnerDraw) && (menuItem.Mnemonic == key))
					{
						message.Result = (IntPtr) (((int)MenuCharReturnValues.MNC_EXECUTE << 16) | index);
						return;
					}

					if (menuItem.Visible) index++;
				}
			}
		}


		void NotifyNeedTextA(ref Message m)
		{
			TOOLTIPTEXTA ttt = (TOOLTIPTEXTA) m.GetLParam(typeof(TOOLTIPTEXTA));
			ToolBarItem item = (ToolBarItem) items[ttt.hdr.idFrom];
			string toolTip = item.ToolTip;
			if ( toolTip != null && toolTip != string.Empty )
			{
				ttt.szText = toolTip;
				ttt.hinst = IntPtr.Zero;
				if ( RightToLeft == RightToLeft.Yes ) ttt.uFlags |= (int)ToolTipFlags.TTF_RTLREADING;
				Marshal.StructureToPtr(ttt, m.LParam, true);
				m.Result = (IntPtr) 1;
			}
		}

		void NotifyNeedTextW(ref Message m)
		{
			if (Marshal.SystemDefaultCharSize != 2) return;

			// This code is a duplicate of NotifyNeedTextA
			TOOLTIPTEXT ttt = (TOOLTIPTEXT) m.GetLParam(typeof(TOOLTIPTEXT));
			ToolBarItem item = (ToolBarItem) items[ttt.hdr.idFrom];
			string toolTip = item.ToolTip;
			if ( toolTip != null && toolTip != string.Empty )
			{
				ttt.szText = toolTip;
				ttt.hinst = IntPtr.Zero;
				if (RightToLeft == RightToLeft.Yes) ttt.uFlags |= (int)ToolTipFlags.TTF_RTLREADING;
				Marshal.StructureToPtr(ttt, m.LParam, true);
				m.Result = (IntPtr) 1;
			}
		}

		void NotifyCustomDraw(ref Message m)
		{
			m.Result = (IntPtr) CustomDrawReturnFlags.CDRF_DODEFAULT;
			NMTBCUSTOMDRAW tbcd = (NMTBCUSTOMDRAW)m.GetLParam(typeof(NMTBCUSTOMDRAW));

			switch (tbcd.nmcd.dwDrawStage)
			{
				case (int)CustomDrawDrawStateFlags.CDDS_PREPAINT:
					// Tell toolbar control that we want to do the painting ourselves
					m.Result = (IntPtr) CustomDrawReturnFlags.CDRF_NOTIFYITEMDRAW;
					break;

				case (int)CustomDrawDrawStateFlags.CDDS_ITEMPREPAINT:
					// Do custom painting
					NotifyCustomDrawToolBar(ref m);
					break;
			}
		}

		void NotifyCustomDrawToolBar(ref Message m)
		{
			m.Result = (IntPtr) CustomDrawReturnFlags.CDRF_DODEFAULT;

			// See if use wants the VSNet look or let XP dictate
			// the toolbar look
			if ( IsCommonCtrl6() )
			{
				// Let the operating system do the drawing
				return;
			}

			NMTBCUSTOMDRAW tbcd = (NMTBCUSTOMDRAW) m.GetLParam(typeof(NMTBCUSTOMDRAW));
			RECT rc = tbcd.nmcd.rc;
			Rectangle rectangle = new Rectangle(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);

			using (Graphics g = Graphics.FromHdc(tbcd.nmcd.hdc))
			{
				int index = tbcd.nmcd.dwItemSpec;
				ToolBarItem item = items[index];

				if ( item.Style == ToolBarItemStyle.ComboBox )
				{
					// ComboBoxes paint themselves
					// the combox new size, after changing the combobox font,
					// does not get updated until later in the drawing logic
					// pick up the right size here and update the combobox position
					UpdateComboBoxPosition(index);
					m.Result = (IntPtr) CustomDrawReturnFlags.CDRF_SKIPDEFAULT;
					return;
				}

				bool hot = (bool)((tbcd.nmcd.uItemState & (int)CustomDrawItemStateFlags.CDIS_HOT) != 0);
				bool selected = (bool)((tbcd.nmcd.uItemState & (int)CustomDrawItemStateFlags.CDIS_SELECTED) != 0);
				bool disabled = (bool)((tbcd.nmcd.uItemState & (int)CustomDrawItemStateFlags.CDIS_DISABLED) != 0);
				string tempString = item.Text;
				bool hasText = (tempString != string.Empty && tempString != null);

				if (item.Checked)
				{
					if ( hot )
						g.FillRectangle(new SolidBrush(ColorUtil.VSNetPressedColor), rectangle);
					else
						g.FillRectangle(new SolidBrush(ColorUtil.VSNetCheckedColor), rectangle);
					g.DrawRectangle(new Pen(new SolidBrush(ColorUtil.VSNetBorderColor), 1),
						rectangle.Left, rectangle.Top, rectangle.Width-1, rectangle.Height-1);

				}
				else if (selected)
				{
					if ( item.Style == ToolBarItemStyle.DropDownButton )
					{
						// Draw background
						g.FillRectangle(new SolidBrush(ColorUtil.VSNetSelectionColor), rectangle);
						g.FillRectangle(new SolidBrush(ColorUtil.VSNetPressedColor), rectangle.Left, rectangle.Top,
							rectangle.Width-DROWPDOWN_ARROW_WIDTH+1, rectangle.Height);
						g.DrawRectangle(new Pen(new SolidBrush(ColorUtil.VSNetBorderColor), 1),
							rectangle.Left, rectangle.Top, rectangle.Width-1, rectangle.Height-1);
					}
					else
					{
						if ( barType == BarType.MenuBar)
						{
							g.FillRectangle(new SolidBrush(ColorUtil.VSNetControlColor), rectangle);
							if ( ColorUtil.UsingCustomColor )
							{
								// Use same color for both sides to make it look flat
								g.DrawRectangle(new Pen(ColorUtil.VSNetBorderColor),
									rectangle.Left, rectangle.Top, rectangle.Width-1, rectangle.Height-1);
							}
							else
							{
								ControlPaint.DrawBorder3D(g, rectangle.Left, rectangle.Top, rectangle.Width-1,
									rectangle.Height-1, Border3DStyle.Flat, Border3DSide.Top | Border3DSide.Left | Border3DSide.Right);
							}
						}
						else
						{
							g.FillRectangle(new SolidBrush(ColorUtil.VSNetPressedColor), rectangle);
							g.DrawRectangle(new Pen(new SolidBrush(ColorUtil.VSNetBorderColor), 1),
								rectangle.Left, rectangle.Top, rectangle.Width-1, rectangle.Height-1);
						}
					}


				}
				else if ( item.Style == ToolBarItemStyle.DropDownButton && item.Dropped )
				{
					g.FillRectangle(new SolidBrush(ColorUtil.VSNetControlColor), rectangle);
					g.DrawRectangle(new Pen(new SolidBrush(SystemColors.ControlDark), 1),
						rectangle.Left, rectangle.Top, rectangle.Width-1, rectangle.Height-1);
				}
				else if (hot)
				{
					g.FillRectangle(new SolidBrush(ColorUtil.VSNetSelectionColor), rectangle);
					g.DrawRectangle(new Pen(new SolidBrush(ColorUtil.VSNetBorderColor), 1),
						rectangle.Left, rectangle.Top, rectangle.Width-1, rectangle.Height-1);
				}
				else
				{
					if ( item.Style == ToolBarItemStyle.DropDownButton )
					{

						IntPtr hreBar = WindowsAPI.GetParent(Handle);
						IntPtr hMainWindow = IntPtr.Zero;
						bool mainHasFocus = false;
						if ( hreBar != IntPtr.Zero )
						{
							hMainWindow = WindowsAPI.GetParent(hreBar);
							if ( hMainWindow != IntPtr.Zero )
								mainHasFocus = ( hMainWindow == WindowsAPI.GetFocus());
						}

						if ( hMainWindow != IntPtr.Zero &&  mainHasFocus)
						{
							Point pos = Control.MousePosition;
							Point clientPoint = PointToClient(pos);
							if ( rectangle.Contains(clientPoint))
							{
								g.FillRectangle(new SolidBrush(ColorUtil.VSNetSelectionColor), rectangle);
								g.DrawRectangle(new Pen(new SolidBrush(ColorUtil.VSNetBorderColor), 1),
									rectangle.Left, rectangle.Top, rectangle.Width-1, rectangle.Height-1);

								rc.right -= DROWPDOWN_ARROW_WIDTH;
								g.DrawLine(new Pen(new SolidBrush(ColorUtil.VSNetBorderColor), 1),
									rc.right+1, rc.top, rc.right+1, rc.top + (rc.bottom-rc.top));
								rc.right += DROWPDOWN_ARROW_WIDTH;
							}
						}
					}
				}

				if ( item.Style == ToolBarItemStyle.DropDownButton )
				{
					DrawArrowGlyph(g, rectangle);

					// Draw line that separates the arrow from the button
					rc.right -= DROWPDOWN_ARROW_WIDTH;
					if ( hot && !item.Dropped )
						g.DrawLine(new Pen(new SolidBrush(ColorUtil.VSNetBorderColor), 1),
							rc.right+1, rc.top, rc.right+1, rc.top + (rc.bottom-rc.top));
					item.Dropped = false;

				}

				Image image = item.Image;
				if (image != null)
				{
					Size size = image.Size;
					Point point = new Point(rc.left + ((rc.right - rc.left - size.Width) / 2),
						rc.top + ((rc.bottom - rc.top - size.Height) / 2));
					if ( hasText ) point.X = rc.left + MARGIN;
					if ( disabled )
						ControlPaint.DrawImageDisabled(g, image, point.X, point.Y, ColorUtil.VSNetSelectionColor);
					else if ( hot && !selected && !item.Checked )
					{
						ControlPaint.DrawImageDisabled(g, image, point.X+1, point.Y, ColorUtil.VSNetSelectionColor);
						g.DrawImage(image, point.X, point.Y-1);
					}
					else
					{
						if ( item.Checked  )
						{
							if  ( !selected ) point.Y -= 1;
						}
						g.DrawImage(image, point.X, point.Y);
					}
				}

				// Draw Text
				if ( hasText )
				{
					string currentText = item.Text;
					int amperSandIndex = currentText.IndexOf('&');
					if ( barType == BarType.MenuBar && amperSandIndex != -1 )
						currentText = item.Text.Remove(amperSandIndex, 1);
					Size textSize = TextUtil.GetTextSize(g, currentText, SystemInformation.MenuFont);
					Point pos;
					if ( barType == BarType.MenuBar )
					{
						int offset = rc.left + ((rc.right - rc.left) - textSize.Width)/2;
						pos = new Point(offset, rc.top + ((rc.bottom - rc.top - textSize.Height) / 2));
					}
					else
					{
						pos = new Point(rc.left, rc.top + ((rc.bottom - rc.top - textSize.Height) / 2));
						if ( image != null )
						{
							pos.X = rc.left + MARGIN + image.Size.Width + MARGIN;
						}
					}

					using (StringFormat stringFormat = new StringFormat())
					{
						stringFormat.HotkeyPrefix = HotkeyPrefix.Show;
						g.DrawString(item.Text, SystemInformation.MenuFont, Brushes.Black, pos, stringFormat);
					}
				}

				m.Result = (IntPtr) CustomDrawReturnFlags.CDRF_SKIPDEFAULT;
			}
		}

		protected void DrawArrowGlyph(Graphics g, Rectangle rectangle)
		{
			// Draw arrow glyph
			Point[] pts = new Point[3];
			int leftEdge = rectangle.Left + (rectangle.Width-DROWPDOWN_ARROW_WIDTH+1);
			int middle = rectangle.Top + rectangle.Height/2-1;
			pts[0] = new Point(leftEdge + 4, middle);
			pts[1] = new Point(leftEdge + 9,  middle);
			pts[2] = new Point(leftEdge + 6, middle+3);
			g.FillPolygon(Brushes.Black, pts);

		}

		public override bool PreProcessMessage(ref Message message)
		{
			if (message.Msg == (int)Msg.WM_KEYDOWN || message.Msg == (int)Msg.WM_SYSKEYDOWN)
			{
				// Check for shortcuts in ToolBarItems in this toolbar
				Keys keys = (Keys)(int) message.WParam  | ModifierKeys;
				ToolBarItem shortcutHit = items[keys];
				if (shortcutHit != null && shortcutHit.Enabled )
				{
					shortcutHit.RaiseClick();
					return true;
				}

				// Check for shortcuts in the menuitems of the popup menu
				// currently being displayed
				if ( barType == BarType.MenuBar )
				{
					MenuItem hitItem = null;
					foreach ( ToolBarItem tbi in items )
					{
						hitItem = FindShortcutItem(tbi.MenuItems, keys);
						if ( hitItem != null)
							break;
					}
					if ( hitItem != null )
						hitItem.PerformClick();
				}

				//  Check if we have a mnemonic
				bool alt = ((keys & Keys.Alt) != 0);
				if ( alt )
				{
					Keys keyCode = keys & Keys.KeyCode;
					char key = (char)(int)keyCode;
					if ((Char.IsDigit(key) || (Char.IsLetter(key))))
					{
						ToolBarItem mnemonicsHit = items[key];
						if ( mnemonicsHit != null )
						{
							if ( barType == BarType.MenuBar )
								TrackDropDown(mnemonicsHit.Index);
							else
								mnemonicsHit.RaiseClick();
							return true;
						}
					}
				}
			}

			return false;
		}


		private MenuItem FindShortcutItem(MenuItem[] menuItems, Keys keys)
		{
			MenuItem resultItem = null;
			foreach (MenuItem item in menuItems )
			{
				if ( ((int)item.Shortcut == (int)keys) && (item.Enabled) && (item.Visible) )
					return item;
				else
				{
					resultItem =  FindShortcutItem(item.MenuItems, keys);
					if ( resultItem != null )
						break;
				}
			}
			return resultItem;
		}

		private MenuItem FindShortcutItem(Menu.MenuItemCollection collection, Keys keys)
		{
			//int count = collection.Count;
			foreach (MenuItem item in collection )
			{
				if ( ((int)item.Shortcut == (int)keys) && (item.Enabled) && (item.Visible) )
					return item;
				else
					return FindShortcutItem(item.MenuItems, keys);
			}
			return null;
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			UpdateItems();
		}

		void Items_Changed(Object s, EventArgs e)
		{
			UpdateItems();
		}

		void Item_Changed(Object s, EventArgs e)
		{
			ToolBarItem[] handledItems = this.handledItems;
			foreach (ToolBarItem item in handledItems)
			{
				if ( item == s )
				{
					if (item == null) return;
					int index = items.IndexOf(item);
					if (index == -1) return;
					UpdateItem(index);
				}
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


		public void UpdateToolBarItems()
		{

			UpdateImageList();
			for (int i = 0; i < items.Count; i++)
			{
				// The toolbar handle is going to be destroy to correctly update the toolbar
				// itself. We need to detach the toolbar as the parent of the comboboxes --otherwise the comboboxes
				// does not behave appropiately after we pull the plug on the parent
				// The combobox will again parented to the toolbar once the handle is recreated and when the
				// RealizeItem routine gets the information for the toolbaritems
				if ( items[i].Style == ToolBarItemStyle.ComboBox )
				{
					WindowsAPI.SetParent(items[i].ComboBox.Handle, IntPtr.Zero);
					items[i].ComboBox.Parent = null;
				}

			}
			UpdateItems();
		}

		void UpdateItems()
		{
			Detach();
			Attach();
			if (IsHandleCreated) RecreateHandle();
		}

		TBBUTTONINFO GetButtonInfo(int index)
		{
			ToolBarItem item = items[index];
			TBBUTTONINFO tbi = new TBBUTTONINFO();
			tbi.cbSize = Marshal.SizeOf(typeof(TBBUTTONINFO));
			tbi.dwMask = (int)(ToolBarButtonInfoFlags.TBIF_IMAGE | ToolBarButtonInfoFlags.TBIF_STATE |
				 ToolBarButtonInfoFlags.TBIF_STYLE | ToolBarButtonInfoFlags.TBIF_COMMAND | ToolBarButtonInfoFlags.TBIF_SIZE);
			tbi.idCommand = index;
			tbi.iImage = (int)ToolBarButtonInfoFlags.I_IMAGECALLBACK;
			tbi.fsState = 0;
			tbi.cx = 0;
			tbi.lParam = IntPtr.Zero;
			tbi.pszText = IntPtr.Zero;
			tbi.cchText = 0;

			if (item.Style == ToolBarItemStyle.ComboBox)
			{
				tbi.fsStyle = (int)(ToolBarButtonStyles.TBSTYLE_BUTTON);
				tbi.cx = (short)item.ComboBox.Width;
				WindowsAPI.SetParent(item.ComboBox.Handle, Handle);
			}
			else if (item.Text != null && item.Text != string.Empty)
			{
				tbi.fsStyle = (int)(ToolBarButtonStyles.TBSTYLE_BUTTON);
				tbi.cx = MARGIN;
				if (item.Image != null)
					tbi.cx += (short)(item.Image.Size.Width + MARGIN);
				if (item.Style == ToolBarItemStyle.DropDownButton)
					tbi.cx += DROWPDOWN_ARROW_WIDTH;
				using (Graphics g = CreateGraphics())
				{
					string currentText = item.Text;
					int amperSandIndex = currentText.IndexOf('&');
					if (amperSandIndex != -1)
						currentText = item.Text.Remove(amperSandIndex, 1);
					Size size = TextUtil.GetTextSize(g, currentText, SystemInformation.MenuFont);

					if ( barType == BarType.MenuBar)
					{
						tbi.cx += (short)(size.Width + 2*MENUTEXT_MARGIN);
					}
					else
						tbi.cx += (short)(size.Width + 2*MARGIN);

					tbi.dwMask |= (int)ToolBarButtonInfoFlags.TBIF_TEXT;
					tbi.pszText = Marshal.StringToHGlobalAuto(item.Text + "\0");
					tbi.cchText = item.Text.Length;

					if (  IsCommonCtrl6() && barType != BarType.MenuBar )
					{
						// If we let the operating system do the drawing
						// the DROWPDOWN_ARROW_WIDTH is slightly bigger than
						// the value we are using add some padding to compensate
						tbi.cx += 6;
					}
				}
			}
			else
			{
				tbi.fsStyle = (int)(ToolBarButtonStyles.TBSTYLE_BUTTON | ToolBarButtonStyles.TBSTYLE_AUTOSIZE );
				tbi.cx = 0;
			}

			if (!item.Visible)
				tbi.fsState |= (int)ToolBarButtonStates.TBSTATE_HIDDEN;

			if (item.Style == ToolBarItemStyle.Separator)
			{
				tbi.fsStyle |= (int)ToolBarButtonStyles.TBSTYLE_SEP;
			}
			else
			{
				if (item.Enabled)
					tbi.fsState |= (int)ToolBarButtonStates.TBSTATE_ENABLED;

				if ( item.Style == ToolBarItemStyle.DropDownButton )
					tbi.fsStyle |= (int)ToolBarButtonStyles.TBSTYLE_DROPDOWN;

				if (item.Style == ToolBarItemStyle.PushButton)
					if (item.Checked)
						tbi.fsState |= (int)ToolBarButtonStates.TBSTATE_CHECKED;
			}

			if (item.Style == ToolBarItemStyle.Separator )
				tbi.iImage = (int)ToolBarButtonInfoFlags.I_IMAGENONE;
			else if (item.Image != null)
				tbi.iImage = index;

			return tbi;
		}

		void RealizeItems()
		{

			UpdateImageList();

			for (int i = 0; i < items.Count; i++)
			{
				items[i].Index = i;
				items[i].ToolBar = this;
				TBBUTTON button = new TBBUTTON();
				button.idCommand = i;
				WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_INSERTBUTTON, i, ref button);

				TBBUTTONINFO tbi = GetButtonInfo(i);
				WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_SETBUTTONINFOW, i, ref tbi);
				if ( items[i].Style == ToolBarItemStyle.ComboBox ) UpdateComboBoxPosition(i);
			}

			UpdateSize();
		}

		void UpdateComboBoxPosition(int index)
		{
			RECT rect = new RECT();
			WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_GETRECT, index, ref rect);
			int rectHeight = (rect.bottom-rect.top);
			int cbHeight = Items[index].ComboBox.Bounds.Height;
			int topOffset = rect.top+(rectHeight-cbHeight)/2;
			Items[index].ComboBox.Bounds = new Rectangle(rect.left+1, topOffset,
					(rect.right-rect.left)-2, cbHeight);

		}


		void UpdateImageList()
		{
			Size size = new Size(16, SystemInformation.MenuFont.Height);
			for (int i = 0; i < items.Count; i++)
			{
				Image image = items[i].Image;
				if (image != null)
				{
					if (image.Width > size.Width) size.Width = image.Width;
					if (image.Height > size.Height) size.Height = image.Height;
				}
			}

			imageList = new ImageList();
			imageList.ImageSize = size;
			imageList.ColorDepth = ColorDepth.Depth32Bit;

			for (int i = 0; i < items.Count; i++)
			{
				// Take combobox size into consideration too
				if ( items[i].Style == ToolBarItemStyle.ComboBox )
				{
					// update combobox to use the current system menu font
					items[i].ComboBox.Font = SystemInformation.MenuFont;
					ComboBoxBase cbb = (ComboBoxBase)items[i].ComboBox;
					int decrease = 2;
					if ( SystemInformation.MenuFont.Height >= 20)
						decrease = 5;
					cbb.SetFontHeight(SystemInformation.MenuFont.Height-decrease);

				}

				Image image = items[i].Image;
				imageList.Images.Add((image != null) ? image : new Bitmap(size.Width, size.Height));
			}

			IntPtr handle = (BarType == BarType.MenuBar) ? IntPtr.Zero : imageList.Handle;
			WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_SETIMAGELIST, 0, handle);
		}


		void UpdateSize()
		{
			Size size = new Size(0, 0);
			for (int i = 0; i < items.Count; i++)
			{
				RECT rect = new RECT();
				WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_GETRECT, i, ref rect);
				int height = rect.bottom - rect.top;
				if (height > size.Height) size.Height = height;
				size.Width += rect.right - rect.left;
			}
			Size = size;
		}

		public int GetIdealSize()
		{
			Size size = new Size(0, 0);
			for (int i = 0; i < items.Count; i++)
			{
				RECT rect = new RECT();
				WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_GETRECT, i, ref rect);
				int height = rect.bottom - rect.top;
				if (height > size.Height) size.Height = height;
				size.Width += rect.right - rect.left;
			}
			return size.Width;
		}

		void UpdateItem(int index)
		{
			if (!IsHandleCreated) return;

			if ( items[index].Visible == handledItemsVisible[index] )
			{
				TBBUTTONINFO tbi = GetButtonInfo(index);
				WindowsAPI.SendMessage(Handle, (int)ToolBarMessages.TB_SETBUTTONINFOW, index, ref tbi);
				if ( items[index].Style == ToolBarItemStyle.ComboBox ) UpdateComboBoxPosition(index);
			}
			else
			{
				UpdateItems();
			}
		}
	}

	public interface IChevron
	{
		void Show(Control control, Point point);
	}

}
