// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwComboBox.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// We override ComboBox because of a bug that allows the SelectedIndex of a ComboBox to be
	/// -1 under certain conditions.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwOverrideComboBox : ComboBox, IFWDisposable
	{
		private int m_lastSelectedIndex = 0;
		private bool m_treatSpaceProplerly = false;	// treat space as expected [existing is false]

		/// <summary>
		///
		/// </summary>
		public FwOverrideComboBox()
			: base()
		{
			this.Enter += new System.EventHandler(this.OnEnter);
		}

		private void OnEnter(object sender, EventArgs e)
		{
			int maxStringLength = 0;
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i].ToString().Length > maxStringLength)
				{
					maxStringLength = Items[i].ToString().Length;
				}
			}
			int factor = 6;
			if (maxStringLength > 0 && DropDownWidth < maxStringLength * factor)
				DropDownWidth = maxStringLength * factor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call this method when you want the space character to be passed into the underlying
		/// control when the style isn't a DropDown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowSpaceInEditBox
		{
			get { return m_treatSpaceProplerly; }
			set { m_treatSpaceProplerly = value; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.ComboBox.SelectedIndexChanged"></see>
		/// event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSelectedIndexChanged(EventArgs e)
		{
			base.OnSelectedIndexChanged(e);
			m_lastSelectedIndex = base.SelectedIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.TextChanged"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDropDownClosed(EventArgs e)
		{
			base.OnDropDownClosed(e);
			if (base.SelectedIndex == -1 && DropDownStyle == ComboBoxStyle.DropDownList &&
				m_lastSelectedIndex >= 0 && m_lastSelectedIndex < base.Items.Count)
			{
				base.SelectedIndex = m_lastSelectedIndex;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == ' ')
			{
				if (this.DropDownStyle == ComboBoxStyle.DropDownList)
					this.DroppedDown = true;
				e.Handled = true;
				// DanH - I think the above code is a BUG. As it causes the space character
				// to get eatten with out ever making it into the base control. That means
				// one can not enter a space character into the text of the edit box of the
				// control. This seems like the wrong default action. But, I'm leaving it as
				// I don't know the history. I'm adding a flag to allow the control to work
				// as expected.
				// AllowSpaceInEditBox = true in owning classes to correct this space problem.
				if (AllowSpaceInEditBox)
				{
					e.Handled = false;
					base.OnKeyPress(e);
				}
			}
			else
			{
				base.OnKeyPress(e);
			}
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
	}
}
