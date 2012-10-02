using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Drawing.Text;
using System.Collections;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using SidebarLibrary.Menus;
using SidebarLibrary.Win32;
using SidebarLibrary.Collections;
using SidebarLibrary.General;


namespace SidebarLibrary.Menus
{
	internal class FocusCatcher : NativeWindow
	{
		public FocusCatcher(IntPtr hParent)
		{
			CreateParams cp = new CreateParams();

			// Any old title will do as it will not be shown
			cp.Caption = "NativeFocusCatcher";

			// Set the position off the screen so it will not be seen
			cp.X = -1;
			cp.Y = -1;
			cp.Height = 0;
			cp.Width = 0;

			// As a top-level window it has no parent
			cp.Parent = hParent;

			// Create as a child of the specified parent
			cp.Style = unchecked((int)(uint)Win32.WindowStyles.WS_CHILD +
				(int)(uint)Win32.WindowStyles.WS_VISIBLE);

			// Create the actual window
			this.CreateHandle(cp);
		}
	}

	[ToolboxItem(false)]
	[DefaultProperty("MenuCommands")]
	public class PopupMenu : NativeWindow, IDisposable
	{
		// Enumeration of Indexes into positioning constants array
		protected enum PI
		{
			BorderTop		= 0,
			BorderLeft		= 1,
			BorderBottom	= 2,
			BorderRight		= 3,
			ImageGapTop		= 4,
			ImageGapLeft	= 5,
			ImageGapBottom	= 6,
			ImageGapRight	= 7,
			TextGapLeft		= 8,
			TextGapRight	= 9,
			SubMenuGapLeft	= 10,
			SubMenuWidth	= 11,
			SubMenuGapRight	= 12,
			SeparatorHeight	= 13,
			SeparatorWidth	= 14,
			ShortcutGap		= 15,
			ShadowWidth		= 16,
			ShadowHeight	= 17,
			ExtraWidthGap	= 18,
			ExtraHeightGap	= 19,
			ExtraRightGap	= 20,
			ExtraReduce		= 21
		}

		// Class constants for sizing/positioning each style
		protected static readonly int[,] _position = {
														{2, 1, 0, 1, 4, 3, 4, 5, 4, 4, 2, 6, 5, 3, 1, 10, 3, 3, 2, 2, 0, 0},	// IDE
														{1, 0, 1, 2, 2, 1, 3, 4, 3, 3, 2, 8, 5, 4, 5, 10, 0, 0, 2, 2, 2, 5}		// Plain
													 };
		// Other class constants
		protected static readonly int _selectionDelay = 400;

		// Class fields
		protected static ImageList _menuImages = null;
		protected static bool _supportsLayered = false;

		// Indexes into the menu images strip
		protected enum ImageIndex
		{
			Check			= 0,
			Radio			= 1,
			SubMenu			= 2,
			CheckSelected	= 3,
			RadioSelected	= 4,
			SubMenuSelected	= 5,
			Expansion		= 6,
			ImageError		= 7
		}

		// Class constants that are marked as 'readonly' are allowed computed initialization
		protected readonly int WM_DISMISS = (int)Msg.WM_USER + 1;
		protected readonly int _imageWidth = SystemInformation.SmallIconSize.Width;
		protected readonly int _imageHeight = SystemInformation.SmallIconSize.Height;

		// Instance fields
		protected Timer _timer;
		protected bool _layered;
		protected Font _textFont;
		protected int _popupItem;
		protected int _trackItem;
		protected int _borderGap;
		protected int _returnDir;
		protected int _extraSize;
		protected bool _exitLoop;
		protected bool _mouseOver;
		protected bool _grabFocus;
		protected bool _popupDown;
		protected bool _popupRight;
		protected bool _excludeTop;
		protected Point _screenPos;
		protected IntPtr _oldFocus;
		protected VisualStyle _style;
		protected Size _currentSize;
		protected int _excludeOffset;
		protected Point _lastMousePos;
		protected Point _currentPoint;
		protected bool _showInfrequent;
		protected PopupMenu _childMenu;
		protected Point _leftScreenPos;
		protected Direction _direction;
		protected Point _aboveScreenPos;
		protected PopupMenu _parentMenu;
		protected ArrayList _drawCommands;
		internal FocusCatcher _focusCatcher;
		protected MenuControl _parentControl;
		protected MenuCommand _returnCommand;
		protected MenuCommandCollection _menuCommands;

		static PopupMenu()
		{
			// Create a strip of images by loading an embedded bitmap resource
			_menuImages = ResourceUtil.LoadImageListResource(Type.GetType("SidebarLibrary.Menus.PopupMenu"),
														 "SidebarLibrary.Resources.ImagesMenu",	// "Resources.ImagesMenu",
														 "MenuControlImages",
														 new Size(16,16),
														 true,
														 new Point(0,0));

			// We need to know if the OS supports layered windows
			_supportsLayered = (OSFeature.Feature.GetVersionPresent(OSFeature.LayeredWindows) != null);
		}

		public PopupMenu()
		{
			// Create collection objects
			_drawCommands = new ArrayList();
			_menuCommands = new MenuCommandCollection();

			// Default the properties
			_returnDir = 0;
			_extraSize = 0;
			_popupItem = -1;
			_trackItem = -1;
			_childMenu = null;
			_exitLoop = false;
			_popupDown = true;
			_mouseOver = false;
			_grabFocus = false;
			_excludeTop = true;
			_popupRight = true;
			_parentMenu = null;
			_excludeOffset = 0;
			_focusCatcher = null;
			_parentControl = null;
			_returnCommand = null;
			_oldFocus = IntPtr.Zero;
			_showInfrequent = false;
			_style = VisualStyle.IDE;
			_lastMousePos = new Point(-1,-1);
			_direction = Direction.Horizontal;
			_textFont = SystemInformation.MenuFont;

			// Create and initialise the timer object (but do not start it running!)
			_timer = new Timer();
			_timer.Interval = _selectionDelay;
			_timer.Tick += new EventHandler(OnTimerExpire);
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~PopupMenu()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				_timer.Dispose();
			}
			IsDisposed = true;
		}
		#endregion

