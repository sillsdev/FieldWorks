// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LabelButton.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implements a simple button-like control derived from the panel control. For the
	/// applications where I use this control, I was using a button control, but that was
	/// giving me some undesired focus problems when the button was the child of other
	/// non-form controls.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LabelButton : Label, IFWDisposable
	{
		/// <summary>Event which occurs when the control's background is being painted
		/// (includes the border).</summary>
		public event PaintEventHandler PaintBackground;
		/// <summary>Event which occurs when the control's text is being painted.</summary>
		public event PaintEventHandler PaintText;
		/// <summary>Event which occurs when the control's image is being painted.</summary>
		public event PaintEventHandler PaintImage;

		private bool m_mouseIsOver = false;
		private bool m_mouseIsDown = false;
		private bool m_canToggle = false;
		private bool m_shadeWhenMouseOver = true;
		private bool m_textIsClipped;
		private int m_textLeadingMargin = 0;
		private StringFormat m_stringFormat;
		private ButtonState m_state = ButtonState.Normal;
		private PaintState m_paintState = PaintState.Normal;

		/// <summary>The various states that cause different painting behavior.</summary>
		protected enum PaintState
		{
			/// <summary>A button control should be painted normally, as though it's
			/// not pushed and the mouse isn't over it.</summary>
			Normal,

			/// <summary>A button control should be painted as though it's pushed and the
			/// mouse is over it.</summary>
			MouseDown,

			/// <summary>A button control should be painted as though it's not pushed and the
			/// mouse is over it.</summary>
			MouseOver,

			/// <summary>A button control should be painted as though it's pushed and the
			/// mouse is not over it (this is for buttons that can toggle)</summary>
			Pushed
		};

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a PanelButton
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LabelButton()
		{
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
				ControlStyles.DoubleBuffer, true);

			this.BackColor = Color.FromArgb(200, SystemColors.Control);
			this.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.Name = "LabelButton";
			this.ResizeRedraw = true;

			SetTextAlignment();
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

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the StringFormat object used to draw the text on the PanelButton.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringFormat TextFormat
		{
			get
			{
				CheckDisposed();

				return m_stringFormat;
			}
			set
			{
				CheckDisposed();

				if (value != null)
					m_stringFormat = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value for the number of pixels of margin to insert before the text
		/// (when right-to-left is specified, then the margin is on the right side. Otherwise
		/// it's on the left).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int TextLeadingMargin
		{
			get
			{
				CheckDisposed();

				return m_textLeadingMargin;
			}
			set
			{
				CheckDisposed();

				m_textLeadingMargin = (value >= 0 ? value : 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the text on the button didn't fit in it's
		/// display rectangle and had to be clipped.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool TextIsClipped
		{
			get
			{
				CheckDisposed();

				return m_textIsClipped;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not a button acts like a toggle button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CanToggle
		{
			get
			{
				CheckDisposed();

				return m_canToggle;
			}
			set
			{
				CheckDisposed();

				m_canToggle = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the state (i.e. normal or pushed) of the button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ButtonState State
		{
			get
			{
				CheckDisposed();

				return m_state;
			}
			set
			{
				CheckDisposed();

				if (m_state != value)
				{
					m_state = value;
					this.Invalidate();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the button get's shaded when the
		/// mouse moves over it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShadeWhenMouseOver
		{
			get
			{
				CheckDisposed();

				return m_shadeWhenMouseOver;
			}
			set
			{
				CheckDisposed();

				if (m_shadeWhenMouseOver != value)
				{
					m_shadeWhenMouseOver = value;
					this.Invalidate();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool ButtonIsOn
		{
			get
			{
				CheckDisposed();

				return State == ButtonState.Pushed;
			}
		}

		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			if (PaintBackground == null)
				OnPaintBackground(e);
			else
				PaintBackground(this, e);

			if (PaintText == null)
				OnPaintText(e);
			else
				PaintText(this, e);

			if (this.Image != null)
			{
				if (PaintImage == null)
					OnPaintImage(e);
				else
					PaintImage(this, e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paints the background and border portion of the button.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected new virtual void OnPaintBackground(PaintEventArgs e)
		{
			DeterminePaintState();

			// Fill with white first, before filling with partially transparent background color.
			SolidBrush brush = new SolidBrush(Color.White);
			e.Graphics.FillRectangle(brush, this.ClientRectangle);

			brush.Color = GetBackColorShade(m_paintState, this.BackColor);
			e.Graphics.FillRectangle(brush, this.ClientRectangle);

			if (m_paintState != PaintState.Normal)
			{
				Rectangle rc = this.ClientRectangle;
				rc.Width--;
				rc.Height--;
				e.Graphics.DrawRectangle(new Pen(new SolidBrush(SystemColors.ActiveCaption)), rc);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paints the Text on the buttons.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnPaintText(PaintEventArgs e)
		{
			Rectangle rc = this.ClientRectangle;
			SolidBrush brush = new SolidBrush(Enabled ? ForeColor : SystemColors.GrayText);

			// If the mouse is over the button then give the text a raised look.
			if (m_paintState == PaintState.MouseOver)
				rc.Offset(-1, -1);
			else
				rc.Height--;

			// Account for any specified leading margin.
			if (TextLeadingMargin > 0)
			{
				rc.Width -= TextLeadingMargin;
				if (this.RightToLeft == RightToLeft.No)
					rc.X += TextLeadingMargin;
			}

			// Now we'll draw the text.
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			e.Graphics.DrawString(Text, Font, brush, rc, TextFormat);

			// Check if the text was clipped.
			Size sz =
				e.Graphics.MeasureString(Text, Font, new Point(rc.X, rc.Y), TextFormat).ToSize();

			m_textIsClipped = (sz.Width > rc.Width || sz.Height > rc.Height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paints the image on the buttons.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnPaintImage(PaintEventArgs e)
		{
			Rectangle rc = this.ClientRectangle;

			// If the mouse is over the button then give the image a raised look.
			if (m_paintState == PaintState.MouseOver)
			{
				rc.Offset(-1, -1);
				rc.Height++;
			}

			this.DrawImage(e.Graphics, this.Image, rc, this.ImageAlign);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			m_mouseIsOver = true;
			this.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			m_mouseIsOver = false;
			this.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set's the button's state and invalidates it to force redrawing.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (e.Button == MouseButtons.Left)
			{
				m_mouseIsDown = true;
				this.Invalidate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set's the button's state and invalidates it to force redrawing.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			if (e.Button == MouseButtons.Left)
			{
				m_mouseIsDown = false;
				m_mouseIsOver = MouseInBounds(e.X, e.Y);

				if (m_mouseIsOver && CanToggle)
				{
					State =	(State == ButtonState.Normal ?
						ButtonState.Pushed : ButtonState.Normal);
				}

				this.Invalidate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (m_mouseIsDown)
			{
				bool inbounds = MouseInBounds(e.X, e.Y);

				if (inbounds != m_mouseIsOver)
				{
					m_mouseIsOver = inbounds;
					this.Invalidate();
				}
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected Color GetBackColorShade(PaintState state)
		{
			return GetBackColorShade(state, SystemColors.Control);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="state"></param>
		/// <param name="normalBack"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected Color GetBackColorShade(PaintState state, Color normalBack)
		{
			if (this.Enabled)
			{
				switch (state)
				{
					case PaintState.MouseOver:	return Color.FromArgb(50, SystemColors.ActiveCaption);
					case PaintState.MouseDown: 	return Color.FromArgb(65, SystemColors.ActiveCaption);
					case PaintState.Pushed:		return Color.FromArgb(40, SystemColors.ActiveCaption);
				}
			}

			return normalBack;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private void DeterminePaintState()
		{
			if ((m_mouseIsDown && m_mouseIsOver && ButtonIsOn) ||
				(m_mouseIsDown && m_mouseIsOver) ||
				(m_mouseIsDown && ButtonIsOn) ||
				(m_mouseIsOver && ButtonIsOn))
			{
				m_paintState = PaintState.MouseDown;
			}
			else if (m_mouseIsDown || m_mouseIsOver && ShadeWhenMouseOver)
				m_paintState = PaintState.MouseOver;
			else if (ButtonIsOn)
				m_paintState = PaintState.Pushed;
			else
				m_paintState = PaintState.Normal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool MouseInBounds(int x, int y)
		{
			return (x >= 0 && y >= 0 && x < Width && y < Height);
		}

		private void SetTextAlignment()
		{
			m_stringFormat = new StringFormat(StringFormat.GenericTypographic);
			m_stringFormat.Alignment = StringAlignment.Center;
			m_stringFormat.LineAlignment = StringAlignment.Center;
			m_stringFormat.Trimming = StringTrimming.EllipsisCharacter;
			m_stringFormat.FormatFlags |= StringFormatFlags.NoWrap;
		}
	}
}
