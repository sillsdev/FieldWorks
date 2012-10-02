using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using SidebarLibrary.General;
using SidebarLibrary.WinControls;
using SidebarLibrary.Win32;

namespace SidebarLibrary.WinControls
{
	#region Delegates
	public delegate void ColorChangeEventHandler(object sender, ColorChangeArgs e);
	#endregion

	#region Helper Classes
	[ToolboxItem(false)]
	public class ColorChangeArgs: EventArgs
	{
		#region Class Variables
		Color newColor;
		string senderName;
		#endregion

		#region Constructors
		public ColorChangeArgs(Color newColor,  string senderName)
		{
			this.newColor = newColor;
			this.senderName = senderName;
		}
		#endregion

		#region Properties
		public Color NewColor
		{
			get { return newColor;}
		}

		public string SenderName
		{
			get	{ return senderName;}
		}
		#endregion
	}

	#endregion

	/// <summary>
	/// Summary description for ColorPickerDropDown.
	/// </summary>
	public class ColorPickerDropDown : System.Windows.Forms.Form
	{

		#region Events
		public event ColorChangeEventHandler ColorChanged;
		#endregion

		#region Class Variables
		private System.Windows.Forms.TabControl tabControl;
		private ColorListBox webColorsList;
		private ColorListBox systemColorsList;
		private System.Windows.Forms.TabPage customColorsPage;
		private System.Windows.Forms.TabPage webColorsPage;
		private System.Windows.Forms.TabPage systemColorsPage;
		private const int CUSTOM_COLORS_COUNT = 64;
		private const int CUSTOM_COLOR_WIDTH = 20;
		private const int CUSTOM_COLOR_HEIGHT = 20;
		private const int CUSTOM_COLORS_HORIZ_ITEMS = 8;
		private const int CUSTOM_COLORS_VERT_ITEMS = 8;
		private const int USED_ROWS = 6;
		private const int USED_COLS = 8;
		private bool customColorsRectsSaved = false;
		private bool rightButtonDown = false;
		private int currentCustomColorIndex = 0;
		private Color currentColor;
		private bool exitLoop = false;

		static Rectangle[] customColorsRects = new Rectangle[64];

		#region Custom Colors Array
		static Color[] customColors = {
										  Color.FromArgb(255,255,255), Color.FromArgb(224,224,224), Color.FromArgb(192,192,192),
										  Color.FromArgb(128,128,128), Color.FromArgb(64,64,64), Color.FromArgb(0,0,0),
										  Color.White, Color.White,

										  Color.FromArgb(255,192,192), Color.FromArgb(255,128,128), Color.FromArgb(255,0,0),
										  Color.FromArgb(192,0,0), Color.FromArgb(128,0,0), Color.FromArgb(64,0,0),
										  Color.White, Color.White,

										  Color.FromArgb(255,224,192), Color.FromArgb(255,192,128), Color.FromArgb(255,128,0),
										  Color.FromArgb(192,64,0), Color.FromArgb(128,64,0), Color.FromArgb(128,64,64),
										  Color.White, Color.White,

										  Color.FromArgb(255,255,192), Color.FromArgb(255,255,128), Color.FromArgb(255,255,0),
										  Color.FromArgb(192,192,0), Color.FromArgb(128,128,0), Color.FromArgb(64,64,0),
										  Color.White, Color.White,

										  Color.FromArgb(192,255,192), Color.FromArgb(128,255,128), Color.FromArgb(0,255,0),
										  Color.FromArgb(0,192,0), Color.FromArgb(0,128,0), Color.FromArgb(0,64,0),
										  Color.White, Color.White,

										  Color.FromArgb(192,255,255), Color.FromArgb(128,255,255), Color.FromArgb(0,255,255),
										  Color.FromArgb(0,192,192), Color.FromArgb(0,128,128), Color.FromArgb(0,64,64),
										  Color.White, Color.White,

										  Color.FromArgb(192,192,255), Color.FromArgb(128,128,255), Color.FromArgb(0,0,255),
										  Color.FromArgb(0,0,192), Color.FromArgb(0,0,128), Color.FromArgb(0,0,64),
										  Color.White, Color.White,

										  Color.FromArgb(255,192,255), Color.FromArgb(255,128,255), Color.FromArgb(255,0,255),
										  Color.FromArgb(192,0,192), Color.FromArgb(128,0,128), Color.FromArgb(64,0,64),
										  Color.White, Color.White};
		#endregion

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#endregion

