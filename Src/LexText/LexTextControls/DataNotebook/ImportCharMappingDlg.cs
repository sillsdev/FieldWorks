// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportCharMappingDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Linq;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ImportCharMappingDlg : Form
	{
		FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		IVwStylesheet m_stylesheet;

		/// <summary>
		/// Initializes a new instance of the <see cref="ImportCharMappingDlg"/> class.
		/// </summary>
		public ImportCharMappingDlg()
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
		}

		public void Initialize(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			IApp app, IVwStylesheet stylesheet, NotebookImportWiz.CharMapping charMapping)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_stylesheet = stylesheet;
			if (charMapping == null)
			{
				m_tbBeginMkr.Text = String.Empty;
				m_tbEndMkr.Text = String.Empty;
				m_rbEndOfWord.Checked = false;
				m_rbEndOfField.Checked = true;
				FillWritingSystemCombo(null);
				FillStylesCombo(null);
				m_chkIgnore.Checked = false;
			}
			else
			{
				m_tbBeginMkr.Text = charMapping.BeginMarker;
				m_tbEndMkr.Text = charMapping.EndMarker;
				m_rbEndOfWord.Checked = charMapping.EndWithWord;
				m_rbEndOfField.Checked = !charMapping.EndWithWord;
				FillWritingSystemCombo(charMapping.DestinationWritingSystemId);
				FillStylesCombo(charMapping.DestinationStyle);
				m_chkIgnore.Checked = charMapping.IgnoreMarkerOnImport;
			}
		}

		private void FillWritingSystemCombo(string sWs)
		{
			m_cbWritingSystem.Enabled = true;
			m_cbWritingSystem.Items.Clear();
			m_cbWritingSystem.Sorted = true;
			foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.AllWritingSystems)
				m_cbWritingSystem.Items.Add(ws);

			if (!string.IsNullOrEmpty(sWs))
			{
				IWritingSystem selectedWs;
				if (!m_cache.ServiceLocator.WritingSystemManager.GetOrSet(sWs, out selectedWs))
					m_cbWritingSystem.Items.Add(selectedWs);
				m_cbWritingSystem.SelectedItem = selectedWs;
			}
			m_btnAddWS.Initialize(m_cache, m_helpTopicProvider, m_app, m_stylesheet, m_cache.ServiceLocator.WritingSystems.AllWritingSystems);
		}

		private void FillStylesCombo(string sStyle)
		{
			m_cbStyle.Enabled = true;
			m_cbStyle.Items.Clear();
			m_cbStyle.Sorted = true;
			IStStyle stySel = null;
			for (int i = 0; i < m_stylesheet.CStyles; ++i)
			{
				int hvo = m_stylesheet.get_NthStyle(i);
				IStStyle sty = m_cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvo);
				if (sty.Type == StyleType.kstCharacter)
				{
					m_cbStyle.Items.Add(sty);
					if (sty.Name == sStyle)
						stySel = sty;
				}
			}
			if (stySel == null)
			{
				// TODO: Should we create a style here to match the name???
			}
			else
			{
				m_cbStyle.SelectedItem = stySel;
			}
		}

		private void m_btnStyles_Click(object sender, EventArgs e)
		{
			bool fRTL = m_cache.WritingSystemFactory.get_EngineOrNull(m_cache.DefaultUserWs).RightToLeftScript;
			using (var dlg = new FwStylesDlg(null, m_cache, m_stylesheet as FwStyleSheet,
				fRTL,
				m_cache.ServiceLocator.WritingSystems.AllWritingSystems.Any(ws => ws.RightToLeftScript),
				m_stylesheet.GetDefaultBasedOnStyleName(),
				0,		// customUserLevel
				m_app.MeasurementSystem,
				m_stylesheet.GetDefaultBasedOnStyleName(),
				String.Empty,
				0,		// hvoRootObject
				m_app, m_helpTopicProvider))
			{
				dlg.ShowTEStyleTypes = false;
				dlg.CanSelectParagraphBackgroundColor = false;
				if (dlg.ShowDialog(this) == DialogResult.OK &&
					((dlg.ChangeType & StyleChangeType.DefChanged) > 0 ||
					(dlg.ChangeType & StyleChangeType.Added) > 0 ||
					(dlg.ChangeType & StyleChangeType.RenOrDel) > 0))
				{
					m_app.Synchronize(SyncMsg.ksyncStyle);
					FwStyleSheet stylesheet = new FwStyleSheet();
					stylesheet.Init(m_cache, m_cache.LangProject.Hvo, LangProjectTags.kflidStyles);
					m_stylesheet = stylesheet;
				}
				string stySel = null;
				if (m_cbStyle.SelectedItem != null)
					stySel = m_cbStyle.SelectedItem.ToString();
				FillStylesCombo(stySel);
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpDataNotebookImportCharMapping");
		}

		private void m_btnAddWS_WritingSystemAdded(object sender, EventArgs e)
		{
			IWritingSystem ws = m_btnAddWS.NewWritingSystem;
			if (ws != null)
				FillWritingSystemCombo(ws.Id);
		}

		private void m_rbEndOfWord_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbEndOfWord.Checked)
				m_rbEndOfField.Checked = false;
		}

		private void m_rbEndOfField_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbEndOfField.Checked)
				m_rbEndOfWord.Checked = false;
		}

		public string BeginMarker
		{
			get { return m_tbBeginMkr.Text; }
		}

		public string EndMarker
		{
			get { return m_tbEndMkr.Text; }
		}

		public bool EndWithWord
		{
			get { return m_rbEndOfWord.Checked; }
		}

		public string WritingSystemId
		{
			get
			{
				if (!m_chkIgnore.Checked && m_cbWritingSystem.SelectedItem is IWritingSystem)
				{
					return ((IWritingSystem) m_cbWritingSystem.SelectedItem).Id;
				}
				else
				{
					return null;
				}
			}
		}

		public string StyleName
		{
			get
			{
				if ((!m_chkIgnore.Checked) && (m_cbStyle.SelectedItem as IStStyle) != null)
				{
					return (m_cbStyle.SelectedItem as IStStyle).Name;
				}
				else
				{
					return null;
				}
			}
		}

		public bool IgnoreOnImport
		{
			get { return m_chkIgnore.Checked; }
		}

		private void m_chkIgnore_CheckedChanged(object sender, EventArgs e)
		{
			if (m_chkIgnore.Checked)
			{
				m_cbStyle.Enabled = false;
				m_cbWritingSystem.Enabled = false;
			}
			else
			{
				m_cbStyle.Enabled = true;
				m_cbWritingSystem.Enabled = true;
			}
		}

		private void m_tbBeginMkr_TextChanged(object sender, EventArgs e)
		{
			if (m_tbBeginMkr.Text.Length == 0)
				m_btnOK.Enabled = false;
			else
				m_btnOK.Enabled = true;
		}
	}
}