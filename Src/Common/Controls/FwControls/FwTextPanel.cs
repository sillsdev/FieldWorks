// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Extends the panel control to support text, including text containing mnemonic
	/// specifiers.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwTextPanel : Panel
	{
		/// <summary>
		/// Event fired when the accelerator key (based on the text's mnemonic) is pressed.
		/// </summary>
		public event EventHandler MnemonicInvoked;

		private TextFormatFlags m_txtFmtFlags = TextFormatFlags.VerticalCenter |
				TextFormatFlags.WordEllipsis | TextFormatFlags.SingleLine |
				TextFormatFlags.LeftAndRightPadding | TextFormatFlags.HidePrefix |
				TextFormatFlags.PreserveGraphicsClipping;

		private bool m_mnemonicGeneratesClick = false;
		private Control m_ctrlRcvingFocusOnMnemonic = null;
		private Rectangle m_rcText;
		private bool m_clipTextForChildControls = true;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwTextPanel()
		{
			DoubleBuffered = true;
			SetStyle(ControlStyles.UseTextForAccessibility, true);
			m_rcText = ClientRectangle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the text in the header label acts like a normal label in that it
		/// responds to Alt+letter keys to send focus to the next control in the tab order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool ProcessMnemonic(char charCode)
		{
			if (IsMnemonic(charCode, Text) && Parent != null)
			{
				if (m_mnemonicGeneratesClick)
				{
					InvokeOnClick(this, EventArgs.Empty);
					return true;
				}

				if (MnemonicInvoked != null)
				{
					MnemonicInvoked(this, EventArgs.Empty);
					return true;
				}

				if (m_ctrlRcvingFocusOnMnemonic != null)
				{
					m_ctrlRcvingFocusOnMnemonic.Focus();
					return true;
				}

				Control ctrl = this;

				do
				{
					ctrl = Parent.GetNextControl(ctrl, true);
				}
				while (ctrl != null && !ctrl.CanSelect);

				if (ctrl != null)
				{
					ctrl.Focus();
					return true;
				}
			}

			return base.ProcessMnemonic(charCode);
		}

		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the header label's text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(true)]
		public override string Text
		{
			get	{return base.Text;}
			set
			{
				base.Text = value;
				CalculateTextRectangle();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the control process the keyboard
		/// mnemonic as a click (like a button) or passes control on to the next control in
		/// the tab order (like a label).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(true)]
		public bool MnemonicGeneratesClick
		{
			get { return m_mnemonicGeneratesClick; }
			set { m_mnemonicGeneratesClick = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the control that receives focus when the label's text is contains a
		/// mnumonic specifier. When this value is null, then focus is given to the next
		/// control in the tab order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Control ControlReceivingFocusOnMnemonic
		{
			get { return m_ctrlRcvingFocusOnMnemonic; }
			set { m_ctrlRcvingFocusOnMnemonic = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text format flags used to draw the header label's text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TextFormatFlags TextFormatFlags
		{
			get { return m_txtFmtFlags; }
			set
			{
				m_txtFmtFlags = value;
				CalculateTextRectangle();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ClipTextForChildControls
		{
			get { return m_clipTextForChildControls; }
			set { m_clipTextForChildControls = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the rectangle of the text when there are child controls. This method
		/// assumes that controls to the right of the text should clip the text. However, if
		/// the controls are above and below the text, this method will probably screw up
		/// the text drawing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CalculateTextRectangle()
		{
			m_rcText = ClientRectangle;

			if (m_clipTextForChildControls)
			{
				int rightExtent = m_rcText.Right;

				foreach (Control child in Controls)
					rightExtent = Math.Min(rightExtent, child.Left);

				if (rightExtent != m_rcText.Right &&
					m_rcText.Contains(new Point(rightExtent, m_rcText.Top + m_rcText.Height / 2)))
				{
					m_rcText.Width -= (m_rcText.Right - rightExtent);

					// Give a bit more to account for the
					if ((m_txtFmtFlags & TextFormatFlags.LeftAndRightPadding) > 0)
						m_rcText.Width += 8;
				}
			}

			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnControlAdded(ControlEventArgs e)
		{
			base.OnControlAdded(e);
			CalculateTextRectangle();

			e.Control.Resize += ChildControl_Resize;
			e.Control.LocationChanged += ChildControl_LocationChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnControlRemoved(ControlEventArgs e)
		{
			e.Control.Resize -= ChildControl_Resize;
			e.Control.LocationChanged -= ChildControl_LocationChanged;

			base.OnControlRemoved(e);
			CalculateTextRectangle();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ChildControl_LocationChanged(object sender, EventArgs e)
		{
			CalculateTextRectangle();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ChildControl_Resize(object sender, EventArgs e)
		{
			CalculateTextRectangle();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure to repaint when resizing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			CalculateTextRectangle();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paint the text on the panel, if there is any.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (!string.IsNullOrEmpty(Text))
			{
				TextRenderer.DrawText(e.Graphics, Text, Font, m_rcText,
					SystemColors.ControlText, m_txtFmtFlags);
			}
		}
	}
}
