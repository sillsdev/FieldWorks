// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Dialog for publishing data to Webonary web site.
	/// </summary>
	public partial class PublishToWebonaryDlg : Form, IPublishToWebonaryView
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		private readonly PublishToWebonaryController m_controller;

		/// <summary>
		/// Needed to get the HelpTopicProvider and to save project specific settings
		/// </summary>
		protected Mediator Mediator { get; set; }

		public PublishToWebonaryDlg()
		{
			InitializeComponent();
			LoadFromModel();
		}

		public PublishToWebonaryDlg(PublishToWebonaryController controller, PublishToWebonaryModel model, Mediator mediator)
		{
			InitializeComponent();
			m_controller = controller;
			Mediator = mediator;
			Model = model;
			LoadFromModel();

			m_helpTopicProvider = mediator.HelpTopicProvider;

			// When a link is clicked, open a web page to the URL.
			explanationLabel.LinkClicked += (sender, args) =>
			{
				using (Process.Start(((LinkLabel) sender).Text.Substring(args.Link.Start, args.Link.Length)))
				{}
			};

			// Start with output log area not shown by default
			// When a user clicks Publish, it is revealed. This is done within the context of having a resizable table of controls, and having
			// the output log area be the vertically growing control when a user increases the height of the dialog.
			this.Shown += (sender, args) => { this.Height = this.Height - outputLogTextbox.Height; };
		}

		private void PopulatePublicationsList()
		{
			foreach(var pub in Model.Publications)
			{
				publicationBox.Items.Add(pub);
			}
		}

		private void PopulateConfigurationsList()
		{
			foreach(var config in Model.Configurations.Keys)
			{
				configurationBox.Items.Add(config);
			}
		}

		private void PopulateReversalsCheckboxList()
		{
			foreach(var reversal in Model.Reversals)
			{
				reversalsCheckedListBox.Items.Add(reversal);
			}
		}

		public PublishToWebonaryModel Model { get; set; }

		private void LoadFromModel()
		{
			if(Model != null)
			{
				// Load the contents of the drop down and checkbox list controls
				PopulatePublicationsList();
				PopulateConfigurationsList();
				PopulateReversalsCheckboxList();

				if(Model.RememberPassword)
				{
					rememberPasswordCheckbox.Checked = true;
					webonaryPasswordTextbox.Text = Model.Password;
				}
				webonaryUsernameTextbox.Text = Model.UserName;
				webonarySiteNameTextbox.Text = Model.SiteName;
				SetSelectedReversals(Model.SelectedReversals);
				if(!String.IsNullOrEmpty(Model.SelectedConfiguration))
				{
					configurationBox.SelectedItem = Model.SelectedConfiguration;
				}
				else
				{
					configurationBox.SelectedIndex = 0;
				}
				if(!String.IsNullOrEmpty(Model.SelectedPublication))
				{
					publicationBox.SelectedItem = Model.SelectedPublication;
				}
				else
				{
					publicationBox.SelectedIndex = 0;
				}
			}
		}

		private void SaveToModel()
		{
			Model.RememberPassword = rememberPasswordCheckbox.Checked ? true : false;
			Model.Password = webonaryPasswordTextbox.Text;
			Model.UserName = webonaryUsernameTextbox.Text;
			Model.SiteName = webonarySiteNameTextbox.Text;
			Model.Reversals = GetSelectedReversals();
			if(configurationBox.SelectedItem != null)
			{
				Model.SelectedConfiguration = configurationBox.SelectedItem.ToString();
			}
			if(publicationBox.SelectedItem != null)
			{
				Model.SelectedPublication = publicationBox.SelectedItem.ToString();
			}
			Model.SaveToSettings();
		}

		private void SetSelectedReversals(IEnumerable<string> selectedReversals)
		{
			if(selectedReversals == null)
				return;
			//Check every reversal in the list that was in the given list (e.g. from settings)
			for(var i = 0; i < reversalsCheckedListBox.Items.Count; ++i)
			{
				if(selectedReversals.Contains(reversalsCheckedListBox.Items[i].ToString()))
				{
					reversalsCheckedListBox.SetItemChecked(i, true);
				}
			}
		}

		private IEnumerable<string> GetSelectedReversals()
		{
			return (from object item in reversalsCheckedListBox.CheckedItems select item.ToString()).ToList();
		}

		private void publishButton_Click(object sender, EventArgs e)
		{
			SaveToModel();

			// Increase height of form so the output log is shown.
			// Account for situations where the user already increased the height of the form
			// or maximized the form, and later reduces the height or unmaximizes the form
			// after clicking Publish.

			var allButTheLogRowHeight = this.tableLayoutPanel.GetRowHeights().Sum() - this.tableLayoutPanel.GetRowHeights().Last();
			var fudge = this.Height - this.tableLayoutPanel.Height;
			var minimumFormHeightToShowLog = allButTheLogRowHeight + this.outputLogTextbox.MinimumSize.Height + fudge;
			this.MinimumSize = new Size(this.MinimumSize.Width, minimumFormHeightToShowLog);

			m_controller.PublishToWebonary(Model, this);
		}


		private void helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpPublishToWebonary");
		}

		public void UpdateStatus(string statusString)
		{
			outputLogTextbox.Text += Environment.NewLine + statusString;
		}

		private void closeButton_Click(object sender, EventArgs e)
		{
			SaveToModel();
		}
	}

	/// <summary>
	/// Interface for controller to interact with the dialog
	/// </summary>
	public interface IPublishToWebonaryView
	{
		void UpdateStatus(string statusString);
		PublishToWebonaryModel Model { get; set; }
	}
}
