// ---------------------------------------------------------
// Windows Forms CommandBar Control
// Copyright (C) 2001-2003 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com
// ---------------------------------------------------------
namespace Reflector.UserInterface
{
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using System.Security;
	using System.Security.Permissions;
	using System.Windows.Forms;

	public class CommandBar : Control, IDisposable
	{
		private CommandBarItemCollection items = new CommandBarItemCollection();
		private CommandBarStyle style = CommandBarStyle.ToolBar;

		private CommandBarContextMenu contextMenu = new CommandBarContextMenu();
		private IntPtr hookHandle = IntPtr.Zero;
		private Point lastMousePosition = new Point(0, 0);
		private int trackHotItem = -1;
		private int trackNextItem = -1;
		private bool trackEscapePressed = false;

		private State state = State.None;
		private State lastState = State.None;
		private ImageList imageList;

		private enum State
		{
			None,
			Hot,
			HotTracking
		}

		public CommandBar()
		{
			this.SetStyle(ControlStyles.UserPaint, false);
			this.TabStop = false;
			this.Font = SystemInformation.MenuFont;
			this.Dock = DockStyle.Top;

			this.items = new CommandBarItemCollection(this);
		}

		public CommandBar(CommandBarStyle style) : this()
		{
			this.style = style;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.items.Clear();
				this.items = null;

				this.contextMenu = null;
			}

			base.Dispose(disposing);
		}

		public CommandBarStyle Style
		{
			set
			{
				this.style = value;
				this.UpdateItems();
			}

			get { return this.style; }
		}

		public CommandBarItemCollection Items
		{
			get { return this.items; }
		}

		protected override Size DefaultSize
		{
			get { return new Size(100, 22); }
		}

		protected override void CreateHandle()
		{
			if (!this.RecreatingHandle)
			{
				NativeMethods.INITCOMMONCONTROLSEX icex = new NativeMethods.INITCOMMONCONTROLSEX();
				icex.Size = Marshal.SizeOf(typeof(NativeMethods.INITCOMMONCONTROLSEX));
				icex.Flags = NativeMethods.ICC_BAR_CLASSES | NativeMethods.ICC_COOL_CLASSES;
				NativeMethods.InitCommonControlsEx(icex);
			}

			base.CreateHandle();
		}

