// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using LanguageExplorer.Areas;
using LanguageExplorer.Works;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorerTests.Works
{
	[TestFixture]
	public class DictionaryConfigurationListenerTests : XWorksAppTestBase
	{
		private FlexComponentParameters _flexComponentParameters;
		private IPropertyTable m_propertyTable;

		#region Overrides of LcmTestBase

		protected override void FixtureInit()
		{
			_flexComponentParameters = TestSetupServices.SetupEverything(Cache);
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
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconEditMachineName, false, false);
			var projectConfigDir = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Dictionary");
			Assert.That(DictionaryConfigurationListener.GetProjectConfigurationDirectory(m_propertyTable), Is.EqualTo(projectConfigDir), "did not return expected directory");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalEditCompleteMachineName, false, false);
			projectConfigDir = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "ReversalIndex");
			Assert.That(DictionaryConfigurationListener.GetProjectConfigurationDirectory(m_propertyTable), Is.EqualTo(projectConfigDir), "did not return expected directory");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, "somethingElse", false, false);
			Assert.IsNull(DictionaryConfigurationListener.GetProjectConfigurationDirectory(m_propertyTable), "Other areas should cause null return");
		}

		[Test]
		public void GetDictionaryConfigurationBaseType_ReportsCorrectlyForDictionaryAndReversal()
		{
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconEditMachineName, false, false);
			Assert.AreEqual("Dictionary", DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconBrowseMachineName, false, false);
			Assert.AreEqual("Dictionary", DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName, false, false);
			Assert.AreEqual("Dictionary", DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalEditCompleteMachineName, false, false);
			Assert.AreEqual("Reversal Index", DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalBulkEditReversalEntriesMachineName, false, false);
			Assert.AreEqual("Reversal Index", DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, "somethingElse", false, false);
			Assert.IsNull(DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "Other areas should return null");
		}

		[Test]
		public void GetDefaultConfigurationDirectory_ReportsCorrectlyForDictionaryAndReversal()
		{
			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconEditMachineName, false, false);
			var configDir = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary");
			Assert.That(DictionaryConfigurationListener.GetDefaultConfigurationDirectory(m_propertyTable), Is.EqualTo(configDir), "did not return expected directory");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.ReversalEditCompleteMachineName, false, false);
			configDir = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "ReversalIndex");
			Assert.That(DictionaryConfigurationListener.GetDefaultConfigurationDirectory(m_propertyTable), Is.EqualTo(configDir), "did not return expected directory");

			m_propertyTable.SetProperty(AreaServices.ToolChoice, "somethingElse", false, false);
			Assert.IsNull(DictionaryConfigurationListener.GetDefaultConfigurationDirectory(m_propertyTable), "Other areas should cause null return");
		}
	}
}