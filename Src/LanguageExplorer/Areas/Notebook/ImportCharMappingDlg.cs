// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.Styles;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.Notebook
{
	/// <summary />
	public partial class ImportCharMappingDlg : Form
	{
		private LcmCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		private IVwStylesheet m_stylesheet;

		/// <summary />
		public ImportCharMappingDlg()
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
		}

		internal void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, IApp app, IVwStylesheet stylesheet, CharMapping charMapping)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_stylesheet = stylesheet;
			if (charMapping == null)
			{
				m_tbBeginMkr.Text = string.Empty;
				m_tbEndMkr.Text = string.Empty;
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
			foreach (var ws in m_cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				m_cbWritingSystem.Items.Add(ws);
			}
			if (!string.IsNullOrEmpty(sWs))
			{
				CoreWritingSystemDefinition selectedWs;
				if (!m_cache.ServiceLocator.WritingSystemManager.GetOrSet(sWs, out selectedWs))
				{
					m_cbWritingSystem.Items.Add(selectedWs);
				}
				m_cbWritingSystem.SelectedItem = selectedWs;
			}
			m_btnAddWS.Initialize(m_cache, m_helpTopicProvider, m_app, m_cache.ServiceLocator.WritingSystems.AllWritingSystems);
		}

		private void FillStylesCombo(string sStyle)
		{
			m_cbStyle.Enabled = true;
			m_cbStyle.Items.Clear();
			m_cbStyle.Sorted = true;
			IStStyle stySel = null;
			for (var i = 0; i < m_stylesheet.CStyles; ++i)
			{
				var hvo = m_stylesheet.get_NthStyle(i);
				var sty = m_cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvo);
				if (sty.Type == StyleType.kstCharacter)
				{
					m_cbStyle.Items.Add(sty);
					if (sty.Name == sStyle)
					{
						stySel = sty;
					}
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
			var fRTL = m_cache.WritingSystemFactory.get_EngineOrNull(m_cache.DefaultUserWs).RightToLeftScript;
			using (var dlg = new FwStylesDlg(null, m_cache, m_stylesheet as LcmStyleSheet,
				fRTL,
				m_cache.ServiceLocator.WritingSystems.AllWritingSystems.Any(ws => ws.RightToLeftScript),
				m_stylesheet.GetDefaultBasedOnStyleName(),
				m_app.MeasurementSystem,
				m_stylesheet.GetDefaultBasedOnStyleName(),
				string.Empty,
				m_app, m_helpTopicProvider))
			{
				dlg.CanSelectParagraphBackgroundColor = false;
				if (dlg.ShowDialog(this) == DialogResult.OK
				    && ((dlg.ChangeType & StyleChangeType.DefChanged) > 0 || (dlg.ChangeType & StyleChangeType.Added) > 0 || (dlg.ChangeType & StyleChangeType.RenOrDel) > 0))
				{
					m_app.Synchronize(SyncMsg.ksyncStyle);
					var stylesheet = new LcmStyleSheet();
					stylesheet.Init(m_cache, m_cache.LangProject.Hvo, LangProjectTags.kflidStyles);
					m_stylesheet = stylesheet;
				}
				string stySel = null;
				if (m_cbStyle.SelectedItem != null)
				{
					stySel = m_cbStyle.SelectedItem.ToString();
				}
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
			var ws = m_btnAddWS.NewWritingSystem;
			if (ws != null)
			{
				FillWritingSystemCombo(ws.Id);
			}
		}

		private void m_rbEndOfWord_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbEndOfWord.Checked)
			{
				m_rbEndOfField.Checked = false;
			}
		}

		private void m_rbEndOfField_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbEndOfField.Checked)
			{
				m_rbEndOfWord.Checked = false;
			}
		}

		public string BeginMarker => m_tbBeginMkr.Text;

		public string EndMarker => m_tbEndMkr.Text;

		public bool EndWithWord => m_rbEndOfWord.Checked;

		public string WritingSystemId
		{
			get
			{
				if (!m_chkIgnore.Checked && m_cbWritingSystem.SelectedItem is CoreWritingSystemDefinition)
				{
					return ((CoreWritingSystemDefinition)m_cbWritingSystem.SelectedItem).Id;
				}
				return null;
			}
		}

		public string StyleName
		{
			get
			{
				if (!m_chkIgnore.Checked && (m_cbStyle.SelectedItem as IStStyle) != null)
				{
					return ((IStStyle)m_cbStyle.SelectedItem).Name;
				}
				return null;
			}
		}

		public bool IgnoreOnImport => m_chkIgnore.Checked;

		private void m_chkIgnore_CheckedChanged(object sender, EventArgs e)
		{
			m_cbStyle.Enabled = !m_chkIgnore.Checked;
			m_cbWritingSystem.Enabled = !m_chkIgnore.Checked;
		}

		private void m_tbBeginMkr_TextChanged(object sender, EventArgs e)
		{
			m_btnOK.Enabled = m_tbBeginMkr.Text.Length != 0;
		}
	}
}