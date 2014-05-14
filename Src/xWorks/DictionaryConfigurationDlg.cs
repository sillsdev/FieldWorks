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
		public event EventHandler ManageViews;

		public event SwitchViewEvent SwitchView;

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
			manageViews_viewSplit.IsSplitterFixed = true;
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

				// TODO pH 2014.02: ensure adequate size
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
			m_cbDictType.Items.Clear();
			if(choices != null)
			{
				foreach(var choice in choices)
				{
					m_cbDictType.Items.Add(choice);
				}
			}
		}

		private void m_linkManageViews_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ManageViews(sender, e);
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

		private void OnViewChanged(object sender, EventArgs e)
		{
			SwitchView(sender, new SwitchViewEventArgs { ViewPicked = m_cbDictType.SelectedItem.ToString() });
		}
	}
}
