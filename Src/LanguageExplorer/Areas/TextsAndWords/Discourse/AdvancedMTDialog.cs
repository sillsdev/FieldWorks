// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// A dialog class for marking SOME of the text in a CChart cell as pre/postposed
	/// from the same or different rows of the chart. The logic is in a separate class.
	/// </summary>
	internal sealed partial class AdvancedMTDialog : Form
	{
		private HelpProvider _helpProvider;

		internal AdvancedMTDialog(LcmCache cache, bool fPrepose, CChartSentenceElements ccSentElem, IHelpTopicProvider helpTopicProvidor)
		{
			InitializeComponent();

			SuspendLayout();

			m_helpTopicProvider = helpTopicProvidor;
			if (m_helpTopicProvider != null)
			{
				_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
				_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
				_helpProvider.SetShowHelp(this, true);
			}
			DlgLogic = new AdvancedMTDialogLogic(cache, fPrepose, ccSentElem);
			m_bottomStuff.SuspendLayout();
			m_bottomStuff.Controls.AddRange(new Control[] { DlgLogic.DlgRibbon });

			m_bottomStuff.ResumeLayout();

			// Setup localized dialog
			SetCaption(fPrepose ? LanguageExplorerResources.ksAdvDlgPreposeCaption : LanguageExplorerResources.ksAdvDlgPostposeCaption);
			SetMainText(fPrepose ? LanguageExplorerResources.ksAdvDlgMainPreText : LanguageExplorerResources.ksAdvDlgMainPostText);
			SetPartialText(fPrepose ? LanguageExplorerResources.ksAdvDlgPartialPre : LanguageExplorerResources.ksAdvDlgPartialPost);

			ResumeLayout();

			InitLogicAndDialog();
		}

		#region LabelSettingMethods

		/// <summary>
		/// Sets the dialog box caption
		/// </summary>
		/// <param name="label"></param>
		private void SetCaption(string label)
		{
			Text = label;
		}

		/// <summary>
		/// Sets the main text of the dialog box.
		/// </summary>
		/// <param name="label"></param>
		private void SetMainText(string label)
		{
			m_mainText.Text = label;
		}

		/// <summary>
		/// Sets the second text box of the dialog box (explains choosing part of a cell for moved text).
		/// </summary>
		/// <param name="label"></param>
		private void SetPartialText(string label)
		{
			m_partialText.Text = label;
		}

		#endregion

		internal void InitLogicAndDialog()
		{
			DlgLogic.Init();
			SetRows(DlgLogic.SentElem.EligibleRows);
			// Preselect the row closest to sender
			if (DlgLogic.Prepose)
			{
				SelectedRow = m_rowsCombo.Items[0] as RowMenuItem;
			}
			else
			{
				SelectedRow = m_rowsCombo.Items[DlgLogic.SentElem.EligibleRows.Length - 1] as RowMenuItem;
			}
			SetColumns(DlgLogic.SentElem.ComboCols);
		}

		internal void SetRows(IConstChartRow[] items)
		{
			// Convert ConstChartRows to RowMenuItems
			var rows = new RowMenuItem[items.Length];
			for (var i = 0; i < items.Length; i++)
			{
				rows[i] = new RowMenuItem(items[i]);
			}
			m_rowsCombo.Items.Clear();
			m_rowsCombo.Items.AddRange(rows);
		}

		internal AnalysisOccurrence[] SelectedOccurrences { get; set; }

		/// <summary>
		/// Row selected by user in combobox
		/// </summary>
		internal RowMenuItem SelectedRow
		{
			get { return (RowMenuItem)m_rowsCombo.SelectedItem; }
			set { m_rowsCombo.SelectedItem = value; }
		}

		/// <summary>
		/// Sets the dialog combobox for columns according to what the logic filled in the parameter object.
		/// Also selects the first column as selected initially, since we have no idea what might be wanted.
		/// </summary>
		internal void SetColumns(ColumnMenuItem[] items)
		{
			m_columnsCombo.Items.Clear();
			if (items.Length <= 0)
			{
				return;
			}
			m_columnsCombo.Items.AddRange(items);
			SelectedColumn = items[0]; // No way of knowing which column might be wanted, select the first.
		}

		// Column selected by user in combobox
		internal ColumnMenuItem SelectedColumn
		{
			get { return (ColumnMenuItem)m_columnsCombo.SelectedItem; }
			set { m_columnsCombo.SelectedItem = value; }
		}

		internal AdvancedMTDialogLogic DlgLogic { get; private set; }

		private void m_OkButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			// Must save selected occurrences before closing or the rootbox will close on them!
			SelectedOccurrences = DlgLogic.DlgRibbon.SelectedOccurrences;
			DlgLogic.SetAffectedWordGroups(SelectedOccurrences);
			Close();
		}

		private void m_cancelButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		/// <summary>
		/// Display AnotherClause dialog help here.
		/// </summary>
		private void m_helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		private void m_rowsCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Figure out if some column choices need to be changed.
			DlgLogic.CollectColumnsToCombo(DlgLogic.GetColumnChoices(SelectedRow.Row));
			SetColumns(DlgLogic.SentElem.ComboCols);
			m_columnsCombo.Refresh();
		}
	}
}