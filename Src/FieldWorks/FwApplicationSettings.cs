// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Reporting;

namespace SIL.FieldWorks
{
	/// <summary>
	/// This class encapsulates the application settings for FW.
	/// </summary>
	public class FwApplicationSettings : FwApplicationSettingsBase
	{
		private readonly Common.FwUtils.Properties.Settings m_settings;

		/// <summary />
		public FwApplicationSettings()
		{
			m_settings = Common.FwUtils.Properties.Settings.Default;
		}

		/// <summary />
		public override bool UpdateGlobalWSStore
		{
			get { return m_settings.UpdateGlobalWSStore; }
			set { m_settings.UpdateGlobalWSStore = value; }
		}

		/// <summary />
		public override ReportingSettings Reporting
		{
			get { return m_settings.Reporting; }
			set { m_settings.Reporting = value; }
		}

		/// <summary />
		public override string LocalKeyboards
		{
			get { return m_settings.LocalKeyboards; }
			set { m_settings.LocalKeyboards = value; }
		}

		/// <summary />
		public override string WebonaryUser
		{
			get { return m_settings.WebonaryUser; }
			set { m_settings.WebonaryUser = value; }
		}

		/// <summary />
		public override string WebonaryPass
		{
			get { return m_settings.WebonaryPass; }
			set { m_settings.WebonaryPass = value; }
		}

		/// <summary />
		public override void UpgradeIfNecessary()
		{
			if (m_settings.CallUpgrade)
			{
				// LT-18723 Upgrade m_settings to generate the user.config file for FLEx 9.0
				m_settings.Save();
				m_settings.Upgrade();
				string baseConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.CompanyName, Application.ProductName);
				if (Directory.Exists(baseConfigFolder))
				{
					// For some reason the version returned from Assembly.GetExecutingAssembly.GetName().Version does not return the
					// exact same version number that was written by m_settings.Upgrade() so we find it by looking for the lastest version
					var directoryList = new List<string>(Directory.EnumerateDirectories(baseConfigFolder));
					directoryList.Sort();
					var pathToPreviousSettingsFile = Path.Combine(directoryList[directoryList.Count - 1], "user.config");
					using (var stream = new FileStream(pathToPreviousSettingsFile, FileMode.Open))
					{
						MigrateIfNecessary(stream);
					}
				}
				m_settings.CallUpgrade = false;
				m_settings.Save();
			}
		}

		/// <summary />
		public override void Save()
		{
			m_settings.Save();
		}

		/// <summary />
		public void DeleteCorruptedSettingsFilesIfPresent()
		{
			var pathToConfigFiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.CompanyName, Application.ProductName);
			if (!Directory.Exists(pathToConfigFiles))
			{
				return;
			}

			var localConfigFolders = new List<string>(Directory.EnumerateDirectories(pathToConfigFiles));
			localConfigFolders.Sort();
			var highestVersionFolder = localConfigFolders.Count > 0 ? localConfigFolders[localConfigFolders.Count - 1] : string.Empty;
			var corruptFileFound = false;

			while (highestVersionFolder != string.Empty)
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
				highestVersionFolder = localConfigFolders.Count > 0 ? localConfigFolders[localConfigFolders.Count - 1] : string.Empty;
			}
			if (!corruptFileFound)
			{
				return;
			}
			var caption = Properties.Resources.ksCorruptSettingsFileCaption;
			var text = Properties.Resources.ksDeleteAndReportCorruptSettingsFile;
			MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}
}
