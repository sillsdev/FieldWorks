// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	// ReSharper disable InconsistentNaming
	[TestFixture]
	public class ConfiguredLcmGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private PropertyTable m_propertyTable;
		private FwXApp m_application;
		private FwXWindow m_window;

		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			Dispose();
		}

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
		}

		#region disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				m_application?.Dispose();
				m_window?.Dispose();
				m_propertyTable?.Dispose();
			}
		}

		~ConfiguredLcmGeneratorTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
		#endregion

		[Test]
		public void GeneratorSettings_NullArgsThrowArgumentNull()
		{
			// ReSharper disable AccessToDisposedClosure // Justification: Assert calls lambdas immediately, so XHTMLWriter is not used after being disposed
			// ReSharper disable ObjectCreationAsStatement // Justification: We expect the constructor to throw, so there's no created object to assign anywhere :)
			Assert.Throws(typeof(ArgumentNullException), () => new ConfiguredLcmGenerator.GeneratorSettings(Cache, (PropertyTable)null, false, false, null));
			Assert.Throws(typeof(ArgumentNullException), () => new ConfiguredLcmGenerator.GeneratorSettings(null, new ReadOnlyPropertyTable(m_propertyTable), false, false, null));
			// ReSharper restore ObjectCreationAsStatement
			// ReSharper restore AccessToDisposedClosure
		}

		[Test]
		public void GenerateXHTMLForEntry_NullArgsThrowArgumentNull()
		{
			var mainEntryNode = new ConfigurableDictionaryNode();
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null);
			//SUT
			Assert.Throws(typeof(ArgumentNullException), () => ConfiguredLcmGenerator.GenerateXHTMLForEntry(null, mainEntryNode, null, settings));
			Assert.Throws(typeof(ArgumentNullException), () => ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, (ConfigurableDictionaryNode)null, null, settings));
			Assert.Throws(typeof(ArgumentNullException), () => ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, null));
		}

		[Test]
		public void GenerateXHTMLForEntry_BadConfigurationThrows()
		{
			var mainEntryNode = new ConfigurableDictionaryNode();
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null);
			//SUT
			//Test a blank main node description
			Assert.That(() => ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings),
				Throws.InstanceOf<ArgumentException>().With.Message.Contains("Invalid configuration"));
			//Test a configuration with a valid but incorrect type
			mainEntryNode.FieldDescription = "LexSense";
			Assert.That(() => ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings),
				Throws.InstanceOf<ArgumentException>().With.Message.Contains("doesn't configure this type"));
		}
	}
}
