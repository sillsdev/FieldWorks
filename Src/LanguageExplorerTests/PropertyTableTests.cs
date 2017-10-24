// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorerTests
{
	/// <summary>
	/// PropertyTableTests.
	/// </summary>
	[TestFixture]
	public class PropertyTableTests
	{
		private IPublisher m_publisher;
		private IPropertyTable m_propertyTable;
		string m_originalSettingsPath;
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
			m_originalSettingsPath = Path.Combine(TempPath, "SettingsBackup");
			if (!Directory.Exists(m_originalSettingsPath))
				Directory.CreateDirectory(m_originalSettingsPath);

			File.WriteAllText(Path.Combine(m_originalSettingsPath, "db$TestLocal$Settings.xml"), LanguageExplorerTestsResources.db_TestLocal_Settings_xml);
			File.WriteAllText(Path.Combine(m_originalSettingsPath, "Settings.xml"), LanguageExplorerTestsResources.Settings_xml);
		}

		/// <summary />
		[SetUp]
		public void SetUp()
		{
			TestSetupServices.SetupTestPubSubSystem(out m_publisher);
			m_propertyTable = TestSetupServices.SetupTestPropertyTable(m_publisher);
			m_propertyTable.LocalSettingsId = "TestLocal";
			m_propertyTable.UserSettingDirectory = m_originalSettingsPath;

			LoadOriginalSettings();
		}

		/// <summary />
		[TearDown]
		public void TearDown()
		{
			m_propertyTable.Dispose();
			m_propertyTable = null;
			m_publisher = null;
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
			m_propertyTable.UserSettingDirectory = m_originalSettingsPath;
			m_propertyTable.RestoreFromFile(settingsId);
		}

		private void LoadOriginalSettings()
		{
			LoadOriginalSettings(m_propertyTable.LocalSettingsId);
			LoadOriginalSettings(m_propertyTable.GlobalSettingsId);
		}

		/// <summary>
		/// Test the various versions of TryGetValue.
		/// </summary>
		[Test]
		public void TryGetValueTest()
		{
			object bestValue;

			// Test nonexistent properties.
			var fPropertyExists = m_propertyTable.TryGetValue("NonexistentPropertyA", out bestValue);
			Assert.IsFalse(fPropertyExists, string.Format("{0} {1} should not exist.", "best", "NonexistentPropertyA"));
			Assert.IsNull(bestValue, string.Format("Invalid value for {0} {1}.", "best", "NonexistentPropertyA"));

			// Test global property values.
			bool gpba;
			fPropertyExists = m_propertyTable.TryGetValue("BooleanPropertyA", SettingsGroup.GlobalSettings, out gpba);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "global", "BooleanPropertyA"));
			Assert.IsFalse(gpba, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));

			int gpia;
			fPropertyExists = m_propertyTable.TryGetValue("IntegerPropertyA", SettingsGroup.GlobalSettings, out gpia);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "global", "IntegerPropertyA"));
			Assert.AreEqual(253, gpia, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));

			string gpsa;
			fPropertyExists = m_propertyTable.TryGetValue("StringPropertyA", SettingsGroup.GlobalSettings, out gpsa);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "global", "StringPropertyA"));
			Assert.AreEqual("global_StringPropertyA_value", gpsa, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));

			// Test local property values
			bool lpba;
			fPropertyExists = m_propertyTable.TryGetValue("BooleanPropertyA", SettingsGroup.LocalSettings, out lpba);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "local", "BooleanPropertyA"));
			Assert.IsTrue(lpba, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			int lpia;
			fPropertyExists = m_propertyTable.TryGetValue("IntegerPropertyA", SettingsGroup.LocalSettings, out lpia);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "local", "IntegerPropertyA"));
			Assert.AreEqual(333, lpia, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			string lpsa;
			fPropertyExists = m_propertyTable.TryGetValue("StringPropertyA", SettingsGroup.LocalSettings, out lpsa);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "local", "StringPropertyA"));
			Assert.AreEqual("local_StringPropertyA_value", lpsa, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			// Test best settings
			// Match on unique globals.
			bool ugpba;
			fPropertyExists = m_propertyTable.TryGetValue("BestBooleanPropertyA", SettingsGroup.BestSettings, out ugpba);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BestBooleanPropertyA"));
			Assert.IsTrue(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			fPropertyExists = m_propertyTable.TryGetValue("BestBooleanPropertyA", out ugpba);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BestBooleanPropertyA"));
			Assert.IsTrue(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));

			// Match on unique locals
			int ulpia;
			fPropertyExists = m_propertyTable.TryGetValue("BestIntegerPropertyB", SettingsGroup.BestSettings, out ulpia);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BestIntegerPropertyB"));
			Assert.AreEqual(-586, ulpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			fPropertyExists = m_propertyTable.TryGetValue("BestIntegerPropertyB", out ulpia);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BestIntegerPropertyB"));
			Assert.AreEqual(-586, ulpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));

			// Match best locals common with global properties
			bool bpba;
			fPropertyExists = m_propertyTable.TryGetValue("BooleanPropertyA", SettingsGroup.BestSettings, out bpba);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BooleanPropertyA"));
			Assert.IsTrue(bpba, string.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
			fPropertyExists = m_propertyTable.TryGetValue("BooleanPropertyA", out bpba);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BooleanPropertyA"));
			Assert.IsTrue(bpba, string.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
		}

		/// <summary>
		/// Test the various versions of PropertyExists.
		/// </summary>
		[Test]
		public void PropertyExists()
		{
			bool fPropertyExists;
			object bestValue;
			SettingsGroup bestSettings;

			// Test nonexistent properties.
			fPropertyExists = m_propertyTable.PropertyExists("NonexistentPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(fPropertyExists, string.Format("{0} {1} should not exist.", "global", "NonexistentPropertyA"));
			fPropertyExists = m_propertyTable.PropertyExists("NonexistentPropertyA", SettingsGroup.LocalSettings);
			Assert.IsFalse(fPropertyExists, string.Format("{0} {1} should not exist.", "local", "NonexistentPropertyA"));
			fPropertyExists = m_propertyTable.PropertyExists("NonexistentPropertyA");
			Assert.IsFalse(fPropertyExists, string.Format("{0} {1} should not exist.", "best", "NonexistentPropertyA"));

			// Test global property values.
			bool gpba;
			fPropertyExists = m_propertyTable.PropertyExists("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "global", "BooleanPropertyA"));

			int gpia;
			fPropertyExists = m_propertyTable.PropertyExists("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "global", "IntegerPropertyA"));

			string gpsa;
			fPropertyExists = m_propertyTable.PropertyExists("StringPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "global", "StringPropertyA"));

			// Test local property values
			bool lpba;
			fPropertyExists = m_propertyTable.PropertyExists("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "local", "BooleanPropertyA"));

			int lpia;
			fPropertyExists = m_propertyTable.PropertyExists("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "local", "IntegerPropertyA"));

			string lpsa;
			fPropertyExists = m_propertyTable.PropertyExists("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "local", "StringPropertyA"));

			// Test best settings
			// Match on unique globals.
			bool ugpba;
			fPropertyExists = m_propertyTable.PropertyExists("BestBooleanPropertyA");
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BestBooleanPropertyA"));
			fPropertyExists = m_propertyTable.PropertyExists("BestBooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BestBooleanPropertyA"));

			// Match on unique locals
			int ulpia;
			fPropertyExists = m_propertyTable.PropertyExists("BestIntegerPropertyB");
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BestIntegerPropertyB"));
			fPropertyExists = m_propertyTable.PropertyExists("BestIntegerPropertyB", SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BestIntegerPropertyB"));
			fPropertyExists = m_propertyTable.PropertyExists("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BestIntegerPropertyB"));
			fPropertyExists = m_propertyTable.PropertyExists("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsFalse(fPropertyExists, string.Format("{0} {1} not found.", "best", "BestIntegerPropertyB"));

			// Match best locals common with global properties
			bool bpba;
			fPropertyExists = m_propertyTable.PropertyExists("BooleanPropertyA");
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BooleanPropertyA"));
			fPropertyExists = m_propertyTable.PropertyExists("BooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, string.Format("{0} {1} not found.", "best", "BooleanPropertyA"));
		}

		/// <summary>
		/// Test the various versions of GetValue.
		/// </summary>
		[Test]
		public void GetValue()
		{
			// Test nonexistent values.
			object bestValue;
			bestValue = m_propertyTable.GetValue<object>("NonexistentPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsNull(bestValue, string.Format("Invalid value for {0} {1}.", "global", "NonexistentPropertyA"));
			bestValue = m_propertyTable.GetValue<object>("NonexistentPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(bestValue, string.Format("Invalid value for {0} {1}.", "local", "NonexistentPropertyA"));
			bestValue = m_propertyTable.GetValue<object>("NonexistentPropertyA", SettingsGroup.BestSettings);
			Assert.IsNull(bestValue, string.Format("Invalid value for {0} {1}.", "best", "NonexistentPropertyA"));
			bestValue = m_propertyTable.GetValue<object>("NonexistentPropertyA");
			Assert.IsNull(bestValue, string.Format("Invalid value for {0} {1}.", "best", "NonexistentPropertyA"));

			// Test global property values.
			bool gpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));
			gpba = m_propertyTable.GetValue("BooleanPropertyA", SettingsGroup.GlobalSettings, true);
			Assert.IsFalse(gpba, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));

			int gpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));
			gpia = m_propertyTable.GetValue("IntegerPropertyA", SettingsGroup.GlobalSettings, 352);
			Assert.AreEqual(253, gpia, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));

			string gpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));
			gpsa = m_propertyTable.GetValue("StringPropertyA", SettingsGroup.GlobalSettings, "global_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));

			// Test locals property values.
			bool lpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));
			lpba = m_propertyTable.GetValue("BooleanPropertyA", SettingsGroup.LocalSettings, false);
			Assert.IsTrue(lpba, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			int lpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));
			lpia = m_propertyTable.GetValue("IntegerPropertyA", SettingsGroup.LocalSettings, 111);
			Assert.AreEqual(333, lpia, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			string lpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));
			lpsa = m_propertyTable.GetValue("StringPropertyA", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyA_value", lpsa, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			// Make new properties.
			object nullObject;
			// --- Set Globals and make sure Locals are still null.
			bool gpbc = m_propertyTable.GetValue("BooleanPropertyC", SettingsGroup.GlobalSettings, true);
			Assert.IsTrue(gpbc, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyC"));
			nullObject = m_propertyTable.GetValue<object>("BooleanPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyC"));

			int gpic = m_propertyTable.GetValue("IntegerPropertyC", SettingsGroup.GlobalSettings, 352);
			Assert.AreEqual(352, gpic, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyC"));
			nullObject = m_propertyTable.GetValue<object>("IntegerPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyC"));

			string gpsc = m_propertyTable.GetValue("StringPropertyC", SettingsGroup.GlobalSettings, "global_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyC_value", gpsc, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyC"));
			nullObject = m_propertyTable.GetValue<object>("StringPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyC"));

			// -- Set Locals and make sure Globals haven't changed.
			bool lpbc = m_propertyTable.GetValue("BooleanPropertyC", SettingsGroup.LocalSettings, false);
			Assert.IsFalse(lpbc, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyC"));
			gpbc = m_propertyTable.GetValue<bool>("BooleanPropertyC", SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyC"));

			int lpic = m_propertyTable.GetValue("IntegerPropertyC", SettingsGroup.LocalSettings, 111);
			Assert.AreEqual(111, lpic, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyC"));
			gpic = m_propertyTable.GetValue<int>("IntegerPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyC"));

			string lpsc = m_propertyTable.GetValue("StringPropertyC", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyC_value", lpsc, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyC"));
			gpsc = m_propertyTable.GetValue<string>("StringPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyC"));

			// Test best property values;
			// Match on locals common with globals first.
			bool bpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsTrue(bpba, string.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
			bpba = m_propertyTable.GetValue<bool>("BooleanPropertyA");
			Assert.IsTrue(bpba, string.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
			bpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(bpba, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));
			bpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(bpba, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));

			int bpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual(333, bpia, string.Format("Invalid value for {0} {1}.", "best", "IntegerPropertyA"));
			bpia = m_propertyTable.GetValue<int>("IntegerPropertyA");
			Assert.AreEqual(333, bpia, string.Format("Invalid value for {0} {1}.", "best", "IntegerPropertyA"));
			bpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(333, bpia, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));
			bpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, bpia, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));

			string bpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual("local_StringPropertyA_value", bpsa, string.Format("Invalid value for {0} {1}.", "best", "StringPropertyA"));
			bpsa = m_propertyTable.GetValue<string>("StringPropertyA");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, string.Format("Invalid value for {0} {1}.", "best", "StringPropertyA"));
			bpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", bpsa, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));
			bpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", bpsa, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));

			// Match on unique globals.
			bool ubpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsTrue(ubpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			bool ugpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			ugpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyA");
			Assert.IsTrue(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			ugpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyA", false);
			Assert.IsTrue(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			nullObject = m_propertyTable.GetValue<object>("BestBooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));

			int ubpia = m_propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual(-101, ubpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			int ugpia = m_propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-101, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			ugpia = m_propertyTable.GetValue<int>("BestIntegerPropertyA");
			Assert.AreEqual(-101, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			ugpia = m_propertyTable.GetValue("BestIntegerPropertyA", -818);
			Assert.AreEqual(-101, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			nullObject = m_propertyTable.GetValue<object>("BestIntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));

			string ubpsa = m_propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ubpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			string ugpsa = m_propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			ugpsa = m_propertyTable.GetValue<string>("BestStringPropertyA");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			ugpsa = m_propertyTable.GetValue("BestStringPropertyA", "global_BestStringPropertyC_value");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			nullObject = m_propertyTable.GetValue<object>("BestStringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));

			// Match on unique locals.
			ubpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyB", SettingsGroup.BestSettings);
			Assert.IsFalse(ubpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			bool ulpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyB", SettingsGroup.LocalSettings);
			Assert.IsFalse(ubpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			ulpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyB");
			Assert.IsFalse(ulpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			ulpba = m_propertyTable.GetValue("BestBooleanPropertyB", true);
			Assert.IsFalse(ulpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			nullObject = m_propertyTable.GetValue<object>("BestBooleanPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));

			ubpia = m_propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual(-586, ubpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			int ulpia = m_propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual(-586, ulpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			ulpia = m_propertyTable.GetValue<int>("BestIntegerPropertyB");
			Assert.AreEqual(-586, ulpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			ulpia = m_propertyTable.GetValue<int>("BestIntegerPropertyB", -685);
			Assert.AreEqual(-586, ulpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			nullObject = m_propertyTable.GetValue<object>("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));

			ubpsa = m_propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ubpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			string ulpsa = m_propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			ulpsa = m_propertyTable.GetValue<string>("BestStringPropertyB");
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			ulpsa = m_propertyTable.GetValue("BestStringPropertyB", "local_BestStringPropertyC_value");
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			nullObject = m_propertyTable.GetValue<object>("BestStringPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));

			// Make new best (global) properties
			ugpba = m_propertyTable.GetValue("BestBooleanPropertyC", false);
			Assert.IsFalse(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			ugpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyC", SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			ugpia = m_propertyTable.GetValue("BestIntegerPropertyC", -818);
			Assert.AreEqual(-818, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			ugpia = m_propertyTable.GetValue<int>("BestIntegerPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-818, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			ugpsa = m_propertyTable.GetValue("BestStringPropertyC", "global_BestStringPropertyC_value");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
			ugpsa = m_propertyTable.GetValue<string>("BestStringPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
		}


		/// <summary>
		/// Test the various versions of GetProperty.
		/// </summary>
		[Test]
		public void Get_X_Property()
		{
			// Test global property values.
			bool gpba = m_propertyTable.GetValue("BooleanPropertyA", SettingsGroup.GlobalSettings, true);
			Assert.IsFalse(gpba, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));

			int gpia = m_propertyTable.GetValue("IntegerPropertyA", SettingsGroup.GlobalSettings, 352);
			Assert.AreEqual(253, gpia, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));

			string gpsa = m_propertyTable.GetValue("StringPropertyA", SettingsGroup.GlobalSettings, "global_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));

			// Test locals property values.
			bool lpba = m_propertyTable.GetValue("BooleanPropertyA", SettingsGroup.LocalSettings, false);
			Assert.IsTrue(lpba, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			int lpia = m_propertyTable.GetValue("IntegerPropertyA", SettingsGroup.LocalSettings, 111);
			Assert.AreEqual(333, lpia, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			string lpsa = m_propertyTable.GetValue("StringPropertyA", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyA_value", lpsa, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			// Make new properties.
			bool gpbc = m_propertyTable.GetValue("BooleanPropertyC", SettingsGroup.GlobalSettings, true);
			Assert.IsTrue(gpbc, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyC"));
			int gpic = m_propertyTable.GetValue("IntegerPropertyC", SettingsGroup.GlobalSettings, 352);
			Assert.AreEqual(352, gpic, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyC"));
			string gpsc = m_propertyTable.GetValue("StringPropertyC", SettingsGroup.GlobalSettings, "global_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyC_value", gpsc, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyC"));

			bool lpbc = m_propertyTable.GetValue("BooleanPropertyC", SettingsGroup.LocalSettings, false);
			Assert.IsFalse(lpbc, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyC"));
			int lpic = m_propertyTable.GetValue("IntegerPropertyC", SettingsGroup.LocalSettings, 111);
			Assert.AreEqual(111, lpic, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyC"));
			string lpsc = m_propertyTable.GetValue("StringPropertyC", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyC_value", lpsc, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyC"));

			// Test best property values;
			// Match on locals common with globals first.
			bool bpba = m_propertyTable.GetValue("BooleanPropertyA", SettingsGroup.BestSettings, false);
			Assert.IsTrue(bpba, string.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
			bpba = m_propertyTable.GetValue("BooleanPropertyA", false);
			Assert.IsTrue(bpba, string.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));

			int bpia = m_propertyTable.GetValue("IntegerPropertyA", SettingsGroup.BestSettings, -333);
			Assert.AreEqual(333, bpia, string.Format("Invalid value for {0} {1}.", "best", "IntegerPropertyA"));
			bpia = m_propertyTable.GetValue("IntegerPropertyA", -333);
			Assert.AreEqual(333, bpia, string.Format("Invalid value for {0} {1}.", "best", "IntegerPropertyA"));

			string bpsa = m_propertyTable.GetValue("StringPropertyA", SettingsGroup.BestSettings, "global_StringPropertyA_value");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, string.Format("Invalid value for {0} {1}.", "best", "StringPropertyA"));
			bpsa = m_propertyTable.GetValue("StringPropertyA", "global_StringPropertyA_value");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, string.Format("Invalid value for {0} {1}.", "best", "StringPropertyA"));

			// Match on unique globals.
			bool ugpba = m_propertyTable.GetValue("BestBooleanPropertyA", SettingsGroup.BestSettings, false);
			Assert.IsTrue(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			ugpba = m_propertyTable.GetValue("BestBooleanPropertyA", false);
			Assert.IsTrue(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));

			int ugpia = m_propertyTable.GetValue("BestIntegerPropertyA", SettingsGroup.BestSettings, 101);
			Assert.AreEqual(-101, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			ugpia = m_propertyTable.GetValue("BestIntegerPropertyA", 101);
			Assert.AreEqual(-101, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));

			string ugpsa = m_propertyTable.GetValue("BestStringPropertyA", SettingsGroup.BestSettings, "local_BestStringPropertyA_value");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			ugpsa = m_propertyTable.GetValue("BestStringPropertyA", "local_BestStringPropertyA_value");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));

			// Match on unique locals.
			bool ulpba = m_propertyTable.GetValue("BestBooleanPropertyB", SettingsGroup.BestSettings, true);
			Assert.IsFalse(ulpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			ulpba = m_propertyTable.GetValue("BestBooleanPropertyB", true);
			Assert.IsFalse(ulpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));

			int ulpia = m_propertyTable.GetValue("BestIntegerPropertyB", SettingsGroup.BestSettings, 586);
			Assert.AreEqual(-586, ulpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			ulpia = m_propertyTable.GetValue("BestIntegerPropertyB", 586);
			Assert.AreEqual(-586, ulpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));

			string ulpsa = m_propertyTable.GetValue("BestStringPropertyB", SettingsGroup.BestSettings, "global_BestStringPropertyC_value");
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			ulpsa = m_propertyTable.GetValue("BestStringPropertyB", "global_BestStringPropertyC_value");
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));

			// Make new best (global) properties
			ugpba = m_propertyTable.GetValue("BestBooleanPropertyC", false);
			Assert.IsFalse(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			ugpia = m_propertyTable.GetValue("BestIntegerPropertyC", -818);
			Assert.AreEqual(-818, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			ugpsa = m_propertyTable.GetValue("BestStringPropertyC", "global_BestStringPropertyC_value");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
		}

		/// <summary>
		/// Test the various versions of SetProperty.
		/// </summary>
		[Test]
		public void SetProperty()
		{
			// Change existing Global & Local values, check that they don't overwrite each other.
			m_propertyTable.SetProperty("BooleanPropertyA", false, SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty("BooleanPropertyA", true, SettingsGroup.GlobalSettings, true, false);
			bool gpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpba, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));
			bool lpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsFalse(lpba, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			m_propertyTable.SetProperty("BooleanPropertyA", false, SettingsGroup.GlobalSettings, true, false);
			m_propertyTable.SetProperty("BooleanPropertyA", true, SettingsGroup.LocalSettings, true, false);
			lpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));
			gpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));

			m_propertyTable.SetProperty("IntegerPropertyA", 253, SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty("IntegerPropertyA", -253, SettingsGroup.GlobalSettings, true, false);
			int gpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-253, gpia, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));
			int lpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(253, lpia, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			m_propertyTable.SetProperty("IntegerPropertyA", 253, SettingsGroup.GlobalSettings, true, false);
			m_propertyTable.SetProperty("IntegerPropertyA", -253, SettingsGroup.LocalSettings, true, false);
			lpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(-253, lpia, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));
			gpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));

			m_propertyTable.SetProperty("StringPropertyA", "local_StringPropertyC_value", SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty("StringPropertyA", "global_StringPropertyC_value", SettingsGroup.GlobalSettings, true, false);
			string gpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyC_value", gpsa, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));
			string lpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsa, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			m_propertyTable.SetProperty("StringPropertyA", "global_StringPropertyA_value", SettingsGroup.GlobalSettings, true, false);
			m_propertyTable.SetProperty("StringPropertyA", "local_StringPropertyA_value", SettingsGroup.LocalSettings, true, false);
			lpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));
			gpsa = m_propertyTable.GetValue("StringPropertyA", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));

			// Make new properties. ------------------
			//---- Global Settings
			m_propertyTable.SetProperty("BooleanPropertyC", true, SettingsGroup.GlobalSettings, true, false);
			bool gpbc = m_propertyTable.GetValue("BooleanPropertyC", SettingsGroup.GlobalSettings, false);
			Assert.IsTrue(gpbc, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyC"));

			m_propertyTable.SetProperty("IntegerPropertyC", 352, SettingsGroup.GlobalSettings, true, false);
			int gpic = m_propertyTable.GetValue("IntegerPropertyC", SettingsGroup.GlobalSettings, -352);
			Assert.AreEqual(352, gpic, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyC"));

			m_propertyTable.SetProperty("StringPropertyC", "global_StringPropertyC_value", SettingsGroup.GlobalSettings, true, false);
			string gpsc = m_propertyTable.GetValue("StringPropertyC", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyC_value", gpsc, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyC"));

			//---- Local Settings
			m_propertyTable.SetProperty("BooleanPropertyC", false, SettingsGroup.LocalSettings, true, false);
			bool lpbc = m_propertyTable.GetValue("BooleanPropertyC", SettingsGroup.LocalSettings, true);
			Assert.IsFalse(lpbc, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyC"));

			m_propertyTable.SetProperty("IntegerPropertyC", 111, SettingsGroup.LocalSettings, true, false);
			int lpic = m_propertyTable.GetValue("IntegerPropertyC", SettingsGroup.LocalSettings, -111);
			Assert.AreEqual(111, lpic, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyC"));

			m_propertyTable.SetProperty("StringPropertyC", "local_StringPropertyC_value", SettingsGroup.LocalSettings, true, false);
			string lpsc = m_propertyTable.GetValue("StringPropertyC", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyC_value", lpsc, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyC"));

			// Set best property on locals common with globals first.
			m_propertyTable.SetProperty("BooleanPropertyA", true, SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty("BooleanPropertyA", true, SettingsGroup.GlobalSettings, true, false);
			m_propertyTable.SetProperty("BooleanPropertyA", false, SettingsGroup.BestSettings, true, false);
			bool bpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsFalse(bpba, string.Format("Invalid value for {0} {1}.", "best", "BooleanPropertyA"));
			gpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpba, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));
			lpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsFalse(lpba, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			m_propertyTable.SetProperty("IntegerPropertyA", 253, SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty("IntegerPropertyA", -253, SettingsGroup.GlobalSettings, true, false);
			m_propertyTable.SetProperty("IntegerPropertyA", 352, SettingsGroup.BestSettings, true, false);
			int bpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual(352, bpia, string.Format("Invalid value for {0} {1}.", "best", "IntegerPropertyA"));
			gpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-253, gpia, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));
			lpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(352, lpia, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			m_propertyTable.SetProperty("StringPropertyA", "local_StringPropertyA_value", SettingsGroup.LocalSettings, true, false);
			m_propertyTable.SetProperty("StringPropertyA", "global_StringPropertyA_value", SettingsGroup.GlobalSettings, true, false);
			m_propertyTable.SetProperty("StringPropertyA", "best_StringPropertyA_value", SettingsGroup.BestSettings, true, false);
			string bpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual("best_StringPropertyA_value", bpsa, string.Format("Invalid value for {0} {1}.", "best", "StringPropertyA"));
			gpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));
			lpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("best_StringPropertyA_value", lpsa, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			object nullObject = null;

			// Set best setting on unique globals.
			m_propertyTable.SetProperty("BestBooleanPropertyA", false, SettingsGroup.BestSettings, true, false);
			bool ubpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsFalse(ubpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			bool ugpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			nullObject = m_propertyTable.GetValue<object>("BestBooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));

			m_propertyTable.SetProperty("BestIntegerPropertyA", 101, SettingsGroup.BestSettings, true, false);
			var ubpia = m_propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual(101, ubpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			var ugpia = m_propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(101, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			nullObject = m_propertyTable.GetValue<object>("BestIntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));

			m_propertyTable.SetProperty("BestStringPropertyA", "best_BestStringPropertyA_value", SettingsGroup.BestSettings, true, false);
			var ubpsa = m_propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual("best_BestStringPropertyA_value", ubpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			var ugpsa = m_propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("best_BestStringPropertyA_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			nullObject = m_propertyTable.GetValue<object>("BestStringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));

			// Set best setting on unique locals
			m_propertyTable.SetProperty("BestBooleanPropertyB", true, SettingsGroup.BestSettings, true, false);
			ubpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyB", SettingsGroup.BestSettings);
			Assert.IsTrue(ubpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			Assert.IsTrue(ubpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			nullObject = m_propertyTable.GetValue<object>("BestBooleanPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));

			m_propertyTable.SetProperty("BestIntegerPropertyB", 586, SettingsGroup.BestSettings, true, false);
			ubpia = m_propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual(586, ubpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			int ulpia = m_propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual(586, ulpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			nullObject = m_propertyTable.GetValue<object>("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));

			m_propertyTable.SetProperty("BestStringPropertyB", "best_BestStringPropertyB_value", SettingsGroup.BestSettings, true, false);
			ubpsa = m_propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual("best_BestStringPropertyB_value", ubpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			var ulpsa = m_propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual("best_BestStringPropertyB_value", ulpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			nullObject = m_propertyTable.GetValue<object>("BestStringPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));

			// Make new best (global) properties
			m_propertyTable.SetProperty("BestBooleanPropertyC", false, true, false);
			ugpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyC");
			Assert.IsFalse(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			ugpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyC", SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));

			m_propertyTable.SetProperty("BestIntegerPropertyC", -818, true, false);
			ugpia = m_propertyTable.GetValue<int>("BestIntegerPropertyC");
			Assert.AreEqual(-818, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			ugpia = m_propertyTable.GetValue<int>("BestIntegerPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-818, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));

			m_propertyTable.SetProperty("BestStringPropertyC", "global_BestStringPropertyC_value".Clone(), true, false);
			ugpsa = m_propertyTable.GetValue<string>("BestStringPropertyC");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
			ugpsa = m_propertyTable.GetValue<string>("BestStringPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyC"));
		}

		/// <summary>
		/// Test the various versions of SetDefault.
		/// </summary>
		[Test]
		public void SetDefault()
		{
			// Try changing existing Global & Local values, check that they don't overwrite existing ones.
			m_propertyTable.SetDefault("BooleanPropertyA", false, SettingsGroup.LocalSettings, false, false);
			m_propertyTable.SetDefault("BooleanPropertyA", true, SettingsGroup.GlobalSettings, false, false);
			var gpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyA"));
			var lpba = m_propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyA"));

			m_propertyTable.SetDefault("IntegerPropertyA", 253, SettingsGroup.LocalSettings, false, false);
			m_propertyTable.SetDefault("IntegerPropertyA", -253, SettingsGroup.GlobalSettings, false, false);
			var gpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));
			var lpia = m_propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyA"));

			m_propertyTable.SetDefault("StringPropertyA", "local_StringPropertyC_value", SettingsGroup.LocalSettings, false, false);
			m_propertyTable.SetDefault("StringPropertyA", "global_StringPropertyC_value", SettingsGroup.GlobalSettings, false, false);
			var gpsa = m_propertyTable.GetValue("StringPropertyA", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyA"));
			var lpsa = m_propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyA"));

			// Make new properties. ------------------
			//---- Global Settings
			m_propertyTable.SetDefault("BooleanPropertyC", true, SettingsGroup.GlobalSettings, true, false);
			var gpbc = m_propertyTable.GetValue("BooleanPropertyC", SettingsGroup.GlobalSettings, false);
			Assert.IsTrue(gpbc, string.Format("Invalid value for {0} {1}.", "global", "BooleanPropertyC"));

			m_propertyTable.SetDefault("IntegerPropertyC", 352, SettingsGroup.GlobalSettings, true, false);
			var gpic = m_propertyTable.GetValue("IntegerPropertyC", SettingsGroup.GlobalSettings, -352);
			Assert.AreEqual(352, gpic, string.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyC"));

			m_propertyTable.SetDefault("StringPropertyC", "global_StringPropertyC_value", SettingsGroup.GlobalSettings, true, false);
			var gpsc = m_propertyTable.GetValue("StringPropertyC", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyC_value", gpsc, string.Format("Invalid value for {0} {1}.", "global", "StringPropertyC"));

			//---- Local Settings
			m_propertyTable.SetDefault("BooleanPropertyC", false, SettingsGroup.LocalSettings, false, false);
			var lpbc = m_propertyTable.GetValue("BooleanPropertyC", SettingsGroup.LocalSettings, true);
			Assert.IsFalse(lpbc, string.Format("Invalid value for {0} {1}.", "local", "BooleanPropertyC"));

			m_propertyTable.SetDefault("IntegerPropertyC", 111, SettingsGroup.LocalSettings, false, false);
			var lpic = m_propertyTable.GetValue("IntegerPropertyC", SettingsGroup.LocalSettings, -111);
			Assert.AreEqual(111, lpic, string.Format("Invalid value for {0} {1}.", "local", "IntegerPropertyC"));

			m_propertyTable.SetDefault("StringPropertyC", "local_StringPropertyC_value", SettingsGroup.LocalSettings, false, false);
			var lpsc = m_propertyTable.GetValue("StringPropertyC", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyC_value", lpsc, string.Format("Invalid value for {0} {1}.", "local", "StringPropertyC"));

			object nullObject;
			// Set best setting on unique globals.
			m_propertyTable.SetDefault("BestBooleanPropertyA", false, SettingsGroup.BestSettings, false, false);
			var ubpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsTrue(ubpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			var ugpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));
			nullObject = m_propertyTable.GetValue<object>("BestBooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyA"));

			m_propertyTable.SetDefault("BestIntegerPropertyA", 101, SettingsGroup.BestSettings, false, false);
			var ubpia = m_propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual(-101, ubpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			var ugpia = m_propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-101, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));
			nullObject = m_propertyTable.GetValue<object>("BestIntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyA"));

			m_propertyTable.SetDefault("BestStringPropertyA", "best_BestStringPropertyA_value", SettingsGroup.BestSettings, false, false);
			var ubpsa = m_propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ubpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			var ugpsa = m_propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));
			nullObject = m_propertyTable.GetValue<object>("BestStringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyA"));

			// Set best setting on unique locals
			m_propertyTable.SetDefault("BestBooleanPropertyB", true, SettingsGroup.BestSettings, false, false);
			ubpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyB", SettingsGroup.BestSettings);
			Assert.IsFalse(ubpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			Assert.IsFalse(ubpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));
			nullObject = m_propertyTable.GetValue<object>("BestBooleanPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyB"));

			m_propertyTable.SetDefault("BestIntegerPropertyB", 586, SettingsGroup.BestSettings, false, false);
			ubpia = m_propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual(-586, ubpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			var ulpia = m_propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual(-586, ulpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));
			nullObject = m_propertyTable.GetValue<object>("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyB"));

			m_propertyTable.SetDefault("BestStringPropertyB", "best_BestStringPropertyB_value", SettingsGroup.BestSettings, false, false);
			ubpsa = m_propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ubpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			var ulpsa = m_propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));
			nullObject = m_propertyTable.GetValue<object>("BestStringPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "best", "BestStringPropertyB"));

			// Make new best (global) properties
			m_propertyTable.SetDefault("BestBooleanPropertyC", false, SettingsGroup.GlobalSettings, false, false);
			ugpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyC");
			Assert.IsFalse(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			ugpba = m_propertyTable.GetValue<bool>("BestBooleanPropertyC", SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, string.Format("Invalid value for {0} {1}.", "best", "BestBooleanPropertyC"));
			nullObject = m_propertyTable.GetValue<object>("BestBooleanPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "local", "BestBooleanPropertyC"));

			m_propertyTable.SetDefault("BestIntegerPropertyC", -818, SettingsGroup.GlobalSettings, false, false);
			ugpia = m_propertyTable.GetValue<int>("BestIntegerPropertyC");
			Assert.AreEqual(-818, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			ugpia = m_propertyTable.GetValue<int>("BestIntegerPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-818, ugpia, string.Format("Invalid value for {0} {1}.", "best", "BestIntegerPropertyC"));
			nullObject = m_propertyTable.GetValue<object>("BestIntegerPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, string.Format("Invalid value for {0} {1}.", "local", "BestIntegerPropertyC"));

			m_propertyTable.SetDefault("BestStringPropertyC", "global_BestStringPropertyC_value", SettingsGroup.GlobalSettings, false, false);
			ugpsa = m_propertyTable.GetValue<string>("BestStringPropertyC");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, "Invalid value for best BestStringPropertyC.");
			ugpsa = m_propertyTable.GetValue<string>("BestStringPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, "Invalid value for best BestStringPropertyC.");
			nullObject = m_propertyTable.GetValue<object>("BestStringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local BestStringPropertyA.");
		}

		/// <summary />
		[Test]
		public void ReadOnlyPropertyTable_GetWithDefaultDoesNotSet()
		{
			const string noSuchPropName = "No Such Property";
			const string myDefault = "MyDefault";
			const string notDefault = "NotDefault";
			IReadonlyPropertyTable roPropTable = new ReadOnlyPropertyTable(m_propertyTable);
			// Initial conditions
			Assert.IsNull(m_propertyTable.GetValue<string>(noSuchPropName));
			var getResult = roPropTable.GetValue(noSuchPropName, myDefault);
			Assert.IsNull(m_propertyTable.GetValue<string>(noSuchPropName), "Default should not have been set in the property table.");
			Assert.AreEqual(myDefault, getResult, "Default value not returned.");
			m_propertyTable.SetProperty(noSuchPropName, notDefault, SettingsGroup.GlobalSettings, false, false);
			Assert.AreEqual(roPropTable.GetValue(noSuchPropName, myDefault), notDefault, "Default was used instead of value from property table.");
		}
	}
}
