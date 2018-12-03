// Copyright (c) 2014-2018 SIL International
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
	public class DictionaryConfigurationListenerTests : AppTestBase
	{
		private FlexComponentParameters _flexComponentParameters;
		private IPropertyTable m_propertyTable;

		#region Overrides of LcmTestBase

		protected override void FixtureInit()
		{
			ISharedEventHandlers dummy;
			_flexComponentParameters = TestSetupServices.SetupEverything(Cache, out dummy);
			m_propertyTable = _flexComponentParameters.PropertyTable;
		}

		public override void FixtureTeardown()
		{
			_flexComponentParameters.PropertyTable.Dispose();
			_flexComponentParameters = null;
			m_propertyTable = null;

			base.FixtureTeardown();
		}

		#endregion

		[Test]
		public void GetProjectConfigurationDirectory_ReportsCorrectlyForDictionaryAndReversal()
		{
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconEditMachineName);
			var projectConfigDir = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Dictionary");
			Assert.That(DictionaryConfigurationServices.GetProjectConfigurationDirectory(m_propertyTable), Is.EqualTo(projectConfigDir), "did not return expected directory");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalEditCompleteMachineName);
			projectConfigDir = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "ReversalIndex");
			Assert.That(DictionaryConfigurationServices.GetProjectConfigurationDirectory(m_propertyTable), Is.EqualTo(projectConfigDir), "did not return expected directory");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, "somethingElse");
			Assert.IsNull(DictionaryConfigurationServices.GetProjectConfigurationDirectory(m_propertyTable), "Other areas should cause null return");
		}

		[Test]
		public void GetDictionaryConfigurationBaseType_ReportsCorrectlyForDictionaryAndReversal()
		{
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconEditMachineName);
			Assert.AreEqual("Dictionary", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconBrowseMachineName);
			Assert.AreEqual("Dictionary", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName);
			Assert.AreEqual("Dictionary", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalEditCompleteMachineName);
			Assert.AreEqual("Reversal Index", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalBulkEditReversalEntriesMachineName);
			Assert.AreEqual("Reversal Index", DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, "somethingElse");
			Assert.IsNull(DictionaryConfigurationServices.GetDictionaryConfigurationBaseType(m_propertyTable), "Other areas should return null");
		}

		[Test]
		public void GetDefaultConfigurationDirectory_ReportsCorrectlyForDictionaryAndReversal()
		{
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconEditMachineName);
			var configDir = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary");
			Assert.That(DictionaryConfigurationServices.GetDefaultConfigurationDirectory(m_propertyTable), Is.EqualTo(configDir), "did not return expected directory");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalEditCompleteMachineName);
			configDir = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "ReversalIndex");
			Assert.That(DictionaryConfigurationServices.GetDefaultConfigurationDirectory(m_propertyTable), Is.EqualTo(configDir), "did not return expected directory");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, "somethingElse");
			Assert.IsNull(DictionaryConfigurationServices.GetDefaultConfigurationDirectory(m_propertyTable), "Other areas should cause null return");
		}
	}
}