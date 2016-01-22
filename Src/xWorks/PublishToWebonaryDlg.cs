// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Dialog for publishing data to Webonary web site.
	/// </summary>
	public partial class PublishToWebonaryDlg : Form, IPublishToWebonaryView
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private const string reversalType = "Reversal Index";
		private const string dictionaryType = "Dictionary";
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

			// Restore the location and size from last time we called this dialog.
			if (Mediator != null && Mediator.PropertyTable != null)
			{
				object locWnd = Mediator.PropertyTable.GetValue("PublishToWebonaryDlg_Location");
				object szWnd = Mediator.PropertyTable.GetValue("PublishToWebonaryDlg_Size");
				if (locWnd != null && szWnd != null)
				{
					Rectangle rect = new Rectangle((Point) locWnd, (Size) szWnd);
					ScreenUtils.EnsureVisibleRect(ref rect);
					DesktopBounds = rect;
					StartPosition = FormStartPosition.Manual;
				}
			}
			// Start with output log area not shown by default
			// When a user clicks Publish, it is revealed. This is done within the context of having a resizable table of controls, and having
			// the output log area be the vertically growing control when a user increases the height of the dialog
			this.Shown += (sender, args) => { this.Height = this.Height - outputLogTextbox.Height; };
		}

		private void UpdateEntriesToBePublishedLabel()
		{
			var clerk = Mediator.PropertyTable.GetValue("ActiveClerk", null) as RecordClerk;
			if (clerk != null)
			{
				int[] entriesToSave;
				if (clerk.Id == "entries")
				{
					ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(Mediator, out entriesToSave, dictionaryType);
				}
				else
				{
					GetDictioneryEntriesfromNewClerk(Mediator, dictionaryType, out entriesToSave);
					clerk.ActivateUI(true);
				}
				howManyPubsAlertLabel.Text = string.Format(xWorksStrings.PublicationEntriesLabel, entriesToSave.Count(),
					GetReversalentriesCount());
			}
		}

		private int GetReversalentriesCount()
		{
			// TODO: we need to add some logic to retrive reversal entry based on Selected publication in future.

			var mCache = (FdoCache)Mediator.PropertyTable.GetValue("cache");
			var reversalrepo = mCache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances();
			return reversalrepo.Select(repo => mCache.ServiceLocator.GetObject(repo.Guid) as IReversalIndex)
				.Where(reversalindex => reversalindex != null && GetSelectedReversals().Contains(reversalindex.ShortName))
				.Sum(reversalindex => reversalindex.AllEntries.Count);
		}

		private void GetDictioneryEntriesfromNewClerk(Mediator mediator, string clerkType, out int[] entriesToSave)
		{
			RecordClerk newclerk = CreateClerkMatchingDictionaryType(mediator, clerkType);
			newclerk.ActivateUI(true);
			newclerk.UpdateList(true, true);
			ConfiguredXHTMLGenerator.GetPublicationDecoratorAndEntries(Mediator, out entriesToSave, clerkType);
		}

		private RecordClerk CreateClerkMatchingDictionaryType(Mediator m_mediator, string dictionaryType)
		{
			XmlNode parameters = null;
			var area = "lexicon";
			var tool = "lexiconDictionary";
			var entriestype = "entries";
			if (dictionaryType == reversalType)
			{
				tool = "reversalToolEditComplete";
				entriestype = "AllReversalEntries";
			}
			var collector = new XmlNode[1];
			var parameter = new Tuple<string, string, XmlNode[]>(area, tool, collector);
			m_mediator.SendMessage("GetContentControlParameters", parameter);
			parameters = collector[0].SelectSingleNode("//parameters[@clerk='" + entriestype + "']");
			return RecordClerkFactory.CreateClerk(m_mediator, parameters, true);
		}

		private void PopulatePublicationsList()
		{
			foreach (var pub in Model.Publications)
			{
				publicationBox.Items.Add(pub);
			}
		}

		private void publicationBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			PopulateConfigurationsListBySelectedPublication();
		}

		private void configurationBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateEntriesToBePublishedLabel();
		}

		private void reversalsCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateEntriesToBePublishedLabel();
		}

		private void PopulateConfigurationsListBySelectedPublication()
		{
			var selectedConfiguration =
				Model.Configurations.Where(prop => prop.Value.Publications.Contains(publicationBox.SelectedItem.ToString())).ToList();
			configurationBox.Items.Clear();
			foreach (var config in selectedConfiguration)
			{
				configurationBox.Items.Add(config.Value.Label);
			}
			if (selectedConfiguration.Count > 0)
				configurationBox.SelectedIndex = 0;
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
				PopulateReversalsCheckboxList();

				if(Model.RememberPassword)
				{
					rememberPasswordCheckbox.Checked = true;
					webonaryPasswordTextbox.Text = Model.Password;
				}
				webonaryUsernameTextbox.Text = Model.UserName;
				webonarySiteNameTextbox.Text = Model.SiteName;
				SetSelectedReversals(Model.SelectedReversals);
				if (!String.IsNullOrEmpty(Model.SelectedPublication))
				{
					publicationBox.SelectedItem = Model.SelectedPublication;
				}
				else
				{
					publicationBox.SelectedIndex = 0;
				}
				if(!String.IsNullOrEmpty(Model.SelectedConfiguration))
				{
					configurationBox.SelectedItem = Model.SelectedConfiguration;
				}
				else
				{
					configurationBox.SelectedIndex = 0;
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

		/// <summary>
		/// Save the location and size for next time.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			if (Mediator != null)
			{
				Mediator.PropertyTable.SetProperty("PublishToWebonaryDlg_Location", Location, false);
				Mediator.PropertyTable.SetPropertyPersistence("PublishToWebonaryDlg_Location", true);
				Mediator.PropertyTable.SetProperty("PublishToWebonaryDlg_Size", Size, false);
				Mediator.PropertyTable.SetPropertyPersistence("PublishToWebonaryDlg_Size", true);
			}
			base.OnClosing(e);
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
