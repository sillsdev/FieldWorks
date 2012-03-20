using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Resources;
using System.Reflection;
using SidebarLibrary.WinControls;
using SidebarLibrary.General;

namespace SidebarLibrary.WinControls
{
	/// <summary>
	/// Summary description for CustomColorDlg.
	/// </summary>
	public class CustomColorDlg : System.Windows.Forms.Form
	{
		#region Class Variables
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label Label1;
		private Color currentColor = Color.White;
		private System.Windows.Forms.Panel currentColorPanel;
		private System.Windows.Forms.PictureBox paletteBox;
		private NumericTextBox hueEdit;
		private NumericTextBox satEdit;
		private NumericTextBox lumEdit;
		private NumericTextBox redEdit;
		private NumericTextBox greenEdit;
		private NumericTextBox blueEdit;
		private System.Windows.Forms.PictureBox lumBox;
		private Color paletteColor;
		private bool drawCrossHair = true;
		private Point crossPos = new Point(0, 0);
		private float DELTA_WIDTH = 240.0f/200.0f;
		private float DELTA_HEIGHT = 240.0f/186.0f;
		private bool updatingUI = false;
		private ResourceManager rm = null;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		#endregion

		#region Constructors
		public CustomColorDlg()
		{

			Assembly thisAssembly = Assembly.GetAssembly(Type.GetType("SidebarLibrary.WinControls.CustomColorDlg"));
			rm = new ResourceManager("SidebarLibrary.Resources.ImagesColorPicker", thisAssembly);
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			paletteBox.Image = (Bitmap)rm.GetObject("Palette");
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

		#region Properties
		public Color CurrentColor
		{
			get
			{
				return currentColor;
			}
			set
			{
				currentColor = value;
			}
		}

		#endregion

		#region Implementation
		void CustomColorDlg_Load(object sender, System.EventArgs e)
		{
			updatingUI = true;
			currentColorPanel.BackColor = CurrentColor;
			float r = CurrentColor.R;
			float g = CurrentColor.G;
			float b = CurrentColor.B;
			redEdit.Text = (Convert.ToInt32(r)).ToString();
			greenEdit.Text = (Convert.ToInt32(g)).ToString();
			blueEdit.Text = (Convert.ToInt32(b)).ToString();

			float h = 0;
			float s = 0;
			float l = 0;

			// Get h,s,l
			ColorUtil.RGBToHSL( (int)r, (int)g, (int)b, ref h, ref s, ref l);
			hueEdit.Text = Convert.ToString((int)h);
			satEdit.Text = Convert.ToString((int)s);
			lumEdit.Text = Convert.ToString((int)l);

			// Get palette color for interpolating the luminosity picture box
			ColorUtil.HSLToRGB( h, s, 120, ref r, ref g, ref b);
			paletteColor = Color.FromArgb((int)r, (int)g, (int)b);
			lumBox.Invalidate();

			// Calculate coordinates using default values for Hue and Sat
			CalculatePaletteMarkerCoordinates((int)h, (int)s);
			updatingUI = false;

		}

		void LumBox_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			Rectangle r = new Rectangle(0, 5, lumBox.Width - 10, lumBox.Height - 10);


			// Draw the color gradient
			using (LinearGradientBrush b = new LinearGradientBrush(r, Color.White, CurrentColor,
				System.Drawing.Drawing2D.LinearGradientMode.Vertical))
			{
				ColorBlend cb = new ColorBlend(3);
				cb.Colors[0] = Color.White;
				cb.Colors[1] = paletteColor;
				cb.Colors[2] = Color.Black;
				cb.Positions[0] = 0f;
				cb.Positions[1] = 0.5f;
				cb.Positions[2] = 1.0f;

				b.InterpolationColors = cb;
				e.Graphics.FillRectangle(b, r);
			}

			// Draw border around gradient rectangle
			Rectangle borderRect = r;
			borderRect.Inflate(0, 0);
			e.Graphics.DrawRectangle(new Pen(new SolidBrush(Color.Black), 1), borderRect);

			// Draw the indicator
			Point[] pts = new Point[3];
			int iTop = 240 - Convert.ToInt32(lumEdit.Text);
			iTop = ((lumBox.Height - 10) * iTop )/240 + 4;

			// Make sure we don't go out of bounds
			if ( iTop > lumBox.Height - 5 ) iTop = lumBox.Height - 5;
			if ( iTop < 5 ) iTop = 5;
			pts[0] = new Point(lumBox.Width - 8, iTop);
			pts[1] = new Point(lumBox.Width - 2, iTop - 7);
			pts[2] = new Point(lumBox.Width - 2, iTop + 7);
			e.Graphics.FillPolygon(Brushes.Black, pts);

		}

