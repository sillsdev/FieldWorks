// Copyright (c) 2002-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls.Design;

namespace SIL.FieldWorks.Common.Controls
{
	/// ENHANCE EberhardB: Show Tooltip if text is to long to fit in button
	/// <summary />
	[Serializable]
	[Designer(typeof(FwButtonDesigner))]
	public sealed class FwButton : Button
	{
		/// <summary>
		/// States that the button can have
		/// </summary>
		private enum ButtonStates
		{
			/// <summary>Button is flat</summary>
			Flat,
			/// <summary>Button is raised</summary>
			Up,
			/// <summary>Button is pressed</summary>
			Down
		}

		/// <summary />
		private bool m_fMouseButtonDown;
		/// <summary />
		private bool m_fTextInButton = true;
		/// <summary />
		private bool m_fButtonFillsControl = true;
		/// <summary />
		private int m_dpBorderWidth = 1;
		/// <summary />
		private Size m_ButtonSize;
		/// <summary />
		private Rectangle m_rect;
		/// <summary />
		private ButtonStates m_PaintState = ButtonStates.Flat;
		/// <summary />
		private SunkenAppearances m_SunkenAppearance = SunkenAppearances.Shallow;
		/// <summary />
		private ButtonStyles m_Style = ButtonStyles.Popup;
		/// <summary>
		/// This flag is only relevant when the button is a toggle button (i.e.
		/// m_fButtonToggles = true). When this flag is true, it causes the button
		/// to act like it's part of a group of mutually exclusive buttons in that
		/// when the button has already been pushed-in by clicking, clicking on it
		/// again does not allow the button's state to be changed to unpushed.
		/// </summary>
		private bool m_behaveLikeOptionButton = false;
		/// <summary />
		private BorderDrawing m_BorderDrawingObj = new BorderDrawing();

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		/// <summary />
		public FwButton()
		{
			SetStyle(ControlStyles.ResizeRedraw, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initially Button fills all available space
			m_ButtonSize = Size;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run more than once.
				return;
			}

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
		}

		#endregion

