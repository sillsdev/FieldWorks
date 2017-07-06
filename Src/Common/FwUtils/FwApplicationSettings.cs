// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
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
				string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
				string configFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					Application.CompanyName, Application.ProductName, version, "user.config");
				if (File.Exists(configFilename))
				{
					using (var stream = new FileStream(configFilename, FileMode.Open))
					{
						if (!MigrateIfNecessary(stream))
							m_settings.Upgrade();
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
	}
}
