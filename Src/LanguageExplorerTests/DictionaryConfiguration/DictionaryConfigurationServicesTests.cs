// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using LanguageExplorer;
using LanguageExplorer.Areas;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	[TestFixture]
	public class DictionaryConfigurationServicesTests : AppTestBase
	{
		private FlexComponentParameters _flexComponentParameters;
		private IPropertyTable _propertyTable;

		#region Overrides of LcmTestBase

		protected override void FixtureInit()
		{
			_flexComponentParameters = TestSetupServices.SetupEverything(Cache);
			_propertyTable = _flexComponentParameters.PropertyTable;
		}

		public override void FixtureTeardown()
		{
			TestSetupServices.DisposeTrash(_flexComponentParameters);
			_flexComponentParameters = null;
			_propertyTable = null;

			base.FixtureTeardown();
		}

		#endregion

		[Test]
		public void GetProjectConfigurationDirectory_ReportsCorrectlyForDictionaryAndReversal()
		{
			_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconEditMachineName);
			var projectConfigDir = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Dictionary");
			Assert.That(DictionaryConfigurationServices.GetProjectConfigurationDirectory(_propertyTable), Is.EqualTo(projectConfigDir), "did not return expected directory");

			_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalEditCompleteMachineName);
			projectConfigDir = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "ReversalIndex");
			Assert.That(DictionaryConfigurationServices.GetProjectConfigurationDirectory(_propertyTable), Is.EqualTo(projectConfigDir), "did not return expected directory");

			_propertyTable.SetProperty(AreaServices.ToolChoice, "somethingElse");
			Assert.IsNull(DictionaryConfigurationServices.GetProjectConfigurationDirectory(_propertyTable), "Other areas should cause null return");
		}

		[Test]
		public void GetDictionaryConfigurationBaseType_ReportsCorrectlyForDictionaryAndReversal()
		{
			_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconEditMachineName);
			Assert.AreEqual("Dictionary", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), "did not return expected type");
			_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconBrowseMachineName);
			Assert.AreEqual("Dictionary", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), "did not return expected type");
			_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName);
			Assert.AreEqual("Dictionary", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), "did not return expected type");

			_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalEditCompleteMachineName);
			Assert.AreEqual("Reversal Index", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), "did not return expected type");
			_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalBulkEditReversalEntriesMachineName);
			Assert.AreEqual("Reversal Index", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), "did not return expected type");

			_propertyTable.SetProperty(AreaServices.ToolChoice, "somethingElse");
			Assert.IsNull(DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), "Other areas should return null");
		}

		[Test]
		public void GetDefaultConfigurationDirectory_ReportsCorrectlyForDictionaryAndReversal()
		{
			_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconEditMachineName);
			var configDir = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary");
			Assert.That(DictionaryConfigurationServices.GetDefaultConfigurationDirectory(_propertyTable), Is.EqualTo(configDir), "did not return expected directory");

			_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalEditCompleteMachineName);
			configDir = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "ReversalIndex");
			Assert.That(DictionaryConfigurationServices.GetDefaultConfigurationDirectory(_propertyTable), Is.EqualTo(configDir), "did not return expected directory");

			_propertyTable.SetProperty(AreaServices.ToolChoice, "somethingElse");
			Assert.IsNull(DictionaryConfigurationServices.GetDefaultConfigurationDirectory(_propertyTable), "Other areas should cause null return");
		}
	}
}