		#region Additional methods
		/// <summary>
		/// Press or release the button. If the button does not toggle, this method does nothing.
		/// </summary>
		/// <param name="fPressed">True if button should be shown pressed.</param>
		public void PressButton(bool fPressed)
		{
			if (ButtonToggles && Pressed != fPressed)
			{
				Pressed = fPressed;
				if (Pressed)
				{
					m_PaintState = ButtonStates.Down;
				}
				else if (ButtonStyle == ButtonStyles.Popup)
				{
					m_PaintState = ButtonStates.Flat;
				}
				else
				{
					m_PaintState = ButtonStates.Up;
				}

				Invalidate();
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Sets or returns the button's 3D border style.
		/// </summary>
		/// <remarks>
		/// The valid button styles are: FwControls.ButtonStyles.Popup and
		/// FwControls.ButtonStyles.Standard. The popup style will show a raised single border when
		/// the mouse is over it and a sunken single border when pressed. The standard style will
		/// give the button a 3D beveled border, 2 pixels wide.
		/// </remarks>
		[DefaultValue(ButtonStyles.Popup)]
		[Category("Appearance")]
		[Description("Determines the button's 3D style. The popup style will show a raised " +
			"single border when the mouse is over it and a sunken single border when " +
			"pressed. The standard style will give the button a 3D beveled border, 2 pixels " +
			"wide. The raised style is equal to the standard style, except that the border " +
			"is only 1 pixel wide.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public ButtonStyles ButtonStyle
		{
			get
			{
				return m_Style;
			}
			set
			{
				m_Style = value;
				m_dpBorderWidth = (m_Style == ButtonStyles.Standard ? 2 : 1);
				Invalidate();
			}
		}

		/// <summary>
		/// Sets or retrieves the buttons alignment
		/// </summary>
		/// <remarks>
		/// Determines the alignment of the button if the button doesn't fill the whole control
		/// </remarks>
		[DefaultValue(ContentAlignment.TopCenter)]
		[Category("Appearance")]
		[Description("Determines the alignment of the button if the button doesn't fill the whole control.")]
		public ContentAlignment ButtonAlign { get; set; } = ContentAlignment.TopCenter;

		/// <summary>
		/// Sets or returns the button's appearance when pressed.
		/// </summary>
		/// <remarks>
		/// The valid sunken appearance values FwControls.SunkenApperances.Shallow and
		/// FwControls.SunkenAppearances.Deep. A shallow appearance will cause a single border to be
		/// painted around the button when it is pressed. The deep appearance will paint a border
		/// around the button that makes it look sunken.
		/// </remarks>
		[DefaultValue(SunkenAppearances.Shallow)]
		[Category("Appearance")]
		[Description("Determines how indented the button looks when pressed (ignored for popup button).")]
		public SunkenAppearances SunkenAppearance
		{
			get
			{
				return m_SunkenAppearance;
			}
			set
			{
				m_SunkenAppearance = value;
				Invalidate();
			}
		}

		/// <summary>
		/// Sets or Returns a boolean determining if the control's text will appear
		/// inside the button portion of the control.
		/// </summary>
		/// <remarks>
		/// If the ButtonFillsControl property is set to false, this property is ignored.
		/// </remarks>
		/// <seealso cref="ButtonFillsControl"></seealso>
		[DefaultValue(true)]
		[Category("Appearance")]
		[Description("Determines if the button's text will appear inside or outside the button area. If the ButtonHeight property is the full height of the control, this property is ignored.")]
		public bool TextInButton
		{
			get
			{
				return m_fTextInButton;
			}
			set
			{
				m_fTextInButton = value;
				Invalidate();
			}
		}

		/// <summary>
		/// Sets or returns a boolean determining if the button maintains its state
		/// when pressed.
		/// </summary>
		/// <remarks>
		/// If this property is set to true, a button will stay in the pushed-in state when the
		/// user presses the button and it isn't already in the pushed-in state. If the user
		/// presses the button when it's in the pushed-in state, it will stay in the unpushed
		/// state.
		/// </remarks>
		[DefaultValue(false)]
		[Category("Behavior")]
		[Description("Determines if the button will maintain its state when pressed.")]
		public bool ButtonToggles { get; set; }

		/// <summary>
		/// Gets a value indicating what toggle state the button is in.
		/// </summary>
		[Browsable(false)]
		public ButtonToggleStates ButtonToggleState => (ButtonToggles && Pressed ? ButtonToggleStates.Pushed : ButtonToggleStates.Unpushed);

		/// <summary>
		/// Sets or returns the height, in pixels, of the button portion of the control.
		/// </summary>
		/// <remarks>
		/// The button portion  of the control (i.e. what visibly changes when the user hovers
		/// over or clicks the control) may be shorter than the control itself. To make the button
		/// portion of the control shorter than the control, set this property to a value that is
		/// less than the control's height. Then set the ButtonFillsControl property to false.
		/// If the ButtonFillsControl property is set to true, this property is ignored.
		/// </remarks>
		/// <seealso cref="ButtonFillsControl"></seealso>
		[Category("Layout")]
		[Description("Determines the button's size. This property is ignored when the ButtonFillsControl property is true.")]
		public Size ButtonSize
		{
			get
			{
				return m_ButtonSize;
			}
			set
			{
				m_ButtonSize = value;
				Invalidate();
			}
		}

		/// <summary>
		/// Sets or returns a boolean determining if the button portion of the control
		/// fills the entire control.
		/// </summary>
		/// <remarks>
		/// The button portion  of the control (i.e. what visibly changes when the user hovers
		/// over or clicks the control) may be shorter than the control itself. Making the button
		/// portion shorter allows the button's text to appear below (outside) the button (See the
		/// TextInButton property). To make the button portion of the control shorter than the
		/// control, set this value to true and the ButtonHeight to a value less than the
		/// the control's height. If this property is set to true, both the ButtonHeight and
		/// TextInButton properties are ignored.
		/// </remarks>
		/// <seealso cref="TextInButton"/>
		[DefaultValue(true)]
		[Category("Layout")]
		[Description("Determines if the button fills the entire control.")]
		public bool ButtonFillsControl
		{
			get
			{
				return m_fButtonFillsControl;
			}
			set
			{
				m_fButtonFillsControl = value;
				Invalidate();
			}
		}

		/// <summary>
		/// sets or returns the position of the text relative to the button. This
		/// property is ignored if TextInButton is true.
		/// </summary>
		/// <remarks>
		/// The following values are possible: None - default position. Right - the text
		/// appears to the right of the button. Bottom - the text appears below the button.
		/// </remarks>
		[DefaultValue(TextLocation.Below)]
		[Category("Appearance")]
		[Description("Determines the position of the text relative to the button.")]
		public TextLocation TextPosition { get; set; } = TextLocation.Below;

		/// <summary>
		/// Gets or sets how the text should be trimmed if it is to long to fit
		/// into the rectangle
		/// </summary>
		[DefaultValue(StringTrimming.EllipsisCharacter)]
		[Category("Appearance")]
		[Description("Determines how the text should be trimmed if it is to long to fit on the button")]
		public StringTrimming TextTrimming { get; set; } = StringTrimming.EllipsisCharacter;

		/// <summary>
		/// Returns true if the buttons is pressed (locked down)
		/// </summary>
		[Browsable(false)]
		public bool Pressed { get; private set; }

		#endregion

		#region Overridden Methods

		/// <inheritdoc />
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (m_fButtonFillsControl)
			{
				m_ButtonSize = Size;
			}

			Invalidate();
		}

		/// <inheritdoc />
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.FillRectangle(new SolidBrush(BackColor), 0, 0, Width, Height);
			m_rect = new Rectangle(0, 0, m_fButtonFillsControl ? Width : m_ButtonSize.Width, m_fButtonFillsControl ? Height : m_ButtonSize.Height);

			if (!m_fButtonFillsControl)
			{
				if (TextPosition == TextLocation.Below)
				{
					switch (ButtonAlign)
					{
						case ContentAlignment.TopCenter:
						case ContentAlignment.MiddleCenter:
						case ContentAlignment.BottomCenter:
							// Adjust the alignment of the button.
							// We treat Top/Middle/Bottom the same
							m_rect.X = (Width - m_ButtonSize.Width) / 2;
							break;
						case ContentAlignment.TopRight:
						case ContentAlignment.MiddleRight:
						case ContentAlignment.BottomRight:
							m_rect.X = Width - m_ButtonSize.Width;
							break;
					}
				}
				else
				{
					switch (ButtonAlign)
					{
						case ContentAlignment.MiddleLeft:
						case ContentAlignment.MiddleCenter:
						case ContentAlignment.MiddleRight:
							// Adjust the alignment of the button.
							// We treat Top/Middle/Bottom the same
							m_rect.Y = (Height - m_ButtonSize.Height) / 2;
							break;
						case ContentAlignment.BottomLeft:
						case ContentAlignment.BottomCenter:
						case ContentAlignment.BottomRight:
							m_rect.Y = Height - m_ButtonSize.Height;
							break;
					}
				}
			}

			// If there's text, draw it in the control.
			if (Text != null)
			{
				PutTextOnButton(e.Graphics);
			}
			// If there's an image, draw it on button.
			if (Image != null)
			{
				PutImageOnButton(e.Graphics);
			}
			// If the button style is popup and the mouse isn't over the button then
			// leave without drawing any border.
			if (m_PaintState == ButtonStates.Flat && ButtonStyle == ButtonStyles.Popup)
			{
				return;
			}
			BorderTypes brdrType;

			// Determine what type of border to draw for the button.
			if (ButtonStyle == ButtonStyles.Popup)
			{
				brdrType = m_PaintState == ButtonStates.Up ? BorderTypes.SingleRaised : BorderTypes.SingleSunken;
			}
			else if (m_PaintState == ButtonStates.Up || m_PaintState == ButtonStates.Flat)
			{
				brdrType = ButtonStyle == ButtonStyles.Raised ? BorderTypes.SingleRaised : BorderTypes.DoubleRaised;
			}
			else
			{
				brdrType = SunkenAppearance == SunkenAppearances.Deep ? BorderTypes.DoubleSunken : SunkenAppearance == SunkenAppearances.Sunken ? BorderTypes.SingleSunken : BorderTypes.Single;
			}

			// Finally, draw the border.
			m_BorderDrawingObj.Draw(e.Graphics, m_rect, brdrType);
		}

		/// <inheritdoc />
		protected override void OnMouseEnter(EventArgs e)
		{
			m_PaintState = (Pressed || m_fMouseButtonDown ? ButtonStates.Down : ButtonStates.Up);
			Invalidate();

			base.OnMouseEnter(e);
		}

		/// <inheritdoc />
		protected override void OnMouseLeave(EventArgs e)
		{
			m_PaintState = (Pressed ? ButtonStates.Down : ButtonStates.Flat);
			Invalidate();

			base.OnMouseLeave(e);
		}

		/// <inheritdoc />
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				m_fMouseButtonDown = true;
				m_PaintState = ButtonStates.Down;
				Invalidate();
			}
			base.OnMouseDown(e);
		}

