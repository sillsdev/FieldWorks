// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using LanguageExplorer;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorerTests.Impls
{
	/// <summary>
	/// PropertyTable tests.
	/// </summary>
	[TestFixture]
	public class PropertyTableTests
	{
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
			IPublisher publisherDummy;
			ISubscriber subscriberDummy;
			TestSetupServices.SetupTestTriumvirate(out _propertyTable, out publisherDummy, out subscriberDummy);
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
			fPropertyExists = _propertyTable.TryGetValue("BooleanPropertyA", out gpba, SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, "global BooleanPropertyA not found.");
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");

			int gpia;
			fPropertyExists = _propertyTable.TryGetValue("IntegerPropertyA", out gpia, SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, "global IntegerPropertyA not found.");
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");

			string gpsa;
			fPropertyExists = _propertyTable.TryGetValue("StringPropertyA", out gpsa, SettingsGroup.GlobalSettings);
			Assert.IsTrue(fPropertyExists, "global StringPropertyA not found.");
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");

			// Test local property values
			bool lpba;
			fPropertyExists = _propertyTable.TryGetValue("BooleanPropertyA", out lpba, SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, "local BooleanPropertyA not found.");
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");

			int lpia;
			fPropertyExists = _propertyTable.TryGetValue("IntegerPropertyA", out lpia, SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, "local IntegerPropertyA not found.");
			Assert.AreEqual(333, lpia, "Invalid value for local IntegerPropertyA.");

			string lpsa;
			fPropertyExists = _propertyTable.TryGetValue("StringPropertyA", out lpsa, SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, "local StringPropertyA not found.");
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");

			// Test best settings
			// Match on unique globals.
			bool ugpba;
			fPropertyExists = _propertyTable.TryGetValue("BestBooleanPropertyA", out ugpba);
			Assert.IsTrue(fPropertyExists, "best BestBooleanPropertyA not found.");
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			fPropertyExists = _propertyTable.TryGetValue("BestBooleanPropertyA", out ugpba);
			Assert.IsTrue(fPropertyExists, "best BestBooleanPropertyA not found.");
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");

			// Match on unique locals
			int ulpia;
			fPropertyExists = _propertyTable.TryGetValue("BestIntegerPropertyB", out ulpia);
			Assert.IsTrue(fPropertyExists, "best BestIntegerPropertyB not found.");
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			fPropertyExists = _propertyTable.TryGetValue("BestIntegerPropertyB", out ulpia);
			Assert.IsTrue(fPropertyExists, "best BestIntegerPropertyB not found.");
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");

			// Match best locals common with global properties
			bool bpba;
			fPropertyExists = _propertyTable.TryGetValue("BooleanPropertyA", out bpba);
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
			fPropertyExists = _propertyTable.PropertyExists("BestBooleanPropertyA");
			Assert.IsTrue(fPropertyExists, "best BestBooleanPropertyA not found.");

			// Match on unique locals
			int ulpia;
			fPropertyExists = _propertyTable.PropertyExists("BestIntegerPropertyB");
			Assert.IsTrue(fPropertyExists, "best BestIntegerPropertyB not found.");
			fPropertyExists = _propertyTable.PropertyExists("BestIntegerPropertyB");
			Assert.IsTrue(fPropertyExists, "best BestIntegerPropertyB not found.");
			fPropertyExists = _propertyTable.PropertyExists("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.IsTrue(fPropertyExists, "best BestIntegerPropertyB not found.");
			fPropertyExists = _propertyTable.PropertyExists("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsFalse(fPropertyExists, "best BestIntegerPropertyB not found.");

			// Match best locals common with global properties
			bool bpba;
			fPropertyExists = _propertyTable.PropertyExists("BooleanPropertyA");
			Assert.IsTrue(fPropertyExists, "best BooleanPropertyA not found.");
			fPropertyExists = _propertyTable.PropertyExists("BooleanPropertyA");
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
			bestValue = _propertyTable.GetValue<object>("NonexistentPropertyA");
			Assert.IsNull(bestValue, "Invalid value for best NonexistentPropertyA.");
			bestValue = _propertyTable.GetValue<object>("NonexistentPropertyA");
			Assert.IsNull(bestValue, "Invalid value for best NonexistentPropertyA.");

			// Test global property values.
			var gpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");
			gpba = _propertyTable.GetValue("BooleanPropertyA", true, SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");

			var gpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");
			gpia = _propertyTable.GetValue("IntegerPropertyA", 352, SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");

			var gpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");
			gpsa = _propertyTable.GetValue("StringPropertyA", "global_StringPropertyC_value", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");

			// Test locals property values.
			var lpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");
			lpba = _propertyTable.GetValue("BooleanPropertyA", false, SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");

			var lpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, "Invalid value for local IntegerPropertyA.");
			lpia = _propertyTable.GetValue("IntegerPropertyA", 111, SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, "Invalid value for local IntegerPropertyA.");

			var lpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");
			lpsa = _propertyTable.GetValue("StringPropertyA", "local_StringPropertyC_value", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");

			// Make new properties.
			// --- Set Globals and make sure Locals are still null.
			var gpbc = _propertyTable.GetValue("BooleanPropertyC", true, SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, "Invalid value for global BooleanPropertyC.");
			var nullObject = _propertyTable.GetValue<object>("BooleanPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local BooleanPropertyC.");

			var gpic = _propertyTable.GetValue("IntegerPropertyC", 352, SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, "Invalid value for global IntegerPropertyC.");
			nullObject = _propertyTable.GetValue<object>("IntegerPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local IntegerPropertyC.");

			var gpsc = _propertyTable.GetValue("StringPropertyC", "global_StringPropertyC_value", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, "Invalid value for global StringPropertyC.");
			nullObject = _propertyTable.GetValue<object>("StringPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local StringPropertyC.");

			// -- Set Locals and make sure Globals haven't changed.
			var lpbc = _propertyTable.GetValue("BooleanPropertyC", false, SettingsGroup.LocalSettings);
			Assert.IsFalse(lpbc, "Invalid value for local BooleanPropertyC.");
			gpbc = _propertyTable.GetValue<bool>("BooleanPropertyC", SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, "Invalid value for global BooleanPropertyC.");

			var lpic = _propertyTable.GetValue("IntegerPropertyC", 111, SettingsGroup.LocalSettings);
			Assert.AreEqual(111, lpic, "Invalid value for local IntegerPropertyC.");
			gpic = _propertyTable.GetValue<int>("IntegerPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, "Invalid value for global IntegerPropertyC.");

			var lpsc = _propertyTable.GetValue("StringPropertyC", "local_StringPropertyC_value", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsc, "Invalid value for local StringPropertyC.");
			gpsc = _propertyTable.GetValue<string>("StringPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, "Invalid value for global StringPropertyC.");

			// Test best property values;
			// Match on locals common with globals first.
			var bpba = _propertyTable.GetValue<bool>("BooleanPropertyA");
			Assert.IsTrue(bpba, "Invalid value for best BooleanPropertyA.");
			bpba = _propertyTable.GetValue<bool>("BooleanPropertyA");
			Assert.IsTrue(bpba, "Invalid value for best BooleanPropertyA.");
			bpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(bpba, "Invalid value for local BooleanPropertyA.");
			bpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(bpba, "Invalid value for global BooleanPropertyA.");

			var bpia = _propertyTable.GetValue<int>("IntegerPropertyA");
			Assert.AreEqual(333, bpia, "Invalid value for best IntegerPropertyA.");
			bpia = _propertyTable.GetValue<int>("IntegerPropertyA");
			Assert.AreEqual(333, bpia, "Invalid value for best IntegerPropertyA.");
			bpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(333, bpia, "Invalid value for local IntegerPropertyA.");
			bpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, bpia, "Invalid value for global IntegerPropertyA.");

			var bpsa = _propertyTable.GetValue<string>("StringPropertyA");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, "Invalid value for best StringPropertyA.");
			bpsa = _propertyTable.GetValue<string>("StringPropertyA");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, "Invalid value for best StringPropertyA.");
			bpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", bpsa, "Invalid value for local StringPropertyA.");
			bpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", bpsa, "Invalid value for global StringPropertyA.");

			// Match on unique globals.
			var ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA");
			Assert.IsTrue(ubpba, "Invalid value for best BestBooleanPropertyA.");
			var ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA");
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			ugpba = _propertyTable.GetValue("BestBooleanPropertyA", false);
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyA.");

			var ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyA");
			Assert.AreEqual(-101, ubpia, "Invalid value for best BestIntegerPropertyA.");
			var ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyA");
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			ugpia = _propertyTable.GetValue("BestIntegerPropertyA", -818);
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyA.");

			var ubpsa = _propertyTable.GetValue<string>("BestStringPropertyA");
			Assert.AreEqual("global_BestStringPropertyA_value", ubpsa, "Invalid value for best BestStringPropertyA.");
			var ugpsa = _propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			ugpsa = _propertyTable.GetValue<string>("BestStringPropertyA");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			ugpsa = _propertyTable.GetValue("BestStringPropertyA", "global_BestStringPropertyC_value");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestStringPropertyA.");

			// Match on unique locals.
			ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyB");
			Assert.IsFalse(ubpba, "Invalid value for best BestBooleanPropertyB.");
			var ulpba = _propertyTable.GetValue<bool>("BestBooleanPropertyB", SettingsGroup.LocalSettings);
			Assert.IsFalse(ulpba, "Invalid value for best BestBooleanPropertyB.");
			ulpba = _propertyTable.GetValue<bool>("BestBooleanPropertyB");
			Assert.IsFalse(ulpba, "Invalid value for best BestBooleanPropertyB.");
			ulpba = _propertyTable.GetValue("BestBooleanPropertyB", true);
			Assert.IsFalse(ulpba, "Invalid value for best BestBooleanPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyB.");

			ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyB");
			Assert.AreEqual(-586, ubpia, "Invalid value for best BestIntegerPropertyB.");
			var ulpia = _propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			ulpia = _propertyTable.GetValue<int>("BestIntegerPropertyB");
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			ulpia = _propertyTable.GetValue("BestIntegerPropertyB", -685);
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyB.");

			ubpsa = _propertyTable.GetValue<string>("BestStringPropertyB");
			Assert.AreEqual("local_BestStringPropertyB_value", ubpsa, "Invalid value for best BestStringPropertyB.");
			var ulpsa = _propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.LocalSettings);
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
			var gpba = _propertyTable.GetValue("BooleanPropertyA", true, SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");

			var gpia = _propertyTable.GetValue("IntegerPropertyA", 352, SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");

			var gpsa = _propertyTable.GetValue("StringPropertyA", "global_StringPropertyC_value", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");

			// Test locals property values.
			var lpba = _propertyTable.GetValue("BooleanPropertyA", false, SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");

			var lpia = _propertyTable.GetValue("IntegerPropertyA", 111, SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, "Invalid value for local IntegerPropertyA.");

			var lpsa = _propertyTable.GetValue("StringPropertyA", "local_StringPropertyC_value", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");

			// Make new properties.
			var gpbc = _propertyTable.GetValue("BooleanPropertyC", true, SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, "Invalid value for global BooleanPropertyC.");
			var gpic = _propertyTable.GetValue("IntegerPropertyC", 352, SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, "Invalid value for global IntegerPropertyC.");
			var gpsc = _propertyTable.GetValue("StringPropertyC", "global_StringPropertyC_value", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, "Invalid value for global StringPropertyC.");

			var lpbc = _propertyTable.GetValue("BooleanPropertyC", false, SettingsGroup.LocalSettings);
			Assert.IsFalse(lpbc, "Invalid value for local BooleanPropertyC.");
			var lpic = _propertyTable.GetValue("IntegerPropertyC", 111, SettingsGroup.LocalSettings);
			Assert.AreEqual(111, lpic, "Invalid value for local IntegerPropertyC.");
			var lpsc = _propertyTable.GetValue("StringPropertyC", "local_StringPropertyC_value", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsc, "Invalid value for local StringPropertyC.");

			// Test best property values;
			// Match on locals common with globals first.
			var bpba = _propertyTable.GetValue("BooleanPropertyA", false);
			Assert.IsTrue(bpba, "Invalid value for best BooleanPropertyA.");
			bpba = _propertyTable.GetValue("BooleanPropertyA", false);
			Assert.IsTrue(bpba, "Invalid value for best BooleanPropertyA.");

			var bpia = _propertyTable.GetValue("IntegerPropertyA", -333);
			Assert.AreEqual(333, bpia, "Invalid value for best IntegerPropertyA.");
			bpia = _propertyTable.GetValue("IntegerPropertyA", -333);
			Assert.AreEqual(333, bpia, "Invalid value for best IntegerPropertyA.");

			var bpsa = _propertyTable.GetValue("StringPropertyA", "global_StringPropertyA_value");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, "Invalid value for best StringPropertyA.");
			bpsa = _propertyTable.GetValue("StringPropertyA", "global_StringPropertyA_value");
			Assert.AreEqual("local_StringPropertyA_value", bpsa, "Invalid value for best StringPropertyA.");

			// Match on unique globals.
			var ugpba = _propertyTable.GetValue("BestBooleanPropertyA", false);
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			ugpba = _propertyTable.GetValue("BestBooleanPropertyA", false);
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");

			var ugpia = _propertyTable.GetValue("BestIntegerPropertyA", 101);
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			ugpia = _propertyTable.GetValue("BestIntegerPropertyA", 101);
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");

			var ugpsa = _propertyTable.GetValue("BestStringPropertyA", "local_BestStringPropertyA_value");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			ugpsa = _propertyTable.GetValue("BestStringPropertyA", "local_BestStringPropertyA_value");
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");

			// Match on unique locals.
			var ulpba = _propertyTable.GetValue("BestBooleanPropertyB", true);
			Assert.IsFalse(ulpba, "Invalid value for best BestBooleanPropertyB.");
			ulpba = _propertyTable.GetValue("BestBooleanPropertyB", true);
			Assert.IsFalse(ulpba, "Invalid value for best BestBooleanPropertyB.");

			var ulpia = _propertyTable.GetValue("BestIntegerPropertyB", 586);
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			ulpia = _propertyTable.GetValue("BestIntegerPropertyB", 586);
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");

			var ulpsa = _propertyTable.GetValue("BestStringPropertyB", "global_BestStringPropertyC_value");
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
			_propertyTable.SetProperty("BooleanPropertyA", false, true, settingsGroup: SettingsGroup.LocalSettings);
			_propertyTable.SetProperty("BooleanPropertyA", true, true, settingsGroup: SettingsGroup.GlobalSettings);
			var gpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpba, "Invalid value for global BooleanPropertyA.");
			var lpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsFalse(lpba, "Invalid value for local BooleanPropertyA.");

			_propertyTable.SetProperty("BooleanPropertyA", false, true, settingsGroup: SettingsGroup.GlobalSettings);
			_propertyTable.SetProperty("BooleanPropertyA", true, true, settingsGroup: SettingsGroup.LocalSettings);
			lpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");
			gpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");

			_propertyTable.SetProperty("IntegerPropertyA", 253, true, settingsGroup: SettingsGroup.LocalSettings);
			_propertyTable.SetProperty("IntegerPropertyA", -253, true, settingsGroup: SettingsGroup.GlobalSettings);
			var gpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-253, gpia, "Invalid value for global IntegerPropertyA.");
			var lpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(253, lpia, "Invalid value for local IntegerPropertyA.");

			_propertyTable.SetProperty("IntegerPropertyA", 253, true, settingsGroup: SettingsGroup.GlobalSettings);
			_propertyTable.SetProperty("IntegerPropertyA", -253, true, settingsGroup: SettingsGroup.LocalSettings);
			lpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(-253, lpia, "Invalid value for local IntegerPropertyA.");
			gpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");

			_propertyTable.SetProperty("StringPropertyA", "local_StringPropertyC_value", true, settingsGroup: SettingsGroup.LocalSettings);
			_propertyTable.SetProperty("StringPropertyA", "global_StringPropertyC_value", true, settingsGroup: SettingsGroup.GlobalSettings);
			var gpsa = _propertyTable.GetValue("StringPropertyA", "local_StringPropertyC_value", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsa, "Invalid value for global StringPropertyA.");
			var lpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsa, "Invalid value for local StringPropertyA.");

			_propertyTable.SetProperty("StringPropertyA", "global_StringPropertyA_value", true, settingsGroup: SettingsGroup.GlobalSettings);
			_propertyTable.SetProperty("StringPropertyA", "local_StringPropertyA_value", true, settingsGroup: SettingsGroup.LocalSettings);
			lpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");
			gpsa = _propertyTable.GetValue("StringPropertyA", "local_StringPropertyC_value", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");

			// Make new properties. ------------------
			//---- Global Settings
			_propertyTable.SetProperty("BooleanPropertyC", true, true, settingsGroup: SettingsGroup.GlobalSettings);
			var gpbc = _propertyTable.GetValue("BooleanPropertyC", false, SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, "Invalid value for global BooleanPropertyC.");

			_propertyTable.SetProperty("IntegerPropertyC", 352, true, settingsGroup: SettingsGroup.GlobalSettings);
			var gpic = _propertyTable.GetValue("IntegerPropertyC", -352, SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, "Invalid value for global IntegerPropertyC.");

			_propertyTable.SetProperty("StringPropertyC", "global_StringPropertyC_value", true, settingsGroup: SettingsGroup.GlobalSettings);
			var gpsc = _propertyTable.GetValue("StringPropertyC", "local_StringPropertyC_value", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, "Invalid value for global StringPropertyC.");

			//---- Local Settings
			_propertyTable.SetProperty("BooleanPropertyC", false, true, settingsGroup: SettingsGroup.LocalSettings);
			var lpbc = _propertyTable.GetValue("BooleanPropertyC", true, SettingsGroup.LocalSettings);
			Assert.IsFalse(lpbc, "Invalid value for local BooleanPropertyC.");

			_propertyTable.SetProperty("IntegerPropertyC", 111, true, settingsGroup: SettingsGroup.LocalSettings);
			var lpic = _propertyTable.GetValue("IntegerPropertyC", -111, SettingsGroup.LocalSettings);
			Assert.AreEqual(111, lpic, "Invalid value for local IntegerPropertyC.");

			_propertyTable.SetProperty("StringPropertyC", "local_StringPropertyC_value", true, settingsGroup: SettingsGroup.LocalSettings);
			var lpsc = _propertyTable.GetValue("StringPropertyC", "local_StringPropertyC_value", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsc, "Invalid value for local StringPropertyC.");

			// Set best property on locals common with globals first.
			_propertyTable.SetProperty("BooleanPropertyA", true, true, settingsGroup: SettingsGroup.LocalSettings);
			_propertyTable.SetProperty("BooleanPropertyA", true, true, settingsGroup: SettingsGroup.GlobalSettings);
			_propertyTable.SetProperty("BooleanPropertyA", false, true);
			var bpba = _propertyTable.GetValue<bool>("BooleanPropertyA");
			Assert.IsFalse(bpba, "Invalid value for best BooleanPropertyA.");
			gpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpba, "Invalid value for global BooleanPropertyA.");
			lpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsFalse(lpba, "Invalid value for local BooleanPropertyA.");

			_propertyTable.SetProperty("IntegerPropertyA", 253, true, settingsGroup: SettingsGroup.LocalSettings);
			_propertyTable.SetProperty("IntegerPropertyA", -253, true, settingsGroup: SettingsGroup.GlobalSettings);
			_propertyTable.SetProperty("IntegerPropertyA", 352, true);
			var bpia = _propertyTable.GetValue<int>("IntegerPropertyA");
			Assert.AreEqual(352, bpia, "Invalid value for best IntegerPropertyA.");
			gpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-253, gpia, "Invalid value for global IntegerPropertyA.");
			lpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(352, lpia, "Invalid value for local IntegerPropertyA.");

			_propertyTable.SetProperty("StringPropertyA", "local_StringPropertyA_value", true, settingsGroup: SettingsGroup.LocalSettings);
			_propertyTable.SetProperty("StringPropertyA", "global_StringPropertyA_value", true, settingsGroup: SettingsGroup.GlobalSettings);
			_propertyTable.SetProperty("StringPropertyA", "best_StringPropertyA_value", true);
			var bpsa = _propertyTable.GetValue<string>("StringPropertyA");
			Assert.AreEqual("best_StringPropertyA_value", bpsa, "Invalid value for best StringPropertyA.");
			gpsa = _propertyTable.GetValue("StringPropertyA", "local_StringPropertyC_value", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");
			lpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("best_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");

			// Set best setting on unique globals.
			_propertyTable.SetProperty("BestBooleanPropertyA", false, true);
			var ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA");
			Assert.IsFalse(ubpba, "Invalid value for best BestBooleanPropertyA.");
			var ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA");
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyA.");
			var nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyA.");

			_propertyTable.SetProperty("BestIntegerPropertyA", 101, true);
			var ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyA");
			Assert.AreEqual(101, ubpia, "Invalid value for best BestIntegerPropertyA.");
			var ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyA.");

			_propertyTable.SetProperty("BestStringPropertyA", "best_BestStringPropertyA_value", true);
			var ubpsa = _propertyTable.GetValue<string>("BestStringPropertyA");
			Assert.AreEqual("best_BestStringPropertyA_value", ubpsa, "Invalid value for best BestStringPropertyA.");
			var ugpsa = _propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("best_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestStringPropertyA.");

			// Set best setting on unique locals
			_propertyTable.SetProperty("BestBooleanPropertyB", true, true);
			ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyB");
			Assert.IsTrue(ubpba, "Invalid value for best BestBooleanPropertyB.");
			Assert.IsTrue(ubpba, "Invalid value for best BestBooleanPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyB.");

			_propertyTable.SetProperty("BestIntegerPropertyB", 586, true);
			ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyB");
			Assert.AreEqual(586, ubpia, "Invalid value for best BestIntegerPropertyB.");
			var ulpia = _propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual(586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyB.");

			_propertyTable.SetProperty("BestStringPropertyB", "best_BestStringPropertyB_value", true);
			ubpsa = _propertyTable.GetValue<string>("BestStringPropertyB");
			Assert.AreEqual("best_BestStringPropertyB_value", ubpsa, "Invalid value for best BestStringPropertyB.");
			var ulpsa = _propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual("best_BestStringPropertyB_value", ulpsa, "Invalid value for best BestStringPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestStringPropertyB.");

			// Make new best (global) properties
			_propertyTable.SetProperty("BestBooleanPropertyC", false, true);
			ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyC");
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyC.");
			ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyC", SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyC.");

			_propertyTable.SetProperty("BestIntegerPropertyC", -818, true);
			ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyC");
			Assert.AreEqual(-818, ugpia, "Invalid value for best BestIntegerPropertyC.");
			ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-818, ugpia, "Invalid value for best BestIntegerPropertyC.");

			_propertyTable.SetProperty("BestStringPropertyC", "global_BestStringPropertyC_value".Clone(), true);
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
			_propertyTable.SetDefault("BooleanPropertyA", false);
			_propertyTable.SetDefault("BooleanPropertyA", true, settingsGroup: SettingsGroup.GlobalSettings);
			var gpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsFalse(gpba, "Invalid value for global BooleanPropertyA.");
			var lpba = _propertyTable.GetValue<bool>("BooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsTrue(lpba, "Invalid value for local BooleanPropertyA.");

			_propertyTable.SetDefault("IntegerPropertyA", 253);
			_propertyTable.SetDefault("IntegerPropertyA", -253, settingsGroup: SettingsGroup.GlobalSettings);
			var gpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(253, gpia, "Invalid value for global IntegerPropertyA.");
			var lpia = _propertyTable.GetValue<int>("IntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual(333, lpia, "Invalid value for local IntegerPropertyA.");

			_propertyTable.SetDefault("StringPropertyA", "local_StringPropertyC_value");
			_propertyTable.SetDefault("StringPropertyA", "global_StringPropertyC_value", settingsGroup: SettingsGroup.GlobalSettings);
			var gpsa = _propertyTable.GetValue("StringPropertyA", "local_StringPropertyC_value", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyA_value", gpsa, "Invalid value for global StringPropertyA.");
			var lpsa = _propertyTable.GetValue<string>("StringPropertyA", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyA_value", lpsa, "Invalid value for local StringPropertyA.");

			// Make new properties. ------------------
			//---- Global Settings
			_propertyTable.SetDefault("BooleanPropertyC", true, true, settingsGroup: SettingsGroup.GlobalSettings);
			var gpbc = _propertyTable.GetValue("BooleanPropertyC", false, SettingsGroup.GlobalSettings);
			Assert.IsTrue(gpbc, "Invalid value for global BooleanPropertyC.");

			_propertyTable.SetDefault("IntegerPropertyC", 352, true, settingsGroup: SettingsGroup.GlobalSettings);
			var gpic = _propertyTable.GetValue("IntegerPropertyC", -352, SettingsGroup.GlobalSettings);
			Assert.AreEqual(352, gpic, "Invalid value for global IntegerPropertyC.");

			_propertyTable.SetDefault("StringPropertyC", "global_StringPropertyC_value", true, settingsGroup: SettingsGroup.GlobalSettings);
			var gpsc = _propertyTable.GetValue("StringPropertyC", "local_StringPropertyC_value", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_StringPropertyC_value", gpsc, "Invalid value for global StringPropertyC.");

			//---- Local Settings
			_propertyTable.SetDefault("BooleanPropertyC", false);
			var lpbc = _propertyTable.GetValue("BooleanPropertyC", true, SettingsGroup.LocalSettings);
			Assert.IsFalse(lpbc, "Invalid value for local BooleanPropertyC.");

			_propertyTable.SetDefault("IntegerPropertyC", 111);
			var lpic = _propertyTable.GetValue("IntegerPropertyC", -111, SettingsGroup.LocalSettings);
			Assert.AreEqual(111, lpic, "Invalid value for local IntegerPropertyC.");

			_propertyTable.SetDefault("StringPropertyC", "local_StringPropertyC_value");
			var lpsc = _propertyTable.GetValue("StringPropertyC", "local_StringPropertyC_value", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_StringPropertyC_value", lpsc, "Invalid value for local StringPropertyC.");

			// Set best setting on unique globals.
			_propertyTable.SetDefault("BestBooleanPropertyA", false, settingsGroup: SettingsGroup.BestSettings);
			var ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA");
			Assert.IsTrue(ubpba, "Invalid value for best BestBooleanPropertyA.");
			var ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyA", SettingsGroup.GlobalSettings);
			Assert.IsTrue(ugpba, "Invalid value for best BestBooleanPropertyA.");
			var nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyA.");

			_propertyTable.SetDefault("BestIntegerPropertyA", 101, settingsGroup: SettingsGroup.BestSettings);
			var ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyA");
			Assert.AreEqual(-101, ubpia, "Invalid value for best BestIntegerPropertyA.");
			var ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-101, ugpia, "Invalid value for best BestIntegerPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyA.");

			_propertyTable.SetDefault("BestStringPropertyA", "best_BestStringPropertyA_value", settingsGroup: SettingsGroup.BestSettings);
			var ubpsa = _propertyTable.GetValue<string>("BestStringPropertyA");
			Assert.AreEqual("global_BestStringPropertyA_value", ubpsa, "Invalid value for best BestStringPropertyA.");
			var ugpsa = _propertyTable.GetValue<string>("BestStringPropertyA", SettingsGroup.GlobalSettings);
			Assert.AreEqual("global_BestStringPropertyA_value", ugpsa, "Invalid value for best BestStringPropertyA.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyA", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestStringPropertyA.");

			// Set best setting on unique locals
			_propertyTable.SetDefault("BestBooleanPropertyB", true, settingsGroup: SettingsGroup.BestSettings);
			ubpba = _propertyTable.GetValue<bool>("BestBooleanPropertyB");
			Assert.IsFalse(ubpba, "Invalid value for best BestBooleanPropertyB.");
			Assert.IsFalse(ubpba, "Invalid value for best BestBooleanPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestBooleanPropertyB.");

			_propertyTable.SetDefault("BestIntegerPropertyB", 586, settingsGroup: SettingsGroup.BestSettings);
			ubpia = _propertyTable.GetValue<int>("BestIntegerPropertyB");
			Assert.AreEqual(-586, ubpia, "Invalid value for best BestIntegerPropertyB.");
			var ulpia = _propertyTable.GetValue<int>("BestIntegerPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual(-586, ulpia, "Invalid value for best BestIntegerPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestIntegerPropertyB.");

			_propertyTable.SetDefault("BestStringPropertyB", "best_BestStringPropertyB_value", settingsGroup: SettingsGroup.BestSettings);
			ubpsa = _propertyTable.GetValue<string>("BestStringPropertyB");
			Assert.AreEqual("local_BestStringPropertyB_value", ubpsa, "Invalid value for best BestStringPropertyB.");
			var ulpsa = _propertyTable.GetValue<string>("BestStringPropertyB", SettingsGroup.LocalSettings);
			Assert.AreEqual("local_BestStringPropertyB_value", ulpsa, "Invalid value for best BestStringPropertyB.");
			nullObject = _propertyTable.GetValue<object>("BestStringPropertyB", SettingsGroup.GlobalSettings);
			Assert.IsNull(nullObject, "Invalid value for best BestStringPropertyB.");

			// Make new best (global) properties
			_propertyTable.SetDefault("BestBooleanPropertyC", false, settingsGroup: SettingsGroup.GlobalSettings);
			ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyC");
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyC.");
			ugpba = _propertyTable.GetValue<bool>("BestBooleanPropertyC", SettingsGroup.GlobalSettings);
			Assert.IsFalse(ugpba, "Invalid value for best BestBooleanPropertyC.");
			nullObject = _propertyTable.GetValue<object>("BestBooleanPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local BestBooleanPropertyC.");

			_propertyTable.SetDefault("BestIntegerPropertyC", -818, settingsGroup: SettingsGroup.GlobalSettings);
			ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyC");
			Assert.AreEqual(-818, ugpia, "Invalid value for best BestIntegerPropertyC.");
			ugpia = _propertyTable.GetValue<int>("BestIntegerPropertyC", SettingsGroup.GlobalSettings);
			Assert.AreEqual(-818, ugpia, "Invalid value for best BestIntegerPropertyC.");
			nullObject = _propertyTable.GetValue<object>("BestIntegerPropertyC", SettingsGroup.LocalSettings);
			Assert.IsNull(nullObject, "Invalid value for local BestIntegerPropertyC.");

			_propertyTable.SetDefault("BestStringPropertyC", "global_BestStringPropertyC_value", settingsGroup: SettingsGroup.GlobalSettings);
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
			_propertyTable.SetProperty(noSuchPropName, notDefault, settingsGroup: SettingsGroup.GlobalSettings);
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
				"LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance.ClosedFeatureValue",
				"LanguageExplorer.Areas.Lexicon.CircularRefBreaker",
				"LanguageExplorer.Areas.TextsAndWords.Interlinear.AddAllomorphDlg",
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
