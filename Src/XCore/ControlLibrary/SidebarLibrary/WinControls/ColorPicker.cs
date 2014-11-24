using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using SidebarLibrary.General;
using SidebarLibrary.Win32;
using SidebarLibrary.WinControls;

namespace SidebarLibrary.WinControls
{

	#region Helper Classes
	public class NewColorArgs : EventArgs
	{
		#region Class Variables
		Color newColor;
		#endregion

		#region Class Variables
		public NewColorArgs(Color newColor)
		{
			this.newColor = newColor;
		}
		#endregion

		#region Properties
		public Color NewColor
		{
			get { return newColor;}
		}
		#endregion
	}

	[ToolboxItem(false)]
	internal class ArrowButton : System.Windows.Forms.Button
	{

		#region Class Variables
		ColorPicker colorPicker = null;
		DrawState drawState = DrawState.Normal;
		#endregion

		#region Constructor
		public ArrowButton()
		{
		}

		public ArrowButton(ColorPicker colorPicker)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.Opaque, true);
			this.colorPicker = colorPicker;
			TabStop = false;
		}
		#endregion

		#region Overrides
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "g is a reference")]
		protected override void OnPaint(PaintEventArgs pe)
		{
			base.OnPaint(pe);
			Graphics g = pe.Graphics;
			DrawArrowState(g, drawState);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if ( colorPicker != null )
				colorPicker.DrawState = DrawState.Pressed;
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			if ( colorPicker != null )
				colorPicker.DrawState = DrawState.Hot;
		}

		protected override void OnMouseLeave(EventArgs e)
		{

			base.OnMouseLeave(e);
			if ( colorPicker == null ) return;

			Point mousePos = Control.MousePosition;
			if ( !colorPicker.ClientRectangle.Contains(colorPicker.PointToClient(mousePos))
				&& !colorPicker.colorTextBox.ContainsFocus )
			{
				colorPicker.DrawState = DrawState.Normal;
			}
		}

		protected override  void WndProc(ref Message m)
		{
			//Msg currentMessage = (Msg)m.Msg;

			switch(m.Msg)
			{
					// The NET MouseEnter Mouse Leave mechanism seems to fall
					// apart when I use PeekMessage function in the ColorPickerDropDown class
					// use the Windows API to keep it working
				case ((int)Msg.WM_MOUSEMOVE):
					if ( colorPicker != null )
					{
						RequestMouseLeaveMessage(m.HWnd);
						colorPicker.DrawState = DrawState.Hot;
					}
					break;
				case ((int)Msg.WM_MOUSELEAVE):
					Point mousePos = Control.MousePosition;
					if ( colorPicker != null && !colorPicker.ClientRectangle.Contains(colorPicker.PointToClient(mousePos))
						&& !colorPicker.colorTextBox.ContainsFocus )
					{
						colorPicker.DrawState = DrawState.Normal;
					}
					break;
				default:
					break;
			}

			base.WndProc(ref m);
		}

		#endregion

		#region Properties
		internal DrawState DrawState
		{
			set
			{
				if ( drawState != value )
				{
					drawState = value;
					Invalidate();
				}
			}
			get { return drawState; }
		}

		#endregion

		#region Implementation
		void DrawArrowState(Graphics g, DrawState state)
		{
			if ( colorPicker == null ) return;

			if ( state == DrawState.Pressed && colorPicker.colorDropDown.Visible == false )
				state = DrawState.Hot;

			DrawBackground(g, state);
			Rectangle rc = ClientRectangle;
			SizeF sizeF = Image.PhysicalDimension;
			int imageWidth = (int)sizeF.Width;
			int imageHeight = (int)sizeF.Height;

			// We are assuming that the button image is smaller than
			// the button itself
			if ( imageWidth > rc.Width || imageHeight > rc.Height)
			{
				Debug.WriteLine("Image dimensions need to be smaller that button's dimension...");
				return;
			}

			// Image only drawing
			int	x = (Width - imageWidth)/2;
			int	y = (Height - imageHeight)/2;
			DrawImage(g, state, Image, x, y);

		}

		void DrawBackground(Graphics g, DrawState state)
		{
			Rectangle rc = ClientRectangle;
			// Draw background
			if ( state == DrawState.Normal || state == DrawState.Disable )
			{
				g.FillRectangle(new SolidBrush(SystemColors.Control), rc);
				// Draw border rectangle
				g.DrawRectangle(Pens.White, rc.Left, rc.Top, rc.Width-1, rc.Height-1);
			}
			else if ( state == DrawState.Hot || state == DrawState.Pressed  )
			{
				// Erase whatever that was there before
				if ( state == DrawState.Hot )
					g.FillRectangle(new SolidBrush(ColorUtil.VSNetSelectionColor), rc);
				else
					g.FillRectangle(new SolidBrush(ColorUtil.VSNetPressedColor), rc);
				// Draw border rectangle
				g.DrawRectangle(SystemPens.Highlight, rc.Left, rc.Top, rc.Width-1, rc.Height-1);
			}
		}

		void DrawImage(Graphics g, DrawState state, Image image, int x, int y)
		{
			SizeF sizeF = Image.PhysicalDimension;
			int imageWidth = (int)sizeF.Width;
			int imageHeight = (int)sizeF.Height;

			if ( state == DrawState.Disable )
				ControlPaint.DrawImageDisabled(g, Image, x, y, SystemColors.Control);
			else
				g.DrawImage(Image, x, y, imageWidth, imageHeight);
		}

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

		#endregion

	}

	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="colorPicker is a reference")]
	internal class ColorPickerEditCtrlHook : System.Windows.Forms.NativeWindow
	{
		#region Class Variables
		private ColorPicker colorPicker;
		private bool ignoreNextPaintMessage;
		#endregion

		#region Constructors
		public ColorPickerEditCtrlHook(ColorPicker colorPicker)
		{
			this.colorPicker = colorPicker;
		}
		#endregion

		#region Overrides
		protected override  void WndProc(ref Message m)
		{
			//Msg currentMessage = (Msg)m.Msg;
			switch(m.Msg)
			{
				case ((int)Msg.WM_PAINT):
					if ( ignoreNextPaintMessage )
					{
						ignoreNextPaintMessage = false;
						return;
					}

					if ( colorPicker.Enabled == false )
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
				default:
					break;
			}

			base.WndProc(ref m);
		}

		#endregion

		#region Implementation
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
					g.DrawString(text, colorPicker.Font, b, pt);
				}
			}
		}

		#endregion
	}

	#endregion

	/// <summary>
	/// Summary description for ColorPicker.
	/// </summary>
	[ToolboxItem(false)]
	public class ColorPicker : System.Windows.Forms.UserControl
	{

		#region Class Variables
		internal System.Windows.Forms.TextBox colorTextBox;
		internal ArrowButton arrowButton;
		internal ColorPickerDropDown colorDropDown;
		const int COLOR_PANEL_WIDTH = 20;
		const int ARROW_BUTTON_WIDTH = 15;
		int TEXTBOX_HEIGHT = -1;
		const int MARGIN = 8;
		ResourceManager rm = null;
		Color currentColor = Color.White;
		public delegate void NewColorEventHandler(object sender, NewColorArgs e);
		public event NewColorEventHandler NewColor;
		internal DrawState drawState = DrawState.Normal;
		internal DrawState previousState = DrawState.Normal;
		Rectangle displayColorRect;
		ColorPickerEditCtrlHook editHook = null;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		#endregion

		#region Constructors
		public ColorPicker()
		{
			Assembly thisAssembly = Assembly.GetAssembly(Type.GetType("SidebarLibrary.WinControls.ColorPicker"));
			rm = new ResourceManager("SidebarLibrary.Resources.ImagesColorPicker", thisAssembly);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}


		#endregion

		#region Overrides
		protected override void OnPaint(PaintEventArgs pe)
		{
			base.OnPaint(pe);
			DrawColorPickerState();
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			// Hook the edit control
			editHook = new ColorPickerEditCtrlHook(this);
			editHook.AssignHandle(colorTextBox.Handle);
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			if ( Enabled == true )
				drawState = DrawState.Normal;
			else
				drawState = DrawState.Disable;
			base.OnEnabledChanged(e);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			DrawState = DrawState.Hot;
		}

		protected override void OnMouseLeave(EventArgs e)
		{

			base.OnMouseLeave(e);
			Point mousePos = Control.MousePosition;
			if ( !ClientRectangle.Contains(PointToClient(mousePos))
				&& !colorTextBox.ContainsFocus )
			{
				DrawState = DrawState.Normal;
				Debug.WriteLine("ON MOUSE LEAVE NORMAL PAINTING...");
			}
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			DrawState = DrawState.Hot;
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			Point mousePos = Control.MousePosition;
			if ( !ClientRectangle.Contains(PointToClient(mousePos)) )
				DrawState = DrawState.Normal;
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			if ( TEXTBOX_HEIGHT == -1 )
			{
				TEXTBOX_HEIGHT = colorTextBox.Height;
			}

			if ( Height != TEXTBOX_HEIGHT + MARGIN )
			{
				Height = TEXTBOX_HEIGHT + MARGIN;
				return;
			}

			// Size children controls
			Rectangle rc = ClientRectangle;
			//int width = rc.Width;
			int height = rc.Height;

			// ColorPanel
			displayColorRect = new Rectangle(rc.Left + 4, rc.Top + 4, COLOR_PANEL_WIDTH, height - 8);

			// TextBox
			colorTextBox.Left = displayColorRect.Right + 4;
			colorTextBox.Top = rc.Top + MARGIN/2;
			colorTextBox.Width = rc.Width - COLOR_PANEL_WIDTH - ARROW_BUTTON_WIDTH - 10;
			colorTextBox.Height = height - MARGIN;

			// ArrowButton
			arrowButton.Left = colorTextBox.Right + 2;
			arrowButton.Top = rc.Top;
			arrowButton.Width = ARROW_BUTTON_WIDTH;
			arrowButton.Height = height;

		}

		#endregion

		#region Properties
		internal DrawState DrawState
		{
			set
			{
				if ( drawState != value)
				{
					drawState = value;
					Invalidate();
				}
			}
			get { return  drawState; }
		}

		public Color Color
		{
			get { return currentColor; }
			set { currentColor = value; }
		}


		#endregion

		#region Implementation
		void ColorTextBox_LostFocus(object sender, EventArgs e)
		{
			if ( !arrowButton.ContainsFocus )
				DrawState = DrawState.Normal;
		}

		void ColorTextBox_GotFocus(object sender, EventArgs e)
		{
			DrawState = DrawState.Hot;
		}

		void ColorTextBox_MouseEnter(object sender, EventArgs e)
		{
			DrawState = DrawState.Hot;
		}

//		void ColorPanel_MouseEnter(object sender, EventArgs e)
//		{
//			DrawState = DrawState.Hot;
//		}

		void ColorTextBox_MouseLeave(object sender, EventArgs e)
		{
			Point pos = Control.MousePosition;
			if ( !ClientRectangle.Contains(PointToClient(pos)) )
				DrawState = DrawState.Normal;
		}

//		void ColorPanel_MouseLeave(object sender, EventArgs e)
//		{
//			Point pos = Control.MousePosition;
//			if ( !ClientRectangle.Contains(PointToClient(pos)) )
//			{
//				DrawState = DrawState.Normal;
//				Debug.WriteLine("COLOR PANEL ON MOUSE LEAVE...");
//			}
//		}

		void OnNewColor(NewColorArgs e)
		{
			currentColor = e.NewColor;
			if ( NewColor != null )
				NewColor(this, e);
		}

		void SetupChildrenControls()
		{
			// Associate arrow bitmap with button
			Bitmap bm = (Bitmap)rm.GetObject("Arrow");
			bm.MakeTransparent(Color.White);
			arrowButton.Image = bm;

			// Initialize color panel to default color
			currentColor = Color;

			// Create color picker dropdown portion
			colorDropDown = new ColorPickerDropDown();
			colorDropDown.ColorChanged += new  ColorChangeEventHandler(ColorChanged);

			// Get LostFocus events from text box
			colorTextBox.LostFocus += new EventHandler(ColorTextBox_LostFocus);
			colorTextBox.GotFocus += new EventHandler(ColorTextBox_GotFocus);
			colorTextBox.MouseLeave += new EventHandler(ColorTextBox_MouseLeave);
			colorTextBox.MouseEnter += new EventHandler(ColorTextBox_MouseEnter);

			// Change colorTextBox font to bold
			colorTextBox.Font = new Font(colorTextBox.Font, colorTextBox.Font.Style | FontStyle.Bold);
			// See if we have a known color, or just format the rgb values
			InitializeColorTextBoxString(Color, null);

		}

		void InitializeColorTextBoxString(Color color, string sender)
		{
			bool useTransparent = false;
			Color knownColor = Color.Empty;
			bool bKnownColor = true;
			if ( sender == "CustomTab" || sender == null )
				bKnownColor = ColorUtil.IsKnownColor(color, ref knownColor, useTransparent);
			else
				knownColor = color;

			if ( bKnownColor )
			{
				color = knownColor;
				// update color in dropdown part
				colorTextBox.Text = color.Name;
			}
			else
			{   // Format rgb string
				string sep = ",";
				string rgb = color.R.ToString() + sep + color.G.ToString() + sep + color.B.ToString();
				colorTextBox.Text = rgb;

			}

			colorDropDown.CurrentColor = color;
			colorTextBox.SelectionLength = 0;
			colorTextBox.SelectionStart = colorTextBox.Text.Length + 1;

			// Fire event to whoever is listening
			if ( sender != null )
			{
				NewColorArgs nca = new NewColorArgs(color);
				OnNewColor(nca);
			}

		}

		void arrowButton_Click(object sender, System.EventArgs e)
		{
			// Show color picker dropdown control
			Point point = new Point(0,0);
			CalculateSafeDisplayPoint(ref point);
			colorDropDown.DesktopBounds = new Rectangle(point.X, point.Y,
				colorDropDown.ClientRectangle.Width, colorDropDown.ClientRectangle.Height);
			if ( !colorDropDown.Visible )
				colorDropDown.Show();
			else
			{
				colorDropDown.Visible = false;
			}
		}

		void CalculateSafeDisplayPoint(ref Point point)
		{

			Rectangle rc = ClientRectangle;
			rc = RectangleToScreen(rc);
			int screenWidth = Screen.PrimaryScreen.Bounds.Width;
			int screenHeight = Screen.PrimaryScreen.Bounds.Height;

			// Correct x coordinate if necessary
			point.X = rc.Right - colorDropDown.Width;
			if ( point.X < 0 )
				point.X = 0;
			else if ( point.X + colorDropDown.Width  > screenWidth )
				point.X = screenWidth - colorDropDown.Width;

			// Correct y coordinate if necessary
			point.Y = rc.Bottom+1;
			if ( point.Y < 0 )
				point.Y = 0;
			else if ( point.Y + colorDropDown.Height  > screenHeight )
				point.Y = rc.Top -1 - colorDropDown.Height;
		}

		void ColorChanged( object sender, ColorChangeArgs ea)
		{
			currentColor = ea.NewColor;
			InitializeColorTextBoxString(ea.NewColor, ea.SenderName);
		}

		void ColorPicker_Load(object sender, System.EventArgs e)
		{
			SetupChildrenControls();
		}

		void colorTextBox_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			// If not the enter key then do not process
			if ( e.KeyCode != Keys.Enter)
				return;

			// Check if the string contains commas -- that would be the case
			// if we have a "custom" color
			string text = colorTextBox.Text;
			if ( text.IndexOf(',') != -1 )
			{
				FormatRGB(text);
				return;
			}

			// If it is in hexadecimal format
			try
			{

				if ( text.IndexOf('x') != -1 || text.IndexOf('X') != -1 )
				{
					int newValue = Convert.ToInt32(text, 16);
					int Red = (0xFF0000 & newValue)>>16;
					int Green = (0x00FF00 & newValue)>>8;
					int Blue = 0x0000FF & newValue;
					Color hexColor = Color.FromArgb(Red, Green, Blue);
					InitializeColorTextBoxString(hexColor, "CustomTab");
					return;
				}

				// If there are not commnas
				// then check if we have either a valid number
				int colorValue = Convert.ToInt32(text);
				if ( colorValue >= 0 && colorValue <= 255 )
				{
					string formattedValue = "0, 0," + colorValue.ToString();
					FormatRGB(formattedValue);
					return;
				}
			}
			catch ( Exception )
			{
				// Could not figure out what the string is
				ResetTextBox("Invalid Color Value");
				return;
			}

			// If we get here maybe we have a web or systemcolor
			Color currentColor = Color.FromName(text);
			if ( !(currentColor.A == 0 && currentColor.R == 0
				&& currentColor.G == 0 && currentColor.B == 0) )
			{
				// reset color in text box
				InitializeColorTextBoxString(currentColor, "CustomTab");
				return;
			}

			// Could not figure out what the string is
			ResetTextBox("Invalid Color Value");
		}

		void FormatRGB(string text)
		{

			string[] RGBs = text.Split(',');
			if ( RGBs.Length != 3 )
			{
				// If we don't have three pieces of information, then the
				// string is not properly formatted, inform the use
				ResetTextBox("Invalid color value");
				return;
			}

			string stringR = RGBs[0];
			string stringG = RGBs[1];
			string stringB = RGBs[2];
			int R, G, B;

			try
			{
				R = Convert.ToInt32(stringR);
				G = Convert.ToInt32(stringG);
				B = Convert.ToInt32(stringB);
				if ( ( R < 0 || R > 255 ) || ( G < 0 || G > 255 ) || ( B < 0 || B > 255 ) )
				{
					ResetTextBox("Out of bounds RGB value");
					return;
				}
				else
				{
					// Convert to color
					Color color = Color.FromArgb(R, G, B);
					// See if we have either a web color or a system color
					Color knownColor = Color.Empty;
					bool isKnown = ColorUtil.IsKnownColor( color, ref knownColor, true);
					if ( !isKnown )
						isKnown = ColorUtil.IsSystemColor(color, ref knownColor);
					if ( isKnown )
						color = knownColor;
					// reset color in text box
					currentColor = color;
					InitializeColorTextBoxString(color, "CustomTab");
				}
			}
			catch ( InvalidCastException )
			{
				ResetTextBox("Invalid RGB value");
				return;
			}
		}

		void ResetTextBox(string ErrorMessage)
		{
			MessageBox.Show(this, ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
			// reset to old value
			InitializeColorTextBoxString(Color, null);
		}
		void DrawColorPickerState()
		{

			using (Graphics g = CreateGraphics())
			{
				Rectangle rc = ClientRectangle;

				if ( drawState != DrawState.Disable )
				{
					using ( Brush b = new SolidBrush(currentColor) )
					{
						g.FillRectangle(b, displayColorRect);
						g.DrawRectangle(Pens.Black, displayColorRect.Left,
							displayColorRect.Top, displayColorRect.Width-1, displayColorRect.Height-1);
					}
				}

				if ( drawState == DrawState.Normal )
				{
					// Make sure arrow button look in its
					// normal state too
					arrowButton.DrawState = DrawState.Normal;
					// Draw white border
					using ( Pen p = new Pen(Color.White) )
					{
						g.DrawRectangle(p, rc.Left, rc.Top, rc.Width-1, rc.Height-1);
					}
				}
				else if ( drawState == DrawState.Hot )
				{
					if ( !(previousState == DrawState.Pressed   &&
						colorDropDown.Visible == true) )
					{

						// Make sure arrow button look in its
						// normal state too
						arrowButton.DrawState = DrawState.Hot;
					}
					g.DrawRectangle(new Pen(ColorUtil.VSNetBorderColor), rc.Left, rc.Top, rc.Width-1, rc.Height-1);
				}
				else if ( drawState == DrawState.Pressed )
				{
					// Make sure arrow button look in its
					// normal state too
					arrowButton.DrawState = DrawState.Pressed;
					g.DrawRectangle(new Pen(ColorUtil.VSNetBorderColor), rc.Left, rc.Top, rc.Width-1, rc.Height-1);
				}
				else if ( drawState == DrawState.Disable )
				{
					arrowButton.DrawState = DrawState.Disable;
					g.DrawRectangle(SystemPens.ControlDark, displayColorRect.Left,
						displayColorRect.Top, displayColorRect.Width-1, displayColorRect.Height-1);
					using ( Pen p = new Pen(Color.White) )
					{
						g.DrawRectangle(p, rc.Left, rc.Top, rc.Width-1, rc.Height-1);
					}
				}
			}

			previousState = drawState;
		}

		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.colorTextBox = new System.Windows.Forms.TextBox();
			this.arrowButton = new ArrowButton(this);
			this.SuspendLayout();
			//
			// colorTextBox
			//
			this.colorTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.colorTextBox.Location = new System.Drawing.Point(38, 4);
			this.colorTextBox.Name = "colorTextBox";
			this.colorTextBox.Size = new System.Drawing.Size(122, 13);
			this.colorTextBox.TabIndex = 0;
			this.colorTextBox.Text = "textBox1";
			this.colorTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.colorTextBox_KeyUp);
			//
			// arrowButton
			//
			this.arrowButton.BackColor = System.Drawing.SystemColors.Control;
			this.arrowButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.arrowButton.Location = new System.Drawing.Point(166, 4);
			this.arrowButton.Name = "arrowButton";
			this.arrowButton.Size = new System.Drawing.Size(18, 10);
			this.arrowButton.TabIndex = 1;
			this.arrowButton.Click += new System.EventHandler(this.arrowButton_Click);
			//
			// ColorPicker
			//
			this.BackColor = System.Drawing.Color.White;
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.arrowButton,
																		  this.colorTextBox});
			this.Name = "ColorPicker";
			this.Size = new System.Drawing.Size(190, 24);
			this.Load += new System.EventHandler(this.ColorPicker_Load);
			this.ResumeLayout(false);

		}
		#endregion
	}

}