		[Category("Appearance")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public MenuCommandCollection MenuCommands
		{
			get { return _menuCommands; }

			set
			{
				_menuCommands.Clear();
				_menuCommands = value;
			}
		}

		[Category("Appearance")]
		[DefaultValue(VisualStyle.IDE)]
		public VisualStyle Style
		{
			get { return _style; }

			set
			{
				if (_style != value)
					_style = value;
			}
		}

		[Category("Appearance")]
		public Font Font
		{
			get { return _textFont; }

			set
			{
				if (_textFont != value)
					_textFont = value;
			}
		}

		[Category("Behaviour")]
		[DefaultValue(false)]
		public bool ShowInfrequent
		{
			get { return _showInfrequent; }

			set
			{
				if (_showInfrequent != value)
					_showInfrequent = value;
			}
		}

		public MenuCommand TrackPopup(Point screenPos)
		{
			return TrackPopup(screenPos, false);
		}

		public MenuCommand TrackPopup(Point screenPos, bool selectFirst)
		{
			// No point in showing PopupMenu if there are no entries
			if (_menuCommands.VisibleItems())
			{
				// We are being called as a popup window and not from a MenuControl instance
				// and so we need to the focus during the lifetime of the popup and then return
				// focus back again when we are dismissed. Otherwise keyboard input will still go
				// to the control that has it when the popup is created.
				_grabFocus = true;

				// Default the drawing direction
				_direction = Direction.Horizontal;

				// Remember screen positions
				_screenPos = screenPos;
				_aboveScreenPos = screenPos;
				_leftScreenPos = screenPos;

				return InternalTrackPopup(selectFirst);
			}
			else
				return null;
		}

		internal MenuCommand TrackPopup(Point screenPos, Point aboveScreenPos,
										Direction direction,
										MenuCommandCollection menuCollection,
										int borderGap,
										bool selectFirst,
										MenuControl parentControl,
										ref int returnDir)
		{
			// Remember which direction the MenuControl is drawing in
			_direction = direction;

			// Remember the MenuControl that initiated us
			_parentControl = parentControl;

			// Remember the gap in drawing the top border
			_borderGap = borderGap;

			// Remember any currect menu item collection
			MenuCommandCollection oldCollection = _menuCommands;

			// Use the passed in collection of menu commands
			_menuCommands = menuCollection;

			// Remember screen positions
			_screenPos = screenPos;
			_aboveScreenPos = aboveScreenPos;
			_leftScreenPos = screenPos;

			MenuCommand ret = InternalTrackPopup(selectFirst);

			// Restore to original collection
			_menuCommands = oldCollection;

			// Remove reference no longer required
			_parentControl = null;

			// Return the direction key that caused dismissal
			returnDir = _returnDir;

			return ret;
		}

		protected MenuCommand InternalTrackPopup(Point screenPosTR, Point screenPosTL,
												 MenuCommandCollection menuCollection,
												 PopupMenu parentMenu, bool selectFirst,
												 MenuControl parentControl, bool popupRight,
												 bool popupDown, ref int returnDir)
		{
			// Default the drawing direction
			_direction = Direction.Horizontal;

			// Remember the MenuControl that initiated us
			_parentControl = parentControl;

			// We have a parent popup menu that should be consulted about operation
			_parentMenu = parentMenu;

			// Remember any currect menu item collection
			MenuCommandCollection oldCollection = _menuCommands;

			// Use the passed in collection of menu commands
			_menuCommands = menuCollection;

			// Remember screen positions
			_screenPos = screenPosTR;
			_aboveScreenPos = screenPosTR;
			_leftScreenPos = screenPosTL;

			// Remember display directions
			_popupRight = popupRight;
			_popupDown = popupDown;

			MenuCommand ret = InternalTrackPopup(selectFirst);

			// Restore to original collection
			_menuCommands = oldCollection;

			// Remove references no longer required
			_parentControl = null;
			_parentMenu = null;

			// Return the direction key that caused dismissal
			returnDir = _returnDir;

			return ret;
		}

		protected MenuCommand InternalTrackPopup(bool selectFirst)
		{
			// MenuCommand to return as method result
			_returnCommand = null;

			// No item is being tracked
			_trackItem = -1;

			// Flag to indicate when to exit the message loop
			_exitLoop = false;

			// Assume the mouse does not start over our window
			_mouseOver = false;

			// Direction of key press if this caused dismissal
			_returnDir = 0;

			// Flag to indicate if the message should be dispatched
			bool leaveMsg = false;

			// Create and show the popup window (without taking the focus)
			CreateAndShowWindow();

			// Create an object for storing windows message information
			Win32.MSG msg = new Win32.MSG();

			// Draw everything now...
			//RefreshAllCommands();

			// Pretend user pressed key down to get the first valid item selected
			if (selectFirst)
				ProcessKeyDown();

			// Process messages until exit condition recognised
			while(!_exitLoop)
			{
				// Suspend thread until a windows message has arrived
				if (WindowsAPI.WaitMessage())
				{
					// Take a peek at the message details without removing from queue
					while(!_exitLoop && WindowsAPI.PeekMessage(ref msg, 0, 0, 0, (int)Win32.PeekMessageFlags.PM_NOREMOVE))
					{
						//Console.WriteLine("Track {0} {1}", this.Handle, ((Msg)msg.message).ToString());
						//Console.WriteLine("Message is for {0 }", msg.hwnd);

						// Leave messages for children
						IntPtr hParent = WindowsAPI.GetParent(msg.hwnd);
						bool child = hParent == Handle;
						bool combolist = IsComboBoxList(msg.hwnd);

						// Mouse was pressed in a window of this application
						if ((msg.message == (int)Msg.WM_LBUTTONDOWN) ||
							(msg.message == (int)Msg.WM_MBUTTONDOWN) ||
							(msg.message == (int)Msg.WM_RBUTTONDOWN) ||
							(msg.message == (int)Msg.WM_NCLBUTTONDOWN) ||
							(msg.message == (int)Msg.WM_NCMBUTTONDOWN) ||
							(msg.message == (int)Msg.WM_NCRBUTTONDOWN))
						{
							// Is the mouse event for this popup window?
							if (msg.hwnd != this.Handle)
							{
								// Let the parent chain of PopupMenu's decide if they want it
								if (!ParentWantsMouseMessage(ref msg)&& !child && !combolist)
								{
									// No, then we need to exit the popup menu tracking
									_exitLoop = true;

									// DO NOT process the message, leave it on the queue
									// and let the real destination window handle it.
									leaveMsg = true;

									// Is a parent control specified?
									if (_parentControl != null)
									{
										// Is the mouse event destination the parent control?
										if (msg.hwnd == _parentControl.Handle)
										{
											// Then we want to consume the message so it does not get processed
											// by the parent control. Otherwise, pressing down will cause this
											// popup to disappear but the message will then get processed by
											// the parent and cause a popup to reappear again. When we actually
											// want the popup to disappear and nothing more.
											leaveMsg = false;
										}
									}
								}
							}
						}
						else
						{
							// Mouse move occured
							if (msg.message == (int)Msg.WM_MOUSEMOVE)
							{
								// Is the mouse event for this popup window?
								if (msg.hwnd != this.Handle)
								{
									// Do we still think the mouse is over our window?
									if (_mouseOver)
									{
										// Process mouse leaving situation
										OnWM_MOUSELEAVE();
									}

									// Let the parent chain of PopupMenu's decide if they want it
									if (!ParentWantsMouseMessage(ref msg) && !child && !combolist)
									{
										// Eat the message to prevent the destination getting it
										Win32.MSG eat = new Win32.MSG();
										WindowsAPI.GetMessage(ref eat, 0, 0, 0);

										// Do not attempt to pull a message off the queue as it has already
										// been eaten by us in the above code
										leaveMsg = true;
									}
								}
							}
							else
							{
								// Was the alt key pressed?
								if (msg.message == (int)Msg.WM_SYSKEYDOWN)
								{
									// Alt key pressed on its own
									if((int)msg.wParam == (int)Win32.VirtualKeys.VK_MENU)	// ALT key
									{
										// Then we should dimiss ourself
										_exitLoop = true;
									}
								}

								// Was a key pressed?
								if (msg.message == (int)Msg.WM_KEYDOWN)
								{
									switch((int)msg.wParam)
									{
									case (int)Win32.VirtualKeys.VK_UP:
										ProcessKeyUp();
										break;
									case (int)Win32.VirtualKeys.VK_DOWN:
										ProcessKeyDown();
										break;
									case (int)Win32.VirtualKeys.VK_LEFT:
										ProcessKeyLeft();
										break;
									case (int)Win32.VirtualKeys.VK_RIGHT:
										if(ProcessKeyRight())
										{
											// Do not attempt to pull a message off the queue as the
											// ProcessKeyRight has eaten the message for us
											leaveMsg = true;
										}
										break;
									case (int)Win32.VirtualKeys.VK_RETURN:
										// Is an item currently selected
										if (_trackItem != -1)
										{
											DrawCommand dc = _drawCommands[_trackItem] as DrawCommand;

											// Does this item have a submenu?
											if (dc.SubMenu)
											{
												// Consume the keyboard message to prevent the submenu immediately
												// processing the same message again. Remember this routine is called
												// after PeekMessage but the message is still on the queue at this point
												Win32.MSG eat = new Win32.MSG();
												WindowsAPI.GetMessage(ref eat, 0, 0, 0);

												// Handle the submenu
												OperateSubMenu(_trackItem, false);

												// Do not attempt to pull a message off the queue as it has already
												// been eaten by us in the above code
												leaveMsg = true;
											}
											else
											{
												// Is this item the expansion command?
												if (dc.Expansion)
												{
													RegenerateExpansion();
												}
												else
												{
													// Define the selection to return to caller
													_returnCommand = dc.MenuCommand;

													// Finish processing messages
													_exitLoop = true;
												}
											}
										}
										break;
									case (int)Win32.VirtualKeys.VK_ESCAPE:
										// User wants to exit the menu, so set the flag to exit the message loop but
										// let the message get processed. This way the key press is thrown away.
										_exitLoop = true;
										break;
									default:
										// Any other key is treated as a possible mnemonic
										int selectItem = ProcessMnemonicKey((char)msg.wParam);

										if (selectItem != -1)
										{
											DrawCommand dc = _drawCommands[selectItem] as DrawCommand;

											// Define the selection to return to caller
											_returnCommand = dc.MenuCommand;

											// Finish processing messages
											_exitLoop = true;
										}
										break;
									}
								}
							}
						}

						// Should the message we pulled from the queue?
						if (!leaveMsg)
						{
							if (WindowsAPI.GetMessage(ref msg, 0, 0, 0))
							{
								WindowsAPI.TranslateMessage(ref msg);
								WindowsAPI.DispatchMessage(ref msg);
							}
						}
						else
							leaveMsg = false;
					}
				}
			}

			// Do we have a focus we need to restore?
			if (_oldFocus != IntPtr.Zero)
				ReturnTheFocus();

			// Need to unset this window as the parent of the comboboxes
			// -- if any -- otherwise the combobox use in an toolbar would get "sick"
			UnsetComboBoxesParent();

			// Hide the window from view before killing it, as sometimes there is a
			// short delay between killing it and it disappearing because of the time
			// it takes for the destroy messages to get processed
			WindowsAPI.ShowWindow(this.Handle, (short)Win32.ShowWindowStyles.SW_HIDE);

			// Commit suicide
			DestroyHandle();

			// Was a command actually selected?
			if ((_parentMenu == null) && (_returnCommand != null))
			{
				// Pulse the selected event for the command
				_returnCommand.OnClick(EventArgs.Empty);
			}

			return _returnCommand;
		}

		public void Dismiss()
		{
			if (this.Handle != IntPtr.Zero)
			{
				// Prevent the timer from expiring
				_timer.Stop();

				// Kill any child menu
				if (_childMenu != null)
					_childMenu.Dismiss();

				// Finish processing messages
				_exitLoop = true;

				// Hide ourself
				WindowsAPI.ShowWindow(this.Handle, (short)Win32.ShowWindowStyles.SW_HIDE);

				// Cause our own message loop to exit
				WindowsAPI.PostMessage(this.Handle, WM_DISMISS, 0, 0);
			}
		}

		protected void CreateAndShowWindow()
		{
			// Decide if we need layered windows
			_layered = (_supportsLayered && (_style == VisualStyle.IDE));

			// Don't use layered windows because we would need to do more work
			// to paint the comboboxes
			_layered = false;

			// Process the menu commands to determine where each one needs to be
			// drawn and return the size of the window needed to display it.
			Size winSize = GenerateDrawPositions();

			Point screenPos = CorrectPositionForScreen(winSize);

			CreateParams cp = new CreateParams();

			// Any old title will do as it will not be shown
			cp.Caption = "NativePopupMenu";

			// Define the screen position/size
			cp.X = screenPos.X;
			cp.Y = screenPos.Y;
			cp.Height = winSize.Height;
			cp.Width = winSize.Width;

			// As a top-level window it has no parent
			cp.Parent = IntPtr.Zero;

			// Appear as a top-level window
			cp.Style = unchecked((int)(uint)Win32.WindowStyles.WS_POPUP |
				(int)(uint)Win32.WindowStyles.WS_CLIPCHILDREN);

			// Set styles so that it does not have a caption bar and is above all other
			// windows in the ZOrder, i.e. TOPMOST
			cp.ExStyle = (int)Win32.WindowExStyles.WS_EX_TOPMOST + (int)Win32.WindowExStyles.WS_EX_TOOLWINDOW;

			// OS specific style
			if (_layered)
			{
				// If not on NT then we are going to use alpha blending on the shadow border
				// and so we need to specify the layered window style so the OS can handle it
				cp.ExStyle += (int)Win32.WindowExStyles.WS_EX_LAYERED;
			}

			// Is this the plain style of appearance?
			if (_style == VisualStyle.Plain)
			{
				// We want the tradiditonal 3D border
				cp.Style += unchecked((int)(uint)Win32.WindowStyles.WS_DLGFRAME);
			}

			// Create the actual window
			this.CreateHandle(cp);

			// Update the window clipping region
			if (!_layered)
				SetWindowRegion(winSize);

			// Make sure comboboxes are the children of this window
			SetComboBoxesParent();

			// Show the window without activating it (i.e. do not take focus)
			WindowsAPI.ShowWindow(this.Handle, (short)Win32.ShowWindowStyles.SW_SHOWNOACTIVATE);

			if (_layered)
			{
				// Remember the correct screen drawing details
				_currentPoint = screenPos;
				_currentSize = winSize;

				// Update the image for display
				UpdateLayeredWindow();

				// Must grab the focus immediately
				if (_grabFocus)
					GrabTheFocus();
			}
		}

		void SetComboBoxesParent()
		{
			foreach(MenuCommand command in _menuCommands)
			{
				// no need for recursion, comboboxes are only
				// on the first level
				if ( command.ComboBox != null)
				{
					WindowsAPI.SetParent(command.ComboBox.Handle, Handle);
				}
			}
		}

		void UnsetComboBoxesParent()
		{
			foreach(MenuCommand command in _menuCommands)
			{
				// no need for recursion, comboboxes are only
				// on the first level
				if ( command.ComboBox != null)
				{
					command.ComboBox.Visible = false;
					WindowsAPI.SetParent(command.ComboBox.Handle, IntPtr.Zero);
				}
			}
		}

		bool IsComboBoxList(IntPtr hWnd)
		{
			STRINGBUFFER className;
			WindowsAPI.GetClassName(hWnd, out className, 80);
			if ( className.szText == "ComboLBox" )
				return true;
			return false;

		}

		protected void UpdateLayeredWindow()
		{
			UpdateLayeredWindow(_currentPoint, _currentSize);
		}

		protected void UpdateLayeredWindow(Point point, Size size)
		{
			// Create bitmap for drawing onto
			using (Bitmap memoryBitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb))
			using(Graphics g = Graphics.FromImage(memoryBitmap))
			{
				Rectangle area = new Rectangle(0, 0, size.Width, size.Height);

				// Draw the background area
				DrawBackground(g, area);

				// Draw the actual menu items
				DrawAllCommands(g);

				// Get hold of the screen DC
				IntPtr hDC = WindowsAPI.GetDC(IntPtr.Zero);

				// Create a memory based DC compatible with the screen DC
				IntPtr memoryDC = WindowsAPI.CreateCompatibleDC(hDC);

				// Get access to the bitmap handle contained in the Bitmap object
				IntPtr hBitmap = memoryBitmap.GetHbitmap(Color.FromArgb(0));

				// Select this bitmap for updating the window presentation
				IntPtr oldBitmap = WindowsAPI.SelectObject(memoryDC, hBitmap);

				// New window size
				Win32.SIZE ulwsize;
				ulwsize.cx = size.Width;
				ulwsize.cy = size.Height;

				// New window position
				Win32.POINT topPos;
				topPos.x = point.X;
				topPos.y = point.Y;

				// Offset into memory bitmap is always zero
				Win32.POINT pointSource;
				pointSource.x = 0;
				pointSource.y = 0;

				// We want to make the entire bitmap opaque
				Win32.BLENDFUNCTION blend = new Win32.BLENDFUNCTION();
				blend.BlendOp             = (byte)Win32.AlphaFlags.AC_SRC_OVER;
				blend.BlendFlags          = 0;
				blend.SourceConstantAlpha = 255;
				blend.AlphaFormat         = (byte)Win32.AlphaFlags.AC_SRC_ALPHA;

				// Tell operating system to use our bitmap for painting
				WindowsAPI.UpdateLayeredWindow(Handle, hDC, ref topPos, ref ulwsize,
										   memoryDC, ref pointSource, 0, ref blend,
										   (int)Win32.UpdateLayeredWindowsFlags.ULW_ALPHA);

				// Put back the old bitmap handle
				WindowsAPI.SelectObject(memoryDC, oldBitmap);

				// Cleanup resources
				WindowsAPI.ReleaseDC(IntPtr.Zero, hDC);
				WindowsAPI.DeleteObject(hBitmap);
				WindowsAPI.DeleteDC(memoryDC);
			}
		}

