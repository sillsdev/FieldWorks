// Copyright (c) 2015-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class ConfigureHomographDlg : Form
	{
		public ConfigureHomographDlg()
		{
			InitializeComponent();
		}

		private HomographConfiguration m_homographConfiguration;
		private LcmCache m_cache;
		LcmStyleSheet m_stylesheet;
		private IApp m_app;
		private IHelpTopicProvider m_helpTopicProvider;
		protected HelpProvider m_helpProvider;

		protected string m_helpTopic = ""; // Default help topic ID

		private bool m_masterRefreshRequired;

		public void SetupDialog(HomographConfiguration hc, LcmCache cache, LcmStyleSheet stylesheet, IApp app,
			IHelpTopicProvider helpTopicProvider)
		{
			SetHelpTopic("khtpConfigureHeadwordNumbers"); // Default help topic ID
			m_helpProvider = new HelpProvider();
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);

			m_homographConfiguration = hc;
			m_cache = cache;
			m_stylesheet = stylesheet;
			m_app = app;
			m_helpTopicProvider = helpTopicProvider;
			if (m_helpTopicProvider != null)
			{
				m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				SetHelpButtonEnabled();
			}

			if (hc.ShowHomographNumber(HomographConfiguration.HeadwordVariant.Main))
			{
				m_radioHide.Checked = false;
				m_radioBefore.Checked = hc.HomographNumberBefore;
				m_radioAfter.Checked = !hc.HomographNumberBefore;
				m_chkShowSenseNumInReversal.Checked = hc.ShowSenseNumberReversal;
			}
			else
			{
				m_radioHide.Checked = true;
				m_radioBefore.Checked = false;
				m_radioAfter.Checked = false;
				m_chkShowSenseNumInReversal.Checked = false;
			}
			m_chkShowHomographNumInReversal.Checked =
				hc.ShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef);
			EnableControls();
		}

		/// <summary>
		/// Set the properties of the HomographConfiguration to those specified in the dialog.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			if (DialogResult == DialogResult.OK)
			{
				m_homographConfiguration.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.Main, !m_radioHide.Checked);
				m_homographConfiguration.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef,
					!m_radioHide.Checked && m_chkShowHomographNumInReversal.Checked);
				m_homographConfiguration.HomographNumberBefore = m_radioBefore.Checked;
				m_homographConfiguration.ShowSenseNumberReversal = !m_radioHide.Checked && m_chkShowSenseNumInReversal.Checked;
			}
			base.OnClosing(e);
			if (m_masterRefreshRequired)
				DialogResult = DialogResult.OK; // let the client know that something has changed
		}

		private void m_radioBefore_CheckedChanged(object sender, EventArgs e)
		{
			if (m_radioBefore.Checked)
			{
				m_radioAfter.Checked = false;
				m_radioHide.Checked = false;
			}
			EnableControls();
		}

		void EnableControls()
		{
			m_chkShowHomographNumInReversal.Enabled = !m_radioHide.Checked;
			m_chkShowSenseNumInReversal.Enabled = !m_radioHide.Checked;
			if (m_radioHide.Checked)
			{
				m_chkShowHomographNumInReversal.Checked = false;
				m_chkShowSenseNumInReversal.Checked = false;
			}
		}

		private void m_radioAfter_CheckedChanged(object sender, EventArgs e)
		{
			if (m_radioAfter.Checked)
			{
				m_radioHide.Checked = false;
				m_radioBefore.Checked = false;
			}
			EnableControls();
		}

		private void m_radioHide_CheckedChanged(object sender, EventArgs e)
		{
			if (m_radioHide.Checked)
			{
				m_radioAfter.Checked = false;
				m_radioBefore.Checked = false;
			}
			EnableControls();

		}

		private void m_linkConfigHomographNumber_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			RunStyleDialog(HomographConfiguration.ksHomographNumberStyle);
		}

		private void RunStyleDialog(string styleName)
		{
			using (var dlg = new FwStylesDlg(null, m_cache, m_stylesheet,
				m_cache.WritingSystemFactory.get_EngineOrNull(m_cache.DefaultUserWs).RightToLeftScript,
				m_cache.ServiceLocator.WritingSystems.AllWritingSystems.Any(ws => ws.RightToLeftScript),
				m_stylesheet.GetDefaultBasedOnStyleName(),
				0,		// customUserLevel
				m_app.MeasurementSystem,
				styleName, //m_stylesheet.GetDefaultBasedOnStyleName(),
				styleName,
				0,		// hvoRootObject
				m_app, m_helpTopicProvider))
			{
				dlg.ShowTEStyleTypes = false;
				dlg.CanSelectParagraphBackgroundColor = false;
				if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ChangeType != StyleChangeType.None)
				{
					m_app.Synchronize(SyncMsg.ksyncStyle);
					LcmStyleSheet stylesheet = new LcmStyleSheet();
					stylesheet.Init(m_cache, m_cache.LangProject.Hvo, LangProjectTags.kflidStyles);
					m_stylesheet = stylesheet;
					m_masterRefreshRequired = true;
				}
			}
		}


		/// <summary>
		/// Sets the help topic ID for the window.  This is used in both the Help button and when the user hits F1
		/// </summary>
		public void SetHelpTopic(string helpTopic)
		{
			//CheckDisposed();

			m_helpTopic = helpTopic;
			if (m_helpTopicProvider != null)
			{
				SetHelpButtonEnabled();
			}
		}

		private void SetHelpButtonEnabled()
		{
			m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
			m_btnHelp.Enabled = !string.IsNullOrEmpty(m_helpTopic);
		}

		private void m_linkConfigSenseRefNumber_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			RunStyleDialog(HomographConfiguration.ksSenseReferenceNumberStyle);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display help for this dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}
	}
}
