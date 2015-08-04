// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using SIL.CoreImpl.Properties;
using SIL.Utils;

namespace SIL.CoreImpl.SilSidePane
{
	/// <summary></summary>
	[Flags]
	internal enum ButtonState
	{
		/// <summary></summary>
		None = 0,
		/// <summary></summary>
		Passive = 1,
		/// <summary></summary>
		Hovering = 2,
		/// <summary></summary>
		Selected = 4,
		/// <summary></summary>
		Pressed = 8, // Currently not used.
		/// <summary></summary>
		Disabled = 16,
	}

	/// <summary></summary>
	internal enum Renderer
	{
		/// <summary></summary>
		Outlook2003,
		/// <summary></summary>
		Outlook2007,
		/// <summary></summary>
		Custom
	}

	/// <summary></summary>
	[DefaultEvent("ButtonClicked")]
	internal class OutlookBar : Control
	{
		/// <summary></summary>
		public event ButtonClickedEventHandler ButtonClicked;
		/// <summary></summary>
		public delegate void ButtonClickedEventHandler(object sender, OutlookBarButton button);

		#region Member variables and constants
		private const int kImageDimensionLarge = 26;
		private const int kImageDimensionSmall = 19;

		private ToolTip m_toolTip = new ToolTip();
		private OutlookBarButtonCollection m_buttons;
		private OutlookBarButton m_hoveringButton;
		private OutlookBarButton m_leftClickedButton;
		private OutlookBarButton m_rightClickedButton;
		/// <summary></summary>
		internal OutlookBarButton m_selectedButton;

#if __MonoCS__ // TODO-Linux: FWNX-459
		private Renderer m_renderer = Renderer.Outlook2003;
#else
		// This choice seems arbirtrary, but it gives highlighting and coloring
		// similar to FW6.0
		private Renderer m_renderer = Renderer.Outlook2007;
#endif
		private bool m_dropDownHovering;
		private bool m_isResizing;
		private bool m_canGrow;
		private bool m_canShrink;
		private int m_maxLargeButtonCount;
		private int m_maxSmallButtonCount;
		private int m_buttonHeight = 26;
		private int m_buttonIconMarginFromLeft = 2; // px
		private int m_buttonTextMarginFromIconOnLeft = 6; // px
		private Color m_outlookBarLineColor = ProfessionalColors.ToolStripBorder;
		private Color m_foreColorSelected;
		private Color m_buttonColorHoveringTop;
		private Color m_buttonColorSelectedTop;
		private Color m_buttonColorSelectedAndHoveringTop;
		private Color m_buttonColorPassiveTop;
		private Color m_buttonColorHoveringBottom;
		private Color m_buttonColorSelectedBottom;
		private Color m_buttonColorSelectedAndHoveringBottom;
		private Color m_buttonColorPassiveBottom;
		private IContainer components = new Container();
		#endregion

