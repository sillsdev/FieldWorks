// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;
using XCore;

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
		}

		// Review - leaving Mediator here for now due to the likelihood of needing it when the preview code is completed
		public DictionaryConfigurationDlg(Mediator mediator)
		{
			InitializeComponent();
			InitializeDictionaryPubPreview();
			manageViews_viewSplit.IsSplitterFixed = true;
			treeDetail_Button_Split.IsSplitterFixed = true;
		}

		private void InitializeDictionaryPubPreview()
		{
			//TODO apolk 2014.02
			//If using a file, use Navigate with the path
			m_preview.Navigate(Path.Combine(DirectoryFinder.FWDataDirectory, "SamplePreviewToBeDeleted.xhtml"));

			//Otherwise, LoadHtml isn't current exposed to us from GeckoWebBrowser
			//Options:
			//1. Change XWebBrowser to expose LoadHtml (the reason it isn't there now is there is no directly correlating method for the Windows Forms Browser)
			//2. Use m_preview.NativeBrowser and cast to GeckoWebBrowser (as below. We would also need to add references to the project.)
			//3. Write to a temp file
			//((GeckoWebBrowser)m_preview.NativeBrowser).LoadHtml(GetHtmlForPreview());
		}
		// TODO apolk 2014.02: This is temporary and for developer use only.  Needs to be removed.
		private string GetHtmlForPreview()
		{
			return "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"utf-8\" lang=\"utf-8\">" +
				"<head>" +
					"<style>" +
					".grammatical-info {" +
						"font-family: \"Charis SIL\", serif;" +
						"font-size: 10pt;" +
						"font-style: italic;" +
					"}" +
					"</style>" +
				"</head>" +
				"<body>" +
				"<div class=\"entry\">" +
				"<span class=\"headword\" lang=\"fr\">entry<span lang=\"en\" xml:space=\"preserve\">  </span></span><span class=\"senses\"><span class=\"sense\" id=\"hvo3122\"><span class=\"grammatical-info\"><span class=\"partofspeech\" lang=\"en\"><span xml:space=\"preserve\" lang=\"en\">n</span><span lang=\"en\" xml:space=\"preserve\"> </span></span><span lang=\"en\" xml:space=\"preserve\"> </span></span><span class=\"definition\" lang=\"en\"><span xml:space=\"preserve\" lang=\"en\">gloss</span><span lang=\"en\" xml:space=\"preserve\"> </span></span></span></span>" +
				"</div>" +
				"</body>" +
				"</html>";
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
