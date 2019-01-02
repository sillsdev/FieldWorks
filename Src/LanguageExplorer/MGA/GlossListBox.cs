// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace LanguageExplorer.MGA
{
	/// <summary />
	public class GlossListBox : ListBox
	{
		private MGADialog m_MGAForm;

		public MGADialog MGADialog
		{
			get { return m_MGAForm; }
			set
			{
				if (m_MGAForm != null)
				{
					Detach();
				}
				m_MGAForm = value;
				if (m_MGAForm == null)
				{
					return;
				}
				m_MGAForm.InsertMGAGlossListItem += OnInsertItem;
				m_MGAForm.RemoveMGAGlossListItem += OnRemoveItem;
				m_MGAForm.MoveDownMGAGlossListItem += OnMoveDownItem;
				m_MGAForm.MoveUpMGAGlossListItem += OnMoveUpItem;
			}
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			base.Dispose(disposing);
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
			var tmp = Items[SelectedIndex];
			Items[SelectedIndex] = Items[SelectedIndex + 1];
			Items[SelectedIndex + 1] = tmp;
			SelectedIndex = SelectedIndex + 1;
		}

		private void OnMoveUpItem(object sender, EventArgs e)
		{
			var tmp = Items[SelectedIndex];
			Items[SelectedIndex] = Items[SelectedIndex - 1];
			Items[SelectedIndex - 1] = tmp;
			SelectedIndex = SelectedIndex - 1;
		}

		public void Detach()
		{
			// Detach the events and delete the form
			m_MGAForm.InsertMGAGlossListItem -= OnInsertItem;
			m_MGAForm.RemoveMGAGlossListItem -= OnRemoveItem;
			m_MGAForm.MoveDownMGAGlossListItem -= OnMoveDownItem;
			m_MGAForm.MoveUpMGAGlossListItem -= OnMoveUpItem;
			m_MGAForm = null;
		}

		/// <summary>
		/// See if the selected item conflicts with any item already inserted into the gloss list box
		/// </summary>
		/// <param name="glbiNew">new item to be checked for</param>
		/// <param name="glbiConflict"></param>
		/// <returns>true if there is a conflict; false otherwise</returns>
		/// <remarks>Is internal for testing</remarks>
		internal bool NewItemConflictsWithExtantItem(GlossListBoxItem glbiNew, out GlossListBoxItem glbiConflict)
		{
			glbiConflict = null;
			if (!glbiNew.IsValue)
			{
				return false; // only terminal nodes will conflict
			}
			foreach (GlossListBoxItem item in Items)
			{
				if (!item.IsValue)
				{
					continue;
				}
				// when they are values and have the same parent, they conflict
				if (item.XmlNode.ParentNode != glbiNew.XmlNode.ParentNode)
				{
					continue;
				}
				glbiConflict = item;
				return true;
			}
			return false;
		}
	}
}