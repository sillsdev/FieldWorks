// Copyright (c) 2002-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.LCModel.Core.Text;
using SIL.PlatformUtilities;
using SIL.Windows.Forms;

namespace LanguageExplorer.Controls.Styles
{
	/// <summary />
	public class FwColorCombo : UserControl
	{
		/// <summary>This event occurs when the color has been picked</summary>
		public event EventHandler ColorPicked;

		private string m_colorName = Color.Black.Name;
		private Rectangle m_buttonRect;
		private DropDownButtonState m_buttonState = DropDownButtonState.Normal;
		private ColorPickerDropDown m_dropDown;
		/// <summary>True if user clicked anywhere in combo box to close drop down</summary>
		private bool m_fDropDownClickedClose;
		private Color m_currentColor = Color.Empty;
		private bool _showUnspecified;

		/// <summary />
		public bool IsInherited { get; set; }

		/// <summary>
		/// Property which indicates if the Unspecified button should be available.
		/// </summary>
		public bool ShowUnspecified
		{
			get { return _showUnspecified; }
			set { IsInherited = _showUnspecified = value; }
		}

		/// <summary>State values for the drop down button</summary>
		private enum DropDownButtonState
		{
			Hot,
			Pressed,
			Normal
		}

		/// <summary />
		public FwColorCombo()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			SetStyle(ControlStyles.Selectable, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
			}
			m_colorName = null;
			m_dropDown = null;

			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
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

		/// <summary>
		/// Gets or sets the color value.
		/// </summary>
		public Color ColorValue
		{
			get
			{
				return m_currentColor;
			}
			set
			{
				m_currentColor = value;
				m_colorName = ColorPickerMatrix.ColorToName(value);
				Invalidate();

				// notify any subscriber of the color change
				ColorPicked?.Invoke(this, EventArgs.Empty);
			}
		}

		#endregion

		/// <inheritdoc />
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			// make space for the button rectangle
			var buttonWidth = 16;
			var buttonMargin = 1;
			if (!Application.RenderWithVisualStyles)
			{
				buttonMargin = 2;
				buttonWidth = 14;
			}
			m_buttonRect = new Rectangle(ClientRectangle.Width - (buttonWidth + buttonMargin), ClientRectangle.Y + buttonMargin, buttonWidth, ClientRectangle.Height - (buttonMargin * 2));
		}

