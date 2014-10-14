// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

		private PublishToWebonaryController m_controller;

		public PublishToWebonaryDlg()
		{
			InitializeComponent();
			LoadFromSettings();
		}

		public PublishToWebonaryDlg(PublishToWebonaryController controller, IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();
			m_controller = controller;
			controller.PopulateReversalsCheckboxList(this);
			controller.PopulateConfigurationsList(this);
			controller.PopulatePublicationsList(this);
			LoadFromSettings();

			m_helpTopicProvider = helpTopicProvider;

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
			webonarySiteNameTextbox.Text = Settings.Default.WebonarySite;
			var reversals = Settings.Default.WebonaryReversals;
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
			var savedConfig = Settings.Default.WebonaryConfiguration;
			if(!String.IsNullOrEmpty(savedConfig))
				configurationBox.SelectedItem = savedConfig;

			var savedPub = Settings.Default.WebonaryPublication;
			if(!String.IsNullOrEmpty(savedPub))
				publicationBox.SelectedItem = savedPub;
		}

		private void SaveToSettings()
		{
			Settings.Default.WebonaryPass = rememberPasswordCheckbox.Checked ? EncryptPassword(webonaryPasswordTextbox.Text) : null;
			Settings.Default.WebonaryUser = webonaryUsernameTextbox.Text;
			Settings.Default.WebonarySite = webonarySiteNameTextbox.Text;
			var reversals = new StringCollection();
			foreach(var item in reversalsCheckedListBox.CheckedItems)
			{
				reversals.Add(item.ToString());
			}
			Settings.Default.WebonaryReversals = reversals;
			if (configurationBox.SelectedItem != null)
				Settings.Default.WebonaryConfiguration = configurationBox.SelectedItem.ToString();
			if (publicationBox.SelectedItem != null)
				Settings.Default.WebonaryPublication = publicationBox.SelectedItem.ToString();
			Settings.Default.Save();
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
	}
}
