// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.PlatformUtilities;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class supports rendering of TabControl correctly when using bottom alignment with visual
	/// styles enabled. When ones are disabled the default method of rendering is used. Adapted from
	/// XPTabControl by Vladimir Svyatsky.
	/// </summary>
	public partial class VSTabControl : TabControl
	{
		//This field tells us whether custom drawing is turned on.
		private bool fCustomDraw;
		/* Handle to the font used for custom drawing. We do not use this native font directly but tab control
		 * adjusts size of tabs and tab scroller being based on the size of that font.*/
		private IntPtr fSysFont = IntPtr.Zero;
		/* We have to remember the index of last hot tab for our native updown hook to overdraw that tab as
		 * normal when mouse is moving over it.*/
		private int lastHotIndex = -1;
		//handle to our hook
		private NativeUpDown fUpDown;

		/// <summary />
		public VSTabControl()
		{
			InitializeComponent();
			fUpDown = new NativeUpDown(this);
		}

		#region Some overridden properties

		/// <summary>
		/// Gets or sets the area of the control (for example, along the top) where the tabs are aligned.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// One of the <see cref="T:System.Windows.Forms.TabAlignment"/> values. The default is Top.
		/// </returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
		/// The property value is not a valid <see cref="T:System.Windows.Forms.TabAlignment"/> value.
		/// </exception>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
		/// 	<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[Browsable(true), DefaultValue(TabAlignment.Bottom)]
		public new TabAlignment Alignment
		{
			get { return base.Alignment; }
			set
			{
				if (value <= TabAlignment.Bottom)
				{
					base.Alignment = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the control's tabs change in appearance when the mouse passes over them.
		/// </summary>
		/// <value></value>
		/// <returns>true if the tabs change in appearance when the mouse passes over them; otherwise, false. The default is false.
		/// </returns>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
		/// 	<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[Browsable(true), DefaultValue(true)]
		public new bool HotTrack
		{
			get { return base.HotTrack; }
			set { base.HotTrack = value; }
		}

		/// <summary>
		/// Gets or sets the visual appearance of the control's tabs.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// One of the <see cref="T:System.Windows.Forms.TabAppearance"/> values. The default is Normal.
		/// </returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
		/// The property value is not a valid <see cref="T:System.Windows.Forms.TabAppearance"/> value.
		/// </exception>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
		/// 	<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
		EditorBrowsable(EditorBrowsableState.Never)]
		public new TabAppearance Appearance
		{
			get { return base.Appearance; }
			set
			{
				if (value == TabAppearance.Normal)
				{
					base.Appearance = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the way that the control's tabs are drawn.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// One of the <see cref="T:System.Windows.Forms.TabDrawMode"/> values. The default is Normal.
		/// </returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
		/// The property value is not a valid <see cref="T:System.Windows.Forms.TabDrawMode"/> value.
		/// </exception>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
		/// 	<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
		EditorBrowsable(EditorBrowsableState.Never)]
		public new TabDrawMode DrawMode
		{
			get { return base.DrawMode; }
			set
			{
				if (value == TabDrawMode.Normal)
				{
					base.DrawMode = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether control's elements are aligned to support locales using right-to-left fonts.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// One of the <see cref="T:System.Windows.Forms.RightToLeft"/> values. The default is <see cref="F:System.Windows.Forms.RightToLeft.Inherit"/>.
		/// </returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
		/// The assigned value is not one of the <see cref="T:System.Windows.Forms.RightToLeft"/> values.
		/// </exception>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
		/// 	<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		public override RightToLeft RightToLeft
		{
			get { return base.RightToLeft; }
			set
			{
				if (value == RightToLeft.No)
				{
					base.RightToLeft = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether right-to-left mirror placement is turned on.
		/// </summary>
		/// <value></value>
		/// <returns>true if right-to-left mirror placement is turned on; false for standard child control placement. The default is false.
		/// </returns>
		public override bool RightToLeftLayout
		{
			get { return base.RightToLeftLayout; }
			set
			{
				if (!value)
				{
					base.RightToLeftLayout = value;
				}
			}
		}

		#endregion

		/// <summary>
		/// Turns custom drawing on/off and sets native font for the control (it's required for tabs to
		/// adjust their size correctly). If one doesn't install native font manually then Windows will
		/// install ugly system font for the control.
		/// </summary>
		private void InitializeDrawMode()
		{
			fCustomDraw = Application.RenderWithVisualStyles && TabRenderer.IsSupported;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, this.fCustomDraw);
			UpdateStyles();
			if (fCustomDraw) //custom drawing will be used
			{
				if (fSysFont == IntPtr.Zero) fSysFont = Font.ToHfont();
				NativeMethods.SendMessage(Handle, NativeMethods.WM_SETFONT, fSysFont, (IntPtr)1);
			}
			else //default drawing will be used
			{
				/* Note that in the SendMessage call below we do not delete HFONT passed to control. If we do
				 * so we can see ugly system font. I think in this case the control deletes this font by itself
				 * when disposing or finalizing.*/
				NativeMethods.SendMessage(Handle, NativeMethods.WM_SETFONT, Font.ToHfont(), (IntPtr)1);
				//but we need to delete the font(if any) created when being in custom drawing mode
				if (fSysFont != IntPtr.Zero)
				{
					NativeMethods.DeleteObject(fSysFont);
					fSysFont = IntPtr.Zero;
				}
			}
		}

		/// <summary>
		/// This member overrides <see cref="M:System.Windows.Forms.Control.OnHandleCreated(System.EventArgs)"/>.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			//after the control has been created we should turn custom drawing on/off etc.
			InitializeDrawMode();
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.FontChanged"/> event.
		/// </summary>
		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			if (fCustomDraw)
			{
				/* The control is being custom drawn and managed font size is changed. We should inform system
				 * about such great event for it to adjust tabs' sizes. And certainly we have to create a new
				 * native font from managed one.*/
				if (fSysFont != IntPtr.Zero)
				{
					NativeMethods.DeleteObject(fSysFont);
				}
				fSysFont = Font.ToHfont();
				NativeMethods.SendMessage(Handle, NativeMethods.WM_SETFONT, fSysFont, (IntPtr)1);
			}
		}

		/// <summary>
		/// This member overrides <see cref="M:System.Windows.Forms.Control.WndProc(System.Windows.Forms.Message@)"/>.
		/// </summary>
		protected override void WndProc(ref Message m)
		{
			/* If visual theme is changed we have to reinitialize drawing mode to prevent exception being
			 * thrown by TabRenderer when switching from visual styles to "Windows Classic" and vise versa.*/
			if (m.Msg == NativeMethods.WM_THEMECHANGED)
			{
				InitializeDrawMode();
			}
			else if (m.Msg == NativeMethods.WM_PARENTNOTIFY && (m.WParam.ToInt32() & 0xffff) == NativeMethods.WM_CREATE)
			{
				/* Tab scroller has created(too many tabs to display and tab control is not multiline), so
				 * let's attach our hook to it.*/
				var className = new StringBuilder(16);
				if (NativeMethods.RealGetWindowClass(m.LParam, className, 16) > 0 && className.ToString() == "msctls_updown32")
				{
					fUpDown.ReleaseHandle();
					fUpDown.AssignHandle(m.LParam);
				}
			}
			base.WndProc(ref m);
		}

		//required to correct pane height on Win Vista(and possible Win 7)
		private static readonly int sAdjHeight = Environment.OSVersion.Version.Major >= 6 ? 1 : 0;

		/// <summary>
		/// Draws our tab control.
		/// </summary>
		/// <param name="g">The <see cref="T:System.Drawing.Graphics"/> object used to draw tab control.</param>
		/// <param name="clipRect">The <see cref="T:System.Drawing.Rectangle"/> that specifies clipping rectangle
		/// of the control.</param>
		private void DrawCustomTabControl(Graphics g, Rectangle clipRect)
		{
			/* In this method we draw only those parts of the control which intersects with the
			 * clipping rectangle. It's some kind of optimization.*/
			if (!Visible)
			{
				return;
			}
			//selected tab index and rectangle
			var iSel = SelectedIndex;
			var selRect = iSel != -1 ? GetTabRect(iSel) : Rectangle.Empty;
			var rcPage = ClientRectangle;
			//correcting page rectangle
			switch (Alignment)
			{
				case TabAlignment.Top:
					{
						var trunc = selRect.Height * RowCount + 2;
						rcPage.Y += trunc; rcPage.Height -= trunc;
					}
					break;
				case TabAlignment.Bottom: rcPage.Height -= (selRect.Height + sAdjHeight) * RowCount; break;
			}
			//draw page itself
			if (rcPage.IntersectsWith(clipRect))
			{
				TabRenderer.DrawTabPage(g, rcPage);
			}
			var tabCount = TabCount;
			if (tabCount == 0)
			{
				return;
			}
			using (var textFormat = new StringFormat())
			{
				textFormat.Alignment = StringAlignment.Center;
				textFormat.LineAlignment = StringAlignment.Center;
				//drawing unselected tabs
				lastHotIndex = HitTest();//hot tab
				for (var iTab = 0; iTab < tabCount; iTab++)
				{
					if (iTab != iSel)
					{
						var tabRect = GetTabRect(iTab);
						if (tabRect.Right >= 3 && tabRect.IntersectsWith(clipRect))
						{
							var state = iTab == lastHotIndex ? TabItemState.Hot : TabItemState.Normal;
							DrawTabItem(g, iTab, state, tabRect, textFormat);
						}
					}
				}
				/* Drawing selected tab. We'll also increase selected tab's rectangle. It should be a little
				 * bigger than other tabs.*/
				selRect.Inflate(2, 2);
				if (iSel != -1 && selRect.IntersectsWith(clipRect))
				{
					DrawTabItem(g, iSel, TabItemState.Selected, selRect, textFormat);
				}
			}
		}

		/// <summary>
		/// Draws certain tab.
		/// </summary>
		/// <param name="g">The <see cref="T:System.Drawing.Graphics"/> object used to draw tab control.</param>
		/// <param name="index">Index of the tab being drawn.</param>
		/// <param name="state">State of the tab item.</param>
		/// <param name="tabRect">The <see cref="T:System.Drawing.Rectangle"/> object specifying tab bounds.</param>
		/// <param name="textFmt">The <see cref="T:System.Drawing.StringFormat"/> object specifying text formatting
		/// in the tab.</param>
		private void DrawTabItem(Graphics g, int index, TabItemState state, Rectangle tabRect, StringFormat textFmt)
		{
			//if scroller is visible and the tab is fully placed under it we don't need to draw such tab
			if (fUpDown.X <= 0 || tabRect.X < fUpDown.X)
			{
				/* We will draw our tab on the bitmap and then will transfer image on the control
				 * graphic context.*/
				using (var bmp = new Bitmap(tabRect.Width, tabRect.Height))
				{
					using (var bitmapContext = Graphics.FromImage(bmp))
					{
						var drawRect = new Rectangle(0, 0, tabRect.Width, tabRect.Height);
						TabRenderer.DrawTabItem(bitmapContext, drawRect, state);
						if (state == TabItemState.Selected && tabRect.X == 0)
						{
							var corrY = bmp.Height - 1;
							bmp.SetPixel(0, corrY, bmp.GetPixel(0, corrY - 1));
						}
						/* Important moment. If tab alignment is bottom we should flip image to display tab
						 * correctly.*/
						if (Alignment == TabAlignment.Bottom)
						{
							bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
						}
						var focusRect = Rectangle.Inflate(drawRect, -3, -3);
						//focus rect
						var pg = TabPages[index];
						//tab page whose tab we're drawing
						//trying to get tab image if any
						var pagePict = GetImageByIndexOrKey(pg.ImageIndex, pg.ImageKey);
						if (pagePict != null)
						{
							//If tab image is present we should draw it.
							var imgLoc = state == TabItemState.Selected ? new Point(8, 2) : new Point(6, 2);
							if (Alignment == TabAlignment.Bottom)
							{
								imgLoc.Y = drawRect.Bottom - 2 - pagePict.Height;
							}
							bitmapContext.DrawImageUnscaled(pagePict, imgLoc);
							//Correcting rectangle for drawing text.
							drawRect.X += imgLoc.X + pagePict.Width;
							drawRect.Width -= imgLoc.X + pagePict.Width;
						}
						//drawing tab text
						using (Brush b = new SolidBrush(SystemColors.ControlText))
						{
							bitmapContext.DrawString(pg.Text, this.Font, b, (RectangleF)drawRect, textFmt);
						}
						//and finally drawing focus rect(if needed)
						if (Focused && state == TabItemState.Selected)
						{
							ControlPaint.DrawFocusRectangle(bitmapContext, focusRect);
						}
					}
					//If the tab has part under scroller we shouldn't draw that part.
					var shift = state == TabItemState.Selected ? 2 : 0;
					if (fUpDown.X > 0 && fUpDown.X >= tabRect.X - shift && fUpDown.X < tabRect.Right + shift)
					{
						tabRect.Width -= tabRect.Right - fUpDown.X + shift;
					}
					g.DrawImageUnscaledAndClipped(bmp, tabRect);
				}
			}
		}

		/// <summary>
		/// This function attempts to get tab image by index first or if not set then by key.
		/// </summary>
		/// <param name="index">Index of tab image in tab control image list.</param>
		/// <param name="key">Key of tab image in tab control image list.</param>
		/// <returns><see cref="T:System.Drawing.Image"/> that represents image of the tab or null if not assigned.</returns>
		private Image GetImageByIndexOrKey(int index, string key)
		{
			return ImageList == null ? null : index > -1 ? ImageList.Images[index] : key.Length > 0 ? ImageList.Images[key] : null;
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			//drawing our control
			DrawCustomTabControl(e.Graphics, e.ClipRectangle);
		}

		/// <summary>
		/// Gets hot tab index.
		/// </summary>
		/// <returns>Index of the tab over that the mouse is hovering or -1 if the mouse isn't over any tab.</returns>
		private int HitTest()
		{
			var hti = new NativeMethods.TCHITTESTINFO();
			var mousePos = PointToClient(MousePosition);
			hti.pt.x = mousePos.X;
			hti.pt.y = mousePos.Y;
			var htiPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf(hti));
			Marshal.StructureToPtr(hti, htiPointer, false);
			var result = (int)NativeMethods.SendMessage(Handle, NativeMethods.TCM_HITTEST, IntPtr.Zero, htiPointer);
			Marshal.DestroyStructure(htiPointer, typeof(NativeMethods.TCHITTESTINFO));
			Marshal.FreeCoTaskMem(htiPointer);
			return result;
		}

		/// <summary>
		/// This class represents low level hook to updown control used to scroll tabs. We need it to know the
		/// position of scroller and to draw hot tab as normal when the mouse moves from that tab to scroller.
		/// </summary>
		private sealed class NativeUpDown : NativeWindow
		{
			public NativeUpDown(VSTabControl ctrl) { fparent = ctrl; }

			private VSTabControl fparent;

			/// <summary>
			/// Reports about current position of tab scroller.
			/// </summary>
			public int X { get; private set; }

			protected override void WndProc(ref Message m)
			{
				//if native updown is destroyed we need release our hook
				if (m.Msg == NativeMethods.WM_DESTROY || m.Msg == NativeMethods.WM_NCDESTROY)
				{
					ReleaseHandle();
				}
				else if (m.Msg == NativeMethods.WM_WINDOWPOSCHANGING)
				{
					//When scroller position is changed we should remember that new position.
					X = ((NativeMethods.WINDOWPOS)m.GetLParam(typeof(NativeMethods.WINDOWPOS))).x;
				}
				else if (m.Msg == NativeMethods.WM_MOUSEMOVE && fparent.lastHotIndex > 0 && fparent.lastHotIndex != fparent.SelectedIndex)
				{
					//ownerdrawing former hot tab as normal
					using (var context = Graphics.FromHwnd(fparent.Handle))
					using (var textFormat = new StringFormat())
					{
						textFormat.Alignment = StringAlignment.Center;
						textFormat.LineAlignment = StringAlignment.Center;
						fparent.DrawTabItem(context, fparent.lastHotIndex, TabItemState.Normal, fparent.GetTabRect(fparent.lastHotIndex), textFormat);
						if (fparent.lastHotIndex - fparent.SelectedIndex == 1)
						{
							var selRect = fparent.GetTabRect(fparent.SelectedIndex);
							selRect.Inflate(2, 2);
							fparent.DrawTabItem(context, fparent.SelectedIndex, TabItemState.Selected, selRect, textFormat);
						}
					}
				}
				else if (m.Msg == NativeMethods.WM_LBUTTONDOWN)
				{
					var invalidRect = fparent.GetTabRect(fparent.SelectedIndex);
					invalidRect.X = 0; invalidRect.Width = 2;
					invalidRect.Inflate(0, 2);
					fparent.Invalidate(invalidRect);
				}
				else if (m.Msg == NativeMethods.WM_SHOWWINDOW)
				{
					// If the scroll control for the labels is being hidden we need to stop
					// clipping the labels.
					// USUALLY we get WM_WINDOWPOSCHANGING with position 0 as well as this
					// message, but not while starting up a parent control that gets laid out
					// twice, first at a width that needs the scroll control and later at a width
					// that does not. See FWR-1794 for one case where this can be a problem.
					if (m.WParam.ToInt32() == 0) // window being hidden
					{
						X = 0;
					}
				}
				base.WndProc(ref m);
			}
		}

		private static class NativeMethods
		{
			public static IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam)
			{
				if (Platform.IsWindows)
				{
					return SendMessageWindows(hWnd, Msg, wParam, lParam);
				}
				Console.WriteLine("Warning: using unimplemented method NativeMethods.SendMessage");
				return IntPtr.Zero;
			}

			public static bool DeleteObject(IntPtr hObject)
			{
				if (Platform.IsWindows)
				{
					return DeleteObjectWindows(hObject);
				}
				Console.WriteLine("Warning: using unimplemented method NativeMethods.DeleteObject");
				return true;
			}

			public static uint RealGetWindowClass(IntPtr hWnd, StringBuilder ClassName, uint ClassNameMax)
			{
				if (Platform.IsWindows)
				{
					return RealGetWindowClassWindows(hWnd, ClassName, ClassNameMax);
				}
				Console.WriteLine("Warning: using unimplemented method NativeMethods.RealGetWindowClass");
				return 0;
			}

			[DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
			private static extern IntPtr SendMessageWindows(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

			[DllImport("gdi32.dll", EntryPoint = "DeleteObject", CallingConvention = CallingConvention.StdCall)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool DeleteObjectWindows(IntPtr hObject);

			[DllImport("user32.dll", EntryPoint = "RealGetWindowClassW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
			private static extern uint RealGetWindowClassWindows(IntPtr hWnd, StringBuilder ClassName, uint ClassNameMax);

			#region API Structures
			[StructLayout(LayoutKind.Sequential)]
			public struct POINT
			{
				public int x, y;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct TCHITTESTINFO
			{
				public POINT pt;
				public uint flags;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct WINDOWPOS
			{
				public IntPtr hwnd, hwndInsertAfter;
				public int x, y, cx, cy, flags;
			}
			#endregion

			#region Messages
			public const int TCM_HITTEST = 0x130d, WM_SETFONT = 0x0030, WM_THEMECHANGED = 0x031a,
				WM_DESTROY = 0x0002, WM_NCDESTROY = 0x0082, WM_WINDOWPOSCHANGING = 0x0046,
				WM_PARENTNOTIFY = 0x0210, WM_CREATE = 0x0001, WM_MOUSEMOVE = 0x0200,
				WM_LBUTTONDOWN = 0x0201, WM_SHOWWINDOW = 0x18;
			#endregion
		}
	}
}