		/// <inheritdoc />
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			Invalidate();
		}

		/// <inheritdoc />
		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			Invalidate();
		}

		#region Mouse events

		/// <inheritdoc />
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			UpdateOnMouseAction();
		}

		/// <inheritdoc />
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			UpdateOnMouseAction();

			// Drop down if user clicks anywhere in the combo box
			if (!m_fDropDownClickedClose && ClientRectangle.Contains(e.Location) && (e.Button & MouseButtons.Left) > 0)
			{
				ShowDropDown();
			}
		}

		/// <inheritdoc />
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			UpdateOnMouseAction();

			// If the user closes the drop down by clicking on the button again we don't want
			// to show the button pressed. Therefore we have to leave the m_fDropDownClickedClose
			// variable set to true until the user releases the mouse button.
			m_fDropDownClickedClose = false;
		}

		/// <inheritdoc />
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			UpdateOnMouseAction();
		}

		/// <summary />
		private void UpdateOnMouseAction()
		{
			var newButtonState = MouseOnButton
				? (MouseButtons & MouseButtons.Left) != 0 && m_dropDown == null
					&& !m_fDropDownClickedClose ? DropDownButtonState.Pressed : DropDownButtonState.Hot : DropDownButtonState.Normal;

			if (newButtonState != m_buttonState)
			{
				m_buttonState = newButtonState;
				// for some reason the button doesn't redraw immediately if we call Invalidate,
				// so we redraw it now.
				using (var graphics = CreateGraphics())
				{
					DrawDropDownButton(graphics);
				}
			}
		}

		/// <summary>
		/// Draws the drop down button.
		/// </summary>
		private void DrawDropDownButton(Graphics graphics)
		{
			var element = VisualStyleElement.ComboBox.DropDownButton.Disabled;
			var state = ButtonState.Normal;
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
				var rect = new Rectangle(m_buttonRect.X - 1, m_buttonRect.Y - 1, m_buttonRect.Width + 2, m_buttonRect.Height + 2);
				ControlPaint.DrawComboButton(graphics, rect, state);
			}
			else
			{
				var renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(graphics, m_buttonRect);
			}
		}
		#endregion

		/// <inheritdoc />
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (!Application.RenderWithVisualStyles)
			{
				e.Graphics.FillRectangle(SystemBrushes.Window, ClientRectangle);
			}
			else
			{
				var element = VisualStyleElement.TextBox.TextEdit.Normal;
				var renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(e.Graphics, ClientRectangle);
			}

			// Draw the color box (doesn't actually draw anything if the Empty color is picked)
			var boxRect = new Rectangle(2, 2, m_currentColor == Color.Empty ? 0 : Height - 4, Height - 4);
			e.Graphics.FillRectangle(new SolidBrush(m_currentColor), boxRect);
			DrawRectangle(e.Graphics, boxRect, SystemPens.WindowText, 1);

			// Draw the text (color name or Unspecified)
			var ht = Font.GetHeight(e.Graphics);
			e.Graphics.DrawString(m_colorName, Font, new SolidBrush(ForeColor), boxRect.Right + 1, Height / 2 - ht / 2);

			if (!Application.RenderWithVisualStyles)
			{
				ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle, Border3DStyle.Sunken);
			}
			// Draw the drop down button
			DrawDropDownButton(e.Graphics);

			if (ContainsFocus)
			{
				ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(boxRect.Right + 1, boxRect.Top, Width - (boxRect.Width + m_buttonRect.Width + 8), boxRect.Height));
			}
		}

		/// <summary>
		/// Draw a rectangle. The Graphics.DrawRect method does not draw at the exact location
		/// so this method fixes that.
		/// </summary>
		/// <param name="g">Graphics object to use</param>
		/// <param name="drawRect">rect to draw</param>
		/// <param name="pen">The pen to draw with</param>
		/// <param name="width">The width of the rectangle</param>
		private static void DrawRectangle(Graphics g, Rectangle drawRect, Pen pen, int width)
		{
			while (width-- > 0)
			{
				g.DrawRectangle(pen, drawRect.X, drawRect.Y, drawRect.Width - 1, drawRect.Height - 1);
				drawRect.Inflate(-1, -1);
			}
		}

		/// <inheritdoc />
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

		/// <summary>
		/// This event occurs when the color picker drop down has chosen a color.
		/// </summary>
		private void dropDown_ColorPicked(object sender, EventArgs e)
		{
			var dropDown = sender as ColorPickerDropDown;
			Debug.Assert(dropDown != null);
			ColorValue = dropDown.CurrentColor;

			Focus();
			Refresh();
		}

		/// <summary>
		/// Handles the Closed event of the m_dropDown control.
		/// </summary>
		private void dropDown_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			// we have to set a variable so that we know that user clicked on drop down button
			// or in text box to close the drop down.
			m_fDropDownClickedClose = ClientRectangle.Contains(PointToClient(MousePosition));
			m_dropDown = null;
		}

		/// <summary>
		/// Handles the PreviewKeyDown event of the m_dropDown control. If the user presses
		/// Alt-ArrowDown we should close the drop down.
		/// </summary>
		private void m_dropDown_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Down && (e.Modifiers & Keys.Alt) > 0)
			{
				m_dropDown.Close();
			}
		}

		/// <summary>
		/// Gets a value indicating if the mouse cursor is on the button.
		/// </summary>
		private bool MouseOnButton => m_buttonRect.Contains(PointToClient(MousePosition));

		/// <summary>
		/// Shows the drop down.
		/// </summary>
		private void ShowDropDown()
		{
			m_dropDown = new ColorPickerDropDown(ShowUnspecified, m_currentColor);
			m_dropDown.ColorPicked += dropDown_ColorPicked;
			m_dropDown.Closed += dropDown_Closed;
			m_dropDown.PreviewKeyDown += m_dropDown_PreviewKeyDown;
			m_dropDown.Show(this, new Point(0, Height));
		}

		/// <summary />
		private sealed class ColorPickerDropDown : ToolStripDropDown
		{
			/// <summary />
			internal event EventHandler ColorPicked;

			private ToolStripButton m_autoItem;
			private ToolStripMenuItem m_moreItem;
			private ColorPickerMatrix m_colorMatrix;
			private Color m_currColor;

			/// <summary>
			/// Encapsulates a color picker drop-down almost just like Word 2003's.
			/// </summary>
			/// <param name="fShowUnspecified">if set to <c>true</c> control will include a button
			/// for the "automatic" choice (i.e., not explicitly specified).</param>
			/// <param name="selectedColor">Initial color to select.</param>
			internal ColorPickerDropDown(bool fShowUnspecified, Color selectedColor)
			{
				LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;

				if (fShowUnspecified)
				{
					// Add the "Automatic" button.
					m_autoItem = new ToolStripButton(ColorPickerStrings.kstidUnspecifiedText)
					{
						TextAlign = System.Drawing.ContentAlignment.MiddleCenter
					};
					m_autoItem.Margin = new Padding(1, m_autoItem.Margin.Top, m_autoItem.Margin.Right, m_autoItem.Margin.Bottom);
					m_autoItem.Click += m_autoItem_Click;

					Items.Add(m_autoItem);
				}

				// Add all the colored squares.
				m_colorMatrix = new ColorPickerMatrix();
				m_colorMatrix.ColorPicked += m_colorMatrix_ColorPicked;
				var host = new ToolStripControlHost(m_colorMatrix)
				{
					AutoSize = false,
					Size = new Size(m_colorMatrix.Width + 6, m_colorMatrix.Height + 6),
					Padding = new Padding(3)
				};
				Items.Add(host);

				// Add the "More Colors..." button.
				m_moreItem = new ToolStripMenuItem(ColorPickerStrings.kstidMoreColors)
				{
					TextAlign = System.Drawing.ContentAlignment.MiddleCenter
				};
				m_moreItem.Click += m_moreItem_Click;
				Items.Add(m_moreItem);

				CurrentColor = selectedColor;
			}

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
				base.Dispose(disposing);
			}

			/// <summary>
			/// Gets or sets the drop-down's current color. Color.Empty is equivalent to the
			/// automatic value.
			/// </summary>
			internal Color CurrentColor
			{
				get
				{
					return m_currColor;
				}
				private set
				{
					m_currColor = value;
					m_colorMatrix.CurrentColor = value;
					if (m_autoItem != null)
					{
						m_autoItem.Checked = (value == Color.Empty);
					}
				}
			}

			/// <summary>
			/// Handle a color change from clicking on one of the colored squares.
			/// </summary>
			private void m_colorMatrix_ColorPicked(object sender, EventArgs e)
			{
				m_currColor = m_colorMatrix.CurrentColor;

				Hide();

				ColorPicked?.Invoke(this, EventArgs.Empty);
			}

			/// <summary>
			/// Show the color dialog.
			/// </summary>
			private void m_moreItem_Click(object sender, EventArgs e)
			{
				Hide();

				using (var dlg = new ColorDialog())
				{
					dlg.FullOpen = true;

					if (dlg.ShowDialog() == DialogResult.OK)
					{
						CurrentColor = dlg.Color;
						ColorPicked?.Invoke(this, EventArgs.Empty);
					}
				}
			}

			/// <summary />
			private void m_autoItem_Click(object sender, EventArgs e)
			{
				CurrentColor = Color.Empty;

				Hide();

				ColorPicked?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary />
		private sealed class ColorPickerMatrix : UserControl
		{
			/// <summary />
			internal event EventHandler ColorPicked;

			private const int kColorSquareSize = 18;
			private const int kNumberOfCols = 8;
			private Dictionary<XButton, Color> m_clrButtons = new Dictionary<XButton, Color>();
			private Color m_currColor = Color.Empty;

			/// <summary />
			internal ColorPickerMatrix()
			{
				InitializeComponent();

				DoubleBuffered = true;

				m_toolTip = new ToolTip();

				var row = 0;
				var col = 0;

				for (var i = 0; i < ColorUtil.kNumberOfColors; i++)
				{
					// Get the entry from the resources that has the color name and RGB value.
					var color = ColorUtil.ColorAtIndex(i);
					if (color == Color.Empty)
					{
						continue;
					}

					var btn = new XButton
					{
						CanBeChecked = true,
						DrawEmpty = true,
						Size = new Size(kColorSquareSize, kColorSquareSize),
						BackColor = BackColor,
						Location = new Point(col * kColorSquareSize, row * kColorSquareSize)
					};
					btn.Paint += btn_Paint;
					btn.Click += btn_Click;
					Controls.Add(btn);

					// Store the name in the tooltip and create a color from the RGB values.
					m_toolTip.SetToolTip(btn, ColorUtil.ColorNameAtIndex(i));
					m_clrButtons[btn] = color;

					col++;
					if (col == kNumberOfCols)
					{
						col = 0;
						row++;
					}
				}
			}

			/// <summary>
			/// Returns the name of a color
			/// </summary>
			internal static string ColorToName(Color color)
			{
				return color == Color.Empty ? ColorPickerStrings.kstidUnspecifiedSettingText : ColorUtil.ColorToName(color);
			}

			/// <summary>
			/// Gets or sets the control's current color.
			/// </summary>
			internal Color CurrentColor
			{
				get
				{
					return m_currColor;
				}
				set
				{
					m_currColor = value;

					foreach (var square in m_clrButtons)
					{
						square.Key.Checked = (value == square.Value);
					}
				}
			}

			/// <summary>
			/// Gets the name of the current color.
			/// </summary>
			private string CurrentColorName { get; set; }

			/// <summary />
			private void btn_Click(object sender, EventArgs e)
			{
				var btn = sender as XButton;
				if (btn != null && m_clrButtons.ContainsKey(btn) && m_clrButtons[btn] != m_currColor)
				{
					CurrentColorName = m_toolTip.GetToolTip(btn);
					CurrentColor = m_clrButtons[btn];
					ColorPicked?.Invoke(this, EventArgs.Empty);
				}
			}

			/// <summary />
			private void btn_Paint(object sender, PaintEventArgs e)
			{
				var btn = sender as XButton;
				if (btn == null)
				{
					return;
				}

				using (var br = new SolidBrush(btn.BackColor))
				{
					var rc = btn.ClientRectangle;
					e.Graphics.FillRectangle(br, rc);

					br.Color = Color.Gray;
					rc.Inflate(-3, -3);
					e.Graphics.FillRectangle(br, rc);

					br.Color = m_clrButtons[btn];
					rc.Inflate(-1, -1);
					e.Graphics.FillRectangle(br, rc);
				}
			}
			/// <summary>
			/// Required designer variable.
			/// </summary>
			private System.ComponentModel.IContainer components;

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (disposing)
				{
					components?.Dispose();
				}
				base.Dispose(disposing);
			}

			#region Component Designer generated code

			/// <summary>
			/// Required method for Designer support - do not modify
			/// the contents of this method with the code editor.
			/// </summary>
			private void InitializeComponent()
			{
				components = new System.ComponentModel.Container();
				var resources = new System.ComponentModel.ComponentResourceManager(typeof(ColorPickerMatrix));
				m_toolTip = new ToolTip(this.components);
				SuspendLayout();
				//
				// ColorPickerMatrix
				//
				resources.ApplyResources(this, "$this");
				AutoScaleMode = AutoScaleMode.Font;
				BackColor = Color.Transparent;
				Name = "ColorPickerMatrix";
				ResumeLayout(false);
			}

			#endregion

			private ToolTip m_toolTip;

			/// <summary />
			private sealed class XButton : Label
			{
				private bool m_drawLeftArrowButton;
				private bool m_drawRightArrowButton;
				private bool m_checked;
				private bool m_mouseDown;
				private bool m_mouseOver;
				private PaintState m_state = PaintState.Normal;
				private Image m_disabledImage;
				private TextFormatFlags m_txtFmtflags = TextFormatFlags.NoPadding | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;

				/// <summary />
				internal XButton()
				{
					AutoSize = false;
					BackColor = SystemColors.Control;
					// Linux doesn't have the Marlett font -- do we need to pick a special one to get
					// Unicode dingbats?  (In practice, the only use of XButton in FieldWorks is in the
					// color picker, and it never uses a path that tries to draw the dingbats.)
					var fontName = Platform.IsWindows ? "Marlett" : "OpenSymbol";
					Font = new Font(fontName, 9, GraphicsUnit.Point);
					Size = new Size(16, 16);
				}

				/// <summary/>
				protected override void Dispose(bool disposing)
				{
					System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
					base.Dispose(disposing);
				}

				/// <summary>
				/// Gets or sets a value indicating whether or not the button's checked state changes
				/// when clicked.
				/// </summary>
				internal bool CanBeChecked { get; set; }

				/// <summary>
				/// Gets or sets a value indicating whether or not the button is checked.
				/// </summary>
				internal bool Checked
				{
					private get
					{
						return m_checked;
					}
					set
					{
						if (CanBeChecked)
						{
							m_checked = value;
							Invalidate();
						}
					}
				}

				/// <summary>
				/// Gets or sets a value indicating whether or not an X is drawn on the button when no
				/// image, text or arrow direction is specified. By default, when no image, text or
				/// arrow direction is specified, the button is drawn with an X (like a close window-
				/// type of X). However, when DrawEmtpy is true, nothing will be drawn except the
				/// highlighted look given when the mouse is over or down or when the button is checked.
				/// </summary>
				internal bool DrawEmpty { private get; set; }

				/// <summary />
				private new Image Image
				{
					get
					{
						return base.Image;
					}
				}

				/// <inheritdoc />
				protected override void OnSystemColorsChanged(EventArgs e)
				{
					base.OnSystemColorsChanged(e);
					if (Image != null)
					{
						BackColor = Color.Transparent;
					}
				}

				/// <inheritdoc />
				protected override void OnMouseLeave(EventArgs e)
				{
					base.OnMouseLeave(e);
					m_mouseOver = false;
					Invalidate();
				}

				/// <inheritdoc />
				protected override void OnMouseDown(MouseEventArgs e)
				{
					base.OnMouseDown(e);

					if (e.Button == MouseButtons.Left)
					{
						m_mouseDown = true;
						Invalidate();
					}
				}

				/// <inheritdoc />
				protected override void OnMouseUp(MouseEventArgs e)
				{
					base.OnMouseUp(e);

					if (e.Button == MouseButtons.Left)
					{
						m_mouseDown = false;
						Invalidate();
					}
				}

				/// <inheritdoc />
				protected override void OnMouseMove(MouseEventArgs e)
				{
					base.OnMouseMove(e);
					m_mouseOver = ClientRectangle.Contains(e.Location);
					var newState = (m_mouseOver ? PaintState.Hot : PaintState.Normal);

					if (m_mouseOver && m_mouseDown)
					{
						newState = PaintState.HotDown;
					}
					if (newState != m_state)
					{
						m_state = newState;
						Invalidate();
					}
				}

				/// <inheritdoc />
				protected override void OnPaintBackground(PaintEventArgs e)
				{
					base.OnPaintBackground(e);

					if (m_mouseOver || Checked)
					{
						m_state = (m_mouseDown ? PaintState.HotDown : PaintState.Hot);
					}
					else
					{
						m_state = PaintState.Normal;
					}
					var rc = ClientRectangle;

					using (var br = new SolidBrush(BackColor))
					{
						e.Graphics.FillRectangle(br, rc);
					}
					if (m_state != PaintState.Normal)
					{
						PaintingHelper.DrawHotBackground(e.Graphics, rc, m_state);
					}
				}

				/// <inheritdoc />
				protected override void OnPaint(PaintEventArgs e)
				{
					if (Image != null)
					{
						DrawWithImage(e);
					}
					else if (m_drawLeftArrowButton || m_drawRightArrowButton)
					{
						DrawArrow(e);
					}
					else if (!string.IsNullOrEmpty(Text))
					{
						DrawText(e);
					}
					else if (!DrawEmpty)
					{
						DrawWithX(e);
					}
					else
					{
						base.OnPaint(e);
					}
				}

				/// <summary>
				/// Draws the button's text.
				/// </summary>
				private void DrawText(PaintEventArgs e)
				{
					var flags = TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
					var clr = (Enabled ? ForeColor : SystemColors.GrayText);
					TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, clr, flags);
				}

				/// <summary>
				/// Draw the button with text.
				/// </summary>
				private void DrawWithX(PaintEventArgs e)
				{
					var rc = ClientRectangle;
					var clr = (m_state == PaintState.Normal ? SystemColors.ControlDarkDark : SystemColors.ControlText);

					// Linux doesn't have the Marlett font, so use a standard Unicode dingbat here.
					var glyph = Platform.IsWindows ? "r" : "\u2573";
					TextRenderer.DrawText(e.Graphics, glyph, Font, rc, clr, m_txtFmtflags);

					// Draw the border around the button.
					rc.Width--;
					rc.Height--;
					using (var pen = new Pen(clr))
					{
						e.Graphics.DrawRectangle(pen, rc);
					}
				}

				/// <summary>
				/// Draws the button with an image.
				/// </summary>
				private void DrawWithImage(PaintEventArgs e)
				{
					var img = (Enabled ? Image : m_disabledImage);
					if (img != null)
					{
						var x = (Width - img.Width) / 2;
						var y = (Height - img.Height) / 2;
						var rc = new Rectangle(x, y, img.Width, img.Height);
						e.Graphics.DrawImageUnscaledAndClipped(img, rc);
					}
				}

				/// <summary>
				/// Draws the button with an image.
				/// </summary>
				private void DrawArrow(PaintEventArgs e)
				{
					var rc = ClientRectangle;

					// If possible, render the button with visual styles. Otherwise,
					// paint the plain Windows 2000 push button.
					var element = GetCorrectVisualStyleArrowElement();
					if (PaintingHelper.CanPaintVisualStyle(element))
					{
						var renderer = new VisualStyleRenderer(element);
						renderer.DrawParentBackground(e.Graphics, rc, this);
						renderer.DrawBackground(e.Graphics, rc);
						return;
					}

					if (Font.SizeInPoints != 12)
					{
						Font = new Font(Font.FontFamily, 12, GraphicsUnit.Point);
					}
					ControlPaint.DrawButton(e.Graphics, rc, (m_state == PaintState.HotDown ? ButtonState.Pushed : ButtonState.Normal));
					var arrowGlyph = Platform.IsWindows
						// In the Marlett font, '3' is the left arrow and '4' is the right.
						? (m_drawLeftArrowButton ? "3" : "4")
						// Linux doesn't have the Marlett font, so use standard Unicode dingbats here.
						: (m_drawLeftArrowButton ? "\u25C4" : "\u25BA");

					var clr = (Enabled ? SystemColors.ControlText : SystemColors.GrayText);
					TextRenderer.DrawText(e.Graphics, arrowGlyph, Font, rc, clr, m_txtFmtflags);
				}

				/// <summary>
				/// Gets the correct visual style arrow button and in the correct state.
				/// </summary>
				private VisualStyleElement GetCorrectVisualStyleArrowElement()
				{
					if (m_drawLeftArrowButton)
					{
						if (!Enabled)
						{
							return VisualStyleElement.Spin.DownHorizontal.Disabled;
						}
						if (m_state == PaintState.Normal)
						{
							return VisualStyleElement.Spin.DownHorizontal.Normal;
						}
						return (m_state == PaintState.Hot ? VisualStyleElement.Spin.DownHorizontal.Hot : VisualStyleElement.Spin.DownHorizontal.Pressed);
					}

					if (!Enabled)
					{
						return VisualStyleElement.Spin.UpHorizontal.Disabled;
					}
					if (m_state == PaintState.Normal)
					{
						return VisualStyleElement.Spin.UpHorizontal.Normal;
					}
					return (m_state == PaintState.Hot ? VisualStyleElement.Spin.UpHorizontal.Hot : VisualStyleElement.Spin.UpHorizontal.Pressed);
				}
			}
		}
	}
}