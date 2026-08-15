// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Lets the user pick which project texts to include in a grammar+texts-for-AI export,
	/// showing each text's Words and Analyses counts from
	/// GrammarTextsAIExportHelpers.CountWordsAndAnalyses.
	/// </summary>
	public partial class GrammarAndTextsAIExportSelectionDlg : Form
	{
		private readonly List<IStText> m_texts;

		public GrammarAndTextsAIExportSelectionDlg(LcmCache cache, IEnumerable<IStText> texts)
		{
			InitializeComponent();
			m_texts = texts.ToList();
			foreach (var stText in m_texts)
			{
				var counts = GrammarTextsAIExportHelpers.CountWordsAndAnalyses(stText);
				var name = GrammarTextsAIExportHelpers.GetTextDisplayName(stText);
				var item = new ListViewItem(new[] { name, counts.Words.ToString(), counts.Analyses.ToString() })
				{
					Tag = stText,
					Checked = true
				};
				m_textListView.Items.Add(item);
			}
		}

		/// <summary>
		/// Checks only the texts whose Guid string is in previousSelectionGuids, leaving
		/// every other row unchecked. If previousSelectionGuids is null (first use), every
		/// row stays checked (the default set in the constructor).
		/// </summary>
		public void ApplyPreviousSelection(HashSet<string> previousSelectionGuids)
		{
			if (previousSelectionGuids == null)
				return;
			foreach (ListViewItem item in m_textListView.Items)
			{
				var stText = (IStText)item.Tag;
				item.Checked = previousSelectionGuids.Contains(stText.Guid.ToString());
			}
		}

		public IEnumerable<IStText> SelectedTexts =>
			m_textListView.Items.Cast<ListViewItem>().Where(i => i.Checked).Select(i => (IStText)i.Tag);

		private void m_btnOk_Click(object sender, System.EventArgs e)
		{
			if (!SelectedTexts.Any())
			{
				MessageBox.Show(this, xWorksStrings.ksNoTextsSelectedForAIExport);
				DialogResult = DialogResult.None;
			}
		}
	}
}
