// Copyright (c) 2015-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Palaso.TestUtilities;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	// ReSharper disable InconsistentNaming
	[TestFixture]
	public class ConfiguredXHTMLGeneratorReversalTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private int m_wsEn, m_wsFr;

		private FwXApp m_application;
		private FwXWindow m_window;
		private Mediator m_mediator;

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
			m_mediator = m_window.Mediator;
			// Set up the mediator to look as if we are working in the Reversal Index area
			m_mediator.PropertyTable.SetProperty("ToolForAreaNamed_lexicon", "reversalEditComplete");
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
				if (m_mediator != null)
					m_mediator.Dispose();
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
				Label = "Reversal Form"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { reversalFormNode },
				FieldDescription = "ReversalIndexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingEnglishReversalEntry();
			var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, false, false, null);
			//SUT
			string result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string frenchLexForm = "/div[@class='reversalindexentry']/span[@class='reversalform']/span[@lang='en' and text()='ReversalForm']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(frenchLexForm, 1);
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfNoPreviousHeader()
		{
			var entry = CreateInterestingEnglishReversalEntry();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				string last = null;
				XHTMLWriter.WriteStartElement("TestElement");
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache));
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
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache));
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
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache);
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache);
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
			var rie = CreateInterestingFrenchReversalEntry() as IReversalIndexEntry;
			var entryHeadWord = rie.ReferringSenses.First().Entry.HeadWord;

			var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(rie, reversalNode, null, settings);
			var reversalFormDataPath = string.Format("/div[@class='reversalindexentry']/span[@class='reversalform']/span[text()='{0}']", rie.LongName);
			var entryDataPath = string.Format("//span[text()='{0}']", entryHeadWord.Text);
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
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions()
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
					DisplayWritingSystemAbbreviations = false,
				},
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingEnglishReversalEntry();
			AddSenseToReversaEntry(testEntry, "second gloss", m_wsEn, Cache);
			var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, false, false, null);
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
			const string senseNumberOne = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
			const string senseNumberTwo = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss']";
			//This assert is dependent on the specific entry data created in CreateInterestingEnglishReversalEntry
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);

			const string headwordOne = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/span[@lang='fr' and following-sibling::span[@lang='fr']/a[text()='1']]/a[text()='Citation']";
			const string headwordTwo = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/span[@lang='fr' and following-sibling::span[@lang='fr']/a[text()='2']]/a[text()='Citation']";
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
					DisplayWritingSystemAbbreviations = false,
				},
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingEnglishReversalEntry();
			AddSingleSubSenseToSense(testEntry, "second gloss", m_wsEn, Cache);
			var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, false, false, null);
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
			// REVIEW (Hasso) 2016.03: we should probably do something about the leading space in the Sense Number Run, as it is currently in addition to the "between" space.
			const string subSenseOneOne = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/span/a[text()=' 1.1']";
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
				DictionaryNodeOptions = new DictionaryNodeComplexFormOptions {DisplayEachComplexFormInAParagraph = true},
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
					DisplayWritingSystemAbbreviations = false,
				},
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { subEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingEnglishSubReversalEntryWithSubSense();
			var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, false, false, null);
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
			const string subSenseOneOne = "/div[@class='reversalindexentry']/span[@class='subentries']/span[@class='subentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/span/a[text()=' 1.1']";
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
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					DisplayWritingSystemAbbreviations = false,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption> { new DictionaryNodeListOptions.DictionaryNodeOption { Id="reversal", IsEnabled=true } }
				},
				Children = new List<ConfigurableDictionaryNode> { }
			};
			var catInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				CSSClassNameOverride = "partofspeech",
				Between = " ",
				After = " ",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					DisplayWritingSystemAbbreviations = false,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption> { new DictionaryNodeListOptions.DictionaryNodeOption { Id="reversal", IsEnabled=true } }
				},
				Children = new List<ConfigurableDictionaryNode> { }
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "morphosyntaxanalysis",
				After = " ",
				Style = "Dictionary-Contrasting",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { catInfoNode }
			};
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalName",
				Between = " ",
				After = " ",
				StyleType = ConfigurableDictionaryNode.StyleTypes.Character,
				Style = "Reversal-Vernacular",
				IsEnabled = true,
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
					DisplayWritingSystemAbbreviations = false,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption> { new DictionaryNodeListOptions.DictionaryNodeOption { Id = "vernacular", IsEnabled=true } }
				},
				Children = new List<ConfigurableDictionaryNode> { }
			};
			var vernFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReferringSenses",
				Between = "; ",
				After = " ",
				IsEnabled = true,
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
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					DisplayWritingSystemAbbreviations = false,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption> { new DictionaryNodeListOptions.DictionaryNodeOption { Id = "reversal", IsEnabled=true } }
				},
				Children = new List<ConfigurableDictionaryNode> { }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalIndexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { formNode, vernFormNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, false, false, null);

			var noun = CreatePartOfSpeech("noun", "n");
			var verb = CreatePartOfSpeech("verb", "v");
			var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(m_wsEn);

			var entry1 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			entry1.CitationForm.set_String(m_wsFr, "premier");
			entry1.SensesOS.First().Gloss.set_String(m_wsEn, "first");
			var msa1 = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry1.MorphoSyntaxAnalysesOC.Add(msa1);
			msa1.PartOfSpeechRA = noun;
			entry1.SensesOS.First().MorphoSyntaxAnalysisRA = msa1;

			var entry2 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
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
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(m_wsFr);
			var riEntry = revIndex.FindOrCreateReversalEntry("intéressant");
			entry.SensesOS.First().ReversalEntriesRC.Add(riEntry);
			return riEntry;
		}

		private IReversalIndexEntry CreateInterestingEnglishReversalEntry()
		{
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(m_wsEn);
			var riEntry = revIndex.FindOrCreateReversalEntry("ReversalForm");
			entry.SensesOS.First().ReversalEntriesRC.Add(riEntry);
			return riEntry;
		}
		private IReversalIndexEntry CreateInterestingEnglishSubReversalEntryWithSubSense()
		{
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(m_wsEn);
			var riEntry = revIndex.FindOrCreateReversalEntry("MainReversal");
			var risubEntry = revIndex.FindOrCreateReversalEntry("SubReversal");
			riEntry.SubentriesOS.Add(risubEntry);
			AddSingleSubSenseToSense(risubEntry, "subgloss", m_wsEn, Cache);
			//entry.SensesOS.First().ReversalEntriesRC.Add(risubEntry);
			return riEntry;
		}

		private static void AddSenseToReversaEntry(IReversalIndexEntry riEntry, string gloss, int wsId, FdoCache cache)
		{
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(cache);
			entry.SensesOS.First().ReversalEntriesRC.Add(riEntry);
			entry.SensesOS[0].Gloss.set_String(wsId, gloss);
		}

		private static void AddSingleSubSenseToSense(IReversalIndexEntry riEntry, string gloss, int wsId, FdoCache cache)
		{
			CreateSubsenseModel(cache);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(cache);
			var sense = entry.SensesOS.First();
			sense.Gloss.set_String(wsId, cache.TsStrFactory.MakeString(gloss, wsId));
			var subSensesOne = sense.Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			sense.SensesOS.Add(subSensesOne);
			var subGloss = "subgloss ";
			subSensesOne.Gloss.set_String(wsId, cache.TsStrFactory.MakeString(subGloss + "1.1", wsId));
			entry.SensesOS[0].SensesOS[0].Gloss.set_String(wsId, subGloss);
			entry.SensesOS.First().SensesOS[0].ReversalEntriesRC.Add(riEntry);
		}

		private static void CreateSubsenseModel(FdoCache Cache)
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
