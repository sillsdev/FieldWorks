// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LanguageExplorer.DictionaryConfiguration;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Impls
{

	public class UploadToWebonaryModel
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

		private string m_selectedConfiguration;

		public string SiteName { get; set; }

		public string UserName { get; set; }

		public string Password { get; set; }

		public bool RememberPassword { get; set; }

		// REVIEW (Hasso) 2014.11: should this have a default?
		public string SelectedPublication { get; set; }

		public string SelectedConfiguration
		{
			get
			{
				if (!string.IsNullOrEmpty(m_selectedConfiguration))
				{
					return m_selectedConfiguration;
				}
				var pathToCurrentConfiguration = DictionaryConfigurationServices.GetCurrentConfiguration(PropertyTable, DictionaryConfigurationServices.DictionaryConfigurationDirectoryName);
				var curConfig =  Configurations.Values.FirstOrDefault(config => pathToCurrentConfiguration.Equals(config.FilePath));
				return curConfig?.Label;
			}
			set { m_selectedConfiguration = value; }
		}

		public ICollection<string> SelectedReversals { get; set; }


		public List<string> Publications { get; set; }

		public Dictionary<string, DictionaryConfigurationModel> Configurations { get; set; }
		public Dictionary<string, DictionaryConfigurationModel> Reversals { get; set; }

		private IPropertyTable PropertyTable { get; }

		public UploadToWebonaryModel(IPropertyTable propertyTable)
		{
			Guard.AgainstNull(propertyTable, nameof(propertyTable));

			PropertyTable = propertyTable;
			LoadFromSettings();
		}

		internal static string EncryptPassword(string encryptMe)
		{
			return string.IsNullOrEmpty(encryptMe) ? encryptMe : Convert.ToBase64String(ProtectedData.Protect(Encoding.Unicode.GetBytes(encryptMe), Encoding.Unicode.GetBytes(EntropyValue), DataProtectionScope.CurrentUser));
		}

		internal static string DecryptPassword(string decryptMe)
		{
			return !string.IsNullOrEmpty(decryptMe) ? Encoding.Unicode.GetString(ProtectedData.Unprotect(Convert.FromBase64String(decryptMe), Encoding.Unicode.GetBytes(EntropyValue), DataProtectionScope.CurrentUser)) : decryptMe;
		}

		private void LoadFromSettings()
		{
				var appSettings = PropertyTable.GetValue<FwApplicationSettingsBase>("AppSettings");
				if (!string.IsNullOrEmpty(appSettings.WebonaryPass))
				{
					RememberPassword = true;
					Password = DecryptPassword(appSettings.WebonaryPass);
				}
				UserName = appSettings.WebonaryUser;

			SiteName = PropertyTable.GetValue<string>(WebonarySite, null);
			SelectedPublication = PropertyTable.GetValue<string>(WebonaryPublication, null);
			SelectedConfiguration = PropertyTable.GetValue<string>(WebonaryConfiguration, null);
			SelectedReversals = SplitReversalSettingString(PropertyTable.GetValue<string>(WebonaryReversals, null));
		}

		internal void SaveToSettings()
		{
			var appSettings = PropertyTable.GetValue<FwApplicationSettingsBase>("AppSettings");
			appSettings.WebonaryPass = RememberPassword ? EncryptPassword(Password) : null;
			appSettings.WebonaryUser = UserName;

			PropertyTable.SetProperty(WebonarySite, SiteName, true);
			PropertyTable.SetProperty(WebonaryReversals, CombineReversalSettingStrings(Reversals.Keys), true);
			PropertyTable.SetProperty(WebonaryReversals, CombineReversalSettingStrings(SelectedReversals), true);
			if(m_selectedConfiguration != null)
			{
				PropertyTable.SetProperty(WebonaryConfiguration, m_selectedConfiguration, true, settingsGroup: SettingsGroup.LocalSettings);
			}
			if (SelectedPublication != null)
			{
				PropertyTable.SetProperty(WebonaryPublication, SelectedPublication, true, settingsGroup: SettingsGroup.LocalSettings);
			}
			PropertyTable.SaveGlobalSettings();
			appSettings.Save();
		}

		/// <summary>
		/// We don't have code to persist collections of strings in the project settings, so we'll combine our list into
		/// a single string and split it when we pull it out.
		/// </summary>
		private string CombineReversalSettingStrings(IEnumerable<string> selectedReversals)
		{
			return string.Join<string>(ReversalSeperator, selectedReversals);
		}

		/// <summary>
		/// This method will split the given reversal string and return the resulting list
		/// </summary>
		private static ICollection<string> SplitReversalSettingString(string savedReversalList)
		{
			return !string.IsNullOrEmpty(savedReversalList) ? savedReversalList.Split(new[] { ReversalSeperator }, StringSplitOptions.RemoveEmptyEntries) : null;
		}
	}
}
