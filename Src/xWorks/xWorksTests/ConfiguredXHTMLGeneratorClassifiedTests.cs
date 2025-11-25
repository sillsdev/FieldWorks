// Copyright (c) 2015-2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.TestUtilities;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainImpl;
using XCore;
using CXGTests = SIL.FieldWorks.XWorks.ConfiguredXHTMLGeneratorTests;

namespace SIL.FieldWorks.XWorks
{
	// ReSharper disable InconsistentNaming
	[TestFixture]
	public class ConfiguredXHTMLGeneratorClassifiedTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private int m_wsEn, m_wsFr;

		private FwXApp m_application;
		private FwXWindow m_window;
		private PropertyTable m_propertyTable;

		private ConfiguredLcmGenerator.GeneratorSettings DefaultSettings
		{
			get { return new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null); }
		}

		// XPath constants for Semantic Domain tests
		private const string domainXpath = "/div[@class='domain']";
		private const string domainAbbrXpath = domainXpath + "/span[@class='abbreviation']";
		private const string domainNameXpath = domainXpath + "/span[@class='name']";
		private const string sensesXpath = domainXpath + "/span[@class='senses']";
		private const string senseContentXpath = sensesXpath + "/span[@class='sensecontent']";
		private const string senseXpath = senseContentXpath + "/span[@class='sense']";
		private const string headwordXpath = senseXpath + "/span[@class='headword-classified']";
		private const string definitionXpath = senseXpath + "/span[@class='definitionorgloss']";

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			ConfiguredLcmGenerator.Init();
			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory,
				m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
			// Set up the mediator to look as if we are working in the Classified Dictionary area
			m_propertyTable.SetProperty("ToolForAreaNamed_lexicon", "classifiedDictionary", true);
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory,
				"xWorks/xWorksTests/TestData/");
			m_wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			m_wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
		}

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			FwRegistrySettings.Release();
			Dispose();
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

		~ConfiguredXHTMLGeneratorClassifiedTests()
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
		#endregion disposal

		#region Helper Methods

		/// <summary>
		/// Creates a semantic domain with the specified abbreviation, name, and senses.
		/// Uses analysis writing system (English).
		/// </summary>
		private ICmSemanticDomain CreateSemanticDomainWithSenses(string abbr, string name, params ILexSense[] senses)
		{
			var semDomList = Cache.LangProject.SemanticDomainListOA;
			var semDomFactory = Cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
			var semDom = semDomFactory.Create();
			semDomList.PossibilitiesOS.Add(semDom);

			semDom.Abbreviation.set_String(m_wsEn, abbr);
			semDom.Name.set_String(m_wsEn, name);

			foreach (var sense in senses)
			{
				sense.SemanticDomainsRC.Add(semDom);
			}

			return semDom;
		}

		/// <summary>
		/// Creates a test publication for filtering tests
		/// </summary>
		private ICmPossibility CreateTestPublication(string name)
		{
			var pubTypesList = Cache.LangProject.LexDbOA.PublicationTypesOA;
			var factory = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			var publication = factory.Create();
			pubTypesList.PossibilitiesOS.Add(publication);
			publication.Name.set_String(m_wsEn, name);
			return publication;
		}

		/// <summary>
		/// Creates a lexical entry with a sense, optionally with a subsense
		/// </summary>
		private ILexSense CreateEntryWithSense(string headword, string gloss, bool addSubsense = false, string subgloss = null)
		{
			var entry = SIL.FieldWorks.XWorks.ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, headword, gloss);
			var sense = entry.SensesOS.First();

			if (addSubsense)
			{
				var subsense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				sense.SensesOS.Add(subsense);
				subsense.Gloss.set_String(m_wsEn, TsStringUtils.MakeString(subgloss ?? "subsense", m_wsEn));
			}

			return sense;
		}

		/// <summary>
		/// Builds the configuration for semantic domain with senses
		/// </summary>
		private static ConfigurableDictionaryNode BuildSemanticDomainConfig(bool includeSubsenses = false)
		{
			var abbrNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Abbreviation",
				DictionaryNodeOptions = CXGTests.GetWsOptionsForLanguages(new[] { "analysis" })
			};

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = CXGTests.GetWsOptionsForLanguages(new[] { "analysis" })
			};

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				CSSClassNameOverride = "headword-classified",
				DictionaryNodeOptions = CXGTests.GetWsOptionsForLanguages(new[] { "vernacular" })
			};

			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "DefinitionOrGloss",
				DictionaryNodeOptions = CXGTests.GetWsOptionsForLanguages(new[] { "analysis" })
			};

			var senseChildren = new List<ConfigurableDictionaryNode> { headwordNode, glossNode };

			// Add subsense node if requested
			if (includeSubsenses)
			{
				var subsenseNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "SensesOS",
					CSSClassNameOverride = "senses",
					Label = "Subsenses",
					DictionaryNodeOptions = new DictionaryNodeSenseOptions
					{
						NumberingStyle = "%d",
						DisplayEachSenseInAParagraph = true,
						NumberEvenASingleSense = true
					},
					Children = new List<ConfigurableDictionaryNode> { headwordNode, glossNode }
				};
				senseChildren.Add(subsenseNode);
			}

			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReferringSenses",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberingStyle = "%d",
					DisplayEachSenseInAParagraph = true
				},
				Children = senseChildren
			};

			var domainNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "CmSemanticDomain",
				CSSClassNameOverride = "domain",
				Children = new List<ConfigurableDictionaryNode> { abbrNode, nameNode, sensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(domainNode);
			return domainNode;
		}

		#endregion Helper Methods

		#region Tests

		[Test]
		public void GenerateXHTMLForSemanticDomain_BasicStructure_GeneratesCorrectResult()
		{
			// Arrange
			var sense = CreateEntryWithSense("testword", "test gloss");
			var domain = CreateSemanticDomainWithSenses("1.1", "Universe", sense);
			var config = BuildSemanticDomainConfig();

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(domain, config, null, DefaultSettings).ToString();

			// Assert
			const string abbrXpath = domainAbbrXpath + "/span[@lang='en' and text()='1.1']";
			const string nameXpath = domainNameXpath + "/span[@lang='en' and text()='Universe']";
			const string headwordXpathCheck = headwordXpath + "//span[@lang='fr']/a[text()='testword']";
			const string glossXpath = definitionXpath + "//span[@lang='en' and text()='test gloss']";

			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(abbrXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nameXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordXpathCheck, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(glossXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForSemanticDomain_MultipleSenses_GeneratesNumberedSenses()
		{
			// Arrange
			var sense1 = CreateEntryWithSense("word1", "first gloss");
			var sense2 = CreateEntryWithSense("word2", "second gloss");
			var domain = CreateSemanticDomainWithSenses("1.1", "Universe", sense1, sense2);
			var config = BuildSemanticDomainConfig();

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(domain, config, null, DefaultSettings).ToString();

			// Assert - Verify sense structure: headword first, then sense number, then gloss
			const string sense1HeadwordXpath = senseXpath + "/span[@class='headword-classified'][1]//span[@lang='fr']/a[text()='word1']";
			const string sense1NumberXpath = senseXpath + "/span[@class='sensenumber' and text()='1' and preceding-sibling::span[@class='headword-classified']]";
			const string sense1GlossXpath = senseXpath + "//span[@lang='en' and text()='first gloss']";
			
			const string sense2HeadwordXpath = "(" + senseXpath + ")[2]/span[@class='headword-classified'][1]//span[@lang='fr']/a[text()='word2']";
			const string sense2NumberXpath = "(" + senseXpath + ")[2]/span[@class='sensenumber' and text()='2' and preceding-sibling::span[@class='headword-classified']]";
			const string sense2GlossXpath = "(" + senseXpath + ")[2]//span[@lang='en' and text()='second gloss']";
			
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(sense1HeadwordXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(sense1NumberXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(sense1GlossXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(sense2HeadwordXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(sense2NumberXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(sense2GlossXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForSemanticDomain_WithSubsenses_GeneratesCorrectNumbering()
		{
			// Arrange
			var sense = CreateEntryWithSense("word", "main gloss", true, "sub gloss");
			var domain = CreateSemanticDomainWithSenses("1.1", "Universe", sense);
			var config = BuildSemanticDomainConfig(includeSubsenses: true);

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(domain, config, null, DefaultSettings).ToString();

			// Assert - Verify main sense structure: headword first, then sense number, then gloss
			const string mainSenseHeadwordXpath = senseXpath + "/span[@class='headword-classified'][1]//span[@lang='fr']/a[text()='word']";
			const string mainSenseNumberXpath = senseXpath + "/span[@class='sensenumber' and text()='1' and preceding-sibling::span[@class='headword-classified']]";
			const string mainSenseGlossXpath = senseXpath + "//span[@lang='en' and text()='main gloss']";
			
			const string subSenseNumberXpath = senseXpath + "/span[@class='senses-2']//span[@class='sensenumber' and text()='1']";
			const string subSenseGlossXpath = senseXpath + "/span[@class='senses-2']//span[@lang='en' and text()='sub gloss']";

			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(mainSenseHeadwordXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(mainSenseNumberXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(mainSenseGlossXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseGlossXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForSemanticDomain_EmptyDomain_GeneratesNoSenses()
		{
			// Arrange
			var domain = CreateSemanticDomainWithSenses("1.1", "Universe"); // no senses
			var config = BuildSemanticDomainConfig();

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(domain, config, null, DefaultSettings).ToString();

			// Assert
			const string abbrXpath = domainAbbrXpath + "/span[@lang='en' and text()='1.1']";
			const string nameXpath = domainNameXpath + "/span[@lang='en' and text()='Universe']";

			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(abbrXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nameXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseXpath, 0);
		}

		[Test]
		public void GenerateXHTMLForSemanticDomain_FilteredPublication_ExcludesUnpublishedSenses()
		{
			// Arrange
			var publication = CreateTestPublication("Test Publication");
			var sense1 = CreateEntryWithSense("included", "included gloss");
			var sense2 = CreateEntryWithSense("excluded", "excluded gloss");
			sense1.Entry.DoNotPublishInRC.Clear();

			CollectionAssert.Contains(sense2.Entry.DoNotPublishInRC, publication);

			var domain = CreateSemanticDomainWithSenses("1.1", "Universe", sense1, sense2);
			var config = BuildSemanticDomainConfig();

			var decorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.DomainDataByFlid,
				Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries, publication);

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(domain, config, decorator, DefaultSettings).ToString();

			// Assert
			const string includedXpath = senseXpath + "//span[@lang='en' and text()='included gloss']";
			const string excludedXpath = senseXpath + "//span[@lang='en' and text()='excluded gloss']";

			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(includedXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(excludedXpath, 0);
		}

		[Test]
		public void GenerateXHTMLForSemanticDomain_AllSensesExcluded_GeneratesEmptyDomain()
		{
			// Arrange
			var publication = CreateTestPublication("Test Publication");
			var sense1 = CreateEntryWithSense("excluded1", "excluded gloss 1");
			var sense2 = CreateEntryWithSense("excluded2", "excluded gloss 2");
			sense1.Entry.DoNotPublishInRC.Add(publication);

			var domain = CreateSemanticDomainWithSenses("1.1", "Universe", sense1, sense2);
			var config = BuildSemanticDomainConfig();

			var decorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.DomainDataByFlid,
				Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries, publication);

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(domain, config, decorator, DefaultSettings).ToString();

			// Assert
			const string abbrXpath = domainAbbrXpath + "/span[@lang='en' and text()='1.1']";
			const string nameXpath = domainNameXpath + "/span[@lang='en' and text()='Universe']";

			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(abbrXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nameXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseXpath, 0);
		}

		#endregion Tests
	}
}