		protected void SetWindowRegion(Size winSize)
		{
			// Style specific handling
			if (_style == VisualStyle.IDE)
			{
				int shadowHeight = _position[(int)_style, (int)PI.ShadowHeight];
				int shadowWidth = _position[(int)_style, (int)PI.ShadowWidth];

				// Create a new region object
				using (Region drawRegion = new Region())
				{
				// Can draw anywhere
				drawRegion.MakeInfinite();

				// Remove the area above the right hand shadow
				drawRegion.Xor(new Rectangle(winSize.Width - shadowWidth, 0, shadowWidth, shadowHeight));

				// When drawing upwards from a vertical menu we need to allow a connection between the
				// MenuControl selection box and the PopupMenu shadow
				if (!((_direction == Direction.Vertical) && !_excludeTop))
				{
					// Remove the area left of the bottom shadow
					drawRegion.Xor(new Rectangle(0, winSize.Height - shadowHeight, shadowWidth, shadowHeight));
				}

				// Define a region to prevent drawing over exposed corners of shadows
				using(Graphics g = Graphics.FromHwnd(this.Handle))
					WindowsAPI.SetWindowRgn(this.Handle, drawRegion.GetHrgn(g), false);
			}
			}
		}

		protected Point CorrectPositionForScreen(Size winSize)
		{
			Point screenPos = _screenPos;

			int screenWidth = SystemInformation.WorkingArea.Width;
			int screenHeight = SystemInformation.WorkingArea.Height;

			// Default to excluding menu border from top
			_excludeTop = true;
			_excludeOffset = 0;

			// Calculate the downward position first
			if (_popupDown)
			{
				// Ensure the end of the menu is not off the bottom of the screen
				if ((screenPos.Y + winSize.Height) > screenHeight)
				{
					// If the parent control exists then try and position upwards instead
					if ((_parentControl != null) && (_parentMenu == null))
					{
						// Is there space above the required position?
						if ((_aboveScreenPos.Y - winSize.Height) > 0)
						{
							// Great...do that instead
							screenPos.Y = _aboveScreenPos.Y - winSize.Height;

							// Reverse direction of drawing this and submenus
							_popupDown = false;

							// Remember to exclude border from bottom of menu and not the top
							_excludeTop = false;

							// Inform parent it needs to redraw the selection upwards
							_parentControl.DrawSelectionUpwards();
						}
					}

					// Did the above logic still fail?
					if ((screenPos.Y + winSize.Height) > screenHeight)
					{
						// If not a top level PopupMenu then..
						if (_parentMenu != null)
						{
							// Reverse direction of drawing this and submenus
							_popupDown = false;

							// Is there space above the required position?
							if ((_aboveScreenPos.Y - winSize.Height) > 0)
								screenPos.Y = _aboveScreenPos.Y - winSize.Height;
							else
								screenPos.Y = 0;
						}
						else
							screenPos.Y = screenHeight - winSize.Height - 1;
					}
				}
			}
			else
			{
				// Ensure the end of the menu is not off the top of the screen
				if ((screenPos.Y - winSize.Height) < 0)
				{
					// Reverse direction
					_popupDown = true;

					// Is there space below the required position?
					if ((screenPos.Y + winSize.Height) > screenHeight)
						screenPos.Y = screenHeight - winSize.Height - 1;
				}
				else
					screenPos.Y -= winSize.Height;
			}

			// Calculate the across position next
			if (_popupRight)
			{
				// Ensure that right edge of menu is not off right edge of screen
				if ((screenPos.X + winSize.Width) > screenWidth)
				{
					// If not a top level PopupMenu then...
					if (_parentMenu != null)
					{
						// Reverse direction
						_popupRight = false;

						// Adjust across position
						screenPos.X = _leftScreenPos.X - winSize.Width;

						if (screenPos.X < 0)
							screenPos.X = 0;
					}
					else
					{
						// Find new position of X coordinate
						int newX = screenWidth - winSize.Width - 1;

						// Modify the adjust needed when drawing top/bottom border
						_excludeOffset = screenPos.X - newX;

						// Use new position for popping up menu
						screenPos.X = newX;
					}
				}
			}
			else
			{
				// Start by using the left screen pos instead
				screenPos.X = _leftScreenPos.X;

				// Ensure the left edge of the menu is not off the left of the screen
				if ((screenPos.X - winSize.Width) < 0)
				{
					// Reverse direction
					_popupRight = true;

					// Is there space below the required position?
					if ((_screenPos.X + winSize.Width) > screenWidth)
						screenPos.X = screenWidth - winSize.Width - 1;
					else
						screenPos.X = _screenPos.X;
				}
				else
					screenPos.X -= winSize.Width;
			}

			return screenPos;
		}

		protected void RegenerateExpansion()
		{
			// Remove all existing draw commands
			_drawCommands.Clear();

			// Move into the expanded mode
			_showInfrequent = true;

			// Generate new ones
			Size newSize = GenerateDrawPositions();

			// Find the new screen location for the window
			Point newPos = CorrectPositionForScreen(newSize);

			// Update the window clipping region
			if (!_layered)
			{
				SetWindowRegion(newSize);

				// Alter size and location of window
				WindowsAPI.MoveWindow(this.Handle, newPos.X, newPos.Y, newSize.Width, newSize.Height, true);
			}
			else
			{
				// Remember the correct screen drawing details
				_currentPoint = newPos;
				_currentSize = newSize;

				// Update the image for display
				UpdateLayeredWindow();
			}

			// Lets repaint everything
			RefreshAllCommands();
		}


