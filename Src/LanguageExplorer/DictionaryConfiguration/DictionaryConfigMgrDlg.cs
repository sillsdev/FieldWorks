// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// Interacts with the user and the DictionaryConfigManager to deal with the stored
	/// dictionary configurations. In the Model-View-Presenter pattern, this
	/// represents the View which displays data given to it by the DictionaryConfigManager(DCM)
	/// and reports user actions to the DCM.
	/// </summary>
	internal sealed partial class DictionaryConfigMgrDlg : Form, IDictConfigViewer
	{
		private readonly string m_helpTopicId = "khtpDictConfigManager"; // use as default?
		private readonly HelpProvider m_helpProvider;
		private readonly IPropertyTable m_propertyTable;
		private readonly string m_objType;

		/// <summary />
		public DictionaryConfigMgrDlg(IPropertyTable propertyTable, string objType, List<XElement> configViews, XElement current)
		{
			InitializeComponent();
			m_propertyTable = propertyTable;
			Presenter = new DictionaryConfigManager(this, configViews, current);
			m_objType = objType;
			// Make a help topic ID
			m_helpTopicId = generateChooserHelpTopicID(m_objType);
			m_helpProvider = new HelpProvider
			{
				HelpNamespace = m_propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider).HelpFile
			};
			m_helpProvider.SetHelpKeyword(this, m_propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider).GetHelpString(m_helpTopicId));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);
		}

		/// <summary>
		/// Generates a possible help topic id from an identifying string, but does NOT check it for validity!
		/// </summary>
		private static string generateChooserHelpTopicID(string fromStr)
		{
			var candidateID = "khtpManage";
			// Should we capitalize the next letter?
			var nextCapital = true;
			// Lets turn our field into a candidate help page!
			foreach (var ch in fromStr)
			{
				if (char.IsLetterOrDigit(ch)) // might we include numbers someday?
				{
					if (nextCapital)
					{
						candidateID += char.ToUpper(ch);
					}
					else
					{
						candidateID += ch;
					}
					nextCapital = false;
				}
				else // unrecognized character... exclude it
				{
					nextCapital = true; // next letter should be a capital
				}
			}
			return candidateID;
		}

		private VisibleListItem CurrentSelectedItem => m_listView.Items[m_listView.SelectedIndices[0]] as VisibleListItem;

		#region Implementation of IDictConfigViewer

		public IDictConfigPresenter Presenter { get; }

		/// <summary>
		/// Tuples of strings are (uniqueCode, dispName) pairs to be displayed.
		/// </summary>
		/// <param name="listItems"></param>
		/// <param name="selectedItem">The code for the item that should be selected
		/// in the dialog ListView.</param>
		public void SetListViewItems(IEnumerable<Tuple<string, string>> listItems, string selectedItem)
		{
			var itemList = listItems.Select(item => new VisibleListItem(item.Item1, item.Item2)).ToList();
			CheckForValidSelectedItem(itemList, selectedItem);
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
			{
				return; // Just in case.
			}
			// find code in list, select that index
			var idx = -1;
			foreach (VisibleListItem item in m_listView.Items)
			{
				if (item.Code != code)
				{
					continue;
				}
				idx = item.Index;
				item.Selected = true;
				break;
			}
			if (idx < 0) // invalid code! select first item
			{
				m_listView.Items[0].Selected = true;
			}
		}

		/// <summary>
		/// The unique code for the item currently selected in the dialog listView.
		/// </summary>
		public string CurrentSelectedCode => CurrentSelectedItem.Code;

		private static void CheckForValidSelectedItem(IEnumerable<VisibleListItem> itemList, string selectedItem)
		{
			var result = itemList.FirstOrDefault(item => item.Code == selectedItem);
			Debug.Assert(result != null, "Selected item does not exist in list.");
		}

		private void SetListViewItemsInternal(IEnumerable<VisibleListItem> listItems)
		{
			m_listView.BeginUpdate();
			m_listView.Items.Clear();
			foreach (var listItem in listItems)
			{
				m_listView.Items.Add(listItem);
			}
			m_listView.EndUpdate();
		}

		#endregion

		private void m_listView_BeforeLabelEdit(object sender, LabelEditEventArgs e)
		{
			if (CurrentSelectedItem == null || Presenter.IsConfigProtected(CurrentSelectedItem.Code))
			{
				e.CancelEdit = true;
			}
		}

		private void m_listView_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			if (e.Label == null || e.Label == CurrentSelectedItem.Name)
			{
				return; // nothing to do!
			}
			Presenter.RenameConfigItem(CurrentSelectedItem.Code, e.Label);
		}

		private void m_btnCopy_Click(object sender, EventArgs e)
		{
			if (CurrentSelectedItem == null)
			{
				return; // what happened?
			}
			Presenter.CopyConfigItem(CurrentSelectedItem.Code);
			CurrentSelectedItem.BeginEdit();
		}

		private void m_btnDelete_Click(object sender, EventArgs e)
		{
			if (CurrentSelectedItem == null)
			{
				return; // what happened?
			}
			var dr = MessageBox.Show(string.Format(DictionaryConfigurationStrings.ksConfirmDelete, m_objType, CurrentSelectedItem.Name),
				string.Format(DictionaryConfigurationStrings.ksConfirmDeleteTitle, m_objType), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
			if (dr == DialogResult.Yes)
			{
				Presenter.TryMarkForDeletion(CurrentSelectedItem.Code);
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			Presenter.PersistState();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), m_helpTopicId);
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

		private sealed class VisibleListItem : ListViewItem
		{
			internal VisibleListItem(string code, string name)
			{
				Tag = code;
				Name = name;
				Text = name;
			}

			internal string Code => (string)Tag;
		}
	}
}