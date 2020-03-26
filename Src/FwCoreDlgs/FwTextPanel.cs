// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Extends the panel control to support text, including text containing mnemonic specifiers.
	/// </summary>
	internal class FwTextPanel : Panel
	{
		private TextFormatFlags m_txtFmtFlags = TextFormatFlags.VerticalCenter |
				TextFormatFlags.WordEllipsis | TextFormatFlags.SingleLine |
				TextFormatFlags.LeftAndRightPadding | TextFormatFlags.HidePrefix |
				TextFormatFlags.PreserveGraphicsClipping;
		private Rectangle m_rcText;

		/// <summary />
		internal FwTextPanel()
		{
			DoubleBuffered = true;
			SetStyle(ControlStyles.UseTextForAccessibility, true);
			m_rcText = ClientRectangle;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}

		/// <inheritdoc />
		protected override bool ProcessMnemonic(char charCode)
		{
			if (IsMnemonic(charCode, Text) && Parent != null)
			{
				if (MnemonicGeneratesClick)
				{
					InvokeOnClick(this, EventArgs.Empty);
					return true;
				}

				if (ControlReceivingFocusOnMnemonic != null)
				{
					ControlReceivingFocusOnMnemonic.Focus();
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

		/// <inheritdoc />
		[Browsable(true)]
		public override string Text
		{
			get => base.Text;
			set
			{
				base.Text = value;
				CalculateTextRectangle();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not the control process the keyboard
		/// mnemonic as a click (like a button) or passes control on to the next control in
		/// the tab order (like a label).
		/// </summary>
		[Browsable(true)]
		internal bool MnemonicGeneratesClick { get; set; } = false;

		/// <summary>
		/// Gets or sets the control that receives focus when the label's text is contains a
		/// mnemonic specifier. When this value is null, then focus is given to the next
		/// control in the tab order.
		/// </summary>
		internal Control ControlReceivingFocusOnMnemonic { get; set; } = null;

		/// <summary>
		/// Gets or sets the text format flags used to draw the header label's text.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal TextFormatFlags TextFormatFlags
		{
			get => m_txtFmtFlags;
			set
			{
				m_txtFmtFlags = value;
				CalculateTextRectangle();
			}
		}

		/// <summary />
		internal bool ClipTextForChildControls { get; set; } = true;

		/// <summary>
		/// Calculates the rectangle of the text when there are child controls. This method
		/// assumes that controls to the right of the text should clip the text. However, if
		/// the controls are above and below the text, this method will probably screw up
		/// the text drawing.
		/// </summary>
		private void CalculateTextRectangle()
		{
			m_rcText = ClientRectangle;
			if (ClipTextForChildControls)
			{
				var rightExtent = (from Control child in Controls select child.Left).Concat(new[] { m_rcText.Right }).Min();
				if (rightExtent != m_rcText.Right && m_rcText.Contains(new Point(rightExtent, m_rcText.Top + m_rcText.Height / 2)))
				{
					m_rcText.Width -= m_rcText.Right - rightExtent;
					// Give a bit more to account for the
					if ((m_txtFmtFlags & TextFormatFlags.LeftAndRightPadding) > 0)
					{
						m_rcText.Width += 8;
					}
				}
			}

			Invalidate();
		}

		/// <inheritdoc />
		protected override void OnControlAdded(ControlEventArgs e)
		{
			base.OnControlAdded(e);
			CalculateTextRectangle();

			e.Control.Resize += ChildControl_Resize;
			e.Control.LocationChanged += ChildControl_LocationChanged;
		}

		/// <inheritdoc />
		protected override void OnControlRemoved(ControlEventArgs e)
		{
			e.Control.Resize -= ChildControl_Resize;
			e.Control.LocationChanged -= ChildControl_LocationChanged;

			base.OnControlRemoved(e);
			CalculateTextRectangle();
		}

		/// <summary />
		private void ChildControl_LocationChanged(object sender, EventArgs e)
		{
			CalculateTextRectangle();
		}

		/// <summary />
		private void ChildControl_Resize(object sender, EventArgs e)
		{
			CalculateTextRectangle();
		}

		/// <inheritdoc />
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			CalculateTextRectangle();
		}

		/// <inheritdoc />
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (!string.IsNullOrEmpty(Text))
			{
				TextRenderer.DrawText(e.Graphics, Text, Font, m_rcText, SystemColors.ControlText, m_txtFmtFlags);
			}
		}
	}
}
