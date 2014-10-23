// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using SIL.CoreImpl.Properties;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Dialog for publishing data to Webonary web site.
	/// </summary>
	public partial class PublishToWebonaryDlg : Form, IPublishToWebonaryView
	{
		private IHelpTopicProvider m_helpTopicProvider;

		// This value gets used by the microsoft encryption library to increase the complexity of the encryption
		private const string EntropyValue = @"61:3nj 42 ebg68";
		private const string WebonarySite = "WebonarySite_ProjectSetting";
		private const string WebonaryReversals = "WebonaryReversals_ProjectSetting";
		private const string WebonaryPublication = "WebonaryPublication_ProjectSetting";
		private const string WebonaryConfiguration = "WebonaryConfiguration_ProjectSetting";
		/// <summary>
		/// Unicode line break character (will never appear in a reversal config name)
		/// </summary>
		private const char Separator = '\u2028';

		private PublishToWebonaryController m_controller;

		/// <summary>
		/// Needed to get the HelpTopicProvider and to save project specific settings
		/// </summary>
		protected Mediator Mediator { get; set; }

		public PublishToWebonaryDlg()
		{
			InitializeComponent();
			LoadFromSettings();
		}

		public PublishToWebonaryDlg(PublishToWebonaryController controller, Mediator mediator)
		{
			InitializeComponent();
			m_controller = controller;
			controller.PopulateReversalsCheckboxList(this);
			controller.PopulateConfigurationsList(this);
			controller.PopulatePublicationsList(this);
			Mediator = mediator;
			LoadFromSettings();

			m_helpTopicProvider = mediator.HelpTopicProvider;

			// When a link is clicked, open a web page to the URL.
			explanationLabel.LinkClicked += (sender, args) =>
			{
				using (Process.Start(((LinkLabel) sender).Text.Substring(args.Link.Start, args.Link.Length)))
				{};
			};

			// Start with output log area not shown by default
			// When a user clicks Publish, it is revealed. This is done within the context of having a resizable table of controls, and having
			// the output log area be the vertically growing control when a user increases the height of the dialog.
			this.Shown += (sender, args) => { this.Height = tableLayoutPanel.Height - outputLogTextbox.Height; };
		}

		public void PopulatePublicationsList(IEnumerable<string> publications)
		{
			foreach(var pub in publications)
			{
				publicationBox.Items.Add(pub);
			}
		}

		public void PopulateConfigurationsList(IEnumerable<string> configurations)
		{
			foreach(var config in configurations)
			{
				configurationBox.Items.Add(config);
			}
		}

		public void PopulateReversalsCheckboxList(IEnumerable<string> reversals)
		{
			foreach(var reversal in reversals)
			{
				reversalsCheckedListBox.Items.Add(reversal);
			}
		}

		public string Configuration { get { return configurationBox.SelectedItem.ToString(); } }
		public string Publication { get { return publicationBox.SelectedItem.ToString(); } }
		public List<string> Reversals
		{
			get
			{
				return (from object item in reversalsCheckedListBox.CheckedItems select item.ToString()).ToList();
			}
		}
		public string UserName { get { return webonaryUsernameTextbox.Text; } }
		public string Password { get { return webonaryPasswordTextbox.Text; } }
		public string SiteName { get { return webonarySiteNameTextbox.Text; } }

		private void LoadFromSettings()
		{
			if (!string.IsNullOrEmpty(Settings.Default.WebonaryPass))
			{
				rememberPasswordCheckbox.Checked = true;
				webonaryPasswordTextbox.Text = DecryptPassword(Settings.Default.WebonaryPass);
			}
			webonaryUsernameTextbox.Text = Settings.Default.WebonaryUser;
			if(Mediator != null)
			{
				var projectSettings = Mediator.PropertyTable;
				webonarySiteNameTextbox.Text = projectSettings.GetStringProperty(WebonarySite, null);
				var reversals = SplitReversalSettingString(projectSettings.GetStringProperty(WebonaryReversals, null));
				//Check every reversal in the list that was in the settings
				if(reversals != null)
				{
					for(var i = 0; i < reversalsCheckedListBox.Items.Count; ++i)
					{
						if(reversals.Contains(reversalsCheckedListBox.Items[i].ToString()))
						{
							reversalsCheckedListBox.SetItemChecked(i, true);
						}
					}
				}
				var savedConfig = projectSettings.GetStringProperty(WebonaryConfiguration, null);
				if(!String.IsNullOrEmpty(savedConfig))
				{
					configurationBox.SelectedItem = savedConfig;
				}
				else
				{
					configurationBox.SelectedIndex = 0;
				}

				var savedPub = projectSettings.GetStringProperty(WebonaryPublication, null);
				if(!String.IsNullOrEmpty(savedPub))
				{
					publicationBox.SelectedItem = savedPub;
				}
				else
				{
					publicationBox.SelectedIndex = 0;
				}
			}
		}

		private void SaveToSettings()
		{
			Settings.Default.WebonaryPass = rememberPasswordCheckbox.Checked ? EncryptPassword(webonaryPasswordTextbox.Text) : null;
			Settings.Default.WebonaryUser = webonaryUsernameTextbox.Text;

			var projectSettings = Mediator.PropertyTable;
			projectSettings.SetProperty(WebonarySite, webonarySiteNameTextbox.Text, false);
			projectSettings.SetPropertyPersistence(WebonarySite, true);
			projectSettings.SetProperty(WebonaryReversals, CombineReversalSettingStrings(reversalsCheckedListBox.CheckedItems), false);
			projectSettings.SetPropertyPersistence(WebonaryReversals, true);
			if(configurationBox.SelectedItem != null)
			{
				projectSettings.SetProperty(WebonaryConfiguration, configurationBox.SelectedItem.ToString(), false);
				projectSettings.SetPropertyPersistence(WebonaryConfiguration, true);
			}
			if(publicationBox.SelectedItem != null)
			{
				projectSettings.SetProperty(WebonaryPublication, publicationBox.SelectedItem.ToString(), false);
				projectSettings.SetPropertyPersistence(WebonaryPublication, true);
			}
			projectSettings.SaveGlobalSettings();
			Settings.Default.Save();
		}

		/// <summary>
		/// We don't have code to persist collections of strings in the project settings, so we'll combine our list into
		/// a single string and split it.
		/// </summary>
		private string CombineReversalSettingStrings(CheckedListBox.CheckedItemCollection checkedItems)
		{
			var stringSettingBldr = new StringBuilder();
			foreach(var item in checkedItems)
			{
				stringSettingBldr.Append(item);
				stringSettingBldr.Append(Separator); // use a unicode line break as a seperator character
			}
			return stringSettingBldr.ToString();
		}

		/// <summary>
		/// This method will split the given reversal string and return the resulting list
		/// </summary>
		private IEnumerable<string> SplitReversalSettingString(string savedReversalList)
		{
			if(!String.IsNullOrEmpty(savedReversalList))
			{
				return savedReversalList.Split(new [] {Separator}, StringSplitOptions.RemoveEmptyEntries);
			}
			return null;
		}

		internal static string EncryptPassword(string encryptMe)
		{
			if(!String.IsNullOrEmpty(encryptMe))
			{
				byte[] encryptedData = ProtectedData.Protect(Encoding.Unicode.GetBytes(encryptMe), Encoding.Unicode.GetBytes(EntropyValue), DataProtectionScope.CurrentUser);
				return Convert.ToBase64String(encryptedData);
			}
			return encryptMe;
		}

		internal static string DecryptPassword(string decryptMe)
		{
			if(!String.IsNullOrEmpty(decryptMe))
			{
				byte[] decryptedData = ProtectedData.Unprotect(Convert.FromBase64String(decryptMe), Encoding.Unicode.GetBytes(EntropyValue), DataProtectionScope.CurrentUser);
				return Encoding.Unicode.GetString(decryptedData);
			}
			return decryptMe;
		}

		private void publishButton_Click(object sender, EventArgs e)
		{
			SaveToSettings();

			// Increase height of form so the output log is shown.
			// Account for situations where the user already increased the height of the form
			// or maximized the form, and later reduces the height or unmaximizes the form
			// after clicking Publish.

			var allButTheLogRowHeight = this.tableLayoutPanel.GetRowHeights().Sum() - this.tableLayoutPanel.GetRowHeights().Last();
			var fudge = this.Height - this.tableLayoutPanel.Height;
			var minimumFormHeightToShowLog = allButTheLogRowHeight + this.outputLogTextbox.MinimumSize.Height + fudge;
			this.MinimumSize = new Size(this.MinimumSize.Width, minimumFormHeightToShowLog);

			m_controller.PublishToWebonary(this);
		}


		private void helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpPublishToWebonary");
		}

		public void UpdateStatus(string statusString)
		{
			outputLogTextbox.Text += Environment.NewLine + statusString;
		}
	}

	/// <summary>
	/// Interface for controller to interact with the dialog
	/// </summary>
	public interface IPublishToWebonaryView
	{
		void UpdateStatus(string statusString);
		void PopulatePublicationsList(IEnumerable<string> publications);
		void PopulateConfigurationsList(IEnumerable<string> configurations);
		void PopulateReversalsCheckboxList(IEnumerable<string> reversals);
		string Configuration { get; }
		string Publication { get; }
		string SiteName { get; }
		string UserName { get; }
		string Password { get; }
	}
}
