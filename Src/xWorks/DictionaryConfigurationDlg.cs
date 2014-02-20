// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
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

		/// <summary>
		/// When OK or Apply are clicked tell anyone who is listening to do their save.
		/// </summary>
		public event EventHandler SaveModel;

		public DictionaryConfigurationDlg()
		{
			InitializeComponent();
		}

		public DictionaryConfigurationDlg(Mediator mediator)
		{
			InitializeComponent();
			InitializeDictionaryPubPreview(mediator);
			manageViews_viewSplit.IsSplitterFixed = true;
			treeDetail_Button_Split.IsSplitterFixed = true;
		}


		// TODO pH 2014.02: this method is a hack job and must be replaced.  For developer use only.
		private void InitializeDictionaryPubPreview(Mediator mediator)
		{
			m_preview = new RecordDocXmlView();
			previewDetailSplit.Panel1.Controls.Add(m_preview);
			var previewConfiguration = new XmlDocument();
			previewConfiguration.Load(Path.Combine(Path.Combine(Path.Combine(Path.Combine(DirectoryFinder.FlexFolder, "Configuration"), "Lexicon"), "Dictionary"), "toolConfiguration.xml"));
			var parameters = previewConfiguration.SelectSingleNode("/root/reusableControls/control[@id='DictionaryPubPreviewControl']/parameters");
			var clerks = previewConfiguration.CreateElement("clerks");
			var clerk = previewConfiguration.CreateElement("clerk");
			clerk.SetAttribute("id", "entries");
			clerks.AppendChild(clerk);
			parameters.AppendChild(clerks);
			m_preview.Init(mediator, parameters);
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

	}
}
