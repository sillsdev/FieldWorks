// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// For some reason the embedded text box never gets a layout unless we do this.
	/// </summary>
	public class ComboTextBox : InnerFwTextBox
	{
		FwComboBoxBase m_comboBox;

		internal ComboTextBox(FwComboBoxBase comboBox)
		{
			m_comboBox = comboBox;
			// Allows it to be big unless client shrinks it.
			Font = new Font(Font.Name, (float)100.0);
			if (!Platform.IsMono && Application.RenderWithVisualStyles)
			{
				DoubleBuffered = true;
				BackColor = Color.Transparent;
				return;
			}
			// And, if not changed, it's background color is white.
			BackColor = SystemColors.Window;
		}

		/// <summary>
		/// Override to prevent scrolling in DropDownList mode.
		/// </summary>
		public override bool ScrollSelectionIntoView(IVwSelection sel, VwScrollSelOpts scrollOption)
		{
			if (m_comboBox != null && m_comboBox.DropDownStyle == ComboBoxStyle.DropDownList)
			{
				// no meaningful selections are possible, no reason ever to scroll it.

				// That's true as long as we always left-align.
				// If we use right-alignment with a huge width to prevent wrapping, no scrolling means the text is invisible,
				// somewhere way off to the right. We'd then need something like this, but better, because this doesn't
				// work when the combo is resized, as when you change the size of a column and the filter bar combo resizes.
				// See also what I did in OnSizeChanged.
				if (Rtl)
				{
					// But, if it is RTL, we need to scroll, typically only once, to make it as visible as possible.
					// Right alignment otherwise puts the string way off to the right.
					var initialSel = m_rootb.MakeSimpleSel(true, false, false, false);
					base.ScrollSelectionIntoView(initialSel, VwScrollSelOpts.kssoDefault);
				}
				return false;
			}
			return base.ScrollSelectionIntoView(sel, scrollOption);
		}

		/// <summary>
		/// We need to kludge to make sure the content stays visible in RTL scripts.
		/// </summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (m_rootb == null || !Rtl)
			{
				return;
			}
			// To get the text aligned as well as we readily can, first scroll to show all of it,
			// then if need be again to see the start.
			BaseMakeSelectionVisible(m_rootb.MakeSimpleSel(true, false, true, false));
			BaseMakeSelectionVisible(m_rootb.MakeSimpleSel(true, false, false, false));
		}

		internal override void RemoveNonRootNotifications()
		{
			base.RemoveNonRootNotifications();
			DataAccess.RemoveNotification(this);
			DataAccess.RemoveNotification(m_comboBox);
		}

		internal override void RestoreNonRootNotifications()
		{
			base.RestoreNonRootNotifications();
			DataAccess.AddNotification(this);
			DataAccess.AddNotification(m_comboBox);
		}

		/// <summary>
		/// Raises the <see cref="E:Paint"/> event.
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (Application.RenderWithVisualStyles)
			{
				var renderer = new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Normal);
				renderer.DrawParentBackground(e.Graphics, ClientRectangle, this);
			}
			base.OnPaint(e);
		}

		/// <summary>
		/// Stupid required comment!
		/// </summary>
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			PerformLayout();
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.MouseEnter"/> event.
		/// </summary>
		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Pressed && m_comboBox.State != ComboBoxState.Disabled)
			{
				m_comboBox.State = ComboBoxState.Hot;
			}
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.MouseLeave"/> event.
		/// </summary>
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Pressed && m_comboBox.State != ComboBoxState.Disabled)
			{
				m_comboBox.State = ComboBoxState.Normal;
			}
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.MouseUp"/> event.
		/// </summary>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (Application.RenderWithVisualStyles && m_comboBox.State != ComboBoxState.Pressed && m_comboBox.State != ComboBoxState.Disabled)
			{
				m_comboBox.State = ComboBoxState.Hot;
			}
		}
	}
}