		protected Size GenerateDrawPositions()
		{
			// Create a collection of drawing objects
			_drawCommands = new ArrayList();

			// Calculate the minimum cell width and height
			int cellMinHeight = _position[(int)_style, (int)PI.ImageGapTop] +
								_imageHeight +
								_position[(int)_style, (int)PI.ImageGapBottom];

			int cellMinWidth = _position[(int)_style, (int)PI.ImageGapLeft] +
							   _imageWidth +
							   _position[(int)_style, (int)PI.ImageGapRight] +
							   _position[(int)_style, (int)PI.TextGapLeft] +
							   _position[(int)_style, (int)PI.TextGapRight] +
							   _position[(int)_style, (int)PI.SubMenuGapLeft] +
							   _position[(int)_style, (int)PI.SubMenuWidth] +
							   _position[(int)_style, (int)PI.SubMenuGapRight];

			// Find cell height needed to draw text
			int textHeight = _textFont.Height;

			// If height needs to be more to handle image then use image height
			if (textHeight < cellMinHeight)
				textHeight = cellMinHeight;

			// Make sure no column in the menu is taller than the screen
			int screenHeight = SystemInformation.WorkingArea.Height;

			// Define the starting positions for calculating cells
			int xStart = _position[(int)_style, (int)PI.BorderLeft];
			int yStart =_position[(int)_style, (int)PI.BorderTop];
			int yPosition = yStart;

			// Largest cell for column defaults to minimum cell width
			int xColumnMaxWidth = cellMinWidth;

			int xPreviousColumnWidths = 0;
			int xMaximumColumnHeight = 0;

			// Track the row/col of each cell
			int row = 0;
			int col = 0;

			// Are there any infrequent items
			bool infrequent = false;

			// Get hold of the DC for the desktop
			IntPtr hDC = WindowsAPI.GetDC(IntPtr.Zero);

			// Contains the collection of items in the current column
			ArrayList columnItems = new ArrayList();

			using(Graphics g = Graphics.FromHdc(hDC))
			{
				// Handle any extra text drawing
				if (_menuCommands.ExtraText.Length > 0)
				{
					// Calculate the column width needed to show this text
					SizeF dimension = g.MeasureString(_menuCommands.ExtraText, _menuCommands.ExtraFont);

					// Always add 1 to ensure that rounding is up and not down
					int extraHeight = (int)dimension.Height + 1;

					// Find the total required as the text requirement plus style specific spacers
					_extraSize = extraHeight +
								 _position[(int)_style, (int)PI.ExtraRightGap] +
								 _position[(int)_style, (int)PI.ExtraWidthGap] * 2;

					// Push first column of items across from the extra text
					xStart += _extraSize;

					// Add this extra width to the total width of the window
					xPreviousColumnWidths = _extraSize;
				}

				foreach(MenuCommand command in _menuCommands)
				{
					// Give the command a chance to update its state
					command.OnUpdate(EventArgs.Empty);

					// Ignore items that are marked as hidden
					if (!command.Visible)
						continue;

					// If this command has menu items (and so it a submenu item) then check
					// if any of the submenu items are visible. If none are visible then there
					// is no point in showing this submenu item
					if ((command.MenuCommands.Count > 0) && (!command.MenuCommands.VisibleItems()))
						continue;

					// Ignore infrequent items unless flag set to show them
					if (command.Infrequent && !_showInfrequent)
					{
						infrequent = true;
						continue;
					}

					int cellWidth = 0;
					int cellHeight = 0;

					// Shift across to the next column?
					if (command.Break)
					{
						// Move row/col tracking to the next column
						row = 0;
						col++;

						// Apply cell width to the current column entries
						ApplySizeToColumnList(columnItems, xColumnMaxWidth);

						// Move cell position across to start of separator position
						xStart += xColumnMaxWidth;

						// Get width of the separator area
						int xSeparator = _position[(int)_style, (int)PI.SeparatorWidth];

						DrawCommand dcSep = new DrawCommand(new Rectangle(xStart, 0, xSeparator, 0), false);

						// Add to list of items for drawing
						_drawCommands.Add(dcSep);

						// Move over the separator
						xStart += xSeparator;

						// Reset cell position to top of column
						yPosition = yStart;

						// Accumulate total width of previous columns
						xPreviousColumnWidths += xColumnMaxWidth + xSeparator;

						// Largest cell for column defaults to minimum cell width
						xColumnMaxWidth  = cellMinWidth;
					}

					// Is this a horizontal separator?
					if (command.Text == "-")
					{
						cellWidth = cellMinWidth;
						cellHeight = _position[(int)_style, (int)PI.SeparatorHeight];
					}
					else
					{
						// Use precalculated height
						cellHeight = textHeight;

						// Calculate the text width portion of the cell
						SizeF dimension = g.MeasureString(command.Text, _textFont);

						// Always add 1 to ensure that rounding is up and not down
						cellWidth = cellMinWidth + (int)dimension.Width + 1;

						// Does the menu command have a shortcut defined?
						if (command.Shortcut != Shortcut.None)
						{
							// Find the width of the shortcut text
							dimension = g.MeasureString(GetShortcutText(command.Shortcut), _textFont);

							// Add to the width of the cell
							cellWidth += _position[(int)_style, (int)PI.ShortcutGap] + (int)dimension.Width + 1;
						}

						// If this is a combobox, then add the combobox dimension
						if ( command.ComboBox != null )
						{
							cellWidth += command.ComboBox.Width;
						}
					}

					// If the new cell expands past the end of the screen...
					if ((yPosition + cellHeight) >= screenHeight)
					{
						// .. then need to insert a column break

						// Move row/col tracking to the next column
						row = 0;
						col++;

						// Apply cell width to the current column entries
						ApplySizeToColumnList(columnItems, xColumnMaxWidth);

						// Move cell position across to start of separator position
						xStart += xColumnMaxWidth;

						// Get width of the separator area
						int xSeparator = _position[(int)_style, (int)PI.SeparatorWidth];

						DrawCommand dcSep = new DrawCommand(new Rectangle(xStart, yStart, xSeparator, 0), false);

						// Add to list of items for drawing
						_drawCommands.Add(dcSep);

						// Move over the separator
						xStart += xSeparator;

						// Reset cell position to top of column
						yPosition = yStart;

						// Accumulate total width of previous columns
						xPreviousColumnWidths += xColumnMaxWidth + xSeparator;

						// Largest cell for column defaults to minimum cell width
						xColumnMaxWidth  = cellMinWidth;
					}

					// Create a new position rectangle (the width will be reset later once the
					// width of the column has been determined but the other values are correct)
					Rectangle cellRect = new Rectangle(xStart, yPosition, cellWidth, cellHeight);

					// Create a drawing object
					DrawCommand dc = new DrawCommand(command, cellRect, row, col);

					// Add to list of items for drawing
					_drawCommands.Add(dc);

					// Add to list of items in this column
					columnItems.Add(dc);

					// Remember the biggest cell width in this column
					if (cellWidth > xColumnMaxWidth)
						xColumnMaxWidth = cellWidth;

					// Move down to start of next cell in column
					yPosition += cellHeight;

					// Remember the tallest column in the menu
					if (yPosition > xMaximumColumnHeight)
						xMaximumColumnHeight = yPosition;

					row++;
				}

				// Check if we need to add an infrequent expansion item
				if (infrequent)
				{
					// Create a minimum size cell
					Rectangle cellRect = new Rectangle(xStart, yPosition, cellMinWidth, cellMinHeight);

					// Create a draw command to represent the drawing of the expansion item
					DrawCommand dc = new DrawCommand(cellRect, true);

					// Must be last item
					_drawCommands.Add(dc);

					// Add to list of items in this column
					columnItems.Add(dc);

					yPosition += cellMinHeight;

					// Remember the tallest column in the menu
					if (yPosition > xMaximumColumnHeight)
						xMaximumColumnHeight = yPosition;
				}

				// Apply cell width to the current column entries
				ApplySizeToColumnList(columnItems, xColumnMaxWidth);
			}

			// Must remember to release the HDC resource!
			WindowsAPI.ReleaseDC(IntPtr.Zero, hDC);

			// Find width/height of window
			int windowWidth = _position[(int)_style, (int)PI.BorderLeft] +
							  xPreviousColumnWidths +
							  xColumnMaxWidth +
							  _position[(int)_style, (int)PI.BorderRight];

			int windowHeight = _position[(int)_style, (int)PI.BorderTop] +
							  xMaximumColumnHeight +
							  _position[(int)_style, (int)PI.BorderBottom];

			// Define the height of the vertical separators
			ApplyVerticalSeparators(xMaximumColumnHeight);

			// Style specific modification of window size
			int xAdd = _position[(int)_style, (int)PI.ShadowHeight];
			int yAdd = _position[(int)_style, (int)PI.ShadowWidth];

			if (_style == VisualStyle.Plain)
			{
				xAdd += SystemInformation.Border3DSize.Width * 2;
				yAdd += SystemInformation.Border3DSize.Height * 2;
			}

			return new Size(windowWidth + xAdd, windowHeight + yAdd);
		}

		protected void ApplyVerticalSeparators(int sepHeight)
		{
			// Each vertical separator needs to be the same height, this has already
			// been calculated and passed in from the tallest column in the menu
			foreach(DrawCommand dc in _drawCommands)
			{
				if (dc.VerticalSeparator)
				{
					// Grab the current drawing rectangle
					Rectangle cellRect = dc.DrawRect;

					// Modify the height to that requested
					dc.DrawRect = new Rectangle(cellRect.Left, cellRect.Top, cellRect.Width, sepHeight);
				}
			}
		}

		protected void ApplySizeToColumnList(ArrayList columnList, int cellWidth)
		{
			// Each cell in the same column needs to be the same width, this has already
			// been calculated and passed in as the widest cell in the column
			foreach(DrawCommand dc in columnList)
			{
				// Grab the current drawing rectangle
				Rectangle cellRect = dc.DrawRect;

				// Modify the width to that requested
				dc.DrawRect = new Rectangle(cellRect.Left, cellRect.Top, cellWidth, cellRect.Height);
			}

			// Clear collection out ready for reuse
			columnList.Clear();
		}

		protected void RefreshAllCommands()
		{
			Win32.RECT rectRaw = new Win32.RECT();

			// Grab the screen rectangle of the window
			WindowsAPI.GetWindowRect(this.Handle, ref rectRaw);

			// Convert from screen to client sizing
			Rectangle rectWin = new Rectangle(0, 0,
											  rectRaw.right - rectRaw.left,
											  rectRaw.bottom - rectRaw.top);

			using(Graphics g = Graphics.FromHwnd(this.Handle))
			{
				// Draw the background area
				DrawBackground(g, rectWin);

				// Draw the actual menu items
				DrawAllCommands(g);
			}
		}

