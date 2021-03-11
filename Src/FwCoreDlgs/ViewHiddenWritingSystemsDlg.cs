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
				m_lblInstructions.Text = string.Format(m_lblInstructions.Text
							+ "\nDear testers, this dialog is ugly when resized. Will fix next week.", // TODO (Hasso) 2021.03: fix
					model.ListType == FwWritingSystemSetupModel.ListType.Analysis ? FwCoreDlgs.Analysis : FwCoreDlgs.Vernacular);
				BindToModel();
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
			m_btnShow.Enabled = curItem != null;
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
		}

		private void m_btnShow_Click(object sender, EventArgs e)
		{
			m_model.Show(CurrentItem);
			BindToModel();
		}

		private void m_btnDelete_Click(object sender, EventArgs e)
		{
			m_model.Delete(CurrentItem);
			BindToModel();
		}
	}
}
