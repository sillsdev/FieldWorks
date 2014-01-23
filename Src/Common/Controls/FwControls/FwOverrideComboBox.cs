// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwComboBox.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SIL.Utils;

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
		private int m_lastSelectedIndex;

		/// <summary/>
		public FwOverrideComboBox()
		{
			Enter += OnEnter;
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (disposing)
			{
				// dispose managed and unmanaged objects
				DisposeItems();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Dispose of the items, doesn't clear the list box but disposes of all it's contents.
		/// </summary>
		private void DisposeItems()
		{
			// Items collection cannot be modified when the DataSource property is set.
			if (DataSource != null)
				return;
			foreach (var disposable in Items.OfType<IDisposable>())
			{
				disposable.Dispose();
			}
		}

		/// <summary>
		/// Clear the Items collection. Clients should call this method rather than
		/// Items.Clear() directly because this method takes care of disposing
		/// collection elements.
		/// </summary>
		public void ClearItems()
		{
			// Items collection cannot be modified when the DataSource property is set.
			if (DataSource != null)
				return;

			DisposeItems();

			if(Text.Length > 0)
			{
				Text = "";
			}
			Items.Clear();
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
			int maxwidth = maxStringLength * factor;
			if (maxStringLength > 0 && DropDownWidth < maxwidth)
				DropDownWidth = maxwidth;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call this method when you want the space character to be passed into the underlying
		/// control when the style isn't a DropDown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowSpaceInEditBox { get; set; }

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
