// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// We override ComboBox because of a bug that allows the SelectedIndex of a ComboBox to be
	/// -1 under certain conditions.
	/// </summary>
	public class FwOverrideComboBox : ComboBox
	{
		private int m_lastSelectedIndex;

		/// <summary />
		public FwOverrideComboBox()
		{
			Enter += OnEnter;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (disposing)
			{
				// dispose managed objects
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
			{
				return;
			}
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
			{
				return;
			}
			DisposeItems();

			if (Text.Length > 0)
			{
				Text = string.Empty;
			}
			Items.Clear();
		}

		private void OnEnter(object sender, EventArgs e)
		{
			var maxStringLength = 0;
			foreach (var item in Items)
			{
				if (item.ToString().Length > maxStringLength)
				{
					maxStringLength = item.ToString().Length;
				}
			}
			const int factor = 6;
			var maxwidth = maxStringLength * factor;
			if (maxStringLength > 0 && DropDownWidth < maxwidth)
			{
				DropDownWidth = maxwidth;
			}
		}

		/// <summary>
		/// Call this method when you want the space character to be passed into the underlying
		/// control when the style isn't a DropDown.
		/// </summary>
		public bool AllowSpaceInEditBox { get; set; }

		/// <inheritdoc />
		protected override void OnSelectedIndexChanged(EventArgs e)
		{
			base.OnSelectedIndexChanged(e);
			m_lastSelectedIndex = base.SelectedIndex;
		}

		/// <inheritdoc />
		protected override void OnDropDownClosed(EventArgs e)
		{
			base.OnDropDownClosed(e);
			if (base.SelectedIndex == -1 && DropDownStyle == ComboBoxStyle.DropDownList && m_lastSelectedIndex >= 0 && m_lastSelectedIndex < base.Items.Count)
			{
				base.SelectedIndex = m_lastSelectedIndex;
			}
		}

		/// <inheritdoc />
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == ' ')
			{
				if (DropDownStyle == ComboBoxStyle.DropDownList)
				{
					DroppedDown = true;
				}
				e.Handled = true;
				// DanH - I think the above code is a BUG. As it causes the space character
				// to get eaten without ever making it into the base control. That means
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
	}
}