		protected void DrawBackground(Graphics g, Rectangle rectWin)
		{
			Rectangle main = new Rectangle(0, 0,
										   rectWin.Width - 1 - _position[(int)_style, (int)PI.ShadowWidth],
										   rectWin.Height - 1 - _position[(int)_style, (int)PI.ShadowHeight]);

			// Style specific drawing
			switch(_style)
			{
			case VisualStyle.IDE:
				// Calculate some common values
				int imageColWidth = _position[(int)_style, (int)PI.ImageGapLeft] +
									_imageWidth +
									_position[(int)_style, (int)PI.ImageGapRight];

				int xStart = _position[(int)_style, (int)PI.BorderLeft];
				int yStart = _position[(int)_style, (int)PI.BorderTop];
				int yHeight = main.Height - yStart - _position[(int)_style, (int)PI.BorderBottom] - 1;

				// Paint the main area background
				using(SolidBrush mainBrush = new SolidBrush(SystemColors.ControlLightLight))
					g.FillRectangle(mainBrush, main);

				// Draw single line border around the main area
				using(Pen mainBorder = new Pen(SystemColors.ControlDark))
				{
					g.DrawRectangle(mainBorder, main);

					// Should the border be drawn with part of the border missing?
					if (_borderGap > 0)
					{
						using(Pen mainControl = new Pen(SystemColors.ControlLight))
						{
							// Remove the appropriate section of the border
							if (_direction == Direction.Horizontal)
							{
								if (_excludeTop)
									g.DrawLine(mainControl, main.Left + 1 + _excludeOffset,
															main.Top, main.Left + _borderGap + _excludeOffset - 1, main.Top);
								else
									g.DrawLine(mainControl, main.Left + 1 + _excludeOffset,
															main.Bottom, main.Left + _borderGap + _excludeOffset - 1, main.Bottom);
							}
							else
							{
								if (_excludeTop)
									g.DrawLine(mainControl, main.Left, main.Top + 1 + _excludeOffset,
															main.Left, main.Top + _borderGap + _excludeOffset - 1);
								else
									g.DrawLine(mainControl, main.Left, main.Bottom - 1 - _excludeOffset,
															main.Left, main.Bottom - _borderGap - _excludeOffset - 1);
							}
						}
					}
				}

				// Draw the first image column
				Rectangle imageRect = new Rectangle(xStart, yStart, imageColWidth, yHeight);

				g.FillRectangle(SystemBrushes.ControlLight, imageRect);

				// Draw image column after each vertical separator
				foreach(DrawCommand dc in _drawCommands)
				{
					if (dc.Separator && dc.VerticalSeparator)
					{
						// Recalculate starting position (but height remains the same)
						imageRect.X = dc.DrawRect.Right;

						g.FillRectangle(SystemBrushes.ControlLight, imageRect);
					}
				}

				// Draw shadow around borders
				int rightLeft = main.Right + 1;
				int rightTop = main.Top + _position[(int)_style, (int)PI.ShadowHeight];
				int rightBottom = main.Bottom + 1;
				int leftLeft = main.Left + _position[(int)_style, (int)PI.ShadowWidth];
				int xExcludeStart = main.Left + _excludeOffset;
				int xExcludeEnd = main.Left + _excludeOffset + _borderGap;

					using (SolidBrush shadowBrush = _layered ? new SolidBrush(Color.FromArgb(64, 0, 0, 0)) :
						new SolidBrush(SystemColors.ControlDark))
					{
				if ((_borderGap > 0) && (!_excludeTop) && (_direction == Direction.Horizontal))
				{
					int rightright = rectWin.Width;

					if (xExcludeStart >= leftLeft)
						g.FillRectangle(shadowBrush, leftLeft, rightBottom, xExcludeStart - leftLeft, _position[(int)_style, (int)PI.ShadowHeight]);

					if (xExcludeEnd <= rightright)
						g.FillRectangle(shadowBrush, xExcludeEnd, rightBottom, rightright - xExcludeEnd, _position[(int)_style, (int)PI.ShadowHeight]);
				}
				else
				{
					if ((_direction == Direction.Vertical) && (!_excludeTop))
						leftLeft = 0;

					g.FillRectangle(shadowBrush, leftLeft, rightBottom, rightLeft, _position[(int)_style, (int)PI.ShadowHeight]);
				}

				g.FillRectangle(shadowBrush, rightLeft, rightTop, _position[(int)_style, (int)PI.ShadowWidth], rightBottom - rightTop);
					}

				if ((_borderGap > 0) && (!_excludeTop) && (_direction == Direction.Horizontal))
				{
					using(SolidBrush tabBrush = new SolidBrush(SystemColors.ControlLight))
						g.FillRectangle(tabBrush, new Rectangle(xExcludeStart, rightBottom - 1,
																xExcludeEnd - xExcludeStart,
																_position[(int)_style, (int)PI.ShadowHeight] + 1));

					// Draw lines connecting the selected MenuControl item to the submenu
					using(Pen connectPen = new Pen(SystemColors.ControlDark))
					{
						g.DrawLine(connectPen, xExcludeStart, rightBottom - 1, xExcludeStart, rectWin.Height);
						g.DrawLine(connectPen, xExcludeEnd, rightBottom - 1, xExcludeEnd, rectWin.Height);
					}
				}
				break;
			case VisualStyle.Plain:
				// Paint the main area background
				using(SolidBrush mainBrush = new SolidBrush(SystemColors.Control))
					g.FillRectangle(mainBrush, rectWin);
				break;
			}

			// Is there an extra title text to be drawn?
			if (_menuCommands.ExtraText.Length > 0)
				DrawColumn(g, main);
		}

		protected void DrawColumn(Graphics g, Rectangle main)
		{
			// Create the rectangle that encloses the drawing
			Rectangle rectText = new Rectangle(main.Left, main.Top,
											   _extraSize - _position[(int)_style, (int)PI.ExtraRightGap],
											   main.Height);

			Brush backBrush = null;
			bool disposeBack = true;
			try
			{
			if (_menuCommands.ExtraBackBrush != null)
			{
				backBrush = _menuCommands.ExtraBackBrush;
				disposeBack = false;
				rectText.Width++;
			}
			else
				backBrush = new SolidBrush(_menuCommands.ExtraBackColor);

			// Fill background using brush
			g.FillRectangle(backBrush, rectText);
			}
			finally
			{
			// Do we need to dispose of the brush?
			if (disposeBack)
				backBrush.Dispose();
			}

			// Adjust rectangle for drawing the text into
			rectText.X += _position[(int)_style, (int)PI.ExtraWidthGap];
			rectText.Y += _position[(int)_style, (int)PI.ExtraHeightGap];
			rectText.Width -= _position[(int)_style, (int)PI.ExtraWidthGap] * 2;
			rectText.Height -= _position[(int)_style, (int)PI.ExtraHeightGap] * 2;

			// For Plain style we need to take into account the border sizes
			if (_style == VisualStyle.Plain)
				rectText.Height -= SystemInformation.Border3DSize.Height * 2;

			// Draw the text into this rectangle
			using (StringFormat format = new StringFormat())
			{
			format.FormatFlags = StringFormatFlags.DirectionVertical |
								 StringFormatFlags.NoClip |
								 StringFormatFlags.NoWrap;
			format.Alignment = StringAlignment.Near;
			format.LineAlignment = StringAlignment.Center;

			Brush textBrush = null;
			bool disposeText = true;
				try
				{
			if (_menuCommands.ExtraTextBrush != null)
			{
				textBrush = _menuCommands.ExtraTextBrush;
				disposeText = false;
			}
			else
				textBrush = new SolidBrush(_menuCommands.ExtraTextColor);

			// Draw string from bottom of area towards the top using the given Font/Brush
			TextUtil.DrawReverseString(g, _menuCommands.ExtraText, _menuCommands.ExtraFont, rectText, textBrush, format);
				}
				finally
				{
			// Do we need to dispose of the brush?
			if (disposeText)
				textBrush.Dispose();
		}
			}
		}

