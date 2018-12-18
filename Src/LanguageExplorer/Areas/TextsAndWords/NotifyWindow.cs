// NotifyWindow.cs
// Copyright (c) 2004 by Robert Misiak <rmisiak@users.sourceforge.net>
// All Rights Reserved.
//
// Permission is granted to use, modify and distribute this code, as long as credit is given to the original author, and the copyright notice
// is retained.
//	http://www.codeproject.com/cs/miscctrl/RobMisNotifyWindow.asp
// Based on a similar implementation used in ChronosXP, an open-source project:  http://chronosxp.sourceforge.net

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LanguageExplorer.Areas.TextsAndWords
{
#if RANDYTODO
	// TODO: See if NotifyWindow is really needed.
#endif
	/// <summary>
	/// Display An MSN-Messenger-Style NotifyWindow.
	/// </summary>
	internal class NotifyWindow : Form
	{
		#region Public Variables
		/// <summary>
		/// Gets or sets the title text to be displayed in the NotifyWindow.
		/// </summary>
		public string Title;
		/// <summary>
		/// Gets or sets the Font used for the title text.
		/// </summary>
		public Font TitleFont;
		/// <summary>
		/// Gets or sets the Font used when the mouse hovers over the main body of text.
		/// </summary>
		public Font HoverFont;
		/// <summary>
		/// Gets or sets the Font used when the mouse hovers over the title text.
		/// </summary>
		public Font TitleHoverFont;
		/// <summary>
		/// Gets or sets the style used when drawing the background of the NotifyWindow.
		/// </summary>
		public BackgroundStyles BackgroundStyle;
		/// <summary>
		/// Gets or sets the Blend used when drawing a gradient background for the NotifyWindow.
		/// </summary>
		public Blend Blend;
		/// <summary>
		/// Gets or sets the StringFormat used when drawing text in the NotifyWindow.
		/// </summary>
		public StringFormat StringFormat;
		/// <summary>
		/// Gets or sets a value specifying whether or not the window should continue to be displayed if the mouse cursor is inside the bounds
		/// of the NotifyWindow.
		/// </summary>
		public bool WaitOnMouseOver;
		/// <summary>
		/// An EventHandler called when the NotifyWindow main text is clicked.
		/// </summary>
		public event EventHandler TextClicked;
		/// <summary>
		/// An EventHandler called when the NotifyWindow title text is clicked.
		/// </summary>
		public event EventHandler TitleClicked;
		/// <summary>
		/// Gets or sets the color of the title text.
		/// </summary>
		public Color TitleColor;
		/// <summary>
		/// Gets or sets the color of the NotifyWindow main text.
		/// </summary>
		public Color TextColor;
		/// <summary>
		/// Gets or sets the gradient color which will be blended in drawing the background.
		/// </summary>
		public Color GradientColor;
		/// <summary>
		/// Gets or sets the color of text when the user clicks on it.
		/// </summary>
		public Color PressedColor;
		/// <summary>
		/// Gets or sets the amount of milliseconds to display the NotifyWindow for.
		/// </summary>
		public int WaitTime;
		/// <summary>
		/// Gets or sets the full height of the NotifyWindow, used after the opening animation has been completed.
		/// </summary>
		public int ActualHeight;
		/// <summary>
		/// Gets or sets the full width of the NotifyWindow.
		/// </summary>
		public int ActualWidth;

		public enum BackgroundStyles { BackwardDiagonalGradient, ForwardDiagonalGradient, HorizontalGradient, VerticalGradient, Solid };
		public enum ClockStates { Opening, Closing, Showing, None };
		public ClockStates ClockState;
		#endregion

		#region Protected Variables
		protected bool closePressed, textPressed, titlePressed, closeHot, textHot, titleHot;
		protected Rectangle rClose, rText, rTitle, rDisplay, rScreen, rGlobClose, rGlobText, rGlobTitle, rGlobDisplay;
		protected Timer viewClock;
		#endregion

		#region Implementation of IDisposable

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (IsDisposed)
			{
				return; // Only run once.
			}

			base.Dispose(disposing);
		}
		#endregion

		#region Constructor
		/// <param name="title">Title text displayed in the NotifyWindow</param>
		/// <param name="text">Main text displayed in the NotifyWindow</param>
		public NotifyWindow(string title, string text)
			: this()
		{
			Title = title;
			Text = text;
		}

		/// <param name="text">Text displayed in the NotifyWindow</param>
		public NotifyWindow(string text)
			: this()
		{
			Text = text;
		}

		public NotifyWindow()
		{
			AccessibleName = GetType().Name;
			SetStyle(ControlStyles.UserMouse, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);     // WmPaint calls OnPaint and OnPaintBackground
			SetStyle(ControlStyles.DoubleBuffer, true);

			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.None;
			StartPosition = FormStartPosition.Manual;

			// Default values
			BackgroundStyle = BackgroundStyles.VerticalGradient;
			ClockState = ClockStates.None;
			BackColor = Color.SteelBlue;
			GradientColor = Color.WhiteSmoke;
			PressedColor = Color.Gray;
			TitleColor = SystemColors.ControlText;
			TextColor = SystemColors.ControlText;
			WaitOnMouseOver = true;
			ActualWidth = 130;
			ActualHeight = 110;
			WaitTime = 11000;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Sets the width and height of the NotifyWindow.
		/// </summary>
		public void SetDimensions(int width, int height)
		{
			ActualWidth = width;
			ActualHeight = height;
		}

		/// <summary>
		/// Displays the NotifyWindow.
		/// </summary>
		public void Notify()
		{
			if (string.IsNullOrEmpty(Text))
			{
				throw new Exception("You must set NotifyWindow.Text before calling Notify()");
			}
			Width = ActualWidth;
			rScreen = Screen.GetWorkingArea(Screen.PrimaryScreen.Bounds);
			Height = 0;
			Top = rScreen.Bottom;
			Left = rScreen.Width - Width - 11;
			if (HoverFont == null)
			{
				HoverFont = new Font(Font, Font.Style | FontStyle.Underline);
			}
			if (TitleFont == null)
			{
				TitleFont = Font;
			}
			if (TitleHoverFont == null)
			{
				TitleHoverFont = new Font(TitleFont, TitleFont.Style | FontStyle.Underline);
			}
			if (StringFormat == null)
			{
				StringFormat = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center,
					Trimming = StringTrimming.EllipsisWord
				};
			}
			rDisplay = new Rectangle(0, 0, Width, ActualHeight);
			rClose = new Rectangle(Width - 21, 10, 13, 13);
			int offset;
			if (Title != null)
			{
				using (var fx = CreateGraphics())
				{
					var sz = fx.MeasureString(Title, TitleFont, ActualWidth - rClose.Width - 22, this.StringFormat);
					rTitle = new Rectangle(11, 12, (int)Math.Ceiling(sz.Width), (int)Math.Ceiling(sz.Height));
					offset = (int)Math.Max(Math.Ceiling(sz.Height + rTitle.Top + 2), rClose.Bottom + 5);
				}
			}
			else
			{
				offset = rClose.Bottom + 1;
				rTitle = new Rectangle(-1, -1, 1, 1);
			}
			rText = new Rectangle(11, offset, ActualWidth - 22, ActualHeight - (int)(offset * 1.5));
			// rGlob* are Rectangle's Offset'ed to their actual position on the screen, for use with Cursor.Position.
			rGlobClose = rClose;
			rGlobClose.Offset(Left, rScreen.Bottom - ActualHeight);
			rGlobText = rText;
			rGlobText.Offset(Left, rScreen.Bottom - ActualHeight);
			rGlobTitle = rTitle;
			if (Title != null)
			{
				rGlobTitle.Offset(Left, rScreen.Bottom - ActualHeight);
			}
			rGlobDisplay = rDisplay;
			rGlobDisplay.Offset(Left, rScreen.Bottom - ActualHeight);
			rGlobClose = rClose;
			rGlobClose.Offset(Left, rScreen.Bottom - ActualHeight);
			rGlobDisplay = rDisplay;
			rGlobDisplay.Offset(Left, rScreen.Bottom - ActualHeight);
			// Use unmanaged ShowWindow() and SetWindowPos() instead of the managed Show() to display the window - this method will display
			// the window TopMost, but without stealing focus (namely the SW_SHOWNOACTIVATE and SWP_NOACTIVATE flags)
			ShowWindow(Handle, SW_SHOWNOACTIVATE);
			SetWindowPos(Handle, HWND_TOPMOST, rScreen.Width - ActualWidth - 11, rScreen.Bottom, ActualWidth, 0, SWP_NOACTIVATE);
			viewClock = new Timer();
			viewClock.Tick += viewTimer;
			viewClock.Interval = 1;
			viewClock.Start();
			ClockState = ClockStates.Opening;
		}
		#endregion

		#region Drawing
		protected override void OnPaint(PaintEventArgs e)
		{
			// Draw the close button and text.
			drawCloseButton(e.Graphics);
			Font useFont; Color useColor;
			if (Title != null)
			{
				useFont = titleHot ? TitleHoverFont : TitleFont;
				useColor = titlePressed ? PressedColor : TitleColor;
				using (var sb = new SolidBrush(useColor))
				{
					e.Graphics.DrawString(Title, useFont, sb, rTitle, this.StringFormat);
				}
			}
			useFont = textHot ? HoverFont : Font;
			useColor = textPressed ? PressedColor : TextColor;
			using (var sb = new SolidBrush(useColor))
			{
				e.Graphics.DrawString(Text, useFont, sb, rText, this.StringFormat);
			}
		}

		protected override void OnPaintBackground(System.Windows.Forms.PaintEventArgs e)
		{
			// First paint the background
			if (BackgroundStyle == BackgroundStyles.Solid)
			{
				using (var sb = new SolidBrush(BackColor))
				{
					e.Graphics.FillRectangle(sb, rDisplay);
				}
			}
			else
			{
				LinearGradientMode lgm;
				switch (BackgroundStyle)
				{
					case BackgroundStyles.BackwardDiagonalGradient:
						lgm = LinearGradientMode.BackwardDiagonal;
						break;
					case BackgroundStyles.ForwardDiagonalGradient:
						lgm = LinearGradientMode.ForwardDiagonal;
						break;
					case BackgroundStyles.HorizontalGradient:
						lgm = LinearGradientMode.Horizontal;
						break;
					default:
					case BackgroundStyles.VerticalGradient:
						lgm = LinearGradientMode.Vertical;
						break;
				}
				using (var lgb = new LinearGradientBrush(rDisplay, GradientColor, BackColor, lgm))
				{
					if (Blend != null)
					{
						lgb.Blend = Blend;
					}
					e.Graphics.FillRectangle(lgb, rDisplay);
				}
			}
			// Next draw borders...
			drawBorder(e.Graphics);
		}

		protected virtual void drawBorder(Graphics fx)
		{
			fx.DrawRectangle(Pens.Silver, 2, 2, Width - 4, ActualHeight - 4);

			// Top border
			fx.DrawLine(Pens.Silver, 0, 0, Width, 0);
			fx.DrawLine(Pens.White, 0, 1, Width, 1);
			fx.DrawLine(Pens.DarkGray, 3, 3, Width - 4, 3);
			fx.DrawLine(Pens.DimGray, 4, 4, Width - 5, 4);

			// Left border
			fx.DrawLine(Pens.Silver, 0, 0, 0, ActualHeight);
			fx.DrawLine(Pens.White, 1, 1, 1, ActualHeight);
			fx.DrawLine(Pens.DarkGray, 3, 3, 3, ActualHeight - 4);
			fx.DrawLine(Pens.DimGray, 4, 4, 4, ActualHeight - 5);

			// Bottom border
			fx.DrawLine(Pens.DarkGray, 1, ActualHeight - 1, Width - 1, ActualHeight - 1);
			fx.DrawLine(Pens.White, 3, ActualHeight - 3, Width - 3, ActualHeight - 3);
			fx.DrawLine(Pens.Silver, 4, ActualHeight - 4, Width - 4, ActualHeight - 4);

			// Right border
			fx.DrawLine(Pens.DarkGray, Width - 1, 1, Width - 1, ActualHeight - 1);
			fx.DrawLine(Pens.White, Width - 3, 3, Width - 3, ActualHeight - 3);
			fx.DrawLine(Pens.Silver, Width - 4, 4, Width - 4, ActualHeight - 4);
		}

		protected virtual void drawCloseButton(Graphics fx)
		{
			if (visualStylesEnabled())
			{
				drawThemeCloseButton(fx);
			}
			else
			{
				drawLegacyCloseButton(fx);
			}
		}

		/// <summary>
		/// Draw a Windows XP style close button.
		/// </summary>
		protected void drawThemeCloseButton(Graphics fx)
		{
			var hTheme = OpenThemeData(Handle, "Window");
			if (hTheme == IntPtr.Zero)
			{
				drawLegacyCloseButton(fx);
				return;
			}
			int stateId;
			if (closePressed)
			{
				stateId = CBS_PUSHED;
			}
			else if (closeHot)
			{
				stateId = CBS_HOT;
			}
			else
			{
				stateId = CBS_NORMAL;
			}
			var reClose = new RECT(rClose);
			var reClip = reClose; // should fx.VisibleClipBounds be used here?
			var hDC = fx.GetHdc();
			DrawThemeBackground(hTheme, hDC, WP_CLOSEBUTTON, stateId, ref reClose, ref reClip);
			fx.ReleaseHdc(hDC);
			CloseThemeData(hTheme);
		}

		/// <summary>
		/// Draw a Windows 95 style close button.
		/// </summary>
		protected void drawLegacyCloseButton(Graphics fx)
		{
			var bState = closePressed ? ButtonState.Pushed : ButtonState.Normal;
			ControlPaint.DrawCaptionButton(fx, rClose, CaptionButton.Close, bState);
		}

		/// <summary>
		/// Determine whether or not XP Visual Styles are active.  Compatible with pre-UxTheme.dll versions of Windows.
		/// </summary>
		protected bool visualStylesEnabled()
		{
			try
			{
				return IsThemeActive() == 1;
			}
			catch (DllNotFoundException)  // pre-XP systems which don't have UxTheme.dll
			{
				return false;
			}
		}
		#endregion

		#region Timers and EventHandlers
		protected void viewTimer(object sender, EventArgs e)
		{
			switch (ClockState)
			{
				case ClockStates.Opening:
					if (Top - 2 <= rScreen.Height - ActualHeight)
					{
						Top = rScreen.Height - ActualHeight;
						Height = ActualHeight;
						ClockState = ClockStates.Showing;
						viewClock.Interval = WaitTime;
					}
					else
					{
						Top -= 2;
						Height += 2;
					}
					break;
				case ClockStates.Showing:
					if (!WaitOnMouseOver || !rGlobDisplay.Contains(Cursor.Position))
					{
						viewClock.Interval = 1;
						ClockState = ClockStates.Closing;
					}
					break;
				case ClockStates.Closing:
					Top += 2;
					Height -= 2;
					if (Top >= rScreen.Height)
					{
						ClockState = ClockStates.None;
						viewClock.Stop();
						viewClock.Dispose();
						Close();
					}
					break;
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (Title != null && rGlobTitle.Contains(Cursor.Position) && !textPressed && !closePressed)
			{
				Cursor = Cursors.Hand;
				titleHot = true;
				textHot = false; closeHot = false;
				Invalidate();
			}
			else if (rGlobText.Contains(Cursor.Position) && !titlePressed && !closePressed)
			{
				Cursor = Cursors.Hand;
				textHot = true;
				titleHot = false; closeHot = false;
				Invalidate();
			}
			else if (rGlobClose.Contains(Cursor.Position) && !titlePressed && !textPressed)
			{
				Cursor = Cursors.Hand;
				closeHot = true;
				titleHot = false; textHot = false;
				Invalidate();
			}
			else if ((textHot || titleHot || closeHot) && (!titlePressed && !textPressed && !closePressed))
			{
				Cursor = Cursors.Default;
				titleHot = false; textHot = false; closeHot = false;
				Invalidate();
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (rGlobClose.Contains(Cursor.Position))
				{
					closePressed = true;
					closeHot = false;
					Invalidate();
				}
				else if (rGlobText.Contains(Cursor.Position))
				{
					textPressed = true;
					Invalidate();
				}
				else if (Title != null && rGlobTitle.Contains(Cursor.Position))
				{
					titlePressed = true;
					Invalidate();
				}
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (closePressed)
				{
					Cursor = Cursors.Default;
					closePressed = false;
					closeHot = false;
					Invalidate();
					if (rGlobClose.Contains(Cursor.Position))
					{
						Close();
					}
				}
				else if (textPressed)
				{
					Cursor = Cursors.Default;
					textPressed = false;
					textHot = false;
					Invalidate();
					if (rGlobText.Contains(Cursor.Position))
					{
						Close();
						TextClicked?.Invoke(this, new System.EventArgs());
					}
				}
				else if (titlePressed)
				{
					Cursor = Cursors.Default;
					titlePressed = false;
					titleHot = false;
					Invalidate();
					if (rGlobTitle.Contains(Cursor.Position))
					{
						Close();
						TitleClicked?.Invoke(this, new System.EventArgs());
					}
				}
			}
			base.OnMouseUp(e);
		}
		#endregion

		#region P/Invoke
		// DrawThemeBackground()
		protected const int WP_CLOSEBUTTON = 18;
		protected const int CBS_NORMAL = 1;
		protected const int CBS_HOT = 2;
		protected const int CBS_PUSHED = 3;
		[StructLayout(LayoutKind.Explicit)]
		protected struct RECT
		{
			[FieldOffset(0)]
			public int Left;
			[FieldOffset(4)]
			public int Top;
			[FieldOffset(8)]
			public int Right;
			[FieldOffset(12)]
			public int Bottom;

			public RECT(Rectangle bounds)
			{
				Left = bounds.Left;
				Top = bounds.Top;
				Right = bounds.Right;
				Bottom = bounds.Bottom;
			}
		}

		// SetWindowPos()
		protected const int HWND_TOPMOST = -1;
		protected const int SWP_NOACTIVATE = 0x0010;

		// ShowWindow()
		protected const int SW_SHOWNOACTIVATE = 4;

		// UxTheme.dll
		[DllImport("UxTheme.dll")]
		protected static extern int IsThemeActive();
		[DllImport("UxTheme.dll")]
		protected static extern IntPtr OpenThemeData(IntPtr hWnd, [MarshalAs(UnmanagedType.LPTStr)] string classList);
		[DllImport("UxTheme.dll")]
		protected static extern void CloseThemeData(IntPtr hTheme);
		[DllImport("UxTheme.dll")]
		protected static extern void DrawThemeBackground(IntPtr hTheme, IntPtr hDC, int partId, int stateId, ref RECT rect, ref RECT clipRect);

		// user32.dll
		[DllImport("user32.dll")]
		protected static extern bool ShowWindow(IntPtr hWnd, int flags);
		[DllImport("user32.dll")]
		protected static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
		#endregion
	}
}