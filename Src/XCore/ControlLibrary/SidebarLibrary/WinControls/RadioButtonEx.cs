using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using SidebarLibrary.General;
using SidebarLibrary.Win32;

namespace SidebarLibrary.WinControls
{
	/// <summary>
	/// Summary description for RadioButtonEx.
	/// </summary>
	[ToolboxItem(false)]
	public class RadioButtonEx : System.Windows.Forms.RadioButton
	{
		#region Class Variables
		const int CIRCLE_DIAMETER = 11;
		DrawState drawState = DrawState.Normal;
		#endregion

		#region Constructors
		public RadioButtonEx()
		{
			// Our control needs to have Flat style set
			FlatStyle = FlatStyle.Flat;
		}

		#endregion

		#region Overrides
		protected override void OnMouseEnter(EventArgs e)
		{
			// Set state to hot
			base.OnMouseEnter(e);
			drawState = DrawState.Hot;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			// Set state to Normal
			base.OnMouseLeave(e);
			if ( !ContainsFocus )
			{
				drawState = DrawState.Normal;
				Invalidate();
			}
		}

		protected override void OnGotFocus(EventArgs e)
		{
			// Set state to Hot
			base.OnGotFocus(e);
			drawState = DrawState.Hot;
			Invalidate();
		}

		protected override void OnLostFocus(EventArgs e)
		{
			// Set state to Normal
			base.OnLostFocus(e);
			drawState = DrawState.Normal;
			Invalidate();
		}

		protected override  void WndProc(ref Message m)
		{
			bool callBase = true;

			switch(m.Msg)
			{
				case ((int)Msg.WM_PAINT):
				{
					// Let the edit control do its painting
					base.WndProc(ref m);
					// Now do our custom painting
					DrawRadioButtonState(Enabled?drawState:DrawState.Disable);
					callBase = false;
				}
					break;
				default:
					break;
			}

			if ( callBase )
				base.WndProc(ref m);

		}

		#endregion

		#region Properties
		public new FlatStyle FlatStyle
		{
			// Don't let the user change this property
			// to anything other than flat, otherwise
			// there would be painting problems
			get { return base.FlatStyle; }
			set
			{
				if ( value != FlatStyle.Flat )
				{
					// Throw an exception to tell the user
					// that this property needs to be "Flat"
					// if he is to use this class
					string message = "FlatStyle needs to be set to Flat for this class";
					ArgumentException argumentException = new ArgumentException("FlatStyle", message);
					throw(argumentException);
				}
				else
					base.FlatStyle = value;
			}
		}

		#endregion

		#region Implementation
		void DrawRadioButtonState(DrawState state)
		{
			Rectangle rect = ClientRectangle;

			// Create DC for the whole edit window instead of just for the client area
			IntPtr hDC = WindowsAPI.GetDC(Handle);
			Rectangle circleRect = new Rectangle(rect.Left, rect.Top + (rect.Height-CIRCLE_DIAMETER)/2,
				CIRCLE_DIAMETER, CIRCLE_DIAMETER);

			using (Graphics g = Graphics.FromHdc(hDC))
			{
				// Always paint the inner circle with the Window color
				g.FillEllipse(SystemBrushes.Window, circleRect);

				if ( state == DrawState.Normal )
				{
					// Draw normal black circle
					g.DrawEllipse(Pens.Black, circleRect);
				}
				else if ( state == DrawState.Hot )
				{
					g.DrawEllipse(SystemPens.Highlight, circleRect);
				}
				else if ( state == DrawState.Disable )
				{
					// draw highlighted rectangle
					g.DrawEllipse(SystemPens.ControlDark, circleRect);
				}

				if ( Checked )
					DrawCheckGlyph(g, state);
			}

			// Release DC
			WindowsAPI.ReleaseDC(Handle, hDC);

		}

		void DrawCheckGlyph(Graphics g, DrawState state)
		{
			Rectangle rc = ClientRectangle;

			// Calculate coordinates
			Point point1 = new Point(rc.Left + 4, rc.Top + rc.Top + (rc.Height-4)/2 + 2);
			Point point2 = new Point(point1.X + 4, point1.Y);
			Point point3 = new Point(rc.Left + 6, rc.Top + rc.Top + (rc.Height-4)/2);
			Point point4 = new Point(point3.X, point3.Y + 4);

			// Choose color
			Color checkColor = Color.Empty;
			if ( state == DrawState.Normal )
				checkColor = Color.Black;
			else if (state == DrawState.Hot )
				checkColor = ColorUtil.VSNetBorderColor;
			else if ( state == DrawState.Disable )
				checkColor = SystemColors.ControlDark;

			// Draw the check mark
			using ( Pen pen = new Pen(checkColor, 2) )
			{
				g.DrawLine(pen, point1, point2);
				g.DrawLine(pen, point3, point4);
			}

		}

		#endregion


	}
}
