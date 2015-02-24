// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ListRefFieldOptions.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This control display the options for list reference fields.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ListRefFieldOptions : UserControl
	{
		FdoCache m_cache;
		XCore.IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="ListRefFieldOptions"/> class.
		/// </summary>
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
				dlg.Initialize(m_helpTopicProvider, String.Empty, String.Empty);
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
				ListViewItem lvi = m_lvSubstitutions.SelectedItems[0];
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
				ListViewItem lvi = m_lvSubstitutions.SelectedItems[0];
				m_lvSubstitutions.Items.Remove(lvi);
			}
		}

		private void m_rbMatchAbbr_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbMatchAbbr.Checked)
				m_rbMatchName.Checked = false;
		}

		private void m_rbMatchName_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbMatchName.Checked)
				m_rbMatchAbbr.Checked = false;
		}


		internal void Initialize(FdoCache cache, XCore.IHelpTopicProvider helpTopicProvider, IApp app, IVwStylesheet stylesheet,
			NotebookImportWiz.RnSfMarker rsfm, CellarPropertyType cpt)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_btnAddWritingSystem.Initialize(m_cache, helpTopicProvider, app, stylesheet);
			NotebookImportWiz.InitializeWritingSystemCombo(rsfm.m_tlo.m_wsId, cache,
				m_cbWritingSystem);

			bool fNotAtomic = (cpt != CellarPropertyType.ReferenceAtomic);
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
			for (int i = 0; i < rsfm.m_tlo.m_rgsMatch.Count; ++i)
			{
				var lvi = new ListViewItem(new[] { rsfm.m_tlo.m_rgsMatch[i], rsfm.m_tlo.m_rgsReplace[i] });
				m_lvSubstitutions.Items.Add(lvi);
			}
		}

		public string DefaultValue
		{
			get { return m_tbDefaultValue.Text; }
		}

		public bool HaveMultiple
		{
			get { return m_chkDelimMultiEnable.Checked; }
		}

		public string DelimForMultiple
		{
			get { return m_tbDelimMulti.Text; }
		}

		public bool HaveHierarchy
		{
			get { return m_chkDelimSubEnable.Checked; }
		}

		public string DelimForHierarchy
		{
			get { return m_tbDelimSub.Text; }
		}

		public bool HaveBetweenMarkers
		{
			get { return m_chkBetweenEnable.Checked; }
		}

		public string LeadingBetweenMarkers
		{
			get { return m_tbBetweenAfter.Text; }
		}

		public string TrailingBetweenMarkers
		{
			get { return m_tbBetweenBefore.Text; }
		}

		public bool HaveCommentMarker
		{
			get { return m_chkOnlyBeforeEnable.Checked; }
		}

		public string CommentMarkers
		{
			get { return m_tbOnlyBefore.Text; }
		}

		public bool DiscardNewStuff
		{
			get { return m_chkDiscardNewStuff.Checked; }
		}

		public PossNameType MatchAgainst
		{
			get
			{
				if (m_rbMatchName.Checked)
					return PossNameType.kpntName;
				else
					return PossNameType.kpntAbbreviation;
			}
		}

		public int MatchReplaceCount
		{
			get { return m_lvSubstitutions.Items.Count; }
		}

		public List<string> Matches
		{
			get
			{
				List<string> rgsMatch = new List<string>();
				for (int i = 0; i < m_lvSubstitutions.Items.Count; ++i)
				{
					ListViewItem lvi = m_lvSubstitutions.Items[i];
					string s = lvi.SubItems[0].Text;
					rgsMatch.Add(s);
				}
				return rgsMatch;
			}
		}

		public List<string> Replacements
		{
			get
			{
				List<string> rgsReplace = new List<string>();
				for (int i = 0; i < m_lvSubstitutions.Items.Count; ++i)
				{
					ListViewItem lvi = m_lvSubstitutions.Items[i];
					string s = lvi.SubItems[1].Text;
					rgsReplace.Add(s);
				}
				return rgsReplace;
			}
		}

		public string WritingSystem
		{
			get
			{
				var ws = m_cbWritingSystem.SelectedItem as WritingSystem;
				if (ws == null)
					return null;
				else
					return ws.ID;
			}
		}

		private void m_btnAddWritingSystem_WritingSystemAdded(object sender, EventArgs e)
		{
			WritingSystem ws = m_btnAddWritingSystem.NewWritingSystem;
			if (ws != null)
				NotebookImportWiz.InitializeWritingSystemCombo(ws.ID, m_cache,
					m_cbWritingSystem);
		}
	}
}
