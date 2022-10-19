// Copyright (c) 2017-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Reporting;
using SIL.Settings;

namespace FieldWorks.TestUtilities.Tests
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

			var updateSettings = new UpdateSettings
			{
				Channel = UpdateSettings.Channels.Beta,
				Behavior = UpdateSettings.Behaviors.Notify
			};

			var oldConfigXml = CreateOldConfig(reportingSettings, updateSettings, true, "keyboards", "username", "password");
			var appSettings = new TestFwApplicationSettings
			{
				ConfigXml = oldConfigXml
			};
			var appSettingsAsInterface = (IFwApplicationSettings)appSettings;
			appSettingsAsInterface.UpgradeIfNecessary();
			Assert.That(appSettingsAsInterface.Reporting.Launches, Is.EqualTo(reportingSettings.Launches));
			Assert.That(appSettingsAsInterface.Reporting.FirstLaunchDate, Is.EqualTo(reportingSettings.FirstLaunchDate));
			Assert.That(appSettingsAsInterface.Reporting.PreviousVersion, Is.EqualTo(reportingSettings.PreviousVersion));
			Assert.That(appSettingsAsInterface.LocalKeyboards, Is.EqualTo("keyboards"));
			Assert.That(appSettingsAsInterface.WebonaryUser, Is.EqualTo("username"));
			Assert.That(appSettingsAsInterface.WebonaryPass, Is.EqualTo("password"));
			Assert.That(appSettingsAsInterface.Update.Behavior, Is.EqualTo(updateSettings.Behavior));
			Assert.That(appSettingsAsInterface.Update.Channel, Is.EqualTo(updateSettings.Channel));
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
			var appSettings = new TestFwApplicationSettings
			{
				ConfigXml = oldConfigXml
			};
			var appSettingsAsInterface = (IFwApplicationSettings)appSettings;
			appSettingsAsInterface.UpgradeIfNecessary();
			Assert.That(appSettingsAsInterface.Reporting, Is.Null);
			Assert.That(appSettingsAsInterface.LocalKeyboards, Is.Null);
			Assert.That(appSettingsAsInterface.WebonaryUser, Is.Null);
			Assert.That(appSettingsAsInterface.WebonaryPass, Is.Null);
			Assert.That(appSettingsAsInterface.Update.Behavior, Is.Null);
			Assert.That(appSettingsAsInterface.Update.Channel, Is.Null);

		}

		/// <summary>
		/// Tests migration of old config settings when "Reporting" setting is missing.
		/// </summary>
		[Test]
		public void UpgradeIfNecessary_OldConfigReportingMissing_OldConfigMigrated()
		{
			var updateSettings = new UpdateSettings
			{
				Channel = UpdateSettings.Channels.Alpha,
				Behavior = UpdateSettings.Behaviors.DoNotCheck
			};

			var oldConfigXml = CreateOldConfig(null, updateSettings, true, "keyboards", "username", "password");
			var appSettings = new TestFwApplicationSettings
			{
				ConfigXml = oldConfigXml
			};
			var appSettingsAsInterface = (IFwApplicationSettings)appSettings;
			appSettingsAsInterface.UpgradeIfNecessary();
			Assert.That(appSettingsAsInterface.Reporting, Is.Null);
			Assert.That(appSettingsAsInterface.LocalKeyboards, Is.EqualTo("keyboards"));
			Assert.That(appSettingsAsInterface.WebonaryUser, Is.EqualTo("username"));
			Assert.That(appSettingsAsInterface.WebonaryPass, Is.EqualTo("password"));
			Assert.That(appSettingsAsInterface.Update.Behavior, Is.EqualTo(updateSettings.Behavior));
			Assert.That(appSettingsAsInterface.Update.Channel, Is.EqualTo(updateSettings.Channel));

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
				Launches = 120,
				OkToPingBasicUsageData = true,
				PreviousLaunchDate = DateTime.Now.AddDays(-1),
				PreviousVersion = "Version 1.0.0"
			};

			var updateSettings = new UpdateSettings
			{
				Channel = UpdateSettings.Channels.Stable,
				Behavior = UpdateSettings.Behaviors.Download
			};

			var oldConfigXml = CreateOldConfig(reportingSettings, updateSettings, true, "keyboards", null, "password");
			var appSettings = new TestFwApplicationSettings
			{
				ConfigXml = oldConfigXml
			};
			var appSettingsAsInterface = (IFwApplicationSettings)appSettings;
			appSettingsAsInterface.UpgradeIfNecessary();
			Assert.That(appSettingsAsInterface.Reporting.Launches, Is.EqualTo(reportingSettings.Launches));
			Assert.That(appSettingsAsInterface.Reporting.FirstLaunchDate, Is.EqualTo(reportingSettings.FirstLaunchDate));
			Assert.That(appSettingsAsInterface.Reporting.PreviousVersion, Is.EqualTo(reportingSettings.PreviousVersion));
			Assert.That(appSettingsAsInterface.LocalKeyboards, Is.EqualTo("keyboards"));
			Assert.That(appSettingsAsInterface.WebonaryUser, Is.Null);
			Assert.That(appSettingsAsInterface.WebonaryPass, Is.EqualTo("password"));
			Assert.That(appSettingsAsInterface.Update.Behavior, Is.EqualTo(updateSettings.Behavior));
			Assert.That(appSettingsAsInterface.Update.Channel, Is.EqualTo(updateSettings.Channel));
			// check if the old config sections were removed
			Assert.That(appSettings.ConfigXml.Root?.Element("configSections")?.Elements("sectionGroup")
				.First(e => (string)e.Attribute("name") == "userSettings").HasElements, Is.False);
			Assert.That(appSettings.ConfigXml.Root?.Element("userSettings")?.HasElements, Is.False);
		}

		private static XDocument CreateOldConfig(ReportingSettings reportingSettings,
			UpdateSettings updateSettings, bool? updateGlobalWSStore, string localKeyboards,
			string webonaryUser, string webonaryPass)
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
			if (updateSettings != null)
			{
				settingsElem.Add(new XElement("setting", new XAttribute("name", "Update"), new XAttribute("serializeAs", "Xml"),
					new XElement("value", XElement.Parse(XmlSerializationHelper.SerializeToString(updateSettings)))));
			}

			// Leave this obsolete value in in the old config to test that it doesn't cause problems
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