		internal void DrawSingleCommand(Graphics g, DrawCommand dc, bool hotCommand)
		{
			Rectangle drawRect = dc.DrawRect;
			MenuCommand mc = dc.MenuCommand;

			// Remember some often used values
			int textGapLeft = _position[(int)_style, (int)PI.TextGapLeft];
			int imageGapLeft = _position[(int)_style, (int)PI.ImageGapLeft];
			int imageGapRight = _position[(int)_style, (int)PI.ImageGapRight];
			int imageLeft = drawRect.Left + imageGapLeft;

			// Calculate some common values
			int imageColWidth = imageGapLeft + _imageWidth + imageGapRight;

//			int subMenuWidth = _position[(int)_style, (int)PI.SubMenuGapLeft] +
//							   _position[(int)_style, (int)PI.SubMenuWidth] +
//							   _position[(int)_style, (int)PI.SubMenuGapRight];

			int subMenuX = drawRect.Right -
						   _position[(int)_style, (int)PI.SubMenuGapRight] -
						   _position[(int)_style, (int)PI.SubMenuWidth];

			// Text drawing rectangle needs to know the right most position for drawing
			// to stop. This is the width of the window minus the relevant values
			int shortCutX = subMenuX -
							_position[(int)_style, (int)PI.SubMenuGapLeft] -
							_position[(int)_style, (int)PI.TextGapRight];

			// Is this item an expansion command?
			if (dc.Expansion)
			{
				Rectangle box = drawRect;

				// In IDE style the box is next to the image column
				if (_style == VisualStyle.IDE)
				{
					// Reduce the box to take into account the column
					box.X += imageColWidth;
					box.Width -= imageColWidth;
				}

				// Find centre for drawing the image
				int xPos = box.Left + ((box.Width - _imageHeight) / 2);
				;
				int yPos = box.Top + ((box.Height - _imageHeight) / 2);

				switch(_style)
				{
				case VisualStyle.IDE:
					g.FillRectangle(SystemBrushes.ControlLightLight, box);
					break;
				case VisualStyle.Plain:
					g.FillRectangle(SystemBrushes.Control, box);
					break;
				}

				// Should the item look selected
				if (hotCommand)
				{
					switch(_style)
					{
					case VisualStyle.IDE:
						Rectangle selectArea = new Rectangle(drawRect.Left + 1, drawRect.Top,
															 drawRect.Width - 3, drawRect.Height - 1);

						using (Pen selectPen = new Pen(ColorUtil.VSNetBorderColor))
						{
							// Draw the selection area white, because we are going to use an alpha brush
							using (SolidBrush whiteBrush = new SolidBrush(Color.White))
								g.FillRectangle(whiteBrush, selectArea);

							using (SolidBrush selectBrush = new SolidBrush(Color.FromArgb(70,ColorUtil.VSNetBorderColor)))
							{
								// Draw the selection area
								g.FillRectangle(selectBrush, selectArea);

								// Draw a border around the selection area
								g.DrawRectangle(selectPen, selectArea);
							}
						}
						break;
					case VisualStyle.Plain:
						// Shrink the box to provide a small border
						box.Inflate(-2, -2);

						// Grab common values
						//Color baseColor = SystemColors.Control;

						using (Pen lightPen = new Pen(SystemColors.ControlLightLight),
								   darkPen = new Pen(SystemColors.ControlDarkDark))
						{
							g.DrawLine(lightPen, box.Right, box.Top, box.Left, box.Top);
							g.DrawLine(lightPen, box.Left, box.Top, box.Left, box.Bottom);
							g.DrawLine(darkPen, box.Left, box.Bottom, box.Right, box.Bottom);
							g.DrawLine(darkPen, box.Right, box.Bottom, box.Right, box.Top);
						}
						break;
					}
				}
				else
				{
					switch(_style)
					{
					case VisualStyle.IDE:
						// Fill the entire drawing area with white
						using (SolidBrush whiteBrush = new SolidBrush(SystemColors.ControlLightLight))
							g.FillRectangle(whiteBrush, new Rectangle(drawRect.Left + 1, drawRect.Top,
																	  drawRect.Width - 1, drawRect.Height));

						Rectangle imageCol = new Rectangle(drawRect.Left, drawRect.Top,
														   imageColWidth, drawRect.Height);

						// Draw the image column background
						g.FillRectangle(SystemBrushes.Control, imageCol);
						break;
					case VisualStyle.Plain:
							g.FillRectangle(SystemBrushes.Control, new Rectangle(drawRect.Left, drawRect.Top,
																				 drawRect.Width, drawRect.Height));
						break;
					}

				}

				// Always draw the expansion bitmap
				g.DrawImage(_menuImages.Images[(int)ImageIndex.Expansion], xPos, yPos);
			}
			else
			{
				// Is this item a separator?
				if (dc.Separator)
				{
					if (dc.VerticalSeparator)
					{
						switch(_style)
						{
						case VisualStyle.IDE:
							// Draw the separator as a single line
							using (Pen separatorPen = new Pen(SystemColors.ControlDark))
								g.DrawLine(separatorPen, drawRect.Left, drawRect.Top, drawRect.Left, drawRect.Bottom);
							break;
						case VisualStyle.Plain:
							Color baseColor = SystemColors.Control;
							ButtonBorderStyle bsInset = ButtonBorderStyle.Inset;
							ButtonBorderStyle bsNone = ButtonBorderStyle.Inset;

							Rectangle sepRect = new Rectangle(drawRect.Left + 1, drawRect.Top, 2, drawRect.Height);

							// Draw the separator as two lines using Inset style
							ControlPaint.DrawBorder(g, sepRect,
													baseColor, 1, bsInset, baseColor, 0, bsNone,
													baseColor, 1,
								bsInset, baseColor, 0, bsNone);
							break;
						}
					}
					else
					{
						switch(_style)
						{
						case VisualStyle.IDE:
							// Draw the image column background
							Rectangle imageCol = new Rectangle(drawRect.Left, drawRect.Top,
															   imageColWidth,
															   drawRect.Height);

							g.FillRectangle(new SolidBrush(ColorUtil.VSNetBackgroundColor), drawRect);
							g.FillRectangle(new SolidBrush(ColorUtil.VSNetControlColor), imageCol);

							// Draw a separator
							using (Pen separatorPen = new Pen(Color.FromArgb(75, SystemColors.MenuText)))
							{
								// Draw the separator as a single line
								g.DrawLine(separatorPen, drawRect.Left + imageColWidth + textGapLeft, drawRect.Top + 1,
										   drawRect.Right-4, drawRect.Top + 1);

							}
							break;
						case VisualStyle.Plain:
							Color baseColor = SystemColors.Control;
							ButtonBorderStyle bsInset = ButtonBorderStyle.Inset;
							ButtonBorderStyle bsNone = ButtonBorderStyle.Inset;

							Rectangle sepRect = new Rectangle(drawRect.Left + 2, drawRect.Top + 1,
															  drawRect.Width - 4, 2);

							// Draw the separator as two lines using Inset style
							ControlPaint.DrawBorder(g, sepRect,
													baseColor, 0, bsNone, baseColor, 1, bsInset,
													baseColor, 0,
								bsNone, baseColor, 1, bsInset);
							break;
						}
					}
				}
				else
				{
					// Should the command be drawn selected?
					if (hotCommand && mc.ComboBox == null )
					{
						switch(_style)
						{
						case VisualStyle.IDE:
							Rectangle selectArea = new Rectangle(drawRect.Left + 1, drawRect.Top,
																 drawRect.Width - 3, drawRect.Height - 1);

							using (Pen selectPen = new Pen(ColorUtil.VSNetBorderColor))
							{
								// Draw the selection area white, because we are going to use an alpha brush
								using (SolidBrush whiteBrush = new SolidBrush(Color.White))
									g.FillRectangle(whiteBrush, selectArea);

								using (SolidBrush selectBrush = new SolidBrush(Color.FromArgb(70, ColorUtil.VSNetBorderColor)))
								{
									// Draw the selection area
									g.FillRectangle(selectBrush, selectArea);

									// Draw a border around the selection area
									g.DrawRectangle(selectPen, selectArea);
								}
							}
							break;
						case VisualStyle.Plain:
							using (SolidBrush selectBrush = new SolidBrush(ColorUtil.VSNetBorderColor))
								g.FillRectangle(selectBrush, drawRect);
							break;
						}
					}
					else
					{
						switch(_style)
						{
						case VisualStyle.IDE:
							// Fill the entire drawing area with white
							using (SolidBrush whiteBrush = new SolidBrush(ColorUtil.VSNetBackgroundColor))
								g.FillRectangle(whiteBrush, new Rectangle(drawRect.Left + 1, drawRect.Top,
																		  drawRect.Width - 1, drawRect.Height));

							Rectangle imageCol = new Rectangle(drawRect.Left, drawRect.Top,
															   imageColWidth, drawRect.Height);

							// Draw the image column background
							g.FillRectangle(new SolidBrush(ColorUtil.VSNetControlColor), imageCol);

							// If this is a combobox item, make sure to position the combobox a couple
							// of pixel after the icon area
							if ( mc.ComboBox != null )
							{
								// Combobox will paint itself
								mc.ComboBox.Left = drawRect.Left+imageColWidth + 2;
								mc.ComboBox.Top = drawRect.Top + (drawRect.Height - mc.ComboBox.Height)/2;

							}

							break;
						case VisualStyle.Plain:
							g.FillRectangle(SystemBrushes.Control, new Rectangle(drawRect.Left, drawRect.Top,
																				 drawRect.Width, drawRect.Height));
							break;
						}
					}

					int leftPos = drawRect.Left + imageColWidth + textGapLeft;

					// Calculate text drawing rectangle
					Rectangle strRect = new Rectangle(leftPos, drawRect.Top, shortCutX - leftPos, drawRect.Height);

					// Left align the text drawing on a single line centered vertically
					// and process the & character to be shown as an underscore on next character
					using (StringFormat format = new StringFormat())
					{
					format.FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap;
					format.Alignment = StringAlignment.Near;
					format.LineAlignment = StringAlignment.Center;
					format.HotkeyPrefix = HotkeyPrefix.Show;

						SolidBrush textBrush = null;
						try
						{
					// Create brush depending on enabled state
					if (mc.Enabled)
					{
						if (!hotCommand || (_style == VisualStyle.IDE))
							textBrush = new SolidBrush(SystemColors.MenuText);
						else
							textBrush = new SolidBrush(SystemColors.HighlightText);
					}
					else
						textBrush = new SolidBrush(SystemColors.GrayText);

					// Helper values used when drawing grayed text in plain style
					Rectangle rectDownRight = strRect;
					rectDownRight.Offset(1,1);

					if (mc.Enabled || (_style == VisualStyle.IDE))
						g.DrawString(mc.Text, _textFont, textBrush, strRect, format);
					else
					{
						// Draw grayed text by drawing white string offset down and right
						using (SolidBrush whiteBrush = new SolidBrush(SystemColors.HighlightText))
							g.DrawString(mc.Text, _textFont, whiteBrush, rectDownRight, format);

						// And then draw in corret color offset up and left
						g.DrawString(mc.Text, _textFont, textBrush, strRect, format);
					}

					if (mc.Shortcut != Shortcut.None)
					{
						// Right align the shortcut drawing
						format.Alignment = StringAlignment.Far;

						if (mc.Enabled || (_style == VisualStyle.IDE))
						{
							// Draw the shortcut text
							g.DrawString(GetShortcutText(mc.Shortcut), _textFont, textBrush, strRect, format);
						}
						else
						{
							// Draw grayed text by drawing white string offset down and right
							using (SolidBrush whiteBrush = new SolidBrush(SystemColors.HighlightText))
								g.DrawString(GetShortcutText(mc.Shortcut), _textFont, whiteBrush, rectDownRight, format);

							// And then draw in corret color offset up and left
							g.DrawString(GetShortcutText(mc.Shortcut), _textFont, textBrush, strRect, format);
						}
					}
						}
						finally
						{
							textBrush.Dispose();
						}
					}

					// The image offset from top of cell is half the space left after
					// subtracting the height of the image from the cell height
					int imageTop = drawRect.Top + (drawRect.Height - _imageHeight) / 2;

					Image image = null;

					// Should a check mark be drawn?
					if (mc.Checked)
					{
						switch(_style)
						{
						case VisualStyle.IDE:
								using (Pen boxPen = mc.Enabled ?
									new Pen(ColorUtil.VSNetBorderColor) :
									new Pen(SystemColors.GrayText))
								{
							// Draw the box around the checkmark area
							g.DrawRectangle(boxPen, new Rectangle(imageLeft - 1, imageTop - 1,
																  _imageHeight + 2, _imageWidth + 2));
								}
							break;
						case VisualStyle.Plain:
							break;
						}

						// Grab either tick or radio button image
						if (mc.RadioCheck)
						{
							if (hotCommand && (_style == VisualStyle.Plain))
								image = _menuImages.Images[(int)ImageIndex.RadioSelected];
							else
								image = _menuImages.Images[(int)ImageIndex.Radio];
						}
						else
						{
							if (hotCommand && (_style == VisualStyle.Plain))
								image = _menuImages.Images[(int)ImageIndex.CheckSelected];
							else
								image = _menuImages.Images[(int)ImageIndex.Check];
						}
					}
					else
					{
						try
						{
							// Is there an image available to be drawn?
							if ((mc.ImageList != null) && (mc.ImageIndex >= 0))
								image = mc.ImageList.Images[mc.ImageIndex];
							else if ( mc.Image != null)
								image = mc.Image;
						}
						catch(Exception)
						{
							// User supplied ImageList/ImageIndex are invalid, use an error image instead
							image = _menuImages.Images[(int)ImageIndex.ImageError];
						}
					}

					// Is there an image to be drawn?
					if (image != null)
					{
						if (mc.Enabled)
						{
							if ((hotCommand) && (!mc.Checked) && (_style == VisualStyle.IDE))
							{
								// Draw a disabled icon offset down and right
								ControlPaint.DrawImageDisabled(g, image, imageLeft + 1, imageTop + 1,
															   SystemColors.HighlightText);

								// Draw an enabled icon offset up and left
								g.DrawImage(image, imageLeft - 1, imageTop - 1);
							}
							else
							{
								// Draw an enabled icon
								g.DrawImage(image, imageLeft, imageTop);
							}
						}
						else
						{
							// Draw a image disabled
							ControlPaint.DrawImageDisabled(g, image, imageLeft, imageTop,
														   SystemColors.HighlightText);
						}
					}

					// Does the menu have a submenu defined?
					if (dc.SubMenu)
					{
						// Is the item enabled?
						if (mc.Enabled)
						{
							int subMenuIndex = (int)ImageIndex.SubMenu;

							if (hotCommand && (_style == VisualStyle.Plain))
								subMenuIndex = (int)ImageIndex.SubMenuSelected;

							// Draw the submenu arrow
							g.DrawImage(_menuImages.Images[subMenuIndex], subMenuX, imageTop);
						}
						else
						{
							// Draw a image disabled
							ControlPaint.DrawImageDisabled(g, _menuImages.Images[(int)ImageIndex.SubMenu],
														   subMenuX, imageTop, SystemColors.HighlightText);
						}
					}
				}
			}
		}

		protected void DrawAllCommands(Graphics g)
		{
			for(int i=0; i<_drawCommands.Count; i++)
			{
				// Grab some commonly used values
				DrawCommand dc = _drawCommands[i] as DrawCommand;

				// Draw this command only
				DrawSingleCommand(g, dc, (i == _trackItem));
			}
		}

