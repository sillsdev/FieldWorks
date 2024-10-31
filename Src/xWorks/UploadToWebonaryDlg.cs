// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Gecko.WebIDL;
using SIL.FieldWorks.Common.FwUtils;
using SIL.IO;
using SIL.LCModel.Core.Phonology;
using SIL.Windows.Forms;
using SIL.PlatformUtilities;
using PropertyTable = XCore.PropertyTable;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Dialog for publishing data to Webonary web site.
	/// </summary>
	public partial class UploadToWebonaryDlg : Form, IUploadToWebonaryView
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly UploadToWebonaryController m_controller;

		private WebonaryStatusCondition m_uploadStatus = WebonaryStatusCondition.None;

		// Mono 3 handles the display of the size gripper differently than .NET SWF and so the dialog needs to be taller. Part of LT-16433.
		private const int m_additionalMinimumHeightForMono = 26;

		/// <summary>
		/// Needed to get the HelpTopicProvider and to save project specific settings
		/// </summary>
		protected PropertyTable PropertyTable { get; set; }

		public UploadToWebonaryDlg()
		{
			InitializeComponent();
			LoadFromModel();
		}

		public UploadToWebonaryDlg(UploadToWebonaryController controller, UploadToWebonaryModel model, PropertyTable propertyTable)
		{
			InitializeComponent();

			if (Platform.IsUnix)
				MinimumSize = new Size(MinimumSize.Width, MinimumSize.Height + m_additionalMinimumHeightForMono);

			m_controller = controller;
			Model = model;
			LoadFromModel();

			m_helpTopicProvider = propertyTable.GetValue<FwXApp>("App");

			// When a link is clicked, open a web page to the URL.
			explanationLabel.LinkClicked += (sender, args) =>
			{
				using (Process.Start(((LinkLabel) sender).Text.Substring(args.Link.Start, args.Link.Length)))
				{}
			};

			// Restore the location and size from last time we called this dialog.
			if (PropertyTable != null)
			{
				object locWnd = PropertyTable.GetValue<object>("UploadToWebonaryDlg_Location");
				object szWnd = PropertyTable.GetValue<object>("UploadToWebonaryDlg_Size");
				if (locWnd != null && szWnd != null)
				{
					Rectangle rect = new Rectangle((Point) locWnd, (Size) szWnd);
					ScreenHelper.EnsureVisibleRect(ref rect);
					DesktopBounds = rect;
					StartPosition = FormStartPosition.Manual;
				}
			}

			// Start with output log area not shown by default
			// When a user clicks Publish, it is revealed. This is done within the context of having a resizable table of controls, and having
			// the output log area be the vertically growing control when a user increases the height of the dialog
			Shown += (sender, args) => { ValidateSortingOnAlphaHeaders(); };

			// Handle localizable explanation area with link.
			var explanationText = xWorksStrings.toApplyForWebonaryAccountExplanation;
			var explanationTextLink = xWorksStrings.toApplyForWebonaryAccountLink;
			var explanationTextLinkStart = explanationText.IndexOf("{", StringComparison.Ordinal);
			var explanationTextLinkLength = explanationTextLink.Length;
			explanationLabel.Text = string.Format(explanationText, explanationTextLink);
			// Don't blow up if a localization didn't allow for the link.
			if (explanationTextLinkStart < 0)
			{
				explanationTextLinkStart = 0;
				explanationTextLinkLength = 0;
			}
			explanationLabel.LinkArea = new LinkArea(explanationTextLinkStart, explanationTextLinkLength);
		}

		private void siteNameBox_TextChanged(object sender, EventArgs e)
		{
			// ReSharper disable once LocalizableElement -- this is the *world-wide* web, not a LAN.
			webonarySiteURLLabel.Text = $"https://www.{UploadToWebonaryController.Server}/{webonarySiteNameTextbox.Text}";
		}

		private void UpdateEntriesToBePublishedLabel()
		{
			if (GetSelectedDictionaryModel() == null)
			{
				howManyPubsAlertLabel.Text = string.Format(xWorksStrings.ksErrorNoViewOnPublication);
				return;
			}

			var countOfDictionaryEntries = m_controller.CountDictionaryEntries(GetSelectedDictionaryModel());

			var reversalCounts = m_controller.GetCountsOfReversalIndexes(GetSelectedReversals());
			string middle = "";
			foreach (var reversalIndex in reversalCounts.Keys)
			{
				// Use commas and conjunctions as appropriate depending on if this reversal is the first and/or last in the set.
				if (reversalIndex == reversalCounts.Keys.Last())
				{
					if (reversalIndex == reversalCounts.Keys.First())
						middle += string.Format(xWorksStrings.ReversalEntries_Only, reversalCounts[reversalIndex], reversalIndex);
					else
						middle += string.Format(xWorksStrings.ReversalEntries_Last, reversalCounts[reversalIndex], reversalIndex);
				}
				else
				{
					middle += string.Format(xWorksStrings.ReversalEntries, reversalCounts[reversalIndex], reversalIndex);
				}
			}

			howManyPubsAlertLabel.Text = string.Format(xWorksStrings.PublicationEntriesLabel, countOfDictionaryEntries, middle);
		}

		public void ValidateSortingOnAlphaHeaders()
		{
			if (!m_controller.IsSortingOnAlphaHeaders())
			{
				MessageBox.Show(xWorksStrings.msgCantCalculateLetterHeadings,
					xWorksStrings.CantCalculateLetterHeadings, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
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
			var selectedPublication = publicationBox.SelectedItem.ToString();
			m_controller.ActivatePublication(selectedPublication);
			PopulateConfigurationsListByPublication(selectedPublication);
			PopulateReversalsCheckboxListByPublication(selectedPublication);
			UpdateEntriesToBePublishedLabel();
		}

		private void configurationBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateEntriesToBePublishedLabel();
		}

		private void reversalsCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateEntriesToBePublishedLabel();
		}

		private void PopulateConfigurationsListByPublication(string publication)
		{
			var selectedConfiguration = (configurationBox.SelectedItem ?? string.Empty).ToString();
			var availableConfigurations = Model.Configurations.Where(prop => prop.Value.Publications.Contains(publication))
				.Select(prop => prop.Value.Label).ToList();
			configurationBox.Items.Clear();
			foreach (var config in availableConfigurations)
			{
				configurationBox.Items.Add(config);
			}
			if (availableConfigurations.Contains(selectedConfiguration))
				configurationBox.SelectedItem = selectedConfiguration;
			else if (availableConfigurations.Count > 0)
				configurationBox.SelectedIndex = 0;
		}

		private void PopulateReversalsCheckboxListByPublication(string publication)
		{
			var selectedReversals = GetSelectedReversals();
			var availableReversals = Model.Reversals.Where(prop => prop.Value.Publications.Contains(publication)
				&& prop.Value.Label != DictionaryConfigurationModel.AllReversalIndexes
			  && !string.IsNullOrEmpty(prop.Value.WritingSystem)).Select(prop => prop.Value.Label).ToList();
			reversalsCheckedListBox.Items.Clear();
			foreach (var reversal in availableReversals)
				reversalsCheckedListBox.Items.Add(reversal);
			SetSelectedReversals(selectedReversals);
		}

		public void UploadCompleted()
		{
			m_progress.Value = m_progress.Maximum;
			m_progress.Style = ProgressBarStyle.Continuous;
			reportButton.Enabled = true;
			if (m_uploadStatus != WebonaryStatusCondition.Success)
			{
				reportButton_Click(null, null);
			}
		}

		public UploadToWebonaryModel Model { get; set; }

		private void LoadFromModel()
		{
			if(Model != null)
			{
				// Load the contents of the drop down and checkbox list controls
				PopulatePublicationsList();
				if(Model.RememberPassword)
				{
					rememberPasswordCheckbox.Checked = true;
					webonaryPasswordTextbox.Text = Model.Password;
				}
				webonaryUsernameTextbox.Text = Model.UserName;
				webonarySiteNameTextbox.Text = Model.SiteName;
				if (!String.IsNullOrEmpty(Model.SelectedPublication) && publicationBox.Items.Contains(Model.SelectedPublication))
				{
					publicationBox.SelectedItem = Model.SelectedPublication;
				}
				else
				{
					publicationBox.SelectedIndex = 0;
				}
				PopulateReversalsCheckboxListByPublication(publicationBox.SelectedItem.ToString());
				SetSelectedReversals(Model.SelectedReversals);
				if(!String.IsNullOrEmpty(Model.SelectedConfiguration))
				{
					configurationBox.SelectedItem = Model.SelectedConfiguration;
				}
				else
				{
					configurationBox.SelectedIndex = 0;
				}
				UpdateEntriesToBePublishedLabel();
				reportButton.Enabled = Model.CanViewReport;
			}
		}

		private void SaveToModel()
		{
			Model.RememberPassword = rememberPasswordCheckbox.Checked;
			Model.Password = webonaryPasswordTextbox.Text;
			Model.UserName = webonaryUsernameTextbox.Text;
			Model.SiteName = UploadToWebonaryController.NormalizeSiteName(webonarySiteNameTextbox.Text);
			Model.SelectedReversals = GetSelectedReversals();
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

		private void SetSelectedReversals(ICollection<string> selectedReversals)
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

		private List<string> GetSelectedReversals()
		{
			return (from object item in reversalsCheckedListBox.CheckedItems select item.ToString()).ToList();
		}

		private DictionaryConfigurationModel GetSelectedDictionaryModel()
		{
			return configurationBox.SelectedItem == null
				? null
				: Model.Configurations[configurationBox.SelectedItem.ToString()];
		}

		private void publishButton_Click(object sender, EventArgs e)
		{
			SaveToModel();

			using (new WaitCursor(this))
			{
				RobustFile.Delete(Model.LastUploadReport);
				m_progress.Style = ProgressBarStyle.Marquee;
				m_controller.UploadToWebonary(Model, this);
			}
		}

		private void helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpUploadToWebonary");
		}

		private void closeButton_Click(object sender, EventArgs e)
		{
			SaveToModel();
		}

		private void reportButton_Click(object sender, EventArgs e)
		{
			using(var dlg = new WebonaryLogViewer(Model.LastUploadReport))
			{
				dlg.ShowDialog();
			}
		}

		/// <summary>
		/// Add a message to the status area. Make sure the status area is redrawn so the
		/// user can see what's going on even if we are working on something.
		/// </summary>
		public void UpdateStatus(string statusString, WebonaryStatusCondition c)
		{
			// Set the status to the greater of the current or the new update
			m_uploadStatus = (WebonaryStatusCondition)Math.Max(Convert.ToInt32(m_uploadStatus), Convert.ToInt32(c));
			// Log the status
			Model.Log.AddEntry(c, statusString);
			// pump messages
			Application.DoEvents();
		}

		/// <summary>
		/// Save the location and size for next time.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			if (PropertyTable != null)
			{
				PropertyTable.SetProperty("UploadToWebonaryDlg_Location", Location, false);
				PropertyTable.SetPropertyPersistence("UploadToWebonaryDlg_Location", true);
				PropertyTable.SetProperty("UploadToWebonaryDlg_Size", Size, false);
				PropertyTable.SetPropertyPersistence("UploadToWebonaryDlg_Size", true);
			}
			base.OnClosing(e);
		}
	}

	/// <summary>
	/// Interface for controller to interact with the dialog
	/// </summary>
	public interface IUploadToWebonaryView
	{
		void UpdateStatus(string statusString, WebonaryStatusCondition condition);
		void UploadCompleted();
		UploadToWebonaryModel Model { get; set; }
	}

	/// <summary>
	/// Condition of status of uploading to webonary.
	/// </summary>
	public enum WebonaryStatusCondition
	{
		None,
		Success,
		FileRejected,
		Error,
	}
}