		#region Constructors
		public ColorPickerDropDown()
		{
			// This call is required by the Windows.Forms Form Designer.
			webColorsList = new ColorListBox();
			systemColorsList = new ColorListBox();
			StartPosition = FormStartPosition.Manual;
			InitializeComponent();
			InitializeListBoxes();
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
			Graphics g = pe.Graphics;
			Rectangle rc = ClientRectangle;
			g.DrawRectangle(Pens.Black, rc.Left, rc.Top, rc.Width-1, rc.Height-1);
		}

		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);
			exitLoop = true;
			Visible = false;
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			// Size tab control
			Rectangle rc = ClientRectangle;
			tabControl.Left = 2;
			tabControl.Top = 2;
			tabControl.Width = rc.Width-3;
			tabControl.Height = rc.Height-3;

			// Size customColorsPage
			customColorsPage.Left = 0;
			customColorsPage.Top = 0;
			customColorsPage.Width = rc.Width-3;
			customColorsPage.Height = rc.Height-3;

			// Size webColorsPage
			webColorsPage.Left = 0;
			webColorsPage.Top = 0;
			webColorsPage.Width = rc.Width-3;
			webColorsPage.Height = rc.Height-3;
			//  Size listbox in webcolorPage
			webColorsList.Left = 0;
			webColorsList.Top = 0;
			webColorsList.Width = rc.Width-15;
			webColorsList.Height = rc.Height-26;

