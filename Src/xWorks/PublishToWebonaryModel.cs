// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SIL.CoreImpl;
using SIL.CoreImpl.Properties;

namespace SIL.FieldWorks.XWorks
{
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="PropertyTable is a reference")]

	public class PublishToWebonaryModel
	{
		// This value gets used by the microsoft encryption library to increase the complexity of the encryption
		private const string EntropyValue = @"61:3nj 42 ebg68";
		// Constants for setting identifiers
		private const string WebonarySite = "WebonarySite_ProjectSetting";
		private const string WebonaryReversals = "WebonaryReversals_ProjectSetting";
		private const string WebonaryPublication = "WebonaryPublication_ProjectSetting";
		private const string WebonaryConfiguration = "WebonaryConfiguration_ProjectSetting";
		//  Unicode line break to insert between reversals
		private const string ReversalSeperator = "\u2028";

		private string m_selectedPublication;

		private string m_selectedConfiguration;

		public string SiteName { get; set; }

		public string UserName { get; set; }

		public string Password { get; set; }

		public bool RememberPassword { get; set; }

		public string SelectedPublication // REVIEW (Hasso) 2014.11: should this have a default?
		{
			get { return m_selectedPublication; }
			set { m_selectedPublication = value; }
		}

		public string SelectedConfiguration
		{
			get
			{
				if (!String.IsNullOrEmpty(m_selectedConfiguration))
					return m_selectedConfiguration;
				var pathToCurrentConfiguration = DictionaryConfigurationListener.GetCurrentConfiguration(PropertyTable);
				return Configurations.Values.First(config => pathToCurrentConfiguration.Equals(config.FilePath)).Label;
			}
			set { m_selectedConfiguration = value; }
		}

		public IEnumerable<string> SelectedReversals { get; set; }


		public List<string> Publications { get; set; }

		public Dictionary<string, DictionaryConfigurationModel> Configurations { get; set; }

		public IEnumerable<string> Reversals { get; set; }
		private IPropertyTable PropertyTable { get; set; }

		public PublishToWebonaryModel(IPropertyTable propertyTable)
		{
			PropertyTable = propertyTable;
			LoadFromSettings();
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

		private void LoadFromSettings()
		{
			if(!string.IsNullOrEmpty(Settings.Default.WebonaryPass))
			{
				RememberPassword = true;
				Password = DecryptPassword(Settings.Default.WebonaryPass);
			}
			UserName = Settings.Default.WebonaryUser;
			if(PropertyTable != null)
			{
				SiteName = PropertyTable.GetValue<string>(WebonarySite);
				SelectedPublication = PropertyTable.GetValue<string>(WebonaryPublication);
				SelectedConfiguration = PropertyTable.GetValue<string>(WebonaryConfiguration);
				SelectedReversals = SplitReversalSettingString(PropertyTable.GetValue<string>(WebonaryReversals));
			}
		}

		internal void SaveToSettings()
		{
			Settings.Default.WebonaryPass = RememberPassword ? EncryptPassword(Password) : null;
			Settings.Default.WebonaryUser = UserName;

			PropertyTable.SetProperty(WebonarySite, SiteName, true, false);
			PropertyTable.SetProperty(WebonaryReversals, CombineReversalSettingStrings(Reversals), true, false);
			if(m_selectedConfiguration != null)
			{
				PropertyTable.SetProperty(WebonaryConfiguration, m_selectedConfiguration, true, false);
			}
			if (m_selectedPublication != null)
			{
				PropertyTable.SetProperty(WebonaryPublication, m_selectedPublication, true, false);
			}
			PropertyTable.SaveGlobalSettings();
			Settings.Default.Save();
		}

		/// <summary>
		/// We don't have code to persist collections of strings in the project settings, so we'll combine our list into
		/// a single string and split it when we pull it out.
		/// </summary>
		private string CombineReversalSettingStrings(IEnumerable<string> selectedReversals)
		{
			return String.Join<string>(ReversalSeperator, selectedReversals);
		}

		/// <summary>
		/// This method will split the given reversal string and return the resulting list
		/// </summary>
		private IEnumerable<string> SplitReversalSettingString(string savedReversalList)
		{
			if(!String.IsNullOrEmpty(savedReversalList))
			{
				return savedReversalList.Split(new[] { ReversalSeperator }, StringSplitOptions.RemoveEmptyEntries);
			}
			return null;
		}
	}
}