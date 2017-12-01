// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using SIL.Reporting;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This class encapsulates the application settings for FW.
	/// </summary>
	public class FwApplicationSettings : FwApplicationSettingsBase
	{
		private readonly Properties.Settings m_settings;

		public FwApplicationSettings()
		{
			m_settings = Properties.Settings.Default;
		}

		public override bool UpdateGlobalWSStore
		{
			get { return m_settings.UpdateGlobalWSStore; }
			set { m_settings.UpdateGlobalWSStore = value; }
		}

		public override ReportingSettings Reporting
		{
			get { return m_settings.Reporting; }
			set { m_settings.Reporting = value; }
		}

		public override string LocalKeyboards
		{
			get { return m_settings.LocalKeyboards; }
			set { m_settings.LocalKeyboards = value; }
		}

		public override string WebonaryUser
		{
			get { return m_settings.WebonaryUser; }
			set { m_settings.WebonaryUser = value; }
		}

		public override string WebonaryPass
		{
			get { return m_settings.WebonaryPass; }
			set { m_settings.WebonaryPass = value; }
		}

		public override void UpgradeIfNecessary()
		{
			if (m_settings.CallUpgrade)
			{
				// LT-18723 Upgrade m_settings to generate the user.config file for FLEx 9.0
				m_settings.Save();
				m_settings.Upgrade();
				string baseConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					Application.CompanyName, Application.ProductName);

				if (Directory.Exists(baseConfigFolder))
				{
					// For some reason the version returned from Assembly.GetExecutingAssembly.GetName().Version does not return the
					// exact same version number that was written by m_settings.Upgrade() so we find it by looking for the lastest version
					List<string> directoryList = new List<string>(Directory.EnumerateDirectories(baseConfigFolder));
					directoryList.Sort();
					string pathToPreviousSettingsFile = Path.Combine(directoryList[directoryList.Count - 1],"user.config");
					using (var stream = new FileStream(pathToPreviousSettingsFile, FileMode.Open))
					{
						MigrateIfNecessary(stream);
					}
				}
				m_settings.CallUpgrade = false;
				m_settings.Save();
			}
		}

		public override void Save()
		{
			m_settings.Save();
		}

		public void DeleteCorruptedSettingsFilesIfPresent()
		{
			string pathToConfigFiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				Application.CompanyName, Application.ProductName);
			List<string> localConfigFolders;
			bool corruptFileFound = false;
			if (Directory.Exists(pathToConfigFiles))
			{
				localConfigFolders = new List<string>(Directory.EnumerateDirectories(pathToConfigFiles));
				localConfigFolders.Sort();
				string highestVersionFolder = localConfigFolders.Count > 0 ? localConfigFolders[localConfigFolders.Count - 1] : "";

				while (highestVersionFolder != "")
				{
					try
					{
						var highestVersionConfigPath = Path.Combine(highestVersionFolder, "user.config");
						if (File.Exists(highestVersionConfigPath))
						{
							using (var stream = new FileStream(highestVersionConfigPath, FileMode.Open))
							{
								// This will throw an exception if the file is corrupted (LT-18643 Null bytes written to user.config file)
								XDocument.Load(stream);
							}
						}
					}
					catch (XmlException)
					{
						corruptFileFound = true;
						Directory.Delete(highestVersionFolder, true);
					}
					localConfigFolders.Remove(highestVersionFolder);
					highestVersionFolder = localConfigFolders.Count > 0 ? localConfigFolders[localConfigFolders.Count - 1] : "";
				}
				if (corruptFileFound)
				{
					string caption = FwUtilsStrings.ksCorruptSettingsFileCaption;
					string text = FwUtilsStrings.ksDeleteAndReportCorruptSettingsFile;
					MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}
	}
}
