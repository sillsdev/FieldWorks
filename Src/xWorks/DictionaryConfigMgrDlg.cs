// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DictionaryConfigMgrDlg.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interacts with the user and the DictionaryConfigManager to deal with the stored
	/// dictionary configurations. In the Model-View-Presenter pattern, this
	/// represents the View which displays data given to it by the DictionaryConfigManager(DCM)
	/// and reports user actions to the DCM.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class DictionaryConfigMgrDlg : Form, IDictConfigViewer
	{
		private readonly IDictConfigPresenter m_presenter;
		private string m_helpTopicId = "khtpDictConfigManager"; // use as default?
		private readonly HelpProvider m_helpProvider;
		private readonly Mediator m_mediator;
		private readonly PropertyTable m_propertyTable;
		private readonly string m_objType;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DictionaryConfigMgrDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DictionaryConfigMgrDlg(Mediator mediator, PropertyTable propertyTable, string objType, List<XmlNode> configViews, XmlNode current)
		{
			InitializeComponent();

			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_presenter = new DictionaryConfigManager(this, configViews, current);
			m_objType = objType;

			// Make a help topic ID
			m_helpTopicId = generateChooserHelpTopicID(m_objType);

			m_helpProvider = new HelpProvider
			{
				HelpNamespace = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider").HelpFile
			};
			m_helpProvider.SetHelpKeyword(this, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider").GetHelpString(m_helpTopicId));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);
		}

		/// <summary>
		/// Generates a possible help topic id from an identifying string, but does NOT check it for validity!
		/// </summary>
		/// <returns></returns>
		private static string generateChooserHelpTopicID(string fromStr)
		{
			string candidateID = "khtpManage";

			// Should we capitalize the next letter?
			bool nextCapital = true;

			// Lets turn our field into a candidate help page!
			foreach (char ch in fromStr)
			{
				if (Char.IsLetterOrDigit(ch)) // might we include numbers someday?
				{
					if (nextCapital)
						candidateID += Char.ToUpper(ch);
					else
						candidateID += ch;
					nextCapital = false;
				}
				else // unrecognized character... exclude it
					nextCapital = true; // next letter should be a capital
			}

			return candidateID;
		}

		private VisibleListItem CurrentSelectedItem
		{
			get { return m_listView.Items[m_listView.SelectedIndices[0]] as VisibleListItem; }
		}

		#region Implementation of IDictConfigViewer

		public IDictConfigPresenter Presenter
		{
			get { return m_presenter; }
		}

		/// <summary>
		/// Tuples of strings are (uniqueCode, dispName) pairs to be displayed.
		/// </summary>
		/// <param name="listItems"></param>
		/// <param name="selectedItem">The code for the item that should be selected
		/// in the dialog ListView.</param>
		public void SetListViewItems(IEnumerable<Tuple<string,string>> listItems,
			string selectedItem)
		{
			var itemList = listItems.Select(item => new VisibleListItem(item.Item1, item.Item2)).ToList();
			var selItem = CheckForValidSelectedItem(itemList, selectedItem);
			try
			{
				SetListViewItemsInternal(itemList);
			}
			finally
			{
				SetSelectedItem(selectedItem);
				m_listView.RedrawItems(0, m_listView.Items.Count - 1, true);
			}
		}

		private void SetSelectedItem(string code)
		{
			if (m_listView.Items.Count < 1)
				return; // Just in case.

			// find code in list, select that index
			var idx = -1;
			foreach (VisibleListItem item in m_listView.Items)
			{
				if (item.Code != code)
					continue;
				idx = item.Index;
				item.Selected = true;
				break;
			}
			if (idx < 0) // invalid code! select first item
				m_listView.Items[0].Selected = true;
		}

		/// <summary>
		/// The unique code for the item currently selected in the dialog listView.
		/// </summary>
		public string CurrentSelectedCode
		{
			get { return CurrentSelectedItem.Code; }
		}

		private static VisibleListItem CheckForValidSelectedItem(IEnumerable<VisibleListItem> itemList,
			string selectedItem)
		{
			var result = itemList.FirstOrDefault(item => item.Code == selectedItem);
			Debug.Assert(result != null, "Selected item does not exist in list.");
			return result;
		}

		internal void SetListViewItemsInternal(IEnumerable<VisibleListItem> listItems)
		{
			m_listView.BeginUpdate();
			m_listView.Items.Clear();
			foreach (var listItem in listItems)
				m_listView.Items.Add(listItem);
			m_listView.EndUpdate();
		}

		#endregion

		private void m_listView_BeforeLabelEdit(object sender, LabelEditEventArgs e)
		{
			if (CurrentSelectedItem == null || Presenter.IsConfigProtected(CurrentSelectedItem.Code))
				e.CancelEdit = true;
		}

		private void m_listView_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			if (e.Label == null || e.Label == CurrentSelectedItem.Name)
				return; // nothing to do!
			Presenter.RenameConfigItem(CurrentSelectedItem.Code, e.Label);
		}

		private void m_btnCopy_Click(object sender, EventArgs e)
		{
			if (CurrentSelectedItem == null)
				return; // what happened?
			Presenter.CopyConfigItem(CurrentSelectedItem.Code);
			CurrentSelectedItem.BeginEdit();
		}

		private void m_btnDelete_Click(object sender, EventArgs e)
		{
			if (CurrentSelectedItem == null)
				return; // what happened?
			var dr = MessageBox.Show(
				String.Format(xWorksStrings.ksConfirmDelete, m_objType, CurrentSelectedItem.Name),
				String.Format(xWorksStrings.ksConfirmDeleteTitle, m_objType),
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button2);
			if (dr == DialogResult.Yes)
				Presenter.TryMarkForDeletion(CurrentSelectedItem.Code);
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			Presenter.PersistState();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), m_helpTopicId);
		}

		private void m_listView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_listView.SelectedItems.Count == 0)
			{
				m_btnDelete.Enabled = false;
				m_btnCopy.Enabled = false;
				return;
			}
			var newSelection = m_listView.SelectedItems[0] as VisibleListItem;
			if (newSelection == null) // shouldn't happen if earlier check passes!
			{
				m_btnDelete.Enabled = false;
				m_btnCopy.Enabled = false;
				return;
			}

			// Only allow deleting non-protected configuration.
			m_btnDelete.Enabled = !Presenter.IsConfigProtected(newSelection.Code);

			// Only allow copying a configuration that existed before opening this dialog.
			m_btnCopy.Enabled = !Presenter.IsConfigNew(newSelection.Code);

			// The selected view should be selected if the user clicks OK.
			Presenter.FinalConfigurationView = newSelection.Code;
		}
	}

	public class VisibleListItem : ListViewItem
	{
		public VisibleListItem(string code, string name)
		{
			Tag = code;
			Name = name;
			Text = name;
		}

		public string Code { get { return Tag as string; } }
	}
}