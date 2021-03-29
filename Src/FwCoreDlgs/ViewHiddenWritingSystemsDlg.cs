// Copyright (c) 2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <see cref="ViewHiddenWritingSystemsModel"/>
	public partial class ViewHiddenWritingSystemsDlg : Form
	{
		private readonly ViewHiddenWritingSystemsModel m_model;
		private readonly IHelpTopicProvider m_helpTopicProvider;

		/// <inheritdoc />
		public ViewHiddenWritingSystemsDlg(ViewHiddenWritingSystemsModel model = null, IHelpTopicProvider helpTopicProvider = null)
		{
			m_model = model;
			m_helpTopicProvider = helpTopicProvider;

			InitializeComponent();

			if (model != null)
			{
				m_lblInstructions.Text = string.Format(m_lblInstructions.Text,
					model.ListType == FwWritingSystemSetupModel.ListType.Analysis ? FwCoreDlgs.Analysis : FwCoreDlgs.Vernacular);
				BindToModel();
				m_listView.Select();
			}
		}

		private void BindToModel()
		{
			SuspendLayout();
			var otherListName = m_model.ListType == FwWritingSystemSetupModel.ListType.Analysis ? FwCoreDlgs.Vernacular : FwCoreDlgs.Analysis;
			var selection = m_listView.SelectedIndices.GetEnumerator();
			m_listView.Items.Clear();
			m_listView.Items.AddRange(m_model.Items.Select(i => new ListViewItem { Tag = i, Text = i.FormatDisplayLabel(otherListName) }).ToArray());
			while (selection.MoveNext())
			{
				// ReSharper disable once PossibleNullReferenceException
				m_listView.SelectedIndices.Add((int)selection.Current);
			}
			BindButtons();
			ResumeLayout();
		}

		private void BindButtons()
		{
			var curItem = CurrentItem;
			m_btnAdd.Enabled = curItem != null;
			m_btnDelete.Enabled = curItem != null && !curItem.InOppositeList && !curItem.WillDelete;
		}

		private HiddenWSListItemModel CurrentItem => m_listView.SelectedItems.Count == 1
			? (HiddenWSListItemModel)m_listView.SelectedItems[0].Tag
			: null;

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "UserHelpFile", "khtpHiddenWritingSystems");
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void m_listView_SelectedIndexChanged(object sender, EventArgs e)
		{
			BindButtons();
			AcceptButton = m_btnAdd.Enabled ? m_btnAdd : m_btnClose;
		}

		private void m_btnAdd_Click(object sender, EventArgs e)
		{
			m_model.Add(CurrentItem);
			BindToModel();
			AcceptButton = m_btnClose;
		}

		private void m_btnDelete_Click(object sender, EventArgs e)
		{
			m_model.Delete(CurrentItem);
			BindToModel();
			AcceptButton = m_btnClose;
			// Pressing Escape usually closes the dialog and frequently even saves settings (Close vs Save & Cancel),
			// but I don't want to give users the slightest illusion that they're canceling a delete. The can press Enter to close the dialog.
			CancelButton = null;
		}
	}
}
