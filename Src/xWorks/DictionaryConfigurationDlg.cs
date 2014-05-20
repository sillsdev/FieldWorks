// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Gecko;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;

namespace SIL.FieldWorks.XWorks
{
	public partial class DictionaryConfigurationDlg : Form, IDictionaryConfigurationView
	{
		/// <summary>
		/// When manage views is clicked tell the controller to launch the dialog where different
		/// dictionary configurations (or views) are managed.
		/// </summary>
		public event EventHandler ManageConfigurations;

		public event SwitchConfigurationEvent SwitchConfiguration;

		/// <summary>
		/// When OK or Apply are clicked tell anyone who is listening to do their save.
		/// </summary>
		public event EventHandler SaveModel;

		public DictionaryConfigurationDlg()
		{
			InitializeComponent();
			m_preview.Dock = DockStyle.Fill;
			m_preview.Location = new Point(0, 0);
			previewDetailSplit.Panel1.Controls.Add(m_preview);
			manageConfigs_treeDetailButton_split.IsSplitterFixed = true;
			treeDetail_Button_Split.IsSplitterFixed = true;
		}

		public DictionaryConfigurationTreeControl TreeControl
		{
			get { return treeControl; }
		}

		public DetailsView DetailsView
		{
			set
			{
				if(detailsView != null)
					detailsView.Dispose();

				detailsView = value;
				previewDetailSplit.Panel2.Controls.Add(detailsView);
				detailsView.Dock = DockStyle.Fill;
				detailsView.Location = new Point(0, 0);
			}
		}

		public string PreviewData
		{
			set
			{
				//The second parameter is only used if the string data in the first parameter is unusable but it must be
				//set to a valid Uri
				((GeckoWebBrowser)m_preview.NativeBrowser).LoadContent(value, "file:///c:/MayNotExist/doesnotmatter.html", "application/xhtml+xml");
				m_preview.Refresh();
			}
		}

		public void Redraw()
		{
			Invalidate(true);
		}

		public void SetChoices(IEnumerable<string> choices)
		{
			m_cbDictConfig.Items.Clear();
			if(choices != null)
			{
				foreach(var choice in choices)
				{
					m_cbDictConfig.Items.Add(choice);
				}
			}
		}

		public void ShowPublicationsForConfiguration(String publications)
		{
			m_txtPubsForConfig.Text = publications;
		}

		public void SelectConfiguration(string configuration)
		{
			m_cbDictConfig.SelectedItem = configuration;
		}

		private void m_linkManageConfigurations_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ManageConfigurations(sender, e);
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			SaveModel(sender, e);
			Close();
		}

		private void applyButton_Click(object sender, EventArgs e)
		{
			SaveModel(sender, e);
		}

		private void OnConfigurationChanged(object sender, EventArgs e)
		{
			SwitchConfiguration(sender, new SwitchConfigurationEventArgs { ConfigurationPicked = m_cbDictConfig.SelectedItem.ToString() });
		}
	}
}
