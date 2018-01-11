// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorerTests
{
	/// <summary>
	/// PropertyTable tests.
	/// </summary>
	[TestFixture]
	public class PropertyTableTests
	{
		private IPublisher _publisher;
		private IPropertyTable _propertyTable;
		string _originalSettingsPath;

		/// <summary>
		/// Get a temporary path. We add the username for machines where multiple users run
		/// tests (e.g. build machine).
		/// </summary>
		private static string TempPath => Path.Combine(Path.GetTempPath(), Environment.UserName);

		/// <summary>
		/// Set-up for this test fixture involves creating some temporary
		/// settings files. These will be cleaned up in the fixture teardown.
		/// </summary>
		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			// load a persisted version of the property table.
			_originalSettingsPath = Path.Combine(TempPath, "SettingsBackup");
			if (!Directory.Exists(_originalSettingsPath))
				Directory.CreateDirectory(_originalSettingsPath);

			File.WriteAllText(Path.Combine(_originalSettingsPath, "db$TestLocal$Settings.xml"), LanguageExplorerTestsResources.db_TestLocal_Settings_xml);
			File.WriteAllText(Path.Combine(_originalSettingsPath, "Settings.xml"), LanguageExplorerTestsResources.Settings_xml);
		}

		/// <summary />
		[SetUp]
		public void SetUp()
		{
			TestSetupServices.SetupTestPubSubSystem(out _publisher);
			_propertyTable = TestSetupServices.SetupTestPropertyTable(_publisher);
			_propertyTable.LocalSettingsId = "TestLocal";
			_propertyTable.UserSettingDirectory = _originalSettingsPath;

			LoadOriginalSettings();
		}

		/// <summary />
		[TearDown]
		public void TearDown()
		{
			_propertyTable.Dispose();
			_propertyTable = null;
			_publisher = null;
		}

		/// <summary>
		/// Needed to remove temporary settings folder.
		/// </summary>
		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			if (Directory.Exists(_originalSettingsPath))
			{
				Directory.Delete(_originalSettingsPath, true);
			}
		}

		private void LoadOriginalSettings(string settingsId)
		{
			_propertyTable.UserSettingDirectory = _originalSettingsPath;
			_propertyTable.RestoreFromFile(settingsId);
		}

		private void LoadOriginalSettings()
		{
			LoadOriginalSettings(_propertyTable.LocalSettingsId);
			LoadOriginalSettings(_propertyTable.GlobalSettingsId);
		}

		/// <summary>
		/// Test the various versions of TryGetValue.
		/// </summary>
		[Test]
		public void TryGetValueTest()
		{
			object bestValue;

			// Test nonexistent properties.
			var fPropertyExists = _propertyTable.TryGetValue("NonexistentPropertyA", out bestValue);
			Assert.IsFalse(fPropertyExists, "best NonexistentPropertyA should not exist.");
			Assert.IsNull(bestValue, "Invalid value for best NonexistentPropertyA.");

			// Test global property values.
			bool gpba;
			fPropertyExists = _propertyTable.TryGetValue("BooleanPropertyA", SettingsGroup.GlobalSettings, out gpba);
			Assert.IsTrue(fPropertyExists, "global BooleanPropertyA not found.");
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");

			int gpia;
			fPropertyExists = _propertyTable.TryGetValue("IntegerPropertyA", SettingsGroup.GlobalSettings, out gpia);
			Assert.IsTrue(fPropertyExists, "global IntegerPropertyA not found.");
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");

			string gpsa;
			fPropertyExists = _propertyTable.TryGetValue("StringPropertyA", SettingsGroup.GlobalSettings, out gpsa);
			Assert.IsTrue(fPropertyExists, "global StringPropertyA not found.");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");

			// Test local property values
			bool lpba;
			fPropertyExists = _propertyTable.TryGetValue("BooleanPropertyA", SettingsGroup.LocalSettings, out lpba);
			Assert.IsTrue(fPropertyExists, "local BooleanPropertyA not found.");
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");

			int lpia;
			fPropertyExists = _propertyTable.TryGetValue("IntegerPropertyA", SettingsGroup.LocalSettings, out lpia);
			Assert.IsTrue(fPropertyExists, "local IntegerPropertyA not found.");
			Assert.AreEqual(333, lpia, "Invalid value for local IntegerPropertyA.");

			string lpsa;
			fPropertyExists = _propertyTable.TryGetValue("StringPropertyA", SettingsGroup.LocalSettings, out lpsa);
			Assert.IsTrue(fPropertyExists, "local StringPropertyA not found.");
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");

			// Test best settings
			// Match on unique globals.
			bool ugpba;
			fPropertyExists = _propertyTable.TryGetValue("BestBooleanPropertyA", SettingsGroup.BestSettings, out ugpba);
			Assert.IsTrue(fPropertyExists, "best BestBooleanPropertyA not found.");
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			fPropertyExists = _propertyTable.TryGetValue("BestBooleanPropertyA", out ugpba);
			Assert.IsTrue(fPropertyExists, "best BestBooleanPropertyA not found.");
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");

			// Match on unique locals
			int ulpia;
			fPropertyExists = _propertyTable.TryGetValue("BestIntegerPropertyB", SettingsGroup.BestSettings, out ulpia);
			Assert.IsTrue(fPropertyExists, "best BestIntegerPropertyB not found.");
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			fPropertyExists = _propertyTable.TryGetValue("BestIntegerPropertyB", out ulpia);
			Assert.IsTrue(fPropertyExists, "best BestIntegerPropertyB not found.");
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");

			// Match best locals common with global properties
			bool bpba;
			fPropertyExists = _propertyTable.TryGetValue("BooleanPropertyA", SettingsGroup.BestSettings, out bpba);
			Assert.IsTrue(fPropertyExists, "best BooleanPropertyA not found.");
			Assert.IsTrue(bpba, "Invalid value for best BooleanPropertyA.");
			fPropertyExists = _propertyTable.TryGetValue("BooleanPropertyA", out bpba);
			Assert.IsTrue(fPropertyExists, "best BooleanPropertyA not found.");
			Assert.IsTrue(bpba, "Invalid value for best BooleanPropertyA.");
		}

		/// <summary>
		/// Test the various versions of PropertyExists.
		/// </summary>
		[Test]
		public void PropertyExists()
		{
			// Test nonexistent properties.
			var fPropertyExists = _propertyTable.PropertyExists("NonexistentPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(fPropertyExists, "global NonexistentPropertyA should not exist.");
			fPropertyExists = _propertyTable.PropertyExists("NonexistentPropertyA", SettingsGroup.LocalSettings);
			Assert.IsFalse(fPropertyExists, "local NonexistentPropertyA should not exist.");
			fPropertyExists = _propertyTable.PropertyExists("NonexistentPropertyA");
			Assert.IsFalse(fPropertyExists, "best NonexistentPropertyA should not exist.");

			// Test global property values.
			bool gpba;
			fPropertyExists = _propertyTable.PropertyExists("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, "global BooleanPropertyA not found.");

			int gpia;
			fPropertyExists = _propertyTable.PropertyExists("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, "global IntegerPropertyA not found.");

			string gpsa;
			fPropertyExists = _propertyTable.PropertyExists("StringPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, "global StringPropertyA not found.");

			// Test local property values
			bool lpba;
			fPropertyExists = _propertyTable.PropertyExists("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, "local BooleanPropertyA not found.");

			int lpia;
			fPropertyExists = _propertyTable.PropertyExists("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, "local IntegerPropertyA not found.");

			string lpsa;
			fPropertyExists = _propertyTable.PropertyExists("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, "local StringPropertyA not found.");

			// Test best settings
			// Match on unique globals.
			bool ugpba;
			fPropertyExists = _propertyTable.PropertyExists("BestBooleanPropertyA");
			Assert.IsTrue(fPropertyExists, "best BestBooleanPropertyA not found.");
			fPropertyExists = _propertyTable.PropertyExists("BestBooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, "best BestBooleanPropertyA not found.");

			// Match on unique locals
			int ulpia;
			fPropertyExists = _propertyTable.PropertyExists("BestIntegerPropertyB");
			Assert.IsTrue(fPropertyExists, "best BestIntegerPropertyB not found.");
			fPropertyExists = _propertyTable.PropertyExists("BestIntegerPropertyB", SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, "best BestIntegerPropertyB not found.");
			fPropertyExists = _propertyTable.PropertyExists("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, "best BestIntegerPropertyB not found.");
			fPropertyExists = _propertyTable.PropertyExists("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsFalse(fPropertyExists, "best BestIntegerPropertyB not found.");

			// Match best locals common with global properties
			bool bpba;
			fPropertyExists = _propertyTable.PropertyExists("BooleanPropertyA");
			Assert.IsTrue(fPropertyExists, "best BooleanPropertyA not found.");
			fPropertyExists = _propertyTable.PropertyExists("BooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsTrue(fPropertyExists, "best BooleanPropertyA not found.");
		}

		/// <summary>
		/// Test the various versions of GetValue.
		/// </summary>
		[Test]
		public void GetValue()
		{
			// Test nonexistent values.
			var bestValue = _propertyTable.GetValue<object>("NonexistentPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsNull(bestValue, "Invalid value for global NonexistentPropertyA.");
			bestValue = _propertyTable.GetValue<object>("NonexistentPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(bestValue, "Invalid value for local NonexistentPropertyA.");
			bestValue = _propertyTable.GetValue<object>("NonexistentPropertyA", SettingsGroup.BestSettings);
			Assert.IsNull(bestValue, "Invalid value for best NonexistentPropertyA.");
			bestValue = _propertyTable.GetValue<object>("NonexistentPropertyA");
			Assert.IsNull(bestValue, "Invalid value for best NonexistentPropertyA.");

			// Test global property values.
			bool gpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");
			gpba = _propertyTable.GetValue("BooleanPropertyA", SettingsGroup.GlobalSettings, true);
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");

			int gpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");
			gpia = _propertyTable.GetValue("IntegerPropertyA", SettingsGroup.GlobalSettings, 352);
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");

			string gpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");
			gpsa = _propertyTable.GetValue("StringPropertyA", SettingsGroup.GlobalSettings, "global_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");

			// Test locals property values.
			bool lpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");
			lpba = _propertyTable.GetValue("BooleanPropertyA", SettingsGroup.LocalSettings, false);
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");

			int lpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, "Invalid value for local IntegerPropertyA.");
			lpia = _propertyTable.GetValue("IntegerPropertyA", SettingsGroup.LocalSettings, 111);
			Assert.AreEqual(333, lpia, "Invalid value for local IntegerPropertyA.");

			string lpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");
			lpsa = _propertyTable.GetValue("StringPropertyA", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");

			// Make new properties.
			// --- Set Globals and make sure Locals are still null.
			bool gpbc = _propertyTable.GetValue("BooleanPropertyC", SettingsGroup.GlobalSettings, true);
			Assert.IsTrue(gpbc, "Invalid value for global BooleanPropertyC.");
			var nullObject = _propertyTable.GetValue<object>("BooleanPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local BooleanPropertyC.");

			int gpic = _propertyTable.GetValue("IntegerPropertyC", SettingsGroup.GlobalSettings, 352);
			Assert.AreEqual(352, gpic, "Invalid value for global IntegerPropertyC.");
			nullObject = _propertyTable.GetValue<object>("IntegerPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local IntegerPropertyC.");

			string gpsc = _propertyTable.GetValue("StringPropertyC", SettingsGroup.GlobalSettings, "global_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyC_value", gpsc, "Invalid value for global StringPropertyC.");
			nullObject = _propertyTable.GetValue<object>("StringPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local StringPropertyC.");

			// -- Set Locals and make sure Globals haven't changed.
			bool lpbc = _propertyTable.GetValue("BooleanPropertyC", SettingsGroup.LocalSettings, false);
			Assert.IsFalse(lpbc, "Invalid value for local BooleanPropertyC.");
			gpbc = _propertyTable.GetValue<bool>("BooleanPropertyC", SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, "Invalid value for global BooleanPropertyC.");

			int lpic = _propertyTable.GetValue("IntegerPropertyC", SettingsGroup.LocalSettings, 111);
			Assert.AreEqual(111, lpic, "Invalid value for local IntegerPropertyC.");
			gpic = _propertyTable.GetValue<int>("IntegerPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, "Invalid value for global IntegerPropertyC.");

			string lpsc = _propertyTable.GetValue("StringPropertyC", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyC_value", lpsc, "Invalid value for local StringPropertyC.");
			gpsc = _propertyTable.GetValue<string>("StringPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, "Invalid value for global StringPropertyC.");

			// Test best property values;
			// Match on locals common with globals first.
			bool bpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsTrue(bpba, "Invalid value for best BooleanPropertyA.");
			bpba = _propertyTable.GetValue<bool>("BooleanPropertyA");
			Assert.IsTrue(bpba, "Invalid value for best BooleanPropertyA.");
			bpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(bpba, "Invalid value for local BooleanPropertyA.");
			bpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(bpba, "Invalid value for global BooleanPropertyA.");

			int bpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual(333, bpia, "Invalid value for best IntegerPropertyA.");
			bpia = _propertyTable.GetValue<int>("IntegerPropertyA");
			Assert.AreEqual(333, bpia, "Invalid value for best IntegerPropertyA.");
			bpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(333, bpia, "Invalid value for local IntegerPropertyA.");
			bpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, bpia, "Invalid value for global IntegerPropertyA.");

			string bpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual("local_StringPropertyA_value", bpsa, "Invalid value for best StringPropertyA.");
			bpsa = _propertyTable.GetValue<string>("StringPropertyA");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, "Invalid value for best StringPropertyA.");
			bpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", bpsa, "Invalid value for local StringPropertyA.");
			bpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", bpsa, "Invalid value for global StringPropertyA.");

			// Match on unique globals.
			bool ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsTrue(ubpba, "Invalid value for best BestBooleanPropertyA.");
			bool ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA");
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			ugpba = _propertyTable.GetValue("BestBooleanPropertyA", false);
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyA.");

			int ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual(-101, ubpia, "Invalid value for best BestIntegerPropertyA.");
			int ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyA");
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			ugpia = _propertyTable.GetValue("BestIntegerPropertyA", -818);
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyA.");

			string ubpsa = _propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ubpsa, "Invalid value for best BestStringPropertyA.");
			string ugpsa = _propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			ugpsa = _propertyTable.GetValue<string>("BestStringPropertyA");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			ugpsa = _propertyTable.GetValue("BestStringPropertyA", "global_BestStringPropertyC_value");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestStringPropertyA.");

			// Match on unique locals.
			ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyB", SettingsGroup.BestSettings);
			Assert.IsFalse(ubpba, "Invalid value for best BestBooleanPropertyB.");
			bool ulpba = _propertyTable.GetValue<bool>("BestBooleanPropertyB", SettingsGroup.LocalSettings);
			Assert.IsFalse(ulpba, "Invalid value for best BestBooleanPropertyB.");
			ulpba = _propertyTable.GetValue<bool>("BestBooleanPropertyB");
			Assert.IsFalse(ulpba, "Invalid value for best BestBooleanPropertyB.");
			ulpba = _propertyTable.GetValue("BestBooleanPropertyB", true);
			Assert.IsFalse(ulpba, "Invalid value for best BestBooleanPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyB.");

			ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual(-586, ubpia, "Invalid value for best BestIntegerPropertyB.");
			int ulpia = _propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			ulpia = _propertyTable.GetValue<int>("BestIntegerPropertyB");
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			ulpia = _propertyTable.GetValue("BestIntegerPropertyB", -685);
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyB.");

			ubpsa = _propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ubpsa, "Invalid value for best BestStringPropertyB.");
			string ulpsa = _propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, "Invalid value for best BestStringPropertyB.");
			ulpsa = _propertyTable.GetValue<string>("BestStringPropertyB");
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, "Invalid value for best BestStringPropertyB.");
			ulpsa = _propertyTable.GetValue("BestStringPropertyB", "local_BestStringPropertyC_value");
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, "Invalid value for best BestStringPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestStringPropertyB.");

			// Make new best (global) properties
			ugpba = _propertyTable.GetValue("BestBooleanPropertyC", false);
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyC.");
			ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyC", SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyC.");
			ugpia = _propertyTable.GetValue("BestIntegerPropertyC", -818);
			Assert.AreEqual(-818, ugpia, "Invalid value for best BestIntegerPropertyC.");
			ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-818, ugpia, "Invalid value for best BestIntegerPropertyC.");
			ugpsa = _propertyTable.GetValue("BestStringPropertyC", "global_BestStringPropertyC_value");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, "Invalid value for best BestStringPropertyC.");
			ugpsa = _propertyTable.GetValue<string>("BestStringPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, "Invalid value for best BestStringPropertyC.");
		}


		/// <summary>
		/// Test the various versions of GetProperty.
		/// </summary>
		[Test]
		public void Get_X_Property()
		{
			// Test global property values.
			bool gpba = _propertyTable.GetValue("BooleanPropertyA", SettingsGroup.GlobalSettings, true);
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");

			int gpia = _propertyTable.GetValue("IntegerPropertyA", SettingsGroup.GlobalSettings, 352);
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");

			string gpsa = _propertyTable.GetValue("StringPropertyA", SettingsGroup.GlobalSettings, "global_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");

			// Test locals property values.
			bool lpba = _propertyTable.GetValue("BooleanPropertyA", SettingsGroup.LocalSettings, false);
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");

			int lpia = _propertyTable.GetValue("IntegerPropertyA", SettingsGroup.LocalSettings, 111);
			Assert.AreEqual(333, lpia, "Invalid value for local IntegerPropertyA.");

			string lpsa = _propertyTable.GetValue("StringPropertyA", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");

			// Make new properties.
			bool gpbc = _propertyTable.GetValue("BooleanPropertyC", SettingsGroup.GlobalSettings, true);
			Assert.IsTrue(gpbc, "Invalid value for global BooleanPropertyC.");
			int gpic = _propertyTable.GetValue("IntegerPropertyC", SettingsGroup.GlobalSettings, 352);
			Assert.AreEqual(352, gpic, "Invalid value for global IntegerPropertyC.");
			string gpsc = _propertyTable.GetValue("StringPropertyC", SettingsGroup.GlobalSettings, "global_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyC_value", gpsc, "Invalid value for global StringPropertyC.");

			bool lpbc = _propertyTable.GetValue("BooleanPropertyC", SettingsGroup.LocalSettings, false);
			Assert.IsFalse(lpbc, "Invalid value for local BooleanPropertyC.");
			int lpic = _propertyTable.GetValue("IntegerPropertyC", SettingsGroup.LocalSettings, 111);
			Assert.AreEqual(111, lpic, "Invalid value for local IntegerPropertyC.");
			string lpsc = _propertyTable.GetValue("StringPropertyC", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyC_value", lpsc, "Invalid value for local StringPropertyC.");

			// Test best property values;
			// Match on locals common with globals first.
			bool bpba = _propertyTable.GetValue("BooleanPropertyA", SettingsGroup.BestSettings, false);
			Assert.IsTrue(bpba, "Invalid value for best BooleanPropertyA.");
			bpba = _propertyTable.GetValue("BooleanPropertyA", false);
			Assert.IsTrue(bpba, "Invalid value for best BooleanPropertyA.");

			int bpia = _propertyTable.GetValue("IntegerPropertyA", SettingsGroup.BestSettings, -333);
			Assert.AreEqual(333, bpia, "Invalid value for best IntegerPropertyA.");
			bpia = _propertyTable.GetValue("IntegerPropertyA", -333);
			Assert.AreEqual(333, bpia, "Invalid value for best IntegerPropertyA.");

			string bpsa = _propertyTable.GetValue("StringPropertyA", SettingsGroup.BestSettings, "global_StringPropertyA_value");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, "Invalid value for best StringPropertyA.");
			bpsa = _propertyTable.GetValue("StringPropertyA", "global_StringPropertyA_value");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, "Invalid value for best StringPropertyA.");

			// Match on unique globals.
			bool ugpba = _propertyTable.GetValue("BestBooleanPropertyA", SettingsGroup.BestSettings, false);
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			ugpba = _propertyTable.GetValue("BestBooleanPropertyA", false);
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");

			int ugpia = _propertyTable.GetValue("BestIntegerPropertyA", SettingsGroup.BestSettings, 101);
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			ugpia = _propertyTable.GetValue("BestIntegerPropertyA", 101);
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");

			string ugpsa = _propertyTable.GetValue("BestStringPropertyA", SettingsGroup.BestSettings, "local_BestStringPropertyA_value");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			ugpsa = _propertyTable.GetValue("BestStringPropertyA", "local_BestStringPropertyA_value");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");

			// Match on unique locals.
			bool ulpba = _propertyTable.GetValue("BestBooleanPropertyB", SettingsGroup.BestSettings, true);
			Assert.IsFalse(ulpba, "Invalid value for best BestBooleanPropertyB.");
			ulpba = _propertyTable.GetValue("BestBooleanPropertyB", true);
			Assert.IsFalse(ulpba, "Invalid value for best BestBooleanPropertyB.");

			int ulpia = _propertyTable.GetValue("BestIntegerPropertyB", SettingsGroup.BestSettings, 586);
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			ulpia = _propertyTable.GetValue("BestIntegerPropertyB", 586);
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");

			string ulpsa = _propertyTable.GetValue("BestStringPropertyB", SettingsGroup.BestSettings, "global_BestStringPropertyC_value");
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, "Invalid value for best BestStringPropertyB.");
			ulpsa = _propertyTable.GetValue("BestStringPropertyB", "global_BestStringPropertyC_value");
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, "Invalid value for best BestStringPropertyB.");

			// Make new best (global) properties
			ugpba = _propertyTable.GetValue("BestBooleanPropertyC", false);
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyC.");
			ugpia = _propertyTable.GetValue("BestIntegerPropertyC", -818);
			Assert.AreEqual(-818, ugpia, "Invalid value for best BestIntegerPropertyC.");
			ugpsa = _propertyTable.GetValue("BestStringPropertyC", "global_BestStringPropertyC_value");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, "Invalid value for best BestStringPropertyC.");
		}

		/// <summary>
		/// Test the various versions of SetProperty.
		/// </summary>
		[Test]
		public void SetProperty()
		{
			// Change existing Global & Local values, check that they don't overwrite each other.
			_propertyTable.SetProperty("BooleanPropertyA", false, SettingsGroup.LocalSettings, true, false);
			_propertyTable.SetProperty("BooleanPropertyA", true, SettingsGroup.GlobalSettings, true, false);
			bool gpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpba, "Invalid value for global BooleanPropertyA.");
			bool lpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsFalse(lpba, "Invalid value for local BooleanPropertyA.");

			_propertyTable.SetProperty("BooleanPropertyA", false, SettingsGroup.GlobalSettings, true, false);
			_propertyTable.SetProperty("BooleanPropertyA", true, SettingsGroup.LocalSettings, true, false);
			lpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");
			gpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");

			_propertyTable.SetProperty("IntegerPropertyA", 253, SettingsGroup.LocalSettings, true, false);
			_propertyTable.SetProperty("IntegerPropertyA", -253, SettingsGroup.GlobalSettings, true, false);
			int gpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-253, gpia, "Invalid value for global IntegerPropertyA.");
			int lpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(253, lpia, "Invalid value for local IntegerPropertyA.");

			_propertyTable.SetProperty("IntegerPropertyA", 253, SettingsGroup.GlobalSettings, true, false);
			_propertyTable.SetProperty("IntegerPropertyA", -253, SettingsGroup.LocalSettings, true, false);
			lpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(-253, lpia, "Invalid value for local IntegerPropertyA.");
			gpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");

			_propertyTable.SetProperty("StringPropertyA", "local_StringPropertyC_value", SettingsGroup.LocalSettings, true, false);
			_propertyTable.SetProperty("StringPropertyA", "global_StringPropertyC_value", SettingsGroup.GlobalSettings, true, false);
			string gpsa = _propertyTable.GetValue("StringPropertyA", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyC_value", gpsa, "Invalid value for global StringPropertyA.");
			string lpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsa, "Invalid value for local StringPropertyA.");

			_propertyTable.SetProperty("StringPropertyA", "global_StringPropertyA_value", SettingsGroup.GlobalSettings, true, false);
			_propertyTable.SetProperty("StringPropertyA", "local_StringPropertyA_value", SettingsGroup.LocalSettings, true, false);
			lpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");
			gpsa = _propertyTable.GetValue("StringPropertyA", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");

			// Make new properties. ------------------
			//---- Global Settings
			_propertyTable.SetProperty("BooleanPropertyC", true, SettingsGroup.GlobalSettings, true, false);
			bool gpbc = _propertyTable.GetValue("BooleanPropertyC", SettingsGroup.GlobalSettings, false);
			Assert.IsTrue(gpbc, "Invalid value for global BooleanPropertyC.");

			_propertyTable.SetProperty("IntegerPropertyC", 352, SettingsGroup.GlobalSettings, true, false);
			int gpic = _propertyTable.GetValue("IntegerPropertyC", SettingsGroup.GlobalSettings, -352);
			Assert.AreEqual(352, gpic, "Invalid value for global IntegerPropertyC.");

			_propertyTable.SetProperty("StringPropertyC", "global_StringPropertyC_value", SettingsGroup.GlobalSettings, true, false);
			string gpsc = _propertyTable.GetValue("StringPropertyC", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyC_value", gpsc, "Invalid value for global StringPropertyC.");

			//---- Local Settings
			_propertyTable.SetProperty("BooleanPropertyC", false, SettingsGroup.LocalSettings, true, false);
			bool lpbc = _propertyTable.GetValue("BooleanPropertyC", SettingsGroup.LocalSettings, true);
			Assert.IsFalse(lpbc, "Invalid value for local BooleanPropertyC.");

			_propertyTable.SetProperty("IntegerPropertyC", 111, SettingsGroup.LocalSettings, true, false);
			int lpic = _propertyTable.GetValue("IntegerPropertyC", SettingsGroup.LocalSettings, -111);
			Assert.AreEqual(111, lpic, "Invalid value for local IntegerPropertyC.");

			_propertyTable.SetProperty("StringPropertyC", "local_StringPropertyC_value", SettingsGroup.LocalSettings, true, false);
			string lpsc = _propertyTable.GetValue("StringPropertyC", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyC_value", lpsc, "Invalid value for local StringPropertyC.");

			// Set best property on locals common with globals first.
			_propertyTable.SetProperty("BooleanPropertyA", true, SettingsGroup.LocalSettings, true, false);
			_propertyTable.SetProperty("BooleanPropertyA", true, SettingsGroup.GlobalSettings, true, false);
			_propertyTable.SetProperty("BooleanPropertyA", false, SettingsGroup.BestSettings, true, false);
			bool bpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsFalse(bpba, "Invalid value for best BooleanPropertyA.");
			gpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpba, "Invalid value for global BooleanPropertyA.");
			lpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsFalse(lpba, "Invalid value for local BooleanPropertyA.");

			_propertyTable.SetProperty("IntegerPropertyA", 253, SettingsGroup.LocalSettings, true, false);
			_propertyTable.SetProperty("IntegerPropertyA", -253, SettingsGroup.GlobalSettings, true, false);
			_propertyTable.SetProperty("IntegerPropertyA", 352, SettingsGroup.BestSettings, true, false);
			int bpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual(352, bpia, "Invalid value for best IntegerPropertyA.");
			gpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-253, gpia, "Invalid value for global IntegerPropertyA.");
			lpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(352, lpia, "Invalid value for local IntegerPropertyA.");

			_propertyTable.SetProperty("StringPropertyA", "local_StringPropertyA_value", SettingsGroup.LocalSettings, true, false);
			_propertyTable.SetProperty("StringPropertyA", "global_StringPropertyA_value", SettingsGroup.GlobalSettings, true, false);
			_propertyTable.SetProperty("StringPropertyA", "best_StringPropertyA_value", SettingsGroup.BestSettings, true, false);
			string bpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual("best_StringPropertyA_value", bpsa, "Invalid value for best StringPropertyA.");
			gpsa = _propertyTable.GetValue("StringPropertyA", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");
			lpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("best_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");

			// Set best setting on unique globals.
			_propertyTable.SetProperty("BestBooleanPropertyA", false, SettingsGroup.BestSettings, true, false);
			bool ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsFalse(ubpba, "Invalid value for best BestBooleanPropertyA.");
			bool ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyA.");
			var nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyA.");

			_propertyTable.SetProperty("BestIntegerPropertyA", 101, SettingsGroup.BestSettings, true, false);
			var ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual(101, ubpia, "Invalid value for best BestIntegerPropertyA.");
			var ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyA.");

			_propertyTable.SetProperty("BestStringPropertyA", "best_BestStringPropertyA_value", SettingsGroup.BestSettings, true, false);
			var ubpsa = _propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual("best_BestStringPropertyA_value", ubpsa, "Invalid value for best BestStringPropertyA.");
			var ugpsa = _propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("best_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestStringPropertyA.");

			// Set best setting on unique locals
			_propertyTable.SetProperty("BestBooleanPropertyB", true, SettingsGroup.BestSettings, true, false);
			ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyB", SettingsGroup.BestSettings);
			Assert.IsTrue(ubpba, "Invalid value for best BestBooleanPropertyB.");
			Assert.IsTrue(ubpba, "Invalid value for best BestBooleanPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyB.");

			_propertyTable.SetProperty("BestIntegerPropertyB", 586, SettingsGroup.BestSettings, true, false);
			ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual(586, ubpia, "Invalid value for best BestIntegerPropertyB.");
			int ulpia = _propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual(586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyB.");

			_propertyTable.SetProperty("BestStringPropertyB", "best_BestStringPropertyB_value", SettingsGroup.BestSettings, true, false);
			ubpsa = _propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual("best_BestStringPropertyB_value", ubpsa, "Invalid value for best BestStringPropertyB.");
			var ulpsa = _propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual("best_BestStringPropertyB_value", ulpsa, "Invalid value for best BestStringPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestStringPropertyB.");

			// Make new best (global) properties
			_propertyTable.SetProperty("BestBooleanPropertyC", false, true, false);
			ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyC");
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyC.");
			ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyC", SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyC.");

			_propertyTable.SetProperty("BestIntegerPropertyC", -818, true, false);
			ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyC");
			Assert.AreEqual(-818, ugpia, "Invalid value for best BestIntegerPropertyC.");
			ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-818, ugpia, "Invalid value for best BestIntegerPropertyC.");

			_propertyTable.SetProperty("BestStringPropertyC", "global_BestStringPropertyC_value".Clone(), true, false);
			ugpsa = _propertyTable.GetValue<string>("BestStringPropertyC");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, "Invalid value for best BestStringPropertyC.");
			ugpsa = _propertyTable.GetValue<string>("BestStringPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, "Invalid value for best BestStringPropertyC.");
		}

		/// <summary>
		/// Test the various versions of SetDefault.
		/// </summary>
		[Test]
		public void SetDefault()
		{
			// Try changing existing Global & Local values, check that they don't overwrite existing ones.
			_propertyTable.SetDefault("BooleanPropertyA", false, SettingsGroup.LocalSettings, false, false);
			_propertyTable.SetDefault("BooleanPropertyA", true, SettingsGroup.GlobalSettings, false, false);
			var gpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");
			var lpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");

			_propertyTable.SetDefault("IntegerPropertyA", 253, SettingsGroup.LocalSettings, false, false);
			_propertyTable.SetDefault("IntegerPropertyA", -253, SettingsGroup.GlobalSettings, false, false);
			var gpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");
			var lpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, "Invalid value for local IntegerPropertyA.");

			_propertyTable.SetDefault("StringPropertyA", "local_StringPropertyC_value", SettingsGroup.LocalSettings, false, false);
			_propertyTable.SetDefault("StringPropertyA", "global_StringPropertyC_value", SettingsGroup.GlobalSettings, false, false);
			var gpsa = _propertyTable.GetValue("StringPropertyA", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");
			var lpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");

			// Make new properties. ------------------
			//---- Global Settings
			_propertyTable.SetDefault("BooleanPropertyC", true, SettingsGroup.GlobalSettings, true, false);
			var gpbc = _propertyTable.GetValue("BooleanPropertyC", SettingsGroup.GlobalSettings, false);
			Assert.IsTrue(gpbc, "Invalid value for global BooleanPropertyC.");

			_propertyTable.SetDefault("IntegerPropertyC", 352, SettingsGroup.GlobalSettings, true, false);
			var gpic = _propertyTable.GetValue("IntegerPropertyC", SettingsGroup.GlobalSettings, -352);
			Assert.AreEqual(352, gpic, "Invalid value for global IntegerPropertyC.");

			_propertyTable.SetDefault("StringPropertyC", "global_StringPropertyC_value", SettingsGroup.GlobalSettings, true, false);
			var gpsc = _propertyTable.GetValue("StringPropertyC", SettingsGroup.GlobalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("global_StringPropertyC_value", gpsc, "Invalid value for global StringPropertyC.");

			//---- Local Settings
			_propertyTable.SetDefault("BooleanPropertyC", false, SettingsGroup.LocalSettings, false, false);
			var lpbc = _propertyTable.GetValue("BooleanPropertyC", SettingsGroup.LocalSettings, true);
			Assert.IsFalse(lpbc, "Invalid value for local BooleanPropertyC.");

			_propertyTable.SetDefault("IntegerPropertyC", 111, SettingsGroup.LocalSettings, false, false);
			var lpic = _propertyTable.GetValue("IntegerPropertyC", SettingsGroup.LocalSettings, -111);
			Assert.AreEqual(111, lpic, "Invalid value for local IntegerPropertyC.");

			_propertyTable.SetDefault("StringPropertyC", "local_StringPropertyC_value", SettingsGroup.LocalSettings, false, false);
			var lpsc = _propertyTable.GetValue("StringPropertyC", SettingsGroup.LocalSettings, "local_StringPropertyC_value");
			Assert.AreEqual("local_StringPropertyC_value", lpsc, "Invalid value for local StringPropertyC.");

			// Set best setting on unique globals.
			_propertyTable.SetDefault("BestBooleanPropertyA", false, SettingsGroup.BestSettings, false, false);
			var ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.BestSettings);
			Assert.IsTrue(ubpba, "Invalid value for best BestBooleanPropertyA.");
			var ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			var nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyA.");

			_propertyTable.SetDefault("BestIntegerPropertyA", 101, SettingsGroup.BestSettings, false, false);
			var ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual(-101, ubpia, "Invalid value for best BestIntegerPropertyA.");
			var ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyA.");

			_propertyTable.SetDefault("BestStringPropertyA", "best_BestStringPropertyA_value", SettingsGroup.BestSettings, false, false);
			var ubpsa = _propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.BestSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ubpsa, "Invalid value for best BestStringPropertyA.");
			var ugpsa = _propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestStringPropertyA.");

			// Set best setting on unique locals
			_propertyTable.SetDefault("BestBooleanPropertyB", true, SettingsGroup.BestSettings, false, false);
			ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyB", SettingsGroup.BestSettings);
			Assert.IsFalse(ubpba, "Invalid value for best BestBooleanPropertyB.");
			Assert.IsFalse(ubpba, "Invalid value for best BestBooleanPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyB.");

			_propertyTable.SetDefault("BestIntegerPropertyB", 586, SettingsGroup.BestSettings, false, false);
			ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual(-586, ubpia, "Invalid value for best BestIntegerPropertyB.");
			var ulpia = _propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyB.");

			_propertyTable.SetDefault("BestStringPropertyB", "best_BestStringPropertyB_value", SettingsGroup.BestSettings, false, false);
			ubpsa = _propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.BestSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ubpsa, "Invalid value for best BestStringPropertyB.");
			var ulpsa = _propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, "Invalid value for best BestStringPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestStringPropertyB.");

			// Make new best (global) properties
			_propertyTable.SetDefault("BestBooleanPropertyC", false, SettingsGroup.GlobalSettings, false, false);
			ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyC");
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyC.");
			ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyC", SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyC.");
			nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local BestBooleanPropertyC.");

			_propertyTable.SetDefault("BestIntegerPropertyC", -818, SettingsGroup.GlobalSettings, false, false);
			ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyC");
			Assert.AreEqual(-818, ugpia, "Invalid value for best BestIntegerPropertyC.");
			ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-818, ugpia, "Invalid value for best BestIntegerPropertyC.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local BestIntegerPropertyC.");

			_propertyTable.SetDefault("BestStringPropertyC", "global_BestStringPropertyC_value", SettingsGroup.GlobalSettings, false, false);
			ugpsa = _propertyTable.GetValue<string>("BestStringPropertyC");
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, "Invalid value for best BestStringPropertyC.");
			ugpsa = _propertyTable.GetValue<string>("BestStringPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyC_value", ugpsa, "Invalid value for best BestStringPropertyC.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local BestStringPropertyA.");
		}

		/// <summary />
		[Test]
		public void ReadOnlyPropertyTable_GetWithDefaultDoesNotSet()
		{
			const string noSuchPropName = "No Such Property";
			const string myDefault = "MyDefault";
			const string notDefault = "NotDefault";
			IReadonlyPropertyTable roPropTable = new ReadOnlyPropertyTable(_propertyTable);
			// Initial conditions
			Assert.IsNull(_propertyTable.GetValue<string>(noSuchPropName));
			var getResult = roPropTable.GetValue(noSuchPropName, myDefault);
			Assert.IsNull(_propertyTable.GetValue<string>(noSuchPropName), "Default should not have been set in the property table.");
			Assert.AreEqual(myDefault, getResult, "Default value not returned.");
			_propertyTable.SetProperty(noSuchPropName, notDefault, SettingsGroup.GlobalSettings, false, false);
			Assert.AreEqual(roPropTable.GetValue(noSuchPropName, myDefault), notDefault, "Default was used instead of value from property table.");
		}

		/// <summary>
		/// Make sure assimilated projects have xml updated
		/// </summary>
		[Test]
		public void UpdateAssimilatedProjects()
		{
			Assert.That(_propertyTable.GetValue("PropertyTableVersion", 0), Is.EqualTo(0));
			var lotsOfAssimilatedAssemblies = _propertyTable.GetValue<string>("LotsOfAssimilatedAssemblies");
			var element = XElement.Parse(lotsOfAssimilatedAssemblies);
			var before = new Dictionary<string, string>
			{
				{"xCore.dll",  "XCore.Inventory"},
				{"xCoreInterfaces.dll", "XCore.PropertyTable" },
				{"SilSidePane.dll", "SIL.SilSidePane.Banner" },
				{"FlexUIAdapter.dll", "XCore.PaneBar" },
				{"Discourse.dll", "SIL.FieldWorks.Discourse.AdvancedMTDialog" },
				{"ITextDll.dll", "SIL.FieldWorks.IText.ClosedFeatureValue" },
				{"LexEdDll.dll", "SIL.FieldWorks.XWorks.LexEd.CircularRefBreaker" },
				{"LexTextControls.dll", "SIL.FieldWorks.LexText.Controls.AddAllomorphDlg" },
				{"LexTextDll.dll", "SIL.FieldWorks.XWorks.LexText.RestoreDefaultsDlg" },
				{"MorphologyEditorDll.dll", "SIL.FieldWorks.XWorks.MorphologyEditor.AdhocCoProhibAtomicLauncher" },
				{"ParserUI.dll", "SIL.FieldWorks.LexText.Controls.ImportWordSetDlg" },
				{"FdoUi.dll", "SIL.FieldWorks.FdoUi.PosFilter" },
				{"DetailControls.dll", "SIL.FieldWorks.Common.Framework.DetailControls.AtomicReferenceLauncher" },
				{"XMLViews.dll", "SIL.FieldWorks.Common.Controls.BrowseViewer" }
			};
			foreach (var childElement in element.Elements())
			{
				var assemblyPath = childElement.Attribute("assemblyPath").Value;
				var classValue = childElement.Attribute("class").Value;
				Assert.That(before[assemblyPath], Is.EqualTo(classValue));
			}

			// SUT
			_propertyTable.ConvertOldPropertiesToNewIfPresent();
			Assert.That(_propertyTable.GetValue<int>("PropertyTableVersion"), Is.EqualTo(1));
			lotsOfAssimilatedAssemblies = _propertyTable.GetValue<string>("LotsOfAssimilatedAssemblies");
			element = XElement.Parse(lotsOfAssimilatedAssemblies);
			var after = new List<string>
			{
				"LanguageExplorer.Inventory",
				"LanguageExplorer.Impls.PropertyTable",
				"LanguageExplorer.Controls.SilSidePane.Banner",
				"LanguageExplorer.Controls.PaneBar.PaneBar",
				"LanguageExplorer.Areas.TextsAndWords.Discourse.AdvancedMTDialog",
				"LanguageExplorer.Areas.TextsAndWords.Interlinear.ClosedFeatureValue",
				"LanguageExplorer.Areas.Lexicon.CircularRefBreaker",
				"LanguageExplorer.Controls.LexText.AddAllomorphDlg",
				"LanguageExplorer.Impls.RestoreDefaultsDlg",
				"LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit.AdhocCoProhibAtomicLauncher",
				"LanguageExplorer.Areas.TextsAndWords.ImportWordSetDlg",
				"LanguageExplorer.LcmUi.PosFilter",
				"LanguageExplorer.Controls.DetailControls.AtomicReferenceLauncher",
				"LanguageExplorer.Controls.XMLViews.BrowseViewer"
			};
			var idx = 0;
			foreach (var childElement in element.Elements())
			{
				var assemblyPath = childElement.Attribute("assemblyPath").Value;
				Assert.That(assemblyPath, Is.EqualTo("LanguageExplorer.dll"));
				var currentClassValue = childElement.Attribute("class").Value;
				var expectedClassValue = after[idx++];
				Assert.That(currentClassValue, Is.EqualTo(expectedClassValue));
			}
		}
	}
}
