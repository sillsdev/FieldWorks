// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using SIL.CoreImpl.Properties;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.XWorks
{
	public partial class PublishToWebonaryDlg : Form
	{
		// This value gets used by the microsoft encryption library to increase the complexity of the encryption
		private const string m_entropyValue = @"61:3nj 42 rot13";

		public PublishToWebonaryDlg()
		{
			InitializeComponent();
			LoadFromSettings();
		}

		public PublishToWebonaryDlg(IEnumerable<ITsString> reversals, IEnumerable<string> configurations, IEnumerable<string> publications)
		{
			InitializeComponent();
			PopulateReversalsCheckboxList(reversals);
			PopulateConfigurationsList(configurations);
			PopulatePublicationsList(publications);
			LoadFromSettings();
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

		private void PopulateReversalsCheckboxList(IEnumerable<ITsString> reversals)
		{
			foreach(var reversal in reversals)
			{
				reversalsCheckedListBox.Items.Add(reversal.Text);
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

		private string EncryptPassword(string encryptMe)
		{
			byte[] encryptedData = ProtectedData.Protect(Encoding.Unicode.GetBytes(encryptMe), Encoding.Unicode.GetBytes(m_entropyValue), DataProtectionScope.CurrentUser);
			return Convert.ToBase64String(encryptedData);
		}

		private string DecryptPassword(string decryptMe)
		{
			if(!String.IsNullOrEmpty(decryptMe))
			{
				byte[] decryptedData = ProtectedData.Unprotect(Convert.FromBase64String(decryptMe), Encoding.Unicode.GetBytes(m_entropyValue), DataProtectionScope.CurrentUser);
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
			SaveToSettings();
			//TODO: publish
		}
	}
}
