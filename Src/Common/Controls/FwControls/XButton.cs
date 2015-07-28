// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XButton : Label, IFWDisposable
	{
		private bool m_drawLeftArrowButton = false;
		private bool m_drawRightArrowButton = false;
		private bool m_drawEmpty = false;
		private bool m_canBeChecked = false;
		private bool m_checked = false;
		private bool m_mouseDown = false;
		private bool m_mouseOver = false;
		private PaintState m_state = PaintState.Normal;
		private Image m_disabledImage;
		private TextFormatFlags m_txtFmtflags = TextFormatFlags.NoPadding |
				TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix |
				TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XButton()
		{
			AutoSize = false;
			BackColor = SystemColors.Control;
#if __MonoCS__
			// Linux doesn't have the Marlett font -- do we need to pick a special one to get
			// Unicode dingbats?  (In practice, the only use of XButton in FieldWorks is in the
			// color picker, and it never uses a path that tries to draw the dingbats.)
			Font = new Font("OpenSymbol", 9, GraphicsUnit.Point);
#else
			Font = new Font("Marlett", 9, GraphicsUnit.Point);
#endif
			Size = new Size(16, 16);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the button's checked state changes
		/// when clicked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CanBeChecked
		{
			get
			{
				CheckDisposed();

				return m_canBeChecked;
			}
			set
			{
				CheckDisposed();

				m_canBeChecked = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the button is checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Checked
		{
			get
			{
				CheckDisposed();

				return m_checked;
			}
			set
			{
				CheckDisposed();

				if (m_canBeChecked)
				{
					m_checked = value;
					Invalidate();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not an X is drawn on the button when no
		/// image, text or arrow direction is specified. By default, when no image, text or
		/// arrow direction is specified, the button is drawn with an X (like a close window-
		/// type of X). However, when DrawEmtpy is true, nothing will be drawn except the
		/// highlighted look given when the mouse is over or down or when the button is checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DrawEmpty
		{
			get
			{
				CheckDisposed();

				return m_drawEmpty;
			}
			set
			{
				CheckDisposed();

				m_drawEmpty = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new Image Image
		{
			get
			{
				CheckDisposed();

				return base.Image;
			}
			set
			{
				CheckDisposed();

				base.Image = value;
				m_disabledImage = (value == null ?
					null : ColorHelper.MakeDisabledImage(value, SystemColors.Control));

				if (value != null)
				{
					m_drawLeftArrowButton = false;
					m_drawRightArrowButton = false;
				}

				OnSystemColorsChanged(null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the button should be drawn with a
		/// left pointing arrow (like the left button of a horizontal scrollbar).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DrawLeftArrowButton
		{
			get
			{
				CheckDisposed();

				return m_drawLeftArrowButton;
			}
			set
			{
				CheckDisposed();

				m_drawLeftArrowButton = value;
				if (value)
				{
					m_drawRightArrowButton = false;
					base.Image = null;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the button should be drawn with a
		/// right pointing arrow (like the right button of a horizontal scrollbar).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DrawRightArrowButton
		{
			get
			{
				CheckDisposed();

				return m_drawRightArrowButton;
			}
			set
			{
				CheckDisposed();

				m_drawRightArrowButton = value;
				if (value)
				{
					m_drawLeftArrowButton = false;
					base.Image = null;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSystemColorsChanged(EventArgs e)
		{
			base.OnSystemColorsChanged(e);
			if (Image != null)
				BackColor = Color.Transparent;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Repaint the button when the mouse isn't over it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			m_mouseOver = false;
			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change appearance when mouse is pressed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (e.Button == MouseButtons.Left)
			{
				m_mouseDown = true;
				Invalidate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change appearance when the mouse button is released.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			if (e.Button == MouseButtons.Left)
			{
				m_mouseDown = false;
				Invalidate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Track when the mouse leaves the control when a mouse button is pressed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			m_mouseOver = ClientRectangle.Contains(e.Location);
			PaintState newState = (m_mouseOver ? PaintState.Hot : PaintState.Normal);

			if (m_mouseOver && m_mouseDown)
				newState = PaintState.HotDown;

			if (newState != m_state)
			{
				m_state = newState;
				Invalidate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			base.OnPaintBackground(e);

			if (m_mouseOver || Checked)
				m_state = (m_mouseDown ? PaintState.HotDown : PaintState.Hot);
			else
				m_state = PaintState.Normal;

			Rectangle rc = ClientRectangle;

			using (SolidBrush br = new SolidBrush(BackColor))
				e.Graphics.FillRectangle(br, rc);

			if (m_state != PaintState.Normal)
				PaintingHelper.DrawHotBackground(e.Graphics, rc, m_state);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			if (Image != null)
				DrawWithImage(e);
			else if (m_drawLeftArrowButton || m_drawRightArrowButton)
				DrawArrow(e);
			else if (!string.IsNullOrEmpty(Text))
				DrawText(e);
			else if (!m_drawEmpty)
				DrawWithX(e);
			else
				base.OnPaint(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the button's text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawText(PaintEventArgs e)
		{
			TextFormatFlags flags = TextFormatFlags.NoPrefix |
				TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
				TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

			Color clr = (Enabled ? ForeColor : SystemColors.GrayText);
			TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, clr, flags);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the button with text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawWithX(PaintEventArgs e)
		{
			Rectangle rc = ClientRectangle;

			Color clr = (m_state == PaintState.Normal ? SystemColors.ControlDarkDark :
				SystemColors.ControlText);

#if __MonoCS__
			// Linux doesn't have the Marlett font, so use a standard Unicode dingbat here.
			TextRenderer.DrawText(e.Graphics, "\u2573", Font, rc, clr, m_txtFmtflags);
#else
			// The 'r' in the Marlett font is the close button symbol 'X'
			TextRenderer.DrawText(e.Graphics, "r", Font, rc, clr, m_txtFmtflags);
#endif
			// Draw the border around the button.
			rc.Width--;
			rc.Height--;
			using (Pen pen = new Pen(clr))
				e.Graphics.DrawRectangle(pen, rc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the button with an image.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawWithImage(PaintEventArgs e)
		{
			Image img = (Enabled ? Image : m_disabledImage);
			if (img != null)
			{
				int x = (Width - img.Width) / 2;
				int y = (Height - img.Height) / 2;
				Rectangle rc = new Rectangle(x, y, img.Width, img.Height);
				e.Graphics.DrawImageUnscaledAndClipped(img, rc);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the button with an image.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawArrow(PaintEventArgs e)
		{
			Rectangle rc = ClientRectangle;

			// If possible, render the button with visual styles. Otherwise,
			// paint the plain Windows 2000 push button.
			VisualStyleElement element = GetCorrectVisualStyleArrowElement();
			if (PaintingHelper.CanPaintVisualStyle(element))
			{
				VisualStyleRenderer renderer = new VisualStyleRenderer(element);
				renderer.DrawParentBackground(e.Graphics, rc, this);
				renderer.DrawBackground(e.Graphics, rc);
				return;
			}

			if (Font.SizeInPoints != 12)
				Font = new Font(Font.FontFamily, 12, GraphicsUnit.Point);

			ControlPaint.DrawButton(e.Graphics, rc,
				(m_state == PaintState.HotDown ? ButtonState.Pushed : ButtonState.Normal));

#if __MonoCS__
			// Linux doesn't have the Marlett font, so use standard Unicode dingbats here.
			string arrowGlyph = (m_drawLeftArrowButton ? "\u25C4" : "\u25BA");
#else
			// In the Marlett font, '3' is the left arrow and '4' is the right.
			string arrowGlyph = (m_drawLeftArrowButton ? "3" : "4");
#endif
			Color clr = (Enabled ? SystemColors.ControlText : SystemColors.GrayText);
			TextRenderer.DrawText(e.Graphics, arrowGlyph, Font, rc,	clr, m_txtFmtflags);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the correct visual style arrow button and in the correct state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private VisualStyleElement GetCorrectVisualStyleArrowElement()
		{
			if (m_drawLeftArrowButton)
			{
				if (!Enabled)
					return VisualStyleElement.Spin.DownHorizontal.Disabled;

				if (m_state == PaintState.Normal)
					return VisualStyleElement.Spin.DownHorizontal.Normal;

				return (m_state == PaintState.Hot ?
						VisualStyleElement.Spin.DownHorizontal.Hot :
						VisualStyleElement.Spin.DownHorizontal.Pressed);
			}

			if (!Enabled)
				return VisualStyleElement.Spin.UpHorizontal.Disabled;

			if (m_state == PaintState.Normal)
				return VisualStyleElement.Spin.UpHorizontal.Normal;

			return (m_state == PaintState.Hot ?
					VisualStyleElement.Spin.UpHorizontal.Hot :
					VisualStyleElement.Spin.UpHorizontal.Pressed);
		}
	}
}
