// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: FwButton.cs
// Responsibility: DavidO
// Last reviewed:
//
// Implementation of FwButton
//
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using Fw = SIL.FieldWorks.Common;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	// EberhardB: Somehow it didn't work when I put the following enum's inside of FwButton:
	// When the properties were changed in design view in a hosting form, after recompiling
	// the properties changed back to their default values - don't know why. Having these
	// enum's declared here seems to solve this problem.

	/// <summary>
	/// Determines the style of the control.
	/// </summary>
	public enum ButtonStyles : int
	{
		/// <summary>Flat button, pops up when mouse is over it</summary>
		Popup = 0,
		/// <summary>Standard button, but with smaller border (only 1 pixel wide)</summary>
		Raised = 1,
		/// <summary>Standard button (3D beveled border, 2 pixels wide)</summary>
		Standard = 2,
	};

	/// <summary>
	/// Determines where the text is drawn if outside of the button
	/// </summary>
	public enum TextLocation: int
	{
		/// <summary>Text is drawn below the button/image</summary>
		Below = 0,
		/// <summary>Text is drawn to the right of the button/image</summary>
		Right = 1,
	};

	/// <summary>
	/// Determines the appearance when the button is pressed
	/// </summary>
	public enum SunkenAppearances
	{
		/// <summary>border is 1 pixel wide</summary>
		Shallow,
		/// <summary>border is 1 pixel wide, opposite of ButtonStyles.Raised</summary>
		Sunken,
		/// <summary>border is 2 pixels wide, thus looks deeper</summary>
		Deep,
	};

	/// <summary>
	/// Summary description for FwButton.
	/// </summary>
	/// ENHANCE EberhardB: Show Tooltip if text is to long to fit in button
	[Serializable]
	[Designer("SIL.FieldWorks.Common.Controls.Design.FwButtonDesigner")]
	public class FwButton : Button, IFWDisposable
	{
		/// <summary>
		/// States that the button can have
		/// </summary>
		protected enum ButtonStates
		{
			/// <summary>Button is flat</summary>
			Flat,
			/// <summary>Button is raised</summary>
			Up,
			/// <summary>Button is pressed</summary>
			Down
		}

		/// <summary>
		/// States of a button when button is a toggle button (i.e. ButtonToggles is true)
		/// </summary>
		public enum ButtonToggleStates
		{
			/// <summary></summary>
			Pushed,
			/// <summary></summary>
			Unpushed
		}

		/// <summary></summary>
		protected bool m_fButtonLockedDown = false;
		/// <summary></summary>
		protected bool m_fMouseButtonDown = false;
		/// <summary></summary>
		protected bool m_fButtonToggles = false;
		/// <summary></summary>
		protected bool m_fTextInButton = true;
		/// <summary></summary>
		protected bool m_fButtonFillsControl = true;
		/// <summary></summary>
		protected int m_dpBorderWidth = 1;
		/// <summary></summary>
		protected Size m_ButtonSize;
		/// <summary></summary>
		protected Rectangle m_rect;
		/// <summary></summary>
		protected ButtonStates m_PaintState = ButtonStates.Flat;
		/// <summary></summary>
		protected SunkenAppearances m_SunkenAppearance = SunkenAppearances.Shallow;
		/// <summary></summary>
		protected ButtonStyles m_Style = ButtonStyles.Popup;
		/// <summary></summary>
		protected ContentAlignment m_ButtonAlign = ContentAlignment.TopCenter;
		/// <summary></summary>
		protected TextLocation m_TextPosition = TextLocation.Below;
		/// <summary></summary>
		protected StringTrimming m_TextTrimming = StringTrimming.EllipsisCharacter;
		/// <summary>
		/// This flag is only relevant when the button is a toggle button (i.e.
		/// m_fButtonToggles = true). When this flag is true, it causes the button
		/// to act like it's part of a group of mutually exclusive buttons in that
		/// when the button has already been pushed-in by clicking, clicking on it
		/// again does not allow the button's state to be changed to unpushed.
		/// </summary>
		protected bool m_behaveLikeOptionButton = false;
		/// <summary></summary>
		protected Fw.Drawing.BorderDrawing m_BorderDrawingObj = new Fw.Drawing.BorderDrawing();

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		//*******************************************************************************************
		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		public FwButton()
		{
			SetStyle(ControlStyles.ResizeRedraw, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initially Button fills all available space
			m_ButtonSize = Size;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		//*******************************************************************************************
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		//*******************************************************************************************
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
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
		//*******************************************************************************************
		/// <summary>
		/// Press or release the button. If the button does not toggle, this method does nothing.
		/// </summary>
		/// <param name="fPressed">True if button should be shown pressed.</param>
		//*******************************************************************************************
		public void PressButton(bool fPressed)
		{
			CheckDisposed();

			if (m_fButtonToggles && m_fButtonLockedDown != fPressed)
			{
				m_fButtonLockedDown = fPressed;
				if (m_fButtonLockedDown)
					m_PaintState = ButtonStates.Down;
				else if (ButtonStyle == ButtonStyles.Popup)
					m_PaintState = ButtonStates.Flat;
				else
					m_PaintState = ButtonStates.Up;
				this.Invalidate();
			}
		}
		#endregion

		#region Properties
		//*******************************************************************************************
		/// <summary>
		/// Property - Sets or returns the button's 3D border style.
		/// </summary>
		/// <remarks>
		/// The valid button styles are: FwControls.ButtonStyles.Popup and
		/// FwControls.ButtonStyles.Standard. The popup style will show a raised single border when
		/// the mouse is over it and a sunken single border when pressed. The standard style will
		/// give the button a 3D beveled border, 2 pixels wide.
		/// </remarks>
		//*************************************************************************************
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
				CheckDisposed();

				return m_Style;
			}
			set
			{
				CheckDisposed();

				m_Style = value;
				m_dpBorderWidth = (m_Style == ButtonStyles.Standard ? 2 : 1);
				this.Invalidate();
			}
		}

		//*************************************************************************************
		/// <summary>
		/// Property - Sets or retrieves the buttons alignment
		/// </summary>
		/// <remarks>
		/// Determines the alignment of the button if the button doesn't fill the whole control
		/// </remarks>
		//*************************************************************************************
		[DefaultValue(ContentAlignment.TopCenter)]
		[Category("Appearance")]
		[Description("Determines the alignment of the button if the button doesn't fill the whole control.")]
		public ContentAlignment ButtonAlign
		{
			get
			{
				CheckDisposed();

				return m_ButtonAlign;
			}
			set
			{
				CheckDisposed();

				m_ButtonAlign = value;
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Sets or returns the button's appearance when pressed.
		/// </summary>
		/// <remarks>
		/// The valid sunken appearance values FwControls.SunkenApperances.Shallow and
		/// FwControls.SunkenApperances.Deep. A shallow appearance will cause a single border to be
		/// painted around the button when it is pressed. The deep appearance will paint a border
		/// around the button that makes it look sunken.
		/// </remarks>
		//*******************************************************************************************
		[DefaultValue(SunkenAppearances.Shallow)]
		[Category("Appearance")]
		[Description("Determines how indented the button looks when pressed (ignored for popup button).")]
		public SunkenAppearances SunkenAppearance
		{
			get
			{
				CheckDisposed();

				return m_SunkenAppearance;
			}
			set
			{
				CheckDisposed();

				m_SunkenAppearance = value;
				this.Invalidate();
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Property - Sets or Returns a boolean determining if the control's text will appear
		/// inside the button portion of the control.
		/// </summary>
		/// <remarks>
		/// If the ButtonFillsControl property is set to false, this property is ignored.
		/// </remarks>
		/// <seealso cref="ButtonFillsControl"></seealso>
		//*******************************************************************************************
		[DefaultValue(true)]
		[Category("Appearance")]
		[Description("Determines if the button's text will appear inside or outside the button area. If the ButtonHeight property is the full height of the control, this property is ignored.")]
		public bool TextInButton
		{
			get
			{
				CheckDisposed();

				return m_fTextInButton;
			}
			set
			{
				CheckDisposed();

				m_fTextInButton = value;
				this.Invalidate();
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Property - Sets or returns the Color used to paint the darkest portion of a 3D border.
		/// The default value is the Windows system color for the same thing.
		/// </summary>
		//*******************************************************************************************
		[Category("Appearance")]
		public Color BorderDarkestColor
		{
			get
			{
				CheckDisposed();

				return m_BorderDrawingObj.BorderDarkestColor;
			}
			set
			{
				CheckDisposed();

				m_BorderDrawingObj.BorderDarkestColor = value;
				this.Invalidate();
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Property - Sets or returns the Color used to paint the second to darkest portion of
		/// a 3D border. The default value is the Windows system color for the same thing.
		/// </summary>
		//*******************************************************************************************
		[Category("Appearance")]
		public Color BorderDarkColor
		{
			get
			{
				CheckDisposed();

				return m_BorderDrawingObj.BorderDarkColor;
			}
			set
			{
				CheckDisposed();

				m_BorderDrawingObj.BorderDarkColor = value;
				this.Invalidate();
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Property - Sets or returns the Color used to paint the lightest portion of a 3D
		/// border. The default value is the Windows system color for the same thing.
		/// </summary>
		//*******************************************************************************************
		[Category("Appearance")]
		public Color BorderLightestColor
		{
			get
			{
				CheckDisposed();

				return m_BorderDrawingObj.BorderLightestColor;
			}
			set
			{
				CheckDisposed();

				m_BorderDrawingObj.BorderLightestColor = value;
				this.Invalidate();
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Property - Sets or returns the Color used to paint the second to lightest portion of
		/// a 3D border. The default value is the Windows system color for the same thing.
		/// </summary>
		//*******************************************************************************************
		[Category("Appearance")]
		public Color BorderLightColor
		{
			get
			{
				CheckDisposed();

				return m_BorderDrawingObj.BorderLightColor;
			}
			set
			{
				CheckDisposed();

				m_BorderDrawingObj.BorderLightColor = value;
				this.Invalidate();
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Property - Sets or returns a boolean determining if the button maintains its state
		/// when pressed.
		/// </summary>
		/// <remarks>
		/// If this property is set to true, a button will stay in the pushed-in state when the
		/// user presses the button and it isn't already in the pushed-in state. If the user
		/// presses the button when it's in the pushed-in state, it will stay in the unpushed
		/// state.
		/// </remarks>
		//*******************************************************************************************
		[DefaultValue(false)]
		[Category("Behavior")]
		[Description("Determines if the button will maintain its state when pressed.")]
		public bool ButtonToggles
		{
			get
			{
				CheckDisposed();

				return m_fButtonToggles;
			}
			set
			{
				CheckDisposed();

				m_fButtonToggles = value;
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Gets a value indicating what toggle state the button is in.
		/// </summary>
		//*******************************************************************************************
		[Browsable(false)]
		public ButtonToggleStates ButtonToggleState
		{
			get
			{
				CheckDisposed();

				return (m_fButtonToggles && m_fButtonLockedDown ?
					 ButtonToggleStates.Pushed : ButtonToggleStates.Unpushed);
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Property - Sets or returns the height, in pixels, of the button portion of the control.
		/// </summary>
		/// <remarks>
		/// The button portion  of the control (i.e. what visibly changes when the user hovers
		/// over or clicks the control) may be shorter than the control itself. To make the button
		/// portion of the control shorter than the control, set this property to a value that is
		/// less than the control's height. Then set the ButtonFillsControl property to false.
		/// If the ButtonFillsControl property is set to true, this property is ignored.
		/// </remarks>
		/// <seealso cref="ButtonFillsControl"></seealso>
		//*******************************************************************************************
		[Category("Layout")]
		[Description("Determines the button's size. This property is ignored when the ButtonFillsControl property is true.")]
		public Size ButtonSize
		{
			get
			{
				CheckDisposed();

				return m_ButtonSize;
			}
			set
			{
				CheckDisposed();

				m_ButtonSize = value;
				this.Invalidate();
			}
		}

		//**********************************************************************************************
		/// <summary>
		/// Property - Sets or returns a boolean determining if the button portion of the control
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
		//*******************************************************************************************
		[DefaultValue(true)]
		[Category("Layout")]
		[Description("Determines if the button fills the entire control.")]
		public bool ButtonFillsControl
		{
			get
			{
				CheckDisposed();

				return m_fButtonFillsControl;
			}
			set
			{
				CheckDisposed();

				m_fButtonFillsControl = value;
				this.Invalidate();
			}
		}

		//*************************************************************************************
		/// <summary>
		/// Property - sets or returns the position of the text relative to the button. This
		/// property is ignored if TextInButton is true.
		/// </summary>
		/// <remarks>
		/// The following values are possible: None - default position. Right - the text
		/// appears to the right of the button. Bottom - the text appears below the button.
		/// </remarks>
		//*************************************************************************************
		[DefaultValue(TextLocation.Below)]
		[Category("Appearance")]
		[Description("Determines the position of the text relative to the button.")]
		public TextLocation TextPosition
		{
			get
			{
				CheckDisposed();

				return m_TextPosition;
			}
			set
			{
				CheckDisposed();

				m_TextPosition = value;
			}
		}

		//*************************************************************************************
		/// <summary>
		/// Gets or sets how the text should be trimmed if it is to long to fit
		/// into the rectangle
		/// </summary>
		//*************************************************************************************
		[DefaultValue(StringTrimming.EllipsisCharacter)]
		[Category("Appearance")]
		[Description("Determines how the text should be trimmed if it is to long to fit on the button")]
		public StringTrimming TextTrimming
		{
			get
			{
				CheckDisposed();

				return m_TextTrimming;
			}
			set
			{
				CheckDisposed();

				m_TextTrimming = value;
			}
		}

		///*************************************************************************************
		/// <summary>
		/// Returns true if the buttons is pressed (locked down)
		/// </summary>
		///*************************************************************************************
		[Browsable(false)]
		public bool Pressed
		{
			get
			{
				CheckDisposed();

				return m_fButtonLockedDown;
			}
		}
		#endregion
		#region Overridden Methods

		//*************************************************************************************
		//
		// Overridden Methods: This section contains the control's overridden events and methods.
		//
		//*************************************************************************************

		///*************************************************************************************
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		///*************************************************************************************
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (m_fButtonFillsControl)
				m_ButtonSize = this.Size;

			this.Invalidate();
		}

		///*************************************************************************************
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		///*************************************************************************************
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.FillRectangle(new SolidBrush(BackColor), 0, 0, Width, Height);
			m_rect = new Rectangle(0, 0,
				(m_fButtonFillsControl ? Width : m_ButtonSize.Width),
				(m_fButtonFillsControl ? Height : m_ButtonSize.Height));

			if (!m_fButtonFillsControl)
			{
				if (TextPosition == TextLocation.Below)
				{
					// Adjust the alignment of the button.
					// We treat Top/Middle/Bottom the same
					if (m_ButtonAlign == ContentAlignment.TopCenter ||
						m_ButtonAlign == ContentAlignment.MiddleCenter ||
						m_ButtonAlign == ContentAlignment.BottomCenter)
					{
						m_rect.X = (Width - m_ButtonSize.Width) / 2;
					}
					else if (m_ButtonAlign == ContentAlignment.TopRight ||
						m_ButtonAlign == ContentAlignment.MiddleRight ||
						m_ButtonAlign == ContentAlignment.BottomRight)
					{
						m_rect.X = Width - m_ButtonSize.Width;
					}
				}
				else
				{
					// Adjust the alignment of the button.
					// We treat Top/Middle/Bottom the same
					if (m_ButtonAlign == ContentAlignment.MiddleLeft ||
						m_ButtonAlign == ContentAlignment.MiddleCenter ||
						m_ButtonAlign == ContentAlignment.MiddleRight)
					{
						m_rect.Y = (Height - m_ButtonSize.Height) / 2;
					}
					else if (m_ButtonAlign == ContentAlignment.BottomLeft ||
						m_ButtonAlign == ContentAlignment.BottomCenter ||
						m_ButtonAlign == ContentAlignment.BottomRight)
					{
						m_rect.Y = Height - m_ButtonSize.Height;
					}
				}
			}

			// If there's text, draw it in the control.
			if (Text != null)
				PutTextOnButton(e.Graphics);

			// If there's an image, draw it on button.
			if (Image != null)
				PutImageOnButton(e.Graphics);

			// If the button sytle is popup and the mouse isn't over the button then
			// leave without drawing any border.
			if (m_PaintState == ButtonStates.Flat && ButtonStyle == ButtonStyles.Popup)
				return;

			Fw.Drawing.BorderTypes brdrType;

			// Determine what type of border to draw for the button.
			if (ButtonStyle == ButtonStyles.Popup)
			{
				brdrType = (m_PaintState == ButtonStates.Up ?
					Fw.Drawing.BorderTypes.SingleRaised :
					Fw.Drawing.BorderTypes.SingleSunken);
			}
			else if (m_PaintState == ButtonStates.Up || m_PaintState == ButtonStates.Flat)
			{
				brdrType = (ButtonStyle == ButtonStyles.Raised ?
					Fw.Drawing.BorderTypes.SingleRaised :
					Fw.Drawing.BorderTypes.DoubleRaised);
			}
			else
			{
				brdrType = (SunkenAppearance == SunkenAppearances.Deep ?
					Fw.Drawing.BorderTypes.DoubleSunken :
					(SunkenAppearance == SunkenAppearances.Sunken ?
					Fw.Drawing.BorderTypes.SingleSunken :
					Fw.Drawing.BorderTypes.Single));
			}

			// Finally, draw the border.
			m_BorderDrawingObj.Draw(e.Graphics, m_rect, brdrType);
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		//*******************************************************************************************
		protected override void OnMouseEnter(EventArgs e)
		{
			m_PaintState = (m_fButtonLockedDown || m_fMouseButtonDown ? ButtonStates.Down : ButtonStates.Up);
			Invalidate();

			base.OnMouseEnter(e);
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		//*******************************************************************************************
		protected override void OnMouseLeave(EventArgs e)
		{
			m_PaintState = (m_fButtonLockedDown ? ButtonStates.Down : ButtonStates.Flat);
			Invalidate();

			base.OnMouseLeave(e);
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		//*******************************************************************************************
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

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		//*******************************************************************************************
		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				m_fMouseButtonDown = false;

				if (!m_fButtonToggles)
				{
					m_fButtonLockedDown = false;
					m_PaintState = ButtonStates.Flat;
					Invalidate();
				}
				else
				{
					// Only do the following block of code when the button's state is not already
					// pushed in or it's not part of a group of option buttons.
					if (!m_behaveLikeOptionButton || !m_fButtonLockedDown)
					{
						// If the mouse pointer is still over the button when the user lifts
						// the mouse button, toggle the buttons state. Otherwise, ignore the
						// fact that the user ever pressed the mouse button in the first place.
						if (m_rect.Contains(e.X, e.Y))
							PressButton(!m_fButtonLockedDown);
						else
						{
							m_PaintState = (m_fButtonLockedDown ? ButtonStates.Down : ButtonStates.Flat);
							Invalidate();
						}
					}
				}
			}

			base.OnMouseUp(e);
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		//*******************************************************************************************
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (m_fMouseButtonDown && !m_fButtonLockedDown)
			{
				ButtonStates nTmpPaintState = ButtonStates.Flat;

				if (m_rect.Contains(e.X, e.Y))
					nTmpPaintState = ButtonStates.Down;

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

		//*******************************************************************************************
		//
		// Helper Functions: This sections contains private functions.
		//
		//*******************************************************************************************

		//*******************************************************************************************
		//*******************************************************************************************
		private void PutTextOnButton(Graphics g)
		{
			StringFormat sf = new StringFormat();

			// Take the text's content alignment value and convert it into the individual
			// alignment types for horizontal and vertical alignment.
			sf.Alignment = ContentAlignmentHelper.ConAlignToHorizStrAlign(TextAlign);
			sf.LineAlignment = ContentAlignmentHelper.ConAlignToVertStrAlign(TextAlign);
			sf.Trimming = m_TextTrimming;
			sf.FormatFlags = StringFormatFlags.LineLimit;

			// Create a rectangle in which to align text that accounts for the buttons borders.
			// Multiplying by 2 accounts the combined thickness of the top and bottom borders,
			// and the combined thickness of the left and right borders.
			Rectangle rc = new Rectangle(m_dpBorderWidth, m_dpBorderWidth,
				this.Width - (m_dpBorderWidth * 2), this.Height - (m_dpBorderWidth * 2));

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
					rc.Height = this.Height - m_rect.Height - 2;
					rc.Y = m_rect.Height + 2;
				}
				else if (TextPosition == TextLocation.Right && m_rect.Width < this.Width)
				{
					// Make the text's rectangle 2 pixels smaller so the text doesn't get
					// too close to the edges. ENHANCE: Should probably add a property
					// called TextMargin or Padding or something like that, instead of
					// hard coding the value to 2.
					rc.Width = this.Width - m_rect.Width - 2;
					rc.X = m_rect.Width + 2;
				}
			}


			// Ensure that ellipsis draws for text if task bar is to small
			//if (Parent.Width < rc.Width)
			//{
			//	rc.Width = Parent.Width;
			//}

			g.DrawString(Text, this.Font, new SolidBrush(this.ForeColor), rc, sf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the control has an image specified, this function paints it on the button portion
		/// of the control.
		/// </summary>
		/// <param name="g">A Graphics type passed from the OnPaint method.</param>
		/// <seealso cref="OnPaint"/>
		/// ------------------------------------------------------------------------------------
		private void PutImageOnButton(Graphics g)
		{
			Point pt = ContentAlignmentHelper.ConAlignToImgPosition(ImageAlign, Image, m_rect,
				m_dpBorderWidth);
			g.DrawImageUnscaled(Image, pt);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ShouldSerializeBorderDarkColor()
		{
			return m_BorderDrawingObj.BorderDarkColor != SystemColors.ControlDark;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ShouldSerializeBorderDarkestColor()
		{
			return m_BorderDrawingObj.BorderDarkestColor != SystemColors.ControlDarkDark;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ShouldSerializeBorderLightestColor()
		{
			return m_BorderDrawingObj.BorderLightestColor != SystemColors.ControlLightLight;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ShouldSerializeBorderLightColor()
		{
			return m_BorderDrawingObj.BorderLightColor != SystemColors.ControlLight;
		}
		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		private bool ShouldSerializeButtonSize()
		{
			return !(m_ButtonSize.Width == this.Width && m_ButtonSize.Height == this.Height);
		}

		//*******************************************************************************************
		/// <summary>
		/// Resets the button size to fill whole control
		/// </summary>
		//*******************************************************************************************
		private void ResetButtonSize()
		{
			m_ButtonSize.Width = this.Width;
			m_ButtonSize.Height = this.Height;
		}

		#endregion

	} // FwButton Class
}
