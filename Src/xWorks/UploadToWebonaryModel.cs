// Copyright (c) 2014-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
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
		private const string ReversalSeparator = "\u2028";

		private string m_selectedPublication;

		private string m_selectedConfiguration;

		private string m_siteName;

		public string SiteName
		{
			get => m_siteName;
			set
			{
				if (m_siteName != value)
				{
					m_siteName = value;
					Log = new WebonaryUploadLog(LastUploadReport);
				}
			}
		}

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
				if (!string.IsNullOrEmpty(m_selectedConfiguration))
					return m_selectedConfiguration;
				var pathToCurrentConfiguration = DictionaryConfigurationListener.GetCurrentConfiguration(PropertyTable,
					DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
				var curConfig =  Configurations.Values.FirstOrDefault(config => pathToCurrentConfiguration.Equals(config.FilePath));
				return curConfig == null ? null : curConfig.Label;
			}
			set { m_selectedConfiguration = value; }
		}

		public ICollection<string> SelectedReversals { get; set; }


		public List<string> Publications { get; set; }

		public Dictionary<string, DictionaryConfigurationModel> Configurations { get; set; }
		public Dictionary<string, DictionaryConfigurationModel> Reversals { get; set; }

		public bool CanViewReport { get; set; }

		public WebonaryUploadLog Log { get; set; }

		private PropertyTable PropertyTable { get; set; }

		public UploadToWebonaryModel(PropertyTable propertyTable)
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
			if (PropertyTable != null)
			{
				var appSettings = PropertyTable.GetValue<FwApplicationSettingsBase>("AppSettings");
				if (!string.IsNullOrEmpty(appSettings.WebonaryPass))
				{
					RememberPassword = true;
					Password = DecryptPassword(appSettings.WebonaryPass);
				}
				UserName = appSettings.WebonaryUser;

				SiteName = PropertyTable.GetStringProperty(WebonarySite, null);
				SelectedPublication = PropertyTable.GetStringProperty(WebonaryPublication, null);
				SelectedConfiguration = PropertyTable.GetStringProperty(WebonaryConfiguration, null);
				SelectedReversals = SplitReversalSettingString(PropertyTable.GetStringProperty(WebonaryReversals, null));
			}

			Log = new WebonaryUploadLog(LastUploadReport);
			CanViewReport = File.Exists(LastUploadReport);
		}

		// The last upload report is stored in the temp directory, under a folder named for the site name
		public string LastUploadReport => Path.Combine(Path.GetTempPath(), "webonary-export", SiteName ?? "no-site", "last-upload.log");

		internal void SaveToSettings()
		{
			var appSettings = PropertyTable.GetValue<FwApplicationSettingsBase>("AppSettings");
			appSettings.WebonaryPass = RememberPassword ? EncryptPassword(Password) : null;
			appSettings.WebonaryUser = UserName;

			PropertyTable.SetProperty(WebonarySite, SiteName, false);
			PropertyTable.SetPropertyPersistence(WebonarySite, true);
			PropertyTable.SetProperty(WebonaryReversals, CombineReversalSettingStrings(Reversals.Keys), false);
			PropertyTable.SetProperty(WebonaryReversals, CombineReversalSettingStrings(SelectedReversals), false);
			PropertyTable.SetPropertyPersistence(WebonaryReversals, true);
			if(m_selectedConfiguration != null)
			{
				PropertyTable.SetProperty(WebonaryConfiguration, m_selectedConfiguration, PropertyTable.SettingsGroup.LocalSettings, false);
				PropertyTable.SetPropertyPersistence(WebonaryConfiguration, true, PropertyTable.SettingsGroup.LocalSettings);
			}
			if (m_selectedPublication != null)
			{
				PropertyTable.SetProperty(WebonaryPublication, m_selectedPublication, PropertyTable.SettingsGroup.LocalSettings, false);
				PropertyTable.SetPropertyPersistence(WebonaryPublication, true, PropertyTable.SettingsGroup.LocalSettings);
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
			return String.Join<string>(ReversalSeparator, selectedReversals);
		}

		/// <summary>
		/// This method will split the given reversal string and return the resulting list
		/// </summary>
		private static ICollection<string> SplitReversalSettingString(string savedReversalList)
		{
			if(!string.IsNullOrEmpty(savedReversalList))
			{
				return savedReversalList.Split(new[] { ReversalSeparator }, StringSplitOptions.RemoveEmptyEntries);
			}
			return null;
		}
	}
}