		protected string GetShortcutText(Shortcut shortcut)
		{
			// Get the key code
			char keycode = (char)((int)shortcut & 0x0000FFFF);

			// The type converter does not work for numeric values as it returns
			// Alt+D0 instad of Alt+0. So check for numeric keys and construct the
			// return string ourself.
			if ((keycode >= '0') && (keycode <= '9'))
			{
				string display = "";

				// Get the modifier
				int modifier = (int)((int)shortcut & 0xFFFF0000);

				if ((modifier & 0x00010000) != 0)
					display += "Shift+";

				if ((modifier & 0x00020000) != 0)
					display += "Ctrl+";

				if ((modifier & 0x00040000) != 0)
					display += "Alt+";

				display += keycode;

				return display;
			}

			return TypeDescriptor.GetConverter(typeof(Keys)).ConvertToString((Keys)shortcut);
		}

		protected bool ProcessKeyUp()
		{
			int newItem = _trackItem;
			int startItem = newItem;

			for(int i=0; i<_drawCommands.Count; i++)
			{
				// Move to previous item
				newItem--;

				// Have we looped all the way around all the choices
				if (newItem == startItem)
					return false;

				// Check limits
				if (newItem < 0)
					newItem = _drawCommands.Count - 1;

				DrawCommand dc = _drawCommands[newItem] as DrawCommand;

				// Can we select this item?
				if (!dc.Separator && dc.Enabled)
				{
					// If a change has occured
					if (newItem != _trackItem)
					{
						// Modify the display of the two items
						SwitchSelection(_trackItem, newItem, false, false);

						return true;
					}
				}
			}

			return false;
		}

		protected bool ProcessKeyDown()
		{
			int newItem = _trackItem;
			//int startItem = newItem;

			for(int i=0; i<_drawCommands.Count; i++)
			{
				// Move to previous item
				newItem++;

				// Check limits
				if (newItem >= _drawCommands.Count)
					newItem = 0;

				DrawCommand dc = _drawCommands[newItem] as DrawCommand;

				// Can we select this item?
				if (!dc.Separator && dc.Enabled)
				{
					// If a change has occured
					if (newItem != _trackItem)
					{
						// Modify the display of the two items
						SwitchSelection(_trackItem, newItem, false, false);

						return true;
					}
				}
			}

			return false;
		}

		protected void ProcessKeyLeft()
		{
			if (_trackItem != -1)
			{
				// Get the col this item is in
				DrawCommand dc = _drawCommands[_trackItem] as DrawCommand;

				// Grab the current column/row values
				int col = dc.Col;
				int row = dc.Row;

				// If not in the first column then move left one
				if (col > 0)
				{
					int newItem = -1;
					int newRow = -1;
					int findCol = col - 1;
					DrawCommand newDc = null;

					for(int i=0; i<_drawCommands.Count; i++)
					{
						DrawCommand listDc = _drawCommands[i] as DrawCommand;

						// Interesting in cells in the required column
						if (listDc.Col == findCol)
						{
							// Is this Row nearer to the one required than those found so far?
							if ((listDc.Row <= row) && (listDc.Row > newRow) &&
								!listDc.Separator && listDc.Enabled)
							{
								// Remember this item
								newRow = listDc.Row;
								newDc = listDc;
								newItem = i;
							}
						}
					}

					if (newDc != null)
					{
						// Track the new item
						// Modify the display of the two items
						SwitchSelection(_trackItem, newItem, false, false);

						return;
					}
				}

				// Are we the first submenu of a parent control?
				bool autoLeft = (_parentMenu == null) && (_parentControl != null);

				// Do we have a parent menu?
				if ((_parentMenu != null) || autoLeft)
				{
					// Tell the parent on return that nothing was selected
					_returnCommand = null;

					// Finish processing messages
					_timer.Stop();
					_exitLoop = true;

					if (autoLeft)
						_returnDir = -1;
				}
			}
		}

		protected bool ProcessKeyRight()
		{
			// Are we the first submenu of a parent control?
			bool autoRight = (_parentControl != null);

			bool checkKeys = false;

			bool ret = false;

			// Is an item currently selected?
			if (_trackItem != -1)
			{
				DrawCommand dc = _drawCommands[_trackItem] as DrawCommand;

				// Does this item have a submenu?
				if (dc.SubMenu)
				{
					// Consume the keyboard message to prevent the submenu immediately
					// processing the same message again. Remember this routine is called
					// after PeekMessage but the message is still on the queue at this point
					Win32.MSG msg = new Win32.MSG();
					WindowsAPI.GetMessage(ref msg, 0, 0, 0);

					// Handle the submenu
					OperateSubMenu(_trackItem, true);

					ret = true;
				}
				else
				{
					// Grab the current column/row values
					int col = dc.Col;
					int row = dc.Row;

					// If not in the first column then move left one
					int newItem = -1;
					int newRow = -1;
					int findCol = col + 1;
					DrawCommand newDc = null;

					for(int i=0; i<_drawCommands.Count; i++)
					{
						DrawCommand listDc = _drawCommands[i] as DrawCommand;

						// Interesting in cells in the required column
						if (listDc.Col == findCol)
						{
							// Is this Row nearer to the one required than those found so far?
							if ((listDc.Row <= row) && (listDc.Row > newRow) &&
								!listDc.Separator && listDc.Enabled)
							{
								// Remember this item
								newRow = listDc.Row;
								newDc = listDc;
								newItem = i;
							}
						}
					}

					if (newDc != null)
					{
						// Track the new item
						// Modify the display of the two items
						SwitchSelection(_trackItem, newItem, false, false);
					}
					else
						checkKeys = true;
				}
			}
			else
			{
				if (_parentMenu != null)
				{
					if (!ProcessKeyDown())
						checkKeys = true;
				}
				else
					checkKeys = true;
			}

			// If we have a parent control and nothing to move right into
			if (autoRight && checkKeys)
			{
				_returnCommand = null;

				// Finish processing messages
				_timer.Stop();
				_exitLoop = true;

				_returnDir = 1;
			}

			return ret;
		}

		protected int ProcessMnemonicKey(char key)
		{
			// Check against each draw command mnemonic
			for(int i=0; i<_drawCommands.Count; i++)
			{
				DrawCommand dc = _drawCommands[i] as DrawCommand;

				if (dc.Enabled)
				{
					// Does the character match?
					if (key == dc.Mnemonic)
						return i;
				}
			}

			// No match found
			return -1;
		}

		protected bool ParentWantsMouseMessage(ref Win32.MSG msg)
		{
			Win32.POINT screenPos;
			screenPos.x = (int)((uint)msg.lParam & 0x0000FFFFU);
			screenPos.y = (int)(((uint)msg.lParam & 0xFFFF0000U) >> 16);

			// Convert the mouse position to screen coordinates
			WindowsAPI.ClientToScreen(msg.hwnd, ref screenPos);

			// Special case the MOUSEMOVE so if we are part of a MenuControl
			// then we should always allow mousemoves to be processed
			if ((msg.message == (int)Msg.WM_MOUSEMOVE) && (_parentControl != null))
			{
				Win32.RECT rectRaw = new Win32.RECT();

				// Grab the screen rectangle of the parent control
				WindowsAPI.GetWindowRect(_parentControl.Handle, ref rectRaw);

				if ((screenPos.x >= rectRaw.left) &&
					(screenPos.x <= rectRaw.right) &&
					(screenPos.y >= rectRaw.top) &&
					(screenPos.y <= rectRaw.bottom))
					return true;
			}

			if (_parentMenu != null)
				return _parentMenu.WantMouseMessage(screenPos);
			else
				return false;
		}

		protected bool WantMouseMessage(Win32.POINT screenPos)
		{
			Win32.RECT rectRaw = new Win32.RECT();

			// Grab the screen rectangle of the window
			WindowsAPI.GetWindowRect(this.Handle, ref rectRaw);

			bool want = ((screenPos.x >= rectRaw.left) &&
						 (screenPos.x <= rectRaw.right) &&
						 (screenPos.y >= rectRaw.top) &&
						 (screenPos.y <= rectRaw.bottom));

			if (!want && (_parentMenu != null))
				want = _parentMenu.WantMouseMessage(screenPos);

			return want;
		}

		protected void SwitchSelection(int oldItem, int newItem, bool mouseChange, bool reverting)
		{
			bool updateWindow = false;

			// Create a graphics object for drawing with
			using(Graphics g = Graphics.FromHwnd(this.Handle))
			{
				// Deselect the old draw command
				if (oldItem != -1)
				{
					//DrawCommand dc = _drawCommands[oldItem] as DrawCommand;

					// Draw old item not selected
					if (_layered)
						updateWindow = true;
					else
						DrawSingleCommand(g, _drawCommands[oldItem] as DrawCommand, false);
				}

				if (newItem != -1)
				{
					// Stop the timer as a new selection has occured
					_timer.Stop();

					// Do we have a child menu?
					if (!reverting && (_childMenu != null))
					{
						// Start timer to test if it should be dismissed
						_timer.Start();
					}

					DrawCommand dc = _drawCommands[newItem] as DrawCommand;

					// Select the new draw command
					if (!dc.Separator && dc.Enabled)
					{
						// Draw the newly selected item
						if (_layered)
							updateWindow = true;
						else
							DrawSingleCommand(g, dc, true);

						// Only is mouse movement caused the selection change...
						if (!reverting && mouseChange)
						{
							//...should we start a timer to test for sub menu displaying
							_timer.Start();
						}
					}
					else
					{
						// Cannot become selected
						newItem = -1;
					}
				}

				// Remember the new selection
				_trackItem = newItem;

				if (_layered && updateWindow)
				{
					// Update the image for display
					UpdateLayeredWindow();
				}
			}
		}

		protected void OnTimerExpire(object sender, EventArgs e)
		{
			// Prevent it expiring again
			_timer.Stop();

			bool showPopup = true;

			// Is a popup menu already being displayed?
			if (_childMenu != null)
			{
				// If the submenu popup is for a different item?
				if (_popupItem != _trackItem)
				{
					// Then need to kill the submenu
					WindowsAPI.PostMessage(_childMenu.Handle, WM_DISMISS, 0, 0);
				}
				else
					showPopup = false;
			}

			// Should we show the popup for this item
			if (showPopup)
			{
				// Check an item really is selected
				if (_trackItem != -1)
				{
					DrawCommand dc = _drawCommands[_trackItem] as DrawCommand;

					// Does this item have a submenu?
					if (dc.SubMenu)
						OperateSubMenu(_trackItem, false);
					else
					{
						if (dc.Expansion)
							RegenerateExpansion();
					}
				}
			}
		}

