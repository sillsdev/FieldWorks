// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary>
	/// This control display the options for list reference fields.
	/// </summary>
	public partial class ListRefFieldOptions : UserControl
	{
		LcmCache m_cache;
		IHelpTopicProvider m_helpTopicProvider;

		/// <summary />
		public ListRefFieldOptions()
		{
			InitializeComponent();
		}

		private void m_chkDelimMultiEnable_CheckedChanged(object sender, EventArgs e)
		{
			m_tbDelimMulti.Enabled = m_chkDelimMultiEnable.Checked;
		}

		private void m_chkDelimSubEnable_CheckedChanged(object sender, EventArgs e)
		{
			m_tbDelimSub.Enabled = m_chkDelimSubEnable.Checked;
		}

		private void m_chkBetweenEnable_CheckedChanged(object sender, EventArgs e)
		{
			m_tbBetweenAfter.Enabled = m_chkBetweenEnable.Checked;
			m_tbBetweenBefore.Enabled = m_chkBetweenEnable.Checked;
		}

		private void m_chkOnlyBeforeEnable_CheckedChanged(object sender, EventArgs e)
		{
			m_tbOnlyBefore.Enabled = m_chkOnlyBeforeEnable.Checked;
		}


		private void m_btnAddSubst_Click(object sender, EventArgs e)
		{
			using (var dlg = new ImportMatchReplaceDlg())
			{
				dlg.Initialize(m_helpTopicProvider, string.Empty, string.Empty);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					var lvi = new ListViewItem(new[] {dlg.Match, dlg.Replace});
					m_lvSubstitutions.Items.Add(lvi);
				}
			}
		}

		private void m_btnModifySubst_Click(object sender, EventArgs e)
		{
			if (m_lvSubstitutions.SelectedItems.Count > 0)
			{
				var lvi = m_lvSubstitutions.SelectedItems[0];
				using (var dlg = new ImportMatchReplaceDlg())
				{
					dlg.Initialize(m_helpTopicProvider, lvi.SubItems[0].Text, lvi.SubItems[1].Text);
					if (dlg.ShowDialog(this) == DialogResult.OK)
					{
						lvi.SubItems[0].Text = dlg.Match;
						lvi.SubItems[1].Text = dlg.Replace;
					}
				}

			}
		}

		private void m_btnDeleteSubst_Click(object sender, EventArgs e)
		{
			if (m_lvSubstitutions.SelectedItems.Count > 0)
			{
				var lvi = m_lvSubstitutions.SelectedItems[0];
				m_lvSubstitutions.Items.Remove(lvi);
			}
		}

		private void m_rbMatchAbbr_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbMatchAbbr.Checked)
			{
				m_rbMatchName.Checked = false;
			}
		}

		private void m_rbMatchName_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbMatchName.Checked)
			{
				m_rbMatchAbbr.Checked = false;
			}
		}

		internal void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, IApp app, RnSfMarker rsfm, CellarPropertyType cpt)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_btnAddWritingSystem.Initialize(m_cache, helpTopicProvider, app);
			NotebookImportWiz.InitializeWritingSystemCombo(rsfm.m_tlo.m_wsId, cache, m_cbWritingSystem);
			var fNotAtomic = (cpt != CellarPropertyType.ReferenceAtomic);
			m_tbDefaultValue.Text = rsfm.m_tlo.m_sEmptyDefault;

			m_chkDelimMultiEnable.Checked = rsfm.m_tlo.m_fHaveMulti && fNotAtomic;
			m_chkDelimMultiEnable.Enabled = fNotAtomic;
			m_tbDelimMulti.Text = rsfm.m_tlo.m_sDelimMulti;
			m_tbDelimMulti.Enabled = rsfm.m_tlo.m_fHaveMulti && fNotAtomic;

			m_chkDelimSubEnable.Checked = rsfm.m_tlo.m_fHaveSub;
			m_tbDelimSub.Text = rsfm.m_tlo.m_sDelimSub;
			m_tbDelimSub.Enabled = rsfm.m_tlo.m_fHaveSub;

			m_chkBetweenEnable.Checked = rsfm.m_tlo.m_fHaveBetween;
			m_tbBetweenBefore.Text = rsfm.m_tlo.m_sMarkStart;
			m_tbBetweenAfter.Text = rsfm.m_tlo.m_sMarkEnd;
			m_tbBetweenBefore.Enabled = rsfm.m_tlo.m_fHaveBetween;
			m_tbBetweenAfter.Enabled = rsfm.m_tlo.m_fHaveBetween;

			m_chkOnlyBeforeEnable.Checked = rsfm.m_tlo.m_fHaveBefore;
			m_tbOnlyBefore.Text = rsfm.m_tlo.m_sBefore;
			m_tbOnlyBefore.Enabled = rsfm.m_tlo.m_fHaveBefore;

			m_chkDiscardNewStuff.Checked = rsfm.m_tlo.m_fIgnoreNewStuff;

			m_rbMatchName.Checked = rsfm.m_tlo.m_pnt == PossNameType.kpntName;
			m_rbMatchAbbr.Checked = rsfm.m_tlo.m_pnt != PossNameType.kpntName;

			Debug.Assert(rsfm.m_tlo.m_rgsMatch.Count == rsfm.m_tlo.m_rgsReplace.Count);
			m_lvSubstitutions.Items.Clear();
			for (var i = 0; i < rsfm.m_tlo.m_rgsMatch.Count; ++i)
			{
				var lvi = new ListViewItem(new[] { rsfm.m_tlo.m_rgsMatch[i], rsfm.m_tlo.m_rgsReplace[i] });
				m_lvSubstitutions.Items.Add(lvi);
			}
		}

		public string DefaultValue => m_tbDefaultValue.Text;

		public bool HaveMultiple => m_chkDelimMultiEnable.Checked;

		public string DelimForMultiple => m_tbDelimMulti.Text;

		public bool HaveHierarchy => m_chkDelimSubEnable.Checked;

		public string DelimForHierarchy => m_tbDelimSub.Text;

		public bool HaveBetweenMarkers => m_chkBetweenEnable.Checked;

		public string LeadingBetweenMarkers => m_tbBetweenAfter.Text;

		public string TrailingBetweenMarkers => m_tbBetweenBefore.Text;

		public bool HaveCommentMarker => m_chkOnlyBeforeEnable.Checked;

		public string CommentMarkers => m_tbOnlyBefore.Text;

		public bool DiscardNewStuff => m_chkDiscardNewStuff.Checked;

		public PossNameType MatchAgainst => m_rbMatchName.Checked ? PossNameType.kpntName : PossNameType.kpntAbbreviation;

		public int MatchReplaceCount => m_lvSubstitutions.Items.Count;

		public List<string> Matches
		{
			get
			{
				var rgsMatch = new List<string>();
				for (var i = 0; i < m_lvSubstitutions.Items.Count; ++i)
				{
					var lvi = m_lvSubstitutions.Items[i];
					var s = lvi.SubItems[0].Text;
					rgsMatch.Add(s);
				}
				return rgsMatch;
			}
		}

		public List<string> Replacements
		{
			get
			{
				var rgsReplace = new List<string>();
				for (var i = 0; i < m_lvSubstitutions.Items.Count; ++i)
				{
					var lvi = m_lvSubstitutions.Items[i];
					var s = lvi.SubItems[1].Text;
					rgsReplace.Add(s);
				}
				return rgsReplace;
			}
		}

		public string WritingSystem
		{
			get
			{
				var ws = m_cbWritingSystem.SelectedItem as CoreWritingSystemDefinition;
				return ws?.Id;
			}
		}

		private void m_btnAddWritingSystem_WritingSystemAdded(object sender, EventArgs e)
		{
			var ws = m_btnAddWritingSystem.NewWritingSystem;
			if (ws != null)
			{
				NotebookImportWiz.InitializeWritingSystemCombo(ws.Id, m_cache, m_cbWritingSystem);
			}
		}
	}
}
