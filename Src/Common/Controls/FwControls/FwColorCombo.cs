// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: FwColorCombo.cs
// Responsibility: DavidO
// Last reviewed:
//
// Implementation of FwColorCombo
//
// --------------------------------------------------------------------------------------------

using System;
using System.Windows.Forms.VisualStyles;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FwColorCombo.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class FwColorCombo : UserControl, IFWDisposable
	{
		/// <summary>This event occurs when the color has been picked</summary>
		public event EventHandler ColorPicked;

		private String m_colorName = Color.Black.Name;
		private Rectangle m_buttonRect;
		private DropDownButtonState m_buttonState = DropDownButtonState.Normal;
		private ColorPickerDropDown m_dropDown;
		/// <summary>True if user clicked anywhere in combo box to close drop down</summary>
		private bool m_fDropDownClickedClose;
		private Color m_currentColor = Color.Empty;
		private bool m_fShowUnspecified = true;

		/// <summary>State values for the drop down button</summary>
		private enum DropDownButtonState
		{
			Hot,
			Pressed,
			Normal
		};

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwColorCombo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwColorCombo()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			this.SetStyle(ControlStyles.Selectable, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
			}
			m_colorName = null;
			m_dropDown = null;

			base.Dispose(disposing);
		}

		#region Component Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwColorCombo));
			this.SuspendLayout();
			//
			// FwColorCombo
			//
			this.BackColor = System.Drawing.SystemColors.Window;
			resources.ApplyResources(this, "$this");
			this.ForeColor = System.Drawing.SystemColors.WindowText;
			this.Name = "FwColorCombo";
			this.ResumeLayout(false);

		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the color value.
		/// </summary>
		/// <value>The color value.</value>
		/// ------------------------------------------------------------------------------------
		public Color ColorValue
		{
			get
			{
				CheckDisposed();

				return m_currentColor;
			}
			set
			{
				CheckDisposed();

				m_currentColor = value;
				m_colorName = ColorPickerMatrix.ColorToName(value);
				Invalidate();

				// notify any subscriber of the color change
				if (ColorPicked != null)
					ColorPicked(this, EventArgs.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether [show unspecified button].
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [show unspecified button]; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool ShowUnspecifiedButton
		{
			set { CheckDisposed(); m_fShowUnspecified = value;}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the resize event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			// make space for the button rectangle
			int buttonWidth = 16;
			int buttonMargin = 1;
			if (!Application.RenderWithVisualStyles)
			{
				buttonMargin = 2;
				buttonWidth = 14;
			}
			m_buttonRect = new Rectangle(
				ClientRectangle.Width - (buttonWidth + buttonMargin), ClientRectangle.Y + buttonMargin,
				buttonWidth, ClientRectangle.Height - (buttonMargin * 2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.GotFocus"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			this.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.LostFocus"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			this.Invalidate();
		}

		#region Mouse events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the mouse moves, redraw the control
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			UpdateOnMouseAction();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the mouse clicks on the button, set the button state to pressed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			UpdateOnMouseAction();

			// Drop down if user clicks anywhere in the combo box
			if (!m_fDropDownClickedClose && ClientRectangle.Contains(e.Location) &&
				(e.Button & MouseButtons.Left) > 0)
			{
				ShowDropDown();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the mouse releases the mouse button, change the state of the button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			UpdateOnMouseAction();

			// If the user closes the drop down by clicking on the button again we don't want
			// to show the button pressed. Therefore we have to leave the m_fDropDownClickedClose
			// variable set to true until the user releases the mouse button.
			m_fDropDownClickedClose = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the mouse leaves the control, redraw it
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			UpdateOnMouseAction();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a mouse event occurs (click, up, move) update the state of the button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateOnMouseAction()
		{
			DropDownButtonState newButtonState;
			if (MouseOnButton)
			{
				if ((MouseButtons & MouseButtons.Left) != 0 && m_dropDown == null && !m_fDropDownClickedClose)
					newButtonState = DropDownButtonState.Pressed;
				else
					newButtonState = DropDownButtonState.Hot;
			}
			else
				newButtonState = DropDownButtonState.Normal;

			if (newButtonState != m_buttonState)
			{
				m_buttonState = newButtonState;
				// for some reason the button doesn't redraw immediately if we call Invalidate,
				// so we redraw it now.
				using (Graphics graphics = CreateGraphics())
				{
					DrawDropDownButton(graphics);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the drop down button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawDropDownButton(Graphics graphics)
		{
			VisualStyleElement element = VisualStyleElement.ComboBox.DropDownButton.Disabled;
			ButtonState state = ButtonState.Normal;
			switch (m_buttonState)
			{
				case DropDownButtonState.Pressed:
					element = VisualStyleElement.ComboBox.DropDownButton.Pressed;
					state = ButtonState.Pushed;
					break;

				case DropDownButtonState.Hot:
					element = VisualStyleElement.ComboBox.DropDownButton.Hot;
					state = ButtonState.Normal;
					break;

				case DropDownButtonState.Normal:
					element = VisualStyleElement.ComboBox.DropDownButton.Normal;
					state = ButtonState.Normal;
					break;
			}

			if (!Application.RenderWithVisualStyles)
			{
				// Strange, but we have to expand the rectangle a little bit to convince
				// Microsoft's drawing routine to draw an arrow that is the right size;
				// otherwise, the arrows are smaller than for regular comboboxes.
				Rectangle rect = new Rectangle(m_buttonRect.X - 1, m_buttonRect.Y - 1, m_buttonRect.Width + 2, m_buttonRect.Height + 2);
				ControlPaint.DrawComboButton(graphics, rect, state);
			}
			else
			{
				VisualStyleRenderer renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(graphics, m_buttonRect);
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Paint"></see> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"></see> that
		/// contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
		{
			base.OnPaint(e);

			if (!Application.RenderWithVisualStyles)
				e.Graphics.FillRectangle(SystemBrushes.Window, ClientRectangle);
			else
			{
				VisualStyleElement element = VisualStyleElement.TextBox.TextEdit.Normal;
				VisualStyleRenderer renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(e.Graphics, ClientRectangle);
			}

			// Draw the color box (doesn't actually draw anything if the Empty color is picked)
			Rectangle boxRect = new Rectangle(2, 2,
				(m_currentColor == Color.Empty) ? 0 : Height - 4, Height - 4);
			e.Graphics.FillRectangle(new SolidBrush(m_currentColor), boxRect);
			DrawRectangle(e.Graphics, boxRect, SystemPens.WindowText, 1);

			// Draw the text (color name or Unspecified)
			float ht = Font.GetHeight(e.Graphics);
			e.Graphics.DrawString(m_colorName, Font, new SolidBrush(ForeColor),
				boxRect.Right + 1, (Height / 2) - (ht / 2));

			if (!Application.RenderWithVisualStyles)
				ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle, Border3DStyle.Sunken);

			// Draw the drop down button
			DrawDropDownButton(e.Graphics);

			if (ContainsFocus)
			{
				ControlPaint.DrawFocusRectangle(e.Graphics,
					new Rectangle(boxRect.Right + 1, boxRect.Top,
					Width - (boxRect.Width + m_buttonRect.Width + 8), boxRect.Height));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw a rectangle. The Graphics.DrawRect method does not draw at the exact location
		/// so this method fixes that.
		/// </summary>
		/// <param name="g">Graphics object to use</param>
		/// <param name="drawRect">rect to draw</param>
		/// <param name="pen">The pen to draw with</param>
		/// <param name="width">The width of the rectangle</param>
		/// ------------------------------------------------------------------------------------
		private void DrawRectangle(Graphics g, Rectangle drawRect, Pen pen, int width)
		{
			while (width-- > 0)
			{
				g.DrawRectangle(pen, drawRect.X, drawRect.Y, drawRect.Width - 1, drawRect.Height - 1);
				drawRect.Inflate(-1, -1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.KeyDown"></see> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.KeyEventArgs"></see> that
		/// contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Down && (e.Modifiers & Keys.Alt) > 0)
			{
				// Show the drop-down
				e.Handled = true;
				ShowDropDown();
				return;
			}
			base.OnKeyDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This event occurs when the color picker drop down has chosen a color.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void dropDown_ColorPicked(object sender, EventArgs e)
		{
			ColorPickerDropDown dropDown = sender as ColorPickerDropDown;
			Debug.Assert(dropDown != null);

			ColorValue = dropDown.CurrentColor;
			Focus();
			Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Closed event of the m_dropDown control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.ToolStripDropDownClosedEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void dropDown_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			// we have to set a variable so that we know that user clicked on drop down button
			// or in text box to close the drop down.
			m_fDropDownClickedClose = ClientRectangle.Contains(PointToClient(MousePosition));
			m_dropDown = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the PreviewKeyDown event of the m_dropDown control. If the user presses
		/// Alt-ArrowDown we should close the drop down.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.PreviewKeyDownEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_dropDown_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Down && (e.Modifiers & Keys.Alt) > 0)
			{
				m_dropDown.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating if the mouse cursor is on the button.
		/// </summary>
		/// <value><c>true</c> if mouse is on button otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		private bool MouseOnButton
		{
			get
			{
				return m_buttonRect.Contains(PointToClient(MousePosition));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the drop down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ShowDropDown()
		{
			m_dropDown = new ColorPickerDropDown(m_fShowUnspecified, m_currentColor);
			m_dropDown.ColorPicked += new EventHandler(dropDown_ColorPicked);
			m_dropDown.Closed += new ToolStripDropDownClosedEventHandler(dropDown_Closed);
			m_dropDown.PreviewKeyDown += new PreviewKeyDownEventHandler(m_dropDown_PreviewKeyDown);
			m_dropDown.Show(this, new Point(0, Height));
		}
	}
}
