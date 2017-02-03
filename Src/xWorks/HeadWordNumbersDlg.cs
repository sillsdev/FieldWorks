// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Handles the display and manipulation of HeadWordNumbers
	/// </summary>
	public partial class HeadwordNumbersDlg : Form, IHeadwordNumbersView
	{
		public HeadwordNumbersDlg()
		{
			InitializeComponent();
			// Initially we want the combo box to show the user what the style is but not let them change it.
			// Allowing the user to pick the style will be added only at user request.
			_stylesCombo.Items.Add(HomographConfiguration.ksHomographNumberStyle);
			_stylesCombo.SelectedIndex = 0;
			_stylesCombo.Enabled = false;
		}

		/// <summary>
		/// convert the sender to the styles combo box for processing by the controller
		/// </summary>
		private EventHandler ButtonStylesOnClick(EventHandler handler) { return (sender, e) => handler(_stylesCombo, e); }

		/// <summary>Fired when the Styles... button is clicked. Object sender is the Style ComboBox so it can be updated</summary>
		public event EventHandler RunStylesDialog
		{
			add { _stylesButton.Click += ButtonStylesOnClick(value); }
			remove { _stylesButton.Click -= ButtonStylesOnClick(value); }
		}

		public DictionaryHomographConfiguration HomographConfig
		{
			get; private set;
		}

		public bool HomographBefore
		{
			get { return m_radioBefore.Checked; }
			set
			{
				m_radioBefore.Checked = value;
				m_radioAfter.Checked = !value;
				m_radioNone.Checked = false;
			}
		}

		public bool ShowHomographOnCrossRef
		{
			get { return m_chkShowHomographNumInDict.Checked; }
			set
			{
				m_chkShowHomographNumInDict.Checked = value;
				m_chkShowSenseNumber.Enabled = value;
			}
		}

		public bool ShowSenseNumber
		{
			get { return m_chkShowSenseNumber.Checked; }
			set { m_chkShowSenseNumber.Checked = value; }
		}

		public bool ShowHomograph
		{
			get { return !m_radioNone.Checked; }
			set
			{
				if (!value)
				{
					m_radioBefore.Checked = false;
					m_radioAfter.Checked = false;
					m_radioNone.Checked = true;
				}
				else if (!m_radioBefore.Checked && !m_radioAfter.Checked)
				{
					// If we turn the numbers on and it isn't set to before or after default to after
					m_radioAfter.Checked = true;
				}
			}
		}

		public string Description
		{
			get { return m_configurationDescription.Text; }
			set { m_configurationDescription.Text = value; }
		}

		private IHelpTopicProvider m_helpTopicProvider;
		protected HelpProvider m_helpProvider;

		protected string m_helpTopic = ""; // Default help topic ID

		public string CurrentHomographStyle
		{
			get { return _stylesCombo.Items[0].ToString(); }
			set { _stylesCombo.Items[0] = value; }
		}

		public void SetupDialog(IHelpTopicProvider helpTopicProvider)
		{
			SetHelpTopic("khtpConfigureHeadwordNumbers"); // Default help topic ID
			m_helpProvider = new HelpProvider();
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);
			m_helpTopicProvider = helpTopicProvider;
			if (m_helpTopicProvider != null)
			{
				m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				SetHelpButtonEnabled();
			}
		}

		/// <summary>
		/// Sets the help topic ID for the window.  This is used in both the Help button and when the user hits F1
		/// </summary>
		public void SetHelpTopic(string helpTopic)
		{
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

		/// <summary>
		/// Display help for this dialog.
		/// </summary>
		protected void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}

		private void m_chkShowHomographNumInDict_CheckedChanged(object sender, EventArgs e)
		{
			ShowHomographOnCrossRef = m_chkShowHomographNumInDict.Checked;
		}
	}
}