		/// <summary></summary>
		public OutlookBar()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.ResizeRedraw, true);
			m_buttons = new OutlookBarButtonCollection(this);
			Font = new Font(SystemInformation.MenuFont, FontStyle.Bold);
		}

		/// <summary></summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			if (disposing && !IsDisposed)
			{
				if (m_buttons != null)
				{
					for (int i = 0; i < m_buttons.Count; i++)
						m_buttons[i].Dispose();
					m_buttons.Clear();
				}
				if (m_toolTip != null)
					m_toolTip.Dispose();
				if (components != null)
					components.Dispose();
			}
			m_buttons = null;
			m_toolTip = null;
			base.Dispose(disposing);
		}

		#region Public Properties
		/// <summary></summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Behavior")]
		public OutlookBarButtonCollection Buttons
		{
			get { return m_buttons; }
		}

		/// <summary></summary>
		[Browsable(false)]
		public OutlookBarButton SelectedButton
		{
			get { return m_selectedButton; }
		}

		/// <summary></summary>
		[DefaultValue(typeof(Renderer), "Outlook2003")]
		[Category("Appearance")]
		public Renderer Renderer
		{
			get { return m_renderer; }
			set
			{
				m_renderer = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		public override Size MinimumSize
		{
			get { return new Size(BottomContainerLeftMargin, BottomContainerRectangle.Height + GripRectangle.Height); }
			set { }
		}

		/// <summary></summary>
		[Category("Appearance"), DisplayName("LineColor")]
		public Color OutlookBarLineColor
		{
			get { return m_outlookBarLineColor; }
			set
			{
				m_outlookBarLineColor = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[Category("Appearance")]
		public int ButtonHeight
		{
			get { return m_buttonHeight; }
			set
			{
				if (value < 5)
					value = 5;

				m_buttonHeight = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[DisplayName("ForeColorNotSelected")]
		public override Color ForeColor
		{
			get { return base.ForeColor; }
			set
			{
				base.ForeColor = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[Category("Appearance")]
		public Color ForeColorSelected
		{
			get { return m_foreColorSelected; }
			set
			{
				m_foreColorSelected = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[DisplayName("ButtonFont")]
		public override Font Font
		{
			get { return base.Font; }
			set
			{
				base.Font = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[Category("Appearance"), DisplayName("ButtonHovering1")]
		public Color ButtonColorHoveringTop
		{
			get { return m_buttonColorHoveringTop; }
			set
			{
				m_buttonColorHoveringTop = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[Category("Appearance"), DisplayName("ButtonSelected1")]
		public Color ButtonColorSelectedTop
		{
			get { return m_buttonColorSelectedTop; }
			set
			{
				m_buttonColorSelectedTop = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[Category("Appearance"), DisplayName("ButtonSelectedHovering1")]
		public Color ButtonColorSelectedAndHoveringTop
		{
			get { return m_buttonColorSelectedAndHoveringTop; }
			set
			{
				m_buttonColorSelectedAndHoveringTop = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[Category("Appearance"), DisplayName("ButtonPassive1")]
		public Color ButtonColorPassiveTop
		{
			get { return m_buttonColorPassiveTop; }
			set
			{
				m_buttonColorPassiveTop = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[Category("Appearance"), DisplayName("ButtonHovering2")]
		public Color ButtonColorHoveringBottom
		{
			get { return m_buttonColorHoveringBottom; }
			set
			{
				m_buttonColorHoveringBottom = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[Category("Appearance"), DisplayName("ButtonSelected2")]
		public Color ButtonColorSelectedBottom
		{
			get { return m_buttonColorSelectedBottom; }
			set
			{
				m_buttonColorSelectedBottom = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[Category("Appearance"), DisplayName("ButtonSelectedHovering2")]
		public Color ButtonColorSelectedAndHoveringBottom
		{
			get { return m_buttonColorSelectedAndHoveringBottom; }
			set
			{
				m_buttonColorSelectedAndHoveringBottom = value;
				Invalidate();
			}
		}

		/// <summary></summary>
		[Category("Appearance"), DisplayName("ButtonPassive2")]
		public Color ButtonColorPassiveBottom
		{
			get { return m_buttonColorPassiveBottom; }
			set
			{
				m_buttonColorPassiveBottom = value;
				Invalidate();
			}
		}

		#endregion

		/// <summary>
		/// Needed because this way the buttons can raise the ButtonClicked event...
		/// </summary>
		public void SetSelectionChanged(OutlookBarButton button)
		{
			m_selectedButton = button;
			Invalidate();
			if (ButtonClicked != null)
				ButtonClicked(this, button);
		}

		/// <summary>
		/// If possible, increase the main OutlookBar area to expand a collapsed tab.
		/// </summary>
		public void ShowAnotherButton()
		{
			m_canGrow = (m_maxLargeButtonCount < Buttons.VisibleCount);
			if (m_canGrow)
				Height += InternalButtonHeight;
		}

		#region Mouse Events
		/// <summary></summary>
		protected override void OnMouseClick(MouseEventArgs e)
		{
			m_rightClickedButton = null;
			OutlookBarButton button = Buttons.GetItem(e.X, e.Y);
			if (button == null)
			{
				if (DropDownRectangle.Contains(e.X, e.Y))
					CreateContextMenu();

				return;
			}

			if (e.Button == MouseButtons.Right)
				m_rightClickedButton = button;
			else if (e.Button == MouseButtons.Left)
			{
				if (button.Enabled)
				{
					m_selectedButton = button;
					if (ButtonClicked != null)
						ButtonClicked(this, button);
				}
			}
			else
			{
				return;
			}

			Invalidate();
		}

		/// <summary></summary>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			m_isResizing = GripRectangle.Contains(e.X, e.Y);
		}

		/// <summary></summary>
		protected override void OnMouseLeave(EventArgs e)
		{
			if (m_rightClickedButton == null)
			{
				m_hoveringButton = null;
				m_dropDownHovering = false;
				Invalidate();
			}
		}

		/// <summary></summary>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			m_hoveringButton = null;
			m_dropDownHovering = false;

			if (m_isResizing)
			{
				if (e.Y < -InternalButtonHeight)
				{
					if (m_canGrow)
						Height += InternalButtonHeight;
				}
				else if (e.Y > InternalButtonHeight)
				{
					if (m_canShrink)
						Height -= InternalButtonHeight;
				}

				return;
			}

			if (GripRectangle.Contains(e.X, e.Y))
			{
				Cursor = Cursors.SizeNS;
				return;
			}

			if (DropDownRectangle.Contains(e.X, e.Y))
			{
				Cursor = Cursors.Hand;
				m_dropDownHovering = true;
				Invalidate();

				//adjust Tooltip...
				if ((m_toolTip.Tag != null))
				{
					if (!m_toolTip.Tag.Equals("Configure"))
					{
						m_toolTip.Active = true;
						m_toolTip.SetToolTip(this, SilSidePane.ConfigureButtons);
						m_toolTip.Tag = "Configure";
					}
				}
				else
				{
					m_toolTip.Active = true;
					m_toolTip.SetToolTip(this, SilSidePane.ConfigureButtons);
					m_toolTip.Tag = "Configure";
				}
			}
			else if ((Buttons.GetItem(e.X, e.Y) != null))
			{
				Cursor = Cursors.Hand;
				m_hoveringButton = Buttons.GetItem(e.X, e.Y);
				Invalidate();

				//adjust tooltip...
				if (!m_hoveringButton.isLarge)
				{
					if (m_toolTip.Tag == null)
					{
						m_toolTip.Active = true;
						m_toolTip.SetToolTip(this, m_hoveringButton.Text);
						m_toolTip.Tag = m_hoveringButton;
					}
					else
					{
						if (!m_toolTip.Tag.Equals(m_hoveringButton))
						{
							m_toolTip.Active = true;
							m_toolTip.SetToolTip(this, m_hoveringButton.Text);
							m_toolTip.Tag = m_hoveringButton;
						}
					}
				}
				else
				{
					m_toolTip.Active = false;
				}
			}
			else
			{
				Cursor = Cursors.Default;
			}
		}

		/// <summary></summary>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			m_isResizing = false;
			m_leftClickedButton = null;
		}

		#endregion

		#region Painting methods
		/// <summary></summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			m_maxLargeButtonCount = (int)Math.Floor(
				(decimal)(Height - BottomContainerRectangle.Height - GripRectangle.Height) / InternalButtonHeight) + 1;

			if (Buttons.VisibleCount < m_maxLargeButtonCount)
				m_maxLargeButtonCount = Buttons.VisibleCount;

			m_canShrink = (m_maxLargeButtonCount != 0);
			m_canGrow = (m_maxLargeButtonCount < Buttons.VisibleCount);
			Height = (m_maxLargeButtonCount * (InternalButtonHeight + 2)) + GripRectangle.Height + BottomContainerRectangle.Height;

			//Paint Grip...
			PaintGripRectangle(e.Graphics);

			//Paint Large Buttons...
			int SyncLargeButtons = 0;
			int IterateLargeButtons = 0;
			for (IterateLargeButtons = 0; IterateLargeButtons <= Buttons.Count - 1; IterateLargeButtons++)
			{
				if (Buttons[IterateLargeButtons].Visible)
				{
					Rectangle rec = new Rectangle(0,
						(SyncLargeButtons * (InternalButtonHeight + 2)) + GripRectangle.Height, Width, InternalButtonHeight + 2);
					Buttons[IterateLargeButtons].Rectangle = rec;
					Buttons[IterateLargeButtons].isLarge = true;
					PaintButton(Buttons[IterateLargeButtons], e.Graphics, (m_maxLargeButtonCount != SyncLargeButtons));
					if (SyncLargeButtons == m_maxLargeButtonCount)
						break;

					SyncLargeButtons++;
				}
			}

			//Paint Small Buttons...
			m_maxSmallButtonCount =
				(int)Math.Floor((decimal)(Width - DropDownRectangle.Width - BottomContainerLeftMargin) / SmallButtonWidth);

			if ((Buttons.VisibleCount - m_maxLargeButtonCount) <= 0)
				m_maxSmallButtonCount = 0;

			if (m_maxSmallButtonCount > (Buttons.VisibleCount - m_maxLargeButtonCount))
				m_maxSmallButtonCount = (Buttons.VisibleCount - m_maxLargeButtonCount);

			int StartX = Width - DropDownRectangle.Width - (m_maxSmallButtonCount * SmallButtonWidth);
			int SyncSmallButtons = 0;
			int IterateSmallButtons = 0;

			for (IterateSmallButtons = IterateLargeButtons; IterateSmallButtons <= Buttons.Count - 1; IterateSmallButtons++)
			{
				if (SyncSmallButtons == m_maxSmallButtonCount)
					break;

				if (Buttons[IterateSmallButtons].Visible)
				{
					Buttons[IterateSmallButtons].isLarge = false;
					Buttons[IterateSmallButtons].Rectangle = new Rectangle(StartX + (SyncSmallButtons * SmallButtonWidth),
						BottomContainerRectangle.Y, SmallButtonWidth, BottomContainerRectangle.Height);
					PaintButton(Buttons[IterateSmallButtons], e.Graphics, false);
					SyncSmallButtons++;
				}
			}

			for (int i = IterateSmallButtons; i <= Buttons.VisibleCount - 1; i++)
				Buttons[i].Rectangle = Rectangle.Empty;

			//Draw Empty Space...
			Rectangle rc = BottomContainerRectangle;
			rc.Width = Width - (m_maxSmallButtonCount * SmallButtonWidth) - DropDownRectangle.Width;
			FillButton(rc, e.Graphics, ButtonState.Passive, true, true, false);

			//Paint DropDown...
			PaintDropDownRectangle(e.Graphics);

			//Finally, paint the bottom line...
			using (Pen pen = new Pen(InternalOutlookBarLineColor))
				e.Graphics.DrawLine(pen, 0, Height - 1, Width - 1, Height - 1);
		}

		/// <summary>
		/// Paint button background, text, and icon.
		/// </summary>
		/// <remarks>
		/// Parameter isLastLarge is passed from OnPaint() in this way:
		///  * False if you have painted the last large button.
		///  * True if there might be more visible large buttons to paint.
		/// </remarks>
		private void PaintButton(OutlookBarButton button, Graphics g, bool isLastLarge)
		{
			// Paint background

			ButtonState state = ButtonState.None;

			if (button.Equals(SelectedButton) || m_leftClickedButton != null)
				state = ButtonState.Selected;

			if (button == m_hoveringButton)
				state |= ButtonState.Hovering;

			if (button != m_hoveringButton && !button.Equals(SelectedButton))
				state = ButtonState.Passive;

			if (!button.Enabled)
				state = ButtonState.Disabled;

			Debug.Assert(state != ButtonState.None, "Button didn't have a definable state. Programmer error.");

			FillButton(button.Rectangle, g, state, true, button.isLarge, button.isLarge);

			// Paint icon

			Rectangle iconRect = GetRectangleForButtonIcon(button);

			if ((button.isLarge & isLastLarge) || !button.isLarge)
				DrawButtonIcon(g, button, iconRect);

			// Paint text

			if (button.isLarge && isLastLarge)
			{
				PointF location = new PointF();
				location.X = iconRect.X + iconRect.Width + m_buttonTextMarginFromIconOnLeft;
				location.Y = (float)button.Rectangle.Y + ((InternalButtonHeight / 2.0f) - (Font.Height / 2.0f));

				g.DrawString(button.Text, Font, GetButtonTextBrush(button == SelectedButton, button.Enabled), location);
			}
		}

		/// <summary>
		/// Create and position a rectangle for a button's icon to be drawn into,
		/// but no bigger than kImageDimensionLarge or kImageDimensionSmall, as appropriate.
		/// </summary>
		private Rectangle GetRectangleForButtonIcon(OutlookBarButton button)
		{
			Rectangle rc = new Rectangle();

			int imageDimension = button.isLarge ? kImageDimensionLarge : kImageDimensionSmall;

			rc.Width = button.Image.Width;
			rc.Height = button.Image.Height;

			if (button.Image.Width > imageDimension)
				rc.Width = imageDimension;
			if (button.Image.Height > imageDimension)
				rc.Height = imageDimension;

			rc.Y = button.Rectangle.Y +
				(int)Math.Floor(((decimal)InternalButtonHeight / 2) - ((decimal)imageDimension / 2)) + 1;

			if (button.isLarge)
				rc.X = m_buttonIconMarginFromLeft;
			else
				rc.X = button.Rectangle.X +
					(int)Math.Floor(((decimal)SmallButtonWidth / 2) - ((decimal)imageDimension / 2));

			// If button icon is smaller than the standard size, then move it down and over a bit to
			// center it.
			if (button.Image.Width < imageDimension)
				rc.X += (imageDimension - button.Image.Width) / 2;
			if (button.Image.Height < imageDimension)
				rc.Y += (imageDimension - button.Image.Height) / 2;

			return rc;
		}

		/// <summary>
		/// Draws the icon associated with a button. Draws greyscaled if button is disabled.
		/// </summary>
		private void DrawButtonIcon(Graphics g, OutlookBarButton button, Rectangle rc)
		{
			Image icon = button.Image;
			ImageAttributes attributes = null;

			try
			{
				if (!button.Enabled)
				{
					attributes = new ImageAttributes();
					ColorMatrix colorToGreyscale = GetGreyscalingColorMatrix();
					attributes.SetColorMatrix(colorToGreyscale);
				}

				g.DrawImage(icon, rc, 0, 0, icon.Width, icon.Height, GraphicsUnit.Pixel, attributes);
			}
			finally
			{
				if (attributes != null)
					attributes.Dispose();
			}
		}

		/// <returns>
		/// A new ColorMatrix which can be given to an ImageAttributes which can be used to cause
		/// a DrawImage call to draw a greyscale image.
		/// </returns>
		private ColorMatrix GetGreyscalingColorMatrix()
		{
			// Helpful was http://en.wikipedia.org/wiki/Grayscale#Converting_color_to_grayscale
			// and http://blogs.techrepublic.com.com/howdoi/?p=120

			// Adjustments to turn color to greyscale
			float red   = .3f;
			float green = .59f;
			float blue  = .11f;

			float[][] matrix = {
				new float[] {red,   red,   red,   0, 0},
				new float[] {green, green, green, 0, 0},
				new float[] {blue,  blue,  blue,  0, 0},
				new float[] {0,     0,     0,     1, 0},
				new float[] {0,     0,     0,     0, 0},
			};

			ColorMatrix colorToGreyscale = new ColorMatrix(matrix);
			return colorToGreyscale;
		}

		/// <summary></summary>
		private void FillButton(Rectangle rc, Graphics g, ButtonState state,
			bool drawTopBorder, bool drawLeftBorder, bool drawRightBorder)
		{
			Brush br;

			switch (Renderer)
			{
				case Renderer.Outlook2003:
					using (br = new LinearGradientBrush(rc, GetButtonColor(state, 0),
						GetButtonColor(state, 1), LinearGradientMode.Vertical))
					{
						g.FillRectangle(br, rc);
					}
					break;

				case Renderer.Outlook2007:
					//Filling the top part of the button...
					Rectangle TopRectangle = rc;
					using (br = new LinearGradientBrush(TopRectangle, GetButtonColor(state, 0),
						GetButtonColor(state, 1), LinearGradientMode.Vertical))
					{
						TopRectangle.Height = (InternalButtonHeight * 15) / 32;
						g.FillRectangle(br, TopRectangle);
					}

					//and the bottom part...
					Rectangle BottomRectangle = rc;
					using (br = new LinearGradientBrush(BottomRectangle, GetButtonColor(state, 2),
						GetButtonColor(state, 3), LinearGradientMode.Vertical))
					{
						BottomRectangle.Y += (InternalButtonHeight * 12) / 32;
						BottomRectangle.Height -= (InternalButtonHeight * 12) / 32;
						g.FillRectangle(br, BottomRectangle);
					}
					break;

				case Renderer.Custom:
					using (br = new LinearGradientBrush(rc, GetButtonColor(state, 0),
						GetButtonColor(state, 1), LinearGradientMode.Vertical))
					{
						g.FillRectangle(br, rc);
					}
					break;
			}

			using (Pen pen = new Pen(InternalOutlookBarLineColor))
			{
				//Draw Top Border...
				if (drawTopBorder)
					g.DrawLine(pen, rc.X, rc.Y, rc.Width + rc.X, rc.Y);

				//Draw Left Border...
				if (drawLeftBorder)
					g.DrawLine(pen, rc.X, rc.Y, rc.X, rc.Y + rc.Height);

				//Draw Right Border...
				if (drawRightBorder)
					g.DrawLine(pen, rc.X + rc.Width - 1, rc.Y, rc.X + rc.Width - 1, rc.Y + rc.Height);
			}
		}

		private void PaintGripRectangle(Graphics g)
		{
			//Paint the backcolor...
			using (Brush br = GripBrush)
				g.FillRectangle(br, GripRectangle);

			//Draw the icon...
			using (Icon icon = GripIcon)
			{
				Rectangle rc = new Rectangle((int)((int)Width / 2) - ((int)icon.Width / 2),
					(((int)(GripRectangle.Height) / 2) - (int)(icon.Height / 2)), icon.Width, icon.Height);

				g.DrawIcon(icon, rc);
			}

			g.DrawLine(new Pen(OutlookBarLineColor, 1), 0, 0, 0, GripRectangle.Height);
			g.DrawLine(new Pen(OutlookBarLineColor, 1), GripRectangle.Width - 1, 0, GripRectangle.Width - 1, GripRectangle.Height);

		}

		/// <summary></summary>
		private void PaintDropDownRectangle(Graphics g)
		{
			//Repaint the backcolor if the mouse is hovering...
			FillButton(DropDownRectangle, g,
				(m_dropDownHovering ? ButtonState.Hovering : ButtonState.Passive), true, false, true);

			//Draw the icon...
			Icon icon = DropDownIcon;
			Rectangle rc = new Rectangle((DropDownRectangle.X + ((DropDownRectangle.Width / 2) - (icon.Width / 2))),
				(DropDownRectangle.Y + (((DropDownRectangle.Height / 2) - (icon.Height / 2)) + 1)), icon.Width, icon.Height);

			g.DrawIcon(icon, rc);
			icon.Dispose();

		}

		#endregion

		#region Renderer-dependent values
		/// <summary></summary>
		private Color InternalOutlookBarLineColor
		{
			get
			{
				switch (Renderer)
				{
					case Renderer.Outlook2003: return ProfessionalColors.ToolStripBorder;
					case Renderer.Outlook2007: return Color.FromArgb(101, 147, 207);
					default: return OutlookBarLineColor;
				}
			}
		}

		/// <summary></summary>
		private Brush GetButtonTextBrush(bool isSelected, bool isEnabled)
		{
			if (!isEnabled)
				return SystemBrushes.GrayText;

			switch (Renderer)
			{
				case Renderer.Outlook2003: return SystemBrushes.ControlText; // Brushes.Black;
				case Renderer.Outlook2007: return (isSelected ? new SolidBrush(Color.FromArgb(32, 77, 137 )) : Brushes.Black);
				case Renderer.Custom: return new SolidBrush(isSelected ? ForeColor : ForeColorSelected);
			}

			return null;
		}

		///// <summary></summary>
		//private Font ButtonFont
		//{
		//    get
		//    {
		//        switch (Renderer)
		//        {
		//            case Renderer.Custom: return Font;
		//            case Renderer.Outlook2003: return new Font("Tahoma", 8.25f, FontStyle.Bold, GraphicsUnit.Point, 0);
		//            case Renderer.Outlook2007: return new Font("Tahoma", 8.25f, FontStyle.Bold, GraphicsUnit.Point, 0);
		//        }

		//        return null;
		//    }
		//}

		/// <summary></summary>
		private Color GetButtonColor(ButtonState buttonState, int colorIndex)
		{
			switch (Renderer)
			{
				case Renderer.Outlook2003:
					switch (buttonState)
					{
						case ButtonState.Hovering | ButtonState.Pressed:
						case ButtonState.Hovering | ButtonState.Selected:
							if (colorIndex == 0) return ProfessionalColors.ButtonCheckedGradientEnd;
							if (colorIndex == 1) return ProfessionalColors.ButtonCheckedGradientBegin;
							break;
						case ButtonState.Hovering:
							if (colorIndex == 0) return ProfessionalColors.ButtonSelectedGradientBegin;
							if (colorIndex == 1) return ProfessionalColors.ButtonSelectedGradientEnd;
							break;
						case ButtonState.Selected:
							if (colorIndex == 0) return ProfessionalColors.ButtonCheckedGradientBegin;
							if (colorIndex == 1) return ProfessionalColors.ButtonCheckedGradientEnd;
							break;
						case ButtonState.Disabled:
						default:
							if (colorIndex == 0) return ProfessionalColors.ToolStripGradientBegin;
							if (colorIndex == 1) return ProfessionalColors.ToolStripGradientEnd;
							break;
					}
					break;

				case Renderer.Outlook2007:
					switch (buttonState)
					{
						case ButtonState.Hovering | ButtonState.Selected:
							if (colorIndex == 0) return Color.FromArgb(255, 189, 105);
							if (colorIndex == 1) return Color.FromArgb(255, 172, 66);
							if (colorIndex == 2) return Color.FromArgb(251, 140, 60);
							if (colorIndex == 3) return Color.FromArgb(254, 211, 101);
							break;
						case ButtonState.Hovering:
							if (colorIndex == 0) return Color.FromArgb(255, 254, 228);
							if (colorIndex == 1) return Color.FromArgb(255, 232, 166);
							if (colorIndex == 2) return Color.FromArgb(255, 215, 103);
							if (colorIndex == 3) return Color.FromArgb(255, 230, 159);
							break;
						case ButtonState.Selected:
							if (colorIndex == 0) return Color.FromArgb(255, 217, 170);
							if (colorIndex == 1) return Color.FromArgb(255, 187, 109);
							if (colorIndex == 2) return Color.FromArgb(255, 171, 63);
							if (colorIndex == 3) return Color.FromArgb(254, 225, 123);
							break;
						case ButtonState.Disabled:
						case ButtonState.Passive:
							if (colorIndex == 0) return Color.FromArgb(227, 239, 255);
							if (colorIndex == 1) return Color.FromArgb(196, 221, 255);
							if (colorIndex == 2) return Color.FromArgb(173, 209, 255);
							if (colorIndex == 3) return Color.FromArgb(193, 219, 255);
							break;
					}
					break;

				case Renderer.Custom:
					switch (buttonState)
					{
						case ButtonState.Hovering | ButtonState.Selected:
							if (colorIndex == 0) return ButtonColorSelectedAndHoveringTop;
							if (colorIndex == 1) return ButtonColorSelectedAndHoveringBottom;
							break;
						case ButtonState.Hovering:
							if (colorIndex == 0) return ButtonColorHoveringTop;
							if (colorIndex == 1) return ButtonColorHoveringBottom;
							break;
						case ButtonState.Selected:
							if (colorIndex == 0) return ButtonColorSelectedTop;
							if (colorIndex == 1) return ButtonColorSelectedBottom;
							break;
						case ButtonState.Disabled:
						case ButtonState.Passive:
							if (colorIndex == 0) return ButtonColorPassiveTop;
							if (colorIndex == 1) return ButtonColorPassiveBottom;
							break;
					}
					break;
			}

			return Color.Empty;
		}

		/// <summary></summary>
		private int InternalButtonHeight
		{
			get
			{
				return ButtonHeight;
			}
		}

		/// <summary></summary>
		private Rectangle BottomContainerRectangle
		{
			get { return new Rectangle(0, Height - InternalButtonHeight, Width, InternalButtonHeight); }
		}

		/// <summary></summary>
		private int BottomContainerLeftMargin
		{
			get
			{
				return m_buttonIconMarginFromLeft;
			}
		}

		/// <summary></summary>
		private int SmallButtonWidth
		{
			get
			{
				switch (Renderer)
				{
					case Renderer.Outlook2003: return 22;
					case Renderer.Outlook2007: return 26;
					case Renderer.Custom: return 25;
				}

				return 5;
			}
		}

		/// <summary></summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning an object")]
		private Brush GripBrush
		{
			get
			{
				switch (Renderer)
				{
					case Renderer.Outlook2003: return new LinearGradientBrush(GripRectangle,
						ProfessionalColors.OverflowButtonGradientBegin, ProfessionalColors.OverflowButtonGradientEnd,
						LinearGradientMode.Vertical);
					case Renderer.Outlook2007: return new LinearGradientBrush(GripRectangle,
						Color.FromArgb(227, 239, 255), Color.FromArgb(179, 212, 255), LinearGradientMode.Vertical);
					case Renderer.Custom: return new LinearGradientBrush(GripRectangle,
						ButtonColorPassiveTop, ButtonColorPassiveBottom, LinearGradientMode.Vertical);
				}

				return null;
			}
		}

		/// <summary></summary>
		private Rectangle GripRectangle
		{
			get
			{
				int Height = 0;
				switch (Renderer)
				{
					case Renderer.Outlook2003: Height = 6; break;
					case Renderer.Outlook2007: Height = 8; break;
					case Renderer.Custom: Height = 8; break;
				}

				return new Rectangle(0, 0, Width, Height);
			}
		}

		/// <summary></summary>
		private Icon GripIcon
		{
			get
			{
				switch (Renderer)
				{
					case Renderer.Outlook2003: return Resources.Grip2003;
					case Renderer.Outlook2007: return Resources.Grip2007;
					case Renderer.Custom: return Resources.Grip2007;
				}

				return null;
			}
		}

		/// <summary></summary>
		private Rectangle DropDownRectangle
		{
			get
			{
				return new Rectangle((Width - SmallButtonWidth), (Height - InternalButtonHeight),
					SmallButtonWidth, InternalButtonHeight);
			}
		}

		/// <summary>
		/// Icon to click to see overflow buttons from an item or tab area.
		/// </summary>
		private Icon DropDownIcon
		{
			get
			{
				switch (Renderer)
				{
					case Renderer.Outlook2003: return Properties.Resources.DropDown2003;
					case Renderer.Outlook2007: return Properties.Resources.DropDown2007;
					case Renderer.Custom: return Properties.Resources.DropDown2007;
				}

				return null;
			}
		}

		#endregion

		#region " MenuItems and Options "
		/// <summary></summary>
		private void CreateContextMenu()
		{
			var contextMenuStrip = components.ContextMenuStrip("contextMenu");
			contextMenuStrip.Items.Add(SilSidePane.ShowMoreButtons,
					Resources.Arrow_Up.ToBitmap(), ShowMoreButtons);

			contextMenuStrip.Items.Add(SilSidePane.ShowFeWerButtons,
					Resources.Arrow_Down.ToBitmap(), ShowFewerButtons);

				if (m_maxLargeButtonCount >= Buttons.VisibleCount)
				contextMenuStrip.Items[0].Enabled = false;

				if (m_maxLargeButtonCount == 0)
				contextMenuStrip.Items[1].Enabled = false;

			contextMenuStrip.Items.Add(SilSidePane.NavPaneOptions, null, NavigationPaneOptions);

				ToolStripMenuItem mnuAdd = new ToolStripMenuItem(SilSidePane.AddOrRemoveButtons, null);
			contextMenuStrip.Items.Add(mnuAdd);

				foreach (OutlookBarButton btn in Buttons)
				{
					if (btn.Allowed)
					{
						ToolStripMenuItem mnu = new ToolStripMenuItem();
						mnu.Text = btn.Text;
						mnu.Image = btn.Image;
						mnu.CheckOnClick = true;
						mnu.Checked = btn.Visible;
						mnu.Tag = btn;
						mnu.Click += ToggleVisible;
						mnuAdd.DropDownItems.Add(mnu);
					}
				}

				int btnCount = 0;
				foreach (OutlookBarButton btn in Buttons)
				{
					if (btn.Visible && btn.Rectangle == Rectangle.Empty)
						btnCount++;
				}

				if (btnCount > 0)
				contextMenuStrip.Items.Add(new ToolStripSeparator());

				foreach (OutlookBarButton btn in Buttons)
				{
					if (btn.Rectangle == Rectangle.Empty && btn.Visible)
					{
						ToolStripMenuItem mnu = new ToolStripMenuItem();
						mnu.Text = btn.Text;
						mnu.Image = btn.Image;
						mnu.Tag = btn;
						mnu.CheckOnClick = true;
						mnu.Click += MnuClicked;
						mnu.Checked = (SelectedButton == btn);
						mnu.Enabled = btn.Enabled;
					contextMenuStrip.Items.Add(mnu);
					}
				}

			contextMenuStrip.Show(this, new Point(Width, Height - (InternalButtonHeight / 2)));
		}

		/// <summary></summary>
		private void ShowMoreButtons(object sender, System.EventArgs e)
		{
			Height += InternalButtonHeight;
		}

		/// <summary></summary>
		private void ShowFewerButtons(object sender, System.EventArgs e)
		{
			Height -= InternalButtonHeight;
		}

		/// <summary></summary>
		private void NavigationPaneOptions(object sender, System.EventArgs e)
		{
			m_rightClickedButton = null;
			m_hoveringButton = null;
			Invalidate();
			using (NavPaneOptionsDlg frm = new NavPaneOptionsDlg(Buttons))
				frm.ShowDialog();
			Invalidate();
		}

		/// <summary></summary>
		private void ToggleVisible(object sender, System.EventArgs e)
		{
			OutlookBarButton btn = ((ToolStripMenuItem)sender).Tag as OutlookBarButton;
			btn.Visible = !btn.Visible;
			Invalidate();
		}

		/// <summary></summary>
		private void MnuClicked(object sender, System.EventArgs e)
		{
			m_selectedButton = ((ToolStripMenuItem)sender).Tag as OutlookBarButton;
			if (ButtonClicked != null)
				ButtonClicked(this, m_selectedButton);
		}

		#endregion
	}
}
