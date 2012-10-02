using System;
using System.Collections;
using System.Windows.Forms;
using System.IO;

using NUnit.Framework;

using SIL.Utils;
using XCore.Properties;

namespace XCore
{
	/// <summary>
	/// PropertyTableTests.
	/// </summary>
	[TestFixture]
	public class PropertyTableTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		Mediator m_mediator;
		PropertyTable m_settings;

		string m_originalSettingsPath;
		string m_modifiedSettingsPath = TempPath;

		//--------------------------------------------------------------------------------------
		/// <summary>
		/// Get a temporary path. We add the username for machines where multiple users run
		/// tests (e.g. build machine).
		/// </summary>
		//--------------------------------------------------------------------------------------
		private static string TempPath
		{
			get { return Path.Combine(Path.GetTempPath(), Environment.UserName); }
		}

		//--------------------------------------------------------------------------------------
		/// <summary>
		/// Set-up for this test fixture involves creating some temporary
		/// settings files. These will be cleaned up in the fixture teardown.
		/// </summary>
		//--------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			// load a persisted version of the property table.
			m_originalSettingsPath = Path.Combine(TempPath, "settingsBackup");
			if (!Directory.Exists(m_originalSettingsPath))
				Directory.CreateDirectory(m_originalSettingsPath);

			File.WriteAllText(Path.Combine(m_originalSettingsPath, "db$TestLocal$Settings.xml"), Resources.db_TestLocal_Settings_xml);
			File.WriteAllText(Path.Combine(m_originalSettingsPath, "Settings.xml"), Resources.Settings_xml);

//			m_originalSettingsPath = Path.Combine(DirectoryFinder.FwSourceDirectory, @"XCore\xCoreInterfaces\xCoreInterfacesTests\settingsBackup");
//			m_modifiedSettingsPath = Path.Combine(DirectoryFinder.FwSourceDirectory, @"XCore\xCoreInterfaces\xCoreInterfacesTests");
		}

		//--------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		//--------------------------------------------------------------------------------------
		[SetUp]
		public void SetUp()
		{
			ResetPropertyTable();
			m_settings.LocalSettingsId = "TestLocal";
			m_settings.UserSettingDirectory = m_originalSettingsPath;
			LoadOriginalSettings();
		}

		[TearDown]
		public void TearDown()
		{
			m_mediator.Dispose();
		}

		//--------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		//--------------------------------------------------------------------------------------
		private void ResetPropertyTable()
		{
			m_mediator = new Mediator();
			m_settings = m_mediator.PropertyTable;
		}

		//--------------------------------------------------------------------------------------
		/// <summary>
		/// Needed to remove temporary settings folder.
		/// </summary>
		//--------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			try
			{
				Directory.Delete(m_originalSettingsPath);
			}
			catch { }
		}

		private void LoadOriginalSettings(string settingsId)
		{
			m_settings.UserSettingDirectory = m_originalSettingsPath;
			m_settings.RestoreFromFile(settingsId);
		}

		private void LoadOriginalSettings()
		{
			LoadOriginalSettings(m_settings.LocalSettingsId);
			LoadOriginalSettings(m_settings.GlobalSettingsId);
		}

//		private void LoadModifiedSettings(string settingsId)
//		{
//			m_settings.UserSettingDirectory = m_modifiedSettingsPath;
//			m_settings.RestoreFromFile(settingsId);
//		}

