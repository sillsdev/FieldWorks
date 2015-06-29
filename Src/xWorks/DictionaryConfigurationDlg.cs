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

		public IDictionaryDetailsView DetailsView
		{
			set
			{
				if(detailsView == null)
				{
					detailsView = (DetailsView)value;
					previewDetailSplit.Panel2.Controls.Add(detailsView);
					detailsView.Dock = DockStyle.Fill;
					detailsView.Location = new Point(0, 0);
				}
			}
		}

		public string PreviewData
		{
			set
			{
				// Set the preview content when all else has settled, this is really here so that the preview displays properly on the
				// initial dialog load. The GeckoWebBrowser is supposed to handle setting the content before it becomes visible, but it
				// doesn't work
				EventHandler refreshDelegate = null;
				refreshDelegate = delegate(object sender, EventArgs e)
				{
					// Since we are handling this delayed the dialog may have been closed before we get around to it
					if(!m_preview.IsDisposed)
					{
						var browser = (GeckoWebBrowser)m_preview.NativeBrowser;
						// Workaround to prevent the Gecko browser from stealing focus each time we set the PreviewData
						browser.WebBrowserFocus.Deactivate();
						// The second parameter is used only if the string data in the first parameter is unusable,
						// but it must be set to a valid Uri
						browser.LoadContent(value, "file:///c:/MayNotExist/doesnotmatter.html", "application/xhtml+xml");
						m_preview.Refresh();
						Application.Idle -= refreshDelegate;
					}
				};
				Application.Idle += refreshDelegate;
			}
		}

		public void Redraw()
		{
			Invalidate(true);
		}

		public void SetChoices(IEnumerable<DictionaryConfigurationModel> choices)
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

		public void SelectConfiguration(DictionaryConfigurationModel configuration)
		{
			m_cbDictConfig.SelectedItem = configuration;
			if(treeControl.Tree.Nodes.Count > 0)
			{
				treeControl.Tree.Nodes[0].Expand();
			}
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
			SwitchConfiguration(sender, new SwitchConfigurationEventArgs
			{
				ConfigurationPicked = (DictionaryConfigurationModel)m_cbDictConfig.SelectedItem
			});
		}
	}
}
