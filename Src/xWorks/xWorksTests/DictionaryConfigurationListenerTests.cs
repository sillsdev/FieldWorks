// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryConfigurationListenerTests : XWorksAppTestBase, IDisposable
	{
		#region Context
		private PropertyTable m_propertyTable;

		[TestFixtureSetUp]
		public new void FixtureSetup()
		{
			// We won't call Init since XWorksAppTestBase's TestFixtureSetUp calls it.
		}
		#endregion

		[Test]
		public void GetProjectConfigurationDirectory_ReportsCorrectlyForDictionaryAndReversal()
		{
			{
				string projectConfigDir;

				m_propertyTable.SetProperty("currentContentControl", "lexiconEdit", true);
				projectConfigDir = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Dictionary");
				Assert.That(DictionaryConfigurationListener.GetProjectConfigurationDirectory(m_propertyTable), Is.EqualTo(projectConfigDir), "did not return expected directory");

				m_propertyTable.SetProperty("currentContentControl", "reversalToolEditComplete", true);
				projectConfigDir = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "ReversalIndex");
				Assert.That(DictionaryConfigurationListener.GetProjectConfigurationDirectory(m_propertyTable), Is.EqualTo(projectConfigDir), "did not return expected directory");

				m_propertyTable.SetProperty("currentContentControl", "somethingElse", true);
				Assert.IsNull(DictionaryConfigurationListener.GetProjectConfigurationDirectory(m_propertyTable), "Other areas should cause null return");
			}
		}

		[Test]
		public void GetDictionaryConfigurationBaseType_ReportsCorrectlyForDictionaryAndReversal()
		{
			m_propertyTable.SetProperty("currentContentControl", "lexiconEdit", true);
			Assert.AreEqual("Dictionary", DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");
			m_propertyTable.SetProperty("currentContentControl", "lexiconBrowse", true);
			Assert.AreEqual("Dictionary", DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");
			m_propertyTable.SetProperty("currentContentControl", "lexiconDictionary", true);
			Assert.AreEqual("Dictionary", DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");

			m_propertyTable.SetProperty("currentContentControl", "reversalToolEditComplete", true);
			Assert.AreEqual("Reversal Index", DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");
			m_propertyTable.SetProperty("currentContentControl", "reversalToolBulkEditReversalEntries", true);
			Assert.AreEqual("Reversal Index", DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "did not return expected type");

			m_propertyTable.SetProperty("currentContentControl", "somethingElse", true);
			Assert.IsNull(DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_propertyTable), "Other areas should return null");
		}

		[Test]
		public void GetDefaultConfigurationDirectory_ReportsCorrectlyForDictionaryAndReversal()
		{
			string configDir;

			m_propertyTable.SetProperty("currentContentControl", "lexiconEdit", true);
			configDir = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary");
			Assert.That(DictionaryConfigurationListener.GetDefaultConfigurationDirectory(m_propertyTable), Is.EqualTo(configDir), "did not return expected directory");

			m_propertyTable.SetProperty("currentContentControl", "reversalToolEditComplete", true);
			configDir = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "ReversalIndex");
			Assert.That(DictionaryConfigurationListener.GetDefaultConfigurationDirectory(m_propertyTable), Is.EqualTo(configDir), "did not return expected directory");

			m_propertyTable.SetProperty("currentContentControl", "somethingElse", true);
			Assert.IsNull(DictionaryConfigurationListener.GetDefaultConfigurationDirectory(m_propertyTable), "Other areas should cause null return");
		}

		#region Context
		protected override void Init()
		{
			BootstrapSystem(new TestProjectId(BackendProviderType.kMemoryOnly, "TestProject"),
				BackendBulkLoadDomain.Lexicon, new LcmSettings());
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
		}

		~DictionaryConfigurationListenerTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if(m_application != null)
				m_application.Dispose();
			if(m_window != null)
				m_window.Dispose();
			if(m_propertyTable != null)
				m_propertyTable.Dispose();
		}
		#endregion
	}
}