//		private void SaveModifiedSettings(string settingsId)
//		{
//			m_settings.UserSettingDirectory = m_modifiedSettingsPath;
//			m_settings.Save(settingsId, new string[] { });
//		}


		/// <summary>
		/// Test the various versions of PropertyExists.
		/// </summary>
		[Test]
		public void PropertyExists()
		{
			bool fPropertyExists;
			object bestValue;
			PropertyTable.SettingsGroup bestSettings;

			// Test nonexistent properties.
			fPropertyExists = m_settings.PropertyExists("NonexistentPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsFalse(fPropertyExists, String.Format("{0} {1} should not exist.", "global", "NonexistentPropertyA"));
			fPropertyExists = m_settings.PropertyExists("NonexistentPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsFalse(fPropertyExists, String.Format("{0} {1} should not exist.", "local", "NonexistentPropertyA"));
			fPropertyExists = m_settings.PropertyExists("NonexistentPropertyA");
			Assert.IsFalse(fPropertyExists, String.Format("{0} {1} should not exist.", "best", "NonexistentPropertyA"));
			fPropertyExists = m_settings.PropertyExists("NonexistentPropertyA", out bestSettings);
			Assert.IsFalse(fPropertyExists, String.Format("{0} {1} should not exist.", "best", "NonexistentPropertyA"));
			Assert.AreEqual(PropertyTable.SettingsGroup.Undecided, bestSettings, String.Format("Invalid settings for {0} {1}.", "best", "NonexistentPropertyA"));
			fPropertyExists = m_settings.PropertyExists("NonexistentPropertyA", out bestValue, out bestSettings);
			Assert.IsFalse(fPropertyExists, String.Format("{0} {1} should not exist.", "best", "NonexistentPropertyA"));
			Assert.IsNull(bestValue, String.Format("Invalid value for {0} {1}.", "best", "NonexistentPropertyA"));
			Assert.AreEqual(PropertyTable.SettingsGroup.Undecided, bestSettings, String.Format("Invalid settings for {0} {1}.", "best", "NonexistentPropertyA"));

			// Test global property values.
			object gpba;
			fPropertyExists = m_settings.PropertyExists("BooleanPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "global", "BooleanPropertyA"));
			fPropertyExists = m_settings.PropertyExists("BooleanPropertyA", out gpba, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "global", "BooleanPropertyA"));
			Assert.IsFalse((bool)gpba, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));

			object gpia;
			fPropertyExists = m_settings.PropertyExists("IntegerPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "global", "IntegerPropertyA"));
			fPropertyExists = m_settings.PropertyExists("IntegerPropertyA", out gpia, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "global", "IntegerPropertyA"));
			Assert.AreEqual(253, (int)gpia, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));

			object gpsa;
			fPropertyExists = m_settings.PropertyExists("StringPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "global", "StringPropertyA"));
			fPropertyExists = m_settings.PropertyExists("StringPropertyA", out gpsa, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "global", "StringPropertyA"));
			Assert.AreEqual("global_StringPropertyA_value", (string)gpsa, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));

			// Test local property values
			object lpba;
			fPropertyExists = m_settings.PropertyExists("BooleanPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "local", "BooleanPropertyA"));
			fPropertyExists = m_settings.PropertyExists("BooleanPropertyA", out lpba, PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "local", "BooleanPropertyA"));
			Assert.IsTrue((bool)lpba, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			object lpia;
			fPropertyExists = m_settings.PropertyExists("IntegerPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "local", "IntegerPropertyA"));
			fPropertyExists = m_settings.PropertyExists("IntegerPropertyA", out lpia, PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "local", "IntegerPropertyA"));
			Assert.AreEqual(333, (int)lpia, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			object lpsa;
			fPropertyExists = m_settings.PropertyExists("StringPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "local", "StringPropertyA"));
			fPropertyExists = m_settings.PropertyExists("StringPropertyA", out lpsa, PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "local", "StringPropertyA"));
			Assert.AreEqual("local_StringPropertyA_value", (string)lpsa, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			// Test best settings
			// Match on unique globals.
			object ugpba;
			fPropertyExists = m_settings.PropertyExists("BestBooleanPropertyA");
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BestBooleanPropertyA"));
			fPropertyExists = m_settings.PropertyExists("BestBooleanPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BestBooleanPropertyA"));
			fPropertyExists = m_settings.PropertyExists("BestBooleanPropertyA", out ugpba, PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BestBooleanPropertyA"));
			Assert.IsTrue((bool)ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			fPropertyExists = m_settings.PropertyExists("BestBooleanPropertyA", out bestSettings);
			Assert.AreEqual(PropertyTable.SettingsGroup.GlobalSettings, bestSettings, String.Format("Invalid settings for {0} {1}.", "best", "BestBooleanPropertyA"));
			fPropertyExists = m_settings.PropertyExists("BestBooleanPropertyA", out ugpba, out bestSettings);
			Assert.AreEqual(PropertyTable.SettingsGroup.GlobalSettings, bestSettings, String.Format("Invalid settings for {0} {1}.", "best", "BestBooleanPropertyA"));
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BestBooleanPropertyA"));
			Assert.IsTrue((bool)ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));

			// Match on unique locals
			object ulpia;
			fPropertyExists = m_settings.PropertyExists("BestIntegerPropertyB");
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BestIntegerPropertyB"));
			fPropertyExists = m_settings.PropertyExists("BestIntegerPropertyB", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BestIntegerPropertyB"));
			fPropertyExists = m_settings.PropertyExists("BestIntegerPropertyB", out ulpia, PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BestIntegerPropertyB"));
			Assert.AreEqual(-586, (int)ulpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			fPropertyExists = m_settings.PropertyExists("BestIntegerPropertyB", out bestSettings);
			Assert.AreEqual(PropertyTable.SettingsGroup.LocalSettings, bestSettings, String.Format("Invalid settings for {0} {1}.", "best", "BestIntegerPropertyB"));
			fPropertyExists = m_settings.PropertyExists("BestIntegerPropertyB", out ulpia, out bestSettings);
			Assert.AreEqual(PropertyTable.SettingsGroup.LocalSettings, bestSettings, String.Format("Invalid settings for {0} {1}.", "best", "BestIntegerPropertyB"));
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BestIntegerPropertyB"));
			Assert.AreEqual(-586, (int)ulpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));

			// Match best locals common with global properties
			object bpba;
			fPropertyExists = m_settings.PropertyExists("BooleanPropertyA");
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BooleanPropertyA"));
			fPropertyExists = m_settings.PropertyExists("BooleanPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BooleanPropertyA"));
			fPropertyExists = m_settings.PropertyExists("BooleanPropertyA", out bpba, PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BooleanPropertyA"));
			Assert.IsTrue((bool)bpba, String.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
			fPropertyExists = m_settings.PropertyExists("BooleanPropertyA", out bestSettings);
			Assert.AreEqual(PropertyTable.SettingsGroup.LocalSettings, bestSettings, String.Format("Invalid settings for {0} {1}.", "best", "BooleanPropertyA"));
			fPropertyExists = m_settings.PropertyExists("BooleanPropertyA", out bpba, out bestSettings);
			Assert.AreEqual(PropertyTable.SettingsGroup.LocalSettings, bestSettings, String.Format("Invalid settings for {0} {1}.", "best", "BooleanPropertyA"));
			Assert.IsTrue(fPropertyExists, String.Format("{0} {1} not found.", "best", "BooleanPropertyA"));
			Assert.IsTrue((bool)bpba, String.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
		}

		/// <summary>
		/// Test the various versions of GetValue.
		/// </summary>
		[Test]
		public void GetValue()
		{
			// Test nonexistent values.
			object bestValue;
			bestValue = m_settings.GetValue("NonexistentPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsNull(bestValue, String.Format("Invalid value for {0} {1}.", "global", "NonexistentPropertyA"));
			bestValue = m_settings.GetValue("NonexistentPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(bestValue, String.Format("Invalid value for {0} {1}.", "local", "NonexistentPropertyA"));
			bestValue = m_settings.GetValue("NonexistentPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsNull(bestValue, String.Format("Invalid value for {0} {1}.", "best", "NonexistentPropertyA"));
			bestValue = m_settings.GetValue("NonexistentPropertyA");
			Assert.IsNull(bestValue, String.Format("Invalid value for {0} {1}.", "best", "NonexistentPropertyA"));

			// Test global property values.
			bool gpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));
			gpba = (bool)m_settings.GetValue("BooleanPropertyA", true, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));

			int gpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));
			gpia = (int)m_settings.GetValue("IntegerPropertyA", 352, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));

			string gpsa = (string)m_settings.GetValue("StringPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));
			gpsa = (string)m_settings.GetValue("StringPropertyA", "global_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));

			// Test locals property values.
			bool lpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));
			lpba = (bool)m_settings.GetValue("BooleanPropertyA", false, PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			int lpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));
			lpia = (int)m_settings.GetValue("IntegerPropertyA", 111, PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			string lpsa = (string)m_settings.GetValue("StringPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));
			lpsa = (string)m_settings.GetValue("StringPropertyA", "local_StringPropertyC_value", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			// Make new properties.
			object nullObject = null;
			// --- Set Globals and make sure Locals are still null.
			bool gpbc = (bool)m_settings.GetValue("BooleanPropertyC", true, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyC"));
			nullObject = m_settings.GetValue("BooleanPropertyC", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyC"));

			int gpic = (int)m_settings.GetValue("IntegerPropertyC", 352, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyC"));
			nullObject = m_settings.GetValue("IntegerPropertyC", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyC"));

			string gpsc = (string)m_settings.GetValue("StringPropertyC", "global_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyC"));
			nullObject = m_settings.GetValue("StringPropertyC", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyC"));

			// -- Set Locals and make sure Globals haven't changed.
			bool lpbc = (bool)m_settings.GetValue("BooleanPropertyC", false, PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsFalse(lpbc, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyC"));
			gpbc = (bool)m_settings.GetValue("BooleanPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyC"));

			int lpic = (int)m_settings.GetValue("IntegerPropertyC", 111, PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(111, lpic, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyC"));
			gpic = (int)m_settings.GetValue("IntegerPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyC"));

			string lpsc = (string)m_settings.GetValue("StringPropertyC", "local_StringPropertyC_value", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsc, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyC"));
			gpsc = (string)m_settings.GetValue("StringPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyC"));

			// Test best property values;
			// Match on locals common with globals first.
			bool bpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(bpba, String.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
			bpba = (bool)m_settings.GetValue("BooleanPropertyA");
			Assert.IsTrue(bpba, String.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
			bpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(bpba, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));
			bpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsFalse(bpba, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));

			int bpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual(333, bpia, String.Format("Invalid value for {0} {1}.", "best", "IntegerPropertyA"));
			bpia = (int)m_settings.GetValue("IntegerPropertyA");
			Assert.AreEqual(333, bpia, String.Format("Invalid value for {0} {1}.", "best", "IntegerPropertyA"));
			bpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(333, bpia, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));
			bpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, bpia, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));

			string bpsa = (string)m_settings.GetValue("StringPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual("local_StringPropertyA_value", bpsa, String.Format("Invalid value for {0} {1}.", "best", "StringPropertyA"));
			bpsa = (string)m_settings.GetValue("StringPropertyA");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, String.Format("Invalid value for {0} {1}.", "best", "StringPropertyA"));
			bpsa = (string)m_settings.GetValue("StringPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", bpsa, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));
			bpsa = (string)m_settings.GetValue("StringPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", bpsa, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));

			// Match on unique globals.
			bool ubpba = (bool)m_settings.GetValue("BestBooleanPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(ubpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			bool ugpba = (bool)m_settings.GetValue("BestBooleanPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			ugpba = (bool)m_settings.GetValue("BestBooleanPropertyA");
			Assert.IsTrue(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			ugpba = (bool)m_settings.GetValue("BestBooleanPropertyA", false);
			Assert.IsTrue(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			nullObject = m_settings.GetValue("BestBooleanPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));

			int ubpia = (int)m_settings.GetValue("BestIntegerPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual(-101, ubpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			int ugpia = (int)m_settings.GetValue("BestIntegerPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(-101, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			ugpia = (int)m_settings.GetValue("BestIntegerPropertyA");
			Assert.AreEqual(-101, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			ugpia = (int)m_settings.GetValue("BestIntegerPropertyA", -818);
			Assert.AreEqual(-101, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			nullObject = m_settings.GetValue("BestIntegerPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));

			string ubpsa = (string)m_settings.GetValue("BestStringPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ubpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			string ugpsa = (string)m_settings.GetValue("BestStringPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			ugpsa = (string)m_settings.GetValue("BestStringPropertyA");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			ugpsa = (string)m_settings.GetValue("BestStringPropertyA", "global_BestStringPropertyC_value");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			nullObject = m_settings.GetValue("BestStringPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));

			// Match on unique locals.
			ubpba = (bool)m_settings.GetValue("BestBooleanPropertyB", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsFalse(ubpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			bool ulpba = (bool)m_settings.GetValue("BestBooleanPropertyB", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsFalse(ubpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			ulpba = (bool)m_settings.GetValue("BestBooleanPropertyB");
			Assert.IsFalse(ulpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			ulpba = (bool)m_settings.GetValue("BestBooleanPropertyB", true);
			Assert.IsFalse(ulpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			nullObject = m_settings.GetValue("BestBooleanPropertyB", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));

			ubpia = (int)m_settings.GetValue("BestIntegerPropertyB", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual(-586, ubpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			int ulpia = (int)m_settings.GetValue("BestIntegerPropertyB", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(-586, ulpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			ulpia = (int)m_settings.GetValue("BestIntegerPropertyB");
			Assert.AreEqual(-586, ulpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			ulpia = (int)m_settings.GetValue("BestIntegerPropertyB", -685);
			Assert.AreEqual(-586, ulpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			nullObject = m_settings.GetValue("BestIntegerPropertyB", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));

			ubpsa = (string)m_settings.GetValue("BestStringPropertyB", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ubpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			string ulpsa = (string)m_settings.GetValue("BestStringPropertyB", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			ulpsa = (string)m_settings.GetValue("BestStringPropertyB");
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			ulpsa = (string)m_settings.GetValue("BestStringPropertyB", "local_BestStringPropertyC_value");
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			nullObject = m_settings.GetValue("BestStringPropertyB", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));

			// Make new best (global) properties
			ugpba = (bool)m_settings.GetValue("BestBooleanPropertyC", false);
			Assert.IsFalse(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			ugpba = (bool)m_settings.GetValue("BestBooleanPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			ugpia = (int)m_settings.GetValue("BestIntegerPropertyC", -818);
			Assert.AreEqual(-818, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			ugpia = (int)m_settings.GetValue("BestIntegerPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(-818, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			ugpsa = (string)m_settings.GetValue("BestStringPropertyC", "global_BestStringPropertyC_value");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
			ugpsa = (string)m_settings.GetValue("BestStringPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
		}


		/// <summary>
		/// Test the various versions of GetProperty.
		/// </summary>
		[Test]
		public void GetProperty()
		{
			// Test global property values.
			bool gpba = m_settings.GetBoolProperty("BooleanPropertyA", true, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));

			int gpia = m_settings.GetIntProperty("IntegerPropertyA", 352, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));

			string gpsa = m_settings.GetStringProperty("StringPropertyA", "global_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));

			// Test locals property values.
			bool lpba = m_settings.GetBoolProperty("BooleanPropertyA", false, PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			int lpia = m_settings.GetIntProperty("IntegerPropertyA", 111, PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			string lpsa = m_settings.GetStringProperty("StringPropertyA", "local_StringPropertyC_value", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			// Make new properties.
			bool gpbc = m_settings.GetBoolProperty("BooleanPropertyC", true, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyC"));
			int gpic = m_settings.GetIntProperty("IntegerPropertyC", 352, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyC"));
			string gpsc = m_settings.GetStringProperty("StringPropertyC", "global_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyC"));

			bool lpbc = m_settings.GetBoolProperty("BooleanPropertyC", false, PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsFalse(lpbc, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyC"));
			int lpic = m_settings.GetIntProperty("IntegerPropertyC", 111, PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(111, lpic, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyC"));
			string lpsc = m_settings.GetStringProperty("StringPropertyC", "local_StringPropertyC_value", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsc, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyC"));

			// Test best property values;
			// Match on locals common with globals first.
			bool bpba = m_settings.GetBoolProperty("BooleanPropertyA", false, PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(bpba, String.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
			bpba = m_settings.GetBoolProperty("BooleanPropertyA", false);
			Assert.IsTrue(bpba, String.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));

			int bpia = m_settings.GetIntProperty("IntegerPropertyA", -333, PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual(333, bpia, String.Format("Invalid value for {0} {1}.", "best", "IntegerPropertyA"));
			bpia = m_settings.GetIntProperty("IntegerPropertyA", -333);
			Assert.AreEqual(333, bpia, String.Format("Invalid value for {0} {1}.", "best", "IntegerPropertyA"));

			string bpsa = m_settings.GetStringProperty("StringPropertyA", "global_StringPropertyA_value", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual("local_StringPropertyA_value", bpsa, String.Format("Invalid value for {0} {1}.", "best", "StringPropertyA"));
			bpsa = m_settings.GetStringProperty("StringPropertyA", "global_StringPropertyA_value");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, String.Format("Invalid value for {0} {1}.", "best", "StringPropertyA"));

			// Match on unique globals.
			bool ugpba = m_settings.GetBoolProperty("BestBooleanPropertyA", false, PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			ugpba = m_settings.GetBoolProperty("BestBooleanPropertyA", false);
			Assert.IsTrue(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));

			int ugpia = m_settings.GetIntProperty("BestIntegerPropertyA", 101, PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual(-101, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			ugpia = m_settings.GetIntProperty("BestIntegerPropertyA", 101);
			Assert.AreEqual(-101, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));

			string ugpsa = m_settings.GetStringProperty("BestStringPropertyA", "local_BestStringPropertyA_value", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			ugpsa = m_settings.GetStringProperty("BestStringPropertyA", "local_BestStringPropertyA_value");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));

			// Match on unique locals.
			bool ulpba = m_settings.GetBoolProperty("BestBooleanPropertyB", true, PropertyTable.SettingsGroup.BestSettings);
			Assert.IsFalse(ulpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			ulpba = m_settings.GetBoolProperty("BestBooleanPropertyB", true);
			Assert.IsFalse(ulpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));

			int ulpia = m_settings.GetIntProperty("BestIntegerPropertyB", 586, PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual(-586, ulpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			ulpia = m_settings.GetIntProperty("BestIntegerPropertyB", 586);
			Assert.AreEqual(-586, ulpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));

			string ulpsa = m_settings.GetStringProperty("BestStringPropertyB", "global_BestStringPropertyC_value", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			ulpsa = m_settings.GetStringProperty("BestStringPropertyB", "global_BestStringPropertyC_value");
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));

			// Make new best (global) properties
			ugpba = m_settings.GetBoolProperty("BestBooleanPropertyC", false);
			Assert.IsFalse(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			ugpia = m_settings.GetIntProperty("BestIntegerPropertyC", -818);
			Assert.AreEqual(-818, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			ugpsa = m_settings.GetStringProperty("BestStringPropertyC", "global_BestStringPropertyC_value");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
		}

		/// <summary>
		/// Test the various versions of SetProperty.
		/// </summary>
		[Test]
		public void SetProperty()
		{
			// Change existing Global & Local values, check that they don't overwrite each other.
			m_settings.SetProperty("BooleanPropertyA", false, PropertyTable.SettingsGroup.LocalSettings);
			m_settings.SetProperty("BooleanPropertyA", true, PropertyTable.SettingsGroup.GlobalSettings);
			bool gpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpba, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));
			bool lpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsFalse(lpba, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			m_settings.SetProperty("BooleanPropertyA", false, PropertyTable.SettingsGroup.GlobalSettings);
			m_settings.SetProperty("BooleanPropertyA", true, PropertyTable.SettingsGroup.LocalSettings);
			lpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));
			gpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));

			m_settings.SetProperty("IntegerPropertyA",  253, PropertyTable.SettingsGroup.LocalSettings);
			m_settings.SetProperty("IntegerPropertyA", -253, PropertyTable.SettingsGroup.GlobalSettings);
			int gpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(-253, gpia, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));
			int lpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(253, lpia, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			m_settings.SetProperty("IntegerPropertyA", 253, PropertyTable.SettingsGroup.GlobalSettings);
			m_settings.SetProperty("IntegerPropertyA", -253, PropertyTable.SettingsGroup.LocalSettings);
			lpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(-253, lpia, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));
			gpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));

			m_settings.SetProperty("StringPropertyA", "local_StringPropertyC_value", PropertyTable.SettingsGroup.LocalSettings);
			m_settings.SetProperty("StringPropertyA", "global_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			string gpsa = (string)m_settings.GetValue("StringPropertyA", "local_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsa, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));
			string lpsa = (string)m_settings.GetValue("StringPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsa, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			m_settings.SetProperty("StringPropertyA", "global_StringPropertyA_value", PropertyTable.SettingsGroup.GlobalSettings);
			m_settings.SetProperty("StringPropertyA", "local_StringPropertyA_value", PropertyTable.SettingsGroup.LocalSettings);
			lpsa = (string)m_settings.GetValue("StringPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));
			gpsa = (string)m_settings.GetValue("StringPropertyA", "local_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));

			// Make new properties. ------------------
			//---- Global Settings
			m_settings.SetProperty("BooleanPropertyC", true, PropertyTable.SettingsGroup.GlobalSettings);
			bool gpbc = m_settings.GetBoolProperty("BooleanPropertyC", false, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyC"));

			m_settings.SetProperty("IntegerPropertyC", 352, PropertyTable.SettingsGroup.GlobalSettings);
			int gpic = m_settings.GetIntProperty("IntegerPropertyC", -352, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyC"));

			m_settings.SetProperty("StringPropertyC", "global_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			string gpsc = m_settings.GetStringProperty("StringPropertyC", "local_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyC"));

			//---- Local Settings
			m_settings.SetProperty("BooleanPropertyC", false, PropertyTable.SettingsGroup.LocalSettings);
			bool lpbc = m_settings.GetBoolProperty("BooleanPropertyC", true, PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsFalse(lpbc, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyC"));

			m_settings.SetProperty("IntegerPropertyC", 111, PropertyTable.SettingsGroup.LocalSettings);
			int lpic = m_settings.GetIntProperty("IntegerPropertyC", -111, PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(111, lpic, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyC"));

			m_settings.SetProperty("StringPropertyC", "local_StringPropertyC_value", PropertyTable.SettingsGroup.LocalSettings);
			string lpsc = m_settings.GetStringProperty("StringPropertyC", "local_StringPropertyC_value", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsc, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyC"));

			// Set best property on locals common with globals first.
			m_settings.SetProperty("BooleanPropertyA", true, PropertyTable.SettingsGroup.LocalSettings);
			m_settings.SetProperty("BooleanPropertyA", true, PropertyTable.SettingsGroup.GlobalSettings);
			m_settings.SetProperty("BooleanPropertyA", false, PropertyTable.SettingsGroup.BestSettings);
			bool bpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsFalse(bpba, String.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
			gpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpba, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));
			lpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsFalse(lpba, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			m_settings.SetProperty("IntegerPropertyA", 253, PropertyTable.SettingsGroup.LocalSettings);
			m_settings.SetProperty("IntegerPropertyA", -253, PropertyTable.SettingsGroup.GlobalSettings);
			m_settings.SetProperty("IntegerPropertyA", 352, PropertyTable.SettingsGroup.BestSettings);
			int bpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual(352, bpia, String.Format("Invalid value for {0} {1}.", "best", "IntegerPropertyA"));
			gpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(-253, gpia, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));
			lpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(352, lpia, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			m_settings.SetProperty("StringPropertyA", "local_StringPropertyA_value", PropertyTable.SettingsGroup.LocalSettings);
			m_settings.SetProperty("StringPropertyA", "global_StringPropertyA_value", PropertyTable.SettingsGroup.GlobalSettings);
			m_settings.SetProperty("StringPropertyA", "best_StringPropertyA_value", PropertyTable.SettingsGroup.BestSettings);
			string bpsa = (string)m_settings.GetValue("StringPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual("best_StringPropertyA_value", bpsa, String.Format("Invalid value for {0} {1}.", "best", "StringPropertyA"));
			gpsa = (string)m_settings.GetValue("StringPropertyA", "local_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));
			lpsa = (string)m_settings.GetValue("StringPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("best_StringPropertyA_value", lpsa, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			object nullObject = null;

			// Set best setting on unique globals.
			m_settings.SetProperty("BestBooleanPropertyA", false, PropertyTable.SettingsGroup.BestSettings);
			bool ubpba = (bool)m_settings.GetValue("BestBooleanPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsFalse(ubpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			bool ugpba = (bool)m_settings.GetValue("BestBooleanPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			nullObject = m_settings.GetValue("BestBooleanPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));

			m_settings.SetProperty("BestIntegerPropertyA", 101, PropertyTable.SettingsGroup.BestSettings);
			int ubpia = (int)m_settings.GetValue("BestIntegerPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual(101, ubpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			int ugpia = (int)m_settings.GetValue("BestIntegerPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(101, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			nullObject = m_settings.GetValue("BestIntegerPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));

			m_settings.SetProperty("BestStringPropertyA", "best_BestStringPropertyA_value", PropertyTable.SettingsGroup.BestSettings);
			string ubpsa = (string)m_settings.GetValue("BestStringPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual("best_BestStringPropertyA_value", ubpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			string ugpsa = (string)m_settings.GetValue("BestStringPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("best_BestStringPropertyA_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			nullObject = m_settings.GetValue("BestStringPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));

			// Set best setting on unique locals
			m_settings.SetProperty("BestBooleanPropertyB", true, PropertyTable.SettingsGroup.BestSettings);
			ubpba = (bool)m_settings.GetValue("BestBooleanPropertyB", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(ubpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			bool ulpba = (bool)m_settings.GetValue("BestBooleanPropertyB", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(ubpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			nullObject = m_settings.GetValue("BestBooleanPropertyB", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));

			m_settings.SetProperty("BestIntegerPropertyB", 586, PropertyTable.SettingsGroup.BestSettings);
			ubpia = (int)m_settings.GetValue("BestIntegerPropertyB", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual(586, ubpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			int ulpia = (int)m_settings.GetValue("BestIntegerPropertyB", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(586, ulpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			nullObject = m_settings.GetValue("BestIntegerPropertyB", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));

			m_settings.SetProperty("BestStringPropertyB", "best_BestStringPropertyB_value", PropertyTable.SettingsGroup.BestSettings);
			ubpsa = (string)m_settings.GetValue("BestStringPropertyB", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual("best_BestStringPropertyB_value", ubpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			string ulpsa = (string)m_settings.GetValue("BestStringPropertyB", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("best_BestStringPropertyB_value", ulpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			nullObject = m_settings.GetValue("BestStringPropertyB", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));

			// Make new best (global) properties
			m_settings.SetProperty("BestBooleanPropertyC", false);
			ugpba = (bool)m_settings.GetValue("BestBooleanPropertyC");
			Assert.IsFalse(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			ugpba = (bool)m_settings.GetValue("BestBooleanPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));

			m_settings.SetProperty("BestIntegerPropertyC", -818);
			ugpia = (int)m_settings.GetValue("BestIntegerPropertyC");
			Assert.AreEqual(-818, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			ugpia = (int)m_settings.GetValue("BestIntegerPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(-818, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));

			m_settings.SetProperty("BestStringPropertyC", "global_BestStringPropertyC_value");
			ugpsa = (string)m_settings.GetValue("BestStringPropertyC");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
			ugpsa = (string)m_settings.GetValue("BestStringPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
		}

		/// <summary>
		/// Test the various versions of SetDefault.
		/// </summary>
		[Test]
		public void SetDefault()
		{
			// Try changing existing Global & Local values, check that they don't overwrite existing ones.
			m_settings.SetDefault("BooleanPropertyA", false, false, PropertyTable.SettingsGroup.LocalSettings);
			m_settings.SetDefault("BooleanPropertyA", true, false, PropertyTable.SettingsGroup.GlobalSettings);
			bool gpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));
			bool lpba = (bool)m_settings.GetValue("BooleanPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			m_settings.SetDefault("IntegerPropertyA", 253, false, PropertyTable.SettingsGroup.LocalSettings);
			m_settings.SetDefault("IntegerPropertyA", -253, false, PropertyTable.SettingsGroup.GlobalSettings);
			int gpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));
			int lpia = (int)m_settings.GetValue("IntegerPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			m_settings.SetDefault("StringPropertyA", "local_StringPropertyC_value", false, PropertyTable.SettingsGroup.LocalSettings);
			m_settings.SetDefault("StringPropertyA", "global_StringPropertyC_value", false, PropertyTable.SettingsGroup.GlobalSettings);
			string gpsa = (string)m_settings.GetValue("StringPropertyA", "local_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));
			string lpsa = (string)m_settings.GetValue("StringPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			// Make new properties. ------------------
			//---- Global Settings
			m_settings.SetDefault("BooleanPropertyC", true, true, PropertyTable.SettingsGroup.GlobalSettings);
			bool gpbc = m_settings.GetBoolProperty("BooleanPropertyC", false, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, String.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyC"));

			m_settings.SetDefault("IntegerPropertyC", 352, true, PropertyTable.SettingsGroup.GlobalSettings);
			int gpic = m_settings.GetIntProperty("IntegerPropertyC", -352, PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyC"));

			m_settings.SetDefault("StringPropertyC", "global_StringPropertyC_value", true, PropertyTable.SettingsGroup.GlobalSettings);
			string gpsc = m_settings.GetStringProperty("StringPropertyC", "local_StringPropertyC_value", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, String.Format("Invalid value for {0} {1}.", "global", "StringPropertyC"));

			//---- Local Settings
			m_settings.SetDefault("BooleanPropertyC", false, false, PropertyTable.SettingsGroup.LocalSettings);
			bool lpbc = m_settings.GetBoolProperty("BooleanPropertyC", true, PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsFalse(lpbc, String.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyC"));

			m_settings.SetDefault("IntegerPropertyC", 111, false, PropertyTable.SettingsGroup.LocalSettings);
			int lpic = m_settings.GetIntProperty("IntegerPropertyC", -111, PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(111, lpic, String.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyC"));

			m_settings.SetDefault("StringPropertyC", "local_StringPropertyC_value", false, PropertyTable.SettingsGroup.LocalSettings);
			string lpsc = m_settings.GetStringProperty("StringPropertyC", "local_StringPropertyC_value", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsc, String.Format("Invalid value for {0} {1}.", "local", "StringPropertyC"));

			object nullObject = null;

			// Set best setting on unique globals.
			m_settings.SetDefault("BestBooleanPropertyA", false, false, PropertyTable.SettingsGroup.BestSettings);
			bool ubpba = (bool)m_settings.GetValue("BestBooleanPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsTrue(ubpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			bool ugpba = (bool)m_settings.GetValue("BestBooleanPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsTrue(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			nullObject = m_settings.GetValue("BestBooleanPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));

			m_settings.SetDefault("BestIntegerPropertyA", 101, false, PropertyTable.SettingsGroup.BestSettings);
			int ubpia = (int)m_settings.GetValue("BestIntegerPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual(-101, ubpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			int ugpia = (int)m_settings.GetValue("BestIntegerPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(-101, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			nullObject = m_settings.GetValue("BestIntegerPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));

			m_settings.SetDefault("BestStringPropertyA", "best_BestStringPropertyA_value", false, PropertyTable.SettingsGroup.BestSettings);
			string ubpsa = (string)m_settings.GetValue("BestStringPropertyA", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ubpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			string ugpsa = (string)m_settings.GetValue("BestStringPropertyA", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			nullObject = m_settings.GetValue("BestStringPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));

			// Set best setting on unique locals
			m_settings.SetDefault("BestBooleanPropertyB", true, false, PropertyTable.SettingsGroup.BestSettings);
			ubpba = (bool)m_settings.GetValue("BestBooleanPropertyB", PropertyTable.SettingsGroup.BestSettings);
			Assert.IsFalse(ubpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			bool ulpba = (bool)m_settings.GetValue("BestBooleanPropertyB", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsFalse(ubpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			nullObject = m_settings.GetValue("BestBooleanPropertyB", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));

			m_settings.SetDefault("BestIntegerPropertyB", 586, false, PropertyTable.SettingsGroup.BestSettings);
			ubpia = (int)m_settings.GetValue("BestIntegerPropertyB", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual(-586, ubpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			int ulpia = (int)m_settings.GetValue("BestIntegerPropertyB", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual(-586, ulpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			nullObject = m_settings.GetValue("BestIntegerPropertyB", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));

			m_settings.SetDefault("BestStringPropertyB", "best_BestStringPropertyB_value", false, PropertyTable.SettingsGroup.BestSettings);
			ubpsa = (string)m_settings.GetValue("BestStringPropertyB", PropertyTable.SettingsGroup.BestSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ubpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			string ulpsa = (string)m_settings.GetValue("BestStringPropertyB", PropertyTable.SettingsGroup.LocalSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			nullObject = m_settings.GetValue("BestStringPropertyB", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));

			// Make new best (global) properties
			m_settings.SetDefault("BestBooleanPropertyC", false, false);
			ugpba = (bool)m_settings.GetValue("BestBooleanPropertyC");
			Assert.IsFalse(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			ugpba = (bool)m_settings.GetValue("BestBooleanPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, String.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			nullObject = m_settings.GetValue("BestBooleanPropertyC", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "local", "BestBooleanPropertyC"));

			m_settings.SetDefault("BestIntegerPropertyC", -818, false);
			ugpia = (int)m_settings.GetValue("BestIntegerPropertyC");
			Assert.AreEqual(-818, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			ugpia = (int)m_settings.GetValue("BestIntegerPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual(-818, ugpia, String.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			nullObject = m_settings.GetValue("BestIntegerPropertyC", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "local", "BestIntegerPropertyC"));

			m_settings.SetDefault("BestStringPropertyC", "global_BestStringPropertyC_value", false);
			ugpsa = (string)m_settings.GetValue("BestStringPropertyC");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
			ugpsa = (string)m_settings.GetValue("BestStringPropertyC", PropertyTable.SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, String.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
			nullObject = m_settings.GetValue("BestStringPropertyA", PropertyTable.SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, String.Format("Invalid value for {0} {1}.", "local", "BestStringPropertyA"));
		}

		/// <summary>
		/// Test the various versions of SetPropertyPersistence.
		/// </summary>
		[Test]
		[Ignore("Need to write.")]
		public void SetPropertyPersistence()
		{

		}


	}
}
