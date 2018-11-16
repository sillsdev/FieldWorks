// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.Reporting;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Tests application settings classes
	/// </summary>
	[TestFixture]
	public class FwApplicationSettingsTests
	{
		/// <summary>
		/// Tests migration of old config settings.
		/// </summary>
		[Test]
		public void UpgradeIfNecessary_FullOldConfig_OldConfigMigrated()
		{
			var reportingSettings = new ReportingSettings
			{
				FirstLaunchDate = DateTime.Now.AddYears(-1),
				HaveShowRegistrationDialog = false,
				Launches = 100,
				OkToPingBasicUsageData = true,
				PreviousLaunchDate = DateTime.Now.AddDays(-1),
				PreviousVersion = "Version 1.0.0"
			};

			var oldConfigXml = CreateOldConfig(reportingSettings, true, "keyboards", "username", "password");
			var appSettings = new TestFwApplicationSettings { ConfigXml = oldConfigXml };
			appSettings.UpgradeIfNecessary();
			Assert.That(appSettings.Reporting.Launches, Is.EqualTo(reportingSettings.Launches));
			Assert.That(appSettings.Reporting.FirstLaunchDate, Is.EqualTo(reportingSettings.FirstLaunchDate));
			Assert.That(appSettings.Reporting.PreviousVersion, Is.EqualTo(reportingSettings.PreviousVersion));
			Assert.That(appSettings.UpdateGlobalWSStore, Is.True);
			Assert.That(appSettings.LocalKeyboards, Is.EqualTo("keyboards"));
			Assert.That(appSettings.WebonaryUser, Is.EqualTo("username"));
			Assert.That(appSettings.WebonaryPass, Is.EqualTo("password"));
			// check if the old config sections were removed
			Assert.That(appSettings.ConfigXml.Root?.Element("configSections")?.Elements("sectionGroup")
				.First(e => (string)e.Attribute("name") == "userSettings").HasElements, Is.False);
			Assert.That(appSettings.ConfigXml.Root?.Element("userSettings")?.HasElements, Is.False);
		}

		/// <summary>
		/// Tests that nothing is migrated when old config has already been migrated.
		/// </summary>
		[Test]
		public void UpgradeIfNecessary_EmptyOldConfig_NothingMigrated()
		{
			var oldConfigXml = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
				new XElement("configuration",
					new XElement("configSections",
						new XElement("sectionGroup", new XAttribute("name", "userSettings"))),
					new XElement("userSettings")));
			var appSettings = new TestFwApplicationSettings { ConfigXml = oldConfigXml };
			appSettings.UpgradeIfNecessary();
			Assert.That(appSettings.Reporting, Is.Null);
			Assert.That(appSettings.UpdateGlobalWSStore, Is.False);
			Assert.That(appSettings.LocalKeyboards, Is.Null);
			Assert.That(appSettings.WebonaryUser, Is.Null);
			Assert.That(appSettings.WebonaryPass, Is.Null);
		}

		/// <summary>
		/// Tests migration of old config settings when "Reporting" setting is missing.
		/// </summary>
		[Test]
		public void UpgradeIfNecessary_OldConfigReportingMissing_OldConfigMigrated()
		{
			var oldConfigXml = CreateOldConfig(null, true, "keyboards", "username", "password");
			var appSettings = new TestFwApplicationSettings { ConfigXml = oldConfigXml };
			appSettings.UpgradeIfNecessary();
			Assert.That(appSettings.Reporting, Is.Null);
			Assert.That(appSettings.UpdateGlobalWSStore, Is.True);
			Assert.That(appSettings.LocalKeyboards, Is.EqualTo("keyboards"));
			Assert.That(appSettings.WebonaryUser, Is.EqualTo("username"));
			Assert.That(appSettings.WebonaryPass, Is.EqualTo("password"));
			// check if the old config sections were removed
			Assert.That(appSettings.ConfigXml.Root?.Element("configSections")?.Elements("sectionGroup")
				.First(e => (string)e.Attribute("name") == "userSettings").HasElements, Is.False);
			Assert.That(appSettings.ConfigXml.Root?.Element("userSettings")?.HasElements, Is.False);
		}

		/// <summary>
		/// Tests migration of old config settings when "WebonaryUser" is missing.
		/// </summary>
		[Test]
		public void UpgradeIfNecessary_OldConfigWebonaryUserMissing_OldConfigMigrated()
		{
			var reportingSettings = new ReportingSettings
			{
				FirstLaunchDate = DateTime.Now.AddYears(-1),
				HaveShowRegistrationDialog = false,
				Launches = 100,
				OkToPingBasicUsageData = true,
				PreviousLaunchDate = DateTime.Now.AddDays(-1),
				PreviousVersion = "Version 1.0.0"
			};

			var oldConfigXml = CreateOldConfig(reportingSettings, true, "keyboards", null, "password");
			var appSettings = new TestFwApplicationSettings { ConfigXml = oldConfigXml };
			appSettings.UpgradeIfNecessary();
			Assert.That(appSettings.Reporting.Launches, Is.EqualTo(reportingSettings.Launches));
			Assert.That(appSettings.Reporting.FirstLaunchDate, Is.EqualTo(reportingSettings.FirstLaunchDate));
			Assert.That(appSettings.Reporting.PreviousVersion, Is.EqualTo(reportingSettings.PreviousVersion));
			Assert.That(appSettings.UpdateGlobalWSStore, Is.True);
			Assert.That(appSettings.LocalKeyboards, Is.EqualTo("keyboards"));
			Assert.That(appSettings.WebonaryUser, Is.Null);
			Assert.That(appSettings.WebonaryPass, Is.EqualTo("password"));
			// check if the old config sections were removed
			Assert.That(appSettings.ConfigXml.Root?.Element("configSections")?.Elements("sectionGroup")
				.First(e => (string)e.Attribute("name") == "userSettings").HasElements, Is.False);
			Assert.That(appSettings.ConfigXml.Root?.Element("userSettings")?.HasElements, Is.False);
		}

		private static XDocument CreateOldConfig(ReportingSettings reportingSettings, bool? updateGlobalWSStore, string localKeyboards, string webonaryUser, string webonaryPass)
		{
			var settingsElem = new XElement("SIL.CoreImpl.Properties.Settings",
				CreateStringSettingElement("IsBTE", false),
				CreateStringSettingElement("CheckForBetaUpdates", false),
				CreateStringSettingElement("AutoCheckForUpdates", true));
			if (reportingSettings != null)
			{
				settingsElem.Add(new XElement("setting", new XAttribute("name", "Reporting"), new XAttribute("serializeAs", "Xml"),
					new XElement("value", XElement.Parse(XmlSerializationHelper.SerializeToString(reportingSettings)))));
			}
			if (updateGlobalWSStore != null)
			{
				settingsElem.Add(CreateStringSettingElement("UpdateGlobalWSStore", updateGlobalWSStore));
			}
			if (localKeyboards != null)
			{
				settingsElem.Add(CreateStringSettingElement("LocalKeyboards", localKeyboards));
			}
			if (webonaryUser != null)
			{
				settingsElem.Add(CreateStringSettingElement("WebonaryUser", webonaryUser));
			}
			if (webonaryPass != null)
			{
				settingsElem.Add(CreateStringSettingElement("WebonaryPass", webonaryPass));
			}
			return new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
				new XElement("configuration",
					new XElement("configSections",
						new XElement("sectionGroup", new XAttribute("name", "userSettings"),
							new XElement("section", new XAttribute("name", "SIL.CoreImpl.Properties.Settings")))),
					new XElement("userSettings", settingsElem)));
		}

		private static XElement CreateStringSettingElement(string name, object value)
		{
			return new XElement("setting", new XAttribute("name", name), new XAttribute("serializeAs", "String"),
				new XElement("value", value));
		}
	}
}