		void lumBox_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if ( lumBox.Capture == true )
			{
					int y  = e.Y;
				if ( y < 5 ) y = 5;
				if ( y > lumBox.Height - 5 ) y = lumBox.Height - 5;
				int lum = 240 - ((y-5) * 240)/(lumBox.Height-10);
				lumEdit.Text = lum.ToString();
				LuminosityChanged();
			}
		}

		void lumBox_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			lumBox.Capture = false;
			LuminosityChanged();
		}

		void lumBox_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			lumBox.Capture = true;
			int y  = e.Y;
			if ( y < 5 ) y = 5;
			if ( y > lumBox.Height - 5 ) y = lumBox.Height - 5;
			int lum = 240 - ((y-5) * 240)/(lumBox.Height-10);
			lumEdit.Text = lum.ToString();
			LuminosityChanged();
		}

		void LuminosityChanged()
		{
			if ( lumEdit.Text.Length == 0)
				return;

			updatingUI = true;
			float h = (float)Convert.ToDouble(hueEdit.Text);
			float s = (float)Convert.ToDouble(satEdit.Text);
			float l = (float)Convert.ToDouble(lumEdit.Text);
			ConvertToRGB(h, s, l);
			updatingUI = false;

		}

		void HueSatChanged()
		{
			// Calculate new Hue and Sat bases on mouse position coordinates
			updatingUI = true;
			float h = crossPos.X * DELTA_WIDTH;
			float s = (240.0f -(crossPos.Y * DELTA_HEIGHT));
			float l = (float)Convert.ToDouble(lumEdit.Text);

			hueEdit.Text = Convert.ToString((int)h);
			satEdit.Text = Convert.ToString((int)s);
			ConvertToRGB(h, s, l);
			updatingUI = false;

		}

		void HueSatTextBoxChanged()
		{
			// Calculate palette box marker coordinates positon base on
			// the new hue or sat value
			if ( hueEdit.Text == "" || satEdit.Text == "" )
				return;

			updatingUI = true;
			float h = (float)Convert.ToDouble(hueEdit.Text);
			float s = (float)Convert.ToDouble(satEdit.Text);
			float l = (float)Convert.ToDouble(lumEdit.Text);
			CalculatePaletteMarkerCoordinates((int)h, (int)s);
			ConvertToRGB(h, s, l);
			updatingUI = false;



		}

		void RGBTextChanged()
		{
			// Make sure none of the text boxes is empty
			if ( redEdit.Text == "" || greenEdit.Text == "" || blueEdit.Text == "" )
				return;

			updatingUI = true;
			float r = (float)Convert.ToDouble(redEdit.Text);
			float g = (float)Convert.ToDouble(greenEdit.Text);
			float b = (float)Convert.ToDouble(blueEdit.Text);

			float h = 0.0f;
			float s = 0.0f;
			float l = 0.0f;

			// convert to h, s, and l
			ColorUtil.RGBToHSL((int)r, (int)g, (int)b, ref h, ref s, ref l);
			// Update text boxes
			hueEdit.Text = Convert.ToString((int)h);
			satEdit.Text = Convert.ToString((int)s);
			lumEdit.Text = Convert.ToString((int)l);
			// Update current color
			CurrentColor = Color.FromArgb((int)r, (int)g, (int)b);
			currentColorPanel.BackColor = CurrentColor;

			// Update palette color (luminosity box)
			ColorUtil.HSLToRGB( h, s, 120, ref r, ref g, ref b);
			paletteColor = Color.FromArgb((int)r, (int)g, (int)b);
			lumBox.Invalidate();

			// Update palette box
			CalculatePaletteMarkerCoordinates((int)h, (int)s);
			updatingUI = false;

		}

		void ConvertToRGB(float h, float s, float l)
		{
			// Now get the new RGB values
			float r = 0.0f;
			float g = 0.0f;
			float b = 0.0f;
			ColorUtil.HSLToRGB(h, s, l, ref r, ref g, ref b);
			CurrentColor = Color.FromArgb((int)r, (int)g, (int)b);
			redEdit.Text = (Convert.ToInt32(r)).ToString();
			greenEdit.Text = (Convert.ToInt32(g)).ToString();
			blueEdit.Text = (Convert.ToInt32(b)).ToString();
			currentColorPanel.BackColor = CurrentColor;

			// Palette color is the pure form of the hue/sat
			ColorUtil.HSLToRGB( h, s, 120, ref r, ref g, ref b);
			paletteColor = Color.FromArgb((int)r, (int)g, (int)b);
			lumBox.Invalidate();
		}

		void CalculatePaletteMarkerCoordinates( int Hue, int Sat)
		{
			crossPos.X = (int)(Hue/DELTA_WIDTH);
			crossPos.Y = (int)((240 - Sat)/DELTA_HEIGHT);
			paletteBox.Invalidate();

		}

		void paletteBox_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{

			if ( paletteBox.Capture != true )
				return;

			crossPos.X = e.X;
			crossPos.Y = e.Y;

			// Make sure mouse does not go out of the palette picture area
			Rectangle rc = paletteBox.ClientRectangle;
			bool updateMousePos = false;
			if ( e.X <= rc.Left )
			{
				updateMousePos = true;
				crossPos.X = rc.Left;
			}
			if ( e.X >= rc.Right)
			{   updateMousePos = true;
				crossPos.X = rc.Right;
			}
			if ( e.Y <= rc.Top )
			{
				updateMousePos = true;
				crossPos.Y = rc.Top;
			}
			if ( e.Y >= rc.Bottom )
			{
				updateMousePos = true;
				crossPos.Y = rc.Bottom;
			}

			if ( updateMousePos )
				Cursor.Position = paletteBox.PointToScreen(new Point(crossPos.X, crossPos.Y));

			HueSatChanged();

		}

		void paletteBox_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			paletteBox.Capture = false;
			drawCrossHair = true;
			crossPos.X = e.X;
			crossPos.Y = e.Y;
			paletteBox.Invalidate();
			HueSatChanged();
		}

		void paletteBox_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			paletteBox.Capture = true;
			drawCrossHair = false;
			crossPos.X = e.X;
			crossPos.Y = e.Y;
			paletteBox.Invalidate();
		}

		void paletteBox_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{

			// Draw the cross-hair
			if (drawCrossHair)
			{
				using (Pen p = new Pen(Brushes.Black, 3))
				{
					int x = crossPos.X;
					int y = crossPos.Y;
					e.Graphics.DrawLine(p, x - 10, y, x - 5, y);
					e.Graphics.DrawLine(p, x + 10, y, x + 5, y);
					e.Graphics.DrawLine(p, x, y + 11, x, y + 5);
					e.Graphics.DrawLine(p, x, y - 10, x, y - 4);
				}
			}

		}

		void hueEdit_TextChanged(object sender, System.EventArgs e)
		{
			if ( !updatingUI )
				HueSatTextBoxChanged();
		}

		void satEdit_TextChanged(object sender, System.EventArgs e)
		{
			if ( !updatingUI )
				HueSatTextBoxChanged();
		}

		void lumEdit_TextChanged(object sender, System.EventArgs e)
		{
			if ( !updatingUI )
				LuminosityChanged();
		}

		void redEdit_TextChanged(object sender, System.EventArgs e)
		{
			if ( !updatingUI )
				RGBTextChanged();
		}

		void greenEdit_TextChanged(object sender, System.EventArgs e)
		{
			if ( !updatingUI )
				RGBTextChanged();
		}

		void blueEdit_TextChanged(object sender, System.EventArgs e)
		{
			if ( !updatingUI )
				RGBTextChanged();
		}

		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(CustomColorDlg));
			this.paletteBox = new System.Windows.Forms.PictureBox();
			this.lumBox = new System.Windows.Forms.PictureBox();
			this.currentColorPanel = new System.Windows.Forms.Panel();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.hueEdit = new NumericTextBox();
			this.satEdit = new NumericTextBox();
			this.lumEdit = new NumericTextBox();
			this.redEdit = new NumericTextBox();
			this.greenEdit = new NumericTextBox();
			this.blueEdit = new NumericTextBox();
			this.Label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// paletteBox
			//
			this.paletteBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.paletteBox.Location = new System.Drawing.Point(10, 12);
			this.paletteBox.Name = "paletteBox";
			this.paletteBox.Size = new System.Drawing.Size(202, 188);
			this.paletteBox.TabIndex = 0;
			this.paletteBox.TabStop = false;
			this.paletteBox.Paint += new System.Windows.Forms.PaintEventHandler(this.paletteBox_Paint);
			this.paletteBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.paletteBox_MouseUp);
			this.paletteBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.paletteBox_MouseMove);
			this.paletteBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.paletteBox_MouseDown);
			//
			// lumBox
			//
			this.lumBox.Location = new System.Drawing.Point(220, 7);
			this.lumBox.Name = "lumBox";
			this.lumBox.Size = new System.Drawing.Size(24, 198);
			this.lumBox.TabIndex = 1;
			this.lumBox.TabStop = false;
			this.lumBox.Paint += new System.Windows.Forms.PaintEventHandler(this.LumBox_Paint);
			this.lumBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lumBox_MouseUp);
			this.lumBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.lumBox_MouseMove);
			this.lumBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lumBox_MouseDown);
			//
			// currentColorPanel
			//
			this.currentColorPanel.BackColor = System.Drawing.SystemColors.Control;
			this.currentColorPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.currentColorPanel.Location = new System.Drawing.Point(12, 212);
			this.currentColorPanel.Name = "currentColorPanel";
			this.currentColorPanel.Size = new System.Drawing.Size(66, 48);
			this.currentColorPanel.TabIndex = 2;
			//
			// button1
			//
			this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.button1.Location = new System.Drawing.Point(52, 298);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(64, 22);
			this.button1.TabIndex = 3;
			this.button1.Text = "Add Color";
			//
			// button2
			//
			this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button2.Location = new System.Drawing.Point(138, 298);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(64, 22);
			this.button2.TabIndex = 4;
			this.button2.Text = "Cancel";
			//
			// hueEdit
			//
			this.hueEdit.Location = new System.Drawing.Point(124, 218);
			this.hueEdit.MaxLength = 3;
			this.hueEdit.Name = "hueEdit";
			this.hueEdit.SetRange = new System.Drawing.Size(0, 240);
			this.hueEdit.Size = new System.Drawing.Size(30, 20);
			this.hueEdit.TabIndex = 5;
			this.hueEdit.Text = "160";
			this.hueEdit.TextChanged += new System.EventHandler(this.hueEdit_TextChanged);
			//
			// satEdit
			//
			this.satEdit.Location = new System.Drawing.Point(124, 241);
			this.satEdit.MaxLength = 3;
			this.satEdit.Name = "satEdit";
			this.satEdit.SetRange = new System.Drawing.Size(0, 240);
			this.satEdit.Size = new System.Drawing.Size(30, 20);
			this.satEdit.TabIndex = 6;
			this.satEdit.Text = "0";
			this.satEdit.TextChanged += new System.EventHandler(this.satEdit_TextChanged);
			//
			// lumEdit
			//
			this.lumEdit.Location = new System.Drawing.Point(124, 264);
			this.lumEdit.MaxLength = 3;
			this.lumEdit.Name = "lumEdit";
			this.lumEdit.SetRange = new System.Drawing.Size(0, 240);
			this.lumEdit.Size = new System.Drawing.Size(30, 20);
			this.lumEdit.TabIndex = 7;
			this.lumEdit.Text = "0";
			this.lumEdit.TextChanged += new System.EventHandler(this.lumEdit_TextChanged);
			//
			// redEdit
			//
			this.redEdit.Location = new System.Drawing.Point(200, 218);
			this.redEdit.MaxLength = 3;
			this.redEdit.Name = "redEdit";
			this.redEdit.SetRange = new System.Drawing.Size(0, 255);
			this.redEdit.Size = new System.Drawing.Size(30, 20);
			this.redEdit.TabIndex = 8;
			this.redEdit.Text = "0";
			this.redEdit.TextChanged += new System.EventHandler(this.redEdit_TextChanged);
			//
			// greenEdit
			//
			this.greenEdit.Location = new System.Drawing.Point(200, 241);
			this.greenEdit.MaxLength = 3;
			this.greenEdit.Name = "greenEdit";
			this.greenEdit.SetRange = new System.Drawing.Size(0, 255);
			this.greenEdit.Size = new System.Drawing.Size(30, 20);
			this.greenEdit.TabIndex = 9;
			this.greenEdit.Text = "0";
			this.greenEdit.TextChanged += new System.EventHandler(this.greenEdit_TextChanged);
			//
			// blueEdit
			//
			this.blueEdit.Location = new System.Drawing.Point(200, 264);
			this.blueEdit.MaxLength = 3;
			this.blueEdit.Name = "blueEdit";
			this.blueEdit.SetRange = new System.Drawing.Size(0, 255);
			this.blueEdit.Size = new System.Drawing.Size(30, 20);
			this.blueEdit.TabIndex = 10;
			this.blueEdit.Text = "0";
			this.blueEdit.TextChanged += new System.EventHandler(this.blueEdit_TextChanged);
			//
			// Label1
			//
			this.Label1.Location = new System.Drawing.Point(96, 220);
			this.Label1.Name = "Label1";
			this.Label1.Size = new System.Drawing.Size(26, 18);
			this.Label1.TabIndex = 11;
			this.Label1.Text = "Hue";
			this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			//
			// label2
			//
			this.label2.Location = new System.Drawing.Point(96, 243);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(26, 18);
			this.label2.TabIndex = 12;
			this.label2.Text = "Sat";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			//
			// label3
			//
			this.label3.Location = new System.Drawing.Point(96, 266);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(26, 18);
			this.label3.TabIndex = 13;
			this.label3.Text = "Lum";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			//
			// label4
			//
			this.label4.Location = new System.Drawing.Point(166, 220);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(34, 18);
			this.label4.TabIndex = 14;
			this.label4.Text = "Red:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			//
			// label5
			//
			this.label5.Location = new System.Drawing.Point(158, 242);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(42, 18);
			this.label5.TabIndex = 15;
			this.label5.Text = "Green:";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			//
			// label6
			//
			this.label6.Location = new System.Drawing.Point(158, 266);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(42, 18);
			this.label6.TabIndex = 16;
			this.label6.Text = "Blue:";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			//
			// label7
			//
			this.label7.Location = new System.Drawing.Point(18, 266);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(60, 14);
			this.label7.TabIndex = 17;
			this.label7.Text = "Color|Solid";
			//
			// CustomColorDlg
			//
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(246, 330);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.label7,
																		  this.label6,
																		  this.label5,
																		  this.label4,
																		  this.label3,
																		  this.label2,
																		  this.Label1,
																		  this.blueEdit,
																		  this.greenEdit,
																		  this.redEdit,
																		  this.lumEdit,
																		  this.satEdit,
																		  this.hueEdit,
																		  this.button2,
																		  this.button1,
																		  this.currentColorPanel,
																		  this.lumBox,
																		  this.paletteBox});
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "CustomColorDlg";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Define Color";
			this.Load += new System.EventHandler(this.CustomColorDlg_Load);
			this.ResumeLayout(false);

		}
		#endregion

	}
}
