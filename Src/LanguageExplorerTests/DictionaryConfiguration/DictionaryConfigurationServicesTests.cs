// Copyright (c) 2014-2020 SIL International
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
	public class DictionaryConfigurationServicesTests : MemoryOnlyBackendProviderTestBase
	{
		private FlexComponentParameters _flexComponentParameters;
		private IPropertyTable _propertyTable;

		#region Overrides of LcmTestBase

		public override void FixtureSetup()
		{
			base.FixtureSetup();
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
			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, LanguageExplorerConstants.LexiconEditMachineName, true);
			var projectConfigDir = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Dictionary");
			Assert.That(DictionaryConfigurationServices.GetProjectConfigurationDirectory(_propertyTable), Is.EqualTo(projectConfigDir), "did not return expected directory");

			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, LanguageExplorerConstants.ReversalEditCompleteMachineName, true);
			projectConfigDir = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "ReversalIndex");
			Assert.That(DictionaryConfigurationServices.GetProjectConfigurationDirectory(_propertyTable), Is.EqualTo(projectConfigDir), "did not return expected directory");

			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, "somethingElse", true);
			Assert.That(DictionaryConfigurationServices.GetProjectConfigurationDirectory(_propertyTable), Is.Null, "Other areas should cause null return");
		}

		[Test]
		public void GetDictionaryConfigurationBaseType_ReportsCorrectlyForDictionaryAndReversal()
		{
			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, LanguageExplorerConstants.LexiconEditMachineName, true);
			Assert.AreEqual("Dictionary", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), "did not return expected type");
			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, LanguageExplorerConstants.LexiconBrowseMachineName, true);
			Assert.AreEqual("Dictionary", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), "did not return expected type");
			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, LanguageExplorerConstants.LexiconDictionaryMachineName, true);
			Assert.AreEqual("Dictionary", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), "did not return expected type");

			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, LanguageExplorerConstants.ReversalEditCompleteMachineName, true);
			Assert.AreEqual("Reversal Index", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), "did not return expected type");
			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, LanguageExplorerConstants.ReversalBulkEditReversalEntriesMachineName, true);
			Assert.AreEqual("Reversal Index", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), "did not return expected type");

			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, "somethingElse", true);
			Assert.That(DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(_propertyTable), Is.Null, "Other areas should return null");
		}

		[Test]
		public void GetDefaultConfigurationDirectory_ReportsCorrectlyForDictionaryAndReversal()
		{
			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, LanguageExplorerConstants.LexiconEditMachineName, true);
			var configDir = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary");
			Assert.That(DictionaryConfigurationServices.GetDefaultConfigurationDirectory(_propertyTable), Is.EqualTo(configDir), "did not return expected directory");

			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, LanguageExplorerConstants.ReversalEditCompleteMachineName, true);
			configDir = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "ReversalIndex");
			Assert.That(DictionaryConfigurationServices.GetDefaultConfigurationDirectory(_propertyTable), Is.EqualTo(configDir), "did not return expected directory");

			_propertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, "somethingElse", true);
			Assert.That(DictionaryConfigurationServices.GetDefaultConfigurationDirectory(_propertyTable), Is.Null, "Other areas should cause null return");
		}
	}
}