		protected override CreateParams CreateParams
		{
			[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ClassName = NativeMethods.TOOLBARCLASSNAME;
				createParams.ExStyle = 0;
				createParams.Style = NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.WS_CLIPCHILDREN | NativeMethods.WS_CLIPSIBLINGS;
				createParams.Style |= NativeMethods.CCS_NODIVIDER | NativeMethods.CCS_NORESIZE | NativeMethods.CCS_NOPARENTALIGN;
				createParams.Style |= NativeMethods.TBSTYLE_TOOLTIPS | NativeMethods.TBSTYLE_FLAT | NativeMethods.TBSTYLE_TRANSPARENT;

				if (Style == CommandBarStyle.Menu)
				{
					createParams.Style |= NativeMethods.TBSTYLE_LIST;
				}

				return createParams;
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			NativeMethods.SendMessage(Handle, NativeMethods.TB_BUTTONSTRUCTSIZE, Marshal.SizeOf(typeof(NativeMethods.TBBUTTON)), 0);

			int extendedStyle = NativeMethods.TBSTYLE_EX_HIDECLIPPEDBUTTONS | NativeMethods.TBSTYLE_EX_DOUBLEBUFFER;
			if (style == CommandBarStyle.ToolBar)
			{
				extendedStyle |= NativeMethods.TBSTYLE_EX_DRAWDDARROWS;
			}

			NativeMethods.SendMessage(Handle, NativeMethods.TB_SETEXTENDEDSTYLE, 0, extendedStyle);

			this.UpdateImageList();

			for (int i = 0; i < items.Count; i++)
			{
				NativeMethods.TBBUTTON button = new NativeMethods.TBBUTTON();
				button.idCommand = i;
				NativeMethods.SendMessage(this.Handle, NativeMethods.TB_INSERTBUTTON, i, ref button);

				NativeMethods.TBBUTTONINFO buttonInfo = this.GetButtonInfo(i);
				NativeMethods.SendMessage(this.Handle, NativeMethods.TB_SETBUTTONINFO, i, ref buttonInfo);
			}

			// Add ComboBox controls.
			this.Controls.Clear();
			for (int i = 0; i < items.Count; i++)
			{
				CommandBarComboBox comboBox = this.items[i] as CommandBarComboBox;
				if (comboBox != null)
				{
					NativeMethods.RECT rect = new NativeMethods.RECT();
					NativeMethods.SendMessage(this.Handle, NativeMethods.TB_GETITEMRECT, i, ref rect);

					rect.top = rect.top + (((rect.bottom - rect.top) - comboBox.Height) / 2);

					comboBox.ComboBox.Location = new Point(rect.left, rect.top);
					this.Controls.Add(comboBox.ComboBox);
				}
			}

			this.UpdateSize();
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
		public override bool PreProcessMessage(ref Message message)
		{
			if (message.Msg == NativeMethods.WM_KEYDOWN || message.Msg == NativeMethods.WM_SYSKEYDOWN)
			{
				// Process shortcuts.
				Keys keyData = (Keys) (int) message.WParam | ModifierKeys;
				if (state == State.None)
				{
					CommandBarItem[] shortcutHits = this.items[keyData];
					if (shortcutHits.Length > 0)
					{
						if (this.PerformClick(shortcutHits[0]))
						{
							return true;
						}
					}
				}
			}

			// All the following code is for MenuBar only.
			if (this.Style != CommandBarStyle.Menu)
			{
				return false;
			}

			if ((message.Msg >= NativeMethods.WM_LBUTTONDOWN) && (message.Msg <= NativeMethods.WM_MOUSELAST))
			{
				// Check if user clicked outside the MenuBar and end tracking.
				if ((message.HWnd != Handle) && (this.state != State.None))
				{
					this.SetState(State.None, -1);
				}
			}
			else if ((message.Msg == NativeMethods.WM_SYSKEYUP) || (message.Msg == NativeMethods.WM_KEYDOWN) || (message.Msg == NativeMethods.WM_SYSKEYDOWN))
			{
				Keys keyData = ((Keys) (int) message.WParam) | ModifierKeys;
				Keys keyCode = ((Keys) (int) message.WParam);
				if ((keyData == Keys.F10) || (keyCode == Keys.Menu))
				{
					if (message.Msg == NativeMethods.WM_SYSKEYUP)
					{
						if ((this.state == State.Hot) || (this.lastState == State.HotTracking))
						{
							this.SetState(State.None, 0);
						}
						else if (state == State.None)
						{
							this.SetState(State.Hot, 0);
						}

						return true;
					}
				}
				else if (message.Msg == NativeMethods.WM_KEYDOWN || message.Msg == NativeMethods.WM_SYSKEYDOWN)
				{
					if (PreProcessKeyDown(ref message))
						return true;
				}
			}

			return base.PreProcessMessage(ref message);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (this.Style == CommandBarStyle.Menu)
			{
				if ((e.Button == MouseButtons.Left) && (e.Clicks == 1))
				{
					Point point = new Point(e.X, e.Y);
					int index = this.HitTest(point);
					if (this.IsValid(index))
					{
						this.TrackDropDown(index);
						return;
					}
				}
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (this.Style == CommandBarStyle.Menu)
			{
				Point point = new Point(e.X, e.Y);
				if (this.state == State.Hot)
				{
					int index = this.HitTest(point);
					if ((this.IsValid(index)) && (point != lastMousePosition))
						this.SetHotItem(index);
					return;
				}

				lastMousePosition = point;
			}

			base.OnMouseMove(e);
		}

		public bool PreProcessKeyDown(ref Message message)
		{
			Keys keyData = (Keys)(int) message.WParam | ModifierKeys;
			if (state == State.Hot)
			{
				int hotItem = this.GetHotItem();

				if (hotItem != -1)
				{
					if (keyData == Keys.Left)
					{
						this.SetHotItem(this.GetPreviousItem(hotItem));
						return true;
					}

					if (keyData == Keys.Right)
					{
						this.SetHotItem(this.GetNextItem(hotItem));
						return true;
					}

					if ((keyData == Keys.Up) || (keyData == Keys.Down) || (keyData == Keys.Enter))
					{
						this.TrackDropDown(hotItem);
						return true;
					}
				}

				if (keyData == Keys.Escape)
				{
					this.SetState(State.None, -1);
					return true;
				}
			}

			bool alt = ((keyData & Keys.Alt) != 0);
			if ((state == State.Hot) || (alt))
			{
				Keys keyCode = keyData & Keys.KeyCode;
				char key = (char) (int) keyCode;
				if ((Char.IsDigit(key) || (Char.IsLetter(key))))
				{
					// Process mnemonics.
					if (this.PreProcessMnemonic(keyCode))
						return true;

					if ((state == State.Hot) && (!alt))
					{
						NativeMethods.MessageBeep(0);
						return true;
					}
				}
			}

			// return to default state if not handled
			if (state != State.None)
			{
				this.SetState(State.None, -1);
			}

			return false;
		}

		private bool PreProcessMnemonic(Keys keyCode)
		{
			char mnemonic = (char) (int) keyCode;

			CommandBarItem[] mnemonicHits = this.items[mnemonic];
			if (mnemonicHits.Length > 0)
			{
				int index = items.IndexOf(mnemonicHits[0]);
				this.TrackDropDown(index);
				return true;
			}

			return false;
		}

		private bool IsValid(int index)
		{
			int count = NativeMethods.SendMessage(Handle, NativeMethods.TB_BUTTONCOUNT, 0, 0);
			return ((index >= 0) && (index < count));
		}

		private int HitTest(Point point)
		{
			NativeMethods.POINT pt = new NativeMethods.POINT();
			pt.x = point.X;
			pt.y = point.Y;

			int hit = NativeMethods.SendMessage(Handle, NativeMethods.TB_HITTEST, 0, ref pt);
			if (hit > 0)
			{
				point = this.PointToScreen(point);
				Rectangle bounds = this.RectangleToScreen(new Rectangle(0, 0, Width, Height));
				if (!bounds.Contains(point))
				{
					return -1;
				}
			}

			return hit;
		}

		private int GetNextItem(int index)
		{
			if (index < 0)
			{
				throw new ArgumentException("index");
			}

			int count = NativeMethods.SendMessage(this.Handle, NativeMethods.TB_BUTTONCOUNT, 0, 0);

			int nextIndex = index;
			do
			{
				nextIndex = (nextIndex + 1) % count;
			}
			while ((nextIndex != index) && (!items[nextIndex].IsVisible));

			return nextIndex;
		}

		private int GetPreviousItem(int index)
		{
			if (index < 0)
			{
				throw new ArgumentException("index");
			}

			int count = NativeMethods.SendMessage(this.Handle, NativeMethods.TB_BUTTONCOUNT, 0, 0);

			int prevIndex = index;
			do
			{
				prevIndex = (prevIndex + count - 1) % count;
			}
			while ((prevIndex != index) && (!items[prevIndex].IsVisible));

			return prevIndex;
		}

		private int GetHotItem()
		{
			return NativeMethods.SendMessage(Handle, NativeMethods.TB_GETHOTITEM, 0, 0);
		}

		private void SetHotItem(int index)
		{
			NativeMethods.SendMessage(Handle, NativeMethods.TB_SETHOTITEM, index, 0);
		}

		private void SetState(State state, int index)
		{
			if (this.state != state)
			{
				if (state == State.None)
				{
					index = -1;
				}

				this.SetHotItem(index);

				if (state == State.HotTracking)
				{
					this.trackEscapePressed = false;
					this.trackHotItem = index;
				}
			}

			this.lastState = this.state;
			this.state = state;
		}

		private void TrackDropDownNext(int index)
		{
			if (index != this.trackHotItem)
			{
				NativeMethods.PostMessage(Handle, NativeMethods.WM_CANCELMODE, 0, 0);
				this.trackNextItem = index;
			}
		}

		private void TrackDropDown(int index)
		{
			while (index >= 0)
			{
				this.trackNextItem = -1;

				this.BeginUpdate();

				CommandBarMenu menu = this.items[index] as CommandBarMenu;
				if (menu != null)
				{
					menu.PerformDropDown(EventArgs.Empty);
					this.contextMenu.Items.Clear();
					this.contextMenu.Items.AddRange(menu.Items); // = menu.Items;
					this.contextMenu.Mnemonics = true;
				}
				else
				{
					this.contextMenu.Items.Clear(); // .Items = new CommandBarItemCollection();
					this.contextMenu.Mnemonics = true;
				}

				// Item state
				NativeMethods.SendMessage(this.Handle, NativeMethods.TB_PRESSBUTTON, index, -1);

				// Trick to get the first menu item selected
				NativeMethods.PostMessage(this.Handle, NativeMethods.WM_KEYDOWN, (int) Keys.Down, 1);
				NativeMethods.PostMessage(this.Handle, NativeMethods.WM_KEYUP, (int) Keys.Down, 1);

				this.SetState(State.HotTracking, index);

				// Hook
				NativeMethods.HookProc hookProc = new NativeMethods.HookProc(DropDownHook);
				GCHandle hookProcHandle = GCHandle.Alloc(hookProc);
				this.hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MSGFILTER, hookProc, IntPtr.Zero, NativeMethods.GetCurrentThreadId());
				if (this.hookHandle == IntPtr.Zero)
				{
					throw new SecurityException();
				}

				// Ask for position
				NativeMethods.RECT rect = new NativeMethods.RECT();
				NativeMethods.SendMessage(Handle, NativeMethods.TB_GETRECT, index, ref rect);
				Point position = new Point(rect.left, rect.bottom);

				this.EndUpdate();
				this.Update();

				this.contextMenu.Show(this, position);

				// Unhook
				NativeMethods.UnhookWindowsHookEx(hookHandle);
				hookProcHandle.Free();
				this.hookHandle = IntPtr.Zero;

				// Item state
				NativeMethods.SendMessage(Handle, NativeMethods.TB_PRESSBUTTON, index, 0);
				this.SetState(trackEscapePressed ? State.Hot : State.None, index);

				index = trackNextItem;
			}
		}

		public void Show(Control control, Point point)
		{
			CommandBarItemCollection chevronItems = new CommandBarItemCollection();
			Size size = ClientSize;
			for (int i = 0; i < items.Count; i++)
			{
				NativeMethods.RECT rect = new NativeMethods.RECT();
				NativeMethods.SendMessage(Handle, NativeMethods.TB_GETITEMRECT, i, ref rect);
				if (rect.right > size.Width)
				{
					CommandBarItem item = items[i];
					if (item.IsVisible)
					{
						if ((!(item is CommandBarSeparator)) || (chevronItems.Count != 0))
							chevronItems.Add(item);
					}
				}
			}

			this.contextMenu.Mnemonics = false;
			this.contextMenu.Items.Clear();
			this.contextMenu.Items.AddRange(chevronItems);
			this.contextMenu.Show(control, point);
		}

		private bool DropDownFilter(ref Message message)
		{
			if (state != State.HotTracking)
			{
				throw new InvalidOperationException();
			}

			// comctl32 sometimes steals the hot item for unknown reasons.
			this.SetHotItem(this.trackHotItem);

			if (message.Msg == NativeMethods.WM_KEYDOWN)
			{
				Keys keyData = (Keys)(int) message.WParam | ModifierKeys;

				if (keyData == Keys.Left)
				{
					this.TrackDropDownNext(this.GetPreviousItem(trackHotItem));
					return true;
				}

				// Only move right if there is no submenu on the current selected item.
				if ((keyData == Keys.Right) && ((this.contextMenu.SelectedMenuItem == null) || (this.contextMenu.SelectedMenuItem.MenuItems.Count == 0)))
				{
					this.TrackDropDownNext(GetNextItem(trackHotItem));
					return true;
				}

				if (keyData == Keys.Escape)
				{
					trackEscapePressed = true;
				}
			}
			else if ((message.Msg == NativeMethods.WM_MOUSEMOVE) || (message.Msg == NativeMethods.WM_LBUTTONDOWN))
			{
				Point point = new Point(((int) message.LParam) & 0xffff, ((int) message.LParam) >> 16);
				point = this.PointToClient(point);

				if (message.Msg == NativeMethods.WM_MOUSEMOVE)
				{
					if (point != lastMousePosition)
					{
						int index = HitTest(point);
						if ((this.IsValid(index)) && (index != trackHotItem))
							this.TrackDropDownNext(index);

						lastMousePosition = point;
					}
				}
				else if (message.Msg == NativeMethods.WM_LBUTTONDOWN)
				{
					if (HitTest(point) == trackHotItem)
					{
						this.TrackDropDownNext(-1);
						return true;
					}
				}
			}

			return false;
		}

		private IntPtr DropDownHook(int code, IntPtr wparam, IntPtr lparam)
		{
			if (code == NativeMethods.MSGF_MENU)
			{
				NativeMethods.MSG msg = (NativeMethods.MSG) Marshal.PtrToStructure(lparam, typeof(NativeMethods.MSG));
				Message message = Message.Create(msg.hwnd, msg.message, msg.wParam, msg.lParam);
				if (this.DropDownFilter(ref message))
				{
					return (IntPtr)1;
				}
			}

			return NativeMethods.CallNextHookEx(this.hookHandle, code, wparam, lparam);
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
		protected override void WndProc(ref Message message)
		{
			base.WndProc(ref message);

			switch (message.Msg)
			{
				case NativeMethods.WM_COMMAND + NativeMethods.WM_REFLECT:
					int index = (int) message.WParam & 0xFFFF;
					this.PerformClick(this.items[index]);
					base.WndProc(ref message);
					this.ResetMouseEventArgs();
					break;

				case NativeMethods.WM_MENUCHAR:
					this.WmMenuChar(ref message);
					break;

				case NativeMethods.WM_NOTIFY:
				case NativeMethods.WM_NOTIFY + NativeMethods.WM_REFLECT:
					NativeMethods.NMHDR note = (NativeMethods.NMHDR) message.GetLParam(typeof(NativeMethods.NMHDR));
					switch (note.code)
					{
						case NativeMethods.TTN_NEEDTEXTA:
							NotifyNeedTextA(ref message);
							break;

						case NativeMethods.TTN_NEEDTEXTW:
							NotifyNeedTextW(ref message);
							break;

						case NativeMethods.TBN_QUERYINSERT:
							message.Result = (IntPtr) 1;
							break;

						case NativeMethods.TBN_DROPDOWN:
							this.NotifyDropDown(ref message);
							break;

						case NativeMethods.NM_CUSTOMDRAW:
							this.NotifyCustomDraw(ref message);
							break;

						case NativeMethods.TBN_HOTITEMCHANGE:
							break;
					}
					break;
			}
		}

		private void NotifyCustomDrawMenuBar(ref Message m)
		{
			m.Result = (IntPtr) NativeMethods.CDRF_DODEFAULT;
			NativeMethods.LPNMTBCUSTOMDRAW tbcd = (NativeMethods.LPNMTBCUSTOMDRAW) m.GetLParam(typeof(NativeMethods.LPNMTBCUSTOMDRAW));

			bool hot = ((tbcd.nmcd.uItemState & NativeMethods.CDIS_HOT) != 0);
			bool selected = ((tbcd.nmcd.uItemState & NativeMethods.CDIS_SELECTED) != 0);

			if (hot || selected)
			{
				NativeMethods.RECT rect = tbcd.nmcd.rc;

				using (Graphics graphics = Graphics.FromHdc(tbcd.nmcd.hdc))
				{
					graphics.FillRectangle(SystemBrushes.Highlight, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
				}

				using (TextGraphics textGraphics = new TextGraphics(tbcd.nmcd.hdc))
				{
					Font font = this.Font;
					string text = this.items[tbcd.nmcd.dwItemSpec].Text;
					Size size = textGraphics.MeasureText(text, font);
					Point point = new Point(rect.left + ((rect.right - rect.left - size.Width) / 2), rect.top + ((rect.bottom - rect.top - size.Height) / 2));
					textGraphics.DrawText(text, point, font, SystemColors.HighlightText);
				}

				m.Result = (IntPtr) NativeMethods.CDRF_SKIPDEFAULT;
			}
		}

		private void NotifyCustomDrawToolBar(ref Message m)
		{
			m.Result = (IntPtr) NativeMethods.CDRF_DODEFAULT;

			NativeMethods.DLLVERSIONINFO dvi = new NativeMethods.DLLVERSIONINFO();
			dvi.cbSize = Marshal.SizeOf(typeof(NativeMethods.DLLVERSIONINFO));
			NativeMethods.DllGetVersion(ref dvi);
			if (dvi.dwMajorVersion < 6)
			{
				NativeMethods.LPNMTBCUSTOMDRAW tbcd = (NativeMethods.LPNMTBCUSTOMDRAW)m.GetLParam(typeof(NativeMethods.LPNMTBCUSTOMDRAW));
				NativeMethods.RECT rc = tbcd.nmcd.rc;

				Rectangle rectangle = new Rectangle(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);

				Graphics graphics = Graphics.FromHdc(tbcd.nmcd.hdc);
				CommandBarItem item = items[tbcd.nmcd.dwItemSpec];

				bool hot = ((tbcd.nmcd.uItemState & NativeMethods.CDIS_HOT) != 0);
				bool selected = ((tbcd.nmcd.uItemState & NativeMethods.CDIS_SELECTED) != 0);
				bool disabled = ((tbcd.nmcd.uItemState & NativeMethods.CDIS_DISABLED) != 0);

				CommandBarCheckBox checkBox = item as CommandBarCheckBox;
				if ((checkBox != null) && (checkBox.IsChecked))
				{
					ControlPaint.DrawBorder3D(graphics, rectangle, Border3DStyle.SunkenOuter);
				}
				else if (selected)
				{
					ControlPaint.DrawBorder3D(graphics, rectangle, Border3DStyle.SunkenOuter);
				}
				else if (hot)
				{
					ControlPaint.DrawBorder3D(graphics, rectangle, Border3DStyle.RaisedInner);
				}

				Image image = item.Image;
				if (image != null)
				{
					Size size = image.Size;
					Point point = new Point(rc.left + ((rc.right - rc.left - size.Width) / 2), rc.top + ((rc.bottom - rc.top - size.Height) / 2));
					NativeMethods.DrawImage(graphics, image, point, disabled);
				}

				m.Result = (IntPtr)NativeMethods.CDRF_SKIPDEFAULT;
			}
		}

		private void NotifyCustomDraw(ref Message m)
		{
			m.Result = (IntPtr) NativeMethods.CDRF_DODEFAULT;
			NativeMethods.LPNMTBCUSTOMDRAW tbcd = (NativeMethods.LPNMTBCUSTOMDRAW) m.GetLParam(typeof(NativeMethods.LPNMTBCUSTOMDRAW));

			switch (tbcd.nmcd.dwDrawStage)
			{
				case NativeMethods.CDDS_PREPAINT:
					m.Result = (IntPtr) NativeMethods.CDRF_NOTIFYITEMDRAW;
					break;

				case NativeMethods.CDDS_ITEMPREPAINT:
					if (this.style == CommandBarStyle.Menu)
					{
						this.NotifyCustomDrawMenuBar(ref m);
					}
					if (this.style == CommandBarStyle.ToolBar)
					{
						this.NotifyCustomDrawToolBar(ref m);
					}
					break;
			}
		}

		private void WmMenuChar(ref Message message)
		{
			Menu menu = contextMenu.FindMenuItem(MenuItem.FindHandle, message.LParam);
			if (contextMenu.Handle == message.LParam)
			{
				menu = contextMenu;
			}

			if (menu != null)
			{
				char key = char.ToUpper((char) ((int) message.WParam & 0x0000FFFF), CultureInfo.InvariantCulture);
				int index = 0;
				foreach (MenuItem menuItem in menu.MenuItems)
				{
					if ((menuItem != null) && (menuItem.OwnerDraw) && (menuItem.Mnemonic == key))
					{
						message.Result = (IntPtr) ((NativeMethods.MNC_EXECUTE << 16) | index);
						return;
					}

					if (menuItem.Visible) index++;
				}
			}
		}

		private void NotifyDropDown(ref Message message)
		{
			if (this.Style == CommandBarStyle.ToolBar)
			{
				NativeMethods.NMTOOLBAR nmtb = (NativeMethods.NMTOOLBAR) message.GetLParam(typeof(NativeMethods.NMTOOLBAR));
				this.TrackDropDown(nmtb.iItem);
			}
		}

		private void NotifyNeedTextA(ref Message message)
		{
			if (this.Style != CommandBarStyle.Menu)
			{
				NativeMethods.TOOLTIPTEXTA toolTipText = (NativeMethods.TOOLTIPTEXTA) message.GetLParam(typeof(NativeMethods.TOOLTIPTEXTA));
				CommandBarItem item = (CommandBarItem) this.items[toolTipText.hdr.idFrom];
				toolTipText.szText = item.Text;

				CommandBarButtonBase buttonBase = item as CommandBarButtonBase;
				if ((buttonBase != null) && (buttonBase.Shortcut != Keys.None))
				{
					toolTipText.szText += " (" + TypeDescriptor.GetConverter(typeof(Keys)).ConvertToString(null, CultureInfo.InvariantCulture, buttonBase.Shortcut) + ")";
				}

				toolTipText.hinst = IntPtr.Zero;
				if (RightToLeft == RightToLeft.Yes)
				{
					toolTipText.uFlags |= NativeMethods.TTF_RTLREADING;
				}

				Marshal.StructureToPtr(toolTipText, message.LParam, true);
				message.Result = (IntPtr) 1;
			}
		}

		private void NotifyNeedTextW(ref Message message)
		{
			if ((this.Style != CommandBarStyle.Menu) && (Marshal.SystemDefaultCharSize == 2))
			{
				// this code is a duplicate of NotifyNeedTextA
				NativeMethods.TOOLTIPTEXT toolTipText = (NativeMethods.TOOLTIPTEXT) message.GetLParam(typeof(NativeMethods.TOOLTIPTEXT));
				CommandBarItem item = (CommandBarItem) items[toolTipText.hdr.idFrom];
				toolTipText.szText = item.Text;

				CommandBarButtonBase buttonBase = item as CommandBarButton;
				if ((buttonBase != null) && (buttonBase.Shortcut != Keys.None))
				{
					toolTipText.szText += " (" + TypeDescriptor.GetConverter(typeof(Keys)).ConvertToString(null, CultureInfo.InvariantCulture, buttonBase.Shortcut) + ")";
				}

				toolTipText.hinst = IntPtr.Zero;
				if (RightToLeft == RightToLeft.Yes)
				{
					toolTipText.uFlags |= NativeMethods.TTF_RTLREADING;
				}

				Marshal.StructureToPtr(toolTipText, message.LParam, true);
				message.Result = (IntPtr) 1;
			}
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			this.contextMenu.Font = this.Font;
			this.UpdateItems();
		}

		private bool PerformClick(CommandBarItem item)
		{
			Application.DoEvents();

			CommandBarControl control = item as CommandBarControl;
			if (control != null)
			{
				control.PerformClick(EventArgs.Empty);
				return true;
			}

			return false;
		}

		private void BeginUpdate()
		{
			NativeMethods.SendMessage(Handle, NativeMethods.WM_SETREDRAW, 0, 0);
		}

		void EndUpdate()
		{
			NativeMethods.SendMessage(Handle, NativeMethods.WM_SETREDRAW, 1, 0);
		}

		private NativeMethods.TBBUTTONINFO GetButtonInfo(int index)
		{
			CommandBarItem item = items[index];

			NativeMethods.TBBUTTONINFO buttonInfo = new NativeMethods.TBBUTTONINFO();
			buttonInfo.cbSize = Marshal.SizeOf(typeof(NativeMethods.TBBUTTONINFO));

			buttonInfo.dwMask = NativeMethods.TBIF_IMAGE | NativeMethods.TBIF_STATE | NativeMethods.TBIF_STYLE | NativeMethods.TBIF_COMMAND;
			buttonInfo.idCommand = index;
			buttonInfo.iImage = NativeMethods.I_IMAGECALLBACK;
			buttonInfo.fsStyle = NativeMethods.BTNS_BUTTON | NativeMethods.BTNS_AUTOSIZE;
			buttonInfo.fsState = 0;
			buttonInfo.cx = 0;
			buttonInfo.lParam = IntPtr.Zero;
			buttonInfo.pszText = IntPtr.Zero;
			buttonInfo.cchText = 0;

			if (!item.IsVisible)
			{
				buttonInfo.fsState |= NativeMethods.TBSTATE_HIDDEN;
			}

			CommandBarComboBox comboBox = item as CommandBarComboBox;
			if (comboBox != null)
			{
				buttonInfo.cx = (short) (comboBox.Width + 4);
				buttonInfo.dwMask = NativeMethods.TBIF_SIZE;
			}

			if (item is CommandBarSeparator)
			{
				buttonInfo.fsStyle |= NativeMethods.BTNS_SEP;
			}
			else
			{
				if (item.IsEnabled)
				{
					buttonInfo.fsState |= NativeMethods.TBSTATE_ENABLED;
				}

				CommandBarMenu menu = item as CommandBarMenu;
				if ((menu != null) && (menu.Items.Count > 0))
				{
					buttonInfo.fsStyle |= NativeMethods.BTNS_DROPDOWN;
				}

				if (style == CommandBarStyle.ToolBar)
				{
					if (item is CommandBarMenu)
					{
						buttonInfo.fsStyle |= NativeMethods.BTNS_WHOLEDROPDOWN;
					}
				}

				CommandBarCheckBox checkBox = item as CommandBarCheckBox;
				if ((checkBox != null) && (checkBox.IsChecked))
				{
					buttonInfo.fsState |= NativeMethods.TBSTATE_CHECKED;
				}
			}

			if (item is CommandBarSeparator)
			{
				buttonInfo.iImage = NativeMethods.I_IMAGENONE;
			}
			else if (item.Image != null)
			{
				buttonInfo.iImage = index;
			}

			if ((this.Style == CommandBarStyle.Menu) && (item.Text != null) && (item.Text.Length != 0))
			{
				buttonInfo.dwMask |= NativeMethods.TBIF_TEXT;
				buttonInfo.pszText = Marshal.StringToHGlobalUni(item.Text + "\0");
				buttonInfo.cchText = item.Text.Length;
			}

			return buttonInfo;
		}

		private void UpdateImageList()
		{
			IntPtr handle = IntPtr.Zero;
			if (this.Style != CommandBarStyle.Menu)
			{
				Size size = new Size(8, 8);
				for (int i = 0; i < items.Count; i++)
				{
					Image image = items[i].Image;
					if (image != null)
					{
						if (image.Width > size.Width)
						{
							size.Width = image.Width;
						}
						if (image.Height > size.Height)
						{
							size.Height = image.Height;
						}
					}
				}

				Image[] images = new Image[items.Count];
				for (int i = 0; i < items.Count; i++)
				{
					Image image = items[i].Image;
					images[i] = (image != null) ? image : new Bitmap(size.Width, size.Height);
				}

				if (this.imageList == null)
				{
					this.imageList = new ImageList();
					this.imageList.ImageSize = size;
					this.imageList.ColorDepth = ColorDepth.Depth32Bit;

					for (int i = 0; i < images.Length; i++)
					{
						this.imageList.Images.Add(images[i]);
					}
				}
				else if (this.imageList.Images.Count == images.Length)
				{
					for (int i = 0; i < images.Length; i++)
					{
						this.imageList.Images[i] = images[i];
					}
				}
				else
				{
					this.imageList.Images.Clear();

					for (int i = 0; i < images.Length; i++)
					{
						this.imageList.Images.Add(images[i]);
					}
				}

				handle = this.imageList.Handle;
			}

			NativeMethods.SendMessage(this.Handle, NativeMethods.TB_SETIMAGELIST, 0, handle);
		}

		private void UpdateItems()
		{
			if (this.IsHandleCreated)
			{
				this.RecreateHandle();
			}
		}

		private void UpdateSize()
		{
			if (this.style == CommandBarStyle.Menu)
			{
				int fontHeight = Font.Height;

				using (Graphics graphics = this.CreateGraphics())
				{
					using (TextGraphics textGraphics = new TextGraphics(graphics))
					{
						foreach (CommandBarItem item in items)
						{
							Size textSize = textGraphics.MeasureText(item.Text, this.Font);
							if (fontHeight < textSize.Height)
							{
								fontHeight = textSize.Height;
							}
						}
					}
				}

				NativeMethods.SendMessage(this.Handle, NativeMethods.TB_SETBUTTONSIZE, 0, (fontHeight << 16) | 0xffff);
			}

			Size size = new Size(0, 0);
			for (int i = 0; i < items.Count; i++)
			{
				NativeMethods.RECT rect = new NativeMethods.RECT();
				NativeMethods.SendMessage(Handle, NativeMethods.TB_GETRECT, i, ref rect);
				int height = rect.bottom - rect.top;
				if (height > size.Height)
				{
					size.Height = height;
				}

				size.Width += rect.right - rect.left;
			}

			this.Size = size;
		}

		internal void AddItem(CommandBarItem item)
		{
			item.PropertyChanged += new PropertyChangedEventHandler(this.CommandBarItem_PropertyChanged);
			this.UpdateItems();
		}

		internal void RemoveItem(CommandBarItem item)
		{
			item.PropertyChanged -= new PropertyChangedEventHandler(this.CommandBarItem_PropertyChanged);
			this.UpdateItems();
		}

		private void CommandBarItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (this.IsHandleCreated)
			{
				CommandBarItem item = (CommandBarItem)sender;
				int index = this.Items.IndexOf(item);
				if (index != -1)
				{
					switch (e.PropertyName)
					{
						case "IsVisible":
							this.UpdateItems();
							break;

						case "Image":
							this.UpdateImageList();
							break;

						default:
							NativeMethods.TBBUTTONINFO buttonInfo = GetButtonInfo(index);
							NativeMethods.SendMessage(this.Handle, NativeMethods.TB_SETBUTTONINFO, index, ref buttonInfo);
							this.UpdateSize();
							break;
					}
				}
			}
		}
	}
}
