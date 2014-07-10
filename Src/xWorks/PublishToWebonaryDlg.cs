// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using SIL.CoreImpl.Properties;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Dialog for publishing data to Webonary web site.
	/// </summary>
	public partial class PublishToWebonaryDlg : Form
	{
		// This value gets used by the microsoft encryption library to increase the complexity of the encryption
		private const string EntropyValue = @"61:3nj 42 ebg68";

		public PublishToWebonaryDlg()
		{
			InitializeComponent();
			LoadFromSettings();
		}

		public PublishToWebonaryDlg(IEnumerable<string> reversals, IEnumerable<string> configurations, IEnumerable<string> publications)
		{
			InitializeComponent();
			PopulateReversalsCheckboxList(reversals);
			PopulateConfigurationsList(configurations);
			PopulatePublicationsList(publications);
			LoadFromSettings();

			// Start with output log area not shown by default
			// When a user clicks Publish, it is revealed. This is done within the context of having a resizable table of controls, and having
			// the output log area be the vertically growing control when a user increases the height of the dialog.
			this.Shown += (sender, args) => { this.Height = tableLayoutPanel.Height - outputLogTextbox.Height; };
		}

		private void PopulatePublicationsList(IEnumerable<string> publications)
		{
			foreach(var pub in publications)
			{
				publicationBox.Items.Add(pub);
			}
		}

		private void PopulateConfigurationsList(IEnumerable<string> configurations)
		{
			foreach(var config in configurations)
			{
				configurationBox.Items.Add(config);
			}
		}

		private void PopulateReversalsCheckboxList(IEnumerable<string> reversals)
		{
			foreach(var reversal in reversals)
			{
				reversalsCheckedListBox.Items.Add(reversal);
			}
		}

		private void LoadFromSettings()
		{
			webonaryPasswordTextbox.Text = DecryptPassword(Settings.Default.WebonaryPass);
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
			Settings.Default.WebonaryPass = EncryptPassword(webonaryPasswordTextbox.Text);
			Settings.Default.WebonaryUser = webonaryUsernameTextbox.Text;
			Settings.Default.WebonarySite = webonarySiteNameTextbox.Text;
			var reversals = new StringCollection();
			foreach(var item in reversalsCheckedListBox.CheckedItems)
			{
				reversals.Add(item.ToString());
			}
			Settings.Default.WebonaryReversals = reversals;
			Settings.Default.WebonaryConfiguration = configurationBox.SelectedItem.ToString();
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

		private void showPasswordCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (showPasswordCheckBox.Checked)
				webonaryPasswordTextbox.PasswordChar = '\0';
			else
				webonaryPasswordTextbox.PasswordChar = '*';
		}

		private void publishButton_Click(object sender, EventArgs e)
		{
			// TODO: Enable when doesn't crash: SaveToSettings();

			// TODO: publish

			// Increase height of form so the output log is shown.
			// Account for situations where the user already increased the height of the form
			// or maximized the form, and later reduces the height or unmaximizes the form
			// after clicking Publish.

			var allButTheLogRowHeight = this.tableLayoutPanel.GetRowHeights().Sum() - this.tableLayoutPanel.GetRowHeights().Last();
			var fudge = this.Height - this.tableLayoutPanel.Height;
			var minimumFormHeightToShowLog = allButTheLogRowHeight + this.outputLogTextbox.MinimumSize.Height + fudge;
			this.MinimumSize = new Size(this.MinimumSize.Width, minimumFormHeightToShowLog);
		}
	}
}