			// Size systemColorsPage
			systemColorsPage.Left = 0;
			systemColorsPage.Top = 0;
			systemColorsPage.Width = rc.Width-3;
			systemColorsPage.Height = rc.Height-3;
			systemColorsList.Left = 0;
			systemColorsList.Top = 0;
			systemColorsList.Width = rc.Width-15;
			systemColorsList.Height = rc.Height-26;

		}

		protected override  void WndProc(ref Message m)
		{
			bool callBase = true;
			switch(m.Msg)
			{
				case ((int)Msg.WM_PAINT):
					callBase = false;
					base.WndProc(ref m);
					exitLoop = false;
					// Let any message directed to children
					// to be process before we start our loop
					Application.DoEvents();
					StartPeekMessageLoop();
					break;
				default:
					break;
			}
			if ( callBase )
				base.WndProc(ref m);
		}

		#endregion

		#region Properties
		public Color CurrentColor
		{
			set
			{
				currentColor = value;
			}
		}

		#endregion

		#region Implementation
		protected void OnColorChanged(ColorChangeArgs e)
		{
			currentColor = e.NewColor;
			if ( ColorChanged != null )
				ColorChanged(this, e);
		}

		void InitializeListBoxes()
		{
			webColorsList.ColorArray = ColorUtil.KnownColorNames;
			systemColorsList.ColorArray = ColorUtil.SystemColorNames;

			//
			// customColorsPage
			//
			this.customColorsPage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.customColorsPage.Location = new System.Drawing.Point(4, 22);
			this.customColorsPage.Name = "customColorsPage";
			this.customColorsPage.Size = new System.Drawing.Size(178, 188);
			this.customColorsPage.TabIndex = 0;
			this.customColorsPage.Text = "Custom";
			this.customColorsPage.Click += new System.EventHandler(this.customColorsPage_Click);
			this.customColorsPage.Paint += new System.Windows.Forms.PaintEventHandler(this.customColorsPage_Paint);
			this.customColorsPage.MouseDown += new System.Windows.Forms.MouseEventHandler(this.customColorsPage_MouseDown);

			//
			// webColorsList
			//
			this.webColorsList.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.webColorsList.Location = new System.Drawing.Point(6, 4);
			this.webColorsList.Name = "webColorsList";
			this.webColorsList.Size = new System.Drawing.Size(178, 188);
			this.webColorsList.TabIndex = 0;
			this.webColorsList.Click += new System.EventHandler(this.webColorsList_Click);
			this.webColorsPage.Controls.AddRange(new System.Windows.Forms.Control[] {
																						this.webColorsList});
			//
			// systemColorsList
			//
			this.systemColorsList.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.systemColorsList.Location = new System.Drawing.Point(8, 8);
			this.systemColorsList.Name = "systemColorsList";
			this.systemColorsList.Size = new System.Drawing.Size(178, 188);
			this.systemColorsList.TabIndex = 0;
			this.systemColorsList.Click += new System.EventHandler(this.systemColorsList_Click);
			this.systemColorsPage.Controls.AddRange(new System.Windows.Forms.Control[] {
																						   this.systemColorsList});

		}

		void customColorsPage_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			// Paint custom colors
			Graphics g = e.Graphics;
			Rectangle rc = customColorsPage.ClientRectangle;

			rc.Inflate(-2, -2);
			int gap = 3;
			int index;
			for ( int i = 0; i < CUSTOM_COLORS_HORIZ_ITEMS; i++ )
			{
				int Left = rc.Left + (CUSTOM_COLOR_WIDTH*i)+ i*gap;
				int Top = rc.Top;

				for ( int j = 0; j < CUSTOM_COLORS_VERT_ITEMS; j++ )
				{
					Top = rc.Top + (CUSTOM_COLOR_HEIGHT*j) + (j*gap);
					index = i*8 + j;
					ControlPaint.DrawBorder3D(g, Left, Top, CUSTOM_COLOR_WIDTH, CUSTOM_COLOR_HEIGHT, Border3DStyle.Sunken);
					g.FillRectangle(new SolidBrush(customColors[index]),Left+1, Top+1, CUSTOM_COLOR_WIDTH-2, CUSTOM_COLOR_HEIGHT-2);

					if ( !customColorsRectsSaved )
					{
						customColorsRects[index] = new Rectangle(Left+1, Top+1, CUSTOM_COLOR_WIDTH-2, CUSTOM_COLOR_HEIGHT-2);
					}

					if ( currentCustomColorIndex == index )
					{
						Rectangle reverseRect = new Rectangle(Left-1, Top-1, CUSTOM_COLOR_WIDTH+3, CUSTOM_COLOR_HEIGHT+3);
						reverseRect = customColorsPage.RectangleToScreen(reverseRect);
						ControlPaint.DrawReversibleFrame(reverseRect, SystemColors.Control, FrameStyle.Thick);
					}

				}
			}
			customColorsRectsSaved = true;
		}

		void customColorsPage_Click(object sender, System.EventArgs e)
		{

			// Get current mouse position
			Point screenMousePos = Control.MousePosition;
			// Convert mouse position to client coordinates
			Point clientMousePos = customColorsPage.PointToClient(screenMousePos);
			if ( rightButtonDown == true )
			{
				// Check if we need to show the custom color color picker dialog
				int index = -1;
				if ( IsSpareColor(clientMousePos, ref index))
				{
					using (CustomColorDlg dlg = new CustomColorDlg())
					{
						dlg.CurrentColor = customColors[index];
						if ( dlg.ShowDialog(this) == DialogResult.OK )
						{
							customColors[index] = dlg.CurrentColor;
							ColorChangeArgs ca = new ColorChangeArgs(dlg.CurrentColor, "CustomTab");
							OnColorChanged(ca);
						}
					}
				}
				rightButtonDown = false;
				return;
			}

//			Graphics g = customColorsPage.CreateGraphics();
//			Color clickedColor = ColorUtil.ColorFromPoint(g, clientMousePos.X, clientMousePos.Y);
//			g.Dispose();

			Color color = FindCustomColor(clientMousePos);
			if ( color != Color.Empty )
			{
				ColorChangeArgs ca = new ColorChangeArgs(color, "CustomTab");
				OnColorChanged(ca);
				// Hide the dropdown
				exitLoop = true;
				Visible = false;
			}

		}

		Color FindCustomColor(Point point)
		{
			Color color = Color.Empty;
			for ( int i = 0 ; i < CUSTOM_COLORS_COUNT; i++)
			{
				if ( customColorsRects[i].Contains(point) )
				{
					color = customColors[i];
					currentCustomColorIndex = i;
					return color;
				}
			}

			// Did not find the color
			// Should only happen if the user click between colors
			return color;
		}

		bool IsSpareColor(Point p, ref int index)
		{

			for ( int i = 0; i < CUSTOM_COLORS_HORIZ_ITEMS; i++ )
			{
				for ( int j = (CUSTOM_COLORS_VERT_ITEMS-2); j < CUSTOM_COLORS_VERT_ITEMS; j++ )
				{
					if ( customColorsRects[i*8 + j].Contains(p) )
					{
						index = i*8 + j;
						currentCustomColorIndex = index;
						return true;
					}
				}
			}
			return false;
		}

		void customColorsPage_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if ( e.Button == MouseButtons.Right )
			{
				rightButtonDown = true;
			}
		}

		void tabControl_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int index = tabControl.SelectedIndex;
			if ( index == 1 )
				webColorsList.Focus();
			else if ( index == 2)
				systemColorsList.Focus();

		}

		void webColorsList_Click(object sender, System.EventArgs e)
		{
			Color color = Color.FromName(webColorsList.Text);
			ColorChangeArgs ca = new ColorChangeArgs(color, "WebTab");
			OnColorChanged(ca);
			exitLoop = true;
			Visible = false;

		}

		void systemColorsList_Click(object sender, System.EventArgs e)
		{
			Color color = Color.FromName(systemColorsList.Text);
			currentColor = color;
			ColorChangeArgs ca = new ColorChangeArgs(color, "SystemTab");
			OnColorChanged(ca);
			exitLoop = true;
			Visible = false;
		}

		int GetCustomColorIndex(Color color)
		{
			for ( int i = 0; i < CUSTOM_COLORS_COUNT; i++ )
			{
				if ( customColors[i].Equals(color) )
					return i;
			}

			// Should not happen
			return -1;

		}

		bool GetSystemColorIndex(Color color, out int index)
		{
			index = -1;
			string colorName = color.Name;
			index = systemColorsList.FindStringExact(colorName);
			if ( index != ListBox.NoMatches )
			{
				systemColorsList.SelectedIndex = index;
				return true;
			}
			return false;
		}

		bool GetWebColorIndex(Color color, out int index)
		{
			index = -1;
			string colorName = color.Name;
			index = webColorsList.FindStringExact(colorName);
			if ( index != ListBox.NoMatches )
			{
				webColorsList.SelectedIndex = index;
				return true;
			}
			return false;
		}

		void ColorPickerDropDown_VisibleChanged(object sender, System.EventArgs e)
		{

			if ( !Visible )
				return;

			// Select tab base on current color index
			int index;
			bool isNamed = currentColor.IsNamedColor;
			if ( !isNamed )
			{
				// It got to be a custom color
				index = GetCustomColorIndex(currentColor);
				currentCustomColorIndex = index;
				tabControl.SelectedIndex = 0;
				return;
			}

			bool isSystemColor = GetSystemColorIndex(currentColor, out index);
			if ( isSystemColor )
			{
				tabControl.SelectedIndex = 2;
				return;
			}

			bool isWebColor = GetWebColorIndex(currentColor, out index);
			if ( isWebColor )
			{
				tabControl.SelectedIndex = 1;
			}
		}

		bool IsChild(IntPtr hWnd)
		{
			// Consider a child the tab control itself
			// the tab pages, or the list controls in the tab pages
			if ( tabControl.Handle == hWnd )
				return true;
			else if ( webColorsList.Handle == hWnd )
				return true;
			else if ( systemColorsList.Handle == hWnd )
				return true;
			else if ( customColorsPage.Handle == hWnd )
				return true;
			else if ( webColorsPage.Handle == hWnd )
				return true;
			else if ( systemColorsPage.Handle == hWnd )
				return true;

			return false;
		}

		void StartPeekMessageLoop()
		{
			// Create an object for storing windows message information
			Win32.MSG msg = new Win32.MSG();
			bool leaveMsg = false;

			// Process messages until exit condition recognised
			while(!exitLoop)
			{
				// Suspend thread until a windows message has arrived
				if (WindowsAPI.WaitMessage())
				{
					// Take a peek at the message details without removing from queue
					while(!exitLoop && WindowsAPI.PeekMessage(ref msg, 0, 0, 0, (int)Win32.PeekMessageFlags.PM_NOREMOVE))
					{
						//Console.WriteLine("Track {0} {1}", this.Handle, ((Msg)msg.message).ToString());
						//Console.WriteLine("Message is for {0 }", msg.hwnd);

						// Mouse was pressed in a window of this application
						if ((msg.message == (int)Msg.WM_LBUTTONDOWN) ||
							(msg.message == (int)Msg.WM_MBUTTONDOWN) ||
							(msg.message == (int)Msg.WM_RBUTTONDOWN) ||
							(msg.message == (int)Msg.WM_NCLBUTTONDOWN) ||
							(msg.message == (int)Msg.WM_NCMBUTTONDOWN) ||
							(msg.message == (int)Msg.WM_NCRBUTTONDOWN))
						{
							// Is the mouse event for this popup window?
							if (msg.hwnd != this.Handle && !IsChild(msg.hwnd) )
							{
								// No, then we need to exit the popup menu tracking
								exitLoop = true;

								// DO NOT process the message, leave it on the queue
								// and let the real destination window handle it.
								leaveMsg = true;
							}
						}
						else
						{
							// Mouse move occured
							if ( msg.message == (int)Msg.WM_MOUSEMOVE  )
							{
								// Is the mouse event for this popup window?
								if ((msg.hwnd != this.Handle && !IsChild(msg.hwnd)) )
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
		}

		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tabControl = new System.Windows.Forms.TabControl();
			this.customColorsPage = new System.Windows.Forms.TabPage();
			this.webColorsPage = new System.Windows.Forms.TabPage();
			this.systemColorsPage = new System.Windows.Forms.TabPage();
			this.tabControl.SuspendLayout();
			this.SuspendLayout();
			//
			// tabControl
			//
			this.tabControl.Controls.AddRange(new System.Windows.Forms.Control[] {
																					 this.customColorsPage,
																					 this.webColorsPage,
																					 this.systemColorsPage});
			this.tabControl.Cursor = System.Windows.Forms.Cursors.Default;
			this.tabControl.Location = new System.Drawing.Point(0, 4);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(200, 188);
			this.tabControl.TabIndex = 0;
			this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
			//
			// customColorsPage
			//
			this.customColorsPage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.customColorsPage.Location = new System.Drawing.Point(4, 22);
			this.customColorsPage.Name = "customColorsPage";
			this.customColorsPage.Size = new System.Drawing.Size(192, 162);
			this.customColorsPage.TabIndex = 0;
			this.customColorsPage.Text = "Custom";
			this.customColorsPage.Click += new System.EventHandler(this.customColorsPage_Click);
			this.customColorsPage.Paint += new System.Windows.Forms.PaintEventHandler(this.customColorsPage_Paint);
			this.customColorsPage.MouseDown += new System.Windows.Forms.MouseEventHandler(this.customColorsPage_MouseDown);
			//
			// webColorsPage
			//
			this.webColorsPage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.webColorsPage.Location = new System.Drawing.Point(4, 22);
			this.webColorsPage.Name = "webColorsPage";
			this.webColorsPage.Size = new System.Drawing.Size(192, 162);
			this.webColorsPage.TabIndex = 1;
			this.webColorsPage.Text = "Web";
			//
			// systemColorsPage
			//
			this.systemColorsPage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.systemColorsPage.Location = new System.Drawing.Point(4, 22);
			this.systemColorsPage.Name = "systemColorsPage";
			this.systemColorsPage.Size = new System.Drawing.Size(192, 162);
			this.systemColorsPage.TabIndex = 2;
			this.systemColorsPage.Text = "System";
			//
			// ColorPickerDropDown
			//
			this.AutoScaleMode = AutoScaleMode.None;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(198, 215);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.tabControl});
			this.ForeColor = System.Drawing.SystemColors.Control;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "ColorPickerDropDown";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.VisibleChanged += new System.EventHandler(this.ColorPickerDropDown_VisibleChanged);
			this.tabControl.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

	}

}
