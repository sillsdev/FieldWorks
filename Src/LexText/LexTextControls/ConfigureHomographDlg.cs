using System;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class ConfigureHomographDlg : Form
	{
		public ConfigureHomographDlg()
		{
			InitializeComponent();
		}

		private FdoCache m_cache;
		FwStyleSheet m_stylesheet;
		private IApp m_app;
		private IHelpTopicProvider m_helpTopicProvider;
		protected HelpProvider m_helpProvider;

		protected string m_helpTopic = ""; // Default help topic ID


		public void SetupDialog(HomographConfiguration hc, FdoCache cache, FwStyleSheet stylesheet, IApp app,
			IHelpTopicProvider helpTopicProvider)
		{
			SetHelpTopic("khtpConfigureHomograph"); // Default help topic ID
			m_helpProvider = new HelpProvider();
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);

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
				m_chkShowSenseNumInDict.Checked = hc.ShowSenseNumberRef;
				m_chkShowSenseNumInReversal.Checked = hc.ShowSenseNumberReversal;
			}
			else
			{
				m_radioHide.Checked = true;
				m_radioBefore.Checked = false;
				m_radioAfter.Checked = false;
				m_chkShowSenseNumInDict.Checked = false;
				m_chkShowSenseNumInReversal.Checked = false;
			}
			m_chkShowHomographNumInDict.Checked =
				hc.ShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef);
			m_chkShowHomographNumInReversal.Checked =
				hc.ShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef);
			EnableControls();
		}

		/// <summary>
		/// Set the properties of the HC passed in to those indicated by the dialog.
		/// </summary>
		public void GetResults(HomographConfiguration hc)
		{
			hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.Main, !m_radioHide.Checked);
			hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef,
				!m_radioHide.Checked && m_chkShowHomographNumInDict.Checked);
			hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef,
				!m_radioHide.Checked && m_chkShowHomographNumInReversal.Checked);
			hc.HomographNumberBefore = m_radioBefore.Checked;
			hc.ShowSenseNumberRef = !m_radioHide.Checked && m_chkShowSenseNumInDict.Checked;
			hc.ShowSenseNumberReversal = !m_radioHide.Checked && m_chkShowSenseNumInReversal.Checked;

		}

		private void m_radioBefore_CheckedChanged(object sender, System.EventArgs e)
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
			m_chkShowHomographNumInDict.Enabled = !m_radioHide.Checked;
			m_chkShowHomographNumInReversal.Enabled = !m_radioHide.Checked;
			m_chkShowSenseNumInDict.Enabled = !m_radioHide.Checked;
			m_chkShowSenseNumInReversal.Enabled = !m_radioHide.Checked;
			if (m_radioHide.Checked)
			{
				m_chkShowHomographNumInDict.Checked = false;
				m_chkShowHomographNumInReversal.Checked = false;
				m_chkShowSenseNumInDict.Checked = false;
				m_chkShowSenseNumInReversal.Checked = false;
			}
		}

		private void m_radioAfter_CheckedChanged(object sender, System.EventArgs e)
		{
			if (m_radioAfter.Checked)
			{
				m_radioHide.Checked = false;
				m_radioBefore.Checked = false;
			}
			EnableControls();
		}

		private void m_radioHide_CheckedChanged(object sender, System.EventArgs e)
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
				if (dlg.ShowDialog(this) == DialogResult.OK &&
					((dlg.ChangeType & StyleChangeType.DefChanged) > 0 ||
						(dlg.ChangeType & StyleChangeType.Added) > 0 ||
							(dlg.ChangeType & StyleChangeType.RenOrDel) > 0))
				{
					m_app.Synchronize(SyncMsg.ksyncStyle);
					FwStyleSheet stylesheet = new FwStyleSheet();
					stylesheet.Init(m_cache, m_cache.LangProject.Hvo, LangProjectTags.kflidStyles, ResourceHelper.DefaultParaCharsStyleName);
					m_stylesheet = stylesheet;
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
