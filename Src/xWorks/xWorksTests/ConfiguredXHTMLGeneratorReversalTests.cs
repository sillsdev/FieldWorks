// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Palaso.TestUtilities;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	// ReSharper disable InconsistentNaming
	[TestFixture]
	internal class ConfiguredXHTMLGeneratorReversalTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private int m_wsEn, m_wsFr;

		private FwXApp m_application;
		private FwXWindow m_window;
		private PropertyTable m_propertyTable;

		private StringBuilder XHTMLStringBuilder { get; set; }

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory,
				m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
			// Set up the mediator to look as if we are working in the Reversal Index area
			m_propertyTable.SetProperty("ToolForAreaNamed_lexicon", "reversalEditComplete", true);
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory,
				"xWorks/xWorksTests/TestData/");
			m_wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			m_wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
		}

		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			Dispose();
		}

		[SetUp]
		public void SetupExportVariables()
		{
			XHTMLStringBuilder = new StringBuilder();
		}

		#region disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (m_application != null)
					m_application.Dispose();
				if (m_window != null)
					m_window.Dispose();
				if(m_propertyTable != null)
					m_propertyTable.Dispose();
			}
		}

		~ConfiguredXHTMLGeneratorReversalTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}
		#endregion disposal

		[Test]
		public void GenerateXHTMLForEntry_LexemeFormConfigurationGeneratesCorrectResult()
		{
			var reversalFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalForm",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new [] {"en"}),
				Label = "Reversal Form",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { reversalFormNode },
				FieldDescription = "ReversalIndexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var entry = CreateInterestingReversalEntry();
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_propertyTable, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string frenchLexForm = "/div[@class='reversalindexentry']/span[@class='reversalform']/span[@lang='en' and text()='ReversalForm']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(frenchLexForm, 1);
			}
		}


		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfNoPreviousHeader()
		{
			var entry = CreateInterestingReversalEntry();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				string last = null;
				XHTMLWriter.WriteStartElement("TestElement");
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/div[@class='letter' and text()='R r']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
				Assert.AreEqual("r", last, "should have updated the last letter header");
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfPreviousHeaderDoesNotMatch()
		{
			var entry = CreateInterestingReversalEntry();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/div[@class='letter' and text()='R r']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesNoHeaderIfPreviousHeaderDoesMatch()
		{
			var entry = CreateInterestingReversalEntry();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache);
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/div[@class='letter' and text()='R r']";
				const string proveOnlyOneHeader = "//div[@class='letHead']/div[@class='letter']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(proveOnlyOneHeader, 1);
			}
		}


		private ICmObject CreateInterestingReversalEntry()
		{
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var indexfactory = Cache.ServiceLocator.GetInstance<IReversalIndexFactory>();
			var index = indexfactory.Create();
			Cache.LangProject.LexDbOA.ReversalIndexesOC.Add(index);
			var indexEntry = Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create();
			index.EntriesOC.Add(indexEntry);
			indexEntry.ReversalForm.set_String(m_wsEn, "ReversalForm");
			entry.AllSenses[0].ReversalEntriesRC.Add(indexEntry);
			return indexEntry;
		}
	}
}
