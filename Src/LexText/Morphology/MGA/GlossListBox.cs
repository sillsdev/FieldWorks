// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: GLossListBox.cs
// Responsibility: Andy Black

using System;
using System.Windows.Forms;

using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls.MGA
{
	/// <summary>
	/// Summary description for GlossListBox.
	/// </summary>
	public class GlossListBox : ListBox, IFWDisposable
	{
		private MGADialog m_MGAForm;

		public MGADialog MGADialog
		{
			get { return m_MGAForm; }
			set
			{
				CheckDisposed();
				if (m_MGAForm != null)
					Detach();

				m_MGAForm = value;
				if (m_MGAForm != null)
				{
					m_MGAForm.InsertMGAGlossListItem += new GlossListEventHandler(OnInsertItem);
					m_MGAForm.RemoveMGAGlossListItem += new EventHandler(OnRemoveItem);
					m_MGAForm.MoveDownMGAGlossListItem += new EventHandler(OnMoveDownItem);
					m_MGAForm.MoveUpMGAGlossListItem += new EventHandler(OnMoveUpItem);
				}
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

		private void OnInsertItem(object sender, GlossListEventArgs glea)
		{
			// Adds the GlossListBoxItem contained within the GlossListEventArgs object
			Items.Add(glea.GlossListBoxItem);
		}
		private void OnRemoveItem(object sender, EventArgs e)
		{
			Items.Remove(SelectedItem);
		}
		private void OnMoveDownItem(object sender, EventArgs e)
		{
			Object tmp = Items[SelectedIndex];
			Items[SelectedIndex] = Items[SelectedIndex + 1];
			Items[SelectedIndex + 1] = tmp;
			SelectedIndex = SelectedIndex + 1;
		}
		private void OnMoveUpItem(object sender, EventArgs e)
		{
			Object tmp = Items[SelectedIndex];
			Items[SelectedIndex] = Items[SelectedIndex - 1];
			Items[SelectedIndex - 1] = tmp;
			SelectedIndex = SelectedIndex - 1;
		}
		public void Detach()
		{
			CheckDisposed();

			// Detach the events and delete the form
			m_MGAForm.InsertMGAGlossListItem -= new GlossListEventHandler(OnInsertItem);
			m_MGAForm.RemoveMGAGlossListItem -= new EventHandler(OnRemoveItem);
			m_MGAForm.MoveDownMGAGlossListItem -= new EventHandler(OnMoveDownItem);
			m_MGAForm.MoveUpMGAGlossListItem -= new EventHandler(OnMoveUpItem);
			m_MGAForm = null;
		}
		/// <summary>
		/// See if the selected item conflicts with any item already inserted into the gloss list box
		/// </summary>
		/// <param name="glbiNew">new item to be checked for</param>
		/// <returns>true if there is a conflict; false otherwise</returns>
		/// <remarks>Is public for testing</remarks>
		public bool NewItemConflictsWithExtantItem(GlossListBoxItem glbiNew, out GlossListBoxItem glbiConflict)
		{
			CheckDisposed();

			glbiConflict = null;
			if (!glbiNew.IsValue)
				return false; // only terminal nodes will conflict
			foreach (GlossListBoxItem item in Items)
			{
				if (item.IsValue)
				{ // when they are values and have the same parent, they conflict
					if (item.XmlNode.ParentNode == glbiNew.XmlNode.ParentNode)
					{
						glbiConflict = item;
						return true;
					}
				}
			}
			return false;
		}
	}
}