		/// <inheritdoc />
		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				m_fMouseButtonDown = false;

				if (!ButtonToggles)
				{
					Pressed = false;
					m_PaintState = ButtonStates.Flat;
					Invalidate();
				}
				else
				{
					// Only do the following block of code when the button's state is not already
					// pushed in or it's not part of a group of option buttons.
					if (!m_behaveLikeOptionButton || !Pressed)
					{
						// If the mouse pointer is still over the button when the user lifts
						// the mouse button, toggle the buttons state. Otherwise, ignore the
						// fact that the user ever pressed the mouse button in the first place.
						if (m_rect.Contains(e.X, e.Y))
						{
							PressButton(!Pressed);
						}
						else
						{
							m_PaintState = (Pressed ? ButtonStates.Down : ButtonStates.Flat);
							Invalidate();
						}
					}
				}
			}

			base.OnMouseUp(e);
		}

		/// <inheritdoc />
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (m_fMouseButtonDown && !Pressed)
			{
				var nTmpPaintState = ButtonStates.Flat;

				if (m_rect.Contains(e.X, e.Y))
				{
					nTmpPaintState = ButtonStates.Down;
				}
				if (nTmpPaintState != m_PaintState)
				{
					m_PaintState = nTmpPaintState;
					Invalidate();
				}
			}

			base.OnMouseMove(e);
		}

		#endregion

		#region Helper Functions

		private void PutTextOnButton(Graphics g)
		{
			using (var sf = new StringFormat())
			{
				// Take the text's content alignment value and convert it into the individual
				// alignment types for horizontal and vertical alignment.
				sf.Alignment = ConAlignToHorizStrAlign(TextAlign);
				sf.LineAlignment = ConAlignToVertStrAlign(TextAlign);
				sf.Trimming = TextTrimming;
				sf.FormatFlags = StringFormatFlags.LineLimit;

				// Create a rectangle in which to align text that accounts for the buttons borders.
				// Multiplying by 2 accounts the combined thickness of the top and bottom borders,
				// and the combined thickness of the left and right borders.
				var rc = new Rectangle(m_dpBorderWidth, m_dpBorderWidth, Width - m_dpBorderWidth * 2, Height - m_dpBorderWidth * 2);

				// If the text doesn't go on the button portion of the control and the button
				// portion of the control is actually shorter than the control, make the text's
				// rectangle the area below the button's rectangle.
				if (!m_fTextInButton)
				{
					if (TextPosition == TextLocation.Below && m_rect.Height < this.Height)
					{
						// Make the text's rectangle 2 pixels smaller so the text doesn't get
						// too close to the edges. ENHANCE: Should probably add a property
						// called TextMargin or Padding or something like that, instead of
						// hard coding the value to 2.
						rc.Height = Height - m_rect.Height - 2;
						rc.Y = m_rect.Height + 2;
					}
					else if (TextPosition == TextLocation.Right && m_rect.Width < this.Width)
					{
						// Make the text's rectangle 2 pixels smaller so the text doesn't get
						// too close to the edges. ENHANCE: Should probably add a property
						// called TextMargin or Padding or something like that, instead of
						// hard coding the value to 2.
						rc.Width = Width - m_rect.Width - 2;
						rc.X = m_rect.Width + 2;
					}
				}

				g.DrawString(Text, Font, new SolidBrush(ForeColor), rc, sf);
			}
		}

		/// <summary>
		/// If the control has an image specified, this function paints it on the button portion
		/// of the control.
		/// </summary>
		/// <param name="g">A Graphics type passed from the OnPaint method.</param>
		/// <seealso cref="OnPaint"/>
		private void PutImageOnButton(Graphics g)
		{
			var pt = ConAlignToImgPosition(ImageAlign, Image, m_rect, m_dpBorderWidth);
			g.DrawImageUnscaled(Image, pt);
		}

		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		private bool ShouldSerializeButtonSize()
		{
			return !(m_ButtonSize.Width == Width && m_ButtonSize.Height == Height);
		}

		/// <summary>
		/// Resets the button size to fill whole control
		/// </summary>
		private void ResetButtonSize()
		{
			m_ButtonSize.Width = Width;
			m_ButtonSize.Height = Height;
		}

		#endregion

		/// <summary>
		/// Determine horizontal alignment for text based on a content alignment value.
		/// </summary>
		private static StringAlignment ConAlignToHorizStrAlign(ContentAlignment align)
		{
			switch (align)
			{
				case ContentAlignment.BottomRight:
				case ContentAlignment.MiddleRight:
				case ContentAlignment.TopRight:
					return StringAlignment.Far;

				case ContentAlignment.TopLeft:
				case ContentAlignment.MiddleLeft:
				case ContentAlignment.BottomLeft:
					return StringAlignment.Near;

				default:
					return StringAlignment.Center;
			}
		}

		/// <summary>
		/// Determine vertical alignment for text based on a content alignment value.
		/// </summary>
		private static StringAlignment ConAlignToVertStrAlign(ContentAlignment align)
		{
			switch (align)
			{
				case ContentAlignment.TopLeft:
				case ContentAlignment.TopCenter:
				case ContentAlignment.TopRight:
					return StringAlignment.Near;

				case ContentAlignment.BottomLeft:
				case ContentAlignment.BottomCenter:
				case ContentAlignment.BottomRight:
					return StringAlignment.Far;

				default:
					return StringAlignment.Center;
			}
		}

		/// <summary>
		/// This function determines where in a rectangle an image should be drawn given a
		/// content alignment specification.
		/// </summary>
		/// <param name="align">The content alignment type where the image should be drawn.</param>
		/// <param name="img">The image object to be drawn.</param>
		/// <param name="rc">The rectangle in which the image is to be drawn.</param>
		/// <param name="nMargin">The number of pixels of margin between the image and whatever
		/// edge of the rectangle it may be near. When either the X and Y components of the point
		/// are calculated to center the image, this parameter is ignored for that calculation.
		/// For example, if the content alignment is TopCenter, the calculation for X will
		/// ignore the margin but the calculation for Y will include adjusting for it.</param>
		/// <returns>A point type specifying where, relative to the rectangle, the image should
		/// be drawn. </returns>
		private static Point ConAlignToImgPosition(ContentAlignment align, Image img, Rectangle rc, int nMargin)
		{
			var pt = new Point(rc.Left + nMargin, rc.Top + nMargin);

			switch (align)
			{
				// Determine the horizontal location for the image.
				case ContentAlignment.BottomCenter:
				case ContentAlignment.MiddleCenter:
				case ContentAlignment.TopCenter:
					pt.X = rc.Left + (rc.Width - img.Width) / 2;
					break;
				case ContentAlignment.BottomRight:
				case ContentAlignment.MiddleRight:
				case ContentAlignment.TopRight:
					pt.X = rc.Left + (rc.Width - img.Width) - nMargin;
					break;
			}

			switch (align)
			{
				// Determine the vertical location for the image.
				case ContentAlignment.MiddleLeft:
				case ContentAlignment.MiddleCenter:
				case ContentAlignment.MiddleRight:
					pt.Y = rc.Top + (rc.Height - img.Height) / 2;
					break;
				case ContentAlignment.BottomLeft:
				case ContentAlignment.BottomCenter:
				case ContentAlignment.BottomRight:
					pt.Y = rc.Top + (rc.Height - img.Height) - nMargin;
					break;
			}

			return pt;
		}
	}
}