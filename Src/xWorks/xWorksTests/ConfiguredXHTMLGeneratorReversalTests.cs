// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.TestUtilities;
using SIL.FieldWorks.Common.Framework;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using XCore;
using CXGTests = SIL.FieldWorks.XWorks.ConfiguredXHTMLGeneratorTests;

namespace SIL.FieldWorks.XWorks
{
	// ReSharper disable InconsistentNaming
	[TestFixture]
	public class ConfiguredXHTMLGeneratorReversalTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private int m_wsEn, m_wsFr;
		private static readonly StringComparison strComp = StringComparison.InvariantCulture;

		private FwXApp m_application;
		private FwXWindow m_window;
		private PropertyTable m_propertyTable;

		private StringBuilder XHTMLStringBuilder { get; set; }

		private ConfiguredXHTMLGenerator.GeneratorSettings DefaultSettings
		{
			get { return new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null); }
		}

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
				m_application?.Dispose();
				m_window?.Dispose();
				m_propertyTable?.Dispose();
			}
		}

		~ConfiguredXHTMLGeneratorReversalTests()
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

		[Test]
		public void GenerateXHTMLForEntry_LexemeFormConfigurationGeneratesCorrectResult()
		{
			var reversalFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalForm",
				DictionaryNodeOptions = CXGTests.GetWsOptionsForLanguages(new [] {"en"}),
				Label = "Reversal Form"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { reversalFormNode },
				FieldDescription = "ReversalIndexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingEnglishReversalEntry();
			//SUT
			string result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, DefaultSettings);
			const string frenchLexForm = "/div[@class='reversalindexentry']/span[@class='reversalform']/span[@lang='en' and text()='ReversalForm']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(frenchLexForm, 1);
		}

		#region PrimareyEntryReferenceTests
		// Xpath used by PrimaryEntryReference tests
		private const string referringSenseXpath = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']";
		private const string entryRefsXpath = referringSenseXpath + "/span[@class='mainentryrefs']";
		private const string entryRefXpath = entryRefsXpath + "/span[@class='mainentryref']";
		private const string entryRefTypeBit = "span[@class='entrytypes']/span[@class='entrytype']";
		private const string entryRefTypeXpath = entryRefsXpath + "/" + entryRefTypeBit;
		private const string primaryLexemeBit = "/span[@class='primarylexemes']/span[@class='primarylexeme']";
		private const string primaryEntryXpath = entryRefXpath + primaryLexemeBit;
		//private const string primaryEntryXpath = entryRefXpath + "/span[@class='primarylexemes']/span[@class='primarylexeme']";
		private const string refHeadwordXpath = primaryEntryXpath + "/span[@class='headword']/span[@lang='fr']/a[text()='parole']";

		[Test]
		public void GenerateXHTMLForEntry_PrimaryEntryReferencesWork_ComplexFormOfEntry()
		{
			var mainRevEntryNode = PreparePrimaryEntryReferencesConfigSetup();
			var reversalEntry = CreateInterestingEnglishReversalEntry("spokesmanRevForm", "porte-parole", "spokesman:gloss");
			var referringSense = reversalEntry.ReferringSenses.First();
			var paroleEntry = CXGTests.CreateInterestingLexEntry(Cache, "parole", "speech");
			paroleEntry.SummaryDefinition.SetAnalysisDefaultWritingSystem("summDefn");
			CXGTests.CreateComplexForm(Cache, paroleEntry, referringSense.Owner as ILexEntry, true);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(reversalEntry, mainRevEntryNode, null, DefaultSettings);
			const string headwordXpath = referringSenseXpath + "/span[@class='headword']/span[@lang='fr']//a[text()='porte-parole']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordXpath, 1);
			const string refTypeXpath = entryRefTypeXpath + "/span[@class='abbreviation']/span[@lang='en' and text()='comp. of']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(refTypeXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(refHeadwordXpath, 1);
			const string glossOrSummDefXpath = primaryEntryXpath + "/span[@class='glossorsummary']/span[@lang='en' and text()='summDefn']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(glossOrSummDefXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_PrimaryEntryReferencesWork_ComplexFormOfSense()
		{
			var mainRevEntryNode = PreparePrimaryEntryReferencesConfigSetup();
			var reversalEntry = CreateInterestingEnglishReversalEntry("spokesmanRevForm", "porte-parole", "spokesman:gloss");
			var referringSense = reversalEntry.ReferringSenses.First();
			var paroleEntry = CXGTests.CreateInterestingLexEntry(Cache, "parole", "speech");
			CXGTests.CreateComplexForm(Cache, paroleEntry.SensesOS[0], referringSense.Owner as ILexEntry, true);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(reversalEntry, mainRevEntryNode, null, DefaultSettings);
			const string refTypeXpath = entryRefTypeXpath + "/span[@class='abbreviation']/span[@lang='en' and text()='comp. of']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(refTypeXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(refHeadwordXpath, 1);
			const string glossOrSummDefXpath = primaryEntryXpath + "/span[@class='glossorsummary']/span[@lang='en' and text()='speech']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(glossOrSummDefXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_PrimaryEntryReferencesWork_VariantFormOfSense()
		{
			var mainRevEntryNode = PreparePrimaryEntryReferencesConfigSetup();
			var reversalEntry = CreateInterestingEnglishReversalEntry("speechRevForm", "parol", "speech:gloss");
			var variantEntry = reversalEntry.ReferringSenses.First().Owner as ILexEntry;
			var paroleEntry = CXGTests.CreateInterestingLexEntry(Cache, "parole", "speech");
			CXGTests.CreateVariantForm(Cache, paroleEntry.SensesOS[0], variantEntry, "Spelling Variant");
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(reversalEntry, mainRevEntryNode, null, DefaultSettings);
			const string refTypeXpath = entryRefTypeXpath + "/span[@class='abbreviation']/span[@lang='en' and text()='sp. var. of']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(refTypeXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(refHeadwordXpath, 1);
			const string glossOrSummDefXpath = primaryEntryXpath + "/span[@class='glossorsummary']/span[@lang='en' and text()='speech']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(glossOrSummDefXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_PrimaryEntryReferencesWork_VariantFormOfEntry()
		{
			var mainRevEntryNode = PreparePrimaryEntryReferencesConfigSetup();
			var reversalEntry = CreateInterestingEnglishReversalEntry("speechRevForm", "parol", "speech:gloss");
			var variantEntry = reversalEntry.ReferringSenses.First().Owner as ILexEntry;
			var paroleEntry = CXGTests.CreateInterestingLexEntry(Cache, "parole", "speech");
			paroleEntry.SummaryDefinition.SetAnalysisDefaultWritingSystem("summDefn");
			CXGTests.CreateVariantForm(Cache, paroleEntry, variantEntry, "Spelling Variant");
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(reversalEntry, mainRevEntryNode, null, DefaultSettings);
			const string refTypeXpath = entryRefTypeXpath + "/span[@class='abbreviation']/span[@lang='en' and text()='sp. var. of']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(refTypeXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(refHeadwordXpath, 1);
			const string glossOrSummDefXpath = primaryEntryXpath + "/span[@class='glossorsummary']/span[@lang='en' and text()='summDefn']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(glossOrSummDefXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_PrimaryEntryReferences_Ordered()
		{
			var mainRevEntryNode = PreparePrimaryEntryReferencesConfigSetup();

			var reversalEntry = CreateInterestingEnglishReversalEntry();
			var primaryEntry = reversalEntry.ReferringSenses.First().Entry;
			var refer1 = CXGTests.CreateInterestingLexEntry(Cache, "Component Entry", "CompEntry Sense");
			var refer2 = CXGTests.CreateInterestingLexEntry(Cache, "Variant Entry");
			var refer3 = CXGTests.CreateInterestingLexEntry(Cache, "CompSense Entry", "Component Sense").SensesOS.First();
			var refer4 = CXGTests.CreateInterestingLexEntry(Cache, "Invariant Entry");
			var refer5 = CXGTests.CreateInterestingLexEntry(Cache, "Variante Entrie");
			using (CXGTests.CreateComplexForm(Cache, refer3, primaryEntry, new Guid("00000000-0000-0000-cccc-000000000000"), true)) // Compound
			using (CXGTests.CreateVariantForm(Cache, refer2, primaryEntry, new Guid("00000000-0000-0000-bbbb-000000000000"), "Free Variant"))
			using (CXGTests.CreateComplexForm(Cache, refer1, primaryEntry, new Guid("00000000-0000-0000-aaaa-000000000000"), true)) // Compound
			using (CXGTests.CreateVariantForm(Cache, refer4, primaryEntry, new Guid("00000000-0000-0000-dddd-000000000000"), null)) // no Variant Type
			using (CXGTests.CreateVariantForm(Cache, refer5, primaryEntry, new Guid("00000000-0000-0000-eeee-000000000000"), "Spelling Variant"))
			{
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(reversalEntry, mainRevEntryNode, null, DefaultSettings); // SUT
				var assertIt = AssertThatXmlIn.String(result);
				assertIt.HasSpecifiedNumberOfMatchesForXpath(entryRefTypeXpath, 3); // should be one Complex Form Type and two Variant Types.
				const string headwordBit = "/span[@class='headword']/span[@lang='fr']/a[text()='{1}']";
				const string entryRefWithSiblingXpath = entryRefsXpath + "/span[@class='mainentryref' and preceding-sibling::";
				const string typeAndHeadwordXpath = entryRefWithSiblingXpath
					+ entryRefTypeBit + "/span[@class='abbreviation']/span[@lang='en' and text()='{0}']]" + primaryLexemeBit + headwordBit;
				var adjacentHeadwordXpath = entryRefWithSiblingXpath
					+ "span[@class='mainentryref']" + primaryLexemeBit + headwordBit.Replace("{1}", "{0}") + "]" + primaryLexemeBit + headwordBit;
				// check for proper headings on each referenced headword
				assertIt.HasSpecifiedNumberOfMatchesForXpath(string.Format(typeAndHeadwordXpath, "comp. of", "Component Entry"), 1);
				assertIt.HasSpecifiedNumberOfMatchesForXpath(string.Format(adjacentHeadwordXpath, "Component Entry", "CompSense Entry"), 1); // ordered within heading
				assertIt.HasSpecifiedNumberOfMatchesForXpath(string.Format(typeAndHeadwordXpath, "fr. var. of", "Variant Entry"), 1);
				assertIt.HasSpecifiedNumberOfMatchesForXpath(string.Format(typeAndHeadwordXpath, "sp. var. of", "Variante Entrie"), 1);
				// verify there is no heading on the typeless variant
				assertIt.HasNoMatchForXpath(string.Format(entryRefWithSiblingXpath + "span]" + primaryLexemeBit + headwordBit, null, "Invariant Entry"),
					message: "Invariant Entry is the only typeless entry ref; it should not have any preceding siblings (Types or other Entry Refs)");
			}
		}

		private static ConfigurableDictionaryNode PreparePrimaryEntryReferencesConfigSetup()
		{
			var abbrNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Abbreviation",
				DictionaryNodeOptions = CXGTests.GetWsOptionsForLanguages(new[] {"analysis"})
			};
			var typeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "EntryTypes",
				Children = new List<ConfigurableDictionaryNode> {abbrNode},
			};
			var refHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = CXGTests.GetWsOptionsForLanguages(new[] {"vernacular"}),
				Label = "Referenced Headword"
			};
			var glossOrSummaryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "GlossOrSummary",
				DictionaryNodeOptions = CXGTests.GetWsOptionsForLanguages(new[] {"analysis"}),
				Label = "Gloss (or Summary Definition)"
			};
			var primaryEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PrimarySensesOrEntries",
				CSSClassNameOverride = "primarylexemes",
				Children = new List<ConfigurableDictionaryNode> {refHeadwordNode, glossOrSummaryNode},
				Label = "Primary Entry(s)"
			};
			var primaryEntryRefNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MainEntryRefs",
				Children = new List<ConfigurableDictionaryNode> {typeNode, primaryEntryNode},
				Label = "Primary Entry References"
			};
			var headWordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalName",
				CSSClassNameOverride = "headword",
				Label = "Referenced Headword",
				DictionaryNodeOptions = CXGTests.GetWsOptionsForLanguages(new[] {"vernacular"})
			};
			var referencedSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReferringSenses",
				Children = new List<ConfigurableDictionaryNode> {headWordNode, primaryEntryRefNode},
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberingStyle = "Dictionary-SenseNumber",
					DisplayEachSenseInAParagraph = false,
					ShowSharedGrammarInfoFirst = false
				},
				Label = "Referenced Senses"
			};
			var mainRevEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> {referencedSensesNode},
				FieldDescription = "ReversalIndexEntry",
				CSSClassNameOverride = "reversalindexentry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainRevEntryNode);
			return mainRevEntryNode;
		}
		#endregion PrimareyEntryReferenceTests

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfNoPreviousHeader()
		{
			var entry = CreateInterestingEnglishReversalEntry();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				string last = null;
				XHTMLWriter.WriteStartElement("TestElement");
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, DefaultSettings));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/span[@class='letter' and @lang='en' and text()='R r']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
				Assert.AreEqual("r", last, "should have updated the last letter header");
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfPreviousHeaderDoesNotMatch()
		{
			var entry = CreateInterestingEnglishReversalEntry();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, DefaultSettings));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/span[@class='letter' and @lang='en' and text()='R r']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesNoHeaderIfPreviousHeaderDoesMatch()
		{
			var entry = CreateInterestingEnglishReversalEntry();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, DefaultSettings);
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, DefaultSettings);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/span[@class='letter' and @lang='en' and text()='R r']";
				const string proveOnlyOneHeader = "//div[@class='letHead']/span[@class='letter']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(proveOnlyOneHeader, 1);
			}
		}


		[Test]
		public void GenerateXHTMLForEntry_ReversalStringGeneratesContent()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalForm",
				Label = "Form",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption { Id = "fr"}
					},
					DisplayWritingSystemAbbreviations = false
				}
			};
			var reversalNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { formNode },
				FieldDescription = "ReversalIndexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(reversalNode);
			var rie = CreateInterestingFrenchReversalEntry();
			var entryHeadWord = rie.ReferringSenses.First().Entry.HeadWord;

			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(rie, reversalNode, null, DefaultSettings);
			var reversalFormDataPath = string.Format("/div[@class='reversalindexentry']/span[@class='reversalform']/span[text()='{0}']",
				TsStringUtils.Compose(rie.LongName));
			var entryDataPath = string.Format("//span[text()='{0}']", entryHeadWord.get_NormalizedForm(FwNormalizationMode.knmNFC).Text);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(reversalFormDataPath, 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(entryDataPath);
		}

		[Test]
		public void GenerateXHTMLForEntry_SenseNumbersGeneratedForMultipleReferringSenses()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalName",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption {Id = "vernacular"}
					}
				}
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption { Id = "reversal" }
					}
				}
			};
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReferringSenses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" },
				Children = new List<ConfigurableDictionaryNode> {headwordNode, glossNode}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption {Id = "en"}
					},
					DisplayWritingSystemAbbreviations = false
				},
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingEnglishReversalEntry();
			AddSenseToReversaEntry(testEntry, "second gloss", m_wsEn, Cache);
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, DefaultSettings);
			const string senseNumberOne = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
			const string senseNumberTwo = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss']";
			//This assert is dependent on the specific entry data created in CreateInterestingEnglishReversalEntry
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);

			const string headwordOne = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/span[@lang='fr' and child::span[@lang='fr']/a[text()='1']]/span[@lang='fr' and a[text()='Citation']]";
			const string headwordTwo = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/span[@lang='fr' and child::span[@lang='fr']/a[text()='2']]/span[@lang='fr' and a[text()='Citation']]";
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(headwordOne, 1);
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(headwordTwo, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_VernacularFormWithSubSenses()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalName",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption {Id = "vernacular"}
					}
				}
			};
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "reversal" }
				}
			};
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReferringSenses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" },
				Children = new List<ConfigurableDictionaryNode> { headwordNode, glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption {Id = "en"}
					},
					DisplayWritingSystemAbbreviations = false
				},
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingEnglishReversalEntry();
			AddSingleSubSenseToSense(testEntry, "second gloss", m_wsEn, Cache);
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, DefaultSettings);
			// REVIEW (Hasso) 2016.03: we should probably do something about the leading space in the Sense Number Run, as it is currently in addition to the "between" space.
			const string subSenseOneOne = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/span/span/a[text()='1.1']";
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(subSenseOneOne, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_VernacularFormWithSubSensesinReversalSubEntry()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalName",
				Label = "Headword",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption {Id = "vernacular"}
					}
				}
			};
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "reversal" }
				}
			};
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReferringSenses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d",NumberEvenASingleSense = true},
				Children = new List<ConfigurableDictionaryNode> { headwordNode, glossNode }
			};
			var subEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SubentriesOS",
				CSSClassNameOverride = "subentries",
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions {DisplayEachInAParagraph = true},
				Children = new List<ConfigurableDictionaryNode> {formNode}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption {Id = "en"}
					},
					DisplayWritingSystemAbbreviations = false
				},
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { subEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingEnglishSubReversalEntryWithSubSense();
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, DefaultSettings);
			const string subSenseOneOne = "/div[@class='reversalindexentry']/span[@class='subentries']/span[@class='subentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/span/span/a[text()='1.1']";
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(subSenseOneOne, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_SameGramInfoCollapsesOnDemand()
		{
			var defOrGlossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "DefinitionOrGloss",
				Between = " ",
				After = " ",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					DisplayWritingSystemAbbreviations = false,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption> { new DictionaryNodeListOptions.DictionaryNodeOption { Id="reversal" } }
				},
				Children = new List<ConfigurableDictionaryNode>()
			};
			var catInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				CSSClassNameOverride = "partofspeech",
				Between = " ",
				After = " ",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					DisplayWritingSystemAbbreviations = false,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption> { new DictionaryNodeListOptions.DictionaryNodeOption { Id="reversal" } }
				},
				Children = new List<ConfigurableDictionaryNode>()
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "morphosyntaxanalysis",
				After = " ",
				Style = "Dictionary-Contrasting",
				Children = new List<ConfigurableDictionaryNode> { catInfoNode }
			};
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalName",
				Between = " ",
				After = " ",
				StyleType = ConfigurableDictionaryNode.StyleTypes.Character,
				Style = "Reversal-Vernacular",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
					DisplayWritingSystemAbbreviations = false,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption> { new DictionaryNodeListOptions.DictionaryNodeOption { Id = "vernacular" } }
				},
				Children = new List<ConfigurableDictionaryNode>()
			};
			var vernFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReferringSenses",
				Between = "; ",
				After = " ",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberStyle = "Dictionary-SenseNumber",
					AfterNumber = ") ",
					NumberingStyle = "%d",
					NumberEvenASingleSense = false,
					ShowSharedGrammarInfoFirst = true,
					DisplayEachSenseInAParagraph = false
				},
				Children = new List<ConfigurableDictionaryNode> { headwordNode, gramInfoNode, defOrGlossNode }
			};
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalForm",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					DisplayWritingSystemAbbreviations = false,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption> { new DictionaryNodeListOptions.DictionaryNodeOption { Id = "reversal" } }
				},
				Children = new List<ConfigurableDictionaryNode>()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { formNode, vernFormNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var settings = DefaultSettings;

			var noun = CreatePartOfSpeech("noun", "n");
			var verb = CreatePartOfSpeech("verb", "v");
			var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(m_wsEn);

			var entry1 = CXGTests.CreateInterestingLexEntry(Cache);
			entry1.CitationForm.set_String(m_wsFr, "premier");
			entry1.SensesOS.First().Gloss.set_String(m_wsEn, "first");
			var msa1 = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry1.MorphoSyntaxAnalysesOC.Add(msa1);
			msa1.PartOfSpeechRA = noun;
			entry1.SensesOS.First().MorphoSyntaxAnalysisRA = msa1;

			var entry2 = CXGTests.CreateInterestingLexEntry(Cache);
			entry2.CitationForm.set_String(m_wsFr, "primary");
			entry2.SensesOS.First().Gloss.set_String(m_wsEn, "first");
			var msa2 = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry2.MorphoSyntaxAnalysesOC.Add(msa2);
			msa2.PartOfSpeechRA = noun;
			entry2.SensesOS.First().MorphoSyntaxAnalysisRA = msa2;

			var testEntry = revIndex.FindOrCreateReversalEntry("first");
			entry1.SensesOS.First().ReversalEntriesRC.Add(testEntry);
			entry2.SensesOS.First().ReversalEntriesRC.Add(testEntry);

			var xhtml = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
			// check that the sense gram info appears once before the rest of the sense information.
			Assert.IsNotNullOrEmpty(xhtml);
			const string sharedGramInfo = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sharedgrammaticalinfo']/span[@class='morphosyntaxanalysis']/span[@class='partofspeech']/span[@lang='en' and text()='n']";
			const string separateGramInfo = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='morphosyntaxanalysis']/span[@class='partofspeech']/span[@lang='en']";
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(sharedGramInfo, 1);
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(separateGramInfo, 0);

			var msa2a = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry2.MorphoSyntaxAnalysesOC.Add(msa2a);
			msa2a.PartOfSpeechRA = verb;
			entry2.SensesOS.First().MorphoSyntaxAnalysisRA = msa2a;
			xhtml = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
			// check that the sense gram info appears separately for both senses.
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(sharedGramInfo, 0);
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(separateGramInfo, 2);
		}

		private IPartOfSpeech CreatePartOfSpeech(string name, string abbr)
		{
			var factory = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			var pos = factory.Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			pos.Name.set_String(m_wsEn, name);
			pos.Abbreviation.set_String(m_wsEn, abbr);
			return pos;
		}

		private IReversalIndexEntry CreateInterestingFrenchReversalEntry()
		{
			var entry = CXGTests.CreateInterestingLexEntry(Cache);
			var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(m_wsFr);
			var riEntry = revIndex.FindOrCreateReversalEntry("intéressant");
			entry.SensesOS.First().ReversalEntriesRC.Add(riEntry);
			return riEntry;
		}

		private IReversalIndexEntry CreateInterestingEnglishReversalEntry(string reversalForm = "ReversalForm",
			string vernacularHeadword = "Citation", string analysisGloss = "gloss")
		{
			var entry = CXGTests.CreateInterestingLexEntry(Cache, vernacularHeadword, analysisGloss);
			var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(m_wsEn);
			var riEntry = revIndex.FindOrCreateReversalEntry(reversalForm);
			entry.SensesOS.First().ReversalEntriesRC.Add(riEntry);
			return riEntry;
		}

		private IReversalIndexEntry CreateInterestingEnglishSubReversalEntryWithSubSense()
		{
			var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(m_wsEn);
			var riEntry = revIndex.FindOrCreateReversalEntry("MainReversal");
			var risubEntry = revIndex.FindOrCreateReversalEntry("SubReversal");
			riEntry.SubentriesOS.Add(risubEntry);
			AddSingleSubSenseToSense(risubEntry, "subgloss", m_wsEn, Cache);
			return riEntry;
		}

		private static void AddSenseToReversaEntry(IReversalIndexEntry riEntry, string gloss, int wsId, LcmCache cache)
		{
			var entry = CXGTests.CreateInterestingLexEntry(cache);
			entry.SensesOS.First().ReversalEntriesRC.Add(riEntry);
			entry.SensesOS[0].Gloss.set_String(wsId, gloss);
		}

		private static void AddSingleSubSenseToSense(IReversalIndexEntry riEntry, string gloss, int wsId, LcmCache cache)
		{
			CreateSubsenseModel();
			var entry = CXGTests.CreateInterestingLexEntry(cache);
			var sense = entry.SensesOS.First();
			sense.Gloss.set_String(wsId, TsStringUtils.MakeString(gloss, wsId));
			var subSensesOne = sense.Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			sense.SensesOS.Add(subSensesOne);
			var subGloss = "subgloss ";
			subSensesOne.Gloss.set_String(wsId, TsStringUtils.MakeString(subGloss + "1.1", wsId));
			entry.SensesOS[0].SensesOS[0].Gloss.set_String(wsId, subGloss);
			entry.SensesOS.First().SensesOS[0].ReversalEntriesRC.Add(riEntry);
		}

		private static void CreateSubsenseModel()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en" }
				}
			};
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%d",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};
			var DictionaryNodeSubSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%O",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Label = "Subsenses",
				DictionaryNodeOptions = DictionaryNodeSubSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
		}
	}
}