		protected void OperateSubMenu(int popupItem, bool selectFirst)
		{
			_popupItem = popupItem;
			_childMenu = new PopupMenu();

			DrawCommand dc = _drawCommands[popupItem] as DrawCommand;

			// Find screen coordinate of Top right of item cell
			Win32.POINT screenPosTR;
			screenPosTR.x = dc.DrawRect.Right;
			screenPosTR.y = dc.DrawRect.Top;
			WindowsAPI.ClientToScreen(this.Handle, ref screenPosTR);

			// Find screen coordinate of top left of item cell
			Win32.POINT screenPosTL;
			screenPosTL.x = dc.DrawRect.Left;
			screenPosTL.y = dc.DrawRect.Top;
			WindowsAPI.ClientToScreen(this.Handle, ref screenPosTL);

			// Ensure the child has the same properties as ourself
			_childMenu.Style = this.Style;
			_childMenu.Font = this.Font;

			// Record keyboard direction
			int returnDir = 0;

			_returnCommand = _childMenu.InternalTrackPopup(new Point(screenPosTR.x, screenPosTR.y),
														   new Point(screenPosTL.x, screenPosTL.y),
														   dc.MenuCommand.MenuCommands,
														   this,
														   selectFirst,
														   _parentControl,
														   _popupRight,
														   _popupDown,
														   ref returnDir);

			_popupItem = -1;;
			_childMenu = null;

			if ((_returnCommand != null) || (returnDir != 0))
			{
				// Finish processing messages
				_timer.Stop();
				_exitLoop = true;
				_returnDir = returnDir;
			}
		}

		protected void GrabTheFocus()
		{
			_oldFocus = WindowsAPI.GetFocus();

			// Is the focus on a control/window?
			if (_oldFocus != IntPtr.Zero)
			{
				IntPtr hParent = WindowsAPI.GetParent(_oldFocus);

				// Did we find a parent window?
				if (hParent != IntPtr.Zero)
				{
					// Create a new destination for the focus
					_focusCatcher = new FocusCatcher(hParent);

					// Park focus at the temporary window
					WindowsAPI.SetFocus(_focusCatcher.Handle);

					// Kill any capturing of the mouse
					WindowsAPI.ReleaseCapture();
				}
			}

			_grabFocus = false;
		}

		protected void ReturnTheFocus()
		{
			// Restore focus to origin
			WindowsAPI.SetFocus(_oldFocus);

			// Did we use an temporary parking location?
			if (_focusCatcher != null)
			{
				// Kill it
				_focusCatcher.DestroyHandle();
				_focusCatcher = null;
			}

			// Reset state
			_oldFocus = IntPtr.Zero;
		}

		protected void OnWM_PAINT(ref Message m)
		{
			// Paint message occurs after the window is created and we have
			// entered the message loop. So this is a good place to handle focus
			if (_grabFocus)
				GrabTheFocus();

			Win32.PAINTSTRUCT ps = new Win32.PAINTSTRUCT();

			// Have to call BeginPaint whenever processing a WM_PAINT message
			IntPtr hDC = WindowsAPI.BeginPaint(m.HWnd, ref ps);

			Win32.RECT rectRaw = new Win32.RECT();

			// Grab the screen rectangle of the window
			WindowsAPI.GetWindowRect(this.Handle, ref rectRaw);

			// Convert to a client size rectangle
			Rectangle rectWin = new Rectangle(0, 0,
				rectRaw.right - rectRaw.left,
				rectRaw.bottom - rectRaw.top);

			// Create a graphics object for drawing
			using(Graphics g = Graphics.FromHdc(hDC))
			{
				// Create bitmap for drawing onto
				using (Bitmap memoryBitmap = new Bitmap(rectWin.Width, rectWin.Height))
				{
				using(Graphics h = Graphics.FromImage(memoryBitmap))
				{
					// Draw the background area
					DrawBackground(h, rectWin);

					// Draw the actual menu items
					DrawAllCommands(h);
				}

				// Blit bitmap onto the screen
				g.DrawImageUnscaled(memoryBitmap, 0, 0);
			}
			}

			// Don't forget to end the paint operation!
			WindowsAPI.EndPaint(m.HWnd, ref ps);
		}

		protected void OnWM_ACTIVATEAPP(ref Message m)
		{
			// Another application has been activated, so we need to kill ourself
			_timer.Stop();
			_exitLoop = true;
		}

		protected void SubMenuMovement()
		{
			// Cancel timer to prevent auto closing of an open submenu
			_timer.Stop();

			// Has the selected item changed since child menu shown?
			if (_popupItem != _trackItem)
			{
				// Need to put it back again
				SwitchSelection(_trackItem, _popupItem, false, true);
			}

			// Are we a submenu?
			if (_parentMenu != null)
			{
				// Inform parent that we have movement and so do not
				// use a timer to close us up
				_parentMenu.SubMenuMovement();
			}
		}

		protected void OnWM_MOUSEMOVE(ref Message m)
		{
			// Are we a submenu?
			if (_parentMenu != null)
			{
				// Inform parent that we have movement and so do not
				// use a timer to close us up
				_parentMenu.SubMenuMovement();
			}

			// Is the first time we have noticed a mouse movement over our window
			if (!_mouseOver)
			{
				// Crea the structure needed for WindowsAPI call
				Win32.TRACKMOUSEEVENTS tme = new Win32.TRACKMOUSEEVENTS();

				// Fill in the structure
				tme.cbSize = 16;
				tme.dwFlags = (uint)Win32.TrackerEventFlags.TME_LEAVE;
				tme.hWnd = this.Handle;
				tme.dwHoverTime = 0;

				// Request that a message gets sent when mouse leaves this window
				WindowsAPI.TrackMouseEvent(ref tme);

				// Yes, we know the mouse is over window
				_mouseOver = true;
			}

			// Extract the mouse position
			int xPos = (int)((uint)m.LParam & 0x0000FFFFU);
			int yPos = (int)(((uint)m.LParam & 0xFFFF0000U) >> 16);

			Point pos = new Point(xPos, yPos);

			// Has mouse position really changed since last time?
			if (_lastMousePos != pos)
			{
				for(int i=0; i<_drawCommands.Count; i++)
				{
					DrawCommand dc = _drawCommands[i] as DrawCommand;

					if (dc.DrawRect.Contains(pos))
					{
						// Is there a change in selected item?
						if (_trackItem != i)
						{
							// Modify the display of the two items
							SwitchSelection(_trackItem, i, true, false);
						}
					}
				}

				// Remember for next time around
				_lastMousePos = pos;
			}
		}

		protected void OnWM_MOUSELEAVE()
		{
			// Deselect the old draw command if not showing a child menu
			if ((_trackItem != -1) && (_childMenu == null))
			{
				// Modify the display of the two items
				SwitchSelection(_trackItem, -1, false, false);
			}

			// Reset flag so that next mouse move start monitor for mouse leave message
			_mouseOver = false;

			// No point having a last mouse position
			_lastMousePos = new Point(-1,-1);
		}

		protected void OnWM_XBUTTONUP(ref Message m)
		{
			// Extract the mouse position
			int xPos = (int)((uint)m.LParam & 0x0000FFFFU);
			int yPos = (int)(((uint)m.LParam & 0xFFFF0000U) >> 16);

			Point pos = new Point(xPos, yPos);

			for(int i=0; i<_drawCommands.Count; i++)
			{
				DrawCommand dc = _drawCommands[i] as DrawCommand;

				if (dc.DrawRect.Contains(pos))
				{
					// Is there a change in selected item?
					if (_trackItem != i)
					{
						// Modify the display of the two items
						SwitchSelection(_trackItem, i, false, false);
					}
				}
			}

			// Is an item selected?
			if (_trackItem != -1)
			{
				DrawCommand dc = _drawCommands[_trackItem] as DrawCommand;

				// Does this item have a submenu?
				if (dc.SubMenu)
				{
					// If we are not already showing this submenu...
					if (_popupItem != _trackItem)
					{
						// Is a submenu for a different item showing?
						if (_childMenu != null)
						{
							// Inform the child menu it is no longer needed
							WindowsAPI.PostMessage(_childMenu.Handle, WM_DISMISS, 0, 0);
						}

						// Handle the submenu
						OperateSubMenu(_trackItem, false);
					}
				}
				else
				{
					if (dc.Expansion)
						RegenerateExpansion();
					else
					{
						// Kill any child menus open
						if (_childMenu != null)
						{
							// Inform the child menu it is no longer needed
							WindowsAPI.PostMessage(_childMenu.Handle, WM_DISMISS, 0, 0);
						}

						// Define the selection to return to caller
						_returnCommand = dc.MenuCommand;

						// Finish processing messages
						_timer.Stop();
						_exitLoop = true;
					}
				}
			}
		}

		protected void OnWM_MOUSEACTIVATE(ref Message m)
		{
			// Do not allow then mouse down to activate the window, but eat
			// the message as we still want the mouse down for processing
			m.Result = (IntPtr)Win32.MouseActivateFlags.MA_NOACTIVATE;
		}

		protected void OnWM_SETCURSOR(ref Message m)
		{
			// Always use the arrow cursor
			WindowsAPI.SetCursor(WindowsAPI.LoadCursor(IntPtr.Zero, (uint)Win32.CursorType.IDC_ARROW));
		}

		protected void OnWM_DISMISS()
		{
			// Pass on to any child menu of ours
			if (_childMenu != null)
			{
				// Inform the child menu it is no longer needed
				WindowsAPI.PostMessage(_childMenu.Handle, WM_DISMISS, 0, 0);
			}

			// Define the selection to return to caller
			_returnCommand = null;

			// Finish processing messages
			_timer.Stop();
			_exitLoop = true;

			// Hide ourself
			WindowsAPI.ShowWindow(this.Handle, (short)Win32.ShowWindowStyles.SW_HIDE);

			// Kill ourself
			DestroyHandle();
		}

		protected override void WndProc(ref Message m)
		{

			// WM_DISMISS is not a constant and so cannot be in a switch
			if (m.Msg == WM_DISMISS)
				OnWM_DISMISS();
			else
			{
				// Want to notice when the window is maximized
				Msg cmsg = (Msg)m.Msg;
				Debug.WriteLine(cmsg.ToString());

				// Want to notice when the window is maximized
				switch(m.Msg)
				{
					case (int)Msg.WM_PAINT:
						OnWM_PAINT(ref m);
						break;
					case (int)Msg.WM_ACTIVATEAPP:
						OnWM_ACTIVATEAPP(ref m);
						break;
					case (int)Msg.WM_MOUSEMOVE:
						OnWM_MOUSEMOVE(ref m);
						break;
					case (int)Msg.WM_MOUSELEAVE:
						OnWM_MOUSELEAVE();
						break;
					case (int)Msg.WM_LBUTTONUP:
					case (int)Msg.WM_MBUTTONUP:
					case (int)Msg.WM_RBUTTONUP:
						OnWM_XBUTTONUP(ref m);
						break;
					case (int)Msg.WM_MOUSEACTIVATE:
						OnWM_MOUSEACTIVATE(ref m);
						break;
					case (int)Msg.WM_SETCURSOR:
						OnWM_SETCURSOR(ref m);
						break;
					default:
						base.WndProc(ref m);
						break;
				}
			}
		}
	}
}
